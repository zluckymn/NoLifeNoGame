using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Yinhe.ProcessingCenter;
using Yinhe.ProcessingCenter.DataRule;
using MongoDB.Bson;
using Yinhe.ProcessingCenter.Common;
using Yinhe.ProcessingCenter.Document;
using System.Text.RegularExpressions;
using System.IO;
using MongoDB.Driver.Builders;
using System.Web.Script.Serialization;

namespace Yinhe.WebHost.Controllers
{
    public class ClientController : Yinhe.ProcessingCenter.ControllerBase
    {
        //
        // GET: /Test/

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult NodeEdit2()
        {
            return View();
        }

        public ActionResult TZFrmResultEdit()
        {
            return View();
        }

        public ActionResult NodeAttrEdit()
        {
            return View();
        }

        public ActionResult TZSubNode()
        {
            return View();
        }

        public ActionResult TZSubMapCheck()
        {
            return View();
        }

        public ActionResult AlterUpload()
        {
            return View();
        }
        public ActionResult DrawingUpload()
        {
            return View();
        }


        #region 富力天正插件页面
        public ActionResult TZFrmResultEditFL()
        {
            return View();
        }
        public JsonResult GetProjNodeUserSpace()
        {
            string projId = PageReq.GetParam("curNodeId");

            DataOperation dataOp = new DataOperation();

            List<BsonDocument> nodeUserSpaceList = dataOp.FindAllByKeyVal("NodeUserSpace", "nodeId", projId.ToString()).ToList();
            List<string> userIdList = nodeUserSpaceList.Select(s => s.String("userId")).ToList();
            var userList = (from m in dataOp.FindAllByKeyValList("SysUser", userIdList)
                            select new
                            {
                                id = m.Int("userId"),
                                name = m.String("name")
                            }).ToList();
            return Json(userList, JsonRequestBehavior.AllowGet);

        }
        #endregion

