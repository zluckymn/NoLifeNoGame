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
    /// 任务书引用成果处理类
    /// </summary>
    public class PageRefResultBll
    {
        #region 构造函数
        /// <summary>
        /// 类私有变量
        /// </summary>
        private DataOperation _ctx = null;

        /// <summary>
        /// 封闭当前默认构造函数
        /// </summary>
        private PageRefResultBll()
        {
            _ctx = new DataOperation();
        }

        private PageRefResultBll(DataOperation ctx)
        {
            _ctx = ctx;
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static PageRefResultBll _()
        {
            return new PageRefResultBll();
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static PageRefResultBll _(DataOperation ctx)
        {
            return new PageRefResultBll(ctx);
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
            var hitObj = this._ctx.FindOneByKeyVal("PageRefResult", "pagerefRetId", id.ToString());
            return hitObj;

        }
        public IQueryable<BsonDocument> FindByPageId(int pageId)
        {
            
            var hitObj = this._ctx.FindAllByKeyVal("PageRefResult", "pageId", pageId.ToString()).AsQueryable();
            return hitObj .OrderByDescending(c => c.Int("pagerefRetId"));
    
        }

        /// <summary>
        /// 成果还需要列出关联的项目总结
        /// </summary>
        /// <param name="projRetId"></param>
        /// <returns></returns>
        public IQueryable<BsonDocument> GetBookTaskByProjRetId(int projRetId)
        {
      
           var query = from c in this._ctx.FindAll("BookTaskPage")
                        join d in this._ctx.FindAll("PageRefResult") on c.Int("pageId") equals d.Int("pageId")
                        where d.Int("projRetId") == projRetId
                        select c.SourceBson("bookId");
            return query.AsQueryable().Distinct().OrderByDescending(c => c.Date("updateDate"));
        }

        /// <summary>
        /// 获取该任务书下的所有成果关联
        /// </summary>
        /// <param name="bookId"></param>
        /// <param name="pageId"></param>
        /// <returns></returns>
        public IQueryable<BsonDocument> GetRefResultList(int bookId, int pageId)
        {
            if (pageId == 0)
            {
                 var query = from c in this._ctx.FindAll("BookTaskPage")
                            join d in this._ctx.FindAll("PageRefResult") on c.Int("pageId") equals d.Int("pageId")
                            where c.Int("bookId") == bookId
                            select d;
                return query.AsQueryable().OrderByDescending(c => c.Int("pagerefRetId"));
            }
            else
            {

                return this._ctx.FindAllByKeyVal("PageRefResult", "pageId", pageId.ToString()).OrderByDescending(c => c.Int("pagerefRetId")).AsQueryable();
           }
        
        }
        /// <summary>
        /// 获取涉及的路径列表
        /// </summary>
        /// <param name="pageIdList"></param>
        /// <returns></returns>
        public Dictionary<int, string> GetRefResultPathList(List<int> pageIdList)
        {
            Dictionary<int, string> pathList = new Dictionary<int, string>();
            foreach (var pageId in pageIdList)
            {
                if (!pathList.ContainsKey(pageId))
                pathList.Add(pageId, GetRefResultPath(pageId));
            }
            return pathList;
        }

        
        /// <summary>
        /// 返回涉及到的所有页面路径
        /// </summary>
        /// <param name="bookId"></param>
        /// <param name="pageId"></param>
        /// <returns></returns>
        public string  GetRefResultPath(int pageId)
        {
           if (pageId != 0)
            {
             
                var curPage=this._ctx.FindOneByKeyVal("BookPage","pageId",pageId.ToString());
                var nodePid = curPage.Int("nodePid");
                var curPath = string.Format("{0}", curPage.Text("name"));
                while (nodePid != 0)
                {
                     var parentPage = this._ctx.FindOneByKeyVal("BookPage", "pageId", nodePid.ToString());
                    if (parentPage == null) break;
                    else
                    {
                        curPath = string.Format("{0}>{1}", parentPage.Text("name"), curPath);
                        nodePid = parentPage.Int("nodePid");
                    }
                }
                return curPath;
            }
            else
            {
                return string.Empty;
            }
         }

        public BsonDocument FindByPageIdAndRetId(int pageId, int retId)
        {
            var queryStr = string.Format("pageId={0}&projRetId={1}", pageId, retId);
            return this._ctx.FindOneByQueryStr("PageRefResult", queryStr);
        }
        /// <summary>
        /// 查找所有
        /// </summary>
        /// <returns></returns>
        public IQueryable<BsonDocument> FindAll()
        {
             return this._ctx.FindAll("PageRefResult").AsQueryable(); ;
        }


        public IQueryable<BsonDocument> FindByIds(List<int> ids)
        {
            //return this._ctx.PageRefResults.Where(c=>ids.Contains(c.pagerefRetId));
            
            var hitObj = this._ctx.FindAll("PageRefResult");
            return hitObj.AsQueryable().Where(c => ids.Contains(c.Int("pagerefRetId")));
             
            
        }


        #endregion

        #region 操作
        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public InvokeResult Update(BsonDocument entity,BsonDocument updateBson)
        {
            InvokeResult result = new InvokeResult();
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
            if (entity == null) throw new ArgumentNullException();
            InvokeResult  result = new InvokeResult ();
            try
            {
                result = this._ctx.Insert("PageRefResult", entity);
              
            }
            catch(Exception ex)
            {
                result.Status = Status.Failed;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 批量添加
        /// </summary>
        /// <param name="pageId"></param>
        /// <param name="retIds"></param>
        /// <returns></returns>
        public InvokeResult Insert(int pageId,int[] retIds)
        {
           
            InvokeResult result = new InvokeResult();
            try
            {
                foreach (var retId in retIds)
                {
                    var PageRefResultObj = FindByPageIdAndRetId(pageId, retId);
                    if (PageRefResultObj == null)
                    {
                        var RetObj = this._ctx.FindOneByKeyVal("StandardResult_StandardResult", "retId", retId.ToString());
                        if (RetObj != null)
                        {
                            BsonDocument insertBos = new BsonDocument();
                            insertBos.Add("pageId", pageId);
                            insertBos.Add("projRetId", retId);
                            insertBos.Add("coverFile", RetObj.Text("prevFileHash"));
                            result=Insert(PageRefResultObj);
                            if (result.Status != Status.Successful)
                            {
                                return result;
                            }
                        }
                    
                    }
                
                }
               
                return new InvokeResult{ Status = Status.Successful };
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
                  result=this._ctx.Delete("PageRefResult", string.Format("pagerefRetId={0}", id));
                
            }
            catch (Exception ex)
            {
                result.Status = Status.Failed;
                result.Message = ex.Message;
            }

            return result;
        }



        public InvokeResult Delete(List<int> ids)
        {
            InvokeResult result = new InvokeResult() { Status = Status.Successful };

            try
            {
                var entityList =  FindByIds(ids);

               using (TransactionScope tran = new TransactionScope())
                {

                    result=this._ctx.QuickDelete("PageRefResult", entityList.ToList());
              
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
