using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace Yinhe.ProcessingCenter.Permissions
{
    /// <summary>
    /// 身份验证aop处理
    /// </summary>
    public class AuthFilter:FilterAttribute,IActionFilter
    {
        #region 私有变量
        private string[] Codes;
        private string AccessdenyUrl = "/Infomation/Accessdeny";
        #endregion

        #region 构造函数
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="codes">模块权限代码</param>
        public AuthFilter(params string[] codes)
        {
            this.Codes = codes;
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="codes">模块权限代码</param>
        /// <param name="url">验证不通过跳转地址</param>
        public AuthFilter(string[] codes, string url)
        {
            this.Codes = codes;
            this.AccessdenyUrl = url;
        }
        #endregion

        #region IActionFilter 成员

        public void OnActionExecuted(ActionExecutedContext filterContext)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 执行Action过滤验证
        /// </summary>
        /// <param name="filterContext"></param>
        public void OnActionExecuting(ActionExecutingContext filterContext)
        {
           

            List<UserRight> rights = AuthManage._().GetUserFunctionRight(int.Parse(PageReq.GetSession("UserId")));
            if (rights == null)
            {
                filterContext.Result = new RedirectResult(this.AccessdenyUrl);
            }
            else
            {
                if (rights.Where(r=>this.Codes.Contains(r.Code)).Count()<=0)
                {
                    filterContext.Result = new RedirectResult(this.AccessdenyUrl);
                }
            }
        }

        #endregion
    }
}
