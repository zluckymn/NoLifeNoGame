using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Yinhe.ProcessingCenter.DataRule;
using MongoDB.Bson;

///<summary>
///产品系列
///</summary>
namespace Yinhe.ProcessingCenter.Business.ProductDevelop
{
    /// <summary>
    /// 客群分析特征处理类
    /// </summary>
    public class ProductSegmentFeatureBll
    {
        #region 构造函数
        /// <summary>
        /// 类私有变量
        /// </summary>
        private DataOperation dataOp = null;
        private string tableName = "ProductSegmentFeature";
        /// <summary>
        /// 封闭当前默认构造函数
        /// </summary>
        private ProductSegmentFeatureBll()
        {
            dataOp = new DataOperation();
        }

        private ProductSegmentFeatureBll(DataOperation _dataOp)
        {
            dataOp = _dataOp;
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static ProductSegmentFeatureBll _()
        {
            return new ProductSegmentFeatureBll();
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static ProductSegmentFeatureBll _(DataOperation _dataOp)
        {
            return new ProductSegmentFeatureBll(_dataOp);
        }

         
        
         
        #endregion

        #region 初始化系列的客群分析特征 +InitSegmentFeatures(string seriesId)
        /// <summary>
        /// 初始化系列的客群分析特征
        /// </summary>
        /// <param name="seriesId"></param>
        public void InitSegmentFeatures(string seriesId,string relId)
        {
            var storageDatas = new List<StorageData>();
            var dic = new Dictionary<SegmentFeatureType, string[]>();
            dic.Add(SegmentFeatureType.Age, new string[] { "客户年龄(岁)", "数量" });
            dic.Add(SegmentFeatureType.Area, new string[] { "所属区域", "数量" });
            dic.Add(SegmentFeatureType.PayType, new string[] { "付款方式", "数量" });
            dic.Add(SegmentFeatureType.YearCount, new string[] { "按揭年限(年)", "数量" });
            dic.Add(SegmentFeatureType.LoanAmount, new string[] { "按揭贷款额(万)", "数量" });
            dic.Add(SegmentFeatureType.OldClient, new string[] { "", "" });

            foreach (var item in Enum.GetValues(typeof(SegmentFeatureType)))
            {
                string name = EnumDescription.GetFieldText(item);
                SegmentFeatureType type = (SegmentFeatureType)item;
                int featureType = (int)item;
                var feature = new BsonDocument("seriesId", seriesId)
                            .Add("relId",relId)
                            .Add("name", name)
                            .Add("content", "")
                            .Add("featureType", featureType.ToString())
                            .Add("dataKeyName", dic[type][0])
                            .Add("dataValueName", dic[type][1]);
                storageDatas.Add(new StorageData
                {
                    Name = "ProductSegmentFeature",
                    Document = feature,
                    Type = StorageType.Insert
                });
            }
            dataOp.BatchSaveStorageData(storageDatas);
        }
        #endregion
    }
}
