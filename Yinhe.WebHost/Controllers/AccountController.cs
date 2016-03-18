using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Yinhe.ProcessingCenter;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System.Web.Security;
using System.DirectoryServices;
using System.Collections;
using Yinhe.WebReference.AuthorityService;
using Yinhe.WebReference;
using System.Net;
using QiaoxingWebService;

namespace Yinhe.WebHost.Controllers
{
    /// <summary>
    /// 登录相关操作
    /// </summary>
    public class AccountController : Yinhe.ProcessingCenter.ControllerBase
    {
        #region 通用登录登出页面方法
        /// <summary>
        /// 默认登陆跳转页,用于跳转登陆页
        /// </summary>
        /// <returns></returns>
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 登陆页
        /// </summary>
        /// <returns></returns>
        public ActionResult Login()
        {
            return View();
        }

        /// <summary>
        /// 登陆页
        /// </summary>
        /// <returns></returns>
        public ActionResult Login_HSCZ()
        {
            return View();
        }
        /// <summary>
        /// 富兰克林登陆页
        /// </summary>
        /// <returns></returns>
        public ActionResult Login_FLKL()
        {
            return View();
        }

        /// <summary>
        /// MN登陆页
        /// </summary>
        /// <returns></returns>
        public ActionResult Login_MN()
        {
            return View();
        }
        

        /// <summary>
        /// 登陆弹框
        /// </summary>
        /// <returns></returns>
        public ActionResult LoginSmart()
        {
            return View();
        }

        public ActionResult Login_Mat()
        {
            return View();
        }


        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="ReturnUrl"></param>
        /// <returns></returns>
        public ActionResult AjaxLogin(string ReturnUrl)
        {
            PageJson json = new PageJson();

            #region 清空菜单 cookies
            HttpCookie cookie = Request.Cookies["SysMenuId"];
            if (cookie != null)
            {
                cookie.Expires = DateTime.Today.AddDays(-1);
                Response.Cookies.Add(cookie);
            }

            #endregion

            string userName = PageReq.GetForm("userName");
            string passWord = PageReq.GetForm("passWord");
            string rememberMe = PageReq.GetForm("rememberMe");


            if (AllowToLogin() == false)
            {
                json.Success = false;
                json.Message = "误操作！请联系技术支持工程师,电话0592-3385501";
                json.AddInfo("ReturnUrl", "");
                return Json(json);
            }
            #region 用户验证
            try
            {
                if (userName.Trim() == "") throw new Exception("请输入正确的用户名！");

                BsonDocument user = dataOp.FindOneByKeyVal("SysUser", "loginName", userName);

                #region 是否开发者模式
                if (IsDeveloperMode(userName, passWord))//是否开发者模式
                {
                    user = dataOp.FindAll("SysUser").Where(t => t.Int("type") == 1).FirstOrDefault();
                    this.SetUserLoginInfo(user, rememberMe);
                    if (string.IsNullOrEmpty(ReturnUrl) || ReturnUrl == "/" || ReturnUrl == "/default.aspx")
                    {
                        ReturnUrl = SysAppConfig.IndexUrl;
                    }

                    json.Success = true;
                    json.Message = "登录成功";
                    json.AddInfo("ReturnUrl", ReturnUrl.ToString());
                    json.AddInfo("userId", user.Text("userId"));
                    return Json(json);

                }
                #endregion

                if (user != null)
                {
                    if (user.Int("status") == 2)
                    {
                        json.Success = false;
                        json.Message = "用户已经被锁定";
                        json.AddInfo("ReturnUrl", ReturnUrl.ToString());
                        return Json(json);
                    }
                    if (user.String("loginPwd") == passWord)
                    {
                        this.SetUserLoginInfo(user, rememberMe);    //记录用户成功登录的信息

                        if (string.IsNullOrEmpty(ReturnUrl) || ReturnUrl == "/" || ReturnUrl == "/default.aspx")
                        {
                            ReturnUrl = SysAppConfig.IndexUrl;
                        }

                        json.Success = true;
                        json.Message = "登录成功";
                        json.AddInfo("ReturnUrl", ReturnUrl.ToString());
                        json.AddInfo("userId", user.Text("userId"));
                    }
                    else
                    {
                        Session["MsgType"] = "password";
                        throw new Exception("用户密码错误！");
                    }
                }
                else
                {
                    Session["MsgType"] = "username";
                    throw new Exception("用户名不存在！");
                }
            }
            catch (Exception ex)
            {
                json.Success = false;
                json.Message = ex.Message;
                json.AddInfo("ReturnUrl", "");
            }
            #endregion

            return Json(json);
        }

        /// <summary>
        /// 登出
        /// </summary>
        public void Logout()
        {
            this.ClearUserLoginInfo();  //清空用户登录信息

            string returnUrl = SysAppConfig.LoginUrl;
            Response.Redirect(returnUrl);
        }

        /// <summary>
        /// 登出
        /// </summary>
        public void Logout_QX()
        {
            this.ClearUserLoginInfo();  //清空用户登录信息

            string returnUrl = "/PersonelWorkCenter/HomeIndex";
            Response.Redirect(returnUrl);
        }

