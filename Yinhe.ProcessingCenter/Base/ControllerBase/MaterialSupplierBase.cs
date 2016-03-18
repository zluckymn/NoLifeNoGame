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
using NPOI.HSSF.UserModel;
using NPOI;
using NPOI.SS.UserModel;
using NPOI.SS;
using NPOI.HSSF;
using NPOI.POIFS;
using NPOI.Util;
using Yinhoo.Utilities.Util;
using System.IO;
using System.Net;
using System.Threading;

namespace Yinhe.ProcessingCenter
{
    /// <summary>
    /// 设计供应商后台管理基类
    /// </summary>
    public partial class ControllerBase : Controller
    {
        [HttpPost]
        public JsonResult QuickAddBrand(string baseCatId, string name)
        {
            object json = null;
            try
            {
                BsonDocument brand = new BsonDocument { { "name", name }, { "baseCatIds", baseCatId } };
                var resultId = dataOp.Insert("Material_Brand", brand).BsonInfo.String("brandId");
                dataOp.Insert("XH_Material_BaseCatBrand", new BsonDocument { { "brandId", resultId }, { "baseCatId", baseCatId } });
                json = new { success = true, brandId = resultId, name = name };
            }
            catch
            {
                json = new { success = false };
            }
            return Json(json);
        }

        [HttpPost]
        public JsonResult QuickAddSupplier(string baseCatId, string name)
        {
            object json = null;
            try
            {
                BsonDocument brand = new BsonDocument { { "name", name }, { "baseCatIds", baseCatId } };
                var resultId = dataOp.Insert("Material_Supplier", brand).BsonInfo.String("supplierId");
                dataOp.Insert("XH_Material_BaseCatSupplier", new BsonDocument { { "supplierId", resultId }, { "baseCatId", baseCatId } });
                json = new { success = true, supplierId = resultId, name = name };
            }
            catch
            {
                json = new { success = false };
            }
            return Json(json);
        }


        public JsonResult GetBaseExtendAttrInfos(string baseCatId, string matId)
        {
            string BaseCatExtendVal = "BaseCatExtendVal";
            string BaseCatExtend = "BaseCatExtend";
            var attrVals = dataOp.FindAllByQuery(BaseCatExtendVal, Query.EQ("matId", matId));//材料的扩展属性值
            var extends = dataOp.FindAllByQuery(BaseCatExtend, Query.EQ("baseId", baseCatId));//基类扩展属性
            var result = new List<BsonDocument>();
            if (extends.Any())
            {
                foreach (var item in extends)
                {
                    var temp = attrVals.SingleOrDefault(s => s.Int("extendId") == item.Int("extendId"));
                    item.TryAdd("val", temp.String("val"));
                    result.Add(item);
                }
                return Json(new { success = true, data = result.Select(s => new { id = s.String("extendId"), name = s.String("name"), val = s.String("val") }) });
            }

            return Json(new { success = false });
        }


        [HttpPost]
        public JsonResult QucikSelect(string tbName, string baseCatId, string ids)
        {
            string relTable = string.Empty;
            string key = string.Empty;

            if (tbName == "Material_Brand")
            {
                relTable = "XH_Material_BaseCatBrand";
                key = "brandId";
            }
            else if (tbName == "Material_Supplier")
            {
                key = "supplierId";
                relTable = "XH_Material_BaseCatSupplier";
            }
            List<string> tempIds = ids.SplitParam("|$|").ToList();
            object json = null;
            try
            {
                foreach (var id in tempIds)
                {
                    dataOp.Insert(relTable, new BsonDocument { { key, id }, { "baseCatId", baseCatId } });
                }
                List<BsonDocument> valueList = dataOp.FindAllByQuery(tbName, Query.In(key, TypeConvert.StringListToBsonValueList(tempIds))).ToList();
                json = new { success = true, data = valueList.Select(s => new { id = s.String(key), name = s.String("name") }) };
            }
            catch
            {
                json = new { success = false };
            }
            return Json(json);
        }



        [HttpPost]
        public JsonResult QucikSelectSearch(string tbName, string baseCatId, string kw)
        {
            string relTable = string.Empty;
            string key = string.Empty;
            if (tbName == "Material_Brand")
            {
                relTable = "XH_Material_BaseCatBrand";
                key = "brandId";
            }
            else if (tbName == "Material_Supplier")
            {
                key = "supplierId";
                relTable = "XH_Material_BaseCatSupplier";
            }
            List<BsonDocument> relList = dataOp.FindAllByQuery(relTable, Query.EQ("baseCatId", baseCatId)).ToList();//基类下已经挂载的，供应商或者品牌
            List<string> ids = relList.Select(s => s.String(key)).ToList();

            List<BsonDocument> resultList = dataOp.FindAllByQuery(tbName, Query.NotIn(key, TypeConvert.StringListToBsonValueList(ids))).ToList();
            if (!string.IsNullOrEmpty(kw))
            {
                resultList = resultList.Where(s => s.String("name").Contains(kw)).ToList();
            }
            var json = resultList.Select(s => new { id = s.String(key), name = s.String("name") }).OrderBy(s => s.name);
            return Json(json);
        }

        #region 获取材料列表详细信息
        [HttpPost]
        public JsonResult GetMaterialInfos(string baseCatId)
        {
            var mats = dataOp.FindAllByQuery("Material_Material", Query.EQ("baseCatId", baseCatId));
            List<System.Collections.Hashtable> retList = new List<System.Collections.Hashtable>();
            retList.Capacity = (int)mats.Count();
            foreach (var mat in mats.OrderBy(s => s.String("name")))
            {
                var brand = dataOp.FindOneByQuery("Material_Brand", Query.EQ("brandId", mat.String("brandId")));
                var supplier = dataOp.FindOneByQuery("Material_Supplier", Query.EQ("supplierId", mat.String("supplierId")));
                mat.TryAdd("brand", brand.String("name"));
                mat.TryAdd("supplier", supplier.String("name"));
                mat.Remove("_id");
                retList.Add(mat.ToHashtable());
            }
            return Json(retList);

        }

        [HttpPost]
        public JsonResult GetQXMaterialInfos(string baseCatId)
        {
            var mats = dataOp.FindAllByQuery("XH_Material_Material", Query.EQ("baseCatId", baseCatId));
            List<System.Collections.Hashtable> retList = new List<System.Collections.Hashtable>();
            retList.Capacity = (int)mats.Count();
            foreach (var mat in mats.OrderBy(s => s.String("name")))
            {
                var brand = dataOp.FindOneByQuery("XH_Material_Brand", Query.EQ("brandId", mat.String("brandId")));
                var supplier = dataOp.FindOneByQuery("XH_Material_Supplier", Query.EQ("supplierId", mat.String("supplierId")));
                mat.TryAdd("brand", brand.String("name"));
                mat.TryAdd("supplier", supplier.String("name"));
                mat.Remove("_id");
                retList.Add(mat.ToHashtable());
            }
            return Json(retList);

        }
        [HttpPost]
        public JsonResult GetZHTZMaterialInfos(string baseCatId)
        {
            var mats = dataOp.FindAllByQuery("XH_Material_Material", Query.EQ("baseCatId", baseCatId));


            //获取封面图
            var thumbRelList = dataOp.FindAllByQuery("FileRelation",
                    Query.And(
                        Query.EQ("tableName", "XH_Material_Material"),
                        Query.EQ("fileObjId", "33"),
                        Query.In("keyValue", mats.Select(i => i.GetValue("matId", string.Empty)))
                    )
                ).Where(i => i.Text("isCover").ToLower() == "true")
                .OrderByDescending(i => i.Date("createDate"));

            var thumbList = dataOp.FindAllByQuery("FileLibrary",
                Query.In("fileId", thumbRelList.Select(i => i.GetValue("fileId", string.Empty))));


            List<System.Collections.Hashtable> retList = new List<System.Collections.Hashtable>();
            retList.Capacity = (int)mats.Count();
            foreach (var mat in mats.OrderBy(s => s.String("name")))
            {
                var thumbRel = thumbRelList.Where(i => i.Int("keyValue") == mat.Int("matId")).FirstOrDefault();
                var thumb = thumbList.Where(i => i.Int("fileId") == thumbRel.Int("fileId")).FirstOrDefault();
                var thumbPath = string.Empty;
                if (!BsonDocumentExtension.IsNullOrEmpty(thumb))
                {
                    thumbPath = thumb.Text("thumbPicPath").Replace("_m", "_us");
                }
                var brand = dataOp.FindOneByQuery("XH_Material_Brand", Query.EQ("brandId", mat.String("brandId")));
                var supplier = dataOp.FindOneByQuery("XH_Material_Supplier", Query.EQ("supplierId", mat.String("supplierId")));
                mat.TryAdd("brand", brand.String("name"));
                mat.TryAdd("supplier", supplier.String("name"));
                mat.Set("matNum", mat.Text("supplierNumber"));//材料编号
                mat.Set("thumbPath", thumbPath);
                mat.Remove("_id");
                retList.Add(mat.ToHashtable());
            }
            return Json(retList);

        }
        #endregion

        



