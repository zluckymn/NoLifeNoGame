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
    /// 页面内容版本处理类
    /// </summary>
    public class PageBodyVersionBll
    {
        #region 构造函数
        /// <summary>
        /// 类私有变量
        /// </summary>
        private DataOperation _ctx = null;

        /// <summary>
        /// 封闭当前默认构造函数
        /// </summary>
        private PageBodyVersionBll()
        {
            _ctx = new DataOperation();
        }

        private PageBodyVersionBll(DataOperation ctx)
        {
            _ctx = ctx;
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static PageBodyVersionBll _()
        {
            return new PageBodyVersionBll();
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static PageBodyVersionBll _(DataOperation ctx)
        {
            return new PageBodyVersionBll(ctx);
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
            var hitObj = this._ctx.FindOneByKeyVal("PageBodyVersion", "bodyVerId", id.ToString());
            return hitObj;
        }

        public BsonDocument FindByPageIdAndVer(int pageId, int version)
        {
            var queryStr = string.Format("pageId={0}&bodyVersion={1}", pageId, version);
            return this._ctx.FindOneByQueryStr("PageBody", queryStr);
        }

        /// <summary>
        /// 查找所有
        /// </summary>
        /// <returns></returns>
        public IQueryable<BsonDocument> FindAll()
        {
            return this._ctx.FindAll("PageBodyVersion").AsQueryable();
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
            InvokeResult  result = new InvokeResult ();
            try
            {
                var oldBson = FindById(entity.Int("bodyVerId"));
                if (oldBson != null)
                {
                    result = this._ctx.Update(oldBson, entity);
                }
                result.Status = Status.Successful;
               
            }
            catch (Exception ex)
            {
                result.Status = Status.Failed;
                result.Message = ex.Message;
            }

            result.BsonInfo = entity;

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
            InvokeResult result = new InvokeResult ();
            try
            {
                this._ctx.Insert("PageBodyVersion",entity);
                return result;
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
                  
                    this._ctx.Delete("PageBodyVersion", string.Format("bodyVerId={0}", entity.Int("bodyVerId")));
                    tran.Complete();

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


        public InvokeResult Delete(List<string> Ids)
        {
            InvokeResult result = new InvokeResult() { Status = Status.Successful };

            try
            {
                var entityList = this._ctx.FindAllByKeyValList("PageBodyVersion", "bodyVerId", Ids).ToList();
         
                if (entityList.Count() > 0)
                {
                    this._ctx.QuickDelete("PageBodyVersion", entityList);
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
        /// 更新页面版本
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public InvokeResult RecoveryBodyVersion(int Id, int sysUserId)
        {
            InvokeResult result = new InvokeResult() { Status = Status.Successful };
            PageBodyBll bodyBll = PageBodyBll._(this._ctx);
            BookPageBll pageBll = BookPageBll._(this._ctx);
            var bVersion = FindById(Id);
            var Page = bVersion.SourceBson("pageId");
            if (bVersion != null && Page != null)
            {
                var currentBody = Page.ChildBsonList("PageBody").FirstOrDefault();
                //currentBody.lastVersion = currentBody.lastVersion + 1;
                if (currentBody == null)
                {
                    result.Status = Status.Failed;
                    result.Message = "该对象被删除，请刷新后重试";
                    return result;
                }
                var currentBodyUpdateBson = new BsonDocument();
                currentBodyUpdateBson.Add("body", bVersion.Text("body"));
                currentBodyUpdateBson.Add("lastVersion", currentBody.Int("lastVersion") + 1);
                var newBodyVersion = new BsonDocument();
                newBodyVersion.Add("body", bVersion.Text("body"));
                newBodyVersion.Add("userId", sysUserId);
                newBodyVersion.Add("bodyVersion", currentBody.Int("lastVersion") + 1);
                newBodyVersion.Add("bodyId", bVersion.Text("bodyId"));
                this._ctx.Update(Page,string.Format("lastVersion={0}",currentBody.Int("lastVersion")+1));
                using (TransactionScope tran = new TransactionScope())
                {
                    var draft = Page.ChildBsonList("PageTempBody").Where(r=>r.Int("saveUserId") == sysUserId).FirstOrDefault();
                    if (draft !=null) 
                    {
                        var delRet = this._ctx.Delete("PageTempBody", string.Format("tempBodyId={0}", draft.Text("tempBodyId")));
                    }
                    result=this._ctx.Insert("PageBodyVersion", newBodyVersion);
                    result = bodyBll.Update(currentBody, currentBodyUpdateBson);
                   
                    tran.Complete();
                }
                try
                {
                    pageBll.GenerateSnapshot(Page.Int("pageId"));
                    //pageBll.BuildIndex(Page);
                }
                catch (Exception ex)
                {

                }

               
            }
            else
            {
                result.Status = Status.Failed;
                result.Message = "该版本可能已经被刷新请重新刷新！";
            }
            return result;


        }

        #endregion
    }
}
