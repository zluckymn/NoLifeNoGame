using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;

namespace Yinhe.ProcessingCenter.SynAD
{
    /// <summary>
    /// AD数据处理
    /// </summary>
    public class ADToDB
    {
        private CommonLog _log = new CommonLog();
        /// <summary>
        /// Ad属性与部门对应字典
        /// </summary>
        private Dictionary<string, string> DicDep;
        /// <summary>
        /// ad属性与用户的对应字典
        /// </summary>
        private Dictionary<string, string> DicUser;

        /// <summary>
        /// 已经遍历过的部门
        /// </summary>
        private List<string> HadPathList = new List<string>();

        /// <summary>
        /// 用户过滤器
        /// </summary>
        private string UserFilter;

        /// <summary>
        /// 部门过滤器
        /// </summary>
        private string DepFilter;

        /// <summary>
        /// AD域的根路径
        /// </summary>
        private string adRootPath;

        /// <summary>
        /// 获取AD属性对象
        /// </summary>
        ADSynchronization ADsyn = new ADSynchronization();

        /// <summary>
        /// 构造函数
        /// </summary>
        public ADToDB(string rootPath, Dictionary<string, string> dicDep, Dictionary<string, string> dicUser, string depFilter, string userFilter)
        {
            this.adRootPath = rootPath;
            this.DicDep = dicDep;
            this.DicUser = dicUser;
            this.DepFilter = depFilter;
            this.UserFilter = userFilter;
        }

        public ADToDB()
        {

        }

        /// <summary>
        /// 获取ＡＤ部门，用户树
        /// </summary>
        /// <param name="depList"></param>
        /// <param name="userList"></param>
        /// <param name="curDep"></param>
        /// <returns></returns>
        public ADDepartment GetADTreeHQC(List<ADDepartment> depList, List<ADUser> userList, ADDepartment curDep)
        {
            List<ADDepartment> childs = new List<ADDepartment>();

            if (!string.IsNullOrEmpty(this.HadPathList.Where(s => s.Equals(curDep.Path) == true).FirstOrDefault()))
                Console.WriteLine("已经遍历过：" + curDep.Path);
            else
            {
                HadPathList.Add(curDep.Path);
                childs = depList.Where(d => d.ParentName.Trim() == curDep.Name && d.GrandParentName.Trim() == curDep.ParentName).ToList();
            }

            List<ADUser> childUsers = userList.Where(u => u.Code == curDep.Guid).ToList();
            if (childUsers.Count() > 0)
            {
                curDep.SubUsers.AddRange(childUsers);

                foreach (var item in curDep.SubUsers)
                {
                    item.DepartMentGuid = curDep.Guid;
                }
            }
            foreach (ADDepartment child in childs)
            {
                if (!string.IsNullOrEmpty(this.HadPathList.Where(s => s.Equals(child.Path) == true).FirstOrDefault()))
                    Console.WriteLine("已经遍历过：" + child.Path);
                else
                {
                    ADDepartment curChild = this.GetADTreeHQC(depList, userList, child);
                    curChild.ParentGuid = child.Guid;
                    curDep.SubDepartemnt.Add(curChild);
                }
            }
            return curDep;
        }
        /// <summary>
        /// 获取ＡＤ部门，用户树
        /// </summary>
        /// <param name="depList"></param>
        /// <param name="userList"></param>
        /// <param name="curDep"></param>
        /// <returns></returns>
        public ADDepartment GetADTree(List<ADDepartment> depList, List<ADUser> userList, ADDepartment curDep)
        {
            List<ADDepartment> childs = new List<ADDepartment>();

            if (!string.IsNullOrEmpty(this.HadPathList.Where(s => s.Equals(curDep.Path) == true).FirstOrDefault()))
                Console.WriteLine("已经遍历过：" + curDep.Path);
            else
            {
                HadPathList.Add(curDep.Path);
                childs = depList.Where(d => d.ParentName.Trim() == curDep.Name && d.GrandParentName.Trim()==curDep.ParentName).ToList();
            }

            List<ADUser> childUsers = userList.Where(u => u.Code == curDep.Code).ToList();
            if (childUsers.Count() > 0)
            {
                curDep.SubUsers.AddRange(childUsers);

                foreach (var item in curDep.SubUsers)
                {
                    item.DepartMentGuid = curDep.Guid;
                }
            }
            foreach (ADDepartment child in childs)
            {
                if (!string.IsNullOrEmpty(this.HadPathList.Where(s => s.Equals(child.Path) == true).FirstOrDefault()))
                    Console.WriteLine("已经遍历过：" + child.Path);
                else
                {
                    ADDepartment curChild = this.GetADTree(depList, userList, child);
                    curChild.ParentGuid = child.Guid;
                    curDep.SubDepartemnt.Add(curChild);
                }
            }
            return curDep;
        }

