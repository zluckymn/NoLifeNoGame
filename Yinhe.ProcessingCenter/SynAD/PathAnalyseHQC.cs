using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yinhe.ProcessingCenter.SynAD
{
    /// <summary>
    /// 华侨城AD路径解析
    /// </summary>
    public class PathAnalyseHQC : IPathAnalyse
    {
        #region 组织架构
        /// <summary>
        /// 获取部门path
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public string GetDepPath(string str)
        {
            return "";
        }

        /// <summary>
        /// 部门标识
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public string GetDepCode(string str)
        {
            return str.Substring(str.LastIndexOf("/") + 1);
        }

        /// <summary>
        /// 获取父级部门
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public string GetDepParentName(string str)
        {
            var arr = GetDepPathCut(str);
            if (arr.Length <= 1)
            {
                return "";
            }
            else
            {
                return arr[1];
            }

        }

        /// <summary>
        /// 获取爷级部门
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public string GetGrandParentName(string str)
        {
            var arr = GetDepPathCut(str);
            if (arr.Length <= 2)
            {
                return "";
            }
            else
            {
                return arr[2];
            }
        }

        /// <summary>
        /// 获取部门级别
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public int GetDepLevel(string str)
        {
            return GetDepPathCut(str).Length;
        }
        #endregion

        #region  用户

        /// <summary>
        /// 获取用户Path
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public string GetUserPath(string str)
        {
            return "";
        }

        /// <summary>
        /// 获取用户标识
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public string GetUserCode(string str)
        {
            return str.Substring(str.IndexOf(",")+1);
        }

        /// <summary>
        /// 获取用户所属部门
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public string GetUserDepartment(string str)
        {
            var arr = GetUserPathCut(str);

            if (arr.Length <= 1)
            {
                return "";
            }
            else
            {
                return arr[1];
            }
        }

        /// <summary>
        /// 获取用户所属部门的父部门
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public string GetUserGrandDepartment(string str)
        {
            var arr = GetUserPathCut(str);

            if (arr.Length <= 2)
            {
                return "";
            }
            else
            {
                return arr[2];
            }
        }
        #endregion

        /// <summary>
        /// LDAP://192.168.8.33/OU=永升大厦工程部,OU=NQZA.永升大厦管理处,OU=NQ.永升物业上海区域,OU=N.上海永升物业管理有限公司,OU=旭辉集团股份有限公司,DC=cifi,DC=com,DC=cn
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public string[] GetDepPathCut(string str)
        {
            string[] array;
            string ret = "";
             str = str.ToLower();
            ret = str.Substring(str.IndexOf("ou="));
            ret = ret.Substring(0, ret.IndexOf(",dc"));///OU=永升大厦工程部,OU=NQZA.永升大厦管理处,OU=NQ.永升物业上海区域,OU=N.上海永升物业管理有限公司,OU=旭辉集团股份有限公司
            ret = ret.Replace("ou=", "");//永升大厦工程部,NQZA.永升大厦管理处,NQ.永升物业上海区域,N.上海永升物业管理有限公司,旭辉集团股份有限公司
            array = ret.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            return array;
        }

        /// <summary>
        ///LDAP://172.16.9.25/uid=huanghuishan,cn=users,dc=group,dc=chinaoct,dc=com
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public string[] GetUserPathCut(string str)
        {
            string[] array;
            string ret = "";
            str = str.ToLower();
            ret = str.Substring(str.IndexOf("cn"));//CN=陈杰,OU=信息安全部,OU=苏宁环球股份,OU=用户,DC=suning,DC=com,DC=cn
            ret = ret.Substring(0, ret.LastIndexOf("ou") - 1);///LDAP://192.168.8.33/CN=任文立,OU=FF.工程部,OU=F.嘉兴旭辉,OU=旭辉集团股份有限公司,DC=cifi,DC=com,DC=cn
            ret = ret.Replace("ou=", "").Replace("cn=","");//任文立,FF.工程部,F.嘉兴旭辉,旭辉集团股份有限公司
            array = ret.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            return array;
        }

        public List<ADDepartment> GetDepListFilter(List<ADDepartment> depList, ADDepartment dep)
        {
            if (dep.Path.ToLower().Contains("cn=org,") && !string.IsNullOrEmpty(dep.Name) && !dep.Path.ToLower().Contains(",dc=hoding,") && !dep.Path.ToLower().Contains("cn=org,dc=happyvalley"))
            depList.Add(dep);
            return depList;
        }

        public List<ADUser> GetUserListFilter(List<ADUser> userList, ADUser user)
        {
            if (user.Path.ToLower().Contains("uid=") && !string.IsNullOrEmpty(user.Name) && !string.IsNullOrEmpty(user.LoginName) && !user.Path.ToLower().Contains("dc=hoding"))
            {
                userList.Add(user);
            }
            return userList;
        }
    }
}
