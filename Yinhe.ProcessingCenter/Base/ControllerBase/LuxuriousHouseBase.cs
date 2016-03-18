using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Data;
using MongoDB.Bson;
using Yinhe.ProcessingCenter.DataRule;
using MongoDB.Driver.Builders;
using System.Text.RegularExpressions;
using System.IO;
using Yinhe.ProcessingCenter.Reports;
using System.Web;
using Yinhe.ProcessingCenter.Common;
using System.Threading;

///<summary>
///后台处理中心
///</summary>
namespace Yinhe.ProcessingCenter
{
    /// <summary>
    /// 市场调研-豪宅管理基类
    /// </summary>
    public partial class ControllerBase : Controller
    {

        #region 写入项目数据 -void WriteLuxuryData(DataTable dt, string importType)
        private void WriteLuxuryData(DataTable dt, string excelId, string importType)
        {
            List<BsonDocument> buildingList = dataOp.FindAll("LuxuriousHouse_Building").ToList();
            Dictionary<string, int> projValueIdDic = GetValueIdDic("LuxuriousHouse_Project");
            Dictionary<string, int> usageValueIdDic = GetValueIdDic("LuxuriousHouse_Usage");
            Dictionary<string, int> bTypeValueIdDic = GetValueIdDic("LuxuriousHouse_BuildingType");
            List<BsonDocument> projList = dataOp.FindAll("LuxuriousHouse_Building").ToList();
            List<StorageData> storageDataList = new List<StorageData>();
            #region 遍历表格的行
            DateTime start = DateTime.Now;
            foreach (DataRow dr in dt.Rows)
            {
                #region 插入新的项目、用途类型，房型
                string projName = GetDataColumnValue(dr, 0);
                string usageType = GetDataColumnValue(dr, 3);
                string usageName = GetDataColumnValue(dr, 4);//##
                string bTypeName = GetDataColumnValue(dr, 5);//##
                Dictionary<string, string> tempDic = new Dictionary<string, string>();
                tempDic.Add("LuxuriousHouse_Project", projName);
                tempDic.Add("LuxuriousHouse_Usage", usageName);
                tempDic.Add("LuxuriousHouse_BuildingType", bTypeName);
                InvokeResult invokeResult = new InvokeResult();
                foreach (KeyValuePair<string, string> kvp in tempDic)
                {
                    string tableName = kvp.Key;
                    string idColumnName = "";
                    Dictionary<string, int> sourceDic = null;
                    switch (tableName)
                    {
                        case "LuxuriousHouse_Project": idColumnName = "projId"; sourceDic = projValueIdDic; break;
                        case "LuxuriousHouse_Usage": idColumnName = "usageId"; sourceDic = usageValueIdDic; break;
                        case "LuxuriousHouse_BuildingType": idColumnName = "bTypeId"; sourceDic = bTypeValueIdDic; break;
                    }
                    if (!string.IsNullOrEmpty(kvp.Value) && !sourceDic.ContainsKey(kvp.Value))
                    {
                        BsonDocument newBson = new BsonDocument().Add("name", kvp.Value);
                        if (kvp.Key == "LuxuriousHouse_Usage") newBson.Set("usageType", usageType);
                        if (kvp.Key == "LuxuriousHouse_Project") newBson.Set("isShow", "1");
                        invokeResult = dataOp.Insert(tableName, newBson);
                        BsonDocument baseInfo = invokeResult.BsonInfo;
                        if (baseInfo != null)
                        {
                            sourceDic.Add(baseInfo.String("name", ""), baseInfo.Int(idColumnName));
                        }
                    }
                }
                #endregion
                #region 扫描楼房，插入或更新新的楼房
                string buildingName = GetDataColumnValue(dr, 1);
                string roomNumber = GetDataColumnValue(dr, 2);
                //判断记录是否是有效记录
                bool isLegal = !(string.IsNullOrEmpty(buildingName) || string.IsNullOrEmpty(projName) || string.IsNullOrEmpty(roomNumber)
                    || string.IsNullOrEmpty(bTypeName) || string.IsNullOrEmpty(usageName));
                if (!isLegal)
                {
                    continue;
                }
                StorageData storageData = new StorageData()
                {
                    Name = "LuxuriousHouse_Building",
                    Type = StorageType.None
                };
                int projId = projValueIdDic[projName];
                int usageId = usageValueIdDic[usageName];
                int bTypeId = bTypeValueIdDic[bTypeName];
                string completeDate = GetDataColumnValue(dr, 6).Trim();
                completeDate = (Regex.Replace(completeDate, @"\d*?:\d*?:\d*", "")).Trim();
                string area = GetDataColumnValue(dr, 7).Trim();
                string suiteCount = GetDataColumnValue(dr, 8).Trim();
                string avgPrice = GetDataColumnValue(dr, 9).Trim();
                string totalPrice = GetDataColumnValue(dr, 10).Trim();
                //判断是否要插入
                bool isInsert = projList.Where(c => c.Int("projId") == projId &&
                    c.String("name") == buildingName &&
                    c.String("roomNumber") == roomNumber &&
                    c.String("completeDate") == completeDate &&
                    c.String("area") == area)
                    .Count() == 0
                    && storageDataList.Where(c => c.Document.Int("projId") == projId &&
                        c.Document.String("name") == buildingName &&
                        c.Document.String("roomNumber") == roomNumber &&
                        c.Document.String("completeDate") == completeDate &&
                        c.Document.String("area") == area
                        ).Count() == 0;
                BsonDocument building = null;
                if (isInsert)
                {
                    //插入新的记录
                    building = new BsonDocument();
                    building.Add("projId", projId).Add("name", buildingName).Add("roomNumber", roomNumber).Add("excelId", excelId);
                    storageData.Type = StorageType.Insert;
                }
                else if (importType == "overwrite")
                {
                    StorageData tempData = storageDataList.Where(c => c.Document.Int("projId") == projId && c.Document.String("name") == buildingName && c.Document.String("roomNumber") == roomNumber).FirstOrDefault();
                    if (tempData != null)
                    {
                        //优先从storageDataList中取；
                        building = tempData.Document;
                    }
                    else
                    {
                        building = projList.Where(c => c.Int("projId") == projId && c.String("name") == buildingName && c.String("roomNumber") == roomNumber).FirstOrDefault();
                    }
                    storageData.Type = StorageType.Update;
                    storageData.Query = Query.And(Query.EQ("projId", projId.ToString()), Query.EQ("name", projName), Query.EQ("roomNumber", roomNumber));
                }
                if (storageData.Type == StorageType.Insert || storageData.Type == StorageType.Update)
                {
                    building.Set("usageId", usageId).Set("bTypeId", bTypeId).Set("completeDate", completeDate)
                        .Set("area", area).Set("suiteCount", suiteCount).Set("avgPrice", avgPrice).Set("totalPrice", totalPrice);
                    storageData.Document = building;
                    storageDataList.Add(storageData);
                }
                #endregion
            }
            #endregion
            dataOp.BatchSaveStorageData(storageDataList);
        }
        #endregion
        #region 获取表格某行某列的值 -string GetDataColumnValue(DataRow dr, int colIndex)
        /// <summary>
        /// 获取表格某行某列的值
        /// </summary>
        /// <param name="dr"></param>
        /// <param name="colIndex"></param>
        /// <returns></returns>
        private string GetDataColumnValue(DataRow dr, int colIndex)
        {
            return (dr[colIndex] == DBNull.Value ? "" : dr[colIndex].ToString());
        }
        #endregion
        #region 获取类型表ValueId字典 -Dictionary<string, int> GetValueIdDic(string tableName)
        /// <summary>
        /// 获取类型表ValueId字典
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        private Dictionary<string, int> GetValueIdDic(string tableName)
        {
            Dictionary<string, int> valueIdDic = new Dictionary<string, int>();
            string idColumnName = "";
            string valueColumnName = "";
            switch (tableName)
            {
                case "LuxuriousHouse_Project":
                    idColumnName = "projId";
                    valueColumnName = "name";
                    break;
                case "LuxuriousHouse_BuildingType":
                    idColumnName = "bTypeId";
                    valueColumnName = "name";
                    break;
                case "LuxuriousHouse_Usage":
                    idColumnName = "usageId";
                    valueColumnName = "name";
                    break;
            }
            List<BsonDocument> bsonList = dataOp.FindAll(tableName).ToList();
            foreach (BsonDocument bson in bsonList)
            {
                if (!valueIdDic.ContainsKey(bson.String(valueColumnName)))
                {
                    valueIdDic.Add(bson.String(valueColumnName), bson.Int(idColumnName));
                }
            }
            return valueIdDic;
        }
        #endregion

