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
    /// 定制报表出来处理基类
    /// </summary>
    public class ReportingData 
    {

        /// <summary>
        /// 分组Id
        /// </summary>
        public object groupById { get; set; }

        /// <summary>
        /// 分组名
        /// </summary>
        public string groupByName { get; set; }


        /// <summary>
        /// 分组统计值
        /// </summary>
        public double statistics { get; set; }
    }

}