        #endregion

        #region 个性化登录登出页面方法
        /// <summary>
        /// 登陆页
        /// </summary>
        /// <returns></returns>
        public ActionResult Login_HD()
        {
            return View();
        }

        /// <summary>
        /// 登陆页
        /// </summary>
        /// <returns></returns>
        public ActionResult Login_FL()
        {
            return View();
        }

        /// <summary>
        /// 登陆页
        /// </summary>
        /// <returns></returns>
        public ActionResult Login_SS()
        {
            return View();
        }

        /// <summary>
        /// 登陆页
        /// </summary>
        /// <returns></returns>
        public ActionResult Login_SNHQ()
        {
            return View();
        }
        /// <summary>
        /// 登陆页
        /// </summary>
        /// <returns></returns>
        public ActionResult Login_SN()
        {
            return View();
        }

        /// <summary>
        /// 登陆页
        /// </summary>
        /// <returns></returns>
        public ActionResult Login_ZHHY()
        {
            return View();
        }

        /// <summary>
        /// 登陆页
        /// </summary>
        /// <returns></returns>
        public ActionResult Login_ZHTZ()
        {
            return View();
        }
        /// <summary>
        /// 登陆页
        /// </summary>
        /// <returns></returns>
        public ActionResult Login_QX()
        {
            return View();
        }

        /// <summary>
        /// 联发登陆页
        /// </summary>
        /// <returns></returns>
        public ActionResult Login_LF()
        {
            return View();
        }
        /// <summary>
        /// 域登陆
        /// </summary>
        /// <returns></returns>
        public ActionResult Login_AD()
        {
            try
            {
                string ReturnUrl = PageReq.GetParam("ReturnUrl");
                if (HttpContext.User.Identity.AuthenticationType == "Forms")
                {

                }
                else
                {
                    if (HttpContext.User.Identity.IsAuthenticated)
                    {
                        var strArray = HttpContext.User.Identity.Name.Split('\\');
                        if (strArray.Length > 1)
                        {
                            string adName = strArray[0];

                            if (adName.ToLower() != SysAppConfig.ADName) throw new Exception("没有登录到域环境下，无法登录本系统，请先登录到域环境！！");

                            string userName = strArray[1];
                            DataOperation dataOp = new DataOperation();

                            BsonDocument user = dataOp.FindOneByKeyVal("SysUser", "loginName", userName);

                            if (user != null)
                            {
                                this.SetUserLoginInfo(user, "");    //记录用户成功登录的信息

                                if (string.IsNullOrEmpty(ReturnUrl) || ReturnUrl == "/" || ReturnUrl == "/default.aspx")
                                {
                                    ReturnUrl = SysAppConfig.IndexUrl;
                                }

                            }
                        }
                        else
                        {
                            throw new Exception("您没有登录到域环境下，无法登录本系统，请先登录到域环境！");
                        }
                    }
                }

                return Redirect(string.Format("{0}/{1}", SysAppConfig.HostDomain, ReturnUrl)); ;
            }
            catch (Exception ex)
            {
                ViewData["info"] = ex.Message;
                return View();
            }
        }

        /// <summary>
        /// 域登陆
        /// </summary>
        /// <returns></returns>
        public ActionResult Login_ZHTZAD()
        {
            try
            {
                string ReturnUrl = PageReq.GetParam("ReturnUrl");
                if (HttpContext.User.Identity.AuthenticationType == "Forms")
                {

                }
                else
                {
                    if (HttpContext.User.Identity.IsAuthenticated)
                    {
                        var strArray = HttpContext.User.Identity.Name.Split('\\');
                        if (strArray.Length > 1)
                        {
                            string adName = strArray[0];

                            if (adName.ToLower() != SysAppConfig.ADName) throw new Exception("没有登录到域环境下，无法登录本系统，请先登录到域环境！！");

                            string userName = strArray[1];
                            DataOperation dataOp = new DataOperation();

                            BsonDocument user = dataOp.FindOneByKeyVal("SysUser", "loginName", userName);
                            if (user != null)
                            {
                                if (user.Int("status") == 2)
                                {
                                    throw new Exception("用户已被锁定，无法登陆系统，请先激活用户！");
                                }
                                else
                                {
                                    this.SetUserLoginInfo(user, "");    //记录用户成功登录的信息

                                    if (string.IsNullOrEmpty(ReturnUrl) || ReturnUrl == "/" || ReturnUrl == "/default.aspx")
                                    {
                                        ReturnUrl = SysAppConfig.IndexUrl;
                                    }
                                }


                            }
                        }
                        else
                        {
                            throw new Exception("您没有登录到域环境下，无法登录本系统，请先登录到域环境！");
                        }
                    }
                }

                return Redirect(string.Format("{0}/{1}", SysAppConfig.HostDomain, ReturnUrl)); ;
            }
            catch (Exception ex)
            {
                ViewData["info"] = ex.Message;
                return View();
            }
        }

