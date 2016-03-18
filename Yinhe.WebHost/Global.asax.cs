using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Yinhe.ProcessingCenter;
using System.Web.Hosting;
using System.IO;


namespace Yinhe.WebHost
{
    // 注意: 有关启用 IIS6 或 IIS7 经典模式的说明，
    // 请访问 http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }

        public static void RegisterRoutes(RouteCollection routes)
        {
            string controller = "CompanySite";
            string action = "Index";

            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.IgnoreRoute("Equipment/ChartImg.axd");
            routes.IgnoreRoute("DesignManage/ChartImg.axd");
            routes.IgnoreRoute("LuxuriousHouse/ChartImg.axd");
            routes.IgnoreRoute("Supplier/ChartImg.axd");
            routes.IgnoreRoute("DesignManage/ChartImg.axd");
            routes.IgnoreRoute("ProductDevelop/ChartImg.axd");
            routes.MapRoute(
                "Default", // 路由名称
                "{controller}/{action}/{id}", // 带有参数的 URL
                new { controller = controller, action = action, id = UrlParameter.Optional }, // 参数默认值
                new string[] { "Yinhe.WebHost.Controllers" }      //默认Controllers命名空间,防止添加插件DLL后路由出错
            );
        }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            HostingEnvironment.RegisterVirtualPathProvider(new AssemblyResourceProvider()); //注册新的VirtualProvider实例

            PluginHelper.InitializePluginsAndLoadViewLocations();           //初始化插件和视图位置信息

            RegisterRoutes(RouteTable.Routes);
           
            //RegisterGlobalFilters(GlobalFilters.Filters);
            //RegisterRoutes(RouteTable.Routes);

        }
        protected void Session_Start(object sender, EventArgs e) { string sessionId = Session.SessionID; }
    }
}