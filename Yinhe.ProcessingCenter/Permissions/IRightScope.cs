using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yinhe.ProcessingCenter.Permissions
{
    /// <summary>
    /// 角色数据范围接口
    /// </summary>
    public interface IRightScope
    {
        /// <summary>
        /// 获取角色可访问的数据范围列表
        /// </summary>
        /// <param name="roleId">角色ID</param>
        /// <returns></returns>
        List<DataScopeEntity> GetDataScopes(int roleId);
        /// <summary>
        /// 保存角色数据范围数据
        /// </summary>
        /// <param name="dataScopes">角色范围数据</param>
        /// <param name="roleId">角色ID</param>
        /// <returns></returns>
        InvokeResult Save(List<DataScopeEntity> dataScopes,int roleId);
        /// <summary>
        /// 获取用户客户访问的数据权限
        /// </summary>
        /// <param name="userId">用户Id</param>
        /// <returns></returns>
        object GetUserDataScopeRight(int userId);
    }
}