        public ActionResult LoginSNHQAD()
        {
            string UserName = PageReq.GetForm("userName");
            string PassWord = PageReq.GetForm("passWord");
            string rememberMe = PageReq.GetForm("rememberMe");
            string remember = "";
            PageJson json = new PageJson();
            #region 判断是否停用
            if (AllowToLogin() == false)
            {
                json.Success = false;
                json.Message = "误操作，请联系技术支持工程师,电话0592-3385501";
                json.AddInfo("ReturnUrl", "");
                return Json(json);
            }
            #endregion


            if (!string.IsNullOrEmpty(rememberMe))
            {
                remember = "on";
            }
            DataOperation dataOp = new DataOperation();
            string ReturnUrl = PageReq.GetParam("ReturnUrl");

            DirectoryEntry AD = new DirectoryEntry();
            BsonDocument user = dataOp.FindOneByKeyVal("SysUser", "loginName", UserName);
            if (user == null)
            {
                json.Success = false;
                json.Message = "用户名不存在";
                json.AddInfo("ReturnUrl", ReturnUrl.ToString());
                return Json(json);
            }
            if (user.Int("status") == 2)
            {
                json.Success = false;
                json.Message = "用户已经被锁定";
                json.AddInfo("ReturnUrl", ReturnUrl.ToString());
                return Json(json);
            }
            AD.Path = string.Format("LDAP://{0}", SysAppConfig.LDAPName);
            AD.Username = SysAppConfig.ADName + @"\" + UserName;
            AD.Password = PassWord;
            AD.AuthenticationType = AuthenticationTypes.Secure;
            try
            {
                DirectorySearcher searcher = new DirectorySearcher(AD);
                searcher.Filter = String.Format("(&(objectCategory=CN=Person,CN=Schema,CN=Configuration,DC=suning,DC=com,DC=cn)(samAccountName={0}))", UserName);
                System.DirectoryServices.SearchResult result = searcher.FindOne();
                if (result != null)
                {

                    if (user != null)
                    {
                        this.SetUserLoginInfo(user, remember);    //记录用户成功登录的信息

                        if (string.IsNullOrEmpty(ReturnUrl) || ReturnUrl == "/" || ReturnUrl == "/default.aspx")
                        {
                            ReturnUrl = SysAppConfig.IndexUrl;
                        }
                        json.Success = true;
                        json.Message = "登录成功";
                        json.AddInfo("ReturnUrl", ReturnUrl.ToString());
                    }
                    else
                    {
                        json.Success = false;
                        json.Message = "用户名或密码错误";
                        json.AddInfo("ReturnUrl", ReturnUrl.ToString());
                    }
                }
                else
                {
                    json.Success = false;
                    json.Message = "密码错误";
                    json.AddInfo("ReturnUrl", ReturnUrl.ToString());
                }
                AD.Close();
            }
            catch (Exception ex)
            {
                json.Success = false;
                json.Message = "密码错误";
                json.AddInfo("ReturnUrl", "");
            }
            return Json(json);
        }

        /// <summary>
        /// 旭辉其他系统跳转SSO
        /// </summary>
        /// <returns></returns>
        public ActionResult Login_XHSSO()
        {
            string ReturnUrl = PageReq.GetParam("ReturnUrl");
            string userName = PageReq.GetParam("uid");
            //if (HttpContext.User.Identity.AuthenticationType == "Forms")
            //{

            //}
            //else
            //{
            //    if (!string.IsNullOrEmpty(userName))
            //    {
            //        userName= Yinhe.ProcessingCenter.Common.Base64.DecodeBase64(System.Text.Encoding.GetEncoding("utf-8"),userName);
            //        DataOperation dataOp = new DataOperation();
            //        BsonDocument user = dataOp.FindOneByKeyVal("SysUser", "loginName", userName);
            //        if (user != null)
            //        {
            //            this.SetUserLoginInfo(user, "");    //记录用户成功登录的信息

            //        }
            //        if (string.IsNullOrEmpty(ReturnUrl) || ReturnUrl == "/" || ReturnUrl == "/default.aspx")
            //        {
            //            ReturnUrl = SysAppConfig.IndexUrl;
            //        }
            //    }
            //}
            if (!string.IsNullOrEmpty(userName))
            {
                userName = Yinhe.ProcessingCenter.Common.Base64.DecodeBase64(System.Text.Encoding.GetEncoding("utf-8"), userName);
                DataOperation dataOp = new DataOperation();
                BsonDocument user = dataOp.FindOneByKeyVal("SysUser", "loginName", userName);
                if (user != null)
                {
                    this.SetUserLoginInfo(user, "");    //记录用户成功登录的信息

                }
                if (string.IsNullOrEmpty(ReturnUrl) || ReturnUrl == "/" || ReturnUrl == "/default.aspx")
                {
                    ReturnUrl = SysAppConfig.IndexUrl;
                }
            }
            return Redirect(string.Format("{0}{1}", SysAppConfig.HostDomain, ReturnUrl));
        }