        /// <summary>
        /// 获取ＡＤ部门，用户树 用户单个部门
        /// </summary>
        /// <param name="depList"></param>
        /// <param name="userList"></param>
        /// <param name="curDep"></param>
        /// <returns></returns>
        public ADDepartment GetADTreeByGuid(List<ADDepartment> depList, List<ADUser> userList, ADDepartment curDep)
        {
            List<ADDepartment> childs = new List<ADDepartment>();

            if (!string.IsNullOrEmpty(this.HadPathList.Where(s => s.Equals(curDep.Path) == true).FirstOrDefault()))
                Console.WriteLine("已经遍历过：" + curDep.Path);
            else
            {
                HadPathList.Add(curDep.Path);
                childs = depList.Where(d => d.ParentGuid ==curDep.Guid).ToList();
            }

            List<ADUser> childUsers = userList.Where(u => u.Code == curDep.Code).ToList();
            if (childUsers.Count() > 0)
            {
                curDep.SubUsers.AddRange(childUsers);

                foreach (var item in curDep.SubUsers)
                {
                    item.DepartMentGuid = curDep.Guid;
                }
            }
            foreach (ADDepartment child in childs)
            {
                if (!string.IsNullOrEmpty(this.HadPathList.Where(s => s.Equals(child.Path) == true).FirstOrDefault()))
                    Console.WriteLine("已经遍历过：" + child.Path);
                else
                {
                    ADDepartment curChild = this.GetADTree(depList, userList, child);
                    curChild.ParentGuid = child.Guid;
                    curDep.SubDepartemnt.Add(curChild);
                }
            }
            return curDep;
        }

        /// <summary>
        /// 获取ＡＤ部门，用户树 用户属于多个部门
        /// </summary>
        /// <param name="depList"></param>
        /// <param name="userList"></param>
        /// <param name="curDep"></param>
        /// <returns></returns>
        public ADDepartment GetADTreeByGuidMultDepZHHY(List<ADDepartment> depList, List<ADUser> userList, ADDepartment curDep)
        {
            List<ADDepartment> childs = new List<ADDepartment>();

            if (!string.IsNullOrEmpty(this.HadPathList.Where(s => s.Equals(curDep.Guid) == true).FirstOrDefault()))
                Console.WriteLine("已经遍历过：" + curDep.Guid);
            else
            {
                HadPathList.Add(curDep.Guid);
                childs = depList.Where(d => d.ParentGuid == curDep.Guid).ToList();
            }

            List<ADUser> childUsers = userList.Where(u => u.DepartMentGuids.Contains(curDep.DepId.ToString())).ToList();
            if (childUsers.Count() > 0)
            {
                curDep.SubUsers.AddRange(childUsers);

                foreach (var item in curDep.SubUsers)
                {
                    item.DepartMentGuid = curDep.Guid;
                }
            }
            foreach (ADDepartment child in childs)
            {
                if (!string.IsNullOrEmpty(this.HadPathList.Where(s => s.Equals(child.Guid) == true).FirstOrDefault()))
                    Console.WriteLine("已经遍历过：" + child.Guid);
                else
                {
                    ADDepartment curChild = this.GetADTreeByGuidMultDepZHHY(depList, userList, child);
                    curChild.ParentGuid = child.Guid;
                    curDep.SubDepartemnt.Add(curChild);
                }
            }
            return curDep;
        }

