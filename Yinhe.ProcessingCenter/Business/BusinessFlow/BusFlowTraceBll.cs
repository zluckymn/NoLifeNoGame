using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using Yinhe.ProcessingCenter.DataRule;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
namespace Yinhe.ProcessingCenter.BusinessFlow
{
    /// <summary>
    /// 流程日志跟踪处理类
    /// </summary>
    public class BusFlowTraceBll
    {
        #region 构造函数
        /// <summary>
        /// 类私有变量
        /// </summary>
        private DataOperation dataOp = null;

        private string tableName = "";
        /// <summary>
        /// 封闭当前默认构造函数
        /// </summary>
        private BusFlowTraceBll()
        {
            dataOp = new DataOperation();
        }

        private BusFlowTraceBll(DataOperation _dataOp)
        {
            dataOp = _dataOp;
        }

        public static BusFlowTraceBll _()
        {
            return new BusFlowTraceBll();
        }

        public static BusFlowTraceBll _(DataOperation _dataOp)
        {
            return new BusFlowTraceBll(_dataOp);
        }
        #endregion

        #region  查询

        /// <summary>
        /// 获取某个会签步骤中未执行步骤Id列表
        /// </summary>
        /// <param name="flowId"></param>
        /// <param name="flowInstanceId"></param>
        /// <param name="preStepObj"></param>
        /// <returns></returns>
        public List<int> GetNextAbaiableStepList(int flowId,int flowInstanceId,BsonDocument preStepObj)
        {
            var currentStepList = dataOp.FindAllByQueryStr("BusFlowStep", string.Format("flowId={0}&actTypeId=2&stepOrder={1}&stepId!={2}", flowId, preStepObj.Text("stepOrder"), preStepObj.Text("stepId")));
            var currentStepIdStrList=currentStepList.Select(c => c.Text("stepId")).ToList();
            var currentStepIdList = currentStepList.Select(c => c.Int("stepId")).ToList();
            var query1 = Query.EQ("flowInstanceId", flowInstanceId.ToString());
            var query2 = Query.EQ("traceType", "2");
            var query3 = Query.EQ("actionAvaiable","1");
            var query4 = Query.In("preStepId", TypeConvert.StringListToBsonValueList(currentStepIdStrList));
            var hitStepIdList = dataOp.FindAllByQuery("BusFlowTrace", Query.And(query1, query2, query3, query4)).Select(c => c.Int("preStepId"));
            var resultIds = currentStepIdList.Where(c => !hitStepIdList.Contains(c)).ToList();
            return resultIds;
        }

        /// <summary>
        /// 判断当前步骤是否可以跳转,此时当前记录动作还未添加日志所以应该-1
        /// </summary>
        /// <param name="flowId"></param>
        /// <param name="preStepId"></param>
        /// <returns></returns>
        