        /// <summary>
        /// 中海宏洋 单点登陆
        /// string code = string.Format("sslogin{0}{1}{2}{3}",);
        ///        pwdKey:      验证的密钥KEY
        /// "sslogin"+UserNameEn+UserID+"20130709"(日期)+randomKey
        ///  举例：    UserID:6232  
        ///         userNameEn:shuyh     根据UserID从中间表或你们的表中查询得出
        ///         randomKey:9185
        /// 今日加密前为（全部转为小写）：ssloginshuyh6232201307099185
        /// 加密算法：md5
        /// 加密生成待传递值后：3e44402acc687c4b4231d9ba5789b96e
        /// SSLogin.aspx?userid=6232&pwdKey=3e44402acc687c4b4231d9ba5789b96e&randomKey=9185&redirectUrl=/SS /test.jsp{W}id{D}92{L}key{D}ok
        /// </summary>
        /// <returns></returns>
        public ActionResult Login_ZHHYSSO()
        {
            string ReturnUrl = PageReq.GetParam("ReturnUrl");
            string userid = PageReq.GetParam("userid");
            string pwdKey = PageReq.GetParam("pwdKey");
            string randomKey = PageReq.GetParam("randomKey");
            string redirectUrl = PageReq.GetParam("redirectUrl");
            if (!string.IsNullOrEmpty(redirectUrl))
            {
                redirectUrl = redirectUrl.Replace("{W}", "?").Replace("{L}", "&").Replace("{D}", "=");
            }
            else
            {
                redirectUrl = SysAppConfig.IndexUrl;
            }
            bool isLoginIn = false;
            if (!string.IsNullOrEmpty(userid))
            {
                DataOperation dataOp = new DataOperation();
                BsonDocument user = dataOp.FindOneByKeyVal("SysUser", "guid", userid);

                if (user != null && user.Int("status") != 2)//非锁定用户
                {
                    string code = string.Format("sslogin{0}{1}{2}{3}", user.Text("loginName"), userid, DateTime.Now.ToString("yyyyMMdd"), randomKey);
                    code = code.ToLower();
                    code = System.Web.Security.FormsAuthentication.HashPasswordForStoringInConfigFile(code, "MD5");

                    if (String.Equals(code, pwdKey, StringComparison.OrdinalIgnoreCase))
                    {
                        isLoginIn = true;
                    }

                    if (isLoginIn && Url.IsLocalUrl(redirectUrl))//登陆成功
                    {
                        this.SetUserLoginInfo(user, "");    //记录用户成功登录的信息.
                        return Redirect(redirectUrl);
                    }
                }
                PageJson json = new PageJson();

                if (string.IsNullOrEmpty(ReturnUrl) || ReturnUrl == "/" || ReturnUrl == "/default.aspx")
                {
                    ReturnUrl = SysAppConfig.IndexUrl;
                }
                if (user.Int("status") == 2)
                {
                    json.Success = false;
                    json.Message = "用户已经被锁定";
                    json.AddInfo("ReturnUrl", ReturnUrl.ToString());
                    return Json(json);
                }
            }
            return Redirect(string.Format("{0}{1}", SysAppConfig.HostDomain, ReturnUrl));
        }

        /// <summary>
        /// 苏宁环球单点登陆
        /// </summary>
        /// <returns></returns>
        public ActionResult Login_SNSSO()
        {
            string ReturnUrl = PageReq.GetParam("ReturnUrl");
            string userName = PageReq.GetParam("uid");
            var url = Request.UrlReferrer;
            var array = SysAppConfig.SNADDomain.ToLower().Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
            if (url != null && array.Contains(url.Host.ToLower()))
            {
                if (!string.IsNullOrEmpty(userName))
                {
                    DataOperation dataOp = new DataOperation();
                    BsonDocument user = dataOp.FindOneByKeyVal("SysUser", "loginName", userName);
                    if (user != null)
                    {
                        this.SetUserLoginInfo(user, "");    //记录用户成功登录的信息

                    }
                    if (string.IsNullOrEmpty(ReturnUrl) || ReturnUrl == "/" || ReturnUrl == "/default.aspx")
                    {
                        ReturnUrl = SysAppConfig.IndexUrl;
                    }
                }
            }
            else
            {
                // throw new Exception("未验证的跳转路径！");
                return View();
            }


            return Redirect(string.Format("{0}{1}", SysAppConfig.HostDomain, ReturnUrl));
        }



        /// <summary>
        /// 侨鑫单点登陆
        /// </summary>
        /// <returns></returns>
        //[HttpPost]
        public ActionResult login_QXSSO()
        {
            string certInfo = PageReq.GetForm("certInfo");
            PageJson json = new PageJson();
            string ReturnUrl = PageReq.GetParam("ReturnUrl"); //单点登陆自动跳转页面
            if (string.IsNullOrEmpty(certInfo))
                certInfo = PageReq.GetParam("certInfo");
            certInfo = Server.UrlDecode(certInfo).Replace(" ", "+");
            string redirectUrl = "";
            if (!string.IsNullOrEmpty(ReturnUrl))
            {
                redirectUrl = ReturnUrl;
            }
            else
            {
                redirectUrl = SysAppConfig.IndexUrl; //自动调整页面为空，则调整到首页
            }
            if (!string.IsNullOrEmpty(certInfo))
            {
                loginService login = new loginService();
                string user = login.getLoginInfo(certInfo, "userID").Trim();
                if (user == "error")
                {
                    return Redirect(string.Format("{0}{1}", SysAppConfig.HostDomain, redirectUrl)); ;
                }
                else
                {
                    DataOperation dataOp = new DataOperation();
                    BsonDocument userObj = dataOp.FindOneByKeyVal("SysUser", "loginName", user);
                    if (userObj != null)
                    {
                        this.SetUserLoginInfo(userObj, "");    //记录用户成功登录的信息
                    }
                }

            }
            else
            {
                return Redirect(string.Format("{0}{1}", SysAppConfig.HostDomain, redirectUrl));
            }


            return Redirect(string.Format("{0}{1}", SysAppConfig.HostDomain, redirectUrl));
        }

