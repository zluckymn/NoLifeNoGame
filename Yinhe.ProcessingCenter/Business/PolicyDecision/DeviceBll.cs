using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using System.Data;
using Yinhe.ProcessingCenter.DataRule;
using System.Data.OleDb;
using MongoDB.Driver;
using System.Web;
using System.Text.RegularExpressions;
using System.IO;
using MongoDB.Driver.Builders;

namespace Yinhe.ProcessingCenter.Business.PolicyDecision
{
    public class DeviceBll
    {
        #region 构造函数
        /// <summary>
        /// 类私有变量
        /// </summary>
        public DataOperation dataOp = null;

        /// <summary>
        /// 封闭当前默认构造函数
        /// </summary>
        public DeviceBll()
        {
            dataOp = new DataOperation();
        }

        public DeviceBll(DataOperation dataOp)
        {
            dataOp = dataOp;
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static DeviceBll _()
        {
            return new DeviceBll();
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static DeviceBll _(DataOperation dataOp)
        {
            return new DeviceBll(dataOp);
        }
        #endregion

        string[] docExts = new string[] { ".xls", ".xlsx" };
        Dictionary<string, string> AttrValueDic = null;
        Dictionary<string, string> BrandDic = null;

        #region 判断文件后缀的合法性 -bool CheckFileExt(string fileName, string type)
        /// <summary>
        /// 判断文件后缀的合法性
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool CheckFileExt(string fileName, string type)
        {
            string[] targetExts = null;
            if (type == "doc")
            {
                targetExts = docExts;
            }
            return targetExts.Contains(Path.GetExtension(fileName));
        }
        #endregion

        
        #region 获取汇总表的sheetName -string GetSummarySheetName(List<string> sheetNames)
        /// <summary>
        /// 获取汇总表的sheetName
        /// </summary>
        /// <param name="sheetNames"></param>
        /// <returns></returns>
        public string GetSummarySheetName(List<string> sheetNames)
        {
            string sheetName = "";
            foreach (string name in sheetNames)
            {
                if (Regex.IsMatch(name, "总表"))
                {//保证优先匹配到”总表“
                    sheetName = name;
                    return sheetName;
                }
                else if (Regex.IsMatch(name, "总"))
                {
                    //如果没有"总表"关键字，则匹配"总"；遍历每一sheetName
                    sheetName = name;
                }
            }
            return sheetName;
        }
        #endregion

        #region 获取要插入数据库的dataTable +DataTable GetDataTable(string filePath)
        /// <summary>
        /// 获取要插入数据库的dataTable 
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public DataTable GetDataTable(string filePath)
        {
            DataTable dt = null;
            OleDbConnection conn = CreateDbConnection(filePath);
            conn.Open();
            dt = conn.GetOleDbSchemaTable(System.Data.OleDb.OleDbSchemaGuid.Tables, new object[] { null, null, null, "TABLE" });
            List<string> sheetNames = new List<string>();
            foreach (DataRow dr in dt.Rows)
            {
                sheetNames.Add(dr[2].ToString());
            }
            string sheetName = GetSummarySheetName(sheetNames);
            if (string.IsNullOrEmpty(sheetName))
            {
                throw new Exception("查不到汇总表");
            }
            DataTable targetDt = new DataTable();
            using (OleDbCommand cmd = new OleDbCommand())
            {
                cmd.CommandText = "select * from [" + sheetName + "]";
                cmd.Connection = conn;
                OleDbDataAdapter adt = new OleDbDataAdapter(cmd);
                adt.Fill(targetDt);
            }
            conn.Dispose();
            System.IO.File.Delete(filePath);
            return targetDt;
        } 
        #endregion

        

        #region 获取时间差 +double GetPassedMiliSecond(DateTime start)
        /// <summary>
        /// 获取时间间隔，毫秒为单位
        /// </summary>
        /// <param name="start"></param>
        /// <returns></returns>
        public double GetPassedMiliSecond(DateTime start)
        {
            TimeSpan timeSpan = DateTime.Now - start;
            return timeSpan.TotalMilliseconds;
        }
        #endregion

        #region 获取有效行数 +GetRowCount(DataTable table)
        /// <summary>
        /// 获取有效行数
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public int GetRowCount(DataTable table)
        {
            int count = 0;
            for (int col = 0; col < table.Columns.Count; col++)
            {
                int tempCount = 0;
                for (int row = 0; row < table.Rows.Count; row++)
                {
                    if (table.Rows[row][col] != DBNull.Value && table.Rows[row][col].ToString() != "")
                    {
                        tempCount = row + 1;
                    }
                }
                count = (tempCount >= count ? tempCount : count);
            }
            return count;
        }
        #endregion

        #region 初始化品牌类型字典 -void InitBrandDic()
        /// <summary>
        /// 初始化品牌类型字典key:名称 value:Id
        /// </summary>
        public void InitBrandDic()
        {
            BrandDic = new Dictionary<string, string>();
            MongoCursor<BsonDocument> brandList = dataOp.FindAll("Device_brand");
            foreach (BsonDocument brand in brandList)
            {
                string brandName = brand.String("name");
                string brandId = brand.String("brandId");
                try
                {//避免重复主键
                    BrandDic.Add(brandName, brandId);
                }
                catch { }
            }
        }
        #endregion

        #region 初始化属性类型值字典 -void InitAttrValueDic()
        /// <summary>
        /// 初始化属性类型值字典 key:名称 value:Id
        /// </summary>
        public void InitAttrValueDic()
        {
            AttrValueDic = new Dictionary<string, string>();
            MongoCursor<BsonDocument> attrValueList = dataOp.FindAll("Device_attrType");
            foreach (BsonDocument attrValue in attrValueList)
            {
                string attrValueName = attrValue.String("value");
                string attrId = attrValue.String("typeId");
                try
                {
                    AttrValueDic.Add(attrValueName, attrId);
                }
                catch { }
            }
        }
        #endregion

        #region 初始化数据库中品牌表 -void InitBrand()
        /// <summary>
        /// 初始化数据库中品牌表
        /// </summary>
        public void InitTable(string tableName)
        {
            string[] brands = new string[] { "A品牌", "B品牌", "C品牌", "D品牌", "E品牌", "F品牌" };
            string[] attValueTypes = new string[] { "不详", "双门", "单门", "净水", "直饮水", "普通", "自动", "妇洗", "按摩", "分体", "大中央", "VRV独立新风", "空调自带新风", "原装进口", "合资", "国产", "观光梯", "客梯", "普通", "IC卡", "指纹识别" };
            List<StorageData> storageDatas = new List<StorageData>();
            string[] dataArr = null;
            string columnName = "";
            if (tableName == "Device_brand")
            {
                dataArr = brands;
                columnName = "name";
            }
            else if (tableName == "Device_attrType")
            {
                dataArr = attValueTypes;
                columnName = "value";
            }
            foreach (string data in dataArr)
            {
                StorageData storageData = new StorageData();
                BsonDocument brand = new BsonDocument().Add(columnName, data);
                storageData.Name = tableName;
                storageData.Type = StorageType.Insert;
                storageData.Document = brand;
                storageDatas.Add(storageData);
            }
            dataOp.BatchSaveStorageData(storageDatas);
        }
        #endregion

        


        #region 写入项目属性 void WriteProjectAtt(DataTable dt,string importType)
        /// <summary>
        /// 写入项目属性
        /// </summary>
        /// <param name="dt"></param>
        public void WriteProjectAtt(DataTable dt, string importType)
        {
            int rowCount = GetRowCount(dt);
            List<BsonDocument> category = dataOp.FindAll("Device_category").ToList();
            List<BsonDocument> attList = dataOp.FindAll("Device_attribute").ToList();
            List<StorageData> storageDatas = new List<StorageData>();
            StorageData storageData = null;
            InvokeResult result = null;
            for (int colIndex = 4; colIndex < dt.Columns.Count; colIndex += 2)
            {
                string projName = dt.Columns[colIndex].ColumnName;
                BsonDocument project = dataOp.FindOneByKeyVal("Device_project", "name", projName);
                if (importType == "increment" && project != null)
                {
                    //跳过
                    continue;
                }
                string area = dt.Rows[0][colIndex].ToString();
                string buildingType = dt.Rows[1][colIndex].ToString();
                string price = dt.Rows[2][colIndex].ToString();
                string introduction = dt.Rows[3][colIndex].ToString();
                string projId = "";
                if (project == null)
                {
                    //插入
                    project = new BsonDocument();
                    project.Set("area", area).Set("buildingType", buildingType)
                        .Set("price", price).Set("introduction", introduction)
                        .Set("name", projName);
                    result = dataOp.Insert("Device_project", project);
                    projId = result.BsonInfo.String("projId");
                }
                else if (importType == "overwrite")
                {
                    //更新
                    projId = project.String("projId");
                    project.Set("area", area).Set("buildingType", buildingType).Set("price", price).Set("introduction", introduction);
                    dataOp.Update("Device_project", Query.EQ("projId", projId), project);
                }

                //添加或修改属性
                StorageData deviceAttData = null;
                for (int rowIndex = 5; rowIndex < rowCount; rowIndex++)
                {
                    deviceAttData = new StorageData()
                    {
                        Name = "Device_attribute",
                        Type = StorageType.None
                    };
                    //查找该单元格对应的设备类型
                    string isExist = dt.Rows[rowIndex][colIndex].ToString();
                    string attValue = "";
                    //读取下一行，判断是否为特殊属性
                    if (!string.IsNullOrEmpty(dt.Rows[rowIndex + 1][3].ToString()))
                    {
                        attValue = dt.Rows[rowIndex + 1][colIndex].ToString();
                    }
                    string brand = dt.Rows[rowIndex][colIndex + 1].ToString();
                    string attrValueId = "";
                    string brandId = "";
                    try
                    {
                        attrValueId = AttrValueDic[attValue];
                    }
                    catch { }
                    try
                    {
                        brandId = BrandDic[brand];
                    }
                    catch { }
                    string catPosition = GetAttCatPosition(category, rowCount, rowIndex);
                    //是否该行的属性值有效
                    bool isAttNull = string.IsNullOrEmpty(isExist) && string.IsNullOrEmpty(brand) && string.IsNullOrEmpty(attrValueId);
                    //数据库中是否已经存在该行属性
                    bool isExistAttRecord = attList.Where(c => c.String("projId") == projId && c.String("catPosition") == catPosition).FirstOrDefault() != null;
                    if (string.IsNullOrEmpty(catPosition))
                    {
                        catPosition = GetAttCatPosition(category, rowCount, rowIndex - 1);
                    }
                    if (string.IsNullOrEmpty(catPosition))
                    {
                        //跳过
                        continue;
                    }
                    else if (isAttNull && isExistAttRecord)
                    {
                        //删除
                        deviceAttData.Type = StorageType.Delete;
                        deviceAttData.Query = Query.And(Query.EQ("projId", projId), Query.EQ("catPosition", catPosition));
                    }
                    else
                    {
                        //插入或更新
                        BsonDocument deviceAtt = new BsonDocument();
                        deviceAtt.Set("typeId", attrValueId);
                        deviceAtt.Set("isExist", isExist);
                        deviceAtt.Set("brandId", brandId);
                        deviceAtt.Set("projId", projId);
                        deviceAtt.Set("catPosition", catPosition);
                        if (!isAttNull && !isExistAttRecord)
                        {
                            //插入
                            deviceAttData.Type = StorageType.Insert;

                        }
                        else if (!isAttNull && isExistAttRecord)
                        {
                            //更新
                            deviceAttData.Type = StorageType.Update;
                            deviceAttData.Query = Query.And(Query.EQ("projId", projId), Query.EQ("catPosition", catPosition));
                        }
                        deviceAttData.Document = deviceAtt;
                    }
                    if (deviceAttData.Type != StorageType.None)
                    {
                        storageDatas.Add(deviceAttData);
                    }
                    if (!string.IsNullOrEmpty(dt.Rows[rowIndex + 1][3].ToString()))
                    {
                        rowIndex++;
                    }
                }
            }
            dataOp.BatchSaveStorageData(storageDatas);
        }
        #endregion

        #region 获取某一行属性对应的设备类型Id -string GetAttCatPosition(List<BsonDocument> category, int rowCount, int rowIndex)
        /// <summary>
        /// 获取某一行属性对应的设备类型Id
        /// </summary>
        /// <returns></returns>
        public string GetAttCatPosition(List<BsonDocument> category, int rowCount, int rowIndex)
        {
            string position = "";
            for (int i = 2; i >= 0; i--)
            {
                string temp = GetPosition(rowCount, rowIndex, i);
                if (category.Where(c => c.String("position") == temp).FirstOrDefault() != null)
                {
                    position = temp;
                    break;
                }
            }
            return position;
        }
        #endregion

        #region 根据position获取在表格中的行索引 -int GetCatRowIndex(int catPosition, int rowCount)
        /// <summary>
        /// 根据position获取在表格中的行索引
        /// </summary>
        /// <param name="catId"></param>
        /// <param name="rowCount"></param>
        /// <returns></returns>
        public int GetCatRowIndex(int catPosition, int rowCount)
        {
            return catPosition % rowCount;
        }
        #endregion

        #region 计算某一单元格对应的 -string GetPosition(int rowCount, int rowIndex, int colIndex)
        /// <summary>
        /// 计算某一单元格对应的Position值
        /// </summary>
        /// <param name="rowCount"></param>
        /// <param name="rowIndex"></param>
        /// <param name="colIndex"></param>
        /// <returns></returns>
        public string GetPosition(int rowCount, int rowIndex, int colIndex)
        {
            return (rowIndex + rowCount * colIndex).ToString();
        }
        #endregion

        #region 获取某个节点的父节点position  +string GetParentPosition(DataTable dt, int rowCount, int rowIndex, int colIndex)
        /// <summary>
        /// 获取某个节点的父节点position
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="rowCount"></param>
        /// <param name="rowIndex"></param>
        /// <param name="colIndex"></param>
        /// <returns></returns>
        public string GetParentPosition(DataTable dt, int rowCount, int rowIndex, int colIndex)
        {
            string position = "0";
            if (colIndex != 0)
            {
                for (int i = rowIndex; i >= 5; i--)
                {
                    if (dt.Rows[i][colIndex - 1] != DBNull.Value && dt.Rows[i][colIndex - 1].ToString() != "")
                    {
                        position = GetPosition(rowCount, i, colIndex - 1);
                        break;
                    }
                }
            }
            return position;
        }
        #endregion

        #region 获取品牌或者属性类型字典  key:Id value:名称 +Dictionary<string, string> GetIdNameDic(string tableName)
        /// <summary>
        /// 获取品牌或者属性类型字典  key:Id value:名称
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public Dictionary<string, string> GetIdNameDic(string tableName)
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            Dictionary<string, string> oriDic = null;
            if (tableName == "Device_attrType")
            {
                if (AttrValueDic == null)
                {
                    InitAttrValueDic();
                }
                oriDic = AttrValueDic;
            }
            else if (tableName == "Device_brand")
            {
                if (BrandDic == null)
                {
                    InitBrandDic();
                }
                oriDic = BrandDic;
            }
            foreach (KeyValuePair<string, string> kvp in oriDic)
            {
                dic.Add(kvp.Value, kvp.Key);
            }
            return dic;
        }
        #endregion

        #region 创建数据库连接 OleDbConnection CreateDbConnection(string dataSource)
        public static OleDbConnection CreateDbConnection(string dataSource)
        {
            string connStr = "Provider=Microsoft.Ace.OleDb.12.0;Data Source='" + dataSource + "';Extended Properties='Excel 12.0;HDR=YES;IMEX=1'";
            return new OleDbConnection(connStr);
        }
        #endregion

        #region 统计品牌或者设备类型  +Dictionary<string, int> CountBrandOrType(string catId, string type)
        /// <summary>
        /// 统计品牌或者设备类型
        /// </summary>
        /// <param name="catId"></param>
        /// <returns></returns>
        public Dictionary<string, int> CountBrandOrType(string catId, string type)
        {

            Dictionary<string, int> dic = new Dictionary<string, int>();
            string tableName = "";
            string columnName = "";
            if (type == "brand")
            {
                tableName = "Device_brand";
                columnName = "brandId";
            }
            else if (type == "deviceType")
            {
                tableName = "Device_attrType";
                columnName = "typeId";
            }
            else
            {
                return dic;
            }
            Dictionary<string, string> oriDic = GetIdNameDic(tableName);
            BsonDocument catNode = dataOp.FindOneByKeyVal("Device_category", "catId", catId);
            if (catNode != null)
            {
                string catPosition = catNode.String("position");
                List<BsonDocument> attList = dataOp.FindAllByKeyVal("Device_attribute", "catPosition", catPosition).ToList();
                foreach (BsonDocument att in attList)
                {
                    string id = att.String(columnName);
                    if (string.IsNullOrEmpty(id))
                    {
                        continue;
                    }
                    string name = "";
                    try
                    {
                        name = oriDic[id];
                    }
                    catch
                    {
                        continue;
                    }
                    if (string.IsNullOrEmpty(name))
                    {
                        continue;
                    }
                    if (dic.ContainsKey(name))
                    {
                        dic[name]++;
                    }
                    else
                    {
                        dic.Add(name, 1);
                    }
                }
            }
            //排序
            SortDicById(dic, type);
            return dic;
        }
        #endregion

        #region 根据名称对应ID对字典排序 -void SortDicById(Dictionary<string, int> dic, string type)
        /// <summary>
        /// 根据名称对应ID对字典排序
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="type"></param>
        public void SortDicById(Dictionary<string, int> dic, string type)
        {
            List<KeyValuePair<string, int>> keyValueList = new List<KeyValuePair<string, int>>(dic);
            Dictionary<string, string> oriDic = null;
            if (type == "brand")
            {
                if (BrandDic == null)
                {
                    InitBrandDic();
                }
                oriDic = BrandDic;
            }
            else if (type == "deviceType")
            {
                if (AttrValueDic == null)
                {
                    InitAttrValueDic();
                }
                oriDic = AttrValueDic;
            }
            else if (type == "exist")
            {
                oriDic = GetExistDic();
            }
            keyValueList.Sort(delegate(KeyValuePair<string, int> s1, KeyValuePair<string, int> s2)
            {
                return Convert.ToInt32(oriDic[s1.Key]) - Convert.ToInt32(oriDic[s2.Key]);
            });
            dic.Clear();
            foreach (KeyValuePair<string, int> kvp in keyValueList)
            {
                dic.Add(kvp.Key, kvp.Value);
            }
        }

        #endregion

        #region  获取有，无，不详字典 -Dictionary<string, string> GetExistDic()
        /// <summary>
        /// 获取有，无，不详字典
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string> GetExistDic()
        {
            string[] keyWords = new string[] { "有", "无", "不详" };
            string[] values = new string[] { "1", "2", "3" };
            Dictionary<string, string> existDic = new Dictionary<string, string>();
            for (int i = 0; i < keyWords.Length; i++)
            {
                existDic.Add(keyWords[i], values[i]);
            }
            return existDic;
        }
        #endregion

        #region 统计某设备有无不详的信息  +Dictionary<string, int> CountExist(string catId)
        /// <summary>
        /// 统计某设备有无不详信息
        /// </summary>
        /// <param name="catId"></param>
        /// <returns></returns>
        public Dictionary<string, int> CountExist(string catId)
        {
            Dictionary<string, int> dic = new Dictionary<string, int>();
            string[] keyWords = new string[] { "有", "无", "不详" };
            BsonDocument catNode = dataOp.FindOneByKeyVal("Device_category", "catId", catId);
            if (catNode != null)
            {
                string catPosition = catNode.String("position");
                List<BsonDocument> attList = dataOp.FindAllByKeyVal("Device_attribute", "catPosition", catPosition).ToList();
                foreach (BsonDocument att in attList)
                {
                    string existInfo = att.String("isExist");
                    if (string.IsNullOrEmpty(existInfo))
                    {
                        continue;
                    }
                    if (keyWords.Contains(existInfo))
                    {
                        if (dic.ContainsKey(existInfo))
                        {
                            dic[existInfo]++;
                        }
                        else
                        {
                            dic.Add(existInfo, 1);
                        }
                    }
                }
            }
            SortDicById(dic, "exist");
            return dic;
        }
        #endregion

        #region 获取最大值 +int GetMax(int n1, params int[] numbers)
        /// <summary>
        /// 获取最大值
        /// </summary>
        /// <param name="n1"></param>
        /// <param name="numbers"></param>
        /// <returns></returns>
        public int GetMax(int n1, params int[] numbers)
        {
            int max = numbers.Max();
            return max > n1 ? max : n1;
        }
        #endregion

        #region 获取在Device_attribute中有记录的设备类型 List<BsonDocument> GetProjectDevices()
        /// <summary>
        /// 获取在Device_attribute中有记录的设备类型
        /// </summary>
        /// <returns></returns>
        public List<BsonDocument> GetProjectDevices()
        {
            List<string> catPositionList = new List<string>();
            List<BsonDocument> attList = dataOp.FindAll("Device_attribute").ToList();
            List<BsonDocument> oriCategory = dataOp.FindAll("Device_category").ToList();
            List<BsonDocument> tarCategory = new List<BsonDocument>();
            foreach (BsonDocument att in attList)
            {
                string catPosition = att.String("catPosition");
                if (!catPositionList.Contains(catPosition))
                {
                    catPositionList.Add(catPosition);
                }
            }

            foreach (string catPosition in catPositionList)
            {
                BsonDocument catNode = oriCategory.Where(c => c.String("position") == catPosition).FirstOrDefault();
                if (catNode != null)
                {
                    tarCategory.Add(catNode);
                }
            }
            return tarCategory;
        }
        #endregion

        #region 获取所有的项目地区 +List<string> GetProjectAreas()
        /// <summary>
        /// 获取所有的项目地区
        /// </summary>
        /// <returns></returns>
        public List<string> GetProjectAreas()
        {
            List<string> areas = new List<string>();
            List<BsonDocument> projects = dataOp.FindAll("Device_project").ToList();
            foreach (BsonDocument project in projects)
            {
                string area = project.String("area", "").Trim();
                if (!areas.Contains(area))
                {
                    areas.Add(area);
                }
            }
            return areas;
        }
        #endregion

        #region 获取所有楼房类型 +List<string> GetProjectBuildingTypes()
        /// <summary>
        /// 获取所有楼房类型
        /// </summary>
        /// <returns></returns>
        public List<string> GetProjectBuildingTypes()
        {
            List<string> bTypes = new List<string>();
            List<BsonDocument> projects = dataOp.FindAll("Device_project").ToList();
            foreach (BsonDocument project in projects)
            {
                string bType = project.String("buildingType", "").Trim();
                if (!bTypes.Contains(bType))
                {
                    bTypes.Add(bType);
                }
            }
            return bTypes;
        }
        #endregion

        #region 根据地区和房型或取项目列表 +List<BsonDocument> GetProjectList(string area, string bType)
        /// <summary>
        /// 根据地区和房型或取项目列表
        /// </summary>
        /// <param name="area"></param>
        /// <param name="bType"></param>
        /// <returns></returns>
        public List<BsonDocument> GetProjectList(string area, string bType)
        {
            if (area == "全部")
            {
                area = "";
            }
            if (bType == "全部")
            {
                bType = "";
            }
            IMongoQuery query = null;
            if (string.IsNullOrEmpty(area) && string.IsNullOrEmpty(bType))
            {
                query = Query.Exists("projId", true);
            }
            else if (!string.IsNullOrEmpty(area) && string.IsNullOrEmpty(bType))
            {
                query = Query.EQ("area", area);
            }
            else if (!string.IsNullOrEmpty(bType) && string.IsNullOrEmpty(area))
            {
                query = Query.EQ("buildingType", bType);
            }
            else
            {
                query = Query.And(Query.EQ("area", area), Query.EQ("buildingType", bType));
            }
            List<BsonDocument> projList = dataOp.FindAllByQuery("Device_project", query).ToList();
            return projList;
        }
        #endregion
    }
}
