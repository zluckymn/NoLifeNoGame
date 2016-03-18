using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using Yinhe.ProcessingCenter.DataRule;

namespace Yinhe.ProcessingCenter.Business.Device
{
    /// <summary>
    /// 设备通用处理类
    /// </summary>
    public class DeviceCommonBll
    {
        private DataOperation dataOp = null;

        string[] docExts = new string[] { ".xls", ".xlsx" };
        Dictionary<string, string> AttrValueDic = null;
        Dictionary<string, string> BrandDic = null;


        #region 构造函数
        /// <summary>
        /// 类私有变量
        /// </summary>
        
        /// <summary>
        /// 封闭当前默认构造函数
        /// </summary>
        public DeviceCommonBll()
        {
            dataOp = new DataOperation();
        }

        /// <summary>
        /// 封闭当前默认构造函数
        /// </summary>
        private DeviceCommonBll(DataOperation ctx)
        {
            dataOp = ctx;
        }


        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static DeviceCommonBll _()
        {
            return new DeviceCommonBll();
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static DeviceCommonBll _(DataOperation ctx)
        {
            return new DeviceCommonBll(ctx);
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

        #region 初始化品牌类型字典 -void InitBrandDic()
        /// <summary>
        /// 初始化品牌类型字典key:名称 value:Id
        /// </summary>
        private void InitBrandDic()
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
        private void InitAttrValueDic()
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

        

        #region 根据名称对应ID对字典排序 -void SortDicById(Dictionary<string, int> dic, string type)
        /// <summary>
        /// 根据名称对应ID对字典排序
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="type"></param>
        private void SortDicById(Dictionary<string, int> dic, string type)
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
        private Dictionary<string, string> GetExistDic()
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

        #region 获取字符数组字符的平均长度 +double GetWordsAvgLength(string[] strArr)
        /// <summary>
        /// 获取字符数组字符的平均长度
        /// </summary>
        /// <param name="strArr"></param>
        /// <returns></returns>
        public double GetWordsAvgLength(string[] strArr)
        {
            return strArr.Average(new Func<string, double>(c => GetStrLength(c)));
        } 
        #endregion

        public int GetWordsMaxLength(string[] strArr)
        {
            return strArr.Max(c => GetStrLength(c));
        }

        #region 计算字符串的实际长度 +int GetStrLength(string str)
        /// <summary>
        /// 计算字符串的实际长度
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public int GetStrLength(string str)
        {
            int length = str.Length;
            foreach (char ch in str)
            {
                if ((int)ch > 128)
                    length++;
            }
            return length;
        } 
        #endregion


     
    }
}
