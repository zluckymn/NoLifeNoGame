using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Data;
 

namespace Yinhe.ProcessingCenter.Reports
{
    /// <summary>
    /// 定制报表serie基类
    /// </summary>
    public class ReportingSerie 
    {

        /// <summary>
        /// 分组Id
        /// </summary>
        public List<ReportingData> reportingData { get; set; }

        /// <summary>
        /// 分组名
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// 图表类型
        /// </summary>
        public string chartType { get; set; }

        /// <summary>
        /// 是否显示该系列的值标签
        /// </summary>
        private bool displayValueLable = true;
        public bool DisplayValueLable
        {
            get { return displayValueLable; }
            set { displayValueLable = value; }
        }
    }

}
