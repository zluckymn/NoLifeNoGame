using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;

using Yinhe.ProcessingCenter.Permissions;

///<summary>
///MVC Html页面权限验证相关
///</summary>
namespace System.Web.Mvc.Html
{
    /// <summary>
    /// 权限验证
    /// </summary>
    public static class AuthExtensions
    {
        public static MvcHtmlString SysAuthButton(this HtmlHelper htmlHelper, string name, AuthManage auth, string code, IDictionary<string, object> htmlAttributes)
        {
            string result = string.Empty;
            if (auth.CheckRight(code))
            {
                StringBuilder sb = new StringBuilder();
                string attrs = string.Empty;
                foreach (var kv in htmlAttributes)
                {
                    sb.AppendFormat(@"{0}='{1}' ", kv.Key, kv.Value);
                }
                result = string.Format(@"<a {0} href='javascript:void(0)' >{1}<span></span></a>",sb.ToString(), name);
                
            }
            return MvcHtmlString.Create(result);
        }

        public static MvcHtmlString SysAuthButton(this HtmlHelper htmlHelper, string name, bool hasRight, IDictionary<string, object> htmlAttributes)
        {
            string result = string.Empty;
            if (hasRight)
            {
                StringBuilder sb = new StringBuilder();
                string attrs = string.Empty;
                foreach (var kv in htmlAttributes)
                {
                    sb.AppendFormat(@"{0}='{1}' ", kv.Key, kv.Value);
                }
                result = string.Format(@"<a {0} href='javascript:void(0)' >{1}<span></span></a>", sb.ToString(), name);

            }
            return MvcHtmlString.Create(result);
        }
    }
}
