using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using MongoDB.Bson;
using System.Web;
using System.Text.RegularExpressions;
using MongoDB.Driver.Builders;
using System.Data.OleDb;
using System.Data;
using MongoDB.Driver;
using System.IO;
using Yinhe.ProcessingCenter.DataRule;
using Yinhe.ProcessingCenter.Reports;
using Yinhe.ProcessingCenter.Document;
using System.Web.Script.Serialization;
namespace Yinhe.ProcessingCenter
{
    /// <summary>
    /// 项目资料库后台管理基类
    /// </summary>
    public partial class ControllerBase : Controller
    {

        /// <summary>
        /// 保存提交上来的数据
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        [ValidateInput(false)]
        public ActionResult SaveDocPackage(FormCollection saveForm)
        {
            InvokeResult result = new InvokeResult() { Status = Status.Successful };

            #region 构建数据
            string tbName = PageReq.GetForm("tbName");
            string queryStr = PageReq.GetForm("queryStr");
            string dataStr = PageReq.GetForm("dataStr");
            BsonDocument dataBson = new BsonDocument();

            if (dataStr.Trim() == "")
            {
                foreach (var tempKey in saveForm.AllKeys)
                {
                    if (tempKey == "tbName" || tempKey == "queryStr" || tempKey.Contains("fileList[") || tempKey.Contains("param.") || tempKey == "PackageRe") continue;

                    dataBson.Add(tempKey, PageReq.GetForm(tempKey));
                }
            }
            else
            {
                dataBson = TypeConvert.ParamStrToBsonDocument(dataStr);
            }
            #endregion
            #region 保存数据
            var packageId = PageReq.GetForm("packageId");
            var curObj = dataOp.FindOneByKeyVal("ProjDocPackage", "packageId", packageId);
            var projId = PageReq.GetForm("projId");
            var engId = PageReq.GetForm("engId");
            var catTypeId = PageReq.GetForm("catTypeId");
            var projDocCatId = PageReq.GetForm("projDocCatId");
            var projEngId = PageReq.GetForm("projEngId");
            var supplierId = PageReq.GetForm("supplierId");
            var name = PageReq.GetForm("name");
            var completedDate = PageReq.GetForm("completedDate");
            var profId = PageReq.GetForm("profId");
            var stageId = PageReq.GetForm("stageId");
            var docType = PageReq.GetForm("docType");
            var remark = PageReq.GetForm("remark");
            var firstCatId = PageReq.GetForm("firstCatId"); //当前地块下类别对应的一级分类Id
            
            var isShowProjEng = PageReq.GetParamInt("isShowProjEng"); //是否展示工程
            var isShowSupplier = PageReq.GetParamInt("isShowSupplier"); //是否展示供应商
            var isShowCat = PageReq.GetParamInt("isShowCat"); //是否展示目录
            var dngChangeId = PageReq.GetForm("dngChangeId"); //记录关联的设计变更单
            //if (isShowProjEng == 0) projEngId = "";  //改为必填和非必填，不是可选和不可选
            //if (isShowSupplier == 0) supplierId = "";//
            if (isShowCat == 0) //针对政府报文类别 需要存地块中对应的一级分类
            {
                profId = ""; stageId = "";
                projDocCatId = firstCatId;
            }
            var catTypeObj = dataOp.FindOneByQuery("ProjCategoryType", Query.EQ("catTypeId", catTypeId));
            var updateBosn = new BsonDocument();
            updateBosn.Add("projId", projId);
            updateBosn.Add("engId", engId);
            updateBosn.Add("catTypeId", catTypeId);
            updateBosn.Add("projDocCatId", projDocCatId);
            updateBosn.Add("projEngId", projEngId);
            updateBosn.Add("supplierId", supplierId);
            //bool propertyChanged = false;
            //if (curObj.Text("catTypeId") != catTypeId || curObj.Text("completedDate") != completedDate || curObj.Text("projEngId") != projEngId)
            //{
            //    propertyChanged = true;
            //}
            //if ((catTypeObj.Int("isNeedHide") != 1 && propertyChanged)||curObj.IsNullOrEmpty())
            //{
            //    name = GetDocTitle(packageId, projEngId, catTypeId, completedDate);
            //}
            updateBosn.Add("name", name);

            updateBosn.Add("completedDate", completedDate);
            updateBosn.Add("profId", profId);
            updateBosn.Add("stageId", stageId);
            updateBosn.Add("docType", docType);
            updateBosn.Set("remark", remark);
            updateBosn.Add("firstCatId", firstCatId);
            updateBosn.Add("dngChangeId", dngChangeId);

            //var hasExistObj = dataOp.FindAllByQuery("ProjDocPackage", Query.And(Query.EQ("projDocCatId", projDocCatId.ToString()), Query.EQ("name", name.Trim()))).Where(c => c.Text("packageId") != packageId).Count() > 0;
            //if (hasExistObj)
            //{
            //    result.Status = Status.Failed;
            //    result.Message = "不能创建重名的对象";
            //    return Json(TypeConvert.InvokeResultToPageJson(result));
            //}
            #region 修改工程下其他图纸包的版本 (确保当前项目-当前工程-当前分类下只有一个最终版本)
            if (PageReq.GetForm("docType") == "1" && PageReq.GetForm("projId")!="")
            {
                List<StorageData> updateList = new List<StorageData>();
                var QueryHit = Query.And(Query.EQ("projId", PageReq.GetForm("projId")), Query.EQ("projEngId", projEngId), Query.EQ("catTypeId", PageReq.GetForm("catTypeId")), Query.EQ("projDocCatId", projDocCatId));
                List<BsonDocument> needUpdateList = dataOp.FindAllByQuery("ProjDocPackage", QueryHit).ToList();
                foreach (var updateObj in needUpdateList)
                {

                    StorageData relData = new StorageData();
                    updateObj.Set("docType", "0");
                    relData.Name = "ProjDocPackage";
                    relData.Document = updateObj;
                    relData.Type = StorageType.Update;
                    relData.Query = Query.EQ("packageId", updateObj.String("packageId"));
                    updateList.Add(relData);
                }
                dataOp.BatchSaveStorageData(updateList);
            }
            //地块资料
            if (PageReq.GetForm("docType") == "1" && PageReq.GetForm("engId") != "")
            {
                List<StorageData> updateList = new List<StorageData>();
                var QueryHit = Query.And(Query.EQ("engId", PageReq.GetForm("engId")), Query.EQ("projEngId", projEngId), Query.EQ("catTypeId", PageReq.GetForm("catTypeId")), Query.EQ("projDocCatId", projDocCatId));
                List<BsonDocument> needUpdateList = dataOp.FindAllByQuery("ProjDocPackage", QueryHit).ToList();
                foreach (var updateObj in needUpdateList)
                {

                    StorageData relData = new StorageData();
                    updateObj.Set("docType", "0");
                    relData.Name = "ProjDocPackage";
                    relData.Document = updateObj;
                    relData.Type = StorageType.Update;
                    relData.Query = Query.EQ("packageId", updateObj.String("packageId"));
                    updateList.Add(relData);
                }
                dataOp.BatchSaveStorageData(updateList);
            }
            #endregion
            if (curObj != null)
            {
                result = dataOp.Update(curObj, updateBosn);
            }
            else
            {
                result = dataOp.Insert("ProjDocPackage", updateBosn);
            }
            //result = dataOp.Save(tbName, queryStr != "" ? TypeConvert.NativeQueryToQuery(queryStr) : Query.Null, dataBson);
            #endregion

            #region 文件上传
            int primaryKey = 0;
            TableRule rule = new TableRule(tbName);

            ColumnRule columnRule = rule.ColumnRules.Where(t => t.IsPrimary == true).FirstOrDefault();
            string keyName = columnRule != null ? columnRule.Name : "";
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

            #region  保存图纸包关联
            string packageRel = PageReq.GetForm("PackageRe"); //图纸包关联
            List<string> teamRelArray = packageRel.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).Distinct().ToList();
            string packId = result.BsonInfo.Text("packageId");
            if (result.Status == Status.Successful)
            {
                List<StorageData> saveList = new List<StorageData>();
                List<BsonDocument> oldTeamRelList = dataOp.FindAllByKeyVal("ProjDocPackageRelation", "curPackId", packId).ToList();   //所有旧的图纸包关联
                foreach (var teamRel in teamRelArray) //循环新的关联,已存在则不添加,不存在则添加新的
                {
                    BsonDocument oldRel = oldTeamRelList.Where(t => t.String("refPackId") == teamRel).FirstOrDefault();
                    BsonDocument packAgeObj = dataOp.FindOneByQuery("ProjDocPackage", Query.EQ("packageId", teamRel)); //查找图纸包对象
                    if (oldRel == null && packAgeObj != null)
                    {
                        StorageData tempData = new StorageData();

                        tempData.Name = "ProjDocPackageRelation";
                        tempData.Document = TypeConvert.ParamStrToBsonDocument("curPackId=" + packId + "&refPackId=" + teamRel + "&catTypeId=" + packAgeObj.Text("catTypeId") + "&status=0");
                        tempData.Type = StorageType.Insert;

                        saveList.Add(tempData);
                    }
                }

                foreach (var oldRel in oldTeamRelList) //删除旧数据
                {
                    if (!teamRelArray.Contains(oldRel.Text("refPackId")))
                    {
                        StorageData tempData = new StorageData();

                        tempData.Name = "ProjDocPackageRelation";
                        tempData.Query = Query.EQ("relationId", oldRel.String("relationId"));
                        tempData.Type = StorageType.Delete;

                        saveList.Add(tempData);
                    }
                }
                dataOp.BatchSaveStorageData(saveList);
            }
            #endregion

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }
        #region 下载图纸包
        /// <summary>
        /// 图纸包单个下载与批量下载
        /// </summary>
        /// <returns></returns>
        public string GetFilePackageInfo()
        {
            PageJson json = new PageJson();
            string tbName = PageReq.GetParam("tableName");             //关联表
            string keyName = PageReq.GetParam("keyName");
            string fileObjStr = PageReq.GetParam("fileObjStr");       //文档对象
            string fileInfoStr = PageReq.GetForm("packInfoStr").Trim();//图纸包Id
            var packageArray = fileInfoStr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries); //图纸包数组
            var fileObjIdList = fileObjStr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries); //图纸包文档类型数组
            var fileObjList = dataOp.FindAllByKeyValList("FileObject", "fileObjId", fileObjIdList);
            var fileInfo = string.Empty;
            var fileNameList = new List<string>();
            foreach (var packId in packageArray)
            {  //循环图纸包，获取待下载文件信息
                int fileCount = 0;
                try
                {
                    BsonDocument curPackage = new BsonDocument();                        //当前图纸包
                    curPackage = dataOp.FindOneByQuery(tbName, Query.EQ(keyName, packId)); //当前图纸包
                    string PathFirstStr = curPackage.Text("name"); //图纸包下载路径
                    foreach (var fileObj in fileObjList)  //下载图纸包下所有的文件类型
                    {
                        List<BsonDocument> showStructList = new List<BsonDocument>();
                        List<BsonDocument> showFileList = new List<BsonDocument>();
                        List<BsonDocument> fileRelList = dataOp.FindAllByQueryStr("FileRelation", "tableName=" + tbName + "&fileObjId=" + fileObj.Text("fileObjId") + "&keyValue=" + packId).ToList();   //所有当前节点下文档关联
                        fileCount = fileRelList.Count();
                        List<string> structIdList = fileRelList.Select(t => t.String("structId")).ToList();                               //节点下的所有目录结构
                        List<string> fileIdList = fileRelList.Select(t => t.String("fileId")).ToList();                                 //节点下所有非目录文档
                        showStructList = dataOp.FindAllByKeyValList("FileStructure", "structId", structIdList).ToList(); //所有要展示的目录
                        showFileList = dataOp.FindAllByKeyValList("FileLibrary", "fileId", fileIdList).Where(c => c.Text("structId") == "").ToList(); //所有要展示的非目录文档
                        List<BsonDocument> allStructList = new List<BsonDocument>();    //所有目录结构
                        allStructList.AddRange(showStructList);
                        foreach (var showStruct in showStructList)
                        {
                            allStructList.AddRange(dataOp.FindChildNodes("FileStructure", showStruct.String("structId")));
                        }
                        List<string> allDirIdList = allStructList.Select(t => t.String("structId")).ToList();  //所有的目录结构文档的目录Id  structId

                        List<BsonDocument> allDirFileList = dataOp.FindAllByKeyValList("FileLibrary", "structId", allDirIdList).ToList(); //所有目录结构的文件
                        foreach (var showStruct in allStructList.OrderBy(t => t.String("nodeKey"))) //所有要展示的目录
                        {

                            var parentStrList = allStructList.Where(c => showStruct.Text("nodeKey").IndexOf(c.Text("nodeKey")) == 0).OrderBy(c => c.Text("nodeKey")).Select(c => c.Text("name")).ToArray();//目录列表

                            var curPathStr = PathFirstStr + "\\" + fileObj.Text("name") + "\\";
                            if (parentStrList.Count() > 0) curPathStr += string.Join("\\", parentStrList) + "\\";
                            List<BsonDocument> subFileList = allDirFileList.Where(t => t.Int("structId") == showStruct.Int("structId")).ToList();
                            foreach (var file in subFileList)
                            {
                                fileInfo += ",{\"name\":\"" + file.Text("name").Replace("\\", "\\\\") + "\", \"ext\":\"" + file.Text("ext") + "\", \"fileId\":\"" + file.Text("fileId") + "\", \"filePath\":\"" + curPathStr.Replace("\\", "\\\\") + "\", \"guid\":\"" + file.Text("guid") + "\"}";
                            }
                        }
                        foreach (var file in showFileList)
                        {
                            var curPathStr = PathFirstStr + "\\" + fileObj.Text("name") + "\\";
                            fileInfo += ",{\"name\":\"" + file.Text("name").Replace("\\", "\\\\") + "\", \"ext\":\"" + file.Text("ext") + "\", \"fileId\":\"" + file.Text("fileId") + "\", \"filePath\":\"" + curPathStr.Replace("\\", "\\\\") + "\", \"guid\":\"" + file.Text("guid") + "\"}";
                        }
                    }
                }

                catch (InvalidCastException ex)
                {
                    json.Success = false;
                    json.Message = "传入参数有误";
                    return json.ToString();

                }
                catch (System.Exception ex)
                {
                    json.Success = false;
                    json.Message = "传入参数有误";
                    return json.ToString();
                }
                #region 更新图纸包下载次数
                if (fileCount > 0)
                {
                    DownLoadCount(Convert.ToInt32(packId));
                }
                #endregion
            }
            json.Success = true;
            if (!string.IsNullOrEmpty(fileInfo))
            {
                fileInfo = "[" + fileInfo.Remove(0, 1) + "]";
            }
            var picResult = json.ToString() + "|" + fileInfo;
            return picResult;
        }
        #endregion

        #region 乔鑫项目资料联动
        /// <summary>
        /// 获取分类下的子集分类
        /// </summary>
        /// <returns></returns>
        public JsonResult GetCatThree()
        {
            int catId = PageReq.GetParamInt("catId");
            List<BsonDocument> metaObjList = dataOp.FindAllByQuery("ProjDocCategory", Query.EQ("nodePid", catId.ToString())).ToList();
            List<object> metaObjListInfo = new List<object>();
            foreach (var metaObj in metaObjList)
            {
                metaObjListInfo.Add(new { metaObjId = metaObj.Text("projDocCatId"), metaObjName = metaObj.Text("name") });
            }

            PageJson json = new PageJson();
            JavaScriptSerializer script = new JavaScriptSerializer();
            string strJson = script.Serialize(metaObjListInfo);
            if (metaObjListInfo != null && metaObjListInfo.Count > 0)
            {
                json.AddInfo("CatList", strJson);
                json.Success = true;
            }
            else
            {
                json.AddInfo("CatList", "");
                json.Success = true;
            }

            return Json(json);
        }

        /// <summary>
        /// 获取工程下的子工程
        /// </summary>
        /// <returns></returns>
        public JsonResult GetSubProject()
        {
            int catId = PageReq.GetParamInt("catId");
            List<BsonDocument> metaObjList = dataOp.FindAllByQuery("XH_DesignManage_ProjEngineering", Query.EQ("nodePid", catId.ToString())).ToList();
            List<object> metaObjListInfo = new List<object>();
            foreach (var metaObj in metaObjList)
            {
                metaObjListInfo.Add(new { metaObjId = metaObj.Text("projEngId"), metaObjName = metaObj.Text("name") });
            }

            PageJson json = new PageJson();
            JavaScriptSerializer script = new JavaScriptSerializer();
            string strJson = script.Serialize(metaObjListInfo);
            if (metaObjListInfo != null && metaObjListInfo.Count > 0)
            {
                json.AddInfo("CatList", strJson);
                json.Success = true;
            }
            else
            {
                json.AddInfo("CatList", "");
                json.Success = true;
            }

            return Json(json);
        }
        #endregion
        /// <summary>
        /// 记录图纸包的下载量
        /// </summary>
        /// <param name="id">图纸包Id</param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult DownLoadCount(int id)
        {
            PageJson json = new PageJson();
            var packageObj = dataOp.FindOneByKeyVal("ProjDocPackage", "packageId", id.ToString());
            InvokeResult result = new InvokeResult();
            if (packageObj == null)
            {
                json.Success = false;
                json.Message = "图纸包不存在!";
            }
            else
            {
                int count = packageObj.Int("downLoadCount") + 1;
                BsonDocument dataBson = new BsonDocument();
                dataBson.Add("downLoadCount", count);
                //result = dataOp.Save("ProjDocPackage", Query.EQ("packageId",id.ToString()), dataBson); //通用方法
                result = dataOp.Update(packageObj, dataBson);
                json = TypeConvert.InvokeResultToPageJson(result);
            }
            return Json(json);
        }


        /// <summary>
        /// 导出目录模板
        /// </summary>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult ExportCat()
        {
            PageJson json = new PageJson();
            InvokeResult result = new InvokeResult() { Status = Status.Successful };
            int engId = PageReq.GetParamInt("engId"); //获取当前地块Id
            int projCatModelId = PageReq.GetParamInt("projCatModelId"); //需要导出的模板
            var modelObj = dataOp.FindOneByQuery("ProjDocCategoryModel", Query.EQ("projCatModelId", projCatModelId.ToString()));
            var modelItemList = dataOp.FindAllByKeyVal("ProjDocCategory", "engId", engId.ToString()).ToList();  //地块下的目录
            var ObjIdsList = new Dictionary<int, int>();//用于存储新增的旧keyId 与新keyId对应字典
            if (modelObj == null)
            {
                result.Message = "模板创建失败，请重试！";
                json = TypeConvert.InvokeResultToPageJson(result);
                return Json(json);
            }
            #region 创建目录
            try
            {
                foreach (var modelVauleObj in modelItemList.OrderBy(c => c.Text("nodeKey")))  //遍历模板数据
                {

                    var uniqueKey = modelVauleObj.Int("projDocCatId");
                    var curTtemValueObj = new BsonDocument();
                    var nodePid = modelVauleObj.Int("nodePid");   //获取父节点
                    var ParentId = 0;
                    var name = modelVauleObj.Text("name");
                    if (nodePid != 0)
                    {
                        if (!ObjIdsList.ContainsKey(nodePid)) continue;
                        ParentId = ObjIdsList[nodePid];
                    }
                    else
                    {
                        name = modelObj.Text("name") + "根目录";
                    }
                    curTtemValueObj.Add("projCatModelId", projCatModelId);//模板Id
                    curTtemValueObj.Add("name", name);
                    curTtemValueObj.Add("nodePid", ParentId);
                    curTtemValueObj.Add("catTypeId", modelVauleObj.Text("catTypeId"));
                    curTtemValueObj.Add("srcprojDocCatId", modelVauleObj.Text("projDocCatId"));
                    curTtemValueObj.Add("isModelCat", "1");
                    curTtemValueObj.Add("status", modelVauleObj.Text("status"));
                    curTtemValueObj.Add("viewLevel", modelVauleObj.Text("viewLevel"));
                    curTtemValueObj.Add("isAllowUpload", modelVauleObj.Text("isAllowUpload"));
                    curTtemValueObj.Add("isNeedHide", modelVauleObj.Text("isNeedHide"));
                    curTtemValueObj.Add("isHideProjEng", modelVauleObj.Text("isHideProjEng"));
                    curTtemValueObj.Add("isHideSupplier", modelVauleObj.Text("isHideSupplier"));
                    curTtemValueObj.Add("isNeedVersion", modelVauleObj.Text("isNeedVersion"));
                    curTtemValueObj.Add("firstTagName", modelVauleObj.Text("firstTagName"));
                    curTtemValueObj.Add("SecendTagName", modelVauleObj.Text("SecendTagName"));
                    result = dataOp.Insert("ProjDocCategory", curTtemValueObj);   //插入
                    ObjIdsList.Add(uniqueKey, result.BsonInfo.Int("projDocCatId"));
                }

            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }

            #endregion
            json = TypeConvert.InvokeResultToPageJson(result);
            return Json(json);
        }

        /// <summary>
        /// 获取目录模板
        /// </summary>
        /// <param name="tbName"></param>
        /// <returns></returns>
        public ActionResult GetProjCatModelTree()
        {
            int projCatModelId = PageReq.GetParamInt("projCatModelId");
            List<BsonDocument> catList = dataOp.FindAllByQuery("ProjDocCategory", Query.EQ("projCatModelId", projCatModelId.ToString())).ToList();//获取模板下的目录
            List<TreeNode> treeList = TreeHelper.GetSingleTreeList(catList);

            return new XmlTree(treeList);
        }

        /// <summary>
        /// 载入目录模板
        /// </summary>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult ProjCatModelInsert()
        {
            PageJson json = new PageJson();
            InvokeResult result = new InvokeResult() { Status = Status.Successful };
            int engId = PageReq.GetParamInt("engId"); //获取当前地块Id
            var engObj = dataOp.FindOneByQuery("XH_DesignManage_Engineering", Query.EQ("engId",engId.ToString()));//地块对象
            int projCatModelId = PageReq.GetParamInt("projCatModelId"); //需要导入的模板
            var modelObj = dataOp.FindOneByQuery("ProjDocCategoryModel", Query.EQ("projCatModelId", projCatModelId.ToString())); //模板对象
            var modelItemList = dataOp.FindAllByKeyVal("ProjDocCategory", "projCatModelId", projCatModelId.ToString()).ToList();  //模板下的目录
            var ObjIdsList = new Dictionary<int, int>();//用于存储新增的旧keyId 与新keyId对应字典
            if (modelObj == null || engObj==null)
            {
                result.Message = "模板创建失败，请重试！";
                json = TypeConvert.InvokeResultToPageJson(result);
                return Json(json);
            }
            #region 创建目录
            try
            {
                foreach (var modelVauleObj in modelItemList.OrderBy(c => c.Text("nodeKey")))  //遍历模板数据
                {

                    var uniqueKey = modelVauleObj.Int("projDocCatId");
                    var curTtemValueObj = new BsonDocument();
                    var nodePid = modelVauleObj.Int("nodePid");   //获取父节点
                    var ParentId = 0;
                    var name = modelVauleObj.Text("name");
                    if (nodePid != 0)
                    {
                        if (!ObjIdsList.ContainsKey(nodePid)) continue;
                        ParentId = ObjIdsList[nodePid];
                    }
                    else
                    {
                        name = engObj.Text("name") + "根目录";
                    }
                    curTtemValueObj.Add("engId", engId.ToString());//地块Id
                    curTtemValueObj.Add("srcprojCatModelId", projCatModelId.ToString()); //来源模板Id
                    curTtemValueObj.Add("srcprojDocCatId", modelVauleObj.Text("projDocCatId")); //来源目录Id
                    curTtemValueObj.Add("name", name);
                    curTtemValueObj.Add("nodePid", ParentId);
                    curTtemValueObj.Add("catTypeId", modelVauleObj.Text("catTypeId"));
                    curTtemValueObj.Add("status", modelVauleObj.Text("status"));
                    curTtemValueObj.Add("viewLevel", modelVauleObj.Text("viewLevel"));
                    curTtemValueObj.Add("isAllowUpload", modelVauleObj.Text("isAllowUpload"));
                    curTtemValueObj.Add("isNeedHide", modelVauleObj.Text("isNeedHide"));
                    curTtemValueObj.Add("isHideProjEng", modelVauleObj.Text("isHideProjEng"));
                    curTtemValueObj.Add("isNeedVersion", modelVauleObj.Text("isNeedVersion"));
                    curTtemValueObj.Add("isHideSupplier", modelVauleObj.Text("isHideSupplier"));
                    curTtemValueObj.Add("firstTagName", modelVauleObj.Text("firstTagName"));
                    curTtemValueObj.Add("SecendTagName", modelVauleObj.Text("SecendTagName"));
                    result = dataOp.Insert("ProjDocCategory", curTtemValueObj);   //插入
                    ObjIdsList.Add(uniqueKey, result.BsonInfo.Int("projDocCatId"));
                }

            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }

            #endregion
            json = TypeConvert.InvokeResultToPageJson(result);
            return Json(json);
        }
        /// <summary>
        /// 保存设计单位评价关联图纸包文件
        /// </summary>
        /// <param name="packageId">packageIds</param>
        /// <param name="queryStr">queryStr</param>
        /// <param name="query">tbName</param>
        /// <returns></returns>
        public ActionResult SavePacekageFiles(string packageIds, string queryStr, string tbName)
        {
            var result = new InvokeResult();
            var packIds = packageIds.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            var fileIds = "";
            foreach (var packId in packIds)
            {
                var fileRels = dataOp.FindAllByQuery("FileRelation", Query.And(
                    Query.EQ("tableName", "ProjDocPackage"),
                    Query.EQ("keyName", "packageId"),
                    Query.EQ("keyValue", packId)
                )).ToList();
                var tempFileIds = string.Join(",", fileRels.Select(t => t.String("fileId")));
                if (tempFileIds.Length > 0)
                {
                    if (fileIds != "")
                        fileIds += "," + tempFileIds;
                    else
                        fileIds = tempFileIds;
                }
            }

            result = dataOp.Update(tbName, TypeConvert.NativeQueryToQuery(queryStr), new BsonDocument().Add("fileIds", fileIds));


            return Json(TypeConvert.InvokeResultToPageJson(result));
        }
    }
}
