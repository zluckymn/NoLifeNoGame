using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using Yinhe.ProcessingCenter.DataRule;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using Yinhe.ProcessingCenter.Common;
///<summary>
///人员组织管理
///</summary>
namespace Yinhe.ProcessingCenter.Administration
{
    /// <summary>
    /// 系统用户表处理类
    /// </summary>
    public class SysUserBll
    {
        #region 构造函数
        /// <summary>
        /// 类私有变量
        /// </summary>
        private DataOperation dataOp = null;

        private string tableName = "";
        /// <summary>
        /// 封闭当前默认构造函数
        /// </summary>
        private SysUserBll()
        {
            dataOp = new DataOperation();
        }

        private SysUserBll(DataOperation _dataOp)
        {
            dataOp = _dataOp;
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static SysUserBll _()
        {
            return new SysUserBll();
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static SysUserBll _(DataOperation _dataOp)
        {
            return new SysUserBll(_dataOp);
        }

        #endregion
        #region  操作

        /// <summary>
        /// 获取部门下的用户（通过部门id)2011.2.28郑伯锰优化搜索速度
        /// </summary>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public List<BsonDocument> FindUsersByOrgIdByView(int orgId)
        {
            var AllOrgList = new List<string>();
            AllOrgList.AddRange(dataOp.FindChildNodes("Organization", orgId.ToString()).Select(c => c.Text("orgId")));
            AllOrgList.Add(orgId.ToString());
            //获取组织下的岗位列表
            var OrgPostList = dataOp.FindAllByKeyValList("OrgPost", "orgId", AllOrgList).Select(c => c.Text("postId")).ToList();
            var uerPostList = dataOp.FindAllByKeyValList("UserOrgPost", "postId", OrgPostList).Select(c => c.Text("userId")).ToList();
            var result = dataOp.FindAllByKeyValList("SysUser", "userId", uerPostList).Where(c => c.Int("status") != 2);
            return result.Distinct().ToList();
        }
        /// <summary>
        /// 获取部门岗位下的用户2011.2.28郑伯锰优化搜索速度
        /// </summary>
        /// <param name="orgPostId">部门岗位id</param>
        /// <returns></returns>
        public List<BsonDocument> FindUsersByOrgPostIdByView(int orgPostId)
        {
            var uerPostList = dataOp.FindAllByKeyVal("UserOrgPost", "postId", orgPostId.ToString()).Select(c => c.Text("userId")).ToList();
            var result = dataOp.FindAllByKeyValList("SysUser", "userId", uerPostList).Where(c => c.Int("status") != 2);

            return result.ToList();
        }
        /// <summary>
        /// 获取通用岗位下的用户)2011.2.28郑伯锰优化搜索速度
        /// </summary>
        /// <param name="compostId">通用岗位id</param>
        /// <returns></returns>
        public List<BsonDocument> FindUsersByCommonPostIdByView(int compostId)
        {
            var OrgPostList = dataOp.FindAllByKeyVal("OrgPost", "commPostId", compostId.ToString()).Select(c => c.Text("postId")).ToList();
            var uerPostList = dataOp.FindAllByKeyValList("UserOrgPost", "postId", OrgPostList).Select(c => c.Text("userId")).ToList();
            var result = dataOp.FindAllByKeyValList("SysUser", "userId", uerPostList).Where(c => c.Int("status") != 2);
            return result.ToList();
        }

        /// <summary>
        /// 获取群组下的用户.2011.2.28 郑伯锰添加视图 优化查找速度
        /// </summary>
        /// <param name="sysgrpId">群组id</param>
        /// <returns></returns>
        public List<BsonDocument> FindUsersByGroupIdView(int sysgrpId)
        {
            return new List<BsonDocument>();
        }


        /// <summary>
        /// 查询出系统角色对应的人员表
        /// </summary>
        /// <returns></returns>
        public List<BsonDocument> FindSysRoleUsers(int roleId)
        {
            return new List<BsonDocument>();
        }


        /// <summary>
        /// 通过名称搜索 2011.2.28郑伯锰优化搜索速度
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public List<BsonDocument> FindUsersBySearchByView(string key)
        {
            var query = Query.NE("status", "2");
            var query2 = Query.Matches("name", key);
            var query3 = Query.Matches("tags", key);
            var userList = dataOp.FindAllByQuery("SysUser", Query.And(query, Query.Or(query2, query3)));
            return userList.ToList();
        }

        /// <summary>
        /// 通过微信名称搜索
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public BsonDocument FindUsersByWeiXin(string FromUserName)
        {
            var query1 = Query.EQ("weixin", FromUserName);
            var query2 = Query.NE("status", "2");
            var userObj = dataOp.FindOneByQuery("SysUser", Query.And(query1,query2));
            return userObj;
        }

           /// <summary>
        /// 通过微信名称搜索
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public List<BsonDocument> GetSearchPhoneResult(string keyWord)
        {
            var userObjList = dataOp.FindAll("SysUser").Where(c => c.Text("name").Contains(keyWord) || c.Text("loginName").Contains(keyWord)).ToList();
            return userObjList;
        }

        

        #region 通用树
        public List<Tree> SearchOrg(string key)
        {
            List<Tree> treeList = new List<Tree>();

            var query2 = Query.Matches("name", key);
            var query3 = Query.Matches("tags", key);
            var orgList = dataOp.FindAllByQuery("Organization", Query.Or(query2, query3));

            var orgPostList = dataOp.FindAllByKeyValList("OrgPost", "orgId", orgList.Select(c => c.Text("orgId")).ToList());
            foreach (var org in orgList)
            {
                Tree tree = new Tree();

                tree.Id = org.Text("orgId");
                tree.Name = org.Text("name");
                tree.Level = org.Int("nodeLevel");
                tree.Pid = org.Text("nodePid");
                tree.Key = org.Text("nodeKey");
                tree.NodeEvent = String.Format("parent.GetUserList('{0}',{1},'{2}')", org.Text("name"), org.Text("orgId"), "org");

                treeList.Add(tree);
            }

            foreach (var post in orgPostList)
            {
                Tree tree = new Tree();

                tree.Id = post.Text("postId");
                tree.Name = post.Text("name");
                //tree.Level = 0;
                tree.Pid = post.Text("orgId");
                //tree.Key = org.nodeKey;
                tree.Type = 1;
                tree.NodeEvent = String.Format("parent.GetUserList('{0}',{1},'{2}')", post.Text("name"), post.Text("postId"), "orgpost");

                treeList.Add(tree);
            }

            return treeList;
        }


        public List<Tree> SearchCompost(string key)
        {
            List<Tree> treeList = new List<Tree>();

            #region 通用岗位
            var query2 = Query.Matches("name", key);
            var query3 = Query.Matches("tags", key);
            var comPostList = dataOp.FindAllByQuery("CommonPost", Query.Or(query2, query3));
            foreach (var comPost in comPostList)
            {
                Tree tree = new Tree();
                tree.Id = comPost.Text("comPostId");
                tree.Name = comPost.Text("name");
                tree.Level = 1;
                tree.Pid = "-1";
                //tree.Key = comPost.order.ToString();
                tree.NodeEvent = String.Format("parent.GetUserList('{0}',{1},'{2}')", comPost.Text("name"), comPost.Text("comPostId"), "compost");

                treeList.Add(tree);
            }
            #endregion

            #region 通用岗位下的部门岗位
            var query5 = Query.Matches("name", key);
            var query6 = Query.Matches("tags", key);
            var orgPostList = dataOp.FindAllByQuery("OrgPost", Query.Or(query5, query6));

            foreach (var post in orgPostList)
            {
                Tree tree = new Tree();

                tree.Id = post.Text("postId");
                tree.Name = post.Text("name");
                //tree.Level = 0;
                tree.Pid = post.Text("comPostId");
                //tree.Key = org.nodeKey;
                tree.Type = 1;
                tree.NodeEvent = String.Format("parent.GetUserList('{0}',{1},'{2}')", post.Text("name"), post.Text("postId"), "orgpost");

                treeList.Add(tree);
            }
            #endregion

            return treeList;
        }

        public List<Tree> SearchGroup(string key)
        {
            List<Tree> treeList = new List<Tree>();

            var groupList = new List<BsonDocument>();

            foreach (var group in groupList)
            {
                Tree tree = new Tree();

                tree.Id = group.Text("sysGrpId");
                tree.Name = group.Text("name");
                tree.Level = group.Int("nodeLevel");
                tree.Pid = group.Text("nodePid");
                tree.Key = group.Text("nodeKey");

                tree.NodeEvent = String.Format("parent.GetUserList('{0}',{1},'{2}')", group.Text("name"), group.Text("sysGrpId"), "group");

                treeList.Add(tree);
            }

            return treeList;

        }
        #endregion

        #endregion

    }

}
