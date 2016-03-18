using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yinhe.ProcessingCenter.SynAD
{
    /// <summary>
    /// 路径解析接口
    /// </summary>
   public interface IPathAnalyse
    {
        #region 组织架构
        /// <summary>
        /// 获取部门path
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        string GetDepPath(string str);

        /// <summary>
        /// 部门标识
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        string GetDepCode(string str);

        /// <summary>
        /// 获取父级部门
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        string GetDepParentName(string str);

        /// <summary>
        /// 获取爷级部门
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        string GetGrandParentName(string str);

        /// <summary>
        /// 获取部门级别
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        int GetDepLevel(string str);

        /// <summary>
        /// 部门过滤
        /// </summary>
        /// <param name="depList"></param>
        /// <param name="dep"></param>
        /// <returns></returns>
        List<ADDepartment> GetDepListFilter(List<ADDepartment> depList, ADDepartment dep);
        #endregion

        #region  用户

        /// <summary>
        /// 获取用户Path
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        string GetUserPath(string str);

        /// <summary>
        /// 获取用户标识
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        string GetUserCode(string str);

        /// <summary>
        /// 获取用户所属部门
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        string GetUserDepartment(string str);

        /// <summary>
        /// 获取用户所属部门的父部门
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        string GetUserGrandDepartment(string str);

        /// <summary>
        /// 用户过滤
        /// </summary>
        /// <param name="userList">用户列表</param>
        /// <param name="user">欲添加的用户</param>
        /// <returns></returns>
        List<ADUser> GetUserListFilter(List<ADUser> userList, ADUser user);
        #endregion
    }
}
