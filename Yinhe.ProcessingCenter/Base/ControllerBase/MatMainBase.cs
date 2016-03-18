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
using System.Web.Security;
using MongoDB.Driver.Builders;
using System.Collections;
using Yinhe.ProcessingCenter.Permissions;
using System.IO;
using MongoDB.Driver;
using MongoDB.Bson.IO;
using Yinhe.ProcessingCenter.Common;
using Yinhe.WebReference.Schdeuler;
using System.Transactions;
using System.Threading;
using System.Diagnostics;
using org.in2bits.MyXls;


namespace Yinhe.ProcessingCenter
{
     
    /// <summary>
    /// MVC Controller的通用基类
    /// </summary>
    [CommonExceptionAttribute]
    public partial class MatControllerBase : Controller
    {
        /// <summary>
        /// "table1,key1|table2,key2"
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="keyName"></param>
        /// <param name="keyValue"></param>
        /// <param name="relInfos"></param>
        /// <returns></returns>
        public JsonResult CommonDel(string tbName, string keyName, string keyValue, string relInfos)
        {
            try
            {
                if (string.IsNullOrEmpty(tbName) && string.IsNullOrEmpty(keyName))
                    throw new Exception("传入参数有误，删除失败！");
                if (!string.IsNullOrEmpty(relInfos))
                {
                    var tempList = relInfos.SplitParam("|");
                    foreach (var temp in tempList)
                    {
                        var kv = temp.SplitParam(",");
                        IMongoQuery relQuery = Query.EQ(kv[1], keyValue);
                        if (dataOp.FindAllByQuery(kv[0], relQuery).Any())
                            return Json(new { Success = false, msg = "数据已经被引用，不能删除" });
                    }
                }
                IMongoQuery query = Query.EQ(keyName, keyValue);
                var result = dataOp.Delete(tbName, query);
                return Json(ConvertToPageJson(result));
            }
            catch (Exception ex)
            {
                return Json(new { Success = false, msg = ex.Message });
            }
        }


        /// <summary>
        /// 通过主键删除
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="keyValue"></param>
        /// <param name="relTbNames"></param>
        /// <returns></returns>
        public JsonResult CommonDelByPK(string tbName, string keyVal, string relTbName, string relKey)
        {
            try
            {
                TableRule rule = new TableRule(tbName);
                var pk = rule.PrimaryKey;

                if (!string.IsNullOrEmpty(relTbName))//如果传入关联表，则判断该数据是否被关联。
                {
                    var relKey1 = pk;
                    if (!string.IsNullOrEmpty(relKey))
                        relKey1 = relKey;
                    IMongoQuery relQuery = Query.EQ(relKey1, keyVal);
                    var rels = dataOp.FindAllByQuery(relTbName, relQuery);
                    if (rels.Any())//存在关联数据，不能被删除，直接返回错误提示信息
                    {
                        return Json(new { Success = false, msg = "数据已经被引用，不能删除" });
                    }
                }
                IMongoQuery query = Query.EQ(pk, keyVal);
                var result = dataOp.Delete(tbName, query);

                return Json(ConvertToPageJson(result));
            }
            catch (Exception ex)
            {
                return Json(new { Success = false, msg = ex.Message });
            }
        }

        #region 私有属性
        private DataOperation _dataOp = null;



        /// <summary>
        /// 数据操作类
        /// </summary>
        public DataOperation dataOp
        {
            get
            {
                if (_dataOp == null) _dataOp = new DataOperation();
                return this._dataOp;
            }
        }
        #endregion

        #region 公共属性
        /// <summary>
        /// 获取当前用户的Id
        /// </summary>
        /// <value>当前用户的ID</value>
        public int CurrentUserId
        {
            get
            {
                if (string.IsNullOrEmpty(PageReq.GetSession("UserId").ToString()))
                {
                    return -1;
                }
                else
                {
                    return int.Parse(PageReq.GetSession("UserId"));
                }
            }
        }

        #endregion

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

            #region 构建数据
            string tbName = PageReq.GetForm("tbName");
            string queryStr = PageReq.GetForm("queryStr");
            string dataStr = PageReq.GetForm("dataStr");

            BsonDocument dataBson = new BsonDocument();

