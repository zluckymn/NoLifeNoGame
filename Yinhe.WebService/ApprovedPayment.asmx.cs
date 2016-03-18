using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using Yinhe.ProcessingCenter;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using System.Xml.Serialization;

namespace Yinhe.WebService
{
    /// <summary>
    /// ApprovedPayment 的摘要说明
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // 若要允许使用 ASP.NET AJAX 从脚本中调用此 Web 服务，请取消对下行的注释。
    // [System.Web.Script.Services.ScriptService]
    public class ApprovedPayment : System.Web.Services.WebService
    {

        [WebMethod]
        public string HelloWorld()
        {
            return "Hello World";
        }

        /// <summary>
        /// 获取审批通过的费用支付任务审批信息
        /// </summary>
        /// <returns></returns>
        [WebMethod]
        public List<ApprovalInfo> GetApprovedPaymentInfo()
        {
            DataOperation dataOp = new DataOperation();
            List<ApprovalInfo> list = new List<ApprovalInfo>();
            List<BsonDocument> paymentList = GetApprovedPaymentTask();
            
            list = GenerateApprovalInfo(paymentList);
            return list;
        }
       
        #region 获取已通过审批的费用支付任务以及每笔支付任务
        /// <summary>
        /// 获取已通过审批的费用支付任务以及每笔支付任务
        /// </summary>
        /// <returns></returns>
        private List<BsonDocument> GetApprovedPaymentTask()
        {
            DataOperation dataOp = new DataOperation();
            List<BsonDocument> taskList = new List<BsonDocument>();
            var allProj = dataOp.FindAllByQuery("XH_DesignManage_Project", Query.EQ("isProj", "1"));

            var allPayPlanQuery=Query.And(
                Query.EQ("isContractPlan","1"),
                Query.In("projId",allProj.Select(p=>(BsonValue)p.Text("projId")))
                );
            var allPayPlan=dataOp.FindAllByQuery("XH_DesignManage_Plan",allPayPlanQuery);

            var instanceQuery=Query.And(
                Query.EQ("tableName","XH_DesignManage_Task"),
                Query.EQ("referFieldName","taskId"),
                Query.EQ("instanceStatus","1")
                );
            var instance = dataOp.FindAllByQuery("BusFlowInstance", instanceQuery);

            var payTaskQuery=Query.And(
                Query.EQ("nodeTypeId", ((int)ConcernNodeType.FeePayment).ToString()),
                Query.In("taskId", instance.Select(p => (BsonValue)p.Text("referFieldValue"))),
                Query.In("planId",allPayPlan.Select(p=>(BsonValue)p.Text("planId")))
                );
            var payTask = dataOp.FindAllByQuery("XH_DesignManage_Task",payTaskQuery);

            var allPayTask = dataOp.FindAllByQuery("XH_DesignManage_Task",
                    Query.And(
                        Query.EQ("nodeTypeId", ((int)ConcernNodeType.FeePayment).ToString()),
                        Query.In("planId", allPayPlan.Select(p => (BsonValue)p.Text("planId")))
                    )
                );

            var eachPayTaskQuery = Query.And(
                Query.In("nodePid", allPayTask.Select(p => (BsonValue)p.Text("taskId"))),
                Query.In("taskId", instance.Select(p => (BsonValue)p.Text("referFieldValue")))
                );
            var eachPayTask = dataOp.FindAllByQuery("XH_DesignManage_Task",eachPayTaskQuery);

            taskList = payTask.Union(eachPayTask).ToList();
            var tempList = from p in taskList
                           join q in instance on p.Text("taskId") equals q.Text("referFieldValue")
                           select new
                           {
                               task = p,
                               date = q.Date("updateDate")
                           };
            taskList = tempList.OrderByDescending(p => p.date).Select(p => p.task).ToList();
            return taskList;
        }
        #endregion

