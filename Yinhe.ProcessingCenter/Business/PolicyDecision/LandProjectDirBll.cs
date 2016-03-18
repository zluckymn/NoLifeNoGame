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
    /// 地块处理类
    /// </summary>
    public class LandProjectDirBll
    {
        #region 构造函数
        /// <summary>
        /// 类私有变量
        /// </summary>
        private DataOperation _ctx = null;

        /// <summary>
        /// 封闭当前默认构造函数
        /// </summary>
        public LandProjectDirBll()
        {
            _ctx = new DataOperation();
        }

        public LandProjectDirBll(DataOperation ctx)
        {
            _ctx = ctx;
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static LandProjectDirBll _()
        {
            return new LandProjectDirBll();
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static LandProjectDirBll _(DataOperation ctx)
        {
            return new LandProjectDirBll(ctx);
        }
        #endregion
        #region 操作
        /// <summary>
        /// 创建地块目录
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="landId"></param>
        /// <param name="templateId"></param>
        public void LandDirCreate(string tbName, int landId, int templateId)
        {
            InvokeResult result = new InvokeResult();
            TableRule tableEntity = new TableRule(tbName);
            string primaryKey = tableEntity.GetPrimaryKey();
            if (string.IsNullOrEmpty(primaryKey))
            {
                throw new ArgumentNullException();
            }
            BsonDocument templateInfo = this._ctx.FindOneByQuery(tbName, Query.EQ(primaryKey, templateId.ToString()));
            if (templateInfo == null)
            {
                templateInfo = this._ctx.FindOneByQuery(tbName, Query.EQ("isTemplate", "1"));//传入模板Id不存在，查找默认模板
            }
            if (templateInfo != null)
            {
                templateId = templateInfo.Int(primaryKey);//模板Id
                List<BsonDocument> templateDataSubList = new List<BsonDocument>();//模板详细目录
                BsonDocument land = this._ctx.FindOneByQuery("Land", Query.EQ("landId", landId.ToString()));//地块信息
                BsonDocument entity = this._ctx.FindOneByQuery(tbName, Query.EQ("landId", landId.ToString()));//地块对应表目录实体
                if (entity == null)
                {
                    entity = new BsonDocument();
                    entity.Add("name", land != null ? land.String("name") : string.Empty + "目录");
                    entity.Add("landId", landId.ToString());
                    entity.Add("isTemplate", "0");
                    entity.Add("srcId", templateId.ToString());
                    result = this._ctx.Insert(tbName, entity);
                    entity = result.BsonInfo;
                }
                List<BsonDocument> dataSubList = this._ctx.FindAllByQuery(tbName + "Dir", Query.EQ(primaryKey, entity.String(primaryKey))).ToList();
                if (dataSubList.Count() > 0)
                {
                    this._ctx.Delete(tbName + "Dir", Query.EQ(primaryKey, entity.String(primaryKey)));
                }
                if (result.Status == Status.Successful || result.Status == Status.Invaild)
                {
                    templateDataSubList = this._ctx.FindAllByQuery(tbName + "Dir", Query.EQ(primaryKey, templateId.ToString())).ToList();
                    TemplateDirBll tdBll = TemplateDirBll._();
                    List<BsonDocument> newDir = new List<BsonDocument>();
                    tdBll.CopyGeneralDir(newDir, templateDataSubList, 0, null, tbName, entity);
                }
                else
                {
                    throw new ArgumentNullException();
                }

            }
        }
        /// <summary>
        /// 初始化地块信息
        /// </summary>
        /// <param name="landId"></param>
        /// <param name="?"></param>
        public void LandInfoInit(int landId, Dictionary<string, int> templateDC)
        {
            InvokeResult result = new InvokeResult();
            BsonDocument landInfo = this._ctx.FindOneByQuery("Land", Query.EQ("landId", landId.ToString()));//项目信息
            if (landInfo == null)
            {
                throw new ArgumentNullException();
            }
            foreach (var dc in templateDC)
            {
                int templateId = dc.Value;
                TableRule tableEntity = new TableRule(dc.Key);
                string primaryKey = tableEntity.GetPrimaryKey();
                if (string.IsNullOrEmpty(primaryKey))
                {
                    throw new ArgumentNullException();

                }
                BsonDocument templateInfo = this._ctx.FindOneByQuery(dc.Key, Query.EQ(primaryKey, dc.Value.ToString()));
                if (templateInfo == null)
                {
                    templateInfo = this._ctx.FindOneByQuery(dc.Key, Query.EQ("isTemplate", "1"));//传入模板Id不存在，查找默认模板
                }
                if (templateInfo != null)
                {
                    templateId = templateInfo.Int(primaryKey);//模板Id
                    List<BsonDocument> templateDataSubList = new List<BsonDocument>();//模板详细目录

                    BsonDocument entity = this._ctx.FindOneByQuery(dc.Key, Query.EQ("landId", landId.ToString()));//项目对应表目录实体
                    if (entity == null)
                    {
                        entity = new BsonDocument();
                        entity.Add("name", landInfo != null ? landInfo.String("name") : string.Empty + "目录");
                        entity.Add("landId", landId.ToString());
                        entity.Add("isTemplate", "0");
                        entity.Add("srcId", templateId.ToString());
                        result = this._ctx.Insert(dc.Key, entity);
                        entity = result.BsonInfo;
                    }
                    List<BsonDocument> dataSubList = this._ctx.FindAllByQuery(dc.Key + "Dir", Query.EQ(primaryKey, entity.String(primaryKey))).ToList();
                    if (dataSubList.Count() > 0)
                    {
                        this._ctx.Delete(dc.Key + "Dir", Query.EQ(primaryKey, entity.String(primaryKey)));//删除旧信息
                    }
                    if (result.Status == Status.Successful || result.Status == Status.Invaild)
                    {
                        templateDataSubList = this._ctx.FindAllByQuery(dc.Key + "Dir", Query.EQ(primaryKey, templateId.ToString())).ToList();
                        TemplateDirBll tdBll = TemplateDirBll._();
                        List<BsonDocument> newDir = new List<BsonDocument>();
                        tdBll.CopyGeneralDir(newDir, templateDataSubList, 0, null, dc.Key, entity);
                    }
                    else
                    {
                        throw new ArgumentNullException();
                    }

                }
            }
        }
        /// <summary>
        /// 初始化项目信息
        /// </summary>
        /// <param name="projId"></param>
        /// <param name="templateDC">传入表名和对应的模板Id</param>
        public void ProjectInfoInit(int projId, Dictionary<string, int> templateDC)
        {
            InvokeResult result = new InvokeResult();
            BsonDocument project = this._ctx.FindOneByQuery("Project", Query.EQ("projId", projId.ToString()));//项目信息
            if (project == null) { throw new ArgumentNullException();
            }
            foreach (var dc in templateDC)
            {
                int templateId = dc.Value;
                TableRule tableEntity = new TableRule(dc.Key);
                string primaryKey = tableEntity.GetPrimaryKey();
                if (string.IsNullOrEmpty(primaryKey))
                {
                    throw new ArgumentNullException();

                }
                BsonDocument templateInfo = this._ctx.FindOneByQuery(dc.Key, Query.EQ(primaryKey, dc.Value.ToString()));
                if (templateInfo == null)
                {
                    templateInfo = this._ctx.FindOneByQuery(dc.Key, Query.EQ("isTemplate", "1"));//传入模板Id不存在，查找默认模板
                }
                if (templateInfo != null)
                {
                    templateId = templateInfo.Int(primaryKey);//模板Id
                    List<BsonDocument> templateDataSubList = new List<BsonDocument>();//模板详细目录

                    BsonDocument entity = this._ctx.FindOneByQuery(dc.Key, Query.EQ("projId", projId.ToString()));//项目对应表目录实体
                    if (entity == null)
                    {
                        entity = new BsonDocument();
                        entity.Add("name", project != null ? project.String("name") : string.Empty + "目录");
                        entity.Add("projId", projId.ToString());
                        entity.Add("isTemplate", "0");
                        entity.Add("srcId", templateId.ToString());
                        result = this._ctx.Insert(dc.Key, entity);
                        entity = result.BsonInfo;
                    }
                    List<BsonDocument> dataSubList = this._ctx.FindAllByQuery(dc.Key + "Dir", Query.EQ(primaryKey, entity.String(primaryKey))).ToList();
                    if (dataSubList.Count() > 0)
                    {
                        this._ctx.Delete(dc.Key + "Dir", Query.EQ(primaryKey, entity.String(primaryKey)));//删除旧信息
                    }
                    if (result.Status == Status.Successful || result.Status == Status.Invaild)
                    {
                        templateDataSubList = this._ctx.FindAllByQuery(dc.Key + "Dir", Query.EQ(primaryKey, templateId.ToString())).ToList();
                        TemplateDirBll tdBll = TemplateDirBll._();
                        List<BsonDocument> newDir = new List<BsonDocument>();
                        tdBll.CopyGeneralDir(newDir, templateDataSubList, 0, null, dc.Key, entity);
                    }
                    else
                    {
                        throw new ArgumentNullException();
                    }

                }
            }
        }
        #endregion
    }
}
