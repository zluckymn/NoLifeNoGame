using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Yinhe.ProcessingCenter
{
    /// <summary>
    /// 程序集属性
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public class PluginViewLocations : Attribute
    {
        /// <summary>
        /// 视图位置信息
        /// </summary>
        public string[] viewLocations { get; set; } 
    
        /// <summary>
        /// 是否添加到连接
        /// </summary>
        public bool addLink { get; set; }

        /// <summary>
        /// 名称
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// 默认controller
        /// </summary>
        public string controller { get; set; }

        /// <summary>
        /// 默认Action
        /// </summary>
        public string action { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="viewLocations"></param>
        /// <param name="addLink"></param>
        public PluginViewLocations(string[] viewLocations, bool addLink)
        {
            this.viewLocations = viewLocations;
            this.addLink = addLink;
        }
    }
}