        /// <summary>
        /// 旭辉
        /// </summary>
        public void ImportToDBXH()
        {
            OrganizationAD org = new OrganizationAD();
           Dictionary<string, string> dicDep =new Dictionary<string,string>();
           ADsyn = new ADSynchronization("", "administrator", "*03ci33fiit#");   
           dicDep.Add("name", "Name");
           dicDep.Add("adspath", "Path");
           var list = ADsyn.SearchDepartment("192.168.8.33", "(objectCategory=CN=Organizational-Unit,CN=Schema,CN=Configuration,DC=cifi,DC=com,DC=cn)", dicDep, "Yinhe.ProcessingCenter.SynAD.PathAnalyseXH");

           Console.WriteLine(list.Count());
           Yinhe.ProcessingCenter.SerializerXml<List<ADDepartment>> ser = new Yinhe.ProcessingCenter.SerializerXml<List<ADDepartment>>(list);
           ser.BuildXml("dep.xml");


           Dictionary<string, string> dicDep1 = new Dictionary<string, string>();

           dicDep1.Add("name", "Name");
           dicDep1.Add("adspath", "Path");
           dicDep1.Add("samaccountname", "LoginName");
           dicDep1.Add("userprincipalname", "EmailAddr");
           var list1 = ADsyn.SearchUser("192.168.8.33", "(objectCategory=CN=Person,CN=Schema,CN=Configuration,DC=cifi,DC=com,DC=cn)", dicDep1, "Yinhe.ProcessingCenter.SynAD.PathAnalyseXH");

           Console.WriteLine(list1.Count());
           Yinhe.ProcessingCenter.SerializerXml<List<ADUser>> ser1 = new Yinhe.ProcessingCenter.SerializerXml<List<ADUser>>(list1);
           ser1.BuildXml("user.xml");

           ADDepartment curChild = this.GetADTree(list, list1, list.Where(t=>t.Name.Contains("旭辉")).FirstOrDefault());

           Yinhe.ProcessingCenter.SerializerXml<ADDepartment> ser2 = new Yinhe.ProcessingCenter.SerializerXml<ADDepartment>(curChild);
           ser2.BuildXml("tree.xml");

           if (list.Count() > 5)
           {
               org.OrganizationSave(list, curChild);
               org.UserInsert(list1, 2, 0);

           }
           //Console.WriteLine(Count(curChild));
           
        }

        /// <summary>
        /// 苏宁
        /// </summary>
        public void ImportToDBSN()
        {
            OrganizationAD org = new OrganizationAD();
            Dictionary<string, string> dicDep = new Dictionary<string, string>();
            ADsyn = new ADSynchronization("", "plmsystem@suning.com.cn", "ghr9fg_(&*3Nnl0A");
            dicDep.Add("name", "Name");
            dicDep.Add("adspath", "Path");
            var list = ADsyn.SearchDepartment("10.0.0.1", "(objectCategory=CN=Organizational-Unit,CN=Schema,CN=Configuration,DC=suning,DC=com,DC=cn)", dicDep, "Yinhe.ProcessingCenter.SynAD.PathAnalyseSN");

            Console.WriteLine(list.Count());
            Yinhe.ProcessingCenter.SerializerXml<List<ADDepartment>> ser = new Yinhe.ProcessingCenter.SerializerXml<List<ADDepartment>>(list);
            ser.BuildXml("dep.xml");

            

            Dictionary<string, string> dicDep1 = new Dictionary<string, string>();
            dicDep1.Add("name", "Name");
            dicDep1.Add("adspath", "Path");
            dicDep1.Add("samaccountname", "LoginName");
            dicDep1.Add("mail", "EmailAddr");
            dicDep1.Add("mobile", "MobieNumber");
            dicDep1.Add("telephonenumber", "PhoneNumber");
            var list1 = ADsyn.SearchUser("10.0.0.1", "(objectCategory=CN=Person,CN=Schema,CN=Configuration,DC=suning,DC=com,DC=cn)", dicDep1, "Yinhe.ProcessingCenter.SynAD.PathAnalyseSN");

            Console.WriteLine(list1.Count());
            Yinhe.ProcessingCenter.SerializerXml<List<ADUser>> ser1 = new Yinhe.ProcessingCenter.SerializerXml<List<ADUser>>(list1);
            ser1.BuildXml("user.xml");

          // list.Where(t => t.Name == "用户").FirstOrDefault().Name = "苏宁组织架构";

            ADDepartment curChild = this.GetADTree(list, list1, list.Where(t => t.Name == "用户").FirstOrDefault());

            Yinhe.ProcessingCenter.SerializerXml<ADDepartment> ser2 = new Yinhe.ProcessingCenter.SerializerXml<ADDepartment>(curChild);
            ser2.BuildXml("tree.xml");

            if (list.Count() > 5)
            {
                org.OrganizationSave(list, curChild);
                org.UserInsert(this.UserListFilter(curChild), 2, 0);
            }
        }

