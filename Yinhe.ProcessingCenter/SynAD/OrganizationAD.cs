using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System.IO;
using System.Web.Script.Serialization;
using Yinhe.ProcessingCenter.DataRule;
using System.Diagnostics;
using Yinhe.ProcessingCenter.Common;

namespace Yinhe.ProcessingCenter.SynAD
{
    /// <summary>
    /// AD组织部门处理类
    /// </summary>
    public class OrganizationAD
    {
        private DataOperation _dataOp = new DataOperation();
        public OrganizationAD()
        {

        }

        /// <summary>
        /// 组织架构同步
        /// </summary>
        /// <param name="adList"></param>
        /// <param name="tree"></param>
        /// <returns></returns>
        public InvokeResult OrganizationSave(List<ADDepartment> adList, ADDepartment tree)
        {
            InvokeResult result = new InvokeResult();
            try
            {
                #region 删除已经删除的组织架构
                var oldOrgList = _dataOp.FindAll("Organization").Where(t => t.Int("state", 0) == 0).ToList();
                var delOrgQuery = (from i in oldOrgList
                                   where !(from d in adList select d.Guid).Contains(i.Text("guid"))
                                   select i).Distinct().ToList();

                //删除岗位
                var orgIds = delOrgQuery.Select(t => t.Text("orgId")).Distinct().ToList();

                var delOrgPost = _dataOp.FindAll("OrgPost").Where(t => orgIds.Contains(t.Text("orgId"))).Distinct().ToList();

                //删除用户岗位关联
                var postIds = delOrgPost.Select(t => t.Text("postId")).Distinct().ToList();

                var delUserOrgPost = _dataOp.FindAll("UserOrgPost").Where(t => postIds.Contains(t.Text("postId"))).Distinct().ToList();

                var userOrgPostIds = delUserOrgPost.Select(t => t.Text("relId")).Distinct().ToList();

                BsonDocument doc = new BsonDocument();
                doc.Add("state", 1);

                result = _dataOp.BatchUpdateByPrimaryKey("Organization", orgIds, doc); //逻辑删除

                if (result.Status == Status.Failed)
                {
                    Console.WriteLine("Organization 逻辑删除失败：" + result.Message);
                }

                result = _dataOp.BatchDeleteByPrimaryKey("OrgPost", postIds);
                //result = _dataOp.BatchDeleteByPrimaryKey("OrgPost", postIds);

                if (result.Status == Status.Failed)
                {
                    Console.WriteLine("OrgPost 删除失败：" + result.Message);
                }

                result = _dataOp.BatchDeleteByPrimaryKey("UserOrgPost", userOrgPostIds);

                if (result.Status == Status.Failed)
                {
                    Console.WriteLine("UserOrgPost 删除失败：" + result.Message);
                }

                OrgTreeInsert(tree, null);
                #endregion
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return result;
        }

        /// <summary>
        /// 组织架构同步
        /// </summary>
        /// <param name="node"></param>
        /// <param name="pnode"></param>
        /// <returns></returns>
        public List<ADDepartment> OrgTreeInsert(ADDepartment node, ADDepartment pnode)
        {
            InvokeResult result = new InvokeResult();
            List<ADDepartment> nodeList = new List<ADDepartment>();
            ADDepartment retNode = new ADDepartment();
            ADDepartment subnode = new ADDepartment();
            try
            {
                subnode = node;
                retNode.Name = subnode.Name;
                retNode.Level = subnode.Level;
                int pid = 0;

                if (pnode == null)
                {
                    pid = 0;
                }
                else
                {
                    pid = pnode.DepId;
                }

                var org = _dataOp.FindAll("Organization").Where(t => t.Text("guid") == subnode.Guid).FirstOrDefault();
                BsonDocument structModel = new BsonDocument();
                structModel.Add("name", subnode.Name);
                structModel.Add("nodePid", pid.ToString());
                structModel.Add("guid", subnode.Guid);
                structModel.Add("state", "0");
                structModel.Add("code", subnode.Code);
                Console.WriteLine("组织架构：{0}——>{1}", structModel.Text("name"), structModel.Text("guid"));
                if (org == null)
                {

                    result = _dataOp.Insert("Organization", structModel);

                }
                else
                {
                    result = _dataOp.Update("Organization", "db.Organization.distinct('_id',{'guid':'" + subnode.Guid + "'})", structModel);
                }
                if (result.Status == Status.Successful)
                {
                    var orgpost = _dataOp.FindAllByKeyVal("OrgPost", "orgId", result.BsonInfo.Text("orgId"));
                    if (orgpost.Count() == 0)
                    {
                        BsonDocument post = new BsonDocument();
                        post.Add("name", "空缺岗位");
                        post.Add("orgId", result.BsonInfo.Text("orgId"));
                        post.Add("type", "0");
                        _dataOp.Insert("OrgPost", post);
                    }

                    subnode.DepId = result.BsonInfo.Int("orgId");
                    retNode.DepId = subnode.DepId;
                }
                nodeList.Add(retNode);
                // Console.WriteLine(string.Format("Level:{0}  Name:{1}", subnode.Level, subnode.Name));

                if (subnode.SubDepartemnt != null)
                {
                    foreach (var item in subnode.SubDepartemnt)
                    {
                        nodeList.AddRange(OrgTreeInsert(item, subnode));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return nodeList;
        }

        /// <summary>
        /// 组织架构同步
        /// </summary>
        /// <param name="node"></param>
        /// <param name="pnode"></param>
        /// <returns></returns>
        public List<ADDepartment> OrgTreeInsertSS(ADDepartment node, ADDepartment pnode)
        {
            InvokeResult result = new InvokeResult();
            List<ADDepartment> nodeList = new List<ADDepartment>();
            ADDepartment retNode = new ADDepartment();
            ADDepartment subnode = new ADDepartment();
            try
            {
                subnode = node;
                retNode.Name = subnode.Name;
                retNode.Level = subnode.Level;
                int pid = 0;

                if (pnode == null)
                {
                    pid = 0;
                }
                else
                {
                    pid = pnode.DepId;
                }
                var allList = _dataOp.FindAll("Organization").ToList();
                var porg = allList.Where(t => t.Text("name") == node.ParentName).FirstOrDefault();
                var org = allList.Where(t => t.Text("name") == subnode.Name && t.Int("nodePid") == porg.Int("orgId")).FirstOrDefault();

                BsonDocument structModel = new BsonDocument();
                structModel.Add("name", subnode.Name);
                structModel.Add("nodePid", pid.ToString());
                structModel.Add("guid", subnode.Guid);
                structModel.Add("state", "0");
                structModel.Add("code", subnode.Code);
                Console.WriteLine("组织架构：{0}——>{1}", structModel.Text("name"), structModel.Text("guid"));
                if (org == null)
                {
                    result = _dataOp.Insert("Organization", structModel);
                }
                else
                {
                    result = _dataOp.Update("Organization", Query.And(Query.EQ("name", subnode.Name), Query.EQ("nodePid", porg.Int("orgId").ToString())), structModel);
                }
                if (result.Status == Status.Successful)
                {
                    var orgpost = _dataOp.FindAllByKeyVal("OrgPost", "orgId", result.BsonInfo.Text("orgId"));
                    if (orgpost.Count() == 0)
                    {
                        BsonDocument post = new BsonDocument();
                        post.Add("name", "空缺岗位");
                        post.Add("orgId", result.BsonInfo.Text("orgId"));
                        post.Add("type", "0");
                        _dataOp.Insert("OrgPost", post);
                    }
                    subnode.DepId = result.BsonInfo.Int("orgId");
                    retNode.DepId = subnode.DepId;
                }
                nodeList.Add(retNode);
                // Console.WriteLine(string.Format("Level:{0}  Name:{1}", subnode.Level, subnode.Name));

                if (subnode.SubDepartemnt != null)
                {
                    foreach (var item in subnode.SubDepartemnt)
                    {
                        nodeList.AddRange(OrgTreeInsertSS(item, subnode));
                    }
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return nodeList;
        }

        /// <summary>
        /// 部门筛选
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public InvokeResult SetDepartmentFilter(List<string> list)
        {
            InvokeResult result = new InvokeResult();
            var orgList = _dataOp.FindAll("Organization").ToList();
            try
            {
                foreach (var item in orgList)
                {
                    if (!list.Contains(item.Text("name")))
                    {
                        BsonDocument doc = new BsonDocument();
                        doc.Add("state", "1");
                        _dataOp.Update("Organization", Query.EQ("orgId", item.Text("orgId")), doc);
                    }
                    //else {
                    //    BsonDocument doc = new BsonDocument();
                    //    doc.Add("state", "0");
                    //    _dataOp.Update("Organization", Query.EQ("orgId", item.Text("orgId")), doc);
                    //}
                }
            }
            catch (Exception ex)
            { 
            
            }
            return result;
        }
        /// <summary>
        /// 用户同步  单用户
        /// </summary>
        /// <param name="userList"></param>
        /// <param name="type"></param>
        /// <param name="defaultState"></param>
        /// <returns></returns>
        public List<ADUser> UserInsert(List<ADUser> userList, int type, int defaultState)
        {
            return UserInsertByDepType(userList, type, defaultState,0,0);
        }

        /// <summary>
        /// 用户同步
        /// </summary>
        /// <param name="userList">用户列表</param>
        /// <param name="type">部门获取方式 0 名字 1 guid</param>
        /// <returns></returns>
        public List<ADUser> UserInsertByDepType(List<ADUser> userList, int type, int defaultState,int depType,int userType)
        {


            List<ADUser> list = new List<ADUser>();
            InvokeResult result = new InvokeResult();
            try
            {
                var oldUserList = _dataOp.FindAll("SysUser").Where(t => t.Int("state") == 0).Distinct().ToList();
                var delUserQuery = userType==0?(from i in oldUserList
                                    where !(from d in userList select d.Guid).Contains(i.Text("guid"))
                                    select i).Where(t => t.Text("name") != "admin").Distinct().ToList():
                                    (from i in oldUserList
                                     where !(from d in userList select d.LoginName).Contains(i.Text("loginName"))
                                     select i).Where(t => t.Text("name") != "admin").Distinct().ToList();

                var userIds = delUserQuery.Select(t => t.Text("userId")).Distinct().ToList();

                var delUserOrgPost = _dataOp.FindAll("UserOrgPost").Where(t => userIds.Contains(t.Text("userId"))).Distinct().ToList();

                var relIds = delUserOrgPost.Select(t => t.Text("relIds")).Distinct().ToList();

                result = _dataOp.BatchDeleteByPrimaryKey("UserOrgPost", relIds);

                if (result.Status == Status.Failed)
                {
                    Console.WriteLine("人员岗位删除失败:" + result.Message);
                }
                BsonDocument doc = new BsonDocument();
                doc.Add("state", "1");
                doc.Add("status", "1");
                result = _dataOp.BatchUpdateByPrimaryKey("SysUser", userIds, doc); //逻辑删除
                if (result.Status == Status.Failed)
                {
                    Console.WriteLine("人员删除失败:" + result.Message);
                }

                foreach (var user in userList)
                {
                    if (depType == 0) //单部门
                    {
                        UserAdd(user, type, defaultState, userType);
                    }
                    else if(depType == 1) //多部门
                    {
                        UserAddMulDep(user, type, defaultState, userType);
                    }
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return list;
        }


        /// <summary>
        /// 单个部门
        /// </summary>
        /// <param name="user"></param>
        /// <param name="type"></param>
        /// <param name="defaultState"></param>
        /// <returns></returns>
        public InvokeResult UserAdd(ADUser user, int type, int defaultState, int userType)
        {
            InvokeResult result = new InvokeResult();

            try
            {

                string guid = user.Guid;
                var oldUser = userType == 0 ? _dataOp.FindOneByKeyVal("SysUser", "guid", guid) : _dataOp.FindOneByKeyVal("SysUser", "loginName", user.LoginName);
                BsonDocument doc = new BsonDocument();
                doc.Add("name", user.Name);
                doc.Add("loginName", user.LoginName);
                doc.Add("phoneNumber", user.PhoneNumber);
                doc.Add("guid", user.Guid);
                doc.Add("adpath", user.Path);
                doc.Add("state", defaultState.ToString());
                doc.Add("status", user.Status.ToString());
                doc.Add("remark", user.Remark);
                Console.WriteLine("人员：{0}——>{1}", user.Name, user.Guid);
                if (!string.IsNullOrEmpty(user.MobieNumber))
                {
                    doc.Add("mobileNumber", user.MobieNumber);
                }
                if (!string.IsNullOrEmpty(user.EmailAddr))
                {
                    doc.Add("emailAddr", user.EmailAddr);
                }
                if (!string.IsNullOrEmpty(user.PassWord))
                {
                    doc.Add("loginPwd", user.PassWord);
                }
                if (oldUser == null)//新增
                {
                    if (doc.Elements.Where(t => t.Name == "loginPwd").Count() == 0)
                    {
                        doc.Add("loginPwd", "8888");
                    }
                    result = _dataOp.Insert("SysUser", doc);
                }
                else
                {

                        if (userType == 0)
                        {
                            result = _dataOp.Update("SysUser", "db.SysUser.distinct('_id',{'guid':'" + user.Guid + "'})", doc);
                        }
                        else
                        {
                            result = _dataOp.Update("SysUser", "db.SysUser.distinct('_id',{'loginName':'" + user.LoginName + "'})", doc);
                        }
                }



                if (result.Status != Status.Failed)
                {
                    if (SysAppConfig.IsBelongOneOrg == true)
                    {
                        _dataOp.Delete("UserOrgPost", Query.EQ("userId", result.BsonInfo.Text("userId")));
                    }


                    BsonDocument org = new BsonDocument();
                    switch (type)
                    {
                        case 0:
                            org = _dataOp.FindOneByKeyVal("Organization", "name", user.DepartMentID);
                            break;
                        case 1:
                            org = _dataOp.FindOneByKeyVal("Organization", "guid", user.DepartMentGuid);
                            break;
                        case 2:
                            org = _dataOp.FindOneByKeyVal("Organization", "code", user.Code);
                            break;
                        case 3:
                            var porg = _dataOp.FindOneByQuery("Organization", Query.EQ("name", user.GrandDepartMentID));
                            org = _dataOp.FindOneByQuery("Organization", Query.And( Query.EQ("name", user.DepartMentID),Query.EQ("nodePid",porg.Text("orgId"))));
                            break;

                    }

                    if (org != null)
                    {

                        var post = _dataOp.FindOneByKeyVal("OrgPost", "orgId", org.Text("orgId"));
                        if (post != null)
                        {

                            var subPost = _dataOp.FindOneByQueryStr("UserOrgPost", string.Format("userId={0}&postId={1}", result.BsonInfo.Text("userId"), post.Text("postId")));
                            if (subPost == null)
                            {
                                BsonDocument relDoc = new BsonDocument();
                                relDoc.Add("userId", result.BsonInfo.Text("userId"));
                                relDoc.Add("postId", post.Text("postId"));
                                result = _dataOp.Insert("UserOrgPost", relDoc);
                            }
                        }
                    }

                }


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return result;
        }

        /// <summary>
        /// 多个部门
        /// </summary>
        /// <param name="user"></param>
        /// <param name="type"></param>
        /// <param name="defaultState"></param>
        /// <returns></returns>
        public InvokeResult UserAddMulDep(ADUser user, int type, int defaultState, int userType)
        {
            InvokeResult result = new InvokeResult();

            try
            {

                string guid = user.Guid;
                var oldUser = userType == 0 ? _dataOp.FindOneByKeyVal("SysUser", "guid", guid) : _dataOp.FindOneByKeyVal("SysUser", "loginName", user.LoginName);
               
                BsonDocument doc = new BsonDocument();
                doc.Add("name", user.Name);
                doc.Add("loginName", user.LoginName);
                //doc.Add("mobileNumber", user.MobieNumber);
                doc.Add("phoneNumber", user.PhoneNumber);
                doc.Add("guid", user.Guid);
                doc.Add("adpath", user.Path);
                doc.Add("state", defaultState.ToString());
                doc.Add("status", user.Status.ToString());
                Console.WriteLine("人员：{0}——>{1}", user.Name, user.Guid);
                if (!string.IsNullOrEmpty(user.MobieNumber))
                {
                    doc.Add("mobileNumber", user.MobieNumber);
                }
                if (!string.IsNullOrEmpty(user.EmailAddr))
                {
                    doc.Add("emailAddr", user.EmailAddr);
                }

                if (oldUser == null)//新增
                {
                    doc.Add("loginPwd", "8888");
                    result = _dataOp.Insert("SysUser", doc);
                }
                else
                {
                    if (userType == 0)
                    {
                        result = _dataOp.Update("SysUser", "db.SysUser.distinct('_id',{'guid':'" + user.Guid + "'})", doc);
                    }
                    else
                    {
                        result = _dataOp.Update("SysUser", "db.SysUser.distinct('_id',{'loginName':'" + user.LoginName + "'})", doc);
                    }
                }



                if (result.Status != Status.Failed)
                {
                    foreach (var item in user.DepartMentGuids)
                    {
                        BsonDocument org = new BsonDocument();
                        switch (type)
                        {
                            case 0:
                                org = _dataOp.FindOneByKeyVal("Organization", "name", item);
                                break;
                            case 1:
                                org = _dataOp.FindOneByKeyVal("Organization", "guid", item);
                                break;
                            case 2:
                                org = _dataOp.FindOneByKeyVal("Organization", "code", item);
                                break;

                        }

                        if (org != null)
                        {

                            var post = _dataOp.FindOneByKeyVal("OrgPost", "orgId", org.Text("orgId"));
                            if (post != null)
                            {

                                var subPost = _dataOp.FindOneByQueryStr("UserOrgPost", string.Format("userId={0}&postId={1}", result.BsonInfo.Text("userId"), post.Text("postId")));
                                if (subPost == null)
                                {
                                    BsonDocument relDoc = new BsonDocument();
                                    relDoc.Add("userId", result.BsonInfo.Text("userId"));
                                    relDoc.Add("postId", post.Text("postId"));
                                    
                                    //result = _dataOp.Insert("UserOrgPost", relDoc);
                                    if (SysAppConfig.CustomerCode == CustomerCode.ZHHY.ToString() && oldUser != null)
                                    {

                                    }
                                    else
                                    {
                                        result = _dataOp.Insert("UserOrgPost", relDoc);
                                    }
                                }
                            }
                        }
                    }


                }


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return result;
        }
    }
}