        #region 获取审批详细信息
        /// <summary>
        /// 获取审批详细信息
        /// </summary>
        /// <param name="paytask"></param>
        /// <returns></returns>
        public List<ApprovalInfo> GenerateApprovalInfo(List<BsonDocument> taskList)
        {
            
            DataOperation dataOp=new DataOperation();
            var list = new List<ApprovalInfo>();
            if (taskList.Count() == 0)
            {
                return list;
            }
            foreach (var payTask in taskList)
            {
                ApprovalInfo info = new ApprovalInfo();
                info.BaseInfo = new BaseInfo();
                info.Extentions = new List<ExtensionInfo>();
                info.Log = new List<ApprovalLog>();
                var instance=dataOp.FindOneByQuery("BusFlowInstance",
                        Query.And(
                            Query.EQ("tableName", "XH_DesignManage_Task"),
                            Query.EQ("referFieldName", "taskId"),
                            Query.EQ("referFieldValue", payTask.Text("taskId"))
                        )
                    );
                if (instance == null) continue;
                var parentTask=dataOp.FindOneByQuery("XH_DesignManage_Task",Query.EQ("taskId",payTask.Text("nodePid")));

                #region 设置基本属性
                //获取任务名称
                if (string.IsNullOrEmpty(payTask.Text("paymentDisplayName")))
                {
                    info.BaseInfo.Name = payTask.Text("name");
                }
                else
                {
                    info.BaseInfo.Name = payTask.Text("paymentDisplayName");
                }
                if (parentTask != null && parentTask.Int("nodeTypeId") == (int)ConcernNodeType.FeePayment)
                {
                    var childTasks = dataOp.FindAllByQuery("XH_DesignManage_Task", Query.EQ("nodePid", parentTask.Text("taskId"))).OrderBy(p => p.Int("nodeOrder")).ToList();
                    var index = childTasks.IndexOf(payTask) + 1;
                    info.BaseInfo.Name = string.Format("{0}第{1}笔支付", parentTask.Text("name"), index);
                }
                var appUser = dataOp.FindOneByQuery("SysUser", Query.EQ("userId", instance.Text("approvalUserId")));
                if (appUser != null)
                {
                    info.BaseInfo.AppUser = appUser.Text("name");
                }
                //获取流程发起人公司部门信息
                string approvalUserId = instance.String("approvalUserId");
                BsonDocument userOrgPost = dataOp.FindOneByQuery("UserOrgPost", Query.EQ("userId", approvalUserId));
                BsonDocument orgPost = dataOp.FindOneByQuery("OrgPost", Query.EQ("postId", userOrgPost.String("postId")));
                BsonDocument org = dataOp.FindOneByQuery("Organization", Query.EQ("orgId", orgPost.String("orgId")));
                info.BaseInfo.Org = org.Text("name");
                info.BaseInfo.StartTime = instance.ShortDate("createDate");
                info.BaseInfo.ProjectName = instance.String("projName");
                info.BaseInfo.ProfType = instance.Text("profType");
                info.BaseInfo.ProjectScale = instance.Text("projScale");
                info.BaseInfo.ApprovedAmount = instance.Text("approvedAmount");
                //获取总部经办人
                List<BsonDocument> userRels = dataOp.FindAllByQuery("FlowInstanceUserRel", Query.EQ("instanceId", instance.Text("flowInstanceId"))).ToList();
                List<string> userIds = userRels.Select(s => s.String("userId")).ToList();
                List<BsonDocument> users = dataOp.FindAllByQuery("SysUser", Query.In("userId", TypeConvert.StringListToBsonValueList(userIds))).ToList();
                info.BaseInfo.Transactor = string.Join(",", users.Select(p => p.Text("name")));
                #endregion

                #region 设置扩展属性
                //获取流程的扩展属性
                List<string> attrIds = dataOp.FindAllByQuery("ExtensionAttrRel", Query.EQ("flowId", instance.Text("flowId"))).Select(s => s.String("attrId")).ToList();
                List<BsonDocument> attrs = dataOp.FindAllByQuery("ExtensionAttr", Query.In("attrId", TypeConvert.StringListToBsonValueList(attrIds))).ToList();
                foreach (var attr in attrs)
                {
                    ExtensionInfo extInfo = new ExtensionInfo();
                    string attrId = attr.String("attrId");//扩展属性Id
                    BsonElement element = instance.Elements.Where(s => s.Name == attrId).SingleOrDefault();
                    BsonValue val = null;
                    if (element != null)
                    {
                        val = element.Value;
                        extInfo.AttrName = attr.Text("name");
                        extInfo.AttrValue = val.ToString();
                        info.Extentions.Add(extInfo);
                    }
                }
                #endregion

                #region 设置审批记录
                var queryStr = string.Format("flowInstanceId={0}", instance.Text("flowInstanceId"));
                var logList = dataOp.FindAllByQueryStr("BusFlowTrace", queryStr).Where(c => c.Int("traceType") == 2 || c.Int("traceType") == 6).ToList();
                var stepUserIdList = new List<int>();
                //判断流程是否已经发起

                var hitAllUser = string.Empty;
                var hitShortUser = string.Empty;

                 foreach (var log in logList)
                 {
                     var AllUserString = new List<string>();
                     var ShortUserString = new List<string>();
                     var action = log.SourceBson("actId");
                     var form = log.SourceBson("formId");
                     var content = form != null ? form.Text("content") : string.Empty;
                     var actionTypeName = action != null ? action.SourceBsonField("actTypeId", "name") : "系统自动执行";
                     var result = action != null ? action.Text("name") : string.Empty;
                     var preStep = log.SourceBson("preStepId");
                     var preStepName = preStep != null ? preStep.SourceBsonField("flowPosId", "name") : string.Empty;
                     var helper = new Yinhe.ProcessingCenter.BusinessFlow.FlowInstanceHelper();
                     var curFlowInstance1 = log.SourceBson("flowInstanceId");
                     if (curFlowInstance1 != null && log.Int("flowInstanceId") != 0)
                     {
                         stepUserIdList = helper.GetFlowInstanceAvaiableStepUser(curFlowInstance1.Int("flowId"), log.Int("flowInstanceId"), preStep.Int("stepId"));
                     }
                     var userList = dataOp.FindAllByKeyValList("SysUser", "userId", stepUserIdList.Select(c => c.ToString()).ToList());
                     var userStr = string.Join(",", userList.Select(c => c.Text("name")).ToArray());
                     ShortUserString.Add(StringExtension.CutStr(userStr, 8, "..."));
                     AllUserString.Add(userStr);
                     hitShortUser = string.Join(@"<br>", ShortUserString.ToArray());
                     hitAllUser = string.Join(@"<br>", AllUserString.ToArray());
                     switch (log.Int("traceType"))
                     {
                         case -1: actionTypeName = "重启流程"; result = "重启流程"; break;
                         case 0: result = "启动流程"; break;
                         case 1: result = "系统自动执行"; break;
                         case 3: actionTypeName = "执行回滚操作"; result = "回滚"; break;
                         case 5: actionTypeName = "废弃流程"; result = "流程结束"; break;
                         case 6: actionTypeName = "转办"; result = log.Text("result"); break;
                         default: break;
                     }
                     ApprovalLog appLog = new ApprovalLog();
                     appLog.Position = !string.IsNullOrEmpty(preStepName) ? preStepName : string.Empty;
                     appLog.UserName = log.CreateUserName();
                     appLog.Time = log.ShortDate("createDate");
                     appLog.Content = string.IsNullOrEmpty(content) ? log.Text("remark") : content;
                     appLog.Action = actionTypeName;
                     appLog.Result = result;
                     info.Log.Add(appLog);
                }
                
                #endregion

                list.Add(info);
            }
            return list;
        }
        #endregion
    }
        

