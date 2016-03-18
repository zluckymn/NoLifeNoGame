using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.DirectoryServices;
using System.Diagnostics;

///<summary>
///AD同步功能
///</summary>
namespace Yinhe.ProcessingCenter.SynAD
{
  /// <summary>
  /// AD域登录同步处理
  /// </summary>
    public class ADSynchronization
    {
        /// <summary>
        /// AD域名
        /// </summary>
        public string Domain { get; set; }
        /// <summary>
        /// 登陆AD域的用户名
        /// </summary>
        public string UserName { get; set; }
        /// <summary>
        /// 密码
        /// </summary>
        public string PassWord { get; set; }

        public AuthorizeType authorizeType { get; set; }

        #region 构造函数
        public ADSynchronization()
        {
        }

        public ADSynchronization(string domain, string userName, string passWord)
        {
            this.Domain = domain;
            this.UserName = userName;
            this.PassWord = passWord;

            if (string.IsNullOrEmpty(userName) && string.IsNullOrEmpty(passWord))
            {
                authorizeType = AuthorizeType.anonymous;
            }
            else
            {
                authorizeType = AuthorizeType.none;
            }

        }

        /// <summary>
        /// 搜索部门AD
        /// </summary>
        /// <param name="path"></param>
        /// <param name="objFilter"></param>
        /// <param name="dicPropertes"></param>
        /// <returns></returns>
        public List<ADDepartment> SearchDepartment(string path, string objFilter, Dictionary<string, string> dicPropertes, string pathAnalyseClass)
        {
            List<ADDepartment> depList = new List<ADDepartment>();

            string[] arrPropertes = dicPropertes.Keys.ToArray();
            try
            {
                path = "LDAP://" + (String.IsNullOrEmpty(this.Domain) == false ? this.Domain : "") + path;
                DirectoryEntry root = null;
                if (authorizeType == AuthorizeType.anonymous)
                {
                    root = new DirectoryEntry(path, "", "", AuthenticationTypes.Anonymous);

                }
                else if (authorizeType == AuthorizeType.none)
                {
                    root = new DirectoryEntry(path, this.UserName, this.PassWord, AuthenticationTypes.None);
                }
                if (root != null)
                {
                    Console.WriteLine("开始遍历AD部门" + root.Path);
                    using (DirectorySearcher searcher = new DirectorySearcher())
                    {
                        searcher.SearchRoot = root;
                        searcher.SearchScope = SearchScope.Subtree;
                        searcher.Filter = objFilter;
                        searcher.PropertiesToLoad.AddRange(arrPropertes);

                        SearchResultCollection results = searcher.FindAll();
                        StringBuilder summary = new StringBuilder();
                        foreach (SearchResult result in results)
                        {
                            ADDepartment dep = new ADDepartment();
                            foreach (string propName in result.Properties.PropertyNames)
                            {
                                if (dicPropertes[propName] != null)
                                {
                                    dep.SetDynamicProperty(dicPropertes[propName].ToString(), result.Properties[propName][0].ToString());
                                }
                            }
                            IPathAnalyse analyse = null;
                            analyse = (IPathAnalyse)Activator.CreateInstance("Yinhe.ProcessingCenter", pathAnalyseClass).Unwrap();//"Yinhe.ProcessingCenter.SynAD.PathAnalyseXH"
                            if (analyse != null)
                            {
                                if (String.IsNullOrEmpty(dep.Name) == false)
                                {

                                    dep.Code = analyse.GetDepCode(dep.Path);
                                    dep.Level = analyse.GetDepLevel(dep.Path);
                                    dep.ParentName = analyse.GetDepParentName(dep.Path);
                                    dep.GrandParentName = analyse.GetGrandParentName(dep.Path);
                                    if (pathAnalyseClass != "Yinhe.ProcessingCenter.SynAD.PathAnalyseHQC")  //中海投资
                                    {
                                        dep.Guid = result.GetDirectoryEntry().Guid.ToString();
                                    }
                                    else
                                    {

                                        if (dep.ParentName.ToLower() == "cn=org")
                                        {
                                            dep.ParentName = "华侨城组织架构";
                                        }
                                        if (dep.GrandParentName.ToLower() == "cn=org")
                                        {
                                            dep.GrandParentName = "华侨城组织架构";
                                        }


                                    }

                                }
                                depList = analyse.GetDepListFilter(depList, dep);
                            }
                            Console.WriteLine(dep.Name);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return depList;
        }

        /// <summary>
        /// 搜索用户
        /// </summary>
        /// <param name="path"></param>
        /// <param name="objFilter"></param>
        /// <param name="dicPropertes"></param>
        /// <returns></returns>
        public List<ADUser> SearchUser(string path, string objFilter, Dictionary<string, string> dicPropertes, string pathAnalyseClass)
        {
            List<ADUser> userList = new List<ADUser>();

            string[] arrPropertes = dicPropertes.Keys.ToArray();


            try
            {
                path = "LDAP://" + (String.IsNullOrEmpty(this.Domain) == false ? this.Domain : "") + path;
                DirectoryEntry root = null;
                if (authorizeType == AuthorizeType.anonymous)
                {
                    root = new DirectoryEntry(path, "", "", AuthenticationTypes.Anonymous);

                }
                else if (authorizeType == AuthorizeType.none)
                {
                    root = new DirectoryEntry(path, this.UserName, this.PassWord, AuthenticationTypes.None);
                }

                if (root != null)
                {
                    Console.WriteLine(root.Path);
                    using (DirectorySearcher searcher = new DirectorySearcher())
                    {
                        searcher.SearchRoot = root;
                        searcher.SearchScope = SearchScope.Subtree;
                        searcher.Filter = objFilter;
                        searcher.PageSize = 10000;
                        searcher.PropertiesToLoad.AddRange(arrPropertes);

                        SearchResultCollection results = searcher.FindAll();
                        StringBuilder summary = new StringBuilder();
                        foreach (SearchResult result in results)
                        {
                            ADUser user = new ADUser();
                            foreach (string propName in result.Properties.PropertyNames)
                            {
                                //Console.WriteLine("字段名称：{0}, 字段值：{1}\n", propName, result.Properties[propName][0].ToString());                                
                                if (dicPropertes.Keys.Contains(propName) == true)
                                {
                                    if (dicPropertes[propName] != null)
                                    {
                                        user.SetDynamicProperty(dicPropertes[propName].ToString(), result.Properties[propName][0].ToString());
                                    }
                                }
                            }
                            //Console.ReadLine();
                            IPathAnalyse analyse = null;
                            analyse = (IPathAnalyse)Activator.CreateInstance("Yinhe.ProcessingCenter", pathAnalyseClass).Unwrap();
                            if (analyse != null)
                            {
                                if (String.IsNullOrEmpty(user.Name) == false)
                                {
                                    if (pathAnalyseClass != "Yinhe.ProcessingCenter.SynAD.PathAnalyseHQC")
                                    {
                                        user.Guid = result.GetDirectoryEntry().Guid.ToString();
                                        user.Code = analyse.GetUserCode(user.Path);
                                        user.DepartMentID = analyse.GetUserDepartment(user.Path);
                                        user.GrandDepartMentID = analyse.GetUserGrandDepartment(user.Path);
                                    }
                                    else
                                    {
                                        user.DepartMentGuid = user.Code;
                                    }

                                }
                                userList = analyse.GetUserListFilter(userList, user);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return userList;
        }

        #endregion
    }


    public enum AuthorizeType
    {
        none = AuthenticationTypes.None,
        anonymous = AuthenticationTypes.Anonymous
    }

}
