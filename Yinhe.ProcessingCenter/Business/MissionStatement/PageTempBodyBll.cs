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
    /// 页面临时内容处理类
    /// </summary>
    public class PageTempBodyBll
    {
        #region 构造函数
        /// <summary>
        /// 类私有变量
        /// </summary>
        private DataOperation _ctx = null;

        /// <summary>
        /// 封闭当前默认构造函数
        /// </summary>
        private PageTempBodyBll()
        {
            _ctx = new DataOperation();
        }

        private PageTempBodyBll(DataOperation ctx)
        {
            _ctx = ctx;
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static PageTempBodyBll _()
        {
            return new PageTempBodyBll();
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static PageTempBodyBll _(DataOperation ctx)
        {
            return new PageTempBodyBll(ctx);
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
            var hitObj = this._ctx.FindOneByKeyVal("PageTempBody", "tempBodyId", id.ToString());
            return hitObj;
        }

        /// <summary>
        /// 查找所有
        /// </summary>
        /// <returns></returns>
        public IQueryable<BsonDocument> FindAll()
        {
            return this._ctx.FindAll("PageTempBody").AsQueryable();
        }

        public BsonDocument FindByPageIdUserId(int pageId, int currUserId) 
        {
            var queryStr = string.Format("pageId={0}&saveUserId={1}", pageId, currUserId);
            return this._ctx.FindOneByQueryStr("PageTempBody", queryStr);
        }

        public int FindByPageIdUserIdCount(int pageId, int currUserId)
        {
           var queryStr=string.Format("pageId={0}&saveUserId={1}",pageId,currUserId);
           var count = this._ctx.FindAllByQueryStr("PageTempBody", queryStr).Count();
            return (int)count;
        }

        #endregion

        #region 操作
        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public InvokeResult  Update(BsonDocument entity,BsonDocument updateBson)
        {
            InvokeResult result = new InvokeResult ();
            try
            {
                result = this._ctx.Update(entity, updateBson);
                
            }
            catch(Exception ex)
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
            InvokeResult  result = new InvokeResult ();
            try
            {
               result = this._ctx.Insert("PageTempBody", entity);
               return result;
            }
            catch(Exception ex)
            {
                result.Status = Status.Failed;
                result.Message = "原始页面已被删除,保存失败";
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
                using (TransactionScope tran = new TransactionScope())
                {
                    result = this._ctx.Delete("PageTempBody", string.Format("tempBodyId={0}", id.ToString()));
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
