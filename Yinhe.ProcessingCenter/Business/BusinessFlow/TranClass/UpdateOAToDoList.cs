using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using Yinhe.ProcessingCenter.DataRule;
using MongoDB.Bson;
using QXWebSrvPending;
using QiaoxingWebService;
using Yinhe.ProcessingCenter.Common;
using Yinhe.ProcessingCenter.BusinessFlow;
using MongoDB.Driver.Builders;
using System.Web;
using NLog;


namespace Yinhe.ProcessingCenter.BusinessFlow
{
    /// <summary>
    /// 更新OA待办列表类
    /// </summary>
    public class UpdateOAToDoList : IExecuteTran
    {        
        protected loginService loginService;
        protected Logger log = LogManager.GetCurrentClassLogger();
        protected DataOperation dataOp;
        protected FlowInstanceHelper helper;
        public static bool isUpdate = SysAppConfig.IsUpdateOAToDo;

        public UpdateOAToDoList()
        {
            loginService = new loginService();
            log = LogManager.GetCurrentClassLogger();
            dataOp = new DataOperation();
            helper = new FlowInstanceHelper(dataOp);
        }
        /// <summary>
        /// 修改OA待办列表
        /// </summary>
        /// <param name="UserId"></param>
        /// <param name="instance"></param>
        /// <param name="stepTran"></param>
        public void ExecuteTran(int UserId, BsonDocument instance, BsonDocument stepTran)
        {
            if (SysAppConfig.CustomerCode == CustomerCode.QX)
            {
                QXUpdateOAToDoList(UserId, instance, stepTran);
            }
        }

        #region QX更新OA待办已办

        #region 根据mode调用插入待办或已办方法
        /// <summary>
        /// 根据mode调用插入待办或已办方法
        /// </summary>
        /// <param name="curUserId"></param>
        /// <param name="instance">tbName:BusFlowInstance</param>
        /// <param name="stepTran">tbName:StepTransaction</param>
        public void QXUpdateOAToDoList(int curUserId, BsonDocument instance, BsonDocument stepTran)
        {
            if (!isUpdate) return;
            var transaction = dataOp.FindOneByQuery("TransactionStore", Query.EQ("transactionId", stepTran.Text("transactionId")));

            //等待事务
            switch (transaction.Int("mode"))
            {
                case 0://推送待办
                    //插入待办前先将所有待办置为已办，用于结束会签的跳转
                    QXInsertAllFlowDone(instance.Int("flowInstanceId"), curUserId);
                    QXInsertToDo(curUserId, instance, stepTran); 
                    break;
                case 1: //推送已办
                    QXInsertDone(curUserId, instance, stepTran); 
                    break;
                default: 
                    break;
            }
        }
        #endregion

        #region QX 获取流程步骤人员
        /// <summary>
        /// 获取流程步骤传阅人员，包括同stepOrder的所有传阅人
        /// 作为当前流程步骤审批人员的秘书进行推送
        /// </summary>
        /// <param name="flowId"></param>
        /// <param name="stepId"></param>
        /// <returns></returns>
        IEnumerable<int> QXGetCirculateUserIds(int flowInstanceId,int flowId,int stepId)
        {
            var curInstanceStep = dataOp.FindOneByQuery("InstanceActionUser",
                    Query.And(
                        Query.EQ("flowInstanceId", flowInstanceId.ToString()),
                        Query.EQ("stepId", stepId.ToString())
                    )
                );
            //获取当前真正可执行的步骤id(主要考虑会签情况)
            var curAvaiStepIds = dataOp.FindAllByQuery("InstanceActionUser",
                    Query.And(
                        Query.EQ("flowInstanceId", flowInstanceId.ToString()),
                        Query.EQ("stepOrder", curInstanceStep.Text("stepOrder"))
                    )
                ).Where(i => i.Int("status") == 0).Select(i => i.Int("stepId")).ToList();
            var allCirUserIds = dataOp.FindAllByQuery("StepCirculation",
                        Query.And(
                            Query.In("stepId", curAvaiStepIds.Select(i=>(BsonValue)i.ToString())),
                            Query.EQ("unAvaiable", "0"),
                            Query.EQ("flowId", flowId.ToString())
                        )).Select(i => i.Int("userId"));
            return allCirUserIds;
        }

