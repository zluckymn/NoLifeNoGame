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

namespace Yinhe.ProcessingCenter
{
    /// <summary>
    /// 标准成果后台管理基类
    /// </summary>
    public partial class ControllerBase : Controller
    {
        /// <summary>
        /// 审核成果
        /// </summary>
        /// <param name="retId">成果Id</param>
        /// <param name="status"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult CheckResult(string retId, string isCheck)
        {
            var result = dataOp.Update("StandardResult_StandardResult", Query.EQ("retId", retId), new BsonDocument { { "isCheck", isCheck } });
            return Json(ConvertToPageJson(result));
        }

        /// <summary>
        /// 保存成果配置单中,价值项值与材料的关联
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SaveResultItemMatRelInfo(FormCollection saveForm)
        {
            InvokeResult result = new InvokeResult();
            PageJson json = new PageJson();

            string tbName = "XH_StandardResult_ResultItemMatRelation";    //表名
            string queryStr = PageReq.GetForm("queryStr");          //定位记录
            string matIds = PageReq.GetForm("matIds");              //材料Id列表

            if (PageReq.GetForm("retId").Trim() == "" || PageReq.GetForm("typeId").Trim() == "" || PageReq.GetForm("itemId").Trim() == "")
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

                    tempBson.Add("retId", dataBson.String("retId"));
                    tempBson.Add("typeId", dataBson.String("typeId"));
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

        #region 联发修改
        /// <summary>
        /// 联发保存成果配置单中,价值项值与材料的关联
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SaveResultItemMatRelInfoLF(FormCollection saveForm)
        {
            InvokeResult result = new InvokeResult();
            PageJson json = new PageJson();

            string tbName = PageReq.GetForm("tbName");    //表名
            string queryStr = PageReq.GetForm("queryStr");          //定位记录
            string matIds = PageReq.GetForm("matIds");              //材料Id列表

            if (PageReq.GetForm("retId").Trim() == "" || PageReq.GetForm("typeId").Trim() == "" || PageReq.GetForm("itemId").Trim() == "")
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
                var relList = dataOp.FindAllByQuery(tbName,Query.And(
                    Query.EQ("retId", dataBson.String("retId")),
                    Query.EQ("itemId", dataBson.String("itemId"))
                    )).ToList();
                foreach (var matId in matIdList)
                {
                    //判断材料是否已经关联
                    var oldData = relList.Find(t => t.String("matId") == matId);
                    if (oldData == null)
                    {
                        BsonDocument tempBson = new BsonDocument();

                        tempBson.Add("retId", dataBson.String("retId"));
                        tempBson.Add("typeId", dataBson.String("typeId"));
                        tempBson.Add("itemId", dataBson.String("itemId"));
                        tempBson.Add("matId", matId);

                        StorageData tempData = new StorageData();
                        tempData.Document = tempBson;
                        tempData.Name = tbName;
                        tempData.Type = StorageType.Insert;

                        allDataList.Add(tempData);
                    }
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

        #endregion
        /// <summary>
        /// 保存成果与户型的关联
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        public ActionResult SaveStandardResultUnit(FormCollection saveForm)
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

                #region 合作伙伴关联
                string retId = result.BsonInfo.String("retId");
                string teamRels = saveForm["teamRels"] != null ? saveForm["teamRels"] : "";

                List<StorageData> saveList = new List<StorageData>();
                #region 合作伙伴
                List<string> teamRelArray = teamRels.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();

                List<BsonDocument> oldTeamRelList = dataOp.FindAllByKeyVal("StandardResult_Apartment", "retId", retId).ToList();   //所有旧的供应商关联

                foreach (var teamRel in teamRelArray) //循环新的关联,已存在则不添加,不存在则添加新的
                {
                    string[] infoArr = teamRel.Split(new string[] { ":" }, StringSplitOptions.None);

                    if (infoArr.Count() >= 5 && infoArr[0].Trim() != "")
                    {

                        string apartmentName = infoArr[0];
                        string room = infoArr[1];
                        string Lobby = infoArr[2];
                        string washroom = infoArr[3];
                        string apartmentArea = infoArr[4];

                        BsonDocument oldRel = oldTeamRelList.Where(t => t.String("apartmentName") == apartmentName && t.String("room") == room && t.String("Lobby") == Lobby && t.String("washroom") == washroom && t.String("apartmentArea") == apartmentArea).FirstOrDefault();

                        if (oldRel == null)
                        {
                            StorageData tempData = new StorageData();

                            tempData.Name = "StandardResult_Apartment";
                            tempData.Document = new BsonDocument().Add("retId", retId.ToString())
                                                                  .Add("apartmentName", apartmentName.ToString())
                                                                  .Add("room", room.ToString())
                                                                  .Add("Lobby", Lobby.ToString())
                                                                  .Add("washroom", washroom.ToString())
                                                                  .Add("apartmentArea", apartmentArea.ToString());
                            tempData.Type = StorageType.Insert;

                            saveList.Add(tempData);
                        }
                    }
                }

                foreach (var oldRel in oldTeamRelList)
                {
                    if (!teamRelArray.Contains(string.Format("{0}:{1}:{2}:{3}:{4}", oldRel.String("apartmentName"), oldRel.String("room"), oldRel.String("Lobby"), oldRel.String("washroom"), oldRel.String("apartmentArea"))))
                    {
                        StorageData tempData = new StorageData();

                        tempData.Name = "StandardResult_Apartment";
                        tempData.Query = Query.EQ("ApaId", oldRel.String("ApaId"));
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
        /// 保存成果与户型的关联 (新，修改户型保存的结构)
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        public ActionResult SaveStandardResultUnitNew(FormCollection saveForm)
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

                #region 合作伙伴关联
                string retId = result.BsonInfo.String("retId");
                string teamRels = saveForm["teamRels"] != null ? saveForm["teamRels"] : "";

                List<StorageData> saveList = new List<StorageData>();
                #region 保存户型
                List<string> teamRelArray = teamRels.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();

                List<BsonDocument> oldTeamRelList = dataOp.FindAllByKeyVal("StandardResult_Apartment", "retId", retId).ToList();

                foreach (var teamRel in teamRelArray) //循环新的关联,已存在则不添加,不存在则添加新的
                {
                    string[] infoArr = teamRel.Split(new string[] { ":" }, StringSplitOptions.None);

                    if (infoArr.Count() >= 3 && infoArr[0].Trim() != "")  //完整资料才会保存 
                    {

                        string apartmentName = infoArr[0];
                        string room = infoArr[1];
                        string apartmentArea = infoArr[2];

                        BsonDocument oldRel = oldTeamRelList.Where(t => t.String("apartmentName") == apartmentName && t.String("room") == room  && t.String("apartmentArea") == apartmentArea).FirstOrDefault();

                        if (oldRel == null)
                        {
                            StorageData tempData = new StorageData();

                            tempData.Name = "StandardResult_Apartment";
                            tempData.Document = new BsonDocument().Add("retId", retId.ToString())
                                                                  .Add("apartmentName", apartmentName.ToString())
                                                                  .Add("room", room.ToString())
                                                                  .Add("apartmentArea", apartmentArea.ToString());
                            tempData.Type = StorageType.Insert;

                            saveList.Add(tempData);
                        }
                    }
                }

                foreach (var oldRel in oldTeamRelList)
                {
                    if (!teamRelArray.Contains(string.Format("{0}:{1}:{2}", oldRel.String("apartmentName"), oldRel.String("room"), oldRel.String("apartmentArea"))))
                    {
                        StorageData tempData = new StorageData();

                        tempData.Name = "StandardResult_Apartment";
                        tempData.Query = Query.EQ("ApaId", oldRel.String("ApaId"));
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
        /// 保存成果与材料的关联
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SaveResultMatRelInfo()
        {
            InvokeResult result = new InvokeResult();
            PageJson json = new PageJson();

            int retId = PageReq.GetFormInt("retId");
            string matIds = PageReq.GetForm("matIds");              //材料Id列表

            List<StorageData> allDataList = new List<StorageData>();
            List<string> matIdList = matIds.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();

            foreach (var matId in matIdList)
            {
                BsonDocument tempBson = new BsonDocument();

                tempBson.Add("retId", retId.ToString());
                tempBson.Add("matId", matId);

                StorageData tempData = new StorageData();
                tempData.Document = tempBson;
                tempData.Name = "StandardResult_ResultMatRelation";
                tempData.Type = StorageType.Insert;

                allDataList.Add(tempData);
            }

            result = dataOp.BatchSaveStorageData(allDataList);

            json = TypeConvert.InvokeResultToPageJson(result);

            return Json(json);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult CaseSaveDir(FormCollection saveForm)
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
            #endregion
            //if (result.Status == Status.Successful)
            //{
            //    #region 创建默认的案例成果
            //    InvokeResult result1 = new InvokeResult();
            //    var ddlId = result.BsonInfo.Int("ddlId");
            //    tbName = "XH_StandardResult_DiscreteDesignItem";
            //    queryStr = "";
            //    dataStr = string.Format("itemId=0&classId={0}&resultType=0&name=案例成果&", ddlId);
            //    result1 = dataOp.Save(tbName, queryStr, dataStr);
            //    if (result1.Status == Status.Failed)
            //    {
            //        result1.Message = "创建默认成果案例失败";
            //        return Json(TypeConvert.InvokeResultToPageJson(result1));
            //    }
            //    #endregion
            //}
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }
        /// <summary>
        /// 获取目录树形
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="moveId"></param>
        /// <param name="ddirId">目录树Id</param>
        /// <returns></returns>
        public ActionResult GetCategoryTree(string tbName, string moveId, string libId)
        {
            TableRule tbRule = new TableRule(tbName);

            string tbKey = tbRule.PrimaryKey;

            List<BsonDocument> catList = dataOp.FindAllByQuery(tbName, Query.EQ("libId", libId)).ToList();//获取模板下的目录
            List<TreeNode> treeList = TreeHelper.GetSingleTreeList(catList);

            return new XmlTree(treeList);
        }
        #region 外部项目库的目录模板导入
        /// <summary>
        /// 获取目录模板
        /// </summary>
        /// <param name="tbName"></param>
        /// <returns></returns>
        public ActionResult GetCatModelTree()
        {
            List<BsonDocument> catList = dataOp.FindAllByQuery("OutSideProject_Category", Query.EQ("modelCat", "1")).ToList();//获取模板下的目录
            List<TreeNode> treeList = TreeHelper.GetSingleTreeList(catList);

            return new XmlTree(treeList);
        }
        /// <summary>
        /// 插入目录模板
        /// </summary>
        public void CatModelInsert()
        {
            var OutProjId = PageReq.GetParamInt("OutProjId");
            BsonDocument projObj = dataOp.FindOneByQuery("StandardResult_OutSideProject", Query.EQ("OutProjId", OutProjId.ToString()));
            var modelItemList = dataOp.FindAllByKeyVal("OutSideProject_Category", "modelCat", "1").ToList();  //模板下的目录
            var ObjIdsList = new Dictionary<int, int>();//用于存储新增的旧keyId 与新keyId对应字典
            #region 添加预定义目录
            foreach (var modelVauleObj in modelItemList.OrderBy(c=>c.Text("nodeKey")))  //遍历模板数据
            {

                var uniqueKey = modelVauleObj.Int("catId");
                var curTtemValueObj = new BsonDocument();
                var nodePid = modelVauleObj.Int("nodePid");   //获取父节点
                var ParentId = 0;
                var name=modelVauleObj.Text("name");
                if (nodePid != 0)
                {
                    if (!ObjIdsList.ContainsKey(nodePid)) continue;
                    ParentId = ObjIdsList[nodePid];
                }
                else {
                    name = projObj.Text("name") + "根目录";
                }
                curTtemValueObj.Add("OutProjId", OutProjId);//项目Id
                curTtemValueObj.Add("name", name);
                curTtemValueObj.Add("nodeLevel", modelVauleObj.Text("nodeLevel"));
                curTtemValueObj.Add("nodePid", ParentId);
                var result = dataOp.Insert("OutSideProject_Category", curTtemValueObj);   //插入
                ObjIdsList.Add(uniqueKey, result.BsonInfo.Int("catId"));
            }

            #endregion
        }
        #endregion

        /// <summary>
        /// 保存外部项目基本信息
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        public ActionResult SaveOutProjInfo(FormCollection saveForm)
        {
            PageJson json = new PageJson();
            InvokeResult result = new InvokeResult();
            string tbName = PageReq.GetForm("tbName");
            string queryStr = PageReq.GetForm("queryStr");
            string propertyStr = PageReq.GetForm("propertyIdArray");//物业形态
            BsonDocument projInfo = new BsonDocument();
            List<string> propertyIdList = new List<string>();

            if (!string.IsNullOrEmpty(propertyStr))
            {
                propertyIdList = propertyStr.SplitParam(StringSplitOptions.RemoveEmptyEntries, ",").ToList();
            }
            List<StorageData> dataSource = new List<StorageData>();
            if (!string.IsNullOrEmpty(queryStr))
            {
                projInfo = dataOp.FindOneByQuery("StandardResult_OutSideProject", TypeConvert.NativeQueryToQuery(queryStr));
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
                    tempData1.Name = "StandardResult_OutSideProject";
                    tempData1.Type = StorageType.Update;
                    tempData1.Document = projInfo;
                    tempData1.Query = Query.EQ("OutProjId", projInfo.String("OutProjId"));
                    dataSource.Add(tempData1);
                    var exitProperty = dataOp.FindAllByQuery("OutProjectProperty", Query.EQ("OutProjId", projInfo.String("OutProjId"))).ToList();
                    foreach (var tempProperty in propertyIdList)
                    {
                        var temp = exitProperty.Where(x => x.String("propertyId") == tempProperty).FirstOrDefault();

                        if (temp != null) { exitProperty.Remove(temp); }
                        else
                        {
                            BsonDocument tempProper = new BsonDocument();
                            tempProper.Add("OutProjId", projInfo.String("OutProjId"));
                            tempProper.Add("propertyId", tempProperty);
                            StorageData tempData = new StorageData();
                            tempData.Name = "OutProjectProperty";
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
                            tempData.Name = "OutProjectProperty";
                            tempData.Type = StorageType.Delete;
                            tempData.Query = Query.EQ("outProjPropertyId", tempProperty.String("outProjPropertyId"));
                            tempData.Document = tempProperty;
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
                result = dataOp.Insert("StandardResult_OutSideProject", projInfo);
                if (result.Status == Status.Successful)
                {
                    projInfo = result.BsonInfo; projInfo = result.BsonInfo;
                    try
                    {
                      
                        
                        foreach (var tempProperty in propertyIdList)
                        {
                            BsonDocument tempProper = new BsonDocument();
                            tempProper.Add("OutProjId", projInfo.String("OutProjId"));
                            tempProper.Add("propertyId", tempProperty);
                            StorageData tempData = new StorageData();
                            tempData.Name = "OutProjectProperty";
                            tempData.Type = StorageType.Insert;
                            tempData.Document = tempProper;
                            dataSource.Add(tempData);

                        }
                        result = dataOp.BatchSaveStorageData(dataSource);

                    }
                    catch (Exception ex) { }



                }
            }

            if (result.Status == Status.Successful)
            {
                json.htInfo = new System.Collections.Hashtable(); ;
                json.htInfo.Add("OutProjId", projInfo.String("OutProjId"));
                json.Success = true;
            }
            else
            {
                json = TypeConvert.InvokeResultToPageJson(result);
            }
            return Json(json);

        }

        /// <summary>
        /// 保存构造库基本信息和关联项目
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        public ActionResult SaveStandardConstructInfo(FormCollection saveForm)
        {
            InvokeResult result = new InvokeResult();
            var tbName = saveForm["tbName"];
            var queryStr = saveForm["queryStr"];
            var allKeys = saveForm.AllKeys;

            BsonDocument resultDataBson = new BsonDocument();
            BsonDocument consDataBson = new BsonDocument();

            foreach (var key in allKeys)
            {
                if (key == "tbName" || key == "queryStr" || key=="consId")
                    continue;
                if (key == "projId" || key == "appDiscription" || key == "appEvaluation" || key=="retId")
                {
                    consDataBson.Add(key, saveForm[key]);
                    continue;
                }
                resultDataBson.Add(key, saveForm[key]);
            }
            #region 保存数据
            result = dataOp.Save(tbName, queryStr != "" ? TypeConvert.NativeQueryToQuery(queryStr) : Query.Null, resultDataBson);
            #endregion

            if (result.Status == Status.Failed)
            {
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            else 
            {
                if (string.IsNullOrEmpty(saveForm["consId"]) == false)
                {
                    var consId = saveForm["consId"];
                    if (consId == "0")
                    {
                        result = dataOp.Insert("XH_DesignManage_ProjectConstructor", consDataBson);
                        return Json(TypeConvert.InvokeResultToPageJson(result));
                    }
                    var projCons = dataOp.FindOneByKeyVal("XH_DesignManage_ProjectConstructor", "consId", consId);
                    if (projCons == null)
                    {
                        result = new InvokeResult();
                        result.Message = "没有找到构造做法的项目关联，请重新选择项目，或者联系管理员";
                        result.Status = Status.Failed;
                    }
                    else
                    {
                        result = dataOp.Save("XH_DesignManage_ProjectConstructor", Query.EQ("consId", consId), consDataBson);
                    }
                }
            }
       
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        #region 新城修改

        #region 新城保存成果配置单中,价值项值与材料的关联
        /// <summary>
        /// 保存成果配置单中,价值项值与材料的关联
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SaveResultItemMatRelInfoXC(FormCollection saveForm)
        {
            InvokeResult result = new InvokeResult();
            PageJson json = new PageJson();

            string tbName = "XH_StandardResult_ResultItemMatRelation";    //表名
            string queryStr = PageReq.GetForm("queryStr");          //定位记录
            string matIds = PageReq.GetForm("matIds");              //材料Id列表

            if (PageReq.GetForm("retId").Trim() == "" || PageReq.GetForm("itemId").Trim() == "")
            {
                json.Success = false;
                json.Message = "传入参数有空值!";
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

                    tempBson.Add("retId", dataBson.String("retId"));
                    tempBson.Add("typeId", dataBson.String("typeId"));
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
        #endregion

        #endregion

    }
 
            
}