        public JsonResult GetBrandSupplier(string baseCatId)
        {
            string tbName = "Material_Brand";
            string relTable = "XH_Material_BaseCatBrand";
            string key = "brandId";


            List<BsonDocument> relList = dataOp.FindAllByQuery(relTable, Query.EQ("baseCatId", baseCatId)).ToList();//基类下已经挂载的，供应商或者品牌
            List<string> ids = relList.Select(s => s.String(key)).ToList();
            List<BsonDocument> resultList = dataOp.FindAllByQuery(tbName, Query.In(key, TypeConvert.StringListToBsonValueList(ids))).ToList();
            object brandJson = resultList.Select(s => new { id = s.String(key), name = s.String("name") });

            string tbName1 = "Material_Supplier";
            string key1 = "supplierId";
            string relTable1 = "XH_Material_BaseCatSupplier";

            List<BsonDocument> relList1 = dataOp.FindAllByQuery(relTable1, Query.EQ("baseCatId", baseCatId)).ToList();//基类下已经挂载的，供应商或者品牌
            List<string> ids1 = relList1.Select(s => s.String(key1)).ToList();
            List<BsonDocument> resultList1 = dataOp.FindAllByQuery(tbName1, Query.In(key1, TypeConvert.StringListToBsonValueList(ids1))).ToList();
            object supplierJson = resultList1.Select(s => new { id = s.String(key1), name = s.String("name") });

            return Json(new { brandJson = brandJson, supplierJson = supplierJson });

        }


