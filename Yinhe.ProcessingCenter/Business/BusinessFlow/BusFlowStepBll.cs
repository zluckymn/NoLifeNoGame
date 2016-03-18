using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using Yinhe.ProcessingCenter.DataRule;
using MongoDB.Bson;
namespace Yinhe.ProcessingCenter.BusinessFlow
{
    /// <summary>
    /// 流程步骤处理类
    /// </summary>
    public class BusFlowStepBll
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
        private BusFlowStepBll() 
        {
            dataOp = new DataOperation();
        }

        private BusFlowStepBll(DataOperation _dataOp)
        {
            dataOp = _dataOp;
        }

        public static BusFlowStepBll _()
        {
            return new BusFlowStepBll();
        }

        public static BusFlowStepBll _(DataOperation _dataOp)
        {
            return new BusFlowStepBll(_dataOp);
        }
        #endregion

        #region 查询
        public BsonDocument FindById(string  Id)
        {
            var obj = dataOp.FindOneByKeyVal("BusFlowStep", "stepId", Id);
            return obj;
        }
        
        /// <summary>
        /// 获取该步骤的下一个步骤
        /// </summary>
        /// <param name="flowId"></param>
        /// <param name="step"></param>
        /// <returns></returns>
        public BsonDocument GetNextStep(int flowInstanceId,int flowId, BsonDocument step)
        {
          
            var stepList = dataOp.FindAllByKeyVal("BusFlowStep", "flowId", flowId.ToString()).OrderBy(c=>c.Int("stepOrder"));
            if (step == null) return stepList.Where(c => c.Int("actTypeId") == (int)FlowActionType.Launch).FirstOrDefault();
            else
            {
                //过滤不可用的步骤列表
                //var filterStepIds = dataOp.FindAllByKeyVal("InstanceActionUser", "flowInstanceId", flowInstanceId.ToString()).Where(c => c.Int("status") == 1 || c.Int("isSkip")==1).Select(c => c.Int("stepId")).ToList();
                var avaiAbleStepIds = dataOp.FindAllByKeyVal("InstanceActionUser", "flowInstanceId", flowInstanceId.ToString()).Where(c => c.Int("status") == 0 && c.Int("isSkip") == 0).Select(c => c.Int("stepId")).ToList();
                var curStepOrder = step.Int("stepOrder");
                var hitObj = stepList.Where(c =>avaiAbleStepIds.Contains(c.Int("stepId")) && c.Int("stepOrder") > curStepOrder).FirstOrDefault();
                return hitObj;
            }
         }

        /// <summary>
        /// 获取该步骤的下一个步骤
        /// </summary>
        /// <param name="flowId"></param>
        /// <param name="stepId"></param>
        /// <returns></returns>
        public BsonDocument GetNextStep(int flowInstanceId,int flowId, int stepId)
        {
            var step = dataOp.FindOneByKeyVal("BusFlowStep", "stepId", stepId.ToString());
            return GetNextStep(flowInstanceId,flowId, step);
        }

        /// <summary>
        /// 获取该发起步骤
        /// </summary>
        /// <param name="flowId"></param>
        /// <param name="stepId"></param>
        /// <returns></returns>
        public BsonDocument GetStartStep(int flowId)
        {
            var stepList = dataOp.FindAllByKeyVal("BusFlowStep", "flowId", flowId.ToString()).OrderBy(c => c.Int("stepOrder"));
            return stepList.Where(c=>c.Int("actTypeId")== (int)FlowActionType.Launch).FirstOrDefault();
        }

