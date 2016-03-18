using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MongoDB.Bson;
using Yinhe.ProcessingCenter;
using System.Web.Security;

namespace Plugin_LifeDayGame.Controllers
{
    public class LifeDungeonController : Yinhe.ProcessingCenter.ControllerBase
    {
        //
        // GET: /Home/

        public ActionResult Index()
        {

            return View();
        }
       

        /// <summary>
        /// 记录用户成功登录的信息
        /// </summary>
        /// <param name="user"></param>
        /// <param name="rememberMe"></param>
        private void SetUserLoginInfo(BsonDocument user, string rememberMe)
        {
            string strUserName = user.String("userId") + "\\" + user.String("name") + "\\" + user.String("cardNumber");

            Identity identity = new Identity
            {
                AuthenticationType = "form",
                IsAuthenticated = true,
                Name = strUserName
            };

            Principal principal = new Principal { Identity = identity };

            HttpContext.User = principal;
            Session["UserId"] = user.String("userId");
            Session["UserName"] = user.String("name");
            Session["LoginName"] = user.String("loginName");
            Session["UserType"] = user.String("type");

            if (rememberMe.ToLower() != "on")
            {
                FormsAuthentication.SetAuthCookie(strUserName, false);
            }
            else
            {
                FormsAuthentication.SetAuthCookie(strUserName, true);
                HttpCookie lcookie = Response.Cookies[FormsAuthentication.FormsCookieName];
                lcookie.Expires = DateTime.Now.AddDays(7);
            }

            #region 记录登录日志
            dataOp.LogSysBehavior(SysLogType.Login, HttpContext);
            #endregion
        }

    }
}
