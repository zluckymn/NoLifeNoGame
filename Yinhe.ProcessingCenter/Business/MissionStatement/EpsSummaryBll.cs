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
    public class EpsSummaryBll
    {
        #region 构造函数
        /// <summary>
        /// 类私有变量
        /// </summary>
        private DataOperation _ctx = null;

        /// <summary>
        /// 封闭当前默认构造函数
        /// </summary>
        private EpsSummaryBll()
        {
            _ctx = new DataOperation();
        }

        private EpsSummaryBll(DataOperation ctx)
        {
            _ctx = ctx;
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static EpsSummaryBll _()
        {
            return new EpsSummaryBll();
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static EpsSummaryBll _(DataOperation ctx)
        {
            return new EpsSummaryBll(ctx);
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
            var hitObj = this._ctx.FindOneByKeyVal("EpsSummary", "SummaryrefId", id.ToString());
            return hitObj;
        }

        /// <summary>
        /// 查找所有
        /// </summary>
        /// <returns></returns>
        public IQueryable<BsonDocument> FindAll()
        {
           return this._ctx.FindAll("EpsSummary").AsQueryable();
        }

        public IQueryable<BsonDocument> FindByIdAndType(List<string> ids, int type)
        {
            switch (type)
            {
                case 0:
                default:
                    var a = string.Format("epsId={0}&projId={1}","","");
                    return this._ctx.FindAllByQueryStr("EpsSummary",a).Where(c=>ids.Contains(c.Text("engId"))).AsQueryable();
                case 1:
                case 2:
                   var hitCollection = this._ctx.FindAllByKeyValList("EpsSummary", "epsId", ids);
                   return hitCollection.AsQueryable();
                case 3:
                     var  hitCollection1 = this._ctx.FindAllByKeyValList("EpsSummary", "projId", ids);
                     return hitCollection1.AsQueryable();
                  

            }
        }

        /// <summary>
        /// 汇总成果被项目总结引用的情况
        /// </summary>
        /// <param name="projIdList"></param>
        /// <param name="pageSize"></param>
        /// <param name="curPage"></param>
        /// <param name="keyWord"></param>
        /// <param name="allCount"></param>
        /// <returns></returns>
        //public object SearchEpsSummaryRet(List<int> projIdList,int pageSize,int curPage,string keyWord,out int allCount)
            public object SearchEpsSummaryRet(List<int> projIdList,int pageSize,int curPage,string keyWord,out long allCount)
        {  
            //    var resultList = from epsSummary in this._ctx.EpsSummaries
            //                 join bookPage in  this._ctx.BookTaskPages
            //                 on epsSummary.bookId equals bookPage.bookId
            //                 join pageResult in this._ctx.PageRefResults
            //                 on bookPage.pageId equals pageResult.pageId
            //                 where epsSummary.projId.HasValue && projIdList.ToArray().Contains(epsSummary.projId.Value)
            //                 select new
            //                 {
            //                     projRetId = pageResult.projRetId,
            //                     projId = epsSummary.projId.Value,
            //                     name = pageResult.ProjectResult.name,
            //                     pageName = bookPage.BookPage.name,
            //                     summaryName = epsSummary.BookTask.name,
            //                     projPath = GetProjectPath(epsSummary.projId.Value),
            //                     updateUser = epsSummary.BookTask.updateUserId.HasValue ? epsSummary.BookTask.SysUser1.name : "",
            //                     updateDate = epsSummary.BookTask.updateDate,
            //                 };
            //#endregion
                #region 获取数据
             
                var updateDate=new DateTime();
                var resultList = from epsSummary in FindAll()
                             join bookPage in this._ctx.FindAll("BookTaskPage")
                             on epsSummary.Int("bookId") equals bookPage.Int("bookId")
                             join pageResult in this._ctx.FindAll("PageRefResult")
                             on bookPage.Int("pageId") equals pageResult.Int("pageId")
                              where !string.IsNullOrEmpty(epsSummary.Text("projId")) && projIdList.ToArray().Contains(epsSummary.Int("projId"))
                             select new
                             {
                                 projRetId = pageResult.Int("projRetId"),
                                 projId = epsSummary.Int("IntprojId"),
                                 name = pageResult.SourceBsonField("projRetId","name"),
                                 pageName=bookPage.SourceBson("pageId").Text("name"),
                                 summaryName = epsSummary.SourceBson("bookId").Text("name"),
                                 projPath = GetProjectPath(epsSummary.Int("projId")),
                                 updateUser = epsSummary.SourceBson("bookId").UpdateUserName(),
                                 updateDate = epsSummary.SourceBson("bookId").ShortDate("updateDate")
                             };
            #endregion

            #region 根据关键字筛选

            if (keyWord.Trim() != "")
            {
              resultList = resultList.Where(t => t.name.Contains(keyWord.Trim()) || t.pageName.Contains(keyWord) || t.summaryName.Contains(keyWord));
            }

            #endregion

            #region 根据页面获取对应数据
            //allCount = resultList.Count();
            allCount = this._ctx.FindAll("resultList").Count();
            if (pageSize == 0 && curPage == 0)
            {
                if (curPage == 0) curPage = 1;

                resultList = resultList.Skip((curPage - 1) * pageSize).Take(pageSize);
            }
            #endregion
             var resultJsonList = from result in resultList.ToList() 
                       select new
                                 {
                                     projRetId = result.projRetId,
                                     projId = result.projId,
                                     name = result.name,
                                     pageName = result.pageName,
                                     summaryName = result.summaryName,
                                     projPath = result.projPath,
                                     updateUser = result.updateUser,
                                     updateDate = updateDate
                                        };
            return resultJsonList;
        }

        private string GetProjectPath(int id)
        {
            var project = _ctx.FindOneByKeyVal("XH_DesignManage_Project", "projId", id.ToString());
            if (project != null)
            {
                var engName = project.SourceBsonField("engId","name") ;
                string projName = engName.Trim() != "" ? engName + " - " : "";
                projName += project.Text("name");
                return projName;
            }
            return String.Empty;
        }
        #endregion
    }
}
