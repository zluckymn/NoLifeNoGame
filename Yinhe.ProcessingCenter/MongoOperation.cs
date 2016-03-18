using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace Yinhe.ProcessingCenter
{
   /// <summary>
    /// Mongos数据连接操作
   /// </summary>
    public class MongoOperation
    {
        #region 私有字段
        private MongoServer _mongoServer = null;        //当前连接的数据库服务器
        private MongoDatabase _mongoDatabase = null;
        #endregion

        #region 构造函数
        /// <summary>
        /// 默认构造函数,读取配置项中的数据库
        /// </summary>
        public MongoOperation()
        {
            if (_mongoServer == null || _mongoDatabase == null)
            {
                string connStr = "mongodb://sa:dba@localhost/A3";       //默认数据库连接串

                if (SysAppConfig.DataBaseConnectionString != "")      //读取数据库连接串
                {
                    connStr = SysAppConfig.DataBaseConnectionString;       //默认数据库连接串
                }

                ConnectionDataBase(connStr);        //连接数据库
            }
        }

        /// <summary>
        /// 构造函数,串入数据库连接串,进行数据库连接
        /// </summary>
        /// <param name="connectionString"></param>
        public MongoOperation(string connStr)
        {
            if (_mongoServer == null || _mongoDatabase == null)
            {
                if (string.IsNullOrEmpty(connStr)) throw new Exception("数据库连接串为空!!");

                ConnectionDataBase(connStr);    //连接数据库
            }
        }

        /// <summary>
        /// 连接数据库
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public bool ConnectionDataBase(string connStr)
        {
            bool isSuccess = false;

            try
            {
                _mongoDatabase = MongoDatabase.Create(connStr);    //获取连接字符串所对应数据库
                _mongoServer = _mongoDatabase.Server;                       //获取数据库对应的服务器
            }
            catch
            {
                isSuccess = false;
            }

            return isSuccess;
        }

        /// <summary>
        /// 切换使用的数据库
        /// </summary>
        /// <param name="dbName">切换的数据库名称</param>
        /// <returns></returns>
        public bool UseDataBase(string dbName)
        {
            bool isSuccess = true;

            try
            {
                _mongoDatabase = _mongoServer.GetDatabase(dbName);      //切换数据库
            }
            catch
            {
                isSuccess = false;
            }

            return isSuccess;
        }

        /// <summary>
        /// 获取当前服务器
        /// </summary>
        /// <returns></returns>
        public MongoServer GetServer()
        {
            return this._mongoServer;
        }

        /// <summary>
        /// 获取当前数据库
        /// </summary>
        /// <returns></returns>
        public MongoDatabase GetDataBase()
        {
            return this._mongoDatabase;
        }

        /// <summary>
        /// 获取所需集合
        /// </summary>
        /// <param name="collName"></param>
        /// <returns></returns>
        public MongoCollection<BsonDocument> GetCollection(string collName)
        {
            return this._mongoDatabase.GetCollection(collName);
        }

        #endregion

        #region 查询方法
        /// <summary>
        /// 查找集合中的默认第一条记录
        /// </summary>
        /// <param name="collName"></param>
        /// <returns></returns>
        public BsonDocument FindOne(string collName)
        {
            BsonDocument entity = this.GetCollection(collName).FindOne();

            //if (entity == null) entity = new BsonDocument();

            return entity;
        }

        /// <summary>
        /// 根据搜索条件查找一条记录
        /// </summary>
        /// <param name="collName"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public BsonDocument FindOne(string collName, IMongoQuery query)
        {
            BsonDocument entity = this.GetCollection(collName).FindOne(query);

            //if (entity == null) entity = new BsonDocument();

            return entity;
        }

        /// <summary>
        /// 查找集合中所有记录
        /// </summary>
        /// <param name="collName"></param>
        /// <returns></returns>
        public MongoCursor<BsonDocument> FindAll(string collName)
        {
            MongoCursor<BsonDocument> entityCursor = this.GetCollection(collName).FindAll();

            return entityCursor;
        }

        /// <summary>
        /// 根据搜索条件查找多条记录
        /// </summary>
        /// <param name="collName"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public MongoCursor<BsonDocument> FindAll(string collName, IMongoQuery query)
        {
            MongoCursor<BsonDocument> entityCursor = this.GetCollection(collName).Find(query);

            return entityCursor;
        }

        /// <summary>
        /// 执行原生查询
        /// </summary>
        /// <param name="queryStr"></param>
        /// <returns></returns>
        public BsonValue EvalNativeQuery(string queryStr)
        {
            BsonValue resultVal = null;

            BsonJavaScript queryScript = new BsonJavaScript(queryStr);

            try
            {
                resultVal = _mongoDatabase.Eval(queryScript);
            }
            catch
            {
                resultVal = null;
            }

            return resultVal;
        }

        #endregion

        #region 操作方法
        /// <summary>
        /// 插入单条数据
        /// </summary>
        /// <param name="collName"></param>
        /// <param name="saveDoc"></param>
        /// <returns></returns>
        public InvokeResult Save(string collName, BsonDocument saveDoc)
        {
            InvokeResult result = new InvokeResult() { Status = Status.Failed };

            try
            {
                MongoCollection<BsonDocument> entityColl = _mongoDatabase.GetCollection(collName);

                using (_mongoServer.RequestStart(_mongoDatabase))
                {
                    entityColl.Save(saveDoc); //非安全模式,即无返回消息
                    //SafeModeResult safeResult = entityColl.Save(saveDoc, SafeMode.True);
                    //if (safeResult.HasLastErrorMessage) throw new Exception(safeResult.LastErrorMessage);
                }

                result.Status = Status.Successful;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// 批量插入数据
        /// </summary>
        /// <param name="collName"></param>
        /// <param name="saveDocList"></param>
        /// <returns></returns>
        public InvokeResult Save(string collName, List<BsonDocument> saveDocList)
        {
            InvokeResult result = new InvokeResult() { Status = Status.Failed };

            try
            {
                MongoCollection<BsonDocument> entityColl = _mongoDatabase.GetCollection(collName);

                using (_mongoServer.RequestStart(_mongoDatabase))
                {
                    entityColl.Save(saveDocList);//非安全模式,即无返回消息
                    //SafeModeResult safeResult = entityColl.Save(saveDocList, SafeMode.True);
                    //if (safeResult.HasLastErrorMessage) throw new Exception(safeResult.LastErrorMessage);

                }

                result.Status = Status.Successful;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// 更新记录
        /// </summary>
        /// <param name="collName"></param>
        /// <param name="searchDoc">可为空,为空为插入</param>
        /// <param name="saveDoc"></param>
        /// <returns></returns>
        public InvokeResult Save(string collName, IMongoQuery query, BsonDocument saveDoc)
        {
            InvokeResult result = new InvokeResult() { Status = Status.Failed };

            try
            {
                MongoCollection<BsonDocument> entityColl = _mongoDatabase.GetCollection(collName);

                #region 保存到数据库中
                List<BsonDocument> entityList = new List<BsonDocument>();       //获取更新数据

                if (query != null) entityList = entityColl.Find(query).ToList();
                else entityList = entityColl.FindAll().ToList();

                using (_mongoServer.RequestStart(_mongoDatabase))
                {
                    //如果是一条记录,且与查询匹配,且有包含_id,则直接更新记录
                    if (entityList.Count == 1 && saveDoc.ContainsColumn("_id") && (entityList.FirstOrDefault().String("_id") == saveDoc.String("_id")))
                    {
                        entityColl.Save(saveDoc);
                    }
                    else                                //不包含_id,则逐条记录,逐个字段更新
                    {
                        foreach (var entity in entityList)
                        {
                            foreach (var temp in saveDoc.Elements)
                            {
                                entity[temp.Name] = temp.Value;
                            }

                            entityColl.Save(entity); //非安全模式,即无返回消息
                            //SafeModeResult safeResult = entityColl.Save(entity, SafeMode.True);
                            //if (safeResult.HasLastErrorMessage) throw new Exception(safeResult.LastErrorMessage);
                        }
                    }
                }
                #endregion

                result.Status = Status.Successful;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// 删除记录
        /// </summary>
        /// <param name="collName"></param>
        /// <param name="condition"></param>
        /// <returns></returns>
        public InvokeResult Delete(string collName, IMongoQuery query)
        {
            InvokeResult result = new InvokeResult() { Status = Status.Failed };

            try
            {
                MongoCollection<BsonDocument> entityColl = _mongoDatabase.GetCollection(collName);

                using (_mongoServer.RequestStart(_mongoDatabase))
                {
                    entityColl.Remove(query);//非安全模式
                    //SafeModeResult safeResult = entityColl.Remove(query, SafeMode.True);
                    //if (safeResult.HasLastErrorMessage) throw new Exception(safeResult.LastErrorMessage);
                }

                result.Status = Status.Successful;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }

            return result;
        }

        #endregion
    }
}
