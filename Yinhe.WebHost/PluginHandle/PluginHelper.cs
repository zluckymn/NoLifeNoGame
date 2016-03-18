using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Reflection;
using System.Web.Mvc;
using Yinhe.ProcessingCenter;

namespace Yinhe.WebHost
{
    public class PluginHelper
    {
        /// <summary>
        /// 初始化插件和视图位置信息
        /// </summary>
        public static void InitializePluginsAndLoadViewLocations()
        {
            Assembly[] pluginAssemblies = AssemblyHelper.GetPluginAssemblies().ToArray();    //获取所有插件程序集

            List<string> viewLocations = new List<string>();        //视图位置列表

            foreach (Assembly plugin in pluginAssemblies)           //循环每个程序集,添加视图位置
            {
                var pluginAttribute = plugin.GetCustomAttributes(typeof(PluginViewLocations), false).FirstOrDefault() as PluginViewLocations;

                if (pluginAttribute != null)
                    viewLocations.AddRange(pluginAttribute.viewLocations);
            }

            //The PluginViewEngine is used to locate views in the assemlbies 
            ViewEngines.Engines.Clear();
            ViewEngines.Engines.Add(new PluginViewEngine(viewLocations.ToArray()));
        }

        #region 遍历所有插件的默认展示页
        /// <summary>
        /// 获取插件的默认页面
        /// </summary>
        /// <returns></returns>
        public static List<PluginAction> GetPluginActions()
        {
            Assembly[] pluginAssemblies = AssemblyHelper.GetPluginAssemblies().ToArray();

            List<PluginAction> pluginLinks = new List<PluginAction>();

            foreach (Assembly plugin in pluginAssemblies)
            {
                var pluginAttribute = plugin.GetCustomAttributes(typeof(PluginViewLocations), false).FirstOrDefault() as PluginViewLocations;

                var abc = plugin.GetCustomAttributes(false);
                var temp = plugin.GetCustomAttributes(typeof(PluginViewLocations), false).ToList();

                if (pluginAttribute.addLink)
                {
                    pluginLinks.Add(new PluginAction()
                    {
                        Name = pluginAttribute.name,
                        Action = pluginAttribute.action,
                        Controller = pluginAttribute.controller
                    });
                }
            }

            return pluginLinks;
        }

        /// <summary>
        /// 插件默认页面类
        /// </summary>
        public class PluginAction
        {
            public string Name { get; set; }
            public string Controller { get; set; }
            public string Action { get; set; }
        }
        #endregion
    }

    /// <summary>
    /// 文本日志类,临时类,临时日志处理
    /// </summary>
    public static class TextLog
    {
        public static void writeLog(string logInfo)
        {
            //FileStream fs = new FileStream("C:/log.txt", FileMode.Append);
            //StreamWriter sw = new StreamWriter(fs);
            ////开始写入
            //sw.Write(logInfo + "||" + DateTime.Now + "||\r\n");
            ////清空缓冲区
            //sw.Flush();
            ////关闭流
            //sw.Close();
            //fs.Close();
        }
    }
}