            if (dataStr.Trim() == "")
            {
                foreach (var tempKey in saveForm.AllKeys)
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

            #region 保存数据
            result = dataOp.Save(tbName, queryStr != "" ? TypeConvert.NativeQueryToQuery(queryStr) : Query.Null, dataBson);
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

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        private static readonly object objPad = new object();
        /// <summary>
        /// 乔鑫保存材料
        /// </summary>
        /// <param name="saveForm">上传表单</param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        [ValidateInput(false)]
        public ActionResult SaveMaterialQX(FormCollection saveForm)
        {
            lock (objPad)
            {
                InvokeResult result = new InvokeResult();

                string tbName = PageReq.GetForm("tbName");
                string queryStr = PageReq.GetForm("queryStr");
                string dataStr = PageReq.GetForm("dataStr");

                BsonDocument dataBson = new BsonDocument();

                if (dataStr.Trim() == "")
                {
                    foreach (var tempKey in saveForm.AllKeys)
                    {
                        if (tempKey == "tbName" || tempKey == "queryStr" || tempKey.Contains("fileList[") || tempKey.Contains("param.")) continue;

                        dataBson.Add(tempKey, PageReq.GetForm(tempKey));
                    }
                }
                else
                {
                    dataBson = TypeConvert.ParamStrToBsonDocument(dataStr);
                }



                IMongoQuery query = TypeConvert.NativeQueryToQuery(queryStr);
                BsonDocument old = dataOp.FindOneByQuery(tbName, query);//更新之前的材料信息
                MaterialBll materialBll = MaterialBll._();
                string code = materialBll.CalcMaterialCodeQX(dataBson, old);//计算材料编码
                dataBson.TryAdd("materialNumber", code);
                if (query == Query.Null)//新增材料
                {
                    result = dataOp.Insert(tbName, dataBson);
                }
                else//编辑材料
                {
                    //BsonDocument old = dataOp.FindOneByQuery(tbName, query);
                    result = dataOp.Update(tbName, query, dataBson);
                }

                BsonDocument material = result.BsonInfo;
                #region QX产品系列
                int serialSave = PageReq.GetParamInt("serialSave");  //值为1保存关联
                if (serialSave == 1)
                {
                    var seriesId = PageReq.GetParam("seriesId");
                    var typeId = PageReq.GetParam("typeId");
                    var relId = PageReq.GetParam("relId");
                    var itemId = PageReq.GetParam("itemId");
                    var matId = result.BsonInfo.Text("matId");
                    BsonDocument serialData = new BsonDocument();
                    serialData.Add("seriesId", seriesId);
                    serialData.Add("typeId", typeId);
                    serialData.Add("retId", relId);
                    serialData.Add("itemId", itemId);
                    serialData.Add("matId", matId);
                    dataOp.Insert("ProductItemMatRelation", serialData);
                }
                #endregion
                #region 文件上传
                int primaryKey = 0;
                TableRule rule = new TableRule(tbName);

                ColumnRule columnRule = rule.ColumnRules.Where(t => t.IsPrimary == true).FirstOrDefault();
                string keyName = columnRule != null ? columnRule.Name : "";
                if (query != Query.Null)
                {
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

                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
        }

        /// <summary>
        /// 三盛保存材料
        /// </summary>
        /// <param name="saveForm">上传表单</param>
        /// <param name="prices">价格列表</param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        [ValidateInput(false)]
        public ActionResult SaveMaterial(FormCollection saveForm, string prices, string extendAttrs)
        {
            lock (objPad)
            {
                InvokeResult result = new InvokeResult();

                string tbName = PageReq.GetForm("tbName");
                string queryStr = PageReq.GetForm("queryStr");
                string dataStr = PageReq.GetForm("dataStr");

                BsonDocument dataBson = new BsonDocument();

                if (dataStr.Trim() == "")
                {
                    foreach (var tempKey in saveForm.AllKeys)
                    {
                        if (tempKey == "tbName" || tempKey == "queryStr" || tempKey.Contains("fileList[") || tempKey.Contains("param.")) continue;

                        dataBson.Add(tempKey, PageReq.GetForm(tempKey));
                    }
                }
                else
                {
                    dataBson = TypeConvert.ParamStrToBsonDocument(dataStr);
                }



                IMongoQuery query = TypeConvert.NativeQueryToQuery(queryStr);
                BsonDocument old = dataOp.FindOneByQuery(tbName, query);//更新之前的材料信息
                MaterialBll materialBll = MaterialBll._();
                string code =string.Empty;
                if (CustomerCode.LF == SysAppConfig.CustomerCode)
                {
                    code = materialBll.CalcMaterialCode(dataBson, old, "SysCity");//计算材料编码
                }
                else
                {
                     code = materialBll.CalcMaterialCode(dataBson, old);//计算材料编码
                }
                dataBson.TryAdd("matNum", code);
                if (query == Query.Null)//新增材料
                {
                    result = dataOp.Insert(tbName, dataBson);
                }
                else//编辑材料
                {
                    //BsonDocument old = dataOp.FindOneByQuery(tbName, query);
                    result = dataOp.Update(tbName, query, dataBson);
                }

                BsonDocument material = result.BsonInfo;
                SaveMaterialPrices(prices, material.String("matId"));//保存材料价格
                SaveExtendAttrs(extendAttrs, material.String("matId"));

                #region 文件上传
                int primaryKey = 0;
                TableRule rule = new TableRule(tbName);

                ColumnRule columnRule = rule.ColumnRules.Where(t => t.IsPrimary == true).FirstOrDefault();
                string keyName = columnRule != null ? columnRule.Name : "";
                if (query != Query.Null)
                {
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

                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
        }

        private void SaveExtendAttrs(string extendAttrs, string matId)
        {
            string BaseCatExtendVal = "BaseCatExtendVal";
            dataOp.Delete(BaseCatExtendVal, Query.EQ("matId", matId));//删除原有扩展属性信息
            foreach (var item in extendAttrs.SplitParam("|Y|"))
            {
                var infos = item.SplitParam(StringSplitOptions.None, "|$|");
                if (infos.Count() > 1 && !string.IsNullOrEmpty(infos[1]))
                {
                    dataOp.Insert(BaseCatExtendVal, new BsonDocument { { "matId", matId }, { "extendId", infos[0] }, { "val", infos[1] } });
                }
            }
        }

        /// <summary>
        /// 保存材料的价格
        /// </summary>
        /// <param name="prices"></param>
        private void SaveMaterialPrices(string prices, string matId)
        {
            int tempId;
            if (int.TryParse(matId, out tempId) && tempId > 0)
            {
                string tbName = "MaterialPrices";
                dataOp.Delete(tbName, Query.EQ("matId", matId));//删除之前的所有价格信息
                var temps = prices.SplitParam("|Y|");

                foreach (var temp in temps)
                {
                    var s = temp.SplitParam(StringSplitOptions.None, "|$|");

                    BsonDocument bson = new BsonDocument { { "date", s[0] }, { "price", s[1] }, { "matId", matId }, { "market", s[2] } };
                    dataOp.Insert(tbName, bson);

                }
            }
        }


        /// <summary>
        /// 通用删除
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="queryJson"></param>
        /// <returns></returns>
        public ActionResult UniversalSave(string tbName, string queryJson, string dataJson)
        {
            InvokeResult result = new InvokeResult();

            QueryDocument query = null;

            if (string.IsNullOrEmpty(queryJson) == false)
            {
                query = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<QueryDocument>(queryJson);
            }

            BsonDocument data = null;

            if (string.IsNullOrEmpty(dataJson) == false)
            {
                data = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(dataJson);
            }

            if (tbName != "")
            {
                result = dataOp.Save(tbName, query, data);
            }

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        /// <summary>
        /// 删除提交上来的信息
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        /// 
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult DelePostInfo(FormCollection saveForm)
        {
            InvokeResult result = new InvokeResult();

            string tbName = saveForm["tbName"] != null ? saveForm["tbName"] : "";
            string queryStr = saveForm["queryStr"] != null ? saveForm["queryStr"] : "";
            string dataStr = "";

            #region 删除文档
            int primaryKey = 0;
            TableRule rule = new TableRule(tbName);
            string keyName = rule.GetPrimaryKey();
            if (!string.IsNullOrEmpty(queryStr))
            {
                var query = TypeConvert.NativeQueryToQuery(queryStr);
                var recordDoc = dataOp.FindOneByQuery(tbName, query);
                saveForm["keyValue"] = result.BsonInfo.Text(keyName);
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
            //删除文件

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        /// <summary>
        /// 通用删除
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="queryJson"></param>
        /// <returns></returns>
        public ActionResult UniversalDelete(string tbName, string queryJson)
        {
            InvokeResult result = new InvokeResult();

            QueryDocument query = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<QueryDocument>(queryJson);

            if (tbName != "" && query != null)
            {
                result = dataOp.Delete(tbName, query);
            }

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        /// <summary>
        /// 根据主键Id批量删除
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult BatchDeleteByPrimaryKey(FormCollection saveForm)
        {
            string tbName = saveForm["tbName"] != null ? saveForm["tbName"] : "";

            string ids = saveForm["ids"] != null ? saveForm["ids"] : "";

            InvokeResult result = new InvokeResult();

            List<int> idList = TypeConvert.StringToIntEnum(ids, ",").ToList();

            List<StorageData> delList = new List<StorageData>();

            TableRule table = new TableRule(tbName);

            foreach (var tempId in idList)
            {
                var query = Query.EQ(table.GetPrimaryKey(), tempId.ToString());

                StorageData temp = new StorageData();

                temp.Name = tbName;
                temp.Query = query;
                temp.Type = StorageType.Delete;
                temp.Document = new BsonDocument();

                delList.Add(temp);
            }


            result = dataOp.BatchSaveStorageData(delList);

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        /// <summary>
        /// 移动提交上来的信息
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        [ValidateInput(false)]
        public ActionResult MovePostInfo(FormCollection saveForm)
        {
            string tbName = saveForm["tbName"] != null ? saveForm["tbName"] : "";       //要移动的表
            string moveId = saveForm["moveId"] != null ? saveForm["moveId"] : "";       //要移动的节点Id
            string moveToId = saveForm["moveToId"] != null ? saveForm["moveToId"] : ""; //要移动至的目标节点
            string type = saveForm["type"] != null ? saveForm["type"] : "";             //要移动的类型:pre,next,child

            InvokeResult result = dataOp.Move(tbName, moveId, moveToId, type);

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        /// <summary>
        /// 获取简单表的Json列表
        /// </summary>
        /// <param name="tbName">表名</param>
        /// <param name="ps">每页条数(默认20,-1不翻页)</param>
        /// <param name="cu">当前页</param>
        /// <param name="qu">查询语句(原生查询)</param>
        /// <param name="of">排序字段</param>
        /// <param name="ot">排序类型(空正序,desc倒序)</param>
        /// <returns></returns>
        public ActionResult GetSingleTableJson(string tbName, int? ps, int? cu, string qu, string of, string ot)
        {
            int pageSize = (ps != null && ps.Value != 0) ? ps.Value : 20;
            int current = (cu != null && cu.Value != 0) ? cu.Value : 1;

            string query = qu != null ? qu : "";
            string orderField = of != null ? of : "";
            string orderType = ot != null ? ot : "";

            var queryComp = TypeConvert.NativeQueryToQuery(query);

            List<BsonDocument> allDocList = queryComp != null ? dataOp.FindAllByQuery(tbName, queryComp).ToList() : dataOp.FindAll(tbName).ToList();

            int allCount = allDocList.Count();

            if (orderField != null && orderField != "")
            {
                if (orderType != null && orderType == "desc")
                {
                    allDocList = allDocList.OrderByDescending(t => t.String(orderField)).ToList();
                }
                else
                {
                    allDocList = allDocList.OrderBy(t => t.String(orderField)).ToList();
                }
            }

            List<Hashtable> retList = new List<Hashtable>();

            if (pageSize != -1)
            {
                allDocList = allDocList.Skip((current - 1) * pageSize).Take(pageSize).ToList();
            }

            foreach (var tempDoc in allDocList)
            {
                tempDoc.Add("allCount", allCount.ToString());
                tempDoc.Remove("_id");

                retList.Add(tempDoc.ToHashtable());
            }

            return this.Json(retList, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 将中文转换为拼音
        /// </summary>
        /// <param name="cnStr"></param>
        /// <returns></returns>
        public string ChangeChinesetoPinYin(string cnStr)
        {
            cnStr = HttpUtility.UrlDecode(cnStr);

            string tempResult = PinyinHelper.GetPinyin(cnStr);

            Regex rex = new Regex("[a-z0-9A-Z_]+");
            MatchCollection mc = rex.Matches(tempResult);

            string retStr = "";

            foreach (Match m in mc)
            {
                retStr += m.ToString();
            }

            return retStr;
        }

        public JsonResult GetList()
        {
            var list = dataOp.FindAll("SysUser").Take(10).ToList();
            List<Hashtable> retList = new List<Hashtable>();

            foreach (var tempDoc in list)
            {
                tempDoc.Remove("_id");

                retList.Add(tempDoc.ToHashtable());
            }

            return Json(retList, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 系统权限通用列表新增接口 对比数据库已有记录 
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult CommonInsertPost()
        {
            InvokeResult result = new InvokeResult();
            string tbName = PageReq.GetForm("tbName");//表名
            string ids = PageReq.GetForm("ids");//主键值
            int roleId = PageReq.GetFormInt("roleId");//角色ID
            TableRule table = new TableRule(tbName);
            string primaryKey = table.GetPrimaryKey();//获得主键名
            string[] idArray = ids.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);

            #region 删除旧的全部关联
            result = dataOp.Delete(tbName, "db." + tbName + ".distinct('_id',{'roleId':'" + roleId + "'})");
            #endregion

            if (idArray.Length > 0 && result.Status == Status.Successful)
            {
                foreach (var item in idArray)
                {
                    BsonDocument doc = new BsonDocument();
                    foreach (var entity in table.ColumnRules.Where(t => t.IsPrimary == false && t.Name != "roleId").Take(1))
                    {
                        doc.Add(entity.Name, item);
                    }
                    doc.Add("roleId", roleId);
                    result = dataOp.Insert(tbName, doc);
                }
            }

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        #region 获取树形XML数据

        /// <summary>
        /// 获取单表普通树的XML列表
        /// </summary>
        /// <param name="tbName">表名</param>
        /// <param name="queryStr">查询条件(a=b&a=b&....)</param>
        /// <returns></returns>
        public ActionResult GetSingleTreeXML(string tbName, string queryStr)
        {
            List<BsonDocument> allNodeList = new List<BsonDocument>();

            if (string.IsNullOrEmpty(queryStr))
            {
                allNodeList = dataOp.FindAll(tbName).ToList();
            }
            else
            {
                allNodeList = dataOp.FindAllByQueryStr(tbName, queryStr).ToList();
            }

            List<TreeNode> treeList = TreeHelper.GetSingleTreeList(allNodeList);

            return new XmlTree(treeList);
        }

        /// <summary>
        /// 通过多条件获取单表普通树的XML列表
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="queryStr">(a=a:b=b:c=c)</param>
        /// <returns></returns>
        public ActionResult GetSingleTreeMutiQueryXML(string tbName, string queryStr)
        {
            List<BsonDocument> allNodeList = new List<BsonDocument>();

            if (string.IsNullOrEmpty(queryStr))
            {
                allNodeList = dataOp.FindAll(tbName).ToList();
            }
            else
            {
                queryStr = queryStr.Replace(":", "&");
                allNodeList = dataOp.FindAllByQueryStr(tbName, queryStr).ToList();
            }

            List<TreeNode> treeList = TreeHelper.GetSingleTreeList(allNodeList);

            return new XmlTree(treeList);
        }

        /// <summary>
        /// 获取单表普通树的XML列表
        /// </summary>
        /// <param name="tbName">表名</param>
        /// <param name="queryStr">查询条件(a=b&a=b&....)</param>
        /// <returns></returns>
        public ActionResult GetSingleNoRootTreeXML(string tbName, string queryStr)
        {
            List<BsonDocument> allNodeList = new List<BsonDocument>();

            if (string.IsNullOrEmpty(queryStr))
            {
                allNodeList = dataOp.FindAll(tbName).ToList();
            }
            else
            {
                allNodeList = dataOp.FindAllByQueryStr(tbName, queryStr).ToList();
            }

            List<TreeNode> treeList = new List<TreeNode>();
            List<TreeNode> tempTreeList = TreeHelper.GetSingleTreeList(allNodeList);

            if (tempTreeList.Count == 1)
            {
                treeList = tempTreeList[0].SubNodes;
            }

            return new XmlTree(treeList);
        }


        /// <summary>
        /// 获取单表普通树的XML列表
        /// </summary>
        /// <param name="tbName">表名</param>
        /// <param name="queryStr">查询条件(a=b&a=b&....)</param>
        /// <returns></returns>
        public ActionResult GetSingleTreeXMLByIntOrStr(string tbName, string queryStr)
        {
            List<BsonDocument> allNodeList = new List<BsonDocument>();
            string[] paramArry = queryStr.Split('&');
            if (paramArry.Length == 0)
            {
                allNodeList = dataOp.FindAll(tbName).ToList();
            }
            else
            {
                allNodeList = dataOp.FindAll(tbName).ToList();

                foreach (var tempArr in paramArry)
                {
                    var temp = tempArr.Split('=');

                    if (temp.Length == 2)
                    {
                        bool tempBool = true;
                        if (string.IsNullOrEmpty(temp[1]))
                        {
                            tempBool = false;
                        }
                        foreach (char c in temp[1])
                        {
                            if (!char.IsDigit(c))
                                tempBool = false;
                            break;
                        }

                        if (tempBool)
                        {
                            allNodeList = allNodeList.Where(x => x.String(temp[0]) == temp[1] || x.Int(temp[0]) == int.Parse(temp[1])).ToList();
                        }
                        else
                        {
                            allNodeList = allNodeList.Where(x => x.String(temp[0]) == temp[1]).ToList();
                        }
                    }
                }
            }
            List<TreeNode> treeList = TreeHelper.GetSingleTreeList(allNodeList);
            return new XmlTree(treeList);
        }

        /// <summary>
        /// 获取单表普通树的XML列表
        /// </summary>
        /// <param name="tbName">表名</param>
        /// <param name="queryStr">查询条件(原生查询:db.tbName....)</param>
        /// <returns></returns>
        public ActionResult GetSingleTreeXMLByQuery(string tbName, string queryStr)
        {
            List<BsonDocument> allNodeList = new List<BsonDocument>();

            var query = TypeConvert.NativeQueryToQuery(queryStr);

            if (query == null)
            {
                allNodeList = dataOp.FindAll(tbName).ToList();
            }
            else
            {
                allNodeList = dataOp.FindAllByQuery(tbName, query).ToList();
            }

            List<TreeNode> treeList = TreeHelper.GetSingleTreeList(allNodeList);

            return new XmlTree(treeList);
        }

        /// <summary>
        /// 获取单表普通树的子树XML
        /// </summary>
        /// <param name="tbName">表名</param>
        /// <param name="curNodeId">当前节点Id</param>
        /// <param name="lv">获取层级</param>
        /// <param name="itself">是否包含本身节点,0不包含,1包含</param>
        /// <returns></returns>
        public ActionResult GetSingleSubTreeXML(string tbName, string curNodeId, int itself)
        {
            int lv = PageReq.GetFormInt("lv") != 0 ? PageReq.GetFormInt("lv") : (PageReq.GetParamInt("lv") != 0 ? PageReq.GetParamInt("lv") : 0);   //展示层级

            TableRule tableEntity = new TableRule(tbName);    //获取表结构

            string primaryKey = tableEntity.ColumnRules.Where(t => t.IsPrimary == true).FirstOrDefault().Name;  //寻找默认主键

            List<BsonDocument> allNodeList = new List<BsonDocument>();

            if (curNodeId.Trim() != "0" && curNodeId != "")
            {
                BsonDocument curNode = dataOp.FindOneByKeyVal(tbName, primaryKey, curNodeId);

                if (lv == 0)    //获取所有子节点
                {
                    allNodeList = dataOp.FindChildNodes(tbName, curNodeId).ToList();
                }
                else        //获取对应层级的子节点
                {
                    allNodeList = dataOp.FindChildNodes(tbName, curNodeId).Where(t => t.Int("nodeLevel") <= (curNode.Int("nodeLevel") + lv)).ToList();
                }

                if (itself == 1)
                {
                    allNodeList.Add(curNode);
                }
            }
            else
            {
                allNodeList = dataOp.FindAll(tbName).ToList();
            }

            List<TreeNode> treeList = TreeHelper.GetSingleTreeList(allNodeList);

            return new XmlTree(treeList);
        }

        #endregion

        #region 文件相关操作
        /// <summary>
        /// 上传单个文件
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        public JsonResult SaveSingleUploadFile(FormCollection saveForm)
        {
            string tableName = saveForm["tableName"] != null ? saveForm["tableName"] : "";
            string keyName = saveForm["keyName"] != null ? saveForm["keyName"] : "";
            string keyValue = saveForm["keyValue"] != null ? saveForm["keyValue"] : "";
            int fileTypeId = saveForm["fileTypeId"] != null ? int.Parse(saveForm["fileTypeId"]) : 0;
            int fileObjId = saveForm["fileObjId"] != null ? int.Parse(saveForm["fileObjId"]) : 0;
            string localPath = saveForm["localPath"] != null ? saveForm["localPath"] : "";
            int uploadType = saveForm["uploadType  "] != null ? int.Parse(saveForm["uploadType  "]) : 0;
            bool isPreDefine = saveForm["isPreDefine"] != null ? bool.Parse(saveForm["isPreDefine"]) : false;
            bool isCover = saveForm["isCover"] != null ? bool.Parse(saveForm["isCover"]) : false;
            Dictionary<string, string> propDic = new Dictionary<string, string>();
            FileOperationHelper opHelper = new FileOperationHelper();
            InvokeResult<FileUploadSaveResult> result = new InvokeResult<FileUploadSaveResult>();
            #region 通过关联读取对象属性
            if (fileObjId != 0)
            {

                List<BsonDocument> docs = new List<BsonDocument>();
                docs = dataOp.FindAllByKeyVal("FileObjPropertyRelation", "fileTypeId", fileObjId.ToString()).ToList();

                List<string> strList = new List<string>();
                strList = docs.Where(t => t.Int("fileObjId") == fileObjId).Select(t => t.Text("dataKey")).Distinct().ToList();
                foreach (var item in strList)
                {
                    var formValue = saveForm[item];
                    if (formValue != null)
                    {
                        propDic.Add(item, formValue.ToString());
                    }
                }
            }
            FileUploadObject obj = new FileUploadObject();
            obj.fileTypeId = fileTypeId;
            obj.fileObjId = fileObjId;
            obj.localPath = localPath;
            obj.tableName = tableName;
            obj.keyName = keyName;
            obj.keyValue = keyValue;
            obj.uploadType = uploadType;
            obj.isPreDefine = isPreDefine;
            obj.isCover = isCover;
            obj.propvalueDic = propDic;
            result = opHelper.UploadSingleFile(obj);
            #endregion
            var ret = opHelper.ResultConver(result);
            return Json(ret);
        }

        /// <summary>
        /// 上传多个文件
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        public string SaveMultipleUploadFiles(FormCollection saveForm)
        {
            string tableName = PageReq.GetForm("tableName");
            tableName = !string.IsNullOrEmpty(PageReq.GetForm("tableName")) ? PageReq.GetForm("tableName") : PageReq.GetForm("tbName");
            string keyName = PageReq.GetForm("keyName");
            string keyValue = PageReq.GetForm("keyValue");
            if (string.IsNullOrEmpty(keyName))
            {
                keyName = saveForm["keyName"];
            }
            if (string.IsNullOrEmpty(keyValue) || keyValue == "0")
            {
                keyValue = saveForm["keyValue"];
            }
            string localPath = PageReq.GetForm("uploadFileList");
            string fileSaveType = saveForm["fileSaveType"] != null ? saveForm["fileSaveType"] : "multiply";
            int fileTypeId = PageReq.GetFormInt("fileTypeId");
            int fileObjId = PageReq.GetFormInt("fileObjId");
            int uploadType = PageReq.GetFormInt("uploadType");
            int fileRel_profId = PageReq.GetFormInt("fileRel_profId");
            int fileRel_stageId = PageReq.GetFormInt("fileRel_stageId");
            int fileRel_fileCatId = PageReq.GetFormInt("fileRel_fileCatId");

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
                Dictionary<string, string> filePathInfo = new Dictionary<string, string>();
                string s = fileSaveType.Length.ToString();
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
                                if (filePaths.Length == 3)
                                {
                                    filePath.Add(subfile[0], filePaths[1]);
                                    filePathInfo.Add(subfile[0], filePaths[2]);
                                }
                                else if (filePaths.Length == 2 || filePaths.Length > 3)
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
                #region 文档直接关联属性

                foreach (var tempKey in saveForm.AllKeys)
                {
                    if (!string.IsNullOrEmpty(tempKey) && tempKey.Contains("Property_"))
                    {
                        var formValue = saveForm[tempKey];
                        propDic.Add(tempKey, formValue.ToString());
                    }

                }

                #endregion

                List<FileUploadObject> singleList = new List<FileUploadObject>();   //纯文档上传
                List<FileUploadObject> objList = new List<FileUploadObject>();      //当前传入类型文件上传
                foreach (var str in filePath)
                {
                    FileUploadObject obj = new FileUploadObject();
                    List<string> infoList = new List<string>();
                    Dictionary<string, string> infoDc = new Dictionary<string, string>();
                    if (filePathInfo.ContainsKey(str.Key))
                    {
                        infoList = Regex.Split(filePathInfo[str.Key], @"\|N\|", RegexOptions.IgnoreCase).ToList();
                        foreach (var tempInfo in infoList)
                        {
                            string[] tempSingleInfo = Regex.Split(tempInfo, @"\|-\|", RegexOptions.IgnoreCase);
                            if (tempSingleInfo.Length == 2)
                            {
                                infoDc.Add(tempSingleInfo[0], tempSingleInfo[1]);
                            }
                        }

                    }
                    if (infoDc.ContainsKey("fileTypeId"))
                    {
                        obj.fileTypeId = Convert.ToInt32(infoDc["fileTypeId"]);
                    }
                    else
                    {
                        obj.fileTypeId = fileTypeId;
                    }
                    if (infoDc.ContainsKey("fileObjId"))
                    {
                        obj.fileObjId = Convert.ToInt32(infoDc["fileObjId"]);
                    }
                    else
                    {
                        obj.fileObjId = fileObjId;
                    }
                    if (filePathInfo.ContainsKey(str.Key))
                    {
                        obj.localPath = Regex.Split(str.Key, @"\|N\|", RegexOptions.IgnoreCase)[0];
                    }
                    else
                    {
                        obj.localPath = str.Key;
                    }
                    if (infoDc.ContainsKey("tableName"))
                    {
                        obj.tableName = infoDc["tableName"];
                    }
                    else
                    {
                        obj.tableName = tableName;
                    }
                    if (infoDc.ContainsKey("keyName"))
                    {
                        obj.keyName = infoDc["keyName"];
                    }
                    else
                    {
                        obj.keyName = keyName;
                    }
                    if (infoDc.ContainsKey("keyValue"))
                    {
                        if (infoDc["keyValue"] != "0")
                        {
                            obj.keyValue = infoDc["keyValue"];
                        }
                        else
                        {
                            obj.keyValue = keyValue;
                        }

                    }
                    else
                    {
                        obj.keyValue = keyValue;
                    }
                    if (infoDc.ContainsKey("uploadType"))
                    {
                        if (infoDc["uploadType"] != null && infoDc["uploadType"] != "undefined")
                        {
                            obj.uploadType = Convert.ToInt32(infoDc["uploadType"]);
                        }
                        else
                        {
                            obj.uploadType = uploadType;
                        }
                    }
                    else
                    {
                        obj.uploadType = uploadType;
                    }
                    obj.isPreDefine = isPreDefine;
                    if (infoDc.ContainsKey("isCover"))
                    {
                        if (infoDc["isCover"] == "Yes") { obj.isCover = true; }
                        else
                        {
                            obj.isCover = false;
                        }
                    }
                    else
                    {
                        obj.propvalueDic = propDic;
                    }
                    obj.rootDir = str.Value;
                    obj.fileRel_profId = fileRel_profId.ToString();
                    obj.fileRel_stageId = fileRel_stageId.ToString();
                    obj.fileRel_fileCatId = fileRel_fileCatId.ToString();

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
            json.Success = ret.Status == Status.Successful ? true : false;
            var strResult = json.ToString() + "|" + ret.Value;
            return strResult;
        }

        /// <summary>
        /// 上传新版本
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        public string SaveNewVersion(FormCollection saveForm)
        {
            InvokeResult<FileUploadSaveResult> result = new InvokeResult<FileUploadSaveResult>();
            FileOperationHelper opHelper = new FileOperationHelper();
            PageJson json = new PageJson();
            int fileId = saveForm["fileId"] != null ? int.Parse(saveForm["fileId"]) : 0;
            string localPath = saveForm["uploadFileList"] != null ? saveForm["uploadFileList"] : "";
            Dictionary<string, string> propDic = new Dictionary<string, string>();

            if (fileId != 0)
            {

                var fileModel = dataOp.FindOneByKeyVal("FileLibrary", "fileId", fileId.ToString());
                int fileObjId = fileModel.Int("fileObjId");
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
                FileUploadVersionObject obj = new FileUploadVersionObject();
                obj.fileId = fileId;
                obj.localPath = localPath;
                obj.propvalueDic = propDic;


                result = opHelper.UploadNewVersion(obj);
            }
            var ret = opHelper.ResultConver(result);
            json.Success = ret.Status == Status.Successful ? true : false;
            var strResult = json.ToString() + "|" + ret.Value;
            return strResult;
        }

        /// <summary>
        /// 设置其他首脑图
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public JsonResult SetBrainCoverImage(int id)
        {
            InvokeResult result = new InvokeResult();
            FileOperationHelper opHelper = new FileOperationHelper();
            result = opHelper.SetBrainCoverImage(id);
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        /// <summary>
        /// 设置封面图
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public JsonResult SetCoverImage(int id)
        {
            InvokeResult result = new InvokeResult();
            FileOperationHelper opHelper = new FileOperationHelper();
            result = opHelper.SetCoverImage(id);
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        /// <summary>
        /// 设置首页推送
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public JsonResult SetIndexPush()
        {
            bool flag = false;
            int id = PageReq.GetFormInt("fileRelId");
            string isPush = PageReq.GetForm("isPush");
            if (!string.IsNullOrEmpty(isPush))
            {
                bool.TryParse(isPush, out flag);
            }
            InvokeResult result = new InvokeResult();
            FileOperationHelper opHelper = new FileOperationHelper();
            result = opHelper.SetIndexPush(id, flag);
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }


        /// <summary>
        /// 更新文件描述
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        [ValidateInput(false)]
        public ActionResult UpdateFileDescription(FormCollection saveForm)
        {
            InvokeResult result = new InvokeResult();
            int fileId = saveForm["fileId"] != null ? int.Parse(saveForm["fileId"]) : 0;

            if (fileId != 0)
            {
                FileOperationHelper op = new FileOperationHelper();
                Dictionary<string, string> propDic = new Dictionary<string, string>();
                foreach (var tempKey in saveForm.AllKeys)
                {
                    if (tempKey.Contains("fileId")) continue;
                    propDic.Add(tempKey, saveForm[tempKey]);

                }
                result = op.UpdateFileDescription(fileId, propDic);

            }
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        /// <summary>
        /// 更新文件描述
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        [ValidateInput(false)]
        public ActionResult AddFileDescription(FormCollection saveForm)
        {
            InvokeResult result = new InvokeResult();
            string fileIds = saveForm["fileIds"] != null ? saveForm["fileIds"] : "";
            Dictionary<int, Dictionary<string, string>> propDic = new Dictionary<int, Dictionary<string, string>>();

            if (!string.IsNullOrEmpty(fileIds))
            {
                try
                {
                    string[] fileIdArray = fileIds.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    FileOperationHelper op = new FileOperationHelper();
                    Dictionary<string, string[]> prop = new Dictionary<string, string[]>();

                    foreach (var tempKey in saveForm.AllKeys)
                    {
                        if (tempKey.Contains("fileIds")) continue;
                        string propStr = saveForm[tempKey];
                        string[] array = propStr.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                        prop.Add(tempKey, array);
                    }

                    for (int i = 0; i < fileIdArray.Length; i++)
                    {
                        Dictionary<string, string> subDic = new Dictionary<string, string>();
                        foreach (var item in prop)
                        {
                            subDic.Add(item.Key, item.Value[i]);
                        }
                        propDic.Add(int.Parse(fileIdArray[i]), subDic);
                    }

                    result = op.SetFileDescription(propDic);
                }
                catch (Exception ex)
                {
                    result.Status = Status.Failed;
                    result.Message = ex.Message;
                }


            }
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }


        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        public JsonResult DeleFiles(FormCollection saveForm)
        {
            string fileRelIds = saveForm["delFileRelIds"] != null ? saveForm["delFileRelIds"] : "";

            InvokeResult result = new InvokeResult();
            FileOperationHelper opHelper = new FileOperationHelper();
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
            }

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        /// <summary>
        /// 通过文件ID删除文件
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        public JsonResult DeleFilesByFileId(FormCollection saveForm)
        {
            string fileRelIds = saveForm["delFileIds"] != null ? saveForm["delFileIds"] : "";

            InvokeResult result = new InvokeResult();
            FileOperationHelper opHelper = new FileOperationHelper();
            try
            {
                string[] fileArray;
                if (fileRelIds.Length > 0)
                {
                    fileArray = fileRelIds.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    if (fileArray.Length > 0)
                    {
                        //List<string> structIdList =  _dataOp.FindAllByKeyValList("FileLibrary", fileArray.ToList()).Select(f=>f.String("structId")).Distinct().ToList();
                        foreach (var item in fileArray)
                        {
                            result = opHelper.DeleteFileByFileId(int.Parse(item));
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
            }

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        /// <summary>
        /// 通过文件版本ID删除文件
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        public JsonResult DeleFilesByFileVerId(FormCollection saveForm)
        {
            string fileRelIds = saveForm["delFileIds"] != null ? saveForm["delFileIds"] : "";

            InvokeResult result = new InvokeResult();
            FileOperationHelper opHelper = new FileOperationHelper();
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
                            result = opHelper.DeleteFileByFileVerId(int.Parse(item));
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
            }

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        public JsonResult DeleFolder(FormCollection saveForm)
        {
            string fileRelIds = saveForm["delstructIds"] != null ? saveForm["delstructIds"] : "";

            InvokeResult result = new InvokeResult();
            FileOperationHelper opHelper = new FileOperationHelper();
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
                            result = opHelper.DeleteFolder(int.Parse(item));
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
            }

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        /// <summary>
        /// 判断用户是否能够下载,小于等于0不能下载
        /// </summary>
        /// <returns></returns>
        public int IsUserCanDownLoad()
        {
            return 100;
        }

        /// <summary>
        /// 批量设置文档缩略图地址
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public string SetFileThumbPicPath(string head)
        {
            List<BsonDocument> allFileList = dataOp.FindAllByQuery("FileLibrary", Query.Matches("thumbPicPath", "thum")).ToList();  //所有有缩略图的文件

            foreach (var tempFile in allFileList)
            {
                string oldHead = tempFile.String("thumbPicPath").Split(new string[] { "thum" }, StringSplitOptions.None)[0];   //路径头部

                string newHead = string.IsNullOrEmpty(head) ? "/" : head;

                if (newHead[newHead.Length - 1] != '/') newHead = newHead + "/";

                tempFile["thumbPicPath"] = tempFile.String("thumbPicPath").Replace(oldHead, newHead);
                dataOp.Update("FileLibrary", Query.EQ("fileId", tempFile.String("fileId")), new BsonDocument { { "thumbPicPath", tempFile["thumbPicPath"] } });
            }

            //MongoOperation mongoOp = new MongoOperation();

            //InvokeResult result = mongoOp.Save("FileLibrary", allFileList);

            //return result.Status == Status.Successful ? "Successful" : result.Message;
            return "";
        }

        #endregion

        #region 生命周期事件
        /// <summary>
        /// 权限验证
        /// </summary>
        /// <param name="filterContext"></param>
        protected override void OnAuthorization(AuthorizationContext filterContext)
        {
            dataOp.LogSysBehavior(SysLogType.General, HttpContext);    //当用户登录后 记录系统行为日志

            base.OnAuthorization(filterContext);
        }

        /// <summary>
        /// Action执行之前
        /// </summary>
        /// <param name="filterContext"></param>
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            //filterContext.RouteData[]
            base.OnActionExecuting(filterContext);
        }

        /// <summary>
        /// Action执行之后
        /// </summary>
        /// <param name="filterContext"></param>
        protected override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            base.OnActionExecuted(filterContext);
        }

        /// <summary>
        /// Result执行之前
        /// </summary>
        /// <param name="filterContext"></param>
        protected override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            #region 设置响应流
            if (this.CurrentUserId != -1 && AuthManage.UserType != UserTypeEnum.DebugUser && SysAppConfig.IsPlugIn == false)
            {
                if (filterContext.HttpContext.Response.ContentType == "text/html")
                {
                    filterContext.HttpContext.Response.Filter = new ResponseAuthFilter(Response.Filter, this.CurrentUserId);
                }
            }
            #endregion
            base.OnResultExecuting(filterContext);
        }

        /// <summary>
        /// Result执行之后
        /// </summary>
        /// <param name="filterContext"></param>
        protected override void OnResultExecuted(ResultExecutedContext filterContext)
        {
            base.OnResultExecuted(filterContext);
        }

        #endregion

        #region 系统方法
        /// <summary>
        /// 将页面导出成为PDF文件
        /// </summary>
        /// <returns></returns>
        public ActionResult SavePageToPdf()
        {
            string url = PageReq.GetParam("url");
            string name = PageReq.GetParam("name");
            if (string.IsNullOrEmpty(name))
            {
                name = DateTime.Now.ToString("yyyyMMddHHmmss");
            }
            string savePath = Server.MapPath("/UploadFiles/PDF/");
            if (!System.IO.Directory.Exists(savePath))
            {
                System.IO.Directory.CreateDirectory(savePath);
            }

            string fileName = Path.Combine(savePath, string.Format("{0}.pdf", name));
            url = string.Format("http://{0}/{1}", System.Web.HttpContext.Current.Request.ServerVariables["HTTP_HOST"], url);
            bool HasSucceed = true;

            // HtmlHelper.PageToPdfByteArray(url, fileName, Encoding.GetEncoding("utf-8"));
            // bool HasSucceed = HtmlHelper.Html2PDFSynch(url, fileName,""); 

            if (HasSucceed)
            {
                //讀成串流     
                Stream iStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                //回傳出檔案     
                return File(iStream, "application/pdf", name + ".pdf");
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 将JSON数据导入数据库
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        [ValidateInput(false)]
        public ActionResult ImportJsonInfoToDataBase(FormCollection saveForm)
        {
            PageJson json = new PageJson();

            string tbName = saveForm["tbName"] != null ? saveForm["tbName"] : "";
            string jsonUrl = saveForm["jsonUrl"] != null ? saveForm["jsonUrl"] : "";
            string isAdd = saveForm["isAdd"] != null ? saveForm["isAdd"] : "";          //是否追加 0 覆盖, 1 追加
            string isAffect = saveForm["isAffect"] != null ? saveForm["isAffect"] : "";   //是否影响其他表 0 不影响 1 影响

            if (System.IO.File.Exists(@jsonUrl) == false) //如果存在,则读取规则文件
            {
                json.Success = false;
                json.Message = "文件不存在";
                return Json(json);
            }

            if (tbName.Trim() == "")
            {
                json.Success = false;
                json.Message = "表名不能为空";
                return Json(json);
            }

            #region 读取导入数据
            StreamReader objReader = new StreamReader(@jsonUrl);

            string jsonStr = objReader.ReadToEnd();

            objReader.Close();

            List<BsonDocument> allBson = new List<BsonDocument>();

            BsonReader bsonReader = BsonReader.Create(jsonStr);

            while (bsonReader.CurrentBsonType != BsonType.EndOfDocument)
            {
                BsonDocument tempBson = BsonDocument.ReadFrom(bsonReader);

                allBson.Add(tempBson);
            }
            #endregion

            if (isAffect == "1")    //对其他表产生影响
            {
                #region 导入数据

                List<StorageData> saveDataList = new List<StorageData>();   //保存数据 

                if (isAdd != "1")       //覆盖,表中所有内容
                {
                    StorageData delAll = new StorageData();
                    delAll.Name = tbName;
                    delAll.Type = StorageType.Delete;
                    delAll.Query = Query.Exists("_id", true);

                    saveDataList.Add(delAll);
                }

                foreach (var tempBson in allBson)
                {
                    StorageData tempAdd = new StorageData();

                    tempAdd.Name = tbName;
                    tempAdd.Document = tempBson;
                    tempAdd.Type = StorageType.Insert;

                    saveDataList.Add(tempAdd);
                }

                InvokeResult result = dataOp.BatchSaveStorageData(saveDataList);
                json = TypeConvert.InvokeResultToPageJson(result);

                #endregion
            }
            else
            {
                #region 导入数据
                InvokeResult result = new InvokeResult();

                try
                {
                    MongoOperation mongoOp = new MongoOperation();

                    if (isAdd != "1")       //覆盖,表中所有内容
                    {
                        mongoOp.GetCollection(tbName).RemoveAll(SafeMode.True);
                        //SafeModeResult safeResult = mongoOp.GetCollection(tbName).RemoveAll(SafeMode.True);  //移除表中所有内容
                        //if (safeResult.HasLastErrorMessage) throw new Exception(safeResult.LastErrorMessage);
                    }

                    List<StorageData> saveDataList = new List<StorageData>();   //保存数据 

                    foreach (var tempBson in allBson)
                    {
                        StorageData tempAdd = new StorageData();

                        tempAdd.Name = tbName;
                        tempAdd.Document = tempBson;
                        tempAdd.Type = StorageType.Insert;

                        saveDataList.Add(tempAdd);
                    }

                    dataOp.BatchSaveStorageData(saveDataList);

                    result.Status = Status.Successful;
                }
                catch (Exception ex)
                {
                    result.Status = Status.Failed;
                    result.Message = ex.Message;
                }

                json = TypeConvert.InvokeResultToPageJson(result);

                #endregion
            }

            return Json(json);
        }

        /// <summary>
        /// 重置树形表中的4个关键属性
        /// </summary>
        /// <param name="tbName"></param>
        /// <returns></returns>
        public ActionResult ReSetTableTreeKey(string tbName)
        {
            PageJson json = new PageJson();

            TableRule tableEntity = new TableRule(tbName);    //获取表结构

            string primaryKey = tableEntity.ColumnRules.Where(t => t.IsPrimary == true).FirstOrDefault().Name;  //寻找默认主键

            if (tbName.Trim() == "" || primaryKey.Trim() == "")
            {
                json.Success = false;
                json.Message = "传入表名有误,或表不在规则文件中!";
                return Json(json);
            }

            List<BsonDocument> allDataList = dataOp.FindAll(tbName).ToList();   //获取表中的所有数据

            List<StorageData> allSaveData = this.ReSetSubNodeTreeKey(tbName, primaryKey, new BsonDocument(), allDataList, new Dictionary<int, string>(), new Dictionary<int, int>());

            InvokeResult result = dataOp.BatchSaveStorageData(allSaveData);

            json = TypeConvert.InvokeResultToPageJson(result);

            return Json(json);
        }

        /// <summary>
        /// 重置树形表的4个关键字
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="primaryKey"></param>
        /// <param name="curNode"></param>
        /// <param name="allDataList"></param>
        /// <param name="newKeyDic"></param>
        /// <param name="newLvDic"></param>
        /// <returns></returns>
        public List<StorageData> ReSetSubNodeTreeKey(string tbName, string primaryKey, BsonDocument curNode, List<BsonDocument> allDataList, Dictionary<int, string> newKeyDic, Dictionary<int, int> newLvDic)
        {
            List<StorageData> allSaveData = new List<StorageData>();

            int curPrimaryId = curNode.Int(primaryKey);
            List<BsonDocument> subNodeList = allDataList.Where(t => t.Int("nodePid") == curPrimaryId).ToList();  //当前级的所有对应子节点

            int curLevel = newLvDic.ContainsKey(curPrimaryId) ? newLvDic[curPrimaryId] : 0;
            string curNodeKey = newKeyDic.ContainsKey(curPrimaryId) ? newKeyDic[curPrimaryId] : "";
            int index = 1;

            foreach (var subNode in subNodeList.OrderBy(t => t.Int(primaryKey)))  //循环子节点,改变子节点的对应值
            {
                int nodeLevel = curLevel + 1;
                string nodeOrder = index.ToString();
                string nodeKey = curNodeKey != "" ? curNodeKey + "." + nodeOrder.PadLeft(6, '0') : nodeOrder.PadLeft(6, '0');

                newLvDic.Add(subNode.Int(primaryKey), nodeLevel);
                newKeyDic.Add(subNode.Int(primaryKey), nodeKey);

                StorageData tempData = new StorageData();
                tempData.Name = tbName;
                tempData.Type = StorageType.Update;
                tempData.Query = Query.EQ(primaryKey, subNode.String(primaryKey));
                tempData.Document = new BsonDocument().Add("nodeLevel", nodeLevel.ToString())
                                                      .Add("nodeOrder", nodeOrder.ToString())
                                                      .Add("nodeKey", nodeKey.ToString());
                allSaveData.Add(tempData);
                allSaveData.AddRange(ReSetSubNodeTreeKey(tbName, primaryKey, subNode, allDataList, newKeyDic, newLvDic));   //递归

                index++;
            }

            return allSaveData;
        }

        /// <summary>
        /// 消息发送
        /// </summary>
        /// <returns></returns>
        public ActionResult SendMsg()
        {
            InvokeResult result = new InvokeResult();

            SystemMsg msgToManager = new SystemMsg();

            msgToManager.Content = "这是一条测试信息!!";
            msgToManager.ContentHead = "测试头部";
            msgToManager.CreateDate = DateTime.Now;

            msgToManager.ToUserIds = new int[] { 1, 2 };
            msgToManager.SenderId = 1;
            msgToManager.Title = "标准下载申请";
            msgToManager.TypeId = 1;//系统消息的类型，
            msgToManager.SendEmail = true;
            msgToManager.SendMobileSMS = false;

            try
            {
                if (msgToManager.ToUserIds.Length > 0 && msgToManager.ToUserIds[0] != 0)
                {
                    Yinhe.WebReference.YinheSchdeuler.SendSysMessage(Yinhe.WebReference.Schdeuler.Group.Msg_Result_ResultJudge,
                                       Guid.NewGuid().ToString(), DateTime.Now, msgToManager);

                }

                result.Status = Status.Successful;
            }
            catch (Exception ex)
            {
                result.Status = Status.Failed;
                result.Message = ex.Message;
            }

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        /// <summary>
        /// 将数据库表的所有字段重置为字符串型
        /// </summary>
        /// <param name="tbName"></param>
        /// <returns></returns>
        public ActionResult ReSetTableDataToString(string tbName)
        {
            PageJson json = new PageJson();
            InvokeResult result = new InvokeResult();

            TableRule table = new TableRule(tbName);

            string primaryKey = table.GetPrimaryKey();

            List<BsonDocument> oldDataList = dataOp.FindAll(tbName).ToList();

            List<StorageData> saveList = new List<StorageData>();

            List<string> filterList = new List<string>() { "_id", primaryKey, "createDate", "createUserId", "order", "underTable", "updateDate", "updateUserId", "nodePid", "nodeOrder", "nodeLevel", "nodeKey" };

            foreach (var tempOld in oldDataList)
            {
                StorageData tempSave = new StorageData();

                tempSave.Name = tbName;
                tempSave.Query = Query.EQ(primaryKey, tempOld.String(primaryKey));

                BsonDocument tempdata = dataOp.FindOneByQuery(tbName, tempSave.Query);

                BsonDocument bson = new BsonDocument();

                foreach (var tempEl in tempOld.Elements)
                {
                    if (filterList.Contains(tempEl.Name)) continue;

                    bson.Add(tempEl.Name, tempEl.Value.ToString());
                }

                tempSave.Document = bson;
                tempSave.Type = StorageType.Update;

                saveList.Add(tempSave);
            }

            result = dataOp.BatchSaveStorageData(saveList);

            json = TypeConvert.InvokeResultToPageJson(result);
            return Json(json);
        }

        /// <summary>
        /// 获取当前数据库所有表信息
        /// </summary>
        /// <returns></returns>
        public ActionResult GetCurDBAllTableJson()
        {
            MongoOperation mongoOp = new MongoOperation();

            List<string> allNameList = mongoOp.GetDataBase().GetCollectionNames().ToList();   //所有表名

            List<Hashtable> retList = new List<Hashtable>();

            List<TableRule> allTableList = TableRule.GetAllTables();

            foreach (var tempName in allNameList)
            {
                Hashtable temp = new Hashtable();

                temp.Add("name", tempName);

                TableRule tempTable = allTableList.Where(t => t.Name == tempName).FirstOrDefault();

                if (tempTable != null)
                {
                    temp.Add("isRule", "1");
                    temp.Add("remark", tempTable.Remark);
                }
                else
                {
                    temp.Add("isRule", "0");
                }

                retList.Add(temp);
            }

            return this.Json(retList, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 重置日志日期时间
        /// </summary>
        /// <param name="tbName"></param>
        /// <returns></returns>
        public ActionResult ReSetSysLogTime(string tbName)
        {
            MongoOperation mongoOp = new MongoOperation();
            MongoServer server = mongoOp.GetServer();
            MongoDatabase database = mongoOp.GetDataBase();
            MongoCollection<BsonDocument> logs = database.GetCollection(tbName);
            MongoCursor<BsonDocument> allLogs = logs.FindAll();

            InvokeResult result = new InvokeResult() { Status = Status.Successful };

            try
            {
                using (server.RequestStart(database))
                {
                    foreach (var tempLog in allLogs)
                    {
                        if (tempLog.String("timeSort", "") == "")
                        {
                            var query = Query.EQ("logTime", tempLog.String("logTime"));
                            var update = Update.Set("timeSort", tempLog.Date("logTime").ToString("yyyyMMddHHmmss"))
                                               .Set("logTime", tempLog.Date("logTime").ToString("yyyy-MM-dd HH:mm:ss"));

                            if (tempLog.String("logUserId", "") == "")
                            {
                                update.Set("logUserId", tempLog.String("userId"));
                            }

                            logs.Update(query, update);
                        }
                    }
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
        /// 重置任意表的任意字段为Int类型
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public ActionResult ReSetColumnToInt(string tbName, string columnName)
        {
            PageJson json = new PageJson();
            InvokeResult result = new InvokeResult();

            MongoOperation mongoOp = new MongoOperation();

            try
            {
                if (mongoOp.GetDataBase().CollectionExists(tbName))
                {
                    List<BsonDocument> bsonList = dataOp.FindAll(tbName).ToList();

                    if (bsonList.Count > 0)
                    {
                        foreach (var tempBson in bsonList)
                        {
                            QueryComplete query = Query.EQ("_id", ObjectId.Parse(tempBson.String("_id")));

                            BsonDocument bson = new BsonDocument();

                            bson.Add(columnName, tempBson.Int(columnName));

                            mongoOp.Save(tbName, query, bson);
                        }
                    }
                }
                result.Status = Status.Successful;
            }
            catch (Exception ex)
            {
                result.Status = Status.Failed;
                result.Message = ex.Message;
            }

            json = TypeConvert.InvokeResultToPageJson(result);
            return Json(json);

        }

        /// <summary>
        /// 将数据库中数据导出为文件
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="queryJson"></param>
        public void ExportDataBaseToFile(string tbName, string queryJson)
        {
            string fileName = string.Empty;
            List<BsonDocument> resultList = new List<BsonDocument>();

            if (tbName != "" && queryJson != "")
            {
                QueryDocument query = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<QueryDocument>(queryJson);

                resultList = dataOp.FindAllByQuery(tbName, query).ToList();
                fileName = string.Format("{0}_{1}_{2}.json", tbName, DateTime.Now.ToString("yyyyMMdd"), "A");
            }
            else if (tbName != "")
            {
                resultList = dataOp.FindAll(tbName).ToList();
                fileName = string.Format("{0}_{1}_{2}.json", tbName, DateTime.Now.ToString("yyyyMMdd"), "I");
            }

            List<string> filterList = new List<string>() { "_id", "underTable" };     //多余字段

            StringBuilder fileStr = new StringBuilder();

            foreach (var tempBson in resultList)
            {
                BsonDocument tempData = new BsonDocument();

                if (BsonDocumentExtension.IsNullOrEmpty(tempBson) == false)
                {
                    foreach (var tempElement in tempBson.Elements)
                    {
                        if (filterList.Contains(tempElement.Name)) continue;

                        tempData.Add(tempElement.Name, tempElement.Value);
                    }
                }

                fileStr.Append(tempData.ToString());
            }

            OutputFile("text/plain", fileName, fileStr.ToString());
        }

        /// <summary>
        /// 将html表格导出成为Excel
        /// </summary>
        /// <param name="FileType"></param>
        /// <param name="FileName"></param>
        /// <param name="ExcelContent"></param>
        public void OutputFile(string fileType, string fileName, string fileContent)
        {
            System.Web.HttpContext.Current.Response.Charset = "UTF-8";
            System.Web.HttpContext.Current.Response.ContentEncoding = System.Text.Encoding.UTF8;
            System.Web.HttpContext.Current.Response.AppendHeader("Content-Disposition", "attachment;filename=" + HttpUtility.UrlEncode(fileName, System.Text.Encoding.UTF8).ToString());
            System.Web.HttpContext.Current.Response.ContentType = fileType;
            System.IO.StringWriter tw = new System.IO.StringWriter();
            System.Web.HttpContext.Current.Response.Output.Write(fileContent.ToString());
            System.Web.HttpContext.Current.Response.Flush();
            System.Web.HttpContext.Current.Response.End();
        }

        #endregion

        #region 复制通用目录
        /// <summary>
        /// 复制通用目录
        /// </summary>
        /// <param name="bookTask"></param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult CopeGeneralDirectory(string tableName)
        {
            InvokeResult result = new InvokeResult();
            int projId = PageReq.GetParamInt("projId");
            int sourceId = PageReq.GetParamInt("sourceId");//复制来源

            int nodePid = PageReq.GetParamInt("nodePid");
            PageJson json = new PageJson();
            List<BsonDocument> newDir = new List<BsonDocument>();
            List<BsonDocument> oldDir = new List<BsonDocument>();
            BsonDocument projInfo = new BsonDocument();
            projInfo = dataOp.FindOneByQuery("XH_DesignManage_Project", Query.EQ("projId", projId.ToString()));
            string projName = string.Empty;
            if (projInfo != null)
            {
                projName = projInfo.String("name");
            }
            //oldDir=dataOp.FindAllByQuery(tableName,Query.EQ("typeId","1")).ToList();


            TableRule tableEntity = new TableRule(tableName);
            string primaryKey = string.Empty;
            if (tableEntity == null)
            {
                primaryKey = "nodeId";
            }
            else
            {
                primaryKey = tableEntity.ColumnRules.Where(t => t.IsPrimary == true).FirstOrDefault().Name;
            }
            var entity = new BsonDocument();
            entity.Add("name", projName + "目录");
            entity.Add("projId", projId.ToString());
            entity.Add("isTemplate", "0");
            result = dataOp.Insert(tableName, entity);
            oldDir = dataOp.FindAllByQuery(tableName + "Dir", Query.EQ(primaryKey, sourceId.ToString())).ToList();
            if (result.Status == Status.Successful)
            {
                CopyGeneralDir(newDir, oldDir, nodePid, null, projId, tableName, result.BsonInfo);
            }
            json.Success = result.Status == Status.Successful;
            json.Message = result.Message;

            return this.Json(json); ;


        }
        public void CopyGeneralDir(List<BsonDocument> newDirList, List<BsonDocument> oldDirList, int nodePid, BsonDocument parent, int projId, string tableName, BsonDocument dirIfno)
        {
            var dir = oldDirList.Where(m => m.Int("nodePid") == nodePid).ToList();
            if (dir.Count() == 0)
                return;
            TableRule tableEntity = new TableRule(tableName + "Dir");
            TableRule tableEntity1 = new TableRule(tableName);//模板或项目目录数据项
            string primaryKey = string.Empty;
            string primaryKey1 = string.Empty;
            if (tableEntity1 == null)
            {
                primaryKey = "nodeDirId";
                primaryKey1 = "nodeId";
            }
            else
            {
                primaryKey1 = tableEntity1.ColumnRules.Where(t => t.IsPrimary == true).FirstOrDefault().Name;
                if (tableEntity == null)
                {
                    primaryKey = primaryKey1.Substring(0, primaryKey1.Length - 2);
                    primaryKey = primaryKey + "Id";
                }
                else
                {
                    primaryKey = tableEntity.ColumnRules.Where(t => t.IsPrimary == true).FirstOrDefault().Name;
                }
            }
            foreach (var tempDir in dir)
            {
                #region  复制目录
                var newTempDir = new BsonDocument();
                newTempDir.Add("projId", projId.ToString());
                newTempDir.Add("src" + primaryKey, tempDir.String(primaryKey));
                newTempDir.Add("name", tempDir.String("name"));
                newTempDir.Add("typeId", "0");
                newTempDir.Add("isFill", tempDir.String("isFill"));
                newTempDir.Add("colsType", tempDir.String("colsType"));
                newTempDir.Add(primaryKey1, dirIfno.String(primaryKey1));
                newTempDir.Add("nodePid", parent != null ? parent.String(primaryKey) : "0");
                #endregion

                var result = dataOp.Insert(tableName + "Dir", newTempDir);
                if (result.Status != Status.Successful)
                {
                    return;
                }
                newTempDir = result.BsonInfo;
                newDirList.Add(newTempDir);
                CopyGeneralDir(newDirList, oldDirList, tempDir.Int(primaryKey), newTempDir, projId, tableName, dirIfno);
            }
        }
        #endregion

        #region 问答系统打分
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult ChangeUserScore()
        {
            InvokeResult result = new InvokeResult();

            #region 构建数据
            string tbName = "UserScore";
            string queryStr = "";
            int type = PageReq.GetParamInt("type"); //打分类型
            int score = 0;
            if (type == 1)
            { //新增问题
                score = 5;
            }
            if (type == 2)
            { //回答问题
                score = 5;
            }
            if (type == 3)
            { //采纳问题
                score = 20;
            }
            if (type == 4)
            { //取消采纳
                score = -20;
            }
            if (type == 5)  //删除解答
            {
                score = -5;
            }
            var obj = dataOp.FindOneByQuery("UserScore", Query.EQ("userId", CurrentUserId.ToString()));
            score = score + obj.Int("score");
            if (obj != null)
            {
                queryStr = "db.UserScore.distinct('_id',{'userId':'" + CurrentUserId.ToString() + "'})";
            }
            BsonDocument dataBson = new BsonDocument();
            dataBson.Add("userId", CurrentUserId.ToString());
            dataBson.Add("score", score);
            #endregion

            #region 保存数据
            result = dataOp.Save(tbName, queryStr != "" ? TypeConvert.NativeQueryToQuery(queryStr) : Query.Null, dataBson);
            #endregion



            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        #endregion

        #region  成果查看的统计
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult ViewCount()
        {
            InvokeResult result = new InvokeResult();

            #region 构建数据
            string tbName = "ResultView";
            string queryStr = "";
            string tableName = PageReq.GetParam("tb");
            string keyName = PageReq.GetParam("keyName");
            string keyValue = PageReq.GetParam("keyValue");
            int count = 0;
            var obj = dataOp.FindOneByQuery("ResultView", Query.And(Query.EQ("keyName", keyName), Query.EQ("keyValue", keyValue), Query.EQ("tableName", tableName)));
            count = obj.Int("count") + 1;
            if (obj != null)
            {
                queryStr = "db.ResultView.distinct('_id',{'viewId':'" + obj.Text("viewId") + "'})";
            }
            BsonDocument dataBson = new BsonDocument();
            dataBson.Add("count", count);
            dataBson.Add("keyName", keyName);
            dataBson.Add("keyValue", keyValue);
            dataBson.Add("tableName", tableName);
            #endregion

            #region 保存数据
            result = dataOp.Save(tbName, queryStr != "" ? TypeConvert.NativeQueryToQuery(queryStr) : Query.Null, dataBson);
            #endregion
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        #endregion

        /// <summary>
        /// 保存系统任务
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        public ActionResult SaveTaskAccept(FormCollection saveForm)
        {
            string tbName = saveForm["tbName"] != null ? saveForm["tbName"] : "";
            string queryStr = saveForm["queryStr"] != null ? saveForm["queryStr"] : "";
            string dataStr = saveForm["dataStr"] != null ? saveForm["dataStr"] : "";
            int saveType = PageReq.GetParamInt("saveType");
            BsonDocument dataBson = new BsonDocument();
            if (dataStr.Trim() == "")
            {
                foreach (var tempKey in saveForm.AllKeys)
                {
                    if (tempKey == "tbName" || tempKey == "queryStr" || tempKey.Contains("teamRels")) continue;

                    //dataStr += string.Format("{0}={1}&", tempKey, saveForm[tempKey]);
                    dataBson.Add(tempKey, PageReq.GetForm(tempKey));
                }
            }

            //InvokeResult result = dataOp.Save(tbName, queryStr, dataStr);
            InvokeResult result = dataOp.Save(tbName, queryStr != "" ? TypeConvert.NativeQueryToQuery(queryStr) : Query.Null, dataBson);
            #region 成果交付物要求
            string systaskId = result.BsonInfo.String("systaskId");
            string teamRels = saveForm["teamRels"] != null ? saveForm["teamRels"] : "";
            List<StorageData> saveList = new List<StorageData>();
            List<string> teamRelArray = teamRels.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();
            List<BsonDocument> oldTeamRelList = dataOp.FindAllByKeyVal("SysTaskAccept", "systaskId", systaskId).ToList();   //所有关联
            foreach (var teamRel in teamRelArray) //循环新的关联,已存在则不添加,不存在则添加新的
            {
                string[] infoArr = teamRel.Split(new string[] { ":" }, StringSplitOptions.None);

                if (infoArr.Count() >= 2 && infoArr[0].Trim() != "")  //完整资料才会保存 
                {

                    string name = infoArr[0];
                    string remark = infoArr[1];

                    BsonDocument oldRel = oldTeamRelList.Where(t => t.Text("name") == name && t.Text("remark") == remark).FirstOrDefault();

                    if (oldRel == null || saveType == 2)
                    {
                        StorageData tempData = new StorageData();

                        tempData.Name = "SysTaskAccept";
                        tempData.Document = new BsonDocument().Add("systaskId", systaskId)
                                                              .Add("name", name)
                                                              .Add("remark", remark);
                        tempData.Type = StorageType.Insert;

                        saveList.Add(tempData);
                    }
                }
            }
            if (saveType != 2)  //两种保存数据的方式。 
            {
                foreach (var oldRel in oldTeamRelList)
                {
                    if (!teamRelArray.Contains(string.Format("{0}:{1}", oldRel.String("name"), oldRel.String("remark"))))
                    {
                        StorageData tempData = new StorageData();

                        tempData.Name = "SysTaskAccept";
                        tempData.Query = Query.EQ("acceptId", oldRel.String("acceptId"));
                        tempData.Type = StorageType.Delete;

                        saveList.Add(tempData);
                    }
                }
            }
            dataOp.BatchSaveStorageData(saveList);
            #endregion
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        #region  任务交互的日志记录
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult TaskLog()
        {
            InvokeResult result = new InvokeResult();

            #region 构建数据
            string tbName = "SysTaskLog";
            string queryStr = "";
            string acceptId = PageReq.GetParam("acceptId");
            string systaskId = PageReq.GetParam("systaskId");
            string logType = PageReq.GetParam("logType");  //1为任务负责人   2为任务创建人
            var fileObj = dataOp.FindAllByQuery("FileRelation", Query.And(Query.EQ("tableName", "SysTaskAccept"), Query.EQ("keyName", "acceptId"), Query.EQ("keyValue", acceptId))).OrderByDescending(c => c.Text("createDate")).FirstOrDefault(); //查找最新上传文件
            BsonDocument dataBson = new BsonDocument();
            dataBson.Add("fileId", fileObj.Text("fileId"));
            dataBson.Add("acceptId", acceptId);
            dataBson.Add("systaskId", systaskId);
            dataBson.Add("logType", logType);
            #endregion

            #region 保存数据
            result = dataOp.Save(tbName, queryStr != "" ? TypeConvert.NativeQueryToQuery(queryStr) : Query.Null, dataBson);
            #endregion
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        #endregion

        #region 创建表格
        /// <summary>
        ///创建表格
        /// </summary>
        /// <param name="htmlCode"></param>
        /// <param name="sheetName"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public string CreateExcelByHtmlCode(string htmlCode, string sheetName, string fileName)
        {
            PageJson result = new PageJson();
            if (string.IsNullOrEmpty(sheetName))
                sheetName = "sheet1";
            sheetName = Server.UrlDecode(sheetName);
            fileName = Server.UrlDecode(fileName);
            htmlCode = Server.UrlDecode(htmlCode);
            string fullFileName = string.Empty;
            try
            {
                ExcelWriter myExcel = new ExcelWriter(sheetName);
                myExcel.WriteData(htmlCode);
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tempFiles");
                fullFileName = myExcel.SaveAsFile(path, fileName);
            }
            catch
            {
                result.Success = false;
                result.Message = "未知原因导致生成表格失败！";
                return result.ToJsonString();
            }
            result.Success = true;
            result.Message = fullFileName;
            string str = result.ToJsonString();
            str = Regex.Replace(str, @"\\", "/");

            //删除临时生成的文件
            ThreadStart deleTempFile = () =>
            {
                Thread.Sleep(1000 * 30);
                System.IO.File.Delete(fullFileName);
            };
            Thread newThread = new Thread(deleTempFile);
            newThread.Start();
            return str;
        }
        #endregion

        #region 下载文件 +ActionResult GetFile(string filePath, string fileName)
        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="fileName">下载时的默认文件名称</param>
        /// <returns></returns>
        public ActionResult GetFile(string fullFileName, string downloadName, string contentType)
        {
            Response.Clear();
            Response.Buffer = true;
            Response.Charset = "UTF-8";
            Response.ContentEncoding = System.Text.Encoding.UTF8;
            Response.HeaderEncoding = System.Text.Encoding.UTF8;

            fullFileName = Server.UrlDecode(fullFileName);
            downloadName = Server.UrlDecode(downloadName);
            if (string.IsNullOrEmpty(downloadName))
                downloadName = "新建文件";
            string ext = Path.GetExtension(fullFileName);
            return File(fullFileName, contentType, Url.Encode(downloadName + ext));
        }
        #endregion



        #region GetExcelFile(string fullFileName, string downloadName) 下载Excel文件
        /// <summary>
        /// 下载Excel文件
        /// </summary>
        /// <param name="fullFileName"></param>
        /// <param name="downloadName"></param>
        /// <returns></returns>
        public ActionResult GetExcelFile(string fullFileName, string downloadName)
        {
            return GetFile(fullFileName, downloadName, "application/ms-excel");
        }
        #endregion

        #region  处理旧数据  设置由模板导入的任务预定义交付物 EditType=1
        /// <summary>
        /// 批量设置
        /// </summary>
        /// <returns></returns>
        public ActionResult SetEditType()
        {
            InvokeResult result = new InvokeResult();
            int expLibPlanId = PageReq.GetParamInt("expLibPlanId");
            int curChosePlanId = PageReq.GetParamInt("curChosePlanId");
            var expTaskList = dataOp.FindAllByQuery("XH_DesignManage_Task", Query.EQ("planId", expLibPlanId.ToString())); //获取计划的任务
            var curTaskList = dataOp.FindAllByQuery("XH_DesignManage_Task", Query.EQ("planId", curChosePlanId.ToString())).ToList(); //获取选中的具体项目的任务
            var taskDeliverAll = dataOp.FindAll("Scheduled_deliver");
            var srcTaskAll = dataOp.FindAll("XH_DesignManage_Task");
            var professionalAll = dataOp.FindAll("System_Professional");
            var sysStageAll = dataOp.FindAll("System_Stage");
            var projFilecatAll = dataOp.FindAll("ProjFileCategory");
            try
            {
                foreach (var curTask in curTaskList)
                {
                    var taskDeliver = taskDeliverAll.Where(c => c.Text("taskId") == curTask.Text("srcPrimTaskId"));
                    //dataOp.FindAllByQuery("Scheduled_deliver", Query.EQ("taskId", curTask.Text("taskId")));
                    var srcTask = srcTaskAll.Where(c => c.Text("taskId") == curTask.Text("srcPrimTaskId")).FirstOrDefault();
                    //dataOp.FindOneByQuery("XH_DesignManage_Task", Query.EQ("taskId", curTask.Text("srcPrimTaskId")));
                    if (srcTask == null) continue; //具体项目中，只操作模板导入的任务
                    var srctaskDeliver = taskDeliverAll.Where(c => c.Text("taskId") == srcTask.Text("taskId"));
                    //dataOp.FindAllByQuery("Scheduled_deliver", Query.EQ("taskId", srcTask.Text("taskId"))); //计划模板的交付物
                    foreach (var deliver in srctaskDeliver)
                    {
                        var professional = professionalAll.Where(c => c.Text("profId") == deliver.Text("profId")).FirstOrDefault();
                        //dataOp.FindOneByQuery("System_Professional", Query.EQ("profId", deliver.Text("profId")));
                        var sysStage = sysStageAll.Where(c => c.Text("stageId") == deliver.Text("stageId")).FirstOrDefault();
                        //dataOp.FindOneByQuery("System_Stage", Query.EQ("stageId", deliver.Text("stageId")));
                        var projFilecat = projFilecatAll.Where(c => c.Text("fileCatId") == deliver.Text("fileCatId")).FirstOrDefault();
                        //dataOp.FindOneByQuery("ProjFileCategory", Query.EQ("fileCatId", deliver.Text("fileCatId")));
                        var flag = taskDeliver.Where(c => c.Text("profId") == deliver.Text("profId") && c.Text("stageId") == deliver.Text("stageId") && c.Text("fileCatId") == deliver.Text("fileCatId")).FirstOrDefault();
                        BsonDocument dataBson = new BsonDocument();
                        if (flag != null)
                        { //通过 专业，阶段，属性判断，如果具体项目中任务交付物组合与模板中的组合一致，则判断此交付物组合是由模板导入时创建的
                            dataBson.Add("EditType", "1");
                            dataBson.Add("Remark", deliver.Text("Remark"));
                            dataBson.Add("srcDeliverId", deliver.Text("deliverId"));
                            result = dataOp.Save("Scheduled_deliver", Query.EQ("deliverId", flag.Text("deliverId")), dataBson); //更新旧数据，给旧数据加标记与关联
                        }
                        else
                        {
                            dataBson.Add("EditType", "1");
                            dataBson.Add("srcDeliverId", deliver.Text("deliverId"));
                            dataBson.Add("isForce", "1");
                            dataBson.Add("profId", deliver.Text("profId"));
                            dataBson.Add("stageId", deliver.Text("stageId"));
                            dataBson.Add("fileCatId", deliver.Text("fileCatId"));
                            dataBson.Add("projId", curTask.Text("projId"));
                            dataBson.Add("taskId", curTask.Text("taskId"));
                            dataBson.Add("Remark", deliver.Text("Remark"));
                            result = dataOp.Save("Scheduled_deliver", Query.Null, dataBson); //插入新数据
                        }
                    }
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
        /// 批量设置更新模板改动
        /// </summary>
        /// <returns></returns>
        public ActionResult CascadeUpdaeDeliverProperty()
        {
            InvokeResult result = new InvokeResult();
            int taskId = PageReq.GetParamInt("taskId");
            int porjId = PageReq.GetParamInt("porjId");
            int type = PageReq.GetParamInt("type");//-1 更新所有0更新未开始审批的任务

            var curTask = dataOp.FindOneByKeyVal("XH_DesignManage_Task", "taskId", taskId.ToString());//模板任务对象
            if (curTask == null)
            {
                result.Status = Status.Failed;
                result.Message = "当前任务不存在";
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }

            //dataOp.FindAllByQuery("Scheduled_deliver", Query.EQ("taskId", curTask.Text("taskId")));
            var srcTaskAll = dataOp.FindAll("XH_DesignManage_Task").Where(c => c.Text("srcPrimTaskId") == curTask.Text("taskId")).ToList();//获取从当前任务载入的所有任务
            switch (type)
            {
                case 0:
                    var fileterBusFlowInstanceIds = dataOp.FindAllByKeyVal("BusFlowInstance", "tableName", "XH_DesignManage_Task").Where(c => c.Int("approvalUserId") != 0).Select(c => c.Int("referFieldValue")).ToList();
                    srcTaskAll = srcTaskAll.Where(c => !fileterBusFlowInstanceIds.Contains(c.Int("taskId"))).ToList();//过滤已发起的任务
                    break;
                case -1: break;
                default: break;

            }

            var professionalAll = dataOp.FindAll("System_Professional").ToList();
            var sysStageAll = dataOp.FindAll("System_Stage").ToList();
            var projFilecatAll = dataOp.FindAll("ProjFileCategory").ToList();
            try
            {

                //dataOp.FindOneByQuery("XH_DesignManage_Task", Query.EQ("taskId", curTask.Text("srcPrimTaskId")));

                var taskDeliver = dataOp.FindAllByKeyVal("Scheduled_deliver", "taskId", taskId.ToString()).ToList();//计划模板的交付物
                //dataOp.FindAllByQuery("Scheduled_deliver", Query.EQ("taskId", srcTask.Text("taskId"))); 
                var srcTaskIds = srcTaskAll.Select(c => c.Text("taskId")).ToList();//获取派生任务id列表
                var srcTaskDeliverAll = dataOp.FindAllByKeyValList("Scheduled_deliver", "taskId", srcTaskIds).ToList();//获取派生任务对应交付物列表

                List<StorageData> updateDataList = new List<StorageData>();

                foreach (var deliver in taskDeliver)//模板任务交付物
                {
                    var professional = professionalAll.Where(c => c.Text("profId") == deliver.Text("profId")).FirstOrDefault();
                    //dataOp.FindOneByQuery("System_Professional", Query.EQ("profId", deliver.Text("profId")));
                    var sysStage = sysStageAll.Where(c => c.Text("stageId") == deliver.Text("stageId")).FirstOrDefault();
                    //dataOp.FindOneByQuery("System_Stage", Query.EQ("stageId", deliver.Text("stageId")));
                    var projFilecat = projFilecatAll.Where(c => c.Text("fileCatId") == deliver.Text("fileCatId")).FirstOrDefault();

                    foreach (var srcTask in srcTaskAll)//遍历所有派生任务
                    {

                        //dataOp.FindOneByQuery("ProjFileCategory", Query.EQ("fileCatId", deliver.Text("fileCatId")));
                        var flag = srcTaskDeliverAll.Where(c => c.Int("taskId") == srcTask.Int("taskId") && c.Text("profId") == deliver.Text("profId") && c.Text("stageId") == deliver.Text("stageId") && c.Text("fileCatId") == deliver.Text("fileCatId")).FirstOrDefault();

                        if (flag != null)
                        { //通过 专业，阶段，属性判断，如果具体项目中任务交付物组合与模板中的组合一致，则判断此交付物组合是由模板导入时创建的
                            BsonDocument dataBson = new BsonDocument();
                            dataBson.Add("EditType", "1");
                            dataBson.Add("Remark", deliver.Text("Remark"));
                            dataBson.Add("srcDeliverId", deliver.Text("deliverId"));
                            StorageData update = new StorageData();
                            update.Name = "Scheduled_deliver";
                            update.Query = Query.EQ("deliverId", flag.Text("deliverId"));
                            update.Type = StorageType.Update;
                            update.Document = dataBson;
                            updateDataList.Add(update);

                        }
                        else
                        {
                            StorageData update = new StorageData();
                            BsonDocument dataBson = new BsonDocument();
                            dataBson.Add("EditType", "1");
                            dataBson.Add("srcDeliverId", deliver.Text("deliverId"));
                            dataBson.Add("isForce", "1");
                            dataBson.Add("profId", deliver.Text("profId"));
                            dataBson.Add("stageId", deliver.Text("stageId"));
                            dataBson.Add("fileCatId", deliver.Text("fileCatId"));
                            dataBson.Add("projId", srcTask.Text("projId"));
                            dataBson.Add("taskId", srcTask.Text("taskId"));
                            dataBson.Add("Remark", deliver.Text("Remark"));
                            update.Name = "Scheduled_deliver";
                            update.Type = StorageType.Insert;
                            update.Document = dataBson;
                            updateDataList.Add(update);
                        }
                    }
                }
                if (updateDataList.Count() > 0)
                {
                    dataOp.BatchSaveStorageData(updateDataList);
                    #region 处理备注列表
                    List<StorageData> updateDataNewList = new List<StorageData>();
                    var remarkList = dataOp.FindAll("PredefineFileRemark").ToList();
                    var predefineFileRemarkList = remarkList.Where(c => taskDeliver.Select(x => x.Text("deliverId")).ToList().Contains(c.Text("deliverId"))).ToList();//获取模板任务下组合备注列表
                    var srcTaskDeliverNew = dataOp.FindAllByKeyValList("Scheduled_deliver", "taskId", srcTaskIds).ToList();//获取最新派生任务对应交付物列表
                    foreach (var expRemark in predefineFileRemarkList)//遍历计划模板的备注列表
                    {
                        var srcTempdeliverList = srcTaskDeliverNew.Where(c => c.Text("srcDeliverId") == expRemark.Text("deliverId")).ToList(); //查询派生的组合
                        foreach (var tempDeliver in srcTempdeliverList) //遍历
                        {
                            //查找派生组合备注
                            var remark = remarkList.Where(c => c.Text("deliverId") == tempDeliver.Text("deliverId") && c.Text("srcRemarkId") == expRemark.Text("remarkId")).FirstOrDefault();
                            if (remark != null)
                            {
                                BsonDocument dataBson = new BsonDocument();
                                dataBson.Add("EditType", "1");
                                dataBson.Add("name", expRemark.Text("name"));
                                dataBson.Add("isNeedUpLoad", expRemark.Text("isNeedUpLoad"));
                                dataBson.Add("srcRemarkId", expRemark.Text("remarkId"));
                                StorageData update = new StorageData();
                                update.Name = "PredefineFileRemark";
                                update.Query = Query.EQ("remarkId", remark.Text("remarkId"));
                                update.Type = StorageType.Update;
                                update.Document = dataBson;
                                updateDataNewList.Add(update);
                            }
                            else
                            {
                                StorageData update = new StorageData();
                                BsonDocument dataBson = new BsonDocument();
                                dataBson.Add("EditType", "1");
                                dataBson.Add("name", expRemark.Text("name"));
                                dataBson.Add("isNeedUpLoad", expRemark.Text("isNeedUpLoad"));
                                dataBson.Add("srcRemarkId", expRemark.Text("remarkId"));
                                dataBson.Add("taskId", tempDeliver.Text("taskId"));
                                dataBson.Add("deliverId", tempDeliver.Text("deliverId"));
                                update.Name = "PredefineFileRemark";
                                update.Type = StorageType.Insert;
                                update.Document = dataBson;
                                updateDataNewList.Add(update);

                            }

                        }

                    }
                    if (updateDataNewList.Count() > 0)
                    {
                        dataOp.BatchSaveStorageData(updateDataNewList);
                    }
                    #endregion
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


        #region  利用插件PDF导出
        /// <summary>
        ///
        /// </summary>
        public void PDFImport(string keyValue, string url, string tableName)
        {
            try
            {
                if (string.IsNullOrEmpty(keyValue) && string.IsNullOrEmpty(url) && string.IsNullOrEmpty(tableName))
                {
                    throw new Exception("传入参数有误！");
                }
                TableRule rule = new TableRule(tableName);
                string keyName = rule.GetPrimaryKey();
                if (string.IsNullOrEmpty(keyName))
                {
                    throw new Exception("传入参数有误！");
                }
                var entity = dataOp.FindOneByQuery(tableName, Query.EQ(keyName, keyValue));
                url = string.Format("{0}{1}?{2}={3}", SysAppConfig.Domain, url, keyName, keyValue);
                string tmpName = entity.Text("name") + ".pdf";
                string tmpName1 = HttpUtility.UrlEncode(tmpName, System.Text.Encoding.UTF8).Replace("+", "%20"); //主要为了解决包含非英文/数字名称的问题
                string savePath = Server.MapPath("/UploadFiles/temp");
                if (!System.IO.Directory.Exists(savePath))
                {
                    System.IO.Directory.CreateDirectory(savePath);
                }
                savePath = System.IO.Path.Combine(savePath, tmpName1);
                string wkhtmltopdfUrl = Server.MapPath("/bin/wkhtmltopdf.exe");
                Process p = System.Diagnostics.Process.Start(@"" + wkhtmltopdfUrl + "", @"" + url + " " + savePath);
                p.WaitForExit();
                DownloadFileZHTZ(savePath, tmpName);


            }
            catch (Exception ex)
            {

            }
        }
        #endregion


        #region 日志相关
        /// <summary>
        /// 读取日志文件
        /// </summary>
        /// <param name="jsonUrl"></param>
        /// <returns></returns>
        private List<BsonDocument> ReadLogFile(string tbName, string jsonUrl)
        {
            StreamReader objReader = new StreamReader(@jsonUrl);

            string jsonStr = objReader.ReadToEnd();

            objReader.Close();

            List<BsonDocument> allBson = new List<BsonDocument>();

            BsonReader bsonReader = BsonReader.Create(jsonStr);

            while (bsonReader.CurrentBsonType != BsonType.EndOfDocument)
            {
                BsonDocument tempBson = BsonDocument.ReadFrom(bsonReader);
                tempBson.Add("underTable", tbName);

                tempBson.Add("updateDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                tempBson.Add("updateUserId", CurrentUserId.ToString());

                allBson.Add(tempBson);
            }

            return allBson;
        }

        /// <summary>
        /// 插入日志文件中的数据
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="jsonUrl"></param>
        /// <returns></returns>
        private InvokeResult InsertLogData(string tbName, string jsonUrl)
        {
            InvokeResult result = new InvokeResult();

            MongoOperation mongoOp = new MongoOperation();

            TableRule tableEntity = new TableRule(tbName);    //获取表结构

            try
            {
                List<BsonDocument> allBsonList = this.ReadLogFile(tbName, jsonUrl);

                //插入数据
                mongoOp.GetCollection(tbName).InsertBatch(allBsonList);
            }
            catch (Exception ex)
            {
                result.Status = Status.Failed;
                result.Message = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// 删除日志文件中的数据
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="jsonUrl"></param>
        /// <returns></returns>
        private InvokeResult DeleteLogData(string tbName, string jsonUrl)
        {
            InvokeResult result = new InvokeResult();

            MongoOperation mongoOp = new MongoOperation();

            TableRule tableEntity = new TableRule(tbName);    //获取表结构

            try
            {
                List<BsonDocument> allBsonList = this.ReadLogFile(tbName, jsonUrl);

                //删除数据
                foreach (var temp in allBsonList)
                {
                    mongoOp.GetCollection(tbName).Remove(Query.EQ(tableEntity.PrimaryKey, temp.String(tableEntity.PrimaryKey)));
                }
            }
            catch (Exception ex)
            {
                result.Status = Status.Failed;
                result.Message = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// 恢复日志数据(直接操作数据库)
        /// </summary>
        /// <param name="tbName">表名</param>
        /// <param name="jsonUrl">文件地址</param>
        /// <param name="type">恢复类型 Ins:插入,Del:删除</param>
        /// <returns></returns>
        public ActionResult RecoveryLogData(string tbName, string jsonUrl, string type)
        {
            PageJson json = new PageJson();

            if (System.IO.File.Exists(@jsonUrl) == false) //如果存在,则读取规则文件
            {
                json.Success = false;
                json.Message = "文件不存在";
                return Json(json);
            }

            if (tbName == null || tbName.Trim() == "")
            {
                json.Success = false;
                json.Message = "表名不能为空";
                return Json(json);
            }

            if (type == null || type.Trim() == "")
            {
                json.Success = false;
                json.Message = "类型不能为空";
                return Json(json);
            }

            InvokeResult result = new InvokeResult();

            if (type == "Ins") result = this.InsertLogData(tbName, jsonUrl);
            else if (type == "Del") result = this.DeleteLogData(tbName, jsonUrl);

            json = TypeConvert.InvokeResultToPageJson(result);

            return Json(json);

        }

        /// <summary>
        /// 批量恢复日志数据
        /// </summary>
        /// <param name="dirUrl"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public string BatchRecoveryLogData(string dirUrl, string type)
        {
            if (System.IO.Directory.Exists(dirUrl) == false)
            {
                Response.Write("文件夹不存在");
                Response.Write("<br></br>");
                return "Error!";
            }

            if (type == null || type.Trim() == "")
            {
                Response.Write("类型不能为空");
                Response.Write("<br></br>");
                return "Error!";
            }

            DirectoryInfo dir = new DirectoryInfo(@dirUrl);

            foreach (var tempFile in dir.GetFiles())
            {
                string tbName = tempFile.Name.Substring(0, tempFile.Name.IndexOf('.'));
                string fileUrl = tempFile.FullName;

                Response.Write("开始处理表:" + tbName);
                Response.Write("<br></br>");

                Response.Write("所在地址:" + fileUrl);
                Response.Write("<br></br>");

                InvokeResult result = new InvokeResult();

                if (type == "Ins") result = this.InsertLogData(tbName, fileUrl);
                else if (type == "Del") result = this.DeleteLogData(tbName, fileUrl);

                if (result.Status == Status.Successful)
                {
                    Response.Write("处理完成!");
                    Response.Write("<br></br>");
                }
                else
                {
                    Response.Write("处理失败:" + result.Message);
                    Response.Write("<br></br>");
                }

                Response.Write("<hr></hr>");
            }

            return "Success!!";
        }

        /// <summary>
        /// 从系统日志中获取恢复数据的JSON
        /// </summary>
        /// <param name="behaviorId"></param>
        /// <param name="dataType">opData/oldData,默认oldData</param>
        /// <returns></returns>
        public ActionResult GetLogRecoveryFile(string behaviorId, string type, string dataType)
        {
            InvokeResult result = new InvokeResult();

            if (behaviorId != "")
            {
                List<BsonDocument> allLogList = dataOp.FindAllByQuery("SysAssoDataLog", Query.And(
                    Query.EQ("behaviorId", behaviorId),
                    Query.EQ("logType", type))
                ).ToList();

                List<string> tbNameList = allLogList.Select(t => t.String("tableName")).Distinct().ToList();

                StreamWriter sw = null;

                try
                {
                    foreach (var tempName in tbNameList)
                    {
                        List<BsonDocument> tempLogList = allLogList.Where(t => t.String("tableName") == tempName).ToList();

                        string fileUrl = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DataJson", tempName + ".json");

                        if (!System.IO.File.Exists(fileUrl))
                        {
                            sw = System.IO.File.CreateText(fileUrl);
                        }
                        else
                        {
                            sw = System.IO.File.AppendText(fileUrl);
                        }

                        foreach (var tempLog in tempLogList)
                        {
                            sw.Write(tempLog.String(string.IsNullOrEmpty(dataType) ? "oldData" : dataType));
                        }

                        sw.Flush();
                        sw.Close();
                    }
                    result.Status = Status.Successful;
                }
                catch (Exception ex)
                {
                    result.Status = Status.Failed;
                    result.Message = ex.Message;
                }
                finally
                {

                }
            }

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        /// <summary>
        /// 将用户访问系统的相关数据导出Excel文件
        /// </summary>
        /// <returns></returns>
        public void ExportUserVisitLogToExcel()
        {
            //导出表格的StringBuilder变量
            StringBuilder htmlStr = new StringBuilder(string.Empty);

            #region 获取相关展示信息

            //所有供应商信息
            List<BsonDocument> logList = dataOp.FindAllByQuery("SysBehaviorLog", Query.And(
                Query.Matches("path", "AjaxLogin"),
                Query.EQ("method", "POST"),
                Query.Matches("logTime", "2014")
                )).ToList();

            List<BsonValue> userIdList = logList.Select(t => t.GetValue("logUserId")).Distinct().ToList();    //所有相关的用户Id

            List<BsonDocument> userList = dataOp.FindAllByQuery("SysUser", Query.In("userId", userIdList)).ToList();    //所有用到的用户

            List<BsonDocument> userPostList = dataOp.FindAllByQuery("UserOrgPost", Query.In("userId", userIdList)).ToList();    //所有用户岗位关联

            List<BsonValue> postIdList = userPostList.Select(t => t.GetValue("postId")).Distinct().ToList();    //所有岗位Id

            List<BsonDocument> postList = dataOp.FindAllByQuery("OrgPost", Query.In("postId", postIdList)).ToList();        //所有岗位

            List<BsonDocument> orgList = dataOp.FindAll("Organization").ToList();                               //所有岗位部门

            #endregion

            #region 形成对应Html表格

            htmlStr.Append("<html xmlns:v=\"urn:schemas-microsoft-com:vml\" xmlns:o=\"urn:schemas-microsoft-com:office:office\" xmlns:x=\"urn:schemas-microsoft-com:office:excel\" xmlns=\"http://www.w3.org/TR/REC-html40\">");
            htmlStr.Append("<head><meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\">");
            htmlStr.Append("<!--[if gte mso 9]><xml><x:ExcelWorkbook><x:ExcelWorksheets><x:ExcelWorksheet><x:Name></x:Name><x:WorksheetOptions><x:Selected/></x:WorksheetOptions></x:ExcelWorksheet></x:ExcelWorksheets></x:ExcelWorkbook></xml><![endif]-->");
            htmlStr.Append("</head>");
            htmlStr.Append("<body>");
            htmlStr.Append("<table>");

            #region 表头
            htmlStr.Append("<thead>");

            #region 第一层
            htmlStr.Append("<tr>");
            htmlStr.Append("<th>序号</th>");
            htmlStr.Append("<th>用户</th>");
            htmlStr.Append("<th>部门1</th>");
            htmlStr.Append("<th>部门2</th>");
            htmlStr.Append("<th>部门3</th>");
            htmlStr.Append("<th>岗位</th>");
            htmlStr.Append("<th>登陆时间</th>");
            htmlStr.Append("<td>登陆IP</th>");
            htmlStr.Append("<td>浏览器</th>");
            htmlStr.Append("<td>用户名</th>");
            htmlStr.Append("<td>密码</th>");
            htmlStr.Append("</tr>");
            #endregion

            htmlStr.Append("</thead>");
            #endregion

            #region 表身
            htmlStr.Append("<tbody>");

            int index = 1;
            foreach (var tempLog in logList.OrderBy(t => t.String("timeSort")))
            {
                BsonDocument tempUser = tempLog.Int("logUserId") > 0 ? userList.Where(t => t.Int("userId") == tempLog.Int("logUserId")).FirstOrDefault() : null;      //对应用户
                BsonDocument tempPostRel = userPostList.Where(t => t.Int("userId") == tempUser.Int("userId")).FirstOrDefault(); //对应用户岗位关联
                BsonDocument tempPost = postList.Where(t => t.Int("postId") == tempPostRel.Int("postId")).FirstOrDefault();     //对应岗位
                BsonDocument tempOrg3 = orgList.Where(t => t.Int("orgId") == tempPost.Int("orgId")).FirstOrDefault();            //对应第三级部门
                BsonDocument tempOrg2 = tempOrg3.Int("nodePid") != 0 ? orgList.Where(t => t.Int("orgId") == tempOrg3.Int("nodePid")).FirstOrDefault() : null;   //对应第二级部门
                BsonDocument tempOrg1 = (tempOrg2 != null && tempOrg2.Int("nodePid") != 0) ? orgList.Where(t => t.Int("orgId") == tempOrg2.Int("nodePid")).FirstOrDefault() : null;   //对应第二级部门

                htmlStr.Append("<tr>");
                htmlStr.AppendFormat("<td>{0}</td>", index);
                htmlStr.AppendFormat("<td>{0}</td>", tempUser != null ? tempUser.String("name") : "调试管理员");
                htmlStr.AppendFormat("<td>{0}</td>", tempOrg1 != null ? tempOrg1.String("name") : "");
                htmlStr.AppendFormat("<td>{0}</td>", tempOrg2 != null ? tempOrg2.String("name") : "");
                htmlStr.AppendFormat("<td>{0}</td>", tempOrg3 != null ? tempOrg3.String("name") : "");
                htmlStr.AppendFormat("<td>{0}</td>", tempPost.String("name"));
                htmlStr.AppendFormat("<td>{0}</td>", tempLog.String("logTime"));
                htmlStr.AppendFormat("<td>{0}</td>", tempLog.String("ipAddress"));
                htmlStr.AppendFormat("<td>{0}</td>", tempLog.String("browser"));
                htmlStr.AppendFormat("<td>{0}</td>", tempLog.String("userName"));
                htmlStr.AppendFormat("<td>{0}</td>", tempLog.String("password"));

                htmlStr.Append("</tr>");
                index++;
            }

            htmlStr.Append("</tbody>");
            #endregion

            htmlStr.Append("</table>");
            htmlStr.Append("</body>");
            htmlStr.Append("</html>");
            #endregion

            //调用输出Excel表的方法
            ExportToExcel("application/ms-excel", "用户访问日志.xls", htmlStr.ToString());
        }

        #endregion


        public void ExportSSMatInfoToExcel()
        {
            //导出表格的StringBuilder变量
            StringBuilder htmlStr = new StringBuilder(string.Empty);

            #region 获取相关展示信息

            List<BsonDocument> matList = dataOp.FindAll("Material_Material").ToList();  //材料

            List<BsonDocument> baseCatList = dataOp.FindAll("Material_BaseCat").ToList();   //基类

            List<BsonDocument> categoryList = dataOp.FindAll("Material_Category").ToList(); //类目

            List<BsonDocument> brandList = dataOp.FindAll("Material_Brand").ToList();       //品牌

            List<BsonDocument> supplierList = dataOp.FindAll("Material_Supplier").ToList();    //供应商

            List<BsonDocument> allBaseCatRelList = dataOp.FindAll("XH_Material_BaseCatBrand").ToList(); //所有材料基类关联

            List<BsonDocument> allBaseCatSuList = dataOp.FindAll("XH_Material_BaseCatSupplier").ToList();

            #endregion

            XlsDocument xlsDoc = new XlsDocument();

            List<string> sheetNameList = new List<string>() { "基类模板", "品牌管理", "供应商管理" };

            foreach (var sheetName in sheetNameList)
            {
                Worksheet sheet = xlsDoc.Workbook.Worksheets.Add(sheetName);

                // 开始填充数据到单元格
                Cells cells = sheet.Cells;

                if (sheetName == "基类模板")
                {
                    #region 输出标题
                    int j = 1;

                    cells.Add(j, 1, "一级类目");
                    cells.Add(j, 2, "二级类目");
                    cells.Add(j, 3, "基类");
                    cells.Add(j, 4, "材料名称");
                    cells.Add(j, 5, "材料编号");
                    cells.Add(j, 6, "规格");
                    cells.Add(j, 7, "品牌");
                    cells.Add(j, 8, "主材供货方式");
                    cells.Add(j, 9, "供应商");
                    cells.Add(j, 10, "采购方式");
                    cells.Add(j, 11, "市场价格");
                    cells.Add(j, 12, "采购价格");
                    cells.Add(j, 13, "型号");
                    cells.Add(j, 14, "单位");
                    cells.Add(j, 15, "是否含施工费");

                    #endregion

                    #region 输出指标
                    j++;

                    foreach (var firstCat in categoryList.Where(t => t.Int("nodeLevel") == 1).OrderBy(t => t.String("nodeKey")))
                    {
                        foreach (var secondCat in categoryList.Where(t => t.Int("nodePid") == firstCat.Int("categoryId")).OrderBy(t => t.String("nodeKey")))
                        {
                            foreach (var tempBase in baseCatList.Where(t => t.Int("categoryId") == secondCat.Int("categoryId")).OrderBy(t => t.Int("order")))
                            {
                                foreach (var tempMat in matList.Where(t => t.Int("baseCatId") == tempBase.Int("baseCatId")).OrderBy(t => t.Int("order")))
                                {
                                    BsonDocument tempBrand = brandList.Where(t => t.Int("brandId") == tempMat.Int("brandId")).FirstOrDefault();
                                    BsonDocument tempSupplier = supplierList.Where(t => t.Int("supplierId") == tempMat.Int("supplierId")).FirstOrDefault();

                                    cells.Add(j, 1, firstCat.String("name"));
                                    cells.Add(j, 2, secondCat.String("name"));
                                    cells.Add(j, 3, tempBase.String("name"));
                                    cells.Add(j, 4, tempMat.String("name"));
                                    cells.Add(j, 5, tempMat.String("matNum"));
                                    cells.Add(j, 6, tempMat.String("specification"));
                                    cells.Add(j, 7, tempBrand.String("name"));
                                    cells.Add(j, 8, tempMat.String("supplierManner"));
                                    cells.Add(j, 9, tempSupplier.String("name"));
                                    cells.Add(j, 10, tempMat.String("procurementMethod"));
                                    cells.Add(j, 11, tempMat.String("marketPrice"));
                                    cells.Add(j, 12, tempMat.String("purchasePrice"));
                                    cells.Add(j, 13, tempMat.String("supplierModel"));
                                    cells.Add(j, 14, tempMat.String("costUnit"));
                                    cells.Add(j, 15, tempMat.String("ConstrCost"));

                                    j++;
                                }
                            }
                        }
                    }
                    #endregion
                }

                if (sheetName == "品牌管理")
                {
                    #region 输出标题
                    int j = 1;

                    cells.Add(j, 1, "名称");
                    cells.Add(j, 2, "所属基类");
                    cells.Add(j, 3, "生产厂商");

                    #endregion

                    #region 输出指标
                    j++;

                    foreach (var temp in brandList.OrderByDescending(c => c.String("baseCatIds")))
                    {
                        List<BsonDocument> tempRelList = allBaseCatRelList.Where(t => t.Int("brandId") == temp.Int("brandId")).ToList();   //所有关联

                        List<int> tempBaseCatIdList = tempRelList.Select(t => t.Int("baseCatId")).ToList();     //对应基类列表 

                        int i = 0;

                        foreach (var tempBaseCatId in tempBaseCatIdList.Distinct())
                        {
                            BsonDocument baseCat = baseCatList.Where(t => t.Int("baseCatId") == tempBaseCatId).FirstOrDefault();
                            BsonDocument secondCat = categoryList.Where(t => t.Int("categoryId") == baseCat.Int("categoryId")).FirstOrDefault();
                            BsonDocument firstCat = categoryList.Where(t => t.Int("categoryId") == secondCat.Int("nodePid")).FirstOrDefault();

                            if (i == 0)
                            {
                                cells.Add(j, 1, temp.String("name"));
                                if (i == 0) cells.Add(j, 3, temp.String("Province"));
                            }

                            cells.Add(j, 2, string.Format("{0}>>{1}>>{2}", firstCat.String("name"), secondCat.String("name"), baseCat.String("name")));

                            i++;
                            j++;
                        }
                    }
                    #endregion
                }

                if (sheetName == "供应商管理")
                {
                    #region 输出标题
                    int j = 1;

                    cells.Add(j, 1, "名称");
                    cells.Add(j, 2, "所属基类");
                    cells.Add(j, 3, "编号");
                    cells.Add(j, 4, "省份");
                    cells.Add(j, 5, "地址");
                    cells.Add(j, 6, "联系人");
                    cells.Add(j, 7, "电话");

                    #endregion

                    #region 输出指标
                    j++;

                    foreach (var temp in supplierList.OrderByDescending(c => c.String("baseCatIds")))
                    {
                        List<BsonDocument> tempSuList = allBaseCatSuList.Where(c => c.String("supplierId") == temp.String("supplierId")).ToList();
                        List<int> tempBaseCatIdList = tempSuList.Select(t => t.Int("baseCatId")).ToList();

                        int i = 0;

                        foreach (var tempBaseCatId in tempBaseCatIdList.Distinct())
                        {
                            BsonDocument baseCat = baseCatList.Where(t => t.Int("baseCatId") == tempBaseCatId).FirstOrDefault();
                            BsonDocument secondCat = categoryList.Where(t => t.Int("categoryId") == baseCat.Int("categoryId")).FirstOrDefault();
                            BsonDocument firstCat = categoryList.Where(t => t.Int("categoryId") == secondCat.Int("nodePid")).FirstOrDefault();

                            if (i == 0)
                            {
                                cells.Add(j, 1, temp.String("name"));
                                cells.Add(j, 3, temp.String("SupplierCode"));
                                cells.Add(j, 4, temp.String("Province"));
                                cells.Add(j, 5, temp.String("Address"));
                                cells.Add(j, 6, temp.String("LinkMan"));
                                cells.Add(j, 7, temp.String("TEL"));
                            }

                            cells.Add(j, 2, string.Format("{0}>>{1}>>{2}", firstCat.String("name"), secondCat.String("name"), baseCat.String("name")));

                            i++;
                            j++;
                        }
                    }
                    #endregion
                }


            }

            using (MemoryStream ms = new MemoryStream())
            {
                System.Web.HttpContext context = System.Web.HttpContext.Current;
                context.Response.ContentType = "application/vnd.ms-excel";
                context.Response.ContentEncoding = Encoding.UTF8;
                context.Response.Charset = "";
                context.Response.AppendHeader("Content-Disposition", "attachment;filename=" + HttpUtility.UrlEncode("材料库导出", Encoding.UTF8) + ".xls");
                xlsDoc.Save(ms);
                ms.Flush();
                ms.Position = 0;
                context.Response.BinaryWrite(ms.GetBuffer());
                context.Response.End();
            }
        }

        #region 公共私有函数
        /// <summary>
        ///  将InvokeResult转成PageJson对象
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        [NonAction]
        private PageJson ConvertToPageJson(InvokeResult result)
        {
            PageJson json = new PageJson();
            json.Success = result.Status == Status.Successful;
            json.Message = result.Message;

            return json;
        }
        /// <summary>
        ///  将InvokeResult转成PageJson对象
        /// </summary>
        /// <param name="formValueString"></param>
        /// <returns></returns>
        [NonAction]
        private Int32[] GetSelectElementIdArray(string formValueString)
        {
            var idList = new List<Int32>();

            if (!string.IsNullOrEmpty(formValueString))
            {
                var valuesArray = formValueString.Split(",".ToCharArray());
                foreach (var valueString in valuesArray)
                {
                    if (string.IsNullOrEmpty(valueString)) continue;
                    idList.Add(Int32.Parse(valueString));
                }
            }

            return idList.ToArray();
        }
        #endregion

        /// <summary>
        /// 将html表格导出成为Excel
        /// </summary>
        /// <param name="FileType"></param>
        /// <param name="FileName"></param>
        /// <param name="ExcelContent"></param>
        public void ExportToExcel(string FileType, string FileName, string ExcelContent)
        {
            System.Web.HttpContext.Current.Response.Charset = "UTF-8";
            System.Web.HttpContext.Current.Response.ContentEncoding = System.Text.Encoding.UTF8;
            System.Web.HttpContext.Current.Response.AppendHeader("Content-Disposition", "attachment;filename=" + HttpUtility.UrlEncode(FileName, System.Text.Encoding.UTF8).ToString());
            System.Web.HttpContext.Current.Response.ContentType = FileType;
            System.IO.StringWriter tw = new System.IO.StringWriter();
            System.Web.HttpContext.Current.Response.Output.Write(ExcelContent.ToString());
            System.Web.HttpContext.Current.Response.Flush();
            System.Web.HttpContext.Current.Response.End();
        }
        /// <summary>
        /// 中海投资工作函的PDF导出
        /// </summary>
        /// <param name="path"></param>
        /// <param name="name"></param>
        [NonAction]
        public void DownloadFileZHTZ(string path, string name)
        {
            System.IO.FileInfo file = new System.IO.FileInfo(path);
            name = HttpUtility.UrlEncode(name, System.Text.Encoding.UTF8).Replace("+", "%20");
            Response.Clear();
            Response.ClearHeaders();
            Response.Buffer = false;
            Response.AddHeader("Content-Disposition", "attachment;filename=" + name);
            Response.AddHeader("Content-Length", file.Length.ToString());
            Response.ContentType = "application/pdf";
            Response.WriteFile(path);
            Response.Flush();
            Response.End();
        }
    }
}
