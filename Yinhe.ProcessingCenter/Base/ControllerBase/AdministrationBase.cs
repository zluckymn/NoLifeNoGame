using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Collections.Specialized;
using MongoDB.Bson;
using System.Web;
using Yinhe.ProcessingCenter.MvcFilters;
using Yinhe.ProcessingCenter.Document;
using System.Text.RegularExpressions;
using Yinhe.ProcessingCenter.DataRule;
using Yinhe.ProcessingCenter;
using Yinhe.ProcessingCenter.Common;
using Yinhe.ProcessingCenter.DesignManage.TaskFormula;
using MongoDB.Driver.Builders;
using System.Transactions;
using Yinhe.ProcessingCenter.DesignManage;
using System.Xml.Linq;
using Yinhe.ProcessingCenter.Administration;
using MongoDB.Driver;
using System.Collections;

///<summary>
///后台处理中心
///</summary>
namespace Yinhe.ProcessingCenter
{
    /// <summary>
    /// 用户管理后台处理基类
    /// </summary>
    public partial class ControllerBase : Controller
    {
        /// <summary>
        /// 保存提交上来的数据
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SaveSysUser(FormCollection saveForm)
        {
            InvokeResult result = new InvokeResult();
            string tbName = saveForm["tbName"] != null ? saveForm["tbName"] : "";
            string queryStr = saveForm["queryStr"] != null ? saveForm["queryStr"] : "";
            string dataStr = "";

            string loginName = saveForm["loginName"];

            if (loginName != "")
            {
                var user = dataOp.FindOneByKeyVal("SysUser", "loginName", loginName);

                if (user != null && string.IsNullOrEmpty(queryStr))
                {
                    result.Message = "系统已经存在同名用户！";
                    result.Status = Status.Failed;
                    return Json(TypeConvert.InvokeResultToPageJson(result), JsonRequestBehavior.AllowGet);
                }
            }

            foreach (var tempKey in saveForm.AllKeys)
            {
                if (tempKey == "tbName" || tempKey == "queryStr" || tempKey.Contains("fileList[") || tempKey.Contains("param.") || tempKey.Contains("postIds") || tempKey.Contains("projRoleId") || tempKey.Contains("sysRoleId")) continue;

                dataStr += string.Format("{0}={1}&", tempKey, saveForm[tempKey]);
            }


            result = dataOp.Save(tbName, queryStr, dataStr);

            if (result.Status == Status.Successful)
            {
                #region 插入部门关联
                List<StorageData> saveList = new List<StorageData>();

                string postIdStr = PageReq.GetForm("postIds");
                List<string> postIdList = postIdStr.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();

                List<BsonDocument> oldRelList = dataOp.FindAllByKeyVal("UserOrgPost", "userId", result.BsonInfo.String("userId")).ToList();   //旧的关联

                foreach (var postId in postIdList)
                {
                    BsonDocument tempRel = oldRelList.Where(t => t.String("postId") == postId.Trim()).FirstOrDefault(); //旧的关联

                    if (tempRel == null)    //如果不存在,则添加
                    {
                        StorageData tempSave = new StorageData();
                        tempSave.Name = "UserOrgPost";
                        tempSave.Type = StorageType.Insert;
                        tempSave.Document = new BsonDocument().Add("userId", result.BsonInfo.String("userId")).Add("postId", postId.Trim().ToString());

                        saveList.Add(tempSave);
                    }
                }

                foreach (var tempRel in oldRelList)
                {
                    if (postIdList.Contains(tempRel.String("postId")) == false)    //如果不存在,则删除
                    {
                        StorageData tempSave = new StorageData();
                        tempSave.Name = "UserOrgPost";
                        tempSave.Type = StorageType.Delete;
                        tempSave.Query = Query.EQ("relId", tempRel.String("relId"));

                        saveList.Add(tempSave);
                    }
                }

                dataOp.BatchSaveStorageData(saveList);

                #endregion

                #region  角色关联
                string projRoleId = PageReq.GetForm("projRoleId");
                string sysRoleId = PageReq.GetForm("sysRoleId");

                string allStr = string.Format("{0},{1}", projRoleId, sysRoleId);
                SaveUserRole(allStr, result.BsonInfo.Text("userId"), 0);

                #endregion

                #region 文件上传
                int primaryKey = 0;
                TableRule rule = new TableRule(tbName);
                string keyName = rule.ColumnRules.Where(t => t.IsPrimary == true).FirstOrDefault().Name;
                if (!string.IsNullOrEmpty(queryStr))
                {
                    var query = TypeConvert.NativeQueryToQuery(queryStr);
                    var recordDoc = dataOp.FindOneByQuery(tbName, query);
                    saveForm["keyValue"] = result.BsonInfo.Text(keyName);
                    if (recordDoc != null)
                    {
                        primaryKey = recordDoc.Int(keyName);
                    }
                }

                if (primaryKey == 0)//新建
                {
                    if (saveForm["tableName"] != null)
                    {
                        saveForm["keyValue"] = result.BsonInfo.Text(keyName);

                    }
                }
                else//编辑
                {
                    #region 删除文件
                    string delFileRelIds = saveForm["delFileRelIds"] != null ? saveForm["delFileRelIds"] : "";
                    if (!string.IsNullOrEmpty(delFileRelIds))
                    {
                        FileOperationHelper opHelper = new FileOperationHelper();
                        try
                        {
                            string[] fileArray;
                            if (delFileRelIds.Length > 0)
                            {
                                fileArray = delFileRelIds.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                                if (fileArray.Length > 0)
                                {
                                    foreach (var item in fileArray)
                                    {
                                        result = opHelper.DeleteFileByRelId(int.Parse(item));
                                        if (result.Status == Status.Failed)
                                        {
                                            break;
                                        }
                                    }
                                }
                            }

                        }
                        catch (Exception ex)
                        {
                            result.Status = Status.Failed;
                            result.Message = ex.Message;
                            return Json(TypeConvert.InvokeResultToPageJson(result));
                        }
                    }
                    #endregion

                    saveForm["keyValue"] = primaryKey.ToString();
                }
                result.FileInfo = SaveMultipleUploadFiles(saveForm);
                #endregion
            }

            return Json(TypeConvert.InvokeResultToPageJson(result), JsonRequestBehavior.AllowGet);
        }