        /// <summary>
        /// 保存材料品牌
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SaveMaterialBrand(FormCollection saveForm)
        {
            string tbName = saveForm["tbName"] != null ? saveForm["tbName"] : "";
            string queryStr = saveForm["queryStr"] != null ? saveForm["queryStr"] : "";
            string dataStr = saveForm["dataStr"] != null ? saveForm["dataStr"] : "";

            if (dataStr.Trim() == "")
            {
                foreach (var tempKey in saveForm.AllKeys)
                {
                    if (tempKey == "tbName" || tempKey == "queryStr" || tempKey.Contains("baseCatSup")) continue;

                    dataStr += string.Format("{0}={1}&", tempKey, saveForm[tempKey]);
                }
            }

            InvokeResult result = dataOp.Save(tbName, queryStr, dataStr);    //保存品牌

            if (result.Status == Status.Successful)         //保存品牌下的基类关联
            {
                List<StorageData> saveList = new List<StorageData>();

                #region 新的保存,品牌,基类两者的关联

                string baseCatIds = saveForm["baseCatIds"] != null ? saveForm["baseCatIds"] : "";   //变量: baseCatIds , 格式:baseCatId,baseCatId,baseCatId,baseCatId

                List<string> baseCatIdList = baseCatIds.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();

                List<BsonDocument> oldRelList = dataOp.FindAllByKeyVal("XH_Material_BaseCatBrand", "brandId", result.BsonInfo.String("brandId")).ToList();   //品牌基类关联

                #region 循环获取保存列表
                foreach (var baseCatId in baseCatIdList)       //循环新的基类关联
                {
                    var tempRel = oldRelList.Where(t => t.String("baseCatId") == baseCatId).FirstOrDefault();

                    if (tempRel == null)        //如果不存在,则添加
                    {
                        StorageData relData = new StorageData();

                        relData.Name = "XH_Material_BaseCatBrand";
                        relData.Document = new BsonDocument().Add("baseCatId", baseCatId.ToString())
                                                             .Add("brandId", result.BsonInfo.String("brandId"));
                        relData.Type = StorageType.Insert;

                        saveList.Add(relData);
                    }
                }
                foreach (var oldRel in oldRelList)  //循环旧的基类关联
                {
                    if (baseCatIdList.Contains(oldRel.String("baseCatId")) == false) //如果不存在,则删除
                    {
                        StorageData relData = new StorageData();

                        relData.Name = "XH_Material_BaseCatBrand";
                        relData.Type = StorageType.Delete;
                        relData.Query = Query.EQ("relId", oldRel.String("relId"));

                        saveList.Add(relData);
                    }
                }
                #endregion

                #endregion

                var tempBson = result.BsonInfo;
                result = dataOp.BatchSaveStorageData(saveList);
                result.BsonInfo = tempBson;

            }

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        /// <summary>
        /// 保存材料供应商
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SaveMaterialSupplier(FormCollection saveForm)
        {
            string tbName = saveForm["tbName"] != null ? saveForm["tbName"] : "";
            string queryStr = saveForm["queryStr"] != null ? saveForm["queryStr"] : "";
            string dataStr = saveForm["dataStr"] != null ? saveForm["dataStr"] : "";
            string customerCode = saveForm["customerCode"] ?? string.Empty;

            if (dataStr.Trim() == "")
            {
                foreach (var tempKey in saveForm.AllKeys)
                {
                    if (tempKey == "tbName" || tempKey == "queryStr" || tempKey.Contains("baseCatSup")) continue;

                    dataStr += string.Format("{0}={1}&", tempKey, saveForm[tempKey]);
                }
            }

            InvokeResult result = dataOp.Save(tbName, queryStr, dataStr);    //保存供应商

            if (result.Status == Status.Successful)         //保存供应商下的基类关联
            {
                List<StorageData> saveList = new List<StorageData>();

                string baseCatIds = saveForm["baseCatIds"] != null ? saveForm["baseCatIds"] : "";   //变量: baseCatIds , 格式:baseCatId,baseCatId,baseCatId,baseCatId

                List<string> baseCatIdList = baseCatIds.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();

                List<BsonDocument> oldRelList = dataOp.FindAllByKeyVal("XH_Material_BaseCatSupplier", "supplierId", result.BsonInfo.String("supplierId")).ToList();   //供应商基类关联

                string curSupplierId = result.BsonInfo.String("supplierId");

                #region 循环获取保存列表
                foreach (var baseCatId in baseCatIdList)       //循环新的基类关联
                {
                    var tempRel = oldRelList.Where(t => t.String("baseCatId") == baseCatId).FirstOrDefault();

                    if (tempRel == null)        //如果不存在,则添加
                    {
                        StorageData relData = new StorageData();

                        relData.Name = "XH_Material_BaseCatSupplier";
                        relData.Document = new BsonDocument().Add("baseCatId", baseCatId.ToString())
                                                             .Add("supplierId", result.BsonInfo.String("supplierId"));
                        relData.Type = StorageType.Insert;

                        saveList.Add(relData);
                    }
                }
                foreach (var oldRel in oldRelList)  //循环旧的基类关联
                {
                    if (baseCatIdList.Contains(oldRel.String("baseCatId")) == false) //如果不存在,则删除
                    {
                        StorageData relData = new StorageData();

                        relData.Name = "XH_Material_BaseCatSupplier";
                        relData.Type = StorageType.Delete;
                        relData.Query = Query.EQ("relId", oldRel.String("relId"));
                        saveList.Add(relData);
                    }
                }
                #endregion

                #region ZHTZ保存联系人
                if (customerCode == "73345DB5-DFE5-41F8-B37E-7D83335AZHTZ")//zhtz
                {
                    if (result.Status == Status.Successful)
                    {
                        result = SaveMaterialSupplierContact_ZHTZ(curSupplierId);
                    }
                }
                #endregion

                var tempBson = result.BsonInfo;
                result = dataOp.BatchSaveStorageData(saveList);
                result.BsonInfo = tempBson;

            }

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        /// <summary>
        /// 创建材料清单
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SaveMaterialList()
        {
            InvokeResult result = new InvokeResult();
            string projId = PageReq.GetForm("projId");
            string matIds = PageReq.GetForm("matIds");
            string[] matIdArray = matIds.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);

            var matList = dataOp.FindAllByKeyVal("Material_List", "projId", projId);
            int version = matList.Count() > 0 ? matList.OrderByDescending(t => t.Int("version")).FirstOrDefault().Int("version") + 1 : 1;
            BsonDocument doc = new BsonDocument();
            doc.Add("version", version.ToString());
            doc.Add("projId", projId);
            doc.Add("name", string.Format("{0}V{1}", DateTime.Now.ToString("yyyy-MM-dd"), version));
            result = dataOp.Insert("Material_List", doc);
            if (result.Status == Status.Successful)
            {
                string listId = result.BsonInfo.Text("listId");
                foreach (var item in matIdArray.Distinct())
                {
                    BsonDocument listdoc = new BsonDocument();
                    listdoc.Add("matId", item);
                    listdoc.Add("listId", listId);
                    result = dataOp.Insert("Material_ListRelation", listdoc);
                }
            }

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        /// <summary>
        ///编辑材料清单
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult EditMaterialList()
        {
            InvokeResult result = new InvokeResult();
            string projId = PageReq.GetForm("projId");
            var matIdList = PageReq.GetFormIntList("matIds").ToList();
            string listId = PageReq.GetForm("listId");

            var curList = dataOp.FindOneByQuery("Material_List", Query.EQ("listId", listId));
            if (curList.IsNullOrEmpty())
            {
                result.Status = Status.Failed;
                result.Message = "未能找到当前材料清单";
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            var oldRelList = dataOp.FindAllByQuery("Material_ListRelation", Query.EQ("listId", listId));
            matIdList = matIdList.Where(i => !oldRelList.Select(u => u.Int("matId")).Contains(i)).Distinct().ToList();
            List<StorageData> store = new List<StorageData>();
            foreach (var matId in matIdList)
            {
                StorageData data = new StorageData();
                data.Name = "Material_ListRelation";
                data.Type = StorageType.Insert;
                data.Document = new BsonDocument(){
                    {"matId",matId.ToString()},
                    {"listId",listId.ToString()},
                    {"projId",projId.ToString()}
                };
                store.Add(data);
            }
            result = dataOp.BatchSaveStorageData(store);
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        /// <summary>
        ///编辑材料清单
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult EditMaterialListZHTZ()
        {
            InvokeResult result = new InvokeResult();
            string projId = PageReq.GetForm("projId");
            var matIdList = PageReq.GetFormIntList("matIds").ToList();
            string listId = PageReq.GetForm("listId");
            var curList = dataOp.FindOneByQuery("Material_List", Query.EQ("listId", listId));
            if (BsonDocumentExtension.IsNullOrEmpty(curList))
            {
                result.Status = Status.Failed;
                result.Message = "未能找到当前材料清单";
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            var oldRelList = dataOp.FindAllByQuery("Material_ListRelation", Query.EQ("listId", listId));
            matIdList = matIdList.Where(i => !oldRelList.Select(u => u.Int("matId")).Contains(i)).ToList();

            foreach (var matId in matIdList)
            {
                BsonDocument listdoc = new BsonDocument();
                listdoc.Add("matId", matId.ToString());
                listdoc.Add("listId", listId);
                listdoc.Add("projId", projId);
                result = dataOp.Insert("Material_ListRelation", listdoc);
            }
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }


        /// <summary>
        ///删除材料清单
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult DeleteMaterialList()
        {
            InvokeResult result = new InvokeResult();
            string listId = PageReq.GetForm("listId");

            result = dataOp.Delete("Material_ListRelation", "db.Material_ListRelation.distinct('_id',{'listId':'" + listId + "'})");
            if (result.Status == Status.Successful)
            {
                result = dataOp.Delete("Material_List", "db.Material_List.distinct('_id',{'listId':'" + listId + "'})");
            }
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        /// <summary>
        ///批量移动基类保存方法
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult MoveCatListSave(FormCollection saveForm)
        {
            InvokeResult result = new InvokeResult();
            string tbName = PageReq.GetForm("tbName");
            BsonDocument dataBson = new BsonDocument();
            foreach (var tempKey in saveForm.AllKeys)
            {
                if (tempKey == "tbName" || tempKey == "queryStr" || tempKey.Contains("fileList[") || tempKey.Contains("param.")) continue;
                dataBson.Add(tempKey, PageReq.GetForm(tempKey));
            }
            string ids = PageReq.GetForm("baseIdList");  //获取需要移动的基类Id
            foreach (var id in ids.Split(','))
            {
                if (id != "")
                {
                    var queryStr = "db.XH_Material_BaseCat.distinct('_id',{'baseCatId':'" + id + "'})";
                    result = dataOp.Save(tbName, queryStr != "" ? TypeConvert.NativeQueryToQuery(queryStr) : Query.Null, dataBson);
                    if (result.Status != Status.Successful)
                    {
                        return Json(TypeConvert.InvokeResultToPageJson(result));
                    }
                }

            }
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }



        /// <summary>
        ///编辑材料清单
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult AddMaterialList()
        {
            InvokeResult result = new InvokeResult();
            string OutMatId = PageReq.GetForm("OutMatId");
            string matIds = PageReq.GetForm("matIds");
            result = dataOp.Delete("OutMaterialList", "db.OutMaterialList.distinct('_id',{'OutMatId':'" + OutMatId + "'})"); //删除所有材料
            if (result.Status == Status.Successful)
            {
                string[] matIdArray = matIds.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var item in matIdArray.Distinct())  //添加新传入的材料
                {
                    BsonDocument listdoc = new BsonDocument();
                    listdoc.Add("matId", item);
                    listdoc.Add("OutMatId", OutMatId);
                    result = dataOp.Insert("OutMaterialList", listdoc);
                }
            }
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }
        /// <summary>
        ///创建材料清单并并复制材料
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult CopyMaterialList(FormCollection saveForm)
        {
            InvokeResult result = new InvokeResult();
            string tbName = PageReq.GetForm("tbName");
            string queryStr = PageReq.GetForm("queryStr");

            int copyListId = PageReq.GetFormInt("srcId");//被复制的材料清单Id
            BsonDocument newList = new BsonDocument();
            foreach (var tempKey in saveForm.AllKeys)
            {
                if (tempKey == "tbName" || tempKey == "queryStr") continue;

                newList.Add(tempKey, PageReq.GetForm(tempKey));
            }
            result = dataOp.Save(tbName, queryStr != "" ? TypeConvert.NativeQueryToQuery(queryStr) : Query.Null, newList);
            if (result.Status == Status.Successful && queryStr == "")
            {
                newList = result.BsonInfo;
                List<BsonDocument> copyMat = dataOp.FindAllByQuery("Material_ListRelation", Query.EQ("listId", copyListId.ToString())).ToList();
                List<string> matIdList = copyMat.Select(x => x.String("matId")).ToList();

                List<StorageData> relDataSource = new List<StorageData>();
                foreach (var matId in matIdList)
                {
                    BsonDocument tempRel = new BsonDocument();
                    tempRel.Add("matId", matId);
                    tempRel.Add("listId", newList.String("listId"));
                    StorageData tempData = new StorageData();
                    tempData.Document = tempRel;
                    tempData.Name = "Material_ListRelation";
                    tempData.Type = StorageType.Insert;
                    tempData.Query = Query.And(Query.EQ("matId", matId), Query.EQ("listId", newList.String("listId")));
                    relDataSource.Add(tempData);


                }
                result.Status = dataOp.BatchSaveStorageData(relDataSource).Status;
            }
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        public JsonResult ImportMaterialList()
        {
            InvokeResult result = new InvokeResult();
            PageJson json = new PageJson();

            int srcId = PageReq.GetFormInt("srcId");//导入清单源
            int typeId = PageReq.GetFormInt("typeId");//是否保留被导入清单中原有的材料 1：保留;，0或其他不保留
            int newListId = PageReq.GetFormInt("listId");//被导入清单
            BsonDocument newlist = dataOp.FindOneByQuery("Material_List", Query.EQ("listId", newListId.ToString()));
            List<StorageData> dataSource = new List<StorageData>();
            if (newlist != null)
            {

                newlist["srcId"] = srcId.ToString();
                StorageData tempData = new StorageData();
                tempData.Document = newlist;
                tempData.Name = "Material_List";
                tempData.Type = StorageType.Update;
                tempData.Query = Query.EQ("listId", newListId.ToString());
                dataSource.Add(tempData);
            }
            else
            {
                json.Message = "被导入清单已被移除或不存在！请刷新后重试！";
                json.Success = false;
                return Json(json);
            }


            List<BsonDocument> oldListmat = dataOp.FindAllByQuery("Material_ListRelation", Query.EQ("listId", newListId.ToString())).ToList();

            if (typeId != 1 && oldListmat.Count() > 0)
            {
                StorageData tempData = new StorageData();
                tempData.Name = "Material_ListRelation";
                tempData.Type = StorageType.Delete;
                tempData.Query = Query.EQ("listId", newListId.ToString());
                dataSource.Add(tempData);
            }


            List<BsonDocument> newListmat = dataOp.FindAllByQuery("Material_ListRelation", Query.EQ("listId", srcId.ToString())).ToList();
            foreach (var matrel in newListmat)
            {
                BsonDocument tempRel = new BsonDocument();
                tempRel.Add("matId", matrel.String("matId"));
                tempRel.Add("listId", newListId.ToString());
                StorageData tempData = new StorageData();
                tempData.Document = tempRel;
                tempData.Name = "Material_ListRelation";
                tempData.Type = StorageType.Insert;
                tempData.Query = Query.And(Query.EQ("matId", matrel.String("matId")), Query.EQ("listId", newListId.ToString()));
                dataSource.Add(tempData);
            }
            result = dataOp.BatchSaveStorageData(dataSource);
            if (result.Status == Status.Successful)
            {
                json = TypeConvert.InvokeResultToPageJson(result);
                newListmat = dataOp.FindAllByQuery("Material_ListRelation", Query.EQ("listId", newListId.ToString())).ToList();
                string matStr = string.Join(",", newListmat.Select(x => x.String("matId")));
                json.htInfo = new System.Collections.Hashtable();
                json.htInfo.Add("matStr", matStr);

            }
            else
            {
                json = TypeConvert.InvokeResultToPageJson(result);
            }
            return Json(json);
        }

        /// <summary>
        /// 材料清单导出
        /// </summary>
        /// <param name="projId">项目Id</param> 
        /// <param name="type">项目类型 1为项目库，2为外部材料库</param>
        public void ExportMatItemsNPOI()
        {

            var catList = dataOp.FindAll("XH_Material_Category").ToList(); //查找所有类目，包括一级和二级
            var baseCatList = dataOp.FindAll("XH_Material_BaseCat").ToList(); //查找所有基类
            int projId = PageReq.GetParamInt("projId");  //获取项目Id
            int type = PageReq.GetParamInt("type");
            var versionList = dataOp.FindAllByQuery("Material_List", Query.EQ("projId", projId.ToString())).ToList(); //版本列表
            List<string> ListIds = versionList.Select(c => c.String("listId")).ToList(); // 列表Id
            List<BsonDocument> projMatList = dataOp.FindAllByKeyValList("Material_ListRelation", "listId", ListIds).ToList();
            List<string> projmatList = projMatList.Select(c => c.String("matId")).Distinct().ToList(); //项目下的材料
            string projName = "";
            if (type == 1)
            {
                var projObj = dataOp.FindOneByQuery("XH_DesignManage_Project", Query.EQ("projId", projId.ToString()));
                projName = projObj.Text("name");
            }
            else
            {
                projMatList = dataOp.FindAllByQuery("OutMaterialList", Query.EQ("OutMatId", projId.ToString())).ToList();
                projmatList = projMatList.Select(c => c.String("matId")).Distinct().ToList(); //项目下的材料
                var outProjObj = dataOp.FindOneByQuery("OutMaterial", Query.EQ("OutMatId", projId.ToString()));
                projName = outProjObj.Text("name");
            }

            //建立Excel文档
            IWorkbook hssfworkbook = new HSSFWorkbook();  //创建Workbook对象
            NPOI.SS.UserModel.ISheet sheet = hssfworkbook.CreateSheet("sheet1");//创建工作表


            int rowIndex = 0;


            //创建单元格式
            ICellStyle style = hssfworkbook.CreateCellStyle();

            style.Alignment = HorizontalAlignment.CENTER;  //居中
            IFont font = hssfworkbook.CreateFont();  //创建字体
            style.VerticalAlignment = VerticalAlignment.CENTER;
            font.Boldweight = (short)FontBoldWeight.BOLD;
            style.SetFont(font);  //设置字体
            ICellStyle style1 = hssfworkbook.CreateCellStyle();
            style1.Alignment = HorizontalAlignment.CENTER;
            style1.VerticalAlignment = VerticalAlignment.CENTER;
            style1.WrapText = true;


            //创建表格题头
            NPOI.SS.UserModel.IRow row = sheet.CreateRow(rowIndex);   //创建第一行
            ICell cell = row.CreateCell(0);  //创建第一行第一列
            cell.SetCellValue(projName + "-材料清单");
            cell.CellStyle = style;
            //标题行的合并
            sheet.AddMergedRegion(new NPOI.SS.Util.CellRangeAddress(0, 0, 0, 30)); //开始行号，结束行号，开始列号，结束列号。
            rowIndex++;  //rowIndex=1  (第二行)
            //合并二级表头
            NPOI.SS.Util.CellRangeAddress ranA = new NPOI.SS.Util.CellRangeAddress(rowIndex, rowIndex, 0, 1); //类目
            NPOI.SS.Util.CellRangeAddress ranB = new NPOI.SS.Util.CellRangeAddress(rowIndex, rowIndex, 2, 4); //基类
            NPOI.SS.Util.CellRangeAddress ranC = new NPOI.SS.Util.CellRangeAddress(rowIndex, rowIndex, 5, 7); //材料名
            NPOI.SS.Util.CellRangeAddress ranD = new NPOI.SS.Util.CellRangeAddress(rowIndex, rowIndex, 8, 9); //图片
            //价格 1个单元格
            NPOI.SS.Util.CellRangeAddress ranE = new NPOI.SS.Util.CellRangeAddress(rowIndex, rowIndex, 11, 15); //材料样板名称
            NPOI.SS.Util.CellRangeAddress ranF = new NPOI.SS.Util.CellRangeAddress(rowIndex, rowIndex, 16, 20); //材料使用位置
            NPOI.SS.Util.CellRangeAddress ranG = new NPOI.SS.Util.CellRangeAddress(rowIndex, rowIndex, 21, 23); //设计编号
            NPOI.SS.Util.CellRangeAddress ranJ = new NPOI.SS.Util.CellRangeAddress(rowIndex, rowIndex, 24, 27); //品牌
            NPOI.SS.Util.CellRangeAddress ranK = new NPOI.SS.Util.CellRangeAddress(rowIndex, rowIndex, 28, 33); //供应商名
            NPOI.SS.Util.CellRangeAddress ranL = new NPOI.SS.Util.CellRangeAddress(rowIndex, rowIndex, 34, 42); //备注

            sheet.AddMergedRegion(ranA);
            sheet.AddMergedRegion(ranB);
            sheet.AddMergedRegion(ranC);
            sheet.AddMergedRegion(ranD);
            sheet.AddMergedRegion(ranE);
            sheet.AddMergedRegion(ranF);
            sheet.AddMergedRegion(ranG);
            sheet.AddMergedRegion(ranJ);
            sheet.AddMergedRegion(ranK);
            sheet.AddMergedRegion(ranL);

            //二级表头
            row = sheet.CreateRow(1);
            cell = row.CreateCell(0);
            cell.SetCellValue("材料类目");
            cell.CellStyle = style;

            cell = row.CreateCell(2);
            cell.SetCellValue("材料基类");
            cell.CellStyle = style;

            cell = row.CreateCell(5);
            cell.SetCellValue("材料名称");
            cell.CellStyle = style;

            cell = row.CreateCell(8);
            cell.SetCellValue("图片");
            cell.CellStyle = style;

            cell = row.CreateCell(10);
            cell.SetCellValue("价格");
            cell.CellStyle = style;

            cell = row.CreateCell(11);
            cell.SetCellValue("材料样板名称");
            cell.CellStyle = style;

            cell = row.CreateCell(16);
            cell.SetCellValue("材料使用位置");
            cell.CellStyle = style;

            cell = row.CreateCell(21);
            cell.SetCellValue("设计编号");
            cell.CellStyle = style;

            cell = row.CreateCell(24);
            cell.SetCellValue("品牌");
            cell.CellStyle = style;

            cell = row.CreateCell(28);
            cell.SetCellValue("供应商名");
            cell.CellStyle = style;

            cell = row.CreateCell(34);
            cell.SetCellValue("备注");
            cell.CellStyle = style;


            int catMinRow = rowIndex + 1;   //catMinRow=2  第三行
            int baseMinrow = catMinRow;     //基类展示从Excel第三行开始  Nopi中是2
            foreach (var catItem in catList.Where(c => c.Text("nodeLevel") == "1")) //循环一级类目
            {
                int countOfCat = 0; //统计当前类目下有多少行


                foreach (var catS in catList.Where(c => c.Text("nodeLevel") == "2" && c.Text("nodePid") == catItem.Text("categoryId"))) //循环二级类目
                {

                    var baseCatListNew = baseCatList.Where(c => c.Text("categoryId") == catS.Text("categoryId"));
                    foreach (var item in baseCatListNew) //基类（统计基类下的材料）
                    {
                        int tempCount = 0;
                        var matList = dataOp.FindAllByQuery("XH_Material_Material", Query.EQ("baseCatId", item.Text("baseCatId"))).Where(c => projmatList.Contains(c.String("matId")));
                        tempCount += matList.Count();
                        if (tempCount == 0)  //基类下无材料，也要占一行
                        {
                            tempCount = 1;
                        }
                        countOfCat += tempCount;  //统计当前类目下有多少行
                    }

                    NPOI.SS.Util.CellRangeAddress cellRangeAddress = new NPOI.SS.Util.CellRangeAddress(catMinRow, catMinRow + countOfCat - 1, 0, 1); //合并单元格(类目的)
                    sheet.AddMergedRegion(cellRangeAddress);

                    row = sheet.CreateRow(catMinRow);
                    row.HeightInPoints = 80;
                    cell = row.CreateCell(0);
                    cell.SetCellValue(catItem.Text("name"));//填写材料类型名称
                    cell.CellStyle = style;
                    int tempCatMinRow = catMinRow;  //当前的展示行
                    catMinRow += countOfCat;//设置最小行值 （下次循环的最小行）
                    foreach (var baseCatItem in baseCatListNew)
                    {


                        var matList = dataOp.FindAllByQuery("XH_Material_Material", Query.EQ("baseCatId", baseCatItem.Text("baseCatId"))).Where(c => projmatList.Contains(c.String("matId")));
                        int countOfBase = matList.Count(); //基类下的材料数
                        bool token = true;
                        if (countOfBase == 0)
                        {
                            countOfBase = 1;    //基类下无材料，也要占一行
                            token = false;
                        }
                        NPOI.SS.Util.CellRangeAddress cellRangeAddress1 = new NPOI.SS.Util.CellRangeAddress(baseMinrow, baseMinrow + countOfBase - 1, 2, 4); //合并单元格，基类的
                        sheet.AddMergedRegion(cellRangeAddress1);
                        if (tempCatMinRow != baseMinrow) //创建基类展示行
                        {
                            row = sheet.CreateRow(baseMinrow);
                            row.HeightInPoints = 80;  //设置行高
                        }

                        cell = row.CreateCell(2); //（类目占了2列）创建当前行的第三列，此列为材料名。   Execl是从1开始，Nopi是从0开始，要注意换算 
                        cell.SetCellValue(baseCatItem.Text("name"));
                        cell.CellStyle = style;


                        if (countOfBase > 0 && token) //基类下有材料的情况
                        {
                            foreach (var matItem in matList)
                            {


                                //合并单元格
                                rowIndex++;  //第一次循环的值为2 为Execl中的第三行
                                ranA = new NPOI.SS.Util.CellRangeAddress(rowIndex, rowIndex, 0, 1); //类目
                                ranB = new NPOI.SS.Util.CellRangeAddress(rowIndex, rowIndex, 2, 4); //基类
                                ranC = new NPOI.SS.Util.CellRangeAddress(rowIndex, rowIndex, 5, 7); //材料名
                                ranD = new NPOI.SS.Util.CellRangeAddress(rowIndex, rowIndex, 8, 9); //图片
                                //价格 1个单元格
                                ranE = new NPOI.SS.Util.CellRangeAddress(rowIndex, rowIndex, 11, 15); //材料样板名称
                                ranF = new NPOI.SS.Util.CellRangeAddress(rowIndex, rowIndex, 16, 20); //材料使用位置
                                ranG = new NPOI.SS.Util.CellRangeAddress(rowIndex, rowIndex, 21, 23); //设计编号
                                ranJ = new NPOI.SS.Util.CellRangeAddress(rowIndex, rowIndex, 24, 27); //品牌
                                ranK = new NPOI.SS.Util.CellRangeAddress(rowIndex, rowIndex, 28, 33); //供应商名
                                ranL = new NPOI.SS.Util.CellRangeAddress(rowIndex, rowIndex, 34, 42); //备注


                                sheet.AddMergedRegion(ranA);
                                sheet.AddMergedRegion(ranB);
                                sheet.AddMergedRegion(ranC);
                                sheet.AddMergedRegion(ranD);
                                sheet.AddMergedRegion(ranE);
                                sheet.AddMergedRegion(ranF);
                                sheet.AddMergedRegion(ranG);
                                sheet.AddMergedRegion(ranJ);
                                sheet.AddMergedRegion(ranK);
                                sheet.AddMergedRegion(ranL);

                                int matCols = 5; //展示材料名的列，从第六列开始
                                if (tempCatMinRow != rowIndex && baseMinrow != rowIndex)//创建新行
                                {
                                    row = sheet.CreateRow(rowIndex);
                                    row.HeightInPoints = 80;
                                }

                                cell = row.CreateCell(matCols); //创建第六列的单元格，展示材料名
                                cell.SetCellValue(matItem.Text("name"));
                                cell.CellStyle = style1;
                                matCols = matCols + 3;   //创建第九列的单元格，展示材料图片
                                List<BsonDocument> fileList = dataOp.FindAllByQueryStr("FileRelation", "tableName=XH_Material_Material&fileObjId=33&keyValue=" + matItem.String("matId").ToString()).OrderByDescending(t => DateTime.Parse(t.Text("createDate"))).ToList();//查找材料下的图文件
                                var relModel = fileList.Where(t => t.Text("isCover").ToLower() == "true").FirstOrDefault();  //查找封面图
                                string path = "";
                                if (relModel != null)
                                {
                                    var fileModel = dataOp.FindOneByKeyVal("FileLibrary", "fileId", relModel.Text("fileId"));
                                    path = fileModel.Text("thumbPicPath").Replace("_m", "_us");
                                }
                                if (path == "")
                                {
                                    path = SysAppConfig.HostDomain + "/Content/images/Docutype/default_hss.png";
                                }
                                var path1 = Server.MapPath("/Content/images/Docutype/default_hss.png");
                                byte[] bytes = System.IO.File.ReadAllBytes(path1);
                                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(path);
                                WebResponse response = request.GetResponse();
                                using (Stream stream = response.GetResponseStream())
                                {
                                    using (MemoryStream ms = new MemoryStream())
                                    {
                                        Byte[] buffer = new Byte[1024 * 10];
                                        int current = 0;
                                        while ((current = stream.Read(buffer, 0, buffer.Length)) != 0)
                                        {
                                            ms.Write(buffer, 0, current);
                                        }
                                        bytes = ms.ToArray();
                                    }
                                }
                                int pictureIdx = hssfworkbook.AddPicture(bytes, PictureType.PNG);
                                HSSFPatriarch patriarch = (HSSFPatriarch)sheet.CreateDrawingPatriarch();
                                HSSFClientAnchor anchor = new HSSFClientAnchor(0, 0, 0, 0, matCols, rowIndex, matCols + 2, rowIndex + 1); //参数说明：第一个单元格的x,y坐标，第二个单元格的x,y坐标，图片左上角所在单元格的列行，图片右下角所在单元格的列行
                                IPicture pict = patriarch.CreatePicture(anchor, pictureIdx);



                                matCols = matCols + 2; //第十一列的单元格展示材料价格  matCols=10
                                cell = row.CreateCell(matCols);
                                cell.SetCellValue(matItem.Text("costPrice"));
                                cell.CellStyle = style1;

                                matCols = matCols + 1;   //第十二列的单元格展示材料样板名称  11
                                cell = row.CreateCell(matCols);
                                cell.SetCellValue(matItem.Text("ModelName"));
                                cell.CellStyle = style1;

                                matCols = matCols + 5;   //第十七列的单元格展示材料使用位置 16
                                cell = row.CreateCell(matCols);
                                cell.SetCellValue(matItem.Text("UsePart"));
                                cell.CellStyle = style1;

                                matCols = matCols + 5;  //第二十二列的单元格展示设计编号  21
                                cell = row.CreateCell(matCols);
                                cell.SetCellValue(matItem.Text("designCode"));
                                cell.CellStyle = style1;

                                matCols = matCols + 3;  //第二十五列的单元格展示品牌  24
                                cell = row.CreateCell(matCols);
                                cell.SetCellValue(matItem.SourceBson("brandId").String("name"));
                                cell.CellStyle = style1;

                                matCols = matCols + 4;  //第二十九列的单元格展示品牌  28
                                cell = row.CreateCell(matCols);
                                cell.SetCellValue(matItem.SourceBson("supplierId").String("name"));
                                cell.CellStyle = style1;

                                matCols = matCols + 6;  //第二十九列的单元格展示品牌  34
                                cell = row.CreateCell(matCols);
                                cell.SetCellValue(matItem.String("MaterDesc").Replace("\r", "<br />"));
                                cell.CellStyle = style1;

                            }
                        }
                        else
                        {
                            //如果这个基类无材料
                            rowIndex++;
                            ranA = new NPOI.SS.Util.CellRangeAddress(rowIndex, rowIndex, 0, 1); //类目
                            ranB = new NPOI.SS.Util.CellRangeAddress(rowIndex, rowIndex, 2, 4); //基类
                            ranC = new NPOI.SS.Util.CellRangeAddress(rowIndex, rowIndex, 5, 7); //材料名
                            ranD = new NPOI.SS.Util.CellRangeAddress(rowIndex, rowIndex, 8, 9); //图片
                            //价格 1个单元格
                            ranE = new NPOI.SS.Util.CellRangeAddress(rowIndex, rowIndex, 11, 15); //材料样板名称
                            ranF = new NPOI.SS.Util.CellRangeAddress(rowIndex, rowIndex, 16, 20); //材料使用位置
                            ranG = new NPOI.SS.Util.CellRangeAddress(rowIndex, rowIndex, 21, 23); //设计编号
                            ranJ = new NPOI.SS.Util.CellRangeAddress(rowIndex, rowIndex, 24, 27); //品牌
                            ranK = new NPOI.SS.Util.CellRangeAddress(rowIndex, rowIndex, 28, 33); //供应商名
                            ranL = new NPOI.SS.Util.CellRangeAddress(rowIndex, rowIndex, 34, 42); //备注


                            sheet.AddMergedRegion(ranA);
                            sheet.AddMergedRegion(ranB);
                            sheet.AddMergedRegion(ranC);
                            sheet.AddMergedRegion(ranD);
                            sheet.AddMergedRegion(ranE);
                            sheet.AddMergedRegion(ranF);
                            sheet.AddMergedRegion(ranG);
                            sheet.AddMergedRegion(ranJ);
                            sheet.AddMergedRegion(ranK);
                            sheet.AddMergedRegion(ranL);

                            int matCols = 5; //展示材料名的列，从第六列开始
                            if (tempCatMinRow != rowIndex && baseMinrow != rowIndex)//创建新行
                            {
                                row = sheet.CreateRow(rowIndex);
                                //row.HeightInPoints = 80;
                            }

                            cell = row.CreateCell(matCols); //创建第六列的单元格，展示材料名
                            cell.SetCellValue("");

                            matCols = matCols + 3;   //创建第九列的单元格，展示材料图片
                            cell = row.CreateCell(matCols);
                            cell.SetCellValue("");



                            matCols = matCols + 2; //第十一列的单元格展示材料价格  matCols=10
                            cell = row.CreateCell(matCols);
                            cell.SetCellValue("");

                            matCols = matCols + 1;   //第十二列的单元格展示材料样板名称  11
                            cell = row.CreateCell(matCols);
                            cell.SetCellValue("");

                            matCols = matCols + 5;   //第十七列的单元格展示材料使用位置 16
                            cell = row.CreateCell(matCols);
                            cell.SetCellValue("");

                            matCols = matCols + 5;  //第二十二列的单元格展示设计编号  21
                            cell = row.CreateCell(matCols);
                            cell.SetCellValue("");

                            matCols = matCols + 3;  //第二十五列的单元格展示品牌  24
                            cell = row.CreateCell(matCols);
                            cell.SetCellValue("");

                            matCols = matCols + 4;  //第二十九列的单元格展示品牌  28
                            cell = row.CreateCell(matCols);
                            cell.SetCellValue("");

                            matCols = matCols + 6;  //第二十九列的单元格展示品牌  34
                            cell = row.CreateCell(matCols);
                            cell.SetCellValue("");
                        }
                        baseMinrow += countOfBase;//基类合并最小值
                    }

                }



            }
            MyXlsUtility.ExportByWebNPOI(hssfworkbook, projName + "-材料清单");

        }
        /// <summary>
        /// 判断重名
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult CheckNameSave()
        {
            PageJson json = new PageJson();
            string tbName = PageReq.GetForm("tbName");
            string name = PageReq.GetForm("name");
            string id = PageReq.GetForm("id");
            TableRule tableRule = new TableRule(tbName);
            var primaryKey = tableRule.PrimaryKey;
            BsonDocument entity = new BsonDocument();
            entity = dataOp.FindOneByQuery(tbName, Query.EQ(primaryKey, id));
            List<BsonDocument> tempData = dataOp.FindAllByQuery(tbName, Query.EQ("name", name)).ToList();
            if (tempData.Count() == 0)
            {
                json.Success = false;
                return Json(json);
            }
            else
            {
                if (entity != null)
                {
                    if (tempData.Where(x => x.String(primaryKey) != entity.String(primaryKey)).ToList().Count > 0)
                    {
                        json.Message = "存在同名，请重新命名";
                        json.Success = true;
                        return Json(json);
                    }
                }
                else
                {
                    if (tempData.Count > 0)
                    {
                        json.Message = "存在同名，请重新命名";
                        json.Success = true;
                        return Json(json);
                    }
                }
            }
            json.Success = false;
            return Json(json);

        }

        #region ZHTZ保存材料供应商联系人
        public InvokeResult SaveMaterialSupplierContact_ZHTZ(string supplierId)
        {
            var contacts = PageReq.GetForm("contacts");
            //contacts形式如下
            //contactId_1,name_1,tel_1,address_1;contactId_2,name_2,tel_2,address_2;.....
            string tbName = "XH_Material_SupplierContact";
            List<StorageData> saveList = new List<StorageData>();
            List<string> contactList = contacts.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            var oldContactList = dataOp.FindAllByQuery(tbName, Query.EQ("supplierId", supplierId)).ToList();
            List<string> contactIds = new List<string>();
            foreach (var contact in contactList)
            {
                List<string> items = contact.Split(new string[] { "," }, StringSplitOptions.None).ToList();
                int count = items.Count();
                string contactId = string.Empty;
                string name = string.Empty;
                string tel = string.Empty;
                string address = string.Empty;
                if (count > 0) contactId = HttpUtility.UrlDecode(items.ElementAt(0));
                if (count > 1) name = HttpUtility.UrlDecode(items.ElementAt(1));
                if (count > 2) tel = HttpUtility.UrlDecode(items.ElementAt(2));
                if (count > 3) address = HttpUtility.UrlDecode(items.ElementAt(3));
                BsonDocument doc = new BsonDocument();
                doc.Add("name", name).Add("tel", tel).Add("address", address).Add("supplierId", supplierId);
                StorageData data = new StorageData();
                data.Name = tbName;
                data.Document = doc;

                if (string.IsNullOrEmpty(contactId))
                {
                    data.Type = StorageType.Insert;

                }
                else
                {
                    var curContact = dataOp.FindOneByQuery(tbName, Query.EQ("contactId", contactId));
                    if (curContact != null)
                    {
                        contactIds.Add(contactId);
                        data.Query = Query.EQ("contactId", curContact.Text("contactId"));
                        data.Type = StorageType.Update;
                    }
                }
                saveList.Add(data);
            }
            foreach (var oldRel in oldContactList)
            {
                if (!contactIds.Contains(oldRel.Text("contactId")))
                {
                    StorageData tempData = new StorageData();

                    tempData.Name = tbName;
                    tempData.Query = Query.EQ("contactId", oldRel.Text("contactId"));
                    tempData.Type = StorageType.Delete;

                    saveList.Add(tempData);
                }
            }
            InvokeResult result = dataOp.BatchSaveStorageData(saveList);
            if (result.Status == Status.Failed)
            {

            }
            return result;
        }
        #endregion

        #region ZHTZ移动二级类目
        public JsonResult CategoryMove()
        {
            InvokeResult result = new InvokeResult() { Status = Status.Successful };
            var catId = PageReq.GetParam("catId");
            var curOrder = PageReq.GetParam("curOrder");
            var type = PageReq.GetParamInt("type");//type  0:上移  1:下移
            var catObj = dataOp.FindOneByQuery("XH_Material_Category", Query.EQ("categoryId", catId));
            if (catObj == null)
            {
                result.Status = Status.Failed;
                result.Message = "未找到当前类目";
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            var cat_lv2 = new BsonDocument();
            if (type == 0)
            {
                cat_lv2 = dataOp.FindAllByQuery("XH_Material_Category",
                           Query.And(
                               Query.EQ("nodePid", catObj.Text("nodePid"))
                           )
                   ).Where(p => p.Int("nodeOrder") < catObj.Int("nodeOrder"))
                   .OrderByDescending(p => p.Int("nodeOrder"))
                   .FirstOrDefault();
            }
            else if (type == 1)
            {
                cat_lv2 = dataOp.FindAllByQuery("XH_Material_Category",
                            Query.And(
                                Query.EQ("nodePid", catObj.Text("nodePid"))
                            )
                    ).Where(p => p.Int("nodeOrder") > catObj.Int("nodeOrder"))
                    .OrderBy(p => p.Int("nodeOrder"))
                    .FirstOrDefault();
            }
            if (cat_lv2 != null)
            {
                BsonDocument doc1 = new BsonDocument().Add("nodeOrder", catObj.Text("nodeOrder"));
                BsonDocument doc2 = new BsonDocument().Add("nodeOrder", cat_lv2.Text("nodeOrder"));
                result = dataOp.Update("XH_Material_Category", Query.EQ("categoryId", cat_lv2.Text("categoryId")), doc1);

                if (result.Status == Status.Successful)
                {
                    result = dataOp.Update("XH_Material_Category", Query.EQ("categoryId", catObj.Text("categoryId")), doc2);
                }
            }


            return Json(TypeConvert.InvokeResultToPageJson(result));
        }
        #endregion


        /***********************袁辉2013/8/27*******************************/
        #region 材料编号自动生成
        /// <summary>
        /// 接收字符串，输出其中的中文首字母，用于材料编号自动生成
        /// </summary>
        /// <param name="cnStr"></param>
        /// <returns></returns>
        public string GetCapWords(string cnStr)
        {
            string[] inputStrs = cnStr.Split('-');
            string retStr = "";
            foreach (var temp in inputStrs)
            {
                for (int i = 0; i < temp.Length; i++)
                {
                    retStr += PinyinHelper.GetPinyin(temp.Substring(i, 1)).Substring(0, 1).ToUpper();
                }
                retStr += "-";
            }
            return retStr;
        }
        #endregion
        /// <summary>
        /// 检查是否有相同的材料类型
        /// </summary>
        /// <param name="checkStr">材料编号</param>
        /// <param name="matId">材料Id</param>
        /// <returns></returns>
        public int CheckSupplierModel(string checkStr, string matId)
        {
            var temp = dataOp.FindOneByQuery("XH_Material_Material", Query.And(
                Query.EQ("supplierNumber", checkStr),
                Query.NE("matId", matId)
                ));
            if (temp != null)
                return -1;
            else
                return 0;
        }
        ///<summary>
        ///查找供应商信息
        ///</summary>
        ///<param name="supplierId">供应商Id</param>
        ///<returns></returns>
        public string GetSupplierInfo(string supplierId)
        {
            var supplier = dataOp.FindOneByKeyVal("XH_Material_Supplier", "supplierId", supplierId);

            if (supplier == null)
                return "-1";

            var result = supplier.String("contacts") + "," + supplier.String("TEL") + "," + supplier.String("Address");

            return result;
        }

        public ActionResult saveQRImagePath(int matId)
        {
            InvokeResult result = new InvokeResult();
            var infoStr = SysAppConfig.HostDomain + "/Material/MaterialShow?matId=" + matId;
            string info = Server.UrlDecode(infoStr);
            List<BsonDocument> fileList = dataOp.FindAllByQueryStr("FileRelation", "tableName=XH_Material_Material&fileObjId=33&keyValue=" + matId.ToString()).OrderByDescending(t => DateTime.Parse(t.Text("createDate"))).ToList();//

            var relModel = fileList.Where(t => t.Text("isCover").ToLower() == "true").FirstOrDefault();
            string path = "";
            BsonDocument fileModel = new BsonDocument();
            if (relModel != null)
            {
                fileModel = dataOp.FindOneByKeyVal("FileLibrary", "fileId", relModel.Text("fileId"));
                path = fileModel.Text("thumbPicPath").Replace("_m", "_l");
            }
            string midImgPath = Server.UrlDecode(path);
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string saveDir = System.IO.Path.Combine(baseDir, @"Content\QRTempImage");
            string fileName = DateTime.Now.Ticks.ToString();
            string QRImgPath = "";
            string relPath = QREncodeHelper.CreateQRCodePic(info, midImgPath, saveDir, fileName, 200, 200);
            if (string.IsNullOrEmpty(relPath))
                QRImgPath = "/Content/images/Docutype/bmp_hm.png";
            else
                QRImgPath = relPath.Replace(baseDir, "");
            QRImgPath = "/" + Regex.Replace(QRImgPath, @"\\", "/");

            dataOp.Update("XH_Material_Material", Query.EQ("matId", matId.ToString()), new BsonDocument().Add("QRImgPath", QRImgPath));

            result.Status = Status.Successful;
            result.Message = "保存成功";
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }


        ///<summary>
        ///QX改变材料第一级类目的顺序，此处只考虑的上下移动一级情况
        ///</summary>
        ///<param name="moveId">要移动的id</param>
        ///<param name="type">移动的类型，pre：上移；next：下移；</param>
        ///<returns></returns>
        public ActionResult MatFirstCatMoveOrder(string moveId, string type)
        {
            var result = new InvokeResult();
            var moveObj = dataOp.FindOneByKeyVal("XH_Material_Category", "categoryId", moveId);
            if (string.IsNullOrEmpty(moveId) || string.IsNullOrEmpty(type)
                || moveId == "0" || (type != "pre" && type != "next") || moveObj == null)
            {
                result.Message = "参数错误，请重试或者联系管理员";
                result.Status = Status.Failed;
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }

            var moveToObj = new BsonDocument();
            if (type == "pre")
                moveToObj = dataOp.FindOneByQuery("XH_Material_Category", Query.EQ("order", (moveObj.Int("order") - 1).ToString()));
            else
                moveToObj = dataOp.FindOneByQuery("XH_Material_Category", Query.EQ("order", (moveObj.Int("order") + 1).ToString()));
            var storageList = new List<StorageData>();
            var moveData = new StorageData();

            moveData.Name = "XH_Material_Category";
            moveData.Query = Query.EQ("categoryId", moveObj.String("categoryId"));
            moveData.Document = new BsonDocument().Add("order", moveToObj.String("order"));
            moveData.Type = StorageType.Update;

            storageList.Add(moveData);

            var moveToData = new StorageData();
            moveToData.Name = "XH_Material_Category";
            moveToData.Query = Query.EQ("categoryId", moveToObj.String("categoryId"));
            moveToData.Document = new BsonDocument().Add("order", moveObj.String("order"));
            moveToData.Type = StorageType.Update;

            storageList.Add(moveToData);

            result = dataOp.BatchSaveStorageData(storageList);

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        /// <summary>
        /// 新城保存材料
        /// </summary>
        /// <param name="saveForm">上传表单</param>
        /// <param name="prices">价格列表</param>
        ///  <param name="retIds">关联的工艺工法</param>
        ///  <param name="projIds">关联的项目</param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        [ValidateInput(false)]
        public ActionResult SaveMaterialXC(FormCollection saveForm, string prices, string extendAttrs, string retIds,string projIds)
        {
            lock (objPad)
            {
                InvokeResult result = new InvokeResult();

                string tbName = PageReq.GetForm("tbName");
                string queryStr = PageReq.GetForm("queryStr");
                string dataStr = PageReq.GetForm("dataStr");
                string matProjRelStr = PageReq.GetForm("matProjRel");
                int serialSave = PageReq.GetParamInt("serialSave");//判断是否是产品系列新增材料 1为产品系列新增
                bool isFromProject = PageReq.GetParamBoolean("isFromProject");//判断是否是从项目库新增材料
                bool isFromStdResult = PageReq.GetFormBoolean("isFromStdResult");//判断是否是从专业库新增材料
                BsonDocument dataBson = new BsonDocument();

                var filterKeys = new string[] { "tbName", "queryStr", "isFromStdResult" };

                if (dataStr.Trim() == "")
                {
                    foreach (var tempKey in saveForm.AllKeys.Where(i=>!filterKeys.Contains(i)))
                    {
                        if (tempKey == "tbName" || tempKey == "queryStr" || tempKey.Contains("fileList[") || tempKey.Contains("param.") || tempKey=="seriesIds") continue;

                        dataBson.Add(tempKey, PageReq.GetForm(tempKey));
                    }
                }
                else
                {
                    dataBson = TypeConvert.ParamStrToBsonDocument(dataStr);
                }


                var query = TypeConvert.NativeQueryToQuery(queryStr);
                BsonDocument old = dataOp.FindOneByQuery(tbName, query);//更新之前的材料信息
                MaterialBll materialBll = MaterialBll._();
                string code = string.Empty;
                if (CustomerCode.LF == SysAppConfig.CustomerCode)
                {
                    code = materialBll.CalcMaterialCode(dataBson, old, "SysCity");//计算材料编码
                }
                else
                {
                    code = materialBll.CalcMaterialCode(dataBson, old);//计算材料编码
                }
                dataBson.TryAdd("matNum", code);
                //从产品系列或专业库新增的材料初始状态为非正式材料，用status区分
                //0或空：从材料库创建
                //产品系列: 1 非正式  2：正式
                //项目库： 3：非正式  4：正式
                //专业库： 5：非正式  6：正式
                if (serialSave == 1) {
                    dataBson.TryAdd("status", "2");
                }
                if (isFromProject)
                {
                    dataBson.Set("status", "4");
                }
                if (isFromStdResult)
                {
                    dataBson.Set("status", "6");
                }
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
                if (prices != null)
                    //SaveMaterialPrices(prices, material.String("matId"));//保存材料价格
                {
                    string priceTable = "MaterialPrices";
                    dataOp.Delete(priceTable, Query.EQ("matId", material.String("matId")));//删除之前的所有价格信息
                    var temps = prices.SplitParam(StringSplitOptions.None, "|Y|");
                    var storageList = new List<StorageData>();
                    foreach (var temp in temps)
                    {
                        if (temp != "")
                        {
                            var priceInfo = temp.SplitParam(StringSplitOptions.None, "|$|");
                            if (priceInfo.Length > 1)
                            {
                                var tempData = new StorageData();
                                tempData.Name = priceTable;
                                tempData.Document = new BsonDocument().Add("date", priceInfo[0]).Add("price", priceInfo[1]).Add("matId",material.String("matId"));
                                tempData.Type = StorageType.Insert;
                                storageList.Add(tempData);
                            }
                        }
                    }
                    dataOp.BatchSaveStorageData(storageList);
                }
                if(extendAttrs!=null)
                SaveExtendAttrs(extendAttrs, material.String("matId"));
                if(matProjRelStr!=null)
                SaveMatProjRel(matProjRelStr, material.String("matId"));//保存关联引用项目
                if (retIds != null)
                {
                    var newFormData = new FormCollection();
                    newFormData.Add("tbName", "MaterialTecMethodRel");
                    newFormData.Add("relKey", "retId");
                    newFormData.Add("relValues", retIds);
                    newFormData.Add("matId", material.String("matId"));
                    SaveTableRelation(newFormData);
                }
                if (projIds != null)
                {
                    var newFormData = new FormCollection();
                    newFormData.Add("tbName", "MaterialProjectRel");
                    newFormData.Add("relKey", "projId");
                    newFormData.Add("relValues", projIds);
                    newFormData.Add("matId", material.String("matId"));
                    SaveTableRelation(newFormData);
                }
                var seriesIds = saveForm["seriesIds"];
                if (seriesIds != null)
                {
                    var newFormData = new FormCollection();
                    newFormData.Add("tbName", "MaterialSeriesRel");
                    newFormData.Add("relKey", "seriesId");
                    newFormData.Add("relValues", seriesIds);
                    newFormData.Add("matId", material.String("matId"));
                    SaveTableRelation(newFormData);
                }
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

                #region 产品系列新增材料
                string seriesId = PageReq.GetParam("seriesId");
                if (serialSave == 1) {
                    
                    string combinationId = PageReq.GetParam("combinationId");
                    string treeId = PageReq.GetParam("treeId");
                    string itemId = PageReq.GetParam("itemId");
                    var matId = result.BsonInfo.Text("matId");
                    BsonDocument serialData = new BsonDocument();
                    serialData.Add("seriesId", seriesId);
                    serialData.Add("combinationId", combinationId);
                    serialData.Add("treeId", treeId);
                    serialData.Add("itemId", itemId);
                    serialData.Add("matId", matId);
                    serialData.Add("isSeries", "1");
                    serialData.Add("matName", PageReq.GetForm("name"));
                    dataOp.Insert("ProductItemMatRelation", serialData);
                }
                #endregion

                #region 专业库新增材料
                if (isFromStdResult)
                {
                    string itemId=PageReq.GetForm("itemId");//成果价值项id
                    string retId = PageReq.GetForm("retId");//所属成果id
                    string matId = result.BsonInfo.Text("matId");
                    BsonDocument tempDoc = new BsonDocument(){
                        {"itemId",itemId.ToString()},
                        {"retId",retId.ToString()},
                        {"matId",matId.ToString()}
                    };
                    dataOp.Insert("XH_StandardResult_ResultItemMatRelation", tempDoc);
                }
                #endregion

                #region 项目库新增材料
                if (isFromProject)
                {
                    string itemId = PageReq.GetParam("itemId");
                    var matId = result.BsonInfo.Text("matId");
                    BsonDocument serialData = new BsonDocument();
                    serialData.Add("seriesId", seriesId);
                    serialData.Add("itemId", itemId);
                    serialData.Add("matId", matId);
                    serialData.Add("formProj", "1");
                    serialData.Add("matName", PageReq.GetForm("name"));
                    dataOp.Insert("ProjectItemMatRelation", serialData);
                }
                #endregion

                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
        }
  
        /***********************end****************************************/

        #region 判断基类扩展属性
        /// <summary>
        /// 判断扩展属性是否被材料引用
        /// </summary>
        /// <param name="extendId"></param>
        /// <returns></returns>
        public JsonResult BaseCatExtendIsUse(int extendId)
        {
            PageJson json = new PageJson();
            List<BsonDocument> isUseExtend = dataOp.FindAllByQuery("BaseCatExtendVal", Query.EQ("extendId", extendId.ToString())).ToList();
            if (isUseExtend.Count() > 0)
            {
                json.Message = "该扩展属性已经被引用,不能删除!";
                json.Success = false;
                return Json(json);
            }
            json.Success = true;
            return Json(json, JsonRequestBehavior.AllowGet);
        }

        public JsonResult test()
        {
            PageJson json = new PageJson();
            json.Success = true;
            return Json(json, JsonRequestBehavior.AllowGet);
        }
        /// <summary>
        /// 获取类目目录树形
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="moveId"></param>
        /// <param name="ddirId">目录树Id</param>
        /// <returns></returns>
        public ActionResult GetMaterialCategoryTree(string tbName, string moveId)
        {
            TableRule tbRule = new TableRule(tbName);

            string tbKey = tbRule.PrimaryKey;

            List<BsonDocument> catList = dataOp.FindAll(tbName).OrderBy(x => x.String("nodeKey")).ToList();//获取模板下的目录
            List<TreeNode> treeList = TreeHelper.GetSingleTreeList(catList);

            return new XmlTree(treeList);
        }
        #endregion



        #region 乔鑫设计供应商保存
        /// <summary>
        /// 保存提交上来的数据
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        [ValidateInput(false)]
        public ActionResult SaveSupplierQX(FormCollection saveForm)
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
                    if (tempKey == "tbName" || tempKey == "queryStr" || tempKey.Contains("fileList[") || tempKey.Contains("param.") || tempKey.Contains("designers")) continue;

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

            #region 设计师信息保存
            if (result.Status == Status.Successful)
            {
                int designId = result.BsonInfo.Int("supplierId");
                List<StorageData> designList = DesignSupplierDataInfo(designId, PageReq.GetForm("designers"));
                if (designList.Count() > 0)
                {
                    dataOp.BatchSaveStorageData(designList);
                }
            }
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
        /// <summary>
        /// 返回设计师数据操作列表
        /// </summary>
        /// <param name="supplierId"></param>
        /// <param name="designers"></param>
        /// <returns></returns>
        public List<StorageData> DesignSupplierDataInfo(int supplierId, string designers)
        {
            List<StorageData> dataList = new List<StorageData>();//返回的设计师更新列表
            List<BsonDocument> exitDesignerList = dataOp.FindAllByQuery("XH_Supplier_Designer", Query.EQ("supplierId", supplierId.ToString())).ToList();//该供应商已经存在的设计师信息

            string[] item = Regex.Split(designers, @"\*\#", RegexOptions.IgnoreCase);//每条数据对象字符串
            foreach (var tempItem in item)
            {
                if (!string.IsNullOrEmpty(tempItem))
                {
                    BsonDocument tempEntity = new BsonDocument();
                    StorageData tempStorageData = new StorageData();
                    string[] designerArr = Regex.Split(tempItem, @"\|\#", RegexOptions.IgnoreCase);//单条设计师所有属性字符串
                    foreach (var tempdesigner in designerArr)
                    {
                        if (!string.IsNullOrEmpty(tempdesigner))
                        {
                            string[] propertyItem = Regex.Split(tempdesigner, @"_\#", RegexOptions.IgnoreCase);//单个属性对应值
                            if (propertyItem.Length == 2)
                            {
                                if (propertyItem[0] == "designerId")
                                {
                                    tempEntity.Add("designerId", propertyItem[1]);
                                }
                                else
                                {
                                    tempEntity.Add(propertyItem[0], propertyItem[1]);
                                }
                            }
                            else
                            {
                            }
                        }
                    }
                    if (tempEntity.ContainsColumn("designerId"))
                    {
                        BsonDocument tempDsner = exitDesignerList.Where(x => x.String("designerId") == tempEntity.String("designerId")).FirstOrDefault();
                        if (tempDsner == null)
                        {
                            tempEntity.Add("supplierId", supplierId.ToString());
                            tempStorageData.Document = tempEntity;
                            tempStorageData.Name = "XH_Supplier_Designer";
                            tempStorageData.Type = StorageType.Insert;
                            dataList.Add(tempStorageData);
                        }
                        else
                        {
                            exitDesignerList.Remove(tempDsner);//去除更新的列表
                            foreach (var tempCol in tempEntity.Names)
                            {
                                if (tempCol == "designerId")
                                {
                                    continue;
                                }
                                tempDsner[tempCol] = tempEntity[tempCol];
                            }
                            tempStorageData.Document = tempDsner;
                            tempStorageData.Name = "XH_Supplier_Designer";
                            tempStorageData.Query = Query.EQ("designerId", tempDsner.String("designerId"));
                            tempStorageData.Type = StorageType.Update;
                            dataList.Add(tempStorageData);
                        }
                    }
                }
            }
            if (exitDesignerList.Count() > 0)
            {
                foreach (var tempDsner in exitDesignerList)
                {
                    StorageData tempStorageData = new StorageData();
                    tempStorageData.Document = tempDsner;
                    tempStorageData.Name = "XH_Supplier_Designer";
                    tempStorageData.Query = Query.EQ("designerId", tempDsner.String("designerId"));
                    tempStorageData.Type = StorageType.Delete;
                    dataList.Add(tempStorageData);
                }
            }
            return dataList;
        }
        /// <summary>
        /// 刷新头像
        /// </summary>
        /// <param name="designerId"></param>
        /// <returns></returns>
        public JsonResult GetPhotoHtml(int designerId)
        {
            PageJson json = new PageJson();
            string html = string.Empty;
            string fileName = string.Empty;
            BsonDocument fileRel = dataOp.FindOneByQuery("FileRelation", Query.And(
                             Query.EQ("tableName", "XH_Supplier_Designer"),
                             Query.EQ("keyName", "designerId"),
                             Query.EQ("keyValue", designerId.ToString()),
                             Query.EQ("fileObjId", "91")));
            BsonDocument file = new BsonDocument();
            if (fileRel != null)
            {
                file = dataOp.FindOneByQuery("FileLibrary", Query.EQ("fileId", fileRel.String("fileId")));
                if (file.String("name").Length < 5)
                {
                    fileName = file.String("name");
                }
                else
                {
                    fileName = file.String("name").Substring(0, 3) + "...";
                }
            }
            html = "<div id=\"UserPhoto\">";
            if (file != null)
            {
                html += "<img src=\"";
                html += file.String("thumbPicPath").Replace("_m", "_us");
                html += "\" width=\"60\" height=\"60\" /> ";
            }
            html += "</div>";
            if (fileRel == null)
            {
                html += "<a class=\"btn_04\" id=\"upPhoto\" href=\"javascript:void(0);\"";
                html += "onclick='UploadFiles(this,\"false\",reLoadUser)'";
                html += " filetypeid=\"0\" fileobjid=\"91\" keyvalue=\"";
                html += designerId.ToString();
                html += "\" tablename=\"XH_Supplier_Designer\" keyname=\"designerId\" >";
                html += "<img src=\"<%=SysAppConfig.HostDomain%>/Content/images/icon/ico-up.png\" />上传头像<span></span>";
                html += "</a>";
            }
            else
            {
                html += "<a class=\"btn_04\" id=\"delPhoto\" href=\"javascript:void(0);\" onclick='DeleteFiles(\"";
                html += fileRel.String("fileRelId");
                html += "\" ,reLoadUser)'>";
                html += "<img src=\"<%=SysAppConfig.HostDomain%>/Content/images/icon/icon003a3.gif\" />删除头像<span></span>";
                html += "</a>";
            }
            json.Success = true;
            if (file != null)
            {
                json.AddInfo("listHtml", "<a href=\"javascript:void(0);\" onclick='" + FileCommonOperation.GetClientOnlineRead(file) + "'>"+fileName+"</a>");
                //json.AddInfo("onclick","onclick=\""+ FileCommonOperation.GetClientOnlineRead(file)+"\"");
            }
            else 
            {
                json.AddInfo("listHtml", "");
            }
            json.AddInfo("html", html);
            json.AddInfo("fileRelId", fileRel.String("fileRelId"));
            json.AddInfo("fileId", fileRel.String("fileId"));
            return Json(json);

        }


        #region 获取设计人员头像 +string GetDeisnerPhotoPath(string designerId)
        /// <summary>
        /// 获取设计人员头像
        /// </summary>
        /// <param name="designerId"></param>
        /// <returns></returns>
        public string GetDeisnerPhotoPath(string designerId)
        {
            string imgPhoto = "";
            var fileRel = dataOp.FindAllByQuery("FileRelation", Query.And(
                Query.EQ("tableName", "XH_Supplier_Designer"),
                Query.EQ("keyName", "designerId"),
                Query.EQ("keyValue", designerId),
                Query.EQ("fileObjId", "91")
                )).OrderByDescending(c => c.Date("createDate")).FirstOrDefault();
            if (fileRel != null)
            {
                var fileObj = dataOp.FindOneByQuery("FileLibrary", Query.EQ("fileId", fileRel.String("fileId")));
                if (fileObj != null)
                    imgPhoto = fileObj.String("thumbPicPath");
            }
            return imgPhoto;
        } 
        #endregion

        #endregion

       #region 判断供应商名称是否已经存在 string IsExistsSupplierName(string supplierName)
       public string IsExistsSupplierName(string supplierId,string name)
       {
           var ret = false;
           name = Server.UrlDecode(name);
           var supplier = dataOp.FindOneByQuery("XH_Supplier_Supplier", Query.EQ("name", name));
           if (supplier != null && supplier.String("supplierId") != supplierId)
               ret = true;
           return ret.ToString();
       } 
       #endregion

       #region 新城选材
       [HttpPost]
       public JsonResult GetXCMaterialInfos(string baseCatId)
       {
           var mats = dataOp.FindAllByQuery("Material_Material", Query.EQ("baseCatId", baseCatId));
           List<System.Collections.Hashtable> retList = new List<System.Collections.Hashtable>();
           retList.Capacity = (int)mats.Count();
           foreach (var mat in mats.OrderBy(s => s.String("name")))
           {
               var brand = dataOp.FindOneByQuery("Material_Brand", Query.EQ("brandId", mat.String("brandId")));
               var supplier = dataOp.FindOneByQuery("Material_Supplier", Query.EQ("supplierId", mat.String("supplierId")));
               mat.TryAdd("brand", brand.String("name"));
               mat.TryAdd("supplier", supplier.String("name"));
               mat.Remove("_id");
               retList.Add(mat.ToHashtable());
           }
           return Json(retList);

       }
       #endregion


    }
}
