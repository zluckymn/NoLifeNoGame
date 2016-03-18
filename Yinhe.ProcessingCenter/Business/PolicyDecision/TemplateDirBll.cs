using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using Yinhe.ProcessingCenter.DataRule;
using MongoDB.Bson;
using Yinhe.ProcessingCenter;
using MongoDB.Driver.Builders;

namespace Yinhe.ProcessingCenter.Business.PolicyDecision
{
    /// <summary>
    /// 模板目录处理类
    /// </summary>
    public class TemplateDirBll
    {
                #region 构造函数
        /// <summary>
        /// 类私有变量
        /// </summary>
        private readonly DataOperation _ctx = null;

        /// <summary>
        /// 封闭当前默认构造函数
        /// </summary>
        public TemplateDirBll()
        {
            _ctx = new DataOperation();
        }

        public TemplateDirBll(DataOperation ctx)
        {
            _ctx = ctx;
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static TemplateDirBll _()
        {
            return new TemplateDirBll();
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static TemplateDirBll _(DataOperation ctx)
        {
            return new TemplateDirBll(ctx);
        }
        #endregion
        #region 操作

        /// <summary>
        /// 拷贝列表信息
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="dataList"></param>
        /// <param name="primaryKey"></param>
        /// <param name="fields"></param>
        /// <returns></returns>
        public List<BsonDocument> CopyTable(string tableName,List<BsonDocument> dataList, string primaryKey, string[] fields)
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
        public List<BsonDocument> CopyTable(string tableName, List<BsonDocument> dataList, string primaryKey, string[] fields, Dictionary<string, string> keyValueDict)
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

        /// <summary>
        /// 复制树形结构目录
        /// </summary>
        /// <param name="tableName">树形结构的表名称</param>
        /// <param name="dataList">被复制的树型结构列表</param>
        /// <param name="fields">要复制的字段</param>
        /// <param name="keyValueDict">固定加入的值：比如树形目录挂在项目下可能就要加入项目的主键Id</param>
        /// <returns></returns>
        public List<BsonDocument> CopyTreeTable(string tableName, List<BsonDocument> dataList, string[] fields, Dictionary<string, string> keyValueDict)
        {
            var count = dataList.Count;
            var datas = new List<BsonDocument>(count);
            var tableRule = new TableRule(tableName);
            var primaryKey = tableRule.PrimaryKey;
            var keyDict = new Dictionary<string, string>();
            foreach (var d in dataList.OrderBy(s=>s.String("nodeKey")))
            {
                var record = new BsonDocument();
                foreach (var s in fields)
                {
                    record.Add(s, d.String(s));
                }

                foreach (var dict in keyValueDict)
                {
                    record.Add(dict.Key, dict.Value);
                }

                var keyValue = d.String(primaryKey);
                record.Add("srcId", keyValue);//记录被复制节点的主键值
                var oldNodePid = d.String("nodePid");//获取被复制节点的父节点id；
                var nodePid = "0";
               
                if (keyDict.ContainsKey(oldNodePid))
                {
                    nodePid = keyDict[oldNodePid];
                }
                record.Add("nodePid", nodePid);
                var result =  _ctx.Insert(tableName, record);
                if (result.Status == Status.Successful)
                {
                    keyDict.Add(d.String(primaryKey), result.BsonInfo.String(primaryKey));//记录树形对应节点的主键对
                }
                datas.Add(record);
            }
            return datas;
        }

        /// <summary>
        /// 从模板复制目录
        /// </summary>
        /// <param name="newDirList"></param>
        /// <param name="oldDirList"></param>
        /// <param name="nodePid"></param>
        /// <param name="parent"></param>
        /// <param name="projId"></param>
        /// <param name="tableName"></param>
        /// <param name="dirIfno"></param>
        public void CopyGeneralDir(List<BsonDocument> newDirList, List<BsonDocument> oldDirList, int nodePid, BsonDocument parent, int projId, string tableName, BsonDocument dirIfno)
        {
            var dir = oldDirList.Where(m => m.Int("nodePid") == nodePid).ToList();
            if (dir.Any())
                return;
            var tableEntity = new TableRule(tableName + "Dir");
            var tableEntity1 = new TableRule(tableName);//模板或项目目录数据项
            string primaryKey;
            string primaryKey1;
            if (tableEntity1 == null)
            {
                primaryKey = "nodeDirId";
                primaryKey1 = "nodeId";
            }
            else
            {
                primaryKey1 = tableEntity1.PrimaryKey;
                if (tableEntity == null)
                {
                    primaryKey = primaryKey1.Substring(0, primaryKey1.Length - 2);
                    primaryKey = primaryKey + "Id";
                }
                else
                {
                    primaryKey = tableEntity.PrimaryKey;
                }
            }
            foreach (var tempDir in dir)
            {
                #region  复制目录
                var newTempDir = new BsonDocument
                {
                    {"projId", projId.ToString()},
                    {"src" + primaryKey, tempDir.String(primaryKey)},
                    {"name", tempDir.String("name")},
                    {"typeId", "0"},
                    {"isFill", tempDir.String("isFill")},
                    {"colsType", tempDir.String("colsType")},
                    {primaryKey1, dirIfno.String(primaryKey1)},
                    {"nodePid", parent != null ? parent.String(primaryKey) : "0"}
                };

                #endregion

                var result = this._ctx.Insert(tableName + "Dir", newTempDir);
                if (result.Status != Status.Successful)
                {
                    return;
                }
                newTempDir = result.BsonInfo;
                newDirList.Add(newTempDir);
                CopyGeneralDir(newDirList, oldDirList, tempDir.Int(primaryKey), newTempDir, projId, tableName, dirIfno);
            }
        }
        /// <summary>
        /// 从模板复制目录
        /// </summary>
        /// <param name="newDirList"></param>
        /// <param name="oldDirList"></param>
        /// <param name="nodePid"></param>
        /// <param name="parent"></param>
        /// <param name="tableName"></param>
        /// <param name="dirIfno"></param>
        public void CopyGeneralDir(List<BsonDocument> newDirList, List<BsonDocument> oldDirList, int nodePid, BsonDocument parent, string tableName, BsonDocument dirIfno)
        {
            var dir = oldDirList.Where(m => m.Int("nodePid") == nodePid).ToList();
            if (!dir.Any())
                return;
            var tableEntity = new TableRule(tableName + "Dir");
            var tableEntity1 = new TableRule(tableName);//模板或项目目录数据项
            string primaryKey;
            string primaryKey1;
            if (tableEntity1 == null)
            {
                primaryKey = "nodeDirId";
                primaryKey1 = "nodeId";
            }
            else
            {
                primaryKey1 = tableEntity1.PrimaryKey;
                if (tableEntity == null)
                {
                    primaryKey = primaryKey1.Substring(0, primaryKey1.Length - 2);
                    primaryKey = primaryKey + "Id";
                }
                else
                {
                    primaryKey = tableEntity.PrimaryKey;
                }
            }
            foreach (var tempDir in dir)
            {
                #region  复制目录
                var newTempDir = new BsonDocument();
                newTempDir.Add("srcId", tempDir.String(primaryKey));
                newTempDir.Add("name", tempDir.String("name"));
                newTempDir.Add("typeId", "0");
                newTempDir.Add("isFill", tempDir.String("isFill"));
                newTempDir.Add("isTime", tempDir.String("isTime"));
                if (tableName == "CostEcoIndicator" || tableName == "KeyTechEcoIndicator") 
                {
                    newTempDir.Add("content", tempDir.String("content"));
                }
                newTempDir.Add("colsType", tempDir.String("colsType"));
                newTempDir.Add(primaryKey1, dirIfno.String(primaryKey1));
                newTempDir.Add("nodePid", parent != null ? parent.String(primaryKey) : "0");
                #endregion

                var result = this._ctx.Insert(tableName + "Dir", newTempDir);
                if (result.Status != Status.Successful)
                {
                    return;
                }
                newTempDir = result.BsonInfo;
                newDirList.Add(newTempDir);
                CopyGeneralDir(newDirList, oldDirList, tempDir.Int(primaryKey), newTempDir, tableName, dirIfno);
            }
        }

       /// <summary>
        /// 保存版本目录
       /// </summary>
       /// <param name="newDirList"></param>
       /// <param name="oldDirList"></param>
       /// <param name="nodePid"></param>
       /// <param name="parent"></param>
       /// <param name="tableName"></param>
       /// <param name="dirIfno"></param>
       /// <param name="sourceDirTb"></param>
       /// <param name="filed"></param>
        public void SaveVerDir(List<BsonDocument> newDirList, List<BsonDocument> oldDirList, int nodePid, BsonDocument parent, string tableName, BsonDocument dirIfno,string sourceDirTb,List<string> filed)
        {
            var dir = oldDirList.Where(m => m.Int("nodePid") == nodePid).ToList();
            if (!dir.Any())
                return;
            string sourceKey = new TableRule(sourceDirTb)!=null?new TableRule(sourceDirTb).GetPrimaryKey():string.Empty;
            var tableEntity = new TableRule(tableName + "Dir");
            var tableEntity1 = new TableRule(tableName);//模板或项目目录数据项
            string primaryKey;
            string primaryKey1;
            if (tableEntity1 == null)
            {
                primaryKey = "nodeDirId";
                primaryKey1 = "nodeId";
            }
            else
            {
                primaryKey1 = tableEntity1.PrimaryKey;
                if (tableEntity == null)
                {
                    primaryKey = primaryKey1.Substring(0, primaryKey1.Length - 2);
                    primaryKey = primaryKey + "Id";
                }
                else
                {
                    primaryKey = tableEntity.PrimaryKey;
                }
            }
            foreach (var tempDir in dir)
            {
                #region  复制目录
                var newTempDir = new BsonDocument();
                newTempDir.Add("src" + primaryKey, tempDir.String(primaryKey));
                newTempDir.Add("name", tempDir.String("name"));
                newTempDir.Add("typeId", "0");
                newTempDir.Add("isFill", tempDir.String("isFill"));
                newTempDir.Add("colsType", tempDir.String("colsType"));
                newTempDir.Add(primaryKey1, dirIfno.String(primaryKey1));
                foreach (var tempFiled in filed) {
                    newTempDir.Add(tempFiled, tempDir.String(tempFiled));
                }
                newTempDir.Add("nodePid", parent != null ? parent.String(primaryKey) : "0");
                #endregion

                var result = this._ctx.Insert(tableName + "Dir", newTempDir);
                if (result.Status != Status.Successful)
                {
                    return;
                }
                newTempDir = result.BsonInfo;
                newDirList.Add(newTempDir);
                SaveVerDir(newDirList, oldDirList, tempDir.Int(sourceKey), newTempDir, tableName, dirIfno, sourceDirTb, filed);
            }
        }



        public void SaveGeneralDir(string tbName,int landId,int templateId) 
        {
            var newTempDir = new BsonDocument();
        }
        #endregion
    }
}
