using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yinhe.ProcessingCenter.Reports
{
    /// <summary>
    /// 报表工厂映射类
    /// </summary>
    public class ReportingFactory
    {
        private static ReportingFactory _instance = new ReportingFactory();
        public static ReportingFactory Instance
        {
            get { return _instance; }
        }
        
        public IReportingDisplay Create(string Name,object[] paramObj)
        {
            IReportingDisplay reportingDisplay = null;
            try
            {
                Type type = Type.GetType(Name, true);
                
                reportingDisplay = (IReportingDisplay)Activator.CreateInstance(type, paramObj);
                if (reportingDisplay.ValidataReporting == false) {
                    reportingDisplay = null;
                }
            }
            catch (TypeLoadException e)
            {
                throw e;
            }
            return reportingDisplay;
        }
    }
}
