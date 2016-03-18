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
  public  class GenerateDesignChangeFileNum : IExecuteTran
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

                       var firstWords = "";
                       switch (instance.String("tableName"))
                       {
                           case "ProgrammeEvaluation":
                               firstWords = "SJPS";
                               break;
                           case "DesignChange":
                               firstWords = "SJBG";
                               break;
                           default: break;
                       }

                      //档案编号生成SJBG-201402-003
                       BusFlowFileNumberBll fileNumBll = BusFlowFileNumberBll._();
                       if (!string.IsNullOrEmpty(Obj.Text("fileNumCode"))|| fileNumBll.HasExist(instance.Text("tableName"), instance.Text("referFieldName"), instance.Text("referFieldValue"), firstWords))
                       {
                           return;
                       }
                       else
                       {
                           var result = fileNumBll.GenerateFileNum(instance.Text("tableName"), instance.Text("referFieldName"), instance.Text("referFieldValue"), firstWords);

                           if (result.Status == Status.Successful)
                           {
                               var fileNumObj = result.BsonInfo;
                               if (fileNumObj != null&&!string.IsNullOrEmpty(fileNumObj.Text("fileNumCode")))
                               {
                                   var updateBson = new BsonDocument().Add("fileNumCode", fileNumObj.Text("fileNumCode")); 
                                   DataOp.Update(Obj, updateBson);
                               }
                           }
                       }
                   }
                }
        }
    }

}
