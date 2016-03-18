using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System.Web.Mvc;
using System.Xml.Linq;
using System.Transactions;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using Yinhe.ProcessingCenter.DataRule;

namespace Yinhe.ProcessingCenter
{
    /// <summary>
    /// mongo交互操作处理
    /// </summary>
    public class DataOperation
    {
        #region 私有字段
        private MongoOperation _dbOp = null;    //Mongo数据库操作类

        private string _tbName = null;                          //操作表

        private IMongoQuery _query = Query.Null;       //存储用查询
        private BsonDocument _dataBson = null;                  //存储用数据

        private static object obj = new object();               //用于操作锁

        private int _currentUserId = -1;                        //当前操作用户Id
        private string _currentUserName = null;                 //当前操作用户名

        #endregion

        #region 构造函数
        /// <summary>
        /// 默认构造函数
        /// </summary>
        public DataOperation()
        {
            if (_dbOp == null)
            {
                _dbOp = new MongoOperation();
            }
        }
        /// <summary>
        /// 默认构造函数
        /// </summary>
        public DataOperation(string constr,bool isOtherConnection)
        {
            if (_dbOp == null)
            {
                _dbOp = new MongoOperation(constr);
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="nameStr"></param>
        /// <param name="querStr"></param>
        /// <param name="dataStr"></param>
        public DataOperation(string tbName)
            : this()
        {
            _tbName = tbName;
        }

        #endregion

        #region 公共方法
        /// <summary>
        /// 设置操作数据库
        /// </summary>
        /// <param name="connStr"></param>
        public void SetOperationDataBase(string connStr)
        {
            this._dbOp = new MongoOperation(connStr);
        }

        /// <summary>
        /// 设置操作表
        /// </summary>
        /// <param name="tbName"></param>
        public void SetOperationTable(string tbName)
        {
            this._tbName = tbName;
        }

        /// <summary>
        /// 设置要进行数据库操作的数据
        /// </summary>
        /// <param name="nameStr"></param>
        /// <param name="queryStr"></param>
        /// <param name="dataStr"></param>
        public void SetOperationData(string nameStr, string queryStr, string dataStr)
        {
            if (nameStr.Trim() != "")
            {
                _tbName = nameStr;

                if (queryStr != null && queryStr.Trim() != "")
                {
                    _query = TypeConvert.NativeQueryToQuery(queryStr);
                }

                if (dataStr != null && dataStr.Trim() != "")
                {
                    _dataBson = TypeConvert.ParamStrToBsonDocument(dataStr);
                }
            }
            else
            {
                throw new Exception("传入参数有误");
            }
        }

        /// <summary>
        /// 返回当前用户Id
        /// </summary>
        /// <returns></returns>
        public int GetCurrentUserId()
        {
            SetCurrentUser();
            return this._currentUserId;
        }

        /// <summary>
        /// 返回当前用户名称
        /// </summary>
        /// <returns></returns>
        public string GetCurrentUserName()
        {
            SetCurrentUser();
            return this._currentUserName;
        }

        /// <summary>
        /// 获取用户Id地址
        /// </summary>
        /// <returns></returns>
        public string GetUserIPAddress()
        {
            string ip = String.Empty;

            try
            {
                if (System.Web.HttpContext.Current != null)
                {
                    ip = HttpContext.Current.Request.UserHostAddress;

                    if (string.IsNullOrEmpty(ip))
                    {
                        if (System.Web.HttpContext.Current.Request.ServerVariables["HTTP_VIA"] != null)
                        {
                            ip = System.Web.HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"].ToString();
                        }
                        else
                        {
                            ip = System.Web.HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"].ToString();
                        }
                    }
                }

                if (ip == null || ip == String.Empty)
                {
                    return "0.0.0.0";
                }
            }
            catch (Exception ex)
            {
                ip = "127.0.0.1";
            }

            return ip == "::1" ? "127.0.0.1" : ip;
        }

        /// <summary>
        /// 获取findeOne原生查询
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="queryInfo">包含大括号的查询信息</param>
        /// <returns></returns>
        public string GetNativeQueryFindOne(string tbName, string queryInfo)
        {
            string retStr = string.Format("db.{0}.findOne({1})", tbName, queryInfo);

            return retStr;
        }

        /// <summary>
        /// 获取distinct原生查询
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="queryInfo">包含大括号的查询信息</param>
        /// <returns></returns>
        public string GetNativeQueryDistinct(string tbName, string queryInfo)
        {
            string retStr = string.Format("db.{0}.distinct('_id',{1})", tbName, queryInfo);

            return retStr;
        }

        /// <summary>
        /// 保存级联数据
        /// </summary>
        /// <param name="dataList"></param>
        /// <returns></returns>
        public InvokeResult BatchSaveStorageData(List<StorageData> dataList)
        {
            InvokeResult result = new InvokeResult() { Status = Status.Successful };

            result = this.SaveStorageData(dataList);

            return result;
        }

        /// <summary>
        /// 通过主键批量更新
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="idList"></param>
        /// <param name="doc"></param>
        /// <returns></returns>
        public InvokeResult BatchUpdateByPrimaryKey(string tbName, List<string> idList, BsonDocument doc)
        {
            InvokeResult result = new InvokeResult();
            List<StorageData> upList = new List<StorageData>();

            TableRule table = new TableRule(tbName);

            foreach (var tempId in idList)
            {
                var query = Query.EQ(table.GetPrimaryKey(), tempId.ToString());

                StorageData temp = new StorageData();

                BsonDocument tempBson = new BsonDocument();

                foreach (var tempEle in doc.Elements)
                {
                    tempBson.Add(tempEle.Name, tempEle.Value);
                }

                temp.Name = tbName;
                temp.Query = query;
                temp.Document = tempBson;
                temp.Type = StorageType.Update;

                upList.Add(temp);
            }

            result = BatchSaveStorageData(upList);

            return result;
        }

        /// <summary>
        /// 通过主键批量删除
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="idList"></param>
        /// <returns></returns>
        public InvokeResult BatchDeleteByPrimaryKey(string tbName, List<string> idList)
        {
            InvokeResult result = new InvokeResult();
            List<StorageData> delList = new List<StorageData>();

            TableRule table = new TableRule(tbName);

            foreach (var tempId in idList)
            {
                var query = Query.EQ(table.GetPrimaryKey(), tempId.ToString());

                StorageData temp = new StorageData();

                temp.Name = tbName;
                temp.Query = query;
                temp.Type = StorageType.Delete;

                delList.Add(temp);
            }

            result = BatchSaveStorageData(delList);
            return result;
        }

        /// <summary>
        /// 保存数据操作日志
        /// </summary>
        /// <param name="tbName">操作表</param>
        /// <param name="type">操作类型</param>
        /// <param name="beforeBson">操作前数据</param>
        /// <param name="afterBson">操作后数据</param>
        /// <returns></returns>
        public InvokeResult LogSystemData(TableRule tbEntity, SysLogType logType, string oldDataStr, string opDataStr)
        {
            InvokeResult result = new InvokeResult();
            BsonDocument logBson = new BsonDocument();

            DateTime now = DateTime.Now;

            logBson.Add("logUserId", this.GetCurrentUserId().ToString());
            logBson.Add("ipAddress", this.GetUserIPAddress());
            logBson.Add("logTime", now.ToString("yyyy-MM-dd HH:mm:ss"));
            logBson.Add("timeSort", now.ToString("yyyyMMddHHmmss"));
            logBson.Add("logType", ((int)logType).ToString());
            logBson.Add("tableName", tbEntity.Name);
            logBson.Add("oldData", oldDataStr);
            logBson.Add("opData", opDataStr);

            if (System.Web.HttpContext.Current != null)
            {
                if (System.Web.HttpContext.Current.Items.Contains("BehaviorLogId"))
                {
                    logBson.Add("behaviorId", System.Web.HttpContext.Current.Items["BehaviorLogId"].ToString());
                }
            }

            if (oldDataStr.Trim() != "" || opDataStr.Trim() != "")
            {
                if (tbEntity.HasLog == true)    //如果是主表,则记录到主表日志中
                {
                    result = _dbOp.Save("SysMainDataLog", logBson);     //SysMainDataLog 正式数据,即在规则中标记haslog的记录
                }
                else                //如果是关联表,则记录到关联表日志中
                {
                    result = _dbOp.Save("SysAssoDataLog", logBson);     //SysAssoDataLog  非正式数据,即关联表记录,没在规则中作出标记的记录
                }
            }

            if (result.Status == Status.Successful) result.BsonInfo = logBson;

            return result;
        }

        /// <summary>
        /// 保存系统行为日志
        /// </summary>
        /// <param name="logType"></param>
        /// <param name="curUrl"></param>
        /// <param name="browser"></param>
        /// <param name="param"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public InvokeResult LogSysBehavior(SysLogType logType, HttpContextBase context)
        {
            InvokeResult result = new InvokeResult();

            #region 获取信息
            string referrer = "";
            string path = "";
            string browser = "";
            string method = "";
            BsonDocument paramBson = new BsonDocument();

            if (context != null)
            {
                HttpRequestBase request = context.Request;

                referrer = request.UrlReferrer != null ? request.UrlReferrer.ToString() : "";
                path = request.Url.AbsolutePath;
                browser = request.Browser.Browser + request.Browser.Version;
                method = request.HttpMethod;

                if (method == "GET") paramBson = TypeConvert.NameValueToBsonDocument(request.QueryString);
                if (method == "POST") paramBson = TypeConvert.NameValueToBsonDocument(request.Form);
            }
            #endregion

            #region 记录日志
            DateTime now = DateTime.Now;

            BsonDocument logBson = new BsonDocument();

            logBson.Add("logUserId", this.GetCurrentUserId().ToString());
            logBson.Add("ipAddress", this.GetUserIPAddress());
            logBson.Add("logTime", now.ToString("yyyy-MM-dd HH:mm:ss"));
            logBson.Add("timeSort", now.ToString("yyyyMMddHHmmss"));
            logBson.Add("logType", ((int)logType).ToString());
            logBson.Add("referrer", referrer.Replace("'", "''"));       //当前操作的父级url地址
            logBson.Add("path", path.Replace("'", "''"));       //当前操作的地址
            logBson.Add("browser", browser.Replace("'", "''"));     //访问用的浏览器
            logBson.Add("method", method);                  //提交方式,post或者get

            if (BsonDocumentExtension.IsNullOrEmpty(paramBson) == false)    //访问的地址附带的参数(包含post和get)
            {
                foreach (var temp in paramBson.Elements)
                {
                    logBson.Add(temp.Name, temp.Value.ToString());
                }
            }

            result = _dbOp.Save("SysBehaviorLog", logBson);     //SysBehaviorLog 系统行为日志

            if (result.Status == Status.Successful) result.BsonInfo = logBson;
            #endregion

            #region 记录行为日志Id
            if (context.Items.Contains("BehaviorLogId")) context.Items["BehaviorLogId"] = logBson.String("_id");
            else context.Items.Add("BehaviorLogId", logBson.String("_id"));
            #endregion

            if (result.Status == Status.Successful) result.BsonInfo = logBson;

            return result;
        }

        #endregion

        #region 普通查询
        /// <summary>
        /// 查找集合中的默认第一条记录
        /// </summary>
        /// <returns></returns>
        public BsonDocument FindOne()
        {
            if (_tbName == null) throw new Exception("未能正确构造数据处理中心");

            return _dbOp.FindOne(_tbName);
        }

        /// <summary>
        /// 查找集合中的默认第一条记录
        /// </summary>
        /// <param name="tbName"></param>
        /// <returns></returns>
        public BsonDocument FindOne(string tbName)
        {
            return _dbOp.FindOne(tbName);
        }

        /// <summary>
        /// 根据单个条件查找
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public BsonDocument FindOneByKeyVal(string key, string val)
        {
            if (_tbName == null) throw new Exception("未能正确构造数据处理中心");

            var query = Query.EQ(key, val);

            return _dbOp.FindOne(_tbName, query);
        }

        /// <summary>
        /// 根据单个条件查找
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public BsonDocument FindOneByKeyVal(string tbName, string key, string val)
        {
            var query = Query.EQ(key, val);

            return _dbOp.FindOne(tbName, query);
        }

        /// <summary>
        /// 根据单个条件查找
        /// </summary>
        /// <param name="queryStr"></param>
        /// <returns></returns>
        public BsonDocument FindOneByQueryStr(string queryStr)
        {
            var query = TypeConvert.ParamStrToQuery(queryStr);

            return _dbOp.FindOne(_tbName, query);
        }

        /// <summary>
        /// 根据单个条件查找
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="queryStr">格式如：userId={0}&postId={1}</param>
        /// <returns></returns>
        public BsonDocument FindOneByQueryStr(string tbName, string queryStr)
        {
            var query = TypeConvert.ParamStrToQuery(queryStr);

            return _dbOp.FindOne(tbName, query);
        }

        /// <summary>
        /// 根据搜索条件查找一条记录
        /// </summary>
        /// <param name="searchQuery"></param>
        /// <returns></returns>
        public BsonDocument FindOneByQuery(IMongoQuery query)
        {
            if (_tbName == null) throw new Exception("未能正确构造数据处理中心");

            return _dbOp.FindOne(_tbName, query);
        }

        /// <summary>
        /// 根据搜索条件查找一条记录
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="searchQuery"></param>
        /// <returns></returns>
        public BsonDocument FindOneByQuery(string tbName, IMongoQuery query)
        {
            return _dbOp.FindOne(tbName, query);
        }

        /// <summary>
        /// 查找集合中所有记录
        /// </summary>
        /// <returns></returns>
        public MongoCursor<BsonDocument> FindAll()
        {
            if (_tbName == null) throw new Exception("未能正确构造数据处理中心");

            return _dbOp.FindAll(_tbName);
        }

        /// <summary>
        /// 查找集合中所有记录
        /// </summary>
        /// <param name="tbName"></param>
        /// <returns></returns>
        public MongoCursor<BsonDocument> FindAll(string tbName)
        {
            return _dbOp.FindAll(tbName);
        }

        /// <summary>
        /// 根据单个条件查找
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public MongoCursor<BsonDocument> FindAllByKeyVal(string key, string val)
        {
            if (_tbName == null) throw new Exception("未能正确构造数据处理中心");

            var query = Query.EQ(key, val);

            return _dbOp.FindAll(_tbName, query);
        }

        /// <summary>
        /// 根据单个条件查找
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public MongoCursor<BsonDocument> FindAllByKeyVal(string tbName, string key, string val)
        {
            var query = Query.EQ(key, val);

            return _dbOp.FindAll(tbName, query);
        }

        /// <summary>
        /// 根据单个条件查找
        /// </summary>
        /// <param name="key"></param>
        /// <param name="valList"></param>
        /// <returns></returns>
        public MongoCursor<BsonDocument> FindAllByKeyValList(string key, IEnumerable<string> valList)
        {
            if (_tbName == null) throw new Exception("未能正确构造数据处理中心");

            var query = Query.In(key, TypeConvert.StringListToBsonValueList(valList));

            return _dbOp.FindAll(_tbName, query);
        }

        /// <summary>
        /// 根据单个条件查找
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="key"></param>
        /// <param name="valList"></param>
        /// <returns></returns>
        public MongoCursor<BsonDocument> FindAllByKeyValList(string tbName, string key, IEnumerable<string> valList)
        {
            var query = Query.In(key, TypeConvert.StringListToBsonValueList(valList));

            return _dbOp.FindAll(tbName, query);
        }

        /// <summary>
        /// 根据搜索条件查找多条记录
        /// </summary>
        /// <param name="queryStr"></param>
        /// <returns></returns>
        public MongoCursor<BsonDocument> FindAllByQueryStr(string queryStr)
        {
            if (_tbName == null) throw new Exception("未能正确构造数据处理中心");

            var query = TypeConvert.ParamStrToQuery(queryStr);

            return _dbOp.FindAll(_tbName, query);
        }

        /// <summary>
        /// 根据搜索条件查找多条记录
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="queryStr"></param>
        /// <returns></returns>
        public MongoCursor<BsonDocument> FindAllByQueryStr(string tbName, string queryStr)
        {
            var query = TypeConvert.ParamStrToQuery(queryStr);

            return _dbOp.FindAll(tbName, query);
        }

        /// <summary>
        /// 根据搜索条件查找多条记录
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public MongoCursor<BsonDocument> FindAllByQuery(IMongoQuery query)
        {
            if (_tbName == null) throw new Exception("未能正确构造数据处理中心");

            return _dbOp.FindAll(_tbName, query);
        }

        /// <summary>
        /// 根据搜索条件查找多条记录
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public MongoCursor<BsonDocument> FindAllByQuery(string tbName, IMongoQuery query)
        {
            return _dbOp.FindAll(tbName, query);
        }

        #endregion

        #region 树形查询
        /// <summary>
        /// 查找当前节点的所有子,孙子节点(不包含当前节点)
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        public MongoCursor<BsonDocument> FindChildNodes(string nodeId)
        {
            if (_tbName == null) throw new Exception("未能正确构造数据处理中心");

            return this.FindChildNodes(_tbName, nodeId);
        }

        /// <summary>
        /// 查找当前节点的所有子,孙子节点(不包含当前节点)
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        public MongoCursor<BsonDocument> FindChildNodes(string tbName, string nodeId)
        {
            TableRule tableEntity = new TableRule(tbName);    //获取表结构

            string primaryKey = tableEntity.ColumnRules.Where(t => t.IsPrimary == true).FirstOrDefault().Name;  //寻找默认主键

            BsonDocument nodeEntity = this.FindOneByKeyVal(tbName, primaryKey, nodeId);

            var query = Query.Null;

            if (nodeEntity != null)
            {
                query = Query.And(
                    Query.Matches("nodeKey", string.Format(@"^({0}).*?", nodeEntity.String("nodeKey"))),
                    Query.NE(primaryKey, nodeId)
                );
            }

            return _dbOp.FindAll(tbName, query);
        }

        /// <summary>
        /// 查找当前节点的所有子,孙子节点(包含当前节点)
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        public List<BsonDocument> FindTreeByRootId(string tbName, string nodeId, bool isContainRoot)
        {
            TableRule tableEntity = new TableRule(tbName);    //获取表结构

            string primaryKey = tableEntity.ColumnRules.Where(t => t.IsPrimary == true).FirstOrDefault().Name;  //寻找默认主键

            BsonDocument nodeEntity = this.FindOneByKeyVal(tbName, primaryKey, nodeId);

            var query = Query.Null;

            if (nodeEntity != null)
            {
                query = Query.And(
                    Query.Matches("nodeKey", string.Format(@"^({0}).*?", nodeEntity.String("nodeKey"))),
                    Query.GT("nodeLevel", nodeEntity.String("nodeLevel"))
                );
            }

            var list = _dbOp.FindAll(tbName, query).ToList();
            if (isContainRoot)
            {
                list.Add(nodeEntity);
                list = list.OrderBy(t => t.Text("nodeKey")).ToList();
            }

            return list;
        }

        /// <summary>
        /// 查找当前节点的所有父,祖父节点(不包含当前节点)
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        public List<BsonDocument> FindParentNodeList(string nodeId)
        {
            if (_tbName == null) throw new Exception("未能正确构造数据处理中心");

            return this.FindParentNodeList(_tbName, nodeId);
        }

        /// <summary>
        /// 查找当前节点的所有父,祖父节点(不包含当前节点)
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        public List<BsonDocument> FindParentNodeList(string tbName, string nodeId)
        {
            TableRule tableEntity = new TableRule(tbName);    //获取表结构

            string primaryKey = tableEntity.ColumnRules.Where(t => t.IsPrimary == true).FirstOrDefault().Name;  //寻找默认主键

            BsonDocument nodeEntity = this.FindOneByKeyVal(tbName, primaryKey, nodeId);

            var query = Query.LT("nodeLevel", nodeEntity.String("nodeLevel"));

            List<BsonDocument> parentNodeList = _dbOp.FindAll(tbName, query).Where(t => nodeEntity.String("nodeKey").IndexOf(t.String("nodeKey")) == 0).ToList();

            return parentNodeList;
        }

        #endregion

        #region 保存操作
        /// <summary>
        /// 保存操作
        /// </summary>
        /// <returns></returns>
        public InvokeResult Save()
        {
            return this.Save(_tbName, _query, _dataBson);
        }

        /// <summary>
        /// 保存操作
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="queryStr"></param>
        /// <param name="strData"></param>
        /// <returns></returns>
        public InvokeResult Save(string tbName, string queryStr, string dataStr)
        {
            InvokeResult result = new InvokeResult();

            var query = TypeConvert.NativeQueryToQuery(queryStr);
            BsonDocument dataBson = TypeConvert.ParamStrToBsonDocument(dataStr);

            if (queryStr != "")
            {
                result = this.Update(tbName, query, dataBson);
            }
            else
            {
                result = this.Insert(tbName, dataBson);
            }

            return result;
        }

        /// <summary>
        /// 保存操作
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="query"></param>
        /// <param name="dataBson"></param>
        /// <returns></returns>
        public InvokeResult Save(string tbName, IMongoQuery query, BsonDocument dataBson)
        {
            InvokeResult result = new InvokeResult();

            if (!string.IsNullOrEmpty(tbName))
            {
                if (query != Query.Null)
                {
                    result = this.Update(tbName, query, dataBson);
                }
                else
                {
                    result = this.Insert(tbName, dataBson);
                }
            }
            else
            {
                result.Status = Status.Failed;
                result.Message = "传入参数有误";
            }

            return result;
        }

        #endregion

        #region 插入操作
        /// <summary>
        /// 插入操作
        /// </summary>
        /// <returns></returns>
        public InvokeResult Insert()
        {
            return this.Insert(_tbName, _dataBson);
        }

        /// <summary>
        /// 插入操作
        /// </summary>
        /// <param name="dataStr"></param>
        /// <returns></returns>
        public InvokeResult Insert(string dataStr)
        {
            BsonDocument dataBson = TypeConvert.ParamStrToBsonDocument(dataStr);
            return this.Insert(_tbName, dataBson);
        }

        /// <summary>
        /// 插入操作
        /// </summary>
        /// <param name="dataBson"></param>
        /// <returns></returns>
        public InvokeResult Insert(BsonDocument dataBson)
        {
            return this.Insert(_tbName, dataBson);
        }

        /// <summary>
        /// 插入操作
        /// </summary>
        /// <param name="valueDic"></param>
        /// <returns></returns>
        public InvokeResult Insert(Dictionary<string, string> valueDic)
        {
            BsonDocument dataBson = TypeConvert.DicToBsonDocument(valueDic);
            return this.Insert(_tbName, dataBson);
        }

        /// <summary>
        /// 插入操作
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="dataStr"></param>
        /// <returns></returns>
        public InvokeResult Insert(string tbName, string dataStr)
        {
            BsonDocument dataBson = TypeConvert.ParamStrToBsonDocument(dataStr);
            return this.Insert(tbName, dataBson);
        }

        /// <summary>
        /// 插入操作
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="dataStr"></param>
        /// <returns></returns>
        public InvokeResult Insert(string tbName, Dictionary<string, string> valueDic)
        {
            BsonDocument dataBson = TypeConvert.DicToBsonDocument(valueDic);
            return this.Insert(tbName, dataBson);
        }

        /// <summary>
        /// 插入数据
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="dataBson"></param>
        /// <returns></returns>
        public InvokeResult Insert(string tbName, BsonDocument dataBson)
        {
            InvokeResult result = new InvokeResult();

            lock (obj)
            {
                if (string.IsNullOrEmpty(tbName) == false && BsonDocumentExtension.IsNullOrEmpty(dataBson) == false)
                {
                    #region 插入操作
                    try
                    {
                        BsonDocument insertDoc = new BsonDocument();    //保存数据

                        TableRule tableEntity = new TableRule(tbName);    //获取表结构

                        string primaryKey = "";

                        if (tableEntity.ColumnRules.Count > 0)
                        {
                            primaryKey = tableEntity.ColumnRules.Where(t => t.IsPrimary == true).FirstOrDefault().Name;  //寻找默认主键
                        }

                        #region 获取主键counter
                        BsonDocument counter = this.FindOneByKeyVal("TablePKCounter", "tbName", tbName);

                        if (counter == null)    //数据库中还没有当前表的记录
                        {
                            BsonDocument temp = new BsonDocument();

                            temp.Add("tbName", tbName);

                            MongoCursor<BsonDocument> allData = _dbOp.FindAll(tbName);  //当前表中的总数据,用于处理自增和排序

                            if (allData != null && allData.Count() > 0)
                            {
                                temp.Add("count", allData.Max(t => t.Int(tableEntity.GetPrimaryKey())));
                            }
                            else
                            {
                                temp.Add("count", "0");
                            }

                            InvokeResult tempRet = _dbOp.Save("TablePKCounter", temp);

                            if (tempRet.Status == Status.Failed) throw new Exception(tempRet.Message);

                            counter = temp;
                        }
                        #endregion

                        #region 构建保存数据

                        #region 保存规则文件中的关键值
                        foreach (var column in tableEntity.ColumnRules)     //循环数据规则中的字段
                        {
                            BsonValue colValue = null;

                            #region 如果是自增,进行自增处理
                            if (column.IsIdentity == true) //自增处理
                            {

                                if (counter.Int("count") > 0)
                                {
                                    colValue = (counter.Int("count") + 1).ToString();
                                }
                                else
                                {
                                    colValue = "1";
                                }
                            }
                            #endregion

                            #region 如果是NodePid,则默认多添加(NodeKey,NodeOrder,NodeLevel)
                            else if (column.Name == "nodePid")
                            {
                                BsonDocument tempParent = this.FindOneByKeyVal(tbName, primaryKey, dataBson.String("nodePid", "0"));     //获取对应父节点

                                BsonDocument keyAttr = this.GetNewTreeNodeKeyAttr(tableEntity, tempParent);

                                insertDoc.Add("nodePid", keyAttr.String("nodePid", "0"));
                                insertDoc.Add("nodeLevel", keyAttr.String("nodeLevel", "1"));
                                insertDoc.Add("nodeOrder", keyAttr.String("nodeOrder", "1"));
                                insertDoc.Add("nodeKey", keyAttr.String("nodeKey", "000001"));
                            }
                            else if (column.Name == "NodeKey" || column.Name == "NodeLevel" || column.Name == "NodeOrder")
                            {
                                continue;
                            }
                            #endregion

                            #region 如果有默认值,且没有传入值
                            else if (column.Value != "" && dataBson.Contains(column.Name) == false)
                            {
                                colValue = column.Value;
                            }
                            #endregion

                            #region 如果是普通值
                            else if (dataBson != null && dataBson.Contains(column.Name))
                            {
                                colValue = dataBson[column.Name];   //字段值
                            }
                            #endregion

                            if (colValue != null && insertDoc.Contains(column.Name) == false)
                            {
                                if (colValue.IsBoolean || colValue.IsDateTime || colValue.IsGuid || colValue.IsInt32 || colValue.IsDouble || colValue.IsInt64 || colValue.IsNumeric)
                                {
                                    insertDoc.Add(column.Name, colValue.ToString());
                                }
                                else
                                {
                                    insertDoc.Add(column.Name, colValue);
                                }
                            }
                        }
                        #endregion

                        #region 保存非规则文件中的附加值
                        foreach (var tempElement in dataBson.Elements)         //循环数据中的其他字段
                        {
                            if (insertDoc.Contains(tempElement.Name) == false)
                            {
                                BsonValue colValue = dataBson[tempElement.Name];

                                if (colValue.IsBoolean || colValue.IsDateTime || colValue.IsGuid || colValue.IsInt32 || colValue.IsDouble || colValue.IsInt64 || colValue.IsNumeric)
                                {
                                    insertDoc.Add(tempElement.Name, colValue.ToString());
                                }
                                else
                                {
                                    insertDoc.Add(tempElement.Name, colValue);
                                }
                            }
                        }
                        #endregion

                        #region 保存通用数据
                        if (insertDoc.Contains("createDate") == false) insertDoc.Add("createDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));      //添加时,默认增加创建时间
                        if (insertDoc.Contains("updateDate") == false) insertDoc.Add("updateDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));      //更新时间
                        if (insertDoc.Contains("createUserId") == false) insertDoc.Add("createUserId", this.GetCurrentUserId().ToString());                         //创建用户 -1为未登录创建
                        if (insertDoc.Contains("updateUserId") == false) insertDoc.Add("updateUserId", this.GetCurrentUserId().ToString());                         //更新用户
                        if (insertDoc.Contains("underTable") == false) insertDoc.Add("underTable", tbName);                                            //标记当前记录所属表

                        if (tbName == "FileLibrary") //文件上传，统计文件数量
                        {
                            var model = this.FindOneByKeyVal("FileCount", "userId", this.GetCurrentUserId().ToString());
                            int count = model == null ? 0 : model.Int("count");
                            ++count;
                            this.Update("FileCount", "db.FileCount.distinct('_id',{'userId':'" + this.GetCurrentUserId().ToString() + "'})", string.Format("count={0}", count));

                        }
                        if (insertDoc.Contains("order") == false && tableEntity.ColumnRules.Where(t => t.Name == "nodePid").Count() == 0)       //非树形记录在表中添加排序
                        {
                            if (counter.Int("count") > 0)
                            {
                                insertDoc.Add("order", counter.Int("count") + 1);
                            }
                            else
                            {
                                insertDoc.Add("order", 1);
                            }
                        }
                        #endregion

                        #endregion

                        #region 处理级联数据
                        List<StorageData> beforeCascadeList = new List<StorageData>();        //插入操作执行前的级联列表
                        List<StorageData> afterCascadeList = new List<StorageData>();         //插入操作执行后的级联列表

                        if (tableEntity.CascadeRules.Count > 0)
                        {
                            BsonDocument afterBson = insertDoc;

                            List<CascadeRule> beforeRuleList = tableEntity.CascadeRules.Where(t => t.Event == StorageType.Insert && t.Time == CascadeTimeType.Before).ToList();
                            List<CascadeRule> afterRuleList = tableEntity.CascadeRules.Where(t => t.Event == StorageType.Insert && t.Time == CascadeTimeType.After).ToList();

                            Dictionary<string, BsonDocument> afterDic = new Dictionary<string, BsonDocument>();

                            afterDic.Add("this", afterBson);

                            beforeCascadeList.AddRange(this.ProcessCascadeData(beforeRuleList, null));
                            afterCascadeList.AddRange(this.ProcessCascadeData(afterRuleList, afterDic));
                        }

                        #endregion

                        #region 保存到数据库
                        InvokeResult tempResult = new InvokeResult();

                        using (TransactionScope trans = new TransactionScope())
                        {
                            //插入前级联处理
                            tempResult = this.SaveStorageData(beforeCascadeList);
                            if (tempResult.Status == Status.Failed) throw new Exception(tempResult.Message);

                            //插入数据
                            tempResult = _dbOp.Save(tbName, insertDoc);
                            if (tempResult.Status == Status.Failed) throw new Exception(tempResult.Message);

                            //插入后级联处理
                            tempResult = this.SaveStorageData(afterCascadeList);
                            if (tempResult.Status == Status.Failed) throw new Exception(tempResult.Message);

                            //记录插入日志
                            tempResult = this.LogSystemData(tableEntity, SysLogType.Insert, "", TypeConvert.BsonDocumentToLogStr(insertDoc));
                            if (tempResult.Status == Status.Failed) throw new Exception(tempResult.Message);

                            //更新计数器
                            counter["count"] = (counter.Int("count") + 1).ToString();
                            tempResult = _dbOp.Save("TablePKCounter", Query.EQ("tbName", tbName), counter);
                            if (tempResult.Status == Status.Failed) throw new Exception(tempResult.Message);

                            trans.Complete();
                        }
                        #endregion

                        result.Status = Status.Successful;
                        result.BsonInfo = insertDoc;
                    }
                    catch (Exception ex)
                    {
                        result.Status = Status.Failed;
                        result.Message = ex.Message;
                    }
                    #endregion
                }
                else
                {
                    result.Status = Status.Failed;
                    result.Message = "表名为空，或者传入参数有误";
                }
            }

            return result;
        }

        #endregion

        #region 更新操作
        /// <summary>
        /// 更新操作
        /// </summary>
        /// <returns></returns>
        public InvokeResult Update()
        {
            return this.Update(_tbName, _query, _dataBson);
        }

        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="queryStr"></param>
        /// <param name="dataStr"></param>
        /// <returns></returns>
        public InvokeResult Update(string queryStr, string dataStr)
        {
            return this.Update(_tbName, queryStr, dataStr);
        }

        /// <summary>
        /// 更新操作
        /// </summary>
        /// <param name="query"></param>
        /// <param name="dataBson"></param>
        /// <returns></returns>
        public InvokeResult Update(IMongoQuery query, BsonDocument dataBson)
        {
            return this.Update(_tbName, query, dataBson);
        }

        /// <summary>
        /// 更新操作
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="queryStr"></param>
        /// <param name="strData"></param>
        /// <returns></returns>
        public InvokeResult Update(string tbName, string queryStr, string dataStr)
        {
            var query = TypeConvert.NativeQueryToQuery(queryStr);
            BsonDocument dataBson = TypeConvert.ParamStrToBsonDocument(dataStr);

            return this.Update(tbName, query, dataBson);
        }

        /// <summary>
        /// 更新操作
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="queryStr">格式如db.Organization.distinct('_id',{'guid':'" + subnode.Guid + "'})</param>
        /// <param name="strData"></param>
        /// <returns></returns>
        public InvokeResult Update(string tbName, string queryStr, BsonDocument dataBson)
        {
            var query = TypeConvert.NativeQueryToQuery(queryStr);
            return this.Update(tbName, query, dataBson);
        }

        /// <summary>
        /// 更新数据
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="queryData"></param>
        /// <param name="tbData"></param>
        /// <returns></returns>
        public InvokeResult Update(BsonDocument beforeBson, BsonDocument dataBson)
        {
            InvokeResult result = new InvokeResult();

            string tbName = beforeBson != null ? beforeBson.TableName() : "";
            string keyFiled = beforeBson != null ? beforeBson.KeyField() : "";
            string keyValue = beforeBson != null ? beforeBson.KeyValue() : "";

            if (string.IsNullOrEmpty(tbName) || string.IsNullOrEmpty(keyFiled) || string.IsNullOrEmpty(keyValue))
            {
                result.Status = Status.Failed;
                result.Message = "传入参数有误";
                return result;
            }
            var queryStr = GetFindOneQueryStr(tbName, keyFiled, keyValue);

            var query = TypeConvert.NativeQueryToQuery(queryStr);

            return this.Update(tbName, query, dataBson);
        }

        /// <summary>
        /// 更新数据
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="queryData"></param>
        /// <param name="tbData"></param>
        /// <returns></returns>
        public InvokeResult Update(BsonDocument beforeBson, string updateQuery)
        {
            InvokeResult result = new InvokeResult();

            string tbName = beforeBson != null ? beforeBson.TableName() : "";
            string keyFiled = beforeBson != null ? beforeBson.KeyField() : "";
            string keyValue = beforeBson != null ? beforeBson.KeyValue() : "";

            if (string.IsNullOrEmpty(tbName) || string.IsNullOrEmpty(keyFiled) || string.IsNullOrEmpty(keyValue))
            {
                result.Status = Status.Failed;
                result.Message = "传入参数有误";
                return result;
            }
            var queryStr = GetFindOneQueryStr(tbName, keyFiled, keyValue);

            BsonDocument dataBson = TypeConvert.ParamStrToBsonDocument(updateQuery);
            var query = TypeConvert.NativeQueryToQuery(queryStr);

            return this.Update(tbName, query, dataBson);
        }

        /// <summary>
        /// 更新数据
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="queryData"></param>
        /// <param name="tbData"></param>
        /// <returns></returns>
        public InvokeResult Update(string tbName, IMongoQuery query, BsonDocument dataBson)
        {
            InvokeResult result = new InvokeResult();

            lock (obj)
            {
                if (string.IsNullOrEmpty(tbName) == false && query != null && BsonDocumentExtension.IsNullOrEmpty(dataBson) == false)
                {
                    #region 更新操作
                    try
                    {
                        BsonDocument oldData = this.FindOneByQuery(tbName, query);  //更新前数据,

                        BsonDocument updateDoc = new BsonDocument();    //更新数据

                        TableRule tableEntity = new TableRule(tbName);    //获取表结构

                        #region 添加通用数据
                        if (dataBson == null) dataBson = new BsonDocument();
                        if (dataBson.Elements.Where(t => t.Name == "updateDate").Count() == 0)
                        {
                            dataBson.Add("updateDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        }
                        if (dataBson.Elements.Where(t => t.Name == "updateUserId").Count() == 0)
                        {
                            dataBson.Add("updateUserId", this.GetCurrentUserId().ToString());
                        }

                        #endregion

                        #region 构建更新数据
                        if (dataBson.ContainsColumn("_id")) //如果包含_id,则数据完全更改
                        {
                            updateDoc = dataBson;
                        }
                        else
                        {
                            foreach (var tempElement in dataBson.Elements)         //循环数据中的字段
                            {
                                string tempKey = tempElement.Name;

                                ColumnRule tempColumn = tableEntity.ColumnRules.Where(t => t.Name == tempKey).FirstOrDefault();         //获取对应关键字字段

                                if (tempColumn != null && (tempColumn.IsIdentity == true || tempColumn.IsPrimary == true)) continue;    //特殊关键字,跳过

                                if (tempKey == "createDate" || tempKey == "createUserId" || tempKey == "underTable") continue;            //特殊关键字,跳过

                                if (updateDoc.Contains(tempKey)) continue;          //已含有重复关键字,跳过

                                var colValue = dataBson[tempKey];    //字段值

                                updateDoc.Add(tempKey, colValue);
                            }
                        }

                        #endregion

                        #region 保存到数据库
                        InvokeResult tempResult = new InvokeResult();

                        using (TransactionScope trans = new TransactionScope())
                        {
                            //记录更新日志
                            tempResult = this.LogSystemData(tableEntity, SysLogType.Update, TypeConvert.BsonDocumentToLogStr(oldData), TypeConvert.BsonDocumentToLogStr(updateDoc));
                            if (tempResult.Status == Status.Failed) throw new Exception(tempResult.Message);

                            //更新数据
                            tempResult = _dbOp.Save(tbName, query, updateDoc);
                            if (tempResult.Status == Status.Failed) throw new Exception(tempResult.Message);

                            trans.Complete();
                        }
                        #endregion

                        result.Status = Status.Successful;
                        result.BsonInfo = this.FindOneByQuery(tbName, query);
                    }
                    catch (Exception ex)
                    {
                        result.Status = Status.Failed;
                        result.Message = ex.Message;
                    }
                    #endregion
                }
                else
                {
                    result.Status = Status.Failed;
                    result.Message = "表名为空，传入参数有误";
                }
            }

            return result;
        }

        #endregion

        #region 删除操作
        /// <summary>
        /// 删除操作
        /// </summary>
        /// <returns></returns>
        public InvokeResult Delete()
        {
            return this.Delete(_tbName, _query);
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="queryStr"></param>
        /// <returns></returns>
        public InvokeResult Delete(string queryStr)
        {
            return this.Delete(_tbName, queryStr);
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public InvokeResult Delete(IMongoQuery query)
        {
            return this.Delete(_tbName, query);
        }

        /// <summary>
        /// 删除操作
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="queryStr"></param>
        /// <returns></returns>
        public InvokeResult Delete(string tbName, string queryStr)
        {
            var query = TypeConvert.NativeQueryToQuery(queryStr);

            return this.Delete(tbName, query);
        }

        /// <summary>
        /// 删除数据
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="queryData"></param>
        /// <returns></returns>
        public InvokeResult Delete(string tbName, IMongoQuery query)
        {
            InvokeResult result = new InvokeResult();

            lock (obj)
            {
                if (string.IsNullOrEmpty(tbName) == false && query != null)
                {
                    #region 删除操作
                    try
                    {
                        TableRule tableEntity = new TableRule(tbName);    //获取表结构

                        string primaryKey = tableEntity.GetPrimaryKey();

                        List<BsonDocument> curDocList = this.FindAllByQuery(tbName, query).ToList();    //根据Query得到的所有节点列表

                        List<BsonDocument> delDocList = new List<BsonDocument>();   //需要删除的所有节点列表
                        delDocList.AddRange(curDocList);

                        #region 获取相关节点

                        if (tableEntity.ColumnRules.Where(t => t.Name == "nodePid").FirstOrDefault() != null)   //如果存在属性关系,则进行树形处理
                        {
                            foreach (var tempDoc in curDocList.OrderBy(t => t.String("nodeKey")))          //按序循环每条记录,删除子表记录
                            {
                                //如果节点的父节点在删除列表内,则跳过查询
                                if (delDocList.Where(t => t.Int(primaryKey) == tempDoc.Int("nodePid")).Count() > 0) continue;

                                List<BsonDocument> subNodeList = this.FindChildNodes(tbName, tempDoc.String(primaryKey)).ToList();  //所有子节点

                                delDocList.AddRange(subNodeList);
                            }

                            delDocList = delDocList.Distinct().ToList();
                        }

                        #endregion

                        #region 处理外键关联

                        List<StorageData> foreignDataList = new List<StorageData>();        //外键数据记录

                        if (delDocList.Count > 0)
                        {
                            foreach (var tempTable in TableRule.GetAllForeignTables(tbName))    //循环有外键关联的表
                            {
                                ColumnRule tempColumn = tempTable.ColumnRules.Where(t => t.SourceTable == tbName).FirstOrDefault();   //关联字段

                                List<string> sourceIdList = delDocList.Select(t => t.String(tempColumn.SourceColumn)).Distinct().ToList();     //需删除记录对应外键的Id列表

                                if (sourceIdList.Count > 0)
                                {
                                    StorageData tempData = new StorageData();

                                    tempData.Name = tempTable.Name;
                                    tempData.Type = StorageType.Delete;
                                    tempData.Query = Query.In(tempColumn.Name, TypeConvert.StringListToBsonValueList(sourceIdList));

                                    foreignDataList.Add(tempData);
                                }
                            }
                        }

                        #endregion

                        #region 保存到数据库
                        InvokeResult tempResult = new InvokeResult();

                        using (TransactionScope trans = new TransactionScope())
                        {
                            //记录删除日志
                            tempResult = this.LogSystemData(tableEntity, SysLogType.Delete, TypeConvert.BsonDocumentToLogStr(delDocList), "");
                            if (tempResult.Status == Status.Failed) throw new Exception(tempResult.Message);

                            //删除外键记录
                            tempResult = this.SaveStorageData(foreignDataList);
                            if (tempResult.Status == Status.Failed) throw new Exception(tempResult.Message);

                            //删除所有记录
                            tempResult = _dbOp.Delete(tbName, Query.In(primaryKey, delDocList.Select(t => (BsonValue)t.String(primaryKey))));
                            if (tempResult.Status == Status.Failed) throw new Exception(tempResult.Message);

                            trans.Complete();
                        }
                        #endregion

                        result.Status = Status.Successful;
                    }
                    catch (Exception ex)
                    {
                        result.Status = Status.Failed;
                        result.Message = ex.Message;
                    }
                    #endregion
                }
                else
                {
                    result.Status = Status.Failed;
                    result.Message = "表名为空，传入参数有误";
                }
            }

            return result;
        }

        #endregion

        #region 移动操作
        /// <summary>
        /// 移动操作
        /// </summary>
        /// <param name="moveId"></param>
        /// <param name="moveToId"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public InvokeResult Move(string moveId, string moveToId, string type)
        {
            return this.Move(_tbName, moveId, moveToId, type);
        }

        /// <summary>
        /// 节点的移动
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="moveId"></param>
        /// <param name="moveToId"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public InvokeResult Move(string tbName, string moveId, string moveToId, string type)
        {
            InvokeResult result = new InvokeResult();

            if (moveId != moveToId && moveId.Trim() != "" && moveId != "" && type != "")
            {
                TableRule tableEntity = new TableRule(tbName);    //获取表结构

                string primaryKey = tableEntity.ColumnRules.Where(t => t.IsPrimary == true).FirstOrDefault().Name;      //获取主键

                BsonDocument moveEntity = this.FindOneByKeyVal(tbName, primaryKey, moveId);             //要移动的节点
                BsonDocument toMoveEntity = this.FindOneByKeyVal(tbName, primaryKey, moveToId);         //移动至的节点

                if (tableEntity.ColumnRules.Where(t => t.Name == "nodePid").FirstOrDefault() != null)   //树形处理
                {
                    result = TreeMove(tableEntity, moveEntity, toMoveEntity, type);
                }
                else            //非树形处理
                {
                    result = NoTreeMove(tableEntity, moveEntity, toMoveEntity, type);
                }
            }
            else
            {
                result.Status = Status.Failed;
                result.Message = "传入参数有误";
            }

            return result;
        }

        /// <summary>
        /// 非树形节点移动
        /// </summary>
        /// <param name="tableEntity"></param>
        /// <param name="moveEntity"></param>
        /// <param name="toMoveEntity"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private InvokeResult NoTreeMove(TableRule tableEntity, BsonDocument moveEntity, BsonDocument toMoveEntity, string type)
        {
            InvokeResult result = new InvokeResult();

            lock (obj)
            {
                #region 移动非树形节点
                try
                {
                    List<StorageData> orderDataList = new List<StorageData>();

                    string primaryKey = tableEntity.ColumnRules.Where(t => t.IsPrimary == true).FirstOrDefault().Name;      //获取主键

                    #region 获取要改变order值的节点列表,和起始order
                    List<BsonDocument> needChangeNodes = new List<BsonDocument>();      //需要改变order的节点列表

                    int startOrder = 0;         //列表起始order

                    if (moveEntity.Int("order") > toMoveEntity.Int("order"))
                    {
                        startOrder = toMoveEntity.Int("order");

                        var temp = Query.And(
                            Query.GTE("order", toMoveEntity.Int("order")),
                            Query.LTE("order", moveEntity.Int("order"))
                            );

                        needChangeNodes = this.FindAllByQuery(tableEntity.Name, temp).ToList();
                    }
                    else
                    {
                        startOrder = moveEntity.Int("order");

                        var temp = Query.And(
                            Query.GTE("order", moveEntity.Int("order")),
                            Query.LTE("order", toMoveEntity.Int("order"))
                            );

                        needChangeNodes = this.FindAllByQuery(tableEntity.Name, temp).ToList();
                    }
                    #endregion

                    #region 改变要移动的节点值
                    BsonDocument tempMove = needChangeNodes.Where(t => t.Int(primaryKey) == moveEntity.Int(primaryKey)).FirstOrDefault();   //列表中要移动的节点

                    if (type == "pre")
                    {
                        tempMove["order"] = toMoveEntity.Int("order") - 0.5;
                    }
                    else if (type == "next")
                    {
                        tempMove["order"] = toMoveEntity.Int("order") + 0.5;
                    }
                    #endregion

                    #region 循环列表,改变对应order
                    foreach (var temp in needChangeNodes.OrderBy(t => t.Decimal("order")))
                    {
                        StorageData tempData = new StorageData();

                        tempData.Type = StorageType.Update;
                        tempData.Name = tableEntity.Name;
                        tempData.Query = Query.EQ(primaryKey, temp.String(primaryKey));
                        tempData.Document = new BsonDocument("order", startOrder);

                        orderDataList.Add(tempData);

                        startOrder++;
                    }
                    #endregion

                    result = this.SaveStorageData(orderDataList);
                    if (result.Status == Status.Failed) throw new Exception(result.Message);
                }
                catch (Exception ex)
                {
                    result.Status = Status.Failed;
                    result.Message = ex.Message;
                }
                #endregion
            }

            return result;
        }

        /// <summary>
        /// 树形节点的移动
        /// </summary>
        /// <param name="tableEntity"></param>
        /// <param name="moveEntity"></param>
        /// <param name="toMoveEntity"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private InvokeResult TreeMove(TableRule tableEntity, BsonDocument moveEntity, BsonDocument toMoveEntity, string type)
        {
            InvokeResult result = new InvokeResult();

            lock (obj)
            {
                #region 移动树形节点
                try
                {
                    List<StorageData> orderDataList = new List<StorageData>();

                    string primaryKey = tableEntity.ColumnRules.Where(t => t.IsPrimary == true).FirstOrDefault().Name;      //获取主键

                    if (type == "child")        //如果是子节点操作,直接改变4个关键值,并改变其子孙节点4个关键值
                    {
                        #region 改变当前节点的4个关键值
                        BsonDocument keyAttr = this.GetNewTreeNodeKeyAttr(tableEntity, toMoveEntity);

                        StorageData curData = new StorageData();
                        curData.Type = StorageType.Update;
                        curData.Name = tableEntity.Name;
                        curData.Query = Query.EQ(primaryKey, moveEntity.String(primaryKey));
                        curData.Document = keyAttr;

                        orderDataList.Add(curData);
                        #endregion

                        #region 改变孙子节点的4个关键值

                        List<BsonDocument> allChildList = this.FindChildNodes(tableEntity.Name, moveEntity.String(primaryKey)).ToList();    //所有子孙节点

                        foreach (var child in allChildList)
                        {
                            BsonDocument subDoc = new BsonDocument();
                            subDoc.Add("nodeLevel", (keyAttr.Int("nodeLevel") - moveEntity.Int("nodeLevel") + child.Int("nodeLevel")).ToString());                    //子孙对应层级
                            subDoc.Add("nodeKey", keyAttr.String("nodeKey") + "." + child.String("nodeKey").Substring(moveEntity.String("nodeKey").Length + 1));    //子孙对应nodeKey

                            StorageData subData = new StorageData();
                            subData.Type = StorageType.Update;
                            subData.Name = tableEntity.Name;
                            subData.Query = Query.EQ(primaryKey, child.String(primaryKey));
                            subData.Document = subDoc;

                            orderDataList.Add(subData);
                        }
                        #endregion
                    }
                    else if (type == "pre" || type == "next")
                    {
                        #region 获取重新计算的列表
                        List<BsonDocument> needChangeNodes = new List<BsonDocument>();   //需要重算的列表

                        int startOrder = 1;

                        if (moveEntity.Int("nodePid") == toMoveEntity.Int("nodePid"))   //如果两者同一父节点,则根据两者顺序,取其中间值
                        {
                            if (moveEntity.Int("nodeOrder") > toMoveEntity.Int("nodeOrder"))
                            {
                                startOrder = toMoveEntity.Int("nodeOrder");

                                needChangeNodes = this.FindAllByQuery(tableEntity.Name, Query.And(
                                    Query.EQ("nodePid", toMoveEntity.String("nodePid")),
                                    Query.GTE("nodeKey", toMoveEntity.String("nodeKey")),
                                    Query.LT("nodeKey", moveEntity.String("nodeKey"))
                                    )).ToList();
                            }
                            else
                            {
                                startOrder = moveEntity.Int("nodeOrder");

                                needChangeNodes = this.FindAllByQuery(tableEntity.Name, Query.And(
                                    Query.EQ("nodePid", toMoveEntity.String("nodePid")),
                                    Query.GT("nodeKey", moveEntity.String("nodeKey")),
                                    Query.LTE("nodeKey", toMoveEntity.String("nodeKey"))
                                    )).ToList();
                            }
                        }
                        else    //如果两者不同父节点,则直接取目标节点底下所有节点
                        {
                            startOrder = toMoveEntity.Int("nodeOrder");

                            needChangeNodes = this.FindAllByQuery(tableEntity.Name, Query.And(
                                    Query.EQ("nodePid", toMoveEntity.String("nodePid")),
                                    Query.GTE("nodeKey", toMoveEntity.String("nodeKey"))
                                    )).ToList();
                        }

                        if (type == "pre")
                        {
                            moveEntity["nodeOrder"] = (toMoveEntity.Int("nodeOrder") - 0.5).ToString();
                        }
                        else if (type == "next")
                        {
                            moveEntity["nodeOrder"] = (toMoveEntity.Int("nodeOrder") + 0.5).ToString();
                        }

                        needChangeNodes.Add(moveEntity);
                        #endregion

                        #region 循环计算列表,改变关键值
                        foreach (var tempNode in needChangeNodes.OrderBy(t => t.Decimal("nodeOrder")))
                        {
                            #region 改变当前节点的4个关键值
                            string nodeKey = toMoveEntity.String("nodeKey").Substring(0, toMoveEntity.String("nodeKey").Length - 6) + startOrder.ToString().PadLeft(6, '0');//新的nodeKey

                            BsonDocument curDoc = new BsonDocument();
                            curDoc.Add("nodeOrder", startOrder.ToString());
                            curDoc.Add("nodeKey", nodeKey);

                            //如果是移动的节点,改变其父节点和层级
                            if (tempNode.Int(primaryKey) == moveEntity.Int(primaryKey))
                            {
                                curDoc.Add("nodePid", toMoveEntity.String("nodePid"));
                                curDoc.Add("nodeLevel", toMoveEntity.String("nodeLevel"));
                            }

                            StorageData curData = new StorageData();

                            curData.Type = StorageType.Update;
                            curData.Name = tableEntity.Name;
                            curData.Query = Query.EQ(primaryKey, tempNode.String(primaryKey));
                            curData.Document = curDoc;

                            orderDataList.Add(curData);
                            #endregion

                            #region 改变孙子节点的4个关键值

                            List<BsonDocument> allChildList = this.FindChildNodes(tableEntity.Name, tempNode.String(primaryKey)).ToList();    //所有子孙节点

                            foreach (var child in allChildList)
                            {
                                if (child.Int(primaryKey) == moveEntity.Int(primaryKey)) continue;

                                BsonDocument subDoc = new BsonDocument();
                                subDoc.Add("nodeKey", nodeKey + "." + child.String("nodeKey").Substring(tempNode.String("nodeKey").Length + 1));    //子孙对应nodeKey

                                if (tempNode.Int(primaryKey) == moveEntity.Int(primaryKey))
                                {
                                    subDoc.Add("nodeLevel", (curDoc.Int("nodeLevel") - tempNode.Int("nodeLevel") + child.Int("nodeLevel")).ToString());                    //子孙对应层级
                                }

                                StorageData subData = new StorageData();
                                subData.Type = StorageType.Update;
                                subData.Name = tableEntity.Name;
                                subData.Query = Query.EQ(primaryKey, child.String(primaryKey));
                                subData.Document = subDoc;

                                orderDataList.Add(subData);
                            }
                            #endregion

                            startOrder++;
                        }
                        #endregion
                    }

                    result = this.SaveStorageData(orderDataList);
                    if (result.Status == Status.Failed) throw new Exception(result.Message);
                }
                catch (Exception ex)
                {
                    result.Status = Status.Failed;
                    result.Message = ex.Message;
                }
                #endregion
            }

            return result;
        }

        #endregion

        #region 私有方法
        /// <summary>
        /// 处理级联数据
        /// </summary>
        /// <param name="ruleList"></param>
        /// <param name="thisEntity"></param>
        /// <returns></returns>
        private List<StorageData> ProcessCascadeData(List<CascadeRule> ruleList, Dictionary<string, BsonDocument> sourceDic)
        {
            List<StorageData> resultList = new List<StorageData>();

            foreach (var rule in ruleList)        //循环级联规则
            {
                if (rule.Condition.GetResult(new List<VarRule>(), sourceDic))   //判断级联规则的条件是否成立
                {
                    List<StorageData> tempDataList = rule.GetStorageDatas(_dbOp, new List<VarRule>(), sourceDic);

                    resultList.AddRange(tempDataList);
                }
            }

            return resultList;
        }

        /// <summary>
        /// 保存存储数据
        /// </summary>
        /// <param name="dataList"></param>
        /// <returns></returns>
        private InvokeResult SaveStorageData(List<StorageData> dataList)
        {
            InvokeResult result = new InvokeResult() { Status = Status.Successful };

            if (dataList != null && dataList.Count > 0)
            {
                try
                {
                    foreach (var temp in dataList)
                    {
                        switch (temp.Type)
                        {
                            case StorageType.Insert:
                                result = this.Insert(temp.Name, temp.Document);
                                break;
                            case StorageType.Update:
                                result = this.Update(temp.Name, temp.Query, temp.Document);
                                break;
                            case StorageType.Delete:
                                result = this.Delete(temp.Name, temp.Query);
                                break;
                        }

                        if (result.Status == Status.Failed) throw new Exception(result.Message);
                    }
                }
                catch (Exception ex)
                {
                    result.Message = ex.Message;
                }
            }

            return result;
        }

        /// <summary>
        /// 计算值
        /// </summary>
        /// <param name="leftStr">左边值</param>
        /// <param name="rightStr">右边值</param>
        /// <param name="operatorStr">运算符</param>
        /// <returns></returns>
        private string CalculateValue(string leftStr, string rightStr, string operatorStr)
        {
            decimal leftVal = 0;
            decimal rightVal = 0;

            if (decimal.TryParse(leftStr, out leftVal) && decimal.TryParse(rightStr, out rightVal)) //如果两个都是数字,则进行四则运算
            {
                decimal resultVal = 0;

                switch (operatorStr)
                {
                    case "+": resultVal = leftVal + rightVal; break;
                    case "-": resultVal = leftVal - rightVal; break;
                    case "*": resultVal = leftVal * rightVal; break;
                    case "/": resultVal = leftVal / rightVal; break;
                }

                return resultVal.ToString();
            }
            else        //不是全部为数字,则进行字符串运算
            {
                string resultStr = string.Empty;

                switch (operatorStr)
                {
                    case "+": resultStr = leftStr + rightStr; break;
                    case "-": resultStr = leftStr + "-" + rightStr; break;
                    case "*": resultStr = leftStr + "*" + rightStr; break;
                    case "/": resultStr = leftStr + "/" + rightStr; break;
                }

                return resultStr;
            }
        }

        /// <summary>
        /// 判断是否具有运算符
        /// </summary>
        /// <param name="valStr"></param>
        /// <returns></returns>
        private bool HasExistOperator(string valStr)
        {
            bool flag = false;

            if (valStr.IndexOf('+') > 0) flag = true;
            if (valStr.IndexOf('-') > 0) flag = true;
            if (valStr.IndexOf('*') > 0) flag = true;
            if (valStr.IndexOf('/') > 0) flag = true;

            return flag;
        }

        /// <summary>
        /// 获取新树节点的4个关键属性
        /// </summary>
        /// <param name="tableEntity"></param>
        /// <param name="parentEntity"></param>
        /// <returns></returns>
        private BsonDocument GetNewTreeNodeKeyAttr(TableRule tableEntity, BsonDocument parentEntity)
        {
            BsonDocument retDoc = new BsonDocument();

            string primaryKey = tableEntity.ColumnRules.Where(t => t.IsPrimary == true).FirstOrDefault().Name;      //获取主键

            string nodePid = parentEntity != null ? parentEntity.String(primaryKey) : "0";
            string nodeLevel = parentEntity != null ? (parentEntity.Int("nodeLevel") + 1).ToString() : "1";
            string nodeOrder = "";
            string nodeKey = "";

            MongoCursor<BsonDocument> allSub = this.FindAllByKeyVal(tableEntity.Name, "nodePid", nodePid);  //当前父节点的所有子节点

            if (allSub.Count() > 0)             //如果存在子节点,则order+1
            {
                int maxOrder = allSub.Max(t => t.Int("nodeOrder"));
                nodeOrder = (maxOrder + 1).ToString();
            }
            else                                //否则order为1
            {
                nodeOrder = "1";
            }

            if (parentEntity != null)             //存在父节点
            {
                nodeKey = parentEntity.String("nodeKey") + "." + nodeOrder.PadLeft(6, '0');
            }
            else                                    //父节点为空(即父节点为根节点)
            {
                nodeKey = nodeOrder.PadLeft(6, '0');
            }

            retDoc.Add("nodePid", nodePid);
            retDoc.Add("nodeLevel", nodeLevel);
            retDoc.Add("nodeOrder", nodeOrder);
            retDoc.Add("nodeKey", nodeKey);

            return retDoc;
        }

        /// <summary>
        /// 获取当前操作用户
        /// </summary>
        /// <returns></returns>
        private void SetCurrentUser()
        {
            if (System.Web.HttpContext.Current != null)
            {
                if (string.IsNullOrEmpty(PageReq.GetSession("UserId")) == false)
                {
                    _currentUserId = Int32.Parse(PageReq.GetSession("UserId"));

                    if (string.IsNullOrEmpty(PageReq.GetSession("UserName")) == false)
                    {
                        _currentUserName = PageReq.GetSession("UserName");
                    }
                    else
                    {
                        BsonDocument user = this.FindOneByKeyVal("SysUser", "userId", _currentUserId.ToString());
                        _currentUserName = user != null ? user.String("name") : "";
                    }
                }
            }
            else
            {
                _currentUserId = -1;
                _currentUserName = "";
            }
        }

        #endregion

        #region 快捷更新
        /// <summary>
        /// 快捷更新操作
        /// </summary>
        /// <param name="tbName">表名Task表</param>
        /// <param name="QueryKey">字段taskId</param>
        /// <param name="QueryValue">值123</param>
        /// <param name="SetValueStr">设置值remark=123&pointId=2&1=3</param>
        /// <returns></returns>
        public InvokeResult QuickUpdate(string tbName, string QueryKey, string QueryValue, string SetValueStr)
        {
            var updateStr = GetFindOneQueryStr(tbName, QueryKey, QueryValue);

            return this.Update(tbName, updateStr, SetValueStr);
        }

        /// <summary>
        /// 快捷更新操作
        /// </summary>
        /// <param name="tbName">表名Task表</param>
        /// <param name="QueryKey">字段taskId</param>
        /// <param name="QueryValue">值123</param>
        /// <param name="SetValueStr">设置值remark=123&pointId=2&1=3</param>
        /// <returns></returns>
        public InvokeResult QuickUpdate(string tbName, BsonDocument bsonDoc, string updateQueryStr)
        {
            var result = new InvokeResult() { Status = Status.Successful };
            BsonDocument dataBson = TypeConvert.ParamStrToBsonDocument(updateQueryStr);
            result = Update(bsonDoc, dataBson);
            return result;
        }

        /// <summary>
        /// 快捷更新操作
        /// </summary>
        /// <param name="tbName">表名Task表</param>
        /// <param name="QueryKey">字段taskId</param>
        /// <param name="QueryValue">值123</param>
        /// <param name="SetValueStr">设置值remark=123&pointId=2&1=3</param>
        /// <returns></returns>
        public InvokeResult QuickUpdate(string tbName, List<BsonDocument> bsonDocList, string updateQueryStr)
        {
            var result = new InvokeResult() { Status = Status.Successful };
            var StorageDataList = new List<StorageData>();
            using (TransactionScope tran = new TransactionScope())
            {
                foreach (var bsonDoc in bsonDocList)
                {
                    var curTbName = bsonDoc.TableName();
                    var keyFiled = bsonDoc.KeyField();
                    var keyValue = bsonDoc.KeyValue();

                    BsonDocument dataBson = TypeConvert.ParamStrToBsonDocument(updateQueryStr);
                    var query = Query.EQ(keyFiled, keyValue);

                    StorageData storageData = new StorageData();
                    storageData.Name = tbName;
                    storageData.Query = query;
                    storageData.Type = StorageType.Update;
                    storageData.Document = dataBson;
                    StorageDataList.Add(storageData);
                }
                result = this.SaveStorageData(StorageDataList);
                tran.Complete();
                return result;
            }
        }

        /// <summary>
        /// 快捷更新操作
        /// </summary>
        /// <param name="tbName">表名Task表</param>
        /// <param name="QueryKey">字段taskId</param>
        /// <param name="QueryValue">值123</param>
        /// <param name="SetValueStr">设置值remark=123&pointId=2&1=3</param>
        /// <returns></returns>
        public InvokeResult QuickUpdate(string tbName, Dictionary<BsonDocument, string> uploadList)
        {
            var result = new InvokeResult() { Status = Status.Successful };
            var StorageDataList = new List<StorageData>();
            using (TransactionScope tran = new TransactionScope())
            {
                foreach (var obj in uploadList)
                {
                    var bsonDoc = obj.Key;
                    var updateQueryStr = obj.Value;
                    var curTbName = bsonDoc.TableName();
                    var keyFiled = bsonDoc.KeyField();
                    var keyValue = bsonDoc.KeyValue();

                    BsonDocument dataBson = TypeConvert.ParamStrToBsonDocument(updateQueryStr);
                    var query = Query.EQ(keyFiled, keyValue);

                    StorageData storageData = new StorageData();
                    storageData.Name = tbName;
                    storageData.Query = query;
                    storageData.Type = StorageType.Update;
                    storageData.Document = dataBson;
                    StorageDataList.Add(storageData);
                }
                result = this.SaveStorageData(StorageDataList);
                tran.Complete();
            }
            return result;
        }

        /// <summary>
        /// 快捷更新操作
        /// </summary>
        /// <param name="tbName">表名Task表</param>
        /// <param name="QueryKey">字段taskId</param>
        /// <param name="QueryValue">值123</param>
        /// <param name="SetValueStr">设置值remark=123&pointId=2&1=3</param>
        /// <returns></returns>
        public InvokeResult QuickDelete(string tbName, List<BsonDocument> dataBsonList)
        {
            var result = new InvokeResult() { Status = Status.Successful };
            var StorageDataList = new List<StorageData>();
            using (TransactionScope tran = new TransactionScope())
            {
                foreach (var bsonDoc in dataBsonList)
                {
                    var curTbName = bsonDoc.TableName();
                    var keyFiled = bsonDoc.KeyField();
                    var keyValue = bsonDoc.KeyValue();

                    var query = Query.EQ(keyFiled, keyValue);

                    StorageData storageData = new StorageData();
                    storageData.Name = tbName;
                    storageData.Query = query;
                    storageData.Type = StorageType.Delete;

                    StorageDataList.Add(storageData);
                }
                result = this.SaveStorageData(StorageDataList);
                tran.Complete();
            }
            return result;
        }

        /// <summary>
        /// 插入数据
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="dataBson"></param>
        /// <returns></returns>
        public InvokeResult QuickInsert(string tbName, List<BsonDocument> dataBsonList)
        {
            var result = new InvokeResult() { Status = Status.Successful };
            var StorageDataList = new List<StorageData>();
            using (TransactionScope tran = new TransactionScope())
            {
                foreach (var bsonDoc in dataBsonList)
                {
                    StorageData storageData = new StorageData();
                    storageData.Name = tbName;
                    storageData.Type = StorageType.Insert;
                    storageData.Document = bsonDoc;
                    StorageDataList.Add(storageData);
                }
                result = this.SaveStorageData(StorageDataList);
                tran.Complete();
            }
            return result;
        }

        /// <summary>
        /// 删除操作
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="keyFiled">主键</param>
        /// <param name="keyValue">值</param>
        /// <returns></returns>
        public InvokeResult QuickDelete(string tbName, string keyFiled, string keyValue)
        {
            var queryStr = GetFindOneQueryStr(tbName, keyFiled, keyValue);
            return this.Delete(tbName, queryStr);
        }


        #endregion

        #region 帮助函数
        /// <summary>
        /// 返回单个查询字符串
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="keyFiled"></param>
        /// <param name="keyValue"></param>
        /// <returns></returns>
        private string GetFindOneQueryStr(string tbName, string keyFiled, string keyValue)
        {
            var queryStr = "db." + tbName + ".findOne({";
            queryStr += string.Format("'{0}':'{1}'", keyFiled, keyValue);
            queryStr += "})";
            return queryStr;
        }
        #endregion
    }

    /// <summary>
    /// mongo表名查询参数实体
    /// </summary>
    public class MutualData
    {
        /// <summary>
        /// 表名
        /// </summary>
        public string tbName { get; set; }

        /// <summary>
        /// 操作类型
        /// </summary>
        public string queryStr { get; set; }

        /// <summary>
        /// 定位关键字
        /// </summary>
        public string dataStr { get; set; }
    }
}
