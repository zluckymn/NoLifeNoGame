using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using Yinhe.ProcessingCenter.DataRule;
using MongoDB.Bson;
using Yinhe.ProcessingCenter;
using MongoDB.Driver.Builders;
namespace Yinhoo.Autolink.Business.MissionStatement
{
    /// <summary>
    /// 系统任务书结构处理类
    /// </summary>
    public class BookTaskBll  
    {
        #region 构造函数
        /// <summary>
        /// 类私有变量
        /// </summary>
        private DataOperation _ctx = null;

        /// <summary>
        /// 封闭当前默认构造函数
        /// </summary>
        public BookTaskBll()
        {
            _ctx = new DataOperation();
        }

        public BookTaskBll(DataOperation ctx)
        {
            _ctx = ctx;
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static BookTaskBll _()
        {
            return new BookTaskBll();
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static BookTaskBll _(DataOperation ctx)
        {
            return new BookTaskBll(ctx);
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
            var  hitObj=this._ctx.FindOneByKeyVal("BookTask","bookId",id.ToString());
            return hitObj;
        }

        /// <summary>
        /// 查找所有
        /// </summary>
        /// <returns></returns>
        public IQueryable<BsonDocument> FindAll()
        { 
            return this._ctx.FindAll("BookTask").AsQueryable();
        }

        /// <summary>
        /// 查询记录是否存在
        /// </summary>
        /// <returns></returns>
        public bool IsExists(string name)
        {
            var hitCollection = this._ctx.FindAllByKeyVal("BookTask", "name", name);
            return hitCollection.Count() > 0;
       }

        public IQueryable<BsonDocument> FindAllTemplates()
        {
            var hitCollection = this._ctx.FindAllByKeyVal("BookTask", "type", "1");
            return hitCollection.AsQueryable();
        }

        public IQueryable<BsonDocument> FindByType(int type)
        {
            
            //var hitCollection = this._ctx.FindAllByKeyVal("BookTask", "type", type.ToString());
            var hitCollection = this._ctx.FindAllByQuery("BookTask", Query.Or( Query.EQ("type", type),Query.EQ("type",type.ToString())));
            return hitCollection.AsQueryable();
        }
        /// <summary>
        /// 查询分项的任务书
        /// </summary>
        /// <param name="type"></param>
        /// <param name="projId"></param>
        /// <returns></returns>
        public IQueryable<BsonDocument> FindByTypeProjId(int type, int projId)
        {
            var bookTaskQuery = FindByType(type);
            var projectBookTask = this._ctx.FindAll("ProjectBookTask");
            var query = from b in bookTaskQuery
                        join p in projectBookTask
                        on b.Int("bookId") equals p.Int("bookId")
                        where b.Int("type") == type && p.Int("projId") == projId
                        select b;
            return query;
        }

        /// <summary>
        /// 通过nodeType和projNodeId查找
        /// </summary>
        /// <param name="nodeType"></param>
        /// <param name="projNodeId"></param>
        /// <returns></returns>
        public IQueryable<BsonDocument> FindByNodeIdAndType(int nodeType, int projNodeId)
        {
            
            switch (nodeType)
            {
                case 0:
                default:
                    var engQuery = from b in FindAll()
                                   join p in this._ctx.FindAll("EpsSummary")
                                   on b.Int("bookId") equals p.Int("bookId")
                                   where p.Int("engId") == projNodeId && p.Int("epsId") == null && p.Int("projId") == null
                                   select b;
                    return engQuery;
                case 1:
                case 2:
                    var epsQuery = from b in FindAll()
                                   join p in this._ctx.FindAll("EpsSummary")
                                   on b.Int("bookId") equals p.Int("bookId")
                                   where p.Int("epsId") == projNodeId
                                   select b;
                    return epsQuery;
                case 3:
                    var projQuery = from b in FindAll()
                                    join p in this._ctx.FindAll("EpsSummary")
                                    on b.Int("bookId") equals p.Int("bookId")
                                    where p.Int("projId") == projNodeId
                                    select b;
                    return projQuery;
            }
        }

        /// <summary>
        /// 查找所有引用的任务书
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public List<BsonDocument> FindAllRef(int id)
        {
            return this._ctx.FindAllByKeyVal("BookTask", "refBookId", id.ToString()).ToList();
        } 

        /// <summary>
        /// 查找
        /// </summary>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        /// <param name="keyWord"></param>
        /// <param name="categoryId"></param>
        /// <param name="type"></param>
        /// <param name="stageId"></param>
        /// <param name="profId"></param>
        /// <param name="patternId"></param>
        /// <param name="taskType"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public List<BsonDocument > SearchTemplates(int page, int pageSize, string keyWord,int categoryId,int type,int stageId,int profId,int patternId,int taskType,out int count)
        {
            int skipNum = (page - 1) * pageSize;
            var query = this._ctx.FindAllByQueryStr("BookTask", string.Format("type={0}&categoryId={1}", type, categoryId)).AsQueryable();
            if (keyWord != "")
            {
                query = query.Where(c => c.Text("name").Contains(keyWord) || c.Text("remark").Contains(keyWord));
            }
            if (stageId != 0)
            {
                query = query.Where(m => m.Int("sysStageId") == stageId);
            }
            if (profId != 0)
            {
                query = query.Where(m => m.Int("sysProfId") == profId);
            }
            if (patternId != 0)
            {
                query = query.Where(m => m.ChildBsonList("BookTaskPattern").Where(n => n.Int("patternId") == patternId).Count() > 0);
            }
            if (taskType != 0)
            {
                query = query.Where(t => t.Int("taskType") == taskType);
            }
            count = query.Count();
            return query.OrderByDescending(m => m.Date("updateDate")).Skip(skipNum).Take(pageSize).ToList();
        }

        /// <summary>
        /// 查找任务书
        /// </summary>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        /// <param name="keyWord"></param>
        /// <param name="type"></param>
        /// <param name="stageId"></param>
        /// <param name="profId"></param>
        /// <param name="patternId"></param>
        /// <param name="projId"></param>
        /// <param name="taskType"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public List<BsonDocument> Search(int page, int pageSize, string keyWord, int type, int stageId, int profId, int patternId, int projId,int taskType, out int count)
        {
            int skipNum = (page - 1) * pageSize;
            var query = from t in FindAll()
                        join p in this._ctx.FindAll("ProjectBookTask")
                        on t.Int("bookId") equals p.Int("bookId")
                        where p.Int("projId") == projId && t.Int("type")==type
                        select t;
            if (keyWord != "")
            {
               query = query.Where(c => c.Text("name").Contains(keyWord) || c.Text("remark").Contains(keyWord));
            }
            if (stageId != 0)
            {
                query = query.Where(m => m.Int("sysStageId") == stageId);
            }
            if (profId != 0)
            {
                query = query.Where(m => m.Int("sysProfId") == profId);
            }
            if (patternId != 0)
            {
                //query = query.Where(m => m.BookTaskPatterns.Where(n => n.patternId == patternId).Count() > 0);
                query = query.Where(m => m.ChildBsonList("BookTaskPattern").Where(n => n.Int("patternId") == patternId).Count() > 0);
            }
            if (taskType != 0)
            {
                //query = query.Where(t => t.taskType == taskType);
                query = query.Where(t => t.Int("taskType") == taskType);
            }
            count = query.Count();
            //return query.OrderByDescending(m => m.updateDate).Skip(skipNum).Take(pageSize).ToList();
            return query.OrderByDescending(m => m.Date("updateDate")).Skip(skipNum).Take(pageSize).ToList();
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
                result=this._ctx.Update(entity, updateBson);
                //BuildIndex(entity);
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
                result = this._ctx.Insert("BookTask", entity);
                 // BuildIndex(entity);
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
        /// 检查重名（区分任务书和模板）
        /// </summary>
        /// <param name="bookId"></param>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool CheckTemplateName(int bookId, int type, string name, int categoryId)
        {
            var queryStr = string.Format("type={0}&name={1}&categoryId={2}",  type, name, categoryId);
            var count = this._ctx.FindAllByQueryStr("BookTask", queryStr).Where(t=>t.Int("bookId") != bookId).Count();
            return count == 0;
        }

        /// <summary>
        /// 检查重名（区分任务书和模板）
        /// </summary>
        /// <param name="bookId"></param>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool CheckName(int bookId, int type,string name, int nodeType, int projNodeId)
        {
            long  count = 0;
            switch (nodeType)
            {
                case 0:
                default:
                    //var queryStr = string.Format("bookId={0}&type={1}&name={2}&categoryId={3}", bookId, type, name, categoryId);
                     //count = this._ctx.EpsSummaries.Where(m => m.bookId != bookId && m.BookTask.type == type && m.engId == projNodeId && m.BookTask.name == name).Count();
                    var queryStr1 = string.Format("bookId={0}&type={1}&endId={2}&name={3}", bookId, type, projNodeId, name);
                    count = this._ctx.FindAllByQueryStr("EpsSummarie", queryStr1).Count();
                     break;
                case 1:
                case 2:
                    //count = this._ctx.EpsSummaries.Where(m => m.bookId != bookId && m.BookTask.type == type && m.epsId == projNodeId && m.BookTask.name == name).Count();
                     var queryStr2 = string.Format("bookId={0}&type={1}&epsId={2}&name={3}", bookId, type, projNodeId, name);
                    count = this._ctx.FindAllByQueryStr("EpsSummarie", queryStr2).Count();
                    break;
                case 3:
                    //count = this._ctx.EpsSummaries.Where(m => m.bookId != bookId && m.BookTask.type == type && m.projId == projNodeId && m.BookTask.name == name).Count();
                    var queryStr3 = string.Format("bookId={0}&type={1}&projId={2}&name={3}", bookId, type, projNodeId, name);
                    count = this._ctx.FindAllByQueryStr("EpsSummarie", queryStr3).Count();
                    break;
            }
            return count == 0;
        }

        public bool CheckName(int bookId, int type, string name, int projId)
        {
            var query = from bookTask in FindAll()
                        join projBookTask in this._ctx.FindAll("ProjectBookTask")
                        on bookTask.Int("bookId") equals projBookTask.Int("bookId")
                        where bookTask.Int("bookId") != bookId && projBookTask.Int("projId") == projId && bookTask.Int("type") == type && bookTask.Text("name") == name
                        select bookTask;


            return query.Count() == 0;
        }

        /// <summary>
        /// 保存任务书模板
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="ids"></param>
        /// <returns></returns>
        public InvokeResult Save(BsonDocument entity,BsonDocument updateEntityBson, int[] ids)
        {
            if (entity == null) throw new ArgumentNullException();
            InvokeResult result = new InvokeResult();
            var needCreate = 0;
            var needCopy = 0;
            try
            {
                if (!CheckTemplateName(entity.Int("bookId"), entity.Int("type"), string.IsNullOrEmpty(updateEntityBson.Text("name")) ? entity.Text("name") : updateEntityBson.Text("name"), entity.Int("categoryId")))
                {
                    result.Status = Status.Failed;
                    if (entity.Int("type") == 0)
                    {
                        result.Message = "任务书名称不能重复！";
                    }
                    else
                    {
                        result.Message = "任务书模板名称不能重复！";
                    }
                    return result;
                }
                List<BsonDocument> newBookPageList = new List<BsonDocument>();
                using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, new TimeSpan(0, 10, 0)))
                {
                    var entityUpdateBson=new BsonDocument();
                    if (entity.Int("bookId") == 0)
                    {
                        if (entity.Int("refBookId") == -1 || string.IsNullOrEmpty(entity.Text("refBookId")))
                        {
                            needCreate = 1;
                            entity.Add("refBookId", "");
                        }
                        else
                        {
                            needCopy = 1;
                        }
                        //entity.Add("type", "1");
                        result = this._ctx.Insert("BookTask", entity);
                        if (result.Status != Status.Successful)
                        {
                            return result;
                        }
                        else
                        {

                            entity = result.BsonInfo;
                        }
                    }
                    else
                    {
                        var entityResult = this._ctx.Update(entity, updateEntityBson);
                        if (entityResult.Status != Status.Successful)
                        {
                            return entityResult;
                        }
                        entity = entityResult.BsonInfo; 
                        var rootPage = entity.ChildBsonList("BookPage").Where(m => m.Int("nodePid") == 0).FirstOrDefault();
                        if (rootPage != null)
                        {
                            var rootPageUpdateBson = new BsonDocument();
                            rootPageUpdateBson.Add("name", entity.Text("name"));
                            this._ctx.Update(rootPage, rootPageUpdateBson);
                        }
                    }
                    var bookId = entity.Int("bookId");
                    var delBsonList = this._ctx.FindAll("BookTaskPattern").Where(x => x.Int("bookId") == entity.Int("bookId")).ToList();//.FindAllByKeyVal("BookTaskPattern", "bookId", entity.Text("bookId")).ToList();
                    if (delBsonList.Count() > 0)
                    {
                        this._ctx.QuickDelete("BookTaskPattern", delBsonList);
                    }
                    if (ids.Count() > 0)
                    {
                        List<BsonDocument> bookTaskPatterns = new List<BsonDocument>();
                        foreach (var id in ids)
                        {
                            var newBsonDoc=new BsonDocument();
                            newBsonDoc.Add("bookId",bookId);
                            newBsonDoc.Add("patternId",id);
                            bookTaskPatterns.Add(newBsonDoc);
                        }
                        this._ctx.QuickInsert("BookTaskPattern", bookTaskPatterns);
                       
                    }
                    #region 默认创建一个页面
                    if (needCreate == 1)
                    {
                        var bookPage = new BsonDocument();
                        bookPage.Add("name", entity.Text("name"));
                        bookPage.Add("IsPageTempate", entity.Text("name"));
                        bookPage.Add("lastVersion", 0);
                        bookPage.Add("categoryId", entity.Text("categoryId"));
                        List<BsonDocument> bookPageList = new List<BsonDocument>();
                        bookPageList.Add(bookPage);
                        List<int> temp = new List<int>();
                        BookPageBll._(this._ctx).Create(bookPageList, 0, entity.Int("bookId"), temp);


                    }
                    if (needCopy == 1)
                    {
                        var updateBookTaskBson = new BsonDocument();
                        var bookTask = this._ctx.FindOneByKeyVal("BookTask", "bookId", entity.Text("refBookId"));
                        updateBookTaskBson.Add("refTime", bookTask.Int("refTime") + 1);
                        this._ctx.Update(bookTask, updateBookTaskBson);
                        var oldBookPageList = bookTask.ChildBsonList("BookPage").ToList();
                        CopyNode(newBookPageList, oldBookPageList, 0, null, entity);
                    }
                    #endregion
                    scope.Complete();
                }
                if (entity.Int("type") == 0)
                {
                    try
                    {
                   //BuildIndex(entity);
                    }
                    catch (Exception ex)
                    { }
                }

                result.Status = Status.Successful;
                result.Message = "保存成功!";
            }
            catch (Exception ex)
            {
                result.Status = Status.Failed;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 保存任务书
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="ids"></param>
        /// <returns></returns>
        public InvokeResult Save(BsonDocument entity, BsonDocument updateEntityBson, int[] ids, int projId,int taskId)
        {
            if (entity == null) throw new ArgumentNullException();
            InvokeResult result = new InvokeResult();
            var needCreate = 0;
            var needCopy = 0;
            try
            {
                if (!CheckName(entity.Int("bookId"), entity.Int("type"), entity.String("name"), projId))
                {
                    result.Status = Status.Failed;
                    result.Message = "名称不能重复";
                    return result;
                }
                List<BsonDocument> newBookPageList = new List<BsonDocument>();
                using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, new TimeSpan(0, 10, 0)))
                {
                    if (entity.Int("bookId") == 0)
                    {
                        if ( entity.Int("refBookId") == -1 || entity.Int("refBookId")==0)
                        {
                            needCreate = 1;
                            this._ctx.Update(entity, string.Format("refBookId={0}", ""));
                        }
                        else
                        {
                            needCopy = 1;
                        }
                        entity.Add("type", 0);
                        result=this._ctx.Insert("BookTask", entity);
                        if (result.Status != Status.Successful)
                        {
                            return result;
                        }
                        else {

                            entity = result.BsonInfo;
                        }
                    
                        if (projId != 0)
                        {
                            BsonDocument projBookTask = new BsonDocument();
                            projBookTask.Add("bookId",entity.Int("bookId"));
                            projBookTask.Add("projId",projId);
                            projBookTask.Add("taskId", taskId);
                            this._ctx.Insert("ProjectBookTask", projBookTask); 
                        }
                    }
                    else
                    {
                        var entityResult=this._ctx.Update(entity, updateEntityBson);
                        if (entityResult.Status != Status.Successful)
                        {
                            return entityResult;
                        }
                        entity = entityResult.BsonInfo; 

                        var rootPage = entity.ChildBsonList("BookPage").Where(m => m.Int("nodePid") == 0).FirstOrDefault();
                        if (rootPage != null)
                        {
                            var rootPageUpdateBson = new BsonDocument();
                            rootPageUpdateBson.Add("name", entity.Text("name"));
                            this._ctx.Update(rootPage, rootPageUpdateBson);
                        }
                    }
                    var bookId = entity.Int("bookId");
                    //var delBsonList = this._ctx.FindAllByKeyVal("BookTaskPattern", "bookId", entity.Text("bookId")).ToList();
                    var delBsonList = this._ctx.FindAllByQuery("BookTaskPattern", Query.Or( Query.EQ("bookId",entity.Int("bookId")),Query.EQ("bookId",entity.Text("bookId")))).ToList();
                    if (delBsonList.Count() > 0)
                    {
                        this._ctx.QuickDelete("BookTaskPattern", delBsonList);
                    }
              
                   
                    if (ids.Count() > 0)
                    {
                        List<BsonDocument> bookTaskPatterns = new List<BsonDocument>();
                        foreach (var id in ids)
                        {
                            var newBsonDoc = new BsonDocument();
                            newBsonDoc.Add("bookId", bookId);
                            newBsonDoc.Add("patternId", id);
                            bookTaskPatterns.Add(newBsonDoc);
                        }
                        this._ctx.QuickInsert("BookTaskPattern", bookTaskPatterns);
                    }
                    #region 默认创建一个页面
                    if (needCreate == 1)
                    {
                        var bookPage = new BsonDocument();
                        bookPage.Add("name", entity.Text("name"));
                        bookPage.Add("IsPageTempate", entity.Text("name"));
                        bookPage.Add("lastVersion", 0);
                        bookPage.Add("categoryId", entity.Text("categoryId"));
                        List<BsonDocument> bookPageList = new List<BsonDocument>();
                        bookPageList.Add(bookPage);
                        List<int> temp = new List<int>();
                        BookPageBll._(this._ctx).Create(bookPageList, 0, entity.Int("bookId"), temp);
                    }
                    if (needCopy == 1)
                    {
                        var updateBookTaskBson = new BsonDocument();
                        var bookTask = this._ctx.FindOneByKeyVal("BookTask", "bookId", entity.Text("refBookId"));
                        updateBookTaskBson.Add("refTime", bookTask.Int("refTime") + 1);
                        this._ctx.Update(bookTask, updateBookTaskBson);
                        var oldBookPageList = bookTask.ChildBsonList("BookPage").ToList();
                        CopyNode(newBookPageList, oldBookPageList, 0, null, entity);
                    }
                    #endregion
                    scope.Complete();
                }
                //#region 添加索引
                //if (entity.type == 0)
                //{
                //    try
                //    {
                //        BuildIndex(entity);
                //    }
                //    catch (Exception ex)
                //    { }
                //}
                //#endregion

                ////建设索引
                //foreach (var bookPage in newBookPageList)
                //    BookPageBll._(this._ctx).BuildIndex(bookPage);
                result.Status = Status.Successful;
                result.Message = "保存成功!";
            }
            catch (Exception ex)
            {
                result.Status = Status.Failed;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 保存任务书
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="ids"></param>
        /// <returns></returns>
        public InvokeResult Save(BsonDocument entity, int[] ids, int nodeType, int projNodeId, int engId)
        {
            if (entity == null) throw new ArgumentNullException();
            InvokeResult result = new InvokeResult();
            var needCreate = 0;
            var needCopy = 0;
            try
            {
                if (!CheckName(entity.Int("bookId"), entity.Int("type"), entity.Text("name"), nodeType, projNodeId))
                {
                    result.Status = Status.Failed;
                    result.Message = "名称不能重复";
                    return result;
                }
                List<BsonDocument> newBookPageList = new List<BsonDocument>();
                using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, new TimeSpan(0, 10, 0)))
                {
                    if (entity.Int("bookId") == 0)
                    {
                        if (entity.Int("refBookId") == -1 || entity.Int("refBookId") == 0)
                        {
                            needCreate = 1;
                            this._ctx.Update(entity, string.Format("refBookId={0}", ""));
                        }
                        else
                        {
                            needCopy = 1;
                        }
                        result = this._ctx.Insert("BookTask", entity);
                        if (result.Status != Status.Successful)
                        {
                            return result;
                        }
                    
                        if (projNodeId != 0)
                        {
                            BsonDocument epsSummary = new BsonDocument ();
                            epsSummary.Add("bookId",entity.Int("bookId"));
                            epsSummary.Add("nodeType",nodeType);
                         
                            switch (nodeType)
                            {
                                case 0:
                                default:
                                   epsSummary.Add("engId", projNodeId);
                                    break;
                                case 1:
                                case 2:
                                    epsSummary.Add("engId", engId);
                                    epsSummary.Add("epsId", projNodeId);
                                    break;
                                case 3:
                                     epsSummary.Add("engId", engId);
                                     epsSummary.Add("projId", projNodeId);
                                    break;
                            }
                            result = this._ctx.Insert("EpsSummary", epsSummary);
                            if (result.Status != Status.Successful)
                            {
                                return result;
                            }
                        }
                    }
                    else
                    {
                        var rootPage = entity.ChildBsonList("BookPage").Where(m => m.Int("nodePid") == 0).FirstOrDefault();
                        if (rootPage != null)
                        {
                            var rootPageUpdateBson = new BsonDocument();
                            rootPageUpdateBson.Add("name", entity.Text("name"));
                            this._ctx.Update(rootPage, rootPageUpdateBson);
                        }
                    }
                    var bookId = entity.Int("bookId");
                    var delBsonList = this._ctx.FindAllByKeyVal("BookTaskPattern", "bookId", entity.Text("bookId")).ToList();
                    if (delBsonList.Count() > 0)
                    {
                        this._ctx.QuickDelete("BookTaskPattern", delBsonList);
                    }
              
                    if (ids.Count() > 0)
                    {
                        List<BsonDocument> bookTaskPatterns = new List<BsonDocument>();
                        foreach (var id in ids)
                        {
                            var newBsonDoc = new BsonDocument();
                            newBsonDoc.Add("bookId", bookId);
                            newBsonDoc.Add("patternId", id);
                            bookTaskPatterns.Add(newBsonDoc);
                        }
                        this._ctx.QuickInsert("BookTaskPattern", bookTaskPatterns);
                    }
                    #region 默认创建一个页面
                    if (needCreate == 1)
                    {
                        var bookPage = new BsonDocument();
                        bookPage.Add("name", entity.Text("name"));
                        bookPage.Add("IsPageTempate", entity.Text("name"));
                        bookPage.Add("lastVersion", 0);
                        bookPage.Add("categoryId", entity.Text("categoryId"));
                        List<BsonDocument> bookPageList = new List<BsonDocument>();
                        bookPageList.Add(bookPage);
                        List<int> temp = new List<int>();
                        BookPageBll._(this._ctx).Create(bookPageList, 0, entity.Int("bookId"), temp);
                    }
                    if (needCopy == 1)
                    {
                        var updateBookTaskBson = new BsonDocument();
                        var bookTask = this._ctx.FindOneByKeyVal("BookTask", "bookId", entity.Text("refBookId"));
                        updateBookTaskBson.Add("refTime", bookTask.Int("refTime") + 1);
                        this._ctx.Update(bookTask, updateBookTaskBson);
                        var oldBookPageList = bookTask.ChildBsonList("BookPage").ToList();
                        CopyNode(newBookPageList, oldBookPageList, 0, null, entity);
                    }
                    #endregion
                    scope.Complete();
                    //#region 添加索引
                    //if (entity.type == 0)
                    //{
                    //    try
                    //    {
                    //        BuildIndex(entity);
                    //    }
                    //    catch (Exception ex)
                    //    { }
                    //}
                    //#endregion
                }

                //建设索引
                //foreach (var bookPage in newBookPageList)
                //    BookPageBll._(this._ctx).BuildIndex(bookPage);
                result.Status = Status.Successful;
                result.Message = "保存成功!";
            }
            catch (Exception ex)
            {
                result.Status = Status.Failed;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 复制任务书模板
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="pageList"></param>
        /// <param name="nodePid"></param>
        /// <param name="parent"></param>
        /// <param name="bookTask"></param>
        public void CopyNode(List<BsonDocument> newPageList, List<BsonDocument> pageList, int nodePid, BsonDocument parent, BsonDocument bookTask)
        {
            var bookPages = pageList.Where(m => m.Int("nodePid") == nodePid).ToList();
            if (bookPages.Count() == 0)
                return;
            foreach (var bookPage in bookPages)
            {
                #region 复制树结构
                var newBookPage = new BsonDocument();
                newBookPage.Add("sysProfId", bookPage.Text("sysProfId"));
                newBookPage.Add("sysStageId", bookPage.Text("sysStageId"));
                newBookPage.Add("name", parent == null ? bookTask.Text("name") : bookPage.Text("name"));
                newBookPage.Add("IsPageTempate", 0);
                newBookPage.Add("lastVersion", 1);
                newBookPage.Add("categoryId", bookPage.Text("categoryId"));
                newBookPage.Add("nodePid", parent!=null?parent.Text("pageId"):"0");
                newBookPage.Add("bookId", bookTask.Text("bookId"));
                var result= _ctx.Insert("BookPage", newBookPage);
                if (result.Status != Status.Successful)
                {
                    return  ;
                }
                newBookPage = result.BsonInfo;
                
                #endregion

                #region 复制页面内容
                var pageBody = this._ctx.FindOneByKeyVal("PageBody", "pageId", bookPage.Text("pageId"));
                if (pageBody != null)
                {
                     var newPageBody = new BsonDocument();
                     newPageBody.Add("pageId",newBookPage.Text("pageId"));
                     newPageBody.Add("body", pageBody.Text("body"));
                     newPageBody.Add("lastVersion",1);
                     var bodyResult = _ctx.Insert("PageBody", newPageBody);
                     if (bodyResult.Status != Status.Successful)
                     {
                        return  ;
                     }
                    newPageBody = bodyResult.BsonInfo;

                    var newPageBodyVersion = new BsonDocument();
                    newPageBodyVersion.Add("pageId", newBookPage.Text("pageId"));
                    newPageBodyVersion.Add("bodyId", newPageBody.Text("bodyId"));
                    newPageBodyVersion.Add("userId", bookTask.Text("updateUserId"));
                    newPageBodyVersion.Add("body", newBookPage.Text("body"));
                    newPageBodyVersion.Add("bodyVersion", 1);
                    var bodyVersionResult = _ctx.Insert("PageBodyVersion", newPageBodyVersion);
                    if (bodyVersionResult.Status != Status.Successful)
                    {
                        return;
                    }
                    newPageBodyVersion = bodyVersionResult.BsonInfo;
                  
                }
                #endregion

                #region 复制书签
                //var pageMarkList = bookPage.PageMarks.ToList();
                //var newPageMarkList = new List<PageMark>();
                //foreach (var pageMark in pageMarkList)
                //{
                //    var newPageMark = new PageMark()
                //    {
                //        pageId = newBookPage.pageId,
                //        name = pageMark.name,
                //        markIdentity = pageMark.name,
                //        createDate = DateTime.Now,
                //        update = DateTime.Now,
                //        createUserId = bookTask.updateUserId.Value,
                //        updateUserId = bookTask.updateUserId.Value,
                //    };
                //    newPageMarkList.Add(newPageMark);
                //}
                //this._ctx.PageMarks.InsertAllOnSubmit(newPageMarkList);
                //this._ctx.SubmitChanges();
                #endregion

                #region 复制标签
                //ObjectEntityTagBll bllTag = ObjectEntityTagBll._(this._ctx);
                //var tagList = bllTag.FindAll(3,bookPage.pageId);
                //var newTagList = new List<ObjectEntityTag>();
                //foreach (var tag in tagList)
                //{
                //    var newTag = new ObjectEntityTag()
                //    {
                //        sysObjId=3,
                //        name = tag.name,
                //        entityId = newBookPage.pageId,
                //        createUserId = bookTask.updateUserId.Value,
                //        createDate = DateTime.Now
                //    };
                //    newTagList.Add(newTag);
                //}
                //this._ctx.ObjectEntityTags.InsertAllOnSubmit(newTagList);
                //this._ctx.SubmitChanges();
                #endregion

                #region 复制页面业态
                var bookPagePatterns = bookPage.ChildBsonList("BookPagePattern").ToList();
                var newBookPagePatterns = new List<BsonDocument>();
                foreach (var pattern in bookPagePatterns)
                {
                    var newBookPagePattern = new BsonDocument();
                    newBookPagePattern.Add("pageId",newBookPage.Text("pageId"));
                    newBookPagePattern.Add("patternId",newBookPage.Text("patternId"));
                    newBookPagePatterns.Add(newBookPagePattern);
                }
                   var patternResult= this._ctx.QuickInsert("BookPagePattern",newBookPagePatterns);
                   if (patternResult.Status != Status.Successful)
                   {
                       return;
                   }
               
                #endregion

                #region 文档复制
               
                #region 新版文档复制,复制关联信息
                  Yinhe.ProcessingCenter.Document.FileOperationHelper opHelper = new Yinhe.ProcessingCenter.Document.FileOperationHelper();  
                  var files = _ctx.FindAllByQueryStr("FileRelation", "tableName=BookPage&keyValue=" + bookPage.Text("pageId").ToString()).ToList();
                   //复制文档
                  var fileresult = opHelper.CopyFileRelation(files, "BookPage", "pageId", newBookPage.Text("pageId"));
                  if (fileresult.Status != Status.Successful)
                   {
                       return  ;
                   }

             
                #endregion
                #endregion

                newPageList.Add(newBookPage);
                CopyNode(newPageList,pageList, bookPage.Int("pageId"), newBookPage,bookTask);
            }
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public InvokeResult Delete(int id)
        {
            InvokeResult result = new InvokeResult();
            result.Status = Status.Failed;
            BookPageBll bookPageBll = BookPageBll._(_ctx);
            try
            {
                using (TransactionScope scope = new TransactionScope())
                {
                    #region 关联删除
                    var entity = this.FindById(id);
                    if (entity != null)
                    {
                        var bookPages = entity.ChildBsonList("BookPage").ToList();
                        foreach (var bookPage in bookPages)
                        {
                            if (bookPageBll.Delete(bookPage).Status != Status.Successful)
                            {
                                return result;
                            }
                           
                        }
                      
                        if (entity.Int("type") == 0&&!string.IsNullOrEmpty(entity.Text("refBookId")) )
                        {
                            var refBookTask = this.FindById(entity.Int("refBookId"));
                            if (refBookTask != null && refBookTask.Int("refTime") > 0)
                            {
                                var refBookTaskUpdateBosn = new BsonDocument();
                                refBookTaskUpdateBosn.Add("refTime", refBookTask.Int("refTime") - 1);
                                result = this._ctx.Update(refBookTask, refBookTaskUpdateBosn);
                                if(result.Status!=Status.Successful)
                                {
                                    return result;
                                }

                            }
                        }
                        //删除关联业态
                        var delBsonList = this._ctx.FindAllByKeyVal("BookTaskPattern", "bookId", entity.Text("bookId")).ToList();
                        if (delBsonList.Count() == 0)
                        {
                            delBsonList = this._ctx.FindAllByQuery("BookTaskPattern", MongoDB.Driver.Builders.Query.EQ("bookId", entity.Text("bookId"))).ToList();
                        }
                        if (delBsonList.Count() > 0)
                        {
                            result = this._ctx.QuickDelete("BookTaskPattern", delBsonList);
                            if (result.Status != Status.Successful)
                            {
                                return result;
                            }
                        }

                        result = this._ctx.Delete("BookTask", "db.BookTask.distinct('_id',{'bookId':'" + id + "'})");
                    }
                    #endregion
                    scope.Complete();
                }
            }
            catch (Exception ex)
            {
                result.Status = Status.Failed;
                result.Message = ex.Message;
            }

            return result;
        }


        #region 索引相关
        ///// <summary>
        ///// 其他线程创建索引
        ///// </summary>
        ///// <param name="_bookPage"></param>
        ///// <returns></returns>
        //public InvokeResult BuildIndex(BookTask bookTask)
        //{
        //    Thread NewThread = new Thread(delegate()
        //    {
        //        try
        //        {
        //            #region
        //              var exenstionInfo = string.Empty;
                    
        //                if (bookTask != null)
        //                {
        //                    var professionName = bookTask.SysProfession != null ? bookTask.SysProfession.name : "";
        //                    var projPharseName = bookTask.ProjectPhase != null ? bookTask.ProjectPhase.name : "";
        //                    var patternName = new StringBuilder();
        //                    var BookTaskPatternList = bookTask.BookTaskPatterns;
        //                    if (BookTaskPatternList != null)
        //                    {
        //                        foreach (var pattern in BookTaskPatternList)
        //                        {
        //                            patternName.Append(string.Format(" {0} ", pattern.SysPattern != null ? pattern.SysPattern.name : ""));
        //                        }
        //                    }
        //                    exenstionInfo = string.Format("   {0},{1},{2}   ", professionName, projPharseName, patternName.ToString());
        //                }
        //            #endregion


        //            DocumentQueue DQ = new DocumentQueue();
        //            DQ.docPath = "";
        //            DQ.docSource = "bookTask";
        //            DQ.fileId = bookTask.Int("bookId");
        //            DQ.content = string.IsNullOrEmpty(exenstionInfo) ? "" : exenstionInfo.Trim();
        //            DQ.docActionType = 3;//非动态读取的特殊页面类型
        //            DQ.name = bookTask.name  +".TagContent";
        //            DQ.hash = "";
        //             DocumentQueueBll DQB = DocumentQueueBll._();
        //            var Result = DQB.Push(DQ);
        //        }
        //        catch (ThreadInterruptedException ex)
        //        {
        //            // ReportErrors(ex.Message, "PushQueue");
        //            return;
        //        }
        //        catch (TimeoutException ex)
        //        {
        //            // ReportErrors(ex.Message, "PushQueue");
        //            return;
        //        }
        //        catch (Exception ex)
        //        {
        //            //  Yinhoo.Framework.Log.LogWarpper._().PushApplicationException(ex);
        //            //ReportErrors(ex.Message, "PushQueue");
        //            return;
        //        }
        //        return;
        //    });
        //    NewThread.Start();
        //    return new InvokeResult() { Status = Status.Successful };
        //}


        ///// <summary>
        ///// 动态获取索引内容
        ///// </summary>
        ///// <param name="key"></param>
        ///// <param name="fileVersion"></param>
        ///// <returns></returns>
        //public string GetIndexContent(int key, int? fileVersion)
        //{
          
        //    return string.Empty;
        //}

        ///// <summary>
        /////  获取搜索信息
        ///// </summary>
        ///// <param name="key"></param>
        ///// <param name="fileVersion"></param>
        ///// <returns></returns>
        //public FullTextSrearchResult GetDetailObjectInfo(int key, int fileVersion)
        //{
        //    ProjectBll projBll = ProjectBll._();
        //    var FTResult = new FullTextSrearchResult();
        //    var book = this._ctx.BookTasks.Where(c => c.Int("bookId") == key).FirstOrDefault();
        //    FTResult.Version = "1";//文件版本
        //    FTResult.BelongToObject = "所属任务书";//所属对象名称
        //    FTResult.ext = ".tagcontent";
        //    if (book != null)
        //    {
        //        FTResult.BelongToFileTableName = "BookTask";
        //        FTResult.CreateUser = book.SysUser.name;//创建人
        //        FTResult.CreateDate = book.createDate.ToString("yyyy-MM-dd");//创建日期
        //        FTResult.BelongToObjectName = book.name;//所属对象
        //        FTResult.BelongToObjectId = book.Int("bookId").ToString();//所属对象Id
        //        FTResult.ShowTypeIndex = -1;//用于下载查看等操作所需要的数字（SysObject为0）
        //        FTResult.LinkObj = string.Format("/MissionStatement/Home/BookPage/{0}", book != null ? book.Int("bookId") : 0);//链接跳转地址
        //        var dir = "";
        //        var firstBookPage = book.BookTaskPages.FirstOrDefault();
        //        if (firstBookPage != null && firstBookPage.pageId.HasValue)
        //        {
        //            //FTResult.ImgPath = CommonBll.GetBookPageSnapshot(firstBookPage.pageId.Value, out dir); ;//缩略图路径
        //            FTResult.ImgPath = CommonBll.GetBookPageOrgionImg(firstBookPage.pageId.Value, out dir); ;//缩略图路径
        //        }
        //        try
        //        {
        //            var projId = book.ProjectBookTasks.FirstOrDefault().projId;
        //            FTResult.BelongToProject = projBll.GetProjectFullPath(projId);
        //            FTResult.BelongToProjLink = projBll.GetProjectLink(projId);
        //        }
        //        catch (Exception ex)
        //        {

        //        }
        //    }
        //    else
        //    {
        //        FTResult.HasDeleted = "true";
        //    }

        //    return FTResult;


        //}

        #endregion


        #endregion
    }
}