        /// <summary>
        /// 获取流程步骤传阅人员，只获取当前步骤下的人员
        /// </summary>
        /// <param name="flowInstanceId"></param>
        /// <param name="flowId"></param>
        /// <param name="stepId"></param>
        /// <returns></returns>
        IEnumerable<int> QXGetCurStepCirUserIds(int flowInstanceId, int flowId, int stepId)
        {
            var allCirUserIds = dataOp.FindAllByQuery("StepCirculation",
                        Query.And(
                            Query.EQ("stepId", stepId.ToString()),
                            Query.EQ("unAvaiable", "0"),
                            Query.EQ("flowId", flowId.ToString())
                        )).Select(i => i.Int("userId"));
            return allCirUserIds;
        }

        /// <summary>
        /// 获取当前步骤下一步需要推送待办的人员
        /// </summary>
        /// <param name="flowInstanceId"></param>
        /// <param name="flowId"></param>
        /// <param name="stepId"></param>
        /// <returns></returns>
        IEnumerable<int> QXGetTodoUserIds(int flowInstanceId, int flowId, int stepId)
        {
            var stepAvaiUserIds = helper.GetFlowInstanceAvaiableStepUser(flowId, flowInstanceId, stepId);
            var stepCirUserIds = this.QXGetCirculateUserIds(flowInstanceId, flowId, stepId).ToList();
            return stepAvaiUserIds.Union(stepCirUserIds);
        }

        #endregion

        #region QX webservice推送待办
        /// <summary>
        /// QX webservice插入待办
        /// 用于事务正常执行
        /// </summary>
        /// <param name="curUserId"></param>
        /// <param name="instance"></param>
        /// <param name="stepTranRel"></param>
        public void QXInsertToDo(int curUserId,BsonDocument instance,BsonDocument stepTranRel)
        {
            if (!isUpdate) return;

            //获取当前流程发起人id
            var approvalUserId = instance.Int("approvalUserId");
            List<int> userIdList = new List<int>();

            if (stepTranRel.Int("type") == 2)//跳出事务
            {
                //重新获取当前步骤以及流程可执行人
                var curFlowInstance = dataOp.FindOneByQuery("BusFlowInstance", Query.EQ("flowInstanceId", instance.Text("flowInstanceId")));
                if (curFlowInstance.Int("instanceStatus") != 1)
                {
                    //获取下一步需要推送待办的人员
                    userIdList = QXGetTodoUserIds(curFlowInstance.Int("flowInstanceId"), curFlowInstance.Int("flowId"), curFlowInstance.Int("stepId")).ToList();
                    QXInsertFlowToDo(curFlowInstance, curFlowInstance.Int("stepId"), approvalUserId, userIdList);
                }
            }
            else
            {
                //获取下一步需要推送待办的人员
                userIdList = QXGetTodoUserIds(instance.Int("flowInstanceId"), instance.Int("flowId"), stepTranRel.Int("stepId")).ToList();
                QXInsertFlowToDo(instance, stepTranRel.Int("stepId"), approvalUserId, userIdList);
            }
        }