        /// <summary>
        /// 华侨城同步
        /// </summary>
        public void ImportToDBHQC()
        {
            OrganizationAD org = new OrganizationAD();
            Dictionary<string, string> dicDep = new Dictionary<string, string>();
            ADsyn = new ADSynchronization("", "uid=a3admin,cn=users,dc=group,dc=chinaoct,dc=com", "a3admin");
            dicDep.Add("ou", "Name");
            dicDep.Add("adspath", "Path");
            dicDep.Add("searchguide", "Guid");
            var list = ADsyn.SearchDepartment("172.16.9.25", "(objectclass=organizationalUnit)", dicDep, "Yinhe.ProcessingCenter.SynAD.PathAnalyseHQC");
            ADDepartment deproot = new ADDepartment() { Name = "华侨城组织架构", Path = "LDAP://172.16.9.26/cn=chinaoct,cn=groups,dc=group,dc=chinaoct,DC=COM", Code = "LDAP://172.16.9.26/cn=华侨城组织架构,cn=groups,dc=group,dc=chinaoct,DC=COM", Level = 0, Guid = "0000000000000000000", ParentName = "" };
            list.Add(deproot);
            Console.WriteLine(list.Count());
            Yinhe.ProcessingCenter.SerializerXml<List<ADDepartment>> ser = new Yinhe.ProcessingCenter.SerializerXml<List<ADDepartment>>(list);
            ser.BuildXml("dep.xml");



            Dictionary<string, string> dicDep1 = new Dictionary<string, string>();
            dicDep1.Add("sn", "Name");
            dicDep1.Add("adspath", "Path");
            dicDep1.Add("uid", "LoginName");
            dicDep1.Add("mail", "EmailAddr");
            dicDep1.Add("seealso", "Guid");
           // dicDep1.Add("o", "DepartMentID");
            dicDep1.Add("o", "Code");
            var list1 = ADsyn.SearchUser("172.16.9.25", "(!(objectclass=container))", dicDep1, "Yinhe.ProcessingCenter.SynAD.PathAnalyseHQC");

            
            Console.WriteLine(list1.Count());
            Yinhe.ProcessingCenter.SerializerXml<List<ADUser>> ser1 = new Yinhe.ProcessingCenter.SerializerXml<List<ADUser>>(list1);
            ser1.BuildXml("user.xml");

            // list.Where(t => t.Name == "用户").FirstOrDefault().Name = "苏宁组织架构";

            ADDepartment curChild = this.GetADTreeHQC(list, list1, deproot);
            _log.Info(string.Format("获取数据 部门数：{0}  用户数 ：{1}", list.Count(), list1.Count()));
            Yinhe.ProcessingCenter.SerializerXml<ADDepartment> ser2 = new Yinhe.ProcessingCenter.SerializerXml<ADDepartment>(curChild);
            ser2.BuildXml("tree.xml");

            if (list.Count() > 5)
            {
                _log.Info("开始同步");
                org.OrganizationSave(list, curChild);
                org.UserInsert(this.UserListFilter(curChild), 1, 0);
            }
            _log.Info("同步结束");
        }


