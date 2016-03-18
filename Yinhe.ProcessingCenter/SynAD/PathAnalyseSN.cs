using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yinhe.ProcessingCenter.SynAD
{
    /// <summary>
    /// 苏宁AD路径解析
    /// </summary>
    public class PathAnalyseSN : IPathAnalyse
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
        /// 获取用户标识 LDAP://10.0.0.1/CN=王鸿斌,OU=财务管理中心,OU=苏宁环球股份,OU=用户,DC=suning,DC=com,DC=cn
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public string GetUserCode(string str)
        {
            return str.Substring(str.IndexOf(",")+1);
        }

        /// <summary>
        /// 获取用户所属部门LDAP://10.0.0.1/CN=李洁,OU=历史用户,OU=用户,DC=suning,DC=com,DC=cn
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
            //str = str.ToLower();
            ret = str.Substring(str.IndexOf("OU="));
            ret = ret.Substring(0, ret.IndexOf(",DC"));///OU=永升大厦工程部,OU=NQZA.永升大厦管理处,OU=NQ.永升物业上海区域,OU=N.上海永升物业管理有限公司,OU=旭辉集团股份有限公司
            ret = ret.Replace("OU=", "");//永升大厦工程部,NQZA.永升大厦管理处,NQ.永升物业上海区域,N.上海永升物业管理有限公司,旭辉集团股份有限公司
            array = ret.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            return array;
        }

        /// <summary>
        /// LDAP://192.168.8.33/CN=任文立,OU=FF.工程部,OU=F.嘉兴旭辉,OU=旭辉集团股份有限公司,DC=cifi,DC=com,DC=cn
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public string[] GetUserPathCut(string str)
        {
            //LDAP://10.0.0.1/CN=李洁,OU=历史用户,OU=用户,DC=suning,DC=com,DC=cn
            string[] array;
            string ret = "";
            //str = str.ToLower();
            ret = str.Substring(str.IndexOf("CN="));
            ret = ret.Substring(0, ret.IndexOf(",DC"));///LDAP://192.168.8.33/CN=任文立,OU=FF.工程部,OU=F.嘉兴旭辉,OU=旭辉集团股份有限公司,DC=cifi,DC=com,DC=cn
            ret = ret.Replace("OU=", "").Replace("CN=","");//任文立,FF.工程部,F.嘉兴旭辉,旭辉集团股份有限公司
            array = ret.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            return array;
        }

        public List<ADDepartment> GetDepListFilter(List<ADDepartment> depList, ADDepartment dep)
        {
            if (dep.Code.Contains("OU=用户,DC=suning,DC=com,DC=cn"))
            {
                depList.Add(dep);
            }
            return depList;
        }

        public List<ADUser> GetUserListFilter(List<ADUser> userList, ADUser user)
        {
            if (!user.DepartMentID.Contains("Users"))
            {
                if (user.DepartMentID.Contains("历史用户") || user.GrandDepartMentID.Contains("历史用户"))
                {
                    user.Status = 2;
                }
                else
                {
                    user.Status = 0;
                }
                userList.Add(user);
            }
            return userList;
        }
    }
}
