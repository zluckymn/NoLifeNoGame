using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using MongoDB.Driver;
using System.Configuration;
namespace Yinhe.ProcessingCenter.DesignManage
{
    /// <summary>
    /// 设计费处理类
    /// </summary>
    public class DesignPayBll
    {
        #region 构造函数
        private DataOperation dataOp = null;
        private DesignPayBll()
        {
            dataOp = new DataOperation();
        }
        private DesignPayBll(DataOperation _dataOp)
        {
            this.dataOp = _dataOp;
        }
        public static DesignPayBll _()
        {
            return new DesignPayBll();
        }
        public static DesignPayBll _(DataOperation _dataOp)
        {
            return new DesignPayBll(_dataOp);
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 获取项目批复总设计费用
        /// ZHHY
        /// </summary>
        /// <param name="projId"></param>
        /// <returns></returns>
        public decimal GetTotalPrice(int projId)
        {
            decimal result = 0;
            //总体批复设计费审批流程
            string flowId = "";
            BsonDocument flowObj = new BsonDocument();
            if (ConfigurationManager.AppSettings["TotalDesignPayFlowId"] != null)
            {
                flowId = ConfigurationManager.AppSettings["TotalDesignPayFlowId"];
            }
            if (string.IsNullOrEmpty(flowId))
            {
                flowId = dataOp.FindAll("BusFlow").ToList().LastOrDefault(i => i.Text("name").Contains("总体设计费")).Text("flowId");
            }
            if (string.IsNullOrEmpty(flowId))
            {
                return result;
            }

            //旧数据手动填写的总体设计费金额,如果有手动填写的金额则取该值
            var oldTotalPay = string.Empty;
            var designPay = dataOp.FindOneByQuery("DesignPay", Query.EQ("projId", projId.ToString()));
            if (!designPay.IsNullOrEmpty())
            {
                oldTotalPay = designPay.Text("totalPrice");
            }
            if (!string.IsNullOrWhiteSpace(oldTotalPay))
            {
                result = designPay.Decimal("totalPrice");
                return result;
            }

            flowObj = dataOp.FindOneByQuery("BusFlow", Query.EQ("flowId", flowId));
            List<BsonDocument> taskList = dataOp.FindAllByQuery("XH_DesignManage_Task", Query.EQ("projId", projId.ToString())).ToList();
            List<BsonValue> taskIdList = taskList.Select(i => i.GetValue("taskId", string.Empty)).ToList();
            
            //找出已经完成的总体设计费审批
            List<BsonDocument> instanceList = dataOp.FindAllByQuery("BusFlowInstance",
                    Query.And(
                        Query.EQ("flowId", flowObj.Text("flowId")),
                        Query.EQ("tableName", "XH_DesignManage_Task"),
                        Query.EQ("referFieldName", "taskId"),
                        Query.In("referFieldValue", taskIdList),
                        Query.EQ("instanceStatus","1")
                    )
                ).ToList();
            if (instanceList.Count() > 0)
            {
                result = instanceList.Sum(i => i.Decimal("approvedAmount"));
            }
            
            return result;
        }
        #endregion
    }
}
