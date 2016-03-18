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
using System.Collections;
using System.Web.Script.Serialization;
using MongoDB.Driver;
using MongoDB.Bson.IO;

namespace Yinhe.ProcessingCenter
{
    /// <summary>
    /// 项目管理后台处理基类
    /// </summary>
    public partial class ControllerBase : Controller
    {
        /// <summary>
        /// 获取类型对象的父级JSON列表
        /// </summary>
        /// <param name="objId"></param>
        /// <returns></returns>
        public ActionResult GetParentObjJson(int objId)
        {
            List<BsonDocument> parentList = dataOp.FindParentNodeList("ProjTypeObj", objId.ToString()); //所有父亲类型

            List<string> attrIdList = parentList.Select(t => t.String("attrId")).Distinct().ToList();     //所有父亲类型对应属性

            List<BsonDocument> attrList = dataOp.FindAllByKeyValList("ProjAttribute", "arrtId", attrIdList).ToList();   //所有属性

            List<object> retList = new List<object>();

            foreach (var temp in parentList.OrderBy(t => t.String("nodeKey")))
            {
                BsonDocument tempAttr = attrList.Where(t => t.Int("attrId") == temp.Int("attrId")).FirstOrDefault();

                string attrKey = tempAttr != null ? tempAttr.String("dataKey") : "";

                retList.Add(new
                {
                    objId = temp.Int("objId"),
                    name = temp.Int("name"),
                    attrKey = attrKey
                });
            }

            return this.Json(retList, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 保存属性对象
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SaveProjAttrObj(FormCollection saveForm)
        {
            string tbName = "ProjAttrObj";
            string queryStr = saveForm["queryStr"] != null ? saveForm["queryStr"] : "";
            string dataStr = saveForm["dataStr"] != null ? saveForm["dataStr"] : "";

            if (dataStr.Trim() == "")
            {
                foreach (var tempKey in saveForm.AllKeys)
                {
                    if (tempKey == "tbName" || tempKey == "queryStr" || tempKey.Contains("attrRels")) continue;

                    dataStr += string.Format("{0}={1}&", tempKey, saveForm[tempKey]);
                }
            }

            InvokeResult result = dataOp.Save(tbName, queryStr, dataStr);    //保存

            if (result.Status == Status.Successful)         //保存属性关联
            {
                string attrObjId = result.BsonInfo.String("attrObjId");

                List<StorageData> saveList = new List<StorageData>();

                #region 添加和更新已有关联
                string attrRels = saveForm["attrRels"] != null ? saveForm["attrRels"] : "";   //变量: attrRels , 格式:attrId,isSearch,fillType|Y|attrId,isSearch,fillType

                List<string> attrRelList = attrRels.Split(new string[] { "|Y|" }, StringSplitOptions.RemoveEmptyEntries).ToList();

                List<BsonDocument> oldRelList = dataOp.FindAllByKeyVal("ObjAttrRelation", "attrObjId", attrObjId).ToList();

                List<string> newAttrIdList = new List<string>();

                foreach (var tempAttr in attrRelList)       //循环新的基类关联
                {
                    var attrId = tempAttr.Split(new string[] { "," }, StringSplitOptions.None)[0];
                    var isSearch = tempAttr.Split(new string[] { "," }, StringSplitOptions.None)[1];
                    var fillType = tempAttr.Split(new string[] { "," }, StringSplitOptions.None)[2];

                    BsonDocument tempBson = new BsonDocument();
                    tempBson.Add("attrObjId", attrObjId);
                    tempBson.Add("attrId", attrId);
                    tempBson.Add("isSearch", isSearch);
                    tempBson.Add("fillType", fillType);

                    var tempRel = oldRelList.Where(t => t.String("attrId") == attrId).FirstOrDefault();

                    if (tempRel == null)        //如果不存在,则添加
                    {
                        StorageData relData = new StorageData();

                        relData.Name = "ObjAttrRelation";
                        relData.Document = tempBson;
                        relData.Type = StorageType.Insert;

                        saveList.Add(relData);
                    }
                    else                        //如果存在则更新
                    {
                        StorageData relData = new StorageData();

                        relData.Name = "ObjAttrRelation";
                        relData.Document = tempBson;
                        relData.Type = StorageType.Update;
                        relData.Query = Query.EQ("relId", tempRel.String("relId"));

                        saveList.Add(relData);
                    }

                    newAttrIdList.Add(attrId);
                }
                #endregion

                #region 删除已删除关联
                foreach (var oldRel in oldRelList)  //循环旧的基类关联
                {
                    if (newAttrIdList.Contains(oldRel.String("attrId")) == false) //如果不存在,则删除
                    {
                        StorageData relData = new StorageData();

                        relData.Name = "ObjAttrRelation";
                        relData.Type = StorageType.Delete;
                        relData.Query = Query.EQ("relId", oldRel.String("relId"));

                        saveList.Add(relData);
                    }
                }
                #endregion

                result = dataOp.BatchSaveStorageData(saveList);
            }

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        /// <summary>
        /// 保存属性对象
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SaveAttrType(FormCollection saveForm)
        {
            string tbName = "AttrType";
            string queryStr = saveForm["queryStr"] != null ? saveForm["queryStr"] : "";
            string dataStr = saveForm["dataStr"] != null ? saveForm["dataStr"] : "";

            if (dataStr.Trim() == "")
            {
                foreach (var tempKey in saveForm.AllKeys)
                {
                    if (tempKey == "tbName" || tempKey == "queryStr" || tempKey.Contains("attrRels")) continue;

                    dataStr += string.Format("{0}={1}&", tempKey, saveForm[tempKey]);
                }
            }

            InvokeResult result = dataOp.Save(tbName, queryStr, dataStr);    //保存

            if (result.Status == Status.Successful)         //保存属性关联
            {
                string typeId = result.BsonInfo.String("typeId");

                List<StorageData> saveList = new List<StorageData>();

                #region 添加和更新已有关联
                string attrRels = saveForm["attrRels"] != null ? saveForm["attrRels"] : "";   //变量: attrRels , 格式:attrId,groupKey,fillType|Y|attrId,groupKey,fillType

                List<string> attrRelList = attrRels.Split(new string[] { "|Y|" }, StringSplitOptions.RemoveEmptyEntries).ToList();

                List<BsonDocument> oldRelList = dataOp.FindAllByKeyVal("TypeAttrRelation", "typeId", typeId).ToList();

                List<string> newAttrIdList = new List<string>();

                foreach (var tempAttr in attrRelList)       //循环新的基类关联
                {
                    var attrId = tempAttr.Split(new string[] { "," }, StringSplitOptions.None)[0];
                    var groupKey = tempAttr.Split(new string[] { "," }, StringSplitOptions.None)[1];
                    var fillType = tempAttr.Split(new string[] { "," }, StringSplitOptions.None)[2];

                    BsonDocument tempBson = new BsonDocument();
                    tempBson.Add("typeId", typeId);
                    tempBson.Add("attrId", attrId);
                    tempBson.Add("groupKey", groupKey);
                    tempBson.Add("fillType", fillType);

                    var tempRel = oldRelList.Where(t => t.String("attrId") == attrId).FirstOrDefault();

                    if (tempRel == null)        //如果不存在,则添加
                    {
                        StorageData relData = new StorageData();

                        relData.Name = "TypeAttrRelation";
                        relData.Document = tempBson;
                        relData.Type = StorageType.Insert;

                        saveList.Add(relData);
                    }
                    else                        //如果存在则更新
                    {
                        StorageData relData = new StorageData();

                        relData.Name = "TypeAttrRelation";
                        relData.Document = tempBson;
                        relData.Type = StorageType.Update;
                        relData.Query = Query.EQ("relId", tempRel.String("relId"));

                        saveList.Add(relData);
                    }

                    newAttrIdList.Add(attrId);
                }
                #endregion

                #region 删除已删除关联
                foreach (var oldRel in oldRelList)  //循环旧的基类关联
                {
                    if (newAttrIdList.Contains(oldRel.String("attrId")) == false) //如果不存在,则删除
                    {
                        StorageData relData = new StorageData();

                        relData.Name = "TypeAttrRelation";
                        relData.Type = StorageType.Delete;
                        relData.Query = Query.EQ("relId", oldRel.String("relId"));

                        saveList.Add(relData);
                    }
                }
                #endregion

                result = dataOp.BatchSaveStorageData(saveList);
            }

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        /// <summary>
        /// 获取项目节点树形XML
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="curNodeId"></param>
        /// <param name="typeObjId"></param>
        /// <param name="itself"></param>
        /// <returns></returns>
        public ActionResult GetProjNodeTreeXML(string tbName, string curNodeId, int typeObjId, int itself)
        {
            TableRule tableEntity = new TableRule(tbName);    //获取表结构

            string primaryKey = tableEntity.ColumnRules.Where(t => t.IsPrimary == true).FirstOrDefault().Name;  //寻找默认主键

            List<BsonDocument> allNodeList = new List<BsonDocument>();

            if (curNodeId.Trim() != "0" && curNodeId != "")
            {
                BsonDocument curNode = dataOp.FindOneByKeyVal(tbName, primaryKey, curNodeId);

                if (typeObjId != 0)
                {
                    allNodeList = dataOp.FindChildNodes(tbName, curNodeId).Where(t => t.Int("typeObjId") == typeObjId).ToList();
                }
                else
                {
                    allNodeList = dataOp.FindChildNodes(tbName, curNodeId).ToList();
                }

                if (itself == 1)
                {
                    allNodeList.Add(curNode);
                }
            }
            else
            {
                if (typeObjId != 0)
                {
                    allNodeList = dataOp.FindAll(tbName).Where(t => t.Int("typeObjId") == typeObjId).ToList();
                }
                else
                {
                    allNodeList = dataOp.FindAll(tbName).ToList();
                }
            }

            List<TreeNode> treeList = TreeHelper.GetSingleTreeList(allNodeList);

            return new XmlTree(treeList);
        }

        /// <summary>
        /// 根据类型内标获取节点树,不包含本身
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="curNodeId"></param>
        /// <param name="typeObjIds"></param>
        /// <param name="maxLv"></param>
        /// <returns></returns>
        public ActionResult GetProjNodeTreeXMLByType(string tbName, string curNodeId, string typeObjIds, int maxLv)
        {
            TableRule tableEntity = new TableRule(tbName);    //获取表结构

            string primaryKey = tableEntity.ColumnRules.Where(t => t.IsPrimary == true).FirstOrDefault().Name;  //寻找默认主键

            List<int> typeObjIdList = typeObjIds.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).Select(t => int.Parse(t)).ToList();    //所有类型

            List<BsonDocument> allNodeList = new List<BsonDocument>();

            if (curNodeId.Trim() != "0" && curNodeId != "") //有传入当前节点,则输出当前节点的子节点
            {
                BsonDocument curNode = dataOp.FindOneByKeyVal(tbName, primaryKey, curNodeId);   //当前节点

                if (typeObjIdList.Count > 0)
                {
                    allNodeList = dataOp.FindChildNodes(tbName, curNodeId).Where(t => typeObjIdList.Contains(t.Int("typeObjId")) && (t.Int("nodeLevel") <= (curNode.Int("nodeLevel") + maxLv))).ToList();
                }
                else
                {
                    allNodeList = dataOp.FindChildNodes(tbName, curNodeId).ToList();
                }
            }
            else
            {
                if (typeObjIdList.Count > 0)
                {
                    allNodeList = dataOp.FindAll(tbName).Where(t => typeObjIdList.Contains(t.Int("typeObjId")) && t.Int("nodeLevel") <= maxLv).ToList();
                }
                else
                {
                    allNodeList = dataOp.FindAll(tbName).ToList();
                }
            }

            List<TreeNode> treeList = TreeHelper.GetSingleTreeList(allNodeList);

            return new XmlTree(treeList);
        }

        /// <summary>
        /// 逐级加载节点树
        /// </summary>
        /// <param name="tbName"></param>
        /// <returns></returns>
        public ActionResult GetProjNodeTreeXMLByLevel(string tbName)
        {
            int id = PageReq.GetFormInt("id") != 0 ? PageReq.GetFormInt("id") : (PageReq.GetParamInt("id") != 0 ? PageReq.GetParamInt("id") : 0);   //父级OrgId,如果传入,则展示子级,不包括父级

            TableRule tableEntity = new TableRule(tbName);    //获取表结构

            string primaryKey = tableEntity.GetPrimaryKey();  //寻找默认主键

            BsonDocument curNode = dataOp.FindOneByKeyVal(tbName, primaryKey, id.ToString());   //当前节点

            List<BsonDocument> allNodeList = dataOp.FindChildNodes(tbName, id.ToString()).ToList();

            List<BsonDocument> subNodeList = allNodeList.Where(t => t.Int("nodePid") == id).ToList();

            List<TreeNode> treeList = new List<TreeNode>();

            foreach (var subNode in subNodeList.OrderBy(t => t.Int("nodeOrder")))      //循环子部门列表,赋值
            {
                TreeNode node = new TreeNode();

                node.Id = subNode.Int(primaryKey);
                node.Name = subNode.String("name");
                node.Lv = curNode.Int("nodeLevel") + 1;
                node.Pid = id;
                node.underTable = subNode.String("underTable");
                node.Param = "0_" + subNode.Int(primaryKey) + "_0";

                if (allNodeList.Where(t => t.Int("nodePid") == subNode.Int(primaryKey)).Count() > 0)
                {
                    node.IsLeaf = 0;
                }
                else
                {
                    node.IsLeaf = 1;
                }

                treeList.Add(node);
            }
            return new XmlTree(treeList);
        }

        /// <summary>
        /// 获取属性类型下所有属性
        /// </summary>
        /// <param name="attrObjId"></param>
        /// <returns></returns>
        public ActionResult GetAllAttrJsonByObjId(string attrObjId)
        {
            List<BsonDocument> attrRelList = dataOp.FindAllByKeyVal("ObjAttrRelation", "attrObjId", attrObjId).ToList();

            List<string> attrIdList = attrRelList.Select(t => t.String("attrId")).ToList();

            List<BsonDocument> attrList = dataOp.FindAllByKeyValList("ProjAttribute", "attrId", attrIdList).ToList();

            List<Hashtable> retList = new List<Hashtable>();

            foreach (var tempAttr in attrList)
            {
                retList.Add(tempAttr.ToHashtable());
            }

            return this.Json(retList, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 获取用户对应节点的权限
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="nodeId"></param>
        /// <returns>字典key为对应节点Id,字典val的List为对应的权限(1:查看,2:编辑)</returns>
        public Dictionary<int, List<int>> GetUserNodePurviewHD(int userId)
        {
            Dictionary<int, List<int>> nodePurviewDic = new Dictionary<int, List<int>>();   //用户节点权限

            //if (CacheHelper.GetCache("nodePurviewDic") != null)  //读取缓存中的权限列表,如有,则直接获取,如没有,则重新获取,并存入缓存
            //{
            //    nodePurviewDic = CacheHelper.GetCache("nodePurviewDic") as Dictionary<int, List<int>>;
            //}
            //else
            //{
            BsonDocument user = dataOp.FindOneByKeyVal("SysUser", "userId", userId.ToString());         //当前用户

            List<BsonDocument> userRoleList = dataOp.FindAllByKeyVal("SysRoleUser", "userId", userId.ToString()).ToList(); //当前用户的所有角色

            List<BsonDocument> nodeList = dataOp.FindAll("ProjectNode").ToList();   //所有节点列表

            List<BsonDocument> purviewList = dataOp.FindAll("ProjPurview").ToList();    //所有权限关联

            foreach (var tempNode in nodeList.OrderBy(t => t.String("nodeKey")))      //获取每个节点的权限
            {
                List<int> tempList = new List<int>();

                if (user.Int("type") == 1)
                {
                    tempList = new List<int>() { 0, 1, 2 };
                }
                else
                {
                    tempList = new List<int>() { 0 };

                    #region 当前节点拥有的权限
                    BsonDocument viewPurview = purviewList.Where(t => t.Int("nodeId") == tempNode.Int("nodeId") && t.Int("purview") == 1).FirstOrDefault();    //查看权限
                    string viewUsers = viewPurview != null ? viewPurview.String("userIds") : "";
                    string viewRoles = viewPurview != null ? viewPurview.String("roleIds") : "";

                    if (viewUsers.Contains(userId + ",")) tempList.Add(1);

                    BsonDocument editPurview = purviewList.Where(t => t.Int("nodeId") == tempNode.Int("nodeId") && t.Int("purview") == 2).FirstOrDefault();    //编辑权限
                    string editUsers = editPurview != null ? editPurview.String("userIds") : "";
                    string editRoles = editPurview != null ? editPurview.String("roleIds") : "";

                    if (editUsers.Contains(userId + ",")) tempList.Add(2);

                    foreach (var tempRoleId in userRoleList.Select(t => t.Int("roleId")))
                    {
                        if (viewRoles.Contains(tempRoleId + ",")) tempList.Add(1);
                        if (editRoles.Contains(tempRoleId + ",")) tempList.Add(2);
                    }
                    #endregion

                    #region 其父节点拥有权限
                    if (nodePurviewDic.ContainsKey(tempNode.Int("nodePid")))
                    {
                        tempList.AddRange(nodePurviewDic[tempNode.Int("nodePid")]);
                    }
                    #endregion
                }

                nodePurviewDic.Add(tempNode.Int("nodeId"), tempList.Distinct().ToList());
            }

            CacheHelper.SetCache("nodePurviewDic", nodePurviewDic, null, DateTime.Now.AddMinutes(30));
            //}

            return nodePurviewDic;
        }

        /// <summary>
        /// 将项目节点导出至模板
        /// </summary>
        /// <param name="curNodeId"></param>
        /// <param name="typeObjIds"></param>
        /// <returns></returns>
        public ActionResult ProjNodeExportTemplate(int pid, string typeObjIds)
        {
            string tbName = "ProjectNode";

            BsonDocument pidNode = dataOp.FindOneByKeyVal(tbName, "nodeId", pid.ToString());

            #region 获取所有节点
            List<BsonDocument> allNodeList = new List<BsonDocument>();

            List<int> typeObjIdList = typeObjIds.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).Select(t => int.Parse(t)).ToList(); //模板中的类型列表

            MongoCursor<BsonDocument> query = null;

            if (pid > 0)
            {
                query = dataOp.FindChildNodes(tbName, pid.ToString());
            }
            else
            {
                query = dataOp.FindAll(tbName);
            }

            if (typeObjIdList.Count > 0)
            {
                allNodeList = query.Where(t => typeObjIdList.Contains(t.Int("typeObjId"))).ToList();
            }
            else
            {
                allNodeList = query.ToList();
            }
            #endregion

            #region 获取所有节点对应的模板BSON
            StringBuilder saveContent = new StringBuilder();

            Dictionary<int, int> corDic = new Dictionary<int, int>();

            int index = 0;
            List<string> noNeedColumn = new List<string>() { "_id", "nodeId", "nodePid", "nodeLevel", "nodeOrder", "nodeKey", "createDate", "updateDate", "createUserId", "updateUserId", "underTable" };

            foreach (var tempNode in allNodeList.OrderBy(t => t.String("nodeKey")))
            {
                index++;
                corDic.Add(tempNode.Int("nodeId"), index);

                BsonDocument tempBson = new BsonDocument();

                tempBson.Add("id", index);

                if (corDic.ContainsKey(tempNode.Int("nodePid")))
                {
                    tempBson.Add("pid", corDic[tempNode.Int("nodePid")].ToString());
                }
                else if (tempNode.Int("nodePid") == pid) tempBson.Add("pid", "0");
                else continue;

                foreach (var tempElement in tempNode.Elements)
                {
                    if (noNeedColumn.Contains(tempElement.Name)) continue;

                    tempBson.Add(tempElement.Name, tempElement.Value);
                }

                saveContent.Append(tempBson.ToString());
            }
            #endregion

            #region 保存至模板库
            BsonDocument template = new BsonDocument();

            template.Add("sourceId", pid.ToString());
            template.Add("sourctType", typeObjIds);
            template.Add("name", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + pidNode.String("name") + "项目的成果目录模板");
            template.Add("content", saveContent.ToString());

            InvokeResult result = dataOp.Insert("ProjNodeTemplate", template);
            #endregion

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        /// <summary>
        /// 项目节点由模板中导入
        /// </summary>
        /// <param name="curNodeId"></param>
        /// <param name="typeObjIds"></param>
        /// <returns></returns>
        public ActionResult ProjNodeImportTemplate(int templateId, int pid)
        {
            string tbName = "ProjectNode";

            #region 获取要导入的数据
            BsonDocument template = dataOp.FindOneByKeyVal("ProjNodeTemplate", "templateId", templateId.ToString());

            List<BsonDocument> allDataList = new List<BsonDocument>();     //所有要导入的数据

            BsonReader bsonReader = BsonReader.Create(template.String("content"));

            while (bsonReader.CurrentBsonType != BsonType.EndOfDocument)
            {
                BsonDocument tempBson = BsonDocument.ReadFrom(bsonReader);

                allDataList.Add(tempBson);
            }
            #endregion

            #region 导入数据
            InvokeResult result = new InvokeResult();

            Dictionary<int, int> corDic = new Dictionary<int, int>();
            corDic.Add(0, pid);

            try
            {
                using (TransactionScope trans = new TransactionScope())
                {
                    foreach (var tempBson in allDataList)
                    {
                        int tempId = tempBson.Int("id");
                        int tempPid = tempBson.Int("pid");

                        if (corDic.ContainsKey(tempPid))
                        {
                            tempBson.Add("nodePid", corDic[tempPid].ToString());
                            tempBson.Remove("id");
                            tempBson.Remove("pid");

                            InvokeResult tempRet = dataOp.Insert(tbName, tempBson);
                            if (tempRet.Status == Status.Failed) throw new Exception(tempRet.Message);

                            corDic.Add(tempId, tempRet.BsonInfo.Int("nodeId"));
                        }
                    }
                    trans.Complete();
                }

                result.Status = Status.Successful;
            }
            catch (Exception e)
            {
                result.Status = Status.Failed;
                result.Message = e.Message;
            }

            #endregion

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        /// <summary>
        /// 保存节点指标版本
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        public ActionResult ProjNodeIndexToVersion(int nodeId, string queryStr, string dataStr)
        {
            string tbName = "NodeIndexVersion";

            List<BsonDocument> valueList = dataOp.FindAllByKeyVal("NodeIndexValue", "nodeId", nodeId.ToString()).ToList(); //所有值记录

            #region 获取所有值对应BSON
            StringBuilder content = new StringBuilder();

            List<string> noNeedColumn = new List<string>() { "_id", "nodeId", "nodePid", "nodeLevel", "nodeOrder", "nodeKey", "createDate", "updateDate", "createUserId", "updateUserId", "underTable" };

            foreach (var tempNode in valueList)
            {
                BsonDocument tempBson = new BsonDocument();

                foreach (var tempElement in tempNode.Elements)
                {
                    if (noNeedColumn.Contains(tempElement.Name)) continue;

                    tempBson.Add(tempElement.Name, tempElement.Value);
                }

                content.Append(tempBson.ToString());
            }
            #endregion

            var query = TypeConvert.NativeQueryToQuery(queryStr); //定位

            BsonDocument version = TypeConvert.ParamStrToBsonDocument(dataStr);
            version.Add("nodeId", nodeId.ToString());
            version.Add("content", content.ToString());

            InvokeResult result = dataOp.Save(tbName, query, version);

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        /// <summary>
        /// 设置文档标签
        /// </summary>
        /// <param name="fileIds"></param>
        /// <param name="tags"></param>
        /// <returns></returns>
        public ActionResult SetProjNodeFileTag(string fileIds, string tags)
        {
            string tbName = "ProjFileTags";

            InvokeResult result = new InvokeResult();

            List<string> fileIdList = fileIds.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();

            List<string> tagList = tags.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries).ToList();

            List<StorageData> saveList = new List<StorageData>();

            foreach (var tempFileId in fileIdList)
            {
                StorageData tempSave = new StorageData();

                tempSave.Name = "FileLibrary";
                tempSave.Query = Query.EQ("fileId", tempFileId);
                tempSave.Document = new BsonDocument().Add("tags", tags.ToString());
                tempSave.Type = StorageType.Update;

                saveList.Add(tempSave);
            }

            foreach (var tempTag in tagList)
            {
                BsonDocument oldTag = dataOp.FindOneByQuery(tbName, Query.EQ("name", tempTag));

                if (oldTag == null)
                {
                    StorageData tempSave = new StorageData();

                    tempSave.Name = tbName;
                    tempSave.Document = new BsonDocument().Add("name", tempTag)
                                                          .Add("fileIds", fileIds);
                    tempSave.Type = StorageType.Insert;

                    saveList.Add(tempSave);
                }
                else
                {
                    StorageData tempSave = new StorageData();

                    tempSave.Name = tbName;
                    tempSave.Query = Query.EQ("tagId", oldTag.String("tagId"));
                    tempSave.Document = new BsonDocument().Add("fileIds", oldTag.String("fileIds").TrimEnd(',') + "," + fileIds);
                    tempSave.Type = StorageType.Update;

                    saveList.Add(tempSave);
                }
            }

            result = dataOp.BatchSaveStorageData(saveList);

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        public ActionResult ArchiveProjFile(int nodeId, string guid2d)
        {
            InvokeResult result = new InvokeResult();

            BsonDocument oldFile = dataOp.FindOneByKeyVal("FileLibrary", "guid2d", guid2d); //找到需要归档的文档

            if (oldFile != null)
            {
                List<string> noNeedField = new List<string> { "_id", "fileId", "createDate", "updateDate", "createUserId", "updateUserId", "underTable", "order" };

                BsonDocument newFile = new BsonDocument();

                foreach (var tempEle in oldFile.Elements)
                {
                    if (noNeedField.Contains(tempEle.Name)) continue;

                    newFile.Add(tempEle.Name, tempEle.Value.ToString());
                }

                result = dataOp.Insert("FileLibrary", newFile);

                if (result.Status == Status.Successful)
                {
                    BsonDocument fileRle = new BsonDocument();
                    fileRle.Add("fileId", result.BsonInfo.Text("fileId"));
                    fileRle.Add("fileObjId", 54);
                    fileRle.Add("version", result.BsonInfo.Text("version"));
                    fileRle.Add("tableName", "ProjectNode");
                    fileRle.Add("keyName", "nodeId");
                    fileRle.Add("keyValue", nodeId.ToString());
                    fileRle.Add("uploadType", "0");
                    fileRle.Add("isPreDefine", "False");
                    fileRle.Add("isCover", "False");

                    result = dataOp.Insert("FileRelation", fileRle);
                }
            }
            else
            {
                result.Status = Status.Failed;
                result.Message = "未找到对应文档";
            }

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        public ActionResult SaveProjFiling(string guid2d)
        {
            InvokeResult result = new InvokeResult();

            BsonDocument bson = dataOp.FindOneByKeyVal("FileLibVersion", "guid2d", guid2d); //找到对应文件版本

            BsonDocument file = dataOp.FindOneByKeyVal("FileLibrary", "fileId", bson.String("fileId")); //找到文档

            if (bson != null && file != null)
            {
                List<BsonDocument> filinglist = dataOp.FindAllByKeyVal("FileArchive", "fileId", bson.String("fileId")).ToList();    //找到该文件的所有存档记录

                List<StorageData> saveList = new List<StorageData>();

                StorageData archiveData = new StorageData();    //新增归档记录

                archiveData.Name = "FileArchive";
                archiveData.Type = StorageType.Insert;
                archiveData.Document = new BsonDocument {
                    {"fileId",bson.String("fileId")},
                    {"fileVerId",bson.String("fileVerId")},
                    {"version",bson.String("version")},
                    {"archiveNum",(filinglist.Count() +1).ToString()}
                };

                saveList.Add(archiveData);

                StorageData fileData = new StorageData();   //改变文档中的归档号

                fileData.Name = "FileLibrary";
                fileData.Type = StorageType.Update;
                fileData.Query = Query.EQ("fileId", bson.String("fileId"));
                fileData.Document = new BsonDocument {
                    {"archiveNum",(filinglist.Count() +1).ToString()}
                };

                saveList.Add(fileData);

                result = dataOp.BatchSaveStorageData(saveList);
            }
            else
            {
                result.Status = Status.Failed;
                result.Message = "未找到对应文档";
            }

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }
    }
}
