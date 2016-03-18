using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Yinhe.ProcessingCenter.Common;
using MongoDB.Bson;
using Yinhe.ProcessingCenter.Permissions;

namespace Yinhe.ProcessingCenter
{
    /// <summary>
    /// 用户控件基类
    /// </summary>
    public class ViewUserControlBase : ViewUserControl
    {
        private DataOperation _dataOp = null;
        private Authentication _auth = null;
        private AuthManage _sysAuth = null;
        /// <summary>
        /// 数据操作类
        /// </summary>
        public DataOperation dataOp
        {
            get
            {
                if (_dataOp == null) _dataOp = new DataOperation();
                return this._dataOp;
            }
        }

        /// <summary>
        /// 权限控制对象
        /// </summary>
        protected Authentication auth
        {
            get
            {
                if (_auth != null)
                {
                    return _auth;
                }
                else
                {
                    _auth = new Authentication();
                    return _auth;
                }
            }
        }

        /// <summary>
        /// 系统权限判断，不包括分项权限
        /// </summary>
        protected AuthManage sysAuth
        {
            get
            {
                if (_sysAuth == null)
                    _sysAuth = AuthManage._();
                return this._sysAuth;
            }
        }

        /// <summary>
        /// 获取当前用户的Id
        /// </summary>
        /// <value>当前用户的ID</value>
        public int CurrentUserId
        {
            get
            {
                if (string.IsNullOrEmpty(PageReq.GetSession("UserId").ToString()))
                {
                    return -1;
                }
                else
                    return int.Parse(PageReq.GetSession("UserId"));
            }
        }
        /// <summary>
        /// 获取当前用户的用户名
        /// </summary>
        /// <value>当前用户的用户名</value>
        public string CurrentUserName
        {
            get
            {
                if (CurrentUserId > 0)
                {
                    BsonDocument user = dataOp.FindOneByKeyVal("SysUser", "userId", CurrentUserId.ToString());

                    return user != null ? user.String("name") : "";
                }

                return "";
            }
        }

        ///// <summary>
        ///// 重写加载方法
        ///// </summary>
        ///// <param name="e"></param>
        //protected override void OnLoad(System.EventArgs e)
        //{
        //    if (ViewContext == null) { return; }
           
        //    if (SysAppConfig.IsPlugIn == false)
        //    {
        //        if (string.IsNullOrEmpty(PageReq.GetSession("UserId")) == true)
        //        {
        //            #region 免验证登陆系统
        //            string verfiyCode = Request.QueryString["verfiyCode"] != null ? Request.QueryString["verfiyCode"].ToString() : "";
        //            string uid = Request.QueryString["uid"] != null ? Request.QueryString["uid"].ToString() : "";
        //            if (!string.IsNullOrEmpty(verfiyCode) && !string.IsNullOrEmpty(uid))
        //            {
        //                verfiyCode = Base64.DecodeBase64(verfiyCode);
        //                uid = Base64.DecodeBase64(uid);
        //                if (verfiyCode == SysAppConfig.CustomerCode)
        //                {

        //                    BsonDocument user = dataOp.FindOneByKeyVal("SysUser", "loginName", uid);
        //                    if (user != null)
        //                    {
        //                        string strUserName = user.String("userId") + "\\" + user.String("name") + "\\" + user.String("cardNumber");
        //                        Session["UserId"] = user.String("userId");
        //                        Session["UserName"] = user.String("name");
        //                        Session["LoginName"] = user.String("loginName");
        //                        Session["UserType"] = user.String("type");
        //                    }
        //                }
        //            }
        //            #endregion
        //        }
        //        if (string.IsNullOrEmpty(PageReq.GetSession("UserId")) == true)
        //        {
        //            string returnUrl = SysAppConfig.LoginUrl;

        //            if (returnUrl.IndexOf("?") >= 0)
        //            {
        //                returnUrl += "&ReturnUrl=" + Server.UrlEncode(Request.RawUrl);
        //            }
        //            else
        //            {
        //                returnUrl += "?ReturnUrl=" + Server.UrlEncode(Request.RawUrl);
        //            }
        //            Response.Redirect(returnUrl);
        //            return;
        //        }
        //    }
        //    base.OnLoad(e);
        //}
    }
}
