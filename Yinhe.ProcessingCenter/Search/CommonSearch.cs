using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.IO;
using Yinhoo.Framework.Configuration;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using MongoDB.Driver;

namespace Yinhe.ProcessingCenter.MvcFilters
{
    /// <summary>
    /// 通用搜索处理类
    /// </summary>
    public class CommonSearch  
    {
        private string _tbName = null;                          //操作表
        private IMongoQuery _query = null;                      //存储用查询
        private string _queryStr = null;                        //存储用查询
        private int _pageSize = SysAppConfig.PageSize;
        //private int _totalPages = 0;
        long _totalPages = 0;
        DataOperation dataOp;

        public long totalRecord
        {
            get { return _totalPages; }
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        public CommonSearch()
        {
            dataOp = new DataOperation();
        }
        /// <summary>
        /// 优先级最高
        /// </summary>
        /// <param name="table"></param>
        /// <param name="query"></param>
        public CommonSearch(string table, string queryStr)
        {
            _tbName = table;
            _queryStr = queryStr;
            dataOp = new DataOperation();
        }
        /// <summary>
        /// 第二优先级
        /// </summary>
        /// <param name="table"></param>
        /// <param name="query"></param>
        public CommonSearch(string table, IMongoQuery query)
        {
            _tbName = table;
            _query = query;
            dataOp = new DataOperation();
        }
       /// <summary>
        ///  第三优先级
       /// </summary>
       /// <param name="table"></param>
        public CommonSearch(string table)
        {
            _tbName = table;
            dataOp = new DataOperation();
        }
        
         

        
         /// <summary>
       /// 对列表进行分页搜索
       /// </summary>
       /// <param name="bsonDocQuery"></param>
       /// <returns></returns>
        public List<BsonDocument> Search(int pageIndex, out int pageCount, MongoCursor<BsonDocument> bsonDocQuery)
        {
            if (bsonDocQuery == null)
            {
                pageCount = 0; return null; 
            }
            _totalPages = bsonDocQuery.Count();
            pageCount = GetPageCount(_pageSize, _totalPages);
            if (pageIndex == 0)
                pageIndex = 1;
            return bsonDocQuery.Skip(_pageSize * ((int)pageIndex - 1)).Take(_pageSize).ToList();
        }

         public List<BsonDocument> Search(int pageIndex, out int pageCount, IQueryable<BsonDocument> bsonDocQuery)
         {
             if (bsonDocQuery == null)
             {
                 pageCount = 0; return null;
             }
             _totalPages = bsonDocQuery.Count();
             pageCount = GetPageCount(_pageSize, _totalPages);
             if (pageIndex == 0)
                 pageIndex = 1;
             return bsonDocQuery.Skip(_pageSize * ((int)pageIndex - 1)).Take(_pageSize).ToList();
         }
         /// <summary>
         /// 对列表进行过滤搜索
         /// </summary>
         /// <param name="bsonDocQuery">集合</param>
         /// <returns></returns>
         public List<BsonDocument> Search(int pageIndex, int pageSize, out int pageCount, MongoCursor<BsonDocument> bsonDocQuery)
         {
             _pageSize = pageSize;
             return Search(pageIndex,out pageCount, bsonDocQuery);
         }

         /// <summary>
         /// 单值查询
         /// </summary>
         /// <param name="pageIndex">当前页</param>
         /// <param name="pageCount">当前总页数</param>
         /// <param name="tableName">表名</param>
         /// <param name="key">字段</param>
         /// <param name="val">值</param>
         /// <returns></returns>
         public List<BsonDocument> Search(int pageIndex, out int pageCount, string tableName, string key, string val)
         {
            
             if (!string.IsNullOrEmpty(tableName))
             {
                 _tbName = tableName;
             }
             return Search(pageIndex, out pageCount, key, val);
         }
         /// <summary>
         /// 单值查询 
         /// </summary>
         /// <param name="pageIndex">当前页</param>
         /// <param name="pageCount">当前总页数</param>
         /// <param name="tableName">表名</param>
         /// <param name="key">字段</param>
         /// <param name="val">值</param>
         /// <returns></returns>
         public List<BsonDocument> Search(int pageIndex, out int pageCount, string key, string val)
         {
             MongoCursor<BsonDocument> hitQuery = null;
              if (!String.IsNullOrEmpty(_tbName))
             {
                 hitQuery = dataOp.FindAllByKeyVal(_tbName,key, val);
             }
             return Search(pageIndex, out pageCount, hitQuery);
         }

         #region 通用简单搜索
         /// <summary>
         /// 查询字符串,该方法必须先构造非默认构造函数，即必须至少传入表名
         /// public CommonSearch(string table, IMongoQuery  query)或者
         /// public CommonSearch(string table, IMongoQuery  query)
         /// </summary>
         /// <param name="pageIndex">当前页数</param>
         /// <param name="pageCount">总页数</param>
         /// <returns></returns>
         public List<BsonDocument> QuickSearch(int pageIndex, out int pageCount)
         {
             MongoCursor<BsonDocument> hitQuery = GetBsonDocument();
             return Search(pageIndex, out pageCount, hitQuery);
         }

         /// <summary>
         /// 查询字符串，返回name 字段匹配keyWord的集合
         /// </summary>
         /// <param name="pageIndex">当前页数</param>
         /// <param name="pageCount">总页数</param>
         /// <returns></returns>
         public List<BsonDocument> QuickSearch(int pageIndex, out int pageCount, string keyField, string keyWord)
         {
             MongoCursor<BsonDocument> hitQuery = GetBsonDocument();
               IQueryable<BsonDocument> resultQuery = null;
               if (hitQuery != null)
               {
                   resultQuery = hitQuery.AsQueryable();
                   if (!string.IsNullOrEmpty(keyWord))
                   {
                       resultQuery = resultQuery.Where(c => c.Text(keyField).Trim().Contains(keyWord.Trim()));
                   }
                   return Search(pageIndex, out pageCount, resultQuery);
               }
               return Search(pageIndex, out pageCount, hitQuery);
         }

         /// <summary>
         /// 查询字符串，返回name 字段匹配keyWord的集合
         /// </summary>
         /// <param name="pageIndex">当前页数</param>
         /// <param name="pageCount">总页数</param>
         /// <returns></returns>
         public List<BsonDocument> QuickSearch(int pageIndex, out int pageCount, List<string> keyFieldList, string keyWord)
         {
             var hitList = new List<BsonDocument>();
             MongoCursor<BsonDocument> hitQuery = GetBsonDocument();
             IQueryable<BsonDocument> resultQuery = null;
             if (hitQuery != null)
             {
                 resultQuery = hitQuery.AsQueryable();
                 if (!string.IsNullOrEmpty(keyWord))
                 {
                     foreach (var keyField in keyFieldList)
                     {

                         hitList.AddRange(Search(pageIndex, out pageCount, resultQuery.Where(c => c.Text(keyField).Trim().Contains(keyWord.Trim()))));

                     }
                 }
                 else
                 {
                     hitList.AddRange(Search(pageIndex, out pageCount, resultQuery));
                 }
                 var finalResult=hitList.Distinct();
                 pageCount = GetPageCount(_pageSize, _totalPages);
                 this._totalPages = finalResult.Count();
                 return finalResult.ToList();
             }
             return Search(pageIndex, out pageCount, hitQuery);
         }

          /// <summary>
         /// 查询字符串，返回name 字段匹配keyWord的集合
         /// </summary>
         /// <param name="pageIndex">当前页数</param>
         /// <param name="pageCount">总页数</param>
         /// <returns></returns>
         public List<BsonDocument> QuickSearchForFile(int pageIndex, out int pageCount, string keyWord)
         {
             MongoCursor<BsonDocument> hitQuery = GetBsonDocument();
             IQueryable<BsonDocument> resultQuery = null;
             if (hitQuery != null)
             {
                 resultQuery = hitQuery.AsQueryable();
                 if (!string.IsNullOrEmpty(keyWord))
                 {
                     resultQuery = resultQuery.Select(c => c.SourceBson("fileId"));
                     resultQuery = resultQuery.Where(c => (c.Text("name") + c.Text("ext")).Trim().Contains(keyWord.Trim()));
                 }
                 return Search(pageIndex, out pageCount, resultQuery);
             }
             return Search(pageIndex, out pageCount, hitQuery);
         }


        

         /// <summary>
         /// 查询字符串，返回name 字段匹配keyWord的集合
         /// </summary>
         /// <param name="pageIndex">当前页数</param>
         /// <param name="pageCount"></param>
         /// <param name="keyWord">关键字</param>
         /// <returns></returns>
         public List<BsonDocument> QuickSearch(int pageIndex, out int pageCount, string keyWord)
         {
             return QuickSearch(pageIndex, out pageCount, "name", keyWord);
         }

  
         #endregion

        /// <summary>
        /// 获取当前可以返回的数据列表
        /// </summary>
        /// <returns></returns>
         public MongoCursor<BsonDocument> GetBsonDocument()
        {
               MongoCursor<BsonDocument> hitQuery = null;
               if (!String.IsNullOrEmpty(_tbName))
               {
                   if (!string.IsNullOrEmpty(_queryStr))
                   {
                       hitQuery = dataOp.FindAllByQueryStr(_tbName, _queryStr);
                   }
                   else if (_query != null)
                   {
                       hitQuery = dataOp.FindAllByQuery(_tbName, _query);
                   }
                   else
                   {
                       hitQuery = dataOp.FindAll(_tbName);
                   }
               }
                return hitQuery;
        }

         /// <summary>
        /// 获取分页数
        /// </summary>
        /// <param name="pageSize"></param>
        /// <param name="recordCount"></param>
        /// <returns></returns>
        public   int GetPageCount(long pageSize, long recordCount)
        {
            long pageCount;
            if (recordCount % pageSize == 0)
                pageCount = recordCount / pageSize;
            else
                pageCount = recordCount / pageSize + 1;
            return (int)pageCount;
        }
         
    }
}
