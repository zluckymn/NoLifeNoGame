using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using MongoDB.Bson;
using Yinhe.ProcessingCenter.Common;
using Yinhe.ProcessingCenter.Permissions;

namespace Yinhe.ProcessingCenter
{
    /// <summary>
    /// 免登陆查看页面基类
    /// </summary>
    public class ViewFreePageBase : ViewPage
    {
        private DataOperation _dataOp = null;
       
    
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

         
        /// <summary>
        /// 重写加载方法
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(System.EventArgs e)
        {
            ////冻结判断
            //if (FreezeStrategy.IsFreezeSystem())
            //{
            //    string AccessdenyUrl = "/Home/AlertInfo";
            //    Response.Redirect(AccessdenyUrl);
            //    return;
            //}
            if (ViewContext == null) { return; }


            if (SysAppConfig.IsPlugIn == false)
            {
                if (string.IsNullOrEmpty(PageReq.GetSession("UserId")) == true)
                {
                    #region 免验证登陆系统
                    BsonDocument user = dataOp.FindOneByKeyVal("SysUser", "loginName", "guest");
                    if (user != null)
                    {
                                string strUserName = user.String("userId") + "\\" + user.String("name") + "\\" + user.String("cardNumber");
                                Session["UserId"] = user.String("userId");
                                Session["UserName"] = user.String("name");
                                Session["LoginName"] = user.String("loginName");
                                Session["UserType"] = user.String("type");
                    }
                   #endregion
                }
                if (string.IsNullOrEmpty(PageReq.GetSession("UserId")) == true)
                {
                    string returnUrl = SysAppConfig.IndexUrl;

                    if (returnUrl.IndexOf("?") >= 0)
                    {
                        returnUrl += "&ReturnUrl=" + Server.UrlEncode(Request.RawUrl);
                    }
                    else
                    {
                        returnUrl += "?ReturnUrl=" + Server.UrlEncode(Request.RawUrl);
                    }
                    Response.Redirect(returnUrl);
                    return;
                }
            }
            base.OnLoad(e);
        }
    }
}