        /// <summary>
        /// 中海投资
        /// </summary>
        public void ImportToDBZHTZ()
        {
            OrganizationAD org = new OrganizationAD();
            Dictionary<string, string> dicDep = new Dictionary<string, string>();
            ADsyn = new ADSynchronization("", "coihl\\coihl_sysuser", "Cohl888");    //连接数据源
            dicDep.Add("name", "Name");
            dicDep.Add("adspath", "Path");
            //dicDep.Add("objectguid", "Guid");    
            var list = ADsyn.SearchDepartment("192.0.0.172", "(objectCategory=CN=Organizational-Unit,CN=Schema,CN=Configuration,DC=cohl,DC=com)", dicDep, "Yinhe.ProcessingCenter.SynAD.PathAnalyseZHTZ");//根据Fileter查找树的子节点（部门）

            Console.WriteLine(list.Count());
            Yinhe.ProcessingCenter.SerializerXml<List<ADDepartment>> ser = new Yinhe.ProcessingCenter.SerializerXml<List<ADDepartment>>(list);
            ser.BuildXml("dep.xml");



            Dictionary<string, string> dicDep1 = new Dictionary<string, string>();
            dicDep1.Add("name", "Name");
            dicDep1.Add("adspath", "Path");
            dicDep1.Add("samaccountname", "LoginName");
            dicDep1.Add("mail", "EmailAddr");
            //dicDep1.Add("objectguid", "Guid");   
            var list1 = ADsyn.SearchUser("192.0.0.172", "(objectCategory=CN=Person,CN=Schema,CN=Configuration,DC=cohl,DC=com)", dicDep1, "Yinhe.ProcessingCenter.SynAD.PathAnalyseZHTZ");

            Console.WriteLine(list1.Count());
            Yinhe.ProcessingCenter.SerializerXml<List<ADUser>> ser1 = new Yinhe.ProcessingCenter.SerializerXml<List<ADUser>>(list1);
            ser1.BuildXml("user.xml");
            ADDepartment curChild = this.GetADTree(list, list1, list.Where(t => t.Name == "中海投資").FirstOrDefault());  //顶部
            Yinhe.ProcessingCenter.SerializerXml<ADDepartment> ser2 = new Yinhe.ProcessingCenter.SerializerXml<ADDepartment>(curChild);
            ser2.BuildXml("tree.xml");
            if (list.Count() > 5)
            {
                org.OrganizationSave(list, curChild);
                org.UserInsert(this.UserListFilter(curChild), 2, 0);
            }
        }


        public void ImportToDBZHHY()
        {
            OrganizationAD org = new OrganizationAD();
            PathAnalyseZHHY analyse = new PathAnalyseZHHY();
            var depList = analyse.GetDepList();
            var userList = analyse.GetUserList();
            ADDepartment curChild = this.GetADTreeByGuidMultDepZHHY(depList, userList, depList.Where(t => t.ParentGuid == "0").FirstOrDefault());  //顶部
            Yinhe.ProcessingCenter.SerializerXml<List<ADDepartment>> ser = new Yinhe.ProcessingCenter.SerializerXml<List<ADDepartment>>(depList);
            ser.BuildXml("dep.xml");
            Yinhe.ProcessingCenter.SerializerXml<List<ADUser>> ser1 = new Yinhe.ProcessingCenter.SerializerXml<List<ADUser>>(userList);
            ser1.BuildXml("user.xml");
            Yinhe.ProcessingCenter.SerializerXml<ADDepartment> ser2 = new Yinhe.ProcessingCenter.SerializerXml<ADDepartment>(curChild);
            ser2.BuildXml("tree.xml");
            if (depList.Count() > 5)
            {
                org.OrganizationSave(depList, curChild);
                org.UserInsertByDepType(userList, 1, 0, 1, 0);
            }
        }


        public void ImportToXHMOBILE()
        {
            XHMobileSynchronization syn = new XHMobileSynchronization();
            var list = syn.GetDataList();
            var result = syn.MobileSyn(list);
        }

        //public int Count(ADDepartment dep)
        //{
        //    int count = 0;

        //    count += dep.SubUsers.Count();
        //    foreach (var item in dep.SubDepartemnt)
        //    {
        //        count += Count(item);
        //    }

        //    return count;
        //}


        /// <summary>
        /// 从组织架构树中取出用户列表
        /// </summary>
        /// <param name="dep"></param>
        /// <returns></returns>
        public List<ADUser> UserListFilter(ADDepartment dep)
        {
            List<ADUser> userList = new List<ADUser>();
            foreach (var item in dep.SubDepartemnt)
            {
               userList.AddRange(UserListFilter(item));
            }
            userList.AddRange(dep.SubUsers);
            return userList;
        }



