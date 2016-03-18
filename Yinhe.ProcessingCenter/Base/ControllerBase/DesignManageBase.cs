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
using Yinhe.ProcessingCenter.BusinessFlow;
using MongoDB.Bson.IO;
using System.Collections;
using System.Xml;
using org.in2bits.MyXls;
using org.in2bits.MyOle2;
using Yinhoo.Utilities.Util;
using Yinhe.ProcessingCenter.Business.PolicyDecision;
using Yinhe.ProcessingCenter.Business.DesignManage;
using Yinhe.ProcessingCenter.Permissions;
using System.Web.Script.Serialization;
///<summary>
///后台处理中心
///</summary>
namespace Yinhe.ProcessingCenter
{
    /// <summary>
    /// 设计后台管理基类
    /// </summary>
    public partial class ControllerBase : Controller
    {

        [HttpPost]
        public JsonResult GetTreeInfos(string projId, string opCodes)
        {
            var valueTreeList = dataOp.FindAll("XH_ProductDev_ValueTree");
            Authentication auth = new Authentication();
            List<Hashtable> retList = new List<Hashtable>();
            foreach (var item in valueTreeList.OrderBy(s => s.Int("order")))
            {
                List<string> codes = new List<string>();
                foreach (var opCode in opCodes.SplitParam(","))
                {
                    string treeCode = item.String("code");
                    codes.Add(string.Format("PC{0}_{1}", treeCode, opCode));
                }

                if (auth.CheckProjectRight(AreaRoleType.Project, projId, codes.ToArray()))
                {
                    item.TryAdd("hasRight", true.ToString());
                }
                else
                {
                    item.TryAdd("hasRight", false.ToString());
                }
                item.Remove("_id");
                retList.Add(item.ToHashtable());
            }


            return Json(retList);
        }


        #region 项目相关
        /// <summary>
        /// 旭辉保存项目信息
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SaveProjectInfo(FormCollection saveForm)
        {
            string tbName = saveForm["tbName"] != null ? saveForm["tbName"] : "";
            string queryStr = saveForm["queryStr"] != null ? saveForm["queryStr"] : "";
            string dataStr = saveForm["dataStr"] != null ? saveForm["dataStr"] : "";

            if (dataStr.Trim() == "")
            {
                foreach (var tempKey in saveForm.AllKeys)
                {
                    if (tempKey == "tbName" || tempKey == "queryStr" || tempKey.Contains("fileList[") || tempKey.Contains("param.") || tempKey.Contains("supRels") || tempKey.Contains("teamRels")) continue;

                    dataStr += string.Format("{0}={1}&", tempKey, saveForm[tempKey]);
                }
            }

            InvokeResult result = dataOp.Save(tbName, queryStr, dataStr);

            if (result.Status == Status.Successful)
            {
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

                #region 供应商/合作伙伴关联
                string projId = result.BsonInfo.String("projId");
                string supRels = saveForm["supRels"] != null ? saveForm["supRels"] : "";
                string teamRels = saveForm["teamRels"] != null ? saveForm["teamRels"] : "";
                string teamCreateRels = saveForm["teamCreateRels"] != null ? saveForm["teamCreateRels"] : "";
                string patternIds = saveForm["patternIds"] != null ? saveForm["patternIds"] : "";

                List<StorageData> saveList = new List<StorageData>();


                #region 合作主创 供应商 字符串拆分
                string[] array = supRels.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);

                List<string> supRelArray = new List<string>();
                Dictionary<int, Dictionary<int, Dictionary<string, string>>> newDic = new Dictionary<int, Dictionary<int, Dictionary<string, string>>>();


                foreach (var item in array)
                {
                    Dictionary<int, Dictionary<string, string>> dic = new Dictionary<int, Dictionary<string, string>>();
                    string[] subArray = item.Split(new string[] { "|H|" }, StringSplitOptions.RemoveEmptyEntries);
                    Dictionary<string, string> subDic = new Dictionary<string, string>();

                    if (subArray.Length >= 2)
                    {
                        string secStr = subArray[1];
                        string[] secArray = secStr.Split(new string[] { "|Z|" }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var subItem in secArray)
                        {
                            string[] thirdArray = subItem.Split(new string[] { "|Y|" }, StringSplitOptions.RemoveEmptyEntries);
                            subDic.Add(thirdArray[0], thirdArray[1]);
                        }
                    }

                    string tepStr = subArray[0];
                    string[] tepArray = tepStr.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
                    if (tepArray[1] != "0" && tepArray.Length >= 2)
                    {
                        supRelArray.Add(string.Format("{0}:{1}", tepArray[0], tepArray[1]));
                        dic.Add(int.Parse(tepArray[1]), subDic);
                        //newDic.Add(int.Parse(tepArray[0]), dic);

                        #region 合作主创

                        List<BsonDocument> oldCreateRelList = dataOp.FindAllByKeyVal("XH_DesignManage_ProjectCreative", "projId", projId).ToList();   //所有旧的供应商关联

                        foreach (var teamRel in dic) //循环新的关联,已存在则不添加,不存在则添加新的
                        {
                            foreach (var tm in teamRel.Value)
                            {
                                string userName = tm.Key;
                                string userTitle = tm.Value;

                                BsonDocument oldRel = oldCreateRelList.Where(t => t.String("userName") == userName && t.String("userTitle") == userTitle).FirstOrDefault();

                                if (oldRel == null)
                                {
                                    StorageData tempData = new StorageData();

                                    tempData.Name = "XH_DesignManage_ProjectCreative";
                                    tempData.Document = new BsonDocument().Add("projId", projId.ToString())
                                                                          .Add("userName", userName.ToString())
                                                                          .Add("type", tepArray[0])
                                                                          .Add("supplierId", tepArray[1])
                                                                          .Add("userTitle", userTitle.ToString());
                                    tempData.Type = StorageType.Insert;

                                    saveList.Add(tempData);
                                }
                            }
                        }

                        foreach (var oldRel in oldCreateRelList)
                        {
                            foreach (var m in dic)
                            {
                                foreach (var gm in m.Value)
                                {
                                    if (!(oldRel.Int("supplierId") == m.Key && oldRel.Text("type") == tepArray[0] && gm.Key == oldRel.String("userName") && gm.Value == oldRel.String("userTitle")))
                                    {
                                        StorageData tempData = new StorageData();

                                        tempData.Name = "XH_DesignManage_ProjectCreative";
                                        tempData.Query = Query.EQ("relId", oldRel.String("relId"));
                                        tempData.Type = StorageType.Delete;

                                        saveList.Add(tempData);
                                    }
                                }
                            }
                        }
                        #endregion
                    }
                }
                #endregion

                #region 供应商


                List<BsonDocument> oldSupRelList = dataOp.FindAllByKeyVal("XH_DesignManage_ProjectSupplier", "projId", projId).ToList();   //所有旧的供应商关联

                foreach (var supRel in supRelArray) //循环新的关联,已存在则不添加,不存在则添加新的
                {
                    string[] infoArr = supRel.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);

                    if (infoArr.Count() >= 2)
                    {
                        string type = infoArr[0];
                        string supId = infoArr[1];

                        BsonDocument oldRel = oldSupRelList.Where(t => t.String("type") == type && t.String("supplierId") == supId).FirstOrDefault();

                        if (oldRel == null)
                        {
                            StorageData tempData = new StorageData();

                            tempData.Name = "XH_DesignManage_ProjectSupplier";
                            tempData.Document = new BsonDocument().Add("projId", projId.ToString())
                                                                  .Add("supplierId", supId.ToString())
                                                                  .Add("type", type.ToString());
                            tempData.Type = StorageType.Insert;

                            saveList.Add(tempData);
                        }
                    }
                }

                foreach (var oldRel in oldSupRelList)
                {
                    if (!supRelArray.Contains(string.Format("{0}:{1}", oldRel.Int("type"), oldRel.Int("supplierId"))))
                    {
                        StorageData tempData = new StorageData();

                        tempData.Name = "XH_DesignManage_ProjectSupplier";
                        tempData.Query = Query.EQ("relId", oldRel.String("relId"));
                        tempData.Type = StorageType.Delete;

                        saveList.Add(tempData);
                    }
                }
                #endregion

                #region 合作伙伴
                List<string> teamRelArray = teamRels.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();

                List<BsonDocument> oldTeamRelList = dataOp.FindAllByKeyVal("XH_DesignManage_ProjectTeam", "projId", projId).ToList();   //所有旧的供应商关联

                foreach (var teamRel in teamRelArray) //循环新的关联,已存在则不添加,不存在则添加新的
                {
                    string[] infoArr = teamRel.Split(new string[] { ":" }, StringSplitOptions.None);

                    if (infoArr.Count() >= 2 && infoArr[0].Trim() != "")
                    {

                        string userName = infoArr[0];
                        string userTitle = infoArr[1];

                        BsonDocument oldRel = oldTeamRelList.Where(t => t.String("userName") == userName && t.String("userTitle") == userTitle).FirstOrDefault();

                        if (oldRel == null)
                        {
                            StorageData tempData = new StorageData();

                            tempData.Name = "XH_DesignManage_ProjectTeam";
                            tempData.Document = new BsonDocument().Add("projId", projId.ToString())
                                                                  .Add("userName", userName.ToString())
                                                                  .Add("userTitle", userTitle.ToString());
                            tempData.Type = StorageType.Insert;

                            saveList.Add(tempData);
                        }
                    }
                }

                foreach (var oldRel in oldTeamRelList)
                {
                    if (!teamRelArray.Contains(string.Format("{0}:{1}", oldRel.String("userName"), oldRel.String("userTitle"))))
                    {
                        StorageData tempData = new StorageData();

                        tempData.Name = "XH_DesignManage_ProjectTeam";
                        tempData.Query = Query.EQ("relId", oldRel.String("relId"));
                        tempData.Type = StorageType.Delete;

                        saveList.Add(tempData);
                    }
                }
                #endregion

                //#region 合作主创
                //List<string> createRelArray = teamCreateRels.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();

                //List<BsonDocument> oldCreateRelList = dataOp.FindAllByKeyVal("XH_DesignManage_ProjectCreative", "projId", projId).ToList();   //所有旧的供应商关联

                //foreach (var teamRel in createRelArray) //循环新的关联,已存在则不添加,不存在则添加新的
                //{
                //    string[] infoArr = teamRel.Split(new string[] { ":" }, StringSplitOptions.None);

                //    if (infoArr.Count() >= 2 && infoArr[0].Trim() != "")
                //    {

                //        string userName = infoArr[0];
                //        string userTitle = infoArr[1];

                //        BsonDocument oldRel = oldCreateRelList.Where(t => t.String("userName") == userName && t.String("userTitle") == userTitle).FirstOrDefault();

                //        if (oldRel == null)
                //        {
                //            StorageData tempData = new StorageData();

                //            tempData.Name = "XH_DesignManage_ProjectCreative";
                //            tempData.Document = new BsonDocument().Add("projId", projId.ToString())
                //                                                  .Add("userName", userName.ToString())
                //                                                  .Add("userTitle", userTitle.ToString());
                //            tempData.Type = StorageType.Insert;

                //            saveList.Add(tempData);
                //        }
                //    }
                //}

                //foreach (var oldRel in oldCreateRelList)
                //{
                //    if (!createRelArray.Contains(string.Format("{0}:{1}", oldRel.String("userName"), oldRel.String("userTitle"))))
                //    {
                //        StorageData tempData = new StorageData();

                //        tempData.Name = "XH_DesignManage_ProjectCreative";
                //        tempData.Query = Query.EQ("relId", oldRel.String("relId"));
                //        tempData.Type = StorageType.Delete;

                //        saveList.Add(tempData);
                //    }
                //}
                //#endregion

                #region 项目业态
                List<string> patternIdList = patternIds.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList(); //提交上来的Id列表

                List<BsonDocument> oldPatternRelList = dataOp.FindAllByKeyVal("XH_DesignManage_ProjectPattern", "projId", projId).ToList();   //所有旧的供关联

                foreach (var tempPattern in patternIdList) //循环新的关联,已存在则不添加,不存在则添加新的
                {
                    BsonDocument oldRel = oldPatternRelList.Where(t => t.String("patternId") == tempPattern).FirstOrDefault();

                    if (oldRel == null)
                    {
                        StorageData tempData = new StorageData();

                        tempData.Name = "XH_DesignManage_ProjectPattern";
                        tempData.Document = new BsonDocument().Add("projId", projId.ToString())
                                                              .Add("patternId", tempPattern.ToString());
                        tempData.Type = StorageType.Insert;

                        saveList.Add(tempData);
                    }
                }

                foreach (var oldRel in oldPatternRelList)
                {
                    if (!patternIdList.Contains(oldRel.String("patternId")))
                    {
                        StorageData tempData = new StorageData();

                        tempData.Name = "XH_DesignManage_ProjectPattern";
                        tempData.Query = Query.EQ("relId", oldRel.String("relId"));
                        tempData.Type = StorageType.Delete;

                        saveList.Add(tempData);
                    }
                }
                #endregion

                dataOp.BatchSaveStorageData(saveList);

                #endregion
            }

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }


        /// <summary>
        /// 中海弘扬保存项目信息，默认载入经济技术指标
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult QuickCreateProject(FormCollection saveForm)
        {
            string tbName = saveForm["tbName"] != null ? saveForm["tbName"] : "";
            string queryStr = saveForm["queryStr"] != null ? saveForm["queryStr"] : "";
            string dataStr = saveForm["dataStr"] != null ? saveForm["dataStr"] : "";

            if (dataStr.Trim() == "")
            {
                foreach (var tempKey in saveForm.AllKeys)
                {
                    if (tempKey == "tbName" || tempKey == "queryStr" || tempKey.Contains("fileList[") || tempKey.Contains("param.") || tempKey.Contains("supRels") || tempKey.Contains("teamRels")) continue;

                    dataStr += string.Format("{0}={1}&", tempKey, saveForm[tempKey]);
                }
            }

            InvokeResult prjectResult = dataOp.Save(tbName, queryStr, dataStr);

            if (prjectResult.Status == Status.Successful)
            {
                var bsonObj = prjectResult.BsonInfo;
                if (bsonObj != null)
                {
                    var templateTb = "XH_DesignManage_IndexTemplate";
                    var engId = bsonObj.Int("engId");
                    var projId = bsonObj.Int("projId");
                    var isAdd = 0;
                    var itemTb = "XH_DesignManage_IndexItem";
                    var columnTb = "XH_DesignManage_IndexColumn";
                    BsonDocument template = dataOp.FindAll(templateTb).FirstOrDefault();    //获取模板信息
                    if (template != null)
                    {
                        #region 获取项和列的数据记录
                        List<BsonDocument> itemList = new List<BsonDocument>();     //所有要导入的项数据

                        BsonReader itemReader = BsonReader.Create(template.String("itemContent"));

                        while (itemReader.CurrentBsonType != BsonType.EndOfDocument)
                        {
                            BsonDocument tempBson = BsonDocument.ReadFrom(itemReader);

                            itemList.Add(tempBson);
                        }

                        List<BsonDocument> columnList = new List<BsonDocument>();     //所有要导入的列数据

                        BsonReader columnReader = BsonReader.Create(template.String("columnContent"));

                        while (columnReader.CurrentBsonType != BsonType.EndOfDocument)
                        {
                            BsonDocument tempBson = BsonDocument.ReadFrom(columnReader);

                            columnList.Add(tempBson);
                        }

                        #endregion

                        #region 将项和列的数据导入
                        InvokeResult result = new InvokeResult();

                        Dictionary<int, int> corDic = new Dictionary<int, int>(); //节点Id对应字典(用于树形pid)
                        corDic.Add(0, 0);

                        try
                        {
                            using (TransactionScope trans = new TransactionScope())
                            {
                                if (isAdd == 0)     //如果不是追加,则删除所有已有的项和列记录
                                {
                                    var query = projId == 0 ? Query.And(Query.EQ("engId", engId.ToString()), Query.EQ("projId", "0")) : Query.EQ("projId", projId.ToString());

                                    InvokeResult tempRet = dataOp.Delete(itemTb, query);
                                    if (tempRet.Status == Status.Failed) throw new Exception(tempRet.Message);

                                    tempRet = dataOp.Delete(columnTb, query);
                                    if (tempRet.Status == Status.Failed) throw new Exception(tempRet.Message);
                                }

                                foreach (var tempBson in itemList)
                                {
                                    int tempId = tempBson.Int("id");
                                    int tempPid = tempBson.Int("pid");

                                    if (corDic.ContainsKey(tempPid))
                                    {
                                        tempBson.Add("engId", engId.ToString());
                                        tempBson.Add("projId", projId.ToString());
                                        tempBson.Add("nodePid", corDic[tempPid].ToString());
                                        tempBson.Remove("id");
                                        tempBson.Remove("pid");

                                        InvokeResult tempRet = dataOp.Insert(itemTb, tempBson);
                                        if (tempRet.Status == Status.Failed) throw new Exception(tempRet.Message);

                                        corDic.Add(tempId, tempRet.BsonInfo.Int("itemId"));
                                    }
                                }

                                foreach (var tempBson in columnList)
                                {
                                    tempBson.Add("engId", engId.ToString());
                                    tempBson.Add("projId", projId.ToString());
                                    tempBson.Remove("id");

                                    InvokeResult tempRet = dataOp.Insert(columnTb, tempBson);
                                    if (tempRet.Status == Status.Failed) throw new Exception(tempRet.Message);

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
                    }
                }
            }

            return Json(TypeConvert.InvokeResultToPageJson(prjectResult));
        }


        /// <summary>
        /// 保存提交上来的数据
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        [ValidateInput(false)]
        public ActionResult SaveProjInfoNew(FormCollection saveForm)
        {
            InvokeResult result = new InvokeResult();

            #region 构建数据
            string tbName = saveForm["tbName"] != null ? saveForm["tbName"] : "";
            string queryStr = saveForm["queryStr"] != null ? saveForm["queryStr"] : "";
            string dataStr = saveForm["dataStr"] != null ? saveForm["dataStr"] : "";

            if (dataStr.Trim() == "")
            {
                foreach (var tempKey in saveForm.AllKeys)
                {
                    if (tempKey == "tbName" || tempKey == "queryStr" || tempKey.Contains("fileList[") || tempKey.Contains("param.")) continue;

                    dataStr += string.Format("{0}={1}&", tempKey, saveForm[tempKey]);
                }
            }
            #endregion

            #region 保存数据
            result = dataOp.Save(tbName, queryStr, dataStr);
            result.FileInfo = "";
            if (result.Status == Status.Successful && string.IsNullOrEmpty(queryStr))
            {
                string id = "";
                switch (result.BsonInfo.Text("underTable"))
                {
                    case "XH_ProductDev_Area":
                        id = result.BsonInfo.Text("areaId");
                        break;
                    case "XH_ProductDev_City":
                        id = result.BsonInfo.Text("cityId");
                        break;
                    case "XH_DesignManage_Engineering":
                        id = result.BsonInfo.Text("engId");
                        break;
                    case "XH_DesignManage_Project":
                        id = result.BsonInfo.Text("projId");
                        break;
                }
                if (!string.IsNullOrEmpty(id))
                {
                    CreateDefaultPorjectRole(id, result);
                }
            }
            #endregion

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }


        /// <summary>
        /// 为项目创建角色创建默认角色
        /// </summary>
        /// <param name="id"></param>
        /// <param name="userId"></param>
        /// <param name="tableName">XH_ProductDev_Area XH_ProductDev_City XH_DesignManage_Engineering XH_DesignManage_Project</param>
        /// <returns></returns>
        public InvokeResult CreateDefaultPorjectRole(string id, InvokeResult ret)
        {
            InvokeResult result = new InvokeResult();
            string word = "";
            switch (ret.BsonInfo.Text("underTable"))
            {
                case "XH_ProductDev_Area":
                    word = "区域";
                    break;
                case "XH_ProductDev_City":
                    word = "城市";
                    break;
                case "XH_DesignManage_Engineering":
                    word = "项目";
                    break;
                case "XH_DesignManage_Project":
                    word = "分期";
                    break;
            }
            try
            {
                #region 创建默认角色
                BsonDocument doc = new BsonDocument();
                doc.Add("name", string.Format("{0}{1}默认角色", ret.BsonInfo.Text("name"), word));
                doc.Add("roleType", "1");
                doc.Add("formulaId", "1");
                doc.Add("dataObjId", "1");
                doc.Add("isDataRight", "1");
                doc.Add("isBuiltIn", "-1");
                result = dataOp.Insert("SysRole", doc);
                #endregion


                #region 为角色赋予权限
                BsonDocument score = new BsonDocument();
                BsonDocument proj = new BsonDocument();
                BsonDocument eng = new BsonDocument();
                BsonDocument city = new BsonDocument();
                BsonDocument area = new BsonDocument();
                InvokeResult rett = new InvokeResult();
                string idd = id;
                switch (ret.BsonInfo.Text("underTable"))
                {

                    case "XH_DesignManage_Project":
                        proj = dataOp.FindOneByKeyVal("XH_DesignManage_Project", "projId", id);
                        idd = proj.Text("engId");
                        score.Add("roleCategoryId", "1");
                        score.Add("dataTableName", "XH_DesignManage_Project");
                        score.Add("dataId", id);
                        score.Add("roleId", result.BsonInfo.Text("roleId"));
                        score.Add("dataFeiIdName", "");
                        score.Add("status", "0");
                        score.Add("remark", "区域权限");
                        rett = dataOp.Insert("DataScope", score);
                        goto case "XH_DesignManage_Engineering";

                    case "XH_DesignManage_Engineering":

                        eng = dataOp.FindOneByKeyVal("XH_DesignManage_Engineering", "engId", idd);
                        idd = eng.Text("cityId");
                        if (dataOp.FindOneByQuery("DataScope", Query.And(Query.EQ("dataTableName", "XH_DesignManage_Engineering"), Query.EQ("dataId", idd), Query.EQ("roleId", result.BsonInfo.Text("roleId")))) == null)
                        {
                            score = new BsonDocument();
                            score.Add("roleCategoryId", "1");
                            score.Add("dataTableName", "XH_DesignManage_Engineering");
                            score.Add("dataId", eng.Text("engId"));
                            score.Add("roleId", result.BsonInfo.Text("roleId"));
                            score.Add("dataFeiIdName", "");
                            score.Add("status", "0");
                            score.Add("remark", "区域权限");
                            rett = dataOp.Insert("DataScope", score);
                        }
                        goto case "XH_ProductDev_City";

                    case "XH_ProductDev_City":
                        city = dataOp.FindOneByKeyVal("XH_ProductDev_City", "cityId", idd);
                        idd = city.Text("areaId");

                        if (dataOp.FindOneByQuery("DataScope", Query.And(Query.EQ("dataTableName", "XH_ProductDev_City"), Query.EQ("dataId", idd), Query.EQ("roleId", result.BsonInfo.Text("roleId")))) == null)
                        {
                            score = new BsonDocument();
                            score.Add("roleCategoryId", "1");
                            score.Add("dataTableName", "XH_ProductDev_City");
                            score.Add("dataId", city.Text("cityId"));
                            score.Add("roleId", result.BsonInfo.Text("roleId"));
                            score.Add("dataFeiIdName", "");
                            score.Add("status", "0");
                            score.Add("remark", "区域权限");
                            rett = dataOp.Insert("DataScope", score);
                        }
                        goto case "XH_ProductDev_Area";

                    case "XH_ProductDev_Area":

                        area = dataOp.FindOneByKeyVal("XH_ProductDev_Area", "areaId", idd);
                        idd = area.Text("areaId");

                        if (dataOp.FindOneByQuery("DataScope", Query.And(Query.EQ("dataTableName", "XH_ProductDev_Area"), Query.EQ("dataId", idd), Query.EQ("roleId", result.BsonInfo.Text("roleId")))) == null)
                        {
                            score = new BsonDocument();
                            score.Add("roleCategoryId", "1");
                            score.Add("dataTableName", "XH_ProductDev_Area");
                            score.Add("dataId", area.Text("areaId"));
                            score.Add("roleId", result.BsonInfo.Text("roleId"));
                            score.Add("dataFeiIdName", "");
                            score.Add("status", "0");
                            score.Add("remark", "区域权限");
                            rett = dataOp.Insert("DataScope", score);
                        }

                        break;
                    default:
                        break;
                }
                #endregion

                #region 将当前创建者加入角色
                if (result.Status == Status.Successful)
                {
                    BsonDocument roleUser = new BsonDocument();
                    roleUser.Add("userId", ret.BsonInfo.Text("createUserId"));
                    roleUser.Add("roleId", result.BsonInfo.Text("roleId"));
                    result = dataOp.Insert("SysRoleUser", roleUser);
                    if (result.Status == Status.Successful)
                    {
                        #region 将同样拥有区域权限的人加入角色中
                        if (SysAppConfig.CustomerCode == "71E8DBA3-5DC6-4597-9DCD-F3CC1F04FCXH")
                        {
                            if (ret.BsonInfo.Text("underTable") == "XH_DesignManage_Engineering")
                            {
                                //拥有当前区域的所有角色
                                var roleIdList = dataOp.FindAllByQuery("DataScope", Query.And(Query.EQ("dataTableName", "XH_ProductDev_City"), Query.EQ("dataId", ret.BsonInfo.Text("cityId")))).Select(t => t.Text("roleId")).ToList();

                                foreach (var item in roleIdList)
                                {
                                    score = new BsonDocument();
                                    score.Add("roleCategoryId", "1");
                                    score.Add("dataTableName", "XH_DesignManage_Engineering");
                                    score.Add("dataId", eng.Text("engId"));
                                    score.Add("roleId", item);
                                    score.Add("dataFeiIdName", "");
                                    score.Add("status", "0");
                                    score.Add("remark", "区域权限");
                                    rett = dataOp.Insert("DataScope", score);
                                }
                            }
                            else if (ret.BsonInfo.Text("underTable") == "XH_DesignManage_Project")
                            {
                                //拥有当前区域的所有角色
                                var roleIdList = dataOp.FindAllByQuery("DataScope", Query.And(Query.EQ("dataTableName", "XH_DesignManage_Engineering"), Query.EQ("dataId", ret.BsonInfo.Text("engId")))).Select(t => t.Text("roleId")).ToList();

                                foreach (var item in roleIdList)
                                {
                                    score = new BsonDocument();
                                    score.Add("roleCategoryId", "1");
                                    score.Add("dataTableName", "XH_DesignManage_Project");
                                    score.Add("dataId", proj.Text("projId"));
                                    score.Add("roleId", item);
                                    score.Add("dataFeiIdName", "");
                                    score.Add("status", "0");
                                    score.Add("remark", "区域权限");
                                    rett = dataOp.Insert("DataScope", score);
                                }
                            }
                        }
                        #endregion
                    }
                }

                #endregion

            }
            catch (Exception ex)
            {

            }
            return result;
        }

        /// <summary>
        /// 中海保存项目信息
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>

        public ActionResult SaveProjectInfoZHHY(FormCollection saveForm)
        {
            string tbName = saveForm["tbName"] != null ? saveForm["tbName"] : "";
            string queryStr = saveForm["queryStr"] != null ? saveForm["queryStr"] : "";
            string dataStr = saveForm["dataStr"] != null ? saveForm["dataStr"] : "";

            if (dataStr.Trim() == "")
            {
                foreach (var tempKey in saveForm.AllKeys)
                {
                    if (tempKey == "tbName" || tempKey == "queryStr" || tempKey.Contains("fileList[") || tempKey.Contains("param.") || tempKey.Contains("supRels") || tempKey.Contains("teamRels")) continue;

                    dataStr += string.Format("{0}={1}&", tempKey, saveForm[tempKey]);
                }
            }

            InvokeResult result = dataOp.Save(tbName, queryStr, dataStr);

            if (result.Status == Status.Successful)
            {
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

                #region 供应商/合作伙伴关联
                string projId = result.BsonInfo.String("projId");
                string supRels = saveForm["supRels"] != null ? saveForm["supRels"] : "";
                string teamRels = saveForm["teamRels"] != null ? saveForm["teamRels"] : "";
                string teamCreateRels = saveForm["teamCreateRels"] != null ? saveForm["teamCreateRels"] : "";
                string patternIds = saveForm["patternIds"] != null ? saveForm["patternIds"] : "";

                List<StorageData> saveList = new List<StorageData>();

                #region 供应商
                if (saveForm["supRels"] != null)
                {
                    List<string> supRelArray = supRels.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();

                    List<BsonDocument> oldSupRelList = dataOp.FindAllByKeyVal("XH_DesignManage_ProjectSupplier", "projId", projId).ToList();   //所有旧的供应商关联

                    foreach (var supRel in supRelArray) //循环新的关联,已存在则不添加,不存在则添加新的
                    {
                        string[] infoArr = supRel.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);

                        if (infoArr.Count() >= 2)
                        {
                            string type = infoArr[0];
                            string supId = infoArr[1];

                            BsonDocument oldRel = oldSupRelList.Where(t => t.String("type") == type && t.String("supplierId") == supId).FirstOrDefault();

                            if (oldRel == null)
                            {
                                StorageData tempData = new StorageData();

                                tempData.Name = "XH_DesignManage_ProjectSupplier";
                                tempData.Document = TypeConvert.ParamStrToBsonDocument("projId=" + projId + "&supplierId=" + supId + "&type=" + type);
                                tempData.Type = StorageType.Insert;

                                saveList.Add(tempData);
                            }
                        }
                    }

                    foreach (var oldRel in oldSupRelList)
                    {
                        if (!supRelArray.Contains(string.Format("{0}:{1}", oldRel.Int("type"), oldRel.Int("supplierId"))))
                        {
                            StorageData tempData = new StorageData();

                            tempData.Name = "XH_DesignManage_ProjectSupplier";
                            tempData.Query = Query.EQ("relId", oldRel.String("relId"));
                            tempData.Type = StorageType.Delete;

                            saveList.Add(tempData);
                        }
                    }
                }

                #endregion

                #region 合作伙伴
                List<string> teamRelArray = teamRels.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();

                List<BsonDocument> oldTeamRelList = dataOp.FindAllByKeyVal("XH_DesignManage_ProjectTeam", "projId", projId).ToList();   //所有旧的供应商关联

                foreach (var teamRel in teamRelArray) //循环新的关联,已存在则不添加,不存在则添加新的
                {
                    string[] infoArr = teamRel.Split(new string[] { ":" }, StringSplitOptions.None);

                    if (infoArr.Count() >= 2 && infoArr[0].Trim() != "")
                    {
                        if (SysAppConfig.CustomerCode == "F8A3250F-A433-42be-9F68-803BBF01ZHHY")
                        {
                            string userName = infoArr[0];
                            string userTitle = infoArr[1];
                            string userId = infoArr[2];
                            BsonDocument oldRel = oldTeamRelList.Where(t => t.String("userName") == userName && t.String("userTitle") == userTitle && t.Text("userId") == userId).FirstOrDefault();

                            if (oldRel == null)
                            {
                                StorageData tempData = new StorageData();

                                tempData.Name = "XH_DesignManage_ProjectTeam";
                                tempData.Document = TypeConvert.ParamStrToBsonDocument("projId=" + projId + "&userName=" + userName + "&userTitle=" + userTitle + "&userId=" + userId);
                                tempData.Type = StorageType.Insert;

                                saveList.Add(tempData);
                            }
                        }
                        else
                        {
                            string userName = infoArr[0];
                            string userTitle = infoArr[1];

                            //BsonDocument oldRel = oldTeamRelList.Where(t => t.String("userName") == userName && t.String("userTitle") == userTitle).FirstOrDefault();

                            //if (oldRel == null)
                            //{
                            //    StorageData tempData = new StorageData();

                            //    tempData.Name = "XH_DesignManage_ProjectTeam";
                            //    tempData.Document = TypeConvert.ParamStrToBsonDocument("projId=" + projId + "&userName=" + userName + "&userTitle=" + userTitle);
                            //    tempData.Type = StorageType.Insert;

                            //    saveList.Add(tempData);
                            //}

                            StorageData tempData = new StorageData();

                            tempData.Name = "XH_DesignManage_ProjectTeam";
                            tempData.Document = TypeConvert.ParamStrToBsonDocument("projId=" + projId + "&userName=" + userName + "&userTitle=" + userTitle);
                            tempData.Type = StorageType.Insert;

                            saveList.Add(tempData);
                        }
                    }
                }

                foreach (var oldRel in oldTeamRelList)
                {
                    if (SysAppConfig.CustomerCode == "F8A3250F-A433-42be-9F68-803BBF01ZHHY")
                    {
                        if (!teamRelArray.Contains(string.Format("{0}:{1}:{2}", oldRel.String("userName"), oldRel.String("userTitle"), oldRel.Text("userId"))))
                        {
                            StorageData tempData = new StorageData();

                            tempData.Name = "XH_DesignManage_ProjectTeam";
                            tempData.Query = Query.EQ("relId", oldRel.String("relId"));
                            tempData.Type = StorageType.Delete;

                            saveList.Add(tempData);
                        }
                    }
                    else
                    {
                        //if (!teamRelArray.Contains(string.Format("{0}:{1}", oldRel.String("userName"), oldRel.String("userTitle"))))
                        //{
                        //    StorageData tempData = new StorageData();

                        //    tempData.Name = "XH_DesignManage_ProjectTeam";
                        //    tempData.Query = Query.EQ("relId", oldRel.String("relId"));
                        //    tempData.Type = StorageType.Delete;

                        //    saveList.Add(tempData);
                        //}

                        StorageData tempData = new StorageData();

                        tempData.Name = "XH_DesignManage_ProjectTeam";
                        tempData.Query = Query.EQ("relId", oldRel.String("relId"));
                        tempData.Type = StorageType.Delete;

                        saveList.Add(tempData);

                    }

                }
                #endregion

                #region 合作主创
                List<string> createRelArray = teamCreateRels.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();

                List<BsonDocument> oldCreateRelList = dataOp.FindAllByKeyVal("XH_DesignManage_ProjectCreative", "projId", projId).ToList();   //所有旧的供应商关联

                foreach (var teamRel in createRelArray) //循环新的关联,已存在则不添加,不存在则添加新的
                {
                    string[] infoArr = teamRel.Split(new string[] { ":" }, StringSplitOptions.None);

                    if (infoArr.Count() >= 2 && infoArr[0].Trim() != "")
                    {

                        string userName = infoArr[0];
                        string userTitle = infoArr[1];

                        BsonDocument oldRel = oldCreateRelList.Where(t => t.String("userName") == userName && t.String("userTitle") == userTitle).FirstOrDefault();

                        if (oldRel == null)
                        {
                            StorageData tempData = new StorageData();

                            tempData.Name = "XH_DesignManage_ProjectCreative";
                            tempData.Document = TypeConvert.ParamStrToBsonDocument("projId=" + projId + "&userName=" + userName + "&userTitle=" + userTitle);
                            tempData.Type = StorageType.Insert;

                            saveList.Add(tempData);
                        }
                    }
                }

                foreach (var oldRel in oldCreateRelList)
                {
                    if (!createRelArray.Contains(string.Format("{0}:{1}", oldRel.String("userName"), oldRel.String("userTitle"))))
                    {
                        StorageData tempData = new StorageData();

                        tempData.Name = "XH_DesignManage_ProjectCreative";
                        tempData.Query = Query.EQ("relId", oldRel.String("relId"));
                        tempData.Type = StorageType.Delete;

                        saveList.Add(tempData);
                    }
                }
                #endregion

                #region 项目业态
                List<string> patternIdList = patternIds.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList(); //提交上来的Id列表

                List<BsonDocument> oldPatternRelList = dataOp.FindAllByKeyVal("XH_DesignManage_ProjectPattern", "projId", projId).ToList();   //所有旧的供关联

                foreach (var tempPattern in patternIdList) //循环新的关联,已存在则不添加,不存在则添加新的
                {
                    BsonDocument oldRel = oldPatternRelList.Where(t => t.String("patternId") == tempPattern).FirstOrDefault();

                    if (oldRel == null)
                    {
                        StorageData tempData = new StorageData();

                        tempData.Name = "XH_DesignManage_ProjectPattern";
                        tempData.Document = new BsonDocument().Add("projId", projId.ToString())
                                                              .Add("patternId", tempPattern.ToString());
                        tempData.Type = StorageType.Insert;

                        saveList.Add(tempData);
                    }
                }

                foreach (var oldRel in oldPatternRelList)
                {
                    if (!patternIdList.Contains(oldRel.String("patternId")))
                    {
                        StorageData tempData = new StorageData();

                        tempData.Name = "XH_DesignManage_ProjectPattern";
                        tempData.Query = Query.EQ("relId", oldRel.String("relId"));
                        tempData.Type = StorageType.Delete;

                        saveList.Add(tempData);
                    }
                }
                #endregion


                dataOp.BatchSaveStorageData(saveList);

                #endregion

                var bsonObj = result.BsonInfo;
                if (bsonObj != null)
                {
                    var templateTb = "XH_DesignManage_IndexTemplate";
                    var engId = bsonObj.Int("engId");
                    var isAdd = 1;
                    var itemTb = "XH_DesignManage_IndexItem";
                    var columnTb = "XH_DesignManage_IndexColumn";
                    var query = projId == "0" ? Query.And(Query.EQ("engId", engId.ToString()), Query.EQ("projId", "0")) : Query.EQ("projId", projId.ToString());
                    var hasExistQuery = dataOp.FindAllByQuery(itemTb, query);
                    if (hasExistQuery.Count() <= 0)
                    {
                        BsonDocument template = dataOp.FindAll(templateTb).FirstOrDefault();    //获取模板信息
                        if (template != null)
                        {
                            #region 获取项和列的数据记录
                            List<BsonDocument> itemList = new List<BsonDocument>();     //所有要导入的项数据

                            BsonReader itemReader = BsonReader.Create(template.String("itemContent"));

                            while (itemReader.CurrentBsonType != BsonType.EndOfDocument)
                            {
                                BsonDocument tempBson = BsonDocument.ReadFrom(itemReader);

                                itemList.Add(tempBson);
                            }

                            List<BsonDocument> columnList = new List<BsonDocument>();     //所有要导入的列数据

                            BsonReader columnReader = BsonReader.Create(template.String("columnContent"));

                            while (columnReader.CurrentBsonType != BsonType.EndOfDocument)
                            {
                                BsonDocument tempBson = BsonDocument.ReadFrom(columnReader);

                                columnList.Add(tempBson);
                            }

                            #endregion

                            #region 将项和列的数据导入
                            InvokeResult indexResult = new InvokeResult();

                            Dictionary<int, int> corDic = new Dictionary<int, int>(); //节点Id对应字典(用于树形pid)
                            corDic.Add(0, 0);

                            try
                            {
                                using (TransactionScope trans = new TransactionScope())
                                {
                                    if (isAdd == 0)     //如果不是追加,则删除所有已有的项和列记录
                                    {

                                        InvokeResult tempRet = dataOp.Delete(itemTb, query);
                                        if (tempRet.Status == Status.Failed) throw new Exception(tempRet.Message);

                                        tempRet = dataOp.Delete(columnTb, query);
                                        if (tempRet.Status == Status.Failed) throw new Exception(tempRet.Message);
                                    }

                                    foreach (var tempBson in itemList)
                                    {
                                        int tempId = tempBson.Int("id");
                                        int tempPid = tempBson.Int("pid");

                                        if (corDic.ContainsKey(tempPid))
                                        {
                                            tempBson.Add("engId", engId.ToString());
                                            tempBson.Add("projId", projId.ToString());
                                            tempBson.Add("nodePid", corDic[tempPid].ToString());
                                            tempBson.Remove("id");
                                            tempBson.Remove("pid");

                                            InvokeResult tempRet = dataOp.Insert(itemTb, tempBson);
                                            if (tempRet.Status == Status.Failed) throw new Exception(tempRet.Message);

                                            corDic.Add(tempId, tempRet.BsonInfo.Int("itemId"));
                                        }
                                    }

                                    foreach (var tempBson in columnList)
                                    {
                                        tempBson.Add("engId", engId.ToString());
                                        tempBson.Add("projId", projId.ToString());
                                        tempBson.Remove("id");

                                        InvokeResult tempRet = dataOp.Insert(columnTb, tempBson);
                                        if (tempRet.Status == Status.Failed) throw new Exception(tempRet.Message);

                                    }
                                    trans.Complete();
                                }

                                indexResult.Status = Status.Successful;
                            }
                            catch (Exception e)
                            {
                                indexResult.Status = Status.Failed;
                                indexResult.Message = e.Message;
                            }

                            #endregion
                        }
                    }
                }
            }

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        public ActionResult saveManager(FormCollection saveForm)
        {
            InvokeResult result = new InvokeResult();
            string teamRels = PageReq.GetParam("teamRels"); //供应商负责人
            string TendertemRels = PageReq.GetParam("TendertemRels"); //投标负责人
            string designers = PageReq.GetParam("designers"); //投资主要设计师
            string supplierId = PageReq.GetParam("supplierId");
            List<string> teamRelArray = teamRels.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();
            List<string> TenderteamRelArray = TendertemRels.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();
            List<string> designersRelArray = designers.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();
            List<BsonDocument> oldTeamRelList = dataOp.FindAllByKeyVal("XH_Supplier_Manager", "supplierId", supplierId).ToList();   //所有旧的供应商关联
            List<BsonDocument> oldTenderTeamRelList = dataOp.FindAllByKeyVal("XH_Supplier_TenderManager", "supplierId", supplierId).ToList();   //所有旧的供应商关联
            List<BsonDocument> olddesignersRelList = dataOp.FindAllByKeyVal("XH_Supplier_Designer", "supplierId", supplierId).ToList();
            List<StorageData> saveList = new List<StorageData>();
            foreach (var teamRel in teamRelArray) //循环新的关联,已存在则不添加,不存在则添加新的
            {
                string[] infoArr = teamRel.Split(new string[] { ":" }, StringSplitOptions.None);

                if (infoArr.Count() >= 3 && infoArr[0].Trim() != "")
                {

                    string manName = infoArr[0];
                    string manPost = infoArr[1];
                    string manphone = infoArr[2];

                    BsonDocument oldRel = oldTeamRelList.Where(t => t.String("ManagerName") == manName && t.String("ManagerPost") == manPost && t.String("ManagerPhone") == manphone).FirstOrDefault();

                    if (oldRel == null)
                    {
                        StorageData tempData = new StorageData();

                        tempData.Name = "XH_Supplier_Manager";
                        tempData.Document = TypeConvert.ParamStrToBsonDocument("supplierId=" + supplierId + "&ManagerName=" + manName + "&ManagerPhone=" + manphone + "&ManagerPost=" + manPost);
                        tempData.Type = StorageType.Insert;

                        saveList.Add(tempData);
                    }
                }
            }

            foreach (var oldRel in oldTeamRelList) //删除旧数据
            {
                if (!teamRelArray.Contains(string.Format("{0}:{1}:{2}", oldRel.String("ManagerName"), oldRel.String("ManagerPost"), oldRel.String("ManagerPhone"))))
                {
                    StorageData tempData = new StorageData();

                    tempData.Name = "XH_Supplier_Manager";
                    tempData.Query = Query.EQ("managerId", oldRel.String("managerId"));
                    tempData.Type = StorageType.Delete;

                    saveList.Add(tempData);
                }
            }
            foreach (var teamRel in TenderteamRelArray) //循环新的关联,已存在则不添加,不存在则添加新的
            {
                string[] infoArr = teamRel.Split(new string[] { ":" }, StringSplitOptions.None);

                if (infoArr.Count() >= 3 && infoArr[0].Trim() != "")
                {

                    string manName = infoArr[0];
                    string manPost = infoArr[1];
                    string manphone = infoArr[2];

                    BsonDocument oldRel = oldTeamRelList.Where(t => t.String("TenderManagerName") == manName && t.String("TenderManagerPost") == manPost && t.String("TenderManagerPhone") == manphone).FirstOrDefault();

                    if (oldRel == null)
                    {
                        StorageData tempData = new StorageData();

                        tempData.Name = "XH_Supplier_TenderManager";
                        tempData.Document = TypeConvert.ParamStrToBsonDocument("supplierId=" + supplierId + "&TenderManagerName=" + manName + "&TenderManagerPhone=" + manphone + "&TenderManagerPost=" + manPost);
                        tempData.Type = StorageType.Insert;

                        saveList.Add(tempData);
                    }
                }
            }

            foreach (var oldRel in oldTenderTeamRelList) //删除旧数据
            {
                if (!teamRelArray.Contains(string.Format("{0}:{1}:{2}", oldRel.String("TenderManagerName"), oldRel.String("TenderManagerPost"), oldRel.String("TenderManagerPhone"))))
                {
                    StorageData tempData = new StorageData();

                    tempData.Name = "XH_Supplier_TenderManager";
                    tempData.Query = Query.EQ("TendermanagerId", oldRel.String("TendermanagerId"));
                    tempData.Type = StorageType.Delete;

                    saveList.Add(tempData);
                }
            }

            foreach (var teamRel in designersRelArray) //循环新的关联,已存在则不添加,不存在则添加新的
            {
                string[] infoArr = teamRel.Split(new string[] { ":" }, StringSplitOptions.None);

                if (infoArr.Count() >= 3 && infoArr[0].Trim() != "")
                {

                    string manName = infoArr[0];
                    string manPost = infoArr[1];
                    string manphone = infoArr[2];

                    BsonDocument oldRel = oldTeamRelList.Where(t => t.String("name") == manName && t.String("Post") == manPost && t.String("Contact") == manphone).FirstOrDefault();

                    if (oldRel == null)
                    {
                        StorageData tempData = new StorageData();

                        tempData.Name = "XH_Supplier_Designer";
                        tempData.Document = TypeConvert.ParamStrToBsonDocument("supplierId=" + supplierId + "&name=" + manName + "&Contact=" + manphone + "&Post=" + manPost);
                        tempData.Type = StorageType.Insert;

                        saveList.Add(tempData);
                    }
                }
            }

            foreach (var oldRel in olddesignersRelList) //删除旧数据
            {
                if (!teamRelArray.Contains(string.Format("{0}:{1}:{2}", oldRel.String("name"), oldRel.String("Post"), oldRel.String("Contact"))))
                {
                    StorageData tempData = new StorageData();

                    tempData.Name = "XH_Supplier_Designer";
                    tempData.Query = Query.EQ("designerId", oldRel.String("designerId"));
                    tempData.Type = StorageType.Delete;

                    saveList.Add(tempData);
                }
            }
            result = dataOp.BatchSaveStorageData(saveList);
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }
        /// <summary>
        /// 获取EPS结构的JSON数据
        /// </summary>
        /// <returns></returns>
        public ActionResult GetEpsStructureJson()
        {
            List<EngStructure> list = new List<EngStructure>();
            string engId = PageReq.GetParam("engId");

            var eng = dataOp.FindOneByKeyVal("XH_DesignManage_Engineering", "engId", engId);
            if (eng != null)
            {
                EngStructure engmodel = new EngStructure();
                engmodel.id = int.Parse(engId);
                engmodel.txt = eng.Text("name");
                engmodel.pid = 0;
                engmodel.nodeId = -1;
                engmodel.type = 0;
                list.Add(engmodel);
                var projList = dataOp.FindAllByKeyVal("XH_DesignManage_Project", "engId", engId);
                foreach (var item in projList)
                {
                    EngStructure model = new EngStructure();
                    model.nodeId = item.Int("projId");
                    model.id = item.Int("projId");
                    model.txt = item.Text("name");
                    model.pid = item.Int("nodePid") == 0 ? -1 : item.Int("nodePid");
                    model.type = 1;
                    list.Add(model);
                }
            }

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 旭辉专用  批量修改engId下项目所属城市
        /// </summary>
        /// <returns></returns>
        public ActionResult ChangeProjectCityId()
        {
            PageJson json = new PageJson();
            InvokeResult result = new InvokeResult();
            int engId = PageReq.GetFormInt("engId");
            int cityId = PageReq.GetFormInt("cityId");
            List<StorageData> saveList = new List<StorageData>();

            var projList = dataOp.FindAllByKeyVal("XH_DesignManage_Project", "engId", engId.ToString()).ToList();

            foreach (var item in projList)
            {
                StorageData tempSave = new StorageData();
                tempSave.Name = "XH_DesignManage_Project";
                tempSave.Query = Query.EQ("projId", item.String("projId"));
                BsonDocument bson = new BsonDocument();
                bson.Add("cityId", cityId.ToString());
                tempSave.Document = bson;
                tempSave.Type = StorageType.Update;
                saveList.Add(tempSave);
            }

            result = dataOp.BatchSaveStorageData(saveList);
            json = TypeConvert.InvokeResultToPageJson(result);
            return Json(json);
        }

        /// <summary>
        /// 获取有权限的城市列表
        /// </summary>
        /// <returns></returns>
        public ActionResult GetCityJson(string areaId)
        {
            string tbName = "XH_ProductDev_City";

            var query = areaId == "-1" ? Query.Null : Query.EQ("areaId", areaId.ToString());

            List<BsonDocument> allDocList = dataOp.FindAllByQuery(tbName, query).ToList();

            int allCount = allDocList.Count();

            List<Hashtable> retList = new List<Hashtable>();

            foreach (var tempDoc in allDocList)
            {
                bool XH_ProductDev_City = Yinhe.ProcessingCenter.Permissions.AuthManage._().CheckRight("XH_ProductDev_City", tempDoc.Text("cityId"));
                if (!XH_ProductDev_City)
                    continue;

                tempDoc.Add("allCount", allCount);
                tempDoc.Remove("_id");

                retList.Add(tempDoc.ToHashtable());
            }

            return this.Json(retList, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 取出一级开发下的nodeType的任务列表
        /// </summary>
        /// <returns></returns>
        public ActionResult GetTaskListByNodeTypeId()
        {
            string taskId = PageReq.GetForm("taskId");
            string projRank = PageReq.GetForm("projRank");
            string isLink = PageReq.GetForm("isLink");
            List<BsonDocument> taskDocList = new List<BsonDocument>();
            var task = dataOp.FindOneByKeyVal("XH_DesignManage_Task", "taskId", taskId);
            if (string.IsNullOrEmpty(projRank) && string.IsNullOrEmpty(isLink))
            {
                taskDocList.Add(task);
            }
            else
            {

                var project = dataOp.FindOneByKeyVal("XH_DesignManage_Project", "projId", task.Text("projId"));

                var projModel = dataOp.FindOneByQuery("XH_DesignManage_Project", Query.And(Query.EQ("engId", project.Text("engId")), Query.EQ("projRank", projRank)));

                taskDocList = dataOp.FindAllByQuery("XH_DesignManage_Task", Query.And(Query.EQ("projId", projModel.Text("projId")), Query.EQ("isLink", isLink))).ToList();

            }
            List<Hashtable> retList = new List<Hashtable>();

            foreach (var tempDoc in taskDocList)
            {
                tempDoc.Remove("_id");

                retList.Add(tempDoc.ToHashtable());
            }



            return Json(retList);
        }

        public ActionResult GetTaskListByPlanTemplate()
        {
            string isLink = PageReq.GetForm("isLink");

            var plan = dataOp.FindOneByQuery("XH_DesignManage_Plan", Query.And(Query.EQ("isExpTemplate", "1"), Query.EQ("planType", "1")));

            List<BsonDocument> taskDocList = new List<BsonDocument>();

            taskDocList = dataOp.FindAllByQuery("XH_DesignManage_Task", Query.And(Query.EQ("planId", plan.Text("planId")), Query.EQ("isLink", isLink))).ToList();

            List<Hashtable> retList = new List<Hashtable>();

            foreach (var tempDoc in taskDocList)
            {
                tempDoc.Remove("_id");

                retList.Add(tempDoc.ToHashtable());
            }

            return Json(retList);
        }

        #endregion

        #region 计划编制

        /// <summary>
        /// 保存项目信息
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SavePlanInfo(FormCollection saveForm)
        {
            string tbName = saveForm["tbName"] != null ? saveForm["tbName"] : "";
            string queryStr = saveForm["queryStr"] != null ? saveForm["queryStr"] : "";
            string dataStr = saveForm["dataStr"] != null ? saveForm["dataStr"] : "";

            if (dataStr.Trim() == "")
            {
                foreach (var tempKey in saveForm.AllKeys.Where(c => c != "managerIds"))
                {
                    if (tempKey == "tbName" || tempKey == "queryStr" || tempKey.Contains("fileList[") || tempKey.Contains("param.") || tempKey.Contains("supRels") || tempKey.Contains("teamRels")) continue;

                    dataStr += string.Format("{0}={1}&", tempKey, saveForm[tempKey]);
                }
            }

            InvokeResult result = dataOp.Save(tbName, queryStr, dataStr);

            if (result.Status == Status.Successful)
            {
                var managerIds = PageReq.GetFormInt("managerIds");
                var planId = result.BsonInfo.Text("planId");
                if (saveForm["showIndex"] == "1")
                {//增加是否首页显示计划
                    List<StorageData> otherPlanList = GetProjPlan(result.BsonInfo.Int("planId"), result.BsonInfo.Int("projId"));
                    dataOp.BatchSaveStorageData(otherPlanList);
                }
                var existManagerQuery = dataOp.FindAllByKeyVal("XH_DesignManage_PlanManager", "planId", planId);
                //result.BsonInfo.ChildBsonList("XH_DesignManage_PlanManager");
                var DeleteManagerQuery = existManagerQuery.Where(c => c.Int("userId") != managerIds);
                var updateManagerQuery = existManagerQuery.Where(c => c.Int("userId") == managerIds);
                if (DeleteManagerQuery.Count() > 0)
                {
                    dataOp.QuickDelete("XH_DesignManage_PlanManager", DeleteManagerQuery.ToList());
                }
                if (updateManagerQuery.Count() <= 0)
                {
                    dataOp.Insert("XH_DesignManage_PlanManager", string.Format("planId={0}&userId={1}", planId, managerIds));

                }
            }

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }
        /// <summary>
        /// 返回当期项目其他计划修改首页显示字段
        /// </summary>
        /// <param name="planId"></param>
        /// <param name="projId"></param>
        /// <returns></returns>
        public List<StorageData> GetProjPlan(int planId, int projId)
        {
            List<BsonDocument> planList = dataOp.FindAllByQuery("XH_DesignManage_Plan", Query.And(Query.EQ("projId", projId.ToString()), Query.NE("planId", planId.ToString()))).ToList();
            List<StorageData> data = new List<StorageData>();
            for (int i = 0; i < planList.Count(); i++)
            {
                if (planList[i].ContainsColumn("showIndex"))
                {
                    planList[i]["showIndex"] = "0";//0 代表不在首页显示
                }
                else
                {
                    planList[i].Add("showIndex", "0");
                }
                StorageData tempData = new StorageData();
                tempData.Type = StorageType.Update;
                tempData.Query = Query.EQ("planId", planList[i].String("planId"));
                tempData.Document = planList[i];
                tempData.Name = "XH_DesignManage_Plan";
                data.Add(tempData);
            }
            return data;
        }


        #endregion

        #region 计划任务
        /// <summary>
        /// 根据分项Id获取任务列表
        /// </summary>
        /// <returns></returns>
        public ActionResult TaskList()
        {
            int planId = PageReq.GetParamInt("planId");     //计划Id
            int levelId = PageReq.GetParamInt("levelId");
            BsonDocument planEntity = dataOp.FindOneByKeyVal("XH_DesignManage_Plan", "planId", planId.ToString());             //获取计划
            if (planEntity == null)
            {
                planEntity = new BsonDocument();
            }
            List<BsonDocument> allProjTaskList = dataOp.FindAllByKeyVal("XH_DesignManage_Task", "projId", planEntity.Text("projId").ToString()).ToList();
            List<BsonDocument> allTaskList = dataOp.FindAllByKeyVal("XH_DesignManage_Task", "planId", planId.ToString()).ToList();

            BsonDocument rootTask = allTaskList.Where(m => m.Int("nodePid") == 0).FirstOrDefault();   //内置的根任务

            List<BsonDocument> taskList = new List<BsonDocument>();
            //所有需要展示的任务
            if (levelId != 0)
            {
                taskList = allTaskList.Where(m => m.Int("levelId") == levelId && m.Int("nodePid") != 0).ToList();
            }
            else
            {
                taskList = allTaskList.Where(m => m.Int("nodePid") != 0).ToList();
            }
            List<string> taskIdList = taskList.Select(t => t.String("taskId")).ToList();     //所有任务的Id列表
            List<int> allProjTaskIdList = allProjTaskList.Select(t => t.Int("taskId")).ToList();     //所有任务的Id列表

            List<BsonDocument> allProjFileList = dataOp.FindAllByQueryStr("FileRelation", "tableName=XH_DesignManage_Task&fileObjId=31").Where(c => allProjTaskIdList.Contains(c.Int("keyValue"))).ToList();
            List<BsonDocument> allManagerList = dataOp.FindAllByKeyValList("XH_DesignManage_TaskManager", "taskId", taskIdList).ToList(); //找出所有任务管理人

            List<string> allUserIdList = taskList.Select(t => t.String("createUser")).ToList();
            allUserIdList.AddRange(taskList.Select(t => t.String("updateUser")));
            allUserIdList.AddRange(allManagerList.Select(t => t.String("userId")));

            List<BsonDocument> allUserList = dataOp.FindAllByKeyValList("SysUser", "userId", allUserIdList).ToList();    //获取所有用到的相关人员

            //List<TaskFileDefaultValue> taskFIleDefaultList = plan.TaskFileDefaultValues != null ? plan.TaskFileDefaultValues.ToList() : new List<TaskFileDefaultValue>();   //任务文件信息

            var relQuery = Query.Or(Query.In("preTaskId", TypeConvert.StringListToBsonValueList(taskIdList)), Query.In("sucTaskId", TypeConvert.StringListToBsonValueList(taskIdList)));

            List<BsonDocument> taskRelList = dataOp.FindAllByQuery("XH_DesignManage_TaskRelation", relQuery).ToList();     //获取所有技术关系

            List<string> conDiagNodeIdList = taskList.Select(t => t.String("diagramId")).Distinct().ToList();                 //获取与脉络图的所有关系

            List<BsonDocument> contextDiagramList = dataOp.FindAllByKeyValList("XH_DesignManage_ContextDiagram", "diagramId", conDiagNodeIdList).ToList();   //所有用到的脉络图节点

            List<string> pointIdList = taskList.Select(t => t.String("pointId")).Distinct().ToList();   //获取与地铁图的所有关系

            List<BsonDocument> decisionPointList = dataOp.FindAllByKeyValList("XH_DesignManage_DecisionPoint", "pointId", pointIdList).ToList();   //所有用到的地铁图

            List<BsonDocument> allFlowRelList = dataOp.FindAllByKeyValList("XH_DesignManage_TaskBusFlow", "taskId", taskIdList).ToList();       //所有任务流程关联

            #region ZHHY获取多审批任务流程关联
            var allProjTasks=new List<BsonDocument>();
            var allClasses=new List<BsonDocument>();
            var allClassTasks=new List<BsonDocument>();
            var allClassTaskBusFlow=new List<BsonDocument>();
            if (SysAppConfig.CustomerCode == CustomerCode.ZHHY)
            {
                //获取当前计划下所有多审批任务
                var allMultiAppTasks = taskList.Where(i => i.Int("nodeTypeId") == (int)ConcernNodeType.MultiApproval).ToList();
                if (allMultiAppTasks.Count > 0)
                {
                    allProjTasks = dataOp.FindAllByQuery("XH_DesignManage_Task", Query.EQ("projId", planEntity.Text("projId"))).ToList();
                    //获取这些任务所关联的所有子任务组
                    allClasses = dataOp.FindAllByQuery("TaskClass",
                            Query.In("taskId", allMultiAppTasks.Select(i => i.GetValue("taskId", string.Empty)))
                        ).ToList();
                    //获取这些任务组所包含的所有任务
                    allClassTasks = dataOp.FindAllByQuery("XH_DesignManage_Task",
                        Query.In("taskClassId", allClasses.Select(p => (BsonValue)p.Text("taskClassId")))
                    ).SetFields("taskClassId", "taskId").ToList();
                    //这些组任务的审批关联
                    allClassTaskBusFlow = dataOp.FindAllByQuery("XH_DesignManage_TaskBusFlow",
                        Query.In("taskId", allClassTasks.Select(p => (BsonValue)p.Text("taskId")))
                    ).ToList();
                    allFlowRelList.AddRange(allClassTaskBusFlow);
                }
            }
            #endregion

            #region 获取任务的index值
            int index = 1;

            Dictionary<int, int> dicTaskIndex = new Dictionary<int, int>(); //任务对应输出的index值,用于技术关系key为任务Id,value为index值

            foreach (var task in taskList.OrderBy(t => t.String("nodeKey")))
            {
                if (!dicTaskIndex.ContainsKey(task.Int("taskId")))
                {
                    dicTaskIndex.Add(task.Int("taskId"), index);
                    index++;
                }
            }
            #endregion

            List<object> taskListJson = new List<object>();
            var hitAllTaskQuery = taskList.OrderBy(t => t.String("nodeKey")).AsQueryable();
            if (planEntity.Int("isContractPlan") == 1)//费用支付计划不展示只展示前3级 包括根节点
            {
                hitAllTaskQuery = hitAllTaskQuery.Where(c => c.Int("nodeLevel") <= 3);
            }
            hitAllTaskQuery = hitAllTaskQuery.Where(c => c.Text("hiddenTask") != "1"); //过滤弘扬的隐藏子任务
            foreach (var task in hitAllTaskQuery)
            {
                #region  获取人员信息
                //计划分解人
                BsonDocument spliterManager = allManagerList.Where(m => m.Int("taskId") == task.Int("taskId") && m.Int("type") == (int)TaskManagerType.PlanSpliter).FirstOrDefault();
                BsonDocument spliterUser = allUserList.Where(t => t.Int("userId") == (spliterManager != null ? spliterManager.Int("userId") : -1)).FirstOrDefault();

                //任务负责人
                BsonDocument ownerManager = allManagerList.Where(m => m.Int("taskId") == task.Int("taskId") && m.Int("type") == (int)TaskManagerType.TaskOwner).FirstOrDefault();
                BsonDocument ownerUser = allUserList.Where(t => t.Int("userId") == (ownerManager != null ? ownerManager.Int("userId") : -1)).FirstOrDefault();

                string orgId = "";
                string orgName = "";
                #region 获取人所在的部门
                if (ownerUser != null)
                {
                    var userOrgPost = dataOp.FindOneByKeyVal("UserOrgPost", "userId", ownerUser.Text("userId").ToString());
                    if (userOrgPost != null)
                    {
                        var orgPost = dataOp.FindOneByKeyVal("OrgPost", "postId", userOrgPost.Text("postId"));
                        if (orgPost != null)
                        {
                            var org = dataOp.FindOneByKeyVal("Organization", "orgId", orgPost.Text("orgId"));
                            if (org != null)
                            {
                                orgId = org.Text("orgId");
                                orgName = org.Text("name");
                            }
                        }
                    }
                }
                #endregion
                //任务参与人
                List<BsonDocument> joinerList = allManagerList.Where(m => m.Int("taskId") == task.Int("taskId") && m.Int("type") == (int)TaskManagerType.TaskJoiner).ToList();

                string joinerName = "";

                foreach (var joiner in joinerList)
                {
                    BsonDocument joinerUser = allUserList.Where(t => t.Int("userId") == joiner.Int("userId")).FirstOrDefault();

                    if (joinerName == "") joinerName += joinerUser.String("name");
                    else joinerName += "," + joinerUser.String("name");
                }

                BsonDocument createUser = allUserList.Where(t => t.Int("userId") == task.Int("createUser")).FirstOrDefault();
                #endregion

                #region 获取任务文档信息
                //List<int> sysprofIdList = taskFIleDefaultList.Count > 0 ? taskFIleDefaultList.Where(c => c.taskId == task.taskId && c.sysProfId.HasValue).Select(c => c.sysProfId.Value).Distinct().ToList() : new List<int>();

                //List<int> sysstageIdList = taskFIleDefaultList.Count > 0 ? taskFIleDefaultList.Where(c => c.taskId == task.taskId && c.sysStageId.HasValue).Select(c => c.sysStageId.Value).Distinct().ToList() : new List<int>();
                #endregion

                #region 获取技术关系信息
                var tempRelList = taskRelList.Where(t => t.Int("sucTaskId") == task.Int("taskId")).ToList();

                List<object> relationList = new List<object>();

                if (tempRelList.Count > 0)
                {
                    foreach (var tempRel in tempRelList)
                    {
                        int relTaskId = tempRel.Int("preTaskId") == task.Int("taskId") ? tempRel.Int("sucTaskId") : tempRel.Int("preTaskId");

                        var tempRelTask = allProjTaskList.Where(t => t.Int("taskId") == relTaskId).FirstOrDefault();
                        var curPreTaskStatus = tempRelTask.Int("status", 2);
                        if (curPreTaskStatus < (int)TaskStatus.NotStarted)
                        {
                            curPreTaskStatus = (int)TaskStatus.NotStarted;
                        }
                        var curPreStatusName = EnumDescription.GetFieldText((TaskStatus)curPreTaskStatus);
                        double preDelayDay = 0;

                        if (curPreTaskStatus < (int)TaskStatus.Completed && tempRelTask.Date("curEndData") != DateTime.MinValue && tempRelTask.Date("curEndData") < DateTime.Now)//延迟结束
                        {
                            curPreTaskStatus = -1;
                            curPreStatusName = "已延迟";
                            preDelayDay = (DateTime.Now - tempRelTask.Date("curEndData")).TotalDays;
                        }
                        // referType 1：FS（结束-开始） 2：SS（开始-开始） 3：FF（结束-结束） 4：SF（开始-结束）
                        if (tempRelTask != null)
                        {
                            relationList.Add(new
                            {
                                relationId = tempRel.Int("relId"),
                                relTaskId = relTaskId,
                                //relTaskIndex = dicTaskIndex[relTaskId],
                                relTaskName = tempRelTask.Text("name"),
                                relTaskType = tempRel.Int("referType") == 1 ? "FS" : (tempRel.Int("referType") == 2 ? "SS" : (tempRel.Int("referType") == 3 ? "FF" : (tempRel.Int("referType") == 4 ? "SF" : ""))),
                                relTaskStateName = curPreStatusName,
                                relTaskFileCount = allProjFileList.Where(c => c.Int("keyValue") == relTaskId).Count()
                            });
                        }
                    }
                }
                #endregion

                #region 获取其他相关信息
                BsonDocument conDiag = contextDiagramList.Where(t => t.Int("diagramId") == task.Int("diagramId")).FirstOrDefault(); //脉络图
                BsonDocument tempPoint = decisionPointList.Where(t => t.Int("pointId") == task.Int("pointId")).FirstOrDefault(); //地铁图

                List<BsonDocument> flowRelList = allFlowRelList.Where(t => t.Int("taskId") == task.Int("taskId")).ToList();     //任务对应流程关联

                if (SysAppConfig.CustomerCode == CustomerCode.ZHHY)
                {
                    //如果该任务是多审批任务
                    if (task.Int("nodeTypeId") == (int)ConcernNodeType.MultiApproval)
                    {
                        var tempClasses = allClasses.Where(i => i.Int("taskId") == task.Int("taskId")).ToList();
                        var tempClassIds=tempClasses.Select(i=>i.Int("taskClassId")).ToList();
                        var tempClassTasks = allClassTasks.Where(i => tempClassIds.Contains(i.Int("taskClassId"))).ToList();
                        var tempClassTaskIds = tempClassTasks.Select(i => i.Int("taskId")).ToList();
                        var tempClassTaskBusFlow = allClassTaskBusFlow.Where(i => tempClassTaskIds.Contains(i.Int("taskId"))).ToList();
                        flowRelList.AddRange(tempClassTaskBusFlow);
                    }
                }

                #endregion

                //查找任务状态
                var curTaskStatus = task.Int("status", 2);
                var curStatusName = EnumDescription.GetFieldText((TaskStatus)task.Int("status"));
                double delayDay = 0;
                //if (curTaskStatus < (int)TaskStatus.NotStarted && task.Date("curStartData") != DateTime.MinValue && task.Date("curStartData") < DateTime.Now)//延迟开始
                //{
                //    curTaskStatus = -1;
                //    curStatusName = "已延迟";
                //    delayDay = (DateTime.Now - task.Date("curStartData")).TotalDays;
                //}
                if (curTaskStatus < (int)TaskStatus.Completed && task.Date("curEndData") != DateTime.MinValue && task.Date("curEndData") < DateTime.Now)//延迟结束
                {
                    curTaskStatus = -1;
                    curStatusName = "已延迟";
                    delayDay = (DateTime.Now - task.Date("curEndData")).TotalDays;
                }
                var childTaskActualPaymentAmount = taskList
                    .Where(i => i.Int("nodeTypeId") != (int)ConcernNodeType.ContractNode && i.Int("nodeTypeId") != (int)ConcernNodeType.FeePayment)
                    .Where(c => c.Text("nodeKey").StartsWith(task.Text("nodeKey"))).Sum(c => c.Double("actualPaymentAmount"));
                #region 添加到任务信息列表中
                taskListJson.Add(new
                {
                    taskId = task.Int("taskId"),
                    nodePid = task.Int("nodePid") == rootTask.Int("taskId") ? 0 : task.Int("nodePid"),
                    name = task.String("name"),
                    levelId = task.Int("levelId", -1),
                    levelName = task.SourceBsonField("levelId", "name"),

                    nodeTypeId = task.Int("nodeTypeId", -1),
                    nodeName = task.SourceBsonField("nodeTypeId", "name"),
                    ownerName = ownerUser != null ? ownerUser.String("name") : "",
                    ownerUserId = ownerUser != null ? ownerUser.Int("userId") : -1,
                    ownerProfId = ownerManager != null ? ownerManager.Int("profId") : -1,
                    startDate = task.Date("curStartData") != DateTime.MinValue ? task.Date("curStartData").ToString("yyyy-MM-dd") : "",
                    endDate = task.Date("curEndData") != DateTime.MinValue ? task.Date("curEndData").ToString("yyyy-MM-dd") : "",
                    period = task.String("period"),
                    status = curTaskStatus,
                    statusName = curStatusName,
                    remark = task.String("remark"),
                    approvalDepart = task.String("approvalDepart"),
                    startDateBg = task.String("curStartDataBgColor"),
                    endDateBg = task.String("curEndDataBgColor"),
                    factStDate = task.Date("factStartDate") != DateTime.MinValue ? task.Date("factStartDate").ToString("yyyy-MM-dd") : "",
                    factEdDate = task.Date("factEndDate") != DateTime.MinValue ? task.Date("factEndDate").ToString("yyyy-MM-dd") : "",
                    taskRelations = relationList,
                    operateStatus = task.Int("operateStatus"),
                    canPassOperate = task.Int("canPassOperate"),
                    needSplit = task.Int("needSplit"),
                    spliterName = spliterUser != null ? spliterUser.String("name") : "",
                    joinerName = joinerName,
                    createrName = createUser != null ? createUser.String("name") : "",
                    relConDiagId = task.Int("diagramId"),          //关联的脉络图节点Id 0为没有关联
                    relConDiagName = conDiag != null ? conDiag.String("name") : "",                //关联的脉络图节点名称
                    strPointId = task.Int("pointId"),
                    pointName = tempPoint.String("name"),
                    //valueId = taskValueRelationBll.FindValueObjByTaskId(task.taskId).valueId,
                    //valueName = taskValueRelationBll.FindValueObjByTaskId(task.taskId).name,
                    hasApproval = flowRelList.Count > 0 ? true : false,
                    isKeyTask = task.Int("keyTask"),
                    hasCIList = task.String("hasCIList"),
                    hasPush=task.Text("hasPush"),
                    strTextPId = tempPoint.Text("textPId"),
                    orgId = orgId,
                    orgName = orgName,
                    delayDay = (int)delayDay,
                    designUnitName = task.Text("designUnitName"),
                    totalContractAmount = task.Double("totalContractAmount"),
                    payedContractAmount = childTaskActualPaymentAmount != 0 ? string.Format("{0}万元", childTaskActualPaymentAmount) : "",
                    unpayedContractAmount = (task.Double("totalContractAmount") - childTaskActualPaymentAmount) != 0 ? string.Format("{0}万元", task.Double("totalContractAmount") - childTaskActualPaymentAmount) : "",
                    //sysprofids = ListToString(sysprofIdList),
                    //sysstageids = ListToString(sysstageIdList),
                    //completedWork = task.TaskToDos.Where(m => m.TodoTask.stateId == (int)WorkStatus.Completed).Count(),
                    //unCompletedWork = task.TaskToDos.Where(m => m.TodoTask.stateId != (int)WorkStatus.Completed).Count(),
                    //fileCount = 0,
                });
                #endregion
            }

            //新增计算公式
            List<BsonDocument> formulaList = dataOp.FindAllByKeyValList("XH_DesignManage_TaskFormula", "taskId", taskIdList).ToList();
            var jsonFormulaList = formulaList.Select(m => m.String("formulaParam")).ToList();
            var jsonResult = new
            {
                jsonGroupTaskList = taskListJson,
                jsonFormulaList = jsonFormulaList
            };

            return this.Json(jsonResult, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 快速创建任务
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult QuickSaveTaskContractFree()
        {
            InvokeResult result = new InvokeResult();
            PageJson json = new PageJson();

            #region 获取原始数据
            int curTaskId = PageReq.GetFormInt("taskId");        //当前任务的上一个任务
            int planId = PageReq.GetFormInt("planId");              //当前任务所属计划
            int projId = PageReq.GetFormInt("projId");              //当前任务所属项目
            int isExpLib = PageReq.GetFormInt("isExpLib");          //当前计划是否经验库
            int nodePid = PageReq.GetFormInt("nodePid");            //当前任务的父级任务
            var taskId = PageReq.GetForm("editTaskId");
            string expectPaymentAmount = PageReq.GetForm("expectPaymentAmount");            //本次应付金额
            string actualPaymentAmount = PageReq.GetForm("actualPaymentAmount");            //实际应付金额
            string paymentRatio = PageReq.GetForm("paymentRatio");            //支付比例
            string payeeCompanyName = PageReq.GetForm("payeeCompanyName");            //收款单位
            string remark = PageReq.GetForm("remark");            //支付理由

            BsonDocument plan = dataOp.FindOneByKeyVal("XH_DesignManage_Plan", "planId", planId.ToString()); //计划

            List<BsonDocument> taskList = dataOp.FindAllByKeyVal("XH_DesignManage_Task", "planId", planId.ToString()).ToList();      //所有需要展示的任务

            BsonDocument rootTask = dataOp.FindOneByQueryStr("XH_DesignManage_Task", "planId=" + planId + "&nodePid=0");   //内置的根任务

            BsonDocument editTask = dataOp.FindOneByKeyVal("XH_DesignManage_Task", "taskId", taskId);   //当前任务节点

            if ((isExpLib == 0) && (plan.Int("status") == (int)ProjectPlanStatus.Completed))
            {
                json.Success = false;
                json.Message = "计划已完成不能创建任务";
                return Json(json);
            }
            #endregion
            if (editTask == null)
            {
                #region 基本信息
                BsonDocument taskInfo = new BsonDocument();

                taskInfo.Add("name", "请填写任务名称");         //名称
                taskInfo.Add("planId", planId.ToString());      //所属计划
                taskInfo.Add("projId", projId.ToString());      //所属项目
                taskInfo.Add("progress", "0");
                taskInfo.Add("levelId", " 2");                  //关注级别
                taskInfo.Add("curEndData", plan.Date("endData") != DateTime.MinValue ? plan.String("endData") : DateTime.Now.ToString("yyyy-MM-dd"));  //任务计划结束日期
                taskInfo.Add("status", ((int)TaskStatus.NotStarted).ToString());    //任务状态

                taskInfo.Add("expectPaymentAmount", expectPaymentAmount);
                taskInfo.Add("actualPaymentAmount", actualPaymentAmount);
                taskInfo.Add("paymentRatio", paymentRatio);
                taskInfo.Add("payeeCompanyName", payeeCompanyName);
                taskInfo.Add("remark", remark);
                if (nodePid != 0)
                {
                    BsonDocument parentTask = dataOp.FindOneByKeyVal("XH_DesignManage_Task", "taskId", nodePid.ToString());

                    taskInfo.Add("stageId", parentTask.String("stageId"));
                    taskInfo.Add("nodePid", nodePid.ToString());
                }
                else
                {
                    taskInfo.Add("stageId", rootTask.String("stageId"));
                    taskInfo.Add("nodePid", rootTask.String("taskId"));
                }
                #endregion

                if (taskInfo.Int("nodePid") <= 0)
                {
                    json.Success = false;
                    json.Message = "计划异常，请刷新页面重试";
                    return Json(json);
                }

                result = dataOp.Insert("XH_DesignManage_Task", taskInfo);
            }
            else
            {
                BsonDocument updateBson = new BsonDocument();

                updateBson.Add("expectPaymentAmount", expectPaymentAmount);
                updateBson.Add("actualPaymentAmount", actualPaymentAmount);
                updateBson.Add("paymentRatio", paymentRatio);
                updateBson.Add("payeeCompanyName", payeeCompanyName);
                updateBson.Add("remark", remark);
                result = dataOp.Update(editTask, updateBson);
            }
            json = TypeConvert.InvokeResultToPageJson(result);

            if (result.Status == Status.Successful)
            {
                BsonDocument task = result.BsonInfo;

                #region 移动任务
                if (curTaskId != 0)
                {
                    dataOp.Move("XH_DesignManage_Task", task.String("taskId"), curTaskId.ToString(), "next");
                }
                #endregion

                #region 添加返回任务信息
                object taskJson = new
                {
                    taskId = task.Int("taskId"),
                    nodePid = task.Int("nodePid") == rootTask.Int("taskId") ? 0 : task.Int("nodePid"),
                    name = task.String("name"),
                    levelId = task.Int("levelId", -1),
                    levelName = "",
                    ownerName = "",
                    ownerUserId = -1,
                    ownerProfId = -1,
                    startDate = "",
                    endDate = task.Date("curEndData") != DateTime.MinValue ? task.Date("curEndData").ToString("yyyy-MM-dd") : "",
                    period = "",
                    status = task.Int("status"),
                    statusName = EnumDescription.GetFieldText((TaskStatus)task.Int("status")),
                    remark = "",
                    approvalDepart = "",
                    startDateBg = "",
                    endDateBg = "",
                    factStDate = "",
                    factEdDate = "",
                    taskRelations = new List<object>(),
                    operateStatus = 0,
                    canPassOperate = 0,
                    needSplit = 0,
                    spliterName = "",
                    joinerName = "",
                    createrName = dataOp.GetCurrentUserName(),
                };

                json.htInfo.Add("taskInfo", taskJson);
                #endregion
            }

            return Json(json);
        }

        /// <summary>
        /// 快速创建任务
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult QuickCreateTask()
        {
            InvokeResult result = new InvokeResult();
            PageJson json = new PageJson();

            #region 获取原始数据
            int curTaskId = PageReq.GetFormInt("curTaskId");        //当前任务的上一个任务
            int planId = PageReq.GetFormInt("planId");              //当前任务所属计划
            int projId = PageReq.GetFormInt("projId");              //当前任务所属项目
            int isExpLib = PageReq.GetFormInt("isExpLib");          //当前计划是否经验库
            int nodePid = PageReq.GetFormInt("nodePid");            //当前任务的父级任务

            BsonDocument plan = dataOp.FindOneByKeyVal("XH_DesignManage_Plan", "planId", planId.ToString()); //计划

            List<BsonDocument> taskList = dataOp.FindAllByKeyVal("XH_DesignManage_Task", "planId", planId.ToString()).ToList();      //所有需要展示的任务

            BsonDocument rootTask = dataOp.FindOneByQueryStr("XH_DesignManage_Task", "planId=" + planId + "&nodePid=0");   //内置的根任务

            if ((isExpLib == 0) && (plan.Int("status") == (int)ProjectPlanStatus.Completed))
            {
                json.Success = false;
                json.Message = "计划已完成不能创建任务";
                return Json(json);
            }
            #endregion

            #region 基本信息
            BsonDocument taskInfo = new BsonDocument();

            taskInfo.Add("name", "请填写任务名称");         //名称
            taskInfo.Add("planId", planId.ToString());      //所属计划
            taskInfo.Add("projId", projId.ToString());      //所属项目
            taskInfo.Add("progress", "0");
            taskInfo.Add("levelId ", " 2");                  //关注级别
            taskInfo.Add("curEndData ", plan.Date("endData") != DateTime.MinValue ? plan.String("endData") : DateTime.Now.ToString("yyyy-MM-dd"));  //任务计划结束日期
            taskInfo.Add("status", ((int)TaskStatus.NotStarted).ToString());    //任务状态

            if (nodePid != 0)
            {
                BsonDocument parentTask = dataOp.FindOneByKeyVal("XH_DesignManage_Task", "taskId", nodePid.ToString());

                taskInfo.Add("stageId", parentTask.String("stageId"));
                taskInfo.Add("nodePid", nodePid.ToString());
            }
            else
            {
                taskInfo.Add("stageId", rootTask.String("stageId"));
                taskInfo.Add("nodePid", rootTask.String("taskId"));
            }
            #endregion

            if (taskInfo.Int("nodePid") <= 0)
            {
                json.Success = false;
                json.Message = "计划异常，请刷新页面重试";
                return Json(json);
            }

            result = dataOp.Insert("XH_DesignManage_Task", taskInfo);

            json = TypeConvert.InvokeResultToPageJson(result);

            if (result.Status == Status.Successful)
            {
                BsonDocument task = result.BsonInfo;

                #region 移动任务
                if (curTaskId != 0)
                {
                    dataOp.Move("XH_DesignManage_Task", task.String("taskId"), curTaskId.ToString(), "next");
                }
                #endregion

                #region 添加返回任务信息
                object taskJson = new
                {
                    taskId = task.Int("taskId"),
                    nodePid = task.Int("nodePid") == rootTask.Int("taskId") ? 0 : task.Int("nodePid"),
                    name = task.String("name"),
                    levelId = task.Int("levelId", -1),
                    levelName = "",
                    ownerName = "",
                    ownerUserId = -1,
                    ownerProfId = -1,
                    startDate = "",
                    endDate = task.Date("curEndData") != DateTime.MinValue ? task.Date("curEndData").ToString("yyyy-MM-dd") : "",
                    period = "",
                    status = task.Int("status"),
                    statusName = EnumDescription.GetFieldText((TaskStatus)task.Int("status")),
                    remark = "",
                    approvalDepart = "",
                    startDateBg = "",
                    endDateBg = "",
                    factStDate = "",
                    factEdDate = "",
                    taskRelations = new List<object>(),
                    operateStatus = 0,
                    canPassOperate = 0,
                    needSplit = 0,
                    spliterName = "",
                    joinerName = "",
                    createrName = dataOp.GetCurrentUserName(),
                };

                json.htInfo.Add("taskInfo", taskJson);
                #endregion
            }

            return Json(json);
        }


        #region 公式

        private void PatchSyncFormulaDate(string strOperate, string projId)
        {

            var taskList = dataOp.FindAllByKeyVal("XH_DesignManage_Task", "projId", projId).ToList();
            var iFormula = new SEDateSyncFormula(taskList);
            iFormula.BeginSyncFormulaDate(strOperate, projId);
            iFormula.ChangeSubmit();
        }
        /// <summary>
        /// 设置公式
        /// </summary>
        /// <param name="bllFormula"></param>
        /// <param name="strOperate"></param>
        /// <param name="projId"></param>
        private void PatchSyncFormula(string strOperate, string projId, int taskId, string formula)
        {

            var taskList = dataOp.FindAllByKeyVal("XH_DesignManage_Task", "projId", projId).ToList();
            var iFormula = new SEDateSyncFormula(taskList);
            iFormula.AnalysisFormula(taskId, formula.ToUpper());
            iFormula.BeginSyncFormulaDate(strOperate, projId);
            iFormula.ChangeSubmit();
        }


        /// <summary>
        /// 递归同步日期
        /// </summary>
        /// <param name="bllFormula"></param>
        /// <param name="taskId"></param>
        private void SyncFormulaDate(TaskFormulaBll bllFormula, string strOperate)
        {
            List<BsonDocument> formulaList = dataOp.FindAll("XH_DesignManage_TaskFormula").Where(m => m.String("relTaskId").Contains(strOperate)).ToList();

            foreach (var formula in formulaList)
            {
                if (!String.IsNullOrEmpty(formula.String("formulaClass")))
                {
                    ITaskFormula iFormula = TaskFormulaFactory.Instance.Create(formula.String("formulaClass"));
                    if (iFormula != null)
                    {
                        var strResult = iFormula.AnalysisFormula(formula.Int("taskId"), formula.String("formulaParam"));
                        if (!String.IsNullOrEmpty(strResult))
                        {
                            SyncFormulaDate(bllFormula, strResult + formula.String("taskId"));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 添加、编辑公式
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult FormulaEdit()
        {
            PageJson json = new PageJson();
            InvokeResult result = new InvokeResult();

            int taskId = PageReq.GetFormInt("taskId");
            string type = PageReq.GetForm("type");
            var formula = PageReq.GetForm("formula");

            if (String.IsNullOrEmpty(formula))
            {
                json.Success = false;
                json.Message = "公式不能为空";
                return this.Json(json);
            }

            var taskObj = dataOp.FindOneByKeyVal("XH_DesignManage_Task", "taskId", taskId.ToString());
            if (taskObj == null)
            {
                json.Success = false;
                json.Message = "传入参数有误请刷新后重试";
                return this.Json(json);
            }

            var taskList = dataOp.FindAllByKeyVal("XH_DesignManage_Task", "projId", taskObj.Text("projId")).ToList();
            var iFormula = new SEDateSyncFormula(taskList);
            FormulaObject formulaObj = new FormulaObject(formula);
            formulaObj.taskList = taskList;
            formulaObj.ConvertExp();
            if (!iFormula.IsClosedLoop(formulaObj))
            {
                json.Success = false;
                json.Message = "循环引用公式，无法添加";
                return this.Json(json);
            }
            BsonDocument taskFormula = new BsonDocument();

            taskFormula.Add("taskId", taskId.ToString());
            taskFormula.Add("formulaParam", formula.ToUpper());
            taskFormula.Add("formulaClass", "Yinhe.ProcessingCenter.DesignManage.TaskFormula.SEDateSyncFormula");
            taskFormula.Add("relTaskId", formulaObj.StrRelTaskId);

            var oldFormula = dataOp.FindAllByKeyVal("XH_DesignManage_TaskFormula", "taskId", taskId.ToString()).Where(t => t.String("formulaParam").StartsWith(type.ToUpper())).FirstOrDefault();

            if (oldFormula == null)
            {
                result = dataOp.Insert("XH_DesignManage_TaskFormula", taskFormula);

            }
            else
            {
                result = dataOp.Update("XH_DesignManage_TaskFormula", Query.EQ("formulaId", oldFormula.String("formulaId")), new BsonDocument().Add("relTaskId", formulaObj.StrRelTaskId)
                                                                                                                                             .Add("formulaParam", formula.ToUpper()));
            }

            if (result.Status == Status.Successful)
            {
                //ITaskFormula iFormula = TaskFormulaFactory.Instance.Create(taskFormula.String("formulaClass"));
                //if (iFormula != null)
                //{
                //    iFormula.AnalysisFormula(taskId, formula.ToUpper());
                //}
                //SyncFormulaDate(bllFormula, formulaObj.OperDateType + formulaObj.TaskId);
                //  PatchSyncFormula(bllFormula, formulaObj.OperDateType + formulaObj.TaskId, taskObj.Text("projId"), taskId, formula.ToUpper());
                iFormula.PatchAnalysisFormula(taskId, formula.ToUpper());
                iFormula.BeginSyncFormulaDate(formulaObj.OperDateType + formulaObj.TaskId, taskObj.Text("projId"));
                iFormula.ChangeSubmit();

            }

            json = TypeConvert.InvokeResultToPageJson(result);

            return this.Json(json);
        }

        /// <summary>
        /// 清空公式
        /// </summary>
        /// <returns></returns>
        public JsonResult DeleteFormula()
        {
            int taskId = PageReq.GetFormInt("taskId");
            string type = PageReq.GetForm("type");

            var formula = dataOp.FindAllByKeyVal("XH_DesignManage_TaskFormula", "taskId", taskId.ToString()).Where(t => t.String("formulaParam").StartsWith(type.ToUpper())).FirstOrDefault();

            var deleteQury = "db.XH_DesignManage_TaskFormula.findOne({'formulaId':'" + formula.String("formulaId") + "'})";

            var result = dataOp.Delete("XH_DesignManage_TaskFormula", deleteQury);

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }
        #endregion

        /// <summary>
        /// 批量设置任务负责人
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult BatchSetTaskOwner()
        {
            InvokeResult result = new InvokeResult();

            string taskIds = PageReq.GetForm("taskIds");
            int userId = PageReq.GetFormInt("userId");
            int profId = PageReq.GetFormInt("profId");

            List<int> taskIdList = TypeConvert.StringToIntEnum(taskIds, ",").ToList();

            try
            {
                using (TransactionScope trans = new TransactionScope())
                {
                    foreach (var taskId in taskIdList)
                    {
                        BsonDocument temp = dataOp.FindOneByQueryStr("XH_DesignManage_TaskManager", "taskId=" + taskId + "&type=" + (int)TaskManagerType.TaskOwner);

                        if (temp != null)
                        {
                            string updateQuery = "db.XH_DesignManage_TaskManager.findOne({'relId':'" + temp.Int("relId") + "'})";

                            result = dataOp.Update("XH_DesignManage_TaskManager", updateQuery, "userId=" + userId + "&profId=" + profId);
                        }
                        else
                        {
                            result = dataOp.Insert("XH_DesignManage_TaskManager", "taskId=" + taskId + "&userId=" + userId + "&type=" + (int)TaskManagerType.TaskOwner + "&profId=" + profId);
                        }
                    }
                    trans.Complete();
                }
            }
            catch (Exception ex)
            {
                result.Status = Status.Failed;
                result.Message = ex.Message;
            }

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        /// <summary>
        /// 改变计划状态
        /// </summary>
        /// <param name="planId"></param>
        /// <param name="status"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public ActionResult ChangePlanStatus(int planId, ProjectPlanStatus? status, string reason)
        {
            PageJson json = new PageJson();

            #region 获取参数
            BsonDocument plan = dataOp.FindOneByKeyVal("XH_DesignManage_Plan", "planId", planId.ToString());

            if (planId == 0 || (!status.HasValue))
            {
                json.Message = "页面参数错误";
                json.Success = false;
                return Json(json);
            }

            if (plan == null)
            {
                json.Message = "计划可能已被删除";
                json.Success = false;
                return Json(json);
            }

            if (this.ValidatePlanStatus(plan, status.Value) == false)
            {
                json.Message = "不能跳转到当前状态";
                json.Success = false;
                return Json(json);
            }
            #endregion

            InvokeResult result = dataOp.QuickUpdate("XH_DesignManage_Plan", "planId", planId.ToString(), "status=" + (int)status.Value);

            json = TypeConvert.InvokeResultToPageJson(result);

            return Json(json);
        }

        /// <summary>
        /// 验证计划状态切换是否合法
        /// </summary>
        /// <param name="plan"></param>
        /// <param name="nextStatus"></param>
        /// <returns></returns>
        private bool ValidatePlanStatus(BsonDocument plan, ProjectPlanStatus nextStatus)
        {
            /**
             * 未确认-->进行中
             * 进行中-->已完成
             * 已完成-->修订中.
             * **/

            try
            {
                ProjectPlanStatus currStatus = (ProjectPlanStatus)plan.Int("status");
                if (nextStatus == ProjectPlanStatus.Unconfirmed)
                {
                    return false;//不能再回到未确认
                }
                if (currStatus == ProjectPlanStatus.Unconfirmed)
                {
                    if (nextStatus != ProjectPlanStatus.Processing)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                if (currStatus == ProjectPlanStatus.Processing)
                {
                    if (nextStatus != ProjectPlanStatus.Completed)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;

        }

        /// <summary>
        /// 任务批量删除
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult DeleteTask()
        {
            DateTime dtStart = DateTime.Now;
            PageJson json = new PageJson();
            InvokeResult result = new InvokeResult();

            string ids = PageReq.GetForm("ids");

            if (String.IsNullOrEmpty(ids) == true)
            {
                json.Success = false;
                json.Message = "请选择需要删除的任务";
                return this.Json(json);
            }

            List<int> idList = TypeConvert.StringToIntEnum(ids, ",").ToList();

            List<StorageData> delList = new List<StorageData>();
            List<StorageData> referenceDelList = new List<StorageData>();//任务流程实例删除
            foreach (var taskId in idList)
            {
                StorageData temp = new StorageData();

                temp.Type = StorageType.Delete;
                temp.Query = Query.EQ("taskId", taskId.ToString());
                temp.Name = "XH_DesignManage_Task";

                delList.Add(temp);

                //var instanceQuery=Query.And(Query.EQ("referFieldValue", taskId.ToString()),Query.EQ("tableName", "XH_DesignManage_Task"),Query.EQ("referFieldName", "taskId"));
                //referenceDelList.Add(new StorageData() { Type = StorageType.Delete, Query = instanceQuery, Name = "BusFlowInstance" });

            }
            result = dataOp.BatchSaveStorageData(delList);
            //if (result.Status == Status.Successful)
            //{
            //    dataOp.BatchSaveStorageData(referenceDelList);
            //}
            json = TypeConvert.InvokeResultToPageJson(result);

            DateTime dtEnd = DateTime.Now;
            double db = dtEnd.Subtract(dtStart).TotalMilliseconds;
            json.AddInfo("db", db.ToString());
            return this.Json(json);
        }

        /// <summary>
        /// 获取技术关系类型
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        private int GetRelationType(string code)
        {
            switch (code)
            {
                case "FS": return 1;
                case "SS": return 2;
                case "FF": return 3;
                case "SF": return 4;
            }

            return 0;
        }

        /// <summary>
        /// 快速保存技术关系
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult QuickSaveTaskRelation()
        {
            PageJson json = new PageJson();

            #region 获取参数
            int projId = PageReq.GetFormInt("projId");
            int planId = PageReq.GetFormInt("planId");
            int sucTaskId = PageReq.GetFormInt("sucTaskId");
            string saveKey = PageReq.GetForm("saveKey");        //saveKey: relId,taskId,type,delayCount|Y|relId,taskId,type,delayCount|Y|

            var saveArray = saveKey.Split(new string[] { "|Y|" }, StringSplitOptions.RemoveEmptyEntries);

            List<BsonDocument> saveList = new List<BsonDocument>();
            foreach (var temp in saveArray)
            {
                if (sucTaskId.ToString() != temp.Split(',')[0])
                {
                    BsonDocument tempBson = new BsonDocument();

                    tempBson.Add("sucTaskId", sucTaskId.ToString());
                    tempBson.Add("preTaskId", temp.Split(',')[0]);
                    tempBson.Add("referType", GetRelationType(temp.Split(',')[2]).ToString());
                    tempBson.Add("delayCount", temp.Split(',')[3]);

                    saveList.Add(tempBson);
                }
            }

            List<BsonDocument> relationList = dataOp.FindAllByKeyVal("XH_DesignManage_TaskRelation", "sucTaskId", sucTaskId.ToString()).ToList();   //已有的技术关系列表

            List<StorageData> storageList = new List<StorageData>();

            #endregion

            #region 获取删除更新列表
            foreach (var tempRel in relationList)
            {
                var tempSave = saveList.Where(t => t.Int("preTaskId") == tempRel.Int("preTaskId") && t.Int("sucTaskId") == tempRel.Int("sucTaskId")).FirstOrDefault();

                if (tempSave == null)           //如果不在传进来的列表里面,则进行删除
                {
                    StorageData temp = new StorageData();

                    temp.Name = "XH_DesignManage_TaskRelation";
                    temp.Type = StorageType.Delete;
                    temp.Query = Query.EQ("relId", tempRel.String("relId"));
                    storageList.Add(temp);
                }
                else                            //如果在,则进行更新
                {
                    StorageData temp = new StorageData();

                    temp.Name = "XH_DesignManage_TaskRelation";
                    temp.Type = StorageType.Update;
                    temp.Query = Query.EQ("relId", tempRel.String("relId"));
                    temp.Document = tempSave;

                    storageList.Add(temp);
                }
            }
            #endregion

            #region  获取添加更新列表
            foreach (var tempSave in saveList)
            {
                var tempRel = relationList.Where(t => t.Int("preTaskId") == tempSave.Int("preTaskId") && t.Int("sucTaskId") == tempSave.Int("sucTaskId")).FirstOrDefault();

                if (tempRel == null)        //判断是否在已有的列表里面,没有则进行添加
                {
                    tempSave.Add("projId", projId);
                    tempSave.Add("planId", planId);

                    StorageData temp = new StorageData();

                    temp.Name = "XH_DesignManage_TaskRelation";
                    temp.Type = StorageType.Insert;
                    temp.Document = tempSave;

                    storageList.Add(temp);
                }
                else                        //有则进行更新
                {
                    StorageData temp = new StorageData();

                    temp.Name = "XH_DesignManage_TaskRelation";
                    temp.Type = StorageType.Update;
                    temp.Query = Query.EQ("relId", tempRel.String("relId"));
                    temp.Document = tempSave;

                    storageList.Add(temp);
                }
            }
            #endregion

            InvokeResult result = dataOp.BatchSaveStorageData(storageList);

            json = TypeConvert.InvokeResultToPageJson(result);

            return Json(json);
        }

        /// <summary>
        /// 保存任务关系
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult CommonSaveTaskRelation()
        {
            PageJson json = new PageJson();

            #region 获取参数
            int projId = PageReq.GetFormInt("projId");
            int planId = PageReq.GetFormInt("planId");
            string saveKey = PageReq.GetForm("saveKey");        //saveKey: preTaskId,sucTaskId,type,delayCount|Y|preTaskId,sucTaskId,type,delayCount|Y|

            var saveArray = saveKey.Split(new string[] { "|Y|" }, StringSplitOptions.RemoveEmptyEntries);

            List<BsonDocument> saveList = new List<BsonDocument>();
            foreach (var temp in saveArray)
            {
                BsonDocument tempBson = new BsonDocument();

                tempBson.Add("preTaskId", temp.Split(',')[0]);
                tempBson.Add("sucTaskId", temp.Split(',')[1]);
                tempBson.Add("referType", GetRelationType(temp.Split(',')[2]).ToString());
                tempBson.Add("delayCount", temp.Split(',')[3]);

                saveList.Add(tempBson);
            }

            List<BsonDocument> relationList = dataOp.FindAllByQuery("XH_DesignManage_TaskRelation", Query.EQ("projId", projId.ToString())).ToList(); //已有的技术关系列表

            List<StorageData> storageList = new List<StorageData>();

            #endregion

            #region 获取删除更新列表
            foreach (var tempRel in relationList)
            {
                var tempSave = saveList.Where(t => t.Int("preTaskId") == tempRel.Int("preTaskId") && t.Int("sucTaskId") == tempRel.Int("sucTaskId")).FirstOrDefault();

                if (tempSave == null)           //如果不在传进来的列表里面,则进行删除
                {
                    StorageData temp = new StorageData();

                    temp.Name = "XH_DesignManage_TaskRelation";
                    temp.Type = StorageType.Delete;
                    temp.Query = Query.EQ("relId", tempRel.String("relId"));
                    storageList.Add(temp);
                }
                else                            //如果在,则进行更新
                {
                    StorageData temp = new StorageData();

                    temp.Name = "XH_DesignManage_TaskRelation";
                    temp.Type = StorageType.Update;
                    temp.Query = Query.EQ("relId", tempRel.String("relId"));
                    temp.Document = tempSave;

                    storageList.Add(temp);
                }
            }
            #endregion

            #region  获取添加更新列表
            foreach (var tempSave in saveList)
            {
                var tempRel = relationList.Where(t => t.Int("preTaskId") == tempSave.Int("preTaskId") && t.Int("sucTaskId") == tempSave.Int("sucTaskId")).FirstOrDefault();

                if (tempRel == null)        //判断是否在已有的列表里面,没有则进行添加
                {
                    tempSave.Add("projId", projId);
                    tempSave.Add("planId", planId);

                    StorageData temp = new StorageData();

                    temp.Name = "XH_DesignManage_TaskRelation";
                    temp.Type = StorageType.Insert;
                    temp.Document = tempSave;

                    storageList.Add(temp);
                }
                else                        //有则进行更新
                {
                    StorageData temp = new StorageData();

                    temp.Name = "XH_DesignManage_TaskRelation";
                    temp.Type = StorageType.Update;
                    temp.Query = Query.EQ("relId", tempRel.String("relId"));
                    temp.Document = tempSave;

                    storageList.Add(temp);
                }
            }
            #endregion

            InvokeResult result = dataOp.BatchSaveStorageData(storageList);

            json = TypeConvert.InvokeResultToPageJson(result);

            return Json(json);
        }


        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult QuickSaveTaskRelationEx()
        {
            PageJson json = new PageJson();

            #region 获取参数
            int projId = PageReq.GetFormInt("projId");
            int planId = PageReq.GetFormInt("planId");
            var type = PageReq.GetFormInt("relationType");//0代表设置前驱动 1代表设置后继
            var curTaskType = type == 0 ? "sucTaskId" : "preTaskId";
            var otherTaskType = type == 0 ? "preTaskId" : "sucTaskId";
            int sucTaskId = PageReq.GetFormInt("taskId");
            string saveKey = PageReq.GetForm("saveKey");        //saveKey: relId,taskId,type,delayCount|Y|relId,taskId,type,delayCount|Y|

            var saveArray = saveKey.Split(new string[] { "|Y|" }, StringSplitOptions.RemoveEmptyEntries);

            List<BsonDocument> saveList = new List<BsonDocument>();
            foreach (var temp in saveArray)
            {
                if (sucTaskId.ToString() != temp.Split(',')[0])
                {
                    BsonDocument tempBson = new BsonDocument();

                    tempBson.Add(curTaskType, temp.Split(',')[1]);
                    tempBson.Add(otherTaskType, temp.Split(',')[0]);
                    tempBson.Add("referType", GetRelationType(temp.Split(',')[2]).ToString());
                    tempBson.Add("delayCount", temp.Split(',')[3]);

                    saveList.Add(tempBson);
                }
            }

            List<BsonDocument> relationList = dataOp.FindAllByKeyVal("XH_DesignManage_TaskRelation", curTaskType, sucTaskId.ToString()).ToList();   //已有的技术关系列表
            var saveTaskIds = saveList.Select(c => c.Int(otherTaskType)).ToList();
            var deleteRelationList = relationList.Where(c => !saveTaskIds.Contains(c.Int(otherTaskType))).ToList();
            List<StorageData> storageList = new List<StorageData>();

            #endregion

            #region 删除更新列表
            if (deleteRelationList.Count() > 0)
            {
                storageList.AddRange(from c in deleteRelationList
                                     select new StorageData()
                                     {
                                         Name = "XH_DesignManage_TaskRelation",
                                         Type = StorageType.Delete,
                                         Document = c,
                                         Query = Query.EQ("relId", c.Text("relId"))
                                     });
            }
            #endregion

            #region  获取添加更新列表
            foreach (var tempSave in saveList)
            {
                var tempRel = relationList.Where(t => t.Int(otherTaskType) == tempSave.Int(otherTaskType) && t.Int(curTaskType) == tempSave.Int(curTaskType)).FirstOrDefault();

                if (tempRel == null)        //判断是否在已有的列表里面,没有则进行添加
                {
                    tempSave.Add("projId", projId.ToString());
                    tempSave.Add("planId", planId.ToString());

                    StorageData temp = new StorageData();

                    temp.Name = "XH_DesignManage_TaskRelation";
                    temp.Type = StorageType.Insert;
                    temp.Document = tempSave;

                    storageList.Add(temp);
                }
                else                        //有则进行更新
                {
                    StorageData temp = new StorageData();

                    temp.Name = "XH_DesignManage_TaskRelation";
                    temp.Type = StorageType.Update;
                    temp.Query = Query.EQ("relId", tempRel.String("relId"));
                    temp.Document = tempSave;

                    storageList.Add(temp);
                }
            }
            #endregion

            InvokeResult result = dataOp.BatchSaveStorageData(storageList);

            json = TypeConvert.InvokeResultToPageJson(result);

            return Json(json);
        }

        /// <summary>
        /// 设置任务开始，结束时间的背景色
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public JsonResult ChangeDateBgColor(int id)
        {
            string startDateBg = PageReq.GetForm("startBg");
            string endDateBg = PageReq.GetForm("endBg");
            int type = PageReq.GetFormInt("type");  //0 设置计划开始，1 设置计划结束

            var query = Query.EQ("taskId", id.ToString());

            BsonDocument taskInfo = new BsonDocument();

            if (startDateBg.Trim() != "")
            {
                taskInfo.Add("curStartDataBgColor", startDateBg);
            }

            if (endDateBg.Trim() != "")
            {
                taskInfo.Add("curEndDataBgColor", endDateBg);
            }

            InvokeResult result = dataOp.Update("XH_DesignManage_Task", query, taskInfo);

            return Json(TypeConvert.InvokeResultToPageJson(result), JsonRequestBehavior.AllowGet);

        }

        /// <summary>
        /// 检查计划下面是否存在未完成的任务
        /// </summary>
        /// <param name="planId"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public JsonResult HasIncompleteTask(int? planId, ProjectPlanStatus? status)
        {
            JsonResult json = new JsonResult();

            if (!(planId.HasValue && status.HasValue))
            {
                json.Data = new
                {
                    success = false,
                    msg = "页面参数错误"
                };

                return json;
            }
            BsonDocument plan = dataOp.FindOneByKeyVal("XH_DesignManage_Plan", "planId", planId.Value.ToString());
            if (plan == null)
            {
                json.Data = new
                {
                    success = false,
                    msg = "计划可能已被删除"
                };

                return json;
            }
            else
            {
                if (this.ValidatePlanStatus(plan, status.Value) == false)
                {
                    json.Data = new
                    {
                        success = false,
                        msg = "不能跳转到当前状态"
                    };
                }

                List<BsonDocument> allTaskList = dataOp.FindAllByKeyVal("XH_DesignManage_Task", "planId", planId.Value.ToString()).ToList();

                bool hasIncompleteTask = true;

                if (status == ProjectPlanStatus.Processing)//下个为进行中，检查待分解的任务
                {
                    hasIncompleteTask = allTaskList.FirstOrDefault(r => r.Int("status") == (int)TaskStatus.ToSplit && r.Int("nodePid") != 0) != null;
                }
                else if (status == ProjectPlanStatus.Completed)//下个为已完成，检查未完成的任务
                {
                    hasIncompleteTask = allTaskList.FirstOrDefault(r => r.Int("status") != (int)TaskStatus.Completed && r.Int("nodePid") != 0) != null;
                }

                if (hasIncompleteTask)
                {
                    json.Data = new
                    {
                        success = true,
                        hasIncompleteTask = true
                    };
                }
                else
                {
                    json.Data = new
                    {
                        success = true,
                        hasIncompleteTask = false
                    };
                }
            }

            return Json(json, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 任务配置单价值项导入
        /// </summary>
        /// <returns></returns>
        public ActionResult TaskCIlistImport()
        {
            PageJson json = new PageJson();

            int taskId = PageReq.GetFormInt("taskId");      //任务Id
            int treeId = PageReq.GetFormInt("treeId");      //价值树Id
            int seriesId = PageReq.GetFormInt("seriesId");  //产品系列Id
            int lineId = PageReq.GetFormInt("lineId");      //产品线Id
            int combId = PageReq.GetFormInt("combId");      //组合Id

            if (taskId > 0 && treeId > 0)    //如果任务Id与价值树Id大于0,判断是否已有重复价值树项
            {
                BsonDocument oldList = dataOp.FindOneByQueryStr("XH_DesignManage_TaskCIlist", "taskId=" + taskId + "&treeId=" + treeId);

                if (oldList != null)
                {
                    json.Message = "已存在对应价值项的配置清单!";
                    json.Success = false;
                    return Json(json);
                }
            }

            InvokeResult result = dataOp.Insert("XH_DesignManage_TaskCIlist", "taskId=" + taskId + "&treeId=" + treeId);

            if (result.Status == Status.Successful)
            {
                InvokeResult tempResult = new InvokeResult();
                string listId = result.BsonInfo.String("listId");

                List<StorageData> newItemDataList = new List<StorageData>();    //导入价值项更新语句
                List<BsonDocument> sourceItemList = new List<BsonDocument>();   //源价值项列表
                List<BsonDocument> sourceValueList = new List<BsonDocument>();   //源价值项列表
                List<BsonDocument> sourctRetList = new List<BsonDocument>();    //源成果关联
                List<BsonDocument> sourctMatList = new List<BsonDocument>();    //源材料关联

                Dictionary<int, int> itemMappingDic = new Dictionary<int, int>();   //新旧价值项Id对应(旧Id,新Id)

                #region 导入价值项
                if (seriesId > 0 && treeId > 0)
                {
                    sourceItemList = dataOp.FindAllByQueryStr("XH_ProductDev_ValueItem", "seriesId=" + seriesId + "&treeId=" + treeId).ToList();
                }

                foreach (var sourceItem in sourceItemList.OrderBy(t => t.String("nodeKey"))) //循环导入价值项
                {
                    BsonDocument tempBson = new BsonDocument();

                    tempBson.Add("listId", listId);
                    tempBson.Add("name", sourceItem.String("name"));
                    tempBson.Add("itemType", sourceItem.String("itemType"));
                    tempBson.Add("dataKey", sourceItem.String("dataKey"));

                    if (sourceItem.Int("nodePid") == 0)
                    {
                        tempBson.Add("nodePid", "0");
                    }
                    else if (itemMappingDic.ContainsKey(sourceItem.Int("nodePid")) == true)
                    {
                        tempBson.Add("nodePid", itemMappingDic[sourceItem.Int("nodePid")]);
                    }

                    if (tempBson.String("nodePid", "") != "")
                    {
                        tempResult = dataOp.Insert("XH_DesignManage_TaskCIItem", tempBson);

                        if (tempResult.Status == Status.Successful)
                        {
                            itemMappingDic.Add(sourceItem.Int("itemId"), tempResult.BsonInfo.Int("itemId"));
                        }
                    }
                }
                #endregion

                #region 导入价值项值
                if (lineId > 0 && combId > 0)
                {
                    sourceValueList = dataOp.FindAllByQueryStr("XH_ProductDev_LineItemValue", "lineId=" + lineId + "&treeId=" + treeId + "&combinationId=" + combId).ToList();
                }

                List<StorageData> valDataList = new List<StorageData>();

                foreach (var sourceVal in sourceValueList) //循环导入价值项
                {
                    if (itemMappingDic.ContainsKey(sourceVal.Int("itemId")) == true)    //有对应项
                    {
                        BsonDocument tempBson = new BsonDocument();

                        tempBson.Add("listId", listId);
                        tempBson.Add("itemId", itemMappingDic[sourceVal.Int("itemId")]);

                        foreach (var element in sourceVal.Elements)
                        {
                            if (element != null && (element.Name != "valueId") && (element.Name != "lineId") && (element.Name != "treeId")
                                && (element.Name != "combinationId") && (element.Name != "itemId") && (element.Name != "createDate")
                            && (element.Name != "updateDate") && (element.Name != "createUserId") && (element.Name != "updateUserId")
                                  && (element.Name != "underTable") && (element.Name != "order"))
                            {
                                tempBson.Add(element.Name, element.Value);
                            }
                        }

                        StorageData tempData = new StorageData();
                        tempData.Name = "XH_DesignManage_TaskCIValue";
                        tempData.Document = tempBson;
                        tempData.Type = StorageType.Insert;
                        valDataList.Add(tempData);
                    }
                }

                tempResult = dataOp.BatchSaveStorageData(valDataList);

                #endregion

                #region 导入成果关联
                if (lineId > 0 && combId > 0)
                {
                    sourctRetList = dataOp.FindAllByQueryStr("XH_ProductDev_LineItemRetRelation", "lineId=" + lineId + "&treeId=" + treeId + "&combinationId=" + combId).ToList();
                }

                List<StorageData> retDataList = new List<StorageData>();

                foreach (var sourceRet in sourctRetList) //循环导入价值项
                {
                    if (itemMappingDic.ContainsKey(sourceRet.Int("itemId")) == true)    //有对应项
                    {
                        BsonDocument tempBson = new BsonDocument();

                        tempBson.Add("listId", listId);
                        tempBson.Add("itemId", itemMappingDic[sourceRet.Int("itemId")]);

                        foreach (var element in sourceRet.Elements)
                        {
                            if (element != null && (element.Name != "valueId") && (element.Name != "lineId") && (element.Name != "treeId")
                                && (element.Name != "combinationId") && (element.Name != "itemId") && (element.Name != "createDate")
                            && (element.Name != "updateDate") && (element.Name != "createUserId") && (element.Name != "updateUserId")
                                  && (element.Name != "underTable") && (element.Name != "order"))
                            {
                                tempBson.Add(element.Name, element.Value);
                            }
                        }

                        StorageData tempData = new StorageData();
                        tempData.Name = "XH_DesignManage_TaskCIItemRetRelation";
                        tempData.Document = tempBson;
                        tempData.Type = StorageType.Insert;
                        retDataList.Add(tempData);
                    }
                }

                tempResult = dataOp.BatchSaveStorageData(retDataList);

                #endregion

                #region 导入材料关联
                if (lineId > 0 && combId > 0)
                {
                    sourctMatList = dataOp.FindAllByQueryStr("XH_ProductDev_LineItemMatRelation", "lineId=" + lineId + "&treeId=" + treeId + "&combinationId=" + combId).ToList(); //所有材料关联
                }

                List<StorageData> matDataList = new List<StorageData>();

                foreach (var sourceMat in sourctMatList) //循环导入价值项
                {
                    if (itemMappingDic.ContainsKey(sourceMat.Int("itemId")) == true)    //有对应项
                    {
                        BsonDocument tempBson = new BsonDocument();

                        tempBson.Add("listId", listId);
                        tempBson.Add("itemId", itemMappingDic[sourceMat.Int("itemId")]);

                        foreach (var element in sourceMat.Elements)
                        {
                            if (element != null && (element.Name != "valueId") && (element.Name != "lineId") && (element.Name != "treeId")
                                && (element.Name != "combinationId") && (element.Name != "itemId") && (element.Name != "createDate")
                            && (element.Name != "updateDate") && (element.Name != "createUserId") && (element.Name != "updateUserId")
                                  && (element.Name != "underTable") && (element.Name != "order"))
                            {
                                tempBson.Add(element.Name, element.Value);
                            }
                        }

                        StorageData tempData = new StorageData();
                        tempData.Name = "XH_DesignManage_TaskCIItemMatRelation";
                        tempData.Document = tempBson;
                        tempData.Type = StorageType.Insert;
                        matDataList.Add(tempData);
                    }
                }

                tempResult = dataOp.BatchSaveStorageData(matDataList);

                #endregion

            }

            json = TypeConvert.InvokeResultToPageJson(result);

            return Json(json);
        }

        /// <summary>
        /// 项目配置单价值项导入
        /// </summary>
        /// <returns></returns>
        public ActionResult ProjCIlistImport()
        {
            PageJson json = new PageJson();

            int projId = PageReq.GetFormInt("projId");      //项目Id
            int treeId = PageReq.GetFormInt("treeId");      //价值树Id
            int seriesId = PageReq.GetFormInt("seriesId");  //产品系列Id
            int lineId = PageReq.GetFormInt("lineId");      //产品线Id
            int combId = PageReq.GetFormInt("combId");      //组合Id

            if (projId > 0 && treeId > 0)    //如果任务Id与价值树Id大于0,判断是否已有重复价值树项
            {
                BsonDocument oldList = dataOp.FindOneByQuery("ProjectCIlist", Query.And(
                    Query.EQ("projId", projId.ToString()),
                    Query.EQ("treeId", treeId.ToString())));

                if (oldList != null)
                {
                    json.Message = "已存在对应价值项的配置清单!";
                    json.Success = false;
                    return Json(json);
                }
            }

            InvokeResult result = dataOp.Insert("ProjectCIlist", "projId=" + projId + "&treeId=" + treeId);

            if (result.Status == Status.Successful)
            {
                InvokeResult tempResult = new InvokeResult();
                string listId = result.BsonInfo.String("listId");

                List<StorageData> newItemDataList = new List<StorageData>();    //导入价值项更新语句
                List<BsonDocument> sourceItemList = new List<BsonDocument>();   //源价值项列表
                List<BsonDocument> sourceValueList = new List<BsonDocument>();   //源价值项列表
                List<BsonDocument> sourctRetList = new List<BsonDocument>();    //源成果关联
                List<BsonDocument> sourctMatList = new List<BsonDocument>();    //源材料关联

                Dictionary<int, int> itemMappingDic = new Dictionary<int, int>();   //新旧价值项Id对应(旧Id,新Id)

                #region 导入价值项
                if (seriesId > 0 && treeId > 0)
                {
                    sourceItemList = dataOp.FindAllByQueryStr("XH_ProductDev_ValueItem", "seriesId=" + seriesId + "&treeId=" + treeId).ToList();
                }

                foreach (var sourceItem in sourceItemList.OrderBy(t => t.String("nodeKey"))) //循环导入价值项
                {
                    BsonDocument tempBson = new BsonDocument();

                    tempBson.Add("listId", listId);
                    tempBson.Add("name", sourceItem.String("name"));
                    tempBson.Add("itemType", sourceItem.String("itemType"));
                    tempBson.Add("dataKey", sourceItem.String("dataKey"));

                    if (sourceItem.Int("nodePid") == 0)
                    {
                        tempBson.Add("nodePid", "0");
                    }
                    else if (itemMappingDic.ContainsKey(sourceItem.Int("nodePid")) == true)
                    {
                        tempBson.Add("nodePid", itemMappingDic[sourceItem.Int("nodePid")]);
                    }

                    if (tempBson.String("nodePid", "") != "")
                    {
                        tempResult = dataOp.Insert("ProjectCIItem", tempBson);

                        if (tempResult.Status == Status.Successful)
                        {
                            itemMappingDic.Add(sourceItem.Int("itemId"), tempResult.BsonInfo.Int("itemId"));
                        }
                    }
                }
                #endregion

                #region 导入价值项值
                if (lineId > 0 && combId > 0)
                {
                    sourceValueList = dataOp.FindAllByQueryStr("XH_ProductDev_LineItemValue", "lineId=" + lineId + "&treeId=" + treeId + "&combinationId=" + combId).ToList();
                }

                List<StorageData> valDataList = new List<StorageData>();

                foreach (var sourceVal in sourceValueList) //循环导入价值项
                {
                    if (itemMappingDic.ContainsKey(sourceVal.Int("itemId")) == true)    //有对应项
                    {
                        BsonDocument tempBson = new BsonDocument();

                        tempBson.Add("listId", listId);
                        tempBson.Add("itemId", itemMappingDic[sourceVal.Int("itemId")]);

                        foreach (var element in sourceVal.Elements)
                        {
                            if (element != null && (element.Name != "valueId") && (element.Name != "lineId") && (element.Name != "treeId")
                                && (element.Name != "combinationId") && (element.Name != "itemId") && (element.Name != "createDate")
                            && (element.Name != "updateDate") && (element.Name != "createUserId") && (element.Name != "updateUserId")
                                  && (element.Name != "underTable") && (element.Name != "order"))
                            {
                                tempBson.Add(element.Name, element.Value);
                            }
                        }

                        StorageData tempData = new StorageData();
                        tempData.Name = "ProjectCIValue";
                        tempData.Document = tempBson;
                        tempData.Type = StorageType.Insert;
                        valDataList.Add(tempData);
                    }
                }

                tempResult = dataOp.BatchSaveStorageData(valDataList);

                #endregion

                #region 导入材料关联
                if (lineId > 0 && combId > 0)
                {
                    sourctMatList = dataOp.FindAllByQueryStr("XH_ProductDev_LineItemMatRelation", "lineId=" + lineId + "&treeId=" + treeId + "&combinationId=" + combId).ToList(); //所有材料关联
                }

                List<StorageData> matDataList = new List<StorageData>();

                foreach (var sourceMat in sourctMatList) //循环导入价值项
                {
                    if (itemMappingDic.ContainsKey(sourceMat.Int("itemId")) == true)    //有对应项
                    {
                        BsonDocument tempBson = new BsonDocument();

                        tempBson.Add("listId", listId);
                        tempBson.Add("itemId", itemMappingDic[sourceMat.Int("itemId")]);

                        foreach (var element in sourceMat.Elements)
                        {
                            if (element != null && (element.Name != "valueId") && (element.Name != "lineId") && (element.Name != "treeId")
                                && (element.Name != "combinationId") && (element.Name != "itemId") && (element.Name != "createDate")
                            && (element.Name != "updateDate") && (element.Name != "createUserId") && (element.Name != "updateUserId")
                                  && (element.Name != "underTable") && (element.Name != "order"))
                            {
                                tempBson.Add(element.Name, element.Value);
                            }
                        }

                        StorageData tempData = new StorageData();
                        tempData.Name = "ProjectCIItemMatRelation";
                        tempData.Document = tempBson;
                        tempData.Type = StorageType.Insert;
                        matDataList.Add(tempData);
                    }
                }

                tempResult = dataOp.BatchSaveStorageData(matDataList);

                #endregion

            }

            json = TypeConvert.InvokeResultToPageJson(result);

            return Json(json);
        }

        /// <summary>
        /// 项目配置单价值项导入（新）
        /// </summary>
        /// <returns></returns>
        public ActionResult ProjCIlistImportNew()
        {
            lock (objPad)
            {
                PageJson json = new PageJson();
                int isImportValue = PageReq.GetFormInt("isImportValue");//1:不导入材料，只导入结构，0或其2：连带导入原有材料

                int projId = PageReq.GetFormInt("projId");      //项目Id
                int treeId = PageReq.GetFormInt("treeId");      //价值树Id
                int seriesId = PageReq.GetFormInt("seriesId");  //产品系列Id
                int lineId = PageReq.GetFormInt("lineId");      //产品线Id
                int combId = PageReq.GetFormInt("combId");      //组合Id
                if (combId == 0)
                {
                    json.Message = "未选择版本!请先选择版本!";
                    json.Success = false;
                    return Json(json);
                }
                //if (projId > 0 && treeId > 0 && combId > 0)    //如果任务Id与价值树Id大于0,判断是否已有重复价值树项
                //{
                //    BsonDocument oldList = dataOp.FindOneByQuery("ProjectCIlist", Query.And(
                //        Query.EQ("projId", projId.ToString()),
                //        Query.EQ("treeId", treeId.ToString()),
                //        Query.EQ("combinationId", combId.ToString())
                //        ));

                //    if (oldList != null)
                //    {
                //        BsonDocument valTree = dataOp.FindOneByQuery("XH_ProductDev_ValueTree", Query.EQ("treeId", treeId.ToString())); //价值树
                //        BsonDocument series = dataOp.FindOneByQuery("XH_ProductDev_ProductSeries",Query.EQ( "seriesId",seriesId.ToString()));//产品系列
                //        BsonDocument line = dataOp.FindOneByQuery("XH_ProductDev_ProductLine", Query.EQ("lineId", lineId.ToString()));//产品线
                //        BsonDocument combination = dataOp.FindOneByQuery("XH_ProductDev_LineValCombination",Query.EQ( "combinationId", combId.ToString()));//产品组合

                //        string errInfo = string.Format("{0}-{1}-{2}-{3} 已经存在！", series.String("name"), line.String("name"), combination.String("name"),valTree.String("name"));

                //        json.Message = errInfo;
                //        json.Success = false;
                //        return Json(json);
                //    }
                //}

                InvokeResult result = dataOp.Insert("ProjectCIlist", "projId=" + projId + "&treeId=" + treeId + "&seriesId=" + seriesId + "&lineId=" + lineId + "&combinationId=" + combId + "&isImportValue=" + isImportValue);

                if (result.Status == Status.Successful)
                {
                    InvokeResult tempResult = new InvokeResult();
                    string listId = result.BsonInfo.String("listId");

                    List<StorageData> newItemDataList = new List<StorageData>();    //导入价值项更新语句
                    List<BsonDocument> sourceItemList = new List<BsonDocument>();   //源价值项列表
                    List<BsonDocument> sourceValueList = new List<BsonDocument>();   //源价值项列表
                    List<BsonDocument> sourctRetList = new List<BsonDocument>();    //源成果关联
                    List<BsonDocument> sourctMatList = new List<BsonDocument>();    //源材料关联


                    Dictionary<int, int> itemMappingDic = new Dictionary<int, int>();   //新旧价值项Id对应(旧Id,新Id)

                    #region 导入价值项
                    if (seriesId > 0 && treeId > 0)
                    {
                        sourceItemList = dataOp.FindAllByQueryStr("XH_ProductDev_ValueItem", "seriesId=" + seriesId + "&treeId=" + treeId).ToList();
                    }

                    foreach (var sourceItem in sourceItemList.OrderBy(t => t.String("nodeKey"))) //循环导入价值项
                    {
                        BsonDocument tempBson = new BsonDocument();

                        tempBson.Add("listId", listId);
                        tempBson.Add("name", sourceItem.String("name"));
                        tempBson.Add("itemType", sourceItem.String("itemType"));
                        tempBson.Add("dataKey", sourceItem.String("dataKey"));

                        if (sourceItem.Int("nodePid") == 0)
                        {
                            tempBson.Add("nodePid", "0");
                        }
                        else if (itemMappingDic.ContainsKey(sourceItem.Int("nodePid")) == true)//通过字典给子节点的nodepid赋值
                        {
                            tempBson.Add("nodePid", itemMappingDic[sourceItem.Int("nodePid")]);
                        }

                        if (tempBson.String("nodePid", "") != "")
                        {
                            tempResult = dataOp.Insert("ProjectCIItem", tempBson);

                            if (tempResult.Status == Status.Successful)
                            {
                                itemMappingDic.Add(sourceItem.Int("itemId"), tempResult.BsonInfo.Int("itemId"));//记录新旧价值项的Id字段
                            }
                        }
                    }
                    #endregion

                    if (isImportValue != 1)
                    {

                        #region 导入价值项值
                        if (lineId > 0 && combId > 0)
                        {
                            sourceValueList = dataOp.FindAllByQueryStr("XH_ProductDev_LineItemValue", "lineId=" + lineId + "&treeId=" + treeId + "&combinationId=" + combId).ToList();
                        }

                        List<StorageData> valDataList = new List<StorageData>();

                        foreach (var sourceVal in sourceValueList) //循环导入价值项
                        {
                            if (itemMappingDic.ContainsKey(sourceVal.Int("itemId")) == true)    //有对应项
                            {
                                BsonDocument tempBson = new BsonDocument();

                                tempBson.Add("listId", listId);
                                tempBson.Add("itemId", itemMappingDic[sourceVal.Int("itemId")]);

                                foreach (var element in sourceVal.Elements)
                                {
                                    if (element != null && (element.Name != "_id") && (element.Name != "valueId") && (element.Name != "lineId") && (element.Name != "treeId")
                                        && (element.Name != "combinationId") && (element.Name != "itemId") && (element.Name != "createDate")
                                    && (element.Name != "updateDate") && (element.Name != "createUserId") && (element.Name != "updateUserId")
                                          && (element.Name != "underTable") && (element.Name != "order"))
                                    {
                                        tempBson.Add(element.Name, element.Value);
                                    }
                                }

                                StorageData tempData = new StorageData();
                                tempData.Name = "ProjectCIValue";
                                tempData.Document = tempBson;
                                tempData.Type = StorageType.Insert;
                                valDataList.Add(tempData);
                            }
                        }

                        tempResult = dataOp.BatchSaveStorageData(valDataList);

                        #endregion

                        #region 导入材料关联

                        if (lineId > 0 && combId > 0)
                        {
                            sourctMatList = dataOp.FindAllByQueryStr("XH_ProductDev_LineItemMatRelation", "lineId=" + lineId + "&treeId=" + treeId + "&combinationId=" + combId).ToList(); //所有材料关联
                        }

                        List<StorageData> matDataList = new List<StorageData>();

                        foreach (var sourceMat in sourctMatList) //循环导入价值项
                        {
                            if (itemMappingDic.ContainsKey(sourceMat.Int("itemId")) == true)    //有对应项
                            {
                                BsonDocument tempBson = new BsonDocument();

                                tempBson.Add("listId", listId);
                                tempBson.Add("itemId", itemMappingDic[sourceMat.Int("itemId")]);

                                foreach (var element in sourceMat.Elements)
                                {
                                    if (element != null && (element.Name != "_id") && (element.Name != "valueId") && (element.Name != "lineId") && (element.Name != "treeId")
                                        && (element.Name != "combinationId") && (element.Name != "itemId") && (element.Name != "createDate")
                                    && (element.Name != "updateDate") && (element.Name != "createUserId") && (element.Name != "updateUserId")
                                          && (element.Name != "underTable") && (element.Name != "order"))
                                    {
                                        tempBson.Add(element.Name, element.Value);
                                    }
                                }

                                StorageData tempData = new StorageData();
                                tempData.Name = "ProjectCIItemMatRelation";
                                tempData.Document = tempBson;
                                tempData.Type = StorageType.Insert;
                                matDataList.Add(tempData);
                            }
                        }

                        tempResult = dataOp.BatchSaveStorageData(matDataList);


                        #endregion

                        #region 导入成果关联
                        if (lineId > 0 && combId > 0)
                        {
                            sourctRetList = dataOp.FindAllByQueryStr("XH_ProductDev_LineItemRetRelation", "lineId=" + lineId + "&treeId=" + treeId + "&combinationId=" + combId).ToList();
                        }

                        List<StorageData> retDataList = new List<StorageData>();

                        foreach (var sourceRet in sourctRetList) //循环导入价值项
                        {
                            if (itemMappingDic.ContainsKey(sourceRet.Int("itemId")) == true)    //有对应项
                            {
                                BsonDocument tempBson = new BsonDocument();

                                tempBson.Add("listId", listId);
                                tempBson.Add("itemId", itemMappingDic[sourceRet.Int("itemId")]);

                                foreach (var element in sourceRet.Elements)
                                {
                                    if (element != null && (element.Name != "_id") && (element.Name != "lineId") && (element.Name != "treeId")
                                        && (element.Name != "combinationId") && (element.Name != "itemId") && (element.Name != "createDate")
                                    && (element.Name != "updateDate") && (element.Name != "createUserId") && (element.Name != "updateUserId")
                                          && (element.Name != "underTable") && (element.Name != "order"))
                                    {
                                        tempBson.Add(element.Name, element.Value);
                                    }
                                }

                                StorageData tempData = new StorageData();
                                tempData.Name = "ProjectCIItemRetRelation";
                                tempData.Document = tempBson;
                                tempData.Type = StorageType.Insert;
                                retDataList.Add(tempData);
                            }
                        }

                        tempResult = dataOp.BatchSaveStorageData(retDataList);

                        #endregion



                    }

                }

                json = TypeConvert.InvokeResultToPageJson(result);

                return Json(json);
            }
        }

        /// <summary>
        /// 保存产品配置清单
        /// </summary>
        /// <returns></returns>
        public ActionResult SaveProjectCIlist()
        {
            PageJson json = new PageJson();
            InvokeResult result = new InvokeResult();

            #region 获取参数
            int projId = PageReq.GetFormInt("projId");              //项目Id
            var treeId = PageReq.GetFormInt("treeId");              //价值树
            var propertyId = PageReq.GetFormInt("propertyId");      //业态
            var addType = PageReq.GetFormInt("addType");            //新增方式(0:空白,1:导入项,2:导入项值
            var importType = PageReq.GetFormInt("importType");      //来源类型 ( 1:产品系列,2:项目)

            var seriesId = PageReq.GetFormInt("seriesId");          //产品系列
            var lineId = PageReq.GetFormInt("lineId");              //产品线
            var combId = PageReq.GetFormInt("combId");              //版本&组合
            var srcProjId = PageReq.GetFormInt("srcProjId");              //来源项目
            var srcListId = PageReq.GetFormInt("srcListId");              //来源配置清单

            #endregion

            #region 判断重复
            if (projId > 0 && treeId > 0 && propertyId > 0)    //如果任务Id与价值树Id大于0,判断是否已有重复价值树项
            {
                BsonDocument oldList = dataOp.FindOneByQuery("ProjectCIlist", Query.And(
                    Query.EQ("projId", projId.ToString()),
                    Query.EQ("treeId", treeId.ToString()),
                    Query.EQ("propertyId", propertyId.ToString())
                    ));

                if (oldList != null)
                {
                    json.Message = "已存在同类型的配置清单!";
                    json.Success = false;
                    return Json(json);
                }
            }
            else
            {
                json.Message = "传入参数有误!";
                json.Success = false;
                return Json(json);
            }
            #endregion

            try
            {
                #region 新增配置清单
                result = dataOp.Insert("ProjectCIlist", new BsonDocument().Add("projId", projId.ToString())
                    .Add("treeId", treeId.ToString())
                    .Add("propertyId", propertyId.ToString())
                    .Add("addType", addType.ToString())
                    .Add("importType", importType.ToString())
                    .Add("seriesId", seriesId.ToString())
                    .Add("lineId", lineId.ToString())
                    .Add("combinationId", combId.ToString())
                    .Add("srcProjId", srcProjId.ToString())
                    .Add("srcListId", srcListId.ToString())
                    );
                if (result.Status == Status.Failed) throw new Exception(result.Message);

                string newListId = result.BsonInfo.String("listId");
                #endregion

                Dictionary<int, int> itemMappingDic = new Dictionary<int, int>();   //新旧价值项Id对应(旧Id,新Id)
                List<string> notNeedColumn = new List<string>() { "_id", "valueId", "lineId", "treeId", "combinationId", "listId", "itemId", "createDate", "updateDate", "createUserId", "updateUserId", "underTable", "order", "isImport" };

                if (addType > 0)
                {
                    #region 导入价值项
                    List<BsonDocument> sourceItemList = new List<BsonDocument>();   //源价值项列表

                    #region 取值
                    if (importType == 1)
                    {
                        sourceItemList = dataOp.FindAllByQueryStr("XH_ProductDev_ValueItem", "seriesId=" + seriesId + "&treeId=" + treeId).ToList();
                    }
                    else if (importType == 2)
                    {
                        sourceItemList = dataOp.FindAllByQuery("ProjectCIItem", Query.EQ("listId", srcListId.ToString())).ToList();
                    }
                    #endregion

                    #region 循环导入
                    foreach (var sourceItem in sourceItemList.OrderBy(t => t.String("nodeKey")))
                    {
                        BsonDocument tempBson = new BsonDocument();

                        tempBson.Add("listId", newListId);
                        tempBson.Add("name", sourceItem.String("name"));
                        tempBson.Add("itemType", sourceItem.String("itemType"));
                        tempBson.Add("dataKey", sourceItem.String("dataKey"));

                        if (sourceItem.Int("nodePid") == 0)
                        {
                            tempBson.Add("nodePid", "0");
                        }
                        else if (itemMappingDic.ContainsKey(sourceItem.Int("nodePid")) == true)
                        {
                            tempBson.Add("nodePid", itemMappingDic[sourceItem.Int("nodePid")]);
                        }

                        if (tempBson.String("nodePid", "") != "")
                        {
                            result = dataOp.Insert("ProjectCIItem", tempBson);
                            if (result.Status == Status.Failed) throw new Exception(result.Message);

                            itemMappingDic.Add(sourceItem.Int("itemId"), result.BsonInfo.Int("itemId"));
                        }
                    }
                    #endregion

                    #endregion
                }

                if (addType == 2)
                {
                    #region 导入价值项值

                    #region 取值
                    List<BsonDocument> sourceValueList = new List<BsonDocument>();   //源价值项列表

                    if (importType == 1)
                    {
                        sourceValueList = dataOp.FindAllByQuery("XH_ProductDev_LineItemValue", Query.And(
                            Query.EQ("lineId", lineId.ToString()),
                            Query.EQ("treeId", treeId.ToString()),
                            Query.EQ("combinationId", combId.ToString())
                        )).ToList();
                    }
                    else if (importType == 2)
                    {
                        sourceValueList = dataOp.FindAllByQuery("ProjectCIValue", Query.EQ("listId", srcListId.ToString())).ToList();
                    }
                    #endregion

                    #region 循环导入
                    List<StorageData> valDataList = new List<StorageData>();

                    foreach (var sourceVal in sourceValueList)
                    {
                        if (itemMappingDic.ContainsKey(sourceVal.Int("itemId")) == true)    //有对应项
                        {
                            BsonDocument tempBson = new BsonDocument();

                            tempBson.Add("listId", newListId);
                            tempBson.Add("itemId", itemMappingDic[sourceVal.Int("itemId")]);
                            tempBson.Add("isImport", "1");

                            foreach (var element in sourceVal.Elements)
                            {
                                if (notNeedColumn.Contains(element.Name) == false)
                                {
                                    tempBson.Add(element.Name, element.Value);
                                }
                            }

                            StorageData tempData = new StorageData();
                            tempData.Name = "ProjectCIValue";
                            tempData.Document = tempBson;
                            tempData.Type = StorageType.Insert;
                            valDataList.Add(tempData);
                        }
                    }

                    result = dataOp.BatchSaveStorageData(valDataList);
                    if (result.Status == Status.Failed) throw new Exception(result.Message);
                    #endregion

                    #endregion

                    #region 导入材料关联

                    #region 取值
                    List<BsonDocument> sourctMatList = new List<BsonDocument>();    //源材料关联

                    if (importType == 1)
                    {
                        sourctMatList = dataOp.FindAllByQuery("XH_ProductDev_LineItemMatRelation", Query.And(
                            Query.EQ("lineId", lineId.ToString()),
                            Query.EQ("treeId", treeId.ToString()),
                            Query.EQ("combinationId", combId.ToString())
                        )).ToList();
                    }
                    else if (importType == 2)
                    {
                        sourctMatList = dataOp.FindAllByQuery("ProjectCIItemMatRelation", Query.EQ("listId", srcListId.ToString())).ToList();
                    }
                    #endregion

                    #region 循环导入
                    List<StorageData> matDataList = new List<StorageData>();

                    foreach (var sourceMat in sourctMatList)
                    {
                        if (itemMappingDic.ContainsKey(sourceMat.Int("itemId")) == true)    //有对应项
                        {
                            BsonDocument tempBson = new BsonDocument();

                            tempBson.Add("listId", newListId);
                            tempBson.Add("itemId", itemMappingDic[sourceMat.Int("itemId")]);

                            foreach (var element in sourceMat.Elements)
                            {
                                if (notNeedColumn.Contains(element.Name) == false)
                                {
                                    tempBson.Add(element.Name, element.Value);
                                }
                            }

                            StorageData tempData = new StorageData();
                            tempData.Name = "ProjectCIItemMatRelation";
                            tempData.Document = tempBson;
                            tempData.Type = StorageType.Insert;
                            matDataList.Add(tempData);
                        }
                    }

                    result = dataOp.BatchSaveStorageData(matDataList);
                    if (result.Status == Status.Failed) throw new Exception(result.Message);
                    #endregion

                    #endregion

                    #region 导入成果关联

                    #region 取值
                    List<BsonDocument> sourctRetList = new List<BsonDocument>();    //源成果关联

                    if (importType == 1)
                    {
                        sourctRetList = dataOp.FindAllByQuery("XH_ProductDev_LineItemRetRelation", Query.And(
                            Query.EQ("lineId", lineId.ToString()),
                            Query.EQ("treeId", treeId.ToString()),
                            Query.EQ("combinationId", combId.ToString())
                        )).ToList();
                    }
                    else if (importType == 2)
                    {
                        sourctRetList = dataOp.FindAllByQuery("ProjectCIItemRetRelation", Query.EQ("listId", srcListId.ToString())).ToList();
                    }
                    #endregion

                    #region 循环导入
                    List<StorageData> retDataList = new List<StorageData>();

                    foreach (var sourceRet in sourctRetList) //循环导入价值项
                    {
                        if (itemMappingDic.ContainsKey(sourceRet.Int("itemId")) == true)    //有对应项
                        {
                            BsonDocument tempBson = new BsonDocument();

                            tempBson.Add("listId", newListId);
                            tempBson.Add("itemId", itemMappingDic[sourceRet.Int("itemId")]);

                            foreach (var element in sourceRet.Elements)
                            {
                                if (notNeedColumn.Contains(element.Name) == false)
                                {
                                    tempBson.Add(element.Name, element.Value);
                                }
                            }

                            StorageData tempData = new StorageData();
                            tempData.Name = "ProjectCIItemRetRelation";
                            tempData.Document = tempBson;
                            tempData.Type = StorageType.Insert;
                            retDataList.Add(tempData);
                        }
                    }

                    result = dataOp.BatchSaveStorageData(retDataList);
                    if (result.Status == Status.Failed) throw new Exception(result.Message);
                    #endregion

                    #endregion
                }

                json.Success = true;
            }
            catch (Exception e)
            {
                json.Message = e.Message;
                json.Success = false;
            }

            return Json(json);
        }

        /// <summary>
        /// 获取某个项目下的配置清单列表
        /// </summary>
        /// <param name="projId"></param>
        /// <returns></returns>
        public ActionResult GetProjectCIList(int projId, int treeId)
        {
            int propertyId = PageReq.GetFormInt("propertyId");//增加业态过滤
            List<Hashtable> retList = new List<Hashtable>();

            List<BsonDocument> ciList = new List<BsonDocument>();

            if (treeId == 0)
            {
                ciList = dataOp.FindAllByQuery("ProjectCIlist", Query.EQ("projId", projId.ToString())).ToList();  //获取所有配置单
            }
            else
            {
                ciList = dataOp.FindAllByQuery("ProjectCIlist", Query.And(    //获取对应价值树配置清单
                    Query.EQ("projId", projId.ToString()),
                    Query.EQ("treeId", treeId.ToString())
                    )).ToList();
            }
            if (propertyId != 0)
            {
                ciList = ciList.Where(x => x.Int("propertyId") == propertyId).ToList();//过滤业态
            }
            List<BsonDocument> propertyList = dataOp.FindAll("SystemProperty").ToList();            //所有业态

            List<BsonDocument> valTreeList = dataOp.FindAll("XH_ProductDev_ValueTree").ToList();    //所有价值树

            foreach (var tempDoc in ciList)
            {
                BsonDocument tempValTree = valTreeList.Where(t => t.Int("treeId") == tempDoc.Int("treeId")).FirstOrDefault();
                BsonDocument tempProperty = propertyList.Where(t => t.Int("propertyId") == tempDoc.Int("propertyId")).FirstOrDefault();

                tempDoc.Remove("_id");
                tempDoc.Add("name", string.Format("{0}({1})", tempValTree.String("name"), tempProperty.String("name")));

                retList.Add(tempDoc.ToHashtable());
            }

            return this.Json(retList, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 保存任务配置单中,价值项值与材料的关联
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SaveTaskCIItemMatRelInfo(FormCollection saveForm)
        {
            InvokeResult result = new InvokeResult();
            PageJson json = new PageJson();

            string tbName = "XH_DesignManage_TaskCIItemMatRelation";    //表名
            string queryStr = PageReq.GetForm("queryStr");          //定位记录
            string matIds = PageReq.GetForm("matIds");              //材料Id列表

            if (PageReq.GetForm("listId").Trim() == "" || PageReq.GetForm("itemId").Trim() == "")
            {
                json.Success = false;
                json.Message = "传入参数有误!";
                return Json(json);
            }

            BsonDocument dataBson = new BsonDocument();             //数据

            foreach (var tempKey in saveForm.AllKeys)
            {
                if (tempKey == "tbName" || tempKey == "queryStr" || tempKey.Contains("matIds")) continue;

                dataBson.Add(tempKey, saveForm[tempKey]);
            }

            var query = TypeConvert.NativeQueryToQuery(queryStr); //定位关联

            if (queryStr != "")  //编辑材料记录
            {
                result = dataOp.Save(tbName, query, dataBson);    //保存关联
            }
            else if (matIds.Trim() != "")       //有选择材料
            {
                List<StorageData> allDataList = new List<StorageData>();
                List<string> matIdList = matIds.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();

                foreach (var matId in matIdList)
                {
                    BsonDocument tempBson = new BsonDocument();

                    tempBson.Add("listId", dataBson.String("listId"));
                    tempBson.Add("itemId", dataBson.String("itemId"));
                    tempBson.Add("matId", matId);

                    StorageData tempData = new StorageData();
                    tempData.Document = tempBson;
                    tempData.Name = tbName;
                    tempData.Type = StorageType.Insert;

                    allDataList.Add(tempData);
                }

                result = dataOp.BatchSaveStorageData(allDataList);
            }
            else
            {
                result = dataOp.Save(tbName, query, dataBson);    //保存关联
            }

            json = TypeConvert.InvokeResultToPageJson(result);

            return Json(json);
        }

        /// <summary>
        /// 保存项目配置单中,价值项值与材料的关联
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SaveProjCIItemMatRelInfo(FormCollection saveForm)
        {
            InvokeResult result = new InvokeResult();
            PageJson json = new PageJson();

            string tbName = "ProjectCIItemMatRelation";    //表名
            string queryStr = PageReq.GetForm("queryStr");          //定位记录
            string matIds = PageReq.GetForm("matIds");              //材料Id列表

            if (PageReq.GetForm("listId").Trim() == "" || PageReq.GetForm("itemId").Trim() == "")
            {
                json.Success = false;
                json.Message = "传入参数有误!";
                return Json(json);
            }

            BsonDocument dataBson = new BsonDocument();             //数据

            foreach (var tempKey in saveForm.AllKeys)
            {
                if (tempKey == "tbName" || tempKey == "queryStr" || tempKey.Contains("matIds")) continue;

                dataBson.Add(tempKey, saveForm[tempKey]);
            }

            var query = TypeConvert.NativeQueryToQuery(queryStr); //定位关联

            if (queryStr != "")  //编辑材料记录
            {
                result = dataOp.Save(tbName, query, dataBson);    //保存关联
            }
            else if (matIds.Trim() != "")       //有选择材料
            {
                List<StorageData> allDataList = new List<StorageData>();
                List<string> matIdList = matIds.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();

                foreach (var matId in matIdList)
                {
                    BsonDocument tempBson = new BsonDocument();

                    tempBson.Add("listId", dataBson.String("listId"));
                    tempBson.Add("itemId", dataBson.String("itemId"));
                    tempBson.Add("matId", matId);

                    StorageData tempData = new StorageData();
                    tempData.Document = tempBson;
                    tempData.Name = tbName;
                    tempData.Type = StorageType.Insert;

                    allDataList.Add(tempData);
                }

                result = dataOp.BatchSaveStorageData(allDataList);
            }
            else
            {
                result = dataOp.Save(tbName, query, dataBson);    //保存关联
            }

            json = TypeConvert.InvokeResultToPageJson(result);

            return Json(json);
        }


        /// <summary>
        /// 保存提交上来的数据
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SaveTaskInfo(FormCollection saveForm)
        {
            string tbName = saveForm["tbName"] != null ? saveForm["tbName"] : "";
            string queryStr = saveForm["queryStr"] != null ? saveForm["queryStr"] : "";
            string dataStr = saveForm["dataStr"] != null ? saveForm["dataStr"] : "";
            string taskId = PageReq.GetForm("taskId");
            var curStartData = saveForm["curStartData"];
            var curEndData = saveForm["curEndData"];
            var period = saveForm["period"];
            var oldTask = dataOp.FindOneByKeyVal(tbName, "taskId", taskId);
            bool needSyncStartDate = false;//是否需要同步开始日期
            bool needSyncEndDate = false;//是否需要同步结束日期
            bool needSyncPeriod = false;//是否需要同步工期
            if (oldTask != null)
            {
                var oldStartData = oldTask.Date("curStartData");
                var oldEndData = oldTask.Date("curEndData");
                var oldPeriod = oldTask.Text("period");
                if (!string.IsNullOrEmpty(curStartData) && oldStartData != DateTime.Parse(curStartData))
                    needSyncStartDate = true;
                if (!string.IsNullOrEmpty(curEndData) && oldEndData != DateTime.Parse(curEndData))
                    needSyncEndDate = true;
                if (oldPeriod != period)
                    needSyncPeriod = true;
            }
            if (dataStr.Trim() == "")
            {
                foreach (var tempKey in saveForm.AllKeys)
                {
                    if (tempKey == "tbName" || tempKey == "queryStr" || tempKey.Contains("splitUserId") || tempKey.Contains("splitProfId") || tempKey.Contains("ownerUserId") || tempKey.Contains("ownerProfId")) continue;

                    dataStr += string.Format("{0}={1}&", tempKey, saveForm[tempKey]);
                }
            }

            InvokeResult result = dataOp.Save(tbName, queryStr, dataStr);

            if (result.Status == Status.Successful)
            {
                if (needSyncStartDate == true)
                {

                    PatchSyncFormulaDate("S" + result.BsonInfo.Int("taskId") + ",", result.BsonInfo.Text("projId"));
                }
                if (needSyncEndDate == true)
                {

                    PatchSyncFormulaDate("E" + result.BsonInfo.Int("taskId") + ",", result.BsonInfo.Text("projId"));
                }
                if (needSyncPeriod == true)
                {

                    PatchSyncFormulaDate("G" + result.BsonInfo.Int("taskId") + ",", result.BsonInfo.Text("projId"));
                }


                #region 保存任务负责人
                int ownerUserId = PageReq.GetFormInt("ownerUserId");
                int ownerProfId = PageReq.GetFormInt("ownerProfId");


                if (ownerUserId != -1)
                {
                    //旧的负责人
                    BsonDocument oldOwner = dataOp.FindOneByQueryStr("XH_DesignManage_TaskManager", "taskId=" + result.BsonInfo.Int("taskId") + "&type=" + (int)TaskManagerType.TaskOwner);   //旧的负责人

                    var ownerQuery = Query.Null;
                    BsonDocument ownerBson = new BsonDocument();
                    ownerBson.Add("userId", ownerUserId.ToString());
                    ownerBson.Add("profId", ownerProfId.ToString());
                    ownerBson.Add("taskId", result.BsonInfo.String("taskId"));
                    ownerBson.Add("type", ((int)TaskManagerType.TaskOwner).ToString());
                    InvokeResult ownerResult = new InvokeResult();
                    if (oldOwner != null)
                    {
                        ownerQuery = Query.EQ("relId", oldOwner.String("relId"));
                        ownerResult = dataOp.Save("XH_DesignManage_TaskManager", ownerQuery, ownerBson);
                    }
                    else
                    {
                        ownerResult = dataOp.Insert("XH_DesignManage_TaskManager", ownerBson);
                    }
                    #region 获取人所在的部门
                    var userOrgPost = dataOp.FindOneByKeyVal("UserOrgPost", "userId", ownerUserId.ToString());
                    if (userOrgPost != null)
                    {
                        var orgPost = dataOp.FindOneByKeyVal("OrgPost", "postId", userOrgPost.Text("postId"));
                        if (orgPost != null)
                        {
                            var org = dataOp.FindOneByKeyVal("Organization", "orgId", orgPost.Text("orgId"));
                            if (org != null)
                            {
                                result.BsonInfo.Add("orgId", org.Text("orgId"));
                                result.BsonInfo.Add("orgName", org.Text("name"));
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    var tempQuery = Query.And(Query.EQ("taskId", result.BsonInfo.String("taskId")), Query.EQ("type", ((int)TaskManagerType.TaskOwner).ToString()));
                    result = _dataOp.Delete("XH_DesignManage_TaskManager", tempQuery);
                }
                #endregion




            }

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        #endregion

        #region  脉络图基类相关
        /// <summary>
        /// 获取任务节点
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public JsonResult projTaskList(int id)
        {
            var dataOp = new DataOperation();
            var taskList = dataOp.FindAllByKeyVal("XH_DesignManage_Task", "projId", id.ToString()).ToList();
            var taskManagerList = dataOp.FindAllByKeyValList("XH_DesignManage_TaskManager", "taskId", taskList.Select(t => t.String("taskId")).ToList());
            var allFileList = dataOp.FindAllByQueryStr("FileRelation", "tableName=XH_DesignManage_Task").ToList();  //所有任务文档
            var allLevelList = dataOp.FindAll("XH_DesignManage_ConcernLevel").ToList();                             //所有级别

            List<string> userIdList = new List<string>();
            userIdList.AddRange(taskList.Select(t => t.String("createUserId")).Distinct().ToList());
            userIdList.AddRange(taskManagerList.Select(t => t.String("userId")).Distinct().ToList());

            var allUserList = dataOp.FindAllByKeyValList("SysUser", "userId", userIdList).ToList();

            List<object> taskListJson = new List<object>();

            foreach (var task in taskList.OrderBy(t => t.String("nodeKey")))
            {
                BsonDocument tempLevel = allLevelList.Where(t => t.Int("levelId") == task.Int("levelId")).FirstOrDefault();
                List<BsonDocument> tempFileList = allFileList.Where(t => t.Int("keyValue") == task.Int("taskId")).ToList();
                BsonDocument tempCreateUser = allUserList.Where(t => t.Int("userId") == task.Int("createUserId")).FirstOrDefault();
                //BsonDocument tempSpliterUser = null;
                //if (taskManagerList.Where(c => c.Int("type") == (int)TaskManagerType.PlanSpliter).Count() > 0)
                //{
                //    var tempUser = taskManagerList.Where(c => c.Int("type") == (int)TaskManagerType.PlanSpliter).FirstOrDefault();
                //    tempSpliterUser = allUserList.Where(t => t.Int("userId") == tempUser.Int("userId")).FirstOrDefault();
                //}
                //BsonDocument tempOwnerUser = null;
                //if (taskManagerList.Where(c => c.Int("type") == (int)TaskManagerType.TaskOwner).Count() > 0)
                //{
                //    var tempUser = taskManagerList.Where(c => c.Int("type") == (int)TaskManagerType.TaskOwner).FirstOrDefault();
                //    tempOwnerUser = allUserList.Where(t => t.Int("userId") == tempUser.Int("userId")).FirstOrDefault();
                //}
                //BsonDocument tempJoinerUser = null;
                //if (taskManagerList.Where(c => c.Int("type") == (int)TaskManagerType.TaskJoiner).Count() > 0)
                //{
                //    var tempUser = taskManagerList.Where(c => c.Int("type") == (int)TaskManagerType.TaskJoiner).FirstOrDefault();
                //    tempJoinerUser = allUserList.Where(t => t.Int("userId") == tempUser.Int("userId")).FirstOrDefault();
                //}

                taskListJson.Add(new
                {
                    nodeId = task.Text("diagramId"),
                    taskId = task.Text("taskId"),
                    name = task.Text("name"),
                    needSplit = !string.IsNullOrEmpty(task.Text("diagramId")) ? task.Text("diagramId") : "0",
                    planStartDate = task.ShortDate("planStartDate"),
                    planEndDate = task.ShortDate("planStartDate"),
                    spliterName = "",                               //tempSpliterUser != null ? tempSpliterUser.String("name") : "",
                    ownerName = "",                                 //tempOwnerUser != null ? tempOwnerUser.String("name") : "",
                    joinerName = "",                                //tempJoinerUser != null ? tempJoinerUser.String("name") : "",
                    completedWork = "0",
                    unCompletedWork = "0",
                    fileCount = tempFileList.Count(),
                    createrName = tempCreateUser != null ? tempCreateUser.String("name") : "",
                    levelId = !string.IsNullOrEmpty(task.Text("levelId")) ? task.Text("levelId") : "-1",
                    levelName = tempLevel != null ? tempLevel.String("name") : "",
                    status = task.Text("status"),
                    ExemptDetail = "",
                    yCardDate = task.Text("yCardDate") != null ? task.Text("yCardDate") : "0",
                    rCardDate = task.Text("rCardDate") != null ? task.Text("rCardDate") : "0",
                });
            }

            return this.Json(taskListJson, JsonRequestBehavior.AllowGet);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult InitialContextDiagram()
        {


            DataOperation dataOp = new DataOperation();
            var result = new InvokeResult();
            var json = new PageJson();
            var path = PageReq.GetForm("path");
            path = Server.MapPath(path);
            if (!System.IO.File.Exists(path))
            {
                json.Success = false;
                json.Message = "文件不存在";
                return Json(json);
            }
            XDocument xdoc = XDocument.Load(path);
            var rGraphModel = xdoc.Element("rGraphModel");
            List<BsonDocument> addList = new List<BsonDocument>();
            Dictionary<BsonDocument, string> updateList = new Dictionary<BsonDocument, string>();

            foreach (var ele in rGraphModel.Elements("Shape"))
            {
                if (ele.Attribute("label") != null && ele.Attribute("id") != null)
                {
                    var label = ele.Attribute("label").Value;
                    var id = ele.Attribute("id").Value;
                    var type = ele.Attribute("type") != null ? ele.Attribute("type").Value : "0";
                    if (string.IsNullOrEmpty(label))
                    {
                        var labObj = rGraphModel.Elements("Label").Where(c => c.Attribute("id") != null && c.Attribute("id").Value == string.Format("lb{0}", ele.Attribute("id").Value)).FirstOrDefault();
                        if (labObj != null && labObj.Attribute("label") != null)
                        {
                            label = labObj.Attribute("label").Value;
                        }
                        if (labObj != null && labObj.Attribute("type") != null)
                        {
                            type = labObj.Attribute("type").Value;
                        }

                    }

                    if (string.IsNullOrEmpty(label))
                    {
                        continue;
                    }

                    try
                    {

                        var oldDiagObj = dataOp.FindOneByKeyVal("XH_DesignManage_ContextDiagram", "xmlId", id);
                        if (oldDiagObj != null)
                        {
                            var updateStr = string.Format("name={0}&type={1}", label.Replace("\n", ""), type);
                            updateList.Add(oldDiagObj, updateStr);
                        }
                        else
                        {
                            var keyTask = 0;
                            if (ele.Attribute("points") != null && ele.Attribute("points").Value == "0")
                            {
                                keyTask = 1;
                            }

                            oldDiagObj = new BsonDocument();
                            oldDiagObj.Add("xmlId", id);
                            oldDiagObj.Add("needApproval", "0");
                            oldDiagObj.Add("type", type);
                            oldDiagObj.Add("keyTask", keyTask);
                            oldDiagObj.Add("name", label.Replace("\n", ""));
                            addList.Add(oldDiagObj);
                        }


                    }
                    catch (Exception ex)
                    {
                        json.Success = false;
                        json.Message = ex.Message;
                    }
                }
            }

            var conDiagramBll = ContextDiagramBll._(dataOp);
            result = conDiagramBll.Update("XH_DesignManage_ContextDiagram", addList, updateList);
            if (result.Status == Status.Successful)
            {
                var planBll = DesignManage_PlanBll._(dataOp);
                var defaultPlan = planBll.FindGroupExpLib();
                if (defaultPlan == null)
                {
                    defaultPlan = new BsonDocument();
                    defaultPlan.Add("name", "集团模板");
                    defaultPlan.Add("isExpTemplate", "1");
                    defaultPlan.Add("isPrimLib", "1");
                    defaultPlan.Add("mapId", "1");
                    defaultPlan.Add("orgId", "1");
                    result = dataOp.Insert("XH_DesignManage_Plan", defaultPlan);
                    if (result.Status != Status.Successful)
                    {

                        json.Success = result.Status == Status.Successful;
                        json.Message = "请先创建集团模板";
                        return Json(json);
                    }
                    defaultPlan = result.BsonInfo;
                }
                result = planBll.InititalDiagramTask(defaultPlan);
            }
            json.Success = result.Status == Status.Successful;
            json.Message = result.Message;
            return this.Json(json);
        }

        #endregion

        #region 地铁图
        /// <summary>
        /// 决策点数据源
        /// </summary>
        /// <returns></returns>
        public JsonResult DecisionPointList()
        {
            var dataOp = new DataOperation();
            int secdPlanId = PageReq.GetParamInt("secdPlanId");
            var plan = dataOp.FindOneByKeyVal("XH_DesignManage_Plan", "planId", secdPlanId.ToString());
            if (plan != null && !string.IsNullOrEmpty(plan.Text("mapId")))
            {
                var map = dataOp.FindOneByKeyVal("XH_DesignManage_SubwayMap", "mapId", plan.Text("mapId"));     //地铁图

                var relList = dataOp.FindAllByKeyVal("XH_DesignManage_MapPointRelation", "mapId", plan.Text("mapId"));  //所有地铁图与节点关联

                if (map != null)
                {

                    var pointList = dataOp.FindAllByKeyValList("XH_DesignManage_DecisionPoint", "pointId", relList.Select(t => t.String("pointId")).ToList());  //map.ChildBsonList("XH_DesignManage_MapPointRelation");
                    var pointListJson = from point in pointList
                                        select new
                                        {
                                            pointId = point.Text("pointId"),
                                            name = point.String("name"),
                                            type = (DecisionPointType)(point.Int("type")),
                                            height = point.Text("height"),
                                            width = point.Text("width"),
                                            radius = point.Text("radius"),
                                            color = point.Text("color"),
                                            text = point.Text("text"),
                                            textColor = point.Text("textColor"),
                                            textSize = point.Text("textSize"),
                                            pointX = point.Text("pointX"),
                                            pointY = point.Text("pointY"),
                                            status = GetPointStatus(point, secdPlanId),
                                            taskId = point.ChildBsonList("XH_DesignManage_Task").Where(c => c.Int("planId") == plan.Int("planId")).Count() > 0 ? point.ChildBsonList("XH_DesignManage_Task").Where(c => c.Int("planId") == plan.Int("planId")).FirstOrDefault().Text("taskId") : "",
                                            textId = point.Text("textId"),
                                            textPId = point.Text("textPId")
                                        };
                    var mapPointListJson = new
                    {
                        mapId = map.Text("mapId"),
                        name = map.Text("name"),
                        imagePath = map.Text("imagePath"),
                        width = map.Text("width"),
                        height = map.Text("height"),
                        pointList = pointListJson,

                    };
                    return this.Json(mapPointListJson, JsonRequestBehavior.AllowGet);
                }
            }
            return this.Json("", JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 获取决策点状态
        /// </summary>
        /// <param name="secdPlanId"></param>
        /// <param name="pointId"></param>
        /// <returns></returns>
        public int GetPointStatus(BsonDocument decisionPoint, int secdPlanId)
        {
            DataOperation dataOp = new DataOperation();

            if (decisionPoint == null)
            {
                return (int)DecisionPointStatus.NotStatus;
            }
            var taskList = decisionPoint.ChildBsonList("XH_DesignManage_Task").Where(c => c.Int("planId") == secdPlanId);
            if (taskList.Count() == 0)
                return (int)DecisionPointStatus.NotStatus;
            var now = DateTime.Now;

            //只要有任务延迟,地铁图节点就延迟
            if (taskList.Where(m => m.Date("curEndData") != DateTime.MinValue && now > m.Date("curEndData") && m.Int("status") != (int)TaskStatus.Completed).Count() > 0)
            {
                return (int)DecisionPointStatus.Delayed;
            }
            else
            {
                List<int> allStatusList = taskList.Select(t => t.Int("status")).Distinct().ToList();   //当前任务的所有状态

                if (allStatusList.Contains((int)TaskStatus.Processing)) //只要有进行中的任务,地铁图节点就是进行中
                {
                    return (int)DecisionPointStatus.Processing;
                }

                if (allStatusList.Count == 1 && allStatusList.FirstOrDefault() == (int)TaskStatus.Completed)    //只有全部是完成才是完成
                {
                    return (int)DecisionPointStatus.Completed;
                }

                return (int)DecisionPointStatus.NotStarted;
            }
        }

        /// <summary>
        /// 添加决策点关联
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult SaveDecisionPoint()
        {
            DataOperation data = new DataOperation();
            PageJson json = new PageJson();
            int taskId = PageReq.GetFormInt("taskId");
            int pointid = PageReq.GetFormInt("pointId");
            int secdPlanId = PageReq.GetFormInt("secdPlanId");
            var fieldValueDic = new Dictionary<string, string>();
            var result = data.QuickUpdate("XH_DesignManage_Task", "taskId", taskId.ToString(), "pointId=" + pointid);
            json.Success = result.Status == Status.Successful;
            json.Message = result.Message;
            return this.Json(json);
        }

        /// <summary>
        /// 添加决策点关联
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult SaveNewDecisionPoint()
        {
            DataOperation data = new DataOperation();
            PageJson json = new PageJson();
            int taskId = PageReq.GetFormInt("taskId");
            var textPid = PageReq.GetForm("pointId");//此处变为地铁图点的textPid
            int secdPlanId = PageReq.GetFormInt("secdPlanId");
            var fieldValueDic = new Dictionary<string, string>();
            var point = dataOp.FindOneByKeyVal("XH_DesignManage_DecisionPoint", "textPId", textPid);
            var result = new InvokeResult();
            if (point != null && !string.IsNullOrEmpty(textPid))
            {
                result = data.QuickUpdate("XH_DesignManage_Task", "taskId", taskId.ToString(), "pointId=" + point.Text("pointId"));
            }
            else
            {
                result = data.QuickUpdate("XH_DesignManage_Task", "taskId", taskId.ToString(), "pointId=");

            }
            json.Success = result.Status == Status.Successful;
            json.Message = result.Message;
            return this.Json(json);
        }


        /// <summary>
        /// 获取地铁图决策点关联任务
        /// </summary>
        /// <param name="id"></param>
        /// <param name="sid"></param>
        /// <returns></returns>
        public JsonResult DecisionPointTask(int id, int sid)
        {
            var textPid = PageReq.GetParam("textPid");
            DataOperation data = new DataOperation();
            List<Item> itmList = new List<Item>();
            var queryStr = string.Format("planId={0}&pointId={1}", id, sid);
            var planTaskList = data.FindAllByQueryStr("XH_DesignManage_Task", queryStr);
            itmList = (from t in planTaskList
                       select new Item()
                       {
                           id = t.Int("taskId"),
                           name = t.Text("name"),
                           type = t.Text("status"),
                           otherParam = t.ShortDate("curEndData"),
                           value = (t.Int("status") == 0 || t.Int("status") == 1) == true ? "未开始" : EnumDescription.GetFieldText((TaskStatus)t.Int("status"))
                       }).ToList();
            return Json(itmList, JsonRequestBehavior.AllowGet);
        }


        /// <summary>
        /// 获取地铁图决策点关联任务
        /// </summary>
        /// <param name="id"></param>
        /// <param name="sid"></param>
        /// <returns></returns>
        public JsonResult DecisionNewPointTask(int id)
        {
            var textPid = PageReq.GetParam("textPid");
            DataOperation data = new DataOperation();
            List<Item> itmList = new List<Item>();
            List<BsonDocument> planTaskList = new List<BsonDocument>();
            var point = dataOp.FindOneByKeyVal("XH_DesignManage_DecisionPoint", "textPId", textPid);
            if (point != null)
            {
                var queryStr = string.Format("planId={0}&pointId={1}", id, point.Text("pointId"));
                planTaskList = data.FindAllByQueryStr("XH_DesignManage_Task", queryStr).ToList();
            }
            itmList = (from t in planTaskList
                       select new Item()
                       {
                           id = t.Int("taskId"),
                           name = t.Text("name"),
                           type = t.Text("status"),
                           otherParam = t.ShortDate("curEndData"),
                           value = (t.Int("status") == 0 || t.Int("status") == 1) == true ? "未开始" : EnumDescription.GetFieldText((TaskStatus)t.Int("status"))
                       }).ToList();
            return Json(itmList, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region 公司经验

        /// <summary>
        /// 从经验库载入计划
        /// </summary>
        /// <param name="id">经验库Id</param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult InitialPlanFormExpLib()
        {
            var copySecPlanId = PageReq.GetFormInt("copySecPlanId");
            var sourceSecPlanId = PageReq.GetFormInt("sourceSecPlanId");
            var OffSetDay = PageReq.GetParamInt("OffSetDay");//任务时间偏移量
            var OffSetMonth = PageReq.GetParamInt("OffSetMonth");//任务时间偏移量
            var OffSetYear = PageReq.GetParamInt("OffSetYear");//任务时间偏移量
            var isIgnorePerson = PageReq.GetParamInt("isIgnorePerson"); //是否忽略人员安排
            var isIgnoreDate = PageReq.GetParamInt("isIgnoreDate"); //是否忽略人员安排
            var isIgnorePassed = PageReq.GetParamInt("isIgnorePassed"); //是否忽略跳过的任务
            DesignManage_PlanBll secPlanBll = DesignManage_PlanBll._();
            secPlanBll.OffSetDay = OffSetDay;
            secPlanBll.OffSetMonth = OffSetMonth;
            secPlanBll.OffSetYear = OffSetYear;
            secPlanBll.IsIgnorePeople = isIgnorePerson == 0 ? false : true;
            secPlanBll.IsIgnoreDate = isIgnoreDate == 0 ? false : true;
            secPlanBll.IsIgnorePassed = isIgnorePassed == 0 ? false : true;
            var result = secPlanBll.CopySecPlan("XH_DesignManage_Plan", copySecPlanId, sourceSecPlanId, dataOp.GetCurrentUserId());
            return Json(ConvertToPageJson(result));
        }


        /// <summary>
        /// 从经验库载入计划,添加计划信息， 计划负责人，负责人专业，快速创建计划
        /// </summary>
        /// <param name="id">经验库Id</param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult QuickyCreatePlan()
        {
            PageJson json = new PageJson();
            var name = PageReq.GetParam("name");
            var takeTime = PageReq.GetParam("takeTime");
            var startData = PageReq.GetParam("startData");
            var endData = PageReq.GetParam("endData");
            var planId = PageReq.GetForm("secdPlanId");
            var remark = PageReq.GetForm("remark");
            var copySecPlanId = PageReq.GetFormInt("copySecPlanId");
            var projId = PageReq.GetFormInt("projId");
            var sourceSecPlanId = PageReq.GetFormInt("sourceSecPlanId");
            var managerId = PageReq.GetParamInt("managerId");//辅助计划负责人Id
            var managerSysProfId = PageReq.GetParamInt("managerSysProfId");//辅助计划专业Id
            var grantCurrentUser = PageReq.GetParamInt("grantCurrentUser");//0 或者1
            var userId = dataOp.GetCurrentUserId();//当前用户Id辅助计划负责人Id
            var userSysProfId = PageReq.GetParamInt("userSysProfId");//当前用户专业Id
            var OffSetDay = PageReq.GetParamInt("OffSetDay");//任务时间偏移量
            var OffSetMonth = PageReq.GetParamInt("OffSetMonth");//任务时间偏移量
            var OffSetYear = PageReq.GetParamInt("OffSetYear");//任务时间偏移量
            var isIgnorePerson = PageReq.GetParamInt("isIgnorePerson"); //是否忽略人员安排
            var isIgnoreDate = PageReq.GetParamInt("isIgnoreDate"); //是否忽略人员安排
            var isIgnorePassed = PageReq.GetParamInt("isIgnorePassed"); //是否忽略跳过的任务
            var isContractPlan = PageReq.GetParamInt("isContractPlan"); //是否合同费用支付计划
            var isSalesPlan = PageReq.GetParamInt("isSalesPlan").ToString();//是否是售楼准备工程计划
            var isDeliveryPlan = PageReq.GetParamInt("isDeliveryPlan").ToString();//是否是整盘交付计划
            DesignManage_PlanBll secPlanBll = DesignManage_PlanBll._();
            secPlanBll.OffSetDay = OffSetDay;
            secPlanBll.OffSetMonth = OffSetMonth;
            secPlanBll.OffSetYear = OffSetYear;
            secPlanBll.IsIgnorePeople = isIgnorePerson == 0 ? false : true;
            secPlanBll.IsIgnoreDate = isIgnoreDate == 0 ? false : true;
            secPlanBll.IsIgnorePassed = isIgnorePassed == 0 ? false : true;

            DesignManage_PlanBll planBll = DesignManage_PlanBll._();
            var planResult = new InvokeResult();
            BsonDocument oldSecPlanObj = null;
            var copySecPlanObj = dataOp.FindOneByKeyVal("XH_DesignManage_Plan", "planId", copySecPlanId.ToString());
            if (copySecPlanObj == null)
            {
                json.Success = false;
                json.Message = "传入参数有误请重试！";
                return Json(json);
            }
            bool needInitialSecdPlan = false;
            if (sourceSecPlanId == 0)
            {
                var defaultPlan = dataOp.FindOneByKeyVal("XH_DesignManage_Plan", "projId", projId.ToString());
                if (defaultPlan != null && String.IsNullOrEmpty(defaultPlan.Text("name")))
                {
                    var dPlanUpdateBson = new BsonDocument();
                    dPlanUpdateBson.Set("name", name);
                    dPlanUpdateBson.Set("takeTime", takeTime);
                    dPlanUpdateBson.Set("takeTime", takeTime);
                    dPlanUpdateBson.Set("startData", startData);
                    dPlanUpdateBson.Set("endData", endData);
                    dPlanUpdateBson.Set("remark", remark);
                    dPlanUpdateBson.Set("planType", copySecPlanObj.Text("planType"));
                    dPlanUpdateBson.Set("isContractPlan", copySecPlanObj.Text("isContractPlan"));
                    dPlanUpdateBson.Set("isSalesPlan", isSalesPlan);
                    dPlanUpdateBson.Set("isDeliveryPlan", isDeliveryPlan);
                    planResult = planBll.Update(defaultPlan, dPlanUpdateBson, managerId, managerSysProfId, userId, userSysProfId, grantCurrentUser == 1);
                    if (planResult.Status != Status.Successful)
                    {
                        return Json(ConvertToPageJson(planResult));
                    }
                    else
                    {
                        oldSecPlanObj = planResult.BsonInfo;

                        if (oldSecPlanObj != null)
                        {
                            sourceSecPlanId = oldSecPlanObj.Int("planId");
                        }
                    }
                }
                else
                {
                    var plan = new BsonDocument();
                    plan.Add("name", name);
                    plan.Add("projId", projId.ToString());
                    plan.Add("takeTime", takeTime);
                    plan.Add("startData", startData);
                    plan.Add("endData", endData);
                    plan.Add("remark", remark);
                    plan.Add("planType", copySecPlanObj.Text("planType"));
                    plan.Add("isContractPlan", copySecPlanObj.Text("isContractPlan"));
                    plan.Set("isSalesPlan", isSalesPlan);
                    plan.Set("isDeliveryPlan", isDeliveryPlan);
                    //plan.Add("isDefault", 1);
                    planResult = planBll.Update(plan, managerId, managerSysProfId, userId, userSysProfId, grantCurrentUser == 1);

                }
                if (planResult.Status != Status.Successful)
                {
                    return Json(ConvertToPageJson(planResult));
                }
                else
                {
                    oldSecPlanObj = planResult.BsonInfo;

                    if (oldSecPlanObj != null)
                    {
                        sourceSecPlanId = oldSecPlanObj.Int("planId");
                    }
                }
            }
            else
            {
                needInitialSecdPlan = true;
                oldSecPlanObj = dataOp.FindOneByKeyVal("XH_DesignManage_Plan", "planId", sourceSecPlanId.ToString());
            }

            if (oldSecPlanObj == null)
            {
                json.Success = false;
                json.Message = "传入参数有误请重试！";
                return Json(json);
            }
            if (needInitialSecdPlan)
            {
                #region  初始化
                List<BsonDocument> secPlanManagerList = new List<BsonDocument>();
                Dictionary<int, int> projProfIdList = new Dictionary<int, int>();
                if (managerId != 0)
                {
                    BsonDocument secPlanManager = new BsonDocument();
                    secPlanManager.Add("planId", sourceSecPlanId.ToString());
                    secPlanManager.Add("userId", managerId.ToString());
                    secPlanManagerList.Add(secPlanManager);
                    //为该用户添加专业
                    if (!projProfIdList.ContainsKey(managerId) && managerSysProfId != 0)
                    {
                        projProfIdList.Add(managerId, managerSysProfId);
                    }
                }
                #region 设置当前用户为计划负责人
                if (grantCurrentUser == 1)
                {

                    //为该用户添加专业
                    if (!projProfIdList.ContainsKey(userId) && userSysProfId != 0)
                    {
                        projProfIdList.Add(userId, userSysProfId);
                    }
                }
                #endregion
                var updateBson = new BsonDocument();

                updateBson.Set("name", name);
                updateBson.Set("startData", startData);
                updateBson.Set("endData", endData);
                updateBson.Set("takeTime", takeTime);
                updateBson.Set("planType", copySecPlanObj.Text("planType"));
                updateBson.Set("isContractPlan", copySecPlanObj.Text("isContractPlan"));
                updateBson.Set("isSalesPlan", isSalesPlan);
                updateBson.Set("isDeliveryPlan", isDeliveryPlan);
                //oldSecPlanObj.actualStartDate = secPlan.actualStartDate;
                var updateResult = secPlanBll.Update(oldSecPlanObj, updateBson, secPlanManagerList, projProfIdList);
                if (updateResult.Status != Status.Successful)
                {
                    return Json(ConvertToPageJson(updateResult));
                }
                #endregion
            }

            //删除原来计划中的所有任务
            InvokeResult result = dataOp.Delete("XH_DesignManage_Task", Query.EQ("planId", sourceSecPlanId.ToString()));

            if (result.Status == Status.Successful)
            {
                result = secPlanBll.CopySecPlan("XH_DesignManage_Plan", copySecPlanId, sourceSecPlanId, dataOp.GetCurrentUserId());
            }
            return Json(ConvertToPageJson(result));
        }

        /// <summary>
        /// 将计划设为模板
        /// </summary>
        /// <param name="id">经验库Id</param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult SaveAsExpLib()
        {
            var name = PageReq.GetParam("name");
            var planId = PageReq.GetParamInt("secdPlanId");
            var validStartDate = PageReq.GetParam("validStartDate");
            var validEndDate = PageReq.GetParam("validEndDate");
            var remark = PageReq.GetParam("remark");
            var type = PageReq.GetParam("type");//新增类型住宅或者商业
            var orgId = PageReq.GetParam("orgId");
            DesignManage_PlanBll secPlanBll = DesignManage_PlanBll._();
            var srcPlan = dataOp.FindOneByKeyVal("XH_DesignManage_Plan", "planId", planId.ToString());
            PageJson json = new PageJson();
            json.Success = true;
            if (string.IsNullOrEmpty(name) || srcPlan == null)
            {
                json.Success = false;
                json.Message = "传入参数有误请重试！";
            }
            else
            {

                var patternIds = PageReq.GetFormList("patternIds");

                var expLib = new BsonDocument();
                expLib.Add("name", name);
                expLib.Add("validStartDate", validStartDate);
                expLib.Add("validEndDate", validEndDate);
                expLib.Add("isExpTemplate", "1");
                expLib.Add("remark", remark);
                expLib.Add("orgId", orgId);
                expLib.Add("type", type);
                expLib.Add("planType", srcPlan.Text("planType"));//1,2级计划
                expLib.Add("isContractPlan", srcPlan.Text("isContractPlan"));//1,2级计划
                //expLib.Add("isSalesPlan", srcPlan.Text("isSalesPlan"));//是否是售楼工程计划
                //expLib.Add("isDeliveryPlan", srcPlan.Text("isDeliveryPlan"));//是否是整盘交付计划
                InvokeResult result = dataOp.Insert("XH_DesignManage_Plan", expLib);

                if (result.Status == Status.Successful)
                {
                    var curExpLibObj = result.BsonInfo;
                    if (curExpLibObj != null)
                    {
                        var copyResult = secPlanBll.CopySecPlan("XH_DesignManage_Plan", planId, curExpLibObj.Int("planId"), dataOp.GetCurrentUserId());
                        if (copyResult.Status != Status.Successful)
                        {
                            return Json(ConvertToPageJson(copyResult));
                        }

                    }
                }

                json = this.ConvertToPageJson(result);
            }
            return this.Json(json, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 保存经验库编辑
        /// </summary>
        /// <param name="expLibObj"></param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult ExperienceDelete(int id)
        {

            var result = dataOp.QuickDelete("XH_DesignManage_Plan", "planId", id.ToString());
            return Json(ConvertToPageJson(result));
        }

        /// <summary>
        /// 查看模板名称是否出错
        /// </summary>
        /// <returns></returns>
        public JsonResult HasExistExpLibObj()
        {
            JsonResult json = new JsonResult();
            var expLibName = PageReq.GetForm("name");
            var existObj = dataOp.FindOneByQueryStr("XH_DesignManage_Plan", string.Format("isExpTemplate=1&name={0}", expLibName));
            var hasExist = existObj != null && existObj.Count() > 0;
            json.Data = new
            {
                success = hasExist,
                msg = "页面参数错误"
            };
            return this.Json(json, JsonRequestBehavior.AllowGet); ;
        }


        /// <summary>
        /// 根据分项Id获取任务列表
        /// </summary>
        /// <returns></returns>
        public JsonResult PlanAndTaskList()
        {

            int secdPlanId = PageReq.GetParamInt("secdPlanId");
            var plan = dataOp.FindOneByKeyVal("XH_DesignManage_Plan", "planId", secdPlanId.ToString());             //获取计划
            var parentTask = plan.ChildBsonList("XH_DesignManage_Task").Where(m => m.Int("nodePid") == 0).FirstOrDefault();
            var taskList = plan.ChildBsonList("XH_DesignManage_Task").OrderBy(c => c.Text("nodeKey")).ToList();
            var taskManagerList = dataOp.FindAllByKeyValList("XH_DesignManage_TaskManager", "taskId", taskList.Select(t => t.String("taskId")).ToList()).ToList();
            var secPlanManager = plan.ChildBsonList("XH_DesignManage_PlanManager").FirstOrDefault();//计划负责人 
            var taskFileList = dataOp.FindAllByQuery("FileRelation",
                        Query.And(
                                Query.EQ("tableName", "XH_DesignManage_Task"),
                                Query.In("keyValue", taskList.Select(p => p.GetValue("taskId", string.Empty)))
                        )
                ).ToList();
            var taskCreateUserList = dataOp.FindAllByQuery("SysUser", Query.In("userId", taskList.Select(p => (BsonValue)p.Text("createUserId")))).ToList();
            var taskConcernLevelList = dataOp.FindAll("XH_DesignManage_ConcernLevel").ToList();
            var sysProfId = 0;


            var taskListJson = from task in taskList
                               select new
                               {
                                   taskId = task.Text("taskId"),
                                   nodePid = task.Int("nodePid") == parentTask.Int("taskId") ? 0 : task.Int("nodePid"),
                                   name = task.Text("name"),
                                   needSplit = task.Int("needSplit"),
                                   planStartDate = task.ShortDate("curStartData"),
                                   planEndDate = task.ShortDate("curEndData"),
                                   sysprofids = "",
                                   sysstageids = "",
                                   spliterName = taskManagerList.Where(c => c.Int("type") == (int)TaskManagerType.PlanSpliter).Count() > 0 ? taskManagerList.Where(c => c.Int("type") == (int)TaskManagerType.PlanSpliter).FirstOrDefault().SourceBsonField("userId", "name") : "",
                                   ownerName = taskManagerList.Where(c => c.Int("type") == (int)TaskManagerType.TaskOwner).Count() > 0 ? taskManagerList.Where(c => c.Int("type") == (int)TaskManagerType.TaskOwner).FirstOrDefault().SourceBsonField("userId", "name") : "",
                                   joinerName = taskManagerList.Where(c => c.Int("type") == (int)TaskManagerType.TaskJoiner).Count() > 0 ? taskManagerList.Where(c => c.Int("type") == (int)TaskManagerType.TaskJoiner).FirstOrDefault().SourceBsonField("userId", "name") : "",
                                   completedWork = "0",
                                   unCompletedWork = "0",
                                   //fileCount = dataOp.FindAllByQueryStr("FileRelation", "tableName=XH_DesignManage_Task&keyValue=" + task.Text("taskId")).Count(),
                                   fileCount = taskFileList.Where(p => p.Text("keyValue").Equals(task.Text("taskId"))).Count(),
                                   //createrName = task.SourceBson("createUserId") != null ? task.SourceBson("createUserId").Text("name") : string.Empty,
                                   createrName = taskCreateUserList.Where(p => p.Text("userId").Equals(task.Text("createUserId"))).FirstOrDefault().Text("name"),
                                   levelId = !string.IsNullOrEmpty(task.Text("levelId")) ? task.Text("levelId") : "-1",
                                   //levelName = task.SourceBson("levelId") != null ? task.SourceBson("levelId").Text("name") : "",
                                   levelName = taskConcernLevelList.Where(p => p.Text("levelId").Equals(task.Text("levelId"))).FirstOrDefault().Text("name"),
                                   status = task.Text("status"),
                                   ExemptDetail = "",
                                   yCardDate = task.Text("yCardDate") != null ? task.Text("yCardDate") : "0",
                                   rCardDate = task.Text("rCardDate") != null ? task.Text("rCardDate") : "0",

                               };
            //2011.4.13修改用于新工作任务
            var enumerList = new List<int>();
            enumerList.Add(1);
            var json = from c in enumerList
                       select
                         new
                         {
                             secPlanName = plan.Text("name"),
                             secPlanStartDate = plan.ShortDate("startData"),
                             secPlanEndDate = plan.ShortDate("endData"),
                             remark = plan.Text("remark"),
                             managerId = secPlanManager.Text("userId"),
                             managerName = secPlanManager.SourceBsonField("userId", "name"),
                             managerSysProfId = sysProfId,

                             taskList = taskListJson

                         };

            return this.Json(json, JsonRequestBehavior.AllowGet);
        }




        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult SaveExpLibObj()
        {
            var name = PageReq.GetParam("name");
            var planId = PageReq.GetParam("secdPlanId");
            var validStartDate = PageReq.GetParam("validStartDate");
            var validEndDate = PageReq.GetParam("validEndDate");
            var remark = PageReq.GetParam("remark");
            var orgId = PageReq.GetParam("orgId");
            var patternIds = PageReq.GetFormList("patternIds");
            var isContractPlan = PageReq.GetParam("isContractPlan");
            var type = PageReq.GetParam("type");//2014.5.5商业或者住宅XH
            var isActive = PageReq.GetParam("isActive");//是否激活 0:激活 1：锁定
            InvokeResult result = new InvokeResult();
            PageJson json = new PageJson();
            json.Success = true;

            var isNew = false;

            if (string.IsNullOrEmpty(name))
            {
                json.Success = false;
                json.Message = "传入参数有误请重试！";
            }
            else
            {
                var expLib = dataOp.FindOneByKeyVal("XH_DesignManage_Plan", "planId", planId.ToString());
                if (expLib == null)
                {
                    expLib = new BsonDocument();
                    expLib.Add("name", name);
                    expLib.Add("validStartDate", validStartDate);
                    expLib.Add("validEndDate", validEndDate);
                    expLib.Add("isExpTemplate", 1);
                    expLib.Add("remark", remark);
                    expLib.Add("orgId", orgId);
                    expLib.Add("isContractPlan", isContractPlan);
                    expLib.Add("isActive", isActive);
                    expLib.Add("type", type);
                    
                    result = dataOp.Insert("XH_DesignManage_Plan", expLib);

                    isNew = true;
                }
                else
                {
                    var updateBson = new BsonDocument();
                    updateBson.Add("name", name);
                    updateBson.Add("validStartDate", validStartDate);
                    updateBson.Add("validEndDate", validEndDate);
                    updateBson.Add("remark", remark);
                    updateBson.Add("orgId", orgId);
                    updateBson.Add("isContractPlan", isContractPlan);
                    updateBson.Add("isActive", isActive);
                    updateBson.Add("type", type);
                    result = dataOp.Update(expLib, updateBson);
                }
                json = this.ConvertToPageJson(result);
            }

            if (result.Status == Status.Successful)
            {
                var curExpLibObj = result.BsonInfo;
                if (curExpLibObj != null && isNew == true)
                {
                    var expLibBll = DesignManage_PlanBll._();
                    #region 默认载入集团模板
                    var groupExpLib = expLibBll.FindGroupExpLib();
                    if (groupExpLib != null)
                    {
                        var copyResult = expLibBll.CopySecPlan("XH_DesignManage_Plan", groupExpLib.Int("planId"), curExpLibObj.Int("planId"), dataOp.GetCurrentUserId());
                        if (copyResult.Status != Status.Successful)
                        {
                            return Json(ConvertToPageJson(copyResult));
                        }
                    }
                    #endregion

                }
            }
            return Json(ConvertToPageJson(result));
        }



        /// <summary>
        /// 初始化并跳转到计划编制页面
        /// </summary>
        /// <param name="id">经验库Id</param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult ModifyExpLibDetail(int id)
        {
            var expLibBll = DesignManage_PlanBll._();
            var json = new PageJson();
            json.Success = true;
            var planId = id;
            var curExpLibObj = dataOp.FindOneByKeyVal("XH_DesignManage_Plan", "planId", planId.ToString());
            #region 默认载入集团模板
            var groupExpLib = expLibBll.FindGroupExpLib();
            if (groupExpLib != null)
            {
                var copyResult = expLibBll.CopySecPlan("XH_DesignManage_Plan", groupExpLib.Int("planId"), curExpLibObj.Int("planId"), dataOp.GetCurrentUserId());
                if (copyResult.Status != Status.Successful)
                {
                    return Json(ConvertToPageJson(copyResult));
                }
            }
            #endregion
            return Json(json);

        }


        /// <summary>
        /// 根据分项Id获取任务列表
        /// </summary>
        /// <returns></returns>
        public JsonResult NewTaskList()
        {
            int secdPlanId = PageReq.GetParamInt("secdPlanId");
            var plan = dataOp.FindOneByKeyVal("XH_DesignManage_Plan", "planId", secdPlanId.ToString());             //获取计划
            var parentTask = plan.ChildBsonList("XH_DesignManage_Task").Where(m => m.Int("nodePid") == 0).FirstOrDefault();
            var taskList = plan.ChildBsonList("XH_DesignManage_Task").OrderBy(c => c.Text("nodeKey")).ToList();
            var taskManagerList = dataOp.FindAllByKeyValList("XH_DesignManage_TaskManager", "taskId", taskList.Select(t => t.String("taskId")).ToList()).ToList();
            var secPlanManager = plan.ChildBsonList("XH_DesignManage_PlanManager").FirstOrDefault();//计划负责人 
            List<BsonDocument> taskFileList = dataOp.FindAllByQuery("FileRelation",
                    Query.And(
                        Query.EQ("tablName", "XH_DesignManage_Task"),
                        Query.In("keyValue", taskList.Select(i => i.GetValue("taskId", string.Empty)))
                    )
                ).ToList();

            #region 任务创建者列表
            var taskCreateUserList = dataOp.FindAllByQuery("SysUser", Query.In("userId", taskList.Select(i => i.GetValue("createUserId"))))
                .Select(i => new BsonDocument
                {
                    {"userId", i.Int("userId")},
                    {"name", i.Text("name")}
                }).ToList();
            #endregion

            #region 任务等级列表
            string taskLevelTbName = string.Empty;
            if (taskList.Count() > 0)
            {
                taskLevelTbName = taskList.FirstOrDefault().SourceBsonField("levelId", "underTable").Trim();
            }
            List<BsonDocument> taskLevelList = new List<BsonDocument>();
            if (!string.IsNullOrWhiteSpace(taskLevelTbName))
            {
                taskLevelList = dataOp.FindAll(taskLevelTbName).ToList();
            }
            #endregion

            List<BsonDocument> profStageList = dataOp.FindAllByQuery("TaskDocProtery", Query.In("taskId", taskList.Select(i => i.GetValue("taskId", string.Empty)))).ToList();

            List<object> taskListJson = new List<object>();
            #region 获取专业 阶段 类别
            List<BsonDocument> sysProf = dataOp.FindAll("System_Professional").ToList();//系统专业
            List<BsonDocument> sysStageList = dataOp.FindAll("System_Stage").ToList();  //系统阶段
            List<BsonDocument> projFileCatList = dataOp.FindAll("System_FileCategory").ToList(); //系统文档类别
            #endregion
            foreach (var task in taskList)
            {
                #region 获取负责人所属部门

                string ownerOrgName = string.Empty;

                #endregion
                #region 类别拼写
                BsonDocument profStage = profStageList.Where(i => i.Int("taskId") == task.Int("taskId")).FirstOrDefault();//专业阶段类别
                string profStageStr = string.Empty;
                if (profStage != null)
                {
                    var tempProf = sysProf.Where(x => x.Int("profId") == profStage.Int("sysProfId")).FirstOrDefault();
                    tempProf = tempProf != null ? tempProf : new BsonDocument();
                    var tempStage = sysStageList.Where(x => x.Int("stageId") == profStage.Int("stageId")).FirstOrDefault();
                    tempStage = tempStage != null ? tempStage : new BsonDocument();
                    var tempFileCat = projFileCatList.Where(x => x.Int("fileCatId") == profStage.Int("fileCatId")).FirstOrDefault();
                    tempFileCat = tempFileCat != null ? tempFileCat : new BsonDocument();
                    profStageStr = string.Format("{0}-{1}-{2}", tempProf.String("name"), tempStage.String("name"), tempFileCat.String("name"));
                }
                #endregion
                taskListJson.Add(new
                {
                    ownerOrgName = ownerOrgName,
                    taskId = task.Text("taskId"),
                    nodePid = task.Int("nodePid") == parentTask.Int("taskId") ? 0 : task.Int("nodePid"),
                    name = task.Text("name"),
                    needSplit = task.Int("needSplit"),
                    startDate = task.ShortDate("curStartData"),
                    endDate = task.ShortDate("curEndData"),
                    sysprofids = "",
                    sysstageids = "",
                    spliterName = taskManagerList.Where(c => c.Int("type") == (int)TaskManagerType.PlanSpliter).Count() > 0 ? taskManagerList.Where(c => c.Int("type") == (int)TaskManagerType.PlanSpliter).FirstOrDefault().SourceBsonField("userId", "name") : "",
                    ownerName = taskManagerList.Where(c => c.Int("type") == (int)TaskManagerType.TaskOwner).Count() > 0 ? taskManagerList.Where(c => c.Int("type") == (int)TaskManagerType.TaskOwner).FirstOrDefault().SourceBsonField("userId", "name") : "",
                    joinerName = taskManagerList.Where(c => c.Int("type") == (int)TaskManagerType.TaskJoiner).Count() > 0 ? taskManagerList.Where(c => c.Int("type") == (int)TaskManagerType.TaskJoiner).FirstOrDefault().SourceBsonField("userId", "name") : "",
                    completedWork = "0",
                    unCompletedWork = "0",
                    fileCount = taskFileList.Where(i=>i.Int("keyValue")==task.Int("taskId")).Count(),
                    createrName = taskCreateUserList.Where(i=>i.Int("userId")==task.Int("createUserId")).FirstOrDefault().Text("name"),
                    levelId = !string.IsNullOrEmpty(task.Text("levelId")) ? task.Text("levelId") : "-1",
                    levelName = taskLevelList.Where(i=>i.Int("levelId")==task.Int("levelId")).FirstOrDefault().Text("name"),
                    status = task.Int("status", 2),
                    ExemptDetail = "",
                    yCardDate = task.Text("yCardDate") != null ? task.Text("yCardDate") : "0",
                    rCardDate = task.Text("rCardDate") != null ? task.Text("rCardDate") : "0",
                    docClass = profStageStr,
                });
            }

            var a = taskListJson;
            return this.Json(taskListJson, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region 流程设置

        ///// <summary>
        ///// 总部经办人
        ///// </summary>
        ///// <returns></returns>
        //public JsonResult SaveFlowInstanceUser()
        //{
        //}

        /// <summary>
        /// 保存流程步骤
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SaveFlowStepUserRelation()
        {
            int stepId = PageReq.GetFormInt("stepId");
            string userIds = PageReq.GetForm("userIds");

            BsonDocument tempSetp = dataOp.FindOneByKeyVal("BusFlowStep", "stepId", stepId.ToString());
            List<StorageData> userDataList = new List<StorageData>();

            List<string> userIdList = userIds.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();
            List<BsonDocument> oldRelList = dataOp.FindAllByKeyVal("BusFlowStepUserRel", "stepId", tempSetp.String("stepId")).ToList();

            foreach (var tempUserId in userIdList)
            {
                BsonDocument tempOld = oldRelList.Where(t => t.String("userId") == tempUserId).FirstOrDefault();    //旧的关联

                if (tempOld == null)    //没有旧的关联则添加
                {
                    StorageData tempData = new StorageData();
                    tempData.Name = "BusFlowStepUserRel";
                    tempData.Type = StorageType.Insert;
                    tempData.Document = new BsonDocument();

                    tempData.Document.Add("flowId", tempSetp.String("flowId"));
                    tempData.Document.Add("postId", tempSetp.String("postId"));
                    tempData.Document.Add("flowPosId", tempSetp.String("flowPosId"));
                    tempData.Document.Add("userId", tempUserId);
                    tempData.Document.Add("stepId", tempSetp.String("stepId"));

                    userDataList.Add(tempData);
                }
            }

            foreach (var tempOld in oldRelList)
            {
                if (userIdList.Contains(tempOld.String("userId")) == false) //不在传入的,则删除
                {
                    StorageData tempData = new StorageData();
                    tempData.Name = "BusFlowStepUserRel";
                    tempData.Type = StorageType.Delete;
                    tempData.Query = Query.EQ("relId", tempOld.String("relId"));

                    userDataList.Add(tempData);
                }
            }

            InvokeResult result = dataOp.BatchSaveStorageData(userDataList);

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        /// <summary>
        /// 保存项目流程岗位人员关联
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SaveProjFlowPositionUser()
        {
            int flowPosId = PageReq.GetFormInt("flowPosId");
            int projId = PageReq.GetFormInt("projId");

            string userIds = PageReq.GetForm("userIds");

            List<StorageData> userDataList = new List<StorageData>();

            List<string> userIdList = userIds.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();
            List<BsonDocument> oldRelList = dataOp.FindAllByQueryStr("XH_DesignManage_ProjFlowPositionUser", "projId=" + projId + "&flowPosId=" + flowPosId).ToList();

            foreach (var tempUserId in userIdList)
            {
                BsonDocument tempOld = oldRelList.Where(t => t.String("userId") == tempUserId).FirstOrDefault();    //旧的关联

                if (tempOld == null)    //没有旧的关联则添加
                {
                    StorageData tempData = new StorageData();
                    tempData.Name = "XH_DesignManage_ProjFlowPositionUser";
                    tempData.Type = StorageType.Insert;
                    tempData.Document = new BsonDocument();

                    tempData.Document.Add("projId", projId);
                    tempData.Document.Add("flowPosId", flowPosId);
                    tempData.Document.Add("userId", tempUserId);

                    userDataList.Add(tempData);
                }
            }

            foreach (var tempOld in oldRelList)
            {
                if (userIdList.Contains(tempOld.String("userId")) == false) //不在传入的,则删除
                {
                    StorageData tempData = new StorageData();
                    tempData.Name = "XH_DesignManage_ProjFlowPositionUser";
                    tempData.Type = StorageType.Delete;
                    tempData.Query = Query.EQ("relId", tempOld.String("relId"));

                    userDataList.Add(tempData);
                }
            }

            InvokeResult result = dataOp.BatchSaveStorageData(userDataList);

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }


        /// <summary>
        /// 重置流程模板，删除还未完成的流程实例
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult ResetFlowInstance()
        {
            var flowId = PageReq.GetForm("flowId");
            var referenceDelList = new List<StorageData>();
            var instanceQuery = Query.And(Query.EQ("flowId", flowId), Query.EQ("instanceStatus", "0"));
            referenceDelList.Add(new StorageData() { Type = StorageType.Delete, Query = instanceQuery, Name = "BusFlowInstance" });
            var result = dataOp.BatchSaveStorageData(referenceDelList);
            var json = TypeConvert.InvokeResultToPageJson(result);
            return this.Json(json);
        }

        /// <summary>
        /// 流程模板替换,将除了进行中的流程对象替换为选中的模板
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult ReplaceUncompleteTaskBusFlow()
        {
            var oldFlowId = PageReq.GetForm("oldFlowId");//旧模板对象
            var newflowId = PageReq.GetForm("newflowId");//新模板对象
            var taskList = new List<BsonDocument>();
            var json = new PageJson();
            if (oldFlowId == newflowId)
            {
                json.Success = false;
                json.Message = "不能替换为相同的模板";
                return this.Json(json);
            }
            if (!string.IsNullOrEmpty(oldFlowId) && oldFlowId != "0" && !string.IsNullOrEmpty(newflowId) && newflowId != "0")
            {
                //var taskIds = allTaskBusFlowQuery.Select(c => c.Text("taskId")).ToList();
                //taskList = dataOp.FindAllByKeyValList("XH_DesignManage_Task", "taskId", taskIds).ToList();
                //获取进行中的任务Id
                var onActionTaskIdList = dataOp.FindAll("BusFlowInstance").Where(c => c.Int("instanceStatus") == 0 && c.Int("approvalUserId") != 0 && c.Text("tableName") == "XH_DesignManage_Task").Select(c => c.Int("referFieldValue")).ToList();
                //获取已完成的任务Id
                var completeActionTaskIdList = dataOp.FindAll("BusFlowInstance").Where(c => c.Int("instanceStatus") == 1 && c.Text("tableName") == "XH_DesignManage_Task").Select(c => c.Int("referFieldValue")).ToList();
                //获取那些只有已完成的任务
                completeActionTaskIdList = completeActionTaskIdList.Where(c => !onActionTaskIdList.Contains(c)).ToList();
                //需要更改的模板对象
                var allTaskBusFlowList = dataOp.FindAll("XH_DesignManage_TaskBusFlow").Where(c => c.Text("flowId") == oldFlowId && !onActionTaskIdList.Contains(c.Int("taskId")) && !completeActionTaskIdList.Contains(c.Int("taskId"))).ToList();
                var result = dataOp.QuickUpdate("XH_DesignManage_TaskBusFlow", allTaskBusFlowList, "flowId=" + newflowId);
                var hitallTaskIds = allTaskBusFlowList.Select(c => c.Int("taskId")).ToList();
                if (result.Status == Status.Successful)
                {
                    // 删除未启动但建立了流程实例的对象
                    var waitForDelete = dataOp.FindAll("BusFlowInstance").Where(c => c.Int("instanceStatus") == 0 && c.Int("approvalUserId") == 0 && hitallTaskIds.Contains(c.Int("referFieldValue")) && c.Text("tableName") == "XH_DesignManage_Task").ToList();
                    result = dataOp.QuickDelete("BusFlowInstance", waitForDelete);
                }
                json = TypeConvert.InvokeResultToPageJson(result);
            }
            else
            {
                json.Success = false;
                json.Message = "传入参数有误";
            }

            return this.Json(json);
        }

        /// <summary>
        /// 级联更新模板中的任务对应载入的任务的流程
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult CascadeUpdateCopyTaskFlow()
        {
            var taskId = PageReq.GetForm("taskId");//旧模板对象
            BsonDocument relEntity = dataOp.FindOneByKeyVal("XH_DesignManage_TaskBusFlow", "taskId", taskId.ToString());
            var json = new PageJson();
            if (relEntity == null)
            {
                json.Success = false;
                json.Message = "当前任务还未引用流程模板，请刷新后重试";
                return this.Json(json);
            }
            var newflowId = relEntity.Text("flowId");//新模板对象
            var taskList = dataOp.FindAllByKeyVal("XH_DesignManage_Task", "srcPrimTaskId", taskId).Select(c => c.Int("taskId")).ToList();

            if (!string.IsNullOrEmpty(newflowId) && newflowId != "0")
            {
                //var taskIds = allTaskBusFlowQuery.Select(c => c.Text("taskId")).ToList();
                //taskList = dataOp.FindAllByKeyValList("XH_DesignManage_Task", "taskId", taskIds).ToList();
                //获取进行中的任务Id
                var hitAllBusFlowInstance = dataOp.FindAll("BusFlowInstance").Where(c => taskList.Contains(c.Int("referFieldValue"))).ToList();
                var onActionTaskIdList = hitAllBusFlowInstance.Where(c => c.Int("instanceStatus") == 0 && c.Int("approvalUserId") != 0 && c.Text("tableName") == "XH_DesignManage_Task").Select(c => c.Int("referFieldValue")).ToList();
                //获取已完成的任务Id
                var completeActionTaskIdList = hitAllBusFlowInstance.Where(c => c.Int("instanceStatus") == 1 && c.Text("tableName") == "XH_DesignManage_Task").Select(c => c.Int("referFieldValue")).ToList();
                //获取那些只有已完成的任务
                completeActionTaskIdList = completeActionTaskIdList.Where(c => !onActionTaskIdList.Contains(c)).ToList();
                //需要更改的模板对象
                var allTaskBusFlowList = dataOp.FindAll("XH_DesignManage_TaskBusFlow").Where(c => c.Text("flowId") != newflowId && taskList.Contains(c.Int("taskId")) && !onActionTaskIdList.Contains(c.Int("taskId")) && !completeActionTaskIdList.Contains(c.Int("taskId"))).ToList();
                var result = dataOp.QuickUpdate("XH_DesignManage_TaskBusFlow", allTaskBusFlowList, "flowId=" + newflowId);
                var hitallTaskIds = allTaskBusFlowList.Select(c => c.Int("taskId")).ToList();
                if (result.Status == Status.Successful)
                {
                    // 删除未启动但建立了流程实例的对象
                    var waitForDelete = dataOp.FindAll("BusFlowInstance").Where(c => c.Int("instanceStatus") == 0 && c.Int("approvalUserId") == 0 && hitallTaskIds.Contains(c.Int("referFieldValue")) && c.Text("tableName") == "XH_DesignManage_Task").ToList();
                    result = dataOp.QuickDelete("BusFlowInstance", waitForDelete);
                }
                json = TypeConvert.InvokeResultToPageJson(result);
            }
            else
            {
                json.Success = false;
                json.Message = "传入参数有误";
            }

            return this.Json(json);
        }


        /// <summary>
        /// 重置还未完成的流程关联
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult ResetTaskFlow()
        {
            var taskId = PageReq.GetForm("taskId");
            var referenceDelList = new List<StorageData>();
            var instanceQuery = Query.And(Query.EQ("tableName", "XH_DesignManage_Task"), Query.EQ("referFieldName", "taskId"), Query.EQ("referFieldValue", taskId), Query.EQ("instanceStatus", "0"));
            referenceDelList.Add(new StorageData() { Type = StorageType.Delete, Query = instanceQuery, Name = "BusFlowInstance" });
            var taskFlowQuery = Query.EQ("taskId", taskId);
            referenceDelList.Add(new StorageData() { Type = StorageType.Delete, Query = taskFlowQuery, Name = "XH_DesignManage_TaskBusFlow" });
            var result = dataOp.BatchSaveStorageData(referenceDelList);
            var json = TypeConvert.InvokeResultToPageJson(result);
            return this.Json(json);
        }
        /// <summary>
        /// 删除任务还未完成的流程实例
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult ResetTaskInstance()
        {
            var taskId = PageReq.GetForm("taskId");
            var referenceDelList = new List<StorageData>();
            var instanceQuery = Query.And(Query.EQ("tableName", "XH_DesignManage_Task"), Query.EQ("referFieldName", "taskId"), Query.EQ("referFieldValue", taskId), Query.EQ("instanceStatus", "0"));
            referenceDelList.Add(new StorageData() { Type = StorageType.Delete, Query = instanceQuery, Name = "BusFlowInstance" });
            var result = dataOp.BatchSaveStorageData(referenceDelList);
            var json = TypeConvert.InvokeResultToPageJson(result);
            return this.Json(json);
        }

        #region 步骤事务
        /// <summary>
        /// 获取所有事务
        /// </summary>
        /// <returns></returns>
        public ActionResult GetAllTran()
        {

            List<BsonDocument> tranList = dataOp.FindAll("TransactionStore").ToList();

            List<Item> resultList = new List<Item>();

            foreach (var tran in tranList)
            {
                Item temp = new Item();

                temp.id = tran.Int("transactionId");
                temp.name = tran.Text("name");

                resultList.Add(temp);
            }

            return Json(resultList, JsonRequestBehavior.AllowGet);
        }


        /// <summary>
        /// 保存事务步骤编辑
        /// </summary>
        /// <param name="bookTask"></param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult SaveStepTranEdit()
        {
            PageJson json = new PageJson();
            var stepId = PageReq.GetFormInt("stepId");
            var inTranIdArray = PageReq.GetFormIntList("inTranIdArray");
            var waitTranIdArray = PageReq.GetFormIntList("waitTranIdArray");
            var outTranIdArray = PageReq.GetFormIntList("outTranIdArray");
            BusFlowStepBll bllStep = BusFlowStepBll._();
            var result = bllStep.SaveStepTran(stepId, inTranIdArray.ToList(), waitTranIdArray.ToList(), outTranIdArray.ToList(), this.CurrentUserId);
            json.Success = result.Status == Status.Successful;
            json.Message = result.Message;
            return this.Json(json);
        }


        #endregion

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SaveReferFieldName(FormCollection saveForm)
        {
            var stepId = PageReq.GetForm("stepId");
            var tbName = PageReq.GetForm("tbName");
            var enslavedStepId = PageReq.GetForm("enslavedStepId");
            var resetCSignStepId = PageReq.GetForm("resetCSignStepId");
            var isFixUser = PageReq.GetForm("isFixUser");
            var canImComplete = PageReq.GetForm("canImComplete");
            var ImCompleteName = PageReq.GetForm("ImCompleteName");
            var isChecker = PageReq.GetForm("isChecker");
            var refuseStepId = PageReq.GetForm("refuseStepId");
            var noTurnRight = PageReq.GetForm("noTurnRight");
            var sameUserStepId = PageReq.GetForm("sameUserStepId");
            var noRefuseBtn = PageReq.GetForm("noRefuseBtn");
            var noReplyBtn = PageReq.GetForm("noReplyBtn");
            var result = new InvokeResult();
            var saveBsonDocumentList = new List<StorageData>();
            var curFieldList = dataOp.FindAllByKeyVal("BusFlowStepReferField", "stepId", stepId).ToList();
            StorageData planData = new StorageData();
            BsonDocument updateBson = new BsonDocument();
            updateBson.Add("enslavedStepId", enslavedStepId);
            updateBson.Add("resetCSignStepId", resetCSignStepId);
            updateBson.Add("isFixUser", isFixUser);
            updateBson.Add("ImCompleteName", ImCompleteName);
            updateBson.Add("isChecker", isChecker);
            updateBson.Add("refuseStepId", refuseStepId);
            updateBson.Add("canImComplete", canImComplete);
            updateBson.Add("noTurnRight", noTurnRight);
            updateBson.Add("sameUserStepId", sameUserStepId);
            updateBson.Add("noRefuseBtn", noRefuseBtn);
            updateBson.Add("noReplyBtn", noReplyBtn);
            planData.Name = "BusFlowStep";
            planData.Query = Query.EQ("stepId", stepId);
            planData.Type = StorageType.Update;
            planData.Document = updateBson;
            saveBsonDocumentList.Add(planData);
            foreach (var tempKey in saveForm.AllKeys)
            {

                if (tempKey == "tbName" || tempKey == "stepId" || tempKey == "queryStr"
                    || tempKey.Contains("fileList[")
                    || tempKey.Contains("param.") 
                    || tempKey == "turnRightName" 
                    || tempKey == "completeStepName"
                    || tempKey == "isHideLog" || tempKey == "isFixUser"
                    || tempKey == "canImComplete" || tempKey == "ImCompleteName"
                    || tempKey == "isChecker" || tempKey == "refuseStepId" || tempKey=="sameUserStepId"
                    || tempKey == "noRefuseBtn" || tempKey == "noReplyBtn" 
                    || tempKey.Contains("actId_")) continue;

                var curFildObj = curFieldList.Where(c => c.Text("referFieldName") == tempKey).FirstOrDefault();
                StorageData tempData = new StorageData();
                tempData.Name = "BusFlowStepReferField";
                if (curFildObj == null)
                {
                    BsonDocument dataBson = new BsonDocument();
                    dataBson.Add("stepId", stepId);
                    dataBson.Add("tableName", tbName);
                    dataBson.Add("referFieldName", tempKey);
                    dataBson.Add("canEdit", PageReq.GetForm(tempKey));
                    tempData.Type = StorageType.Insert;
                    tempData.Document = dataBson;
                    saveBsonDocumentList.Add(tempData);
                }
                else
                {

                    BsonDocument dataBson = new BsonDocument();
                    dataBson.Add("canEdit", PageReq.GetForm(tempKey));
                    tempData.Query = Query.EQ("referFieldId", curFildObj.Text("referFieldId"));
                    tempData.Type = StorageType.Update;
                    tempData.Document = dataBson;
                    saveBsonDocumentList.Add(tempData);
                }
            }

            #region  转办名称修改
            var turnRightName = PageReq.GetForm("turnRightName");
            var completeStepName = PageReq.GetForm("completeStepName");
            var isHideLog = PageReq.GetForm("isHideLog");
            if (!string.IsNullOrEmpty(turnRightName) || !string.IsNullOrEmpty(completeStepName))
            {
                StorageData tempData = new StorageData();
                tempData.Name = "BusFlowStep";
                BsonDocument dataBson = new BsonDocument();
                dataBson.Add("turnRightName", turnRightName);
                dataBson.Add("completeStepName", completeStepName);
                dataBson.Add("isHideLog", isHideLog);
                
                tempData.Query = Query.EQ("stepId", stepId);
                tempData.Type = StorageType.Update;
                tempData.Document = dataBson;
                saveBsonDocumentList.Add(tempData);
            }
            #endregion
            #region 修改动作名
            var entity = dataOp.FindOneByKeyVal("BusFlowStep", "stepId", stepId);
            if (entity != null)
            {
                var curActionNameList = dataOp.FindAllByKeyVal("BusFlowStepActionName", "stepId", stepId).ToList(); //获取动作别名列表
                var curActionList = dataOp.FindAllByKeyVal("BusFlowAction", "actTypeId", entity.Text("actTypeId")).ToList(); //获取动作别名列表
                foreach (var act in curActionList)
                {
                    var actDisplayName = PageReq.GetForm("actId_" + act.Text("actId"));
                    if (string.IsNullOrEmpty(actDisplayName))
                    {
                        actDisplayName = act.Text("name");
                    }
                    var stepActNameObj = curActionNameList.Where(c => c.Int("actId") == act.Int("actId")).FirstOrDefault();
                    if (stepActNameObj != null)
                    {
                        StorageData tempData = new StorageData();
                        tempData.Name = "BusFlowStepActionName";
                        BsonDocument dataBson = new BsonDocument();

                        dataBson.Add("name", actDisplayName);
                        tempData.Query = Query.And(Query.EQ("flowId", entity.Text("flowId")), Query.EQ("stepId", stepId), Query.EQ("actId", act.Text("actId")));
                        tempData.Type = StorageType.Update;
                        tempData.Document = dataBson;
                        saveBsonDocumentList.Add(tempData);
                    }
                    else
                    {
                        StorageData tempData = new StorageData();
                        tempData.Name = "BusFlowStepActionName";
                        BsonDocument dataBson = new BsonDocument();
                        dataBson.Add("name", actDisplayName);
                        dataBson.Add("flowId", entity.Text("flowId"));
                        dataBson.Add("stepId", entity.Text("stepId"));
                        dataBson.Add("actId", act.Text("actId"));
                        tempData.Type = StorageType.Insert;
                        tempData.Document = dataBson;
                        saveBsonDocumentList.Add(tempData);
                    }
                }
            }
            #endregion
            result = dataOp.BatchSaveStorageData(saveBsonDocumentList);
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        /// <summary>
        /// 下步骤实例流程人员控制,将没选中的置为无效
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SetEnSalvedStep()
        {
            var flowInstanceId = PageReq.GetForm("flowInstanceId");
            var activeStepIdList = PageReq.GetFormIntList("stepIds");
            var curStepId = PageReq.GetForm("curStepId");
            var flowId = PageReq.GetForm("flowId");
            //获取可控制的会签步骤
            var allStepList = dataOp.FindAllByKeyVal("BusFlowStep", "flowId", flowId).ToList();
            var hitEnslavedStepOrder = dataOp.FindAllByKeyVal("BusFlowStep", "enslavedStepId", curStepId).OrderBy(c => c.Int("stepOrder")).Select(c => c.Int("stepOrder")).Distinct().ToList();
            var hitStepIds = allStepList.Where(c => hitEnslavedStepOrder.Contains(c.Int("stepOrder"))).Select(c => c.Int("stepId")).ToList();
            var allInstanceActionUser = dataOp.FindAllByKeyVal("InstanceActionUser", "flowInstanceId", flowInstanceId).Where(c => hitStepIds.Contains(c.Int("stepId"))).ToList();
            var saveBsonDocumentList = new List<StorageData>();
            ///需要设置为不执行的步骤列表
            var unActiveStepList = allInstanceActionUser.Where(c => !activeStepIdList.Contains(c.Int("stepId")));
            var result = new InvokeResult();
            foreach (var actionUser in allInstanceActionUser)
            {
                StorageData tempData = new StorageData();
                tempData.Name = "InstanceActionUser";
                BsonDocument dataBson = new BsonDocument();
                tempData.Query = Query.EQ("inActId", actionUser.Text("inActId"));
                tempData.Type = StorageType.Update;
                if (activeStepIdList.Contains(actionUser.Int("stepId")))
                {
                    if (actionUser.Int("status") == 1)
                    {
                        dataBson.Add("status", "0");
                        tempData.Document = dataBson;
                        saveBsonDocumentList.Add(tempData);
                    }
                    else
                    { continue; }
                }
                else
                {
                    if (actionUser.Int("status") == 0)
                    {
                        dataBson.Add("status", "1");
                        tempData.Document = dataBson;
                        saveBsonDocumentList.Add(tempData);
                    }
                    else
                    { continue; }

                }


            }
            if (saveBsonDocumentList.Count > 0)
            {
                result = dataOp.BatchSaveStorageData(saveBsonDocumentList);
            }
            var json = TypeConvert.InvokeResultToPageJson(result);
            return this.Json(json);
        }


        /// <summary>
        /// 启动二次会签 2014.2.12添加2次会签功能
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SetResetCSignStepStep()
        {
            var flowInstanceId = PageReq.GetForm("flowInstanceId");
            var activeStepIdList = PageReq.GetFormIntList("stepIds");
            var curStepId = PageReq.GetFormInt("curStepId");
            var flowId = PageReq.GetFormInt("flowId");
            var remark = PageReq.GetForm("remark");
            var curFlowInstance = dataOp.FindOneByKeyVal("BusFlowInstance", "flowInstanceId", flowInstanceId);
            var result = new InvokeResult() { Status=Status.Successful};
            if (curFlowInstance != null)
            {

                BusFlowTraceBll traceBll = BusFlowTraceBll._(dataOp);
                //获取可控制的会签步骤
                var allStepList = dataOp.FindAllByKeyVal("InstanceActionUser", "flowInstanceId", flowInstanceId.ToString()).ToList();
                var hitEnslavedStepOrder = dataOp.FindAllByKeyVal("InstanceActionUser", "resetCSignStepId", curStepId.ToString()).Where(c=>c.Int("status")==0).OrderBy(c => c.Int("stepOrder")).Select(c => c.Int("stepOrder")).Distinct().ToList();
                var hitStepQuery = allStepList.Where(c => hitEnslavedStepOrder.Contains(c.Int("stepOrder"))).ToList();
                var hitStepIds = activeStepIdList.ToList();
                var filterAssignerStepIds = hitStepQuery.Select(c => c.Int("stepId")).ToList();
                var startStepId = hitStepQuery.OrderBy(c => c.Int("stepOrder")).Select(c => c.Int("stepId")).FirstOrDefault();//获取勾选的步骤列表中最开始的步骤Id
                //更新流程实例步骤回二次会签步骤
                if (hitStepIds.Count() > 0)
                {
                    //更新步骤
                   
                    result=dataOp.Update(curFlowInstance, new BsonDocument().Add("stepId", startStepId));
                    //添加操作日志
                    var flowTrace = new BsonDocument();
                    flowTrace.Add("preStepId", curStepId.ToString());
                    //此处不能添加重复的step字段
                    flowTrace.Add("nextStepId", startStepId.ToString());
                    flowTrace.Add("flowInstanceId", curFlowInstance.Text("flowInstanceId"));
                    flowTrace.Add("traceType", "2");
                    flowTrace.Add("remark", remark );
                    traceBll.Insert(flowTrace);
                    if (result.Status == Status.Successful)
                    {
                        //重置会签状态状态
                        result = traceBll.ChangeActionUnAvaiable(flowId, curFlowInstance.Int("flowInstanceId"), startStepId, curStepId, hitStepIds, filterAssignerStepIds,remark);

                        #region QX推送oa待办已办消息
                        UpdateOAToDoList pending = new UpdateOAToDoList();
                        pending.QXInsertFlowDone(curFlowInstance, curStepId, dataOp.GetCurrentUserId());
                        pending.QXInsertAllFlowToDo(curFlowInstance.Int("flowInstanceId"));
                        #endregion
                    }
                }
            }
            else
            {
                result.Status = Status.Failed;
                result.Message = "该流程实例不存在，请刷新后重置";
              
            }
            var json = TypeConvert.InvokeResultToPageJson(result);
            return this.Json(json);
        }

        #endregion

        #region 删除流程步骤人员
        /// <summary>
        /// 删除流程步骤人员
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult DelFlowStepUserRelation()
        {
            int stepId = PageReq.GetFormInt("stepId");
            string userId = PageReq.GetForm("userId");
            string flowId = PageReq.GetForm("flowId");
            InvokeResult result = new InvokeResult();
            BsonDocument oldRel = dataOp.FindOneByQuery("BusFlowStepUserRel", Query.And(Query.EQ("stepId", stepId.ToString()), Query.EQ("flowId", flowId), Query.EQ("userId", userId)));
            if (oldRel != null)
            {
                result = dataOp.Delete("BusFlowStepUserRel", Query.EQ("relId", oldRel.String("relId")));
            }
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        #endregion

        #region 流程发起跳转
        /// <summary>
        /// 发起流程
        /// </summary>
        /// <returns></returns>                                         
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult StartWorkFlow()
        {
            var instanceName = PageReq.GetForm("instanceName");
            var referFieldValue = PageReq.GetForm("referFieldValue");
            var tableName = PageReq.GetForm("tableName");
            var referFieldName = PageReq.GetForm("referFieldName");
            var flowId = PageReq.GetForm("flowId");
            var stepId = PageReq.GetFormInt("stepId");
            var actId = PageReq.GetFormInt("actId");
            var approvalItem = PageReq.GetForm("approvalItem");
            var completeDate = PageReq.GetForm("completeDate");
            var approvalSubject = PageReq.GetForm("approvalSubject");
            var actionUserStr = PageReq.GetForm("actionUserStr");
            var flowInstanceId = PageReq.GetForm("flowInstanceId");
            var doTime = PageReq.GetForm("doTime");
            var attrVals = PageReq.GetForm("attrVals");
            var userIds = PageReq.GetForm("userIds");
            var patternIsMilti = PageReq.GetFormInt("profTypeIsMulti");//扩展属性物业类型是否多选 1：多选  0或其他单选
            var stepList = dataOp.FindAllByKeyVal("BusFlowStep", "flowId", flowId).OrderBy(c => c.Int("stepOrder")).ToList();
            var bootStep = stepList.Where(c => c.Int("actTypeId") == (int)FlowActionType.Launch).FirstOrDefault();
            var curFlowInstance = dataOp.FindOneByKeyVal("BusFlowInstance", "flowInstanceId", flowInstanceId);
            var activeStepIdList = PageReq.GetFormIntList("stepIds");
            var hitEnslavedStepOrder = dataOp.FindAllByKeyVal("BusFlowStep", "enslavedStepId", bootStep.Text("stepId")).OrderBy(c => c.Int("stepOrder")).Select(c => c.Int("stepOrder")).Distinct().ToList();
            if (activeStepIdList.Count() <= 0 && hitEnslavedStepOrder.Count() > 0)
            {
                PageJson json = new PageJson();
                json.Success = false;
                json.Message = "请先选定会签部门";
                return Json(json);
            
            }
            var instanceHelper = new FlowInstanceHelper();
            if (curFlowInstance == null)
            {
                #region 不存在则新建
                curFlowInstance = new BsonDocument();
                curFlowInstance.Add("instanceName", instanceName);
                curFlowInstance.Add("referFieldValue", referFieldValue);
                curFlowInstance.Add("tableName", tableName);
                curFlowInstance.Add("referFieldName", referFieldName);
                curFlowInstance.Add("flowId", flowId);
                curFlowInstance.Add("stepId", stepId);
                curFlowInstance.Add("approvalItem", approvalItem);
                curFlowInstance.Add("completeDate", completeDate);
                curFlowInstance.Add("approvalSubject", approvalSubject);
                curFlowInstance.Add("approvalUserId", dataOp.GetCurrentUserId());
                curFlowInstance.Add("doTime", doTime);
                AddExtensionAttr(curFlowInstance, attrVals);

                var result = instanceHelper.SaveInstance(curFlowInstance, actionUserStr);
                if (result.Status == Status.Successful)
                {
                    curFlowInstance = result.BsonInfo;

                    #region 增加物业类型多选插入
                    if (patternIsMilti == 1) //增加物业类型多选插入
                    {
                        var profStr=PageReq.GetForm("profType");
                        if(!string.IsNullOrEmpty(profStr))
                        {
                            string[] profArr = profStr.Split(',');
                            if (profArr.Length > 0)
                            {

                                List<StorageData> insertProfList = new List<StorageData>();
                                foreach (var tempStr in profArr)
                                {
                                    if (!string.IsNullOrEmpty(tempStr))
                                    {
                                        StorageData tempProf = new StorageData();
                                        tempProf.Name = "FlowInstancePattern";
                                        tempProf.Type = StorageType.Insert;
                                        tempProf.Document = new BsonDocument().Add("flowInstanceId", curFlowInstance.String("flowInstanceId")).Add("patternId", tempStr);
                                        insertProfList.Add(tempProf);
                                    }
                                }
                                if (insertProfList.Count() > 0) 
                                {
                                    dataOp.BatchSaveStorageData(insertProfList);
                                }
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    AddFlowInstanceUsers(curFlowInstance.String("flowInstanceId"), userIds);
                    Json(TypeConvert.InvokeResultToPageJson(result));
                }
                #endregion
            }
            else//更新流程实例
            {
                if (bootStep != null)
                {
                    stepId = bootStep.Int("stepId");
                    var updateBson = new BsonDocument();
                    updateBson.Add("approvalItem", approvalItem);
                    updateBson.Add("completeDate", completeDate);
                    updateBson.Add("approvalSubject", approvalSubject);
                    updateBson.Add("approvalUserId", dataOp.GetCurrentUserId().ToString());
                    updateBson.Add("instanceStatus", "0");
                    updateBson.Add("doTime", doTime);
                    updateBson.Add("stepId", stepId.ToString());
                    AddExtensionAttr(updateBson, attrVals);
                    var result = instanceHelper.UpdateInstance(curFlowInstance, updateBson, actionUserStr);
                    if (result.Status != Status.Successful)
                    {
                        Json(TypeConvert.InvokeResultToPageJson(result));

                    }
                    else
                    {
                        AddFlowInstanceUsers(curFlowInstance.String("flowInstanceId"), userIds);
                        #region 增加物业类型多选数据更新
                        if (patternIsMilti == 1) //增加物业类型多选数据更新
                        {
                            List<BsonDocument> oldProfTypeList = dataOp.FindAllByQuery("FlowInstancePattern", Query.EQ("flowInstanceId", result.BsonInfo.String("flowInstanceId"))).ToList();
                            var profStr = PageReq.GetForm("profType");
                            if (!string.IsNullOrEmpty(profStr))
                            {
                                string[] profArr = profStr.Split(',');
                                if (profArr.Length > 0)
                                {

                                    List<StorageData> insertProfList = new List<StorageData>();
                                    foreach (var tempStr in profArr)
                                    {
                                        if (!string.IsNullOrEmpty(tempStr))
                                        {
                                            BsonDocument isHasExit = oldProfTypeList.Where(x => x.String("patternId") == tempStr).FirstOrDefault();
                                            if (isHasExit == null)
                                            {
                                                StorageData tempProf = new StorageData();
                                                tempProf.Name = "FlowInstancePattern";
                                                tempProf.Type = StorageType.Insert;
                                                tempProf.Document = new BsonDocument().Add("flowInstanceId", curFlowInstance.String("flowInstanceId")).Add("patternId", tempStr);
                                                insertProfList.Add(tempProf);
                                            }
                                            else
                                            {
                                                oldProfTypeList.Remove(isHasExit);//清除存在的数据
                                            }
                                        }
                                    }
                                    if (oldProfTypeList.Count() > 0) //删除剩下不插入的物业类型
                                    {
                                        foreach (var tempOld in oldProfTypeList)
                                        {
                                            StorageData tempProf = new StorageData();
                                            tempProf.Name = "FlowInstancePattern";
                                            tempProf.Type = StorageType.Delete;
                                            tempProf.Document = tempOld;
                                            tempProf.Query = Query.EQ("relId", tempOld.String("relId"));
                                            insertProfList.Add(tempProf);
                                        }
                                    }
                                    if (insertProfList.Count() > 0)
                                    {
                                        dataOp.BatchSaveStorageData(insertProfList);
                                    }
                                }
                            }
                        }
                        #endregion
                    }
                }
                else
                {
                    PageJson json = new PageJson();
                    json.Success = false;
                    json.Message = "流程没有起始步骤不能启动";
                    return Json(json);
                }
            }

            #region 会签岗位选择
            var curStepId = stepId.ToString();
            if (activeStepIdList.Count() > 0&&hitEnslavedStepOrder.Count()>0)//发起步骤需要确定会签部门
            {
                //获取可控制的会签步骤
                var allStepList = dataOp.FindAllByKeyVal("BusFlowStep", "flowId", flowId).ToList();
                var hitStepIds = allStepList.Where(c => hitEnslavedStepOrder.Contains(c.Int("stepOrder"))).Select(c => c.Int("stepId")).ToList();
                var allInstanceActionUser = dataOp.FindAllByKeyVal("InstanceActionUser", "flowInstanceId", flowInstanceId).Where(c => hitStepIds.Contains(c.Int("stepId"))).ToList();
                var saveBsonDocumentList = new List<StorageData>();
                ///需要设置为不执行的步骤列表
                var unActiveStepList = allInstanceActionUser.Where(c => !activeStepIdList.Contains(c.Int("stepId")));
                foreach (var actionUser in allInstanceActionUser)
                {
                    StorageData tempData = new StorageData();
                    tempData.Name = "InstanceActionUser";
                    BsonDocument dataBson = new BsonDocument();
                    tempData.Query = Query.EQ("inActId", actionUser.Text("inActId"));
                    tempData.Type = StorageType.Update;
                    if (activeStepIdList.Contains(actionUser.Int("stepId")))
                    {
                        if (actionUser.Int("status") == 1)
                        {
                            dataBson.Add("status", "0");
                            tempData.Document = dataBson;
                            saveBsonDocumentList.Add(tempData);
                        }
                        else
                        { continue; }
                    }
                    else
                    {
                        if (actionUser.Int("status") == 0)
                        {
                            dataBson.Add("status", "1");
                            tempData.Document = dataBson;
                            saveBsonDocumentList.Add(tempData);
                        }
                        else
                        { continue; }

                    }


                }
                if (saveBsonDocumentList.Count > 0)
                {
                    dataOp.BatchSaveStorageData(saveBsonDocumentList);
                }
            }
            #endregion

            #region  执行发起动作，跳转步骤
            var actionResult = instanceHelper.ExecAction(curFlowInstance, actId, null, stepId);
            #endregion
            return Json(TypeConvert.InvokeResultToPageJson(actionResult));
        }

        private void AddFlowInstanceUsers(string instanceId, string userIds)
        {
            string[] temp = userIds.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            dataOp.Delete("FlowInstanceUserRel", Query.EQ("instanceId", instanceId));
            foreach (var userId in temp)
            {
                dataOp.Insert("FlowInstanceUserRel", new BsonDocument { { "instanceId", instanceId }, { "userId", userId } });
            }
            //"FlowInstanceUserRel"
        }

        /// <summary>
        /// 为流程实例新增扩展属性值
        /// </summary>
        /// <param name="flow"></param>
        /// <param name="attrVals"></param>
        private void AddExtensionAttr(BsonDocument flow, string attrVals)
        {
            var projScale = PageReq.GetForm("projScale");
            var projName = PageReq.GetForm("projName");
            var profType = PageReq.GetForm("profType");
            var approveAmount = PageReq.GetForm("approvedAmount");
            var patternIsMili = PageReq.GetFormInt("profTypeIsMulti");//扩展属性物业类型是否多选 1：多选  0或其他单选

            flow.Add("projScale", projScale);
            flow.Add("projName", projName);
            if (patternIsMili != 1)//修改物业类型多选
            {
                flow.Add("profType", profType);
            }
            flow.Add("approvedAmount", approveAmount);
            if (SysAppConfig.CustomerCode == CustomerCode.ZHHY)
            {
                flow.Add("contractCat", PageReq.GetForm("contractCat"));//合同类别（下拉字符串）
            }
            string[] temp = attrVals.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var s in temp)
            {
                string[] val = s.Split(',');
                if (val.Count() > 1)
                {
                    flow.Add(val[0], val[1]);
                }
            }
        }

        /// <summary>
        /// 执行动作
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult DoAction()
        {
            var flowId = PageReq.GetForm("flowId");
            var stepId = PageReq.GetFormInt("stepId");
            var actId = PageReq.GetFormInt("actId");
            var content =Server.UrlDecode(PageReq.GetForm("content"));
            var flowInstanceId = PageReq.GetForm("flowInstanceId");
            var jumpStepId = PageReq.GetFormInt("jumpStepId");

            var curFlowInstance = dataOp.FindOneByKeyVal("BusFlowInstance", "flowInstanceId", flowInstanceId);
            var instanceHelper = new FlowInstanceHelper();
            if (jumpStepId == 0)
            {
                #region  执行发起动作，跳转步骤
                var actionResult = instanceHelper.SaveFormAndExecAction(curFlowInstance, actId, content, stepId, null);
                #endregion
                return Json(TypeConvert.InvokeResultToPageJson(actionResult));
            }
            else
            {
                #region  执行发起动作，跳转步骤
                var actionResult = instanceHelper.SaveFormAndExecAction(curFlowInstance, actId, content, stepId, jumpStepId);
                #endregion
                return Json(TypeConvert.InvokeResultToPageJson(actionResult));
            }
        }

        /// <summary>
        /// 强制结束会签
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult ForceCompleteStep()
        {
            var flowId = PageReq.GetForm("flowId");
            var stepId = PageReq.GetFormInt("stepId");
            var actId = PageReq.GetFormInt("actId");
            var content = PageReq.GetForm("content");
            var flowInstanceId = PageReq.GetForm("flowInstanceId");
            var curFlowInstance = dataOp.FindOneByKeyVal("BusFlowInstance", "flowInstanceId", flowInstanceId);
            var jumpStepId = 0;

            var stepList = dataOp.FindAllByKeyVal("BusFlowStep", "flowId", flowId).ToList();
            var curStep = stepList.Where(c => c.Int("stepId") == stepId).FirstOrDefault();
            var json = new PageJson();
            if (curStep == null || curFlowInstance == null)
            {

                json.Success = false;
                json.Message = "传入参数有误请刷新后重试";
                return Json(json);
            }
            if (curStep.Int("actTypeId") == (int)FlowActionType.Countersign)
            {
                var nextStep = stepList.Where(c => c.Int("stepOrder") > curStep.Int("stepOrder")).OrderBy(c => c.Int("stepOrder")).FirstOrDefault();
                if (nextStep != null)
                {
                    jumpStepId = nextStep.Int("stepId");
                }
            }
            var instanceHelper = new FlowInstanceHelper();
            if (jumpStepId == 0)
            {

                json.Success = false;
                json.Message = "无法结束会签请联系管理员";
                return Json(json);
            }
            else
            {
                #region  执行发起动作，跳转步骤
                var actionResult = instanceHelper.SaveFormAndExecAction(curFlowInstance, actId, content, stepId, jumpStepId);
                #endregion
                return Json(TypeConvert.InvokeResultToPageJson(actionResult));
            }
        }

        /// <summary>
        /// 强制结束流程
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult ForceCompleteInstance()
        {
            var flowInstanceId = PageReq.GetFormInt("flowInstanceId");
            var flowId = PageReq.GetFormInt("flowId");
            var content = PageReq.GetForm("content");
            var curStepId = PageReq.GetFormInt("stepId");
            var instanceQuery = Query.And(Query.EQ("flowInstanceId", flowInstanceId.ToString()), Query.EQ("instanceStatus", "0"));
            var referenceUpdateList = new List<StorageData>();
            referenceUpdateList.Add(new StorageData() { Type = StorageType.Update, Query = instanceQuery, Name = "BusFlowInstance", Document = new BsonDocument().Add("instanceStatus","1") });
            var result = dataOp.BatchSaveStorageData(referenceUpdateList);
            if (result.Status == Status.Successful)
            {
                var flowTrace = new BsonDocument();
                BusFlowTraceBll traceBll = BusFlowTraceBll._(dataOp);
                flowTrace.Add("preStepId", curStepId.ToString());
                //此处不能添加重复的step字段
                flowTrace.Add("flowInstanceId", flowInstanceId.ToString());
                flowTrace.Add("traceType", "2");//直接完成
                flowTrace.Add("remark", string.IsNullOrEmpty(content) ? "结束流程" : content);
                traceBll.Insert(flowTrace);
                //创建自动跳转
                traceBll.CreateCompleteActionLog(curStepId, flowInstanceId);
                //var reSetTraceResult = traceBll.ResetActionUnAvaiable(flowId, flowInstanceId);

                #region QX结束该流程所有待办
                if (SysAppConfig.CustomerCode == CustomerCode.QX)
                {
                    UpdateOAToDoList pending = new UpdateOAToDoList();
                    pending.QXInsertAllFlowDone(flowInstanceId, dataOp.GetCurrentUserId());
                }
                #endregion
            }
            var json = TypeConvert.InvokeResultToPageJson(result);
            return this.Json(json);
        }

        #region 取消审批
        /// <summary>
        /// 取消流程审批
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult CancelInstance()
        {
            var flowInstanceId = PageReq.GetFormInt("flowInstanceId");
            var flowId = PageReq.GetFormInt("flowId");
            var content = PageReq.GetForm("content");
            var stepId = PageReq.GetFormInt("stepId");
            var instanceQuery = Query.And(Query.EQ("flowInstanceId", flowInstanceId.ToString()), Query.EQ("instanceStatus", "0"));
            var referenceUpdateList = new List<StorageData>();
            referenceUpdateList.Add(new StorageData() { Type = StorageType.Update, Query = instanceQuery, Name = "BusFlowInstance", Document = new BsonDocument().Add("instanceStatus", "1").Add("approvalUserId", "") });
            var result = dataOp.BatchSaveStorageData(referenceUpdateList);
            if (result.Status == Status.Successful)
            {
                var instance = dataOp.FindOneByQuery("BusFlowInstance", Query.EQ("flowInstanceId", flowInstanceId.ToString()));
                var curStepId = instance.Int("stepId");
                var flowTrace = new BsonDocument();
                BusFlowTraceBll traceBll = BusFlowTraceBll._(dataOp);
                flowTrace.Add("preStepId", stepId.ToString());
                //此处不能添加重复的step字段
                flowTrace.Add("flowInstanceId", flowInstanceId.ToString());
                flowTrace.Add("traceType", "2");//直接完成
                flowTrace.Add("remark", string.IsNullOrEmpty(content) ? "取消审批" : content);
                traceBll.Insert(flowTrace);
                //创建自动跳转
                traceBll.CreateCompleteActionLog(curStepId, flowInstanceId);
                //var reSetTraceResult = traceBll.ResetActionUnAvaiable(flowId, flowInstanceId);

                #region QX结束该流程所有待办
                if (SysAppConfig.CustomerCode == CustomerCode.QX)
                {
                    UpdateOAToDoList pending = new UpdateOAToDoList();
                    pending.QXInsertAllFlowDone(flowInstanceId, dataOp.GetCurrentUserId());
                }
                #endregion
            }
            var json = TypeConvert.InvokeResultToPageJson(result);
            return this.Json(json);
        }
        #endregion

        /// <summary>
        /// 过期流程信息提醒
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult ForceNoticeUser()
        {
            var flowId = PageReq.GetForm("flowId");
            var stepId = PageReq.GetFormInt("stepId");
            var actId = PageReq.GetFormInt("actId");
            var selStepIds = PageReq.GetFormIntList("selStepIds");//需要发送消息的步骤
            var content = PageReq.GetForm("content");
            var flowInstanceId = PageReq.GetForm("flowInstanceId");
            var curFlowInstance = dataOp.FindOneByKeyVal("BusFlowInstance", "flowInstanceId", flowInstanceId);
            var stepList = dataOp.FindAllByKeyVal("BusFlowStep", "flowId", flowId).ToList();
            var curStep = stepList.Where(c => c.Int("stepId") == stepId).FirstOrDefault();
            var json = new PageJson();
            if (curStep == null || curFlowInstance == null)
            {

                json.Success = false;
                json.Message = "传入参数有误请刷新后重试";
                return Json(json);
            }
            var noticeStepIds = new List<int>();
            if (curStep.Int("actTypeId") == (int)FlowActionType.Countersign)
            {
                if (selStepIds.Count() <= 0)
                {
                    var hitStepIds = stepList.Where(c => c.Int("stepOrder") == curStep.Int("stepOrder")).Select(c => c.Int("stepId")).ToList();
                    var query1 = MongoDB.Driver.Builders.Query.In("traceType", "2");//用与过滤已经执行过的 
                    var query2 = MongoDB.Driver.Builders.Query.EQ("actionAvaiable", "1");//用户过滤已经执行过的
                    var query3 = MongoDB.Driver.Builders.Query.EQ("flowInstanceId", flowInstanceId);//用户过滤已经执行过的
                    var hitExecStepIds = dataOp.FindAllByQuery("BusFlowTrace", MongoDB.Driver.Builders.Query.And(query1, query2, query3)).Select(c => c.Int("preStepId")).ToList();
                    noticeStepIds.AddRange(hitStepIds.Where(c => !hitExecStepIds.Contains(c)));
                }
                else
                {
                    noticeStepIds.AddRange(selStepIds);
                }

            }
            else
            {
                noticeStepIds.Add(stepId);
            }
            var userIds = dataOp.FindAllByKeyVal("InstanceActionUser", "flowInstanceId", flowInstanceId).Where(c => noticeStepIds.Contains(c.Int("stepId"))).Select(c => c.Text("userId")).ToList();
            var allHitUser = dataOp.FindAllByKeyValList("SysUser", "userId", userIds).ToList();
            var taskLinkStr = string.Empty;
            var taskName = string.Empty;
            switch (curFlowInstance.Text("tableName"))
            {
                case "DesignChange":

                    var curDesignChangeObj = dataOp.FindOneByKeyVal("DesignChange", "designChangeId", curFlowInstance.Text("referFieldValue"));
                    if (curDesignChangeObj != null)
                    {
                        taskName = string.Format("{0}", curDesignChangeObj.Text("name"));
                    }
                    taskLinkStr = string.Format("{0}{1}{2}", SysAppConfig.MailHostDomain, "/DesignManage/DesignChangeWorkFlowInfo/?dngChangeId=", curFlowInstance.Text("referFieldValue"));
                    break;
                case "ProgrammeEvaluation":

                    var curProEvalObj = dataOp.FindOneByKeyVal("ProgrammeEvaluation", "proEvalId", curFlowInstance.Text("referFieldValue"));
                    if (curProEvalObj != null)
                    {
                        taskName = string.Format("{0}", curProEvalObj.Text("name"));
                        taskLinkStr = string.Format("/ProgrammeEvaluation/EvaluationWorkFlowInfo?projId={0}&proEvalId={1}", curProEvalObj.Text("projId"), curProEvalObj.Text("proEvalId"));

                    }

                    break;
                case "XH_DesignManage_Task":
                    var curTaskObj = dataOp.FindOneByKeyVal("XH_DesignManage_Task", "taskId", curFlowInstance.Text("referFieldValue"));
                    if (curTaskObj != null)
                    {
                        taskName = string.Format("{0}", curTaskObj.Text("name")); ;
                    }

                    taskLinkStr = string.Format("{0}{1}", "/DesignManage/ProjTaskInfo/", curFlowInstance.Text("referFieldValue"));


                    break;
                default:
                    taskLinkStr = string.Format("{0}{1}", "/DesignManage/DesignChangeWorkFlowInfo/?dngChangeId=", curFlowInstance.Text("referFieldValue"));

                    break;

            }
            //设置链接跳转地址


            var curUserName = dataOp.GetCurrentUserName();
            try
            {
                foreach (var toUserObj in allHitUser)
                {
                    var subject = "侨鑫研发信息平台系统流程提醒通知！";
                    var bodyStr = new StringBuilder();
                    //李军您好：侨鑫研发信息平台提醒您，您有一个审批流程已经超过N天时间了，请尽快访问系统予以处理，谢谢！访问地址为：http://xxxxx。
                    bodyStr.AppendFormat("<div>{0}：</div>", toUserObj.Text("name"));
                    bodyStr.AppendFormat("<DIV style='PADDING-LEFT: 20px'>您好！</DIV><DIV style='PADDING-LEFT: 20px'>侨鑫研发信息平台提醒您，");
                    bodyStr.AppendFormat("{0}提醒您 <FONT color=#ff0000 size=4>{0} </FONT>尽快处理以下审批流程</DIV>", curUserName);
                    var index = 1;
                    bodyStr.AppendFormat("<DIV style='PADDING: 20px; FONT-SIZE: 12px;'>");
                    bodyStr.AppendFormat("<TABLE style='BORDER: #c1d9f3 3px solid;' border=1 cellSpacing=0 borderColor=#e3e6eb cellPadding=0 width=500>");
                    bodyStr.AppendFormat("<TBODY>");
                    bodyStr.AppendFormat("<TR>");
                    bodyStr.AppendFormat("<TD bgColor=#f2f4f6 height=27 width=50 align=center><FONT size=2>编号</FONT></TD>");
                    bodyStr.AppendFormat("<TD bgColor=#f2f4f6><FONT size=2>流程名称</FONT></TD>");
                    bodyStr.AppendFormat("<TD bgColor=#f2f4f6 width=80 align=center><FONT size=2>操作</FONT></TD></TR>");
                    //设置邮件内容
                    var loginCheckUrl = SysAppConfig.MailHostDomain + "/Account/Mail_Login?name=";
                    var nameByte = Encoding.Unicode.GetBytes(toUserObj.Text("name"));
                    loginCheckUrl += Convert.ToBase64String(nameByte);
                    loginCheckUrl += "&ReturnUrl=" + Uri.EscapeDataString(taskLinkStr);

                    bodyStr.AppendFormat("<TR>");
                    bodyStr.AppendFormat(" <TD height=27 align=center><FONT size=2>{0}.</FONT></TD>", 1);
                    bodyStr.AppendFormat(" <TD><FONT size=2>{0}</FONT></TD>", taskName);
                    bodyStr.AppendFormat(" <TD align=center><A href='{0}'><FONT size=2>查看</FONT></A></TD></TR>", loginCheckUrl);
                    // bodyStr.AppendFormat("<p style='text-indent: 2em'>{2}.<a target='_blank' href='{0}'>{1}</a>&nbsp;&nbsp;<a target='_blank' href='{0}'>查看</a></p>", loginCheckUrl, taskName, );
                    bodyStr.AppendFormat(" </TR>");


                    bodyStr.AppendFormat("</TBODY></TABLE></DIV>");
                    bodyStr.Append("<DIV style='PADDING-LEFT: 20px'>请尽快访问系统予以处理，谢谢！<BR></DIV>");  //设置邮件内容是否为html格式
                    bool isBodyHtml = true;
                    var receiverIdList = new List<int>();
                    receiverIdList.Add(toUserObj.Int("userId"));
                    SendMail(receiverIdList, subject, bodyStr.ToString(), string.Empty, isBodyHtml);
                }
            }
            catch (Exception ex)
            {

            }
            json.Success = true;
            return Json(json);

        }

        /// <summary>
        /// 修改供应商状态
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult ChangeSupState()
        {
            InvokeResult result = new InvokeResult();
            BsonDocument dataBson = new BsonDocument();
            string tbName = "XH_Supplier_Supplier";

            var supplierId = PageReq.GetForm("supplierId");
            var actId = PageReq.GetForm("actId");
            var flowInstanceId = PageReq.GetForm("flowInstanceId");
            string queryStr = "db.XH_Supplier_Supplier.distinct('_id',{'supplierId':'" + supplierId + "'})";
            var InsObj = dataOp.FindOneByKeyVal("BusFlowInstance", "flowInstanceId", flowInstanceId);
            var actionObj = dataOp.FindOneByKeyVal("BusFlowAction", "actId", actId); //找到动作对象
            if (actionObj.Text("type") == "3") //actionObj.Text("type")=="3" 驳回   actionObj.Text("type")=="2" 通过
            {
                dataBson.Add("state", "0");    //入库待审批
            }
            else
            {
                if (actionObj.Text("type") == "2" && InsObj.Text("instanceStatus") == "1") //审批通过，流程实例结束（供应商入库审批通过）
                {
                    dataBson.Add("state", "2");    //审批通过
                }
                else
                {
                    dataBson.Add("state", "1");    //审批中
                }
            }
            result = dataOp.Save(tbName, queryStr != "" ? TypeConvert.NativeQueryToQuery(queryStr) : Query.Null, dataBson);

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }
        /// <summary>
        /// 删除任务流程实例
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult DeleteTaskInstance()
        {
            var taskId = PageReq.GetParam("taskId");
            var referenceDelList = new List<StorageData>();
            var instanceQuery = Query.And(Query.EQ("referFieldValue", taskId), Query.EQ("tableName", "XH_DesignManage_Task"), Query.EQ("referFieldName", "taskId"));
            referenceDelList.Add(new StorageData() { Type = StorageType.Delete, Query = instanceQuery, Name = "BusFlowInstance" });
            var result = dataOp.BatchSaveStorageData(referenceDelList);
            var json = TypeConvert.InvokeResultToPageJson(result);
            return this.Json(json);
        }




        #region 转办设置
        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult TurnOnRightToUser(int id)
        {
            PageJson json = new PageJson();
            var flowInstanceId = id;
            int givenUserId = PageReq.GetFormInt("givenUserId");//转办给与人
            int grantUserId = PageReq.GetFormInt("grantUserId");//转办接受者
            int flowId = PageReq.GetFormInt("flowId");
            var remark = PageReq.GetForm("remark");
            if (givenUserId == 0 || grantUserId==0)
            {
                json.Success = false;
                json.Message = "用户不存在或者参数有误，请联系管理员";
                return Json(json);
            }

            var result = new InvokeResult() { };
            #region 判断流程是否激活
            BusinessFlowTurnRightBll turnRightBll = BusinessFlowTurnRightBll._();
            BsonDocument flow = dataOp.FindOneByKeyVal("BusFlow", "flowId", flowId.ToString());

            if (flow == null)
            {
                json.Success = false;
                json.Message = "流程不存在或者未激活";
                return Json(json);
            }
            var instance = dataOp.FindOneByKeyVal("BusFlowInstance", "flowInstanceId", flowInstanceId.ToString());
            if (instance == null || instance.Int("instanceStatus") == 1)
            {
                json.Success = false;
                json.Message = "流程实例不存在或者已经审批结束";
                return Json(json);
            }
            #endregion
            #region 设定权限初始人员
            //注意此处可能A可能被B,C同时赋予不同的权限,因此当A要赋予D权限时候需要赋予两个权限项目
            var lastTurnRighQuery = turnRightBll.FindByFlowInstanceId(flowInstanceId, grantUserId).ToList();
            var addList = new List<BsonDocument>();
            if (lastTurnRighQuery.Count() > 0)
            {
                foreach (var c in lastTurnRighQuery)
                {
                    var curNewTurnRight = new BsonDocument();
                    curNewTurnRight.Add("givenUserId", givenUserId);
                    curNewTurnRight.Add("grantUserId", grantUserId);
                    curNewTurnRight.Add("flowInstanceId", flowInstanceId);
                    curNewTurnRight.Add("status", 0);
                    curNewTurnRight.Add("orginalUserId", c.Int("orginalUserId"));
                    curNewTurnRight.Add("remark", remark);
                    addList.Add(curNewTurnRight);
                }

                result = turnRightBll.Insert(addList, instance, this.CurrentUserId);
            }
            else//只添加一个权限项
            {
                var newTurnRight = new BsonDocument();
                newTurnRight.Add("givenUserId", givenUserId);
                newTurnRight.Add("grantUserId", grantUserId);
                newTurnRight.Add("flowInstanceId", flowInstanceId);
                newTurnRight.Add("status", 0);
                newTurnRight.Add("orginalUserId", grantUserId);
                newTurnRight.Add("remark", remark);
                result = turnRightBll.Insert(newTurnRight, instance, this.CurrentUserId);
            }
            #endregion

            #region 更改InstanceActionUser

            var allStepList = dataOp.FindAllByKeyVal("BusFlowStep", "flowId", flowId.ToString()).ToList();
            var curStepObj = allStepList.Where(c => c.Int("stepId") == instance.Int("stepId")).FirstOrDefault();
            if (curStepObj == null)
            {
                json.Success = false;
                json.Message = "流程参数出错，请刷新后重试";
                return Json(json);
            }
            var stepIds = allStepList.Where(c => c.Int("stepOrder") == curStepObj.Int("stepOrder")).Select(c => c.Int("stepId")).ToList();
            var actionUsersQuery = Query.And(Query.EQ("flowInstanceId", flowInstanceId.ToString()), Query.EQ("flowId", flowId.ToString()),Query.EQ("userId",grantUserId.ToString()));
            var actionUser = dataOp.FindAllByQuery("InstanceActionUser", actionUsersQuery).Where(c => stepIds.Contains(c.Int("stepId"))).FirstOrDefault();
            if (actionUser != null)
            {
                actionUser.Set("userId", givenUserId.ToString()).Set("orginalUserId", grantUserId.ToString());
                result = dataOp.Update("InstanceActionUser", actionUsersQuery, actionUser);
            }
            #endregion

            #region 执行事务提醒 给 givenUserId 发送邮件
            //List<int> userIds = new List<int>() { givenUserId };
            //PushToEKP_LG(instance, userIds, "您有一个转办审批任务：" + instance.instanceName);
            string smsTemple = "请审批{0}提交的流程{3}： {1}项目 {2}任务";
            BsonDocument task = _dataOp.FindOneByKeyVal("XH_DesignManage_Task", "taskId", instance.Text("referFieldValue"));
            BsonDocument project = _dataOp.FindOneByKeyVal("XH_DesignManage_Task", "projId", task.Text("projId"));
            BsonDocument givenUser = _dataOp.FindOneByKeyVal("SysUser", "userId", givenUserId.ToString());
            BsonDocument grantUser = _dataOp.FindOneByKeyVal("SysUser", "userId", grantUserId.ToString());
            string content = string.Format(smsTemple, grantUser.Text("name"), project.Text("name"), task.Text("name"), instance.Text("instanceName"));

            Yinhe.ProcessingCenter.BusinessFlow.SendFinishMsg sms = new SendFinishMsg();
            sms.Send(new List<string> { givenUser.Text("mobileNumber") }, content);
            #endregion

            #region QX给OA发送待办已办消息
            if (SysAppConfig.CustomerCode == CustomerCode.QX)
            {
                UpdateOAToDoList QXPending = new UpdateOAToDoList();
                QXPending.QXInsertFlowDone(instance, instance.Int("stepId"), grantUserId);
                QXPending.QXInsertFlowToDo(instance, instance.Int("stepId"), grantUserId, new List<int> { givenUserId });
            }
            #endregion

            json.Success = result.Status == Status.Successful ? true : false;
            json.Message = result.Message;

            return Json(json);
        }

        /// <summary>
        /// 2013.12.2 新增转办直接替换执行人
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult TurnOnToUser(int id)
        {
            PageJson json = new PageJson();
            var flowInstanceId = id;
            int givenUserId = PageReq.GetFormInt("givenUserId");//转办给与人
            int grantUserId = PageReq.GetFormInt("grantUserId");//转办接受者
            int flowId = PageReq.GetFormInt("flowId");
            var remark = Server.UrlDecode(PageReq.GetForm("remark"));
            if (givenUserId == 0 || grantUserId == 0)
            {
                json.Success = false;
                json.Message = "用户不存在或者参数有误，请联系管理员";
                return Json(json);
            }
            var result = new InvokeResult() { };
            #region 判断流程是否激活
            BusinessFlowUserGrantBll turnRightBll = BusinessFlowUserGrantBll._();
            BsonDocument flow = dataOp.FindOneByKeyVal("BusFlow", "flowId", flowId.ToString());

            if (flow == null)
            {
                json.Success = false;
                json.Message = "流程不存在或者未激活";
                return Json(json);
            }
            var instance = dataOp.FindOneByKeyVal("BusFlowInstance", "flowInstanceId", flowInstanceId.ToString());
            if (instance == null || instance.Int("instanceStatus") == 1)
            {
                json.Success = false;
                json.Message = "流程实例不存在或者已经审批结束";
                return Json(json);
            }
            #endregion

           

            #region 更改InstanceActionUser
            var  allInstanceActionUserList=dataOp.FindAllByKeyVal("InstanceActionUser", "flowInstanceId",flowInstanceId.ToString()).ToList();
            var curActionUser = allInstanceActionUserList.Where(c=>c.Int("stepId")==instance.Int("stepId")).FirstOrDefault();
            if (curActionUser != null)
            {
                var curStepOrderStepIds = allInstanceActionUserList.Where(c => c.Text("stepOrder")!=""&& c.Int("stepOrder") == curActionUser.Int("stepOrder")).Select(c=>c.Int("stepId")).ToList();//获取会签步骤的
                curStepOrderStepIds.Add(instance.Int("stepId"));//兼容旧数据
                //此处需要过滤掉已经执行过的对象
                var actionUser = allInstanceActionUserList.Where(c => curStepOrderStepIds.Contains(c.Int("stepId")) && c.Int("userId") == grantUserId).Where(c => c.Int("actionAvaiable")==0).FirstOrDefault();//可能会签步骤中有2个部门同一个人
                if (actionUser != null)
                {
                    if (grantUserId != givenUserId)
                    {
                      var updateBosn=new BsonDocument().Set("userId", givenUserId.ToString());
                        if (actionUser.Int("orginalUserId") == 0)//只能记录一次初始人员
                        {
                            updateBosn.Set("orginalUserId", grantUserId.ToString());
                        }
                        result = dataOp.Update(actionUser, updateBosn);
                        //if (result.Status == Status.Successful)
                        //{
                        //    var givenUserName = dataOp.FindOneByKeyVal("SysUser", "userId", givenUserId.ToString());
                        //    BsonDocument flowTrace = new BsonDocument();
                        //    flowTrace.Add("flowInstanceId", instance.Int("flowInstanceId"));
                        //    flowTrace.Add("traceType", 6);
                        //    flowTrace.Add("remark", remark);
                        //    flowTrace.Add("preStepId", actionUser.Int("stepId"));
                        //    flowTrace.Add("result", string.Format("将操作权转办给了{0}", givenUserName != null ? givenUserName.Text("name") : string.Empty));
                        //    var traceResult = BusFlowTraceBll._(dataOp).Insert(flowTrace);
                        //}
                    }
                    else
                    {
                        json.Success = false;
                        json.Message = "不能转办给自己";
                        return Json(json);
                    }


                    #region 设定权限初始人员
                    //注意此处可能A可能被B,C同时赋予不同的权限,因此当A要赋予D权限时候需要赋予两个权限项目
                        var newTurnRight = new BsonDocument();
                        newTurnRight.Add("givenUserId", givenUserId);
                        newTurnRight.Add("grantUserId", grantUserId);
                        newTurnRight.Add("flowInstanceId", flowInstanceId);
                        newTurnRight.Add("stepId", actionUser.Text("stepId"));
                        newTurnRight.Add("stepOrder", actionUser.Text("stepOrder"));
                        newTurnRight.Add("inActId", actionUser.Text("inActId"));
                        newTurnRight.Add("status", 0);
                        newTurnRight.Add("orginalUserId", grantUserId);
                        newTurnRight.Add("remark", remark);
                        result = turnRightBll.Insert(newTurnRight, instance,this.CurrentUserId);
                    
                    #endregion
                }
            }
            #endregion

            #region 执行事务提醒 给 givenUserId 发送邮件
            //List<int> userIds = new List<int>() { givenUserId };
            //PushToEKP_LG(instance, userIds, "您有一个转办审批任务：" + instance.instanceName);
            string smsTemple = "请审批{0}提交的流程： {1} ";
            // BsonDocument task = _dataOp.FindOneByKeyVal(instance.Text("tableName"), instance.Text("referFieldName"), instance.Text("referFieldValue"));
            BsonDocument givenUser = _dataOp.FindOneByKeyVal("SysUser", "userId", givenUserId.ToString());
            BsonDocument grantUser = _dataOp.FindOneByKeyVal("SysUser", "userId", grantUserId.ToString());
            string content = string.Format(smsTemple, grantUser.Text("name"), instance.Text("instanceName"));

            Yinhe.ProcessingCenter.BusinessFlow.SendFinishMsg sms = new SendFinishMsg();
            sms.Send(new List<string> { givenUser.Text("mobileNumber") }, content);
            #endregion

            #region QX给OA发送待办已办消息
            if (SysAppConfig.CustomerCode == CustomerCode.QX)
            {
                UpdateOAToDoList QXPending = new UpdateOAToDoList();
                QXPending.QXInsertFlowDone(instance, instance.Int("stepId"), grantUserId);
                QXPending.QXInsertFlowToDo(instance, instance.Int("stepId"), grantUserId, new List<int> { givenUserId });
            }
            #endregion

            json.Success = result.Status == Status.Successful ? true : false;
            json.Message = result.Message;

            return Json(json);
        }

        #endregion

        #region  传阅设置
        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult CirculateToUser(int id)
        {
            PageJson json = new PageJson();
            var flowInstanceId = id;
            var givenUserIds = PageReq.GetFormIntList("givenUserIds");//转办给与人
            int grantUserId = PageReq.GetFormInt("grantUserId");//转办接受者
            int flowId = PageReq.GetFormInt("flowId");
            var remark = PageReq.GetForm("remark");
            var stepId = PageReq.GetFormInt("stepId");
            var result = new InvokeResult() { };
            #region 判断流程是否激活
            BusinessFlowCirculationBll turnRightBll = BusinessFlowCirculationBll._();
            BsonDocument flow = dataOp.FindOneByKeyVal("BusFlow", "flowId", flowId.ToString());

            if (flow == null)
            {
                json.Success = false;
                json.Message = "流程不存在或者未激活";
                return Json(json);
            }
            var instance = dataOp.FindOneByKeyVal("BusFlowInstance", "flowInstanceId", flowInstanceId.ToString());
            if (instance == null || instance.Int("instanceStatus") == 1)
            {
                json.Success = false;
                json.Message = "流程实例不存在或者已经审批结束";
                return Json(json);
            }
            #endregion
            #region

            var lastTurnRighQuery = turnRightBll.FindByFlowInstanceId(flowInstanceId, grantUserId, stepId).Select(c => c.Int("givenUserId")).ToList();
            var addCirculateUserId = givenUserIds.Where(c => !lastTurnRighQuery.Contains(c)).ToList();
            var addList = new List<BsonDocument>();

            foreach (var givenUserId in addCirculateUserId)
            {
                var newTurnRight = new BsonDocument();
                newTurnRight.Add("givenUserId", givenUserId);
                newTurnRight.Add("grantUserId", grantUserId);
                newTurnRight.Add("flowInstanceId", flowInstanceId);
                newTurnRight.Add("status", 0);
                newTurnRight.Add("orginalUserId", grantUserId);
                newTurnRight.Add("remark", remark);
                newTurnRight.Add("stepId", stepId);
                addList.Add(newTurnRight);
            }
            result = turnRightBll.Insert(addList, instance, this.CurrentUserId);
            #endregion

            #region 执行事务提醒 给 givenUserId 发送邮件
            //List<int> userIds = new List<int>() { givenUserId };
            //PushToEKP_LG(instance, userIds, "您有一个转办审批任务：" + instance.instanceName);
            string smsTemple = "请查看{0}传阅给您的流程{3}：{1}项目 {2}任务";
            BsonDocument task = _dataOp.FindOneByKeyVal("XH_DesignManage_Task", "taskId", instance.Text("referFieldValue"));
            BsonDocument project = _dataOp.FindOneByKeyVal("XH_DesignManage_Task", "projId", task.Text("projId"));

            BsonDocument grantUser = _dataOp.FindOneByKeyVal("SysUser", "userId", grantUserId.ToString());
            string content = string.Format(smsTemple, grantUser.Text("name"), project.Text("name"), task.Text("name"), instance.Text("instanceName"));

            List<string> userIdList = (from t in givenUserIds
                                       select t.ToString()).ToList();
            List<string> numList = dataOp.FindAllByKeyValList("SysUser", "userId", userIdList).Where(t => t.Text("mobileNumber") != "").Select(t => t.Text("mobileNumber")).ToList();
            Yinhe.ProcessingCenter.BusinessFlow.SendFinishMsg sms = new SendFinishMsg();
            sms.Send(numList, content);
            #endregion
            json.Success = result.Status == Status.Successful ? true : false;
            json.Message = result.Message;

            return Json(json);
        }

        /// <summary>
        /// 阅读闭确认
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult CirculateComplete(int id)
        {
            PageJson json = new PageJson();
            var flowInstanceId = id;
            int flowId = PageReq.GetFormInt("flowId");
            var remark = PageReq.GetForm("remark");
            var stepId = PageReq.GetFormInt("stepId");

            #region 判断流程是否激活
            BusinessFlowCirculationBll turnRightBll = BusinessFlowCirculationBll._();
            BsonDocument flow = dataOp.FindOneByKeyVal("BusFlow", "flowId", flowId.ToString());

            if (flow == null)
            {
                json.Success = false;
                json.Message = "流程不存在或者未激活";
                return Json(json);
            }
            var instance = dataOp.FindOneByKeyVal("BusFlowInstance", "flowInstanceId", flowInstanceId.ToString());
            if (instance == null || instance.Int("instanceStatus") == 1)
            {
                json.Success = false;
                json.Message = "流程实例不存在或者已经审批结束";
                return Json(json);
            }
            #endregion
            var result = turnRightBll.CirculateComplete(flowInstanceId, dataOp.GetCurrentUserId(), stepId, remark);
            json.Success = result.Status == Status.Successful ? true : false;
            json.Message = result.Message;

            return Json(json);

        }
        #endregion

        #endregion

        #region 经济技术指标模板
        /// <summary>
        /// 项目经济技术指标导出成模板
        /// </summary>
        /// <param name="engId">导出的工程Id</param>
        /// <param name="itemTb">项的表名</param>
        /// <param name="columnTb">列的表名</param>
        /// <param name="templateTb">模板的表名</param>
        /// <param name="dataStr">模板记录的附加数据</param>
        /// <returns></returns>
        public ActionResult ProjIndexStructExportTemplate(int engId, string itemTb, string columnTb, string templateTb, string dataStr)
        {
            List<string> noNeedColumn = new List<string>() { "_id", "itemId", "columnId", "engId", "nodePid", "nodeLevel", "nodeOrder", "nodeKey", "createDate", "updateDate", "createUserId", "updateUserId", "underTable", "order" };

            #region 构建项模板
            StringBuilder itemContent = new StringBuilder();

            List<BsonDocument> itemList = dataOp.FindAllByQueryStr(itemTb, "engId=" + engId).ToList();     //所有要导出的项

            Dictionary<int, int> itemDic = new Dictionary<int, int>();  //节点Id对应字典(用于树形pid)

            int itemIndex = 0;
            foreach (var tempItem in itemList.OrderBy(t => t.String("nodeKey")))
            {
                itemIndex++;

                itemDic.Add(tempItem.Int("itemId"), itemIndex);

                BsonDocument tempBson = new BsonDocument(); //导入的模板BSON

                tempBson.Add("id", itemIndex);

                if (itemDic.ContainsKey(tempItem.Int("nodePid")))
                {
                    tempBson.Add("pid", itemDic[tempItem.Int("nodePid")].ToString());
                }
                else if (tempItem.Int("nodePid") == 0) tempBson.Add("pid", "0");
                else continue;

                foreach (var tempElement in tempItem.Elements)
                {
                    if (noNeedColumn.Contains(tempElement.Name)) continue;

                    tempBson.Add(tempElement.Name, tempElement.Value);
                }

                itemContent.Append(tempBson.ToString());
            }
            #endregion

            #region 构建列模板
            StringBuilder columnContent = new StringBuilder();

            List<BsonDocument> columnList = dataOp.FindAllByQueryStr(columnTb, "engId=" + engId).ToList();     //所有要导出的项

            int columnIndex = 0;
            foreach (var tempColumn in columnList.OrderBy(t => t.String("nodeKey")))
            {
                columnIndex++;

                BsonDocument tempBson = new BsonDocument(); //导入的模板BSON

                tempBson.Add("id", columnIndex);

                foreach (var tempElement in tempColumn.Elements)
                {
                    if (noNeedColumn.Contains(tempElement.Name)) continue;

                    tempBson.Add(tempElement.Name, tempElement.Value);
                }

                columnContent.Append(tempBson.ToString());
            }
            #endregion

            #region 保存至模板库
            BsonDocument template = TypeConvert.ParamStrToBsonDocument(dataStr);

            template.Add("engId", engId.ToString());
            template.Add("itemContent", itemContent.ToString());
            template.Add("columnContent", columnContent.ToString());

            InvokeResult result = dataOp.Insert(templateTb, template);
            #endregion

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        /// <summary>
        /// 项目经济技术指标导出成模板
        /// </summary>
        /// <param name="engId">导出的工程Id</param>
        /// <param name="itemTb">项的表名</param>
        /// <param name="columnTb">列的表名</param>
        /// <param name="templateTb">模板的表名</param>
        /// <param name="dataStr">模板记录的附加数据</param>
        /// <returns></returns>
        public ActionResult ProjIndexStructExportTemplateSN(int engId, int projId, string itemTb, string columnTb, string templateTb, string dataStr)
        {
            List<string> noNeedColumn = new List<string>() { "_id", "itemId", "columnId", "engId", "projId", "nodePid", "nodeLevel", "nodeOrder", "nodeKey", "createDate", "updateDate", "createUserId", "updateUserId", "underTable", "order" };

            #region 构建项模板
            StringBuilder itemContent = new StringBuilder();

            List<BsonDocument> itemList = projId == 0 ? dataOp.FindAllByQuery(itemTb, Query.And(Query.EQ("engId", engId.ToString()), Query.EQ("projId", "0"))).ToList() : dataOp.FindAllByQueryStr(itemTb, "projId=" + projId).ToList();     //所有要导出的项

            Dictionary<int, int> itemDic = new Dictionary<int, int>();  //节点Id对应字典(用于树形pid)

            int itemIndex = 0;
            foreach (var tempItem in itemList.OrderBy(t => t.String("nodeKey")))
            {
                itemIndex++;

                itemDic.Add(tempItem.Int("itemId"), itemIndex);

                BsonDocument tempBson = new BsonDocument(); //导入的模板BSON

                tempBson.Add("id", itemIndex);

                if (itemDic.ContainsKey(tempItem.Int("nodePid")))
                {
                    tempBson.Add("pid", itemDic[tempItem.Int("nodePid")].ToString());
                }
                else if (tempItem.Int("nodePid") == 0) tempBson.Add("pid", "0");
                else continue;

                foreach (var tempElement in tempItem.Elements)
                {
                    if (noNeedColumn.Contains(tempElement.Name)) continue;

                    tempBson.Add(tempElement.Name, tempElement.Value);
                }

                itemContent.Append(tempBson.ToString());
            }
            #endregion

            #region 构建列模板
            StringBuilder columnContent = new StringBuilder();

            List<BsonDocument> columnList = projId == 0 ? dataOp.FindAllByQuery(columnTb, Query.And(Query.EQ("engId", engId.ToString()), Query.EQ("projId", "0"))).ToList() : dataOp.FindAllByQueryStr(columnTb, "projId=" + projId).ToList();     //所有要导出的项

            int columnIndex = 0;
            foreach (var tempColumn in columnList.OrderBy(t => t.String("nodeKey")))
            {
                columnIndex++;

                BsonDocument tempBson = new BsonDocument(); //导入的模板BSON

                tempBson.Add("id", columnIndex);

                foreach (var tempElement in tempColumn.Elements)
                {
                    if (noNeedColumn.Contains(tempElement.Name)) continue;

                    tempBson.Add(tempElement.Name, tempElement.Value);
                }

                columnContent.Append(tempBson.ToString());
            }
            #endregion

            #region 保存至模板库
            BsonDocument template = TypeConvert.ParamStrToBsonDocument(dataStr);

            template.Add("engId", engId.ToString());
            template.Add("projId", projId.ToString());
            template.Add("itemContent", itemContent.ToString());
            template.Add("columnContent", columnContent.ToString());

            InvokeResult result = dataOp.Insert(templateTb, template);
            #endregion

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        /// <summary>
        /// 项目经济技术指标由模板导入
        /// </summary>
        /// <param name="templateId">要导入的模板Id</param>
        /// <param name="engId">要导入的工程Id</param>
        /// <param name="isAdd">是否追加,默认0为覆盖1为追加</param>
        /// <param name="itemTb">项的表名</param>
        /// <param name="columnTb">列的表名</param>
        /// <param name="templateTb">模板的表名</param>
        /// <returns></returns>
        public ActionResult ProjIndexStructImportTemplateSN(int templateId, int engId, int projId, int isAdd, string itemTb, string columnTb, string templateTb)
        {

            BsonDocument template = dataOp.FindOneByKeyVal(templateTb, "templateId", templateId.ToString());    //获取模板信息

            #region 获取项和列的数据记录
            List<BsonDocument> itemList = new List<BsonDocument>();     //所有要导入的项数据

            BsonReader itemReader = BsonReader.Create(template.String("itemContent"));

            while (itemReader.CurrentBsonType != BsonType.EndOfDocument)
            {
                BsonDocument tempBson = BsonDocument.ReadFrom(itemReader);

                itemList.Add(tempBson);
            }

            List<BsonDocument> columnList = new List<BsonDocument>();     //所有要导入的列数据

            BsonReader columnReader = BsonReader.Create(template.String("columnContent"));

            while (columnReader.CurrentBsonType != BsonType.EndOfDocument)
            {
                BsonDocument tempBson = BsonDocument.ReadFrom(columnReader);

                columnList.Add(tempBson);
            }

            #endregion

            #region 将项和列的数据导入
            InvokeResult result = new InvokeResult();

            Dictionary<int, int> corDic = new Dictionary<int, int>(); //节点Id对应字典(用于树形pid)
            corDic.Add(0, 0);

            try
            {
                using (TransactionScope trans = new TransactionScope())
                {
                    if (isAdd == 0)     //如果不是追加,则删除所有已有的项和列记录
                    {
                        var query = projId == 0 ? Query.And(Query.EQ("engId", engId.ToString()), Query.EQ("projId", "0")) : Query.EQ("projId", projId.ToString());

                        InvokeResult tempRet = dataOp.Delete(itemTb, query);
                        if (tempRet.Status == Status.Failed) throw new Exception(tempRet.Message);

                        tempRet = dataOp.Delete(columnTb, query);
                        if (tempRet.Status == Status.Failed) throw new Exception(tempRet.Message);
                    }

                    foreach (var tempBson in itemList)
                    {
                        int tempId = tempBson.Int("id");
                        int tempPid = tempBson.Int("pid");

                        if (corDic.ContainsKey(tempPid))
                        {
                            tempBson.Add("engId", engId.ToString());
                            tempBson.Add("projId", projId.ToString());
                            tempBson.Add("nodePid", corDic[tempPid].ToString());
                            tempBson.Remove("id");
                            tempBson.Remove("pid");

                            InvokeResult tempRet = dataOp.Insert(itemTb, tempBson);
                            if (tempRet.Status == Status.Failed) throw new Exception(tempRet.Message);

                            corDic.Add(tempId, tempRet.BsonInfo.Int("itemId"));
                        }
                    }

                    foreach (var tempBson in columnList)
                    {
                        tempBson.Add("engId", engId.ToString());
                        tempBson.Add("projId", projId.ToString());
                        tempBson.Remove("id");

                        InvokeResult tempRet = dataOp.Insert(columnTb, tempBson);
                        if (tempRet.Status == Status.Failed) throw new Exception(tempRet.Message);

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
        /// 项目经济技术指标由模板导入
        /// </summary>
        /// <param name="templateId">要导入的模板Id</param>
        /// <param name="engId">要导入的工程Id</param>
        /// <param name="isAdd">是否追加,默认0为覆盖1为追加</param>
        /// <param name="itemTb">项的表名</param>
        /// <param name="columnTb">列的表名</param>
        /// <param name="templateTb">模板的表名</param>
        /// <returns></returns>
        public ActionResult ProjIndexStructImportTemplate(int templateId, int engId, int isAdd, string itemTb, string columnTb, string templateTb)
        {
            BsonDocument template = dataOp.FindOneByKeyVal(templateTb, "templateId", templateId.ToString());    //获取模板信息

            #region 获取项和列的数据记录
            List<BsonDocument> itemList = new List<BsonDocument>();     //所有要导入的项数据

            BsonReader itemReader = BsonReader.Create(template.String("itemContent"));

            while (itemReader.CurrentBsonType != BsonType.EndOfDocument)
            {
                BsonDocument tempBson = BsonDocument.ReadFrom(itemReader);

                itemList.Add(tempBson);
            }

            List<BsonDocument> columnList = new List<BsonDocument>();     //所有要导入的列数据

            BsonReader columnReader = BsonReader.Create(template.String("columnContent"));

            while (columnReader.CurrentBsonType != BsonType.EndOfDocument)
            {
                BsonDocument tempBson = BsonDocument.ReadFrom(columnReader);

                columnList.Add(tempBson);
            }

            #endregion

            #region 将项和列的数据导入
            InvokeResult result = new InvokeResult();

            Dictionary<int, int> corDic = new Dictionary<int, int>(); //节点Id对应字典(用于树形pid)
            corDic.Add(0, 0);

            try
            {
                using (TransactionScope trans = new TransactionScope())
                {
                    if (isAdd == 0)     //如果不是追加,则删除所有已有的项和列记录
                    {
                        var query = Query.EQ("engId", engId.ToString());

                        InvokeResult tempRet = dataOp.Delete(itemTb, query);
                        if (tempRet.Status == Status.Failed) throw new Exception(tempRet.Message);

                        tempRet = dataOp.Delete(columnTb, query);
                        if (tempRet.Status == Status.Failed) throw new Exception(tempRet.Message);
                    }

                    foreach (var tempBson in itemList)
                    {
                        int tempId = tempBson.Int("id");
                        int tempPid = tempBson.Int("pid");

                        if (corDic.ContainsKey(tempPid))
                        {
                            tempBson.Add("engId", engId.ToString());
                            tempBson.Add("nodePid", corDic[tempPid].ToString());
                            tempBson.Remove("id");
                            tempBson.Remove("pid");

                            InvokeResult tempRet = dataOp.Insert(itemTb, tempBson);
                            if (tempRet.Status == Status.Failed) throw new Exception(tempRet.Message);

                            corDic.Add(tempId, tempRet.BsonInfo.Int("itemId"));
                        }
                    }

                    foreach (var tempBson in columnList)
                    {
                        tempBson.Add("engId", engId.ToString());
                        tempBson.Remove("id");

                        InvokeResult tempRet = dataOp.Insert(columnTb, tempBson);
                        if (tempRet.Status == Status.Failed) throw new Exception(tempRet.Message);

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
        #endregion

        #region 项目设计供应商

        #region 添加项目设计供应商
        /// <summary>
        /// 添加项目设计供应商
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult AddProjectSupplierRelation()
        {
            InvokeResult result = new InvokeResult();
            string projId = PageReq.GetForm("projId");
            string supplierIds = PageReq.GetForm("supplierIds");

            string[] supplierIdArray = supplierIds.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var item in supplierIdArray.Distinct())
            {
                var exist = dataOp.FindAllByQueryStr("XH_DesignManage_ProjectSupplier", string.Format("supplierId={0}&projId={1}", item, projId));
                if (exist.Count() == 0)
                {
                    BsonDocument listdoc = new BsonDocument();
                    listdoc.Add("supplierId", item);
                    listdoc.Add("projId", projId);
                    result = dataOp.Insert("XH_DesignManage_ProjectSupplier", listdoc);
                    if (result.Status == Status.Failed)
                    {
                        return Json(TypeConvert.InvokeResultToPageJson(result));
                    }
                }
            }

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }
        /// <summary>
        /// 添加项目设计供应商
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult QX_AddProjectSupplierRelation()
        {
            InvokeResult result = new InvokeResult();
            string projId = PageReq.GetForm("projId");
            string supplierIds = PageReq.GetForm("supplierIds");
            var type = PageReq.GetForm("type");//合作类型，建筑，景观，室内...
            string[] supplierIdArray = supplierIds.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var item in supplierIdArray.Distinct())
            {
                var exist = dataOp.FindAllByQuery("XH_DesignManage_ProjectSupplier",
                        Query.And(
                            Query.EQ("supplierId", item.ToString()),
                            Query.EQ("projId", projId.ToString()),
                            Query.EQ("type", type.ToString())
                        )
                    );
                if (exist.Count() == 0)
                {
                    BsonDocument listdoc = new BsonDocument();
                    listdoc.Add("supplierId", item);
                    listdoc.Add("projId", projId);
                    listdoc.Add("type", type);
                    result = dataOp.Insert("XH_DesignManage_ProjectSupplier", listdoc);
                    if (result.Status == Status.Failed)
                    {
                        return Json(TypeConvert.InvokeResultToPageJson(result));
                    }
                }
            }

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }
        /// <summary>
        /// 添加地块设计供应商
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult QX_AddEngSupplierRelation()
        {
            InvokeResult result = new InvokeResult();
            string engId = PageReq.GetForm("engId");
            string supplierIds = PageReq.GetForm("supplierIds");
            var type = PageReq.GetForm("type");//合作类型，建筑，景观，室内...
            string[] supplierIdArray = supplierIds.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var item in supplierIdArray.Distinct())
            {
                var exist = dataOp.FindAllByQuery("XH_DesignManage_EngSupplier",
                        Query.And(
                            Query.EQ("supplierId", item.ToString()),
                            Query.EQ("engId", engId.ToString()),
                            Query.EQ("type", type.ToString())
                        )
                    );
                if (exist.Count() == 0)
                {
                    BsonDocument listdoc = new BsonDocument();
                    listdoc.Add("supplierId", item);
                    listdoc.Add("engId", engId);
                    listdoc.Add("type", type);
                    result = dataOp.Insert("XH_DesignManage_EngSupplier", listdoc);
                    if (result.Status == Status.Failed)
                    {
                        return Json(TypeConvert.InvokeResultToPageJson(result));
                    }
                }
            }

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }
        #endregion

        #region 保存项目设计供应商合作信息
        /// <summary>
        /// 保存基本信息及合作部门 QX
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult SaveProjSupInfo(FormCollection saveForm)
        {
            InvokeResult result = new InvokeResult() { Status = Status.Successful };
            string queryStr = PageReq.GetForm("queryStr");//项目与供应商关联ID
            string tableName = PageReq.GetForm("tbName");
            var orgIdList = PageReq.GetFormList("orgIds");//合作部门ID列表
            if (string.IsNullOrWhiteSpace(tableName))
            {
                tableName = "XH_DesignManage_ProjectSupplier";
            }
            BsonDocument doc = new BsonDocument();
            string[] skipKeys = { "tbName", "queryStr", "orgIds" };
            foreach (var key in saveForm.AllKeys)
            {
                if (!skipKeys.Contains(key))
                {
                    doc.Add(key, saveForm[key]);
                }
            }
            result = dataOp.Save(tableName, TypeConvert.NativeQueryToQuery(queryStr), doc);
            if (result.Status == Status.Failed)
            {
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            #region 保存合作部门
            var relObj = result.BsonInfo;
            //获取旧的部门
            var oldRelList = dataOp.FindAllByQuery("XH_Supplier_ProjSupOrg", Query.EQ("relId", relObj.Text("relId"))).ToList();
            List<StorageData> dataList = new List<StorageData>();
            foreach (var orgId in orgIdList)
            {
                var oldRel = oldRelList.Where(p => p.Text("orgId") == orgId).FirstOrDefault();
                if (!BsonDocumentExtension.IsNullOrEmpty(oldRel))
                {
                    oldRelList.Remove(oldRel);
                    continue;
                }
                StorageData data = new StorageData();
                data.Name = "XH_Supplier_ProjSupOrg";
                data.Document = new BsonDocument(){
                    { "relId",relObj.Text("relId")},
                    { "orgId",orgId}
                };
                data.Type = StorageType.Insert;
                dataList.Add(data);
            }
            foreach (var oldRel in oldRelList)
            {
                StorageData data = new StorageData();
                data.Name = "XH_Supplier_ProjSupOrg";
                data.Query = Query.EQ("relId", oldRel.Text("relId"));
                data.Type = StorageType.Delete;
                dataList.Add(data);
            }

            #endregion
            result = dataOp.BatchSaveStorageData(dataList);
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }
        #endregion

        #endregion

        #region 工程项目树形XML
        /// <summary>
        /// 工程项目树形XML
        /// </summary>
        /// <param name="engTb"></param>
        /// <param name="projTb"></param>
        /// <param name="engId"></param>
        /// <returns></returns>
        public ActionResult GetEngProjectTreeXML(string engTb, string projTb, int engId)
        {
            List<BsonDocument> projList = dataOp.FindAllByQuery(projTb, Query.EQ("engId", engId.ToString())).ToList();

            BsonDocument engEntity = dataOp.FindOneByKeyVal(engTb, "engId", engId.ToString());

            TreeNode engNode = new TreeNode();

            engNode.Id = engId;
            engNode.Name = engEntity.String("name");
            engNode.Lv = 1;
            engNode.Pid = 0;
            engNode.underTable = engEntity.String("underTable");
            engNode.Param = "eng";
            engNode.SubNodes = GetEngProjectSubTreeXMLt(projList, 0);    //获取子节点列表;
            engNode.IsLeaf = 0;

            List<TreeNode> treeList = new List<TreeNode>();
            treeList.Add(engNode);

            return new XmlTree(treeList);
        }


        /// <summary>
        /// 工程项目树形XML
        /// </summary>
        /// <param name="engTb"></param>
        /// <param name="projTb"></param>
        /// <param name="engId"></param>
        /// <returns></returns>
        public ActionResult GetLandProjectTreeXML(string LandTb, string projTb, int landId)
        {
            List<BsonDocument> projList = dataOp.FindAllByQuery(projTb, Query.EQ("landId", landId.ToString())).ToList();

            BsonDocument engEntity = dataOp.FindOneByKeyVal(LandTb, "landId", landId.ToString());

            TreeNode engNode = new TreeNode();

            engNode.Id = landId;
            engNode.Name = engEntity.String("name");
            engNode.Lv = 1;
            engNode.Pid = 0;
            engNode.underTable = engEntity.String("underTable");
            engNode.Param = "land";
            engNode.SubNodes = GetEngProjectSubTreeXMLt(projList, 0);    //获取子节点列表;
            engNode.IsLeaf = 0;

            List<TreeNode> treeList = new List<TreeNode>();
            treeList.Add(engNode);

            return new XmlTree(treeList);
        }

        /// <summary>
        /// 获取项目子树XML
        /// </summary>
        /// <param name="projList"></param>
        /// <param name="projId"></param>
        /// <returns></returns>
        public List<TreeNode> GetEngProjectSubTreeXMLt(List<BsonDocument> projList, int projId)
        {
            BsonDocument curNode = projList.Where(t => t.Int("projId") == projId).FirstOrDefault(); //当前项目节点

            List<BsonDocument> subProjList = projList.Where(t => t.Int("nodePid") == projId).ToList(); //子项目列表

            List<TreeNode> treeList = new List<TreeNode>();

            foreach (var subNode in subProjList.OrderBy(t => t.Int("nodeOrder")))      //循环子部门列表,赋值
            {
                TreeNode node = new TreeNode();

                node.Id = subNode.Int("projId");
                node.Name = subNode.String("name");
                node.Lv = curNode.Int("nodeLevel") + 2;
                node.Pid = projId;
                node.underTable = subNode.String("underTable");
                node.Param = "proj";
                node.SubNodes = GetEngProjectSubTreeXMLt(projList, node.Id);    //获取子节点列表;
                node.IsLeaf = 0;

                treeList.Add(node);
            }

            return treeList;
        }

        #endregion

        #region 项目指标值保存
        /// <summary>
        /// 保存项目指标值
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        public ActionResult SaveProjTargetValue(string tbName, string queryStr, string value)
        {
            InvokeResult result = new InvokeResult();

            BsonDocument curData = new BsonDocument();  //当前数据,即操作前数据

            if (queryStr.Trim() != "") curData = dataOp.FindOneByQuery(tbName, TypeConvert.NativeQueryToQuery(queryStr));

            if (value != curData.String("value"))   //如果值发生改变,则保存值,并保存版本,否则不进行操作
            {
                string tempTime = DateTime.Now.ToString("yyyyMMddHHmmss");
                string oldVal = string.IsNullOrEmpty(curData.String("value")) ? "空" : curData.String("value");

                var dataBson = new BsonDocument().Add("value", value)
                                                 .Add("verVal_" + tempTime, oldVal)
                                                 .Add("verUser_" + tempTime, CurrentUserId);
                result = dataOp.Update(tbName, TypeConvert.NativeQueryToQuery(queryStr), dataBson);
            }

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        #endregion

        #region 地铁图编辑


        /// <summary>
        /// 编辑保存地铁图对象
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult AddSubWayMap()
        {
            var json = new PageJson();
            var mapId = PageReq.GetForm("mapId");
            var imagePath = PageReq.GetForm("imagePath");
            var name = PageReq.GetForm("name");
            var type = PageReq.GetForm("type");
            var result = new InvokeResult() { Status = Status.Failed };
            var allSubWayMapList = dataOp.FindAll("XH_DesignManage_SubwayMap").ToList();
            var curMapObj = allSubWayMapList.Where(c => c.Text("mapId") == mapId).FirstOrDefault();
            var needChangPic = PageReq.GetFormInt("needChangPic");
            bool isSave = false;
            try
            {
                var hasExist = allSubWayMapList.Where(c => c.Text("mapId") != mapId && c.Text("name") == name).Count() > 0;
                isSave = !hasExist;
            }
            catch (System.Exception ex)
            {
                return RedirectToAction("/SubwayMapList", new { controller = "DesignManage", save = (result.Status == Status.Successful ? 1 : 0).ToString() + "?error=1" });

            }
            if (!isSave)
            {
                return RedirectToAction("/SubwayMapList", new { controller = "DesignManage", save = (result.Status == Status.Successful ? 1 : 0).ToString() + "?error=1" });

            }

            if (isSave)
            {
                try
                {
                    if (needChangPic == 1)
                    {
                        #region  背景图上传
                        HttpFileCollectionBase filecollection = Request.Files;
                        HttpPostedFileBase postedfile = filecollection[0];
                        byte[] file;
                        // 读取原始文件名
                        var localname = postedfile.FileName;
                        // 初始化byte长度.
                        file = new Byte[postedfile.ContentLength];
                        string ext = System.IO.Path.GetExtension(localname);

                        // 转换为byte类型
                        System.IO.Stream stream = postedfile.InputStream;
                        stream.Read(file, 0, postedfile.ContentLength);
                        stream.Close();
                        //当名称存在的时候不替换图片名称，只替换图片
                        string filename = string.Empty;
                        string attachdir = "/Content/SubwayMap/";
                        CreateSubWayMapFolder(Server.MapPath(attachdir));
                        if (curMapObj != null && !string.IsNullOrEmpty(curMapObj.Text("imagePath")))
                        {
                            filename = curMapObj.Text("imagePath");
                            filename = filename.Replace(" ", ""); //去掉content保存图片的文件名空格 
                        }
                        else
                        {
                            filename = System.IO.Path.GetFileName(localname);
                            filename = filename.Replace(" ", ""); //去掉content保存图片的文件名空格 
                            var hasExistImgPath = allSubWayMapList.Where(t => t.Text("imagePath").Trim() == imagePath.Trim()).Count() > 0;
                            if (hasExistImgPath)//新增图片重名。需要改名字
                            {
                                filename = string.Format("{0}{1}-{2}", attachdir, name, filename);
                            }
                            else
                            {
                                filename = string.Format("{0}{1}", attachdir, filename);
                            }
                        }
                        System.IO.FileStream fs = new System.IO.FileStream(Server.MapPath(filename), System.IO.FileMode.Create, System.IO.FileAccess.Write);
                        fs.Write(file, 0, file.Length);
                        fs.Flush();
                        fs.Close();
                        #endregion
                        imagePath = filename;
                    }
                }
                catch (System.IO.IOException ex)
                {

                }
                catch (Exception ex)
                {

                }

                if (curMapObj == null)
                {
                    curMapObj = new BsonDocument();
                    curMapObj.Add("remark", string.Format("<?xml version=\"1.0\" encoding=\"UTF-8\"?><rGraphModel><canvas bgImg=\"{0}\" bgColor=\"transparent\" width=\"800\" height=\"600\"/></rGraphModel>", imagePath));
                    curMapObj.Add("name", name);
                    curMapObj.Add("imagePath", imagePath);
                    curMapObj.Add("type", type);
                    
                    result = dataOp.Insert("XH_DesignManage_SubwayMap", curMapObj);
                }
                else
                {
                    var updateBson = new BsonDocument();
                    updateBson.Add("name", name);
                    updateBson.Add("type", type);
                    
                    if (!string.IsNullOrEmpty(imagePath) && needChangPic == 1)//上传图片不改变图片名称，只改变文件实体(上传新图片时，不改变文件名，只修改文件实体，同时清空Remark中的描点信息)
                    {
                        //updateBson.Add("remark", string.Format("<?xml version=\"1.0\" encoding=\"UTF-8\"?><rGraphModel><canvas bgImg=\"{0}\" bgColor=\"transparent\" width=\"800\" height=\"600\"/></rGraphModel>", imagePath));
                        updateBson.Add("imagePath", imagePath);
                    }
                    result = dataOp.Update(curMapObj, updateBson);
                }
            }
            return RedirectToAction("/SubwayMapList", new { controller = "DesignManage", save = result.Status == Status.Successful ? 1 : 0 });

        }

        /// <summary>
        /// 编辑保存地铁图对象
        /// </summary>
        /// <returns></returns>
        //[AcceptVerbs(HttpVerbs.Post)]
        //public JsonResult AddSubWayMapZHHY()
        //{
        //    var json = new PageJson();
        //    var mapId = PageReq.GetForm("mapId");
        //    var imagePath = PageReq.GetForm("imagePath");
        //    var name = PageReq.GetForm("name");
        //    var result = new InvokeResult() { Status = Status.Failed };
        //    var allSubWayMapList = dataOp.FindAll("XH_DesignManage_SubwayMap").ToList();
        //    var curMapObj = allSubWayMapList.Where(c => c.Text("mapId") == mapId).FirstOrDefault();
        //    var needChangPic = PageReq.GetFormInt("needChangPic");
        //    bool isSave = false;
        //    try
        //    {
        //        var hasExist = allSubWayMapList.Where(c => c.Text("mapId") != mapId && c.Text("name") == name).Count() > 0;
        //        isSave = !hasExist;
        //    }
        //    catch (System.Exception ex)
        //    {
        //        return Json(TypeConvert.InvokeResultToPageJson(result));

        //    }
        //    if (!isSave)
        //    {
        //        return Json(TypeConvert.InvokeResultToPageJson(result));

        //    }

        //    if (isSave)
        //    {
        //        try
        //        {
        //            if (needChangPic == 1)
        //            {
        //                #region  背景图上传
        //                HttpFileCollectionBase filecollection = Request.Files;
        //                HttpPostedFileBase postedfile = filecollection[0];
        //                byte[] file;
        //                // 读取原始文件名
        //                var localname = postedfile.FileName;
        //                // 初始化byte长度.
        //                file = new Byte[postedfile.ContentLength];
        //                string ext = System.IO.Path.GetExtension(localname);

        //                // 转换为byte类型
        //                System.IO.Stream stream = postedfile.InputStream;
        //                stream.Read(file, 0, postedfile.ContentLength);
        //                stream.Close();
        //                //当名称存在的时候不替换图片名称，只替换图片
        //                string filename = string.Empty;
        //                string attachdir = "/Content/SubwayMap/";
        //                CreateSubWayMapFolder(Server.MapPath(attachdir));
        //                if (curMapObj != null && !string.IsNullOrEmpty(curMapObj.Text("imagePath")))
        //                {
        //                    filename = curMapObj.Text("imagePath");
        //                }
        //                else
        //                {
        //                    filename = System.IO.Path.GetFileName(localname);

        //                    var hasExistImgPath = allSubWayMapList.Where(t => t.Text("imagePath").Trim() == imagePath.Trim()).Count() > 0;
        //                    if (hasExistImgPath)//新增图片重名。需要改名字
        //                    {
        //                        filename = string.Format("{0}{1}-{2}", attachdir, name, filename);
        //                    }
        //                    else
        //                    {
        //                        filename = string.Format("{0}{1}", attachdir, filename);
        //                    }
        //                }
        //                System.IO.FileStream fs = new System.IO.FileStream(Server.MapPath(filename), System.IO.FileMode.Create, System.IO.FileAccess.Write);
        //                fs.Write(file, 0, file.Length);
        //                fs.Flush();
        //                fs.Close();
        //                #endregion
        //                imagePath = filename;
        //            }
        //        }
        //        catch (System.IO.IOException ex)
        //        {

        //        }
        //        catch (Exception ex)
        //        {

        //        }

        //        if (curMapObj == null)
        //        {
        //            curMapObj = new BsonDocument();
        //            curMapObj.Add("remark", string.Format("<?xml version=\"1.0\" encoding=\"UTF-8\"?><rGraphModel><canvas bgImg=\"{0}\" bgColor=\"transparent\" width=\"800\" height=\"600\"/></rGraphModel>", imagePath));
        //            curMapObj.Add("name", name);
        //            curMapObj.Add("imagePath", imagePath);
        //            result = dataOp.Insert("XH_DesignManage_SubwayMap", curMapObj);
        //        }
        //        else
        //        {
        //            var updateBson = new BsonDocument();
        //            updateBson.Add("name", name);
        //            if (string.IsNullOrEmpty(curMapObj.Text("remark")))
        //            {
        //                curMapObj.Add("remark", string.Format("<?xml version=\"1.0\" encoding=\"UTF-8\"?><rGraphModel><canvas bgImg=\"{0}\" bgColor=\"transparent\" width=\"800\" height=\"600\"/></rGraphModel>", imagePath));
        //            }
        //            result = dataOp.Update(curMapObj, updateBson);
        //        }
        //    }
        //    return Json(TypeConvert.InvokeResultToPageJson(result));

        //}

        /// <summary>
        /// 删除地铁图
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult DeleteSubWayMap(int id)
        {
            var deleteQury = "db.XH_DesignManage_SubwayMap.findOne({'mapId':'" + id.ToString() + "'})";
            var result = dataOp.Delete("XH_DesignManage_SubwayMap", deleteQury);
            return Json(ConvertToPageJson(result));
        }

        void CreateSubWayMapFolder(string FolderPath)
        {
            if (!System.IO.Directory.Exists(FolderPath)) System.IO.Directory.CreateDirectory(FolderPath);
        }




        /// <summary>
        /// 获取脉络图列表
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public JsonResult GetSubwayMapList()
        {
            var subWayMapList = dataOp.FindAll("XH_DesignManage_SubwayMap");
            var MapList = subWayMapList.ToList();
            return this.Json(MapList);
        }

        /// <summary>
        /// 获取脉络图列表
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public JsonResult GetData()
        {

            var MapList = dataOp.FindAll("XH_DesignManage_SubwayMap").ToList();
            var finalResult = from c in MapList
                              select new { id = c.Int("mapId"), name = c.Text("name") };
            return this.Json(finalResult.ToList(), JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 获取脉络图背景
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public JsonResult GetBgImgList()
        {

            var MapList = dataOp.FindAll("XH_DesignManage_SubwayMap").ToList();
            var finalResult = from c in MapList
                              select new { id = c.Text("mapId"), name = c.Text("name"), url = c.Text("imagePath") };
            return this.Json(finalResult.ToList());
        }

        /// <summary>
        /// 获取脉络图详细xml
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public string GetEditDt(int id)
        {

            var mapObj = dataOp.FindOneByKeyVal("XH_DesignManage_SubwayMap", "mapId", id.ToString());
            if (mapObj != null)
            {
                return mapObj.Text("remark");
            }
            else
            {
                return @"<?xml version='1.0' encoding='UTF-8'?><rGraphModel></rGraphModel>";
            }
        }


        /// <summary>
        /// 获取脉络图信息
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public JsonResult GetSubwayMapDetail(int id)
        {
            var curMap = dataOp.FindOneByKeyVal("XH_DesignManage_SubwayMap", "mapId", id.ToString());
            List<object> taskListJson = new List<object>();
            taskListJson.Add(new { mapId = id, mapXml = curMap != null ? curMap.Text("remark") : string.Empty });
            return this.Json(taskListJson);

        }


        /// <summary>
        /// 保存地铁图对象
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult SaveSubwayMap()
        {
            var mapId = PageReq.GetForm("mapId");
            var name = PageReq.GetForm("name");
            var imagePath = PageReq.GetForm("imagePath");
            var orgId = PageReq.GetFormInt("orgId");
            var height = PageReq.GetForm("height");
            var width = PageReq.GetForm("width");
            var remark = PageReq.GetParam("remark");
            var curMap = dataOp.FindOneByKeyVal("XH_DesignManage_SubwayMap", "mapId", mapId);
            if (curMap == null)
            {
                curMap = new BsonDocument();
                curMap.Add("height", !string.IsNullOrEmpty(height) ? double.Parse(height) : 1241);
                curMap.Add("width", !string.IsNullOrEmpty(width) ? double.Parse(width) : 3087);
                curMap.Add("imagePath", imagePath);
                curMap.Add("name", name);
                curMap.Add("nodePid", 0);
                curMap.Add("remark", remark);

                var result = dataOp.Insert("XH_DesignManage_SubwayMap", curMap);
                return Json(ConvertToPageJson(result));
            }
            else
            {
                var updateBson = new BsonDocument();
                updateBson.Add("name", name);
                updateBson.Add("height", !string.IsNullOrEmpty(height) ? double.Parse(height) : 1241);
                updateBson.Add("width", !string.IsNullOrEmpty(width) ? double.Parse(width) : 3087);
                updateBson.Add("remark", remark);
                updateBson.Add("imagePath", imagePath);
                var result = dataOp.Update(curMap, updateBson);
                return Json(ConvertToPageJson(result));
            }

        }


        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult SaveSubwayMapDetail()
        {
            DecisionPointBll decisionPointBll = DecisionPointBll._();

            var result = new InvokeResult();
            var detailStr = Server.UrlDecode(PageReq.GetForm("mapXml"));
            var mapId = PageReq.GetForm("mapId");
            var mapName = PageReq.GetForm("mapName");
            var root = GetRooNode(detailStr);
            if (root == null) return Json(ConvertToPageJson(result));

            var updateBson = new BsonDocument();

            updateBson.Add("name", mapName);
            updateBson.Add("remark", detailStr);
            //获取地铁去信息
            var canvasNode = root.SelectSingleNode(@"/rGraphModel/canvas");
            if (canvasNode != null)
            {
                updateBson.Add("imagePath", GetAttribute(canvasNode, "bgImg"));
                updateBson.Add("width", GetAttribute(canvasNode, "width"));
                updateBson.Add("height", GetAttribute(canvasNode, "height"));
            }
            //获取
            var decisionPointList = new List<BsonDocument>();
            var pointNodes = root.SelectNodes(@"/rGraphModel/rText");
            if (pointNodes != null)
            {
                foreach (XmlNode textNode in pointNodes)
                {

                    var pointNode = textNode.SelectSingleNode(@"rCell/rTxt");
                    if (pointNode == null) continue;
                    #region 决策点
                    var decisionPoint = new BsonDocument();
                    if (HasValue(pointNode, "pointId"))
                    {
                        decisionPoint.Add("pointId", GetAttribute(pointNode, "pointId"));

                    }
                    if (HasValue(textNode, "id"))// 获取textId
                    {
                        decisionPoint.Add("textId", GetAttribute(textNode, "id"));
                    }

                    if (HasValue(textNode, "pid"))// 获取textId
                    {
                        decisionPoint.Add("textPId", GetAttribute(textNode, "pid"));
                    }
                    decisionPoint.Add("name", pointNode.InnerText.Replace(@"&#xa;", "").Replace(@"\n", ""));
                    #endregion

                    decisionPointList.Add(decisionPoint);

                }
            }
            result = decisionPointBll.PatchUpdate(mapId, updateBson, decisionPointList);
            return Json(ConvertToPageJson(result));
        }

        /// <summary>
        /// 辅助方法用于根据xml字符串 返回对应Xml根对象
        /// </summary>
        /// <param name="fileXMLStr"></param>
        /// <returns></returns>
        private XmlElement GetRooNode(string fileXMLStr)
        {
            try
            {
                if (fileXMLStr.Trim() == "") return null;
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(fileXMLStr.Trim());
                XmlElement rootEle = (XmlElement)doc.DocumentElement;
                return rootEle;
            }
            catch (XmlException XmlEx)
            {
                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        /// <summary>
        /// 返回对应属性值
        /// </summary>
        /// <param name="XN"></param>
        /// <param name="attr"></param>
        /// <returns></returns>
        private string GetAttribute(XmlNode XN, string attr)
        {
            if (XN == null) return "";

            if (XN.Attributes[attr] != null)
            {
                return XN.Attributes[attr].Value.ToString().Trim();
            }
            else
            {
                return "";
            }
        }

        /// <summary>
        /// 返回对应属性值
        /// </summary>
        /// <param name="XN"></param>
        /// <param name="attr"></param>
        /// <returns></returns>
        private int GetIntAttribute(XmlNode XN, string attr)
        {
            return int.Parse(GetAttribute(XN, attr));
        }

        /// <summary>
        /// 返回对应属性值
        /// </summary>
        /// <param name="XN"></param>
        /// <param name="attr"></param>
        /// <returns></returns>
        private double GetDoubleAttribute(XmlNode XN, string attr)
        {
            return double.Parse(GetAttribute(XN, attr));
        }
        /// <summary>
        /// 是否有值
        /// </summary>
        /// <param name="pointNode"></param>
        /// <param name="attr"></param>
        /// <returns></returns>
        private bool HasValue(XmlNode pointNode, string attr)
        {
            return pointNode.Attributes[attr] != null && !string.IsNullOrEmpty(pointNode.Attributes[attr].Value);
        }
        #endregion

        #region 项目文档案例推送保存接口
        /// <summary>
        /// 保存项目文档的按钮推送信息
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SavePushProjCaseInfo()
        {
            InvokeResult result = new InvokeResult();
            string caseDirId = PageReq.GetForm("ddlId");//案例库目录Id
            string fileId = PageReq.GetForm("fileId");//推送文件Id
            string retId = PageReq.GetForm("retId");//推送成果Id
            //string fileObjId = PageReq.GetForm("fileObjId");//文件对象
            string tableName = PageReq.GetForm("tableName");//推送文件库
            string keyName = PageReq.GetForm("keyName");//推送文件类型名称
            string keyValue = PageReq.GetForm("keyValue");//推送文件任务Id
            BsonDocument oldFileRelInfo = new BsonDocument();//推送文件关联信息
            BsonDocument newFileRelInfo = new BsonDocument();//推送到案例库保存文件关联信息
            BsonDocument newFileInfo = new BsonDocument();//推送到案例库案例成果下的文件信息
            BsonDocument caseItem = new BsonDocument();//按钮目录下默认的按钮成果
            oldFileRelInfo = dataOp.FindOneByQuery("FileRelation", Query.And(Query.EQ("fileId", fileId), Query.EQ("tableName", "XH_DesignManage_Task"), Query.EQ("keyName", "taskId")));
            //caseItem = dataOp.FindOneByQuery("XH_StandardResult_DiscreteDesignItem", Query.And(Query.EQ("classId", caseDirId), Query.EQ("resultType", "0")));
            //if (caseItem == null)
            //{
            //    //InvokeResult result1 = new InvokeResult();
            //    var ddlId = caseDirId;
            //    string tbName = "XH_StandardResult_DiscreteDesignItem";
            //    string queryStr = "";
            //    string dataStr = string.Format("itemId=0&classId={0}&resultType=0&name=案例成果&", ddlId);
            //    result = dataOp.Save(tbName, queryStr, dataStr);
            //    if (result.Status == Status.Failed)
            //    {
            //        result.Message = "该目录下找不到默认案例成果，推送失败！";
            //        return Json(TypeConvert.InvokeResultToPageJson(result));
            //    }
            //}
            //string itemId = "";
            //if (caseItem == null)
            //{
            //    result.BsonInfo.String("itemId");
            //}
            //else
            //{
            //    itemId = caseItem.String("itemId");
            //}
            // string itemId = caseItem.String("itemId");

            BsonDocument tempFileInfo = new BsonDocument();
            tempFileInfo = dataOp.FindOneByQuery("FileRelation", Query.And(Query.EQ("fileId", fileId), Query.EQ("tableName", "StandardResult_StandardResult"), Query.EQ("keyName", "retId"), Query.EQ("keyValue", retId), Query.EQ("fileObjId", "10")));
            if (tempFileInfo == null)
            {
                newFileRelInfo.Add("fileId", fileId);
                newFileRelInfo.Add("fileObjId", "10");
                newFileRelInfo.Add("tableName", "StandardResult_StandardResult");
                newFileRelInfo.Add("keyName", "retId");
                newFileRelInfo.Add("keyValue", retId);
                newFileRelInfo.Add("isPreDefine", "False");
                newFileRelInfo.Add("isCover", "False");
                newFileRelInfo.Add("version", "");
                newFileRelInfo.Add("uploadType", "0");
                newFileRelInfo.Add("fromTableName", "XH_DesignManage_Task");
                newFileRelInfo.Add("fromKeyName", "taskId");
                newFileRelInfo.Add("fromKeyValue", oldFileRelInfo.String("keyValue"));
                newFileRelInfo.Add("fromFileObjId", oldFileRelInfo.String("fileObjId"));
                // newFileRelInfo.Add("")
                //newFileRelInfo.Add("underTable", oldFileRelInfo.String("underTable"));
                result = dataOp.Insert("FileRelation", newFileRelInfo);
                if (result.Status == Status.Successful)
                {
                    result.Message = "案例推送成功";
                }
                else
                {
                    result.Message = "案例推送失败";
                }
            }
            else
            {
                result.Status = Status.Failed;
                result.Message = "该案例已推送过，不能重新推送";
            }

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }
        /// <summary>
        /// 批量推送案例
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult BatchSavePushProjCaseInfo()
        {
            InvokeResult result = new InvokeResult();
            string caseDirId = PageReq.GetForm("ddlId");//案例库目录Id
            string fileIdStr = PageReq.GetForm("fileId");//推送文件Id字符串
            string retId = PageReq.GetForm("retId");//推送成果Id
            var fileIdArry = fileIdStr.Split(',');
            string tableName = PageReq.GetForm("tableName");//推送文件库
            string keyName = PageReq.GetForm("keyName");//推送文件类型名称
            string keyValue = PageReq.GetForm("keyValue");//推送文件任务Id
            BsonDocument oldFileRelInfo = new BsonDocument();//推送文件关联信息
            BsonDocument newFileRelInfo = new BsonDocument();//推送到案例库保存文件关联信息
            BsonDocument newFileInfo = new BsonDocument();//推送到案例库案例成果下的文件信息
            BsonDocument caseItem = new BsonDocument();//按钮目录下默认的按钮成果

            if (fileIdArry.Length > 0)
            {
                using (TransactionScope tran = new TransactionScope())
                {
                    for (var i = 0; i < fileIdArry.Length; i++)
                    {
                        string fileId = fileIdArry[i];
                        oldFileRelInfo = dataOp.FindOneByQuery("FileRelation", Query.And(Query.EQ("fileId", fileId), Query.EQ("tableName", "XH_DesignManage_Task"), Query.EQ("keyName", "taskId")));
                        //caseItem = dataOp.FindOneByQuery("XH_StandardResult_DiscreteDesignItem", Query.And(Query.EQ("classId", caseDirId), Query.EQ("resultType", "0")));
                        //if (caseItem == null)
                        //{
                        //    //InvokeResult result1 = new InvokeResult();
                        //    var ddlId = caseDirId;
                        //    string tbName = "XH_StandardResult_DiscreteDesignItem";
                        //    string queryStr = "";
                        //    string dataStr = string.Format("itemId=0&classId={0}&resultType=0&name=案例成果&", ddlId);
                        //    result = dataOp.Save(tbName, queryStr, dataStr);
                        //    if (result.Status == Status.Failed)
                        //    {
                        //        result.Message = "该目录下找不到默认案例成果，推送失败！";
                        //        return Json(TypeConvert.InvokeResultToPageJson(result));
                        //    }
                        //}
                        //string itemId = "";
                        //if (caseItem == null)
                        //{
                        //    result.BsonInfo.String("itemId");
                        //}
                        //else 
                        //{
                        //    itemId = caseItem.String("itemId");
                        //}

                        BsonDocument tempFileInfo = new BsonDocument();
                        tempFileInfo = dataOp.FindOneByQuery("FileRelation", Query.And(Query.EQ("fileId", fileId), Query.EQ("tableName", "StandardResult_StandardResult"), Query.EQ("keyName", "retId"), Query.EQ("keyValue", retId), Query.EQ("fileObjId", "10")));
                        if (tempFileInfo == null)
                        {
                            newFileRelInfo = new BsonDocument();
                            newFileRelInfo.Add("fileId", fileId);
                            newFileRelInfo.Add("fileObjId", "10");
                            newFileRelInfo.Add("tableName", "StandardResult_StandardResult");
                            newFileRelInfo.Add("keyName", "retId");
                            newFileRelInfo.Add("keyValue", retId);
                            newFileRelInfo.Add("isPreDefine", "False");
                            newFileRelInfo.Add("isCover", "False");
                            newFileRelInfo.Add("version", "");
                            newFileRelInfo.Add("fromTableName", "XH_DesignManage_Task");
                            newFileRelInfo.Add("fromKeyName", "taskId");
                            newFileRelInfo.Add("fromKeyValue", oldFileRelInfo.String("keyValue"));
                            newFileRelInfo.Add("fromFileObjId", oldFileRelInfo.String("fileObjId"));
                            //newFileRelInfo.Add("underTable", oldFileRelInfo.String("underTable"));
                            result = dataOp.Insert("FileRelation", newFileRelInfo);
                            if (result.Status == Status.Successful)
                            {
                                result.Message = "案例推送成功";
                            }
                            else
                            {
                                result.Message = "案例推送失败";
                            }
                        }
                    }
                    tran.Complete();
                }
            }
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }
        #endregion

        #region 删除模块案例的附件
        /// <summary>
        /// 用于删除文件关联表的判断 若有推送则只删除关联 不删除文件表内容
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        public JsonResult DeleCaseFileRelations(FormCollection saveForm)
        {
            string fileRelIds = saveForm["delFileRelIds"] != null ? saveForm["delFileRelIds"] : "";
            string remainFileRelIds = fileRelIds;
            InvokeResult result = new InvokeResult();
            try
            {
                string[] fileArray;
                if (fileRelIds.Length > 0)
                {
                    fileArray = fileRelIds.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    if (fileArray.Length > 0)
                    {
                        foreach (var item in fileArray)
                        {
                            BsonDocument fileRelInfo = new BsonDocument();
                            fileRelInfo = dataOp.FindOneByKeyVal("FileRelation", "fileRelId", item);
                            if (fileRelInfo != null)
                            {
                                var fileId = fileRelInfo.String("fileId");
                                List<BsonDocument> hasRelList = new List<BsonDocument>();
                                hasRelList = dataOp.FindAllByQuery("FileRelation", Query.And(Query.EQ("fileId", fileId), Query.NE("fileRelId", item))).ToList();
                                //hasRelList = dataOp.FindAllByQueryStr("FileRelation", "fileId="+fileId+"&fileRelId!="+item).ToList();
                                if (hasRelList.Count() > 0)
                                {
                                    result = dataOp.Delete("FileRelation", Query.EQ("fileRelId", item));
                                    if (remainFileRelIds == item) { remainFileRelIds = ""; }
                                    else
                                    {
                                        remainFileRelIds = remainFileRelIds.Replace(item + ",", "");
                                    }
                                }
                                else
                                {
                                    result.Status = Status.Successful;
                                }
                            }
                            if (result.Status == Status.Failed)
                            {
                                break;
                            }
                        }
                    }
                }
                if (remainFileRelIds != "")
                {
                    result.FileInfo = remainFileRelIds;
                }
            }
            catch (Exception ex)
            {
                result.Status = Status.Failed;
                result.Message = ex.Message;
            }
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }
        #endregion

        #region   创建案例成果
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SaveReInfo()
        {
            InvokeResult result = new InvokeResult();
            string catId = PageReq.GetForm("catId");//目录Id
            string resName = PageReq.GetForm("resName");//成果名
            string tableName = PageReq.GetForm("tableName"); //表名
            string LibId = PageReq.GetForm("LibId");//库Id
            string queryStr = "";
            var typeId = dataOp.FindOneByQuery("StandardResult_StandardResultCategory", Query.EQ("catId", catId)).Text("typeId");
            string dataStr = string.Format("name={0}&typeId={1}&libId={2}&catId={3}", resName, typeId, LibId, catId);
            result = dataOp.Save(tableName, queryStr, dataStr);
            if (result.Status == Status.Failed)
            {
                result.Message = "创建成果失败！";
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            if (result.Status == Status.Successful)
            {
                result.Message = "创建成果成功！";
            }
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }
        #endregion

        #region 合生创展
        public JsonResult GetProjectJson()
        {
            int comId = PageReq.GetParamInt("comId");
            int projId = PageReq.GetParamInt("projId");
            List<PorjectStructure> list = new List<PorjectStructure>();
            var projList = dataOp.FindAll("XH_DesignManage_Project").ToList();

            if (projId > 0)
            {
                projList = projList.Where(t => t.Int("projId") == projId).ToList();
            }
            if (comId > 0)
            {
                List<string> engId = dataOp.FindAllByKeyVal("XH_DesignManage_Engineering", "comId", comId.ToString()).Select(s => s.String("engId")).ToList();
                projList = dataOp.FindAllByKeyValList("XH_DesignManage_Project", "engId", engId).ToList();

            }
            foreach (var item in projList.OrderByDescending(s => s.Date("createDate")).Take(3))
            {
                List<TaskStructure> plantList = new List<TaskStructure>();
                List<TaskStructure> facttList = new List<TaskStructure>();
                string managerId = item.Text("managerId");
                string[] userId = managerId.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                var userList = dataOp.FindAllByKeyValList("SysUser", "userId", userId.ToList());
                PorjectStructure proj = new PorjectStructure();
                var project = dataOp.FindOneByKeyVal("XH_DesignManage_Project", "projId", item.Text("projId"));
                var eng = dataOp.FindOneByKeyVal("XH_DesignManage_Engineering", "engId", project.Text("engId"));
                var plan = dataOp.FindOneByKeyVal("XH_DesignManage_Plan", "projId", project.Text("projId"));
                var taskList = dataOp.FindAllByKeyVal("XH_DesignManage_Task", "planId", plan.Text("planId")).Where(t => t.Int("nodeLevel") == 2).OrderBy(t => t.Text("nodeKey")).ToList();

                string engName = dataOp.FindOneByKeyVal("XH_DesignManage_Engineering", "engId", project.Text("engId")).String("name");
                proj.name = engName + "--" + item.Text("name");
                proj.projId = item.Int("projId");
                proj.manager = String.Join(",", userList.Select(t => t.Text("name")).ToArray());
                proj.phone = String.Join(",", userList.Select(t => t.Text("mobileNumber")).ToArray());
                proj.plantaskList = new List<TaskStructure>();
                proj.factaskList = new List<TaskStructure>();
                foreach (var task in taskList)
                {
                    TaskStructure ptak = new TaskStructure();
                    TaskStructure ftak = new TaskStructure();
                    ftak.taskId = ptak.taskId = task.Int("taskId");
                    ftak.name = ptak.name = task.Text("name");
                    ftak.status = ptak.status = task.Int("status");
                    ptak.startDate = task.Text("curStartData");
                    ptak.endDate = task.Text("curEndData");
                    ftak.startDate = task.Text("factStartDate");
                    ftak.endDate = task.Text("factEndDate");
                    proj.plantaskList.Add(ptak);
                    proj.factaskList.Add(ftak);
                }

                list.Add(proj);
            }
            return Json(list, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region 批量保存预定义交付文档 zpx
        /// <summary>
        /// 批量保存
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SaveDefineFileInfo(FormCollection saveForm)
        {
            InvokeResult result = new InvokeResult();
            string tbName = "PredefineFile";
            int taskId = PageReq.GetParamInt("taskId");
            int projId = PageReq.GetParamInt("projId");
            string queryStr = "";
            BsonDocument dataBson = new BsonDocument();
            int i = 0;
            foreach (var tempKey in saveForm.AllKeys)
            {
                if (!tempKey.Contains("fileList["))
                {
                    continue;
                }
                var curTempKey = tempKey.Split('.')[1];
                if (i < 6)
                {   //数据源，分为6个字段，name，ext，专业Id，阶段Id，属性Id，交付物组合Id
                    dataBson.Add(curTempKey, PageReq.GetForm(tempKey));

                }
                i++;
                if (i == 6)
                {
                    dataBson.Add("taskId", taskId);
                    dataBson.Add("projId", projId);
                    result = dataOp.Save(tbName, queryStr != "" ? TypeConvert.NativeQueryToQuery(queryStr) : Query.Null, dataBson);
                }
                if (i > 5)
                {
                    i = 0;
                    dataBson = new BsonDocument();

                }
                if (result.Status != Status.Successful)
                {
                    return Json(TypeConvert.InvokeResultToPageJson(result));
                }

            }


            return Json(TypeConvert.InvokeResultToPageJson(result));
        }
        #endregion

        #region 保存流程模版信息
        /// <summary>
        /// 保存流程模版信息
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public JsonResult SaveFlowInfo()
        {
            var flowId = PageReq.GetForm("flowId");
            var name = PageReq.GetForm("name");
            var flowTypeId = PageReq.GetForm("flowTypeId");
            var isActive = PageReq.GetForm("isActive");
            string attrIdStr = PageReq.GetForm("attrIds");

            var FlowDes = PageReq.GetForm("FlowDes");//意见填写说明
            var OperateDes = PageReq.GetForm("OperateDes");//流程使用说明
            var CommentTemplate = PageReq.GetForm("CommentTemplate");//评估意见范本
            var hasApprovalAmount = PageReq.GetForm("hasApprovalAmount");//是否在审批页面显示审批金额
            var remark = PageReq.GetForm("remark");//备注说明

            List<string> attrIds = attrIdStr.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();

            MongoOperation mongoOp = new MongoOperation();

            BsonDocument flow = new BsonDocument { { "name", name }, { "isActive", isActive }, { "flowTypeId", flowTypeId } };

            //zhtz增加任务关注等级
            if (SysAppConfig.CustomerCode == "73345DB5-DFE5-41F8-B37E-7D83335AZHTZ")
            {
                flow.Set("taskLevelId", PageReq.GetForm("taskLevelId"));
            }

            var allKeys = Request.Form.AllKeys;
            flow.Add("FlowDes", FlowDes, allKeys.Contains("FlowDes"));
            flow.Add("OperateDes", OperateDes, allKeys.Contains("OperateDes"));
            flow.Add("CommentTemplate", CommentTemplate, allKeys.Contains("CommentTemplate"));
            flow.Add("hasApprovalAmount", hasApprovalAmount, allKeys.Contains("hasApprovalAmount"));
            flow.Add("remark", remark, allKeys.Contains("remark"));

            object json = null;
            try
            {
                if (string.IsNullOrEmpty(flowId) || flowId == "0")//新增
                {
                    flowId = dataOp.Insert("BusFlow", flow).BsonInfo.String("flowId");

                }
                else//编辑
                {
                    dataOp.Update("BusFlow", Query.EQ("flowId", flowId), flow);//更新
                    InvokeResult result = mongoOp.Delete("ExtensionAttrRel", Query.EQ("flowId", flowId));//删除扩展属性
                }
                foreach (var attrId in attrIds)//新增扩展属性
                {
                    BsonDocument attr = new BsonDocument { { "attrId", attrId }, { "flowId", flowId } };
                    dataOp.Insert("ExtensionAttrRel", attr);
                }
                json = new { success = true, flowId = flowId };

            }
            catch (Exception ex)
            {
                json = new { success = false, msg = ex.Message };
            }

            return Json(json);
        }
        #endregion

        #region 设计变更及指令单保存

        #region 保存变更单
        /// <summary>
        /// 保存变更单
        /// </summary>
        /// <param name="saveform"></param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SaveDesignChangeInfo(FormCollection saveform)
        {
            InvokeResult result = new InvokeResult();
            string designChangeId = saveform["designChangeId"];
            string queryStr = PageReq.GetForm("queryStr");
            BsonDocument designChangeInfo = new BsonDocument();
            List<StorageData> saveList = new List<StorageData>();
            //designChangeInfo = dataOp.FindOneByKeyVal("DesignChange", "designChangeId", designChangeId);
            foreach (var tempKey in saveform.AllKeys)
            {
                if (tempKey == "queryStr" || tempKey == "changeTypeId") continue;

                designChangeInfo.Add(tempKey, PageReq.GetForm(tempKey));
            }

            #region 生成编号
            //    SJBG-GAZ（赣州，公司缩写取3个字母）-201401(年月)-002（本月第几个）
            if (string.IsNullOrWhiteSpace(queryStr) && SysAppConfig.CustomerCode == CustomerCode.ZHHY)
            {
                //获取当前用户所属公司
                var userCompany = new BsonDocument();
                var userOrgIdList = PageReq.GetObjSession("orgIdList") as List<int> ?? new List<int>();
                var userOrgList = dataOp.FindAllByQuery("Organization",
                        Query.In("orgId", userOrgIdList.Select(i => (BsonValue)(i.ToString())))
                    );
                if (userOrgList.Where(i => i.Int("isGroup") == 1).Count() > 0)
                {
                    userCompany = dataOp.FindOneByQuery("DesignManage_Company", Query.EQ("isGroup", "1"));
                }
                else
                {
                    userCompany = dataOp.FindOneByQuery("DesignManage_Company", Query.In("orgId", userOrgList.Select(i => i.GetValue("orgId", string.Empty))));
                }
                string comShortName = userCompany.Text("shortName");
                string year = DateTime.Now.Year.ToString();
                string month = DateTime.Now.Month.ToString();
                var curNum = dataOp.FindOneByQuery("DesignChangeNumber",
                        Query.And(
                            Query.EQ("comId", userCompany.Text("comId")),
                            Query.EQ("year", year),
                            Query.EQ("month", month)
                        )
                    );
                var curCount = 0;
                if (BsonDocumentExtension.IsNullOrEmpty(curNum))
                {
                    curCount = 1;
                    var newNumDoc = new BsonDocument(){
                            {"comId",userCompany.Text("comId")},
                            {"year",year},
                            {"month",month},
                            {"count","1"}
                        };
                    dataOp.Insert("DesignChangeNumber", newNumDoc);
                }
                else
                {
                    curCount = curNum.Int("count") + 1;
                    curNum.Set("count", curCount);
                    dataOp.Save("DesignChangeNumber", Query.EQ("numId", curNum.Text("numId")), curNum);
                }
                string numberStr = curCount.ToString();
                if (curCount < 1000)
                {
                    numberStr = numberStr.PadLeft(3, '0');
                }
                month = month.PadLeft(2, '0');
                var newDesignChangeNum = string.Format("{0}-{1}-{2}-{3}", "SJBG", userCompany.Text("shortName"), year + month, numberStr);
                designChangeInfo.Set("changeNum", newDesignChangeNum);
            }
            #endregion

            #region 保存数据
            result = dataOp.Save("DesignChange", queryStr != "" ? TypeConvert.NativeQueryToQuery(queryStr) : Query.Null, designChangeInfo);
            #endregion
            if (result.Status == Status.Successful)
            {

                designChangeId = result.BsonInfo.String("designChangeId");
                List<BsonDocument> oldTypeList = dataOp.FindAllByKeyVal("DesignChangeTypeRel", "designChangeId", result.BsonInfo.String("designChangeId")).ToList();
                string changeTypeRels = saveform["changeTypeId"] != null ? saveform["changeTypeId"] : "";
                string[] array = changeTypeRels.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var arr in array)
                {
                    BsonDocument tempType = oldTypeList.Where(x => x.String("designChangeId") == designChangeId && x.String("changeTypeId") == arr).FirstOrDefault();
                    if (tempType == null)
                    {
                        StorageData tempData = new StorageData();

                        tempData.Name = "DesignChangeTypeRel";
                        tempData.Document = new BsonDocument().Add("designChangeId", designChangeId)
                                                              .Add("changeTypeId", arr);
                        tempData.Type = StorageType.Insert;
                        saveList.Add(tempData);
                    }
                }
                foreach (var oldRel in oldTypeList)
                {
                    if (!array.Contains(string.Format("{0}", oldRel.String("changeTypeId"))))
                    {
                        StorageData tempData = new StorageData();

                        tempData.Name = "DesignChangeTypeRel";
                        tempData.Query = Query.And(Query.EQ("changeTypeId", oldRel.String("changeTypeId")), Query.EQ("designChangeId", oldRel.String("designChangeId")));
                        tempData.Type = StorageType.Delete;

                        saveList.Add(tempData);
                    }
                }
                BsonDocument comMand = dataOp.FindOneByKeyVal("DesignChangeCommand", "designChangeId", designChangeId);
                if (comMand == null)
                {
                    StorageData tempData = new StorageData();
                    tempData.Name = "DesignChangeCommand";
                    tempData.Document = new BsonDocument().Add("designChangeId", designChangeId)
                                                              .Add("projId", result.BsonInfo.String("projId"))
                                                              .Add("engName", result.BsonInfo.String("engName"));
                    tempData.Type = StorageType.Insert;
                    saveList.Add(tempData);
                } 



            }
            dataOp.BatchSaveStorageData(saveList);
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        #endregion

        #region 保存变更指令单
        /// <summary>
        /// 保存变更单
        /// </summary>
        /// <param name="saveform"></param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SaveCommandInfo(FormCollection saveform)
        {
            InvokeResult result = new InvokeResult();
            //string designChangeId = saveform["designChangeId"];
            string queryStr = PageReq.GetForm("queryStr");
            string changeCommandId = string.Empty;
            BsonDocument designChangeCommandInfo = new BsonDocument();
            List<StorageData> saveList = new List<StorageData>();
            //designChangeInfo = dataOp.FindOneByKeyVal("DesignChange", "designChangeId", designChangeId);
            foreach (var tempKey in saveform.AllKeys)
            {
                if (tempKey == "queryStr" || tempKey == "sendManNames" || tempKey == "signerName" || tempKey == "sendManIds") continue;

                designChangeCommandInfo.Add(tempKey, PageReq.GetForm(tempKey));
            }
            #region 保存数据
            result = dataOp.Save("DesignChangeCommand", queryStr != "" ? TypeConvert.NativeQueryToQuery(queryStr) : Query.Null, designChangeCommandInfo);
            #endregion
            if (result.Status == Status.Successful)
            {
                changeCommandId = result.BsonInfo.String("changeCommandId");
                List<BsonDocument> oldManList = dataOp.FindAllByKeyVal("CommandCopeMan", "changeCommandId", result.BsonInfo.String("changeCommandId")).ToList();
                string commandManRels = saveform["sendManIds"] != null ? saveform["sendManIds"] : "";
                string[] array = commandManRels.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var arr in array)
                {
                    BsonDocument tempMan = oldManList.Where(x => x.String("changeCommandId") == changeCommandId && x.String("sendManId") == arr).FirstOrDefault();
                    if (tempMan == null)
                    {
                        StorageData tempData = new StorageData();

                        tempData.Name = "CommandCopeMan";
                        tempData.Document = new BsonDocument().Add("changeCommandId", changeCommandId)
                                                              .Add("sendManId", arr);
                        tempData.Type = StorageType.Insert;
                        saveList.Add(tempData);
                    }
                }
                foreach (var oldRel in oldManList)
                {
                    if (!array.Contains(string.Format("{0}", oldRel.String("sendManId"))))
                    {
                        StorageData tempData = new StorageData();

                        tempData.Name = "CommandCopeMan";
                        tempData.Query = Query.And(Query.EQ("sendManId", oldRel.String("sendManId")), Query.EQ("changeCommandId", oldRel.String("changeCommandId")));
                        tempData.Type = StorageType.Delete;

                        saveList.Add(tempData);
                    }
                }
                //BsonDocument comMand = dataOp.FindOneByKeyVal("DesignChangeCommand", "designChangeId",designChangeId);
                //if (comMand == null) 
                //{
                //    StorageData tempData = new StorageData();
                //    tempData.Name = "DesignChangeCommand";
                //    tempData.Document = new BsonDocument().Add("designChangeId", designChangeId)
                //                                              .Add("projId", result.BsonInfo.String("projId"))
                //                                              .Add("engName", result.BsonInfo.String("engName"));
                //    tempData.Type = StorageType.Insert;
                //    saveList.Add(tempData);
                //}



            }
            dataOp.BatchSaveStorageData(saveList);
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        #endregion

        #region 保存变更单发起审批时间
        /// <summary>
        /// 保存变更单发起审批时间
        /// </summary>
        /// <param name="designChangeId">变更单ID</param>
        /// <returns></returns>
        public ActionResult SaveDesignChangeStartTime(int designChangeId)
        {
            InvokeResult result = new InvokeResult() { Status = Status.Successful };
            var designChange = dataOp.FindOneByQuery("DesignChange", Query.EQ("designChangeId", designChangeId.ToString()));
            if (BsonDocumentExtension.IsNullOrEmpty(designChange))
            {
                result.Status = Status.Failed;
                result.Message = "未找到该设计变更单";
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            var timeFormat = "yyyy-MM-dd HH:mm:ss";
            BsonDocument doc = new BsonDocument().Add("startTime", DateTime.Now.ToString(timeFormat));
            result = dataOp.Save("DesignChange", Query.EQ("designChangeId", designChange.Text("designChangeId")), doc);
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }
        #endregion

        #region 保存指令单阅读记录
        /// <summary>
        /// 保存变更指令单阅读记录
        /// </summary>
        /// <param name="saveform"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult DesignChange_AddCmdReadLog(FormCollection saveform)
        {
            string cmdTb = "DesignChangeCommand";
            
            var cmdId = PageReq.GetForm("commandId");//指令单id
            var userId = PageReq.GetForm("userId");//阅读者id
            var actionName = PageReq.GetForm("actionName");//操作名称
            var actionAvailable = PageReq.GetForm("actionAvailable");//操作是否有效
            var command = dataOp.FindOneByQuery(cmdTb, Query.EQ("changeCommandId", cmdId));
            if (command.IsNullOrEmpty())
            {
                var json = new PageJson();
                json.Success = false;
                json.Message = "未能找到变更指令单";
                return Json(json);
            }

            var newDoc = new BsonDocument(){
                {"changeCommandId",cmdId},
                {"userId",userId},
                {"actionName","read"},
                {"actionAvailable","0"}
            };
            var result = dataOp.Insert("DesignChangeCommandLog", newDoc);

            #region 判断所有的签收人是否都已经阅读，是则更改变更单状态为已阅
            //判断所有的签收人是否都已经阅读，是则更改变更单状态为已阅
            //获取所有的阅读记录
            var allCmdLogs = dataOp.FindAllByQuery("DesignChangeCommandLog",
                     Query.And(
                         Query.EQ("changeCommandId", cmdId)
                     )
                );
            var signInIds = command.Text("signInId").SplitToIntList(",");
            if (signInIds.Count > 0 && signInIds.All(i =>
                allCmdLogs.Where(u => u.Int("actionAvailable") != 1 && u.Int("userId") == i).Count() > 0
            ))
            {
                dataOp.Update(
                    cmdTb,
                    Query.EQ("changeCommandId", cmdId),
                    new BsonDocument().Add("status", ((int)DesignChangeCmdStatus.Seen).ToString())
                    );
            }
            #endregion
            
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }
        #endregion

        #region 签发指令单
        public ActionResult DesignChange_IssueCommand(FormCollection saveform)
        {
            #region 保存基本信息
            InvokeResult result = new InvokeResult();
            //string designChangeId = saveform["designChangeId"];
            string queryStr = PageReq.GetForm("queryStr");
            string changeCommandId = string.Empty;
            BsonDocument designChangeCommandInfo = new BsonDocument();
            List<StorageData> saveList = new List<StorageData>();
            //designChangeInfo = dataOp.FindOneByKeyVal("DesignChange", "designChangeId", designChangeId);
            foreach (var tempKey in saveform.AllKeys)
            {
                if (tempKey == "queryStr" || tempKey == "sendManNames" || tempKey == "signerName" || tempKey == "sendManIds") continue;

                designChangeCommandInfo.Add(tempKey, PageReq.GetForm(tempKey));
            }
            #region 保存数据
            result = dataOp.Save("DesignChangeCommand", queryStr != "" ? TypeConvert.NativeQueryToQuery(queryStr) : Query.Null, designChangeCommandInfo);
            #endregion
            if (result.Status == Status.Successful)
            {
                changeCommandId = result.BsonInfo.String("changeCommandId");
                List<BsonDocument> oldManList = dataOp.FindAllByKeyVal("CommandCopeMan", "changeCommandId", result.BsonInfo.String("changeCommandId")).ToList();
                string commandManRels = saveform["sendManIds"] != null ? saveform["sendManIds"] : "";
                string[] array = commandManRels.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var arr in array)
                {
                    BsonDocument tempMan = oldManList.Where(x => x.String("changeCommandId") == changeCommandId && x.String("sendManId") == arr).FirstOrDefault();
                    if (tempMan == null)
                    {
                        StorageData tempData = new StorageData();

                        tempData.Name = "CommandCopeMan";
                        tempData.Document = new BsonDocument().Add("changeCommandId", changeCommandId)
                                                              .Add("sendManId", arr);
                        tempData.Type = StorageType.Insert;
                        saveList.Add(tempData);
                    }
                }
                foreach (var oldRel in oldManList)
                {
                    if (!array.Contains(string.Format("{0}", oldRel.String("sendManId"))))
                    {
                        StorageData tempData = new StorageData();

                        tempData.Name = "CommandCopeMan";
                        tempData.Query = Query.And(Query.EQ("sendManId", oldRel.String("sendManId")), Query.EQ("changeCommandId", oldRel.String("changeCommandId")));
                        tempData.Type = StorageType.Delete;

                        saveList.Add(tempData);
                    }
                }
            }
            dataOp.BatchSaveStorageData(saveList);
            #endregion

            var command=result.BsonInfo;
            string actionName = string.Empty;
            int nextStatus = 0;
            List<string> hitActions = new List<string>() { "read", "issue" };
            switch (command.Int("status"))
            {
                case (int)DesignChangeCmdStatus.NotLaunch:
                    actionName = "launch";
                    nextStatus = (int)DesignChangeCmdStatus.NotIssue;//未发起时改为未签发
                    hitActions = new List<string>() { "launch", "issue", "read", "reportRead" };
                    break;
                case (int)DesignChangeCmdStatus.NotIssue:
                    actionName = "issue";
                    nextStatus = (int)DesignChangeCmdStatus.NotSee;//未签发时改为未阅
                    hitActions = new List<string>() { "issue", "read", "reportRead" };
                    break;
                default:
                    actionName = "launch";
                    nextStatus = (int)DesignChangeCmdStatus.NotIssue;
                    break;
            }
            #region 将所有阅读记录设置为无效
            var logQuery = Query.And(
                        Query.EQ("changeCommandId", changeCommandId),
                        Query.In("actionName", hitActions.Select(i=>(BsonValue)i)),
                        Query.EQ("actionAvailable", "0")
                    );
            var logUpdateBson = new BsonDocument(){
                {"actionAvailable","1"}
            };
            dataOp.Update("DesignChangeCommandLog", logQuery, logUpdateBson);
            #endregion

            #region 添加签发操作记录
            
            var issueLogBson = new BsonDocument(){
                {"changeCommandId",changeCommandId},
                {"userId",dataOp.GetCurrentUserId().ToString()},
                {"actionName",actionName},
                {"actionAvailable","0"}
            };
            dataOp.Insert("DesignChangeCommandLog", issueLogBson);
            #endregion

            #region 更新指令单状态为未签发或未阅
            dataOp.Update(
                    "DesignChangeCommand",
                    Query.EQ("changeCommandId", changeCommandId),
                    new BsonDocument().Add("status", nextStatus.ToString())
                    );
            #endregion

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }
        #endregion

        #endregion

        #region ZHTZ费用支付汇总导出excel
        public void ExportContractPayment()
        {
            var comId = PageReq.GetParam("comId");
            var projIds = PageReq.GetParam("projIds");

            var comObj = dataOp.FindOneByQuery("DesignManage_Company", Query.EQ("comId", comId));
            var allEng = dataOp.FindAllByQuery("XH_DesignManage_Engineering", Query.EQ("comId", comId));
            var allProj = dataOp.FindAllByQuery("XH_DesignManage_Project", Query.And(Query.EQ("isProj", "1"), Query.In("engId", allEng.Select(p => (BsonValue)p.Text("engId")))));
            if (!string.IsNullOrEmpty(projIds))
            {
                var projIdList = projIds.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                allProj = dataOp.FindAllByQuery("XH_DesignManage_Project", Query.And(Query.EQ("isProj", "1"), Query.In("projId", projIdList.Select(p => (BsonValue)p))));
            }
            var resultList = new List<BsonDocument>();
            #region 构造数据
            foreach (var proj in allProj)
            {
                var allPaymentPlan = dataOp.FindAllByQuery("XH_DesignManage_Plan", Query.And(Query.EQ("isContractPlan", "1"), Query.NE("isExpTemplate", "1"), Query.EQ("projId", proj.Text("projId"))));
                var allContractNode = dataOp.FindAllByQuery("XH_DesignManage_Task", Query.And(Query.In("planId", allPaymentPlan.Select(p => (BsonValue)p.Text("planId"))), Query.EQ("nodeTypeId", "2")));
                foreach (var contract in allContractNode)
                {

                    BsonDocument contractDoc = new BsonDocument();
                    contractDoc.Add("projId", proj.Text("projId"));
                    contractDoc.Add("projName", proj.Text("name"));
                    contractDoc.Add("contractId", contract.Text("taskId"));
                    contractDoc.Add("contractName", string.IsNullOrEmpty(contract.Text("paymentDisplayName")) ? contract.Text("name") : contract.Text("paymentDisplayName"));
                    contractDoc.Add("nodeName", "合同总额");
                    contractDoc.Add("designUnitName", contract.Text("designUnitName"));
                    contractDoc.Add("contractSignDate", contract.Text("contractSignDate"));
                    contractDoc.Add("designServiceContent", contract.Text("designServiceContent"));
                    contractDoc.Add("areaUnitPrice", contract.Decimal("areaUnitPrice") + "元/平方米");
                    var contractExpectToPay = 0m;//合同总计应付金额
                    var contractActualToPay = 0m;

                    var allPayment = dataOp.FindAllByQuery("XH_DesignManage_Task", Query.EQ("nodePid", contract.Text("taskId")));
                    List<BsonDocument> paymentDocList = new List<BsonDocument>();
                    foreach (var pay in allPayment)
                    {
                        BsonDocument paymentDoc = new BsonDocument();
                        paymentDoc.Add("projId", proj.Text("projId"));
                        paymentDoc.Add("projName", string.Empty);
                        paymentDoc.Add("contractId", contract.Text("taskId"));
                        paymentDoc.Add("contractName", string.IsNullOrEmpty(contract.Text("paymentDisplayName")) ? contract.Text("name") : contract.Text("paymentDisplayName"));
                        if (string.IsNullOrEmpty(pay.Text("paymentDisplayName")))
                        {
                            paymentDoc.Add("nodeName", "        " + pay.Text("name"));
                        }
                        else
                        {
                            paymentDoc.Add("nodeName", "        " + pay.Text("paymentDisplayName"));
                        }
                        paymentDoc.Add("designUnitName", string.Empty);
                        paymentDoc.Add("contractSignDate", string.Empty);
                        paymentDoc.Add("designServiceContent", string.Empty);
                        paymentDoc.Add("areaUnitPrice", string.Empty);

                        var eachPayList = dataOp.FindAllByQuery("XH_DesignManage_Task", Query.EQ("nodePid", pay.Text("taskId")));
                        var eachPayIndex = 1;
                        var paymentExpectToPay = 0m;//费用支付节点总计应付总额
                        var paymentActualToPay = 0m;
                        List<BsonDocument> eachpayDocList = new List<BsonDocument>();
                        foreach (var eachPay in eachPayList)
                        {
                            paymentExpectToPay += eachPay.Decimal("expectPaymentAmount");
                            paymentActualToPay += eachPay.Decimal("actualPaymentAmount");
                            BsonDocument eachpayDoc = new BsonDocument();
                            eachpayDoc.Add("projId", proj.Text("projId"));
                            eachpayDoc.Add("projName", string.Empty);
                            eachpayDoc.Add("contractId", contract.Text("taskId"));
                            eachpayDoc.Add("contractName", string.IsNullOrEmpty(contract.Text("paymentDisplayName")) ? contract.Text("name") : contract.Text("paymentDisplayName"));
                            eachpayDoc.Add("nodeName", string.Format("                第{0}笔支付", eachPayIndex++));
                            eachpayDoc.Add("designUnitName", string.Empty);
                            eachpayDoc.Add("contractSignDate", string.Empty);
                            eachpayDoc.Add("designServiceContent", string.Empty);
                            eachpayDoc.Add("areaUnitPrice", string.Empty);
                            eachpayDoc.Add("expectPaymentAmount", eachPay.Decimal("expectPaymentAmount") + "万");//每笔支付应付金额
                            eachpayDoc.Add("actualPaymentAmount", eachPay.Decimal("actualPaymentAmount") + "万");//每笔已付金额
                            eachpayDoc.Add("paymentDate", eachPay.ShortDate("paymentDate"));
                            var notPaidAmount = eachPay.Decimal("expectPaymentAmount") - eachPay.Decimal("actualPaymentAmount");
                            eachpayDoc.Add("notPaidAmount", notPaidAmount.ToString() + "万");
                            eachpayDoc.Add("remark", eachPay.Text("remark"));//付款理由
                            eachpayDoc.Add("preTaskApprovalStatus", string.Empty);//前置节点审批状态(每笔支付没有前置节点)
                            #region 当前节点审批状态
                            string curTaskApprovalStatus = "未关联流程";
                            var curTaskFlow = dataOp.FindOneByQuery("XH_DesignManage_TaskBusFlow", Query.EQ("taskId", eachPay.Text("taskId")));
                            if (curTaskFlow != null)
                            {
                                curTaskApprovalStatus = "未开始";
                            }
                            var instanceQuery = Query.And(Query.EQ("tableName", "XH_DesignManage_Task"), Query.EQ("referFieldName", "taskId"), Query.EQ("referFieldValue", eachPay.Text("taskId")));
                            var curTaskFlowInstance = dataOp.FindOneByQuery("BusFlowInstance", instanceQuery);
                            if (curTaskFlowInstance != null)
                            {
                                if (curTaskFlowInstance.Int("instanceStatus") == 0)
                                {
                                    curTaskApprovalStatus = "进行中";
                                }
                                else
                                {
                                    curTaskApprovalStatus = "已完成";
                                }
                            }
                            eachpayDoc.Add("curTaskApprovalStatus", curTaskApprovalStatus);
                            #endregion
                            eachpayDocList.Add(eachpayDoc);
                        }
                        paymentDoc.Add("expectPaymentAmount", paymentExpectToPay.ToString() + "万");
                        contractExpectToPay += paymentExpectToPay;
                        paymentDoc.Add("actualPaymentAmount", paymentActualToPay.ToString() + "万");
                        contractActualToPay += paymentActualToPay;
                        paymentDoc.Add("paymentDate", string.Empty);
                        paymentDoc.Add("notPaidAmount", (paymentExpectToPay - paymentActualToPay).ToString() + "万");
                        paymentDoc.Add("remark", string.Empty);

                        #region 前置节点审批状态
                        string preTaskStatus = string.Empty;
                        var preTaskQuery = Query.And(Query.EQ("sucTaskId", pay.Text("taskId")), Query.EQ("referType", "1"));
                        var preTaskRelList = dataOp.FindAllByQuery("XH_DesignManage_TaskRelation", preTaskQuery);
                        foreach (var preTaskRel in preTaskRelList)
                        {
                            var preTask = dataOp.FindOneByQuery("XH_DesignManage_Task", Query.EQ("taskId", preTaskRel.Text("preTaskId")));
                            if (preTask == null) continue;
                            var preTaskApprovalStatus = "未关联流程";
                            var preTaskFlow = dataOp.FindOneByQuery("XH_DesignManage_TaskBusFlow", Query.EQ("taskId", preTask.Text("taskId")));
                            if (preTaskFlow != null)
                            {
                                if (dataOp.FindOneByQuery("BusFlow", Query.EQ("flowId", preTaskFlow.Text("flowId"))) != null)
                                {
                                    preTaskApprovalStatus = "未开始";
                                }
                            }
                            var instanceQuery = Query.And(Query.EQ("tableName", "XH_DesignManage_Task"), Query.EQ("referFieldName", "taskId"), Query.EQ("referFieldValue", preTask.Text("taskId")));
                            var preTaskFlowInstance = dataOp.FindOneByQuery("BusFlowInstance", instanceQuery);
                            if (preTaskFlowInstance != null)
                            {
                                if (preTaskFlowInstance.Int("instanceStatus") == 0)
                                {
                                    preTaskApprovalStatus = "进行中";
                                }
                                else
                                {
                                    preTaskApprovalStatus = "已完成";
                                }
                            }
                            preTaskStatus += string.Format("{0}({1});", preTask.Text("name"), preTaskApprovalStatus);
                        }
                        paymentDoc.Add("preTaskApprovalStatus", preTaskStatus);//前置节点审批状态
                        #endregion

                        #region 当前节点审批状态
                        var curTaskApprovalStatus2 = "未关联流程";
                        var curTaskFlow2 = dataOp.FindOneByQuery("XH_DesignManage_TaskBusFlow", Query.EQ("taskId", pay.Text("taskId")));
                        if (curTaskFlow2 != null)
                        {
                            curTaskApprovalStatus2 = "未开始";
                        }
                        var instanceQuery2 = Query.And(Query.EQ("tableName", "XH_DesignManage_Task"), Query.EQ("referFieldName", "taskId"), Query.EQ("referFieldValue", pay.Text("taskId")));
                        var curTaskFlowInstance2 = dataOp.FindOneByQuery("BusFlowInstance", instanceQuery2);
                        if (curTaskFlowInstance2 != null)
                        {
                            if (curTaskFlowInstance2.Int("instanceStatus") == 0)
                            {
                                curTaskApprovalStatus2 = "进行中";
                            }
                            else
                            {
                                curTaskApprovalStatus2 = "已完成";
                            }
                        }
                        paymentDoc.Add("curTaskApprovalStatus", curTaskApprovalStatus2);
                        #endregion
                        paymentDocList.Add(paymentDoc);
                        paymentDocList.AddRange(eachpayDocList);
                    }
                    contractDoc.Add("expectPaymentAmount", contractExpectToPay.ToString() + "万");
                    contractDoc.Add("actualPaymentAmount", contractActualToPay.ToString() + "万");
                    contractDoc.Add("paymentDate", string.Empty);
                    contractDoc.Add("notPaidAmount", (contractExpectToPay - contractActualToPay).ToString() + "万");
                    contractDoc.Add("remark", string.Empty);

                    #region 前置节点审批状态
                    var preTaskApprovalStatus3 = string.Empty;
                    var preTaskQuery3 = Query.And(Query.EQ("sucTaskId", contract.Text("taskId")), Query.EQ("referType", "1"));
                    var preTaskRelList3 = dataOp.FindAllByQuery("XH_DesignManage_TaskRelation", preTaskQuery3);
                    foreach (var preTaskRel in preTaskRelList3)
                    {
                        var preTask = dataOp.FindOneByQuery("XH_DesignManage_Task", Query.EQ("taskId", preTaskRel.Text("preTaskId")));
                        if (preTask == null) continue;
                        var status = "未关联流程";
                        var preTaskFlow = dataOp.FindOneByQuery("XH_DesignManage_TaskBusFlow", Query.EQ("taskId", preTask.Text("taskId")));
                        if (preTaskFlow != null)
                        {
                            status = "未开始";
                        }
                        var instanceQuery = Query.And(Query.EQ("tableName", "XH_DesignManage_Task"), Query.EQ("referFieldName", "taskId"), Query.EQ("referFieldValue", preTask.Text("taskId")));
                        var preTaskFlowInstance = dataOp.FindOneByQuery("BusFlowInstance", instanceQuery);
                        if (preTaskFlowInstance != null)
                        {
                            if (preTaskFlowInstance.Int("instanceStatus") == 0)
                            {
                                status = "进行中";
                            }
                            else
                            {
                                status = "已完成";
                            }
                        }
                        preTaskApprovalStatus3 += string.Format("{0}({1});", preTask.Text("name"), status);

                    }
                    contractDoc.Add("preTaskApprovalStatus", preTaskApprovalStatus3);//前置节点审批状态
                    #endregion

                    #region 当前节点审批状态
                    string curTaskApprovalStatus3 = "未关联流程";
                    var curTaskFlow3 = dataOp.FindOneByQuery("XH_DesignManage_TaskBusFlow", Query.EQ("taskId", contract.Text("taskId")));
                    if (curTaskFlow3 != null)
                    {
                        curTaskApprovalStatus3 = "未开始";
                    }
                    var instanceQuery3 = Query.And(Query.EQ("tableName", "XH_DesignManage_Task"), Query.EQ("referFieldName", "taskId"), Query.EQ("referFieldValue", contract.Text("taskId")));
                    var curTaskFlowInstance3 = dataOp.FindOneByQuery("BusFlowInstance", instanceQuery3);
                    if (curTaskFlowInstance3 != null)
                    {
                        if (curTaskFlowInstance3.Int("instanceStatus") == 0)
                        {
                            curTaskApprovalStatus3 = "进行中";
                        }
                        else
                        {
                            curTaskApprovalStatus3 = "已完成";
                        }
                    }
                    contractDoc.Add("curTaskApprovalStatus", curTaskApprovalStatus3);
                    #endregion

                    resultList.Add(contractDoc);
                    resultList.AddRange(paymentDocList);
                }
            }
            #endregion
            XlsDocument xlsDoc = new XlsDocument();
            Worksheet sheet1 = xlsDoc.Workbook.Worksheets.Add("sheet1");

            Cells cells = sheet1.Cells;
            #region 表格样式

            XF titleXF = xlsDoc.NewXF();
            titleXF.Font.Bold = true;
            titleXF.UseBorder = true;
            titleXF.TopLineStyle = 1;
            titleXF.TopLineColor = Colors.Black;
            titleXF.RightLineStyle = 1;
            titleXF.RightLineColor = Colors.Black;
            titleXF.BottomLineStyle = 1;
            titleXF.BottomLineColor = Colors.Black;
            titleXF.LeftLineStyle = 1;
            titleXF.LeftLineColor = Colors.Black;

            XF topTitleXF = xlsDoc.NewXF();
            topTitleXF.Font.Bold = true;
            topTitleXF.UseBorder = true;
            topTitleXF.TopLineStyle = 1;
            topTitleXF.TopLineColor = Colors.Black;
            topTitleXF.RightLineStyle = 1;
            topTitleXF.RightLineColor = Colors.Black;
            topTitleXF.BottomLineStyle = 1;
            topTitleXF.BottomLineColor = Colors.Black;
            topTitleXF.LeftLineStyle = 1;
            topTitleXF.LeftLineColor = Colors.Black;
            topTitleXF.HorizontalAlignment = HorizontalAlignments.Centered;

            XF dataXF = xlsDoc.NewXF();
            dataXF.UseBorder = true;
            dataXF.TopLineStyle = 1;
            dataXF.TopLineColor = Colors.Black;
            dataXF.RightLineStyle = 1;
            dataXF.RightLineColor = Colors.Black;
            dataXF.BottomLineStyle = 1;
            dataXF.BottomLineColor = Colors.Black;
            dataXF.LeftLineStyle = 1;
            dataXF.LeftLineColor = Colors.Black;
            dataXF.VerticalAlignment = VerticalAlignments.Centered;

            ColumnInfo projNameColInfo = new ColumnInfo(xlsDoc, sheet1);
            projNameColInfo.ColumnIndexStart = 0;
            projNameColInfo.ColumnIndexEnd = 0;
            projNameColInfo.Width = 20 * 256;
            ColumnInfo contractNameColInfo = new ColumnInfo(xlsDoc, sheet1);
            contractNameColInfo.ColumnIndexStart = 1;
            contractNameColInfo.ColumnIndexEnd = 1;
            contractNameColInfo.Width = 40 * 256;
            ColumnInfo designContentColInfo = new ColumnInfo(xlsDoc, sheet1);
            designContentColInfo.ColumnIndexStart = 4;
            designContentColInfo.ColumnIndexEnd = 4;
            designContentColInfo.Width = 15 * 256;
            ColumnInfo unitPriceColInfo = new ColumnInfo(xlsDoc, sheet1);
            unitPriceColInfo.ColumnIndexStart = 5;
            unitPriceColInfo.ColumnIndexEnd = 5;
            unitPriceColInfo.Width = 15 * 256;
            ColumnInfo nodeNameColInfo = new ColumnInfo(xlsDoc, sheet1);
            nodeNameColInfo.ColumnIndexStart = 6;
            nodeNameColInfo.ColumnIndexEnd = 6;
            nodeNameColInfo.Width = 40 * 256;
            ColumnInfo preTaskColInfo = new ColumnInfo(xlsDoc, sheet1);
            preTaskColInfo.ColumnIndexStart = 12;
            preTaskColInfo.ColumnIndexEnd = 12;
            preTaskColInfo.Width = 30 * 256;
            ColumnInfo curTaskColInfo = new ColumnInfo(xlsDoc, sheet1);
            curTaskColInfo.ColumnIndexStart = 13;
            curTaskColInfo.ColumnIndexEnd = 13;
            curTaskColInfo.Width = 20 * 256;

            sheet1.AddColumnInfo(projNameColInfo);
            sheet1.AddColumnInfo(contractNameColInfo);
            sheet1.AddColumnInfo(designContentColInfo);
            sheet1.AddColumnInfo(unitPriceColInfo);
            sheet1.AddColumnInfo(nodeNameColInfo);
            sheet1.AddColumnInfo(preTaskColInfo);
            sheet1.AddColumnInfo(curTaskColInfo);
            #endregion
            int rowIndex = 1;
            int colIndex = 1;
            MergeArea merge = new MergeArea(rowIndex, rowIndex, 2, 6);
            sheet1.AddMergeArea(merge);
            cells.Add(rowIndex, 2, "合同信息", topTitleXF);
            cells.Add(rowIndex, 7, string.Empty, topTitleXF);
            MergeArea merge2 = new MergeArea(rowIndex, rowIndex, 8, 12);
            cells.Add(rowIndex, 8, "支付信息", topTitleXF);
            sheet1.AddMergeArea(merge2);

            MergeArea merge3 = new MergeArea(rowIndex, rowIndex, 13, 14);
            cells.Add(rowIndex, 13, "审批状态", topTitleXF);
            cells.Add(rowIndex, 14, string.Empty, topTitleXF);
            sheet1.AddMergeArea(merge3);

            rowIndex = 2;
            colIndex = 1;
            cells.Add(rowIndex, colIndex++, "项目名称", titleXF);
            cells.Add(rowIndex, colIndex++, "合同名称", titleXF);
            cells.Add(rowIndex, colIndex++, "设计单位", titleXF);
            cells.Add(rowIndex, colIndex++, "签订时间", titleXF);
            cells.Add(rowIndex, colIndex++, "设计范围", titleXF);
            cells.Add(rowIndex, colIndex++, "设计单价", titleXF);
            cells.Add(rowIndex, colIndex++, "支付明细", titleXF);
            cells.Add(rowIndex, colIndex++, "应付金额", titleXF);
            cells.Add(rowIndex, colIndex++, "已付金额", titleXF);
            cells.Add(rowIndex, colIndex++, "支付时间", titleXF);
            cells.Add(rowIndex, colIndex++, "未付金额", titleXF);
            cells.Add(rowIndex, colIndex++, "支付理由", titleXF);
            cells.Add(rowIndex, colIndex++, "前置节点审批状态", titleXF);
            cells.Add(rowIndex, colIndex++, "当前节点审批状态", titleXF);
            rowIndex = 3;
            var projRowIndex = 3;
            var contractRowIndex = 3;
            var curProjId = 0;
            var curContractId = 0;
            var projCount = 0;
            var contractCount = 0;
            foreach (var item in resultList)
            {
                if (curProjId != item.Int("projId"))
                {
                    curProjId = item.Int("projId");
                    var curProjObj = dataOp.FindOneByQuery("XH_DesignManage_Project", Query.EQ("projId", curProjId.ToString()));
                    var curEngObj = dataOp.FindOneByQuery("XH_DesignManage_Engineering", Query.EQ("engId", curProjObj.Text("engId")));
                    projCount = resultList.Where(p => p.Int("projId") == curProjId).Count();
                    MergeArea mergeProj = new MergeArea(projRowIndex, projRowIndex + projCount - 1, 1, 1);
                    for (int i = projRowIndex; i < projRowIndex + projCount; i++)
                    {
                        cells.Add(i, 1, curEngObj.Text("name") + " —— " + item.Text("projName"), dataXF);
                    }
                    sheet1.AddMergeArea(mergeProj);
                    projRowIndex += projCount;
                }
                if (curContractId != item.Int("contractId"))
                {
                    curContractId = item.Int("contractId");
                    contractCount = resultList.Where(p => p.Int("contractId") == curContractId).Count();
                    MergeArea mergeContract = new MergeArea(contractRowIndex, contractRowIndex + contractCount - 1, 2, 2);
                    MergeArea merge4 = new MergeArea(contractRowIndex, contractRowIndex + contractCount - 1, 3, 3);
                    MergeArea merge5 = new MergeArea(contractRowIndex, contractRowIndex + contractCount - 1, 4, 4);
                    MergeArea merge6 = new MergeArea(contractRowIndex, contractRowIndex + contractCount - 1, 5, 5);
                    MergeArea merge7 = new MergeArea(contractRowIndex, contractRowIndex + contractCount - 1, 6, 6);
                    for (int i = contractRowIndex; i < contractRowIndex + contractCount; i++)
                    {

                        cells.Add(i, 2, item.Text("contractName"), dataXF);
                        cells.Add(i, 3, item.Text("designUnitName"), dataXF);
                        cells.Add(i, 4, item.Text("contractSignDate"), dataXF);
                        cells.Add(i, 5, item.Text("designServiceContent"), dataXF);
                        cells.Add(i, 6, item.Text("areaUnitPrice"), dataXF);
                    }
                    sheet1.AddMergeArea(mergeContract);
                    sheet1.AddMergeArea(merge4);
                    sheet1.AddMergeArea(merge5);
                    sheet1.AddMergeArea(merge6);
                    sheet1.AddMergeArea(merge7);
                    contractRowIndex += contractCount;
                }
                colIndex = 7;
                cells.Add(rowIndex, colIndex++, item.Text("nodeName"), dataXF);
                cells.Add(rowIndex, colIndex++, item.Text("expectPaymentAmount"), dataXF);
                cells.Add(rowIndex, colIndex++, item.Text("actualPaymentAmount"), dataXF);
                cells.Add(rowIndex, colIndex++, item.Text("paymentDate"), dataXF);
                cells.Add(rowIndex, colIndex++, item.Text("notPaidAmount"), dataXF);
                cells.Add(rowIndex, colIndex++, item.Text("remark"), dataXF);
                cells.Add(rowIndex, colIndex++, item.Text("preTaskApprovalStatus"), dataXF);
                cells.Add(rowIndex, colIndex++, item.Text("curTaskApprovalStatus"), dataXF);
                rowIndex++;
            }
            string fileName = string.Format("{0}合同台账", comObj.Text("name"));
            MyXlsUtility.ExportByWeb(xlsDoc, fileName);
        }

        #endregion

        #region 导出项目月计划表
        public void ExportProjTaskMonth()
        {

            XlsDocument xlsDoc = new XlsDocument();
            Worksheet sheet = xlsDoc.Workbook.Worksheets.Add("sheet1");
            #region 表格样式
            XF dataXF = xlsDoc.NewXF();
            dataXF.VerticalAlignment = VerticalAlignments.Centered;

            XF titleXF = xlsDoc.NewXF();
            titleXF.Font.Bold = true;
            ColumnInfo dateColInfo1 = new ColumnInfo(xlsDoc, sheet);
            dateColInfo1.ColumnIndexStart = 4;
            dateColInfo1.ColumnIndexEnd = 6;
            dateColInfo1.Width = 15 * 256;
            ColumnInfo dateColInfo2 = new ColumnInfo(xlsDoc, sheet);
            dateColInfo2.ColumnIndexStart = 8;
            dateColInfo2.ColumnIndexEnd = 10;
            dateColInfo2.Width = 15 * 256;
            ColumnInfo projNameColInfo = new ColumnInfo(xlsDoc, sheet);
            projNameColInfo.ColumnIndexStart = 1;
            projNameColInfo.ColumnIndexEnd = 1;
            projNameColInfo.Width = 20 * 256;
            ColumnInfo taskNameColInfo = new ColumnInfo(xlsDoc, sheet);
            taskNameColInfo.ColumnIndexStart = 3;
            taskNameColInfo.ColumnIndexEnd = 3;
            taskNameColInfo.Width = 40 * 256;
            sheet.AddColumnInfo(dateColInfo1);
            sheet.AddColumnInfo(dateColInfo2);
            sheet.AddColumnInfo(projNameColInfo);
            sheet.AddColumnInfo(taskNameColInfo);
            #endregion

            Cells cells = sheet.Cells;
            cells.Add(1, 1, "城市", titleXF);
            cells.Add(1, 2, "项目名称", titleXF);
            cells.Add(1, 3, "序号", titleXF);
            cells.Add(1, 4, "工作事项（项目计划）", titleXF);
            cells.Add(1, 5, "计划开始时间", titleXF);
            cells.Add(1, 6, "计划完成时间", titleXF);
            cells.Add(1, 7, "责任人", titleXF);
            cells.Add(1, 8, "状态", titleXF);
            cells.Add(1, 9, "实际开始时间", titleXF);
            cells.Add(1, 10, "实际完成时间", titleXF);

            #region 读取列表

            string startDate = PageReq.GetParam("startDate");
            string endDate = PageReq.GetParam("endDate");
            DateTime minDate = DateTime.MinValue;
            DateTime maxDate = DateTime.MaxValue;
            List<string> comIdList = PageReq.GetParamList("city").ToList();
            List<string> projIdList = PageReq.GetParamList("proj").ToList();
            List<string> managerIdList = PageReq.GetParamList("manager").ToList();
            List<int> statusList = PageReq.GetParamIntList("status").ToList();
            //根据城市id找出所有工程，在根据工程找出所有项目，如果传入项目projid则通过这些projid查找项目，找出这些项目所有的默认计划，不包括费用支付
            //根据筛选出的计划查找任务，如果传入了任务负责人managerid则在这些任务里筛选出负责人包括在managerid里的任务
            //通过年月组合再次筛选任务，只要任务的开始或结束时间落在当月则不去除
            var comList = new List<BsonDocument>();
            if (comIdList.Count != 0)
            {
                comList = dataOp.FindAllByKeyValList("DesignManage_Company", "comId", comIdList).ToList();
            }
            else
            {
                comList = dataOp.FindAll("DesignManage_Company").ToList();//找出所有的城市
            }
            var projList = new List<BsonDocument>();
            var engList = new List<BsonDocument>();
            var planList = new List<BsonDocument>();
            if (projIdList.Count != 0)
            {
                projList = dataOp.FindAllByKeyValList("XH_DesignManage_Project", "projId", projIdList).Where(p => p.Int("isProj") == 1).ToList();
                engList = dataOp.FindAllByKeyValList("XH_DesignManage_Engineering", "engId", projList.Select(p => p.Text("engId")).ToList()).ToList();
            }
            else
            {
                engList = dataOp.FindAllByKeyValList("XH_DesignManage_Engineering", "comId", comList.Select(p => p.Text("comId")).ToList()).ToList();
                projList = dataOp.FindAllByKeyValList("XH_DesignManage_Project", "engId", engList.Select(p => p.Text("engId")).ToList()).Where(p => p.Int("isProj") == 1).ToList();
            }
            var planListQuery = Query.And(Query.EQ("isDefault", "1"), Query.In("projId", projList.Select(p => (BsonValue)p.Text("projId")).ToList()));
            planList = dataOp.FindAllByQuery("XH_DesignManage_Plan", planListQuery).ToList();
            var taskList = dataOp.FindAllByKeyValList("XH_DesignManage_Task", "planId", planList.Select(p => p.Text("planId")).ToList()).ToList();
            var managerRelList = new List<BsonDocument>();
            if (managerIdList.Count != 0)
            {
                managerRelList = dataOp.FindAllByKeyValList("XH_DesignManage_TaskManager", "userId", managerIdList).ToList();
                managerRelList = managerRelList.Where(p => p.Int("type") == (int)TaskManagerType.TaskOwner).ToList();
                taskList = taskList.Where(p => managerRelList.Select(q => q.Int("taskId")).Contains(p.Int("taskId"))).ToList();
            }

            DateTime.TryParse(startDate, out minDate);
            DateTime.TryParse(endDate, out maxDate);
            taskList = taskList.Where(p => p.Date("curStartData") < maxDate && p.Date("curEndData") >= minDate).ToList();

            List<BsonDocument> taskTabList = new List<BsonDocument>();
            var taskManager = new BsonDocument();
            string managerType = ((int)TaskManagerType.TaskOwner).ToString();

            var taskResult = from c in comList
                             join e in engList on c.Int("comId") equals e.Int("comId")
                             join p in projList on e.Int("engId") equals p.Int("engId")
                             join t in taskList on p.Int("projId") equals t.Int("projId")
                             where t.Int("nodePid") != 0
                             select new
                             {
                                 comId = c.Int("comId"),
                                 projId = p.Int("projId"),
                                 task = t
                             };
            foreach (var result in taskResult)
            {
                var task = result.task;
                BsonDocument taskInfo = new BsonDocument();
                int status = 0;//1-已超期 2-未开始 3-进行中 4-已完成
                string statusName = "";
                if (task.Int("status", 2) == (int)TaskStatus.NotStarted || task.Int("status", 2) == (int)TaskStatus.SplitCompleted)
                {
                    if (DateTime.Today > task.Date("curStartData"))
                    {
                        status = 1;
                        statusName = "已超期";
                    }
                    else
                    {
                        status = 2;
                        statusName = "未开始";
                    }
                }
                else if (task.Int("status", 2) == (int)TaskStatus.Processing)
                {
                    if (task.Date("curEndData") == DateTime.MinValue)
                    {
                        status = 3;
                        statusName = "进行中";
                    }
                    else
                    {
                        if (task.Date("curEndData") < DateTime.Today)
                        {
                            status = 1;
                            statusName = "已超期";
                        }
                        else
                        {
                            status = 3;
                            statusName = "进行中";
                        }
                    }
                }
                else if (task.Int("status") == (int)TaskStatus.Completed)
                {
                    status = 4;
                    statusName = "已完成";
                }
                if (statusList.Count() != 0)
                {
                    if (!statusList.Contains(status))
                    {
                        continue;
                    }
                }

                //查找该任务负责人
                taskManager = dataOp.FindOneByQuery("XH_DesignManage_TaskManager", Query.And(Query.EQ("taskId", task.Text("taskId")), Query.EQ("type", managerType)));

                string managerName = string.Empty;
                int managerId = 0;
                if (taskManager != null)
                {
                    managerId = taskManager.Int("userId");
                    managerName = taskManager.SourceBsonField("userId", "name");
                }

                taskInfo.Add("managerName", managerName);
                taskInfo.Add("comId", result.comId);
                taskInfo.Add("projId", result.projId);
                taskInfo.Add("curStartDate", task.ShortDate("curStartData"));
                taskInfo.Add("curEndDate", task.ShortDate("curEndData"));
                taskInfo.Add("factStartDate", task.ShortDate("factStartDate"));
                taskInfo.Add("factEndDate", task.ShortDate("factEndDate"));
                taskInfo.Add("taskName", task.Text("name"));
                taskInfo.Add("status", status);
                taskInfo.Add("statusName", statusName);
                taskTabList.Add(taskInfo);
            }

            taskTabList = taskTabList.OrderBy(p => p.Int("comId")).ThenBy(p => p.Int("projId")).ToList();
            #endregion

            int curComId = 0;
            int curProjId = 0;
            int rowIndex = 2;
            int comIndex = 2;
            int projIndex = 2;
            foreach (var table in taskTabList)
            {
                if (curComId != table.Int("comId"))
                {
                    curComId = table.Int("comId");
                    int comCount = taskTabList.Where(p => p.Int("comId") == curComId).Count();
                    var company = dataOp.FindOneByKeyVal("DesignManage_Company", "comId", curComId.ToString());
                    string comName = company == null ? string.Empty : company.Text("name");
                    MergeArea merge = new MergeArea(comIndex, comIndex + comCount - 1, 1, 1);
                    sheet.AddMergeArea(merge);
                    cells.Add(comIndex, 1, comName, dataXF);
                    comIndex += comCount;
                }
                if (curProjId != table.Int("projId"))
                {
                    curProjId = table.Int("projId");
                    int projCount = taskTabList.Where(p => p.Int("projId") == curProjId).Count();
                    var project = dataOp.FindOneByKeyVal("XH_DesignManage_Project", "projId", curProjId.ToString());
                    string projName = project == null ? string.Empty : project.Text("name");
                    MergeArea merge2 = new MergeArea(projIndex, projIndex + projCount - 1, 2, 2);
                    sheet.AddMergeArea(merge2);
                    cells.Add(projIndex, 2, projName, dataXF);
                    projIndex += projCount;
                }


                cells.Add(rowIndex, 3, rowIndex - 1, dataXF);
                cells.Add(rowIndex, 4, table.Text("taskName"), dataXF);
                cells.Add(rowIndex, 5, table.Text("curStartDate"), dataXF);
                cells.Add(rowIndex, 6, table.Text("curEndDate"), dataXF);
                cells.Add(rowIndex, 7, table.Text("managerName"), dataXF);
                cells.Add(rowIndex, 8, table.Text("statusName"), dataXF);
                cells.Add(rowIndex, 9, table.Text("factStartDate"), dataXF);
                cells.Add(rowIndex, 10, table.Text("factEndDate"), dataXF);
                rowIndex++;
            }

            string fileName = string.Format("{0}至{1}项目计划报表", startDate, endDate);

            MyXlsUtility.ExportByWeb(xlsDoc, fileName);

        }
        #endregion

        #region 给流程传阅人员发送邮件提醒
        public ActionResult SendMailByInstance()
        {
            InvokeResult result = new InvokeResult() { Status = Status.Successful };
            var instanceId = PageReq.GetForm("flowInstanceId");
            var toUserIds = PageReq.GetForm("userIds").Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();

            var curFlowInstance = dataOp.FindOneByQuery("BusFlowInstance", Query.EQ("flowInstanceId", instanceId));
            if (curFlowInstance == null)
            {
                result.Status = Status.Failed;
                result.Message = "流程实例不存在";
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            var isBodyHtml = false;
            var mailBody = new StringBuilder();
            var tableName = curFlowInstance.Text("tableName");
            var linkUrl = string.Empty;
            if (tableName == "XH_DesignManage_Task")
            {
                var mailTitle = string.Format("中海宏洋ERP系统审批业务事项提醒通知--您有一个传阅的审批任务");
                var taskObj = dataOp.FindOneByKeyVal(tableName, curFlowInstance.Text("referFieldName"), curFlowInstance.Text("referFieldValue"));
                if (taskObj == null)
                {
                    result.Status = Status.Failed;
                    result.Message = "任务不存在";
                    return Json(TypeConvert.InvokeResultToPageJson(result));
                }
                var projObj = dataOp.FindOneByQuery("XH_DesignManage_Project", Query.EQ("projId", taskObj.Text("projId")));
                if (projObj == null)
                {
                    result.Status = Status.Failed;
                    result.Message = "项目不存在";
                    return Json(TypeConvert.InvokeResultToPageJson(result));
                }
                linkUrl = SysAppConfig.MailHostDomain + "/DesignManage/NewTaskWorkFlowInfo/" + taskObj.Text("taskId");
                mailBody.AppendFormat("您好,{0}项目的任务{1}，流程审批经系统传阅给您，请登录系统进行查阅；链接为： {2}", projObj.Text("name"), taskObj.Text("name"), linkUrl);

                SendMail(toUserIds, mailTitle, mailBody.ToString(), string.Empty, isBodyHtml);
            }
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }
        #endregion

        #region ZHHY保存供应商业务联系人
        public ActionResult saveManagerZHHY(FormCollection saveForm)
        {
            InvokeResult result = new InvokeResult();
            string teamRels = PageReq.GetParam("teamRels"); //供应商负责人
            string TendertemRels = PageReq.GetParam("TendertemRels"); //投标负责人
            string designers = PageReq.GetParam("designers"); //投资主要设计师
            string supplierId = PageReq.GetParam("supplierId");
            List<string> teamRelArray = teamRels.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();
            List<string> TenderteamRelArray = TendertemRels.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();
            List<string> designersRelArray = designers.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();
            List<BsonDocument> oldTeamRelList = dataOp.FindAllByKeyVal("XH_Supplier_Manager", "supplierId", supplierId).ToList();   //所有旧的供应商关联
            List<BsonDocument> oldTenderTeamRelList = dataOp.FindAllByKeyVal("XH_Supplier_TenderManager", "supplierId", supplierId).ToList();   //所有旧的供应商关联
            List<BsonDocument> olddesignersRelList = dataOp.FindAllByKeyVal("XH_Supplier_Designer", "supplierId", supplierId).ToList();
            List<StorageData> saveList = new List<StorageData>();
            foreach (var teamRel in teamRelArray) //循环新的关联,已存在则不添加,不存在则添加新的
            {
                string[] infoArr = teamRel.Split(new string[] { ":" }, StringSplitOptions.None);

                if (infoArr.Count() >= 3 && infoArr[0].Trim() != "")
                {

                    string manName = infoArr[0];
                    string manPost = infoArr[1];
                    string manphone = infoArr[2];

                    BsonDocument oldRel = oldTeamRelList.Where(t => t.String("ManagerName") == manName && t.String("ManagerPost") == manPost && t.String("ManagerPhone") == manphone).FirstOrDefault();

                    if (oldRel == null)
                    {
                        StorageData tempData = new StorageData();

                        tempData.Name = "XH_Supplier_Manager";
                        tempData.Document = TypeConvert.ParamStrToBsonDocument("supplierId=" + supplierId + "&ManagerName=" + manName + "&ManagerPhone=" + manphone + "&ManagerPost=" + manPost);
                        tempData.Type = StorageType.Insert;

                        saveList.Add(tempData);
                    }
                }
            }

            foreach (var oldRel in oldTeamRelList) //删除旧数据
            {
                if (!teamRelArray.Contains(string.Format("{0}:{1}:{2}", oldRel.String("ManagerName"), oldRel.String("ManagerPost"), oldRel.String("ManagerPhone"))))
                {
                    StorageData tempData = new StorageData();

                    tempData.Name = "XH_Supplier_Manager";
                    tempData.Query = Query.EQ("managerId", oldRel.String("managerId"));
                    tempData.Type = StorageType.Delete;

                    saveList.Add(tempData);
                }
            }
            foreach (var teamRel in TenderteamRelArray) //循环新的关联,已存在则不添加,不存在则添加新的
            {
                string[] infoArr = teamRel.Split(new string[] { ":" }, StringSplitOptions.None);

                if (infoArr.Count() >= 3 && infoArr[0].Trim() != "")
                {

                    string manName = infoArr[0];
                    string manPost = infoArr[1];
                    string manphone = infoArr[2];

                    BsonDocument oldRel = oldTeamRelList.Where(t => t.String("TenderManagerName") == manName && t.String("TenderManagerPost") == manPost && t.String("TenderManagerPhone") == manphone).FirstOrDefault();

                    if (oldRel == null)
                    {
                        StorageData tempData = new StorageData();

                        tempData.Name = "XH_Supplier_TenderManager";
                        tempData.Document = TypeConvert.ParamStrToBsonDocument("supplierId=" + supplierId + "&TenderManagerName=" + manName + "&TenderManagerPhone=" + manphone + "&TenderManagerPost=" + manPost);
                        tempData.Type = StorageType.Insert;

                        saveList.Add(tempData);
                    }
                }
            }

            foreach (var oldRel in oldTenderTeamRelList) //删除旧数据
            {
                if (!teamRelArray.Contains(string.Format("{0}:{1}:{2}", oldRel.String("TenderManagerName"), oldRel.String("TenderManagerPost"), oldRel.String("TenderManagerPhone"))))
                {
                    StorageData tempData = new StorageData();

                    tempData.Name = "XH_Supplier_TenderManager";
                    tempData.Query = Query.EQ("TendermanagerId", oldRel.String("TendermanagerId"));
                    tempData.Type = StorageType.Delete;

                    saveList.Add(tempData);
                }
            }

            foreach (var teamRel in designersRelArray) //循环新的关联,已存在则不添加,不存在则添加新的
            {
                string[] infoArr = teamRel.Split(new string[] { ":" }, StringSplitOptions.None);

                if (infoArr[1].Trim() != "")
                {

                    string manName = infoArr[1];
                    string manPost = infoArr[0];
                    string manphone = infoArr[5];
                    string resume = infoArr[2];//简历
                    string qualification = infoArr[3];//职业资质
                    string repreProj = infoArr[4];//代表项目

                    BsonDocument oldRel = oldTeamRelList.Where(t => t.String("name") == manName && t.String("Post") == manPost && t.String("Contact") == manphone && t.String("Profesqualificat") == qualification && t.String("Behalfproject") == repreProj && t.String("Resume") == resume).FirstOrDefault();

                    if (oldRel == null)
                    {
                        StorageData tempData = new StorageData();

                        tempData.Name = "XH_Supplier_Designer";
                        tempData.Document = TypeConvert.ParamStrToBsonDocument("supplierId=" + supplierId + "&name=" + manName + "&Contact=" + manphone + "&Post=" + manPost + "&Resume=" + resume + "&Profesqualificat=" + qualification + "&Behalfproject=" + repreProj);
                        tempData.Type = StorageType.Insert;

                        saveList.Add(tempData);
                    }
                }
            }

            foreach (var oldRel in olddesignersRelList) //删除旧数据
            {
                if (!teamRelArray.Contains(string.Format("{0}:{1}:{2}", oldRel.String("name"), oldRel.String("Post"), oldRel.String("Contact"))))
                {
                    StorageData tempData = new StorageData();

                    tempData.Name = "XH_Supplier_Designer";
                    tempData.Query = Query.EQ("designerId", oldRel.String("designerId"));
                    tempData.Type = StorageType.Delete;

                    saveList.Add(tempData);
                }
            }
            result = dataOp.BatchSaveStorageData(saveList);
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }
        #endregion

        /// <summary>
        /// 一二级关联联动,支持一个前置选多个后置 获知一个后置选择1个前置
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SaveAcrossPlanTaskRelation()
        {
            PageJson json = new PageJson();

            #region 获取参数
            var preTaskId = PageReq.GetFormInt("preTaskId");//只能有一个
            var nextTaskIds = PageReq.GetFormIntList("nextTaskIds");
            var referType = PageReq.GetFormInt("referType");
            var relateMode = PageReq.GetFormInt("relateMode");//0代表选择前置，1代表选择多个后继节点
            //var preProjId = PageReq.GetForm("preProjId");
            //var prePlanId = PageReq.GetForm("prePlanId");
            //var nextProjId = PageReq.GetForm("nextProjId");
            //var nextPlanId = PageReq.GetForm("nextPlanId");

            List<BsonDocument> deleteList = new List<BsonDocument>();
            List<BsonDocument> relationList = new List<BsonDocument>();
            List<StorageData> storageList = new List<StorageData>();
            var preTaskObj = dataOp.FindOneByKeyVal("XH_DesignManage_Task", "taskId", preTaskId.ToString());

            var nextTaskObjList = dataOp.FindAllByKeyValList("XH_DesignManage_Task", "taskId", nextTaskIds.Select(c => c.ToString()).ToList()).ToList();

            if (relateMode == 0)//选择单个个前置节点，并删除该后继结点对应的前置节点 关联
            {
                relationList = dataOp.FindAllByQuery("XH_DesignManage_AcrossPlanTaskRelation", Query.In("nextTaskId", nextTaskIds.Select(t => (BsonValue)t.ToString()))).ToList();
                deleteList = relationList.Where(c => preTaskId != c.Int("preTaskId")).ToList();
            }
            else//选择多个后继节点，并删除该前继结点对应的后置节点
            {
                relationList = dataOp.FindAllByKeyVal("XH_DesignManage_AcrossPlanTaskRelation", "preTaskId", preTaskId.ToString()).ToList();   //已有的技术关系列表
                deleteList = relationList.Where(c => !nextTaskIds.Contains(c.Int("nextTaskId"))).ToList();

            }


            #endregion

            #region  获取添加更新列表
            if (preTaskObj != null)
            {

                foreach (var nextTaskId in nextTaskIds)
                {
                    var nextTaskObj = nextTaskObjList.Where(c => c.Int("taskId") == nextTaskId).FirstOrDefault();
                    if (nextTaskObj == null) continue;
                    BsonDocument tempBson = new BsonDocument();
                    tempBson.Add("preTaskId", preTaskId.ToString());
                    tempBson.Add("nextTaskId", nextTaskId.ToString());
                    tempBson.Add("referType", referType.ToString());
                    tempBson.Add("preProjId", preTaskObj.Text("projId"));
                    tempBson.Add("prePlanId", preTaskObj.Text("planId"));
                    tempBson.Add("nextProjId", nextTaskObj.Text("projId"));
                    tempBson.Add("nextPlanId", nextTaskObj.Text("planId"));
                    BsonDocument tempRel = null;
                    if (relateMode == 0)//选择单个个前置节点，并删除该后继结点对应的前置节点 关联
                    {
                        tempRel = relationList.Where(t => t.Int("preTaskId") == preTaskId && t.Int("referType") == referType).FirstOrDefault();
                    }
                    else//选择多个后继节点，并删除该前继结点对应的后置节点
                    {
                        tempRel = relationList.Where(t => t.Int("nextTaskId") == nextTaskId && t.Int("referType") == referType).FirstOrDefault();

                    }
                    if (tempRel == null)        //判断是否在已有的列表里面,没有则进行添加
                    {
                        StorageData temp = new StorageData();
                        temp.Name = "XH_DesignManage_AcrossPlanTaskRelation";
                        temp.Type = StorageType.Insert;
                        temp.Document = tempBson;
                        storageList.Add(temp);
                    }

                }
            }
            #endregion

            if (deleteList.Count() > 0)
            {
                storageList.AddRange(from c in deleteList
                                     select new StorageData()
                                     {
                                         Document = c,
                                         Name = "XH_DesignManage_AcrossPlanTaskRelation",
                                         Type = StorageType.Delete,
                                         Query = Query.EQ("relId", c.Text("relId"))
                                     }
                                         );
            }
            InvokeResult result = dataOp.BatchSaveStorageData(storageList);

            json = TypeConvert.InvokeResultToPageJson(result);




            return Json(json);
        }

        #region 保存流程传阅设定人员

        /// <summary>
        /// 保存流程步骤
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SaveFlowStepUserCirculation()
        {
            int stepId = PageReq.GetFormInt("stepId");
            string userIds = PageReq.GetForm("userIds");

            BsonDocument tempSetp = dataOp.FindOneByKeyVal("BusFlowStep", "stepId", stepId.ToString());
            List<StorageData> userDataList = new List<StorageData>();

            List<string> userIdList = userIds.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();
            List<BsonDocument> oldRelList = dataOp.FindAllByKeyVal("StepCirculation", "stepId", tempSetp.String("stepId")).ToList();

            foreach (var tempUserId in userIdList)
            {
                BsonDocument tempOld = oldRelList.Where(t => t.String("userId") == tempUserId).FirstOrDefault();    //旧的关联

                if (tempOld == null)    //没有旧的关联则添加
                {
                    StorageData tempData = new StorageData();
                    tempData.Name = "StepCirculation";
                    tempData.Type = StorageType.Insert;
                    tempData.Document = new BsonDocument();

                    tempData.Document.Add("flowId", tempSetp.String("flowId"));
                    tempData.Document.Add("userId", tempUserId);
                    tempData.Document.Add("stepId", tempSetp.String("stepId"));
                    tempData.Document.Add("unAvaiable", "0");
                    userDataList.Add(tempData);
                }
            }

            foreach (var tempOld in oldRelList)
            {
                if (userIdList.Contains(tempOld.String("userId")) == false) //不在传入的,则删除
                {
                    StorageData tempData = new StorageData();
                    tempData.Name = "StepCirculation";
                    tempData.Type = StorageType.Delete;
                    tempData.Query = Query.EQ("circulatStepId", tempOld.String("circulatStepId"));

                    userDataList.Add(tempData);
                }
            }

            InvokeResult result = dataOp.BatchSaveStorageData(userDataList);

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        /// <summary>
        /// 保存流程步骤
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SaveFlowStepCirculationFlowPosition()
        {
            int stepId = PageReq.GetFormInt("stepId");
            string flowPosIds = PageReq.GetForm("flowPosIds");

            BsonDocument tempSetp = dataOp.FindOneByKeyVal("BusFlowStep", "stepId", stepId.ToString());
            List<StorageData> userDataList = new List<StorageData>();

            List<string> flowPosIdList = flowPosIds.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();
            List<BsonDocument> oldRelList = dataOp.FindAllByKeyVal("StepCirculationFlowPosition", "stepId", tempSetp.String("stepId")).ToList();

            foreach (var tempflowPosId in flowPosIdList)
            {
                BsonDocument tempOld = oldRelList.Where(t => t.String("flowPosId") == tempflowPosId).FirstOrDefault();    //旧的关联

                if (tempOld == null)    //没有旧的关联则添加
                {
                    StorageData tempData = new StorageData();
                    tempData.Name = "StepCirculationFlowPosition";
                    tempData.Type = StorageType.Insert;
                    tempData.Document = new BsonDocument();

                    tempData.Document.Add("flowId", tempSetp.String("flowId"));
                    tempData.Document.Add("flowPosId", tempflowPosId);
                    tempData.Document.Add("stepId", tempSetp.String("stepId"));
                    tempData.Document.Add("unAvaiable", "0");
                    userDataList.Add(tempData);
                }
            }

            foreach (var tempOld in oldRelList)
            {
                if (flowPosIdList.Contains(tempOld.String("flowPosId")) == false) //不在传入的,则删除
                {
                    StorageData tempData = new StorageData();
                    tempData.Name = "StepCirculationFlowPosition";
                    tempData.Type = StorageType.Delete;
                    tempData.Query = Query.EQ("circulatStepId", tempOld.String("circulatStepId"));

                    userDataList.Add(tempData);
                }
            }

            InvokeResult result = dataOp.BatchSaveStorageData(userDataList);

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        /// <summary>
        /// 获取岗位列表列表
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public JsonResult GetBusFlowPositionJson()
        {

            var MapList = dataOp.FindAll("BusFlowPosition").ToList();
            var finalResult = from c in MapList
                              select new { id = c.Int("flowPosId"), name = c.Text("name") };
            return this.Json(finalResult.ToList(), JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region 保存土地信息
        /// <summary>
        /// 保存数据库
        /// </summary>
        /// <param name="saveform"></param>
        /// <returns></returns>
        public ActionResult SaveLandInfo(FormCollection saveForm)
        {
            lock (objPad)
            {
                PageJson json = new PageJson();
                InvokeResult result = new InvokeResult();
                BsonDocument landInfo = new BsonDocument();
                string tbName = PageReq.GetForm("tbName");
                string queryStr = PageReq.GetForm("queryStr");
                string propertyStr = PageReq.GetForm("propertyIdArray");//地块物业属性、
                string memberStr = PageReq.GetForm("members");//地块成员
                List<string> notKey = new List<string> { "tbName", "queryStr", "str", "propertyIdArray", "fileTypeId", "fileObjId", "keyValue", "tableName", "keyName", "delFileRelIds", "uploadFileList", "fileSaveType", "members" };
                List<string> propertyIdList = new List<string>();
                List<string> memberIds = new List<string>();

                if (!string.IsNullOrEmpty(propertyStr))
                {
                    propertyIdList = propertyStr.SplitParam(StringSplitOptions.RemoveEmptyEntries, ",").ToList();
                }
                if (!string.IsNullOrEmpty(memberStr))
                {
                    memberIds = memberStr.SplitParam(StringSplitOptions.RemoveEmptyEntries, ",").ToList();
                }

                List<StorageData> dataSource = new List<StorageData>();
                if (!string.IsNullOrEmpty(queryStr))
                {
                    landInfo = dataOp.FindOneByQuery("Land", TypeConvert.NativeQueryToQuery(queryStr));
                    if (landInfo != null)
                    {
                        foreach (var tempKey in saveForm.AllKeys)
                        {
                            //if (tempKey == "tbName" || tempKey == "queryStr" || tempKey == "str" || tempKey == "propertyIdArray") continue;
                            if (notKey.Contains(tempKey)) continue;
                            if (landInfo.Contains(tempKey))
                            {
                                landInfo[tempKey] = PageReq.GetForm(tempKey);
                            }
                            else
                            {
                                landInfo.Add(tempKey, PageReq.GetForm(tempKey));
                            }
                        }
                        StorageData tempData1 = new StorageData();
                        tempData1.Name = "Land";
                        tempData1.Type = StorageType.Update;
                        tempData1.Document = landInfo;
                        tempData1.Query = Query.EQ("landId", landInfo.String("landId"));
                        dataSource.Add(tempData1);
                        var exitProperty = dataOp.FindAllByQuery("LandProperty", Query.EQ("landId", landInfo.String("landId"))).ToList();//存在的属性
                        var exitMemberInfo = dataOp.FindAllByQuery("LandMember", Query.EQ("landId", landInfo.String("landId"))).ToList();//存在的成员
                        foreach (var tempProperty in propertyIdList)
                        {
                            var temp = exitProperty.Where(x => x.String("propertyId") == tempProperty).FirstOrDefault();

                            if (temp != null) { exitProperty.Remove(temp); }
                            else
                            {
                                BsonDocument tempProper = new BsonDocument();
                                tempProper.Add("landId", landInfo.String("landId"));
                                tempProper.Add("propertyId", tempProperty);
                                StorageData tempData = new StorageData();
                                tempData.Name = "LandProperty";
                                tempData.Type = StorageType.Insert;
                                tempData.Document = tempProper;
                                dataSource.Add(tempData);
                                // newPropertyIdList.Add(tempProperty);
                            }
                        }
                        if (exitProperty.Count() > 0)
                        {
                            foreach (var tempProperty in exitProperty)
                            {
                                StorageData tempData = new StorageData();
                                tempData.Name = "LandProperty";
                                tempData.Type = StorageType.Delete;
                                tempData.Query = Query.EQ("landPropertyId", tempProperty.String("landPropertyId"));
                                tempData.Document = tempProperty;
                                dataSource.Add(tempData);
                                //delPropertyId.Add(tempProperty.String("propertyId"));
                            }
                        }
                        foreach (var tempId in memberIds)
                        {
                            var temp = exitMemberInfo.Where(x => x.String("userId") == tempId).FirstOrDefault();

                            if (temp != null) { exitMemberInfo.Remove(temp); }
                            else
                            {
                                BsonDocument tempMember = new BsonDocument();
                                tempMember.Add("landId", landInfo.String("landId"));
                                tempMember.Add("userId", tempId);
                                StorageData tempData = new StorageData();
                                tempData.Name = "LandMember";
                                tempData.Type = StorageType.Insert;
                                tempData.Document = tempMember;
                                dataSource.Add(tempData);
                                // newPropertyIdList.Add(tempProperty);
                            }
                        }
                        if (exitMemberInfo.Count() > 0)
                        {
                            foreach (var tempMember in exitMemberInfo)
                            {
                                StorageData tempData = new StorageData();
                                tempData.Name = "LandMember";
                                tempData.Type = StorageType.Delete;
                                tempData.Query = Query.EQ("landMemberId", tempMember.String("landMemberId"));
                                tempData.Document = tempMember;
                                dataSource.Add(tempData);
                                //delPropertyId.Add(tempProperty.String("propertyId"));
                            }
                        }
                    }
                    result = dataOp.BatchSaveStorageData(dataSource);
                }
                else
                {
                    foreach (var tempKey in saveForm.AllKeys)
                    {
                        if (tempKey == "tbName" || tempKey == "queryStr" || tempKey == "str" || tempKey == "members" || tempKey == "propertyIdArray") continue;
                        if (landInfo.Contains(tempKey))
                        {
                            landInfo[tempKey] = PageReq.GetForm(tempKey);
                        }
                        else
                        {
                            landInfo.Add(tempKey, PageReq.GetForm(tempKey));
                        }
                    }
                    if (landInfo.Contains("areaId"))
                    {
                        var curArea = dataOp.FindAllByQuery("LandArea", Query.EQ("areaId", landInfo.Text("areaId"))).FirstOrDefault();
                        if (curArea == null)
                        {
                            result.Status = Status.Failed;
                            result.Message = "无效的区域参数";
                            return Json(TypeConvert.InvokeResultToPageJson(result));
                        }
                    }
                    if (landInfo.Contains("cityId"))
                    {
                        var curCity = dataOp.FindAllByQuery("LandCity", Query.EQ("cityId", landInfo.Text("cityId"))).FirstOrDefault();
                        if (curCity == null)
                        {
                            result.Status = Status.Failed;
                            result.Message = "无效的城市参数";
                            return Json(TypeConvert.InvokeResultToPageJson(result));
                        }
                    }

                    result = dataOp.Insert("Land", landInfo);
                    if (result.Status == Status.Successful)
                    {
                        landInfo = result.BsonInfo;
                        Dictionary<string, int> tableTemp = new Dictionary<string, int>();//需要初始化的表名及对应模板Id,若id为0查找默认的模板
                        tableTemp.Add("ParcelCirInfo", 0);
                        tableTemp.Add("AdjacentParcelInfo", 0);
                        tableTemp.Add("GovRequest", 0);
                        tableTemp.Add("DesignFileApproval", 0);
                        tableTemp.Add("EngineeringFileApproval", 0);
                        tableTemp.Add("CompetitiveHouse", 0);
                        try
                        {
                            LandProjectDirBll lpdBll = LandProjectDirBll._();
                            lpdBll.LandInfoInit(landInfo.Int("landId"), tableTemp);
                            if (landInfo.Contains("isInit"))
                            {
                                landInfo["isInit"] = "1";
                            }
                            else
                            {
                                landInfo.Add("isInit", "1");
                            }
                            StorageData tempData1 = new StorageData();
                            tempData1.Name = "Land";
                            tempData1.Type = StorageType.Update;
                            tempData1.Query = Query.EQ("landId", landInfo.String("landId"));
                            tempData1.Document = landInfo;
                            dataSource.Add(tempData1);
                            //新增地块业态
                            foreach (var tempProperty in propertyIdList)
                            {
                                BsonDocument tempProper = new BsonDocument();
                                tempProper.Add("landId", landInfo.String("landId"));
                                tempProper.Add("propertyId", tempProperty);
                                StorageData tempData = new StorageData();
                                tempData.Name = "LandProperty";
                                tempData.Type = StorageType.Insert;
                                tempData.Document = tempProper;
                                dataSource.Add(tempData);

                            }
                            //新增地块成员
                            foreach (var tempId in memberIds)
                            {
                                BsonDocument tempMember = new BsonDocument();
                                tempMember.Add("landId", landInfo.String("landId"));
                                tempMember.Add("userId", tempId);
                                StorageData tempData = new StorageData();
                                tempData.Name = "LandMember";
                                tempData.Type = StorageType.Insert;
                                tempData.Document = tempMember;
                                dataSource.Add(tempData);

                            }

                            result = dataOp.BatchSaveStorageData(dataSource);
                            ImportProjOrLandRole(1, 1, landInfo.String("landId"), 1);//新增内置角色
                            //InsertBuiltRole("1", landInfo.String("landId"));取消按部门内置角色
                        }
                        catch (Exception ex) { }





                    }
                }

                if (result.Status == Status.Successful)
                {
                    json.htInfo = new System.Collections.Hashtable(); ;
                    json.htInfo.Add("newland", landInfo.String("landId"));
                    json.Success = true;
                }
                else
                {
                    json = TypeConvert.InvokeResultToPageJson(result);
                }
                return Json(json);
            }
        }
        /// <summary>
        /// 保存项目基本信息
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        public ActionResult SaveLandProjInfo(FormCollection saveForm)
        {
            lock (objPad)
            {
                PageJson json = new PageJson();
                InvokeResult result = new InvokeResult();
                string tbName = PageReq.GetForm("tbName");
                string queryStr = PageReq.GetForm("queryStr");
                string propertyStr = PageReq.GetForm("propertyIdArray");//物业形态
                string memberStr = PageReq.GetForm("members");//项目成员
                BsonDocument projInfo = new BsonDocument();
                List<string> propertyIdList = new List<string>();
                List<string> memberIdList = new List<string>();

                if (!string.IsNullOrEmpty(propertyStr))
                {
                    propertyIdList = propertyStr.SplitParam(StringSplitOptions.RemoveEmptyEntries, ",").ToList();
                }
                if (!string.IsNullOrEmpty(memberStr))
                {
                    memberIdList = memberStr.SplitParam(StringSplitOptions.RemoveEmptyEntries, ",").ToList();
                }
                List<StorageData> dataSource = new List<StorageData>();
                if (!string.IsNullOrEmpty(queryStr))
                {
                    projInfo = dataOp.FindOneByQuery("Project", TypeConvert.NativeQueryToQuery(queryStr));
                    if (projInfo != null)
                    {
                        foreach (var tempKey in saveForm.AllKeys)
                        {
                            if (tempKey == "tbName" || tempKey == "queryStr" || tempKey == "str" || tempKey == "propertyIdArray" || tempKey == "" || tempKey.Contains("fileList[") || tempKey.Contains("param.")) continue;
                            if (projInfo.Contains(tempKey))
                            {
                                projInfo[tempKey] = PageReq.GetForm(tempKey);
                            }
                            else
                            {
                                projInfo.Add(tempKey, PageReq.GetForm(tempKey));
                            }
                        }
                        StorageData tempData1 = new StorageData();
                        tempData1.Name = "Project";
                        tempData1.Type = StorageType.Update;
                        tempData1.Document = projInfo;
                        tempData1.Query = Query.EQ("projId", projInfo.String("projId"));
                        dataSource.Add(tempData1);
                        var exitProperty = dataOp.FindAllByQuery("ProjectBaseProperty", Query.EQ("projId", projInfo.String("projId"))).ToList();//已存在的项目属性
                        var exitMember = dataOp.FindAllByQuery("ProjectMember", Query.EQ("projId", projInfo.String("projId"))).ToList();//已存在的项目成员
                        foreach (var tempProperty in propertyIdList)
                        {
                            var temp = exitProperty.Where(x => x.String("propertyId") == tempProperty).FirstOrDefault();

                            if (temp != null) { exitProperty.Remove(temp); }
                            else
                            {
                                BsonDocument tempProper = new BsonDocument();
                                tempProper.Add("projId", projInfo.String("projId"));
                                tempProper.Add("propertyId", tempProperty);
                                StorageData tempData = new StorageData();
                                tempData.Name = "ProjectBaseProperty";
                                tempData.Type = StorageType.Insert;
                                tempData.Document = tempProper;
                                dataSource.Add(tempData);
                                // newPropertyIdList.Add(tempProperty);
                            }
                        }
                        if (exitProperty.Count() > 0)
                        {
                            foreach (var tempProperty in exitProperty)
                            {
                                StorageData tempData = new StorageData();
                                tempData.Name = "ProjectBaseProperty";
                                tempData.Type = StorageType.Delete;
                                tempData.Query = Query.EQ("projBasePropertyId", tempProperty.String("projBasePropertyId"));
                                tempData.Document = tempProperty;
                                dataSource.Add(tempData);
                                //delPropertyId.Add(tempProperty.String("propertyId"));
                            }
                        }
                        foreach (var tempId in memberIdList)
                        {
                            var temp = exitMember.Where(x => x.String("userId") == tempId).FirstOrDefault();

                            if (temp != null) { exitMember.Remove(temp); }
                            else
                            {
                                BsonDocument tempMember = new BsonDocument();
                                tempMember.Add("projId", projInfo.String("projId"));
                                tempMember.Add("userId", tempId);
                                StorageData tempData = new StorageData();
                                tempData.Name = "ProjectMember";
                                tempData.Type = StorageType.Insert;
                                tempData.Document = tempMember;
                                dataSource.Add(tempData);
                                // newPropertyIdList.Add(tempProperty);
                            }
                        }
                        if (exitMember.Count() > 0)
                        {
                            foreach (var tempMember in exitMember)
                            {
                                StorageData tempData = new StorageData();
                                tempData.Name = "ProjectMember";
                                tempData.Type = StorageType.Delete;
                                tempData.Query = Query.EQ("projMemberId", tempMember.String("projMemberId"));
                                tempData.Document = tempMember;
                                dataSource.Add(tempData);
                                //delPropertyId.Add(tempProperty.String("propertyId"));
                            }
                        }
                        result = dataOp.BatchSaveStorageData(dataSource);
                    }
                }
                else
                {
                    foreach (var tempKey in saveForm.AllKeys)
                    {
                        if (tempKey == "tbName" || tempKey == "queryStr" || tempKey == "str" || tempKey == "propertyIdArray") continue;
                        if (projInfo.Contains(tempKey))
                        {
                            projInfo[tempKey] = PageReq.GetForm(tempKey);
                        }
                        else
                        {
                            projInfo.Add(tempKey, PageReq.GetForm(tempKey));
                        }
                    }
                    result = dataOp.Insert("Project", projInfo);
                    if (result.Status == Status.Successful)
                    {
                        projInfo = result.BsonInfo; projInfo = result.BsonInfo;
                        Dictionary<string, int> tableTemp = new Dictionary<string, int>();//需要初始化的表名及对应模板Id,若id为0查找默认的模板
                        tableTemp.Add("LocationProposal", 0);
                        tableTemp.Add("TypeMatchProposal", 0);
                        tableTemp.Add("SaleOfficeProposal", 0);
                        tableTemp.Add("ExternalCorrect", 0);
                        tableTemp.Add("CostEcoIndicator", 0);
                        tableTemp.Add("KeyTechEcoIndicator", 0);
                        tableTemp.Add("LandInSide", 0);
                        tableTemp.Add("LandInfo", 0);
                        try
                        {
                            LandProjectDirBll lpdBll = LandProjectDirBll._();
                            lpdBll.ProjectInfoInit(projInfo.Int("projId"), tableTemp);

                            if (projInfo.Contains("isInit"))
                            {
                                projInfo["isInit"] = "1";
                            }
                            else
                            {
                                projInfo.Add("isInit", "1");
                            }
                            StorageData tempData1 = new StorageData();
                            tempData1.Name = "Project";
                            tempData1.Type = StorageType.Update;
                            tempData1.Query = Query.EQ("projId", projInfo.String("projId"));
                            tempData1.Document = projInfo;
                            dataSource.Add(tempData1);
                            tempData1 = new StorageData();
                            tempData1.Name = "Material_List";
                            tempData1.Type = StorageType.Insert;
                            tempData1.Document = new BsonDocument().Add("name", "方案阶段").Add("projId", projInfo.String("projId"));
                            dataSource.Add(tempData1);
                            tempData1 = new StorageData();
                            tempData1.Name = "Material_List";
                            tempData1.Type = StorageType.Insert;
                            tempData1.Document = new BsonDocument().Add("name", "施工阶段").Add("projId", projInfo.String("projId"));
                            dataSource.Add(tempData1);
                            //新增项目属性
                            foreach (var tempProperty in propertyIdList)
                            {
                                BsonDocument tempProper = new BsonDocument();
                                tempProper.Add("projId", projInfo.String("projId"));
                                tempProper.Add("propertyId", tempProperty);
                                StorageData tempData = new StorageData();
                                tempData.Name = "ProjectBaseProperty";
                                tempData.Type = StorageType.Insert;
                                tempData.Document = tempProper;
                                dataSource.Add(tempData);
                            }
                            //新增项目成员
                            foreach (var tempId in memberIdList)
                            {
                                BsonDocument tempMember = new BsonDocument();
                                tempMember.Add("projId", projInfo.String("projId"));
                                tempMember.Add("userId", tempId);
                                StorageData tempData = new StorageData();
                                tempData.Name = "ProjectMember";
                                tempData.Type = StorageType.Insert;
                                tempData.Document = tempMember;
                                dataSource.Add(tempData);

                            }
                            result = dataOp.BatchSaveStorageData(dataSource);
                            if (result.Status == Status.Successful)
                            {
                                ImportProjOrLandRole(2, 2, projInfo.String("projId"), 1);//新增内置项目角色
                            }

                        }
                        catch (Exception ex) { }



                    }
                }

                if (result.Status == Status.Successful)
                {
                    json.htInfo = new System.Collections.Hashtable(); ;
                    json.htInfo.Add("newProj", projInfo.String("projId"));
                    json.Success = true;
                }
                else
                {
                    json = TypeConvert.InvokeResultToPageJson(result);
                }
                return Json(json);
            }

        }
        /// <summary>
        /// 删除项目基本信息及关联信息
        /// </summary>
        /// <returns></returns>
        public JsonResult DelLandProjInfo()
        {
            PageJson json = new PageJson();
            string projId = PageReq.GetParam("projId");
            PolicyVersionBll pvBll = PolicyVersionBll._();
            InvokeResult result = pvBll.DeletePolicy(projId);
            List<BsonDocument> roleList = dataOp.FindAllByQuery("SysRole", Query.And(Query.EQ("landOrProj", "2"), Query.EQ("landOrProjId", projId))).ToList();
            List<StorageData> delData = new List<StorageData>();
            StorageData tempData = new StorageData();
            List<BsonDocument> dataScrope = dataOp.FindAllByQuery("DataScope", Query.And(Query.EQ("dataTableName", "Project"), Query.EQ("dataId", projId))).ToList();
            foreach (var tempScrope in dataScrope)
            {
                tempData = new StorageData();
                tempData.Document = tempScrope;
                tempData.Type = StorageType.Delete;
                tempData.Query = Query.EQ("scopeId", tempScrope.String("scopeId"));
                tempData.Name = "DataScope";
                delData.Add(tempData);
            }
            foreach (var tempRole in roleList)
            {

                List<BsonDocument> rightList = dataOp.FindAllByQuery("SysRoleRight", Query.EQ("roleId", tempRole.String("roleId"))).ToList();
                foreach (var tempRight in rightList)
                {
                    tempData = new StorageData();
                    tempData.Document = tempRight;
                    tempData.Type = StorageType.Delete;
                    tempData.Query = Query.EQ("roleOrgId", tempRight.String("roleOrgId"));
                    tempData.Name = "SysRoleRight";
                    delData.Add(tempData);
                }
                List<BsonDocument> landScrope = dataOp.FindAllByQuery("DataScope", Query.And(Query.EQ("dataTableName", "Land"), Query.EQ("roleId", tempRole.String("roleId")))).ToList();
                foreach (var tempLand in landScrope)
                {
                    tempData = new StorageData();
                    tempData.Document = tempLand;
                    tempData.Type = StorageType.Delete;
                    tempData.Query = Query.EQ("scopeId", tempLand.String("scopeId"));
                    tempData.Name = "DataScope";
                    delData.Add(tempData);
                }
                tempData = new StorageData();
                tempData.Document = tempRole;
                tempData.Type = StorageType.Delete;
                tempData.Query = Query.EQ("roleId", tempRole.String("roleId"));
                tempData.Name = "SysRole";
                delData.Add(tempData);

            }


            result = dataOp.BatchSaveStorageData(delData.Distinct().ToList());//删除权限
            if (result.Status == Status.Successful)
            {
                string tbName = "Project";
                string queryStr = "db.Project.distinct('_id',{'projId':'" + projId.ToString() + "'})";
                string dataStr = "";

                #region 删除文档
                int primaryKey = 0;
                TableRule rule = new TableRule(tbName);
                string keyName = rule.GetPrimaryKey();
                if (!string.IsNullOrEmpty(queryStr))
                {
                    var query = TypeConvert.NativeQueryToQuery(queryStr);
                    var recordDoc = dataOp.FindOneByQuery(tbName, query);
                    if (recordDoc != null)
                    {
                        primaryKey = recordDoc.Int(keyName);
                    }

                    FileOperationHelper opHelper = new FileOperationHelper();
                    result = opHelper.DeleteFile(tbName, keyName, primaryKey.ToString());
                }
                #endregion

                #region 删除数据
                BsonDocument curData = new BsonDocument();  //当前数据,即操作前数据

                if (queryStr.Trim() != "") curData = dataOp.FindOneByQuery(tbName, TypeConvert.NativeQueryToQuery(queryStr));

                dataOp.SetOperationData(tbName, queryStr, dataStr);

                result = dataOp.Delete();
                #endregion
                if (result.Status == Status.Successful)
                {
                    json.Message = "删除成功!";
                    json.Success = true;
                }
                else
                {
                    json.Message = "删除失败,请刷新后重试!";
                    json.Success = false;
                }
            }
            else
            {
                json.Message = "删除失败,请刷新后重试!";
                json.Success = false;
            }
            json.Success = result.Status == Status.Successful;
            json.Message = result.Message;

            return Json(json);

        }

        /// <summary>
        /// 删除地块
        /// </summary>
        /// <param name="landId"></param>
        /// <returns></returns>
        public JsonResult DelLandInfo(int landId)
        {
            PageJson json = new PageJson();
            InvokeResult result = new InvokeResult();
            BsonDocument landInfo = dataOp.FindOneByQuery("Land", Query.EQ("landId", landId.ToString()));
            if (landInfo == null)
            {
                json.Message = "实体不存在或已经移除,请刷新后重试!";
                json.Success = false;
                return Json(json);
            }
            List<BsonDocument> projectList = dataOp.FindAllByQuery("Project", Query.EQ("landId", landId.ToString())).ToList();
            if (projectList.Count() > 0)
            {
                json.Message = "该地块已建有分期,请先删除分期后再删除地块!";
                json.Success = false;
                return Json(json);
            }
            List<BsonDocument> roleList = dataOp.FindAllByQuery("SysRole", Query.And(Query.EQ("landOrProj", "1"), Query.EQ("landOrProjId", landId.ToString()))).ToList();
            List<StorageData> delData = new List<StorageData>();
            StorageData tempData = new StorageData();
            List<BsonDocument> dataScrope = dataOp.FindAllByQuery("DataScope", Query.And(Query.EQ("dataTableName", "Land"), Query.EQ("dataId", landId.ToString()))).ToList();
            foreach (var tempScrope in dataScrope)
            {
                tempData = new StorageData();
                tempData.Document = tempScrope;
                tempData.Type = StorageType.Delete;
                tempData.Query = Query.EQ("scopeId", tempScrope.String("scopeId"));
                tempData.Name = "DataScope";
                delData.Add(tempData);
            }
            foreach (var tempRole in roleList)
            {

                List<BsonDocument> rightList = dataOp.FindAllByQuery("SysRoleRight", Query.EQ("roleId", tempRole.String("roleId"))).ToList();
                foreach (var tempRight in rightList)
                {
                    tempData = new StorageData();
                    tempData.Document = tempRight;
                    tempData.Type = StorageType.Delete;
                    tempData.Query = Query.EQ("roleOrgId", tempRight.String("roleOrgId"));
                    tempData.Name = "SysRoleRight";
                    delData.Add(tempData);
                }
                tempData = new StorageData();
                tempData.Document = tempRole;
                tempData.Type = StorageType.Delete;
                tempData.Query = Query.EQ("roleId", tempRole.String("roleId"));
                tempData.Name = "SysRole";
                delData.Add(tempData);
            }

            result = dataOp.BatchSaveStorageData(delData);
            if (result.Status == Status.Successful)
            {
                string tbName = "Land";
                string queryStr = "db.Land.distinct('_id',{'landId':'" + landId.ToString() + "'})";
                string dataStr = "";

                #region 删除文档
                int primaryKey = 0;
                TableRule rule = new TableRule(tbName);
                string keyName = rule.GetPrimaryKey();
                if (!string.IsNullOrEmpty(queryStr))
                {
                    var query = TypeConvert.NativeQueryToQuery(queryStr);
                    var recordDoc = dataOp.FindOneByQuery(tbName, query);
                    if (recordDoc != null)
                    {
                        primaryKey = recordDoc.Int(keyName);
                    }

                    FileOperationHelper opHelper = new FileOperationHelper();
                    result = opHelper.DeleteFile(tbName, keyName, primaryKey.ToString());
                }
                #endregion

                #region 删除数据
                BsonDocument curData = new BsonDocument();  //当前数据,即操作前数据

                if (queryStr.Trim() != "") curData = dataOp.FindOneByQuery(tbName, TypeConvert.NativeQueryToQuery(queryStr));

                dataOp.SetOperationData(tbName, queryStr, dataStr);

                result = dataOp.Delete();
                #endregion
                if (result.Status == Status.Successful)
                {
                    json.Message = "删除成功!";
                    json.Success = true;
                }
                else
                {
                    json.Message = "删除失败,请刷新后重试!";
                    json.Success = false;
                }
            }
            else
            {
                json.Message = "删除失败,请刷新后重试!";
                json.Success = false;
            }
            return Json(json);

        }
        #endregion

        #region 保存分项资料目录
        /// <summary>
        /// 保存分项资料库目录
        /// </summary>
        /// <param name="catCollection"></param>
        /// <returns></returns>
        public ActionResult SaveProjDocSetting(FormCollection catCollection)
        {
            PageJson json = new PageJson();
            InvokeResult result = new InvokeResult();
            int projDoctionId = PageReq.GetFormInt("projDoctionId");//资料库Id
            int projId = PageReq.GetFormInt("projId");//项目Id
            int nodePid = PageReq.GetFormInt("nodePid");//父节点
            string ClassStr = PageReq.GetForm("ClassStr");//目录类型 专业-阶段-文档类型
            string tbName = PageReq.GetForm("tbName");
            string queryStr = PageReq.GetForm("queryStr");
            BsonDocument catEntity = new BsonDocument();
            if (!string.IsNullOrEmpty(queryStr))
            {
                catEntity = dataOp.FindOneByQuery("projDocCatDefId", TypeConvert.NativeQueryToQuery(queryStr));
                nodePid = catEntity.Int("nodePid");
                projId = catEntity.Int("projId");
            }
            else
            {
                foreach (var keyName in catCollection.AllKeys)
                {
                    if (keyName == "queryStr" || keyName == "tbName" || keyName == "ProjPhareId" || keyName == "sysProfId" || keyName == "fileCatId") continue;
                    catEntity.Add(keyName, PageReq.GetForm(keyName));
                }
            }
            ProjDocLibBll pdlBll = ProjDocLibBll._();
            if (pdlBll.CheckName(projDoctionId, catEntity.Int("nodePid"), catEntity.Int("projDocCatId"), catEntity.String("name")))
            {
                json.Message = "资料库下已存在重名目录";
                json.Success = false;
                return Json(json);
            }
            List<BsonDocument> catProtery = dataOp.FindAllByQuery("ProjDoctationCatProtery", Query.EQ("projDocCatId", catEntity.String("projDocCatId"))).ToList();
            List<StorageData> storgeData = new List<StorageData>();
            #region 目录属性列表
            if (!string.IsNullOrEmpty(ClassStr))
            {
                string[] classArr = ClassStr.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string tempClass in classArr)
                {
                    if (!string.IsNullOrEmpty(tempClass))
                    {
                        string[] classId = tempClass.Split('-');
                        if (classId.Length == 3)
                        {
                            BsonDocument tempExit = catProtery.Where(x => x.String("projPhareId") == classId[0] && x.String("sysProfId") == classId[1] && x.String("fileCatId") == classId[2]).FirstOrDefault();
                            if (tempExit == null)
                            {
                                BsonDocument tempPorperty = new BsonDocument();
                                tempPorperty.Add("projPhareId", classId[0]);
                                tempPorperty.Add("sysProfId", classId[1]);
                                tempPorperty.Add("fileCatId", classId[2]);
                                StorageData tempdata = new StorageData();
                                tempdata.Document = tempPorperty;
                                tempdata.Type = StorageType.Insert;
                                tempdata.Name = "ProjDoctationCatProtery";
                                storgeData.Add(tempdata);
                            }
                            else
                            {
                                catProtery.Remove(tempExit);
                            }
                        }
                    }
                }

            }
            foreach (var tempPro in catProtery)
            {
                StorageData tempData = new StorageData();
                tempData.Name = "ProjDoctationCatProtery";
                tempData.Document = tempPro;
                tempData.Query = Query.EQ("projDocCatDefId", tempPro.String("projDocCatDefId"));
                tempData.Type = StorageType.Delete;
                storgeData.Add(tempData);

            }
            #endregion

            return Json(json);
        }
        #endregion

        #region 中海投资经济指标Excel导出
        public void IndexExport()
        {
            int engId = PageReq.GetParamInt("engId");
            int projId = PageReq.GetParamInt("projId");
            string Projname = "";
            if (projId > 0)
            {
                BsonDocument projEndity = dataOp.FindOneByKeyVal("XH_DesignManage_Project", "projId", projId.ToString());
                engId = projEndity.Int("engId");
                Projname = "项目：" + projEndity.Text("name") + "的经济指标";
            }
            else
            {
                var engObj = dataOp.FindOneByKeyVal("XH_DesignManage_Engineering", "engId", engId.ToString());
                Projname = "地块：" + engObj.Text("name") + "的经济指标";
            }
            if (engId != 0 || projId != 0)
            {
                List<BsonDocument> itemList = projId == 0 ? dataOp.FindAllByQuery("XH_DesignManage_IndexItem", Query.And(Query.EQ("engId", engId.ToString()), Query.EQ("projId", "0"))).ToList() : dataOp.FindAllByKeyVal("XH_DesignManage_IndexItem", "projId", projId.ToString()).ToList(); //所有指标项

                List<BsonDocument> columnList = projId == 0 ? dataOp.FindAllByQuery("XH_DesignManage_IndexColumn", Query.And(Query.EQ("engId", engId.ToString()), Query.EQ("projId", "0"))).ToList() : dataOp.FindAllByKeyVal("XH_DesignManage_IndexColumn", "projId", projId.ToString()).ToList();    //所有指标列

                List<BsonDocument> valueList = dataOp.FindAllByQueryStr("XH_DesignManage_IndexValue", "engId=" + engId + "&projId=" + projId).ToList(); //所有值记录

                foreach (var column in columnList)
                {
                    BsonDocument tempVal = valueList.Where(t => t.Int("columnId") == column.Int("columnId")).FirstOrDefault();

                    if (tempVal == null)
                    {
                        InvokeResult result = dataOp.Insert("XH_DesignManage_IndexValue", "engId=" + engId + "&projId=" + projId + "&columnId=" + column.Int("columnId"));
                        if (result.Status == Status.Failed) throw new Exception(result.Message);
                        valueList.Add(result.BsonInfo);
                    }

                }
                XlsDocument xlsDoc = new XlsDocument(); //创建一个工作文档
                Worksheet sheet = xlsDoc.Workbook.Worksheets.Add("sheet1"); //创建一个工作页
                XF dataXF = xlsDoc.NewXF();
                dataXF.VerticalAlignment = VerticalAlignments.Centered;
                dataXF.Font.Bold = true;
                XF dataXF1 = xlsDoc.NewXF();
                dataXF1.VerticalAlignment = VerticalAlignments.Centered;
                ColumnInfo dateColInfo1 = new ColumnInfo(xlsDoc, sheet);
                dateColInfo1.ColumnIndexStart = 1;
                dateColInfo1.ColumnIndexEnd = (ushort)(columnList.Count());
                dateColInfo1.Width = 25 * 256;
                sheet.AddColumnInfo(dateColInfo1);
                ColumnInfo dateColInfo2 = new ColumnInfo(xlsDoc, sheet);
                dateColInfo2.ColumnIndexStart = 0;
                dateColInfo2.ColumnIndexEnd = 0;
                dateColInfo2.Width = 35 * 256;
                sheet.AddColumnInfo(dateColInfo2);
                Cells cells = sheet.Cells;
                cells.Add(1, 1, Projname, dataXF);
                MergeArea merge2 = new MergeArea(1, 1, 1, columnList.Count() + 1);
                sheet.AddMergeArea(merge2);
                int col = 1;
                int row = 2;
                cells.Add(2, 1, "指标项", dataXF);
                foreach (var column in columnList.OrderBy(t => t.Int("order")))
                {
                    col++;
                    cells.Add(2, col, column.Text("name"), dataXF);

                }
                col = 2;
                row = 2;
                foreach (var tempItem in itemList.OrderBy(t => t.String("nodeKey")))
                {
                    row++;
                    cells.Add(row, 1, tempItem.Text("name"), dataXF);
                    int curcol = 2;
                    foreach (var column in columnList.OrderBy(t => t.Int("order")))
                    {

                        BsonDocument tageVal = valueList.Where(t => t.Int("columnId") == column.Int("columnId")).FirstOrDefault();
                        cells.Add(row, curcol, tageVal.String(tempItem.String("dataKey")), dataXF1);
                        curcol++;
                    }

                }
                MyXlsUtility.ExportByWeb(xlsDoc, Projname);
            }

        }
        #endregion

        #region 交付物批量操作
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SaveDeliver(FormCollection saveForm)
        {
            InvokeResult result = new InvokeResult();
            string tbName = "Scheduled_deliver";
            string taskIds = PageReq.GetParam("taskIds");
            string queryStr = "";
            var a = taskIds.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var taskId in a)
            {
                if (taskId == "") continue;
                BsonDocument dataBson = new BsonDocument();
                dataBson.Add("taskId", taskId);
                foreach (var tempKey in saveForm.AllKeys)
                {

                    if (tempKey == "tbName" || tempKey == "queryStr" || tempKey == "taskId")
                    {
                        continue;
                    }
                    dataBson.Add(tempKey, PageReq.GetForm(tempKey));

                }
                result = dataOp.Save(tbName, queryStr != "" ? TypeConvert.NativeQueryToQuery(queryStr) : Query.Null, dataBson);

                if (result.Status != Status.Successful)
                {
                    return Json(TypeConvert.InvokeResultToPageJson(result));
                }
            }
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }
        #endregion

        #region 设计费用的模板导入
        public void DesignModelInsert()
        {
            var projId = PageReq.GetParamInt("projId");
            var cc = projId;
            List<BsonDocument> modelItemList = dataOp.FindAllByKeyVal("DesignPay_IndexItem", "type", "1").OrderBy(c => c.Text("nodeKey")).ToList();
            var ObjIdsList = new Dictionary<int, int>();//用于存储新增的旧keyId 与新keyId对应字典
            #region 添加预定义属性
            foreach (var modelVauleObj in modelItemList)  //遍历模板数据
            {

                var uniqueKey = modelVauleObj.Int("itemId");
                var curTtemValueObj = new BsonDocument();
                var nodePid = modelVauleObj.Int("nodePid");   //获取父节点
                var ParentId = 0;
                if (nodePid != 0)
                {
                    if (!ObjIdsList.ContainsKey(nodePid)) continue;
                    ParentId = ObjIdsList[nodePid];
                }
                curTtemValueObj.Add("type", "2"); // 具体的项目
                curTtemValueObj.Add("projId", projId);//项目Id
                curTtemValueObj.Add("dataKey", modelVauleObj.Text("dataKey"));
                curTtemValueObj.Add("name", modelVauleObj.Text("name"));
                curTtemValueObj.Add("nodeLevel", modelVauleObj.Text("nodeLevel"));
                curTtemValueObj.Add("nodePid", ParentId);
                var result = dataOp.Insert("DesignPay_IndexItem", curTtemValueObj);   //插入
                ObjIdsList.Add(uniqueKey, result.BsonInfo.Int("itemId"));
            }

            #endregion
        }
        #endregion

        #region 三盛
        #region 计划任务
        /// <summary>
        /// 根据分项Id获取任务列表
        /// </summary>
        /// <returns></returns>
        public ActionResult TaskListNew()
        {
            int planId = PageReq.GetParamInt("planId");     //计划Id
            int levelId = PageReq.GetParamInt("levelId");
            BsonDocument planEntity = dataOp.FindOneByKeyVal("XH_DesignManage_Plan", "planId", planId.ToString());             //获取计划
            if (planEntity == null)
            {
                planEntity = new BsonDocument();
            }
            List<BsonDocument> allProjTaskList = dataOp.FindAllByKeyVal("XH_DesignManage_Task", "projId", planEntity.Text("projId").ToString()).ToList();
            List<BsonDocument> allTaskList = dataOp.FindAllByKeyVal("XH_DesignManage_Task", "planId", planId.ToString()).ToList();

            BsonDocument rootTask = allTaskList.Where(m => m.Int("nodePid") == 0).FirstOrDefault();   //内置的根任务

            List<BsonDocument> taskList = new List<BsonDocument>();
            //所有需要展示的任务
            if (levelId != 0)
            {
                taskList = allTaskList.Where(m => m.Int("levelId") == levelId && m.Int("nodePid") != 0).ToList();
            }
            else
            {
                taskList = allTaskList.Where(m => m.Int("nodePid") != 0).ToList();
            }
            List<string> taskIdList = taskList.Select(t => t.String("taskId")).ToList();     //所有任务的Id列表
            List<int> allProjTaskIdList = allProjTaskList.Select(t => t.Int("taskId")).ToList();     //所有任务的Id列表

            List<BsonDocument> allProjFileList = dataOp.FindAllByQueryStr("FileRelation", "tableName=XH_DesignManage_Task&fileObjId=31").Where(c => allProjTaskIdList.Contains(c.Int("keyValue"))).ToList();
            List<BsonDocument> allManagerList = dataOp.FindAllByKeyValList("XH_DesignManage_TaskManager", "taskId", taskIdList).ToList(); //找出所有任务管理人

            List<string> allUserIdList = taskList.Select(t => t.String("createUser")).ToList();
            allUserIdList.AddRange(taskList.Select(t => t.String("updateUser")));
            allUserIdList.AddRange(allManagerList.Select(t => t.String("userId")));

            List<BsonDocument> allUserList = dataOp.FindAllByKeyValList("SysUser", "userId", allUserIdList).ToList();    //获取所有用到的相关人员

            //List<TaskFileDefaultValue> taskFIleDefaultList = plan.TaskFileDefaultValues != null ? plan.TaskFileDefaultValues.ToList() : new List<TaskFileDefaultValue>();   //任务文件信息

            var relQuery = Query.Or(Query.In("preTaskId", TypeConvert.StringListToBsonValueList(taskIdList)), Query.In("sucTaskId", TypeConvert.StringListToBsonValueList(taskIdList)));

            List<BsonDocument> taskRelList = dataOp.FindAllByQuery("XH_DesignManage_TaskRelation", relQuery).ToList();     //获取所有技术关系

            List<string> conDiagNodeIdList = taskList.Select(t => t.String("diagramId")).Distinct().ToList();                 //获取与脉络图的所有关系

            List<BsonDocument> contextDiagramList = dataOp.FindAllByKeyValList("XH_DesignManage_ContextDiagram", "diagramId", conDiagNodeIdList).ToList();   //所有用到的脉络图节点

            List<string> pointIdList = taskList.Select(t => t.String("pointId")).Distinct().ToList();   //获取与地铁图的所有关系

            List<BsonDocument> decisionPointList = dataOp.FindAllByKeyValList("XH_DesignManage_DecisionPoint", "pointId", pointIdList).ToList();   //所有用到的地铁图

            List<BsonDocument> allFlowRelList = dataOp.FindAllByKeyValList("XH_DesignManage_TaskBusFlow", "taskId", taskIdList).ToList();       //所有任务流程关联

            #region 获取任务的index值
            int index = 1;

            Dictionary<int, int> dicTaskIndex = new Dictionary<int, int>(); //任务对应输出的index值,用于技术关系key为任务Id,value为index值

            foreach (var task in taskList.OrderBy(t => t.String("nodeKey")))
            {
                if (!dicTaskIndex.ContainsKey(task.Int("taskId")))
                {
                    dicTaskIndex.Add(task.Int("taskId"), index);
                    index++;
                }
            }
            #endregion
            #region 获取专业 阶段 类别
            List<BsonDocument> sysProf = dataOp.FindAll("System_Professional").ToList();//系统专业
            List<BsonDocument> sysStageList = dataOp.FindAll("System_Stage").ToList();  //系统阶段
            List<BsonDocument> projFileCatList = dataOp.FindAll("System_FileCategory").ToList(); //系统文档类别
            #endregion
            List<object> taskListJson = new List<object>();
            var hitAllTaskQuery = taskList.OrderBy(t => t.String("nodeKey")).AsQueryable();
            if (planEntity.Int("isContractPlan") == 1)//费用支付计划不展示只展示前3级 包括根节点
            {
                hitAllTaskQuery = hitAllTaskQuery.Where(c => c.Int("nodeLevel") <= 3);
            }
            foreach (var task in hitAllTaskQuery)
            {
                #region  获取人员信息
                //计划分解人
                BsonDocument spliterManager = allManagerList.Where(m => m.Int("taskId") == task.Int("taskId") && m.Int("type") == (int)TaskManagerType.PlanSpliter).FirstOrDefault();
                BsonDocument spliterUser = allUserList.Where(t => t.Int("userId") == (spliterManager != null ? spliterManager.Int("userId") : -1)).FirstOrDefault();

                //任务负责人
                BsonDocument ownerManager = allManagerList.Where(m => m.Int("taskId") == task.Int("taskId") && m.Int("type") == (int)TaskManagerType.TaskOwner).FirstOrDefault();
                BsonDocument ownerUser = allUserList.Where(t => t.Int("userId") == (ownerManager != null ? ownerManager.Int("userId") : -1)).FirstOrDefault();
                #region 类别拼写
                BsonDocument profStage = dataOp.FindOneByQuery("TaskDocProtery", Query.EQ("taskId", task.String("taskId")));//专业阶段类别
                string profStageStr = string.Empty;
                if (profStage != null)
                {
                    var tempProf = sysProf.Where(x => x.Int("profId") == profStage.Int("sysProfId")).FirstOrDefault();
                    tempProf = tempProf != null ? tempProf : new BsonDocument();
                    var tempStage = sysStageList.Where(x => x.Int("stageId") == profStage.Int("stageId")).FirstOrDefault();
                    tempStage = tempStage != null ? tempStage : new BsonDocument();
                    var tempFileCat = projFileCatList.Where(x => x.Int("fileCatId") == profStage.Int("fileCatId")).FirstOrDefault();
                    tempFileCat = tempFileCat != null ? tempFileCat : new BsonDocument();
                    profStageStr = string.Format("{0}-{1}-{2}", tempProf.String("name"), tempStage.String("name"), tempFileCat.String("name"));
                }
                #endregion
                string orgId = "";
                string orgName = "";
                #region 获取人所在的部门
                if (ownerUser != null)
                {
                    var userOrgPost = dataOp.FindOneByKeyVal("UserOrgPost", "userId", ownerUser.Text("userId").ToString());
                    if (userOrgPost != null)
                    {
                        var orgPost = dataOp.FindOneByKeyVal("OrgPost", "postId", userOrgPost.Text("postId"));
                        if (orgPost != null)
                        {
                            var org = dataOp.FindOneByKeyVal("Organization", "orgId", orgPost.Text("orgId"));
                            if (org != null)
                            {
                                orgId = org.Text("orgId");
                                orgName = org.Text("name");
                            }
                        }
                    }
                }
                #endregion
                //任务参与人
                List<BsonDocument> joinerList = allManagerList.Where(m => m.Int("taskId") == task.Int("taskId") && m.Int("type") == (int)TaskManagerType.TaskJoiner).ToList();

                string joinerName = "";

                foreach (var joiner in joinerList)
                {
                    BsonDocument joinerUser = allUserList.Where(t => t.Int("userId") == joiner.Int("userId")).FirstOrDefault();

                    if (joinerName == "") joinerName += joinerUser.String("name");
                    else joinerName += "," + joinerUser.String("name");
                }

                BsonDocument createUser = allUserList.Where(t => t.Int("userId") == task.Int("createUser")).FirstOrDefault();
                #endregion

                #region 获取任务文档信息
                //List<int> sysprofIdList = taskFIleDefaultList.Count > 0 ? taskFIleDefaultList.Where(c => c.taskId == task.taskId && c.sysProfId.HasValue).Select(c => c.sysProfId.Value).Distinct().ToList() : new List<int>();

                //List<int> sysstageIdList = taskFIleDefaultList.Count > 0 ? taskFIleDefaultList.Where(c => c.taskId == task.taskId && c.sysStageId.HasValue).Select(c => c.sysStageId.Value).Distinct().ToList() : new List<int>();
                #endregion

                #region 获取技术关系信息
                var tempRelList = taskRelList.Where(t => t.Int("sucTaskId") == task.Int("taskId")).ToList();

                List<object> relationList = new List<object>();

                if (tempRelList.Count > 0)
                {
                    foreach (var tempRel in tempRelList)
                    {
                        int relTaskId = tempRel.Int("preTaskId") == task.Int("taskId") ? tempRel.Int("sucTaskId") : tempRel.Int("preTaskId");

                        var tempRelTask = allProjTaskList.Where(t => t.Int("taskId") == relTaskId).FirstOrDefault();
                        var curPreTaskStatus = tempRelTask.Int("status", 2);
                        if (curPreTaskStatus < (int)TaskStatus.NotStarted)
                        {
                            curPreTaskStatus = (int)TaskStatus.NotStarted;
                        }
                        var curPreStatusName = EnumDescription.GetFieldText((TaskStatus)curPreTaskStatus);
                        double preDelayDay = 0;

                        if (curPreTaskStatus < (int)TaskStatus.Completed && tempRelTask.Date("curEndData") != DateTime.MinValue && tempRelTask.Date("curEndData") < DateTime.Now)//延迟结束
                        {
                            curPreTaskStatus = -1;
                            curPreStatusName = "已延迟";
                            preDelayDay = (DateTime.Now - tempRelTask.Date("curEndData")).TotalDays;
                        }
                        // referType 1：FS（结束-开始） 2：SS（开始-开始） 3：FF（结束-结束） 4：SF（开始-结束）
                        if (tempRelTask != null)
                        {
                            relationList.Add(new
                            {
                                relationId = tempRel.Int("relId"),
                                relTaskId = relTaskId,
                                //relTaskIndex = dicTaskIndex[relTaskId],
                                relTaskName = tempRelTask.Text("name"),
                                relTaskType = tempRel.Int("referType") == 1 ? "FS" : (tempRel.Int("referType") == 2 ? "SS" : (tempRel.Int("referType") == 3 ? "FF" : (tempRel.Int("referType") == 4 ? "SF" : ""))),
                                relTaskStateName = curPreStatusName,
                                relTaskFileCount = allProjFileList.Where(c => c.Int("keyValue") == relTaskId).Count()
                            });
                        }
                    }
                }
                #endregion

                #region 获取其他相关信息
                BsonDocument conDiag = contextDiagramList.Where(t => t.Int("diagramId") == task.Int("diagramId")).FirstOrDefault(); //脉络图
                BsonDocument tempPoint = decisionPointList.Where(t => t.Int("pointId") == task.Int("pointId")).FirstOrDefault(); //地铁图

                List<BsonDocument> flowRelList = allFlowRelList.Where(t => t.Int("taskId") == task.Int("taskId")).ToList();     //任务对应流程关联

                #endregion

                //查找任务状态
                var curTaskStatus = task.Int("status", 2);
                var curStatusName = EnumDescription.GetFieldText((TaskStatus)task.Int("status"));
                double delayDay = 0;
                //if (curTaskStatus < (int)TaskStatus.NotStarted && task.Date("curStartData") != DateTime.MinValue && task.Date("curStartData") < DateTime.Now)//延迟开始
                //{
                //    curTaskStatus = -1;
                //    curStatusName = "已延迟";
                //    delayDay = (DateTime.Now - task.Date("curStartData")).TotalDays;
                //}
                if (curTaskStatus < (int)TaskStatus.Completed && task.Date("curEndData") != DateTime.MinValue && task.Date("curEndData") < DateTime.Now)//延迟结束
                {
                    curTaskStatus = -1;
                    curStatusName = "已延迟";
                    delayDay = (DateTime.Now - task.Date("curEndData")).TotalDays;
                }
                var childTaskActualPaymentAmount = taskList.Where(c => c.Text("nodeKey").StartsWith(task.Text("nodeKey"))).Sum(c => c.Double("actualPaymentAmount"));
                #region 添加到任务信息列表中
                taskListJson.Add(new
                {
                    taskId = task.Int("taskId"),
                    nodePid = task.Int("nodePid") == rootTask.Int("taskId") ? 0 : task.Int("nodePid"),
                    name = task.String("name"),
                    levelId = task.Int("levelId", -1),
                    levelName = task.SourceBsonField("levelId", "name"),

                    nodeTypeId = task.Int("nodeTypeId", -1),
                    nodeName = task.SourceBsonField("nodeTypeId", "name"),
                    ownerName = ownerUser != null ? ownerUser.String("name") : "",
                    ownerUserId = ownerUser != null ? ownerUser.Int("userId") : -1,
                    ownerProfId = ownerManager != null ? ownerManager.Int("profId") : -1,
                    startDate = task.Date("curStartData") != DateTime.MinValue ? task.Date("curStartData").ToString("yyyy-MM-dd") : "",
                    endDate = task.Date("curEndData") != DateTime.MinValue ? task.Date("curEndData").ToString("yyyy-MM-dd") : "",
                    period = task.String("period"),
                    status = curTaskStatus,
                    statusName = curStatusName,
                    remark = task.String("remark"),
                    approvalDepart = task.String("approvalDepart"),
                    startDateBg = task.String("curStartDataBgColor"),
                    endDateBg = task.String("curEndDataBgColor"),
                    factStDate = task.Date("factStartDate") != DateTime.MinValue ? task.Date("factStartDate").ToString("yyyy-MM-dd") : "",
                    factEdDate = task.Date("factEndDate") != DateTime.MinValue ? task.Date("factEndDate").ToString("yyyy-MM-dd") : "",
                    taskRelations = relationList,
                    operateStatus = task.Int("operateStatus"),
                    canPassOperate = task.Int("canPassOperate"),
                    needSplit = task.Int("needSplit"),
                    spliterName = spliterUser != null ? spliterUser.String("name") : "",
                    joinerName = joinerName,
                    createrName = createUser != null ? createUser.String("name") : "",
                    relConDiagId = task.Int("diagramId"),          //关联的脉络图节点Id 0为没有关联
                    relConDiagName = conDiag != null ? conDiag.String("name") : "",                //关联的脉络图节点名称
                    strPointId = task.Int("pointId"),
                    pointName = tempPoint.String("name"),
                    //valueId = taskValueRelationBll.FindValueObjByTaskId(task.taskId).valueId,
                    //valueName = taskValueRelationBll.FindValueObjByTaskId(task.taskId).name,
                    hasApproval = flowRelList.Count > 0 ? true : false,
                    isKeyTask = task.Int("keyTask"),
                    hasCIList = task.String("hasCIList"),
                    hasPush=task.Text("hasPush"),
                    strTextPId = tempPoint.Text("textPId"),
                    orgId = orgId,
                    orgName = orgName,
                    delayDay = (int)delayDay,
                    designUnitName = task.Text("designUnitName"),
                    totalContractAmount = task.Double("totalContractAmount"),
                    payedContractAmount = childTaskActualPaymentAmount != 0 ? string.Format("{0}万元", childTaskActualPaymentAmount) : "",
                    unpayedContractAmount = (task.Double("totalContractAmount") - childTaskActualPaymentAmount) != 0 ? string.Format("{0}万元", task.Double("totalContractAmount") - childTaskActualPaymentAmount) : "",
                    docClass = profStageStr,
                    //sysprofids = ListToString(sysprofIdList),
                    //sysstageids = ListToString(sysstageIdList),
                    //completedWork = task.TaskToDos.Where(m => m.TodoTask.stateId == (int)WorkStatus.Completed).Count(),
                    //unCompletedWork = task.TaskToDos.Where(m => m.TodoTask.stateId != (int)WorkStatus.Completed).Count(),
                    //fileCount = 0,
                });
                #endregion
            }

            //新增计算公式
            List<BsonDocument> formulaList = dataOp.FindAllByKeyValList("XH_DesignManage_TaskFormula", "taskId", taskIdList).ToList();
            var jsonFormulaList = formulaList.Select(m => m.String("formulaParam")).ToList();
            var jsonResult = new
            {
                jsonGroupTaskList = taskListJson,
                jsonFormulaList = jsonFormulaList
            };

            return this.Json(jsonResult, JsonRequestBehavior.AllowGet);
        }
        #endregion
        /// <summary>
        /// 快速创建任务
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult QuickCreateTaskNew()
        {
            InvokeResult result = new InvokeResult();
            PageJson json = new PageJson();

            #region 获取原始数据
            int curTaskId = PageReq.GetFormInt("curTaskId");        //当前任务的上一个任务
            int planId = PageReq.GetFormInt("planId");              //当前任务所属计划
            int projId = PageReq.GetFormInt("projId");              //当前任务所属项目
            int isExpLib = PageReq.GetFormInt("isExpLib");          //当前计划是否经验库
            int nodePid = PageReq.GetFormInt("nodePid");            //当前任务的父级任务

            BsonDocument plan = dataOp.FindOneByKeyVal("XH_DesignManage_Plan", "planId", planId.ToString()); //计划

            List<BsonDocument> taskList = dataOp.FindAllByKeyVal("XH_DesignManage_Task", "planId", planId.ToString()).ToList();      //所有需要展示的任务

            BsonDocument rootTask = dataOp.FindOneByQueryStr("XH_DesignManage_Task", "planId=" + planId + "&nodePid=0");   //内置的根任务

            if ((isExpLib == 0) && (plan.Int("status") == (int)ProjectPlanStatus.Completed))
            {
                json.Success = false;
                json.Message = "计划已完成不能创建任务";
                return Json(json);
            }
            #endregion

            #region 基本信息
            BsonDocument taskInfo = new BsonDocument();

            taskInfo.Add("name", "请填写目录名称");         //名称
            taskInfo.Add("planId", planId.ToString());      //所属计划
            taskInfo.Add("projId", projId.ToString());      //所属项目
            taskInfo.Add("progress", "0");
            taskInfo.Add("levelId ", " 2");                  //关注级别
            taskInfo.Add("curEndData ", plan.Date("endData") != DateTime.MinValue ? plan.String("endData") : DateTime.Now.ToString("yyyy-MM-dd"));  //任务计划结束日期
            taskInfo.Add("status", ((int)TaskStatus.NotStarted).ToString());    //任务状态

            if (nodePid != 0)
            {
                BsonDocument parentTask = dataOp.FindOneByKeyVal("XH_DesignManage_Task", "taskId", nodePid.ToString());

                taskInfo.Add("stageId", parentTask.String("stageId"));
                taskInfo.Add("nodePid", nodePid.ToString());
            }
            else
            {
                taskInfo.Add("stageId", rootTask.String("stageId"));
                taskInfo.Add("nodePid", rootTask.String("taskId"));
            }
            #endregion

            if (taskInfo.Int("nodePid") <= 0)
            {
                json.Success = false;
                json.Message = "目录异常，请刷新页面重试";
                return Json(json);
            }

            result = dataOp.Insert("XH_DesignManage_Task", taskInfo);

            json = TypeConvert.InvokeResultToPageJson(result);

            if (result.Status == Status.Successful)
            {
                BsonDocument task = result.BsonInfo;

                #region 移动任务
                if (curTaskId != 0)
                {
                    dataOp.Move("XH_DesignManage_Task", task.String("taskId"), curTaskId.ToString(), "next");
                }
                #endregion

                #region 添加返回任务信息
                object taskJson = new
                {
                    taskId = task.Int("taskId"),
                    nodePid = task.Int("nodePid") == rootTask.Int("taskId") ? 0 : task.Int("nodePid"),
                    name = task.String("name"),
                    levelId = task.Int("levelId", -1),
                    levelName = "",
                    ownerName = "",
                    ownerUserId = -1,
                    ownerProfId = -1,
                    startDate = "",
                    endDate = task.Date("curEndData") != DateTime.MinValue ? task.Date("curEndData").ToString("yyyy-MM-dd") : "",
                    period = "",
                    status = task.Int("status"),
                    statusName = EnumDescription.GetFieldText((TaskStatus)task.Int("status")),
                    remark = "",
                    approvalDepart = "",
                    startDateBg = "",
                    endDateBg = "",
                    factStDate = "",
                    factEdDate = "",
                    taskRelations = new List<object>(),
                    operateStatus = 0,
                    canPassOperate = 0,
                    needSplit = 0,
                    spliterName = "",
                    joinerName = "",
                    createrName = dataOp.GetCurrentUserName(),
                };

                json.htInfo.Add("taskInfo", taskJson);
                #endregion
            }

            return Json(json);
        }
        #endregion

        #region ZHHY创建自由发起流程任务并关联流程

        public ActionResult CreateFreeTaskWithFlow_ZHHY()
        {
            InvokeResult result = new InvokeResult() { Status = Status.Failed };
            var projId = PageReq.GetForm("projId");
            var flowId = PageReq.GetForm("flowId");
            var taskName = PageReq.GetForm("taskName");

            #region 参数验证
            var projObj = dataOp.FindAllByQuery("XH_DesignManage_Project",
                    Query.And(
                            Query.EQ("projId", projId),
                            Query.EQ("isProj", "1")
                    )
            ).FirstOrDefault();

            if (projObj.IsNullOrEmpty())
            {
                result.Message = "无效的项目";
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }

            var flowObj = dataOp.FindAllByQuery("BusFlow",
                        Query.And(
                                Query.EQ("flowId", flowId),
                                Query.EQ("isActive", "1")
                        )
                ).FirstOrDefault();
            if (flowObj.IsNullOrEmpty())
            {
                result.Message = "无效的流程";
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            if (string.IsNullOrWhiteSpace(taskName))
            {
                result.Message = "无效的任务名称";
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            #endregion
            
            BsonDocument newFreeTaskDoc = new BsonDocument();
            newFreeTaskDoc.Add("name", taskName);
            newFreeTaskDoc.Add("projId", projId);
            result = dataOp.Insert("FreeFlow_FreeTask", newFreeTaskDoc);
            if (result.Status == Status.Failed)
            {
                result.Message = "插入自由任务失败";
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            var newFreeTask = result.BsonInfo;

            BsonDocument newApprovalDoc = new BsonDocument();
            newApprovalDoc.Add("flowId", flowId);
            newApprovalDoc.Add("projId", projId);
            newApprovalDoc.Add("name", taskName);
            newApprovalDoc.Add("tableName", "FreeFlow_FreeTask");
            newApprovalDoc.Add("keyName", "freeTaskId");
            newApprovalDoc.Add("keyValue", newFreeTask.Text("freeTaskId"));
            var tempResult = dataOp.Insert("FreeFlow_FreeApproval", newApprovalDoc);
            if (tempResult.Status == Status.Failed)
            {
                tempResult.Message = "插入审批任务关联失败";
                return Json(TypeConvert.InvokeResultToPageJson(tempResult));
            }
            var newApproval = tempResult.BsonInfo;

            BsonDocument newFreeTaskRefUserDoc = new BsonDocument();
            newFreeTaskRefUserDoc.Add("freeTaskId", newFreeTask.Text("freeTaskId"));
            newFreeTaskRefUserDoc.Add("userId", dataOp.GetCurrentUserId().ToString());
            newFreeTaskRefUserDoc.Add("userType", "0");
            result = dataOp.Insert("FreeFlow_FreeTaskRefUser", newFreeTaskRefUserDoc);
            if (result.Status == Status.Failed)
            {
                result.Message = "插入用户任务关联失败";
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }

            var flowFileRequests = dataOp.FindAllByQuery("FlowFileRequest",
                        Query.EQ("flowId", flowId)
                );
            List<StorageData> dataList = new List<StorageData>();
            foreach (var request in flowFileRequests)
            {
                StorageData data = new StorageData();
                data.Document = new BsonDocument().Add("requestId", request.Text("requestId"))
                    .Add("tableName", "FreeFlow_FreeApproval")
                    .Add("keyName", "freeApprovalId")
                    .Add("keyValue", newApproval.Text("freeApprovalId"));
                data.Name = "FlowApprovalFile";
                data.Type = StorageType.Insert;
                dataList.Add(data);
            }
            result = dataOp.BatchSaveStorageData(dataList);
            if (result.Status == Status.Failed)
            {
                result.Message = "插入自由任务审批附件要求失败";
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }

            return Json(TypeConvert.InvokeResultToPageJson(tempResult));
        }
        #endregion

        /****************************袁辉2013/9/2*******************************/
        #region ZHTZ材料评分
        public int SaveEvaluation()
        {
            string matRelId = Request.Form["matRelId"];
            var evalList = dataOp.FindAllByKeyVal("ProjMaterialEvaluation", "matRelId", matRelId).ToList();

            if (evalList.Count <= 0)
                return -1;

            foreach (var item in evalList)
            {
                dataOp.Update("ProjMaterialEvaluation", Query.EQ("evalId", item.String("evalId")), new BsonDocument().Add("evaluation", Request.Form[item.String("evaluator")]));
            }
            return 0;
        }

        #endregion

        #region QX保存最近浏览工程列表
        /// <summary>
        /// 保存最近浏览工程列表
        /// 如果传入的工程名为空或0则返回最近浏览工程列表
        /// </summary>
        /// <param name="projEngId"></param>
        /// <param name="length"></param>
        /// <param name="sessionName"></param>
        /// <returns></returns>
        public ActionResult SaveRecentProjEng(string projEngId, int length, string sessionName)
        {
            var recBll = GetRecentBll._();
            var userId = PageReq.GetParamInt("userId");
            if (userId == 0)
            {
                userId = dataOp.GetCurrentUserId();
            }
            var result = recBll.SaveRecent(projEngId, length, userId, sessionName);
            if (result.Status == Status.Failed)
            {
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            var recentList = recBll.GetRecentProjEngList(userId, length, sessionName).ToList();
            List<Hashtable> retList = new List<Hashtable>();
            foreach (var tempDoc in recentList)
            {
                if (!tempDoc.IsNullOrEmpty())
                {
                    tempDoc.Remove("_id");
                    retList.Add(tempDoc.ToHashtable());
                }
            }
            return Json(retList, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region QX保存最近浏览的设计单位/编制单位
        public ActionResult SaveRecentSupplier(string supplierId, int length, string sessionName)
        {
            //InvokeResult result = new InvokeResult() { Status = Status.Successful };
            //const int defaultLength = 6;
            //var maxLength = length < 1 ? defaultLength : length;
            //var curSupplierList = Session[sessionName] as List<string> ?? new List<string>();
            //try
            //{
            //    if (curSupplierList.Contains(supplierId))
            //    {
            //        curSupplierList.Remove(supplierId);
            //        curSupplierList.Insert(0, supplierId);
            //    }
            //    else
            //    {
            //        curSupplierList.Insert(0, supplierId);
            //    }
            //    if (curSupplierList.Count() > maxLength)
            //    {
            //        curSupplierList.RemoveRange(maxLength, curSupplierList.Count() - maxLength);
            //    }
            //}
            //catch (Exception ex)
            //{
            //    result.Status = Status.Failed;
            //    result.Message = ex.Message;
            //    return Json(TypeConvert.InvokeResultToPageJson(result));
            //}
            //Session[sessionName] = curSupplierList;
            //List<Hashtable> retList = new List<Hashtable>();
            //var allDocList = dataOp.FindAllByKeyValList("XH_Supplier_Supplier", "supplierId", curSupplierList).ToList();
            //foreach (var id in curSupplierList)
            //{
            //    var tempDoc = allDocList.Where(p => p.Text("supplierId") == id).FirstOrDefault();
            //    tempDoc.Remove("_id");
            //    retList.Add(tempDoc.ToHashtable());
            //}
            //return Json(retList, JsonRequestBehavior.AllowGet);



            var recBll = GetRecentBll._();
            var userId = PageReq.GetParamInt("userId");
            if (userId == 0)
            {
                userId = dataOp.GetCurrentUserId();
            }
            var result = recBll.SaveRecent(supplierId, length, userId, sessionName);
            if (result.Status == Status.Failed)
            {
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            var recentList = recBll.GetRecentSupplierList(userId, length, sessionName).ToList();
            List<Hashtable> retList = new List<Hashtable>();
            foreach (var tempDoc in recentList)
            {
                if (!tempDoc.IsNullOrEmpty())
                {
                    tempDoc.Remove("_id");
                    retList.Add(tempDoc.ToHashtable());
                }
            }
            return Json(retList, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region 通过地块id或项目id获取关联工程列表
        /// <summary>
        /// 通过地块id或项目id获取关联工程列表
        /// 用于QX设计变更首页
        /// </summary>
        /// <returns></returns>
        public JsonResult DesignChangeGetProjEng()
        {
            var projId = PageReq.GetParam("projId");
            var engId = PageReq.GetParam("engId");
            var keyWord = PageReq.GetParam("keyword");

            var projEngRelList = new List<BsonDocument>();
            var projEngList = new List<BsonDocument>();
            if (!string.IsNullOrEmpty(projId))
            {
                projEngRelList = dataOp.FindAllByQuery("XH_DesignManage_ProjEngRelation", Query.EQ("projId", projId)).ToList();
            }
            else if (!string.IsNullOrEmpty(engId))
            {
                projEngRelList = dataOp.FindAllByQuery("XH_DesignManage_ProjEngRelation", Query.EQ("engId", engId)).ToList();
            }
            projEngList = dataOp.FindAllByQuery("XH_DesignManage_ProjEngineering",
                    Query.In("projEngId", projEngRelList.Select(p => p.GetValue("projEngId")))
                ).OrderBy(p => p.Text("nodeKey")).ToList();

            List<Hashtable> retList = new List<Hashtable>();
            foreach (var projEng in projEngList)
            {
                var tempDoc = projEng;
                tempDoc.Remove("_id");
                retList.Add(tempDoc.ToHashtable());
            }
            return Json(retList, JsonRequestBehavior.AllowGet);
        }
        #endregion



        /***********************************end*********************************/
        #region 任务下多审批时，创建隐藏子任务的方法
        /// <summary>
        /// 快速创建多审批任务的隐藏子任务
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult MutilApprovalTaskAdd()
        {
            InvokeResult result = new InvokeResult();
            PageJson json = new PageJson();

            #region 获取原始数据
            int curTaskId = PageReq.GetParamInt("taskId");        //当前任务的上一个任务
            var curTaskObj = dataOp.FindOneByQuery("XH_DesignManage_Task", Query.EQ("taskId", curTaskId.ToString()));
            var taskClassId = PageReq.GetParam("taskClassId");
            BsonDocument plan = dataOp.FindOneByKeyVal("XH_DesignManage_Plan", "planId", curTaskObj.Text("planId")); //计划

            List<BsonDocument> taskList = dataOp.FindAllByKeyVal("XH_DesignManage_Task", "planId", curTaskObj.Text("planId")).ToList();      //所有需要展示的任务

            BsonDocument rootTask = dataOp.FindOneByQueryStr("XH_DesignManage_Task", "planId=" + curTaskObj.Text("planId") + "&nodePid=0");   //内置的根任务

            if (plan.Int("status") == (int)ProjectPlanStatus.Completed)
            {
                json.Success = false;
                json.Message = "计划已完成不能再设置任务审批";
                return Json(json);
            }
            #endregion

            #region 基本信息
            BsonDocument taskInfo = new BsonDocument();
            if (curTaskObj != null)
            {
                taskInfo.Add("taskClassId", taskClassId);
                taskInfo.Add("hiddenTask", "1");         //隐藏标记，在计划页面不展示
                taskInfo.Add("name", curTaskObj.Text("name"));         //名称
                taskInfo.Add("planId", curTaskObj.Text("planId"));      //所属计划
                taskInfo.Add("projId", curTaskObj.Text("projId"));      //所属项目
                taskInfo.Add("progress", "0");
                taskInfo.Add("levelId ", " 2");                  //关注级别
                taskInfo.Add("curEndData ", plan.Date("endData") != DateTime.MinValue ? plan.String("endData") : DateTime.Now.ToString("yyyy-MM-dd"));  //任务计划结束日期
                taskInfo.Add("status", ((int)TaskStatus.NotStarted).ToString());    //任务状态
                taskInfo.Add("stageId", curTaskObj.String("stageId"));
                taskInfo.Add("nodePid", curTaskObj.Text("taskId")); //为当前任务的隐藏子任务
            }
            #endregion

            if (taskInfo.Int("nodePid") <= 0)
            {
                json.Success = false;
                json.Message = "计划异常，请刷新页面重试";
                return Json(json);
            }

            result = dataOp.Insert("XH_DesignManage_Task", taskInfo);
            json = TypeConvert.InvokeResultToPageJson(result);
            return Json(json);
        }
        #endregion

        #region 设计费 创建隐藏设计费下挂的任务
        /// <summary>
        /// 快速创建任务
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult QuickSaveHiddenTaskFree()
        {
            InvokeResult result = new InvokeResult();
            PageJson json = new PageJson();

            #region 获取原始数据
            int planId = PageReq.GetFormInt("planId");              //当前任务所属计划
            int projId = PageReq.GetFormInt("projId");              //当前任务所属项目
            int isExpLib = PageReq.GetFormInt("isExpLib");          //当前计划是否经验库
            int nodePid = PageReq.GetFormInt("nodePid");            //当前任务的父级任务
            var taskId = PageReq.GetForm("editTaskId");
            string expectPaymentAmount = PageReq.GetForm("expectPaymentAmount");            //本次应付金额
            string actualPaymentAmount = PageReq.GetForm("actualPaymentAmount");            //实际应付金额
            string paymentRatio = PageReq.GetForm("paymentRatio");            //支付比例
            string payeeCompanyName = PageReq.GetForm("payeeCompanyName");            //收款单位
            string remark = PageReq.GetForm("remark");            //支付理由

            BsonDocument plan = dataOp.FindOneByKeyVal("XH_DesignManage_Plan", "planId", planId.ToString()); //计划

            List<BsonDocument> taskList = dataOp.FindAllByKeyVal("XH_DesignManage_Task", "planId", planId.ToString()).ToList();      //所有需要展示的任务

            BsonDocument rootTask = dataOp.FindOneByQueryStr("XH_DesignManage_Task", "planId=" + planId + "&nodePid=0");   //内置的根任务

            BsonDocument editTask = dataOp.FindOneByKeyVal("XH_DesignManage_Task", "taskId", taskId);   //当前任务节点

            if ((isExpLib == 0) && (plan.Int("status") == (int)ProjectPlanStatus.Completed))
            {
                json.Success = false;
                json.Message = "计划已完成不能创建任务";
                return Json(json);
            }
            #endregion
            if (editTask == null)
            {
                #region 基本信息
                BsonDocument taskInfo = new BsonDocument();
                taskInfo.Add("name", PageReq.GetForm("name"));         //名称
                taskInfo.Add("planId", planId.ToString());      //所属计划
                taskInfo.Add("projId", projId.ToString());      //所属项目
                taskInfo.Add("progress", "0");
                taskInfo.Add("levelId", " 2");                  //关注级别
                taskInfo.Add("itemId", PageReq.GetForm("itemId"));       //任务所在的设计分类
                taskInfo.Add("curEndData", plan.Date("endData") != DateTime.MinValue ? plan.String("endData") : DateTime.Now.ToString("yyyy-MM-dd"));  //任务计划结束日期
                taskInfo.Add("status", ((int)TaskStatus.NotStarted).ToString());    //任务状态

                taskInfo.Add("expectPaymentAmount", expectPaymentAmount);
                taskInfo.Add("actualPaymentAmount", actualPaymentAmount);
                taskInfo.Add("paymentRatio", paymentRatio);
                taskInfo.Add("payeeCompanyName", payeeCompanyName);
                taskInfo.Add("remark", remark);
                taskInfo.Add("paymentDate", PageReq.GetForm("paymentDate").ToString());  //付款时间
                taskInfo.Add("isDesignFreeTask", "1");  //设计费隐藏任务的标记
                if (nodePid != 0)
                {
                    BsonDocument parentTask = dataOp.FindOneByKeyVal("XH_DesignManage_Task", "taskId", nodePid.ToString());

                    taskInfo.Add("stageId", parentTask.String("stageId"));
                    taskInfo.Add("nodePid", nodePid.ToString());
                }
                else
                {
                    taskInfo.Add("stageId", rootTask.String("stageId"));
                    taskInfo.Add("nodePid", rootTask.String("taskId"));
                }
                #endregion

                if (taskInfo.Int("nodePid") <= 0)
                {
                    json.Success = false;
                    json.Message = "计划异常，请刷新页面重试";
                    return Json(json);
                }

                result = dataOp.Insert("XH_DesignManage_Task", taskInfo);
                if (result.Status == Status.Successful)
                { //创建任务后添加任务负责人
                    BsonDocument taskManagerInfo = new BsonDocument();
                    taskManagerInfo.Add("profId", "0");
                    taskManagerInfo.Add("type", "1");
                    taskManagerInfo.Add("userId", PageReq.GetForm("userId"));
                    taskManagerInfo.Add("taskId", result.BsonInfo.Text("taskId"));
                    dataOp.Insert("XH_DesignManage_TaskManager", taskManagerInfo);
                }
            }
            else
            {
                BsonDocument updateBson = new BsonDocument();
                updateBson.Add("name", PageReq.GetForm("name"));         //名称
                updateBson.Add("expectPaymentAmount", expectPaymentAmount);
                updateBson.Add("actualPaymentAmount", actualPaymentAmount);
                updateBson.Add("paymentRatio", paymentRatio);
                updateBson.Add("payeeCompanyName", payeeCompanyName);
                updateBson.Add("remark", remark);
                updateBson.Add("paymentDate", PageReq.GetForm("paymentDate"));  //付款时间
                updateBson.Add("isDesignFreeTask", "1");  //设计费隐藏任务的标记
                result = dataOp.Update(editTask, updateBson);
                if (result.Status == Status.Successful)
                { //创建任务后添加任务负责人
                    BsonDocument editTaskManager = dataOp.FindOneByKeyVal("XH_DesignManage_TaskManager", "taskId", taskId);   //当前任务负责人
                    if (editTaskManager != null)
                    {
                        BsonDocument updateManagerBson = new BsonDocument();
                        updateManagerBson.Add("userId", PageReq.GetForm("userId"));
                        dataOp.Update(editTaskManager, updateManagerBson); //更新任务负责人
                    }
                    else
                    {
                        BsonDocument uptaskManagerInfo = new BsonDocument();
                        uptaskManagerInfo.Add("profId", "0");
                        uptaskManagerInfo.Add("type", "1");
                        uptaskManagerInfo.Add("userId", PageReq.GetForm("userId"));
                        uptaskManagerInfo.Add("taskId", result.BsonInfo.Text("taskId"));
                        dataOp.Insert("XH_DesignManage_TaskManager", uptaskManagerInfo);
                    }

                }
            }
            json = TypeConvert.InvokeResultToPageJson(result);
            return Json(json);
        }
        #endregion

        #region ZHTZ移动流程顺序
        public JsonResult BusFlowMoveByTaskLevel()
        {
            InvokeResult result = new InvokeResult() { Status = Status.Successful };
            var flowId = PageReq.GetParam("flowId");
            var type = PageReq.GetParamInt("type");//type  0:上移  1:下移
            var flowObj = dataOp.FindOneByQuery("BusFlow", Query.EQ("flowId", flowId));
            if (flowObj == null)
            {
                result.Status = Status.Failed;
                result.Message = "未找到当前流程";
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            var dst_flowObj = new BsonDocument();
            if (type == 0)
            {
                dst_flowObj = dataOp.FindAll("BusFlow")
                    .Where(p => p.Text("taskLevelId") == flowObj.Text("taskLevelId"))
                    .Where(p => p.Int("order") < flowObj.Int("order"))
                   .OrderByDescending(p => p.Int("order"))
                   .FirstOrDefault();
            }
            else if (type == 1)
            {
                dst_flowObj = dataOp.FindAll("BusFlow")
                    .Where(p => p.Text("taskLevelId") == flowObj.Text("taskLevelId"))
                    .Where(p => p.Int("order") > flowObj.Int("order"))
                    .OrderBy(p => p.Int("order"))
                    .FirstOrDefault();
            }
            if (dst_flowObj != null)
            {
                BsonDocument doc1 = new BsonDocument().Add("order", flowObj.Text("order"));
                BsonDocument doc2 = new BsonDocument().Add("order", dst_flowObj.Text("order"));
                result = dataOp.Update("BusFlow", Query.EQ("flowId", dst_flowObj.Text("flowId")), doc1);

                if (result.Status == Status.Successful)
                {
                    result = dataOp.Update("BusFlow", Query.EQ("flowId", flowObj.Text("flowId")), doc2);
                }
            }


            return Json(TypeConvert.InvokeResultToPageJson(result));
        }
        #endregion

        #region ZHHY获取地区公司-工程-项目树XML
        public ActionResult GetComEngProjTreeXML(string comTb, string engTb, string projTb, int comId)
        {
            var comList = dataOp.FindAll(comTb).ToList();
            if (comTb == "DesignManage_Company") comList = comList.Where(p => p.Text("isGroup") != "1").ToList();
            if (comId != 0) comList = comList.Where(p => p.Int("comId") == comId).ToList();
            List<TreeNode> comNodeList = new List<TreeNode>();
            foreach (var com in comList)
            {
                var engList = dataOp.FindAllByQuery(engTb, Query.EQ("comId", com.Text("comId"))).ToList();
                TreeNode comNode = new TreeNode();
                comNode.Id = com.Int("comId");
                comNode.Name = com.Text("name");
                comNode.Lv = 1;
                comNode.Pid = 0;
                comNode.underTable = com.Text("underTable");
                comNode.Param = "com";
                comNode.IsLeaf = 0;
                List<TreeNode> engNodeList = new List<TreeNode>();
                foreach (var eng in engList)
                {
                    var projList = dataOp.FindAllByQuery(projTb, Query.EQ("engId", eng.Text("engId"))).ToList();
                    TreeNode engNode = new TreeNode();
                    engNode.Id = eng.Int("engId");
                    engNode.Name = eng.String("name");
                    engNode.Lv = 2;
                    engNode.Pid = 0;
                    engNode.underTable = eng.String("underTable");
                    engNode.Param = "eng";
                    engNode.IsLeaf = 0;
                    List<TreeNode> projNodeList = new List<TreeNode>();
                    foreach (var proj in projList)
                    {
                        TreeNode projNode = new TreeNode();
                        projNode.Id = proj.Int("projId");
                        projNode.Name = proj.String("name");
                        projNode.Lv = 3;
                        projNode.Pid = 0;
                        projNode.underTable = proj.String("underTable");
                        projNode.Param = "proj";
                        projNode.IsLeaf = 1;
                        projNode.SubNodes = new List<TreeNode>();
                        projNodeList.Add(projNode);
                    }
                    engNode.SubNodes = projNodeList;
                    engNodeList.Add(engNode);
                }
                comNode.SubNodes = engNodeList;
                comNodeList.Add(comNode);
            }
            return new XmlTree(comNodeList);
        }
        #endregion

        #region ZHHY通过ID获取用户岗位
        public string GetUserPostById()
        {
            var idList = PageReq.GetParamList("userId");
            if (idList.Count() == 0)
            {
                idList = PageReq.GetFormList("userId");
            }
            var userList = dataOp.FindAllByQuery("SysUser", Query.In("userId", idList.Select(p => (BsonValue)p)));
            var allPostRel = dataOp.FindAllByQuery("UserOrgPost", Query.In("userId", userList.Select(p => (BsonValue)p.Text("userId"))));
            var allPosition = dataOp.FindAllByQuery("OrgPost", Query.In("postId", allPostRel.Select(p => (BsonValue)p.Text("postId"))));
            return string.Join(",", allPosition.Select(p => p.Text("name")));
        }
        #endregion

        #region ZHHY判断计划任务是否可以开始、完成
        /// <summary>
        /// 是否可以开始任务
        /// </summary>
        /// <param name="taskId"></param>
        /// <returns></returns>
        public JsonResult ZHHY_CanStartTask(string taskId)
        {
            //判断是否能开始任务
            PageJson json = new PageJson();
            json.Success = true;
            InvokeResult<bool> tempResult = ZHHY_IsAllPreTaskFinished(taskId);
            if (tempResult.Value == false)
            {
                json.Success = false;
                json.Message += tempResult.Message;
            }
            return Json(json);
        }
        /// <summary>
        /// ZHHY判断当前任务前置任务是否都已经完成
        /// 只有前置任务都完成了当前任务才能开始
        /// </summary>
        /// <param name="taskId"></param>
        /// <returns></returns>
        public InvokeResult<bool> ZHHY_IsAllPreTaskFinished(string taskId)
        {
            InvokeResult<bool> result = new InvokeResult<bool>() { Status = Status.Successful, Value = true };
            var allPreTaskRels = dataOp.FindAllByQuery("XH_DesignManage_TaskRelation",
                    Query.And(
                        Query.EQ("referType", "1"),
                        Query.EQ("sucTaskId", taskId)
                    )
                ).Select(i => i.Text("preTaskId")).ToList();
            var allPreTasks = dataOp.FindAllByQuery("XH_DesignManage_Task",
                    Query.In("taskId", allPreTaskRels.Select(i => (BsonValue)i.ToString()))
                ).Distinct().ToList();
            if (allPreTasks.Any(i=>!i.IsNullOrEmpty()))
            {
                var notCompleteTasks = allPreTasks.Where(i => i.Int("status") != (int)TaskStatus.Completed).ToList();
                if (notCompleteTasks.Any(i=>!i.IsNullOrEmpty()))
                {
                    result.Status = Status.Successful;
                    result.Value = false;
                    result.Message = "前置任务： " + string.Join(", ", notCompleteTasks.Select(i => i.Text("name"))) + " 尚未完成";
                    return result;
                }
                result.Status = Status.Successful;
                result.Value = true;
                return result;
            }
            else
            {
                result.Status = Status.Successful;
                result.Value = true;
                return result;
            }
        }
        /// <summary>
        /// ZHHY判断计划任务是否可以完成
        /// </summary>
        /// <param name="taskId"></param>
        /// <returns></returns>
        public JsonResult ZHHY_CanFinishTask(string taskId)
        {
            InvokeResult<bool> result = new InvokeResult<bool>() { Status = Status.Successful };
            result=ZHHY_IsTaskExist(taskId);
            if (result.Value == false)
            {
                result.Status = Status.Failed;
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }

            //如果是非关键节点，则不进行文件上传以及审批状态判断
            var tempResult = ZHHY_IsNotPointTask(taskId);
            if (tempResult.Status == Status.Failed)
            {
                result.Status = Status.Failed;
                result.Message = tempResult.Message;
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            else if (tempResult.Value == true)//如果是非关键节点则通过
            {
                result.Status = Status.Successful;
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }

            tempResult = ZHHY_IsTaskFileUploaded(taskId);
            if (tempResult.Value == false)
            {
                result.Status = Status.Failed;
                result.Message = tempResult.Message;
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            tempResult=ZHHY_IsTaskFinishApproval(taskId);
            if (tempResult.Value == false)
            {
                result.Status = Status.Failed;
                result.Message = tempResult.Message;
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            result.Status = Status.Successful;
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }
        /// <summary>
        /// 判断是否是非关键任务（关联地铁图）
        /// 非关键节点可以自由开始完成
        /// </summary>
        /// <param name="taskId"></param>
        /// <returns></returns>
        protected InvokeResult<bool> ZHHY_IsNotPointTask(string taskId)
        {
            InvokeResult<bool> result = new InvokeResult<bool>() { Status = Status.Successful };
            var tempResult=ZHHY_IsTaskExist(taskId);
            if (tempResult.Status == Status.Failed)
            {
                result.Status = Status.Failed;
                result.Value = false;
                return result;
            }
            var taskObj = tempResult.BsonInfo;
            //查找地铁图节点
            var point = dataOp.FindOneByQuery("XH_DesignManage_DecisionPoint", Query.EQ("pointId", taskObj.Text("pointId")));
            if (point.IsNullOrEmpty())
            {
                result.Value = true;
                return result;
            }
            else
            {
                result.Value = false;
            }
            return result;
        }
        protected InvokeResult<bool> ZHHY_IsTaskExist(string taskId)
        {
            InvokeResult<bool> result = new InvokeResult<bool>() { Value=false, BsonInfo = new BsonDocument() };
            var taskObj = dataOp.FindAllByQuery("XH_DesignManage_Task", Query.EQ("taskId", taskId)).FirstOrDefault();
            if (taskObj.IsNullOrEmpty())
            {
                result.Status = Status.Failed;
                result.Value = false;
                result.Message = "未能找到该任务";
                return result;
            }
            result.Status = Status.Successful;
            result.Value = true;
            result.BsonInfo = taskObj;
            return result;
        }
        protected InvokeResult<bool> ZHHY_IsTaskFileUploaded(string taskId)
        {
            InvokeResult<bool> result = new InvokeResult<bool>() { Value = true };
            var tempResult=ZHHY_IsTaskExist(taskId);
            if (tempResult.Value == false)
            {
                result.Status = Status.Failed;
                result.Value = false;
                result.Message = tempResult.Message;
                return result;
            }
            var fileRelList = dataOp.FindAllByQuery("FileRelation",
                  Query.And(
                      Query.EQ("tableName", "XH_DesignManage_Task"),
                      Query.EQ("fileObjId", "31"),
                      Query.EQ("keyValue", taskId)
                  )
              ).ToList();
            var forceDeliverCombine = dataOp.FindAllByQuery("Scheduled_deliver",
                Query.And(
                  Query.EQ("taskId", taskId),
                  Query.EQ("isForce", "1")
                )
              ).ToList();
            foreach (var combine in forceDeliverCombine)
            {
                var allFiles = dataOp.FindAllByQuery("FileLibrary",
                    Query.And(
                        Query.In("fileId", fileRelList.Select(p => (BsonValue)p.Text("fileId"))),
                        Query.EQ("Property_deliverId", combine.Text("deliverId"))
                    )
                ).ToList();
                if (allFiles.Count() <= 0)
                {
                    result.Value = false;
                    result.Message = "任务交付物尚未全部上传";
                    return result;
                }
            }
            return result;
        }
        protected InvokeResult<bool> ZHHY_IsTaskFinishApproval(string taskId)
        {
            InvokeResult<bool> result = new InvokeResult<bool>() { Value = true };
            var tempResult=ZHHY_IsTaskExist(taskId);
            if (tempResult.Value==false)
            {
                result.Status = Status.Failed;
                result.Message = tempResult.Message;
                result.Value = false;
                return result;
            }
            var taskObj = tempResult.BsonInfo;
            if (taskObj.Int("nodeTypeId") == 7)
            {
                return ZHHY_IsTaskFinishMultiApproval(taskId);
            }
            else
            {
                return ZHHY_IsTaskFinishSingleApproval(taskId);
            }
        }
        protected InvokeResult<bool> ZHHY_IsTaskFinishSingleApproval(string taskId)
        {
            InvokeResult<bool> result = new InvokeResult<bool>() { Value = true };
            var tempResult=ZHHY_IsTaskExist(taskId);
            if (tempResult.Value == false)
            {
                result.Status = Status.Failed;
                result.Message = tempResult.Message;
                result.Value = false;
                return result;
            }
            var curFlowRel = dataOp.FindAllByQuery("XH_DesignManage_TaskBusFlow", Query.EQ("taskId", taskId)).FirstOrDefault();
            if (curFlowRel.IsNullOrEmpty())
            {
                result.Value = true;//如果没关联审批流程则当做完成审批
                return result;
            }
            var curFlowInstance = dataOp.FindAllByQuery("BusFlowInstance",
                  Query.And(
                      Query.EQ("tableName", "XH_DesignManage_Task"),
                      Query.EQ("referFieldName", "taskId"),
                      Query.EQ("referFieldValue", taskId)
                  )
              ).FirstOrDefault();
            if (curFlowInstance == null || curFlowInstance.Int("instanceStatus") != 1)
            {
                result.Value = false;
                result.Message = "该任务尚未通过审批";
                return result;
            }
            result.Value = true;
            return result;
        }
        protected InvokeResult<bool> ZHHY_IsTaskFinishMultiApproval(string taskId)
        {
            InvokeResult<bool> result = new InvokeResult<bool>() { Value = true };
            var tempResult=ZHHY_IsTaskExist(taskId);
            if (tempResult.Value == false)
            {
                result.Status = Status.Failed;
                result.Message = tempResult.Message;
                result.Value = false;
                return result;
            }
            var taskObj = tempResult.BsonInfo;
            var taskClassList = dataOp.FindAllByQuery("TaskClass", Query.EQ("taskId", taskId)).ToList();
            var allRefTask = dataOp.FindAllByQuery("XH_DesignManage_Task",
                    Query.In("taskClassId", taskClassList.Select(p => (BsonValue)p.Text("taskClassId")))
                ).Select(p => new
                {
                    taskClassId = p.Text("taskClassId"),
                    taskId = p.Text("taskId"),
                    taskName = p.Text("name")
                }).ToList();
            var allRefTaskBusFlow = dataOp.FindAllByQuery("XH_DesignManage_TaskBusFlow",
                    Query.In("taskId", allRefTask.Select(p => (BsonValue)p.taskId))
                ).Select(p => new
                {
                    taskId = p.Text("taskId"),
                    flowId = p.Text("flowId")
                }).ToList();
            foreach (var taskClass in taskClassList)
            {
                //是否是可选流程步骤
                bool isOptionalClass = taskClass.Text("status") == "1" ? false : true;
                var curClassTaskList = allRefTask.Where(p => p.taskClassId == taskClass.Text("taskClassId"));

                #region 步骤唯一
                if (!isOptionalClass)
                {
                    if (curClassTaskList.Count() == 0)
                    {
                        result.Value = false;
                        result.Message = string.Format("序号{0}尚未选择审批流程", taskClass.Text("name"));
                        return result;
                    }
                    foreach (var task in curClassTaskList)
                    {
                        var taskBusFlow = allRefTaskBusFlow.Where(p => p.taskId == task.taskId).FirstOrDefault();
                        if (taskBusFlow == null)
                        {
                            result.Value = false;
                            result.Message = string.Format("序号{0}所选审批尚未关联流程", taskClass.Text("name"));
                            return result;
                        }
                        if (ZHHY_IsTaskFinishSingleApproval(task.taskId).Value==false)
                        {
                            result.Value = false;
                            result.Message = string.Format("序号{0}尚未通过审批", taskClass.Text("name"));
                            return result;
                        }
                    }
                }
                #endregion
                #region 步骤不唯一
                else
                {
                    var defaultTaskId = taskClass.Text("defaluTask");
                    bool hasDefaultTask = string.IsNullOrEmpty(defaultTaskId) ? false : true;
                    if (hasDefaultTask)
                    {
                        var defaultTaskObj = allRefTask.Where(p => p.taskId == defaultTaskId).FirstOrDefault();
                        if (defaultTaskObj == null)
                        {
                            result.Value = false;
                            result.Message = string.Format("序号{0}未能找到关联审批流程", taskClass.Text("name"));
                            return result;
                        }
                        var defaultTaskFlowRel = allRefTaskBusFlow.Where(p => p.taskId == defaultTaskObj.taskId).FirstOrDefault();
                        if (defaultTaskFlowRel == null)
                        {
                            result.Value = false;
                            result.Message = string.Format("序号{0}所选审批尚未关联流程", taskClass.Text("name"));
                            return result;
                        }
                        if (ZHHY_IsTaskFinishSingleApproval(defaultTaskObj.taskId).Value==false)
                        {
                            result.Value = false;
                            result.Message = string.Format("序号{0}所选审批流程尚未通过审批", taskClass.Text("name"));
                            return result;
                        }
                    }
                    else
                    {
                        result.Value = false;
                        result.Message = string.Format("序号{0}尚未选择审批流程", taskClass.Text("name"));
                        return result;
                    }
                }
                #endregion
            }
            result.Value = true;
            return result;
        }
        #endregion

        #region ZHTZ判断任务是否可以开始完成任务,是否可以发起审批
        /// <summary>
        /// 是否可以开始任务
        /// </summary>
        /// <param name="taskId"></param>
        /// <returns></returns>
        public JsonResult ZHTZ_CanStartTask(string taskId)
        {
            //判断是否能开始任务 一二级联动任务控制
            PageJson json = new PageJson();
            json.Success = true;
            InvokeResult<bool> tempResult = ZHTZ_IsCrossPreTaskStart(taskId);
            if (tempResult.Value == false)
            {
                json.Success = false;
                json.Message += tempResult.Message;
            }
            return Json(json);
        }
        /// <summary>
        /// 是否能结束任务
        /// </summary>
        /// <param name="taskId"></param>
        /// <returns></returns>
        public JsonResult ZHTZ_CanFinishTask(string taskId)
        {
            //判断是否能结束任务,1.所有附件都已上传 2.绑定流程的已通过审批
            PageJson json = new PageJson();
            json.Success = true;
            InvokeResult<bool> tempResult = ZHTZ_IsTaskFileUploaded(taskId);
            if (tempResult.Value == false)
            {
                json.Success = false;
                json.Message += tempResult.Message;
            }
            tempResult = ZHTZ_IsTaskFinishApproval(taskId);
            if (tempResult.Value == false)
            {
                json.Success = false;
                if (string.IsNullOrWhiteSpace(json.Message))
                {
                    json.Message += tempResult.Message;
                }
                else
                {
                    json.Message += "\n\n" + tempResult.Message;
                }
            }
            return Json(json);
        }
        /// <summary>
        /// 是否能发起审批
        /// </summary>
        /// <param name="taskId"></param>
        /// <returns></returns>
        public JsonResult ZHTZ_CanStartApproval(string taskId)
        {
            //判断是否能发起审批 :1.所有附件都已上传
            //2.合同或费用节点的前置节点都已经审批通过
            PageJson json = new PageJson();
            json.Success = true;
            BsonDocument task = dataOp.FindOneByQuery("XH_DesignManage_Task", Query.EQ("taskId", taskId));
            BsonDocument plan = dataOp.FindOneByQuery("XH_DesignManage_Plan", Query.EQ("planId", task.Text("planId")));
            //判断是否是费用支付计划
            bool isContractPlan = plan.Text("isContractPlan") == "1";
            InvokeResult<bool> tempResult = ZHTZ_IsTaskFileUploaded(taskId);
            if (tempResult.Value == false)
            {
                json.Success = false;
                json.Message += tempResult.Message;
            }
            //如果是设计计划或费用支付计划里的合同或费用支付节点
            if (!isContractPlan)
            {
                tempResult = ZHTZ_IsPreTaskFinish(taskId);
            }
            else if (task.Int("nodeTypeId") == (int)ConcernNodeType.FeePayment || task.Int("nodeTypeId") == (int)ConcernNodeType.ContractNode)
            {
                tempResult = ZHTZ_IsPreTaskFinish(taskId);
            }
            if (tempResult.Value == false)
            {
                json.Success = false;
                json.Message += "\n\n" + tempResult.Message;
            }
            return Json(json);
        }

        #region 判断跨计划前置任务是否全部都开始
        protected InvokeResult<bool> ZHTZ_IsCrossPreTaskStart(string taskId)
        {
            InvokeResult<bool> result = new InvokeResult<bool>() { Value = true };
            BsonDocument task = dataOp.FindOneByQuery("XH_DesignManage_Task", Query.EQ("taskId", taskId));
            if (task.Int("nodeTypeId") == (int)ConcernNodeType.LevelRelate)
            {
                //获取前置任务节点
                var preTaskQuery = string.Format("nextTaskId={0}&nextPlanId={1}&nextProjId={2}&referType=1", task.Int("taskId"), task.Int("planId"), task.Int("projId"));
                var preTaskRelList = dataOp.FindAllByQueryStr("XH_DesignManage_AcrossPlanTaskRelation", preTaskQuery).ToList();
                bool hasNotStartPreTask = false;
                var notStartPreTask = new List<string>();
                foreach (var rel in preTaskRelList)
                {
                    var preTask = dataOp.FindOneByKeyVal("XH_DesignManage_Task", "taskId", rel.Text("preTaskId"));
                    if (preTask == null) continue;
                    if (preTask.Int("status", 2) == (int)TaskStatus.NotStarted || preTask.Int("status", 2) == (int)TaskStatus.SplitCompleted)
                    {
                        hasNotStartPreTask = true;
                        notStartPreTask.Add(preTask.Text("name"));
                    }
                }
                if (hasNotStartPreTask)
                {
                    result.Value = false;
                    result.Message = string.Format("前置任务：\n{0}\n尚未开始", string.Join(",", notStartPreTask));
                }
            }
            return result;
        }
#endregion

        #region 判断任务是否已经通过审批
        protected InvokeResult<bool> ZHTZ_IsTaskFinishApproval(string taskId)
        {
            InvokeResult<bool> result = new InvokeResult<bool>() { Status = Status.Successful, Value = true };
            BsonDocument task = dataOp.FindOneByQuery("XH_DesignManage_Task", Query.EQ("taskId", taskId));
            if (task.IsNullOrEmpty())
            {
                result.Status = Status.Failed;
                result.Value = false;
                return result;
            }
            BsonDocument plan = dataOp.FindOneByQuery("XH_DesignManage_Plan", Query.EQ("planId", task.Text("planId")));
            //判断是否是费用支付计划
            bool isContractPlan = plan.Text("isContractPlan") == "1";
            //如果是费用支付计划里的费用支付节点，则判断其下每笔支付任务是否都已完成审批
            if (isContractPlan && (task.Int("nodeTypeId") == (int)ConcernNodeType.FeePayment))
            {
                //该费用支付节点下所有的每笔支付任务
                var allEachPayTasks = dataOp.FindAllByQuery("XH_DesignManage_Task", Query.EQ("nodePid", taskId)).ToList();
                var allEachPayTaskIds=allEachPayTasks.Select(i=>i.Int("taskId")).ToList();
                //所有的流程关联
                var allEachPayFlowRels = dataOp.FindAllByQuery("XH_DesignManage_TaskBusFlow",
                        Query.In("taskId", allEachPayTaskIds.Select(i => (BsonValue)(i.ToString())))
                    ).ToList();
                //有关联流程的每笔支付任务id
                var allRelTaskIds=allEachPayFlowRels.Select(i=>i.Int("taskId")).ToList();
                var allInstances = dataOp.FindAllByQuery("BusFlowInstance",
                        Query.And(
                            Query.EQ("tableName", "XH_DesignManage_Task"),
                            Query.EQ("referFieldName", "taskId"),
                            Query.In("taskId", allEachPayTaskIds.Select(i => (BsonValue)(i.ToString())))
                        )
                    ).ToList();
                //所有已通过审批的每笔支付任务id
                var allPassedTaskIds = allInstances.Where(i => i.Int("instanceStatus") == 1).Select(i => i.Int("referFieldValue")).ToList();
                if (allRelTaskIds.Where(i => !allPassedTaskIds.Contains(i)).Count() > 0)
                {
                    result.Value = false;
                    result.Message = "该费用支付节点尚未全部审批通过";
                }
            }
            else
            {
                BsonDocument flowRel = dataOp.FindOneByKeyVal("XH_DesignManage_TaskBusFlow", "taskId", taskId);
                if (!flowRel.IsNullOrEmpty())
                {
                    var queryStr2 = string.Format("tableName={0}&referFieldName={1}&referFieldValue={2}", "XH_DesignManage_Task", "taskId", taskId);
                    var flowInstance = dataOp.FindAllByQueryStr("BusFlowInstance", queryStr2).FirstOrDefault();
                    if (flowInstance.IsNullOrEmpty() || flowInstance.Int("instanceStatus") != 1)
                    {
                        result.Value = false;
                        result.Message = "该任务尚未审批通过";
                    }
                }
            }
            return result;
        }
        #endregion

        #region 判断前置任务是否已经完成
        /// <summary>
        /// 判断前置任务是否已经完成
        /// </summary>
        /// <param name="taskId"></param>
        /// <returns></returns>
        protected InvokeResult<bool> ZHTZ_IsPreTaskFinish(string taskId)
        {
            InvokeResult<bool> result = new InvokeResult<bool>();
            result.Value = true;
            var preTaskQuery = string.Format("sucTaskId={0}&&referType=1", taskId);
            List<BsonDocument> preTaskRelList = dataOp.FindAllByQueryStr("XH_DesignManage_TaskRelation", preTaskQuery).ToList();
            List<BsonDocument> preTaskList = dataOp.FindAllByQuery("XH_DesignManage_Task", Query.In("taskId", preTaskRelList.Select(i => i.GetValue("preTaskId", string.Empty)))).ToList();
            List<int> preTaskIdList = preTaskList.Select(i => i.Int("taskId")).ToList();
            List<BsonDocument> preTaskFlowList = dataOp.FindAllByQuery("XH_DesignManage_TaskBusFlow", Query.In("taskId", preTaskList.Select(i => i.GetValue("taskId", string.Empty)))).ToList();

            bool hasUnCompletedPreTask = false;
            var unCompletedPreTask = new List<string>();
            bool hasUnApprovedPreTask = false;
            var unApprovedPreTask = new List<string>();
            bool hasNotStartAppPreTask = false;
            var notStartApprovalPreTask = new List<string>();
            foreach (var preTask in preTaskList)
            {
                BsonDocument preTaskFlow = preTaskFlowList.Where(i => i.Int("taskId") == preTask.Int("taskId")).FirstOrDefault();
                var preInstanceQuery = string.Format("tableName=XH_DesignManage_Task&referFieldName=taskId&referFieldValue={0}", preTask.Int("taskId"));
                var hitPreInstance = dataOp.FindAllByQueryStr("BusFlowInstance", preInstanceQuery).OrderByDescending(c => c.Date("createDate")).FirstOrDefault();
                if (preTaskFlow.IsNullOrEmpty())
                {
                    if (preTask.Int("status") != (int)TaskStatus.Completed)
                    {
                        unCompletedPreTask.Add(preTask.Text("name"));
                        hasUnCompletedPreTask = true;
                    }
                }
                else if (hitPreInstance.IsNullOrEmpty())//未开始
                {
                    notStartApprovalPreTask.Add(preTask.Text("name"));
                    hasNotStartAppPreTask = true;
                }
                else if (hitPreInstance.Int("instanceStatus") != 1)//未结束
                {
                    unApprovedPreTask.Add(preTask.Text("name"));
                    hasUnApprovedPreTask = true;
                }
            }

            if (hasNotStartAppPreTask)
            {
                result.Value = false;
                result.Message += string.Format("前置任务:\n{0}\n尚未开始审批\n", string.Join(",", notStartApprovalPreTask));
            }
            else if (hasUnApprovedPreTask)
            {
                result.Value = false;
                result.Message += string.Format("前置任务:\n{0}\n尚未审批通过\n", string.Join(",", unApprovedPreTask));
            }
            else if (hasUnCompletedPreTask)
            {
                result.Value = false;
                result.Message += string.Format("前置任务:\n{0}\n尚未完成\n", string.Join(",", unCompletedPreTask));
            }
            return result;
        }
        #endregion
        
        #region 判断预定义交付物是否按要求上传
        /// <summary>
        /// 判断预定义交付物是否按要求上传
        /// </summary>
        /// <param name="taskId"></param>
        /// <returns></returns>
        protected InvokeResult<bool> ZHTZ_IsTaskFileUploaded(string taskId)
        {
            InvokeResult<bool> result = new InvokeResult<bool>();
            //查找所有需要上传的组合
            var combineList = dataOp.FindAllByQuery("Scheduled_deliver", Query.And(Query.EQ("taskId", taskId), Query.EQ("isForce", "1")));
            //组合下所有的备注,包括必须上传和可选的
            var remarkList = dataOp.FindAllByQuery("PredefineFileRemark",
                    Query.In("deliverId", combineList.Select(i => i.GetValue("deliverId", string.Empty)))
                ).ToList();
            //已经上传的所有附件
            List<BsonDocument> fileRelList = dataOp.FindAllByQuery("FileRelation",
                    Query.And(
                        Query.EQ("tableName", "XH_DesignManage_Task"),
                        Query.EQ("fileObjId", "31"),
                        Query.EQ("keyValue",taskId)
                    )
                ).ToList();
            var fileList = dataOp.FindAllByQuery("FileLibrary", Query.In("fileId", fileRelList.Select(i => i.GetValue("fileId", string.Empty)))).ToList();
            //找出没有全部上传的组合
            var unUploadedCombileList = combineList.Where(delegate(BsonDocument doc)
            {
                //必须上传的
                var remarks = remarkList.Where(i => i.Int("deliverId") == doc.Int("deliverId") && i.Int("isNeedUpLoad") == 1).ToList();
                //已上传的文件
                var files = fileList.Where(u =>
                     (u.Text("Property_profId") == doc.Text("profId") &&
                     u.Text("Property_stageId") == doc.Text("stageId") &&
                     u.Text("Property_typeId") == doc.Text("fileCatId"))
                     ||
                     u.Text("Property_deliverId") == doc.Text("deliverId")
                     );
                return files.Count() < remarks.Count();
            }).Select(i => i.Int("deliverId")).ToList();
            //获取这些组合里必须上传的备注
            List<BsonDocument> unUploaded = remarkList.Where(i => unUploadedCombileList.Contains(i.Int("deliverId")) && i.Int("isNeedUpLoad") == 1).ToList();
            result.Value=unUploadedCombileList.Count() <=0;
            result.Message = string.Format("任务附件组合：\n{0}\n尚未全部上传", string.Join("\n", unUploaded.Select(i => i.Text("name"))));
            return result;
        }
        #endregion

        #endregion

        #region QX保存项目下工程
        public JsonResult QX_SaveProjEng()
        {
            var projId = PageReq.GetForm("projId");
            var typeId = PageReq.GetForm("typeId");
            var nodePid = PageReq.GetForm("nodePid");
            var engName = PageReq.GetForm("name");
            var projEngId = PageReq.GetForm("projEngId");
            InvokeResult result = new InvokeResult() { Status = Status.Successful };
            if (string.IsNullOrWhiteSpace(engName))
            {
                result.Status = Status.Failed;
                result.Message = "工程名称不可为空";
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            var curEngObj = dataOp.FindOneByQuery("XH_DesignManage_ProjEngineering", Query.EQ("projEngId", projEngId));
            var tempEngObj = dataOp.FindAllByQuery("XH_DesignManage_ProjEngineering",
                    Query.EQ("projId", projId)
                ).Where(p => string.Equals(p.Text("name").Trim(), engName.Trim())).FirstOrDefault();
            if (tempEngObj != null && tempEngObj.Int("projEngId") != curEngObj.Int("projEngId"))
            {
                result.Status = Status.Failed;
                result.Message = "该项目下存在同名工程";
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            var engTypeObj = dataOp.FindOneByQuery("XH_DesignManage_ProjEngineeringType", Query.EQ("typeId", typeId));
            if (engTypeObj == null)
            {
                result.Status = Status.Failed;
                result.Message = "无效的工程类型参数";
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            BsonDocument newEng = new BsonDocument();
            newEng.Add("projId", projId);
            newEng.Add("typeId", typeId);
            newEng.Add("nodePid", nodePid);
            newEng.Add("name", engName);
            if (curEngObj == null)
            {
                result = dataOp.Insert("XH_DesignManage_ProjEngineering", newEng);
            }
            else
            {
                result = dataOp.Update("XH_DesignManage_ProjEngineering", Query.EQ("projEngId", projEngId), newEng);
            }
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        public JsonResult QX_SaveProjEngV2()
        {
            var projIdList = PageReq.GetFormList("projId");
            var engIdList = PageReq.GetFormList("engId");
            var typeId = PageReq.GetForm("typeId");
            var nodePid = PageReq.GetForm("nodePid");
            var engName = PageReq.GetForm("name");
            var projEngId = PageReq.GetForm("projEngId");
            InvokeResult result = new InvokeResult() { Status = Status.Successful };
            if (string.IsNullOrWhiteSpace(engName))
            {
                result.Status = Status.Failed;
                result.Message = "工程名称不可为空";
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            var engTypeObj = dataOp.FindOneByQuery("XH_DesignManage_ProjEngineeringType", Query.EQ("typeId", typeId));
            if (engTypeObj == null)
            {
                result.Status = Status.Failed;
                result.Message = "无效的工程类型参数";
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            #region 重名判断
            var curEngObj = dataOp.FindOneByQuery("XH_DesignManage_ProjEngineering", Query.EQ("projEngId", projEngId));

            var tempRelation = dataOp.FindAllByQuery("XH_DesignManage_ProjEngRelation",
                    Query.And(
                        Query.NE("projEngId", curEngObj.Text("projEngId")),
                        Query.Or(
                            Query.And(
                                Query.In("engId", engIdList.Select(p => (BsonValue)p)),
                                Query.EQ("projId", "0")
                            ),
                            Query.And(
                                Query.In("projId", projIdList.Select(p => (BsonValue)p))
                            )
                        )
                    )
                );
            var tempProjEngList = dataOp.FindAllByQuery("XH_DesignManage_ProjEngineering",
                    Query.And(
                        Query.In("projEngId", tempRelation.Select(p => p.GetValue("projEngId")))
                    )
                ).Where(p => p.Text("name").Trim() == engName.Trim());
            if (tempProjEngList != null && tempProjEngList.Count() != 0)
            {
                var tempStr = string.Empty;
                var sameProjName = tempRelation.Select(p => p.SourceBsonField("projId", "name")).ToList();
                var sameLandName = tempRelation.Select(p => p.SourceBsonField("engId", "name")).ToList();
                tempStr = string.Format("{0} 存在与该名称工程的关联", string.Join(",", sameLandName.Union(sameProjName).Where(p => !string.IsNullOrWhiteSpace(p))));
                result.Status = Status.Failed;
                result.Message = tempStr;
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }

            #endregion

            #region 保存工程
            BsonDocument newEng = new BsonDocument();
            newEng.Add("typeId", typeId);
            newEng.Add("nodePid", nodePid);
            newEng.Add("name", engName);
            if (curEngObj == null)
            {
                result = dataOp.Insert("XH_DesignManage_ProjEngineering", newEng);
            }
            else
            {
                result = dataOp.Update("XH_DesignManage_ProjEngineering", Query.EQ("projEngId", projEngId), newEng);
            }

            curEngObj = result.BsonInfo;

            #endregion

            #region 工程与地块和项目关联处理

            var oldRelation = new List<BsonDocument>();
            var dataList = new List<StorageData>();
            oldRelation = dataOp.FindAllByQuery("XH_DesignManage_ProjEngRelation", Query.EQ("projEngId", curEngObj.Text("projEngId"))).ToList();
            foreach (var engId in engIdList)
            {
                var tempRel = oldRelation.Where(p => p.Int("projId") == 0 && p.Text("engId") == engId).FirstOrDefault();
                if (!BsonDocumentExtension.IsNullOrEmpty(tempRel))
                {
                    oldRelation.Remove(tempRel);
                    continue;
                }
                StorageData data = new StorageData();
                data.Name = "XH_DesignManage_ProjEngRelation";
                data.Document = new BsonDocument()
                {
                    { "projEngId" , curEngObj.Text("projEngId") },
                    { "projId" , "0"},
                    { "engId" , engId.ToString()}
                };
                data.Type = StorageType.Insert;
                dataList.Add(data);
            }
            foreach (var projId in projIdList)
            {
                var tempRel = oldRelation.Where(p => p.Text("projId") == projId).FirstOrDefault();
                var projObj = dataOp.FindOneByQuery("XH_DesignManage_Project", Query.EQ("projId", projId));
                var landObj = dataOp.FindOneByQuery("XH_DesignManage_Engineering", Query.EQ("engId", projObj.Text("engId")));
                if (!BsonDocumentExtension.IsNullOrEmpty(tempRel))
                {
                    oldRelation.Remove(tempRel);
                    continue;
                }
                StorageData data = new StorageData();
                data.Name = "XH_DesignManage_ProjEngRelation";
                data.Document = new BsonDocument()
                {
                    { "projEngId" , curEngObj.Text("projEngId") },
                    { "projId" , projId},
                    { "engId" , landObj.Text("engId")}
                };
                data.Type = StorageType.Insert;
                dataList.Add(data);
            }
            foreach (var oldRel in oldRelation)
            {
                StorageData data = new StorageData();
                data.Name = "XH_DesignManage_ProjEngRelation";
                data.Query = Query.EQ("relId", oldRel.Text("relId"));
                data.Type = StorageType.Delete;
                dataList.Add(data);
            }
            result = dataOp.BatchSaveStorageData(dataList);
            #endregion

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }
        #endregion

        #region QX获取地块-项目树XML
        public ActionResult QX_GetEngProjTreeXML(string engTb, string projTb, int? engId)
        {
            List<TreeNode> engNodeList = new List<TreeNode>();
            var engList = dataOp.FindAll("XH_DesignManage_Engineering").ToList();
            if (engId.HasValue && engId != 0)
            {
                engList = engList.Where(p => p.Int("engId") == engId).ToList();
            }

            List<TreeNode> treeList = new List<TreeNode>();
            foreach (var tempEng in engList.OrderByDescending(p => p.String("name")))
            {
                List<BsonDocument> projList = dataOp.FindAllByQuery(projTb,
                Query.EQ("engId", tempEng.Text("engId"))).ToList();
                TreeNode engNode = new TreeNode();

                engNode.Id = tempEng.Int("engId");
                engNode.Name = tempEng.String("name");
                engNode.Lv = 1;
                engNode.Pid = 0;
                engNode.underTable = tempEng.String("underTable");
                engNode.Param = "eng";
                engNode.SubNodes = QX_GetEngProjectSubTreeXMLt(projList, 0);    //获取子节点列表;
                engNode.IsLeaf = 0;
                treeList.Add(engNode);
            }
            treeList = treeList.OrderBy(t => t.Name).ToList();
            return new XmlTree(treeList);
        }
        /// <summary>
        /// 获取项目子树XML
        /// </summary>
        /// <param name="projList"></param>
        /// <param name="projId"></param>
        /// <returns></returns>
        public List<TreeNode> QX_GetEngProjectSubTreeXMLt(List<BsonDocument> projList, int projId)
        {
            BsonDocument curNode = projList.Where(t => t.Int("projId") == projId).FirstOrDefault(); //当前项目节点

            List<BsonDocument> subProjList = projList.Where(t => t.Int("nodePid") == projId).ToList(); //子项目列表

            List<TreeNode> treeList = new List<TreeNode>();

            foreach (var subNode in subProjList.OrderBy(t => t.Int("nodeOrder")))      //循环子部门列表,赋值
            {
                TreeNode node = new TreeNode();

                node.Id = subNode.Int("projId");
                node.Name = subNode.String("name");
                node.Lv = curNode.Int("nodeLevel") + 2;
                node.Pid = projId;
                node.underTable = subNode.String("underTable");
                node.Param = "proj";
                node.SubNodes = QX_GetEngProjectSubTreeXMLt(projList, node.Id);    //获取子节点列表;
                if (node.SubNodes.Count() == 0)
                {
                    node.IsLeaf = 1;
                }
                else
                {
                    node.IsLeaf = 0;
                }
                treeList.Add(node);
            }
            treeList = treeList.OrderBy(t => t.Name).ToList();
            return treeList;
        }
        #endregion

        #region QX设计变更PDF导出

        /// <summary>
        /// 设计变更PDF导出
        /// </summary>
        public void DesignChangeTOPDF()
        {
            try
            {
                var designChangeId = PageReq.GetParam("designChangeId");
                var designChangeObj = dataOp.FindOneByQuery("DesignChange", Query.EQ("designChangeId", designChangeId));
                var url = string.Format("{0}/DesignManage/DesignChangeWorkFlowInfoTable?dngChangeId={1}", SysAppConfig.PDFDomain, designChangeId);
                string pdfUrl = string.Format("{0}/Account/PDF_Login?ReturnUrl={1}", SysAppConfig.PDFDomain, url);
                string tmpName = designChangeObj.Text("name").CutStr(10, string.Empty) + ".pdf";
                if (string.IsNullOrEmpty(designChangeObj.Text("name")))
                {
                    tmpName = designChangeObj.Text("changeTheme").CutStr(10,string.Empty) + ".pdf";
                }
                string tmpName1 = HttpUtility.UrlEncode(tmpName,Encoding.UTF8).Replace("+", "%20"); //主要为了解决包含非英文/数字名称的问题
                string savePath = Server.MapPath("/UploadFiles/temp");
                if (!System.IO.Directory.Exists(savePath))
                {
                    System.IO.Directory.CreateDirectory(savePath);
                }
                savePath = System.IO.Path.Combine(savePath, tmpName1);
                System.Diagnostics.Process p = System.Diagnostics.Process.Start(@"C:\wkhtmltopdf\wkhtmltopdf.exe", @"" + pdfUrl + " " + savePath);
                p.WaitForExit();
                DownloadFileZHTZ(savePath, tmpName);

            }
            catch (Exception ex)
            {

            }
        }
        #endregion
        #region 设计变更指令单PDF导出
        public void DesignChangeCommandTOPDF()
        {
            try
            {
                var pdfUrl = PageReq.GetParam("pdfUrl");
                var pdfName = PageReq.GetParam("pdfName").CutStr(10, "") + ".pdf";
                string loginUrl = string.Format("{0}/Account/PDF_Login?ReturnUrl={1}", SysAppConfig.PDFDomain, HttpUtility.UrlEncode(pdfUrl));
                string tmpName = HttpUtility.UrlEncode(pdfName, System.Text.Encoding.UTF8).Replace("+", "%20"); //主要为了解决包含非英文/数字名称的问题
                string savePath = Server.MapPath("/UploadFiles/temp");
                if (!System.IO.Directory.Exists(savePath))
                {
                    System.IO.Directory.CreateDirectory(savePath);
                }
                savePath = System.IO.Path.Combine(savePath, tmpName);
                System.Diagnostics.Process p = System.Diagnostics.Process.Start(@"C:\wkhtmltopdf\wkhtmltopdf.exe", @"" + loginUrl + " " + savePath);
                p.WaitForExit();
                DownloadFileZHTZ(savePath, pdfName);

            }
            catch (Exception ex)
            {

            }
        }
        #endregion
        #region 计划任务审批PDF导出
        public void ProjTaskWorkFlowTOPDF()
        {
            try
            {
                var flowId = PageReq.GetParam("flowId");
                var flowIntanceId = PageReq.GetParam("flowInstanceId");
                var tableName = PageReq.GetParam("tableName");
                var referFieldName = PageReq.GetParam("referFieldName");
                var referFieldValue = PageReq.GetParam("referFieldValue");
                var flowObj = dataOp.FindOneByQuery("BusFlow", Query.EQ("flowId", flowId));
                var url = string.Format("{0}/DesignManage/ProjTaskWorkFlowPDF?flowId={1}&flowInstanceId={2}&tableName={3}&referFieldName={4}&referFieldValue={5}"
                    , SysAppConfig.PDFDomain, flowId, flowIntanceId, tableName, referFieldName, referFieldValue);
                string pdfUrl = string.Format("{0}/Account/PDF_Login?ReturnUrl={1}", SysAppConfig.PDFDomain, HttpUtility.UrlEncode(url));
                string tmpName = flowObj.Text("name").CutStr(10,"") + ".pdf";
                string tmpName1 = HttpUtility.UrlEncode(tmpName, System.Text.Encoding.UTF8).Replace("+", "%20"); //主要为了解决包含非英文/数字名称的问题
                string savePath = Server.MapPath("/UploadFiles/temp");
                if (!System.IO.Directory.Exists(savePath))
                {
                    System.IO.Directory.CreateDirectory(savePath);
                }
                savePath = System.IO.Path.Combine(savePath, tmpName1);
                System.Diagnostics.Process p = System.Diagnostics.Process.Start(@"C:\wkhtmltopdf\wkhtmltopdf.exe", @"" + pdfUrl + " " + savePath);
                p.WaitForExit();
                DownloadFileZHTZ(savePath, tmpName);

            }
            catch (Exception ex)
            {

            }
        }
        #endregion

        /// <summary>
        /// 获取目录树形
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="moveId"></param>
        /// <param name="ddirId">目录树Id</param>
        /// <returns></returns>
        public ActionResult GetItemTree(string tbName, string moveId, string listId, string itemType)
        {
            TableRule tbRule = new TableRule(tbName);

            string tbKey = tbRule.PrimaryKey;

            List<BsonDocument> itemList = dataOp.FindAllByQuery(tbName, Query.And(Query.EQ("listId", listId), Query.EQ("itemType", itemType))).ToList();//获取模板下的目录
            List<TreeNode> treeList = TreeHelper.GetSingleTreeList(itemList);

            return new XmlTree(treeList);
        }
        /// <summary>
        /// 获取库信息
        /// </summary>
        /// <returns></returns>
        public ActionResult GetLibInfo()
        {
            List<BsonDocument> libList = dataOp.FindAllByQuery("StandardResult_StandardResultLibrary", Query.EQ("isUse", "1")).ToList();
            List<Hashtable> retList = new List<Hashtable>();

            foreach (var tempLib in libList)
            {
                tempLib.Remove("_id");

                retList.Add(tempLib.ToHashtable());
            }
            return Json(retList);
        }
        /// <summary>
        /// 插入内置角色
        /// </summary>
        /// <param name="typeId">1:地块 2：项目</param>
        /// <param name="keyVlaue"></param>
        public int InsertBuiltRole(string typeId, string keyVlaue)
        {
            string landOrProj = string.Empty;//1:代表地块角色 2：代表项目角色
            if (typeId == "1")
            {
                landOrProj = "1";
            }
            else if (typeId == "2")
            {
                landOrProj = "2";
            }
            InvokeResult result = new InvokeResult();
            List<BsonDocument> roleList = dataOp.FindAllByQuery("RoleRightBuiltIn", Query.EQ("typeId", typeId)).ToList();
            List<string> roleIdList = roleList.Select(x => x.String("roleBuiltId")).ToList();
            List<BsonDocument> roleRightList = dataOp.FindAllByQuery("RoleRightBuiltInRel", Query.In("roleBuiltId", TypeConvert.StringListToBsonValueList(roleIdList))).ToList();//内置权限项
            List<string> modulAllId = roleRightList.Select(x => x.String("modulId")).Distinct().ToList();//所有内置模块Id
            List<string> rightAllId = roleRightList.Select(x => x.String("operatingId")).Distinct().ToList();//所有内置权限项Id
            List<BsonDocument> modulAll = dataOp.FindAllByQuery("SysModul", Query.In("modulId", TypeConvert.StringListToBsonValueList(modulAllId))).ToList();
            List<BsonDocument> rightAll = dataOp.FindAllByQuery("SysOperating", Query.In("operatingId", TypeConvert.StringListToBsonValueList(rightAllId))).ToList();

            foreach (var tempRole in roleList)
            {
                BsonDocument tempRoleInfo = new BsonDocument();
                tempRoleInfo.Add("name", tempRole.String("name"));
                tempRoleInfo.Add("groupId", tempRole.String("groupId"));
                tempRoleInfo.Add("roleType", tempRole.String("roleType"));
                tempRoleInfo.Add("formulaId", tempRole.String("formulaId"));
                tempRoleInfo.Add("dataObjId", tempRole.String("dataObjId"));
                tempRoleInfo.Add("isDataRight", tempRole.String("isDataRight"));

                tempRoleInfo.Add("isBuiltIn", "1");//1：内置角色
                tempRoleInfo.Add("landOrProj", landOrProj);
                tempRoleInfo.Add("landOrProjId", keyVlaue);
                result = dataOp.Insert("SysRole", tempRoleInfo);
                if (result.Status == Status.Successful)
                {
                    tempRoleInfo = result.BsonInfo;
                    //增加地块
                    if (typeId == "1")
                    {
                        BsonDocument dataScope = new BsonDocument();
                        dataScope.Add("dataFeiIdName", tempRole.String("keyFeiIdName"));
                        dataScope.Add("dataId", keyVlaue);
                        dataScope.Add("dataTableName", tempRole.String("tableName"));//表名为Land
                        dataScope.Add("remark", "地块权限");
                        dataScope.Add("roleCategoryId", "1");
                        dataScope.Add("roleId", tempRoleInfo.String("roleId"));
                        dataScope.Add("status", "0");
                        result = dataOp.Insert("DataScope", dataScope);
                        if (result.Status == Status.Failed)
                        {
                            dataOp.Delete("SysRoleRight", Query.EQ("roleId", tempRoleInfo.String("roleId")));
                            dataOp.Delete("DataScope", Query.EQ("roleId", tempRoleInfo.String("roleId")));
                            dataOp.Delete("SysRole", Query.EQ("roleId", tempRoleInfo.String("roleId")));
                            return 1;

                        }

                    }
                    else if (typeId == "2")
                    {
                        BsonDocument dataScope = new BsonDocument();
                        dataScope.Add("dataFeiIdName", tempRole.String("keyFeiIdName"));
                        dataScope.Add("dataId", keyVlaue);
                        dataScope.Add("dataTableName", tempRole.String("tableName"));//表名为Project
                        dataScope.Add("remark", "项目权限");
                        dataScope.Add("roleCategoryId", "1");
                        dataScope.Add("roleId", tempRoleInfo.String("roleId"));
                        dataScope.Add("status", "0");
                        result = dataOp.Insert("DataScope", dataScope);
                        if (result.Status == Status.Failed)
                        {
                            dataOp.Delete("SysRoleRight", Query.EQ("roleId", tempRoleInfo.String("roleId")));
                            dataOp.Delete("DataScope", Query.EQ("roleId", tempRoleInfo.String("roleId")));
                            dataOp.Delete("SysRole", Query.EQ("roleId", tempRoleInfo.String("roleId")));
                            return 1;

                        }
                    }
                    List<BsonDocument> curBuiltInRightList = roleRightList.Where(x => x.String("roleBuiltId") == tempRole.String("roleBuiltId")).ToList();
                    List<StorageData> data = new List<StorageData>();
                    foreach (var tempRight in curBuiltInRightList)
                    {
                        BsonDocument curModul = modulAll.Where(x => x.String("modulId") == tempRight.String("modulId")).FirstOrDefault();
                        BsonDocument curRight = rightAll.Where(x => x.String("operatingId") == tempRight.String("operatingId")).FirstOrDefault();
                        BsonDocument tempAddRightInfo = new BsonDocument();
                        tempAddRightInfo.Add("roleId", tempRoleInfo.String("roleId"));
                        tempAddRightInfo.Add("modulId", tempRight.String("modulId"));
                        tempAddRightInfo.Add("operatingId", tempRight.String("operatingId"));
                        tempAddRightInfo.Add("dataObjId", "20");
                        tempAddRightInfo.Add("name", curRight.String("name"));
                        tempAddRightInfo.Add("code", curModul.String("code") + "_" + curRight.String("code"));
                        //tempAddRightInfo.Add("relyon", "");
                        //tempAddRightInfo.Add("remark", ""); tempAddRightInfo.Add("remark", "");
                        tempAddRightInfo.Add("isDataRight", "1");
                        StorageData tempData = new StorageData();
                        tempData.Document = tempAddRightInfo;
                        tempData.Name = "SysRoleRight";
                        tempData.Type = StorageType.Insert;
                        data.Add(tempData);
                    }
                    result = dataOp.BatchSaveStorageData(data);
                    if (result.Status == Status.Failed)
                    {
                        dataOp.Delete("SysRoleRight", Query.EQ("roleId", tempRoleInfo.String("roleId")));
                        dataOp.Delete("DataScope", Query.EQ("roleId", tempRoleInfo.String("roleId")));
                        dataOp.Delete("SysRole", Query.EQ("roleId", tempRoleInfo.String("roleId")));
                        return 1;

                    }
                }

            }

            return 0;
        }
        /// <summary>
        /// 插入项目或地块角色方案
        /// </summary>
        ///<param name="schemeId">项目地块方案</param>
        /// <param name="typeId">1:地块 2：项目</param>
        /// <param name="keyValue">项目或地块的Id</param>
        /// <param name="isDelete">是否删除旧导入的数据 1：删除 0或2 不删除 </param>
        /// <returns></returns>
        public JsonResult ImportProjOrLandRole(int schemeId, int typeId, string keyValue, int isDelete)
        {
            InvokeResult result = new InvokeResult();
            PageJson json = new PageJson();

            string landOrProj = string.Empty;//1:代表地块角色 2：代表项目角色
            string projLandId = string.Empty; //若为项目，则给项目角色增加查看地块的权限
            BsonDocument schemeInfo = dataOp.FindOneByQuery("ProjBuiltInRoleScheme", Query.EQ("schemeId", schemeId.ToString()));
            if (schemeInfo != null)
            {
                typeId = schemeInfo.Int("typeId");
            }
            else
            {
                schemeInfo = dataOp.FindOneByQuery("ProjBuiltInRoleScheme", Query.And(Query.EQ("isDefault", "1"), Query.EQ("typeId", typeId.ToString())));
            }
            if (schemeInfo == null)
            {
                json.Message = "暂无导入角色组方案，请先确认是否已经设置好后在重新导入!";
                json.Success = false;
                return Json(json);
            }
            if (typeId == 1)
            {
                landOrProj = "1";
            }
            else if (typeId == 2)
            {
                landOrProj = "2";
                var projInfo = dataOp.FindOneByQuery("Project", Query.EQ("projId", keyValue));
                if (projInfo != null)
                {
                    projLandId = projInfo.String("landId");
                }

            }
            if (isDelete == 1)
            {
                //删除旧的通过导入的角色和其所拥有的权限
                List<BsonDocument> oldRoleList = dataOp.FindAllByQuery("SysRole", Query.And(Query.EQ("isBuiltIn", "1"), Query.EQ("isDataRight", "1"), Query.EQ("landOrProjId", keyValue), Query.EQ("landOrProj", landOrProj))).ToList();
                foreach (var tempRole in oldRoleList)
                {
                    dataOp.Delete("SysRoleRight", Query.EQ("roleId", tempRole.String("roleId")));
                    dataOp.Delete("DataScope", Query.EQ("roleId", tempRole.String("roleId")));
                    dataOp.Delete("SysRole", Query.EQ("roleId", tempRole.String("roleId")));
                    dataOp.Delete("SysRoleUser", Query.EQ("roleId", tempRole.String("roleId")));
                    dataOp.Delete("SysRoleOrg", Query.EQ("roleId", tempRole.String("roleId")));
                    dataOp.Delete("SysCommPost", Query.EQ("roleId", tempRole.String("roleId")));
                }
            }

            List<BsonDocument> roleList = dataOp.FindAllByQuery("ProjBuiltInRole", Query.And(Query.EQ("schemeId", schemeInfo.String("schemeId")), Query.EQ("typeId", typeId.ToString()))).ToList();//导入角色方案的角色列表
            List<string> roleIdList = roleList.Select(x => x.String("roleId")).ToList();
            List<BsonDocument> roleRightList = dataOp.FindAllByQuery("ProjBuiltInRoleRightRel", Query.In("roleId", TypeConvert.StringListToBsonValueList(roleIdList))).ToList();//内置权限项
            List<string> modulAllCode = roleRightList.Select(x => x.String("modulCode")).Distinct().ToList();//所有内置模块代码
            List<string> rightAllCode = roleRightList.Select(x => x.String("operatingCode")).Distinct().ToList();//所有内置权限项代码
            List<BsonDocument> modulAll = dataOp.FindAllByQuery("SysModule", Query.In("code", TypeConvert.StringListToBsonValueList(modulAllCode))).ToList();
            List<BsonDocument> rightAll = dataOp.FindAllByQuery("SysOperating", Query.In("code", TypeConvert.StringListToBsonValueList(rightAllCode))).ToList();
            foreach (var tempRole in roleList)
            {
                BsonDocument tempRoleInfo = new BsonDocument();
                tempRoleInfo.Add("name", tempRole.String("name"));
                tempRoleInfo.Add("groupId", tempRole.String("groupId"));
                tempRoleInfo.Add("roleType", tempRole.String("roleType"));
                tempRoleInfo.Add("formulaId", tempRole.String("formulaId"));
                tempRoleInfo.Add("dataObjId", tempRole.String("dataObjId"));
                tempRoleInfo.Add("isDataRight", tempRole.String("isDataRight"));

                tempRoleInfo.Add("isBuiltIn", "1");//1：内置角色
                tempRoleInfo.Add("landOrProj", landOrProj);
                tempRoleInfo.Add("landOrProjId", keyValue);
                tempRoleInfo.Add("schemeId", schemeInfo.String("schemeId"));
                result = dataOp.Insert("SysRole", tempRoleInfo);//插入新角色
                if (result.Status == Status.Successful)
                {
                    tempRoleInfo = result.BsonInfo;
                    //增加地块
                    if (typeId == 1)
                    {
                        BsonDocument dataScope = new BsonDocument();
                        dataScope.Add("dataFeiIdName", tempRole.String("keyFeiIdName"));
                        dataScope.Add("dataId", keyValue);
                        dataScope.Add("dataTableName", tempRole.String("tableName"));//表名为Land
                        dataScope.Add("remark", "地块权限");
                        dataScope.Add("roleCategoryId", "1");
                        dataScope.Add("roleId", tempRoleInfo.String("roleId"));
                        dataScope.Add("status", "0");
                        result = dataOp.Insert("DataScope", dataScope);//为新建的角色分配地块
                        if (result.Status == Status.Failed)//分配失败，删除已建立的其他数据
                        {
                            dataOp.Delete("SysRoleRight", Query.EQ("roleId", tempRoleInfo.String("roleId")));
                            dataOp.Delete("DataScope", Query.EQ("roleId", tempRoleInfo.String("roleId")));
                            dataOp.Delete("SysRole", Query.EQ("roleId", tempRoleInfo.String("roleId")));
                            json.Success = false;
                            json.Message = "导入角色方案没有完成，请刷新后重试!";
                            return Json(json, JsonRequestBehavior.AllowGet);
                        }

                    }
                    else if (typeId == 2)
                    {
                        BsonDocument dataScope = new BsonDocument();
                        dataScope.Add("dataFeiIdName", tempRole.String("keyFeiIdName"));
                        dataScope.Add("dataId", keyValue);
                        dataScope.Add("dataTableName", tempRole.String("tableName"));//表名为Project
                        dataScope.Add("remark", "项目权限");
                        dataScope.Add("roleCategoryId", "1");
                        dataScope.Add("roleId", tempRoleInfo.String("roleId"));
                        dataScope.Add("status", "0");
                        result = dataOp.Insert("DataScope", dataScope);
                        if (result.Status == Status.Failed)
                        {
                            dataOp.Delete("SysRoleRight", Query.EQ("roleId", tempRoleInfo.String("roleId")));
                            dataOp.Delete("DataScope", Query.EQ("roleId", tempRoleInfo.String("roleId")));
                            dataOp.Delete("SysRole", Query.EQ("roleId", tempRoleInfo.String("roleId")));
                            json.Success = false;
                            json.Message = "导入角色方案没有完成，请刷新后重试!";
                            return Json(json, JsonRequestBehavior.AllowGet);
                        }
                        BsonDocument landDataScope = new BsonDocument();//增补地块查看权限
                        landDataScope.Add("dataFeiIdName", "landId");
                        landDataScope.Add("dataId", projLandId);
                        landDataScope.Add("dataTableName", "Land");//表名为Project
                        landDataScope.Add("remark", "地块权限");
                        landDataScope.Add("roleCategoryId", "1");
                        landDataScope.Add("roleId", tempRoleInfo.String("roleId"));
                        landDataScope.Add("status", "0");
                        result = dataOp.Insert("DataScope", landDataScope);
                        if (result.Status == Status.Failed)
                        {
                            dataOp.Delete("SysRoleRight", Query.EQ("roleId", tempRoleInfo.String("roleId")));
                            dataOp.Delete("DataScope", Query.EQ("roleId", tempRoleInfo.String("roleId")));
                            dataOp.Delete("SysRole", Query.EQ("roleId", tempRoleInfo.String("roleId")));
                            json.Success = false;
                            json.Message = "导入角色方案没有完成，请刷新后重试!";
                            return Json(json, JsonRequestBehavior.AllowGet);
                        }
                    }
                    List<BsonDocument> curBuiltInRightList = roleRightList.Where(x => x.String("roleId") == tempRole.String("roleId")).ToList();
                    List<StorageData> data = new List<StorageData>();
                    foreach (var tempRight in curBuiltInRightList)
                    {
                        BsonDocument curModul = modulAll.Where(x => x.String("code") == tempRight.String("modulCode")).FirstOrDefault();
                        BsonDocument curRight = rightAll.Where(x => x.String("code") == tempRight.String("operatingCode")).FirstOrDefault();
                        BsonDocument tempAddRightInfo = new BsonDocument();
                        tempAddRightInfo.Add("roleId", tempRoleInfo.String("roleId"));
                        tempAddRightInfo.Add("modulId", curModul.String("modulId"));
                        tempAddRightInfo.Add("operatingId", curRight.String("operatingId"));
                        tempAddRightInfo.Add("dataObjId", "20");
                        tempAddRightInfo.Add("name", curRight.String("name"));
                        tempAddRightInfo.Add("code", curModul.String("code") + "_" + curRight.String("code"));
                        //tempAddRightInfo.Add("relyon", "");
                        //tempAddRightInfo.Add("remark", ""); tempAddRightInfo.Add("remark", "");
                        tempAddRightInfo.Add("isDataRight", "1");
                        StorageData tempData = new StorageData();
                        tempData.Document = tempAddRightInfo;
                        tempData.Name = "SysRoleRight";
                        tempData.Type = StorageType.Insert;
                        data.Add(tempData);
                    }
                    result = dataOp.BatchSaveStorageData(data);
                    if (result.Status == Status.Failed)
                    {
                        dataOp.Delete("SysRoleRight", Query.EQ("roleId", tempRoleInfo.String("roleId")));
                        dataOp.Delete("DataScope", Query.EQ("roleId", tempRoleInfo.String("roleId")));
                        dataOp.Delete("SysRole", Query.EQ("roleId", tempRoleInfo.String("roleId")));
                        json.Success = false;
                        json.Message = "导入角色方案没有完成，请刷新后重试!";
                        return Json(json, JsonRequestBehavior.AllowGet);
                    }
                }
            }
            json.Success = true;
            json.Message = "导入角色方案成功!";
            return Json(json, JsonRequestBehavior.AllowGet);

        }
        /// <summary>
        /// 判断是否已经导入过角色方案
        /// </summary>
        /// <param name="keyValue">项目或地块的Id</param>
        /// <param name="landOrProj">1：地块，2：项目</param>
        /// <returns></returns>
        public JsonResult JudgeIsHasScheme(string keyValue, string landOrProj)
        {
            PageJson json = new PageJson();
            string tempName = string.Empty;
            if (landOrProj == "1")
            {
                tempName = "地块";
            }
            else if (landOrProj == "2")
            {
                tempName = "项目";
            }
            List<BsonDocument> oldRoleList = dataOp.FindAllByQuery("SysRole", Query.And(Query.EQ("isBuiltIn", "1"), Query.EQ("isDataRight", "1"), Query.EQ("landOrProjId", keyValue), Query.EQ("landOrProj", landOrProj), Query.NE("schemeId", null))).ToList();
            List<string> schemeIds = oldRoleList.Select(x => x.String("schemeId")).Distinct().ToList();
            if (schemeIds.Count() > 0)
            {
                json.Success = false;
                json.Message = "该" + tempName + "已导入过角色方案，是否继续导入,继续导入将删除旧角色方案数据，无法恢复!";

            }
            else
            {
                json.Success = true;
                json.Message = "";
            }
            return Json(json);

        }

        /// <summary>
        /// 三盛项目中文件上传 关联表 关联属性数据修复
        /// </summary>
        /// <param name="projId"></param>
        /// <returns></returns>
        public JsonResult RepairFileReltion(string projId)
        {
            InvokeResult result = new InvokeResult();
            var PlanList = dataOp.FindAllByKeyVal("XH_DesignManage_Plan", "projId", projId).ToList();
            var firstPlanId = PlanList.Count() > 0 ? PlanList.FirstOrDefault().Int("planId") : 0;
            List<BsonDocument> taskList = dataOp.FindAllByKeyVal("XH_DesignManage_Task", "planId", firstPlanId.ToString()).Distinct().ToList();    //所有任务节点
            foreach (var tempTask in taskList)
            {
                BsonDocument taskProperty = dataOp.FindOneByQuery("TaskDocProtery", Query.EQ("taskId", tempTask.String("taskId")));
                List<BsonDocument> curTaskFileRel = dataOp.FindAllByQuery("FileRelation", Query.And(Query.EQ("tableName", "XH_DesignManage_Task"), Query.EQ("keyName", "taskId"), Query.EQ("keyValue", tempTask.String("taskId")))).ToList();//查找当前项目所有管理
                List<string> rootStructIdList = curTaskFileRel.Where(s => s.String("structId") != "").Select(x => x.String("structId")).Distinct().ToList();//当前任务所有文件夹根目录
                List<BsonDocument> allStructList = new List<BsonDocument>();//所有目录结构
                foreach (var tempRoot in rootStructIdList)
                {
                    BsonDocument tempRootInfo = dataOp.FindOneByQuery("FileStructure", Query.EQ("structId", tempRoot));
                    allStructList.Add(tempRootInfo);
                    allStructList.AddRange(dataOp.FindChildNodes("FileStructure", tempRoot));
                }
                List<string> allStructIdList = allStructList.Select(x => x.String("structId")).ToList();
                List<BsonDocument> allStructFile = dataOp.FindAllByQuery("FileLibrary", Query.In("structId", TypeConvert.StringListToBsonValueList(allStructIdList))).ToList();
                foreach (var tempFile in allStructFile)
                {
                    BsonDocument tempQueryFile = dataOp.FindOneByQuery("FileRelation", Query.And(Query.EQ("tableName", "XH_DesignManage_Task"), Query.EQ("keyName", "taskId"), Query.EQ("keyValue", tempTask.String("taskId")), Query.EQ("fileId", tempFile.String("fileId"))));
                    if (tempQueryFile == null)
                    {
                        //增补关联
                        BsonDocument tempAddRel = new BsonDocument{
                        {"fileId",tempFile.String("fileId")},
                        {"fileObjId","31"},
                        {"tableName", "XH_DesignManage_Task"},
                        {"keyName", "taskId"},
                        {"keyValue", tempTask.String("taskId")},
                        {"isPreDefine","False"},
                        {"isCover","False"},
                        {"version", "1"},
                        {"uploadType" ,"2"}
                        };
                        result = dataOp.Insert("FileRelation", tempAddRel);
                        if (result.Status == Status.Successful)
                        {
                            BsonDocument tempRelPro = new BsonDocument { 
                            { "fileRelId", result.BsonInfo.String("fileRelId") },
                            { "profId" ,taskProperty.String("sysProfId")},
                            {"stageId" , taskProperty.String("stageId")},
                            {"fileCatId" ,taskProperty.String("fileCatId") } 
                            };
                            result = dataOp.Insert("FileRelProperty", tempRelPro);
                        }
                    }
                    else
                    {
                        BsonDocument tempRelPro = dataOp.FindOneByQuery("FileRelProperty", Query.EQ("fileRelId", tempQueryFile.String("fileRelId")));
                        if (tempRelPro == null)
                        {
                            tempRelPro = new BsonDocument { 
                            { "fileRelId",tempQueryFile.String("fileRelId") },
                            { "profId" ,taskProperty.String("sysProfId")},
                            {"stageId" , taskProperty.String("stageId")},
                            {"fileCatId" ,taskProperty.String("fileCatId") } 
                            };
                            result = dataOp.Insert("FileRelProperty", tempRelPro);
                        }
                    }

                }

            }
            PageJson json = new PageJson();
            json.Success = result.Status == Status.Successful;
            json.Message = result.Message;
            return Json(json, JsonRequestBehavior.AllowGet);
        }

        #region 移动开发数据接口
        /// <summary>
        /// 代办获取接口  loginName  登陆名    type 0  涉及我的 1  我发起的 2 我审批的
        /// </summary>
        /// <returns></returns>
        public JsonResult PeddingTaskList()
        {
            int current = PageReq.GetParamInt("current");
            current = current == 0 ? 1 : current;
            int pageSize = 15;
            string loginName = PageReq.GetParam("loginName");
            int type = PageReq.GetParamInt("type");
            List<Pedding> list = new List<Pedding>();
            BsonDocument user = dataOp.FindOneByKeyVal("SysUser", "loginName", loginName);
            List<BsonDocument> flowList = new List<BsonDocument>();
            var waitMyApprovalInstance = new List<BsonDocument>();
            var waitMyLaunchFlow = new List<BsonDocument>();
            var myBusFlowInstance = new List<BsonDocument>();
            if (user != null)
            {
                var flowInstanceHelper = new Yinhe.ProcessingCenter.BusinessFlow.FlowInstanceHelper();

                if (type == 0)
                {
                    //waitMyLaunchFlow = flowInstanceHelper.GetUserWaitForStartFlow(user.Int("userId")); //等待我发起
                    flowList.AddRange(waitMyLaunchFlow);
                }
                else if (type == 1)
                {

                    myBusFlowInstance = flowInstanceHelper.GetUserAssociatedFlowInstance(user.Int("userId"), 0); //涉及我的审批
                }
                else if (type == 2)
                {
                    waitMyApprovalInstance = flowInstanceHelper.GetUserWaitForApprovaleFlow(user.Int("userId")); //等待我审批
                    flowList.AddRange(waitMyApprovalInstance);
                }


                // myBusFlowInstance = flowInstanceHelper.GetUserAssociatedFlowInstance(user.Int("userId")); //涉及我的审批

                List<string> allTaskIdList = new List<string>();
                allTaskIdList.AddRange(waitMyApprovalInstance.Select(t => t.String("referFieldValue")));
                allTaskIdList.AddRange(waitMyLaunchFlow.Select(t => t.String("taskId")));
                allTaskIdList.AddRange(myBusFlowInstance.Select(t => t.String("referFieldValue")));
                var allTask = dataOp.FindAllByKeyValList("XH_DesignManage_Task", "taskId", allTaskIdList.Distinct().ToList()).ToList();

                //List<BsonDocument> taskMangerList = dataOp.FindAllByKeyVal("XH_DesignManage_TaskManager", "userId", user.Text("userId")).ToList();
                ////获取计划负责人列表
                //var planManagerList = dataOp.FindAllByKeyVal("XH_DesignManage_PlanManager", "userId", user.Text("userId")).ToList();
                //var taskIds = taskMangerList.Select(c => c.Text("taskId"));
                //var planIds = planManagerList.Select(c => c.Text("planId"));
                ////获取想
                //var AllAssociatTaskList = dataOp.FindAllByKeyValList("XH_DesignManage_Task", "taskId", taskIds.ToList());
                //var AllAssociatPlanList = dataOp.FindAllByKeyValList("XH_DesignManage_Plan", "taskId", planIds.ToList());


                ////获取我设计的待阅任务列表
                //var allInstance = dataOp.FindAll("BusFlowInstance").ToList();
                //var queryStr = string.Format("givenUserId={0}&status=0", user.Int("userId"));
                //var circulateTaskList = dataOp.FindAllByQueryStr("BusinessFlowCirculation", queryStr);

                List<int> fIds = new List<int>();



                //List<string> allTaskIdList = new List<string>();
                //allTaskIdList.AddRange(waitMyApprovalInstance.Select(t => t.String("referFieldValue")));
                //allTaskIdList.AddRange(waitMyLaunchFlow.Select(t => t.String("taskId")));
                //allTaskIdList.AddRange(myBusFlowInstance.Select(t => t.String("referFieldValue")));
                //var allTask = dataOp.FindAllByKeyValList("XH_DesignManage_Task", "taskId", allTaskIdList.Distinct().ToList()).ToList();

                List<string> allFlowIdList = new List<string>();
                allFlowIdList.AddRange(waitMyApprovalInstance.Select(t => t.String("flowId")));
                allFlowIdList.AddRange(waitMyLaunchFlow.Select(t => t.String("flowId")));
                allFlowIdList.AddRange(myBusFlowInstance.Select(t => t.String("flowId")));
                var allFlow = dataOp.FindAllByKeyValList("BusFlow", "flowId", allFlowIdList.Distinct().ToList()).ToList();

                //List<string> allProjIdList = new List<string>();
                //allProjIdList.AddRange(allTask.Select(t => t.String("projId")));
                //allProjIdList.AddRange(AllAssociatTaskList.Select(t => t.String("projId")));
                //allProjIdList.AddRange(AllAssociatPlanList.Select(t => t.String("projId")));
                //var allProject = dataOp.FindAllByKeyValList("XH_DesignManage_Project", "projId", allProjIdList.Distinct().ToList()).ToList();

                foreach (var item in flowList)
                {
                    if (item.Int("approvalUserId") == 0)
                        continue;
                    var referFieldValue = item.Int("referFieldValue");
                    var flowObj = allFlow.Where(c => c.Int("flowId") == item.Int("flowId")).FirstOrDefault();
                    // var connectObj = dataOp.FindOneByKeyVal(item.Text("tableName"), item.Text("referFieldName"), item.Text("referFieldValue"));
                    if (!fIds.Contains(item.Int("flowId")))
                        fIds.Add(item.Int("flowId"));
                    Pedding ped = new Pedding();
                    ped.type = 1;
                    ped.title = string.Format("{0}", item.Text("instanceName"));
                    ped.referFieldName = item.Text("referFieldName");
                    ped.referFieldValue = item.Int("referFieldValue");
                    ped.tableName = item.Text("tableName");
                    var u = dataOp.FindOneByKeyVal("SysUser", "userId", item.Text("approvalUserId"));
                    ped.StartMan = u.Text("name");
                    var trace = dataOp.FindAllByQuery("BusFlowTrace", Query.And(Query.EQ("flowInstanceId", item.Text("flowInstanceId")), Query.EQ("traceType", "0"))).OrderByDescending(t => t.Date("")).FirstOrDefault();
                    ped.StartDate = trace.Date("createDate").ToString("MM/dd");
                    //ped.url = string.Format("/DesignManage/TaskWorkFlowInfo/{0}", taskNode.Int("taskId"));

                    list.Add(ped);
                }

                foreach (var item in myBusFlowInstance)
                {
                    if (item.Int("approvalUserId") == 0)
                        continue;
                    var referFieldValue = item.Int("referFieldValue");
                    var connectObj = dataOp.FindOneByKeyVal(item.Text("tableName"), item.Text("referFieldName"), item.Text("referFieldValue"));
                    //  var flowObj = allFlow.Where(c => c.Int("flowId") == item.Int("flowId")).FirstOrDefault();
                    //bool flag = flowInstanceHelper.CanExecute(item.Text("flowId"), item.Text("flowInstanceId"), user.Int("userId"));
                    Pedding ped = new Pedding();
                    ped.type = 2;
                    ped.title = string.Format("{0}", item.Text("instanceName"));
                    ped.referFieldName = item.Text("referFieldName");
                    ped.referFieldValue = item.Int("referFieldValue");
                    ped.tableName = item.Text("tableName");
                    var u = dataOp.FindOneByKeyVal("SysUser", "userId", item.Text("approvalUserId"));
                    ped.StartMan = u.Text("name");
                    var trace = dataOp.FindAllByQuery("BusFlowTrace", Query.And(Query.EQ("flowInstanceId", item.Text("flowInstanceId")), Query.EQ("traceType", "0"))).OrderByDescending(t => t.Date("createDate")).FirstOrDefault();
                    ped.StartDate = trace.Date("createDate").ToString("MM/dd");
                    //ped.url = string.Format("/DesignManage/TaskWorkFlowInfo/{0}", taskNode.Int("taskId"));
                    list.Add(ped);
                }
            }

            int allCount = list.Count();
            Pedding last = new Pedding();

            list = list.Skip((current - 1) * pageSize).Take(pageSize).ToList();
            int remainCount = allCount - current * pageSize;
            last.referFieldValue = remainCount < 0 ? 0 : remainCount;
            list.Add(last);
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        public JsonResult PeddingDesignChangeList()
        {
            int current = PageReq.GetParamInt("current");
            current = current == 0 ? 1 : current;
            int pageSize = 15;
            string loginName = PageReq.GetParam("loginName");
            int type = PageReq.GetParamInt("type");
            List<Pedding> list = new List<Pedding>();
            BsonDocument user = dataOp.FindOneByKeyVal("SysUser", "loginName", loginName);
            List<BsonDocument> flowList = new List<BsonDocument>();
            var waitMyApprovalInstance = new List<BsonDocument>();
            var waitMyLaunchFlow = new List<BsonDocument>();
            var myBusFlowInstance = new List<BsonDocument>();
            if (user != null)
            {
                var flowInstanceHelper = new Yinhe.ProcessingCenter.BusinessFlow.FlowInstanceHelper();

                if (type == 0)
                {
                    //waitMyLaunchFlow = flowInstanceHelper.GetUserWaitForStartFlow(user.Int("userId")); //等待我发起
                   // flowList.AddRange(waitMyLaunchFlow);
                }
                else if (type == 1)
                {

                    myBusFlowInstance = flowInstanceHelper.GetUserAssociatedFlowInstance(user.Int("userId"), 0); //涉及我的审批
                }
                else if (type == 2)
                {
                    waitMyApprovalInstance = flowInstanceHelper.GetUserWaitForApprovaleFlow(user.Int("userId")); //等待我审批
                    flowList.AddRange(waitMyApprovalInstance);
                }




                //List<string> allTaskIdList = new List<string>();
                //allTaskIdList.AddRange(waitMyApprovalInstance.Select(t => t.String("referFieldValue")));
                //allTaskIdList.AddRange(myBusFlowInstance.Select(t => t.String("referFieldValue")));
                //var allTask = dataOp.FindAllByKeyValList("DesignChange", "designChangeId", allTaskIdList.Distinct().ToList()).ToList();

                //List<BsonDocument> taskMangerList = dataOp.FindAllByKeyVal("XH_DesignManage_TaskManager", "userId", user.Text("userId")).ToList();
                ////获取计划负责人列表
                //var planManagerList = dataOp.FindAllByKeyVal("XH_DesignManage_PlanManager", "userId", user.Text("userId")).ToList();
                //var taskIds = taskMangerList.Select(c => c.Text("taskId"));
                //var planIds = planManagerList.Select(c => c.Text("planId"));
                ////获取想
                //var AllAssociatTaskList = dataOp.FindAllByKeyValList("XH_DesignManage_Task", "taskId", taskIds.ToList());
                //var AllAssociatPlanList = dataOp.FindAllByKeyValList("XH_DesignManage_Plan", "taskId", planIds.ToList());


                ////获取我设计的待阅任务列表
                //var allInstance = dataOp.FindAll("BusFlowInstance").ToList();
                //var queryStr = string.Format("givenUserId={0}&status=0", user.Int("userId"));
                //var circulateTaskList = dataOp.FindAllByQueryStr("BusinessFlowCirculation", queryStr);

                List<int> fIds = new List<int>();



                //List<string> allTaskIdList = new List<string>();
                //allTaskIdList.AddRange(waitMyApprovalInstance.Select(t => t.String("referFieldValue")));
                //allTaskIdList.AddRange(waitMyLaunchFlow.Select(t => t.String("taskId")));
                //allTaskIdList.AddRange(myBusFlowInstance.Select(t => t.String("referFieldValue")));
                //var allTask = dataOp.FindAllByKeyValList("XH_DesignManage_Task", "taskId", allTaskIdList.Distinct().ToList()).ToList();

                List<string> allFlowIdList = new List<string>();
                allFlowIdList.AddRange(waitMyApprovalInstance.Select(t => t.String("flowId")));
                allFlowIdList.AddRange(waitMyLaunchFlow.Select(t => t.String("flowId")));
                allFlowIdList.AddRange(myBusFlowInstance.Select(t => t.String("flowId")));
                var allFlow = dataOp.FindAllByKeyValList("BusFlow", "flowId", allFlowIdList.Distinct().ToList()).ToList();

                //List<string> allProjIdList = new List<string>();
                //allProjIdList.AddRange(allTask.Select(t => t.String("projId")));
                //allProjIdList.AddRange(AllAssociatTaskList.Select(t => t.String("projId")));
                //allProjIdList.AddRange(AllAssociatPlanList.Select(t => t.String("projId")));
                //var allProject = dataOp.FindAllByKeyValList("XH_DesignManage_Project", "projId", allProjIdList.Distinct().ToList()).ToList();

                foreach (var item in flowList)
                {
                    if (item.Int("approvalUserId") == 0)
                        continue;
                    var referFieldValue = item.Int("referFieldValue");
                    var flowObj = allFlow.Where(c => c.Int("flowId") == item.Int("flowId")).FirstOrDefault();
                     var connectObj = dataOp.FindOneByKeyVal(item.Text("tableName"), item.Text("referFieldName"), item.Text("referFieldValue"));
                    if (!fIds.Contains(item.Int("flowId")))
                        fIds.Add(item.Int("flowId"));
                    Pedding ped = new Pedding();
                    ped.type = 1;
                    ped.title = string.Format("{0}", connectObj.Text("name"));
                    ped.referFieldName = item.Text("referFieldName");
                    ped.referFieldValue = item.Int("referFieldValue");
                    ped.flowType = GetFlowTypeByTableName(item.Text("tableName"));
                    ped.tableName = item.Text("tableName");
                    var u = dataOp.FindOneByKeyVal("SysUser", "userId", item.Text("approvalUserId"));
                    ped.StartMan = u.Text("name");
                    var trace = dataOp.FindAllByQuery("BusFlowTrace", Query.And(Query.EQ("flowInstanceId", item.Text("flowInstanceId")), Query.EQ("traceType", "0"))).OrderByDescending(t => t.Date("")).FirstOrDefault();
                    ped.StartDate = trace.Date("createDate").ToString("MM/dd");
                    //ped.url = string.Format("/DesignManage/TaskWorkFlowInfo/{0}", taskNode.Int("taskId"));

                    list.Add(ped);
                }

                foreach (var item in myBusFlowInstance)
                {
                  
                    var referFieldValue = item.Int("referFieldValue");
                    var connectObj = dataOp.FindOneByKeyVal(item.Text("tableName"), item.Text("referFieldName"), item.Text("referFieldValue"));
                    if (item.Int("approvalUserId") == 0)
                        continue;
                    
                    //  var flowObj = allFlow.Where(c => c.Int("flowId") == item.Int("flowId")).FirstOrDefault();
                    //bool flag = flowInstanceHelper.CanExecute(item.Text("flowId"), item.Text("flowInstanceId"), user.Int("userId"));
                    Pedding ped = new Pedding();
                    ped.type = 2;
                    ped.title = connectObj.Text("name");
                    ped.referFieldName = item.Text("referFieldName");
                    ped.referFieldValue = item.Int("referFieldValue");
                    ped.tableName = item.Text("tableName");
                    ped.flowType = GetFlowTypeByTableName(item.Text("tableName"));
                    var u = dataOp.FindOneByKeyVal("SysUser", "userId", item.Text("approvalUserId"));
                    ped.StartMan = u.Text("name");
                    var trace = dataOp.FindAllByQuery("BusFlowTrace", Query.And(Query.EQ("flowInstanceId", item.Text("flowInstanceId")), Query.EQ("traceType", "0"))).OrderByDescending(t => t.Date("createDate")).FirstOrDefault();
                    ped.StartDate = trace.Date("createDate").ToString("MM/dd");
                    //ped.url = string.Format("/DesignManage/TaskWorkFlowInfo/{0}", taskNode.Int("taskId"));
                    list.Add(ped);
                }
            }

            int allCount = list.Count();
            Pedding last = new Pedding();
            list = list.OrderByDescending(t => t.StartDate).ToList();
            list = list.Skip((current - 1) * pageSize).Take(pageSize).ToList();
            int remainCount = allCount - current * pageSize;
            last.referFieldValue = remainCount < 0 ? 0 : remainCount;
            list.Add(last);
            return Json(list, JsonRequestBehavior.AllowGet);
        }
        //<summary>
        //获取流程实例
        //</summary>
        //<returns></returns>
        public JsonResult GetPeddingInstance()
        {
            string referFieldValue = PageReq.GetParam("referFieldValue");
            string tableName = PageReq.GetParam("tableName");
            string referFieldName = PageReq.GetParam("referFieldName");
            int userId = PageReq.GetParamInt("userId");
            var model = dataOp.FindOneByKeyVal(tableName, referFieldName, referFieldValue);
            BsonDocument tflowObj = new BsonDocument();
            ApprovalModel appmodel = new ApprovalModel();
            appmodel.actList = new List<ActionModel>();
            string tName = "";
            int flowType = 0;
            switch (tableName)
            {
                case "XH_DesignManage_Task":
                    flowType = 1;
                    tName = "XH_DesignManage_TaskBusFlow";
                    break;

                case "DesignChange":
                    flowType = 2;
                    tName = "DesignChangeBusFlow";
                    break;

                case "ProjScheme":
                    flowType = 3;
                    tName = "ProjSchemeBusFlow";
                    break;

                case "ProgrammeEvaluation":
                    flowType = 4;
                    tName = "ProgrammeEvaluationBusFlow";
                    break;
            }

            tflowObj = dataOp.FindOneByKeyVal(tName, referFieldName, referFieldValue);

            var flowId = tflowObj.Int("flowId");
            var flowObj = dataOp.FindOneByKeyVal("BusFlow", "flowId", tflowObj.Text("flowId"));
            //获取步骤下 对应
            var stepList = flowObj.ChildBsonList("BusFlowStep").OrderBy(c => c.Int("stepOrder")).ToList();
            var bootStep = stepList.Where(c => c.Int("actTypeId") == (int)FlowActionType.Launch).FirstOrDefault();
            var curFlowInstance = flowObj.ChildBsonList("BusFlowInstance").Where(c => c.Text("tableName") == tableName && c.Text("referFieldName") == referFieldName && c.Text("referFieldValue") == referFieldValue).OrderByDescending(c => c.Date("createDate")).FirstOrDefault();


            FlowInstanceHelper helper = new FlowInstanceHelper();
            bool hasOperateRight = false;
            string curAvaiableUserName = "";
            var curStep = helper.InitialExecuteCondition(tflowObj.Text("flowId"), curFlowInstance.Text("flowInstanceId"), userId, ref hasOperateRight, ref curAvaiableUserName);
            if (hasOperateRight)
            {

                if (curFlowInstance != null && curFlowInstance.Int("instanceStatus") != 1 && curStep.Int("actTypeId") != (int)FlowActionType.Launch)//具体动作
                {
                    var actionList = dataOp.FindAllByKeyVal("BusFlowAction", "actTypeId", curStep.Text("actTypeId")).ToList();
                    appmodel.actList.AddRange(ModelConver(actionList));
                    //会签并且不是核稿人过滤驳回步骤

                    if (curStep.Int("isChecker") == 0 && curStep.Int("actTypeId") == (int)FlowActionType.Countersign)
                    {
                        actionList = actionList.Where(c => c.Int("type") != 3).ToList();
                    }

                }
                else
                {
                    var act = dataOp.FindAllByKeyVal("BusFlowAction", "type", "0").FirstOrDefault();

                    act.Remove("name");
                    if (curFlowInstance.Int("instanceStatus") != 1)//发起
                    {
                        act.Add("name", "发起");

                    }
                    else//重新发起
                    {
                        act.Add("name", "重新发起");
                    }
                    appmodel.actList.AddRange(ModelConver(new List<BsonDocument> { act }));
                }
            }
            var drafterOrg=dataOp.FindOneByQuery("Organization",Query.EQ("orgId",model.Text("drafterOrgId")));
            BsonDocument startDepartment = dataOp.FindOneByKeyVal("Organization", "orgId", model.String("orgId"));
             var drafter = dataOp.FindOneByQuery("SysUser", Query.EQ("userId", model.Text("drafterId")));

             var queryStr = string.Format("flowInstanceId={0}", curFlowInstance.Text("flowInstanceId"));
             var logList = dataOp.FindAllByQueryStr("BusFlowTrace", queryStr).Where(c => c.Int("traceType") == 2 || c.Int("traceType") == 6 || c.Int("traceType") == -1 || c.Int("traceType") == 8).ToList();
            appmodel.flowId = flowId.ToString();
            appmodel.flowInstanceId = curFlowInstance.Text("flowInstanceId");
            appmodel.stepId = curStep.Text("stepId");
            appmodel.instanceName = model.Text("name");
            appmodel.tableName = tableName;
            appmodel.referFieldName = referFieldName;
            appmodel.referFieldValue = referFieldValue;
            appmodel.engName = model.SourceBsonField("projEngId", "name");
            appmodel.changeReason = model.Text("changeNum");
            appmodel.changeDate = model.Int("saveStatus")==1?model.ShortDate("startTime"):string.Empty;
            appmodel.changeContent = model.Text("reason");
            appmodel.department = drafterOrg.Text("name");
            appmodel.doMan = drafter.Text("name");
            appmodel.approvalLogCount = logList.Count();
            appmodel.startDepartment = startDepartment.Text("name");
            appmodel.writeTime = model.ShortDate("writeTime");
            appmodel.remark = model.Text("remark");
            appmodel.startTime = model.Int("saveStatus") == 1 ? model.ShortDate("startTime") : string.Empty;
            appmodel.flowType = flowType;
            return Json(appmodel, JsonRequestBehavior.AllowGet);
        }



        public int GetFlowTypeByTableName(string tableName)
        {
            int flowType = 0;
            switch (tableName)
            {
                case "XH_DesignManage_Task":
                    flowType = 1;
                   // tName = "XH_DesignManage_TaskBusFlow";
                    break;

                case "DesignChange":
                    flowType = 2;
                   // tName = "DesignChangeBusFlow";
                    break;

                case "ProjScheme":
                    flowType = 3;
                    //tName = "ProjSchemeBusFlow";
                    break;

                case "ProgrammeEvaluation":
                    flowType = 4;
                   // tName = "ProgrammeEvaluationBusFlow";
                    break;
            }

            return flowType;
        }

        /// <summary>
        /// 获取工程列表
        /// </summary>
        /// <returns></returns>
        public JsonResult GetEngineerList()
        {
            return Json(GetEngList(0), JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 获取项目详细信息
        /// </summary>
        /// <returns></returns>
        public JsonResult EngineerInfo()
        {
            int engId = PageReq.GetParamInt("engId");
            EngineerModel model = new EngineerModel();
            model = GetEngList(engId).FirstOrDefault();
            return Json(model, JsonRequestBehavior.AllowGet);
        }

        public List<EngineerModel> GetEngList(int engId)
        {
            List<EngineerModel> retengList = new List<EngineerModel>();
            var engList = dataOp.FindAll("XH_DesignManage_Engineering").ToList();
            if (engId != 0)
            {
                engList = engList.Where(t => t.Int("engId") == engId).ToList();
            }
            var projList = dataOp.FindAll("XH_DesignManage_Project").ToList(); 
            foreach (var item in engList)
            {
                EngineerModel doc = new EngineerModel();
                doc.projList = new List<ProjectModel>();
                doc.engId = item.Int("engId");
                doc.name = item.Text("name");
                doc.area = item.Text("area");
                doc.buildArea = item.Text("buildArea");
                doc.volumeRate = item.Text("volumeRate");
                var subProjList = projList.Where(t => t.Int("engId") == item.Int("engId")).ToList();
                List<BsonDocument> fileList = dataOp.FindAllByQueryStr("FileRelation", string.Format("tableName={0}&keyName={1}&keyValue={2}&fileObjId={3}", "XH_DesignManage_Engineering", "engId", item.Text("engId"), 29)).ToList();
                //工程封面图
                var relModel = fileList.Where(t => t.Text("isCover").ToLower() == "true").FirstOrDefault();
                string path = "";
                if (relModel != null)
                {
                    var fileModel = dataOp.FindOneByKeyVal("FileLibrary", "fileId", relModel.Text("fileId"));
                    path = fileModel.Text("thumbPicPath").Replace("_m.", "_l.");
                }
                else
                {
                    path = "blank";
                }
                doc.thumbPic = path;
                foreach (var entity in subProjList)
                {
                    List<BsonDocument> fileRelList = dataOp.FindAllByQueryStr("FileRelation", "tableName=XH_DesignManage_Project&fileObjId=28&keyValue=" + entity.Int("projId").ToString()).OrderByDescending(t => DateTime.Parse(t.Text("createDate"))).ToList();
                    List<string> fileIdList = new List<string>();

                    fileIdList = fileRelList.Select(t => t.String("fileId")).ToList();
                    var coverModel = dataOp.FindAllByKeyValList("FileLibrary", "fileId", fileIdList).Where(t => t.Int("fileTypeId") == 1).FirstOrDefault(); 

                    ProjectModel e = new ProjectModel();
                    e.name = entity.Text("name");
                    e.projId = entity.Int("projId");
                    e.thumbPic = coverModel != null ? coverModel.Text("thumbPicPath").Replace("_m.", "_us.") : "blank";
                    doc.projList.Add(e);
                }
                retengList.Add(doc);
            }

            return retengList;
        }
        /// <summary>
        /// 获取工程列表
        /// </summary>
        /// <returns></returns>
        public JsonResult EngineerList()
        {
            string keyWord = PageReq.GetParam("keyWord");

            List<CityModel> cityList = new List<CityModel>();
            var engList = dataOp.FindAll("XH_DesignManage_Engineering").Where(t => !string.IsNullOrEmpty(t.Text("name"))).ToList();
            var allcityList = dataOp.FindAll("XH_ProductDev_City").Where(t => !string.IsNullOrEmpty(t.Text("name"))).ToList();

            if (!string.IsNullOrEmpty(keyWord))
            {
                engList = engList.Where(t => t.Text("name").Contains(keyWord)).ToList();
                allcityList = allcityList.Where(a => engList.Select(t => t.Int("cityId")).Distinct().ToList().Contains(a.Int("cityId"))).ToList();
            }
            foreach (var item in allcityList)
            {
                CityModel doc = new CityModel();
                doc.engList = new List<EngineerModel>();
                doc.cityId = item.Int("cityId");
                doc.name = item.Text("name");

                var subEngList = engList.Where(t => t.Int("cityId") == item.Int("cityId")).ToList();

                foreach (var entity in subEngList)
                {
                    EngineerModel e = new EngineerModel();
                    e.name = entity.Text("name");
                    e.engId = entity.Int("engId");
                    e.area = entity.Text("area");
                    e.buildArea = entity.Text("buildArea");
                    e.volumeRate = entity.Text("volumeRate");
                    doc.engList.Add(e);
                }
                cityList.Add(doc);
            }
            return Json(cityList, JsonRequestBehavior.AllowGet);
        }

        #region 公共
        /// <summary>
        /// 获得审批日志
        /// </summary>
        /// <returns></returns>
        public JsonResult GetApproveLog()
        {
            var flowInstanceId = PageReq.GetParamInt("flowInstanceId");
            var queryStr = string.Format("flowInstanceId={0}", flowInstanceId);
            var logList = dataOp.FindAllByQueryStr("BusFlowTrace", queryStr).Where(c => c.Int("traceType") == 2 || c.Int("traceType") == 6 || c.Int("traceType") == -1 || c.Int("traceType") == 8).ToList();
            var stepUserIdList = new List<int>();
            //判断流程是否已经发起
            var lastOperateDate = DateTime.Now;
            var lastStepId = 0;
            var hitAllUser = string.Empty;
            var hitShortUser = string.Empty;
            var sameStepOperateDate = DateTime.Now;
            List<ApproveLog> alogList = new List<ApproveLog>();
            var index = 1;
            foreach (var log in logList.OrderBy(c => c.Int("order")))
            {
                ApproveLog logg = new ApproveLog();
                var AllUserString = new List<string>();
                var ShortUserString = new List<string>();
                var action = log.SourceBson("actId");
                var form = log.SourceBson("formId");
                var content = form != null ? form.Text("content") : string.Empty;
                var actionTypeName = action != null ? action.SourceBsonField("actTypeId", "name") : "系统自动执行";
                var result = action != null ? action.Text("name") : string.Empty;
                var preStep = log.SourceBson("preStepId");

                var preStepName = preStep != null ? preStep.SourceBsonField("flowPosId", "name") : string.Empty;
                var helper = new Yinhe.ProcessingCenter.BusinessFlow.FlowInstanceHelper();
                var curFlowInstance = log.SourceBson("flowInstanceId");
                if (curFlowInstance != null && log.Int("flowInstanceId") != 0)
                {
                    stepUserIdList = helper.GetFlowInstanceAvaiableStepUser(curFlowInstance.Int("flowId"), log.Int("flowInstanceId"), preStep.Int("stepId"));
                }
                var userList = dataOp.FindAllByKeyValList("SysUser", "userId", stepUserIdList.Select(c => c.ToString()).ToList());
                var userStr = string.Join(",", userList.Select(c => c.Text("name")).ToArray());
                ShortUserString.Add(StringExtension.CutStr(userStr, 8, "..."));
                AllUserString.Add(userStr);
                hitShortUser = string.Join(@"<br>", ShortUserString.ToArray());
                hitAllUser = string.Join(@"<br>", AllUserString.ToArray());
                var hitShortUserObj = dataOp.FindOneByKeyVal("SysUser", "userId", log.Text("createUserId"));
                var titleApprovalUser = hitAllUser;
                var approvalUser = hitShortUser;
                if (index == 1)
                {
                    lastOperateDate = log.Date("createDate");
                    lastStepId = log.Int("nextStepId");
                }
                TimeSpan spendTime = new TimeSpan();
                if (index != 1)
                {
                    spendTime = log.Date("createDate") - lastOperateDate;

                }

                switch (log.Int("traceType"))
                {
                    case -1:
                        if (hitShortUserObj != null)
                        {
                            approvalUser = hitShortUserObj.Text("name");
                            titleApprovalUser = approvalUser;
                        }
                        actionTypeName = "启动流程"; result = "启动流程"; break;
                    case 0:

                        result = "启动流程"; break;
                    case 1: result = "系统自动执行"; break;
                    case 2:
                        if (actionTypeName == "发起" && hitShortUserObj != null)
                        {
                            approvalUser = hitShortUserObj.Text("name");
                            titleApprovalUser = approvalUser;
                        }
                        //获取距离真正执行的上次操作时间
                        if (lastStepId == log.Int("preStepId"))
                        {
                            sameStepOperateDate = lastOperateDate;
                            lastOperateDate = log.Date("createDate");
                            lastStepId = log.Int("nextStepId");
                        }
                        else
                        {
                            //if (index != 1)
                            //{
                            //    spendTime = log.Date("createDate") - lastOperateDate;
                            //}
                        }


                        break;
                    case 3: actionTypeName = "执行回滚操作"; result = "回滚"; break;
                    //case 4: actionTypeName = "重启流程"; result = "重启流程"; break;
                    case 5: actionTypeName = "废弃流程"; result = "流程结束"; break;
                    case 6:

                        if (hitShortUserObj != null)
                        {
                            approvalUser = hitShortUserObj.Text("name");
                            titleApprovalUser = approvalUser;
                        }
                        actionTypeName = "转办"; result = log.Text("result"); break;

                    case 8:

                        if (hitShortUserObj != null)
                        {
                            approvalUser = hitShortUserObj.Text("name");
                            titleApprovalUser = approvalUser;
                        }
                        actionTypeName = "传阅"; result = log.Text("result"); break;
                    default: break;
                }

                logg.preStepName = preStepName;
                logg.approvalUser = approvalUser;
                logg.createDate = log.Date("createDate").ToShortDateString();
                logg.content = string.IsNullOrEmpty(content) ? log.Text("remark") : content;
                logg.actionTypeName = actionTypeName;
                logg.result = result;
                alogList.Add(logg);
            }
            return Json(alogList, JsonRequestBehavior.AllowGet);


        }
        #endregion
        /// <summary>
        /// 对象转换
        /// </summary>
        /// <param name="docList"></param>
        /// <returns></returns>
        public List<ActionModel> ModelConver(List<BsonDocument> docList)
        {
            List<ActionModel> list = new List<ActionModel>();

            foreach (var item in docList)
            {
                ActionModel mod = new ActionModel();
                mod.actId = item.Int("actId");
                mod.actTypeId = item.Int("actTypeId");
                mod.type = item.Int("type");
                mod.name = item.Text("name");
                list.Add(mod);

            }
            return list;
        }



        #endregion

        #region 项目 地块角色权限设置
        /// <summary>
        ///创建地块项目扩展角色
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        public JsonResult SaveExtendRole(FormCollection saveForm)
        {
            PageJson json = new PageJson();
            InvokeResult result = new InvokeResult();
            string roleName = PageReq.GetForm("name");//角色名称
            if (string.IsNullOrEmpty(roleName))
            {
                json.Success = false;
                json.Message = "角色名称不能为空!";
                return Json(json);
            }
            string tbName = PageReq.GetForm("tbName");
            string queryStr = PageReq.GetForm("queryStr");
            string modulOperating = PageReq.GetForm("modulOperating");//模块操作项 格式：模块Id_操作项Id@模块Id_操作项Id...
            string userIds = PageReq.GetForm("userIds");//角色所选人员
            string orgIds = PageReq.GetForm("orgIds");//角色所选部门
            int landOrProjId = PageReq.GetFormInt("landOrProjId");//项目或地块的Id
            int curLandId = landOrProjId;
            int landOrProj = PageReq.GetFormInt("landOrProj");//1：地块角色  2 ：项目角色
            if (landOrProj == 2)
            {
                var projInfo = dataOp.FindOneByQuery("Project", Query.EQ("projId", landOrProjId.ToString()));
                curLandId = projInfo.Int("landId");
            }
            //判断是否重名
            List<BsonDocument> tempRole = dataOp.FindAllByQuery("SysRole", Query.And(Query.EQ("landOrProj", landOrProj.ToString()), Query.EQ("landOrProjId", landOrProjId.ToString()), Query.EQ("name", roleName))).ToList();//该名字存在的角色列表
            if (tempRole.Count() > 0)
            {
                if (string.IsNullOrEmpty(queryStr))
                {
                    json.Success = false;
                    json.Message = "存在同名角色,请从新命名!";
                }
                else
                {
                    var curRole = dataOp.FindOneByQuery("SysRole", TypeConvert.NativeQueryToQuery(queryStr));
                    if (tempRole.Where(x => x.String("roleId") != curRole.String("roleId")).Count() > 0)
                    {
                        json.Success = false;
                        json.Message = "存在同名角色,请从新命名!";
                    }
                }
            }
            BsonDocument dataBson = new BsonDocument();
            foreach (var tempKey in saveForm.AllKeys)
            {
                if (tempKey == "tbName" || tempKey == "queryStr" || tempKey.Contains("fileList[") || tempKey.Contains("param.") || tempKey == "modulOperating" || tempKey == "userIds" || tempKey == "orgIds") continue;

                dataBson.Add(tempKey, PageReq.GetForm(tempKey));
            }
            #region 保存数据
            result = dataOp.Save(tbName, queryStr != "" ? TypeConvert.NativeQueryToQuery(queryStr) : Query.Null, dataBson);

            #endregion

            #region 分配角色 地块 项目的权限 及操作项权限
            List<StorageData> dataList = new List<StorageData>();
            if (result.Status == Status.Successful && string.IsNullOrEmpty(queryStr))
            {

                //分配角色地块权限
                StorageData data = new StorageData();
                data.Document = new BsonDocument().Add("status", "0").Add("roleId", result.BsonInfo.String("roleId")).Add("remark", "地块权限").Add("dataTableName", "Land").Add("dataFeiIdName", "landId").Add("dataId", curLandId.ToString()).Add("roleCategoryId", "1");
                data.Name = "DataScope";
                data.Type = StorageType.Insert;
                dataList.Add(data);
                if (landOrProj == 2)
                {
                    //分配角色项目权限
                    data = new StorageData();
                    data.Document = new BsonDocument().Add("status", "0").Add("roleId", result.BsonInfo.String("roleId")).Add("remark", "项目权限").Add("dataTableName", "Project").Add("dataFeiIdName", "projId").Add("dataId", landOrProjId.ToString()).Add("roleCategoryId", "1");
                    data.Name = "DataScope";
                    data.Type = StorageType.Insert;
                    dataList.Add(data);
                }
                StorageData tempData = new StorageData();
                BsonDocument tempProjView = new BsonDocument();
                tempProjView.Add("roleId", result.BsonInfo.String("roleId"));
                tempProjView.Add("modulId", "19");
                tempProjView.Add("code", "PROJECTLIB_VIEW");
                tempProjView.Add("dataObjId", "20");
                tempProjView.Add("operatingId", "1");
                tempData.Document = tempProjView;
                tempData.Type = StorageType.Insert;
                tempData.Name = "SysRoleRight";
                dataList.Add(tempData);
            }

            if (result.Status == Status.Successful)
            {
                int roleId = result.BsonInfo.Int("roleId");
                json.AddInfo("roleId", result.BsonInfo.String("roleId"));
                json.AddInfo("name", result.BsonInfo.String("name"));
                dataList.AddRange(GetRoleOrgUserData("SysRoleUser", roleId, userIds, "userId"));//更新角色的用户关联
                dataList.AddRange(GetRoleOrgUserData("SysRoleOrg", roleId, orgIds, "orgId"));//更新角色的部门关联
                dataList.AddRange(GetReloRightData(roleId, modulOperating));//增加模块操作权限
                result = dataOp.BatchSaveStorageData(dataList);
            }

            #endregion
            if (result.Status == Status.Successful)
            {
                json.Success = true;
                json.Message = "创建角色及权限成功!";
                if (!string.IsNullOrEmpty(queryStr))
                {
                    json.Message = "编辑角色及权限成功!";
                }
            }
            else
            {
                json.Success = false;
                json.Message = "创建角色失败,请刷新后重试!";
                if (!string.IsNullOrEmpty(queryStr))
                {
                    json.Message = "编辑角色失败,请刷新后重试!";
                }
                else
                {
                    //创建角色失败 删除角色及关联的数据
                    dataOp.Delete("DataScope", Query.EQ("roleId", json.htInfo["roleId"].ToString()));
                    dataOp.Delete("SysRoleRight", Query.EQ("roleId", json.htInfo["roleId"].ToString()));
                    dataOp.Delete("SysRoleUser", Query.EQ("roleId", json.htInfo["roleId"].ToString()));
                    dataOp.Delete("SysRoleOrg", Query.EQ("roleId", json.htInfo["roleId"].ToString()));
                    dataOp.Delete("SysRole", Query.EQ("roleId", json.htInfo["roleId"].ToString()));

                }

            }
            return Json(json);
        }
        /// <summary>
        /// 获取需要更新角色关联数据
        /// </summary>
        /// <param name="tbName">表名</param>
        /// <param name="roleId">角色Id</param>
        /// <param name="keyValues">关联数据的键值</param>
        /// <param name="key">关联数据的键名</param>
        /// <returns></returns>
        public List<StorageData> GetRoleOrgUserData(string tbName, int roleId, string keyValues, string key)
        {
            keyValues = keyValues.Trim(',');
            TableRule table = new TableRule(tbName);
            string primaryKey = table.GetPrimaryKey();//获得主键名
            if (string.IsNullOrEmpty(primaryKey)) //当在插件调试时获取不到主键
            {
                if (tbName == "SysRoleUser")
                {
                    primaryKey = "roleUserId";
                }
                else if (tbName == "SysRoleOrg")
                {
                    primaryKey = "roleOrgId";
                }
            }
            List<StorageData> dataList = new List<StorageData>();//需要操作的数据 保存要删除的和要新增的关联
            string[] idArray = keyValues.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);//获取键值
            List<BsonDocument> isExit = dataOp.FindAllByQuery(tbName, Query.EQ("roleId", roleId.ToString())).ToList();//获取已经存在的角色关联数据
            if (idArray.Count() > 0)
            {
                foreach (var tempId in idArray)
                {
                    var isHasExit = isExit.Where(x => x.String("roleId") == roleId.ToString() && x.String(key) == tempId).FirstOrDefault();
                    if (isHasExit == null)
                    {
                        //不存在该关联 新增关联
                        StorageData tempData = new StorageData();
                        tempData.Name = tbName;
                        tempData.Type = StorageType.Insert;
                        isHasExit = new BsonDocument();
                        isHasExit.Add("roleId", roleId.ToString());
                        isHasExit.Add(key, tempId);
                        tempData.Document = isHasExit;
                        dataList.Add(tempData);
                    }
                    else
                    {
                        isExit.Remove(isHasExit);//已经存在且在新增队列中的从已存在移除
                    }
                }
            }
            //删除已经在存在的权限并且不在新增队列中的 
            if (isExit.Count() > 0)
            {
                foreach (var tempRel in isExit)
                {
                    StorageData tempData = new StorageData();
                    tempData.Document = tempRel;
                    tempData.Name = tbName;
                    tempData.Type = StorageType.Delete;
                    tempData.Query = Query.EQ(primaryKey, tempRel.String(primaryKey));
                    dataList.Add(tempData);
                }
            }
            return dataList;
        }
        /// <summary>
        /// 获取角色项目地块权限
        /// </summary>
        /// <param name="roleId"></param>
        /// <param name="rightCode">格式模块Id_权限Id@模块Id_权限Id...</param>
        /// <returns></returns>
        public List<StorageData> GetReloRightData(int roleId, string rightCode)
        {
            rightCode = rightCode.Trim('@');
            Dictionary<string, List<string>> modulRightDC = new Dictionary<string, List<string>>();
            List<BsonDocument> hasExitRight = dataOp.FindAllByQuery("SysRoleRight", Query.EQ("roleId", roleId.ToString())).ToList();//已经存在的权限
            List<BsonDocument> delRight = new List<BsonDocument>();//要被删除的权限
            List<StorageData> dataList = new List<StorageData>();//需要操作的数据 保存要删除的权限和要新增的权限
            List<string> operatIds = new List<string>();//新增加的操作项Ids
            if (!string.IsNullOrEmpty(rightCode))
            {
                string[] array = rightCode.Split(new string[] { "@" }, StringSplitOptions.RemoveEmptyEntries);
                try
                {
                    if (array.Length > 0)
                    {
                        foreach (var item in array)
                        {
                            string[] subStr = item.Split(new string[] { "_" }, StringSplitOptions.RemoveEmptyEntries);
                            if (subStr.Length == 2)
                            {
                                if (modulRightDC.ContainsKey(subStr[0]))
                                {
                                    modulRightDC[subStr[0]].Add(subStr[1]);
                                }
                                else
                                {
                                    modulRightDC.Add(subStr[0], new List<string> { subStr[1] });
                                }
                                operatIds.Add(subStr[1]);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {

                }
                //modulRightDC = rightCode.ParamStringSplit();
                List<string> modulIds = modulRightDC.Keys.Distinct().ToList();//新增加模块Ids
                operatIds = operatIds.Distinct().ToList();//新增加的操作项Ids
                List<BsonDocument> modulList = dataOp.FindAllByQuery("SysModule", Query.In("modulId", TypeConvert.StringListToBsonValueList(modulIds))).ToList();
                List<BsonDocument> operatingList = dataOp.FindAllByQuery("SysOperating", Query.In("operatingId", TypeConvert.StringListToBsonValueList(operatIds))).ToList();

                if (modulRightDC.Count() > 0)
                {
                    foreach (var tempDc in modulRightDC)
                    {
                        BsonDocument curModul = modulList.Where(x => x.String("modulId") == tempDc.Key).FirstOrDefault();
                        foreach (var str in tempDc.Value)
                        {
                            BsonDocument curOperating = operatingList.Where(x => x.String("operatingId") == str).FirstOrDefault();
                            if (curModul != null && curOperating != null)
                            {
                                BsonDocument isHasExit = hasExitRight.Where(x => x.String("code") == curModul.String("code") + "_" + curOperating.String("code") && x.String("modulId") == tempDc.Key).FirstOrDefault();//判断是否改角色已经存在该模块的权限 
                                if (isHasExit == null)
                                {
                                    //不存在权限项 新增该权限项
                                    StorageData tempData = new StorageData();
                                    tempData.Name = "SysRoleRight";
                                    tempData.Type = StorageType.Insert;
                                    isHasExit = new BsonDocument();
                                    isHasExit.Add("roleId", roleId.ToString());
                                    isHasExit.Add("modulId", curModul.String("modulId"));
                                    isHasExit.Add("operatingId", curOperating.String("operatingId"));
                                    isHasExit.Add("dataObjId", "20");
                                    isHasExit.Add("name", curOperating.String("name") + curModul.String("name"));
                                    isHasExit.Add("code", curModul.String("code") + "_" + curOperating.String("code"));
                                    tempData.Document = isHasExit;
                                    dataList.Add(tempData);
                                }
                                else
                                {
                                    hasExitRight.Remove(isHasExit);//已经存在且在新增队列中的从已存在移除
                                }
                            }
                        }
                    }
                }
            }
            //删除已经在存在的权限并且不在新增队列中的 
            if (hasExitRight.Count() > 0)
            {
                foreach (var tempHasRight in hasExitRight)
                {
                    StorageData tempData = new StorageData();
                    tempData.Document = tempHasRight;
                    tempData.Name = "SysRoleRight";
                    tempData.Type = StorageType.Delete;
                    tempData.Query = Query.EQ("roleOrgId", tempHasRight.String("roleOrgId"));
                    dataList.Add(tempData);
                }
            }
            return dataList;
        }


        /// <summary>
        /// 批量保存角色项目地块权限
        /// </summary>
        /// <param name="roleId"></param>
        /// <param name="rightCode">格式模块Id_权限Id@模块Id_权限Id...</param>
        /// <returns></returns>
        public JsonResult SaveReloRight(int roleId, string rightCode)
        {
            rightCode = rightCode.Trim('@');
            PageJson json = new PageJson();
            InvokeResult result = new InvokeResult();
            Dictionary<string, List<string>> modulRightDC = new Dictionary<string, List<string>>();
            List<BsonDocument> hasExitRight = dataOp.FindAllByQuery("SysRoleRight", Query.EQ("roleId", roleId.ToString())).ToList();//已经存在的权限
            List<BsonDocument> delRight = new List<BsonDocument>();//要被删除的权限
            List<StorageData> dataList = new List<StorageData>();//需要操作的数据 保存要删除的权限和要新增的权限
            List<string> operatIds = new List<string>();//新增加的操作项Ids
            if (!string.IsNullOrEmpty(rightCode))
            {
                string[] array = rightCode.Split(new string[] { "@" }, StringSplitOptions.RemoveEmptyEntries);
                try
                {
                    if (array.Length > 0)
                    {
                        foreach (var item in array)
                        {
                            string[] subStr = item.Split(new string[] { "_" }, StringSplitOptions.RemoveEmptyEntries);
                            if (subStr.Length == 2)
                            {
                                if (modulRightDC.ContainsKey(subStr[0]))
                                {
                                    modulRightDC[subStr[0]].Add(subStr[1]);
                                }
                                else
                                {
                                    modulRightDC.Add(subStr[0], new List<string> { subStr[1] });
                                }
                                operatIds.Add(subStr[1]);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {

                }
                // modulRightDC = rightCode.ParamStringSplit();
                List<string> modulIds = modulRightDC.Keys.Distinct().ToList();//新增加模块Ids
                operatIds = operatIds.Distinct().ToList();//新增加的操作项Ids
                List<BsonDocument> modulList = dataOp.FindAllByQuery("SysModule", Query.In("modulId", TypeConvert.StringListToBsonValueList(modulIds))).ToList();
                List<BsonDocument> operatingList = dataOp.FindAllByQuery("SysOperating", Query.In("operatingId", TypeConvert.StringListToBsonValueList(operatIds))).ToList();

                if (modulRightDC.Count() > 0)
                {
                    foreach (var tempDc in modulRightDC)
                    {
                        BsonDocument curModul = modulList.Where(x => x.String("modulId") == tempDc.Key).FirstOrDefault();
                        foreach (var str in tempDc.Value)
                        {
                            BsonDocument curOperating = operatingList.Where(x => x.String("operatingId") == str).FirstOrDefault();
                            if (curModul != null && curOperating != null)
                            {
                                BsonDocument isHasExit = hasExitRight.Where(x => x.String("code") == curModul.String("code") + "_" + curOperating.String("code") && x.String("modulId") == tempDc.Key).FirstOrDefault();//判断是否改角色已经存在该模块的权限 
                                if (isHasExit == null)
                                {
                                    //不存在权限项 新增该权限项
                                    StorageData tempData = new StorageData();
                                    tempData.Name = "SysRoleRight";
                                    tempData.Type = StorageType.Insert;
                                    isHasExit = new BsonDocument();
                                    isHasExit.Add("roleId", roleId.ToString());
                                    isHasExit.Add("modulId", curModul.String("modulId"));
                                    isHasExit.Add("operatingId", curOperating.String("operatingId"));
                                    isHasExit.Add("dataObjId", "20");
                                    isHasExit.Add("name", curOperating.String("name") + curModul.String("name"));
                                    isHasExit.Add("code", curModul.String("code") + "_" + curOperating.String("code"));
                                    tempData.Document = isHasExit;
                                    dataList.Add(tempData);
                                }
                                else
                                {
                                    hasExitRight.Remove(isHasExit);//已经存在且在新增队列中的从已存在移除
                                }
                            }
                        }
                    }
                }
            }
            //删除已经在存在的权限并且不在新增队列中的 
            if (hasExitRight.Count() > 0)
            {
                foreach (var tempHasRight in hasExitRight)
                {
                    StorageData tempData = new StorageData();
                    tempData.Document = tempHasRight;
                    tempData.Name = "SysRoleRight";
                    tempData.Type = StorageType.Delete;
                    tempData.Query = Query.EQ("roleOrgId", tempHasRight.String("roleOrgId"));
                    dataList.Add(tempData);
                }
            }
            result = dataOp.BatchSaveStorageData(dataList);
            if (result.Status == Status.Successful)
            {
                json.Message = "权限更新成功!";
                json.Success = true;
            }
            else
            {
                json.Message = "权限更新失败!";
                json.Success = true;
            }
            return Json(json);
        }
        /// <summary>
        /// 删除角色
        /// </summary>
        /// <param name="roleId"></param>
        /// <returns></returns>
        public JsonResult DelRole(int roleId)
        {
            PageJson json = new PageJson();
            InvokeResult result = new InvokeResult();
            var roleInfo = dataOp.FindOneByQuery("SysRole", Query.EQ("roleId", roleId.ToString()));
            if (roleInfo == null)
            {
                json.Message = "角色已经不存在,请刷新后重试!";
                json.Success = false;

                return Json(json);
            }
            result = dataOp.Delete("SysRoleRight", Query.EQ("roleId", roleId.ToString()));
            if (result.Status == Status.Successful)
            {
                result = dataOp.Delete("DataScope", Query.EQ("roleId", roleId.ToString()));
            }
            if (result.Status == Status.Successful)
            {
                result = dataOp.Delete("SysRole", Query.EQ("roleId", roleId.ToString()));
            }
            if (result.Status == Status.Successful)
            {
                result = dataOp.Delete("SysRoleUser", Query.EQ("roleId", roleId.ToString()));
            }
            if (result.Status == Status.Successful)
            {
                result = dataOp.Delete("SysRoleOrg", Query.EQ("roleId", roleId.ToString()));
            }
            if (result.Status == Status.Successful)
            {
                result = dataOp.Delete("SysCommPost", Query.EQ("roleId", roleId.ToString()));
            }
            if (result.Status == Status.Successful)
            {
                json.Message = "删除角色成功!";
                json.Success = true;
            }
            else
            {
                json.Message = "删除角色失败!";
                json.Success = false;
            }
            return Json(json);
        }



        #endregion

        #region 扩展内置角色管理
        /// <summary>
        /// 
        /// </summary>
        /// <param name="roleId"></param>
        /// <param name="modulCode">模块代码</param>
        /// <param name="operatingCode">权限代码</param>
        /// <param name="flag">0:删除 1：新增</param>
        /// <returns></returns>
        public JsonResult AddSchemeRoleRight(int roleId, string modulCode, string operatingCode, int flag)
        {
            PageJson json = new PageJson();
            InvokeResult result = new InvokeResult();
            BsonDocument curModul = dataOp.FindOneByQuery("SysModule", Query.EQ("code", modulCode));
            BsonDocument curOp = dataOp.FindOneByQuery("SysOperating", Query.EQ("code", operatingCode));
            List<BsonDocument> roleRight = dataOp.FindAllByQuery("ProjBuiltInRoleRightRel", Query.And(Query.EQ("roleId", roleId.ToString()), Query.EQ("modulCode", modulCode), Query.EQ("operatingCode", operatingCode))).ToList();
            List<StorageData> dataList = new List<StorageData>();
            if (flag == 0)
            {
                if (roleRight.Count() > 0)
                {
                    foreach (var tempRight in roleRight)
                    {
                        StorageData tempStorage = new StorageData();
                        tempStorage.Name = "ProjBuiltInRoleRightRel";
                        tempStorage.Query = Query.EQ("rightRelId", tempRight.String("rightRelId"));
                        tempStorage.Document = tempRight;
                        tempStorage.Type = StorageType.Delete;
                        dataList.Add(tempStorage);
                    }
                }
            }
            else if (flag == 1)
            {
                if (roleRight.Count() == 0)
                {
                    if (curModul != null && curOp != null)
                    {
                        StorageData tempStorage = new StorageData();
                        tempStorage.Name = "ProjBuiltInRoleRightRel";
                        tempStorage.Document = new BsonDocument().Add("operatingCode", operatingCode).Add("modulCode", modulCode).Add("roleId", roleId.ToString()).Add("operatingId", curOp.String("operatingId")).Add("modulId", curModul.String("modulId"));
                        tempStorage.Type = StorageType.Insert;
                        dataList.Add(tempStorage);
                    }
                }
            }
            if (dataList.Count() > 0)
            {
                result = dataOp.BatchSaveStorageData(dataList);
                if (result.Status == Status.Successful)
                {
                    json.Message = "保存成功!";
                    json.Success = true;
                }
                else
                {
                    json.Message = "保存失败,请刷新后重试!";
                    json.Success = true;
                }
            }
            else
            {
                json.Message = "保存失败,请刷新后重试!";
                json.Success = true;
            }
            return Json(json);
        }


        #endregion

        #region 删除系统设置的各个内容 核对是否被使用

        /// <summary>
        /// 删除系统专业
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public JsonResult DelSysProfessional(int id)
        {
            PageJson json = new PageJson();
            InvokeResult result = new InvokeResult();
            BsonDocument entityInfo = dataOp.FindOneByQuery("System_Professional", Query.EQ("profId", id.ToString()));
            //判断实体是否存在
            if (entityInfo == null)
            {
                json.Message = "该实体不存在或已经移除,请刷新后重试";
                json.Success = false;
                return Json(json);
            }
            //判断是否有被引用
            string message = string.Empty;//被引用的信息
            var userProf = dataOp.FindAllByQuery("SysUser", Query.EQ("profId", id.ToString()));
            var task = dataOp.FindAllByQuery("TaskDocProtery", Query.EQ("sysProfId", id.ToString()));
            if (userProf.Count() + task.Count() > 0)
            {
                json.Message = "该实体已被引用，不能删除!";
                json.Success = false;
                return Json(json);
            }
            else
            {
                result = dataOp.Delete("System_Professional", Query.EQ("profId", id.ToString()));
            }
            if (result.Status == Status.Successful)
            {
                json.Message = "删除成功!";
                json.Success = true;
            }
            else
            {
                json.Success = false;
                json.Message = "删除失败,请刷新后重试!";
            }
            return Json(json);
        }
        /// <summary>
        /// 删除系统文档类型
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public JsonResult DelSysFileCategory(int id)
        {
            PageJson json = new PageJson();
            InvokeResult result = new InvokeResult();
            BsonDocument entityInfo = dataOp.FindOneByQuery("System_FileCategory", Query.EQ("fileCatId", id.ToString()));
            //判断实体是否存在
            if (entityInfo == null)
            {
                json.Message = "该实体不存在或已经移除,请刷新后重试";
                json.Success = false;
                return Json(json);
            }
            //判断是否有被引用
            string message = string.Empty;//被引用的信息
            var task = dataOp.FindOneByQuery("TaskDocProtery", Query.EQ("fileCatId", id.ToString()));
            if (task.Count() > 0)
            {
                json.Message = "该实体已被引用，不能删除!";
                json.Success = false;
                return Json(json);
            }
            else
            {
                result = dataOp.Delete("System_FileCategory", Query.EQ("fileCatId", id.ToString()));
            }
            if (result.Status == Status.Successful)
            {
                json.Message = "删除成功!";
                json.Success = true;
            }
            else
            {
                json.Success = false;
                json.Message = "删除失败,请刷新后重试!";
            }
            return Json(json);
        }

        /// <summary>
        /// 删除系统阶段
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public JsonResult DelSysStage(int id)
        {
            PageJson json = new PageJson();
            InvokeResult result = new InvokeResult();
            BsonDocument entityInfo = dataOp.FindOneByQuery("System_Stage", Query.EQ("stageId", id.ToString()));
            //判断实体是否存在
            if (entityInfo == null)
            {
                json.Message = "该实体不存在或已经移除,请刷新后重试";
                json.Success = false;
                return Json(json);
            }
            //判断是否有被引用
            string message = string.Empty;//被引用的信息
            var task = dataOp.FindOneByQuery("TaskDocProtery", Query.EQ("stageId", id.ToString()));
            if (task.Count() > 0)
            {
                json.Message = "该实体已被引用，不能删除!";
                json.Success = false;
                return Json(json);
            }
            else
            {
                result = dataOp.Delete("System_Stage", Query.EQ("stageId", id.ToString()));
            }
            if (result.Status == Status.Successful)
            {
                json.Message = "删除成功!";
                json.Success = true;
            }
            else
            {
                json.Success = false;
                json.Message = "删除失败,请刷新后重试!";
            }
            return Json(json);
        }
        /// <summary>
        /// 删除地块区域
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public JsonResult DelLandArea(int id)
        {
            PageJson json = new PageJson();
            InvokeResult result = new InvokeResult();
            BsonDocument entityInfo = dataOp.FindOneByQuery("LandArea", Query.EQ("areaId", id.ToString()));
            //判断实体是否存在
            if (entityInfo == null)
            {
                json.Message = "该实体不存在或已经移除,请刷新后重试";
                json.Success = false;
                return Json(json);
            }
            //判断是否有被引用
            string message = string.Empty;//被引用的信息
            var land = dataOp.FindAllByQuery("Land", Query.EQ("areaId", id.ToString()));
            var proj = dataOp.FindAllByQuery("Project", Query.EQ("areaId", id.ToString()));
            if (land.Count() + proj.Count() > 0)
            {
                json.Message = "该实体已被引用,不能删除!";
                json.Success = false;
                return Json(json);
            }
            else
            {
                result = dataOp.Delete("LandArea", Query.EQ("landId", id.ToString()));
            }
            if (result.Status == Status.Successful)
            {
                json.Message = "删除成功!";
                json.Success = true;
            }
            else
            {
                json.Success = false;
                json.Message = "删除失败,请刷新后重试!";
            }
            return Json(json);
        }
        /// <summary>
        /// 删除地块城市
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public JsonResult DelLandCity(int id)
        {
            PageJson json = new PageJson();
            InvokeResult result = new InvokeResult();
            BsonDocument entityInfo = dataOp.FindOneByQuery("LandCity", Query.EQ("cityId", id.ToString()));
            //判断实体是否存在
            if (entityInfo == null)
            {
                json.Message = "该实体不存在或已经移除,请刷新后重试";
                json.Success = false;
                return Json(json);
            }
            //判断是否有被引用
            string message = string.Empty;//被引用的信息
            var land = dataOp.FindAllByQuery("Land", Query.EQ("cityId", id.ToString()));
            var proj = dataOp.FindAllByQuery("Project", Query.EQ("cityId", id.ToString()));
            if (land.Count() + proj.Count() > 0)
            {
                json.Message = "该实体已被引用,不能删除!";
                json.Success = false;
                return Json(json);
            }
            else
            {
                result = dataOp.Delete("LandCity", Query.EQ("cityId", id.ToString()));
            }
            if (result.Status == Status.Successful)
            {
                json.Message = "删除成功!";
                json.Success = true;
            }
            else
            {
                json.Success = false;
                json.Message = "删除失败,请刷新后重试!";
            }
            return Json(json);
        }

        /// <summary>
        /// 核对系统专业是否被使用
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public JsonResult CheckSysProfessionalIsUse(int id)
        {
            PageJson json = new PageJson();
            var userProf = dataOp.FindAllByQuery("SysUser", Query.EQ("profId", id.ToString()));
            var task = dataOp.FindAllByQuery("TaskDocProtery", Query.EQ("sysProfId", id.ToString()));
            if (userProf.Count() + task.Count() > 0)
            {
                json.Message = "该实体已被引用，不能删除!";
                json.Success = false;
                return Json(json);
            }
            else
            {
                json.Message = "";
                json.Success = true;
            }
            return Json(json);
        }

        /// <summary>
        /// 核对系统文档类型是否被使用
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public JsonResult CheckSysFileCategoryIsUse(int id)
        {
            PageJson json = new PageJson();
            var task = dataOp.FindAllByQuery("TaskDocProtery", Query.EQ("fileCatId", id.ToString()));
            if (task.Count() > 0)
            {
                json.Message = "该实体已被引用，不能删除!";
                json.Success = false;
                return Json(json);
            }
            else
            {
                json.Message = "";
                json.Success = true;
            }
            return Json(json);
        }

        /// <summary>
        /// 核对系统专业是否被使用
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public JsonResult CheckSysStageIsUse(int id)
        {
            PageJson json = new PageJson();
            var task = dataOp.FindAllByQuery("TaskDocProtery", Query.EQ("stageId", id.ToString()));
            if (task.Count() > 0)
            {
                json.Message = "该实体已被引用，不能删除!";
                json.Success = false;
                return Json(json);
            }
            else
            {
                json.Message = "";
                json.Success = true;
            }
            return Json(json);
        }


        /// <summary>
        /// 核对区域是否被使用
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public JsonResult CheckLandAreaIsUse(int id)
        {
            PageJson json = new PageJson();
            var land = dataOp.FindAllByQuery("Land", Query.EQ("areaId", id.ToString()));
            var proj = dataOp.FindAllByQuery("Project", Query.EQ("areaId", id.ToString()));
            var city = dataOp.FindAllByQuery("LandCity", Query.EQ("areaId", id.ToString()));
            if (land.Count() + proj.Count() > 0)
            {
                json.Message = "该实体已被引用,不能删除!";
                json.Success = false;
                return Json(json);
            }
            else
            {
                json.Message = "";
                json.Success = true;
            }
            return Json(json);
        }



        /// <summary>
        /// 核对城市是否被使用
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public JsonResult CheckLandCityIsUse(int id)
        {
            PageJson json = new PageJson();
            var land = dataOp.FindAllByQuery("Land", Query.EQ("cityId", id.ToString()));
            var proj = dataOp.FindAllByQuery("Project", Query.EQ("cityId", id.ToString()));
            if (land.Count() + proj.Count() > 0)
            {
                json.Message = "该实体已被引用,不能删除!";
                json.Success = false;
                return Json(json);
            }
            else
            {
                json.Message = "";
                json.Success = true;
            }
            return Json(json);
        }


        #endregion


        #region 补置项目库查看权限
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public JsonResult AddProjectViewRight()
        {
            PageJson json = new PageJson();
            List<BsonDocument> roleList = dataOp.FindAllByQuery("SysRole", Query.Or(Query.EQ("landOrProj", "1"), Query.EQ("landOrProj", "2"))).ToList();
            List<StorageData> dataList = new List<StorageData>();
            foreach (var tempRole in roleList)
            {
                BsonDocument tempProjView = dataOp.FindOneByQuery("SysRoleRight", Query.And(Query.EQ("roleId", tempRole.String("roleId")),
                    Query.EQ("code", "PROJECTLIB_VIEW"), Query.EQ("modulId", "19")));
                if (tempProjView == null)
                {
                    StorageData tempData = new StorageData();
                    tempProjView = new BsonDocument();
                    tempProjView.Add("roleId", tempRole.String("roleId"));
                    tempProjView.Add("modulId", "19");
                    tempProjView.Add("code", "PROJECTLIB_VIEW");
                    tempProjView.Add("dataObjId", "20");
                    tempProjView.Add("operatingId", "1");
                    tempData.Document = tempProjView;
                    tempData.Type = StorageType.Insert;
                    tempData.Name = "SysRoleRight";
                    dataList.Add(tempData);
                }
            }
            InvokeResult result = dataOp.BatchSaveStorageData(dataList);
            return Json(ConvertToPageJson(result), JsonRequestBehavior.AllowGet);
        }
        #endregion


        /// <summary>
        /// 移动属性
        /// </summary>
        /// <param name="moveId">要移动的节点目标</param>
        /// <param name="toMoveId">移动到节点的地方</param>
        /// <param name="type">1：节点上方 2：节点下方</param>
        /// <returns></returns>
        public JsonResult MoveSysProperty(int moveId, int toMoveId, int type)
        {
            PageJson json = new PageJson();
            List<BsonDocument> allProperty = dataOp.FindAll("SystemProperty").OrderBy(x => x.Int("order")).ToList();
            BsonDocument moveInfo = allProperty.Where(x => x.String("propertyId") == moveId.ToString()).FirstOrDefault();
            BsonDocument toMoveInfo = allProperty.Where(x => x.String("propertyId") == toMoveId.ToString()).FirstOrDefault();
            int moveOrder = moveInfo.Int("order");
            int toMoveOrder = toMoveInfo.Int("order");
            if (moveInfo == null || toMoveInfo == null)
            {
                json.Success = false;
                json.Message = "移动的目标或移动到的目标不存在,请刷新后重试!";
                return Json(json);
            }
            List<BsonDocument> editProperty = new List<BsonDocument>();
            List<StorageData> dataList = new List<StorageData>();

            if (moveOrder < toMoveOrder) //要移动的节点位于移动到的节点的上方
            {
                editProperty = allProperty.Where(x => x.Int("order") > moveOrder & x.Int("order") < toMoveOrder).ToList();

                foreach (var tempProper in editProperty)
                {
                    tempProper["order"] = moveOrder;
                    StorageData tempData = new StorageData();
                    tempData.Document = tempProper;
                    tempData.Type = StorageType.Update;
                    tempData.Name = "SystemProperty";
                    tempData.Query = Query.EQ("propertyId", tempProper.String("propertyId"));
                    dataList.Add(tempData);
                    moveOrder++;
                }
                if (type == 1)
                {
                    StorageData tempData1 = new StorageData();
                    moveInfo["order"] = moveOrder;
                    tempData1.Document = moveInfo;
                    tempData1.Type = StorageType.Update;
                    tempData1.Name = "SystemProperty";
                    tempData1.Query = Query.EQ("propertyId", moveInfo.String("propertyId"));
                    dataList.Add(tempData1);
                }
                else
                {
                    StorageData tempData1 = new StorageData();
                    toMoveInfo["order"] = moveOrder;
                    tempData1.Document = toMoveInfo;
                    tempData1.Type = StorageType.Update;
                    tempData1.Name = "SystemProperty";
                    tempData1.Query = Query.EQ("propertyId", toMoveInfo.String("propertyId"));
                    dataList.Add(tempData1);
                    tempData1 = new StorageData();
                    moveInfo["order"] = toMoveOrder;
                    tempData1.Document = moveInfo;
                    tempData1.Type = StorageType.Update;
                    tempData1.Name = "SystemProperty";
                    tempData1.Query = Query.EQ("propertyId", moveInfo.String("propertyId"));
                    dataList.Add(tempData1);
                }

            }
            else
            {
                editProperty = allProperty.Where(x => x.Int("order") < moveOrder & x.Int("order") > toMoveOrder).ToList();
                if (type == 1)
                {
                    StorageData tempData1 = new StorageData();
                    moveInfo["order"] = toMoveOrder;
                    tempData1.Document = moveInfo;
                    tempData1.Type = StorageType.Update;
                    tempData1.Name = "SystemProperty";
                    tempData1.Query = Query.EQ("propertyId", moveInfo.String("propertyId"));
                    dataList.Add(tempData1);
                    toMoveOrder++;
                    tempData1 = new StorageData();
                    toMoveInfo["order"] = toMoveOrder;
                    tempData1.Document = toMoveInfo;
                    tempData1.Type = StorageType.Update;
                    tempData1.Name = "SystemProperty";
                    tempData1.Query = Query.EQ("propertyId", toMoveInfo.String("propertyId"));
                    dataList.Add(tempData1);
                    toMoveOrder++;
                }
                else
                {
                    toMoveOrder++;
                    StorageData tempData1 = new StorageData();
                    moveInfo["order"] = toMoveOrder;
                    tempData1.Document = moveInfo;
                    tempData1.Type = StorageType.Update;
                    tempData1.Name = "SystemProperty";
                    tempData1.Query = Query.EQ("propertyId", moveInfo.String("propertyId"));
                    dataList.Add(tempData1);
                    toMoveOrder++;
                }
                foreach (var tempProper in editProperty)
                {
                    tempProper["order"] = toMoveOrder;
                    StorageData tempData = new StorageData();
                    tempData.Document = tempProper;
                    tempData.Type = StorageType.Update;
                    tempData.Name = "SystemProperty";
                    tempData.Query = Query.EQ("propertyId", tempProper.String("propertyId"));
                    dataList.Add(tempData);
                    toMoveOrder++;
                }
            }
            InvokeResult result = dataOp.BatchSaveStorageData(dataList);
            if (result.Status == Status.Successful)
            {
                json.Success = true;
                json.Message = "修改成功!";
            }
            else
            {
                json.Success = false;
                json.Message = "修改失败!";
            }

            return Json(json);

        }


        public JsonResult ProjDataPage(int taskId, int planId, int pageSize, int curPage)
        {
            PageJson json = new PageJson();
            if (pageSize == 0)
            {
                pageSize = 15;//默认一页15个
            }
            if (curPage == 0)
            {
                curPage = 1;//默认第一页
            }
            var viewType = PageReq.GetParamInt("viewType");     //展示类型,0为列表展示,1为图形展示
            var keyWord = Server.UrlDecode(PageReq.GetParam("keyWord")).ToLower();    //搜索关键字
            var structId = PageReq.GetParamInt("structId");         //目录Id,如果有值,则特指展示某一目录下的记录
            string projId = PageReq.GetParam("projId");
            string profStr = PageReq.GetParam("profStr");//搜索专业
            string stageStr = PageReq.GetParam("stageStr");//搜索阶段
            string filecatStr = PageReq.GetParam("fileCatStr");//搜索类别
            int isContion = PageReq.GetParamInt("isContion");//是否显示搜索条件
            var subDiv = PageReq.GetParamInt("subDiv"); //节点显示类型  0显示当前节点文件  1包含子节点
            List<BsonDocument> allFileRelList = new List<BsonDocument>();           //所有文档关联,包含目录数据
            List<BsonDocument> decisionFileList = new List<BsonDocument>();         //决策文档

            List<BsonDocument> deliverStructList = new List<BsonDocument>();        //交付物目录
            List<BsonDocument> deliverFileList = new List<BsonDocument>();          //交付物文档
            List<string> deliveStuctFileId = new List<string>();                     //交付物目录下文档id
            BsonDocument curStruct = new BsonDocument();        //当前目录,当structId !=0时方有用,当前目录的Id
            if (structId == 0)          //当目录Id为0时,正常展示,展示一个任务或者一个计划下所有任务的所有文档
            {
                List<string> structIdList1 = new List<string>();
                if (taskId != 0)//默认载入该计划下面所有
                {
                    var taskIds = dataOp.FindAllByQuery("XH_DesignManage_Task", Query.EQ("planId", planId.ToString())).Select(c => c.Text("taskId"));
                    var queryStr = string.Format("tableName=XH_DesignManage_Task&keyName=taskId");
                    var query1 = Query.EQ("tableName", "XH_DesignManage_Task");
                    var query2 = Query.EQ("keyName", "taskId");
                    var query3 = Query.In("keyValue", TypeConvert.StringListToBsonValueList(taskIds.ToList()));

                    allFileRelList = dataOp.FindAllByQuery("FileRelation", Query.And(query1, query2, query3)).ToList();
                    structIdList1 = allFileRelList.Where(x => x.String("fileObjId") == "31" && x.String("structId") != "").Select(s => s.String("structId")).Distinct().ToList();//所有文件夹根目录
                    List<string> fileRelIds = allFileRelList.Select(x => x.String("fileRelId")).ToList();
                    List<BsonDocument> fileRelProperty = dataOp.FindAllByQuery("FileRelProperty", Query.In("fileRelId", TypeConvert.StringListToBsonValueList(fileRelIds))).ToList();
                    if (!string.IsNullOrEmpty(profStr))
                    {
                        List<string> profArr = profStr.Split(',').ToList();
                        fileRelProperty = fileRelProperty.Where(x => profArr.Contains(x.String("profId"))).ToList();

                    }
                    if (!string.IsNullOrEmpty(stageStr))
                    {
                        List<string> stageArr = stageStr.Split(',').ToList();
                        fileRelProperty = fileRelProperty.Where(x => stageArr.Contains(x.String("stageId"))).ToList();
                    }
                    if (!string.IsNullOrEmpty(filecatStr))
                    {
                        List<string> fileCatArr = filecatStr.Split(',').ToList();
                        fileRelProperty = fileRelProperty.Where(x => fileCatArr.Contains(x.String("fileCatId"))).ToList();
                    }
                    List<string> fileRel_Ids = fileRelProperty.Select(x => x.String("fileRelId")).ToList();
                    if (!string.IsNullOrEmpty(stageStr) || !string.IsNullOrEmpty(filecatStr) || !string.IsNullOrEmpty(profStr))
                    {
                        allFileRelList = allFileRelList.Where(x => fileRel_Ids.Contains(x.String("fileRelId"))).ToList();
                    }

                }
                else
                {

                    if (subDiv == 0)
                    {
                        var queryStr = string.Format("tableName=XH_DesignManage_Task&keyName=taskId&keyValue={0}", taskId);
                        allFileRelList = dataOp.FindAllByQueryStr("FileRelation", queryStr).ToList();
                        deliveStuctFileId = allFileRelList.Select(x => x.String("fileId")).ToList();
                        structIdList1 = allFileRelList.Where(x => x.String("fileObjId") == "31" && x.String("structId") != "").Select(s => s.String("structId")).Distinct().ToList();//所有文件夹根目录


                    }
                    else
                    {
                        var taskObj = dataOp.FindOneByQuery("XH_DesignManage_Task", Query.EQ("taskId", taskId));
                        var taskIds = dataOp.FindAllByQuery("XH_DesignManage_Task", Query.EQ("planId", planId.ToString())).Where(c => c.Text("nodeKey").StartsWith(taskObj.Text("nodeKey"))).Select(c => c.Text("taskId"));
                        var queryStr = string.Format("tableName=XH_DesignManage_Task&keyName=taskId");
                        var query1 = Query.EQ("tableName", "XH_DesignManage_Task");
                        var query2 = Query.EQ("keyName", "taskId");
                        var query3 = Query.In("keyValue", TypeConvert.StringListToBsonValueList(taskIds.ToList()));
                        allFileRelList = dataOp.FindAllByQuery("FileRelation", Query.And(query1, query2, query3)).ToList();
                        deliveStuctFileId = allFileRelList.Select(x => x.String("fileId")).ToList();
                        structIdList1 = allFileRelList.Where(x => x.String("fileObjId") == "31" && x.String("structId") != "").Select(s => s.String("structId")).Distinct().ToList();//所有文件夹根目录


                    }
                }

                //搜索查询
                var keyWordQuery = string.IsNullOrEmpty(keyWord) ? null : Query.Matches("name", keyWord);

                //决策文档,不包含文件夹,平稳输出
                //List<BsonDocument> decisionRelList = allFileRelList.Where(c => c.Text("fileObjId") == "32").ToList();       //决策文档关联列表
                //List<string> decisionIdList = decisionRelList.Select(t => t.String("fileId")).ToList();
                //decisionFileList = dataOp.FindAllByQuery("FileLibrary", Query.In("fileId", TypeConvert.StringListToBsonValueList(decisionIdList))).ToList();


                //交付物,包含文件夹结构,特殊输出
                List<BsonDocument> deliverRelList = allFileRelList.Where(c => c.Text("fileObjId") == "31").ToList();            //交付物文档关联

                List<string> structIdList = deliverRelList.Where(x => x.String("structId") != "").Select(t => t.String("structId")).Distinct().ToList();                  //交付物的所有目录结构
                List<string> deliverIdList = deliverRelList.Select(t => t.String("fileId")).ToList();                   //交付物所有非目录文档
                deliveStuctFileId = deliverIdList;

                deliverStructList = dataOp.FindAllByQuery("FileStructure", Query.In("structId", TypeConvert.StringListToBsonValueList(structIdList1))).ToList();
                deliverFileList = dataOp.FindAllByQuery("FileLibrary", Query.In("fileId", TypeConvert.StringListToBsonValueList(deliverIdList))).ToList();
                if (!string.IsNullOrEmpty(keyWord))
                {
                    decisionFileList = decisionFileList.Where(t => t.Text("name").ToLower().Contains(keyWord)).ToList();
                    deliverStructList = deliverStructList.Where(t => t.Text("name").ToLower().Contains(keyWord)).ToList();
                    deliverFileList = deliverFileList.Where(t => t.Text("name").ToLower().Contains(keyWord)).ToList();
                }
            }
            else
            {
                curStruct = dataOp.FindOneByKeyVal("FileStructure", "structId", structId.ToString());
                deliverStructList = dataOp.FindAllByKeyVal("FileStructure", "nodePid", structId.ToString()).ToList();    //要展示的目录结构

                deliverFileList = dataOp.FindAllByKeyVal("FileLibrary", "structId", structId.ToString()).ToList();

            }
            List<BsonDocument> sysProf = dataOp.FindAll("System_Professional").ToList();//系统专业
            List<BsonDocument> sysStage = dataOp.FindAll("System_Stage").ToList();//系统阶段
            List<BsonDocument> sysfileCat = dataOp.FindAll("System_FileCategory").ToList();//系统文档类别
            BsonDocument taskInfo = new BsonDocument();
            if (taskId != 0)
            {
                taskInfo = dataOp.FindOneByQuery("XH_DesignManage_Task", Query.EQ("taskId", taskId));


            }
            return Json(json);
        }
        #region 生成指令单
        /// <summary>
        /// 宏扬重新生成指令单
        /// </summary>
        /// <param name="commandId">变更指令Id</param>
        /// <param name="designChangeId">设计变更Id</param>
        /// <param name="instanceId">流程Id</param>
        /// <returns></returns>
        public JsonResult CreateChangeCommand(string commandId, string designChangeId, string instanceId)
        {
            PageJson json = new PageJson();

            BsonDocument command = dataOp.FindOneByQuery("DesignChangeCommand", Query.EQ("changeCommandId", commandId));
            BsonDocument designChange = dataOp.FindOneByQuery("DesignChange", Query.EQ("designChangeId", designChangeId));
            BsonDocument instance = dataOp.FindOneByQuery("BusFlowInstance", Query.EQ("flowInstanceId", instanceId));
            if (command == null || designChange == null || instance == null)
            {
                json.Success = false;
                json.Message = "传入参数有误,请刷新后重试";
                return Json(json);
            }

            string num = designChange.String("changeNum").Replace("SJBG", "SJBGZL");
            if (command.ContainsColumn("commandNum"))
            {
                command["commandNum"] = num;
            }
            else
            {
                command.Add("commandNum", num);
            }
            if (command.ContainsColumn("subject"))
            {
                command["subject"] = designChange.String("changeTheme");
            }
            else
            {
                command.Add("subject", designChange.String("changeTheme"));
            }
            if (command.ContainsColumn("content"))
            {
                command["content"] = designChange.String("reason");
            }
            else
            {
                command.Add("content", designChange.String("reason"));
            }
            if (command.ContainsColumn("postManId"))
            {
                command["postManId"] = designChange.String("drafterId");
            }
            else
            {
                command.Add("postManId", designChange.String("drafterId"));
            }
            //设计变更关联流程
            var designChangeBusFlowObj = dataOp.FindAllByKeyVal("DesignChangeBusFlow", "designChangeId", designChange.Text("designChangeId")).FirstOrDefault();
            var flowObj = dataOp.FindOneByKeyVal("BusFlow", "flowId", designChangeBusFlowObj.Text("flowId"));
            //获取步骤下 对应
            var stepList = flowObj.ChildBsonList("BusFlowStep").OrderBy(c => c.Int("stepOrder")).ToList();
            List<string> stepIdList = stepList.Select(x => x.String("stepId")).ToList();
            //当前流程步骤的审批历史
            var stepTrace = dataOp.FindAllByQuery("BusFlowTrace",
                   Query.And(
                   Query.EQ("traceType", "2"),
                       Query.In("preStepId", TypeConvert.StringListToBsonValueList(stepIdList)),
                       Query.EQ("flowInstanceId", instance.Text("flowInstanceId"))));
            BsonDocument areaDesginManager = stepList.Where(x => x.String("flowPosId") == "27").FirstOrDefault();//地区设计部经理
            BsonDocument curAreaTrance = stepTrace.Where(x => x.String("preStepId") == areaDesginManager.String("stepId")).FirstOrDefault();
            BsonDocument projManageAuditer = stepList.Where(x => x.String("flowPosId") == "35").FirstOrDefault();//项目管理部
            BsonDocument curProjTrance = stepTrace.Where(x => x.String("preStepId") == projManageAuditer.String("stepId")).FirstOrDefault();
            var otherTrace = stepTrace.Where(x => x.String("flowPosId") != "35" && x.String("flowPosId") != "27").ToList();//其他部门审批
            if (command.ContainsColumn("signerId"))
            {
                command["signerId"] = curAreaTrance.String("createUserId");
            }
            else
            {
                command.Add("signerId", curAreaTrance.String("createUserId"));
            }
            if (command.ContainsColumn("signInId"))
            {
                command["signInId"] = curProjTrance.String("createUserId");
            }
            else
            {
                command.Add("signInId", curProjTrance.String("createUserId"));
            }
            //抄送人
            List<string> userList = otherTrace.Select(x => x.String("createUserId")).Distinct().ToList();
            if (userList.Contains(curAreaTrance.String("createUserId")))
            {
                userList.Remove(curAreaTrance.String("createUserId"));
            }
            if (userList.Contains(curProjTrance.String("createUserId")))
            {
                userList.Remove(curProjTrance.String("createUserId"));
            }
            if (userList.Contains(designChange.String("drafterId")))
            {
                userList.Remove(designChange.String("drafterId"));
            }
            
            List<StorageData> saveList = new List<StorageData>();
            var sendManList = dataOp.FindAllByKeyVal("CommandCopeMan", "changeCommandId", command.String("changeCommandId")).ToList();
            foreach (var tempHas in sendManList)
            {
                StorageData tempStorage = new StorageData();
                tempStorage.Document = tempHas;
                tempStorage.Name = "CommandCopeMan";
                tempStorage.Type = StorageType.Delete;
                tempStorage.Query = Query.EQ("copeManId", tempHas.String("copeManId"));
                saveList.Add(tempStorage);
            }
            if (userList.Count() > 0)
            {
                foreach (var tempId in userList)
                {
                    BsonDocument tempcopyMan = new BsonDocument();
                    tempcopyMan.Add("sendManId", tempId);
                    tempcopyMan.Add("changeCommandId", command.String("changeCommandId"));
                    StorageData tempStorage = new StorageData();
                    tempStorage.Document = tempcopyMan;
                    tempStorage.Name = "CommandCopeMan";
                    tempStorage.Type = StorageType.Insert;
                    saveList.Add(tempStorage);
                }
            }
            List<DateTime> date = stepTrace.Select(x => x.Date("createDate")).ToList();
            if (date.Count() > 0)
            {
                if (command.ContainsColumn("signerDate"))
                {
                    command["signerDate"] = date.Max().ToString();
                }
                else
                {
                    command.Add("signerDate", stepTrace.Select(x => x.Date("createDate")).Max().ToString());
                }
            }

            StorageData tempChange = new StorageData();
            tempChange.Document = command;
            tempChange.Name = "DesignChangeCommand";
            tempChange.Type = StorageType.Update;
            tempChange.Query = Query.EQ("changeCommandId", command.String("changeCommandId"));
            saveList.Add(tempChange);
            dataOp.BatchSaveStorageData(saveList);

            json.Success = true;
            json.Message = "重新生成成功!";
            return Json(json);
        }
        #endregion

        /// <summary>
        /// 删除评价关联的合同文件
        /// </summary>
        /// <param name="relId"></param>
        /// <param name="fileId"></param>
        /// <returns></returns>
        public ActionResult DelEvalContract(string relId,string fileId)
        {
            var evaluate = dataOp.FindOneByQuery("XH_Supplier_SupplierEvaluation", Query.EQ("relId", relId));
            InvokeResult ret = new InvokeResult { Status = Status.Successful };
            if (evaluate != null) {
                var fileIds = evaluate.String("fileIds").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                fileIds.Remove(fileId);
                var fileIdStr = "";
                foreach (var id in fileIds) {
                    fileIdStr += "," + id;
                }
                fileIdStr = fileIdStr.Length > 0 ? fileIdStr.Substring(1) : fileIdStr;
                evaluate.Set("fileIds", fileIdStr);
                ret=dataOp.Update("XH_Supplier_SupplierEvaluation", Query.EQ("relId", relId), evaluate);
            }
            return Json(TypeConvert.InvokeResultToPageJson(ret));
        }

        #region QX保存设计变更
        public ActionResult SaveDesignChange(FormCollection saveForm)
        {
            InvokeResult result = new InvokeResult();

            #region 构建数据
            string tbName = PageReq.GetForm("tbName");
            string queryStr = PageReq.GetForm("queryStr");
            string dataStr = PageReq.GetForm("dataStr");
            int saveStatus = PageReq.GetFormInt("saveStatus");//0:保存  1：提交
            List<string> filterStrList = new List<string>() { "tbName", "queryStr", "actionUserStr" ,"flowId","stepIds",
            "fileTypeId","fileObjId","tableName","keyName","keyValue","delFileRelIds","uploadFileList","fileSaveType","skipStepIds"
            };
            BsonDocument dataBson = new BsonDocument();
            var allKeys = saveForm.AllKeys.Where(i => !filterStrList.Contains(i));
            if (dataStr.Trim() == "")
            {
                foreach (var tempKey in allKeys)
                {
                    if (tempKey == "tbName" || tempKey == "queryStr" || tempKey.Contains("fileList[") || tempKey.Contains("param.")) continue;

                    dataBson.Add(tempKey, PageReq.GetForm(tempKey));
                }
            }
            else
            {
                dataBson = TypeConvert.ParamStrToBsonDocument(dataStr);
            }
            #endregion

            #region 验证参数
            
            string flowId = PageReq.GetForm("flowId");
            BsonDocument flowObj = dataOp.FindOneByQuery("BusFlow", Query.EQ("flowId", flowId));
            if (flowObj.IsNullOrEmpty())
            {
                result.Message = "无效的流程模板";
                result.Status = Status.Failed;
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }

            var stepList = dataOp.FindAllByKeyVal("BusFlowStep", "flowId", flowId).OrderBy(c => c.Int("stepOrder")).ToList();
            BsonDocument bootStep = stepList.Where(c => c.Int("actTypeId") == (int)FlowActionType.Launch).FirstOrDefault();
            if (saveStatus == 1 && bootStep.IsNullOrEmpty())//提交时才判断
            {
                result.Message = "该流程缺少发起步骤";
                result.Status = Status.Failed;
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            var activeStepIdList = PageReq.GetFormIntList("stepIds");
            List<int> hitEnslavedStepOrder = dataOp.FindAllByKeyVal("BusFlowStep", "enslavedStepId", bootStep.Text("stepId")).OrderBy(c => c.Int("stepOrder")).Select(c => c.Int("stepOrder")).Distinct().ToList();
            List<int> hitStepIds = stepList.Where(c => hitEnslavedStepOrder.Contains(c.Int("stepOrder"))).Select(c => c.Int("stepId")).ToList();
            if (saveStatus == 1 && activeStepIdList.Count() <= 0 && hitEnslavedStepOrder.Count() > 0)//提交时才判断
            {
                result.Status = Status.Failed;
                result.Message = "请先选定会签部门";
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            #endregion

            TableRule rule = new TableRule(tbName);

            ColumnRule columnRule = rule.ColumnRules.Where(t => t.IsPrimary == true).FirstOrDefault();
            string keyName = columnRule != null ? columnRule.Name : "";

            #region 验证重名
            string newName = PageReq.GetForm("name").Trim();
            BsonDocument curChange = dataOp.FindOneByQuery(tbName, TypeConvert.NativeQueryToQuery(queryStr));
            BsonDocument oldChange = dataOp.FindOneByQuery(tbName, Query.EQ("name", newName));
            if (!oldChange.IsNullOrEmpty() && oldChange.Int(keyName) != curChange.Int(keyName))
            {
                result.Message = "已经存在该名称的设计变更";
                result.Status = Status.Failed;
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            #endregion

            #region 保存数据
            result = dataOp.Save(tbName, queryStr != "" ? TypeConvert.NativeQueryToQuery(queryStr) : Query.Null, dataBson);
            if (result.Status == Status.Failed)
            {
                result.Message = "保存设计变更失败";
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            #endregion

            #region 文件上传
            int primaryKey = 0;

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
                                    var result1 = opHelper.DeleteFileByRelId(int.Parse(item));
                                    if (result1.Status == Status.Failed)
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

            #region 保存审批人员
            InvokeResult tempResult = new InvokeResult();
            
            int designChangeId = result.BsonInfo.Int(keyName);
            BsonDocument designChange = result.BsonInfo;
            PageJson json = new PageJson();
            json.AddInfo("designChangeId",designChangeId.ToString());

            var actionUserStr = PageReq.GetForm("actionUserStr");

            #region 查找设计变更流程模板关联，没有则添加
            BsonDocument changeFlowRel = dataOp.FindOneByQuery("DesignChangeBusFlow", Query.EQ("designChangeId", designChangeId.ToString()));
            if (changeFlowRel.IsNullOrEmpty())
            {
                tempResult = dataOp.Insert("DesignChangeBusFlow", "designChangeId=" + designChangeId + "&flowId=" + flowId);
                if (tempResult.Status == Status.Failed)
                {
                    json.Success = false;
                    json.Message = "插入流程关联失败";
                    return Json(json);
                }
                else
                {
                    changeFlowRel = tempResult.BsonInfo;
                }
            }
            #endregion

            #region 初始化流程实例
            var helper = new Yinhe.ProcessingCenter.BusinessFlow.FlowInstanceHelper(dataOp);
            var flowUserHelper = new Yinhe.ProcessingCenter.BusinessFlow.FlowUserHelper(dataOp);

            //当前步骤
            BsonDocument curStep = null;
            var hasOperateRight = false;//是否可以跳转步骤
            var hasEditRight = false;//是否可以编辑表单
            var canForceComplete = false;//是否可以强制结束当前步骤
            string curAvaiableUserName = string.Empty;//当前可执行人
            BsonDocument curFlowInstance = dataOp.FindAllByQuery("BusFlowInstance",
                    Query.And(
                        Query.EQ("tableName", "DesignChange"),
                        Query.EQ("referFieldName", "designChangeId"),
                        Query.EQ("referFieldValue", designChangeId.ToString())
                    )
                ).OrderByDescending(i => i.Date("createDate")).FirstOrDefault();
            if (curFlowInstance.IsNullOrEmpty() == false)
            {
                //初始化流程状态
                curStep = helper.InitialExecuteCondition(flowObj.Text("flowId"), curFlowInstance.Text("flowInstanceId"), dataOp.GetCurrentUserId(), ref hasOperateRight, ref hasEditRight, ref canForceComplete, ref curAvaiableUserName);
                if (curStep == null)
                {
                    curStep = curFlowInstance.SourceBson("stepId");
                }
            }
            else
            {
                curStep = bootStep;
                //初始化流程实例
                if (flowObj != null && curStep != null)
                {
                    curFlowInstance = new BsonDocument();
                    curFlowInstance.Add("flowId", flowObj.Text("flowId"));
                    curFlowInstance.Add("stepId", curStep.Text("stepId"));
                    curFlowInstance.Add("tableName", "DesignChange");
                    curFlowInstance.Add("referFieldName", "designChangeId");
                    curFlowInstance.Add("referFieldValue", designChangeId);
                    curFlowInstance.Add("instanceStatus", "0");
                    curFlowInstance.Add("instanceName", designChange.Text("name"));
                    tempResult = helper.CreateInstance(curFlowInstance);
                    if (tempResult.Status == Status.Successful)
                    {
                        curFlowInstance = tempResult.BsonInfo;
                    }
                    else
                    {
                        json.Success = false;
                        json.Message = "创建流程实例失败:" + tempResult.Message;
                        return Json(json);
                    }
                    helper.InitialExecuteCondition(flowObj.Text("flowId"), curFlowInstance.Text("flowInstanceId"), dataOp.GetCurrentUserId(), ref hasOperateRight, ref hasEditRight, ref canForceComplete, ref curAvaiableUserName);
                }
                if (curStep == null)
                {
                    curStep = stepList.FirstOrDefault();
                }
            }
            #endregion

            #region 保存流程实例步骤人员

            List<BsonDocument> allStepList = dataOp.FindAllByKeyVal("BusFlowStep", "flowId", flowId).ToList();  //所有步骤

            //获取可控制的会签步骤
            string curStepId = curStep.Text("stepId");

            var oldRelList = dataOp.FindAllByKeyVal("InstanceActionUser", "flowInstanceId", curFlowInstance.Text("flowInstanceId")).ToList();  //所有的审批人
            //stepId + "|Y|" + uid +"|N|"+ status + "|H|";
            var arrActionUserStrUserStr = actionUserStr.Split(new string[] { "|H|" }, StringSplitOptions.RemoveEmptyEntries);
            var storageList = new List<StorageData>();
            //不需要审批的所有步骤的id--袁辉
            var skipStepIds = PageReq.GetForm("skipStepIds");
            var flowHelper = new FlowInstanceHelper();
            foreach (var userStr in arrActionUserStrUserStr)
            {
                var arrUserStatusStr = userStr.Split(new string[] { "|N|" }, StringSplitOptions.None);
                if (arrUserStatusStr.Length <= 1)
                    continue;
                string status = arrUserStatusStr[1];//该流程步骤人员是否有效 0：有效 1：无效
                var arrUserStr = arrUserStatusStr[0].Split(new string[] { "|Y|" }, StringSplitOptions.RemoveEmptyEntries);
                var stepId = int.Parse(arrUserStr[0]);
                var curStepObj = allStepList.Where(c => c.Int("stepId") == stepId).FirstOrDefault();
                if (curStepObj == null)
                {
                    continue;
                }
                if (arrUserStr.Length <= 1)
                {
                    //如果被跳过的审批没有选择人员，则在这里进行保存
                    var oldRels = oldRelList.Where(t => t.Int("stepId") == stepId).ToList();
                    if (oldRels.Count > 0)
                    {
                        var skipStr = "1";
                        if (skipStepIds.Contains(stepId.ToString()))
                        {
                            oldRels = oldRels.Where(t => t.Int("isSkip") == 0).ToList();
                        }
                        else
                        {
                            oldRels = oldRels.Where(t => t.Int("isSkip") == 1).ToList();
                            skipStr = "0";
                        }
                        if (oldRels.Count > 0)
                        {
                            foreach (var oldRel in oldRels)
                            {
                                var tempData = new StorageData();
                                tempData.Name = "InstanceActionUser";
                                tempData.Type = StorageType.Update;
                                tempData.Query = Query.EQ("inActId", oldRel.Text("inActId"));
                                tempData.Document = new BsonDocument().Add("isSkip", skipStr);

                                storageList.Add(tempData);
                            }
                        }
                    }
                    else if (skipStepIds.Contains(stepId.ToString()))
                    {
                        var tempData = new StorageData();
                        tempData.Name = "InstanceActionUser";
                        tempData.Type = StorageType.Insert;

                        BsonDocument actionUser = new BsonDocument();
                        actionUser.Add("flowInstanceId", curFlowInstance.Text("flowInstanceId"));
                        actionUser.Add("actionConditionId", curFlowInstance.Text("flowInstanceId"));
                        actionUser.Add("userId", "");
                        actionUser.Add("stepId", stepId);
                        actionUser.Add("isSkip", "1");
                        //新增模板属性对象
                        flowHelper.CopyFlowStepProperty(actionUser, curStepObj);
                        tempData.Document = actionUser;
                        storageList.Add(tempData);
                    }
                    continue;
                }
                var userArrayIds = arrUserStr[1];
                var userIds = userArrayIds.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var userId in userIds)
                {
                    var oldRel = oldRelList.FirstOrDefault(i => i.Int("stepId") == stepId && i.Text("userId") == userId);
                    if (oldRel.IsNullOrEmpty())
                    {
                        var tempData = new StorageData();
                        tempData.Name = "InstanceActionUser";
                        tempData.Type = StorageType.Insert;

                        BsonDocument actionUser = new BsonDocument();
                        actionUser.Add("flowInstanceId", curFlowInstance.Text("flowInstanceId"));
                        actionUser.Add("actionConditionId", curFlowInstance.Text("flowInstanceId"));
                        actionUser.Add("userId", userId);
                        actionUser.Add("stepId", stepId);
                        //新增模板属性对象
                        //actionUser.Add("flowId", curStepObj.Text("flowId"));
                        //actionUser.Add("stepOrder", curStepObj.Text("stepOrder"));
                        //actionUser.Add("flowPosId", curStepObj.Text("flowPosId"));
                        //actionUser.Add("actTypeId", curStepObj.Text("actTypeId"));
                        //actionUser.Add("enslavedStepId", curStepObj.Text("enslavedStepId"));
                        //actionUser.Add("resetCSignStepId", curStepObj.Text("resetCSignStepId"));
                        //actionUser.Add("completeStepName", curStepObj.Text("completeStepName"));
                        //actionUser.Add("isFixUser", curStepObj.Text("isFixUser"));
                        //actionUser.Add("canImComplete", curStepObj.Text("canImComplete"));
                        //actionUser.Add("ImCompleteName", curStepObj.Text("ImCompleteName"));
                        //actionUser.Add("isChecker", curStepObj.Text("isChecker"));
                        //actionUser.Add("refuseStepId", curStepObj.Text("refuseStepId"));
                        //actionUser.Add("isHideLog", curStepObj.Text("isHideLog"));
                        flowHelper.CopyFlowStepProperty(actionUser, curStepObj);
                        if (curStepObj.Int("actTypeId") == 2)//如果是会签步骤
                        {
                            actionUser.Set("status", status);
                        }
                        //判断步骤是否跳过审批--袁辉
                        if (skipStepIds.Contains(stepId.ToString()))
                        {
                            actionUser.Add("isSkip", "1");
                        }
                        else
                        {
                            actionUser.Add("isSkip", "0");
                        }
                        tempData.Document = actionUser;
                        storageList.Add(tempData);
                    }
                    else
                    {
                        var tempData = new StorageData();
                        tempData.Name = "InstanceActionUser";
                        tempData.Type = StorageType.Update;
                        tempData.Query = Query.EQ("inActId", oldRel.Text("inActId"));
                        BsonDocument actionUser = new BsonDocument();
                        if (hitStepIds.Contains(stepId))
                        {
                           actionUser.Add("status", status);
                        }
                        actionUser.Add("converseRefuseStepId", "");
                        actionUser.Add("actionAvaiable", "");
                        flowHelper.CopyFlowStepProperty(actionUser, curStepObj);
                        //actionUser.Add("stepOrder", curStepObj.Text("stepOrder"));
                        //actionUser.Add("flowPosId", curStepObj.Text("flowPosId"));
                        //actionUser.Add("actTypeId", curStepObj.Text("actTypeId"));
                        //actionUser.Add("enslavedStepId", curStepObj.Text("enslavedStepId"));
                        //actionUser.Add("resetCSignStepId", curStepObj.Text("resetCSignStepId"));
                        //actionUser.Add("completeStepName", curStepObj.Text("completeStepName"));
                        //actionUser.Add("isFixUser", curStepObj.Text("isFixUser"));
                        //actionUser.Add("canImComplete", curStepObj.Text("canImComplete"));
                        //actionUser.Add("ImCompleteName", curStepObj.Text("ImCompleteName"));
                        //actionUser.Add("isChecker", curStepObj.Text("isChecker"));
                        //actionUser.Add("refuseStepId", curStepObj.Text("refuseStepId"));
                        //actionUser.Add("isHideLog", curStepObj.Text("isHideLog"));
                        //判断步骤是否跳过审批--袁辉
                        if (skipStepIds.Contains(stepId.ToString()))
                        {
                            actionUser.Add("isSkip", "1");
                        }
                        else
                        {
                            actionUser.Add("isSkip", "0");
                        }
                        tempData.Document = actionUser;
                        storageList.Add(tempData);
                        
                        oldRelList.Remove(oldRel);
                    }
                }
            }
            foreach (var oldRel in oldRelList)
            {
                var tempData = new StorageData();
                tempData.Name = "InstanceActionUser";
                tempData.Type = StorageType.Delete;
                tempData.Query = Query.EQ("inActId", oldRel.Text("inActId"));
                storageList.Add(tempData);
            }

            tempResult = dataOp.BatchSaveStorageData(storageList);
            if (tempResult.Status == Status.Failed)
            {
                json.Success = false;
                json.Message = "保存审批人员失败";
                return Json(json);
            }
            #endregion

            #endregion

            #region 提交时保存提交信息并跳转
            if (saveStatus == 1)//提交时直接发起
            {
                //保存发起人
                BsonDocument tempData = new BsonDocument().Add("approvalUserId", dataOp.GetCurrentUserId().ToString()).Add("instanceStatus","0");
                tempResult = dataOp.Save("BusFlowInstance", Query.EQ("flowInstanceId", curFlowInstance.Text("flowInstanceId")), tempData);
                if (tempResult.Status == Status.Failed)
                {
                    json.Success = false;
                    json.Message = "保存发起人失败";
                    return Json(json);
                }
                //保存发起时间
                var timeFormat = "yyyy-MM-dd HH:mm:ss";
                tempData = new BsonDocument(){
                        {"startTime", DateTime.Now.ToString(timeFormat)}
                    };
                tempResult = dataOp.Save("DesignChange", Query.EQ("designChangeId", designChangeId.ToString()), tempData);
                if (tempResult.Status == Status.Failed)
                {
                    json.Success = false;
                    json.Message = "保存发起时间失败";
                    return Json(json);
                }
                //跳转步骤
                BsonDocument act = dataOp.FindAllByKeyVal("BusFlowAction", "type", "0").FirstOrDefault();
                tempResult = helper.ExecAction(curFlowInstance, act.Int("actId"), null, bootStep.Int("stepId"));
                if (tempResult.Status == Status.Failed)
                {
                    json.Success = false;
                    json.Message = "流程跳转失败：" + tempResult.Message;
                    return Json(json);
                }
            }
            #endregion

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }
        #endregion

        public ActionResult saveAmountInfo(FormCollection saveForm)
        {
            var result = new InvokeResult();

            var designChangeId = saveForm["designChangeId"];
            var designChangeObj = dataOp.FindOneByKeyVal("DesignChange", "designChangeId", designChangeId);
            if (designChangeObj == null) {
                result.Message = "设计变更不存在，请重试或者联系管理员";
                result.Status = Status.Failed;
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            #region 构建数据
            string tbName = PageReq.GetForm("tbName");
            string queryStr = PageReq.GetForm("queryStr");
            string dataStr = PageReq.GetForm("dataStr");

            var amountObj = new BsonDocument();
            if (queryStr != "")
            {
                amountObj = dataOp.FindOneByQuery(tbName, TypeConvert.NativeQueryToQuery(queryStr));
            }

            BsonDocument dataBson = new BsonDocument();

            if (dataStr.Trim() == "")
            {
                foreach (var tempKey in saveForm.AllKeys)
                {
                    if (tempKey == "tbName" || tempKey == "queryStr" ) continue;

                    if(tempKey == "changeAmount")
                    {
                        dataBson.Add(tempKey,(float.Parse(PageReq.GetForm(tempKey))*10000).ToString("f0"));
                        continue;
                    }

                    dataBson.Add(tempKey, PageReq.GetForm(tempKey));
                }
            }
            else
            {
                dataBson = TypeConvert.ParamStrToBsonDocument(dataStr);
            }
            #endregion

            #region 保存数据
            result = dataOp.Save(tbName, queryStr != "" ? TypeConvert.NativeQueryToQuery(queryStr) : Query.Null, dataBson);
            #endregion
            //更新设计变更金额
            var temp = new InvokeResult();
            if(string.IsNullOrEmpty(saveForm["changeAmount"])==false){
            if (queryStr == "")
                temp = dataOp.Update("DesignChange", Query.EQ("designChangeId", designChangeId), new BsonDocument().Add("money", (designChangeObj.Decimal("money") + result.BsonInfo.Decimal("changeAmount")).ToString()));
            else
            {
                temp = dataOp.Update("DesignChange", Query.EQ("designChangeId", designChangeId), new BsonDocument().Add("money", (designChangeObj.Decimal("money") - amountObj.Decimal("changeAmount") + result.BsonInfo.Decimal("changeAmount")).ToString()));
            }
            }
            if (temp.Status == Status.Failed)
            {
                result.Message = "设计变更金额未更新，请联系管理员";
                result.Status = Status.Failed;
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            result.BsonInfo.Add("totalMoney", temp.BsonInfo.Decimal("money").ToString("f0"));
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        public ActionResult DeleteAmountInfo(string amountId)
        {
            var result = new InvokeResult();
            var amountObj = dataOp.FindOneByKeyVal("DesignChangeCountersignAmount", "amountId", amountId);
            if (amountObj == null)
            {
                result.Message = "设计金额不存在，请重试或者联系管理员";
                result.Status = Status.Failed;
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }

            var designChangeObj = dataOp.FindOneByKeyVal("DesignChange", "designChangeId", amountObj.String("designChangeId"));
            if (designChangeObj == null) {
                result.Message = "设计变更不存在，请重试或者联系管理员";
                result.Status = Status.Failed;
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }

            var amount = amountObj.Decimal("changeAmount");

            result = dataOp.Delete("DesignChangeCountersignAmount", Query.EQ("amountId", amountId));

            if (result.Status == Status.Failed)
            {
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }

            result = dataOp.Update("DesignChange", Query.EQ("designChangeId", designChangeObj.String("designChangeId")), new BsonDocument().Add("money", (designChangeObj.Decimal("money") - amount).ToString()));

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }
        ///// <summary>
        ///// 更新模板中的任务关联
        ///// </summary>
        ///// <param name="taskId"></param>
        ///// <returns></returns>
        //public ActionResult UpdateTaskRelation(string taskId)
        //{
        //    InvokeResult ret = new InvokeResult();
        //    var sucTempTaskIds = dataOp.FindAllByQuery("XH_DesignManage_TaskRelation", Query.EQ("preTaskId", taskId))
        //                            .Select(c => c.GetValue("sucTaskId"))
        //                            .ToList();  //模板目标节点的所有后继节点Id;
        //    //查找所有与后继节点相关的的节点；
        //    var preTasks = dataOp.FindAllByQuery("XH_DesignManage_Task", Query.EQ("srcPrimTaskId",taskId)).ToList();
        //    var sucTasks = dataOp.FindAllByQuery("XH_DesignManage_Task", Query.In("srcPrimTaskId",sucTempTaskIds)).ToList();
        //    var taskRels = dataOp.FindAllByQuery("XH_DesignManage_TaskRelation",
        //        Query.In("preTaskId", preTasks.Select(c => c.GetValue("taskId"))))
        //        .ToList();
        //    List<StorageData> storageDatas = new List<StorageData>();
        //    foreach (var sucTask in sucTasks)
        //    { 
        //       //获取当前节点的planId;
        //        var preTask = preTasks.Where(c => c.String("planId") == sucTask.String("planId")).FirstOrDefault();
        //        if (preTask == null||(taskRels.Where(c => c.String("preTaskId") == preTask.String("taskId")
        //            && c.String("sucTaskId") == sucTask.String("taskId")).Count() > 0))
        //            continue;
        //        //插入新的关系
        //        var data = new BsonDocument();
        //        data.Add("preTaskId", preTask.String("taskId"))
        //            .Add("sucTaskId", sucTask.String("taskId"))
        //            .Add("referType", "1")
        //            .Add("planId", sucTask.String("planId"))
        //            .Add("projId", sucTask.String("projId"));
        //        storageDatas.Add(new StorageData {
        //            Name = "XH_DesignManage_TaskRelation",
        //            Document=data,
        //            Type=StorageType.Insert
        //        });
        //    }
        //    if (storageDatas.Count > 0)
        //        ret = dataOp.BatchSaveStorageData(storageDatas);
        //    return Json(TypeConvert.InvokeResultToPageJson(ret));
        //}

        /// <summary>
        /// 更新模板中的任务关联
        /// </summary>
        /// <param name="taskId"></param>
        /// <returns></returns>
        public ActionResult UpdateTaskRelation(string taskId)
        {
            InvokeResult ret = new InvokeResult();
            var preTempTaskIds = dataOp.FindAllByQuery("XH_DesignManage_TaskRelation", Query.EQ("sucTaskId", taskId))
                                    .Select(c => c.GetValue("preTaskId"))
                                    .ToList();  //模板目标节点的所有前置节点Id;
            //查找所有与后继节点相关的的节点；
            var sucTasks = dataOp.FindAllByQuery("XH_DesignManage_Task", Query.EQ("srcPrimTaskId", taskId)).ToList();
            var preTasks = dataOp.FindAllByQuery("XH_DesignManage_Task", Query.In("srcPrimTaskId", preTempTaskIds)).ToList();
            var taskRels = dataOp.FindAllByQuery("XH_DesignManage_TaskRelation",
                Query.In("sucTaskId",sucTasks.Select(c=>c.GetValue("taskId")))
                ).ToList();
            List<StorageData> storageDatas = new List<StorageData>();
            foreach (var sucTask in sucTasks)
            {
                //遍历每个计划
                var tempTaskRels = taskRels.Where(c => c.String("sucTaskId") == sucTask.String("taskId")).ToList();
                var tempPreTaskIds = tempTaskRels.Select(c => c.GetValue("preTaskId")).ToList();
                var curPreTasks = preTasks.Where(c => c.GetValue("planId") == sucTask.String("planId")).ToList();
                foreach (var preTask in curPreTasks)
                {
                    //添加关联
                    tempPreTaskIds.Remove(preTask.GetValue("taskId"));
                    if (taskRels.Where(c => c.String("preTaskId") == preTask.String("taskId")
                    && c.String("sucTaskId") == sucTask.String("taskId")).Count() > 0)
                        continue;
                    var data = new BsonDocument();
                    data.Add("preTaskId", preTask.String("taskId"))
                        .Add("sucTaskId", sucTask.String("taskId"))
                        .Add("referType", "1")
                        .Add("planId", sucTask.String("planId"))
                        .Add("projId", sucTask.String("projId"));
                    storageDatas.Add(new StorageData
                    {
                        Name = "XH_DesignManage_TaskRelation",
                        Document = data,
                        Type = StorageType.Insert
                    });
                }
                //删除关联
                if (tempPreTaskIds.Count > 0)
                    storageDatas.Add(new StorageData {
                        Name = "XH_DesignManage_TaskRelation",
                        Query=Query.And(Query.EQ("sucTaskId",sucTask.String("taskId")),Query.In("preTaskId",tempPreTaskIds)),
                        Type=StorageType.Delete
                    });
            }
            if (storageDatas.Count > 0)
                ret = dataOp.BatchSaveStorageData(storageDatas);
            return Json(TypeConvert.InvokeResultToPageJson(ret));
        }

        #region 联发新增

        #region 联发地块项目保存
        /// <summary>
        /// 保存地块信息 --联发
        /// </summary>
        /// <param name="saveform"></param>
        /// <returns></returns>
        public ActionResult SaveLandContent(FormCollection saveForm)
        {
            lock (objPad)
            {
                PageJson json = new PageJson();
                InvokeResult result = new InvokeResult();
                BsonDocument landInfo = new BsonDocument();
                string tbName = PageReq.GetForm("tbName");
                string queryStr = PageReq.GetForm("queryStr");
                string propertyStr = PageReq.GetForm("propertyIdArray");//地块物业属性、
                string memberStr = PageReq.GetForm("members");//地块成员
                List<string> notKey = new List<string> { "tbName", "queryStr", "str", "propertyIdArray", "fileTypeId", "fileObjId", "keyValue", "tableName", "keyName", "delFileRelIds", "uploadFileList", "fileSaveType", "members" };
                List<string> propertyIdList = new List<string>();
                List<string> memberIds = new List<string>();

                if (!string.IsNullOrEmpty(propertyStr))
                {
                    propertyIdList = propertyStr.SplitParam(StringSplitOptions.RemoveEmptyEntries, ",").ToList();
                }
                if (!string.IsNullOrEmpty(memberStr))
                {
                    memberIds = memberStr.SplitParam(StringSplitOptions.RemoveEmptyEntries, ",").ToList();
                }

                List<StorageData> dataSource = new List<StorageData>();
                if (!string.IsNullOrEmpty(queryStr))
                {
                    landInfo = dataOp.FindOneByQuery("Land", TypeConvert.NativeQueryToQuery(queryStr));
                    if (landInfo != null)
                    {
                        foreach (var tempKey in saveForm.AllKeys)
                        {
                            //if (tempKey == "tbName" || tempKey == "queryStr" || tempKey == "str" || tempKey == "propertyIdArray") continue;
                            if (notKey.Contains(tempKey)) continue;
                            if (landInfo.Contains(tempKey))
                            {
                                landInfo[tempKey] = PageReq.GetForm(tempKey);
                            }
                            else
                            {
                                landInfo.Add(tempKey, PageReq.GetForm(tempKey));
                            }
                        }
                        StorageData tempData1 = new StorageData();
                        tempData1.Name = "Land";
                        tempData1.Type = StorageType.Update;
                        tempData1.Document = landInfo;
                        tempData1.Query = Query.EQ("landId", landInfo.String("landId"));
                        dataSource.Add(tempData1);
                        var exitProperty = dataOp.FindAllByQuery("LandProperty", Query.EQ("landId", landInfo.String("landId"))).ToList();//存在的属性
                        var exitMemberInfo = dataOp.FindAllByQuery("LandMember", Query.EQ("landId", landInfo.String("landId"))).ToList();//存在的成员
                        foreach (var tempProperty in propertyIdList)
                        {
                            var temp = exitProperty.Where(x => x.String("propertyId") == tempProperty).FirstOrDefault();

                            if (temp != null) { exitProperty.Remove(temp); }
                            else
                            {
                                BsonDocument tempProper = new BsonDocument();
                                tempProper.Add("landId", landInfo.String("landId"));
                                tempProper.Add("propertyId", tempProperty);
                                StorageData tempData = new StorageData();
                                tempData.Name = "LandProperty";
                                tempData.Type = StorageType.Insert;
                                tempData.Document = tempProper;
                                dataSource.Add(tempData);
                                // newPropertyIdList.Add(tempProperty);
                            }
                        }
                        if (exitProperty.Count() > 0)
                        {
                            foreach (var tempProperty in exitProperty)
                            {
                                StorageData tempData = new StorageData();
                                tempData.Name = "LandProperty";
                                tempData.Type = StorageType.Delete;
                                tempData.Query = Query.EQ("landPropertyId", tempProperty.String("landPropertyId"));
                                tempData.Document = tempProperty;
                                dataSource.Add(tempData);
                                //delPropertyId.Add(tempProperty.String("propertyId"));
                            }
                        }
                        foreach (var tempId in memberIds)
                        {
                            var temp = exitMemberInfo.Where(x => x.String("userId") == tempId).FirstOrDefault();

                            if (temp != null) { exitMemberInfo.Remove(temp); }
                            else
                            {
                                BsonDocument tempMember = new BsonDocument();
                                tempMember.Add("landId", landInfo.String("landId"));
                                tempMember.Add("userId", tempId);
                                StorageData tempData = new StorageData();
                                tempData.Name = "LandMember";
                                tempData.Type = StorageType.Insert;
                                tempData.Document = tempMember;
                                dataSource.Add(tempData);
                                // newPropertyIdList.Add(tempProperty);
                            }
                        }
                        if (exitMemberInfo.Count() > 0)
                        {
                            foreach (var tempMember in exitMemberInfo)
                            {
                                StorageData tempData = new StorageData();
                                tempData.Name = "LandMember";
                                tempData.Type = StorageType.Delete;
                                tempData.Query = Query.EQ("landMemberId", tempMember.String("landMemberId"));
                                tempData.Document = tempMember;
                                dataSource.Add(tempData);
                                //delPropertyId.Add(tempProperty.String("propertyId"));
                            }
                        }
                    }
                    result = dataOp.BatchSaveStorageData(dataSource);
                }
                else
                {
                    foreach (var tempKey in saveForm.AllKeys)
                    {
                        if (tempKey == "tbName" || tempKey == "queryStr" || tempKey == "str" || tempKey == "members" || tempKey == "propertyIdArray") continue;
                        if (landInfo.Contains(tempKey))
                        {
                            landInfo[tempKey] = PageReq.GetForm(tempKey);
                        }
                        else
                        {
                            landInfo.Add(tempKey, PageReq.GetForm(tempKey));
                        }
                    }
                    //if (landInfo.Contains("areaId"))
                    //{
                    //    var curArea = dataOp.FindAllByQuery("LandArea", Query.EQ("areaId", landInfo.Text("areaId"))).FirstOrDefault();
                    //    if (curArea == null)
                    //    {
                    //        result.Status = Status.Failed;
                    //        result.Message = "无效的区域参数";
                    //        return Json(TypeConvert.InvokeResultToPageJson(result));
                    //    }
                    //}
                    if (landInfo.Contains("cityId"))
                    {
                        var curCity = dataOp.FindAllByQuery("SysCity", Query.EQ("cityId", landInfo.Text("cityId"))).FirstOrDefault();
                        if (curCity == null)
                        {
                            result.Status = Status.Failed;
                            result.Message = "无效的城市参数";
                            return Json(TypeConvert.InvokeResultToPageJson(result));
                        }
                    }

                    result = dataOp.Insert("Land", landInfo);
                    if (result.Status == Status.Successful)
                    {
                        landInfo = result.BsonInfo;
                        //Dictionary<string, int> tableTemp = new Dictionary<string, int>();//需要初始化的表名及对应模板Id,若id为0查找默认的模板
                        //tableTemp.Add("ParcelCirInfo", 0);
                        //tableTemp.Add("AdjacentParcelInfo", 0);
                        //tableTemp.Add("GovRequest", 0);
                        //tableTemp.Add("DesignFileApproval", 0);
                        //tableTemp.Add("EngineeringFileApproval", 0);
                        //tableTemp.Add("CompetitiveHouse", 0);
                        try
                        {
                            LandProjectDirBll lpdBll = LandProjectDirBll._();
                            //lpdBll.LandInfoInit(landInfo.Int("landId"), tableTemp);
                            if (landInfo.Contains("isInit"))
                            {
                                landInfo["isInit"] = "1";
                            }
                            else
                            {
                                landInfo.Add("isInit", "1");
                            }
                            StorageData tempData1 = new StorageData();
                            tempData1.Name = "Land";
                            tempData1.Type = StorageType.Update;
                            tempData1.Query = Query.EQ("landId", landInfo.String("landId"));
                            tempData1.Document = landInfo;
                            dataSource.Add(tempData1);
                            //新增地块业态
                            foreach (var tempProperty in propertyIdList)
                            {
                                BsonDocument tempProper = new BsonDocument();
                                tempProper.Add("landId", landInfo.String("landId"));
                                tempProper.Add("propertyId", tempProperty);
                                StorageData tempData = new StorageData();
                                tempData.Name = "LandProperty";
                                tempData.Type = StorageType.Insert;
                                tempData.Document = tempProper;
                                dataSource.Add(tempData);

                            }
                            //新增地块成员
                            foreach (var tempId in memberIds)
                            {
                                BsonDocument tempMember = new BsonDocument();
                                tempMember.Add("landId", landInfo.String("landId"));
                                tempMember.Add("userId", tempId);
                                StorageData tempData = new StorageData();
                                tempData.Name = "LandMember";
                                tempData.Type = StorageType.Insert;
                                tempData.Document = tempMember;
                                dataSource.Add(tempData);

                            }

                            result = dataOp.BatchSaveStorageData(dataSource);
                           // ImportProjOrLandRole(1, 1, landInfo.String("landId"), 1);//新增内置角色
                            //InsertBuiltRole("1", landInfo.String("landId"));取消按部门内置角色
                        }
                        catch (Exception ex) { }





                    }
                }

                if (result.Status == Status.Successful)
                {
                    json.htInfo = new System.Collections.Hashtable(); ;
                    json.htInfo.Add("newland", landInfo.String("landId"));
                    json.Success = true;
                }
                else
                {
                    json = TypeConvert.InvokeResultToPageJson(result);
                }
                return Json(json);
            }
        }

        /// <summary>
        /// 保存项目基本信息
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        public ActionResult SaveProjInfo(FormCollection saveForm)
        {
            lock (objPad)
            {
                PageJson json = new PageJson();
                InvokeResult result = new InvokeResult();
                string tbName = PageReq.GetForm("tbName");
                string queryStr = PageReq.GetForm("queryStr");
                string propertyStr = PageReq.GetForm("propertyIdArray");//物业形态
                string memberStr = PageReq.GetForm("members");//项目成员
                BsonDocument projInfo = new BsonDocument();
                List<string> propertyIdList = new List<string>();
                List<string> memberIdList = new List<string>();

                if (!string.IsNullOrEmpty(propertyStr))
                {
                    propertyIdList = propertyStr.SplitParam(StringSplitOptions.RemoveEmptyEntries, ",").ToList();
                }
                if (!string.IsNullOrEmpty(memberStr))
                {
                    memberIdList = memberStr.SplitParam(StringSplitOptions.RemoveEmptyEntries, ",").ToList();
                }
                List<StorageData> dataSource = new List<StorageData>();
                if (!string.IsNullOrEmpty(queryStr))
                {
                    projInfo = dataOp.FindOneByQuery("Project", TypeConvert.NativeQueryToQuery(queryStr));
                    if (projInfo != null)
                    {
                        foreach (var tempKey in saveForm.AllKeys)
                        {
                            if (tempKey == "tbName" || tempKey == "queryStr" || tempKey == "str" || tempKey == "propertyIdArray" || tempKey == "" || tempKey.Contains("fileList[") || tempKey.Contains("param.")) continue;
                            if (projInfo.Contains(tempKey))
                            {
                                projInfo[tempKey] = PageReq.GetForm(tempKey);
                            }
                            else
                            {
                                projInfo.Add(tempKey, PageReq.GetForm(tempKey));
                            }
                        }
                        StorageData tempData1 = new StorageData();
                        tempData1.Name = "Project";
                        tempData1.Type = StorageType.Update;
                        tempData1.Document = projInfo;
                        tempData1.Query = Query.EQ("projId", projInfo.String("projId"));
                        dataSource.Add(tempData1);
                        var exitProperty = dataOp.FindAllByQuery("ProjectBaseProperty", Query.EQ("projId", projInfo.String("projId"))).ToList();//已存在的项目属性
                        var exitMember = dataOp.FindAllByQuery("ProjectMember", Query.EQ("projId", projInfo.String("projId"))).ToList();//已存在的项目成员
                        foreach (var tempProperty in propertyIdList)
                        {
                            var temp = exitProperty.Where(x => x.String("propertyId") == tempProperty).FirstOrDefault();

                            if (temp != null) { exitProperty.Remove(temp); }
                            else
                            {
                                BsonDocument tempProper = new BsonDocument();
                                tempProper.Add("projId", projInfo.String("projId"));
                                tempProper.Add("propertyId", tempProperty);
                                StorageData tempData = new StorageData();
                                tempData.Name = "ProjectBaseProperty";
                                tempData.Type = StorageType.Insert;
                                tempData.Document = tempProper;
                                dataSource.Add(tempData);
                                // newPropertyIdList.Add(tempProperty);
                            }
                        }
                        if (exitProperty.Count() > 0)
                        {
                            foreach (var tempProperty in exitProperty)
                            {
                                StorageData tempData = new StorageData();
                                tempData.Name = "ProjectBaseProperty";
                                tempData.Type = StorageType.Delete;
                                tempData.Query = Query.EQ("projBasePropertyId", tempProperty.String("projBasePropertyId"));
                                tempData.Document = tempProperty;
                                dataSource.Add(tempData);
                                //delPropertyId.Add(tempProperty.String("propertyId"));
                            }
                        }
                        foreach (var tempId in memberIdList)
                        {
                            var temp = exitMember.Where(x => x.String("userId") == tempId).FirstOrDefault();

                            if (temp != null) { exitMember.Remove(temp); }
                            else
                            {
                                BsonDocument tempMember = new BsonDocument();
                                tempMember.Add("projId", projInfo.String("projId"));
                                tempMember.Add("userId", tempId);
                                StorageData tempData = new StorageData();
                                tempData.Name = "ProjectMember";
                                tempData.Type = StorageType.Insert;
                                tempData.Document = tempMember;
                                dataSource.Add(tempData);
                                // newPropertyIdList.Add(tempProperty);
                            }
                        }
                        if (exitMember.Count() > 0)
                        {
                            foreach (var tempMember in exitMember)
                            {
                                StorageData tempData = new StorageData();
                                tempData.Name = "ProjectMember";
                                tempData.Type = StorageType.Delete;
                                tempData.Query = Query.EQ("projMemberId", tempMember.String("projMemberId"));
                                tempData.Document = tempMember;
                                dataSource.Add(tempData);
                                //delPropertyId.Add(tempProperty.String("propertyId"));
                            }
                        }
                        result = dataOp.BatchSaveStorageData(dataSource);
                    }
                }
                else
                {
                    foreach (var tempKey in saveForm.AllKeys)
                    {
                        if (tempKey == "tbName" || tempKey == "queryStr" || tempKey == "str" || tempKey == "propertyIdArray") continue;
                        if (projInfo.Contains(tempKey))
                        {
                            projInfo[tempKey] = PageReq.GetForm(tempKey);
                        }
                        else
                        {
                            projInfo.Add(tempKey, PageReq.GetForm(tempKey));
                        }
                    }
                    result = dataOp.Insert("Project", projInfo);
                    if (result.Status == Status.Successful)
                    {
                        projInfo = result.BsonInfo; projInfo = result.BsonInfo;
                        Dictionary<string, int> tableTemp = new Dictionary<string, int>();//需要初始化的表名及对应模板Id,若id为0查找默认的模板
                        tableTemp.Add("LocationProposal", 0);
                        tableTemp.Add("TypeMatchProposal", 0);
                        tableTemp.Add("SaleOfficeProposal", 0);
                        tableTemp.Add("ExternalCorrect", 0);
                        tableTemp.Add("CostEcoIndicator", 0);
                        tableTemp.Add("KeyTechEcoIndicator", 0);
                        tableTemp.Add("LandInSide", 0);
                        tableTemp.Add("LandInfo", 0);
                        try
                        {
                          //  LandProjectDirBll lpdBll = LandProjectDirBll._();
                           // lpdBll.ProjectInfoInit(projInfo.Int("projId"), tableTemp);

                            if (projInfo.Contains("isInit"))
                            {
                                projInfo["isInit"] = "1";
                            }
                            else
                            {
                                projInfo.Add("isInit", "1");
                            }
                            StorageData tempData1 = new StorageData();
                            tempData1.Name = "Project";
                            tempData1.Type = StorageType.Update;
                            tempData1.Query = Query.EQ("projId", projInfo.String("projId"));
                            tempData1.Document = projInfo;
                            dataSource.Add(tempData1);
                            //tempData1 = new StorageData();
                            //tempData1.Name = "Material_List";
                            //tempData1.Type = StorageType.Insert;
                            //tempData1.Document = new BsonDocument().Add("name", "方案阶段").Add("projId", projInfo.String("projId"));
                            //dataSource.Add(tempData1);
                            //tempData1 = new StorageData();
                            //tempData1.Name = "Material_List";
                            //tempData1.Type = StorageType.Insert;
                            //tempData1.Document = new BsonDocument().Add("name", "施工阶段").Add("projId", projInfo.String("projId"));
                            //dataSource.Add(tempData1);
                            //新增项目属性
                            foreach (var tempProperty in propertyIdList)
                            {
                                BsonDocument tempProper = new BsonDocument();
                                tempProper.Add("projId", projInfo.String("projId"));
                                tempProper.Add("propertyId", tempProperty);
                                StorageData tempData = new StorageData();
                                tempData.Name = "ProjectBaseProperty";
                                tempData.Type = StorageType.Insert;
                                tempData.Document = tempProper;
                                dataSource.Add(tempData);
                            }
                            //新增项目成员
                            foreach (var tempId in memberIdList)
                            {
                                BsonDocument tempMember = new BsonDocument();
                                tempMember.Add("projId", projInfo.String("projId"));
                                tempMember.Add("userId", tempId);
                                StorageData tempData = new StorageData();
                                tempData.Name = "ProjectMember";
                                tempData.Type = StorageType.Insert;
                                tempData.Document = tempMember;
                                dataSource.Add(tempData);

                            }
                            result = dataOp.BatchSaveStorageData(dataSource);
                            //if (result.Status == Status.Successful)
                            //{
                            //    ImportProjOrLandRole(2, 2, projInfo.String("projId"), 1);//新增内置项目角色
                            //}

                        }
                        catch (Exception ex) { }



                    }
                }

                if (result.Status == Status.Successful)
                {
                    json.htInfo = new System.Collections.Hashtable(); ;
                    json.htInfo.Add("newProj", projInfo.String("projId"));
                    json.Success = true;
                }
                else
                {
                    json = TypeConvert.InvokeResultToPageJson(result);
                }
                return Json(json);
            }

        }
        /// <summary>
        /// 添加项目设计供应商
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SaveProjectSupplierRel()
        {
            InvokeResult result = new InvokeResult();
            string projId = PageReq.GetForm("projId");
            string supplierIds = PageReq.GetForm("supplierIds");

            string[] supplierIdArray = supplierIds.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var item in supplierIdArray.Distinct())
            {
                var exist = dataOp.FindAllByQueryStr("ProjectSupplier", string.Format("supplierId={0}&projId={1}", item, projId));
                if (exist.Count() == 0)
                {
                    BsonDocument listdoc = new BsonDocument();
                    listdoc.Add("supplierId", item);
                    listdoc.Add("projId", projId);
                    result = dataOp.Insert("ProjectSupplier", listdoc);
                    if (result.Status == Status.Failed)
                    {
                        return Json(TypeConvert.InvokeResultToPageJson(result));
                    }
                }
            }

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }
        #endregion
        /// <summary>
        /// 加表名
        /// </summary>
        /// <param name="projId"></param>
        /// <param name="opCodes"></param>
        /// <param name="tableName">表名</param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult GetTreeInfosNew(string projId, string opCodes,string tableName)
        {
            var valueTreeList = dataOp.FindAll(tableName);
            Authentication auth = new Authentication();
            List<Hashtable> retList = new List<Hashtable>();
            foreach (var item in valueTreeList.OrderBy(s => s.Int("order")))
            {
                List<string> codes = new List<string>();
                foreach (var opCode in opCodes.SplitParam(","))
                {
                    string treeCode = item.String("code");
                    codes.Add(string.Format("PC{0}_{1}", treeCode, opCode));
                }

                if (auth.CheckProjectRight(AreaRoleType.Project, projId, codes.ToArray()))
                {
                    item.TryAdd("hasRight", true.ToString());
                }
                else
                {
                    item.TryAdd("hasRight", false.ToString());
                }
                item.Remove("_id");
                retList.Add(item.ToHashtable());
            }


            return Json(retList);
        }

        #region 保存关联
        /// <summary>
        /// 添加某些关联表记录
        /// </summary>
        /// <param name="tbName">关联表名称</param>
        /// <param name="relKey">关联表关联的主键名称</param>
        /// <param name="relValues">关联表关联的主键值,以逗号隔开</param>
        /// <param name="saveForm">其他相关的参数</param>
        /// <returns></returns>
        public ActionResult SaveTableRelation(FormCollection saveForm)
        {
            InvokeResult result = new InvokeResult();

            #region 构建数据
            string tbName = saveForm["tbName"];
            string relKey = saveForm["relKey"];
            string relValues = saveForm["relValues"];

            BsonDocument dataBson = new BsonDocument();

            var queryStr = "";

            foreach (var tempKey in saveForm.AllKeys)
            {
                if (tempKey == "tbName" || tempKey == "relKey" || tempKey == "relValues" ) continue;

                dataBson.Add(tempKey, saveForm[tempKey]);
                if(queryStr=="")
                    queryStr = tempKey + "="+saveForm[tempKey];
                else 
                    queryStr += "&"+tempKey+"="+saveForm[tempKey];
            }

            #endregion

            #region 保存数据
            //查找原来存在的记录
            var oldRelList = dataOp.FindAllByQuery(tbName, TypeConvert.ParamStrToQuery(queryStr)).ToList();
            var tableRule = new TableRule(tbName);
            var primeKey = tableRule.PrimaryKey;

            var storageList = new List<StorageData>();
            var relValueArray = relValues.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            if (relValueArray.Length > 0)
            {
                
                foreach (var value in relValueArray)
                {
                    //查找原来的记录，如果不存在，则添加
                    var oldRel = oldRelList.Find(t=>t.String(relKey)==value);
                    var tempbson = new BsonDocument(dataBson);
                    if (oldRel == null)
                    {
                        var tempData = new StorageData();
                        tempData.Name = tbName;
                        tempData.Type = StorageType.Insert;
                        tempData.Document = tempbson.Add(relKey, value);
                        storageList.Add(tempData);
                    }
                }
                //删除不存在的记录
                foreach (var oldRel in oldRelList.Where(t=>relValues.Contains(t.String(relKey))==false))
                {
                    var tempData = new StorageData();
                    tempData.Name = tbName;
                    tempData.Type = StorageType.Delete;
                    tempData.Query = Query.EQ(primeKey, oldRel.String(primeKey));
                    storageList.Add(tempData);
                }
            }

            result = dataOp.BatchSaveStorageData(storageList);
            #endregion

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }
        #endregion

        #endregion


        #region 新城
        /// <summary>
        /// 项目地块分期指标导出公用接口
        /// </summary>
        /// <param name="engId">地块Id</param>
        /// <param name="projId">分期Id</param>
        /// <param name="itemTb">指标项表名</param>
        /// <param name="columnTb">指标列表名</param>
        /// <param name="templateTb">指标模板表</param>
        /// <param name="dataStr">模板名以及备注</param>
        /// <param name="engTb">地块表</param>
        /// <param name="projTb">分期表</param>
        /// <returns></returns>
        public ActionResult ProjIndexStructExportTemplateCommon(int engId, int projId, string itemTb, string columnTb, string templateTb, string dataStr,string engTb,string projTb)
        {
            TableRule EngRule = new TableRule(engTb);
            var EngPk = EngRule.PrimaryKey;
            TableRule ProjRule = new TableRule(projTb);
            var ProjPk = ProjRule.PrimaryKey;
            TableRule ItemRule = new TableRule(itemTb);
            var ItemPk = ItemRule.PrimaryKey;
            TableRule ColumnRule = new TableRule(columnTb);
            var ColumnPk = ColumnRule.PrimaryKey;
            List<string> noNeedColumn = new List<string>() { "_id", ItemPk, ColumnPk, EngPk, ProjPk, "nodePid", "nodeLevel", "nodeOrder", "nodeKey", "createDate", "updateDate", "createUserId", "updateUserId", "underTable", "order" };

            #region 构建项模板
            StringBuilder itemContent = new StringBuilder();

            List<BsonDocument> itemList = projId == 0 ? dataOp.FindAllByQuery(itemTb, Query.And(Query.EQ(EngPk, engId.ToString()), Query.EQ(ProjPk, "0"))).ToList() : dataOp.FindAllByQueryStr(itemTb, ProjPk+"=" + projId).ToList();     //所有要导出的项

            Dictionary<int, int> itemDic = new Dictionary<int, int>();  //节点Id对应字典(用于树形pid)

            int itemIndex = 0;
            foreach (var tempItem in itemList.OrderBy(t => t.String("nodeKey")))
            {
                itemIndex++;

                itemDic.Add(tempItem.Int(ItemPk), itemIndex);

                BsonDocument tempBson = new BsonDocument(); //导入的模板BSON

                tempBson.Add("id", itemIndex);

                if (itemDic.ContainsKey(tempItem.Int("nodePid")))
                {
                    tempBson.Add("pid", itemDic[tempItem.Int("nodePid")].ToString());
                }
                else if (tempItem.Int("nodePid") == 0) tempBson.Add("pid", "0");
                else continue;

                foreach (var tempElement in tempItem.Elements)
                {
                    if (noNeedColumn.Contains(tempElement.Name)) continue;

                    tempBson.Add(tempElement.Name, tempElement.Value);
                }

                itemContent.Append(tempBson.ToString());
            }
            #endregion

            #region 构建列模板
            StringBuilder columnContent = new StringBuilder();

            List<BsonDocument> columnList = projId == 0 ? dataOp.FindAllByQuery(columnTb, Query.And(Query.EQ(EngPk, engId.ToString()), Query.EQ(ProjPk, "0"))).ToList() : dataOp.FindAllByQueryStr(columnTb, ProjPk + "=" + projId).ToList();     //所有要导出的项

            int columnIndex = 0;
            foreach (var tempColumn in columnList.OrderBy(t => t.String("nodeKey")))
            {
                columnIndex++;

                BsonDocument tempBson = new BsonDocument(); //导入的模板BSON

                tempBson.Add("id", columnIndex);

                foreach (var tempElement in tempColumn.Elements)
                {
                    if (noNeedColumn.Contains(tempElement.Name)) continue;

                    tempBson.Add(tempElement.Name, tempElement.Value);
                }

                columnContent.Append(tempBson.ToString());
            }
            #endregion

            #region 保存至模板库
            BsonDocument template = TypeConvert.ParamStrToBsonDocument(dataStr);

            template.Add(EngPk, engId.ToString());
            template.Add(ProjPk, projId.ToString());
            template.Add("itemContent", itemContent.ToString());
            template.Add("columnContent", columnContent.ToString());

            InvokeResult result = dataOp.Insert(templateTb, template);
            #endregion

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        /// <summary>
        /// 项目经济技术指标由模板导入
        /// </summary>
        /// <param name="templateId">要导入的模板Id</param>
        /// <param name="engId">要导入的工程Id</param>
        /// <param name="isAdd">是否追加,默认0为覆盖1为追加</param>
        /// <param name="itemTb">项的表名</param>
        /// <param name="columnTb">列的表名</param>
        /// <param name="templateTb">模板的表名</param>
        /// <param name="engTb">地块的表名</param>
        /// <param name="ProjTb">分期的表名</param>
        /// <returns></returns>
        public ActionResult ProjIndexStructImportTemplateCommon(int templateId, int engId, int projId, int isAdd, string itemTb, string columnTb, string templateTb, string engTb, string projTb)
        {
            TableRule EngRule = new TableRule(engTb);
            var EngPk = EngRule.PrimaryKey;
            TableRule ProjRule = new TableRule(projTb);
            var ProjPk = ProjRule.PrimaryKey;
            TableRule ItemRule = new TableRule(itemTb);
            var ItemPk = ItemRule.PrimaryKey;
            TableRule ColumnRule = new TableRule(columnTb);
            var ColumnPk = ColumnRule.PrimaryKey;
            TableRule TemplateRule = new TableRule(templateTb);
            var TemplatePk = TemplateRule.PrimaryKey;
            BsonDocument template = dataOp.FindOneByKeyVal(templateTb, TemplatePk, templateId.ToString());    //获取模板信息

            #region 获取项和列的数据记录
            List<BsonDocument> itemList = new List<BsonDocument>();     //所有要导入的项数据

            BsonReader itemReader = BsonReader.Create(template.String("itemContent"));

            while (itemReader.CurrentBsonType != BsonType.EndOfDocument)
            {
                BsonDocument tempBson = BsonDocument.ReadFrom(itemReader);

                itemList.Add(tempBson);
            }

            List<BsonDocument> columnList = new List<BsonDocument>();     //所有要导入的列数据

            BsonReader columnReader = BsonReader.Create(template.String("columnContent"));

            while (columnReader.CurrentBsonType != BsonType.EndOfDocument)
            {
                BsonDocument tempBson = BsonDocument.ReadFrom(columnReader);

                columnList.Add(tempBson);
            }

            #endregion

            #region 将项和列的数据导入
            InvokeResult result = new InvokeResult();

            Dictionary<int, int> corDic = new Dictionary<int, int>(); //节点Id对应字典(用于树形pid)
            corDic.Add(0, 0);

            try
            {
                using (TransactionScope trans = new TransactionScope())
                {
                    if (isAdd == 0)     //如果不是追加,则删除所有已有的项和列记录
                    {
                        var query = projId == 0 ? Query.And(Query.EQ(EngPk, engId.ToString()), Query.EQ(ProjPk, "0")) : Query.EQ(ProjPk, projId.ToString());

                        InvokeResult tempRet = dataOp.Delete(itemTb, query);
                        if (tempRet.Status == Status.Failed) throw new Exception(tempRet.Message);

                        tempRet = dataOp.Delete(columnTb, query);
                        if (tempRet.Status == Status.Failed) throw new Exception(tempRet.Message);
                    }

                    foreach (var tempBson in itemList)
                    {
                        int tempId = tempBson.Int("id");
                        int tempPid = tempBson.Int("pid");

                        if (corDic.ContainsKey(tempPid))
                        {
                            tempBson.Add(EngPk, engId.ToString());
                            tempBson.Add(ProjPk, projId.ToString());
                            tempBson.Add("nodePid", corDic[tempPid].ToString());
                            tempBson.Remove("id");
                            tempBson.Remove("pid");

                            InvokeResult tempRet = dataOp.Insert(itemTb, tempBson);
                            if (tempRet.Status == Status.Failed) throw new Exception(tempRet.Message);

                            corDic.Add(tempId, tempRet.BsonInfo.Int(ItemPk));
                        }
                    }

                    foreach (var tempBson in columnList)
                    {
                        tempBson.Add(EngPk, engId.ToString());
                        tempBson.Add(ProjPk, projId.ToString());
                        tempBson.Remove("id");

                        InvokeResult tempRet = dataOp.Insert(columnTb, tempBson);
                        if (tempRet.Status == Status.Failed) throw new Exception(tempRet.Message);

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

        #endregion
    }

    /// <summary>
    /// 城市实体
    /// </summary>
    public class CityModel
    {
        public int cityId { get; set; }
        public string name { get; set; }
        public List<EngineerModel> engList { get; set; }
    }
    /// <summary>
    /// 工程实体
    /// </summary>
    public class EngineerModel
    {
        public string thumbPic { get; set; }
        public int engId { get; set; }
        public string name { get; set; }
        public string area { get; set; }
        public string buildArea { get; set; }
        public string volumeRate { get; set; }
        public List<ProjectModel> projList { get; set; }
    }
    /// <summary>
    /// 分项实体
    /// </summary>
    public class ProjectModel
    {
        public int projId { get; set; }
        public string name { get; set; }
        public string thumbPic { get; set; }
    }
    /// <summary>
    /// 设计变更日志实体
    /// </summary>
    public class ApproveLog
    {
        public string preStepName { get; set; }
        public string approvalUser { get; set; }
        public string content { get; set; }
        public string createDate { get; set; }
        public string actionTypeName { get; set; }
        public string result { get; set; }
    }

    /// <summary>
    /// 设计变更实体
    /// </summary>
    public class ApprovalModel
    {
        public string flowId { get; set; }
        public string flowInstanceId { get; set; }
        public string stepId { get; set; }
        public string instanceName { get; set; }
        public string tableName { get; set; }
        public string referFieldName { get; set; }
        public string referFieldValue { get; set; }
        public string engName { get; set; }
        public string changeDate { get; set; }
        public string changeReason { get; set; }
        public string changeContent { get; set; }
        public string department { get; set; }
        public string doMan { get; set; }
        public string remark { get; set; }
        public int approvalLogCount { get; set; }

        /// <summary>
        /// 发起部门
        /// </summary>
        public string startDepartment { get; set; }
        /// <summary>
        /// 发起时间
        /// </summary>
        public string startTime { get; set; }
        /// <summary>
        /// 发文时间
        /// </summary>
        public string writeTime { get; set; }

        public int flowType { get; set; }
        public List<ActionModel> actList { get; set; }
    }
    /// <summary>
    /// 动作类型实体
    /// </summary>
    public class ActionModel
    {
        public int actId { get; set; }
        public int actTypeId { get; set; }
        public int type { get; set; }
        public string name { get; set; }
    }
    /// <summary>
    /// 待办事项实体
    /// </summary>
    public class Pedding
    {
        private int _referFieldValue;

        public int referFieldValue
        {
            get { return _referFieldValue; }
            set { _referFieldValue = value; }
        }

        private string _tableName;

        public string tableName
        {
            get { return _tableName; }
            set { _tableName = value; }
        }

        private string _referFieldName;

        public string referFieldName
        {
            get { return _referFieldName; }
            set { _referFieldName = value; }
        }

        public string StartMan { get; set; }
        public string StartDate { get; set; }

        private string _title;

        public string title
        {
            get { return _title; }
            set { _title = value; }
        }
        private string _url;

        public string url
        {
            get { return _url; }
            set { _url = value; }
        }

        private int _type;

        public int type
        {
            get { return _type; }
            set { _type = value; }
        }

        private int _flowType;

        public int flowType
        {
            get { return _flowType; }
            set { _flowType = value; }
        }
    }




    /// <summary>
    /// EPS实体
    /// </summary>
    public class EngStructure
    {
        public int id { get; set; }
        public int nodeId { get; set; }
        public int pid { get; set; }
        public string txt { get; set; }
        public int type { get; set; }
    }

    /// <summary>
    /// 任务实体
    /// </summary>
    public class TaskStructure
    {
        public int taskId { get; set; }
        public string name { get; set; }
        public string startDate { get; set; }
        public string endDate { get; set; }
        public int status { get; set; }
    }
    /// <summary>
    /// 分项实体
    /// </summary>
    public class PorjectStructure
    {
        public int projId { get; set; }
        public string name { get; set; }
        public string manager { get; set; }
        public string phone { get; set; }
        public List<TaskStructure> plantaskList { get; set; }
        public List<TaskStructure> factaskList { get; set; }
    }
}
