using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace Yinhe.ProcessingCenter.Reports
{
    /// <summary>
    ///  报表展示接口
    /// </summary>
    public interface IReportingDisplay
    {
        bool ValidataReporting { get;}

        /// <summary>
        /// 输出html代码
        /// </summary>
        /// <param name="page"></param>
        void RenderHtml(System.Web.UI.Page page);
    }
}
