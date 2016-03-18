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
    class UpdatePlanStatus : IExecuteTran
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
                  var Obj = DataOp.FindOneByKeyVal(instance.Text("tableName"), instance.Text("referFieldName"), instance.Text("referFieldValue"));
                   if(Obj!=null)
                  {
                      InvokeResult result = DataOp.QuickUpdate("XH_DesignManage_Plan", "planId", Obj.Text("planId"), "status=" + (int)ProjectPlanStatus.Processing);
                   }
                }
     
        }
    }

}