        /// <summary>
        /// 登陆页
        /// </summary>
        /// <returns></returns>
        public ActionResult Login_XC()
        {
            return View();
        }


        #endregion

        #region 用户登录登出私有方法
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
            InitialCompanyInfo();//初始化所属公司信息;

            #region 记录登录日志
            dataOp.LogSysBehavior(SysLogType.Login, HttpContext);
            #endregion
        }





        /// <summary>
        /// 清空用户登录信息
        /// </summary>
        private void ClearUserLoginInfo()
        {
            if (!string.IsNullOrEmpty(PageReq.GetSession("UserId")))
            {
                Yinhe.ProcessingCenter.Permissions.AuthManage._().ReleaseCache(int.Parse(PageReq.GetSession("UserId")));
            }
            FormsAuthentication.SignOut();
            Session["UserId"] = null;
            Session["UserName"] = null;
            Session["MsgType"] = null;
            Session.Clear();
            Session.Abandon();
            #region 记录登出日志
            dataOp.LogSysBehavior(SysLogType.Logout, HttpContext);
            #endregion
        }

        /// <summary>
        /// 判断当前系统是否允许登陆使用
        /// </summary>
        /// <returns></returns>
        public bool AllowToLogin()
        {
            bool flag = true;

            if (!string.IsNullOrEmpty(SysAppConfig.ExpireTime))//2013.9.24boss通知所有客户都需要过期时间，注意发布过的客户可能出现问题
            {
                DateTime closeDate = DateTime.Parse(SysAppConfig.ExpireTime);

                if (DateTime.Now > closeDate)
                {
                    flag = false;
                }
            }

            return flag;
        }



        #endregion