        /// <summary>
        /// 获取项目节点树形XML
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="curNodeId"></param>
        /// <param name="typeObjId"></param>
        /// <param name="itself"></param>
        /// <returns></returns>
        public JsonResult GetProjNodeTreeJson(string tbName, string curNodeId, int typeObjId, int itself)
        {
            DataOperation dataOp = new DataOperation();
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
            allNodeList = allNodeList.OrderBy(a => a.String("nodeKey")).ToList();
            List<Item> treeList = new List<Item>();
            if (allNodeList.Count > 0)
            {
                string tabString = "---";
                int parentNodeLevel = allNodeList[0].Int("nodeLevel");
                foreach (var node in allNodeList)
                {
                    int tabCount = node.Int("nodeLevel") - parentNodeLevel;
                    string nodeTabString = string.Empty;
                    for (int i = 0; i < tabCount; i++)
                    {
                        nodeTabString += tabString;
                    }
                    treeList.Add(new Item()
                    {
                        id = node.Int("nodeId"),
                        name = string.Format("{0}{1}", nodeTabString, node.String("name"))
                    });
                }
            }
            return Json(treeList, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 保存提交上来的数据
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        [ValidateInput(false)]
        public ActionResult SavePostInfo(FormCollection saveForm)
        {
            InvokeResult result = new InvokeResult();
            DataOperation dataOp = new DataOperation();

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
            BsonDocument curData = new BsonDocument();  //当前数据,即操作前数据

            if (queryStr.Trim() != "") curData = dataOp.FindOneByQuery(tbName, TypeConvert.NativeQueryToQuery(queryStr));

            result = dataOp.Save(tbName, queryStr, dataStr);

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
                    string str = result.BsonInfo.Text(keyName);
                    saveForm["keyValue"] = result.BsonInfo.Text(keyName);
                    string t = saveForm["keyValue"].ToString();
                    string c = result.BsonInfo.String(keyName);
                    t = "";
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

            #region 保存日志
            if (result.Status == Status.Successful)
            {
                //dataOp.LogDataStorage(tbName, queryStr.Trim() == "" ? StorageType.Insert : StorageType.Update, curData, result.BsonInfo);
            }
            #endregion

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        /// <summary>
        /// 上传多个文件
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        public string SaveMultipleUploadFiles(FormCollection saveForm)
        {
            DataOperation dataOp = new DataOperation();

            string tableName = PageReq.GetForm("tableName");
            string keyName = PageReq.GetForm("keyName");
            string keyValue = saveForm["keyValue"].ToString();
            string localPath = PageReq.GetForm("uploadFileList");
            string fileSaveType = saveForm["fileSaveType"] != null ? saveForm["fileSaveType"] : "multiply";
            int fileTypeId = PageReq.GetFormInt("fileTypeId");
            int fileObjId = PageReq.GetFormInt("fileObjId");
            int uploadType = PageReq.GetFormInt("uploadType");
            string subMapParam = PageReq.GetForm("subMapParam");
            string guid2d = PageReq.GetForm("guid2d");
            string oldGuid2d = PageReq.GetForm("oldguid2d");
            bool isPreDefine = saveForm["isPreDefine"] != null ? bool.Parse(saveForm["isPreDefine"]) : false;

            Dictionary<string, string> propDic = new Dictionary<string, string>();
            FileOperationHelper opHelper = new FileOperationHelper();
            List<InvokeResult<FileUploadSaveResult>> result = new List<InvokeResult<FileUploadSaveResult>>();

            localPath = localPath.Replace("\\\\", "\\");

            #region 如果保存类型为单个single 则删除旧的所有关联文件
            if (!string.IsNullOrEmpty(fileSaveType))
            {
                if (fileSaveType == "single")
                {
                    opHelper.DeleteFile(tableName, keyName, keyValue);
                }
            }
            #endregion

            #region 通过关联读取对象属性
            if (!string.IsNullOrEmpty(localPath.Trim()))
            {
                string[] fileStr = Regex.Split(localPath, @"\|H\|", RegexOptions.IgnoreCase);
                Dictionary<string, string> filePath = new Dictionary<string, string>();
                foreach (string file in fileStr)
                {
                    string[] filePaths = Regex.Split(file, @"\|Y\|", RegexOptions.IgnoreCase);

                    if (filePaths.Length > 0)
                    {
                        string[] subfile = Regex.Split(filePaths[0], @"\|Z\|", RegexOptions.IgnoreCase);
                        if (subfile.Length > 0)
                        {
                            if (!filePath.Keys.Contains(subfile[0]))
                            {
                                if (filePaths.Length >= 2)
                                {
                                    filePath.Add(subfile[0], filePaths[1]);
                                }
                                else
                                {
                                    filePath.Add(subfile[0], "");
                                }
                            }
                        }
                    }
                }

                if (fileObjId != 0)
                {

                    List<BsonDocument> docs = new List<BsonDocument>();
                    docs = dataOp.FindAllByKeyVal("FileObjPropertyRelation", "fileObjId", fileObjId.ToString()).ToList();

                    List<string> strList = new List<string>();
                    strList = docs.Select(t => t.Text("filePropId")).Distinct().ToList();
                    var doccList = dataOp.FindAllByKeyValList("FileProperty", "filePropId", strList);
                    foreach (var item in doccList)
                    {
                        var formValue = saveForm[item.Text("dataKey")];
                        if (formValue != null)
                        {
                            propDic.Add(item.Text("dataKey"), formValue.ToString());
                        }
                    }
                }

                List<FileUploadObject> singleList = new List<FileUploadObject>();   //纯文档上传
                List<FileUploadObject> objList = new List<FileUploadObject>();      //当前传入类型文件上传
                foreach (var str in filePath)
                {
                    FileUploadObject obj = new FileUploadObject();
                    obj.fileTypeId = fileTypeId;
                    obj.fileObjId = fileObjId;
                    obj.localPath = str.Key;
                    obj.tableName = tableName;
                    obj.keyName = keyName;
                    obj.keyValue = keyValue;
                    obj.uploadType = uploadType;
                    obj.isPreDefine = isPreDefine;
                    obj.isCover = false;
                    obj.propvalueDic = propDic;
                    obj.rootDir = str.Value;
                    obj.subMapParam = subMapParam;
                    obj.guid2d = guid2d;
                    if (uploadType != 0 && (obj.rootDir == "null" || obj.rootDir.Trim() == ""))
                    {
                        singleList.Add(obj);
                    }
                    else
                    {
                        objList.Add(obj);
                    }
                }

                result = opHelper.UploadMultipleFiles(objList, (UploadType)uploadType);//(UploadType)uploadType
                if (singleList.Count > 0)
                {
                    result = opHelper.UploadMultipleFiles(singleList, (UploadType)0);
                }
            }
            else
            {
                PageJson jsonone = new PageJson();
                jsonone.Success = false;
                return jsonone.ToString() + "|";

            }
            #endregion

            PageJson json = new PageJson();
            var ret = opHelper.ResultConver(result);

            #region 如果有关联的文件Id列表,则保存关联记录
            string fileVerIds = PageReq.GetForm("fileVerIds");
            List<int> fileVerIdList = fileVerIds.SplitToIntList(",");

            if (ret.Status == Status.Successful && fileVerIdList.Count > 0)
            {
                List<StorageData> saveList = new List<StorageData>();
                foreach (var tempVerId in fileVerIdList)
                {
                    StorageData tempData = new StorageData();

                    tempData.Name = "FileAlterRelation";
                    tempData.Type = StorageType.Insert;
                    tempData.Document = new BsonDocument().Add("alterFileId", result.FirstOrDefault().Value.fileId.ToString())
                                                          .Add("fileVerId", tempVerId);

                    saveList.Add(tempData);
                }

                dataOp.BatchSaveStorageData(saveList);
            }
            #endregion

            json.Success = ret.Status == Status.Successful ? true : false;
            var strResult = json.ToString() + "|" + ret.Value + "|" + keyValue;
            return strResult;
        }

        public string SaveTZCADDWGFile(FormCollection saveForm)
        {
            DataOperation dataOp = new DataOperation();
            Session["userId"] = PageReq.GetFormInt("userId");
            string oldGuid2d = PageReq.GetForm("oldguid2d");
            bool isPreDefine = saveForm["isPreDefine"] != null ? bool.Parse(saveForm["isPreDefine"]) : false;
            BsonDocument oldfileDoc = dataOp.FindOneByKeyVal("FileLibrary", "guid2d", oldGuid2d);

            #region 判断是否是新建地块
            int isNewProj = PageReq.GetFormInt("isNewProj");

            if (isNewProj == 1)
            {
                InvokeResult newProjRet = new InvokeResult();

                int nodePid = PageReq.GetFormInt("nodePid");
                string name = PageReq.GetForm("name");
                int typeObjId = 9;

                if (nodePid > 0)
                {
                    newProjRet = dataOp.Insert("ProjectNode", new BsonDocument().Add("name", name).Add("nodePid", nodePid.ToString()).Add("typeObjId", typeObjId.ToString()));
                }

                saveForm["keyValue"] = newProjRet.BsonInfo.String("nodeId");
            }
            #endregion

            if (saveForm["fileObjId"].ToString() == "57")   //如果文档类型是变更单，则直接进入保存
            {
                return this.SaveMultipleUploadFiles(saveForm);
            }

            if (oldfileDoc == null)
            {
                return this.SaveMultipleUploadFiles(saveForm);
            }
            else
            {
                return this.SaveDWGNewVersion(saveForm, oldfileDoc, dataOp);
            }

        }

        private string SaveDWGNewVersion(FormCollection saveForm, BsonDocument oldfileDoc, DataOperation dataOp)
        {
            string localPath = PageReq.GetForm("uploadFileList");
            string name = PageReq.GetForm("name");
            string subMapParam = PageReq.GetForm("subMapParam");
            string newGuid2d = PageReq.GetForm("guid2d");

            BsonDocument fileDoc = new BsonDocument();
            fileDoc.Add("version", (oldfileDoc.Int("version") + 1).ToString());
            fileDoc.Add("localPath", localPath);
            fileDoc.Add("ext", Path.GetExtension(localPath));
            fileDoc.Add("name", string.IsNullOrEmpty(name) == true ? Path.GetFileName(localPath) : name);
            fileDoc.Add("subMapParam", subMapParam);
            fileDoc.Add("guid2d", newGuid2d);
            var query = Query.EQ("fileId", oldfileDoc.String("fileId"));
            dataOp.Update("FileLibrary", query, fileDoc);

            BsonDocument fileVerDoc = new BsonDocument();
            fileVerDoc.Add("name", fileDoc.String("name"));
            fileVerDoc.Add("ext", fileDoc.String("ext"));
            fileVerDoc.Add("localPath", localPath);
            fileVerDoc.Add("version", fileDoc.String("version"));
            fileVerDoc.Add("subMapParam", subMapParam);
            fileVerDoc.Add("guid2d", newGuid2d);
            fileVerDoc.Add("fileId", oldfileDoc.String("fileId"));
            InvokeResult result = dataOp.Insert("FileLibVersion", fileVerDoc);
            fileVerDoc = result.BsonInfo;
            int fileRelId = 0;
            if (result.Status == Status.Successful)
            {
                var relResult = dataOp.Update("FileRelation", "db.FileRelation.distinct('_id',{'fileId':'" + fileDoc.String("fileId") + "'})", "version=" + fileDoc.String("version"));
                fileRelId = result.BsonInfo.Int("fileRelId");
            }

            List<FileParam> paramList = new List<FileParam>();
            FileParam fp = new FileParam();
            fp.path = localPath;
            fp.ext = fileDoc.String("ext");
            fp.strParam = string.Format("{0}@{1}-{2}-{3}", "sysObject", fileRelId, oldfileDoc.String("fileId"), fileVerDoc.String("fileVerId"));
            paramList.Add(fp);
            JavaScriptSerializer script = new JavaScriptSerializer();
            string strJson = script.Serialize(paramList);
            PageJson json = new PageJson();
            json.Success = result.Status == Status.Successful ? true : false;
            string keyValue = saveForm["keyValue"] != null ? saveForm["keyValue"] : "0";
            var strResult = json.ToString() + "|" + strJson + "|" + keyValue;
            return strResult;
        }
    }
}
