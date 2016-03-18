using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using Yinhe.ProcessingCenter.DataRule;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using MongoDB.Driver;

namespace Yinhe.ProcessingCenter.Business.DesignManage
{
    public class GetRecentBll
    {
        #region 构造函数
        /// <summary>
        /// 类私有变量
        /// </summary>
        private DataOperation dataOp = null;

        /// <summary>
        /// 封闭当前默认构造函数
        /// </summary>
        public GetRecentBll()
        {
            dataOp = new DataOperation();
        }

        public GetRecentBll(DataOperation _dataOp)
        {
            dataOp = _dataOp;
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static GetRecentBll _()
        {
            return new GetRecentBll();
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static GetRecentBll _(DataOperation _dataOp)
        {
            return new GetRecentBll(_dataOp);
        }
        #endregion

        #region 公共查询方法

        #region 获取最近浏览工程 IEnumerable<BsonDocument> GetRecentProjEngList(int userId, int maxLength, string typeName)
        /// <summary>
        /// 获取最近浏览工程
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="maxLength"></param>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public IEnumerable<BsonDocument> GetRecentProjEngList(int userId, int length, string typeName)
        {
            const int defaultLength = 6;
            var maxLength = length < 1 ? defaultLength : length;
            const string recentProjEngTb = "XH_DesignManage_Recent";//用户最近浏览工程表名
            const string projEngTb = "XH_DesignManage_ProjEngineering";//项目下工程表名
            var allProjEngIds = dataOp.FindAllByQuery(recentProjEngTb,
                    Query.And(
                        Query.EQ("userId", userId.ToString()),
                         Query.EQ("typeName", typeName)
                    )
                ).OrderBy(i => i.Int("order")).Take(maxLength).Select(i => i.Text("keyValue")).ToList();
            var result = dataOp.FindAllByQuery(projEngTb,
                    Query.In("projEngId", allProjEngIds.Select(i => (BsonValue)i.ToString()))
                ).AsEnumerable();
            result = from id in allProjEngIds
                     join r in result on id equals r.Text("projEngId")
                     select r;
            return result;
        }
        #endregion

        #region 获取最近浏览供应商列表 IEnumerable<BsonDocument> GetRecentSupplierList(int userId, int maxLength, string typeName)
        /// <summary>
        /// 获取最近浏览供应商列表
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="maxLength"></param>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public IEnumerable<BsonDocument> GetRecentSupplierList(int userId, int maxLength, string typeName)
        {
            const string recentProjEngTb = "XH_DesignManage_Recent";//用户最近浏览工程表名
            const string supTb = "XH_Supplier_Supplier";//项目下工程表名
            var allProjEngIds = dataOp.FindAllByQuery(recentProjEngTb,
                    Query.And(
                        Query.EQ("userId", userId.ToString()),
                         Query.EQ("typeName", typeName)
                    )
                ).OrderBy(i => i.Int("order")).Take(maxLength).Select(i => i.Text("keyValue")).ToList();
            var result = dataOp.FindAllByQuery(supTb,
                    Query.In("supplierId", allProjEngIds.Select(i => (BsonValue)i.ToString()))
                ).AsEnumerable();
            result = from id in allProjEngIds
                     join r in result on id equals r.Text("supplierId")
                     select r;
            return result;
        }
        #endregion
        
        #endregion

        #region 公共操作方法

        #region 保存最近浏览的关键值 InvokeResult SaveRecent(string keyValue, int length, int userId, string typeName)
        /// <summary>
        /// 保存最近浏览的关键值 
        /// </summary>
        /// <param name="keyValue">关键值</param>
        /// <param name="length"></param>
        /// <param name="userId"></param>
        /// <param name="typeName">类别,同一张表的对象也可能分为不同类别，因此不采用tableName进行存储</param>
        /// <returns></returns>
        public InvokeResult SaveRecent(string keyValue, int length, int userId, string typeName)
        {
            const string recentProjEngTb = "XH_DesignManage_Recent";//用户最近浏览工程表名

            InvokeResult result = new InvokeResult() { Status = Status.Successful };
            if (string.IsNullOrWhiteSpace(keyValue) || keyValue == "0")//如果传入值为空或0则不进行更新
            {
                result.Status = Status.Successful;
                return result;
            }
            const int defaultLength = 6;
            var maxLength = length < 1 ? defaultLength : length;

            var allOldRels = dataOp.FindAllByQuery(recentProjEngTb,
                    Query.And(
                        Query.EQ("userId", userId.ToString()),
                         Query.EQ("typeName", typeName)
                    )
                ).OrderBy(i => i.Int("order")).Take(maxLength).ToList();
            var newValues = allOldRels.Select(i => i.Text("keyValue")).ToList();

            #region 最近浏览没有当前传入值的时候插入当前传入的值
            List<StorageData> datalist = new List<StorageData>();
            if (!newValues.Contains(keyValue))
            {
                newValues.Insert(0, keyValue);
                if (newValues.Count() > maxLength)
                {
                    newValues = newValues.Take(maxLength).ToList();
                }
                try
                {
                    for (var i = 0; i < newValues.Count(); i++)
                    {
                        StorageData data = new StorageData();
                        var newValue = newValues.ElementAt(i);
                        if (i < allOldRels.Count())
                        {
                            var tempEng = allOldRels.ElementAt(i);
                            data.Name = recentProjEngTb;
                            data.Query = Query.EQ("recId", tempEng.Text("recId"));
                            data.Document = new BsonDocument(){
                            {"keyValue",newValue.ToString()}
                        };
                            data.Type = StorageType.Update;
                            datalist.Add(data);
                        }
                        else
                        {
                            data.Name = recentProjEngTb;
                            data.Document = new BsonDocument(){
                            {"keyValue",newValue.ToString()},
                            {"typeName",typeName.ToString()},
                            {"userId",userId.ToString()}
                        };
                            data.Type = StorageType.Insert;
                            datalist.Add(data);
                        }
                    }
                    result = dataOp.BatchSaveStorageData(datalist);
                    if (result.Status == Status.Failed)
                    {
                        return result;
                    }
                }
                catch (System.Exception ex)
                {
                    result.Status = Status.Failed;
                    result.Message = ex.Message;
                    return result;
                }
            }
            #endregion

            return result;

        }
        #endregion
        
        #endregion
    }
}