    #region 审批单定义
    public class ApprovalInfo
    {
        /// <summary>
        /// 基本参数
        /// </summary>
        public BaseInfo BaseInfo { get; set; }
        /// <summary>
        /// 扩展属性
        /// </summary>
        public List<ExtensionInfo> Extentions { get; set; }
        /// <summary>
        /// 审批记录
        /// </summary>
        public List<ApprovalLog> Log { get; set; }
    }

    #endregion

    #region 基本参数类定义
    /// <summary>
    /// 基本参数
    /// </summary>
    public class BaseInfo
    {
        /// <summary>
        /// 审批任务名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 发起人
        /// </summary>
        public string AppUser { get; set; }
        /// <summary>
        /// 发起人部门
        /// </summary>
        public string Org { get; set; }
        /// <summary>
        /// 流水号
        /// </summary>
        public string Num { get; set; }
        /// <summary>
        /// 发起时间
        /// </summary>
        public string StartTime { get; set; }
        /// <summary>
        /// 通过时间
        /// </summary>
        public string EndTime { get; set; }
        /// <summary>
        /// 项目名称
        /// </summary>
        public string ProjectName { get; set; }
        /// <summary>
        /// 物业类型
        /// </summary>
        public string ProfType { get; set; }
        /// <summary>
        /// 项目规模
        /// </summary>
        public string ProjectScale { get; set; }
        /// <summary>
        /// 总部经办人
        /// </summary>
        public string Transactor { get; set; }
        /// <summary>
        /// 审批金额
        /// </summary>
        public string ApprovedAmount { get; set; }
    }
    #endregion

    #region 扩展属性类定义
    public class ExtensionInfo
    {
        /// <summary>
        /// 扩展属性名
        /// </summary>
        public string AttrName { get; set; }
        /// <summary>
        /// 扩展属性值
        /// </summary>
        public string AttrValue { get; set; }
    }
    #endregion

    #region 审批记录类定义
    public class ApprovalLog
    {
        /// <summary>
        /// 审批人流程岗位
        /// </summary>
        public string Position { get; set; }
        /// <summary>
        /// 审批人
        /// </summary>
        public string UserName { get; set; }
        /// <summary>
        /// 处理时间
        /// </summary>
        public string Time { get; set; }
        /// <summary>
        /// 处理意见
        /// </summary>
        public string Content { get; set; }
        /// <summary>
        /// 处理动作
        /// </summary>
        public string Action { get; set; }
        /// <summary>
        /// 处理结果
        /// </summary>
        public string Result { get; set; }
    }
    #endregion  
    
    
}