        #region 豪宅缩略图是否在首页显示修改以及上传
        public int IsShowImg(int projId, int isShow)
        {
            //int projId = PageReq.GetParamInt("projId");
            //int isShow = PageReq.GetParamInt("isShow");

            InvokeResult result = new InvokeResult() { Status = Status.Failed };

            result = dataOp.Update("LuxuriousHouse_Project", Query.EQ("projId", projId.ToString()), new BsonDocument().Add("isShow", isShow.ToString()));

            if (result.Status == Status.Failed)
                return -1;
            return 0;
        }
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult ProjectImgUpload()
        {
            var projId = PageReq.GetForm("Id");
            var imagePath = PageReq.GetForm("imagePath");

            var result = new InvokeResult() { Status = Status.Failed };

            var allImgFile = dataOp.FindAll("LuxuriousHouse_Project").ToList();
            var curMapObj = allImgFile.Find(p => p.String("projId") == projId);

            #region  img文件上传
            HttpFileCollectionBase filecollection = Request.Files;
            if (filecollection.Count <= 0)
            {
                //return -1;
                return RedirectToAction("/HighResidentialDevice", new { controller = "Equipment", error = -1, comId = 6 });
            }
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
            string attachdir = "/Content/LuxuryProjImage/";
            CreateSubWayMapFolder(Server.MapPath(attachdir));
            //if (curMapObj != null && !string.IsNullOrEmpty(curMapObj.Text("imagePath")))
            //{
            //    filename = curMapObj.Text("imagePath");
            //    filename = filename.Replace(" ", ""); //去掉content保存图片的文件名空格 
            //}
            //else
            //{
            filename = System.IO.Path.GetFileName(localname);
            filename = filename.Replace(" ", ""); //去掉content保存图片的文件名空格 
            var hasExistImgPath = allImgFile.Where(t => t.Text("imagePath").Trim() == imagePath.Trim()).Count() > 0;
            if (hasExistImgPath)//新增图片重名。需要改名字
            {
                filename = string.Format("{0}{1}-{2}", attachdir, curMapObj.String("name"), filename);
            }
            else
            {
                filename = string.Format("{0}{1}", attachdir, filename);
            }
            //}
            if (!Directory.Exists(Server.MapPath(Path.GetDirectoryName(filename))))
            {
                Directory.CreateDirectory(Server.MapPath(Path.GetDirectoryName(filename)));
            }
            System.IO.FileStream fs = new System.IO.FileStream(Server.MapPath(filename), System.IO.FileMode.Create, System.IO.FileAccess.Write);
            fs.Write(file, 0, file.Length);
            fs.Flush();
            fs.Close();
            #endregion
            result = dataOp.Update("LuxuriousHouse_Project", Query.EQ("projId", projId.ToString()), new BsonDocument().Add("imagePath", filename));
            //return RedirectToAction("/DeviceProjectAdd", new { controller = "Equipment", save = result.Status == Status.Successful ? 1 : 0 });
            //return RedirectToAction("/HighResidentialDevice", new { controller = "Equipment", comId = 7 });
            if (result.Status == Status.Failed)
            {
                //return -1;
                return RedirectToAction("/HighResidentialDevice", new { controller = "Equipment", error = -1, comId = 6 });
            }
            //return 0;
            return RedirectToAction("/HighResidentialDevice", new { controller = "Equipment", error = 1, comId = 6 });
        }
        #endregion