        /// <summary>
        /// 获取该发起步骤
        /// </summary>
        /// <param name="flowId"></param>
        /// <param name="stepId"></param>
        /// <returns></returns>
        public BsonDocument GetActionRefuseStep(int flowInstanceId, int flowId, int stepId)
        {
            var nextStep = new BsonDocument();
            var preStep = dataOp.FindOneByKeyVal("BusFlowStep", "stepId", stepId.ToString());
            if (preStep != null && preStep.Int("refuseStepId") != 0)
            {
                nextStep = dataOp.FindOneByKeyVal("BusFlowStep", "stepId", preStep.Text("refuseStepId"));
                if (nextStep == null)
                {
                    nextStep = GetStartStep(flowId);
                }
            }
            else
            {
                nextStep = GetStartStep(flowId);
            }
            return nextStep;
        }
        /// <summary>
        /// 获取该驳回动作后，应该执行的步骤,当执行的步骤是领导集团步骤时候需要退回集团领导的第一个步骤
        /// </summary>
        /// <param name="flowId"></param>
        /// <param name="preStepId">当前步骤</param>
        /// <returns></returns>
        public BsonDocument GetGroupRefuseStep(int flowId, int curStepId)
        {
            var stepList = dataOp.FindAllByKeyVal("BusFlowStep", "flowId", flowId.ToString()).OrderBy(c => c.Int("stepOrder"));
            var curStep = stepList.Where(c=>c.Int("stepId")==curStepId).FirstOrDefault();
            var startStep=stepList.Where(c => c.Int("actTypeId") == 1).FirstOrDefault();//第一个发起步骤
            //获取第一个集团审批步骤,有可能为会签步骤
            var fistGroupApprovalStep=stepList.Where(c => c.SourceBsonField("flowPosId","orgLevel") == "1").FirstOrDefault();
            if (curStep != null && curStep.SourceBsonField("flowPosId", "orgLevel") == "1")
            {
                //有可能为会签步骤的驳回步骤，然后第一个会前通过，第二个驳回情况，步骤不一样代表不是同一步骤
                if (curStepId != fistGroupApprovalStep.Int("stepId")&&curStep.Int("stepOrder")!=fistGroupApprovalStep.Int("stepOrder"))
                {
                    return fistGroupApprovalStep;//返回领导审批的第一个步骤
                }
            }
            else
            {
              return startStep;//返回发起步骤
            }
            return startStep;
        }

        /// <summary>
        /// 获取流程模板签发步骤
        /// </summary>
        /// <param name="flowId"></param>
        /// <param name="stepId"></param>
        /// <returns></returns>
        public List<BsonDocument> GetSignStep(int flowId)
        {
            List<BsonDocument> stepList = new List<BsonDocument>();
            var signStepIdStr = SysAppConfig.SignStepId;
            if (!string.IsNullOrEmpty(signStepIdStr))
            {
                List<string> signStepIdList = signStepIdStr.Split(new string[] { ",", "，" }, StringSplitOptions.RemoveEmptyEntries).ToList();
                if (signStepIdList.Count() <= 0) { return stepList; }
                stepList = dataOp.FindAllByKeyValList("BusFlowStep", "stepId", signStepIdList)
                    .Where(i => i.Int("flowId") == flowId).ToList();
            }
            else
            {
                BsonDocument signOrg=dataOp.FindAll("BusFlowPosition")
                    .Where(i=>i.Text("name").Contains("签发"))
                    .OrderBy(i=>i.Int("order")).LastOrDefault();
                if(!signOrg.IsNullOrEmpty()){
                    stepList = dataOp.FindAll("BusFlowStep")
                        .Where(i => i.Int("flowId") == flowId && i.Int("flowPosId") == signOrg.Int("flowPosId")).ToList();
                }
            }
            return stepList;
        }

        #endregion

        #region 表操作
        /// <summary>
            /// 添加实例
            /// </summary>
            /// <returns></returns>
            public InvokeResult  Insert(BsonDocument pwdTip)
            {
                if (pwdTip == null)
                    throw new ArgumentNullException();

                var result = new InvokeResult() { Status = Status.Successful };
                 result= dataOp.Insert("BusFlowStep", pwdTip);
                return result;
            }