        /// <summary>
        /// 保存提交上来的数据
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SaveSysUserZHHY(FormCollection saveForm)
        {
            InvokeResult result = new InvokeResult();
            string tbName = saveForm["tbName"] != null ? saveForm["tbName"] : "";
            string queryStr = saveForm["queryStr"] != null ? saveForm["queryStr"] : "";
            string dataStr = "";
            string postIdStrNew = PageReq.GetForm("postIds");
            List<string> postIdListNew = postIdStrNew.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();
            var postIdNew = "";
            if (postIdListNew.Count() > 0)
            {
                var comPostId = dataOp.FindOneByQuery("OrgPost", Query.EQ("postId", postIdListNew[0])).Text("comPostId"); //找出OrgPost中的comPostId
                postIdNew = comPostId;
                    //dataOp.FindOneByQuery("CommonPost", Query.EQ("comPostId", comPostId)).Text("order"); //将CommonPost表中的Order字段存入用户表中，按照此字段排序
            }
            if (postIdNew == "")
            {
                postIdNew = "999";
            }
            string loginName = saveForm["loginName"].Trim();

            if (loginName != "")
            {
                var user = dataOp.FindOneByKeyVal("SysUser", "loginName", loginName);

                if (user != null && string.IsNullOrEmpty(queryStr))
                {
                    result.Message = "系统已经存在同名用户！";
                    result.Status = Status.Failed;
                    return Json(TypeConvert.InvokeResultToPageJson(result), JsonRequestBehavior.AllowGet);
                }
            }

            foreach (var tempKey in saveForm.AllKeys)
            {
                if (tempKey == "tbName" || tempKey == "queryStr" || tempKey.Contains("fileList[") || tempKey.Contains("param.") || tempKey.Contains("postIds") || tempKey.Contains("projRoleId") || tempKey.Contains("sysRoleId")) continue;

                dataStr += string.Format("{0}={1}&", tempKey, saveForm[tempKey]);
            }
            dataStr += string.Format("comPostId={0}", postIdNew); //记录用户所在岗位（未考虑多岗位情况）

            result = dataOp.Save(tbName, queryStr, dataStr);

            if (result.Status == Status.Successful)
            {
                #region 插入部门关联
                List<StorageData> saveList = new List<StorageData>();

                string postIdStr = PageReq.GetForm("postIds");
                List<string> postIdList = postIdStr.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();

                List<BsonDocument> oldRelList = dataOp.FindAllByKeyVal("UserOrgPost", "userId", result.BsonInfo.String("userId")).ToList();   //旧的关联

                foreach (var postId in postIdList)
                {
                    BsonDocument tempRel = oldRelList.Where(t => t.String("postId") == postId.Trim()).FirstOrDefault(); //旧的关联

                    if (tempRel == null)    //如果不存在,则添加
                    {
                        StorageData tempSave = new StorageData();
                        tempSave.Name = "UserOrgPost";
                        tempSave.Type = StorageType.Insert;
                        tempSave.Document = new BsonDocument().Add("userId", result.BsonInfo.String("userId")).Add("postId", postId.Trim().ToString());

                        saveList.Add(tempSave);
                    }
                }

                foreach (var tempRel in oldRelList)
                {
                    if (postIdList.Contains(tempRel.String("postId")) == false)    //如果不存在,则删除
                    {
                        StorageData tempSave = new StorageData();
                        tempSave.Name = "UserOrgPost";
                        tempSave.Type = StorageType.Delete;
                        tempSave.Query = Query.EQ("relId", tempRel.String("relId"));

                        saveList.Add(tempSave);
                    }
                }

                dataOp.BatchSaveStorageData(saveList);

                #endregion

                #region  角色关联
                string projRoleId = PageReq.GetForm("projRoleId");
                string sysRoleId = PageReq.GetForm("sysRoleId");

                string allStr = string.Format("{0},{1}", projRoleId, sysRoleId);
                SaveUserRole(allStr, result.BsonInfo.Text("userId"), 0);

                #endregion

                #region 文件上传
                int primaryKey = 0;
                TableRule rule = new TableRule(tbName);
                string keyName = rule.ColumnRules.Where(t => t.IsPrimary == true).FirstOrDefault().Name;
                if (!string.IsNullOrEmpty(queryStr))
                {
                    var query = TypeConvert.NativeQueryToQuery(queryStr);
                    var recordDoc = dataOp.FindOneByQuery(tbName, query);
                    saveForm["keyValue"] = result.BsonInfo.Text(keyName);
                    if (recordDoc != null)
                    {
                        primaryKey = recordDoc.Int(keyName);
                    }
                }

                if (primaryKey == 0)//新建
                {
                    if (saveForm["tableName"] != null)
                    {
                        saveForm["keyValue"] = result.BsonInfo.Text(keyName);

                    }
                }
                else//编辑
                {
                    #region 删除文件
                    string delFileRelIds = saveForm["delFileRelIds"] != null ? saveForm["delFileRelIds"] : "";
                    if (!string.IsNullOrEmpty(delFileRelIds))
                    {
                        FileOperationHelper opHelper = new FileOperationHelper();
                        try
                        {
                            string[] fileArray;
                            if (delFileRelIds.Length > 0)
                            {
                                fileArray = delFileRelIds.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                                if (fileArray.Length > 0)
                                {
                                    foreach (var item in fileArray)
                                    {
                                        result = opHelper.DeleteFileByRelId(int.Parse(item));
                                        if (result.Status == Status.Failed)
                                        {
                                            break;
                                        }
                                    }
                                }
                            }

                        }
                        catch (Exception ex)
                        {
                            result.Status = Status.Failed;
                            result.Message = ex.Message;
                            return Json(TypeConvert.InvokeResultToPageJson(result));
                        }
                    }
                    #endregion

                    saveForm["keyValue"] = primaryKey.ToString();
                }
                result.FileInfo = SaveMultipleUploadFiles(saveForm);
                #endregion
            }

            return Json(TypeConvert.InvokeResultToPageJson(result), JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 保存用户角色
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public InvokeResult SaveUserRole(string ids, string userId, int isDataRight)
        {
            InvokeResult result = new InvokeResult();
            try
            {
                List<string> IdList = ids.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();
                List<StorageData> saveList = new List<StorageData>();
                List<BsonDocument> oldRelList = dataOp.FindAllByKeyVal("SysRoleUser", "userId", userId).ToList();   //旧的关联

                foreach (var roleId in IdList)
                {
                    BsonDocument tempRel = oldRelList.Where(t => t.String("roleId") == roleId.Trim()).FirstOrDefault(); //旧的关联

                    if (tempRel == null)    //如果不存在,则添加
                    {
                        StorageData tempSave = new StorageData();
                        tempSave.Name = "SysRoleUser";
                        tempSave.Type = StorageType.Insert;
                        tempSave.Document = new BsonDocument().Add("userId", userId.ToString()).Add("roleId", roleId.Trim().ToString());

                        saveList.Add(tempSave);
                    }
                }

                foreach (var roleId in oldRelList)
                {
                    if (IdList.Contains(roleId.String("roleId")) == false)    //如果不存在,则删除
                    {
                        StorageData tempSave = new StorageData();
                        tempSave.Name = "SysRoleUser";
                        tempSave.Type = StorageType.Delete;
                        tempSave.Query = Query.And(Query.EQ("roleId", roleId.String("roleId")), Query.EQ("userId", roleId.String("userId")));
                        saveList.Add(tempSave);
                    }
                }

                dataOp.BatchSaveStorageData(saveList);
                result.Status = Status.Successful;
            }
            catch (Exception ex)
            {
                result.Status = Status.Failed;
                result.Message = ex.Message;
            }

            return result;
        }
        /// <summary>
        /// 获取部门树的XML列表
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="queryStr"></param>
        /// <returns></returns>
        public ActionResult GetOrgAndPostTreeXML()
        {
            int orgId = PageReq.GetFormInt("id") != 0 ? PageReq.GetFormInt("id") : (PageReq.GetParamInt("id") != 0 ? PageReq.GetParamInt("id") : 0);   //父级OrgId,如果传入,则展示子级,不包括父级
            int lv = PageReq.GetFormInt("lv") != 0 ? PageReq.GetFormInt("lv") : (PageReq.GetParamInt("lv") != 0 ? PageReq.GetParamInt("lv") : 0);   //展示层级,0为不限层级

            BsonDocument curOrg = dataOp.FindOneByKeyVal("Organization", "orgId", orgId.ToString());    //当前org

            int maxLv = lv == 0 ? 0 : curOrg.Int("nodeLevel") + lv;                   //展示的最大层级,0 表示不限层级

            List<BsonDocument> orgList = new List<BsonDocument>();                  //所有展示的部门信息

            if (maxLv > 0)
            {

                orgList = dataOp.FindAll("Organization").Where(t => t.Int("nodeLevel") <= maxLv && t.Int("state") == 0).ToList();
            }
            else
            {

                orgList = dataOp.FindAll("Organization").Where(t => t.Int("state") == 0).ToList();
            }
            if (SysAppConfig.CustomerCode == CustomerCode.ZHHY.ToString() && PageReq.GetParamInt("isHideOrg") == 1)
            {
                orgList = orgList.Where(c => c.Text("isShow") == "1").ToList();
            }
            else if (SysAppConfig.CustomerCode == CustomerCode.ZHHY.ToString()) {
                orgList = orgList.Where(c => c.Text("isShowNew") == "1").ToList();
            }

            List<string> orgIdList = orgList.Select(t => t.String("orgId")).ToList();

            List<BsonDocument> postList = dataOp.FindAllByKeyValList("OrgPost", "orgId", orgIdList).ToList();       //所有展示的岗位列表

            List<TreeNode> treeList = this.GetOrgAndPostTreeList(orgList, postList, orgId, maxLv);

            return new XmlTree(treeList);
        }



        /// <summary>
        /// 获取角色json列表
        /// </summary>
        /// <returns></returns>
        public JsonResult GetRoleJson()
        {
            int isDataRight = PageReq.GetParamInt("isDataRight");
            List<BsonDocument> list = dataOp.FindAll("SysRole").ToList();
            if (isDataRight >= 0)
            {
                list = list.Where(t => t.Int("isDataRight") == isDataRight).ToList();
            }
            List<Item> itemList = new List<Item>();
            foreach (var item in list)
            {
                Item it = new Item();
                it.id = item.Int("roleId");
                it.name = item.Text("name");
                it.type = item.Text("isDataRight");
                itemList.Add(it);
            }
            return Json(itemList, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 递归获取对应的部门树内标
        /// </summary>
        /// <param name="orgList"></param>
        /// <param name="postList"></param>
        /// <param name="orgId"></param>
        /// <returns></returns>
        private List<TreeNode> GetOrgAndPostTreeList(List<BsonDocument> orgList, List<BsonDocument> postList, int orgId, int maxLv)
        {
            BsonDocument curOrg = orgList.Where(t => t.Int("orgId") == orgId).FirstOrDefault(); //当前部门列表

            List<BsonDocument> subOrgList = orgList.Where(t => t.Int("nodePid") == orgId).ToList(); //子部门列表

            List<BsonDocument> subPostList = postList.Where(t => t.Int("orgId") == orgId).ToList(); //子岗位列表

            List<TreeNode> treeList = new List<TreeNode>();

            if (SysAppConfig.CustomerCode == CustomerCode.ZHHY.ToString())
            {
                subOrgList = subOrgList.OrderBy(t => t.Int("showOrder")).ToList();
            }
            else
            {
                subOrgList = subOrgList.OrderBy(t => t.Int("nodeOrder")).ToList();
            }
            foreach (var subNode in subOrgList)      //循环子部门列表,赋值
            {
                TreeNode node = new TreeNode();

                node.Id = subNode.Int("orgId");
                node.Name = subNode.String("name");
                node.Lv = curOrg.Int("nodeLevel") + 1;
                node.Pid = orgId;
                node.underTable = subNode.String("underTable");
                node.Param = "0_" + subNode.Int("orgId") + "_0";
                node.SubNodes = GetOrgAndPostTreeList(orgList, postList, node.Id, maxLv);    //获取子节点列表;
                node.IsLeaf = 0;

                treeList.Add(node);
            }

            if (maxLv > 0 && (curOrg.Int("nodeLevel") + 1) <= maxLv)
            {
                foreach (var subNode in subPostList.OrderBy(t => t.Int("nodeOrder")))      //循环子岗位列表,赋值
                {
                    TreeNode node = new TreeNode();

                    node.Id = subNode.Int("orgId");
                    node.Name = subNode.String("name");
                    node.Lv = curOrg.Int("nodeLevel") + 1;
                    node.Pid = orgId;
                    node.underTable = subNode.String("underTable");
                    node.Param = "1_" + orgId + "_" + subNode.Int("postId");
                    node.IsLeaf = 1;

                    treeList.Add(node);
                }
            }

            return treeList;
        }



        #region 选人操作
        /// <summary>
        /// 查找用户（通过部门id,部门岗位id,群组id,通用岗位id）
        /// </summary>
        /// <returns></returns>
        public JsonResult FindUsers()
        {
            List<Item> userList = new List<Item>();
            string type = PageReq.GetForm("type");
            int id = PageReq.GetFormInt("id");
            var sysUsers = new List<BsonDocument>();
            List<BsonDocument> roleUserList = new List<BsonDocument>();
            var biz = SysUserBll._(dataOp);
            switch (type)
            {
                case "org":
                    sysUsers = biz.FindUsersByOrgIdByView(id);
                    break;
                case "orgpost":
                    sysUsers = biz.FindUsersByOrgPostIdByView(id);
                    break;
                case "compost":
                    sysUsers = biz.FindUsersByCommonPostIdByView(id);
                    break;
                case "group":
                    sysUsers = biz.FindUsersByGroupIdView(id);
                    break;
                case "sysRole":
                    roleUserList = biz.FindSysRoleUsers(id);
                    break;
            }
            if (sysUsers == null && roleUserList == null) { return this.Json(null); }

            //修改排序规则 bts 2058
            if (sysUsers != null && sysUsers.Count() > 0)
            {
                foreach (var u in sysUsers.OrderBy(s => s.Text("name")).Where(u => u.Int("status") != 2))
                {
                    var itm = new Item();
                    itm.id = u.Int("userId");
                    itm.name = u.Text("name");
                    userList.Add(itm);
                }
            }
            else if (roleUserList != null && roleUserList.Count > 0)
            {
                foreach (var user in roleUserList.OrderBy(s => s.Text("name")).Where(u => u.Int("status") != 2))
                {
                    var itm = new Item();
                    itm.id = user.Int("userId");
                    //itm.Text("name") = user.Text("name");
                    itm.name = user.Text("name");
                    userList.Add(itm);
                }
            }
            userList = userList.OrderBy(i => i.name).ToList();
            return this.Json(userList, JsonRequestBehavior.AllowGet);
        }


        /// <summary>
        /// 根据id返回用户名
        /// </summary>
        /// <returns></returns>
        public JsonResult FindUsersNameById()
        {
            List<Item> userList = new List<Item>();
            int id = PageReq.GetFormInt("id");
            BsonDocument userinfo = null;
            userinfo = dataOp.FindOneByKeyVal("SysUser", "userId", id.ToString());
            if (userinfo == null || userinfo.Int("status") == 2) { return this.Json(null); }
            var itm = new Item();
            itm.id = userinfo.Int("userId");
            itm.name = userinfo.Text("name");
            userList.Add(itm);
            return this.Json(userList, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 查找用户
        /// </summary>
        /// <returns></returns>
        public JsonResult SearchUsers()
        {
            string key = PageReq.GetForm("key");
            var biz = SysUserBll._();
            List<BsonDocument> userList = biz.FindUsersBySearchByView(key).OrderBy(u => u.Text("name")).ToList();

            //bts 2058
            var sysUsers = biz.FindUsersBySearchByView(key).OrderBy(o => o.Text("name")).Select(r =>
                new
                {
                    id = r.Int("userId"),
                    name = r.Text("name"),
                    loginName = r.Text("loginName"),
                    sysProfId = r.Text("sysProfId")
                })
                    .ToList();

            return this.Json(sysUsers, JsonRequestBehavior.AllowGet);
        }

        #region 加于2010-1-5 用于构造新的组织架构树
        /// <summary>
        /// 新版构造组织架构树
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ActionResult newOrgTree(string id)
        {
            int orgId = 0;

            if (id == "")
            {
                id = PageReq.GetParam("id");
            }

            if (id != null && id != "0")
            {
                orgId = int.Parse(id.Split('_')[1]);
            }

            // string[] idArr = id.Split('_').ToArray();

            List<BsonDocument> orgList = new List<BsonDocument>();
            BsonDocument orgTemp = new BsonDocument();



            if (orgId == 0)
            {
                orgList = dataOp.FindAllByKeyVal("Organization", "nodeLevel", "1").ToList();
            }
            else
            {
                orgTemp = dataOp.FindOneByKeyVal("Organization", "orgId", orgId.ToString());
                orgList = dataOp.FindChildNodes("Organization", orgId.ToString()).ToList();
            }



            List<TreeNode> treeList = new List<TreeNode>();

            foreach (var org in orgList)
            {
                TreeNode node = new TreeNode();
                node.Id = org.Int("orgId");
                node.Lv = org.Int("nodeLevel");
                node.Name = org.Text("name");
                node.Pid = org.Int("nodePid");
                node.TreeType = "0";

                if ((org.ChildBsonList("OrgPost").Count() + dataOp.FindChildNodes("Organization", org.Text("orgId")).Count()) > 0)
                {
                    node.IsLeaf = 0;
                }
                else
                {
                    node.IsLeaf = 1;
                }

                #region 第一次显示两级
                if (orgId == 0)
                {
                    if (string.IsNullOrEmpty(PageReq.GetParam("orgId")) == false)
                    {
                        node.SubNodes = GetSubNode(org, 2, PageReq.GetParamInt("orgId"));
                    }
                    else
                    {
                        node.SubNodes = GetSubNode(org, 2);
                    }
                }
                #endregion

                treeList.Add(node);
            }

            if (orgId != 0)
            {
                List<BsonDocument> orgPostList = new List<BsonDocument>();

                orgPostList = orgTemp.ChildBsonList("OrgPost").ToList();


                if (orgPostList.Count > 0)
                {
                    foreach (var pos in orgPostList)
                    {
                        TreeNode node = new TreeNode();
                        node.Id = pos.Int("postId");
                        node.Lv = orgTemp.Int("nodeLevel") + 1;
                        node.Name = pos.Text("name");
                        node.Pid = pos.Int("orgId");
                        node.TreeType = "1";
                        node.IsLeaf = 1;

                        treeList.Add(node);
                    }
                }
            }

            return new DynamicXmlTree(treeList);
        }

        /// <summary>
        /// 取节点的子节点,为递归,level为控制展示级数,如:展示两级则level大于2返回空
        /// </summary>
        /// <param name="org"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        [NonAction]
        private List<TreeNode> GetSubNode(BsonDocument org, int level)
        {
            if (level > 2) return null;

            List<TreeNode> treeList = new List<TreeNode>();

            foreach (var o in dataOp.FindChildNodes("Organization", org.Text("orgId")))
            {
                TreeNode tempnode = new TreeNode();
                tempnode.Id = o.Int("orgId");
                tempnode.Lv = 1;
                tempnode.Name = o.Text("name");
                tempnode.Pid = o.Int("nodePid");
                tempnode.TreeType = "0";

                if ((o.ChildBsonList("OrgPost").Count() + dataOp.FindChildNodes("Organization", o.Text("orgId")).Count()) > 0)
                {
                    tempnode.IsLeaf = 0;
                }
                else
                {
                    tempnode.IsLeaf = 1;
                }

                tempnode.SubNodes = GetSubNode(o, level + 1);

                treeList.Add(tempnode);
            }


            foreach (var pos in org.ChildBsonList("OrgPost"))
            {
                TreeNode tempnode = new TreeNode();
                tempnode.Id = pos.Int("postId");
                tempnode.Lv = 1;
                tempnode.Name = pos.Text("name");
                tempnode.Pid = pos.Int("orgId");
                tempnode.TreeType = "1";
                tempnode.IsLeaf = 1;

                treeList.Add(tempnode);
            }


            return treeList;
        }
        /// <summary>
        /// 取节点的子节点,为递归,level为控制展示级数,如:展示两级则level大于2返回空
        /// </summary>
        /// <param name="org"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        [NonAction]
        private List<TreeNode> GetSubNode(BsonDocument org, int level, int subOrgId)
        {
            if (level > 2) return null;
            var orgId = org.Text("orgId");
            List<TreeNode> treeList = new List<TreeNode>();
            var orgList = dataOp.FindChildNodes("Organization", orgId).Where(o => o.Int("orgId") == subOrgId).ToList();
            foreach (var o in orgList)
            {
                TreeNode tempnode = new TreeNode();
                tempnode.Id = o.Int("orgId");
                tempnode.Lv = 1;
                tempnode.Name = o.Text("name");
                tempnode.Pid = o.Int("nodePid");
                tempnode.TreeType = "0";

                if ((o.ChildBsonList("OrgPost").Count() + dataOp.FindChildNodes("Organization", o.Text("orgId")).Count()) > 0)
                {
                    tempnode.IsLeaf = 0;
                }
                else
                {
                    tempnode.IsLeaf = 1;
                }

                tempnode.SubNodes = GetSubNode(o, level + 1);

                treeList.Add(tempnode);
            }

            foreach (var pos in org.ChildBsonList("OrgPost"))
            {
                TreeNode tempnode = new TreeNode();
                tempnode.Id = pos.Int("postId");
                tempnode.Lv = 1;
                tempnode.Name = pos.Text("name");
                tempnode.Pid = pos.Int("orgId");
                tempnode.TreeType = "1";
                tempnode.IsLeaf = 1;

                treeList.Add(tempnode);
            }


            return treeList;
        }


        #endregion

        public JsonResult PlanRelatedPersonnel(int? projId, int? type)
        {
            var json = new JsonResult();
            if (projId.HasValue)
            {
                var planList = dataOp.FindAllByKeyVal("XH_DesignManage_Plan", "projId", projId.ToString());
                var planIdList = planList.Select(c => c.Text("planId")).ToList();
                var taskList = dataOp.FindAllByKeyVal("XH_DesignManage_Task", "projId", projId.ToString());
                var taskIdList = taskList.Select(c => c.Text("taskId")).ToList();
                var curPlanManageList = dataOp.FindAllByKeyValList("XH_DesignManage_PlanManager", "planId", planIdList);
                var curTaskManageList = dataOp.FindAllByKeyValList("XH_DesignManage_TaskManager", "taskId", taskIdList);
                var planManageResult = curPlanManageList.Select(r => new { id = r.Int("userId"), name = r.SourceBsonField("userId", "name"), sysProfId = "", sysProfName = "" });
                var taskManageResult = curTaskManageList.Select(r => new { id = r.Int("userId"), name = r.SourceBsonField("userId", "name"), sysProfId = "", sysProfName = "" });
                json.Data = new
                {
                    Success = true,
                    Items = (planManageResult.Concat(taskManageResult)).Distinct().ToList()
                };
            }
            return Json(json, JsonRequestBehavior.AllowGet);
        }

        #endregion

        /// <summary>
        /// 获取系统专业
        /// </summary>
        /// <returns></returns>
        public JsonResult GetProfJson()
        {
            List<Item> ProfList = dataOp.FindAll("System_Professional").Select(s => new Item { id = s.Int("profId"), name = s.Text("name") }).ToList();
            return this.Json(ProfList, JsonRequestBehavior.AllowGet);
        }


        /// <summary>
        /// 获取部门下所有用户Json
        /// </summary>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public ActionResult GetOrgUserJson(string orgId, int? ps, int? cu)
        {
            int pageSize = (ps != null && ps.Value != 0) ? ps.Value : 20;
            int current = (cu != null && cu.Value != 0) ? cu.Value : 1;

            List<BsonDocument> allPostList = dataOp.FindAllByKeyVal("OrgPost", "orgId", orgId).ToList();    //所有岗位列表

            List<BsonDocument> allRelList = dataOp.FindAllByKeyValList("UserOrgPost", "postId", allPostList.Select(t => t.String("postId")).ToList()).ToList(); //所有用户关联

            List<string> allUserIdList = allRelList.Select(t => t.String("userId")).Distinct().ToList();    //所有用户Id列表   

            int allCount = allUserIdList.Count();

            List<BsonDocument> allUserList = dataOp.FindAllByKeyValList("SysUser", "userId", allUserIdList).OrderBy(t => t.Int("userId")).ToList();

            List<Hashtable> retList = new List<Hashtable>();

            if (pageSize != -1)
            {
                allUserList = allUserList.Skip((current - 1) * pageSize).Take(pageSize).ToList();
            }

            foreach (var tempUser in allUserList)
            {
                tempUser.Add("allCount", allCount);
                tempUser.Remove("_id");

                retList.Add(tempUser.ToHashtable());
            }

            return this.Json(retList, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 获取岗位下所有用户Json
        /// </summary>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public ActionResult GetPostUserJson(string postId, int? ps, int? cu)
        {
            int pageSize = (ps != null && ps.Value != 0) ? ps.Value : 20;
            int current = (cu != null && cu.Value != 0) ? cu.Value : 1;

            List<BsonDocument> allRelList = dataOp.FindAllByKeyVal("UserOrgPost", "postId", postId).ToList(); //所有用户关联

            List<string> allUserIdList = allRelList.Select(t => t.String("userId")).Distinct().ToList();    //所有用户Id列表   

            int allCount = allUserIdList.Count();

            List<BsonDocument> allUserList = dataOp.FindAllByKeyValList("SysUser", "userId", allUserIdList).OrderBy(t => t.Int("userId")).ToList();

            List<Hashtable> retList = new List<Hashtable>();

            if (pageSize != -1)
            {
                allUserList = allUserList.Skip((current - 1) * pageSize).Take(pageSize).ToList();
            }

            foreach (var tempUser in allUserList)
            {
                tempUser.Add("allCount", allCount);
                tempUser.Remove("_id");

                retList.Add(tempUser.ToHashtable());
            }

            return this.Json(retList, JsonRequestBehavior.AllowGet);
        }


        /// <summary>
        /// 获取部门树的XML列表  zpx
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="queryStr"></param>
        /// <returns></returns>
        public ActionResult GetOrgTreeXML()
        {
            int orgId = PageReq.GetFormInt("id") != 0 ? PageReq.GetFormInt("id") : (PageReq.GetParamInt("id") != 0 ? PageReq.GetParamInt("id") : 0);   //父级OrgId,如果传入,则展示子级,不包括父级
            int lv = PageReq.GetFormInt("lv") != 0 ? PageReq.GetFormInt("lv") : (PageReq.GetParamInt("lv") != 0 ? PageReq.GetParamInt("lv") : 0);   //展示层级,0为不限层级

            BsonDocument curOrg = dataOp.FindOneByKeyVal("Organization", "orgId", orgId.ToString());    //当前org

            int maxLv = lv == 0 ? 0 : curOrg.Int("nodeLevel") + lv;                   //展示的最大层级,0 表示不限层级

            List<BsonDocument> orgList = new List<BsonDocument>();                  //所有展示的部门信息

            if (maxLv > 0)
            {
                orgList = dataOp.FindAllByQuery("Organization", Query.EQ("state", "0")).Where(t => t.Int("nodeLevel") <= maxLv).ToList();   //取出所有最大层级的部门
            }
            else
            {
                orgList = dataOp.FindAllByQuery("Organization", Query.EQ("state", "0")).ToList();
            }

            List<string> orgIdList = orgList.Select(t => t.String("orgId")).ToList();

            List<TreeNode> treeList = this.GetOrgTreeList(orgList, orgId, maxLv);

            return new XmlTree(treeList);
        }

        /// <summary>
        /// 递归获取对应的部门树内标  zpx
        /// </summary>
        /// <param name="orgList"></param>
        /// <param name="postList"></param>
        /// <param name="orgId"></param>
        /// <returns></returns>
        private List<TreeNode> GetOrgTreeList(List<BsonDocument> orgList, int orgId, int maxLv)
        {
            BsonDocument curOrg = orgList.Where(t => t.Int("orgId") == orgId).FirstOrDefault(); //当前部门列表

            List<BsonDocument> subOrgList = orgList.Where(t => t.Int("nodePid") == orgId).ToList(); //子部门列表

            List<TreeNode> treeList = new List<TreeNode>();

            foreach (var subNode in subOrgList.OrderBy(t => t.Int("nodeOrder")))      //循环子部门列表,赋值
            {

                TreeNode node = new TreeNode();
                List<BsonDocument> subOrgList1 = orgList.Where(t => t.Int("nodePid") == subNode.Int("orgId")).ToList(); //判断是否具有子部门
                node.Id = subNode.Int("orgId");
                node.Name = subNode.String("name");
                node.Lv = curOrg.Int("nodeLevel") + 1;
                node.Pid = orgId;
                node.underTable = subNode.String("underTable");
                node.Param = "0_" + subNode.Int("orgId") + "_0";
                node.SubNodes = GetOrgTreeList(orgList, node.Id, maxLv);    //获取子节点列表;
                node.IsLeaf = subOrgList1.Count() >= 1 ? 0 : 1;

                treeList.Add(node);
            }

            return treeList;
        }

        /// <summary>
        /// 保存角色权限
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="modulId"></param>
        /// <param name="roleId"></param>
        /// <param name="code"></param>
        /// <param name="dataObjId"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult SaveSysRoleRight(string tbName, string modulId, string roleId, string code, string dataObjId)
        {
            IMongoQuery query = Query.And(Query.EQ("modulId", modulId), Query.EQ("roleId", roleId), Query.EQ("code", code));
            var old = dataOp.FindOneByQuery(tbName, query);
            var roleRight = new BsonDocument { { "modulId", modulId }, { "roleId", roleId }, { "code", code }, { "dataObjId", dataObjId } };
            InvokeResult result = null;
            if (old != null)
            {
                result = dataOp.Update(tbName, query, roleRight);
            }
            else
            {
                result = dataOp.Insert(tbName, roleRight);
            }
            return Json(ConvertToPageJson(result));
        }

        /// <summary>
        /// 删除角色权限
        /// </summary>
        /// <param name="modulId"></param>
        /// <param name="roleId"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult DelSysRoleRight(string modulId, string roleId, string code)
        {
            IMongoQuery query = Query.And(Query.EQ("modulId", modulId), Query.EQ("roleId", roleId), Query.EQ("code", code));
            InvokeResult result = dataOp.Delete("SysRoleRight", query);
            return Json(ConvertToPageJson(result));
        }





    }
}