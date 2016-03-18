using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yinhe.ProcessingCenter.SynAD
{
   /// <summary>
   /// 中海投资AD路径解析
   /// </summary>
    public class PathAnalyseZHTZ:IPathAnalyse
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
        /// 部门标识   LDAP://192.0.0.172/OU=中海和才（北京）股权投资基金管理公司,OU=中海投資,DC=coihl,DC=cohl,DC=com
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public string GetDepCode(string str)
        {
            return str.Substring(str.LastIndexOf("/") + 1);  //OU=中海和才（北京）股权投资基金管理公司,OU=中海投資,DC=coihl,DC=cohl,DC=com
        }

        /// <summary>
        /// 获取父级部门   LDAP://192.0.0.172/OU=中海和才（北京）股权投资基金管理公司,OU=中海投資,DC=coihl,DC=cohl,DC=com
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
                return arr[1];   //中海投资
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
        /// 获取部门级别    LDAP://192.0.0.172/OU=中海和才（北京）股权投资基金管理公司,OU=中海投資,DC=coihl,DC=cohl,DC=com
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
        /// 获取用户标识 LDAP://192.0.0.172/CN=刘召伟,OU=中国海外实业有限公司,OU=中海投資,DC=coihl,DC=cohl,DC=com
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public string GetUserCode(string str)
        {
            return str.Substring(str.IndexOf(",") + 1);   //OU=中国海外实业有限公司,OU=中海投資,DC=coihl,DC=cohl,DC=com
        }

        /// <summary>
        /// 获取用户所属部门  LDAP://192.0.0.172/CN=刘召伟,OU=中国海外实业有限公司,OU=中海投資,DC=coihl,DC=cohl,DC=com
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
        ///  LDAP://192.0.0.172/OU=中海和才（北京）股权投资基金管理公司,OU=中海投資,DC=coihl,DC=cohl,DC=com
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public string[] GetDepPathCut(string str)
        {
            string[] array;
            string ret = "";
            //str = str.ToLower();
            ret = str.Substring(str.IndexOf("OU="));
            ret = ret.Substring(0, ret.IndexOf(",DC"));//OU=中海和才（北京）股权投资基金管理公司,OU=中海投資
            ret = ret.Replace("OU=", "");//中海和才（北京）股权投资基金管理公司,中海投資
            array = ret.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            return array;
        }

        /// <summary>
        /// LDAP://192.0.0.172/CN=刘召伟,OU=中国海外实业有限公司,OU=中海投資,DC=coihl,DC=cohl,DC=com
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public string[] GetUserPathCut(string str)
        {
            //LDAP://192.0.0.172/CN=刘召伟,OU=中国海外实业有限公司,OU=中海投資,DC=coihl,DC=cohl,DC=com
            string[] array;
            string ret = "";
            //str = str.ToLower();
            ret = str.Substring(str.IndexOf("CN="));
            ret = ret.Substring(0, ret.IndexOf(",DC"));//LDAP://192.0.0.172/CN=刘召伟,OU=中国海外实业有限公司,OU=中海投資
            ret = ret.Replace("OU=", "").Replace("CN=", "");//刘召伟,中国海外实业有限公司,中海投資
            array = ret.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            return array;
        }

        public List<ADDepartment> GetDepListFilter(List<ADDepartment> depList, ADDepartment dep)
        {
            depList.Add(dep);
            return depList;
        }

        public List<ADUser> GetUserListFilter(List<ADUser> userList, ADUser user)
        {
            //用户过滤，待定过滤条件
            userList.Add(user);
            return userList;
        }
    }
}
