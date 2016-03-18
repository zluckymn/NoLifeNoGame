using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Yinhe.ProcessingCenter.DataRule;
using MongoDB.Bson;
using Yinhe.ProcessingCenter;
using MongoDB.Driver.Builders;
using System.Data;
namespace Yinhe.ProcessingCenter.Reports
{
    /// <summary>
    /// 报表展示处理类
    /// </summary>
    public abstract class ReportingDisplay
    {
        #region 键值常量
        /// <summary>
        /// 替换选择列
        /// </summary>
        public const string ReplaceSelectColumn = "#Column#";
        /// <summary>
        /// 替换选择列groupBy
        /// </summary>
        public const string ReplaceGroupByColumn = "#GroupByColumn#";
        /// <summary>
        /// 替换选择列过滤条件
        /// </summary>
        public const string ReplaceWhereColumn = "#WhereColumn#";
        /// <summary>
        /// 没有选择列的统计结果Key
        /// </summary>
        public const string DicRowKey = "#RowKey#";
        #endregion

        #region 报表参数
        /// <summary>
        /// 报表数据类型
        /// </summary>
        protected List<ReportingData> reportData = new List<ReportingData>();
        /// <summary>
        /// 报表数据集合类型ReportingSerie 包含ReportingData
        /// </summary>
        protected List<ReportingSerie> reportingSeries=new List<ReportingSerie>();
        

        /// <summary>
        /// 报表参数
        /// </summary>
        protected Dictionary<string, string> dicPrm;

        public bool ValidataReport{get;set;}

        #endregion

        /// <summary>
        /// 报表处理结果信息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 报表数据结果
        /// </summary>
        protected Dictionary<string, DataTable> dicReport = new Dictionary<string, DataTable>();

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        public ReportingDisplay() {

        }

        public void Init(List<ReportingData> data, Dictionary<string, string> prm)
        {
             
            this.reportData = data;
            this.reportingSeries.Add(new ReportingSerie() { reportingData = data, name = "DefaultSeries" });
            this.dicPrm = prm;
            this.dicReport = new Dictionary<string, DataTable>();
            this.ValidataReport = this.CheckValidateSql();
        }

        public void Init(List<ReportingSerie> seriesList, Dictionary<string, string> prm)
        {
            if (seriesList.Count() > 0)
            {
                var firSerie = seriesList.FirstOrDefault();
                if (firSerie != null)
                    this.reportData = firSerie.reportingData;
            }
            this.reportingSeries = seriesList;
            this.dicPrm = prm;
            this.dicReport = new Dictionary<string, DataTable>();
            this.ValidataReport = this.CheckValidateSql();
        }

        #endregion

        #region 保护函数
      

        private Dictionary<string, string> DicFieldMap;

        /// <summary>
        /// 获取字段的映射
        /// </summary>
        /// <param name="FieldName"></param>
        /// <returns></returns>
        protected string GetFieldMap(string FieldName)
        {
         
            if (DicFieldMap.ContainsKey(FieldName) == true)
            {
                return DicFieldMap[FieldName].ToString();
            }
            else {
                return FieldName;
            }
 
        }

        /// <summary>
        /// 检查是否为有效的Sql
        /// </summary>
        /// <returns></returns>
        protected virtual bool CheckValidateSql()
        {
            bool bResult = true;
          
            return bResult;
        }

        #endregion





    }
}