        public bool CanJumpNextStep(int flowId, int flowInstanceId, BsonDocument preStepObj,int actionType)
        {
            if (actionType == 3 || actionType == 4|| actionType == 5)//非会签
            {
                return true;
            }
            if (preStepObj == null)
            {
                return true;
            }
            ///当前步骤涉及需要会签的所有步骤
            ///
           //var invalidStepIds = dataOp.FindAllByKeyVal("InstanceActionUser", "flowInstanceId", flowInstanceId.ToString()).Where(c =>c.Int("status")==1).Select(c=>c.Int("stepId")).ToList();
            var avaiableStepIds = dataOp.FindAllByKeyVal("InstanceActionUser", "flowInstanceId", flowInstanceId.ToString()).Where(c => c.Int("status") == 0 && c.Int("isSkip") == 0).Select(c => c.Int("stepId")).ToList();
            var currentStepList = dataOp.FindAllByQueryStr("BusFlowStep", string.Format("flowId={0}&actTypeId=2&stepOrder={1}&stepId!={2}", flowId, preStepObj.Text("stepOrder"), preStepObj.Text("stepId"))).Where(c => avaiableStepIds.Contains(c.Int("stepId"))).Select(c => c.Text("stepId")).ToList();

            var query1 = Query.EQ("flowInstanceId", flowInstanceId.ToString());
            var query2 = Query.EQ("traceType", "2");
            var query3 = Query.EQ("actionAvaiable", "1");
            var query4 = Query.In("preStepId", TypeConvert.StringListToBsonValueList(currentStepList));
            var hitStepTrace = dataOp.FindAllByQuery("BusFlowTrace", Query.And(query1, query2, query3, query4));
            if (hitStepTrace.Count() >= (currentStepList.Count()))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion


        #region 表操作
        /// <summary>
        /// 添加实例
        /// </summary>
        /// <returns></returns>
        public InvokeResult Insert(BsonDocument pwdTip)
        {
            if (pwdTip == null)
                throw new ArgumentNullException();

            var result = new InvokeResult() { Status = Status.Successful };
            result = dataOp.Insert("BusFlowTrace", pwdTip);
            return result;
        }



        /// <summary>
        /// 创建启动流程跟踪日志
        /// </summary>
        /// <returns></returns>
        public InvokeResult CreateStartInstanceLog(int nextStepId, int flowInstanceId, int traceType)
        {
            var startFlowTrace = new BsonDocument();
            startFlowTrace.Add("nextStepId", nextStepId);
            startFlowTrace.Add("flowInstanceId", flowInstanceId);
            startFlowTrace.Add("remark", "启动流程");
            startFlowTrace.Add("traceType", traceType);
            return this.Insert(startFlowTrace);
        }

        /// <summary>
        /// 创建系统自动跳转跟踪日志
        /// </summary>
        /// <param name="preStepId"></param>
        /// <param name="nextStepId"></param>
        /// <param name="flowInstanceId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public InvokeResult CreateSysActionLog(int? preStepId, int? nextStepId, int flowInstanceId)
        {
            BsonDocument startFlowTrace = new BsonDocument();
            startFlowTrace.Add("preStepId", preStepId);
            startFlowTrace.Add("nextStepId", nextStepId);
            startFlowTrace.Add("flowInstanceId", flowInstanceId);
            startFlowTrace.Add("remark", "自动跳转");
            startFlowTrace.Add("traceType", 1);
            return this.Insert(startFlowTrace);
        }


        /// <summary>
        /// 创建系统自动跳转跟踪日志
        /// </summary>
        /// <param name="preStepId"></param>
        /// <param name="nextStepId"></param>
        /// <param name="flowInstanceId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public InvokeResult CreateCompleteActionLog(int? preStepId, int flowInstanceId)
        {
            BsonDocument startFlowTrace = new BsonDocument();
            startFlowTrace.Add("preStepId", preStepId);
            startFlowTrace.Add("flowInstanceId", flowInstanceId);
            startFlowTrace.Add("remark", "流程结束");
            startFlowTrace.Add("traceType", 1);
            return this.Insert(startFlowTrace);
        }

        /// <summary>
        /// 创建系统自动跳转跟踪日志
        /// </summary>
        /// <param name="preStepId"></param>
        /// <param name="nextStepId"></param>
        /// <param name="flowInstanceId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public InvokeResult CreateAbortActionLog(int? preStepId, int flowInstanceId)
        {
            BsonDocument startFlowTrace = new BsonDocument();
            startFlowTrace.Add("preStepId", preStepId);
            startFlowTrace.Add("flowInstanceId", flowInstanceId);
            startFlowTrace.Add("remark", "废弃了该流程实例");
            startFlowTrace.Add("traceType", 5);
            return this.Insert(startFlowTrace);
        }
        /// <summary>
        /// 重置所有状态为无效
        /// </summary>
        /// <param name="flowId"></param>
        /// <param name="flowInstanceId"></param>
        /// <param name="startStepId"></param>
        /// <param name="curStepId"></param>
        /// <returns></returns>
        public InvokeResult ResetActionUnAvaiable(int flowId, int flowInstanceId)
        {
            ////流程实例模板对象更新
           var hitInstanceActionUser=dataOp.FindAllByQueryStr("InstanceActionUser",string.Format("flowInstanceId={0}&actionAvaiable=1", flowInstanceId)).ToList();
           if (hitInstanceActionUser.Count() > 0)
           { 
               dataOp.QuickUpdate("InstanceActionUser", hitInstanceActionUser, "actionAvaiable=0");
              
           }
            var hitStepTrace = dataOp.FindAllByQueryStr("BusFlowTrace", string.Format("flowInstanceId={0}&traceType=2&actionAvaiable=1", flowInstanceId)).ToList();
            if (hitStepTrace.Count() > 0)
            {
                var result = dataOp.QuickUpdate("BusFlowTrace", hitStepTrace, "actionAvaiable=0");
                return result;
            }
            else
            {
                return new InvokeResult() { Status = Status.Successful };
            }
        }


        /// <summary>
        /// 将步骤列表中的会签步骤设置为无效，二次会签步骤开始,目前 发起二次会签步骤中夹杂着一个会签步骤可能导致问题，所以尽可能按上一步会签
        /// </summary>
        /// <param name="flowId"></param>
        /// <param name="startStepId"></param>
        /// <param name="curStepId"></param>
        /// <returns></returns>
        public InvokeResult ChangeActionUnAvaiable(int flowId, int flowInstanceId, int startStepId,int curStepId,List<int> stepIds,List<int>filterAssignerStepIds, string remark)
        {
            if (startStepId == curStepId)
            {
                return new InvokeResult() { Status = Status.Successful };
            } 
            var hitStepTrace = dataOp.FindAllByKeyVal("BusFlowTrace", "flowInstanceId", flowInstanceId.ToString()).Where(c => c.Int("traceType") == 2 && c.Int("actionAvaiable") == 1).OrderByDescending(c => c.Date("createData"));
            //新增流程会签actionAvaiable替换
            var AllInstanceActionUser = dataOp.FindAllByKeyVal("InstanceActionUser", "flowInstanceId", flowInstanceId.ToString()).ToList();
            var hitInstanceActionUser = AllInstanceActionUser.Where(c => c.Int("actionAvaiable") == 1).OrderByDescending(c => c.Int("stepOrder")).ToList();
            
            var firstStepInstanceAction = AllInstanceActionUser.Where(c => c.Int("stepId") == startStepId).FirstOrDefault();
            var endStepInstanceAction = AllInstanceActionUser.Where(c => c.Int("stepId") == curStepId).FirstOrDefault();
            
            if (hitInstanceActionUser.Count() > 0)
            { 
                //此处相同stepOrder而没有在stepIds里面的需要不需要修改， 不同stepOrder的需要重置actionAvaiable=0，因为有可能二次会签回去的的步骤可能中间夹杂着会签步骤
                var needChangeInstanceAction = hitInstanceActionUser.Where(c =>stepIds.Contains(c.Int("stepId"))|| (c.Int("actTypeId")==2&&c.Int("stepOrder") > firstStepInstanceAction.Int("stepOrder") && c.Int("stepOrder") <= endStepInstanceAction.Int("stepOrder"))).ToList();
                dataOp.QuickUpdate("InstanceActionUser", needChangeInstanceAction, "actionAvaiable=0&resetRemark=" + remark);
            }

            #region 转办替设置为无效
            //var AllBusinessFlowUserGrant = dataOp.FindAllByKeyVal("BusinessFlowUserGrant", "flowInstanceId", flowInstanceId.ToString()).ToList();
            //var hitBusinessFlowUserGrant = AllBusinessFlowUserGrant.Where(c => c.Int("status") == 0).OrderByDescending(c => c.Int("stepOrder")).ToList();
            //var firstStepUserGrant = AllBusinessFlowUserGrant.Where(c => c.Int("stepId") == startStepId).FirstOrDefault();
            //var endStepUserGrant = AllBusinessFlowUserGrant.Where(c => c.Int("stepId") == curStepId).FirstOrDefault();
            //var needChangeUserGrant = hitBusinessFlowUserGrant.Where(c => c.Int("stepOrder") >= firstStepUserGrant.Int("stepOrder") && c.Int("stepOrder") <= endStepUserGrant.Int("stepOrder")).ToList();
            //dataOp.QuickUpdate("BusinessFlowUserGrant", needChangeUserGrant, "staus=1");
            #endregion

            var beginDate = DateTime.MinValue;
            var endDate = DateTime.Now;
            ///步骤里面的第一个不一定是日志执行顺序的第一个 注意会签步骤不按照顺序来 2014.2.14，  当驳回到会签步骤会出现以下问题请注意
            var needChangeTrace = new List<BsonDocument>();
            foreach (var stepId in stepIds)
            {
                var curSteTrace = hitStepTrace.Where(c => c.Int("actionAvaiable") != 0 && c.Int("preStepId") == stepId).OrderByDescending(c => c.Date("createData")).FirstOrDefault();
                if (curSteTrace != null)
                {
                    needChangeTrace.Add(curSteTrace);
                }
            }
            //此处相同stepOrder而没有在stepIds里面的需要不需要修改， 不同stepOrder的需要重置actionAvaiable=0，因为有可能二次会签回去的的步骤可能中间夹杂着会签步骤

            var firstStepTrace = needChangeTrace.OrderByDescending(c => c.Date("createData")).FirstOrDefault();//找出匹配的最后一个作为开始
            var endStepTrace = hitStepTrace.Where(c => c.Int("preStepId") == curStepId).OrderByDescending(c => c.Date("createData")).FirstOrDefault();
            if (firstStepTrace != null)
            {
                beginDate = firstStepTrace.Date("createDate");
            }
            if (endStepTrace != null)
            {
                endDate = endStepTrace.Date("createDate");
            }
            //获取二次会签步骤与发起二次会签步骤之间除了与选中步骤不同stepOrder的步骤设置为0
            var otherAssignerStep = hitStepTrace.Where(c => !filterAssignerStepIds.Contains(c.Int("preStepId")) && c.Date("createDate") > beginDate && c.Date("createDate") <= endDate && c.Int("actionAvaiable") != 0).ToList();
            if (otherAssignerStep.Count > 0)
            {
                needChangeTrace.AddRange(otherAssignerStep);
            }
            var result = dataOp.QuickUpdate("BusFlowTrace", needChangeTrace, "actionAvaiable=0");
            return result;
        }


        /// <summary>
        /// 将当前步骤之前到开始步骤的之间的会签操作设置为无效
        /// </summary>
        /// <param name="flowId"></param>
        /// <param name="startStepId"></param>
        /// <param name="curStepId"></param>
        /// <returns></returns>
        public InvokeResult ChangeActionUnAvaiable(int flowId, int flowInstanceId, int startStepId, int curStepId)
        {
            if (startStepId == curStepId)
            {
                return new InvokeResult() { Status = Status.Successful };
            }
            var hitStepTrace = dataOp.FindAllByKeyVal("BusFlowTrace", "flowInstanceId", flowInstanceId.ToString()).Where(c => c.Int("traceType") == 2 && c.Int("actionAvaiable") == 1).OrderByDescending(c => c.Date("createData"));
            //新增流程会签actionAvaiable替换
            var AllInstanceActionUser = dataOp.FindAllByKeyVal("InstanceActionUser", "flowInstanceId", flowInstanceId.ToString()).ToList();
            var hitInstanceActionUser = AllInstanceActionUser.Where(c => c.Int("actionAvaiable") == 1).OrderByDescending(c => c.Int("stepOrder")).ToList();
            
            if (hitInstanceActionUser.Count() > 0)
            {
                var firstStepInstanceAction = AllInstanceActionUser.Where(c => c.Int("stepId") == startStepId).FirstOrDefault();
                var endStepInstanceAction = AllInstanceActionUser.Where(c => c.Int("stepId") == curStepId).FirstOrDefault();
                var needChangeInstanceAction = hitInstanceActionUser.Where(c => c.Int("stepOrder") >= firstStepInstanceAction.Int("stepOrder") && c.Int("stepOrder") <= endStepInstanceAction.Int("stepOrder")).ToList();
                dataOp.QuickUpdate("InstanceActionUser", needChangeInstanceAction, "actionAvaiable=0");
            }

            #region 转办替设置为无效
            var AllBusinessFlowUserGrant = dataOp.FindAllByKeyVal("BusinessFlowUserGrant", "flowInstanceId", flowInstanceId.ToString()).ToList();
            var hitBusinessFlowUserGrant = AllBusinessFlowUserGrant.Where(c => c.Int("status") == 0).OrderByDescending(c => c.Int("stepOrder")).ToList();
            var firstStepUserGrant = AllBusinessFlowUserGrant.Where(c => c.Int("stepId") == startStepId).FirstOrDefault();
            var endStepUserGrant = AllBusinessFlowUserGrant.Where(c => c.Int("stepId") == curStepId).FirstOrDefault();
            var needChangeUserGrant = hitBusinessFlowUserGrant.Where(c => c.Int("stepOrder") >= firstStepUserGrant.Int("stepOrder") && c.Int("stepOrder") <= endStepUserGrant.Int("stepOrder")).ToList();
            dataOp.QuickUpdate("BusinessFlowUserGrant", needChangeUserGrant, "staus=1");
            #endregion

            var beginDate = DateTime.MinValue;
            var endDate = DateTime.Now;
            var firstStepTrace = hitStepTrace.Where(c => c.Int("preStepId") == startStepId).OrderByDescending(c => c.Date("createData")).FirstOrDefault();
            var endStepTrace = hitStepTrace.Where(c => c.Int("preStepId") == curStepId).OrderByDescending(c => c.Date("createData")).FirstOrDefault();
            if (firstStepTrace != null)
            {
                beginDate = firstStepTrace.Date("createDate");
            }
            if (endStepTrace != null)
            {
                endDate = endStepTrace.Date("createDate");
            }
            var needChangeTrace = hitStepTrace.Where(c => c.Date("createDate") >= beginDate && c.Date("createDate") <= endDate && c.Int("actionAvaiable") != 0).ToList();
            var result = dataOp.QuickUpdate("BusFlowTrace", needChangeTrace, "actionAvaiable=0");
            return result;
        }

        /// <summary>
        /// 将当前步骤之前到开始步骤的之间的会签操作设置为无效
        /// </summary>
        /// <param name="flowId"></param>
        /// <param name="flowInstanceId"></param>
        /// <param name="curStepId"></param>
        /// <returns></returns>
        public InvokeResult ChangeActionUnAvaiable(int flowId, int flowInstanceId,  int curStepId)
        {
            BusFlowStepBll stepBll=BusFlowStepBll._();
            var firstStep = stepBll.GetStartStep(flowId);
            if (firstStep != null)
            {
                return ChangeActionUnAvaiable(flowId, flowInstanceId,firstStep.Int("stepId"), curStepId);
            }
            else
            {
            return ChangeActionUnAvaiable(flowId, flowInstanceId,0, curStepId);
            }
           
        }


        #endregion


    }

}
