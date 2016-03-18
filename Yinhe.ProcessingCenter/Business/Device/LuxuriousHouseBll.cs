using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using Yinhe.ProcessingCenter.DataRule;
using Yinhe.ProcessingCenter.Reports;

///<summary>
///市调豪宅相关
///</summary>
namespace Yinhe.ProcessingCenter.Business.LuxuriousHouse
{
    /// <summary>
    /// 豪宅相关处理类
    /// </summary>
    public class LuxuriousHouseBll
    {
        private DataOperation dataOp = null;

        string[] docExts = new string[] { ".xls", ".xlsx" };
        Dictionary<string, string> AttrValueDic = null;
        Dictionary<string, string> BrandDic = null;


        #region 构造函数
        /// <summary>
        /// 类私有变量
        /// </summary>
        
        /// <summary>
        /// 封闭当前默认构造函数
        /// </summary>
        public LuxuriousHouseBll()
        {
            dataOp = new DataOperation();
        }

        /// <summary>
        /// 封闭当前默认构造函数
        /// </summary>
        private LuxuriousHouseBll(DataOperation ctx)
        {
            dataOp = ctx;
        }


        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static LuxuriousHouseBll _()
        {
            return new LuxuriousHouseBll();
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static LuxuriousHouseBll _(DataOperation ctx)
        {
            return new LuxuriousHouseBll(ctx);
        }
        #endregion

        #region 获取统计数据值
        /// <summary>
        /// 获取统计数据值
        /// </summary>
        /// <param name="tabName"></param>
        /// <param name="g"></param>
        /// <returns></returns>
        public static double GetStatistics(string tabName, IGrouping<string, BsonDocument> g)
        {
            double statistics = 0.0;
            switch (tabName)
            {
                case "dealArea":
                    statistics = g.Sum(c => c.Int("area"));
                    break;
                case "dealSuiteCount":
                    statistics = g.Count();
                    break;
                case "dealAvgPrice":
                    statistics = g.Sum(c => c.Int("area")) == 0 ? 0 : g.Sum(new Func<BsonDocument, long>(c => c.Int("totalPrice"))) / g.Sum(c => c.Int("area"));
                    break;
                case "dealTotalPrice":
                    statistics = g.Sum(new Func<BsonDocument, long>(c => c.Int("totalPrice")));
                    break;
            }
            return statistics;
        } 
        #endregion


        public static double GetStatistics(string tabName, IEnumerable<BsonDocument> g)
        {
            double statistics = 0.0;
            switch (tabName)
            {
                case "dealArea":
                    statistics = g.Sum(c => c.Int("area"));
                    break;
                case "dealSuiteCount":
                    statistics = g.Count();
                    break;
                case "dealAvgPrice":
                    statistics = g.Sum(c => c.Int("area")) == 0 ? 0 : g.Sum(new Func<BsonDocument, long>(c => c.Int("totalPrice"))) / g.Sum(c => c.Int("area"));
                    break;
                case "dealTotalPrice":
                    statistics = g.Sum(new Func<BsonDocument, long>(c => c.Int("totalPrice")));
                    break;
            }
            return statistics;
        } 

        public static double GetStatisticsByProjId(string tabName, int projId,IEnumerable<BsonDocument> tarBuildings)
        {
            double statistics = 0;
            var buildings=tarBuildings.Where(c=>c.Int("projId")==projId).ToList();
            switch (tabName)
            {
                case "dealArea":
                    statistics = buildings.Sum(c => c.Int("area"));
                    break;
                case "dealSuiteCount":
                    statistics = buildings.Count();
                    break;
                case "dealAvgPrice":
                    statistics = buildings.Sum(c => c.Int("area")) == 0 ? 0 : buildings.Sum(new Func<BsonDocument, long>(c => c.Int("totalPrice"))) / buildings.Sum(c => c.Int("area"));
                    break;
                case "dealTotalPrice":
                    statistics = buildings.Sum(new Func<BsonDocument, long>(c => c.Int("totalPrice")));
                    break;
            }
            return statistics;
        }

        #region +string GetFirstSaleDateByProjId(string projId) 根据项目ID获取最早销售记录中的销售年月
        /// <summary>
        /// 根据项目ID获取最早销售记录中的销售年月
        /// </summary>
        /// <returns></returns>
        public DateTime GetFirstSaleDateByProjId(string projId)
        {
            var dt = new DateTime(DateTime.Now.AddMonths(-18).Year, DateTime.Now.AddMonths(-18).Month, 1);
            var firstDate = dataOp.FindAllByKeyVal("LuxuriousHouse_Building", "projId", projId).Min(c => c.Date("completeDate"));
            if (firstDate != null)
                dt = new DateTime(firstDate.Year, firstDate.Month, 1);
            return dt;
        } 
        #endregion

        public static List<ReportingData> OrderHitResult(List<ReportingData> hitResult)
        {
            List<ReportingData> orderHitResult = new List<ReportingData>();
            var newOrderHitResult = hitResult.Where(c => c.statistics > 0).OrderBy(c => c.statistics).ToList();
            var lastIndex = newOrderHitResult.Count - 1;
            for (int index = 0; index <=lastIndex; index++,lastIndex--)
            {
                var item1 = newOrderHitResult[index];
                var item2 = newOrderHitResult[lastIndex];
                if (index == lastIndex)
                    orderHitResult.Add(item1);
                else
                {
                    orderHitResult.Add(item1);
                    orderHitResult.Add(item2);
                }
            }
            return orderHitResult;
        }       
    }
}