        /// <summary>
        /// 发送流程审批待办
        /// 事务正常执行或转办时调用
        /// </summary>
        /// <param name="instance">流程实例</param>
        /// <param name="stepId">步骤id</param>
        /// <param name="submitUserId">提交者</param>
        /// <param name="todoUserIdList">待办者</param>
        public void QXInsertFlowToDo(BsonDocument instance, int stepId, int submitUserId, List<int> todoUserIdList)
        {
            if (!isUpdate) return;
            var flowInstanceId = instance.Int("flowInstanceId");
            instance = dataOp.FindOneByQuery("BusFlowInstance", Query.EQ("flowInstanceId", flowInstanceId.ToString()));
            var flowId = instance.Text("flowId");
            var referFieldValue = instance.Text("referFieldValue");

            //记录在哪一步发送这条待办
            var step = dataOp.FindOneByQuery("BusFlowStep", Query.EQ("stepId", stepId.ToString()));
            var stepOrder = step.Text("stepOrder");
            var actTypeId = step.Text("actTypeId");
            
            //获取提交者在内的所有相关人员
            var allUserIdList = new List<int>();
            allUserIdList.Add(submitUserId);
            allUserIdList.AddRange(todoUserIdList);
            var userList = dataOp.FindAllByQuery("SysUser",
                Query.In("userId", allUserIdList.Select(i => (BsonValue)(i.ToString())))
                ).ToList();
            allUserIdList = userList.Select(i => i.Int("userId")).Distinct().ToList();
            todoUserIdList = todoUserIdList.Where(i => allUserIdList.Contains(i)).ToList();
            //获取当前信息提交者，一般情况下为流程发起人
            var submitUser = userList.FirstOrDefault(i => i.Int("userId") == submitUserId);
            string submitUserName = submitUser.Text("loginName");
            //流程标题
            string title = string.Format("请审批\"{0}\"", instance.Text("instanceName"));
            //待办事项链接地址(审批流程地址)
            string approvalUrl = string.Empty;
            switch (instance.Text("tableName"))
            {
                case "ProgrammeEvaluation":
                    approvalUrl = "/ProgrammeEvaluation/EvaluationWorkFlowInfo?proEvalId=" + referFieldValue;
                    break;
                case "DesignChange":
                    approvalUrl = "/DesignManage/DesignChangeWorkFlowInfo?dngChangeId=" + referFieldValue;
                    break;
                default:
                    break;
            }
            if (string.IsNullOrEmpty(approvalUrl)) return;

            foreach (var userId in todoUserIdList)
            {
                getBPMService pendingService = new getBPMService();
                //StorageData data = new StorageData();
                var user = userList.FirstOrDefault(i => i.Int("userId") == userId);
                //生成链接地址
                string linkUrl = string.Format("{0}/Account/Login_QXSSO?ReturnUrl={1}", SysAppConfig.HostDomain, HttpUtility.UrlEncode(approvalUrl));
                string newGuid = Guid.NewGuid().ToString();
                string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                log.Info("开始发送更新待办命令 用户名：{0}", user.Text("name"));
                //webservice更新待办
                pendingService.setBPMTodoDBAsync(
                "INSERT",                                     //写入或删除，值："INSERT"或"UPDATE"或"DELETE"
                newGuid,                                      //待办 GUID
                "YNH",                                        //业务系统类型,用3个缩写代表银禾
                user.Text("loginName"),                       //待办者
                title,                                        //标题
                submitUserName,                               //提交者
                now,                                          //提交时间
                linkUrl,                                      //链接地址
                string.Empty,                                 //表单基础内容，缺省值“”
                string.Empty,                                 //附件，缺省值“”
                string.Empty,                                 //可提交的路由，缺省值“0”
                string.Empty,                                 //可驳回的路由，缺省值“0”
                0,                                            //是否允许驳回，缺省值 0，0：不允许，1 允许
                0,                                            //是否允许协助，缺省值 0，0：不允许，1 允许
                0,                                            //是否允许转办，缺省值 0，0：不允许，1 允许
                0,                                            //是否允许结束，缺省值 0，0：不允许，1 允许
                string.Empty                                  //未知...
                );
                pendingService.setBPMTodoDBCompleted += (sender, e) =>
                {
                    if (e.Error == null)
                    {
                        var newDoc = new BsonDocument(){
                            {"tableName","BusFlowInstance"},
                            {"referFieldName","flowInstanceId"},
                            {"referFieldValue",instance.Text("flowInstanceId")},
                            {"flowId",flowId.ToString()},
                            {"stepId",stepId.ToString()},
                            {"stepOrder",stepOrder.ToString()},
                            {"actTypeId",actTypeId.ToString()},
                            {"todoUserId",userId.ToString()},
                            {"todoGuid",newGuid},
                            {"todoTime",now},
                            {"doneUserId",string.Empty},
                            {"doneTime",string.Empty},
                            {"status","0"}
                        };
                        dataOp.Insert("OAToDoNumber", newDoc);
                        string msg = string.Format("GUID: {1} 用户名: {0} 发送更新(INSERT)待办命令成功", user.Text("name"), newGuid);
                        log.Info(msg);
                    }
                    else
                    {
                        log.Error("给{0}发送待办出错,instanceId:{1},stepId:{2}", user.Text("name"), instance.Text("flowInstanceId"), stepId);
                        log.Error(e.Error.Message);
                    }
                };
            }
        }

        /// <summary>
        /// 给进行中的流程当前步骤审批人员发送待办
        /// </summary>
        /// <param name="flowInstanceId">流程实例id</param>
        public void QXInsertAllFlowToDo(int flowInstanceId)
        {
            if (!isUpdate) return;
            var instance = dataOp.FindOneByQuery("BusFlowInstance", Query.EQ("flowInstanceId", flowInstanceId.ToString()));
            if (!instance.IsNullOrEmpty())
            {
                if (!string.IsNullOrWhiteSpace(instance.Text("approvalUserId")))
                {
                    //获取当前流程发起人id
                    var approvalUserId = instance.Int("approvalUserId");
                    var userIdList = this.QXGetTodoUserIds(instance.Int("flowInstanceId"), instance.Int("flowId"), instance.Int("stepId")).ToList();
                    QXInsertFlowToDo(instance, instance.Int("stepId"), instance.Int("approvalUserId"), userIdList);
                }
            }
        }

        #endregion

