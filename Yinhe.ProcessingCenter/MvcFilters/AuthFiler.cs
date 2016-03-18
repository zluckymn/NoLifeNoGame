using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web;

using Yinhe.ProcessingCenter.Permissions;

namespace Yinhe.ProcessingCenter
{
    /// <summary>
    /// 验证分项权限
    /// </summary>
    public class ProjectRightAttribute :ActionFilterAttribute, IAuthorizationFilter
    {
        private string[] codes;
        private string roleType;
        private string dataKey;
        private string dataId;

        /// <summary>
        /// 分项权限过滤器
        /// </summary>
        /// <param name="roleType">权限类型</param>
        /// <param name="dataKey">数据对象key</param>
        /// <param name="codes">权限代码</param>
        public ProjectRightAttribute(string roleType, string dataKey, params string[] codes)
        {
            this.roleType = roleType;
            this.dataKey = dataKey;
            this.codes = codes;
            

        }

        public void OnAuthorization(AuthorizationContext filterContext)
        {
            this.dataId = PageReq.GetString(dataKey);
            Authentication auth = new Authentication();
            if (!auth.CheckProjectRight(roleType, dataId, codes))
            {
                filterContext.Result = new EmptyResult();
            }
        }
    }

    /// <summary>
    /// 验证系统权限
    /// </summary>
    public class SysRightAttribute : ActionFilterAttribute, IAuthorizationFilter
    {
        private string[] codes;

        /// <summary>
        /// 验证系统权限
        /// </summary>
        /// <param name="codes">权限码</param>
        public SysRightAttribute(params string[] codes)
        {
            this.codes = codes;
        }
        public void OnAuthorization(AuthorizationContext filterContext)
        {
            Authentication auth = new Authentication();
            if (!auth.CheckSysRight(codes))
            {
                filterContext.Result = new EmptyResult();
            }
        }
    }
}
