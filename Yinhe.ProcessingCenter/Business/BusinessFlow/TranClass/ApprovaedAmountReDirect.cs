using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using Yinhe.ProcessingCenter.DataRule;
using MongoDB.Bson;
///<summary>
///工作流程相关
///</summary>
namespace Yinhe.ProcessingCenter.BusinessFlow
{
    /// <summary>
    /// 事务金额自动跳转步骤
    /// </summary>
    public class ApprovaedAmountReDirect : IExecuteTran
    {
        private string[] _checkPropertyArray = new string[] { "isBuilding", "isAffectArea", "isConstruct", "isMaterial", "changeKindId", "isHandOver", "isReviewTime", "isAffectCheck", "isAffectHandRoom", "isNeedNoticeOwer" };
        public string[] checkPropertyArray
        {
            get
            {
                return _checkPropertyArray;
            }
        }
        /// <summary>
        /// 事务金额自动跳转步骤
        /// </summary>
        /// <param name="UserId"></param>
        /// <param name="instance"></param>
        /// <param name="stepTran"></param>
        public void ExecuteTran(int UserId, BsonDocument instance, BsonDocument stepTran)
        {
            var dataOp = new DataOperation();
                if(instance!=null)
                {
                    var tranStore = stepTran.SourceBson("transactionId");
                    ///设计变更属性
                    //var checkPropertyArray = new string[] { "isBuilding", "isAffectArea", "isConstruct", "isMaterial", "changeKindId", "isHandOver", "isReviewTime", "isAffectCheck", "isAffectHandRoom", "isNeedNoticeOwer" };
                    var approvedAmount = GetApprovedAmount(instance);//获取对应审批金额
                    if (tranStore != null )
                    {
                        var flowPosId = tranStore.Int("flowPosId");
                        if (flowPosId==0) return;
                        FlowInstanceHelper helper = new FlowInstanceHelper(dataOp);
                        if (canJumpStep(approvedAmount, tranStore) && CheckProperty(checkPropertyArray, instance, tranStore))
                        {
                            if (flowPosId == -1)// 默认直接结束流程
                            {
                                helper.ExecStepTranSysAction(instance, 0);
                            }
                            else
                            {
                                var hitStepObj = dataOp.FindAllByKeyVal("BusFlowStep", "flowId", instance.Text("flowId")).Where(c => c.Int("flowPosId") == flowPosId).FirstOrDefault();
                                if (hitStepObj != null)
                                {
                                    //流程跳转
                                    helper.ExecStepTranSysAction(instance, hitStepObj.Int("stepId"));
                                }
                            }
                        }
                    }
                }
        }

        /// <summary>
        /// 获取流程审批金额
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        public string GetApprovedAmount(BsonDocument instance)
        {
            var dataOp = new DataOperation();
            var approvedAmount=string.Empty;
            var tableName = instance.Text("tableName");
            var referFieldName = instance.Text("referFieldName");
            var referFieldValue = instance.Text("referFieldValue");
           
            switch (tableName)
            { 
                case "XH_DesignManage_Task": approvedAmount = instance.Text("approvedAmount"); break;
                case "DesignChange":
                    var designObj =   dataOp.FindOneByKeyVal(tableName, referFieldName, referFieldValue);
                    approvedAmount= designObj.Text("money");
                    break;
                default:
                     var obj =   dataOp.FindOneByKeyVal(tableName, referFieldName, referFieldValue);
                     approvedAmount = obj.Text("approvedAmount"); break;
            
            }
            return approvedAmount;
          }

