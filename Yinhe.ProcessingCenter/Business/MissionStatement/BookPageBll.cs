using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using Yinhe.ProcessingCenter.DataRule;
using MongoDB.Bson;
using Yinhe.ProcessingCenter;
using Yinhe.ProcessingCenter.Document;
using Yinhe.ProcessingCenter.Common;
///<summary>
///任务书
///</summary>
namespace Yinhoo.Autolink.Business.MissionStatement
{
    /// <summary>
    /// 任务书页面处理类
    /// </summary>
    public class BookPageBll 
    {
        #region 构造函数
        /// <summary>
        /// 类私有变量
        /// </summary>
        private DataOperation _ctx = null;

        /// <summary>
        /// 封闭当前默认构造函数
        /// </summary>
        public BookPageBll()
        {
            _ctx = new DataOperation();
        }

        /// <summary>
        /// 封闭当前默认构造函数
        /// </summary>
        private BookPageBll(DataOperation ctx)
        {
            _ctx = ctx;
        }


        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static BookPageBll _()
        {
            return new BookPageBll();
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static BookPageBll _(DataOperation ctx)
        {
            return new BookPageBll(ctx);
        }
        #endregion

        #region 查找

        public MongoDB.Driver.MongoCursor<BsonDocument> FindAll()
        {
           return  _ctx.FindAll("BookPage");
        }

        /// <summary>
        /// 通过任务书ID或模板ID获取Page
        /// </summary>
        /// <returns></returns>
        public IQueryable<BsonDocument> FindAllByBookId(int bookId)
        {
            if (bookId != 0)
            {
                var bookTask = _ctx.FindOneByKeyVal("BookTask","bookId", bookId.ToString());
                if (bookTask != null)
                {
                    return bookTask.ChildBsonList("BookPage").OrderBy(c => c.Text("nodeKey")).AsQueryable();
                }
                else
                {
                   return _ctx.FindAll("BookPage").Where(n => n.Int("bookId") == bookId).OrderBy(o => o.Text("nodeKey")).AsQueryable();
                }
            }
            else
            {
                return  _ctx.FindAllByKeyVal("BookPage","IsPageTempate", "1").OrderBy(o => o.Text("nodeKey")).AsQueryable();
            }
        }

        /// <summary>
        /// 通过nodeKey获取节点下的所有子节点
        /// </summary>
        /// <param name="nodeKey"></param>
        /// <returns></returns>
        public List<BsonDocument> FindByRootDim(string nodeKey)
        {
            return FindAll().Where(m => m.Text("nodeKey").StartsWith(nodeKey) == true).OrderBy(o => o.Text("nodeKey")).ToList();
        }

        public IQueryable<BsonDocument> FindPageTemplates()
        {
            return   _ctx.FindAllByKeyVal("BookPage","IsPageTempate", "1").AsQueryable();
        }

        /// <summary>
        /// 通过主键获取Page
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public BsonDocument FindById(int id)
        {
            return _ctx.FindOneByKeyVal("BookPage","pageId",id.ToString());
        }

        /// <summary>
        /// 查找所有引用的任务书
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public List<BsonDocument> FindAllRef(int id)
        {
         return _ctx.FindAllByKeyVal("BookPage","refPageId",id.ToString()).ToList();
        }

        /// <summary>
        /// 通过nodeKey获取节点下的所有子节点
        /// </summary>
        /// <param name="nodeKey"></param>
        /// <returns></returns>
        public List<BsonDocument> FindByNodeKey(string nodeKey)
        {
            return FindAll().Where(m => m.Text("nodeKey").StartsWith(nodeKey) == true).ToList();
        }

        /// <summary>
        /// 获取所有子节点
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public List<BsonDocument> FindAllChildNode(int id)
        {
            BsonDocument parentNode = this.FindById(id);

            List<BsonDocument> childList = this.FindByNodeKey(parentNode.Text("nodeKey"));

            childList.Remove(parentNode);

            return childList;
        }

        /// <summary>
        /// 通过层级获取Page
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public List<BsonDocument> FindByNodeLevel(int level, int bookId)
        {
            var hitResult= FindAllByBookId(bookId);
            return hitResult.Where( m=>m.Int("nodeLevel")  == level).ToList();
        }

        /// <summary>
        /// 通过父节点id获取页面
        /// </summary>
        /// <param name="nodePid"></param>
        /// <returns></returns>
        public List<BsonDocument> FindByNodePid(int nodePid)
        {
          return this._ctx.FindAllByKeyVal("BookPage","nodePid",nodePid.ToString()).ToList();
        }

        /// <summary>
        /// 获取子页面的所有父页面
        /// </summary>
        /// <param name="childCat"></param>
        /// <returns></returns>
        public List<BsonDocument> FindParentNode(BsonDocument childPage)
        {
            return FindAll().Where(m => childPage.Text("nodeKey").IndexOf(m.Text("nodeKey")) == 0).ToList();
        }

        /// <summary>
        /// 获取层级以内的页面
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public IQueryable<BsonDocument> FindByLevelScope(int level, int bookId)
        {
            var hitResult= FindAllByBookId(bookId);
            return hitResult.Where( m=>m.Int("nodeLevel")  <= level);
        }

        /// <summary>
        /// 获取层级内的子页面
        /// </summary>
        /// <param name="level"></param>
        /// <param name="nodeKey"></param>
        /// <returns></returns>
        public IQueryable<BsonDocument> FindByLevelKey(int level, string nodeKey, int bookId)
        {
            var hitResult= FindAllByBookId(bookId);
            return hitResult.Where( m=>m.Int("nodeLevel")  <= level&&m.Text("nodeKey").StartsWith(nodeKey) == true);
        }

        /// <summary>
        /// 获取页面的所有父页面
        /// </summary>
        /// <param name="nodeKey"></param>
        /// <returns></returns>
        public List<BsonDocument> FindParentByNodekey(string nodeKey)
        {
            return FindAll().Where(pr => nodeKey.IndexOf(pr.Text("nodeKey")) == 0).OrderBy(pr => pr.Text("nodeKey")).ToList();
        }

        /// <summary>
        /// 根据id获取子页面
        /// </summary>
        /// <param name="pid"></param>
        /// <returns></returns>
        public List<BsonDocument> FindChind(int pid)
        {
            return this._ctx.FindAllByKeyVal("BookPage","nodePid",pid.ToString()).OrderBy(pr => pr.Text("nodeKey")).ToList();
        }
        /// <summary>
        /// 是否有草稿，并返回草稿
        /// </summary>
        /// <param name="pageBookId"></param>
        /// <param name="currUserId"></param>
        /// <returns></returns>
        public InvokeResult<BsonDocument> HasDraft(int pageBookId, int currUserId)
        {
            InvokeResult<BsonDocument> result = new InvokeResult<BsonDocument>();

            BsonDocument entity = this._ctx.FindOneByQueryStr("BookPage",string.Format("pageId={0}&saveUserId={1}",pageBookId,currUserId));
            if (entity != null)
            {
                result.Status = Status.Successful;
                result.Value = entity;
            }
            else
            {
                result.Status = Status.Failed;
            }


            return result;
        }
        /// <summary>
        /// 获取最新版本的页面内容
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        public string GetLastestPageBody(BsonDocument page)
        {
            PageBodyBll bodyBll = PageBodyBll._();
            if (page == null) return string.Empty;
            var Body = bodyBll.FindLatestVersionByPageId(page.Int("pageId"), page.Int("lastVersion"));
            if (Body != null)
            {
                return Body.Text("body").Trim();
            }
            return string.Empty;
        }

        /// <summary>
        /// 查找
        /// </summary>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        /// <param name="keyWord"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public List<BsonDocument> Search(int page, int pageSize, string keyWord, int stageId, int profId, int patternId,int categoryId,out int count, out int parentId)
        {
            int skipNum = (page - 1) * pageSize;
            var query= this._ctx.FindAllByQueryStr("BookPage",string.Format("IsPageTempate=1&categoryId={0}",categoryId)).AsQueryable();
        if (keyWord != "")
            {
                // query = query.Where(m => m.contractName.Contains(keyWord));
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
                query = query.Where(m => m.ChildBsonList("BookPagePattern").Where(n => n.Int("patternId") == patternId).Count() > 0);
            }
            
            count = query.Count();
             var parent =this._ctx.FindOneByQueryStr("BookPage",string.Format("IsPageTempate=1&nodePid=0"));
             if (parent != null)
            {
                parentId = parent.Int("pageId");
            }
            else
            {
                parentId = -1;
            }
            return query.OrderByDescending(m => m.Text("nodeKey")).Skip(skipNum).Take(pageSize).ToList();
        }

        /// <summary>
        /// 获取节点下的子节点集
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public IQueryable<BsonDocument> FindSubList(int id)
        {
            var entity = this.FindById(id);

            return FindAll().Where(p => p.Text("nodeKey").StartsWith(entity.Text("nodeKey")) && p.Int("nodeLevel") > entity.Int("nodeLevel")).AsQueryable();
        }

        /// <summary>
        /// 获取节点编号
        /// </summary>
        /// <param name="pageList"></param>
        /// <param name="currPage"></param>
        /// <returns></returns>
        public string CalPrefixNum(List<BsonDocument> pageList, BsonDocument currPage, int topLevel) 
        {
            int num = 0;
            foreach (var page in pageList)
            {
                num++;
                if (page.Int("pageId") == currPage.Int("nodePid"))
                {
                    if (currPage.Int("nodeLevel") == topLevel)
                    {
                        return num.ToString() + ".";
                    }
                    return CalPrefixNum(pageList, page, topLevel) + num.ToString() + ".";
                }
            }
            return num.ToString() + ".";
        }

        public IQueryable<BsonDocument> FindAllBookTaskPage()
        {
            return this._ctx.FindAll("BookTaskPage").AsQueryable();
        }

        #region 通过页面id查找对应的分项

        /// <summary>
        /// 通过PageId,获取对应的分项
        /// </summary>
        /// <param name="pageId"></param>
        /// <returns></returns>
        public int FindProjIdByPageId(int pageId)
        {
            var bookTaskPage=_ctx.FindAll("BookTaskPage");
            var projectBookTask=_ctx.FindAll("ProjectBookTask");
            var query = from bt in bookTaskPage
                        join pbt in projectBookTask
                        on bt.Int("bookId") equals pbt.Int("bookId")
                        where bt.Int("pageId") != 0 && bt.Int("pageId") == pageId
                        select pbt.Int("projId");
            if (query.Count() > 0)
            {
                return query.FirstOrDefault();
            }
            else
            {
                return 0;
            }
        }

        #endregion
        #endregion

        #region 表操作
        /// <summary>
        /// 批量添加同一父页面的子页面
        /// </summary>
        /// <param name="matCatList"></param>
        /// <param name="nodePid"></param>
        /// <returns></returns>
        public InvokeResult<List<BsonDocument>> Create(List<BsonDocument> BsonDocumentList, int nodePid, int bookId, List<int> patternIds)
        {
            BsonDocument BsonDocumentParent = null;
            if (nodePid != 0)
            {
                BsonDocumentParent = this.FindById(nodePid);
            }
            return this.Create(BsonDocumentList, BsonDocumentParent, bookId, patternIds);
        }

        /// <summary>
        /// 批量添加同一父页面的子页面
        /// </summary>
        /// <param name="matCatList"></param>
        /// <param name="matCatParent"></param>
        /// <returns></returns>
        public InvokeResult<List<BsonDocument>> Create(List<BsonDocument> BsonDocumentList, BsonDocument bookPageParent, int bookId, List<int> patternIds)
        {
            if (bookPageParent == null)
            {
                bookPageParent = new BsonDocument();
                 
             }

            string existNames = "";
            if (this.ExsistName(bookPageParent.Int("pageId"), BsonDocumentList, out existNames, bookId) == true)
            {
                return new InvokeResult<List<BsonDocument>>() { Status = Status.Failed, Message = "该页面已经包含 " + existNames + " 的子页面" };
            }
         
            var newBsonDocumentList=new List<BsonDocument>();
            try
            {

                using (TransactionScope scope = new TransactionScope())
                {
                 
                    if (bookId != 0)
                    {
                        foreach (var page in BsonDocumentList)
                        {
                            page.Add("bookId", bookId);
                            var pageResult= _ctx.Insert("BookPage",page);
                            if(pageResult.Status==Status.Successful&&pageResult.BsonInfo!=null)
                            {
                             newBsonDocumentList.Add(pageResult.BsonInfo);
                            }
                            else
                            {
                             return   new InvokeResult<List<BsonDocument>>() { Status = Status.Failed,Message=pageResult.Message };
                            }
                        }
                        
                    }

                    //建业态
                    if (patternIds.Count() > 0)
                    {
                        var newBookPagePatternList=new List<BsonDocument>();
                        foreach (var page in newBsonDocumentList)
                        {
                            foreach (var id in patternIds)
                            {
                                var newBookPagePattern=new BsonDocument();
                                newBookPagePattern.Add("pageId",page.Int("pageId"));
                                newBookPagePattern.Add("patternId", id );
                                newBookPagePatternList.Add(newBookPagePattern);
                            }
                        }
                        this._ctx.QuickInsert("BookPagePattern",newBookPagePatternList);
                    }
                    

                    scope.Complete();
                    return new InvokeResult<List<BsonDocument>>() { Status = Status.Successful, Value=newBsonDocumentList };
                }
                
               
            }
            catch (Exception ex)
            {
                return new InvokeResult<List<BsonDocument>>() { Status = Status.Successful, Message = ex.Message };
            }
        }

        /// <summary>
        /// 更新页面
        /// </summary>
        /// <param name="matCat"></param>
        /// <returns></returns>
        public InvokeResult Update(BsonDocument BsonDocument,BsonDocument updateBosn, string oldName, int bookId, List<int> patternIds)
        {
            try
            {
                using (TransactionScope scope = new TransactionScope())
                {
                    if (BsonDocument == null) throw new ArgumentNullException();
                    if (BsonDocument.Text("name") != oldName)
                    {
                        if (this.ExsistName(BsonDocument.Int("nodePid"), BsonDocument.Int("pageId"), BsonDocument.Text("name"), bookId) == true)
                        {
                            return new InvokeResult<BsonDocument>() { Status = Status.Failed, Message = "该页面已经包含 " + BsonDocument.Text("name") + " 的子页面" };
                        }
                    }
                
                       var result= this._ctx.QuickDelete("BookPagePattern",BsonDocument.ChildBsonList("BookPagePattern"));
                       if(result.Status!=Status.Successful)
                       {
                       return result;
                       }
                     var oldEntity = FindById(BsonDocument.Int("pageId"));
                     if (oldEntity != null)
                     {
                         var updateResult = this._ctx.Update(BsonDocument, updateBosn);
                         if (updateResult.Status != Status.Successful)
                         {
                             return updateResult;
                         }
                         else
                         {
                             BsonDocument = updateResult.BsonInfo;
                         }
                     }
                     else
                     {
                         var addResult = this._ctx.Insert("BookPage", BsonDocument);
                         if (addResult.Status != Status.Successful)
                         {
                             return addResult;
                         }
                         else
                         {
                             BsonDocument = addResult.BsonInfo;
                         }
                     }

                    if (patternIds.Count() > 0)
                    {   var newBookPagePatternList=new List<BsonDocument>();
                        foreach (var id in patternIds)
                        {
                                var newBookPagePattern=new BsonDocument();
                                newBookPagePattern.Add("pageId",BsonDocument.Int("pageId"));
                                newBookPagePattern.Add("patternId", id );
                                if (this._ctx.FindAllByQueryStr("BookPagePattern", string.Format("pageId={0}&patternId={1}", BsonDocument.Int("pageId"), id)).Count() == 0)
                                {
                                    newBookPagePatternList.Add(newBookPagePattern);
                                }
                        }
                         this._ctx.QuickInsert("BookPagePattern",newBookPagePatternList);
                    }
                   
                    
                    scope.Complete();
                }
                return new InvokeResult<BsonDocument>() { Value = BsonDocument, Status = Status.Successful };
            }
            catch (Exception ex)
            {
                return new InvokeResult<BsonDocument>() { Value = BsonDocument, Status = Status.Failed, Message = ex.Message };
            }

            //BuildIndex(BsonDocument);

        }

        /// <summary>
        /// 删除页面
        /// </summary>
        /// <param name="matCat"></param>
        /// <returns></returns>
        public InvokeResult Delete(BsonDocument BsonDocument)
        {
            InvokeResult result = new InvokeResult();
            result.Status = Status.Failed;

            try
            {
                try
                {
                   // DeleteIndex(BsonDocument);
                }
                catch (Exception ex)
                { }
                using (TransactionScope scope = new TransactionScope())
                {
                    #region 关联删除
                    if (!string.IsNullOrEmpty(BsonDocument.Text("refPageId")))
                    {
                        var refPage = this.FindAll().Where(m => m.Int("pageId") == BsonDocument.Int("refPageId")).FirstOrDefault();
                        if (refPage != null && refPage.Int("refTimes") > 0)
                        {
                            var updateBson=new BsonDocument();
                            updateBson.Add("refTimes",refPage.Int("refTimes")-1);
                            this._ctx.Update(refPage,updateBson);
                        }
                    }
                    #endregion

                    result = this._ctx.Delete("BookPage", "db.BookPage.distinct('_id',{'pageId':'" + BsonDocument.Int("pageId") + "'})");

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

        /// <summary>
        /// 删除页面
        /// </summary>
        /// <param name="matCatId"></param>
        /// <returns></returns>
        public InvokeResult Delete(int pageId)
        {
            BsonDocument BsonDocument = this.FindById(pageId);

            return Delete(BsonDocument);
        }

        /// <summary>
        /// 移动页面
        /// </summary>
        /// <param name="moveId"></param>
        /// <param name="toMoveId"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public InvokeResult MoveBookPage(int moveId, int toMoveId, string type)
        {
            InvokeResult result = new InvokeResult();
            result = _ctx.Move("BookPage", moveId.ToString(), toMoveId.ToString(),type);
            return result;
        }
        #endregion

        #region 统计查询

        /// <summary>
        /// 统计指定类目下的子类目个数
        /// </summary>
        /// <param name="matCatId"></param>
        /// <returns></returns>
        public int GetChildNodeCount(int pageId)
        {
            return this.FindAll().Where(m => m.Int("nodePid") == pageId && m.Int("pageId") != pageId).Count();
        }

        #endregion

        #region 私有函数

        /// <summary>
        /// 判断同一父节点下是否有同名的类目
        /// </summary>
        /// <param name="nodePid"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private bool ExsistName(int nodePid, int pageId, string name, int bookId)
        {
            var count = 0;
            if (bookId != 0)
            {
                 var bookTask=_ctx.FindOneByKeyVal("BookTask","bookId",bookId.ToString());
                if (bookTask != null)
                {
                    count = bookTask.ChildBsonList("BookPage").Where(m => m.Int("nodePid") == nodePid && m.Text("name") == name && m.Int("pageId") != pageId).Count();
                }
                else
                {
                    count = this.FindAll().Where(m => m.Int("nodePid") == nodePid && m.Text("name") == name && m.Int("pageId") != pageId && m.Int("bookId") == bookId).Count();
                }

            }
            else
            {
                count = this.FindAll().Where(m => m.Int("nodePid") == nodePid && m.Text("name") == name && m.Int("pageId") != pageId && m.Int("IsPageTempate") == 1).Count();
            }
            if (count > 0)
                return true;
            else
                return false;
        }

        /// <summary>
        /// 判断同一父节点下是否有同名的类目
        /// </summary>
        /// <param name="nodePid"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private bool ExsistName(int nodePid, string name, int bookId)
        {
            var count = 0;
            if (bookId != 0)
            {
                var bookTask=_ctx.FindOneByKeyVal("BookTask","bookId",bookId.ToString());
                if (bookTask != null)
                {
                    count = bookTask.ChildBsonList("BookPage").Where(m => m.Int("nodePid") == nodePid && m.Text("name") == name).Count();
                }
                else
                {
                    count = this.FindAll().Where(m => m.Int("nodePid") == nodePid && m.Text("name") == name && m.Int("bookId") == bookId).Count();
                }
            }
            else
            {
                count = this.FindAll().Where(m => m.Int("nodePid") == nodePid && m.Text("name") == name && m.Int("IsPageTempate") == 1).Count();
            }
            if (count > 0)
                return true;
            else
                return false;
        }

        private int GetCountByName(int nodePid, string name, int bookId)
        {
            var count = 0;
            if (bookId != 0)
            {
                var bookTask=_ctx.FindOneByKeyVal("BookTask","bookId",bookId.ToString());
                if (bookTask != null)
                {
                    count = bookTask.ChildBsonList("BookPage").Where(m => m.Int("nodePid") == nodePid && m.Text("name").StartsWith(name + "(") && m.Text("name").EndsWith(")")).Count();
                }
                else
                {
                    count = this.FindAll().Where(m => m.Int("nodePid") == nodePid && m.Text("name").StartsWith(name + "(") && m.Text("name").EndsWith(")") && m.Int("bookId") == bookId).Count();
                }
            }
            else
            {
                count = this.FindAll().Where(m => m.Int("nodePid") == nodePid && m.Text("name").StartsWith(name + "(") && m.Text("name").EndsWith(")") && m.Int("IsPageTempate") == 1).Count();
            }
            return count;
        }

        /// <summary>
        ///  判断同一父节点下是否有同名的类目
        /// </summary>
        /// <param name="nodePid"></param>
        /// <param name="matCatList"></param>
        /// <param name="existNames"></param>
        /// <returns></returns>
        private bool ExsistName(int nodePid, List<BsonDocument> BsonDocumentList, out string existNames, int bookId)
        {
            bool bResult = false;
            StringBuilder sbName = new StringBuilder();
            if (bookId != 0)
            {
                var bookTask=_ctx.FindOneByKeyVal("BookTask","bookId",bookId.ToString());
                if (bookTask != null)
                {
                    var query = from c in bookTask.ChildBsonList("BookPage")
                                where  c.Int("nodePid") == nodePid
                                select  c;
                    foreach (var q in query)
                    {
                        if (BsonDocumentList.Where(m => m.Text("name") == q.Text("name")).Count() > 0)
                        {
                            sbName.AppendFormat(",{0}", q.Text("name"));
                        }
                    }
                }
                else
                {
                    var query = from c in this.FindAll()
                                where c.Int("nodePid") == nodePid && c.Int("bookId") == bookId
                                select c;
                    foreach (var q in query)
                    {
                        if (BsonDocumentList.Where(m => m.Text("name") == q.Text("name")).Count() > 0)
                        {
                            sbName.AppendFormat(",{0}", q.Text("name"));
                        }
                    }
                }
                if (sbName.Length > 0)
                {
                    sbName = sbName.Remove(0, 1);
                    bResult = true;
                }
                existNames = sbName.ToString();
                return bResult;
            }
            else
            {
                var query = from c in this.FindAll()
                            where c.Int("nodePid") == nodePid && c.Int("IsPageTempate") == 1
                            select c;
                foreach (var q in query)
                {
                    if (BsonDocumentList.Where(m => m.Text("name") == q.Text("name")).Count() > 0)
                    {
                        sbName.AppendFormat(",{0}", q.Text("name"));
                    }
                }

                if (sbName.Length > 0)
                {
                    sbName = sbName.Remove(0, 1);
                    bResult = true;
                }
                existNames = sbName.ToString();
                return bResult;
            }

        }

        private BsonDocument CopyMaterialCategoryEntity(BsonDocument original, int userId)
        {
            BsonDocument newBsonDocument = new BsonDocument();
            newBsonDocument.Add("name" ,original.Text("name"));
            newBsonDocument.Add("remark" ,original.Text("remark"));
            newBsonDocument.Add("status" ,original.Text("status"));
            return newBsonDocument;
        }

        #endregion

        #region 复制页面

        /// <summary>
        /// 复制页面
        /// </summary>
        /// <param name="originalId"></param>
        /// <param name="toCopyId"></param>
        /// <param name="type"></param>
        /// <param name="currentUserId"></param>
        /// <returns></returns>
        public InvokeResult CopyBookPage(int originalId, int toCopyId, string type, int currentUserId, int bookId)
        {
            InvokeResult result = new InvokeResult();

            if (originalId == toCopyId)
            {
                result.Status = Status.Failed;
                return result;
            }
            BsonDocument toCopy = this.FindById(toCopyId);

            BsonDocument original = this.FindById(originalId);


            BsonDocument copy = this.CopyMaterialCategoryEntity(original, currentUserId);

            #region 复制顶级页面

            this.CopyRootNode(toCopy, original, copy, type, currentUserId, bookId);

            #endregion

            #region 复制子页面
            List<BsonDocument> orginalChildList = this.FindByNodeKey(original.Text("nodeKey"));

            orginalChildList.Remove(original);

            this.CopyChildNode(orginalChildList, original, copy, currentUserId);

            #endregion

            return result;
        }

        /// <summary>
        /// 复制根页面
        /// </summary>
        /// <param name="toCopy"></param>
        /// <param name="original"></param>
        /// <param name="copy"></param>
        /// <param name="type"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        private BsonDocument CopyRootNode(BsonDocument toCopy, BsonDocument original, BsonDocument copy, string type, int userId, int bookId)
        {
               var result=this._ctx.Insert("BookPage",copy);
               if(result.Status==Status.Successful&&result.BsonInfo!=null)
               {
                   copy=result.BsonInfo;
                   if (this.ExsistName(toCopy.Int("nodePid"), original.Text("name"), bookId) == true)
                   {
                            int iSameName = this.GetCountByName(toCopy.Int("nodePid"), original.Text("name"), bookId) + 1;
                            var updateBson=new BsonDocument();
                            updateBson.Add("name", original.Text("name") + "(" + iSameName.ToString() + ")");
                           this._ctx.Update(copy,updateBson);
                    }
                    this._ctx.Move(copy.Text("pageId"),original.Text("pageId"),type);
                    copy= this._ctx.FindOneByKeyVal("BookPage","pageId",copy.Text("pageId"));
                }

            return copy;
        }

        /// <summary>
        /// 复制子页面
        /// </summary>
        /// <param name="orginalChildList"></param>
        /// <param name="parent"></param>
        /// <param name="newParent"></param>
        /// <param name="userId"></param>
        private void CopyChildNode(List<BsonDocument> orginalChildList, BsonDocument parent, BsonDocument newParent, int userId)
        {
            List<BsonDocument> childList = orginalChildList.Where(o => o.Int("nodePid") == parent.Int("pageId")).ToList();
          
            foreach (BsonDocument cat in childList)
            {
                BsonDocument copyChild = this.CopyMaterialCategoryEntity(cat, userId);
                copyChild.Add("nodePid",newParent.Int("pageId"));
                this._ctx.Insert("BookPage",copyChild);
                this.CopyChildNode(orginalChildList, cat, copyChild, userId);
             
            }
        }

        //private BsonDocument MoveEntity(BsonDocument toCopy, BsonDocument copy, Func<BsonDocument, bool> fun, int increase)
        //{
        //    IQueryable<BsonDocument> toCopyList = this.FindAll().Where(fun).OrderByDescending(m => m.nodeOrder).AsQueryable();
        //    foreach (BsonDocument cat in toCopyList)
        //    {
        //        string oldNodeKey = cat.Text("nodeKey");
        //        var newNodeKey = this.GetNodeKey(oldNodeKey, increase);
        //        //更新该节点下的所有自己节点
        //        this.UpdateChildeNodeKey(oldNodeKey, newNodeKey);
        //        int oldNodeOrder = cat.nodeOrder;
        //        cat.nodeOrder = oldNodeOrder + increase;
        //        cat.Text("nodeKey") = newNodeKey;

        //        this._ctx.SubmitChanges();
        //    }
        //    return copy;
        //}

        

        #endregion

        #region 索引相关
 

        #endregion

        #region 模板相关

        /// <summary>
        /// 分页获取page模板
        /// </summary>
        /// <param name="currPage"></param>
        /// <param name="pageSize"></param>
        /// <param name="count"></param>
        /// <param name="orderBy"></param>
        /// <param name="order"></param>
        /// <param name="keyword"></param>
        /// <returns></returns>
        public IQueryable<BsonDocument> FindTemplateByPage(int currPage, int pageSize, out int count, string orderBy, string order, string keyword)
        {
            var q = this.FindAllTemplate();
            if (!string.IsNullOrEmpty(keyword))
            {
                q = q.Where(r => r.Text("name").Contains(keyword));
            }
            if (!string.IsNullOrEmpty(orderBy))
            {
                switch (orderBy)
                {

                    case "name":
                        if (order == "asc")
                            q = q.OrderBy(r => r.Text("name"));
                        else
                            q = q.OrderByDescending(r => r.Text("name"));
                        break;
                    case "update":
                    default:
                        if (order == "asc")
                            q = q.OrderBy(r => r.Date("updateDate"));
                        else
                            q = q.OrderByDescending(r => r.Date("updateDate"));
                        break;
                }
            }
            q = q.Skip(currPage * pageSize).Take(pageSize);
            count = q.Count();
            return q;

        }

        public IQueryable<BsonDocument> FindAllTemplate()
        {
            return this.FindAll().Where(r => r.Int("IsPageTempate") == 1).AsQueryable();
        }

        /// <summary>
        ///载入模板
        /// </summary>
        /// <param name="pageId"></param>
        /// <param name="type"></param>
        /// <param name="refPageId"></param>
        /// <returns></returns>
        public InvokeResult LoadPageTemplate(int pageId, int type, int refPageId, int userId)
        {
            InvokeResult result = new InvokeResult();
            try
            {
               
                using (TransactionScope scope = new TransactionScope())
                { 
                    FileOperationHelper opHelper = new FileOperationHelper();
                    var page = this.FindById(pageId);
                    if (page != null)
                    {
                        if (refPageId != 0)
                        {  var pageUpdateBson=new BsonDocument();
                            var refPageUpdateBson=new BsonDocument();
                            pageUpdateBson.Add("refPageId",refPageId.ToString());
                            var pageResult=this._ctx.Update(page,pageUpdateBson);
                            if(pageResult.Status!=Status.Successful)
                            {
                            return pageResult;
                            }
                            var refPage = this._ctx.FindOneByKeyVal("BookPage","pageId",refPageId.ToString());
                            if (refPage != null)
                            {
                                refPageUpdateBson.Add("refTimes",refPage.Int("refTimes") + 1);
                                if (type == 1)
                                {
                                     var pageDelResult=opHelper.DeleteFile("BookPage","pageId",page.Text("pageId")) ;
                                     if(pageDelResult.Status!=Status.Successful)
                                     {
                                        return pageDelResult;
                                     }
                                   
                                }
                                var files =  _ctx.FindAllByQueryStr("FileRelation", "tableName=BookPage&keyValue=" + refPageId.ToString()).ToList();
                                //复制文档
                                result=opHelper.CopyFileRelation(files,"BookPage","pageId",refPageId.ToString());
                                if(result.Status!=Status.Successful)
                                {
                                return result;
                                }
                                  
                            }
                        }
                    }
                    scope.Complete();
                }
                result.Status = Status.Successful;
            }
            catch (Exception ex)
            {
                result.Status = Status.Failed;
                result.Message = ex.Message;
            }
            return result;
        }
        #endregion

        #region 网页快照,pdf相关
        /// <summary>
        /// 生成页面快照
        /// </summary>
        /// <param name="pageId"></param>
        /// <returns></returns>
        public InvokeResult  GenerateSnapshot(int pageId)
        {
            InvokeResult result = new InvokeResult();
            var pageEntity = this.FindById(pageId);
            if (pageEntity == null)
            {
                result.Status = Status.Failed;
                result.Message = "页面已被删除";
                return result;
            }
            var pageBody = pageEntity.ChildBsonList("PageBody").FirstOrDefault();
            if (pageBody == null || string.IsNullOrEmpty(pageBody.Text("body")))
            {
                result.Status = Status.Failed;
                result.Message = "页面还未创建内容或内容为空";
                return result;
            }
            string dir = string.Empty;
            string orgionPath = GetBookPageOrgionImg(pageId, out dir);
            dir = System.Web.HttpContext.Current.Server.MapPath(dir);
            orgionPath = System.Web.HttpContext.Current.Server.MapPath(orgionPath);
            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }
            string dir2 = string.Empty;
            string snapPath = GetBookPageSnapshot(pageId, out dir2);
            dir2 = System.Web.HttpContext.Current.Server.MapPath(dir2);
            string thumbPath = System.Web.HttpContext.Current.Server.MapPath(snapPath);
            if (!System.IO.Directory.Exists(dir2))
            {
                System.IO.Directory.CreateDirectory(dir2);
            }
            //string html = System.Web.HttpContext.Current.Server.UrlDecode(pageBody.body);
            string url = string.Format("http://{0}/account/PageVersionDetail/?pagId={1}", System.Web.HttpContext.Current.Request.ServerVariables["HTTP_HOST"], pageBody.Int("pageId"));
             HtmlToImg htmlToImg = new  HtmlToImg(url, orgionPath, thumbPath, 200, 200);
            var tag =false; 
           // var tag = htmlToImg.GenerateByUrl();
            if (tag == false)
            {
                result.Status = Status.Failed;
                result.Message = "页面快照生成失败";
                result.BsonInfo=pageEntity;
                return result;
            }
            else
            {
                var updateBson=new BsonDocument();
                 updateBson.Add("snapPath",snapPath);
                 result= _ctx.Update(pageEntity,pageEntity);
              
            }

            
            return result;

        }
       /// <summary>
        /// 生成任务书的pdf文档,单页面加载
       /// </summary>
       /// <param name="bookTaskId"></param>
       /// <param name="path"></param>
       /// <param name="addYinhooTag"></param>
       /// <returns></returns>
        public bool GeneratePdf(int bookTaskId, out string path, string customerName)
        {
            var q = this.FindAllByBookId(bookTaskId).ToList();
            var bookTask = BookTaskBll._().FindById(bookTaskId);
            var pdfPages = new List<PdfDoc>();
            
            foreach (BsonDocument BsonDocument in q)
            {
                 PdfDoc page = new  PdfDoc();

                page.Id = BsonDocument.Int("pageId");
                if (BsonDocument.ChildBsonList("PageBodies").FirstOrDefault() != null)
                {
                    //通过url直接生成pdf用
                    string url = string.Format("http://{0}/account/PageVersionDetail/?pagId={1}", System.Web.HttpContext.Current.Request.ServerVariables["HTTP_HOST"], BsonDocument.Int("pageId"));
                    page.Url = url;
                }
                page.Name = BsonDocument.Text("name");
                page.NodeKey = BsonDocument.Text("nodeKey");
                page.NodeLevel = BsonDocument.Int("nodeLevel");
                page.NodeOrder = BsonDocument.Int("nodeOrder");
                page.NodePid = BsonDocument.Int("nodePid");
                page.Expended = true;
                pdfPages.Add(page);
            }
            string dir2 = string.Empty;
            string savePath =  GetBookPagePdf(bookTaskId, out dir2);
            dir2 = System.Web.HttpContext.Current.Server.MapPath(dir2);
            savePath = System.Web.HttpContext.Current.Server.MapPath(savePath);
            if (!System.IO.Directory.Exists(dir2))
            {
                System.IO.Directory.CreateDirectory(dir2);
            }
            string bookTaskUrl = string.Format("<a href=\"{0}\">{1}</a>",
                "http://" + System.Web.HttpContext.Current.Request.ServerVariables["HTTP_HOST"] + "/MissionStatement/Home/BsonDocument/" + bookTaskId,
                bookTask.Text("name"));
            var tag = HtmlHelper.MergePdf2(pdfPages, savePath, customerName, bookTaskUrl);
            if (tag)
            {
                path = savePath;
                return true;
            }
            else
            {
                path = null;
                return false;
            }
        }
        /// <summary>
        /// 任务书导出方法2:全页面加载
        /// </summary>
        /// <param name="bookTaskId"></param>
        /// <param name="path"></param>
        /// <param name="addYinhooTag"></param>
        /// <param name="offsets"></param>
        /// <returns></returns>
        public bool GeneratePdf(int bookTaskId, string url, out string path, string customerName, Dictionary<int, int> tops) 
        {
            var q = this.FindAllByBookId(bookTaskId).ToList();
            var pdfPages = new List<PdfDoc>();

            foreach (BsonDocument BsonDocument in q)
            {
                PdfDoc page = new PdfDoc();

                page.Id = BsonDocument.Int("pageId");
                page.Name = BsonDocument.Text("name");
                page.NodeKey = BsonDocument.Text("nodeKey");
                page.NodeLevel = BsonDocument.Int("nodeLevel");
                page.NodeOrder = BsonDocument.Int("nodeOrder");
                page.NodePid= BsonDocument.Int("nodePid");
                page.Expended = true;
                pdfPages.Add(page);
            }
            string dir2 = string.Empty;
            string savePath =  GetBookPagePdf(bookTaskId, out dir2);
            dir2 = System.Web.HttpContext.Current.Server.MapPath(dir2);
            savePath = System.Web.HttpContext.Current.Server.MapPath(savePath);
            if (!System.IO.Directory.Exists(dir2))
            {
                System.IO.Directory.CreateDirectory(dir2);
            }
            var tag = HtmlHelper.MergePdf3(pdfPages, url, savePath, customerName, tops);
            if (tag)
            {
                path = savePath;
                return true;
            }
            else
            {
                path = null;
                return false;
            }
            return false;
        }
        /// <summary>
        /// 按一级目录生成pdf
        /// </summary>
        /// <param name="bookTaskId"></param>
        /// <param name="path"></param>
        /// <param name="customerName"></param>
        /// <returns></returns>
        public bool GeneratePdf2(int bookTaskId, out string path, string customerName)
        {
            var q = this.FindByNodeLevel(2, bookTaskId);
            var bookTask = BookTaskBll._().FindById(bookTaskId);

            var pdfPages = new List<PdfDoc>();

            foreach (BsonDocument BsonDocument in q)
            {
                PdfDoc page = new PdfDoc();

                string url = string.Format("http://{0}/account/PageVersionChapter/{1}", System.Web.HttpContext.Current.Request.ServerVariables["HTTP_HOST"], BsonDocument.Int("pageId"));
                page.Url = url;
                page.Id = BsonDocument.Int("pageId");
                page.Name  = BsonDocument.Text("name");
                page.NodeKey = BsonDocument.Text("nodeKey");
                page.NodeLevel = BsonDocument.Int("nodeLevel");
                page.NodeOrder = BsonDocument.Int("nodeOrder");
                page.NodePid= BsonDocument.Int("nodePid");
                page.Expended = true;
                pdfPages.Add(page);
            }
            string dir2 = string.Empty;
            string savePath =  GetBookPagePdf(bookTaskId, out dir2);
            dir2 = System.Web.HttpContext.Current.Server.MapPath(dir2);
            savePath = System.Web.HttpContext.Current.Server.MapPath(savePath);
            if (!System.IO.Directory.Exists(dir2))
            {
                System.IO.Directory.CreateDirectory(dir2);
            }
            string bookTaskUrl = string.Format("<a href=\"{0}\"><font color=\"#333333\">{1}</font></a>",
                "http://" + System.Web.HttpContext.Current.Request.ServerVariables["HTTP_HOST"] + "/MissionStatement/Home/BsonDocument/" + bookTaskId, 
                bookTask.Text("name"));
            var tag = HtmlHelper.MergePdf4(pdfPages, savePath, customerName, bookTaskUrl);
            if (tag)
            {

                path = savePath;
                return true;
            }
            else
            {
                path = null;
                return false;
            }
            return false;
        }
        #endregion

        #region Pdf 快照存储
        /// <summary>
        /// page快照路径
        /// </summary>
        /// <param name="pageId"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static string GetBookPageSnapshot(int pageId, out string dir)
        {
            dir = "/UploadFiles/MissionStatement/bookPageThumb";
            return string.Format("/UploadFiles/MissionStatement/bookPageThumb/{0}.jpg", pageId);
        }
        /// <summary>
        /// page页面原始大小图像路径
        /// </summary>
        /// <param name="pageId"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static string GetBookPageOrgionImg(int pageId, out string dir)
        {
            dir = "/UploadFiles/MissionStatement/bookPageOrgion";
            return string.Format("/UploadFiles/MissionStatement/bookPageOrgion/{0}.jpg", pageId);
        }
        /// <summary>
        /// 任务书pdf文件存放路径
        /// </summary>
        /// <param name="bookId"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static string GetBookPagePdf(int bookId, out string dir)
        {
            dir = "/UploadFiles/MissionStatement/bookTaskPDF";
            return string.Format("/UploadFiles/MissionStatement/bookTaskPDF/{0}.pdf", bookId);
        }
      #endregion
    }
}
