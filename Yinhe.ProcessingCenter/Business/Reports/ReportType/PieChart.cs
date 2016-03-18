using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Web.UI;
using System.Web.UI.DataVisualization.Charting;
  
using Yinhoo.Utilities.Core.Extensions;
///<summary>
///系统报表
///</summary>
namespace Yinhe.ProcessingCenter.Reports
{
    /// <summary>
    /// 饼图报表处理类
    /// </summary>
    public class PieChart:ReportingChart
    {
        #region 构造函数
        /// <summary>
        /// PieChart
        /// </summary>
        /// <param name="report"></param>
        /// <param name="data"></param>
        /// <param name="prm"></param>
        public PieChart(List<ReportingData> data, Dictionary<string, string> prm)
            : base(data, prm)
        {
            this.InitChart();
        }

        /// <summary>
        /// PieChart
        /// </summary>
        /// <param name="report"></param>
        /// <param name="data"></param>
        /// <param name="prm"></param>
        public PieChart(List<ReportingSerie> data, Dictionary<string, string> prm)
            : base(data, prm)
        {
            this.InitChart();
        }
        /// <summary>
        /// 检测报表是否有效
        /// </summary>
        public override bool ValidataReporting
        {
            get
            {
                return this.CheckValidata();
            }
        }
        #endregion


    }
}
