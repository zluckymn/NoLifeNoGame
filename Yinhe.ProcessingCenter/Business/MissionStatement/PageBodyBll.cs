using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using Yinhe.ProcessingCenter.DataRule;
using MongoDB.Bson;
using Yinhe.ProcessingCenter;
namespace Yinhoo.Autolink.Business.MissionStatement
{
    /// <summary>
    /// 页面内容处理类
    /// </summary>
    public class PageBodyBll
    {
        #region 构造函数
        /// <summary>
        /// 类私有变量
        /// </summary>
        private DataOperation _ctx = null;

        /// <summary>
        /// 封闭当前默认构造函数
        /// </summary>
        private PageBodyBll()
        {
            _ctx = new DataOperation();
        }

        private PageBodyBll(DataOperation ctx)
        {
            _ctx = ctx;
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static PageBodyBll _()
        {
            return new PageBodyBll();
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static PageBodyBll _(DataOperation ctx)
        {
            return new PageBodyBll(ctx);
        }
        #endregion

        #region 查找
        /// <summary>
        /// 通过主键查找
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public BsonDocument FindById(int id)
        {
            var hitObj = this._ctx.FindOneByKeyVal("PageBody", "bodyId", id.ToString());
            return hitObj;
        }
        /// <summary>
        /// 通过pageId查找
        /// </summary>
        /// <param name="pageId"></param>
        /// <returns></returns>
        public BsonDocument FindByPageId(int pageId) 
        {
            var hitObj = this._ctx.FindOneByKeyVal("PageBody", "pageId", pageId.ToString());
            return hitObj;
        }
        /// <summary>
        /// 获取最新版本的页面内容
        /// </summary>
        /// <param name="pageId"></param>
        /// <returns></returns>
        public BsonDocument FindLatestVersionByPageId(int pageId, int version)
        {

           var queryStr = string.Format("bookId={0}&lastVersion={1}", pageId, version);
            return this._ctx.FindOneByQueryStr("PageBody", queryStr);
            
        }
        /// <summary>
        /// 查找所有
        /// </summary>
        /// <returns></returns>
        public IQueryable<BsonDocument> FindAll()
        {
             return this._ctx.FindAll("PageBody").AsQueryable();
        }

        #endregion

        #region 操作
        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public InvokeResult Update(BsonDocument entity)
        {
            InvokeResult result = new InvokeResult();
            try
            {
                var oldBson = FindById(entity.Int("bodyId"));
                if (oldBson != null)
                {
                    result = this._ctx.Update(oldBson,entity);
                }
                result.Status = Status.Successful;
            }
            catch(Exception ex)
            {
                result.Status = Status.Failed;
                result.Message = ex.Message;
            }
            return result;
        }

        public InvokeResult Update(BsonDocument entity,BsonDocument updateBson)
        {
            InvokeResult result = new InvokeResult();
            try
            {
               result = this._ctx.Update(entity, updateBson);
               result.Status = Status.Successful;
            }
            catch (Exception ex)
            {
                result.Status = Status.Failed;
                result.Message = ex.Message;
            }
            return result;
        }
        /// <summary>
        /// 新增
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public InvokeResult  Insert(BsonDocument entity)
        {
            if (entity == null) throw new ArgumentNullException();
            InvokeResult  result = new InvokeResult  ();
            try
            {
                result = _ctx.Insert("PageBody", entity);

                return result;
            }
            catch(Exception ex)
            {
                result.Status = Status.Failed;
                result.Message = ex.Message;
            }
             return result;
        }

       /// <summary>
        /// 保存内容
       /// </summary>
       /// <param name="entity"></param>
       /// <param name="refPageId"></param>
       /// <param name="tagList"></param>
       /// <param name="strTag"></param>
       /// <param name="retIds">成果Id</param>
       /// <returns></returns>
        public InvokeResult Save(BsonDocument entity, int refPageId, List<BsonDocument> tagList, string strTag, int[] retIds)
        {
            InvokeResult result = new InvokeResult();
            BookPageBll pageBll = BookPageBll._(this._ctx);
            //ObjectEntityTagBll tagBll = ObjectEntityTagBll._(this._ctx);
            BsonDocument curAvaiableEntity = entity;
             try
            {
                using (TransactionScope scope = new TransactionScope())
                {
                    var oldBody =this._ctx.FindOneByKeyVal("PageBody","pageId",entity.Text("pageId"));
                    if (oldBody != null)
                    {
                        var updateBson = new BsonDocument();
                        updateBson.Add("body", entity.Text("body"));
                        result=this._ctx.Update(oldBody, updateBson);
                        if (result.Status != Status.Successful)
                        {
                            return result;
                        }
                        curAvaiableEntity = result.BsonInfo;
                        var oldPage = oldBody.SourceBson("pageId");
                        var pageUpdateBson = new BsonDocument();
                        pageUpdateBson.Add("customTag", strTag);
                        result = _ctx.Update(oldPage, pageUpdateBson);
                        if (result.Status != Status.Successful)
                        {
                            return result;
                        }
                     
                      }
                    else
                    {
                        result=_ctx.Insert("PageBody",entity);
                        if (result.Status != Status.Successful)
                        {
                            return result;
                        }
                     }
                   
                 
                    var bodyId = oldBody == null ? entity.Int("bodyId") : oldBody.Int("bodyId");
                    var query = this._ctx.FindAllByKeyVal("PageBodyVersion","pageId",entity.Text("pageId"));
                    var Max = query.Count() > 0 ? query.Max(c => c.Int("bodyVersion")) : 0;
                    var versionId = Max + 1;
                    var version = new BsonDocument();
                    version.Add("pageId", curAvaiableEntity.Text("pageId"));
                    version.Add("bodyId", bodyId);
                    version.Add("userId", curAvaiableEntity.Text("createUserId"));
                    version.Add("body", curAvaiableEntity.Text("body"));
                    version.Add("bodyVersion", versionId);
                    result=this._ctx.Insert("PageBodyVersion",version);
                    if (result.Status != Status.Successful)
                    {
                        return result;
                    }

                    var versionUpdateBson = new BsonDocument();
                     versionUpdateBson.Add("lastVersion",versionId);
                     if (oldBody != null)
                         _ctx.Update(oldBody, versionUpdateBson);
                     else
                         _ctx.Update(entity, versionUpdateBson);


                    var page = this._ctx.FindOneByKeyVal("BookPage","pageId", entity.Text("pageId"));
                    if (page != null)
                    {
                        var lastVersionBson = new BsonDocument();
                        lastVersionBson.Add("lastVersion", versionId);
                        _ctx.Update(page, versionUpdateBson);
                    }
                  
                  
                    var deletePageTempBody = _ctx.FindAllByKeyVal("PageTempBody", "pageId", entity.Text("pageId"));
                    result=this._ctx.QuickDelete("PageTempBody", deletePageTempBody.ToList());
                    if (result.Status == Status.Successful)
                    {
                        return result;
                    }
                    //var oriTagList = tagBll.FindAll(3, entity.pageId).ToList();
                    //#region 删除标签索引
                    //tagBll.DeleteIndex(oriTagList);//删除标签索引
                    //#endregion
                    //this._ctx.ObjectEntityTags.DeleteAllOnSubmit(oriTagList);
                    ////添加标签
                    //if (tagList.Count() > 0)
                    //{
                    //    this._ctx.ObjectEntityTags.InsertAllOnSubmit(tagList);
                    //    this._ctx.SubmitChanges();

                    //}
                    #region 添加成果关联 2010.12.31更新
                    PageRefResultBll refRetBll = PageRefResultBll._(this._ctx);
                    var tempResult=refRetBll.Insert(entity.Int("pageId"), retIds);
                    if (tempResult.Status != Status.Successful)
                    {
                        return tempResult;
                    }
                    #endregion
                    scope.Complete();
                }
                result.Status = Status.Successful;
                result.Message = "保存成功!";
              try
                {
                    var BookPage = pageBll.FindById(entity.Int("pageId"));
                    // pageBll.SubsribeUpdateObjInfo(curAvaiableEntity);//添加标签订阅相关；
                    //pageBll.BuildIndex(BookPage);//添加索引
                    // tagBll.BuildIndex(tagList);//添加标签索引
                    BookPageBll._().GenerateSnapshot(entity.Int("pageId"));
                }
                catch (Exception ex)
                { 
                
                }
            }
            catch (Exception ex)
            {
                result.Status = Status.Failed;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public InvokeResult Delete(int id)
        {
            InvokeResult result = new InvokeResult() { Status = Status.Successful };

            try
            {
                var entity = this.FindById(id);

                if (entity == null) throw new ArgumentNullException();

                using (TransactionScope tran = new TransactionScope())
                {
                   this._ctx.Delete("PageBody", string.Format("bodyId={0}", entity.Int("bodyId")));
                   tran.Complete();
                }
            }
            catch (Exception ex)
            {
                result.Status = Status.Failed;
                result.Message = ex.Message;
            }

            return result;
        }

        #endregion
    }
}
