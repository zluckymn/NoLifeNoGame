using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.IO;
using Yinhoo.Framework.Configuration;
///<summary>
///Aop截断过滤器
///</summary>
namespace Yinhe.ProcessingCenter.MvcFilters
{
   /// <summary>
   /// 通用异常处理类
   /// </summary>
    public class CommonExceptionAttribute : FilterAttribute, IExceptionFilter 
    {
      

        #region 重写执行代码
        public void OnException(ExceptionContext filterContext)
        {
            if (filterContext == null)
            {
                throw new ArgumentNullException("filterContext");
            }
             //&& filterContext.HttpContext.IsCustomErrorEnabled 是否重写自定义错误
            if (SysAppConfig.IsPublish == true&&!filterContext.IsChildAction && (!filterContext.ExceptionHandled ))
            {
                    string viewName = filterContext.RouteData.GetRequiredString("action");
                    string errorMessage = filterContext.Exception.Message;
                    //找不到相应的view异常
                    UrlHelper url = new UrlHelper(filterContext.RequestContext);
                    #region  添加操作日志 需要在网站下加入Configurations\NLog.config 文件才能添加日志
                    try
                    {
                        NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();
                        _log.Info(errorMessage);
                    }
                    catch (FileNotFoundException ex)
                    { }
                    catch (Exception)
                    { }
                    #endregion 
                    #region 页面跳转到错误页，或者开发模式页面
                    if (errorMessage.Contains(string.Format("'{0}'", viewName)) == true && errorMessage.Contains("not be found") == true)
                    {
                        //跳转默认页面
                        //Response.Redirect(Yinhoo.Autolink.Web.Inc.SysAppConfig.DefaultUrl, true);
                        //未完修改成默认页面
                        filterContext.Result = new RedirectResult(url.Action("Index", "ProductDevelopXH"));
                    }
                    else
                    {
                        #region  用于传递Exception对象，否则直接跳转
                        //跳转到错误信息
                        //HandleErrorInfo model = new HandleErrorInfo(filterContext.Exception, "Home", "ErrorInfo");
                        //ViewResult result = new ViewResult
                        //{
                        //    MasterName="",
                        //    ViewName = "ErrorInfo",
                        //    ViewData = new ViewDataDictionary<HandleErrorInfo>(model),
                        //    TempData = filterContext.Controller.TempData
                        //};
                        //filterContext.Result = result;
                        #endregion
                        filterContext.Result = new RedirectResult(url.Action("ErrorInfo", "Home" ));
                        //Response.Redirect("/Maintenance/Exception", true);
                    }
                    #endregion
                    filterContext.ExceptionHandled = true;
            }
         } 
        #endregion
    }
}
