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
    /// 页面书签处理类
    /// </summary>
    public class PageMarkBll
    {
        #region 构造函数
        /// <summary>
        /// 类私有变量
        /// </summary>
        private DataOperation _ctx = null;

        /// <summary>
        /// 封闭当前默认构造函数
        /// </summary>
        private PageMarkBll()
        {
            _ctx = new DataOperation();
        }

        private PageMarkBll(DataOperation ctx)
        {
            _ctx = ctx;
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static PageMarkBll _()
        {
            return new PageMarkBll();
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static PageMarkBll _(DataOperation ctx)
        {
            return new PageMarkBll(ctx);
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
            var hitObj = this._ctx.FindOneByKeyVal("PageMark", "markId",  id.ToString());
            return hitObj;
        }

        /// <summary>
        /// 通过页面ID查找书签
        /// </summary>
        /// <param name="id"></param>
        /// <param name="keyWord"></param>
        /// <returns></returns>
        public List<BsonDocument> FindByPageId(int id, string keyWord)
        {
        
            var query = this._ctx.FindAllByKeyVal("PageMark","pageId",id.ToString()).ToList();
            if (!String.IsNullOrEmpty(keyWord))
            {
                query = query.Where(m => m.Text("name").Contains(keyWord)).ToList();
            }
            return query.ToList();
        }

        /// <summary>
        /// 通过任务书ID查找书签
        /// </summary>
        /// <param name="id"></param>
        /// <param name="keyWord"></param>
        /// <returns></returns>
        public List<BsonDocument> FindByBookId(int id, string keyWord)
        {
            List<BsonDocument> pageMarkList = new List<BsonDocument>();
            if (id != 0)
            { 
                 
                var bookTask = this._ctx.FindOneByKeyVal("BookTask", "bookId", id.ToString());
                if (bookTask != null)
                {


                    var list = bookTask.ChildBsonList("BookPage").ToList();
                    foreach (var page in list)
                    {  
                        var tempPageMarkList = page.ChildBsonList("PageMark").AsQueryable();
                        
                        if (!String.IsNullOrEmpty(keyWord))
                            tempPageMarkList = tempPageMarkList.Where(m => m.Text("name").Contains(keyWord));
                        pageMarkList.AddRange(tempPageMarkList.ToList());
                    }
                }
            }
            else
            {
                var templatePages = this._ctx.FindAllByKeyVal("BookPage","IsPageTempate","1").ToList();
                
                foreach (var page in templatePages)
                { 
                     var tempPageMarkList = page.ChildBsonList("PageMark").AsQueryable();
                    if (!String.IsNullOrEmpty(keyWord))
                        tempPageMarkList = tempPageMarkList.Where(m => m.Text("name").Contains(keyWord));
                        pageMarkList.AddRange(tempPageMarkList.ToList());
                }
            }
            return pageMarkList;
        }

        /// <summary>
        /// 查找所有
        /// </summary>
        /// <returns></returns>
        public IQueryable<BsonDocument> FindAll()
        {
           return this._ctx.FindAll("PageMark").AsQueryable();
        }

        #endregion

        #region 操作
        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public InvokeResult  Update(BsonDocument entity)
        {
            InvokeResult result = new InvokeResult ();
            try
            {
                if (!checkName(entity.Int("pageId"), entity.Text("name"), entity.Int("markId")))
                {
                    result.Status = Status.Failed;
                    result.Message = "同一页面下书签名称不能重复！";
                    return result;
                }


                    var oldBson = FindById(entity.Int("markId"));
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

        public bool checkName(int pageId, string name, int markId)
        {
            var queryStr = string.Format("pageId={0}&name={1}&markId={2}", pageId, name, markId);
            var count = this._ctx.FindAllByQueryStr("PageMark", queryStr).Count();
            return count == 0;
        }

        /// <summary>
        /// 新增
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public InvokeResult Insert(BsonDocument entity)
        {
            if (entity == null) throw new ArgumentNullException();
            InvokeResult result = new InvokeResult();
            try
            {
                if (!checkName(entity.Int("pageId"), entity.Text("name"), entity.Int("markId")))
                {
                    result.Status = Status.Failed;
                    result.BsonInfo = entity;
                    result.Message = "同一页面下书签名称不能重复！";
                    return result;
                }
                result = this._ctx.Insert(entity);
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
                   result = this._ctx.Delete("PageMark", string.Format("markId={0}", id));
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
