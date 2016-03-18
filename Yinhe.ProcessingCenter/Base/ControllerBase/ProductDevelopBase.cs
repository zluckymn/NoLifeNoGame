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
using Yinhe.ProcessingCenter.DesignManage.TaskFormula;
using MongoDB.Driver.Builders;
using System.Transactions;
using Yinhe.ProcessingCenter.DesignManage;
using System.Xml.Linq;
using System.Collections;

namespace Yinhe.ProcessingCenter
{
    /// <summary>
    /// 产品线后台管理基类
    /// </summary>
    public partial class ControllerBase : Controller
    {
        /// <summary>
        /// 保存产品线中,价值项值与材料的关联
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SaveLineItemMatRelInfo(FormCollection saveForm)
        {
            InvokeResult result = new InvokeResult();
            PageJson json = new PageJson();

            string tbName = "XH_ProductDev_LineItemMatRelation";    //表名
            string queryStr = PageReq.GetForm("queryStr");          //定位记录
            string matIds = PageReq.GetForm("matIds");              //材料Id列表

            if (PageReq.GetForm("lineId").Trim() == "" || PageReq.GetForm("treeId").Trim() == "" || PageReq.GetForm("combinationId").Trim() == "" || PageReq.GetForm("itemId").Trim() == "")
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

                    tempBson.Add("lineId", dataBson.String("lineId"));
                    tempBson.Add("combinationId", dataBson.String("combinationId"));
                    tempBson.Add("treeId", dataBson.String("treeId"));
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
        /// 获取产品线价值树值组合
        /// </summary>
        /// <returns></returns>
        public ActionResult GetLineValCombination(int lineId, int treeId)
        {
            List<BsonDocument> combinationList = dataOp.FindAllByQueryStr("XH_ProductDev_LineValCombination", string.Format("lineId={0}&treeId={1}", lineId, treeId)).ToList();  //获取所有组合列表

            List<BsonDocument> allLandTypeList = dataOp.FindAll("XH_ProductDev_LandType").ToList();

            List<BsonDocument> allSegmentList = dataOp.FindAll("XH_ProductDev_Segment").ToList();

            foreach (var combination in combinationList)
            {
                List<string> hasTypeIdList = combination.String("hasLandTypeIds").Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();//拥有的土地类型列表
                List<BsonDocument> hasLandTypeList = allLandTypeList.Where(t => hasTypeIdList.Contains(t.String("typeId"))).ToList();

                List<string> hasSegmentIdList = combination.String("hasSegmentIds").Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList(); //拥有客群类型列表
                List<BsonDocument> hasSegmentList = allSegmentList.Where(t => hasSegmentIdList.Contains(t.String("typeId"))).ToList();

                string landTypeStr = "";
                string segmentStr = "";

                foreach (var temp in hasLandTypeList)
                {
                    landTypeStr += string.Format("{0},", temp.String("name"));
                }

                foreach (var temp in hasSegmentList)
                {
                    segmentStr += string.Format("{0},", temp.String("name"));
                }

                combination.Add("name", landTypeStr.TrimEnd(',') + segmentStr.TrimEnd(','));
            }

            List<Hashtable> retList = new List<Hashtable>();

            foreach (var tempDoc in combinationList)
            {
                tempDoc.Add("allCount", combinationList.Count);
                tempDoc.Remove("_id");

                retList.Add(tempDoc.ToHashtable());
            }

            return this.Json(retList, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 获取产品线价值树值组合（新）
        /// </summary>
        /// <returns></returns>
        public ActionResult GetLineValCombinationNew(int lineId, int treeId)
        {
            int propertyId = PageReq.GetFormInt("propertyId");//新增业态类型过滤 
            List<BsonDocument> combinationList = dataOp.FindAllByQueryStr("XH_ProductDev_LineValCombination", string.Format("lineId={0}&treeId={1}", lineId, treeId)).ToList();  //获取所有组合列表
            if (propertyId != 0)
            {
                combinationList = combinationList.Where(x => x.Int("propertyId") == propertyId).ToList();
            }
            //List<BsonDocument> allLandTypeList = dataOp.FindAll("XH_ProductDev_LandType").ToList();

            //List<BsonDocument> allSegmentList = dataOp.FindAll("XH_ProductDev_Segment").ToList();

            //foreach (var combination in combinationList)
            //{
            //    List<string> hasTypeIdList = combination.String("hasLandTypeIds").Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();//拥有的土地类型列表
            //    List<BsonDocument> hasLandTypeList = allLandTypeList.Where(t => hasTypeIdList.Contains(t.String("typeId"))).ToList();

            //    List<string> hasSegmentIdList = combination.String("hasSegmentIds").Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList(); //拥有客群类型列表
            //    List<BsonDocument> hasSegmentList = allSegmentList.Where(t => hasSegmentIdList.Contains(t.String("typeId"))).ToList();

            //    string landTypeStr = "";
            //    string segmentStr = "";

            //    foreach (var temp in hasLandTypeList)
            //    {
            //        landTypeStr += string.Format("{0},", temp.String("name"));
            //    }

            //    foreach (var temp in hasSegmentList)
            //    {
            //        segmentStr += string.Format("{0},", temp.String("name"));
            //    }

            //    combination.Add("name", landTypeStr.TrimEnd(',') + segmentStr.TrimEnd(','));
            //}

            List<Hashtable> retList = new List<Hashtable>();

            foreach (var tempDoc in combinationList)
            {
                tempDoc.Add("allCount", combinationList.Count);
                tempDoc.Remove("_id");

                retList.Add(tempDoc.ToHashtable());
            }

            return this.Json(retList, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 将XH土地子地块信息导出Excel
        /// </summary>
        public void GetXHSubLandIndexExcel()
        {
            #region 获取相关展示信息
            //命名导出表格的StringBuilder变量
            StringBuilder htmlStr = new StringBuilder(string.Empty);


            //所有供应商信息
            List<BsonDocument> suplierList = dataOp.FindAll("XH_Supplier_Supplier").ToList();

            StringBuilder indexStr = new StringBuilder();
            #endregion

            #region 形成对应Html表格

            htmlStr.Append("<html xmlns:v=\"urn:schemas-microsoft-com:vml\" xmlns:o=\"urn:schemas-microsoft-com:office:office\" xmlns:x=\"urn:schemas-microsoft-com:office:excel\" xmlns=\"http://www.w3.org/TR/REC-html40\">");
            htmlStr.Append("<head><meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\">");
            htmlStr.Append("<!--[if gte mso 9]><xml><x:ExcelWorkbook><x:ExcelWorksheets><x:ExcelWorksheet><x:Name></x:Name><x:WorksheetOptions><x:Selected/></x:WorksheetOptions></x:ExcelWorksheet></x:ExcelWorksheets></x:ExcelWorkbook></xml><![endif]-->");
            htmlStr.Append("</head>");
            htmlStr.Append("<body>");
            htmlStr.Append("<table>");

            #region 表头
            htmlStr.Append("<thead class=\"landtitle\">");

            #region 第一层
            htmlStr.Append("<tr>");
            htmlStr.Append("<th >序号</th>");
            htmlStr.Append("<th >公司名称</th>");
            htmlStr.Append("<th >负责人</th>");
            htmlStr.Append("<th >联系人</th>");
            htmlStr.Append("<th >电话</th>");
            htmlStr.Append("<th>公司规模</th>");
            htmlStr.Append("<td>组织架构</th>");
            htmlStr.Append("<td>公司资质</th>");
            htmlStr.Append("</tr>");
            #endregion
            htmlStr.Append("</thead>");
            #endregion

            #region 表身
            htmlStr.Append("<tbody>");

            int index = 0;
            foreach (var item in suplierList)
            {
                htmlStr.Append("<tr>");
                htmlStr.AppendFormat("<td>{0}</td>", index);
                htmlStr.AppendFormat("<td>{0}</td>", item.String("name"));
                htmlStr.AppendFormat("<td>{0}</td>", item.String("Personcharge"));
                htmlStr.AppendFormat("<td>{0}</td>", item.String("Contact"));
                htmlStr.AppendFormat("<td>{0}</td>", item.String("TEL"));
                htmlStr.AppendFormat("<td>{0}</td>", item.String("Sizecompany"));
                htmlStr.AppendFormat("<td>{0}</td>", item.String("Structure"));
                htmlStr.AppendFormat("<td>{0}</td>", item.String("Qualification"));
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
            ExportToExcel("application/ms-excel", "供应商信息库.xls", htmlStr.ToString());
        }

        /// <summary>
        /// 将华侨城土地子地块信息导出Excel
        /// </summary>
        public void GetSNHQSubLandIndexExcel()
        {
            #region 获取相关展示信息
            //命名导出表格的StringBuilder变量
            StringBuilder htmlStr = new StringBuilder(string.Empty);


            //所有供应商信息
            List<BsonDocument> suplierList = dataOp.FindAll("Supplier_Supplier").ToList();

            StringBuilder indexStr = new StringBuilder();
            #endregion

            #region 形成对应Html表格

            htmlStr.Append("<html xmlns:v=\"urn:schemas-microsoft-com:vml\" xmlns:o=\"urn:schemas-microsoft-com:office:office\" xmlns:x=\"urn:schemas-microsoft-com:office:excel\" xmlns=\"http://www.w3.org/TR/REC-html40\">");
            htmlStr.Append("<head><meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\">");
            htmlStr.Append("<!--[if gte mso 9]><xml><x:ExcelWorkbook><x:ExcelWorksheets><x:ExcelWorksheet><x:Name></x:Name><x:WorksheetOptions><x:Selected/></x:WorksheetOptions></x:ExcelWorksheet></x:ExcelWorksheets></x:ExcelWorkbook></xml><![endif]-->");
            htmlStr.Append("</head>");
            htmlStr.Append("<body>");
            htmlStr.Append("<table>");

            #region 表头
            htmlStr.Append("<thead class=\"landtitle\">");

            #region 第一层
            htmlStr.Append("<tr>");
            htmlStr.Append("<th >序号</th>");
            htmlStr.Append("<th >名称</th>");
            htmlStr.Append("<th >网站</th>");
            htmlStr.Append("<th >联系人</th>");
            htmlStr.Append("<th >注册资本</th>");
            htmlStr.Append("<th>联系电话</th>");
            htmlStr.Append("<td>公司地址</th>");
            htmlStr.Append("<td>公司规模</th>");
            htmlStr.Append("</tr>");
            #endregion
            htmlStr.Append("</thead>");
            #endregion

            #region 表身
            htmlStr.Append("<tbody>");

            int index = 0;
            foreach (var item in suplierList)
            {
                htmlStr.Append("<tr>");
                htmlStr.AppendFormat("<td>{0}</td>", index);
                htmlStr.AppendFormat("<td>{0}</td>", item.String("name"));
                htmlStr.AppendFormat("<td>{0}</td>", item.String("Website"));
                htmlStr.AppendFormat("<td>{0}</td>", item.String("Contact"));
                htmlStr.AppendFormat("<td>{0}</td>", item.String("Capital"));
                htmlStr.AppendFormat("<td>{0}</td>", item.String("TEL"));
                htmlStr.AppendFormat("<td>{0}</td>", item.String("Personcharge"));
                htmlStr.AppendFormat("<td>{0}</td>", item.String("Email"));
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
            ExportToExcel("application/ms-excel", "苏宁供应商信息库.xls", htmlStr.ToString());
        }



        /// <summary>
        /// 保存产品线加初始化土地客群组合
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        public ActionResult SaveProductLineAndInit(FormCollection saveForm)
        {
            PageJson json = new PageJson();
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
            //if (string.IsNullOrEmpty(queryStr) && result.Status == Status.Successful) 
            //{
            //    List<BsonDocument> treeList = dataOp.FindAll("XH_ProductDev_ValueTree").ToList();
            //    List<BsonDocument> treeListInUse = treeList.Where(x => x.Int("treeType") > 0).ToList();
            //    List<StorageData> storageData = new List<StorageData>();
            //    foreach (var tree in treeListInUse) 
            //    {
            //        BsonDocument tempBson = new BsonDocument();
            //        tempBson.Add("lineId",result.BsonInfo.String("lineId"));
            //        tempBson.Add("treeId", tree.String("treeId"));
            //        tempBson.Add("hasLandTypeIds", "");
            //        tempBson.Add("hasSegmentIds", "");
            //        StorageData tempSto = new StorageData();
            //        tempSto.Document = tempBson;
            //        tempSto.Type = StorageType.Insert;
            //        tempSto.Name = "XH_ProductDev_LineValCombination";
            //        storageData.Add(tempSto);
            //    }
            //    var result1 = dataOp.BatchSaveStorageData(storageData);
            //    if (result1.Status == Status.Failed) 
            //    {
            //        result.Status = Status.Failed;
            //        dataOp.Delete(tbName,Query.EQ("lineId",result.BsonInfo.String("lineId")));
            //    }
            //}
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }


        public ActionResult SaveSegmentFeature(FormCollection form)
        {
            string featureId = form["featureId"];
            var feature = dataOp.FindOneByQuery("ProductSegmentFeature", Query.EQ("featureId", featureId));
            var elementsToDel = feature.Elements.Where(c => c.Name.StartsWith("|Data|_")).ToList();
            foreach (var ele in elementsToDel)
            {
                feature.RemoveElement(ele);
            }
            var dataKeys = form.AllKeys.Where(c => c.StartsWith("dataKey_")).ToList();
            foreach (var dataKey in dataKeys)
            {
                var keyName = dataKey;
                var valueName = "dataValue_" + dataKey.Replace("dataKey_", "");
                var keyStr = ("|Data|_" + form[keyName]).Trim();
                var valueStr = form[valueName].Trim();
                feature.Set(keyStr, valueStr);
            }
            var ret = dataOp.Update("ProductSegmentFeature", Query.EQ("featureId", featureId), feature);
            return Json(TypeConvert.InvokeResultToPageJson(ret));
        }

        //public ActionResult SaveSegmentFeature(FormCollection form)
        //{
        //    string seriesId = form["seriesId"];
        //    var features = dataOp.FindAllByQuery("ProductSegmentFeature", Query.EQ("seriesId", seriesId)).ToList();
        //    var storageDatas = new List<StorageData>();

        //    foreach (var feature in features)
        //    {
        //        string dataKey = "dataKey_" + feature.String("featureId");
        //        string dataValueKey = "dataValue_" + feature.String("featureId");
        //        if (!form.AllKeys.Contains(dataKey)||!form.AllKeys.Contains(dataValueKey))
        //            continue;
        //        var dataKeys = form[dataKey].Split(new char[] { ',' }); //该特征的所有Key
        //        var dataValues = form[dataValueKey].Split(new char[] { ',' });//该特征的所有key对应的value
        //        if (dataKeys.Length != dataValues.Length)//判断防止越界，说明，输入内容中的里面本身含有",";
        //            continue;
        //        //移除原来的数据
        //        var elementsToDel = feature.Elements.Where(c => c.Name.StartsWith("|Data|_")).ToList();
        //        foreach (var ele in elementsToDel)
        //        {
        //            feature.RemoveElement(ele);
        //        }
        //        for (int i = 0; i < dataKeys.Length; i++)
        //        {

        //            string key = "|Data|_" + dataKeys[i].Trim();
        //            string value = dataValues[i].Trim();  //key中不能含有逗号，不然数据会越界
        //            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
        //                continue;
        //            feature.Set(key, value);
        //        }
        //        storageDatas.Add(new StorageData
        //        {
        //            Name = "ProductSegmentFeature",
        //            Document = feature,
        //            Query = Query.EQ("featureId", feature.String("featureId")),
        //            Type = StorageType.Update
        //        });
        //    }
        //    var ret = dataOp.BatchSaveStorageData(storageDatas);
        //    return Json(TypeConvert.InvokeResultToPageJson(ret));
        //}


        #region 乔鑫产品系列
        /// <summary>
        /// 保存乔鑫产品系列
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        public ActionResult SaveQXProductSeries(FormCollection saveForm)
        {
            InvokeResult result = new InvokeResult();

            #region 构建数据
            string tbName = "ProductSeries";

            BsonDocument dataBson = new BsonDocument();
            foreach (var tempKey in saveForm.AllKeys)
            {
                if (tempKey == "seriesId") continue;

                dataBson.Add(tempKey, PageReq.GetForm(tempKey));
            }

            int seriesId = PageReq.GetFormInt("seriesId");
            var query = seriesId == 0 ? Query.Null : Query.EQ("seriesId", seriesId.ToString());

            #endregion

            #region 保存数据
            try
            {

                result = dataOp.Save(tbName, query, dataBson);

                if (seriesId == 0)  //如果是新建,初始化土地客群数据
                {
                    InvokeResult landRet = dataOp.Insert("ProductLand", new BsonDocument()
                        .Add("seriesId", result.BsonInfo.String("seriesId"))
                        .Add("name", result.BsonInfo.String("name") + "土地"));
                    if (landRet.Status == Status.Failed) throw new Exception(landRet.Message);

                    InvokeResult segmentRet = dataOp.Insert("ProductSegment", new BsonDocument()
                        .Add("seriesId", result.BsonInfo.String("seriesId"))
                        .Add("name", result.BsonInfo.String("name") + "客群"));
                    if (landRet.Status == Status.Failed) throw new Exception(landRet.Message);
                }
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
        /// 设置项目所属的系列
        /// </summary>
        /// <returns></returns>
        public ActionResult SetSeriesProject(string seriesId, string projIds)
        {
            InvokeResult result = new InvokeResult();

            var projRelList = dataOp.FindAllByQuery("ProductProjRelation", Query.EQ("seriesId", seriesId.ToString())).ToList();   //所有旧项目关联

            List<string> oldProjIdList = projRelList.Select(t => t.String("projId")).ToList();

            List<string> newProjIdList = projIds.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            List<StorageData> saveList = new List<StorageData>();    //添加列表

            foreach (var projId in newProjIdList)   //循环提交数据,如果已存在关联,则不添加,否则,添加关联
            {
                if (oldProjIdList.Contains(projId) == false)
                {
                    var seriesProjRel = new BsonDocument();
                    seriesProjRel.Add("seriesId", seriesId).Add("projId", projId);
                    var storageData = new StorageData
                    {
                        Name = "ProductProjRelation",
                        Document = seriesProjRel,
                        Type = StorageType.Insert
                    };
                    saveList.Add(storageData);
                }
            }

            foreach (var projId in oldProjIdList)   //循环旧的关联数据,如果提交数据中不存在,则删除关联
            {
                if (newProjIdList.Contains(projId) == false)
                {
                    var storageData = new StorageData
                    {
                        Name = "ProductProjRelation",
                        Query = Query.And(Query.EQ("seriesId", seriesId), Query.EQ("projId", projId)),
                        Type = StorageType.Delete
                    };
                    saveList.Add(storageData);
                }
            }

            //遍历项目关联,构建默认土地项目数据
            var defaultLand = dataOp.FindOneByQuery("ProductLand", Query.EQ("seriesId", seriesId.ToString()));       //默认土地

            var landProjList = dataOp.FindAllByQuery("ProductLandProj", Query.EQ("seriesId", seriesId.ToString())).ToList();        //所有土地项目关联

            try
            {
                var ret = dataOp.BatchSaveStorageData(saveList);
                if (ret.Status == Status.Failed) throw new Exception(result.Message);

                var newRelList = dataOp.FindAllByQuery("ProductProjRelation", Query.EQ("seriesId", seriesId.ToString())).ToList();   //重读所有项目关联

                foreach (var tempRel in newRelList)  //判断是否存在土地关联,没有,则添加
                {
                    if (landProjList.Where(t => t.Int("relId") == tempRel.Int("relId")).Count() <= 0)
                    {
                        var tempRet = dataOp.Insert("ProductLandProj", new BsonDocument()
                            .Add("seriesId", seriesId)
                            .Add("landId", defaultLand.String("landId"))
                            .Add("relId", tempRel.String("relId"))
                            .Add("projId", tempRel.String("projId")));

                        if (tempRet.Status == Status.Failed) throw new Exception(result.Message);
                    }
                }
            }
            catch (Exception e)
            {
                result.Status = Status.Failed;
                result.Message = e.Message;
            }

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        /// <summary>
        /// 设置项目所属的系列
        /// </summary>
        /// <returns></returns>
        public ActionResult SetSeriesHouseType(string seriesId, string treeId, string typeStr)
        {
            InvokeResult result = new InvokeResult();

            var oldTypeList = dataOp.FindAllByQuery("ProductValueType", Query.And(  //所有旧的类型
                Query.EQ("seriesId", seriesId.ToString()),
                Query.EQ("treeId", treeId.ToString())
                )).ToList();

            #region 构建数据
            List<StorageData> saveList = new List<StorageData>();    //添加列表
            List<string> newIdList = new List<string>();

            List<string> tempStrList = typeStr.Split(new string[] { "|Y|" }, StringSplitOptions.RemoveEmptyEntries).ToList();

            #region 需要添加更新的列表
            foreach (var tempStr in tempStrList)
            {
                string tempId = tempStr.Split(new string[] { "|H|" }, StringSplitOptions.None)[0];
                string tempName = tempStr.Split(new string[] { "|H|" }, StringSplitOptions.None)[1];

                if (tempId == "0")
                {
                    var dataBson = new BsonDocument()
                        .Add("seriesId", seriesId.ToString())
                        .Add("treeId", treeId.ToString())
                        .Add("name", tempName);
                    var storageData = new StorageData
                    {
                        Name = "ProductValueType",
                        Document = dataBson,
                        Type = StorageType.Insert
                    };
                    saveList.Add(storageData);
                }
                else
                {
                    newIdList.Add(tempId);

                    var dataBson = new BsonDocument().Add("name", tempName);
                    var storageData = new StorageData
                    {
                        Name = "ProductValueType",
                        Query = Query.EQ("typeId", tempId),
                        Document = dataBson,
                        Type = StorageType.Update
                    };
                    saveList.Add(storageData);
                }
            }
            #endregion

            #region 需要删除的列表
            foreach (var tempOld in oldTypeList)
            {
                if (newIdList.Contains(tempOld.String("typeId")) == false)
                {
                    var storageData = new StorageData
                    {
                        Name = "ProductValueType",
                        Query = Query.EQ("typeId", tempOld.String("typeId")),
                        Type = StorageType.Delete
                    };
                    saveList.Add(storageData);
                }
            }
            #endregion
            #endregion

            result = dataOp.BatchSaveStorageData(saveList);

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        #endregion



        /// <summary>
        /// 保存系列,价值项值与材料的关联 QX
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SaveSeriesItemMatRelInfo(FormCollection saveForm)
        {
            InvokeResult result = new InvokeResult();
            PageJson json = new PageJson();

            string tbName = "ProductItemMatRelation";    //表名
            string queryStr = PageReq.GetForm("queryStr");          //定位记录
            string matIds = PageReq.GetForm("matIds");              //材料Id列表

            if (PageReq.GetForm("seriesId").Trim() == "" || PageReq.GetForm("retId").Trim() == "" || PageReq.GetForm("typeId").Trim() == "" || PageReq.GetForm("itemId").Trim() == "")
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

                    tempBson.Add("seriesId", dataBson.String("seriesId"));
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
            json = TypeConvert.InvokeResultToPageJson(result);

            return Json(json);
        }
        /// <summary>
        /// 根据目录Id加载图文档列表,当前节点
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        public string GetVIRetListHtml(string dirTable, string resultTable, string lastDir, int lastDirCount, string dirIds, string useDirIds)
        {
            ////当前的页数
            //var current = PageReq.GetParamInt("current");
            //最后的目录。
            //var lastDir = PageReq.GetParam("lastDir");
            ////最后的目录已经被取出的成果数
            //var lastDirCount = PageReq.GetParamInt("lastDirCount");
            ////全部的id列表
            //var dirIdList = PageReq.GetParamList("dirIds");
            ////已经显示的Id列表
            //var useDirIds = PageReq.GetParam("useDirIds");

            var dirIdList = dirIds.Split(',');

            //每次读取的成果数量
            int takeNum = 10;
            //记录新增加的成果数量
            int hasNum = 0;
            //是否是营销活动
            var isMarketing = false;

            //排除已经读取的目录
            var hitIds = "";
            foreach (var tempId in dirIdList)
            {
                if (useDirIds.Contains(tempId) == false)
                    hitIds += (hitIds == "") ? tempId : "," + tempId;
            }

            var hitIdList = hitIds.Split(',');
            var dirList = dataOp.FindAllByKeyValList(dirTable, "dirId", hitIdList).OrderBy(t => t.String("nodeKey")).ToList();
            //将最后目录中的剩下的成果取出
            var resultList = new List<BsonDocument>();
            var hitDir = dataOp.FindOneByKeyVal(dirTable, "dirId", lastDir);

            var tempResultList = new List<BsonDocument>();

            if (hitDir.Int("isMarketing") == 1)
            {
                isMarketing = true;
                tempResultList = dataOp.FindAllByKeyVal(resultTable, "dirId", lastDir)
                    .OrderBy(t => t.Date("activeTime", DateTime.MinValue)).Skip(lastDirCount).ToList();
            }
            else
            {
                tempResultList = dataOp.FindAllByKeyVal(resultTable, "dirId", lastDir)
                .OrderBy(t => t.Date("createDate")).Skip(lastDirCount).ToList();
            }

            if (tempResultList.Count > 0)
            {
                resultList = resultList.Union(tempResultList.Take(takeNum)).ToList();
            }

            if (resultList.Count < takeNum)
            {
                hasNum = resultList.Count;
                foreach (var tempDir in dirList)
                {
                    if (tempDir.Int("isMarketing") == 1)
                        tempResultList = dataOp.FindAllByKeyVal(resultTable, "dirId", tempDir.String("dirId")).OrderBy(t => t.Date("activeTime", DateTime.MinValue)).Take(takeNum - hasNum).ToList();
                    else
                        tempResultList = dataOp.FindAllByKeyVal(resultTable, "dirId", tempDir.String("dirId")).OrderBy(t => t.Date("createDate")).Take(takeNum - hasNum).ToList();

                    resultList = resultList.Union(tempResultList).ToList();
                    if (resultList.Count < hasNum)
                    {
                        hasNum = resultList.Count;
                    }
                    else
                    {
                        lastDir = tempDir.String("dirId");
                        lastDirCount = tempResultList.Count();
                        break;
                    }
                }
            }

            StringBuilder strHtml = new StringBuilder();
            if (resultList.Count <= 0)
            {
                return "0";
            }
            else
            {
                foreach (var result in resultList)
                {
                    var fileRel = dataOp.FindAllByQuery("FileRelation", Query.And(
                                            Query.EQ("tableName", resultTable),
                                            Query.EQ("fileObjId", "92"),
                                            Query.EQ("keyValue", result.String("retId"))
                                            )).Where(t => t.String("isCover").ToLower() == "true").FirstOrDefault();
                    var file = dataOp.FindOneByKeyVal("FileLibrary", "fileId", fileRel.String("fileId"));
                    var imgSrc = file.String("thumbPicPath");
                    if (imgSrc == "")
                        imgSrc = "/Content/images/Docutype/default_m.png";
                    if (useDirIds.Contains(result.String("dirId")) == false)
                    {
                        var tempDir = dirList.Find(t => t.String("dirId") == result.String("dirId"));
                        if (tempDir.Int("isMarketing") == 1)
                            isMarketing = true;
                        else
                            isMarketing = false;
                        useDirIds += "," + result.String("dirId");
                        strHtml.AppendFormat("<table width=\"100%\"><tr><td height=\"30\"><h2>{0}</h2></td></tr></table><hr />", tempDir.String("name"));
                    }

                    strHtml.AppendFormat("<div class=\"imageblock\"><div class=\"shortcut\"><a href=\"javascript:void(0);\" onclick=\"window.open('/ProductDevelop/VIResultShow?retId={0}&seriesId={1}')\">", result.String("retId"), result.String("seriesId"));
                    strHtml.AppendFormat("<img src=\"{0}\" /></a></div>", imgSrc);
                    strHtml.AppendFormat("<div class=\"picname\"><a href=\"javascript:void(0);\" onclick=\"window.open('/ProductDevelop/VIResultShow?retId={0}&seriesId={1}')\" class=\"abrown\">{2}</a></div>", result.String("retId"), result.String("seriesId"), result.String("name"));
                    if (isMarketing == true)
                    {
                        if (result.String("activeTime") != "")
                            strHtml.AppendFormat("<div><span>活动时间：{0}</span></div></div>", result.ShortDate("activeTime"));
                        else
                            strHtml.Append("<div><span>活动时间：</span></div></div>");
                    }
                    else
                        strHtml.Append("</div>");
                }
                strHtml.AppendFormat("|Y|{0}|Y|{1}|Y|{2}", useDirIds, lastDir, lastDirCount.ToString());
            }
            return strHtml.ToString();
        }
        /// <summary>
        /// 复制产品系列价值树
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult CopyProductSeriesValTree()
        {

            int seriesId = PageReq.GetInt("seriesId");//目标Id
            int treeId = PageReq.GetInt("treeId");//树的类型
            int selectId = PageReq.GetInt("selectId");//引用的产品系列Id
            int type = PageReq.GetInt("type");//是否删除旧数据类型  0：删除  1不删除
            PageJson json = new PageJson();
            InvokeResult result = new InvokeResult();
            List<BsonDocument> sourceData = dataOp.FindAllByQuery("XH_ProductDev_ValueItem", Query.And(Query.EQ("seriesId", selectId.ToString()), Query.EQ("treeId", treeId.ToString()))).ToList();
            List<BsonDocument> oldData = new List<BsonDocument>();
            List<StorageData> dataList = new List<StorageData>();
            Dictionary<string, string> itemIdMapDC = new Dictionary<string, string>();//新旧id映射
            if (sourceData.Count == 0)
            {
                json.Message = "该系列没有目录结构,请选择其他类型!";
                json.Success = false;
                return Json(json);
            }
            if (type == 0)
            {
                oldData = dataOp.FindAllByQuery("XH_ProductDev_ValueItem", Query.And(Query.EQ("seriesId", seriesId.ToString()), Query.EQ("treeId", treeId.ToString()))).ToList();
                foreach (var tempItem in oldData)
                {
                    StorageData tempStorageData = new StorageData();
                    tempStorageData.Document = tempItem;
                    tempStorageData.Name = "XH_ProductDev_ValueItem";
                    tempStorageData.Type = StorageType.Delete;
                    tempStorageData.Query = Query.EQ("itemId", tempItem.String("itemId"));
                    dataList.Add(tempStorageData);
                }
                result = dataOp.BatchSaveStorageData(dataList);
                if (result.Status == Status.Successful)
                {
                    foreach (var tempItem in sourceData.OrderBy(x => x.String("nodeKey")))
                    {
                        var item = new BsonDocument();
                        item.Add("seriesId", seriesId.ToString());
                        item.Add("treeId", treeId.ToString());
                        item.Add("name", tempItem.String("name"));
                        item.Add("itemType", tempItem.String("itemType"));
                        var nodePid = "0";
                        var oldNodePid = tempItem.String("nodePid");
                        if (itemIdMapDC.ContainsKey(oldNodePid))
                        {
                            nodePid = itemIdMapDC[oldNodePid];
                        }
                        item.Add("nodePid", nodePid);
                        result = dataOp.Insert("XH_ProductDev_ValueItem", item);
                        if (result.Status == Status.Successful)
                        {
                            itemIdMapDC.Add(tempItem.String("itemId"), result.BsonInfo.String("itemId"));//记录树形对应节点的主键对
                        }
                    }
                }
            }
            if (result.Status == Status.Successful)
            {
                json.Message = "导入成功";
                json.Success = true;
            }
            else
            {
                json.Message = "导入失败!";
                json.Success = false;

            }
            return Json(json);
        }

        /// <summary>
        /// 导出产品系列配置项于模板
        /// </summary>
        /// <param name="seriesId"></param>
        /// <param name="treeId"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public ActionResult ExportConfigItemToTemplate(int seriesId, int treeId, int itemType, string name)
        {
            InvokeResult result = new InvokeResult();
            PageJson json = new PageJson();

            //获取对应产品系列下的对应价值树,对应类型的所有配置项 
            List<BsonDocument> sourceItemList = dataOp.FindAllByQuery("ProductConfigItem", Query.And(
                Query.EQ("seriesId", seriesId.ToString()),
                Query.EQ("treeId", treeId.ToString()),
                Query.EQ("itemType", itemType.ToString())
                )).ToList();

            #region 构建配置项数据
            //将所有配置项保存至模板,除去根节点和多余字段
            //需要字段为: name ,nodePid
            BsonArray newItemArray = new BsonArray();
            Dictionary<int, int> itemMappingDic = new Dictionary<int, int>();   //新旧价值项Id对应(旧Id,新Id)

            BsonDocument rootItem = sourceItemList.Where(t => t.Int("nodePid") == 0).FirstOrDefault();

            itemMappingDic.Add(rootItem.Int("itemId"), 0);

            int i = 0;

            foreach (var tempItem in sourceItemList.Where(t => t.Int("nodePid") > 0).OrderBy(t => t.String("nodeKey")))
            {
                i++;

                BsonDocument tempBson = new BsonDocument();

                tempBson.Add("id", i);
                tempBson.Add("name", tempItem.String("name"));
                tempBson.Add("itemType", tempItem.String("itemType"));
                tempBson.Add("dataKey", tempItem.String("dataKey"));

                if (tempItem.Int("nodePid") == 0)
                {
                    tempBson.Add("nodePid", 0);
                }
                else if (itemMappingDic.ContainsKey(tempItem.Int("nodePid")) == true)
                {
                    tempBson.Add("nodePid", itemMappingDic[tempItem.Int("nodePid")]);
                }

                itemMappingDic.Add(tempItem.Int("itemId"), i);

                newItemArray.Add(tempBson);
            }
            #endregion

            if (newItemArray.Count > 0)
            {
                BsonDocument template = new BsonDocument()  //模板记录
                    .Add("seriesId", seriesId.ToString())
                    .Add("treeId", treeId.ToString())
                    .Add("itemType", itemType.ToString())
                    .Add("name", name)
                    .Add("itemInfo", newItemArray);

                result = dataOp.Insert("ProductItemTemplate", template);

                json = TypeConvert.InvokeResultToPageJson(result);
            }
            else
            {
                json.Message = "无法导出只有根节点的配置项列表！";
                json.Success = false;
            }

            return Json(json);
        }

        /// <summary>
        /// 导出产品系列配置项
        /// </summary>
        /// <param name="seriesId"></param>
        /// <param name="treeId"></param>
        /// <param name="itemType"></param>
        /// <param name="templateId"></param>
        /// <returns></returns>
        public ActionResult ImportConfigItemToTemplate(int seriesId, int treeId, int itemType, int templateId)
        {
            InvokeResult result = new InvokeResult();
            PageJson json = new PageJson();

            BsonDocument template = dataOp.FindOneByKeyVal("ProductItemTemplate", "templateId", templateId.ToString());

            try
            {
                //清空现有配置项记录(除根节点外)
                result = dataOp.Delete("ProductConfigItem", Query.And(
                    Query.EQ("seriesId", seriesId.ToString()),
                    Query.EQ("treeId", treeId.ToString()),
                    Query.EQ("itemType", itemType.ToString()),
                    Query.NE("nodePid", "0")));

                if (result.Status == Status.Failed) throw new Exception(result.Message);

                Dictionary<int, int> itemMappingDic = new Dictionary<int, int>();   //新旧价值项Id对应(旧Id,新Id)

                BsonDocument rootItem = dataOp.FindOneByQuery("ProductConfigItem", Query.And(
                    Query.EQ("seriesId", seriesId.ToString()),
                    Query.EQ("treeId", treeId.ToString()),
                    Query.EQ("itemType", itemType.ToString()),
                    Query.EQ("nodePid", "0")));

                itemMappingDic.Add(0, rootItem.Int("itemId"));

                //逐条导入相关配置记录
                foreach (var tempItem in template["itemInfo"].AsBsonArray.OrderBy(t => t.ToBsonDocument().Int("id")))
                {
                    BsonDocument tempBson = new BsonDocument()
                        .Add("seriesId", seriesId.ToString())
                        .Add("treeId", treeId.ToString())
                        .Add("itemType", itemType.ToString())
                        .Add("name", tempItem.ToBsonDocument().String("name"))
                        .Add("dataKey", tempItem.ToBsonDocument().String("dataKey"));

                    if (itemMappingDic.ContainsKey(tempItem.ToBsonDocument().Int("nodePid")) == true)
                    {
                        tempBson.Add("nodePid", itemMappingDic[tempItem.ToBsonDocument().Int("nodePid")]);
                    }
                    else continue;

                    result = dataOp.Insert("ProductConfigItem", tempBson);
                    if (result.Status == Status.Failed) throw new Exception(result.Message);

                    itemMappingDic.Add(tempItem.ToBsonDocument().Int("id"), result.BsonInfo.Int("itemId"));
                }
            }
            catch (Exception e)
            {
                result.Status = Status.Failed;
                result.Message = e.Message;
            }

            json = TypeConvert.InvokeResultToPageJson(result);
            return Json(json);
        }


        #region 新城后台处理逻辑
        /// <summary>
        /// XC保存产品系列VI，目录
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        public ActionResult SaveVIDirectory(FormCollection saveForm)
        {
            var result = new InvokeResult();

            #region 构建数据
            string tbName = PageReq.GetForm("tbName");
            string queryStr = PageReq.GetForm("queryStr");
            string dataStr = PageReq.GetForm("dataStr");

            BsonDocument dataBson = new BsonDocument();

            if (dataStr.Trim() == "")
            {
                foreach (var tempKey in saveForm.AllKeys)
                {
                    if (tempKey == "tbName" || tempKey == "queryStr" || tempKey == "moveToId") continue;

                    dataBson.Add(tempKey, PageReq.GetForm(tempKey));
                }
            }
            else
            {
                dataBson = TypeConvert.ParamStrToBsonDocument(dataStr);
            }
            if (dataBson.String("dataKey") != "")
            {
                var name = saveForm["name"];
                var dataKey = ChangeChinesetoPinYin(name);
                dataBson.Set("dataKey", dataKey);
            }

            #endregion

            #region 保存数据
            result = dataOp.Save(tbName, queryStr != "" ? TypeConvert.NativeQueryToQuery(queryStr) : Query.Null, dataBson);
            #endregion

            if (result.Status == Status.Successful)
            {
                var tableRule = new TableRule(tbName);
                var primkey = tableRule.PrimaryKey;
                result.Message = "保存成功";
                var moveToId = saveForm["moveToId"];
                if (moveToId != "" && moveToId != "0")
                {
                    var tempResult = dataOp.Move(tbName, result.BsonInfo.String(primkey), moveToId, "next");
                    if (tempResult.Status == Status.Failed)
                    {
                        result.Message += ",移动目录失败";
                    }
                }
            }

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }
        /// <summary>
        /// XC保存户型成果
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        public ActionResult SavePSHouseResult(FormCollection saveForm)
        {
            var result = new InvokeResult();
            #region 构建数据
            string tbName = PageReq.GetForm("tbName");
            string queryStr = PageReq.GetForm("queryStr");
            string dataStr = PageReq.GetForm("dataStr");

            BsonDocument dataBson = new BsonDocument();

            if (dataStr.Trim() == "")
            {
                foreach (var tempKey in saveForm.AllKeys)
                {
                    if (tempKey == "tbName" || tempKey == "queryStr" || tempKey == "relRetIds") continue;

                    dataBson.Add(tempKey, PageReq.GetForm(tempKey));
                }
            }
            else
            {
                dataBson = TypeConvert.ParamStrToBsonDocument(dataStr);
            }

            var name = saveForm["name"];
            if (name != null)
            {
                var dataKey = ChangeChinesetoPinYin(name);
                dataBson.Add("dataKey", dataKey);
            }

            #endregion

            #region 保存数据
            result = dataOp.Save(tbName, queryStr != "" ? TypeConvert.NativeQueryToQuery(queryStr) : Query.Null, dataBson);
            #endregion


            var relRetIds = saveForm["relRetIds"];
            if (relRetIds != null)
            {
                var newForm = new FormCollection();
                newForm.Add("tbName", "PSHouseResultRelation");
                newForm.Add("relKey", "retId");
                newForm.Add("relValues", saveForm["relRetIds"]);
                newForm.Add("resultId", result.BsonInfo.String("resultId"));
                newForm.Add("seriesId", result.BsonInfo.String("seriesId"));
                SaveTableRelation(newForm);
            }

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        #region 保存风格（组合）SaveSeriesValType
        /// <summary>
        /// 保存风格（组合）
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        public ActionResult SaveSeriesValType(FormCollection saveForm)
        {
            var result = new InvokeResult();
            var tbName = PageReq.GetForm("tbName");
            var newAttrIds = PageReq.GetFormIntList("attrIds");
            var name = PageReq.GetForm("name");
            var queryStr = PageReq.GetForm("queryStr");
            var seriesId = PageReq.GetForm("seriesId");
            var treeId = PageReq.GetForm("treeId");
            var dataBson = new BsonDocument(){
                {"name",name},
                {"seriesId",seriesId},
                {"treeId",treeId}
            };
            var curTypeId = 0;
            if (queryStr != "")
            {
                var curType = dataOp.FindOneByQuery(tbName, TypeConvert.NativeQueryToQuery(queryStr));
                curTypeId = curType.Int("typeId");
            }

            #region 查重
            //查重
            var groups = dataOp.FindAllByQuery("ProductValueTypeAttrRel", Query.In("attrId", newAttrIds.Select(i => (BsonValue)i.ToString())))
                .GroupBy(i => i.Int("attrId")).ToList();
            if (groups.Count() > 0)
            {
                var tempTypeIds = groups.FirstOrDefault().Select(i => i.Int("typeId")).Distinct();
                foreach (var group in groups)
                {
                    tempTypeIds = tempTypeIds.Intersect(group.Select(i => i.Int("typeId"))).Distinct();
                }
                if (tempTypeIds.Count() > 0)
                {
                    var tempRels = dataOp.FindAllByQuery("ProductValueTypeAttrRel",
                            Query.And(
                                Query.In("typeId", tempTypeIds.Select(i => (BsonValue)i.ToString())),
                                Query.NotIn("attrId", newAttrIds.Select(i => (BsonValue)i.ToString()))
                            )
                        ).ToList();
                    foreach (var id in tempTypeIds)
                    {
                        var rel = tempRels.Where(i => i.Int("typeId") == id);
                        if (tempRels.Count() <= 0 && id != curTypeId)
                        {
                            result.Status = Status.Failed;
                            result.Message = "该组合已经存在";
                            return Json(TypeConvert.InvokeResultToPageJson(result));
                        }
                    }
                }
            }
            #endregion


            result = dataOp.Save(tbName, queryStr != "" ? TypeConvert.NativeQueryToQuery(queryStr) : Query.Null, dataBson);
            var typeId = result.BsonInfo.Int("typeId");
            //找出旧的风格(组合)与属性关联
            var allOldTypeAttrRels = dataOp.FindAllByQuery("ProductValueTypeAttrRel", Query.EQ("typeId", typeId.ToString())).ToList();
            var allOldAttrIds = allOldTypeAttrRels.Select(i => i.Int("attrId")).ToList();
            var addAttrIds = newAttrIds.Except(allOldAttrIds).ToList();//所有需要添加的属性id
            var delAttrIds = allOldAttrIds.Except(newAttrIds).ToList();//需要删除的
            var datalist = new List<StorageData>();
            foreach (var newAttrId in addAttrIds)
            {
                StorageData data = new StorageData();
                data.Name = "ProductValueTypeAttrRel";
                data.Document = new BsonDocument(){
                    {"typeId",typeId.ToString()},
                    {"attrId",newAttrId.ToString()}
                };
                data.Type = StorageType.Insert;
                datalist.Add(data);
            }
            if (delAttrIds.Any())
            {
                StorageData delData = new StorageData();
                delData.Name = "ProductValueTypeAttrRel";
                delData.Type = StorageType.Delete;
                delData.Query = Query.And(
                    Query.EQ("typeId", typeId.ToString()),
                    Query.In("attrId", delAttrIds.Select(i => (BsonValue)i.ToString()))
                    );
                datalist.Add(delData);
            }
            dataOp.BatchSaveStorageData(datalist);
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }
        #endregion

        #region 系列图纸编号
        /// <summary>
        /// XC保存系列图纸生成编号
        /// </summary>
        /// <param queryStr="">搜索条件以，隔开</param>
        /// <returns></returns>
        public ActionResult SDGetNumber()
        {
            var result = new InvokeResult();
            var today = DateTime.Now;
            var thisYear = today.Year;
            var returnStr = "XCDZ-BZH-" + thisYear.ToString("0000") + "-";
            var maxNum = 1;
            var maxNumObj = dataOp.FindOneByQuery("MaxNum", Query.And(
                Query.EQ("tbName", "SeriesDrawingResult"),
                Query.EQ("year", thisYear.ToString("0000"))
                ));
            if (maxNumObj == null)
            {
                var tempResult = dataOp.Insert("MaxNum", new BsonDocument().Add("tbName", "SeriesDrawingResult").Add("maxNumber", "1").Add("year", thisYear.ToString("0000")));
                if (tempResult.Status == Status.Successful)
                    maxNumObj = result.BsonInfo;
                else
                {
                    result.Message = "查找编号错误，请联系管理员";
                    result.Status = Status.Failed;
                    return Json(TypeConvert.InvokeResultToPageJson(result));
                }
            }
            else
            {
                maxNum = maxNumObj.Int("maxNumber") + 1;
                var tempResult = dataOp.Update("MaxNum", Query.EQ("numId", maxNumObj.String("numId")), new BsonDocument().Add("maxNumber", maxNum.ToString()));
                if (tempResult.Status == Status.Failed)
                {
                    result.Message = "更新编号错误，请联系管理员";
                    result.Status = Status.Failed;
                    return Json(TypeConvert.InvokeResultToPageJson(result));
                }
            }
            returnStr += maxNum.ToString("000");
            result.Status = Status.Successful;
            result.Message = returnStr;
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }
        #endregion

        /// <summary>
        /// 保存系列/项目库,价值项值与材料的关联 
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SaveXCSeriesItemMatRelInfo(FormCollection saveForm)
        {
            InvokeResult result = new InvokeResult();
            PageJson json = new PageJson();

            string tbName = PageReq.GetForm("tbName");    //表名
            string queryStr = PageReq.GetForm("queryStr");          //定位记录
            string matIds = PageReq.GetForm("matIds");              //材料Id列表
            int savaType = PageReq.GetParamInt("savaType"); //0为产品系列选择  1为项目库选择
            if (PageReq.GetForm("seriesId").Trim() == "" || PageReq.GetForm("treeId").Trim() == "" || PageReq.GetForm("combinationId").Trim() == "" || PageReq.GetForm("itemId").Trim() == "")
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
                var matList = _dataOp.FindAll("Material_Material").ToList();
                foreach (var matId in matIdList)
                {
                    BsonDocument tempBson = new BsonDocument();
                    var matobj = matList.Where(c => c.Text("matId") == matId).FirstOrDefault();
                    tempBson.Add("seriesId", dataBson.String("seriesId"));
                    tempBson.Add("itemId", dataBson.String("itemId"));
                    tempBson.Add("matId", matId);
                    if (matobj != null)
                    {
                        tempBson.Add("matName", matobj.Text("name"));
                    }
                    if (savaType == 1)
                    {
                        tempBson.Add("formProj", "1");
                    }
                    else
                    {
                        tempBson.Add("combinationId", dataBson.String("combinationId"));
                        tempBson.Add("treeId", dataBson.String("treeId"));
                    }
                    StorageData tempData = new StorageData();
                    tempData.Document = tempBson;
                    tempData.Name = tbName;
                    tempData.Type = StorageType.Insert;

                    allDataList.Add(tempData);
                }

                result = dataOp.BatchSaveStorageData(allDataList);
            }
            json = TypeConvert.InvokeResultToPageJson(result);

            return Json(json);
        }


        /// <summary>
        /// 保存系列,价值项值与材料的关联 
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult ChangeToMat()
        {
            PageJson json = new PageJson();
            InvokeResult result = new InvokeResult();
            int relId = PageReq.GetParamInt("relId");
            int matId = PageReq.GetParamInt("matId");
            bool isFromSeries = PageReq.GetParamBoolean("isFromSeries");//判断是否是从产品系列进行转化
            bool isFromProject = PageReq.GetParamBoolean("isFromProject");//判断是否是从项目库进行转化
            bool isFromStdResult = PageReq.GetParamBoolean("isFromStdResult");//判断是否是从专业库进行转化

            string relTbName = "ProductItemMatRelation";

            var matRelObj = dataOp.FindOneByQuery("ProductItemMatRelation", Query.EQ("relId", relId.ToString()));
            var matObj = dataOp.FindOneByQuery("Material_Material", Query.EQ("matId", matId.ToString()));
            if (isFromSeries)
            {
                matRelObj = dataOp.FindOneByQuery("ProductItemMatRelation", Query.EQ("relId", relId.ToString()));
                matObj = dataOp.FindOneByQuery("Material_Material", Query.EQ("matId", matId.ToString()));
                matRelObj.Set("status", "2");
                matObj.Set("status", "2");
            }
            if (matRelObj.IsNullOrEmpty() || matObj.IsNullOrEmpty())
            {
                result.Status = Status.Successful;
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            
            //matRelObj.Set("isSeries", "");
            //matObj.Set("isSeries", "");
            List<StorageData> dataList = new List<StorageData>();
            StorageData tempData1 = new StorageData();
            tempData1.Name = "ProductItemMatRelation";
            tempData1.Type = StorageType.Update;
            tempData1.Document = matRelObj;
            tempData1.Query = Query.EQ("relId", matRelObj.String("relId"));
            dataList.Add(tempData1);
            StorageData tempData2 = new StorageData();
            tempData2.Name = "Material_Material";
            tempData2.Type = StorageType.Update;
            tempData2.Document = matObj;
            tempData2.Query = Query.EQ("matId", matObj.String("matId"));
            dataList.Add(tempData2);
            result = dataOp.BatchSaveStorageData(dataList);
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        #endregion


        #region
        /// <summary>
        /// 新城项目库导入产品系列配置
        /// </summary>
        /// <returns></returns>
        public ActionResult SaveXCProjectCIlist()
        {
            PageJson json = new PageJson();
            InvokeResult result = new InvokeResult();

            #region 获取参数
            int projId = PageReq.GetFormInt("projId");              //项目Id
            var treeId = PageReq.GetFormInt("treeId");              //价值树
            var stageId = PageReq.GetFormInt("stageId");      //对应阶段
            var addType = PageReq.GetFormInt("addType");            //新增方式(1:导入项,2:导入项值）

            var seriesId = PageReq.GetFormInt("seriesId");          //产品系列
            var combId = PageReq.GetFormInt("combId");              //版本&组合

            #endregion

            #region 判断重复
            if (projId > 0 && treeId > 0 && stageId > 0 && combId > 0)    //如果任务Id与价值树Id大于0,判断是否已有重复价值树项
            {
                BsonDocument oldList = dataOp.FindOneByQuery("ProjectCITypeList", Query.And(
                    Query.EQ("projId", projId.ToString()),
                    Query.EQ("treeId", treeId.ToString()),
                    Query.EQ("stageId", stageId.ToString()),
                    Query.EQ("combinationId", combId.ToString())
                    ));

                if (oldList != null)
                {
                    json.Message = "已存在同组合的配置清单!";
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
                result = dataOp.Insert("ProjectCITypeList", new BsonDocument().Add("projId", projId.ToString())
                    .Add("treeId", treeId.ToString())
                    .Add("stageId", stageId.ToString())
                    .Add("seriesId", seriesId.ToString())
                    .Add("combinationId", combId.ToString())
                    );
                if (result.Status == Status.Failed) throw new Exception(result.Message);

                string newListId = result.BsonInfo.String("listId");
                #endregion


                Dictionary<int, int> itemMappingDic = new Dictionary<int, int>();   //新旧价值项Id对应(旧Id,新Id)
                List<string> notNeedColumn = new List<string>() { "_id", "valId", "treeId", "combinationId", "itemId", "createDate", "updateDate", "createUserId", "updateUserId", "underTable", "order" };

                #region 导入价值项
                List<BsonDocument> sourceItemList = new List<BsonDocument>();   //源价值项列表
                sourceItemList = dataOp.FindAllByQueryStr("ProductConfigItem", "seriesId=" + seriesId + "&treeId=" + treeId).ToList(); //读取产品系列配置项
                #region 循环导入
                foreach (var sourceItem in sourceItemList.OrderBy(t => t.String("nodeKey")))
                {
                    BsonDocument tempBson = new BsonDocument();
                    tempBson.Add("seriesId", seriesId.ToString());//项目价值项记录来源产品系列
                    tempBson.Add("treeId", treeId.ToString());//记录价值树Id
                    tempBson.Add("projectId", projId.ToString()); //项目Id
                    tempBson.Add("name", sourceItem.String("name"));
                    tempBson.Add("itemType", sourceItem.String("itemType"));
                    tempBson.Add("dataKey", sourceItem.String("dataKey"));
                    tempBson.Add("srcSerItemId", sourceItem.String("itemId")); //来源产品系列价值项
                    tempBson.Add("listId", newListId);
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
                        result = dataOp.Insert("ProjectConfigItem", tempBson);
                        if (result.Status == Status.Failed) throw new Exception(result.Message);

                        itemMappingDic.Add(sourceItem.Int("itemId"), result.BsonInfo.Int("itemId"));
                    }
                }
                #endregion

                #endregion

                if (addType >= 1)
                {
                    #region 导入价值项值

                    #region 取值
                    List<BsonDocument> sourceValueList = new List<BsonDocument>();   //源价值项列表
                    sourceValueList = dataOp.FindAllByQuery("ProductItemValue", Query.And(
                            Query.EQ("seriesId", seriesId.ToString()),
                            Query.EQ("treeId", treeId.ToString()),
                            Query.EQ("combinationId", combId.ToString())
                        )).ToList();
                    #endregion

                    #region 循环导入
                    List<StorageData> valDataList = new List<StorageData>();

                    foreach (var sourceVal in sourceValueList)
                    {
                        if (itemMappingDic.ContainsKey(sourceVal.Int("itemId")) == true)    //有对应项
                        {
                            BsonDocument tempBson = new BsonDocument();
                            tempBson.Add("itemId", itemMappingDic[sourceVal.Int("itemId")]);
                            tempBson.Add("srcServalId", sourceVal.Text("valId"));
                            tempBson.Add("listId", newListId);
                            foreach (var element in sourceVal.Elements)
                            {
                                if (notNeedColumn.Contains(element.Name) == false)
                                {
                                    tempBson.Add(element.Name, element.Value);
                                    if (addType == 2)
                                    {

                                        tempBson.Add("Proj_" + element.Name, element.Value); //把系列的价值项值复制给项目
                                    }
                                }
                            }

                            StorageData tempData = new StorageData();
                            tempData.Name = "ProjectItemValue";
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
                    sourctMatList = dataOp.FindAllByQuery("ProductItemMatRelation", Query.And(
                            Query.EQ("seriesId", seriesId.ToString()),
                            Query.EQ("treeId", treeId.ToString()),
                            Query.EQ("combinationId", combId.ToString())
                        )).ToList();
                    #endregion

                    #region 循环导入
                    List<StorageData> matDataList = new List<StorageData>();

                    foreach (var sourceMat in sourctMatList)
                    {
                        if (itemMappingDic.ContainsKey(sourceMat.Int("itemId")) == true)    //有对应项
                        {
                            BsonDocument tempBson = new BsonDocument();
                            tempBson.Add("itemId", itemMappingDic[sourceMat.Int("itemId")]);
                            tempBson.Add("formSer", "1");
                            foreach (var element in sourceMat.Elements)
                            {
                                if (notNeedColumn.Contains(element.Name) == false)
                                {
                                    tempBson.Add(element.Name, element.Value);
                                }
                            }

                            StorageData tempData = new StorageData();
                            tempData.Name = "ProjectItemMatRelation";
                            tempData.Document = tempBson;
                            tempData.Type = StorageType.Insert;
                            matDataList.Add(tempData);

                            if (addType == 2)
                            {
                                BsonDocument tempBson1 = new BsonDocument();
                                tempBson1.Add("itemId", itemMappingDic[sourceMat.Int("itemId")]);
                                tempBson1.Add("formProj", "1");
                                foreach (var element in sourceMat.Elements)
                                {
                                    if (notNeedColumn.Contains(element.Name) == false)
                                    {
                                        tempBson1.Add(element.Name, element.Value);
                                    }
                                }

                                StorageData tempData1 = new StorageData();
                                tempData1.Name = "ProjectItemMatRelation";
                                tempData1.Document = tempBson1;
                                tempData1.Type = StorageType.Insert;
                                matDataList.Add(tempData1);
                            }


                        }
                    }

                    result = dataOp.BatchSaveStorageData(matDataList);
                    if (result.Status == Status.Failed) throw new Exception(result.Message);
                    #endregion

                    #endregion

                    #region 导入成果关联

                    #region 取值
                    List<BsonDocument> sourctRetList = new List<BsonDocument>();    //源成果关联
                    sourctRetList = dataOp.FindAllByQuery("ProductDev_LineItemRetRelation", Query.And(
                            Query.EQ("seriesId", seriesId.ToString()),
                            Query.EQ("treeId", treeId.ToString()),
                            Query.EQ("combinationId", combId.ToString())
                        )).ToList();

                    #endregion

                    #region 循环导入
                    List<StorageData> retDataList = new List<StorageData>();

                    foreach (var sourceRet in sourctRetList) //循环导入价值项
                    {
                        if (itemMappingDic.ContainsKey(sourceRet.Int("itemId")) == true)    //有对应项
                        {
                            BsonDocument tempBson = new BsonDocument();
                            tempBson.Add("itemId", itemMappingDic[sourceRet.Int("itemId")]);
                            tempBson.Add("formSer", "1");
                            foreach (var element in sourceRet.Elements)
                            {
                                if (notNeedColumn.Contains(element.Name) == false)
                                {
                                    tempBson.Add(element.Name, element.Value);
                                }
                            }

                            StorageData tempData = new StorageData();
                            tempData.Name = "Project_LineItemRetRelation";
                            tempData.Document = tempBson;
                            tempData.Type = StorageType.Insert;
                            retDataList.Add(tempData);

                            if (addType == 2)
                            {
                                BsonDocument tempBson1 = new BsonDocument();
                                tempBson1.Add("itemId", itemMappingDic[sourceRet.Int("itemId")]);
                                tempBson1.Add("formProj", "1");
                                foreach (var element in sourceRet.Elements)
                                {
                                    if (notNeedColumn.Contains(element.Name) == false)
                                    {
                                        tempBson1.Add(element.Name, element.Value);
                                    }
                                }

                                StorageData tempData1 = new StorageData();
                                tempData1.Name = "Project_LineItemRetRelation";
                                tempData1.Document = tempBson1;
                                tempData1.Type = StorageType.Insert;
                                retDataList.Add(tempData1);
                            }
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
        #endregion
    }
}