        #region QX webservice推送已办
        /// <summary>
        /// 发送已办消息，OA会自动更新对应待办
        /// 事务正常执行时调用
        /// </summary>
        /// <param name="curUserId"></param>
        /// <param name="instance"></param>
        /// <param name="stepTranRel"></param>
        public void QXInsertDone(int curUserId, BsonDocument instance,BsonDocument stepTranRel)
        {
            if (!isUpdate) return;
            QXInsertFlowDone(instance, stepTranRel.Int("stepId"), curUserId);
        }

        /// <summary>
        /// 事务正常执行或转办、二次会签调用
        /// 发送流程已办消息
        /// 发起二次会签或转办等不通过事务执行的方法需要指定stepId
        /// </summary>
        /// <param name="instance">流程实例</param>
        /// <param name="stepId">当前步骤id</param>
        /// <param name="doneUserId">执行者id</param>
        public void QXInsertFlowDone(BsonDocument instance, int stepId, int doneUserId)
        {
            //当前用户操作完成后也给对应步骤下的传阅人推送已办消息
            var stepCirUserIds = this.QXGetCurStepCirUserIds(instance.Int("flowInstanceId"), instance.Int("flowId"), stepId).ToList();
            QXInsertFlowDone(instance, stepId, stepCirUserIds, doneUserId);
        }

        /// <summary>
        /// 推送已办消息
        /// </summary>
        /// <param name="instance">流程实例</param>
        /// <param name="stepId">当前步骤id</param>
        /// <param name="doneUserId">执行者id</param>
        /// <param name="doneUserIdList">所有需要发送已办消息的人</param>
        public void QXInsertFlowDone(BsonDocument instance, int stepId, IEnumerable<int> doneUserIdList,int doneUserId)
        {
            if (!isUpdate) return;
            var allDoneUserIds = new List<int>();
            if (doneUserIdList != null)
            {
                allDoneUserIds.AddRange(doneUserIdList);
            }
            allDoneUserIds.Add(doneUserId);
            allDoneUserIds = allDoneUserIds.Distinct().ToList();
            var flowInstanceId = instance.Int("flowInstanceId");
            var curStep = dataOp.FindOneByQuery("BusFlowStep", Query.EQ("stepId", stepId.ToString()));
            //var doneUser = dataOp.FindOneByQuery("SysUser", Query.EQ("userId", doneUserId.ToString()));
            var allDoneUsers = dataOp.FindAllByQuery("SysUser",
                    Query.In("userId", allDoneUserIds.Select(i => (BsonValue)(i.ToString())))
                ).ToList();
            allDoneUserIds = allDoneUsers.Select(i => i.Int("userId")).ToList();

            var allPenpens = dataOp.FindAllByQuery("OAToDoNumber",
                        Query.And(
                            Query.EQ("tableName", "BusFlowInstance"),
                            Query.EQ("referFieldName", "flowInstanceId"),
                            Query.EQ("referFieldValue", flowInstanceId.ToString()),
                            Query.In("todoUserId", allDoneUserIds.Select(i => (BsonValue)(i.ToString()))),
                            Query.EQ("stepOrder", curStep.Text("stepOrder")),
                            Query.EQ("status", "0")
                        )
                    ).ToList();
            var allTraces = dataOp.FindAllByQuery("BusFlowTrace",
                            Query.And(
                                Query.EQ("preStepId", stepId.ToString()),
                                Query.EQ("flowInstanceId", flowInstanceId.ToString()),
                                Query.In("traceType", new List<BsonValue>() { "2", "6" }),
                                Query.In("createUserId", allDoneUserIds.Select(i => (BsonValue)(i.ToString())))
                            )
                        ).GroupBy(i => i.Int("createUserId"))
                        .Select(i => i.OrderBy(u => u.Date("createDate")).LastOrDefault()).ToList();
            var allForms = dataOp.FindAllByQuery("BusFlowFormData",
                    Query.In("formId", allTraces.Select(i => (BsonValue)(i.ToString())))
                ).ToList();
            foreach (var tempDoneUserId in allDoneUserIds)
            {
                var doneUser = allDoneUsers.Where(i => i.Int("userId") == tempDoneUserId).FirstOrDefault();
                if (doneUser.IsNullOrEmpty())
                {
                    continue;
                }
                //获取对应的待办（因为会签步骤的关系，所以不用stepId而用stepOrder来查找）
                var penpen = allPenpens.Where(i => i.Int("todoUserId") == doneUser.Int("userId"))
                    .OrderBy(i => i.Int("order")).LastOrDefault();

                if (!penpen.IsNullOrEmpty())
                {
                    //获取审批记录，包括转办记录
                    string content = string.Empty;
                    //var trace = dataOp.FindAllByQuery("BusFlowTrace",
                    //        Query.And(
                    //            Query.EQ("preStepId", stepId.ToString()),
                    //            Query.EQ("flowInstanceId", flowInstanceId.ToString()),
                    //            Query.In("traceType", new List<BsonValue>() { "2", "6" }),
                    //            Query.EQ("createUserId", doneUser.Text("userId"))
                    //        )
                    //    ).LastOrDefault();
                    var trace = allTraces.Where(i => i.Int("createUserId") == doneUser.Int("userId")).FirstOrDefault();
                    string formId = trace.Text("formId");

                    if (!trace.IsNullOrEmpty())
                    {
                        if (!string.IsNullOrEmpty(formId))
                        {
                            //var form = dataOp.FindOneByQuery("BusFlowFormData", Query.EQ("formId", formId));
                            var form = allForms.Where(i => i.Text("formId") == formId).FirstOrDefault();
                            if (!form.IsNullOrEmpty() && !string.IsNullOrEmpty(form.Text("content")))
                            {
                                content = form.Text("content");
                            }
                            else
                            {
                                content = trace.Text("remark");
                            }
                        }
                        else
                        {
                            content = trace.Text("remark");
                        }
                    }
                    string guid = penpen.String("todoGuid");

                    getBPMService pendingService = new getBPMService();

                    //发送已办命令,将待办转为已办
                    pendingService.setBPMDoingDBAsync(
                    guid,                           //已办 GUID（即提交者当前待办 GUID）
                    string.Empty,                   //流程开始节点 ID
                    string.Empty,                   //流程结束节点 ID
                    content                         //审批意见
                    );
                    pendingService.setBPMDoingDBCompleted += (sender, e) =>
                    {
                        if (e.Error == null)
                        {
                            string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            string msg = string.Format("GUID: {0} 用户名: {1} 发送更新已办命令成功 ", guid, doneUser.Text("name"));
                            log.Info(msg);
                            BsonDocument updateDoc = new BsonDocument(){
                            {"doneUserId",doneUserId.ToString()},
                            {"doneTime",now},
                            {"status","1"},
                            {"content",content}
                        };
                            dataOp.Update("OAToDoNumber", Query.EQ("todoNumId", penpen.Text("todoNumId")), updateDoc);
                        }
                        else
                        {
                            log.Error("GUID: {0} 发送已办出错", guid);
                            log.Error(e.Error.Message);
                        }
                    };
                }
            }
        }

