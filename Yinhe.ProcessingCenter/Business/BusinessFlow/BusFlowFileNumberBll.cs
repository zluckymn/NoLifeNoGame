using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using Yinhe.ProcessingCenter.DataRule;
using MongoDB.Bson;
using MongoDB.Driver.Builders;

namespace Yinhe.ProcessingCenter.BusinessFlow
{
    /// <summary>
    /// 档案编号处理类
    /// </summary>
    public class BusFlowFileNumberBll
    {
        #region 构造函数
        /// <summary>
        /// 类私有变量
        /// </summary>
        private DataOperation dataOp = null;

        private string tableName = "";
        /// <summary>
        /// 封闭当前默认构造函数
        /// </summary>
        private BusFlowFileNumberBll() 
        {
            dataOp = new DataOperation();
        }

        private BusFlowFileNumberBll(DataOperation _dataOp)
        {
            dataOp = _dataOp;
        }

        public static BusFlowFileNumberBll _()
        {
            return new BusFlowFileNumberBll();
        }

        public static BusFlowFileNumberBll _(DataOperation _dataOp)
        {
            return new BusFlowFileNumberBll(_dataOp);
        }
        #endregion

        #region 查询
        public BsonDocument FindById(string  Id)
        {
            var obj = dataOp.FindOneByKeyVal("BusFlowFileNumber", "fileNumId", Id);
            return obj;
        }

        /// <summary>
        /// 判断是否存在
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="referFieldName"></param>
        /// <param name="referFieldValue"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        public bool HasExist(string tableName, string referFieldName, string referFieldValue, string code)
        {
            var query = Query.EQ("code", code);
            var query1 = Query.EQ("tableName", tableName);
            var query2= Query.EQ("referFieldName", referFieldName);
            var query3 = Query.EQ("referFieldValue", referFieldValue);
            var objCount = dataOp.FindAllByQuery("BusFlowFileNumber", Query.And(query, query1, query2, query3)).Where(c => c.Int("status") == 0).Count();
            return objCount>0;
        }
        
        /// <summary>
        /// 获取档案最后编号
        /// </summary>
        /// <param name="flowId"></param>
        /// <param name="step"></param>
        /// <returns></returns>
        public int GetAvaibaleNum(string tableName, string referFieldName, string referFieldValue, string code, string year, string month,string day)
        {
           
            var query = Query.EQ("code", code);
            var query1 = Query.EQ("tableName", tableName);
            var query2 = Query.EQ("year", year);
            var query3 = Query.EQ("month", month);
            var query4 = Query.EQ("day", day);
            var curResult = dataOp.FindAllByQuery("BusFlowFileNumber", Query.And(query, query1, query2, query3, query4)).Where(c => c.Int("status") == 0).ToList();
            var maxNum = 0;
            if (curResult.Count() > 0)
            {
                maxNum = curResult.Max(c => c.Int("num"));
            }
            return maxNum+1;
         }
 
 
        #endregion


        #region 操作

        /// <summary>
        /// 生成最新档案编号SJBG-201402-003
        /// </summary>
        /// <param name="flowId"></param>
        /// <param name="step"></param>
        /// <returns></returns>
        public InvokeResult GenerateFileNum(string tableName, string referFieldName, string referFieldValue, string code)
        {  
            var year=DateTime.Now.Year.ToString();
            var day=DateTime.Now.Day.ToString();
            var month = DateTime.Now.Month.ToString();
            var num = GetAvaibaleNum(tableName, referFieldName, referFieldValue, code, year,month, day).ToString();
            var fileNumCode = string.Format("{0}-{1}-{2}", code, DateTime.Now.ToString("yyyyMM"), num.PadLeft(3, '0'));
            var addBson = new BsonDocument();
            var saveBsonDocumentList = new List<StorageData>();
            StorageData tempData = new StorageData();
            tempData.Name = "BusFlowFileNumber";
            BsonDocument dataBson = new BsonDocument();
            dataBson.Add("tableName", tableName);
            dataBson.Add("referFieldName", referFieldName);
            dataBson.Add("referFieldValue", referFieldValue);
            dataBson.Add("num", num);
            dataBson.Add("code", code);
            dataBson.Add("year", year);
            dataBson.Add("month", month);
            dataBson.Add("day", day);
            dataBson.Add("fileNumCode", fileNumCode);
            tempData.Type = StorageType.Insert;
            tempData.Document = dataBson;
            saveBsonDocumentList.Add(tempData);
            var result = dataOp.BatchSaveStorageData(saveBsonDocumentList);
            return result;
        }
        #endregion

    }

}
