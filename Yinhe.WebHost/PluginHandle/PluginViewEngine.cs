using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Yinhe.WebHost
{
    public class PluginViewEngine : System.Web.Mvc.WebFormViewEngine
    {
        /// <summary>
        /// 插件视图构造函数,将视图位置,添加到默认查找中
        /// </summary>
        /// <param name="viewLocations"></param>
        public PluginViewEngine(string[] viewLocations)
            : base()
        {
            string[] tempArray = new string[ViewLocationFormats.Length + viewLocations.Length];
            ViewLocationFormats.CopyTo(tempArray, 0);

            for (int i = 0; i < viewLocations.Length; i++)
            {
                tempArray[ViewLocationFormats.Length + i] = viewLocations[i];
            }

            ViewLocationFormats = tempArray;

            PartialViewLocationFormats = ViewLocationFormats;
        }

        /// <summary>
        /// 判断虚拟文件路径是否来自插件的资源文件
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        private bool IsAppResourcePath(string virtualPath)
        {
            String checkPath = VirtualPathUtility.ToAppRelative(virtualPath);
            return checkPath.StartsWith("~/Plugins/", StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// 重写System.Web.Mvc.BuildManagerViewEngine.FileExists,获取一个值，该值指示文件是否在指定的虚拟文件系统（路径）中。
        /// </summary>
        /// <param name="controllerContext"></param>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        protected override bool FileExists(ControllerContext controllerContext, string virtualPath)
        {
            //If we have a virtual path, we need to override the super class behavior,
            //its implementation ignores custom VirtualPathProviders, unlike the super's super class. 
            //This code basically just reimplements the super-super class (VirtualPathProviderViewEngine) behavior for virtual paths.
            if (IsAppResourcePath(virtualPath))
            {
                return System.Web.Hosting.HostingEnvironment.VirtualPathProvider.FileExists(virtualPath);
            }
            else
                return base.FileExists(controllerContext, virtualPath);
        }
    }
}