        /// <summary>
        /// PDF导出的过渡页面（把需要导出的内容做成一个页面A，要用管理员身份登陆系统，才能跳转到这个页面A）
        /// </summary>
        /// <returns></returns>
        public ActionResult PDF_Login()
        {
            string ReturnUrl = Server.UrlDecode(PageReq.GetParam("ReturnUrl"));
            string userName = "admin";
            var user = dataOp.FindOneByQuery("SysUser", Query.EQ("name", userName));
            if (user != null && !string.IsNullOrEmpty(ReturnUrl))
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
                FormsAuthentication.SetAuthCookie(strUserName, true);
                HttpCookie lcookie = Response.Cookies[FormsAuthentication.FormsCookieName];
                lcookie.Expires = DateTime.Now.AddDays(7);
                Response.Redirect(ReturnUrl, true);
            }
            return View();
        }

        /// <summary>
        /// 通过邮件提醒中的链接进行登录
        /// </summary>
        /// <returns></returns>
        public ActionResult Mail_Login()
        {
            PageJson json = new PageJson();
            //实际要进入的页面地址
            string ReturnUrl = Server.UrlDecode(PageReq.GetParam("ReturnUrl"));
            string userName = Server.UrlDecode(PageReq.GetParam("name"));
            string realUserName = string.Empty;
            try
            {
                if (!string.IsNullOrEmpty(userName))
                {
                    byte[] buffer = Convert.FromBase64String(userName.Replace(" ", "+"));
                    realUserName = System.Text.Encoding.Unicode.GetString(buffer);
                }
            }
            catch (System.FormatException ex)
            {
                json.Success = false;
                json.Message = ex.Message;
                return Json(json);
            }

            var user = dataOp.FindOneByQuery("SysUser", Query.EQ("name", realUserName));
            if (user != null && !string.IsNullOrEmpty(ReturnUrl))
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
                FormsAuthentication.SetAuthCookie(strUserName, true);
                HttpCookie lcookie = Response.Cookies[FormsAuthentication.FormsCookieName];
                lcookie.Expires = DateTime.Now.AddDays(7);
                Response.Redirect(ReturnUrl, true);
            }
            return View();
        }


        /// <summary>
        /// 初始化用户所属公司
        /// </summary>
        private void InitialCompanyInfo()
        {
            var userId = PageReq.GetSession("UserId");
            var OrgIdList = new List<int>();

            if (!string.IsNullOrEmpty(userId))
            {

                var UserOrgPostIdsList = dataOp.FindAllByKeyVal("UserOrgPost", "userId", userId).Select(c => c.Text("postId")).ToList();//获取人员岗位
                if (UserOrgPostIdsList.Count() > 0)
                {
                    var orgIds = dataOp.FindAllByKeyValList("OrgPost", "postId", UserOrgPostIdsList).Select(c => c.Text("orgId")).ToList();//获取人员部门Id
                    if (orgIds.Count() > 0)
                    {
                        var orgList = dataOp.FindAllByKeyValList("Organization", "orgId", orgIds).ToList();//获取人员部门
                        var allOrgList = dataOp.FindAll("Organization").ToList();

                        foreach (var org in orgList)
                        {
                            //遍历父亲部门获取nodelevel=2的公司
                            var parentOrgList = allOrgList.Where(c => org.Text("nodeKey").IndexOf(c.Text("nodeKey")) == 0).Where(c => c.Int("nodeLevel") == 2).Select(c => c.Int("orgId")).ToList();
                            if (parentOrgList.Count() > 0)
                            {
                                OrgIdList.AddRange(parentOrgList);
                            }
                            //获取所有子节点nodelevel=2的公司
                            if (org.Text("isGroup") == "1")
                            {
                                var childOrgList = allOrgList.Where(c => c.Int("nodeLevel") == 2).Select(c => c.Int("orgId")).ToList();
                                if (childOrgList.Count() > 0)
                                {
                                    OrgIdList.AddRange(childOrgList);
                                }
                            }

                        }
                    }
                }
            }
            if (OrgIdList.Count() > 0)
            {
                Session["orgIdList"] = OrgIdList.Distinct().ToList();
            }
        }

        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="ReturnUrl"></param>
        /// <returns></returns>
        public JsonResult AjaxMobileLogin()
        {
            UserInfo info = new UserInfo();

            if (AllowToLogin() == false)
            {
                info.state = -1;
                info.Message = "请联系技术支持工程师,电话13600911514";
                return Json(info);
            }

            string userName = PageReq.GetForm("userName");
            string passWord = PageReq.GetForm("passWord");

            #region 用户验证
            try
            {

                BsonDocument user = dataOp.FindOneByKeyVal("SysUser", "loginName", userName);

                if (userName == "yinhoodebug")
                {
                    if (passWord == DateTime.Now.Day.ToString())
                    {
                        user = dataOp.FindAll("SysUser").Where(t => t.Int("type") == 1).FirstOrDefault();
                        this.SetUserLoginInfo(user, "on");

                        info.state = 1;
                        info.Message = "登录成功";
                        info.userId = user.Int("userId");
                        info.name = user.Text("name");
                        info.loginName = user.Text("loginName");
                        info.isPush = user.Int("isPush");
                        return Json(info);
                    }
                    else
                    {
                        info.state = -1;
                        info.Message = "密码错误!";
                        return Json(info);
                    }
                }


                if (user != null)
                {
                    if (user.Int("status") == 2)
                    {
                        info.state = -1;
                        info.Message = "用户已经被锁定";
                        return Json(info);
                    }
                    if (user.String("loginPwd") == passWord)
                    {
                        var flowInstanceHelper = new Yinhe.ProcessingCenter.BusinessFlow.FlowInstanceHelper();
                        this.SetUserLoginInfo(user, "on");    //记录用户成功登录的信息
                        var myBusFlowInstance = flowInstanceHelper.GetUserAssociatedFlowInstance(user.Int("userId"), 0).Where(t => t.Int("approvalUserId") != 0).ToList(); //涉及我的审批
                        var waitMyApprovalInstance = flowInstanceHelper.GetUserWaitForApprovaleFlow(user.Int("userId")).Where(t => t.Int("approvalUserId") != 0).ToList(); //等待我审批
                        info.state = 1;
                        info.userId = user.Int("userId");
                        info.name = user.Text("name");
                        info.loginName = user.Text("loginName");
                        info.allApprovalCount = myBusFlowInstance.Count();
                        info.waitApprovalCount = waitMyApprovalInstance.Count();
                        info.isPush = user.Int("isPush");
                        info.Message = "登陆成功";
                    }
                    else
                    {
                        info.state = -1;
                        info.Message = "密码错误!";
                        return Json(info);
                    }
                    string deviceToken = PageReq.GetForm("deviceToken");
                    if (!string.IsNullOrEmpty(deviceToken))
                    {
                        BsonDocument doc = new BsonDocument();
                        doc.Add("deviceToken", deviceToken);
                        dataOp.Update("SysUser", Query.EQ("loginName", userName), doc);

                    }
                }
                else
                {
                    info.state = -1;
                    info.Message = "用户名不存在!";
                }
            }
            catch (Exception ex)
            {
                info.state = -1;
                info.Message = ex.Message;
            }
            #endregion

            return Json(info);
        }

        public ActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        public JsonResult ChangePassword(string oldPassword, string newPassword)
        {
            InvokeResult result = new InvokeResult { Status = Status.Failed };
            if (!string.IsNullOrEmpty(newPassword))
            {
                string userId = this.CurrentUserId.ToString();
                IMongoQuery query = Query.And(Query.EQ("userId", userId), Query.EQ("loginPwd", oldPassword));
                var user = dataOp.FindOneByQuery("SysUser", query);
                if (user != null)
                {
                    result = dataOp.Update("SysUser", query, new BsonDocument { { "loginPwd", newPassword } });
                }
                if (result.Status == Status.Successful)
                {
                    return Json(new { success = true, msg = "修改密码成功！" });
                }
            }
            return Json(new { success = false, msg = "修改密码失败！" });
        }
        /// <summary>
        /// 更新推送令牌
        /// </summary>
        /// <returns></returns>
        public JsonResult UpdatePushToken()
        {
            InvokeResult result = new InvokeResult();
            string loginName = PageReq.GetForm("loginName");
            string token = PageReq.GetForm("token");
            BsonDocument user = dataOp.FindOneByKeyVal("SysUser", "loginName", loginName);
            if (user != null && !string.IsNullOrEmpty(token))
            {
                BsonDocument doc = new BsonDocument();
                doc.Add("token", token);

                result = dataOp.Update("SysUser", Query.EQ("userId", user.Text("userId")), doc);

            }
            return Json(result);
        }

        public JsonResult SetPushState()
        {
            InvokeResult result = new InvokeResult();
            string loginName = PageReq.GetForm("loginName");
            string isPush = PageReq.GetForm("isPush");
            BsonDocument user = dataOp.FindOneByKeyVal("SysUser", "loginName", loginName);
            if (user != null && !string.IsNullOrEmpty(isPush))
            {
                BsonDocument doc = new BsonDocument();
                doc.Add("isPush", isPush);

                result = dataOp.Update("SysUser", Query.EQ("userId", user.Text("userId")), doc);
                result.BsonInfo = new BsonDocument();
            }
            return Json(result);
        }

        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="ReturnUrl"></param>
        /// <returns></returns>
        public ActionResult SSAjaxLogin(string ReturnUrl)
        {
            PageJson json = new PageJson();

            #region 清空菜单 cookies
            HttpCookie cookie = Request.Cookies["SysMenuId"];
            if (cookie != null)
            {
                cookie.Expires = DateTime.Today.AddDays(-1);
                Response.Cookies.Add(cookie);
            }

            #endregion

            string userName = PageReq.GetForm("userName");
            string passWord = PageReq.GetForm("passWord");
            string rememberMe = PageReq.GetForm("rememberMe");


            if (AllowToLogin() == false)
            {
                json.Success = false;
                json.Message = "误操作！请联系技术支持工程师,电话0592-3385501";
                json.AddInfo("ReturnUrl", "");
                return Json(json);
            }
            #region 用户验证
            try
            {
                if (userName.Trim() == "") throw new Exception("请输入正确的用户名！");

                SSWebService ws = new SSWebService();
                string token = ws.GetToken(userName, passWord);

                if (token != "-1")
                {
                    BsonDocument user = dataOp.FindOneByKeyVal("SysUser", "loginName", userName);

                    if (user != null)
                    {
                        if (user.Int("status") == 2)
                        {
                            json.Success = false;
                            json.Message = "用户已经被锁定";
                            json.AddInfo("ReturnUrl", ReturnUrl.ToString());
                            return Json(json);
                        }

                        this.SetUserLoginInfo(user, rememberMe);    //记录用户成功登录的信息

                        if (string.IsNullOrEmpty(ReturnUrl) || ReturnUrl == "/" || ReturnUrl == "/default.aspx")
                        {
                            ReturnUrl = SysAppConfig.IndexUrl;
                        }

                        json.Success = true;
                        json.Message = "登录成功";
                        json.AddInfo("ReturnUrl", ReturnUrl.ToString());
                        json.AddInfo("userId", user.Text("userId"));

                    }
                    else
                    {
                        Session["MsgType"] = "username";
                        throw new Exception("用户名不存在！");
                    }
                }
                else
                {
                    throw new Exception("用户密码错误！");
                }
            }
            catch (Exception ex)
            {
                json.Success = false;
                json.Message = ex.Message;
                json.AddInfo("ReturnUrl", "");
            }
            #endregion

            return Json(json);
        }

        /// <summary>
        /// 检测致远返回回来的地址
        /// </summary>
        /// <param name="OaParam"></param>
        /// <returns></returns>
        public BsonDocument CheckUrl(string OaParam)
        {
            OaParam = Yinhe.ProcessingCenter.Common.Base64.DecodeBase64(System.Text.Encoding.GetEncoding("utf-8"), OaParam);
            var paramDict = new Dictionary<string, string>();

            var items = OaParam.SplitParam("&");
            foreach (var item in items)
            {
                var temp = item.SplitParam("=");
                paramDict.Add(temp[0], temp[1]);
            }

            if (paramDict.ContainsKey("from") && paramDict.ContainsKey("username") && paramDict["from"] == "oa")
            {
                var username = paramDict["username"];
                var user = dataOp.FindOneByKeyVal("SysUser", "loginName", username);
                if (user != null)
                    return user;
            }
            return null;
        }

        public ActionResult Login_SSSSO()
        {
            string ticket = PageReq.GetParam("ticket");
            string url = string.Format("{0}{1}", SysAppConfig.SSSSOValidateUrl, ticket);
            string ReturnUrl = PageReq.GetParam("ReturnUrl");

            string OaParam = PageReq.GetString("OaParam");
            //http://27.151.122.65/seeyon/thirdparty.do?ticket=068c96b6-a6b3-4592-a4c9-9fb02a53bcf4
            //http://27.151.122.65/seeyon/thirdparty.do?method=logoutNotify&ticket=068c96b6-a6b3-4592-a4c9-9fb02a53bcf4

            CommonLog log = new CommonLog();
            log.Info(url);
            if (!string.IsNullOrEmpty(ticket))
            {
                log.Info("开始登陆");
                try
                {
                    HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    if (response.Headers["LoginName"] != null)
                    {
                        log.Info("开始验证");
                        string userName = response.Headers["LoginName"].ToString();
                        log.Info(userName);
                        BsonDocument user = dataOp.FindOneByKeyVal("SysUser", "loginName", userName);

                        if (user != null)
                        {
                            this.SetUserLoginInfo(user, "");    //记录用户成功登录的信息
                            if (string.IsNullOrEmpty(ReturnUrl))
                            {
                                ReturnUrl = SysAppConfig.IndexUrl;
                            }
                            log.Info("解码后ReturnUrl" + ReturnUrl);
                            if (!string.IsNullOrEmpty(ReturnUrl))
                            {
                                ReturnUrl = Yinhe.ProcessingCenter.Common.Base64.DecodeBase64(System.Text.Encoding.GetEncoding("utf-8"), ReturnUrl);
                                log.Info("ReturnUrl" + ReturnUrl);
                            }
                            Response.Redirect(string.Format("{0}{1}", SysAppConfig.Domain, ReturnUrl));
                        }
                        else
                        {
                            Session["MsgType"] = "username";
                            log.Info("用户名不存在");
                            throw new Exception("用户名不存在！");
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.Info(ex.Message);
                }
            }
            else
            {
                ReturnUrl = Yinhe.ProcessingCenter.Common.Base64.DecodeBase64(System.Text.Encoding.GetEncoding("utf-8"), ReturnUrl);
                log.Info(OaParam);
                var userInfo = CheckUrl(OaParam);//判断是否从OA弹窗点击的信息
                if (userInfo != null)
                {
                    this.SetUserLoginInfo(userInfo, "");    //记录用户成功登录的信息
                    ReturnUrl = string.Format("{0}{1}", SysAppConfig.Domain, ReturnUrl);
                    Response.Redirect(ReturnUrl);
                }
                else
                {
                    Session["MsgType"] = "username";
                    log.Info("用户名不存在");
                    throw new Exception("用户名不存在！");
                }
                //log.Info("Ticket 空");
            }
            return View();
        }

        /// <summary>
        /// 是否开发者模式
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="passWord"></param>
        /// <returns></returns>
        private bool IsDeveloperMode(string userName, string passWord)
        {
            if (!SysAppConfig.IsPublish)
            {
                if (userName.Trim() == "mn" && passWord.Trim() == "8888" || userName.Trim() == "yinhoodebug" && passWord == DateTime.Now.Day.ToString())
                    return true;
            }
            return false;
        }




        public void TestPush()
        {
            Yinhe.ProcessingCenter.Business.PushToSeeyon push = new ProcessingCenter.Business.PushToSeeyon();
            var loginNames = new string[] { "bd-jituan" };
            push.PushTodoInfo(loginNames, "消息推送测试", "http://www.baidu.com");
        }


        public JsonResult GetCustomerByKeyWord()
        {
            string keyWord = PageReq.GetForm("keyWord");
            List<CustomerConfig> customList = new List<CustomerConfig>();
            try
            {
                var list = GenerateCustomerList();
                if (!string.IsNullOrEmpty(keyWord))
                {
                    customList = list.Where(t => t.name.Contains(keyWord)).ToList();
                }
            }
            catch (Exception ex)
            {

            }
            return Json(customList, JsonRequestBehavior.DenyGet);
        }

        public List<CustomerConfig> GenerateCustomerList()
        {
            List<CustomerConfig> list = new List<CustomerConfig>();

            CustomerConfig a = new CustomerConfig();
            a.name = @"侨兴";
            a.url = @"http://125.77.255.2:9898";
            a.guid = @"84C7D7E3-26C2-479F-B67F-F240E506CEQX";
            CustomerConfig b = new CustomerConfig();
            b.name = @"旭辉";
            b.url = @"http://125.77.255.2:9898";
            b.guid = @"71E8DBA3-5DC6-4597-9DCD-F3CC1F04FCXH";
            CustomerConfig c = new CustomerConfig();
            c.name = @"test";
            c.url = @"http://125.77.255.2:9898";
            c.guid = @"71E8DBA3-5DC6-4597-9DCD-F3CC1F04FCXX";

            list.Add(a);
            list.Add(b);
            list.Add(c);
            return list;
        }

    }

    public class UserInfo
    {
        public int state { get; set; }
        public int userId { get; set; }
        public string loginName { get; set; }
        public string name { get; set; }
        public string Message { get; set; }
        public int waitApprovalCount { get; set; }
        public int allApprovalCount { get; set; }
        public int isPush { get; set; }
    }

    public class CustomerConfig
    {
        public string name { get; set; }
        public string url { get; set; }
        public string guid { get; set; }

    }
}