        #region  +string AddSaleInfo() 保存豪宅销售信息
        /// <summary>
        /// 编辑保存地铁图对象
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public string AddSaleInfo()
        {
            Response.ContentType = "text/html; charset=utf-8;";
            var json = new PageJson(false);
            var excelId = PageReq.GetForm("excelId");
            var title = PageReq.GetForm("title");
            var type = PageReq.GetForm("type");
            var date = PageReq.GetForm("date");
            string clientFilePath = PageReq.GetForm("filePath");

            var result = new InvokeResult() { Status = Status.Failed };
            var allExelList = dataOp.FindAll("LuxuriousHouse_ImportExcelFile").ToList();
            var curMapObj = allExelList.Where(c => c.String("excelId") == excelId).FirstOrDefault();
            string serverFilePath = string.Empty;


            var hasExist = allExelList.Where(c => c.String("excelId") != excelId && c.String("title") == title).Count() > 0;
            bool isSave = !hasExist;
            if (!isSave)
            {
                json.Message = "名字重复已存在!";
                return json.ToJsonString();
            }
            else
            {
                if (Request.Files.Count != 0)
                {
                    try
                    {
                        #region 保存文件到服务器
                        HttpPostedFileBase file = Request.Files[0];
                        string fileName = "Luxurious_" + title + Path.GetExtension(file.FileName);
                        string directory = @"\Content\ExcelFile\";
                        string filePath = Server.MapPath(directory);
                        if (!Directory.Exists(filePath))
                        {
                            Directory.CreateDirectory(filePath);
                        }
                        serverFilePath = Path.Combine(directory, fileName);
                        string fullFilePath = Path.Combine(filePath, fileName);
                        if (!CheckFileExt(fullFilePath, "doc"))
                        {
                            json.Message = "文件格式不正确！";
                            return json.ToJsonString();
                        }
                        file.SaveAs(fullFilePath);

                        curMapObj = new BsonDocument();
                        curMapObj.Add("title", title);
                        curMapObj.Add("type", type);
                        curMapObj.Add("date", date);
                        if (clientFilePath != "")
                            curMapObj.Add("clientPath", clientFilePath);
                        if (serverFilePath != "")
                            curMapObj.Add("serverPath", serverFilePath);
                        result = dataOp.Insert("LuxuriousHouse_ImportExcelFile", curMapObj);

                        #endregion

                        //写入数据
                        DataTable dt = GetDataTable(fullFilePath);
                        WriteLuxuryData(dt, result.BsonInfo.String("excelId"), "increment");
                    }
                    catch (Exception ex)
                    {
                        json.Message = ex.Message;
                        return json.ToJsonString();
                    }
                }
                else
                {
                    var updateBson = new BsonDocument();
                    updateBson.Add("title", title);
                    updateBson.Add("type", type);
                    updateBson.Add("date", date);
                    result = dataOp.Update(curMapObj, updateBson);
                }
                json.Success = true;
                json.Message = "保存成功";
            }
            return json.ToJsonString();
        }
        #endregion

