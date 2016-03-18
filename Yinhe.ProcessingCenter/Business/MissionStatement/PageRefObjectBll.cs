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
    /// 任务书页面引用对象处理类
    /// </summary>
    public class PageRefObjectBll
    {
        #region 构造函数
        /// <summary>
        /// 类私有变量
        /// </summary>
        private DataOperation _ctx = null;

        /// <summary>
        /// 封闭当前默认构造函数
        /// </summary>
        private PageRefObjectBll()
        {
            _ctx = new DataOperation();
        }

        private PageRefObjectBll(DataOperation ctx)
        {
            _ctx = ctx;
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static PageRefObjectBll _()
        {
            return new PageRefObjectBll();
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static PageRefObjectBll _(DataOperation ctx)
        {
            return new PageRefObjectBll(ctx);
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
            var hitObj = this._ctx.FindOneByKeyVal("PageRefObject", "pageRefObjId", id.ToString());
            return hitObj;
        }

        /// <summary>
        /// 查找所有
        /// </summary>
        /// <returns></returns>
        public IQueryable<BsonDocument> FindAll()
        {
            return this._ctx.FindAll("PageRefObject").AsQueryable();
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
                var oldBson = FindById(entity.Int("pageRefObjId"));
                if (oldBson != null)
                {
                    result = this._ctx.Update(oldBson, entity);
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

        /// <summary>
        /// 新增
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public InvokeResult  Insert(BsonDocument entity)
        {
            if (entity == null) throw new ArgumentNullException();
            InvokeResult  result = new InvokeResult ();
            try
            {
                result = this._ctx.Insert("PageRefObject", entity);
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
                    result = this._ctx.Delete("PageRefObject", string.Format("pageRefObjId={0}", id));
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
