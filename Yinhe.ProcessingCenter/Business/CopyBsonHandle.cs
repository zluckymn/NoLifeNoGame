using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Yinhe.ProcessingCenter.DataRule;
using MongoDB.Bson;
using Yinhe.ProcessingCenter;
using MongoDB.Driver.Builders;
///<summary>
///其他
///</summary>
namespace Yinhe.ProcessingCenter.Business
{
    /// <summary>
    /// bson复制处理类
    /// </summary>
    public class CopyBsonHandle
    {
        #region 构造函数
        /// <summary>
        /// 类私有变量
        /// </summary>
        private readonly DataOperation _ctx = null;

        /// <summary>
        /// 封闭当前默认构造函数
        /// </summary>
        public CopyBsonHandle()
        {
            _ctx = new DataOperation();
        }

        public CopyBsonHandle(DataOperation ctx)
        {
            _ctx = ctx;
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static CopyBsonHandle _()
        {
            return new CopyBsonHandle();
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static CopyBsonHandle _(DataOperation ctx)
        {
            return new CopyBsonHandle(ctx);
        }
        #endregion

        #region

        /// <summary>
        /// 拷贝列表信息
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="dataList"></param>
        /// <param name="primaryKey"></param>
        /// <param name="fields"></param>
        /// <returns></returns>
        public BsonDocument CopyBson(string tableName, BsonDocument bson, string primaryKey, string[] fields)
        {

            var record = new BsonDocument();
            foreach (var s in fields)
            {
                record.Add(s, bson.String(s));
            }
            record.Add("srcId", bson.String(primaryKey));
            return _ctx.Insert(tableName, record).BsonInfo;
        }

        /// <summary>
        /// 拷贝列表信息
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="dataList"></param>
        /// <param name="primaryKey"></param>
        /// <param name="fields"></param>
        /// <returns></returns>
        public BsonDocument CopyBson(string tableName, BsonDocument bson, string primaryKey, string[] fields, Dictionary<string, string> keyValueDict)
        {

            var record = new BsonDocument();
            foreach (var s in fields)
            {
                record.Add(s, bson.String(s));
            }
            foreach (var dict in keyValueDict)
            {
                record.Add(dict.Key, dict.Value);
            }
            record.Add("srcId", bson.String(primaryKey));
            return _ctx.Insert(tableName, record).BsonInfo;
        }



        /// <summary>
        /// 拷贝列表信息
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="dataList"></param>
        /// <param name="primaryKey"></param>
        /// <param name="fields"></param>
        /// <returns></returns>
        public List<BsonDocument> CopyBsons(string tableName, List<BsonDocument> dataList, string primaryKey, string[] fields)
        {
            var count = dataList.Count;
            var datas = new List<BsonDocument>(count);
            foreach (var d in dataList)
            {
                var record = new BsonDocument();
                foreach (var s in fields)
                {
                    record.Add(s, d.String(s));
                }
                record.Add("srcId", d.String(primaryKey));
                _ctx.Insert(tableName, record);
                datas.Add(record);
            }
            return datas;
        }

        /// <summary>
        /// 拷贝列表信息
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="dataList"></param>
        /// <param name="primaryKey"></param>
        /// <param name="fields"></param>
        /// <returns></returns>
        public List<BsonDocument> CopyBsons(string tableName, List<BsonDocument> dataList, string primaryKey, string[] fields, Dictionary<string, string> keyValueDict)
        {
            var count = dataList.Count;
            var datas = new List<BsonDocument>(count);
            foreach (var d in dataList)
            {
                var record = new BsonDocument();
                foreach (var s in fields)
                {
                    record.Add(s, d.String(s));
                    record.Add(s + "_bak", d.String(s));//初始化暂存区数据
                }
                foreach (var dict in keyValueDict)
                {
                    record.Add(dict.Key, dict.Value);
                }
                record.Add("srcId", d.String(primaryKey));
                _ctx.Insert(tableName, record);
                datas.Add(record);
            }
            return datas;
        }
        #endregion
    }
}