        #region +string CreateExcel(string htmlCode, string sheetName)  创建Excel文件
        /// <summary>
        ///  创建Excel文件
        /// </summary>
        /// <param name="htmlCode"></param>
        /// <param name="sheetName"></param>
        /// <returns>生成文件的路径</returns>
        public string CreateExcel(string htmlCode, string sheetName, string fileName)
        {
            PageJson result = new PageJson();
            string fullFileName = string.Empty;
            try
            {
                if (string.IsNullOrEmpty(sheetName))
                    sheetName = "sheet1";
                htmlCode = Server.UrlDecode(htmlCode);
                sheetName = Server.UrlDecode(sheetName);
                fileName = Server.UrlDecode(fileName);
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

        #region 更新某一个类型的访问数

        public ActionResult UpdateViewCountByType(string modCode, string typeId, string projId)
        {
            InvokeResult result = new InvokeResult();
            if (modCode == "PROJECTVIEW")
            {
                var viewCountObj = dataOp.FindOneByQuery("ViewCount", Query.And(
                    Query.EQ("modCode", modCode),
                    Query.EQ("projId", projId),
                    Query.EQ("typeId", typeId)
                    ));
                if (viewCountObj == null)
                {
                    result = dataOp.Insert("ViewCount", new BsonDocument().Add("modCode", modCode).Add("projId", projId).Add("typeId", typeId).Add("count", 1));
                    return Json(TypeConvert.InvokeResultToPageJson(result));
                }
                else
                {
                    var viewCount = viewCountObj.Int("count");
                    result = dataOp.Update("ViewCount", Query.EQ("countId", viewCountObj.String("countId")), new BsonDocument().Add("count", viewCount + 1));
                    return Json(TypeConvert.InvokeResultToPageJson(result));
                }
            }
            else
            {
                var viewCountObj = dataOp.FindOneByQuery("ViewCount", Query.And(
                       Query.EQ("modCode", modCode),
                       Query.EQ("typeId", typeId)
                       ));
                if (viewCountObj == null)
                {
                    result = dataOp.Insert("ViewCount", new BsonDocument().Add("modCode", modCode).Add("typeId", typeId).Add("count", 1));
                    return Json(TypeConvert.InvokeResultToPageJson(result));
                }
                else
                {
                    var viewCount = viewCountObj.Int("count");
                    var viewCountId = viewCountObj.String("countId");

                    result = dataOp.Update("ViewCount", Query.EQ("countId", viewCountId), new BsonDocument().Add("count", ++viewCount));

                    return Json(TypeConvert.InvokeResultToPageJson(result));
                }
            }
        }


        #endregion

        #region 删除某个表格中的所有记录
        public ActionResult DelSaleRecords(string excelId)
        {
            InvokeResult result = dataOp.Delete("LuxuriousHouse_ImportExcelFile", Query.EQ("excelId", excelId));
            //删除没有记录的项目
            var existProjIds = dataOp.FindAll("LuxuriousHouse_Building").Select(c => c.GetValue("projId")).Distinct().ToArray(); //保存在所有存在记录的项目Id;
            result = dataOp.Delete("LuxuriousHouse_Project", Query.NotIn("projId", existProjIds));
            return Json(result);
        }
        #endregion
    }
}
