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
    /// 更新计划状态为进行中
    /// </summary>
    class CompleteInstanceStatus : IExecuteTran
    {
        /// <summary>
        /// 修改计划状态为进行中
        /// </summary>
        /// <param name="UserId"></param>
        /// <param name="instance"></param>
        /// <param name="stepTran"></param>
        public void ExecuteTran(int UserId, BsonDocument instance, BsonDocument stepTran)
        {
                var  DataOp=new DataOperation();
                if(instance!=null)
                {
                    var approvalAmount = instance.Double("approvedAmount");
                    double approveAmount = 200000;
                    if (stepTran != null )
                    {
                        var tranStoreObj = stepTran.SourceBson("transactionId");
                        if (tranStoreObj != null && tranStoreObj.Double("approvedAmount")!=0)
                        {
                            approveAmount = tranStoreObj.Double("approvedAmount");
                        }
                        
                    }
                    if (approvalAmount < approveAmount)//小于20w流程将计就计金额数200000
                    {
                        var updateBson = new BsonDocument();
                        updateBson.Add("instanceStatus", "1");
                        DataOp.Update(instance, updateBson);
                    }
                }
     
        }
    }

}