        /// <summary>
        /// 其他属性匹配
        /// </summary>
        /// <param name="checkPropertyArray"></param>
        /// <param name="instance"></param>
        /// <param name="tranStore"></param>
        /// <returns></returns>
        public bool CheckProperty(string[] checkPropertyArray, BsonDocument instance, BsonDocument tranStore)
        {
            var dataOp = new DataOperation();
         
            var tableName = instance.Text("tableName");
            var referFieldName = instance.Text("referFieldName");
            var referFieldValue = instance.Text("referFieldValue");
            var obj = dataOp.FindOneByKeyVal(tableName, referFieldName, referFieldValue);
            var result = true;
            if (obj != null)
            {
                foreach (var prop in checkPropertyArray)
                {
                    if (!string.IsNullOrEmpty(tranStore.Text(prop)))
                    {
                        result = tranStore.Text(prop).Trim() == obj.Text(prop).Trim();
                        if (result == false) return false;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 获取流程审批对象属性
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        public string GetInstanceObjProperty(BsonDocument instance,string propertyName)
        {
            var dataOp = new DataOperation();
            var propertyValue = string.Empty;
            var tableName = instance.Text("tableName");
            var referFieldName = instance.Text("referFieldName");
            var referFieldValue = instance.Text("referFieldValue");
            var obj = dataOp.FindOneByKeyVal(tableName, referFieldName, referFieldValue);
            if (obj != null)
            {
                propertyValue = obj.Text(propertyName);
            }
            return propertyValue;
        }



         /// <summary>
         /// 条件判断是否满足跳转条件
         /// </summary>
         /// <param name="approvedAmount">审批金额</param>
         /// <param name="stepTran"></param>
         /// <returns></returns>
        public bool canJumpStep(string approvedAmount, BsonDocument tranStore)
        {  
           if (tranStore != null)
           {
               var result=false;
               var leftFormulaResult = false;
               var rightFormulaResult = false;
               if (!string.IsNullOrEmpty(tranStore.Text("leftFormula")))
               {
                   leftFormulaResult = CheckExpressionCondition(approvedAmount, tranStore.Text("leftFormula"), tranStore.Text("leftNum"));
               }
              
               if (!string.IsNullOrEmpty(tranStore.Text("rightFormula")))
               {
                   rightFormulaResult = CheckExpressionCondition(approvedAmount, tranStore.Text("rightFormula"), tranStore.Text("rightNum"));
               } 

                 if (!string.IsNullOrEmpty(tranStore.Text("leftFormula")) && !string.IsNullOrEmpty(tranStore.Text("rightFormula")))
                    {
                      
                       if (tranStore.Int("mode") == 0)
                       {
                           return leftFormulaResult && rightFormulaResult;
                       }
                       else
                       {
                           return leftFormulaResult || rightFormulaResult;
                       }
                    }

                   if (!string.IsNullOrEmpty(tranStore.Text("leftFormula")) && string.IsNullOrEmpty(tranStore.Text("rightFormula")))
                   {
                       return leftFormulaResult;
                   }

                   if (string.IsNullOrEmpty(tranStore.Text("leftFormula")) && !string.IsNullOrEmpty(tranStore.Text("rightFormula")))
                   {
                       return rightFormulaResult;
                   }

                   return false;
                 
                 
           }
           else
           {
               return false;
           
           }
        }

        /// <summary>
        /// 表达式过滤
        /// </summary>
        /// <param name="hitFeild"></param>
        /// <param name="conditionExp"></param>
        /// <returns></returns>
        bool CheckExpressionCondition(object A, string Symbol, object B)
        {
            try
            {
                double number = 0;
                double  value = 0;

                if (A!= null && !string.IsNullOrEmpty(A.ToString()))
                {
                    number = double.Parse(A.ToString());
                }
                if (B != null && !string.IsNullOrEmpty(B.ToString()))
                {
                    value = double.Parse(B.ToString());
                }
              
                
                switch (Symbol.Trim())
                {
                    case ">": return number > value;
                    case ">=": return number >= value;
                    case "<": return number < value;
                    case "<=": return number <= value;
                    case "=": return number == value;
                    case "!=": return number != value;
                    default: return false;
                }
            }
            catch (InvalidCastException ex)
            { }
            catch (Exception ex)
            { }

            return false;
        }

        


    }

}