        /// <summary>
        /// 给流程实例所有未完成的待办发送已办消息
        /// </summary>
        /// <param name="flowInstanceId">流程实例id</param>
        /// <param name="doneUserId">当前发送者</param>
        public void QXInsertAllFlowDone(int flowInstanceId, int doneUserId)
        {
            if (!isUpdate) return;
            var doneUser = dataOp.FindOneByQuery("SysUser", Query.EQ("userId", doneUserId.ToString()));
            //获取改流程实例当前的所有待办
            var allPending = dataOp.FindAllByQuery("OAToDoNumber",
                    Query.And(
                        Query.EQ("tableName", "BusFlowInstance"),
                        Query.EQ("referFieldName", "flowInstanceId"),
                        Query.EQ("referFieldValue", flowInstanceId.ToString()),
                        Query.EQ("status", "0")
                    )
                ).ToList();
            foreach (var pending in allPending)
            {
                getBPMService pendingService = new getBPMService();
                //发送已办命令,将待办转为已办
                string guid = pending.String("todoGuid");

                pendingService.setBPMDoingDBAsync(
                guid,                           //已办 GUID（即提交者当前待办 GUID）
                string.Empty,                   //流程开始节点 ID
                string.Empty,                   //流程结束节点 ID
                string.Empty                    //审批意见
                );
                pendingService.setBPMDoingDBCompleted += (sender, e) =>
                {
                    if (e.Error == null)
                    {
                        string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        string msg = string.Format("GUID: {0} 用户名: {1} 发送更新已办命令成功 ", guid, doneUser.Text("name"));
                        log.Info(msg);
                        BsonDocument updateDoc = new BsonDocument(){
                            {"doneUserId",doneUserId.ToString()},
                            {"doneTime",now},
                            {"status","1"}
                        };
                        dataOp.Update("OAToDoNumber", Query.EQ("todoNumId", pending.Text("todoNumId")), updateDoc);
                    }
                    else
                    {
                        log.Error("GUID: {0} 发送已办出错", guid);
                        log.Error(e.Error.Message);
                    }
                };
            }
        }
        #endregion

        #endregion

    }

}