        public InvokeResult QXSyn()
        {
            InvokeResult result = new InvokeResult();
            try
            {

                string QXOrgUrl = SysAppConfig.QXOrgUrl;
                string QXUserUrl = SysAppConfig.QXUserUrl;

                XmlDocument depDoc = new XmlDocument();
                XmlDocument userDoc = new XmlDocument();
                string orgXMLPath = SysAppConfig.QXOrgXMLPath;
                string userXMLPath = SysAppConfig.QXUserXMLPath;
                Console.WriteLine(orgXMLPath);
                Console.WriteLine(userXMLPath);
                depDoc.Load(orgXMLPath);
                userDoc.Load(userXMLPath);
                var depNodeList = depDoc.SelectNodes("//entry");
                var userNodeList = userDoc.SelectNodes("//entry");
                List<ADDepartment> depList = new List<ADDepartment>();
                List<ADUser> userList = new List<ADUser>();
                foreach (var item in depNodeList)
                {
                    XmlNode node = (XmlNode)item;
                    ADDepartment dep = new ADDepartment();
                    var subNodes = node.ChildNodes;
                    foreach (var s in subNodes)
                    { 
                         XmlNode snode = (XmlNode)s;
                         if (snode.Name == "deptID")
                         {
                             dep.Code = snode.InnerText;
                         }
                         else if (snode.Name == "deptName")
                         {
                             dep.Name = snode.InnerText;
                         }
                         else if (snode.Name == "uniqueID")
                         {
                             dep.Guid = snode.InnerText;
                         }
                    }
                    if (!string.IsNullOrEmpty(dep.Code))
                    {
                        dep.Level = dep.Code.Length / 4 + 1;

                        if (dep.Code.Length > 4)
                        {
                            dep.ParentGuid = dep.Code.Substring(0, dep.Code.Length - 4);
                        }
                        else
                        {
                            dep.ParentGuid = "00";
                        }
                    }
                    depList.Add(dep);

                }

                foreach (var item in userNodeList)
                {
                    XmlNode node = (XmlNode)item;
                    ADUser user = new ADUser();
                    var subNodes = node.ChildNodes;
                    foreach (var s in subNodes)
                    {
                        XmlNode snode = (XmlNode)s;
                        if (snode.Name == "userID")
                        {
                            user.LoginName = snode.InnerText;
                        }
                        else if (snode.Name == "userName")
                        {
                            user.Name = snode.InnerText;
                        }
                        else if (snode.Name == "jobNO")
                        {
                            user.Remark = snode.InnerText;
                        }
                        else if (snode.Name == "deptID")
                        {
                            user.Code = snode.InnerText;
                        }
                        else if (snode.Name == "mail")
                        {
                            user.EmailAddr = snode.InnerText;
                        }
                        else if (snode.Name == "mobile")
                        {
                            user.MobieNumber = snode.InnerText;
                        }
                    }

                    userList.Add(user);
                }

                ADDepartment root = new ADDepartment { Code = "00", Name = "侨鑫集团组织架构",Guid = "00000000" };

                root = QXGetADTree(root, depList, userList);
                OrganizationAD org = new OrganizationAD();
                if (depList.Count() > 5 && userList.Count() >4)
                {
                    org.OrganizationSave(depList, root);
                    org.UserInsertByDepType(userList,2, 0, 0, 1);
                }
            }
            catch (Exception ex)
            { 
            
            }
            return result;
        }

        public ADDepartment QXGetADTree(ADDepartment pNode,List<ADDepartment>AllDepList,List<ADUser> AllUserList)
        {
            ADDepartment node = pNode;
            node.SubDepartemnt = new List<ADDepartment>();
            node.SubUsers = new List<ADUser>();
            node.SubUsers.AddRange(AllUserList.Where(t => t.Code == node.Code).ToList());
            foreach (var item in AllDepList.Where(t => t.ParentGuid == pNode.Code).ToList())
            {
                node.SubDepartemnt.Add(QXGetADTree(item, AllDepList, AllUserList));
            }

            return node;
        }
    }
}