           /// <summary>
            /// 保存步骤编辑
            /// </summary>
            /// <param name="stepId"></param>
            /// <param name="inTranIdArray"></param>
            /// <param name="waitTranIdArray"></param>
            /// <param name="outTranIdArray"></param>
            /// <param name="userId"></param>
            /// <returns></returns>
            public InvokeResult SaveStepTran(int stepId, List<int> inTranIdList, List<int> waitTranIdArray, List<int> outTranIdArray, int userId)
            {
                InvokeResult result = new InvokeResult();
                try
                {
                    using (TransactionScope scope = new TransactionScope())
                    {
                        var step =FindById(stepId.ToString());
                        var oldTranList = step.ChildBsonList("StepTransaction").ToList();
                        #region 进入事务
                        var oldInTranList = oldTranList.Where(m => m.Int("type") == 0).ToList();
                        var delInTranList = oldInTranList.Where(m => !inTranIdList.Contains(m.Int("transactionId"))).ToList();
                        var newInTranList = new List<BsonDocument>();
                        foreach (var tranId in inTranIdList)
                        {
                            if (oldInTranList.Where(m => m.Int("transactionId") == tranId).Count() == 0)
                            {
                               var newTran= new BsonDocument();
                               newTran.Add("flowId", step.Text("flowId"));
                               newTran.Add("stepId", stepId);
                               newTran.Add("transactionId", tranId);
                                newTran.Add("type",0);
                                newInTranList.Add(newTran);
                            }
                        }
                        if (delInTranList.Count > 0)
                            dataOp.QuickDelete("StepTransaction", delInTranList);
                        if (newInTranList.Count > 0)
                            dataOp.QuickInsert("StepTransaction", newInTranList);
                         #endregion

                        #region 等待事务
                        var oldWaitTranList = oldTranList.Where(m => m.Int("type") == 1).ToList();
                        var waitTranIdList = waitTranIdArray.ToList();
                        var delWaitTranList = oldWaitTranList.Where(m => !waitTranIdArray.Contains(m.Int("transactionId"))).ToList();
                        var newWaitTranList = new List<BsonDocument>();
                        foreach (var tranId in waitTranIdList)
                        {
                            if (oldWaitTranList.Where(m => m.Int("transactionId") == tranId).Count() == 0)
                            {

                                 var newTran= new BsonDocument();
                                newTran.Add("flowId",step.Text("flowId"));
                                newTran.Add("stepId",stepId);
                                newTran.Add("transactionId",tranId);
                                newTran.Add("type",1);
                                newInTranList.Add(newTran);
                                newWaitTranList.Add(newTran);
                            }
                        }
                        if (delWaitTranList.Count > 0)
                            dataOp.QuickDelete("StepTransaction", delWaitTranList);
                          
                        if (newWaitTranList.Count > 0)
                            dataOp.QuickInsert("StepTransaction", newWaitTranList);
                           
                        #endregion

                        #region 跳出事务
                        var oldOutTranList = oldTranList.Where(m => m.Int("type") == 2).ToList();
                        var outTranIdList = outTranIdArray.ToList();
                        var delOutTranList = oldOutTranList.Where(m => !outTranIdArray.Contains(m.Int("transactionId"))).ToList();
                        var newOutTranList = new List<BsonDocument>();
                        foreach (var tranId in outTranIdList)
                        {
                            if (oldOutTranList.Where(m => m.Int("transactionId") == tranId).Count() == 0)
                            {
                                 var newTran = new BsonDocument();
                                newTran.Add("flowId", step.Text("flowId"));
                                newTran.Add("stepId", stepId);
                                newTran.Add("transactionId", tranId);
                                newTran.Add("type", 2);
                                newInTranList.Add(newTran);
                                newOutTranList.Add(newTran);
                            }
                        }
                        if (delOutTranList.Count > 0)
                            dataOp.QuickDelete("StepTransaction", delOutTranList);
                           
                        if (newOutTranList.Count > 0)
                            dataOp.QuickInsert("StepTransaction", newOutTranList);
                      
                        #endregion
                       
                        scope.Complete();
                    }
                    result.Status = Status.Successful;
                }
                catch (Exception ex)
                {
                    result.Status = Status.Failed;
                    result.Message = ex.Message;
                }
                return result;
            }

            #endregion
  

    }

}
