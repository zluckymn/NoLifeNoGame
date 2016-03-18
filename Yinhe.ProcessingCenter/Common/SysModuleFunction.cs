using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Yinhe.ProcessingCenter
{
    /// <summary>
    /// 系统模块类
    /// </summary>
    public class SysModuleFunction
    {
        public int moduleFunctionId { get; set; }
        public string name { get; set; }
        public int modulId { get; set; }
        public int operatingId { get; set; }
        public string relyon { get; set; }
        public Nullable<int> dataObjId { get; set; }
        public string code { get; set; }
    }
}