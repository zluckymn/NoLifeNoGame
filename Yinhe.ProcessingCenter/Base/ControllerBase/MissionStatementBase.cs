using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using MongoDB.Bson;
using System.Web;
using Yinhe.ProcessingCenter.Common;
using MongoDB.Driver.Builders;
using Yinhoo.Autolink.Business.MissionStatement;
using System.Text.RegularExpressions;
namespace Yinhe.ProcessingCenter
{
    /// <summary>
    /// 任务书后台管理基类
    /// </summary>
    public partial class ControllerBase : Controller
    {
        public ActionResult CheckUpPageStatus(int? bookPageId)
        {
            var json = new PageJson();
            json.Success = true;
            int status = 0;
            string checkUserName = dataOp.GetCurrentUserName();
            json.AddInfo("status", "1");
            json.AddInfo("checkUserName", checkUserName);
            return Json(json);
        }

        #region 签入签出

        #endregion
        #region 保存草稿
        [AcceptVerbs(HttpVerbs.Post)]
        [ValidateInput(false)]
        public JsonResult SaveDraft(string tempBody, int? pageId)
        {
            PageJson json = this.IfCanSave(pageId);

            if (json.Success)
            {
                FileCheckManageBll chkMgrBll = FileCheckManageBll._();
                BsonDocument pageTempBody = new BsonDocument();
                pageTempBody.Add("pageId", pageId ?? 0);
                pageTempBody.Add("tempBody", tempBody);
                pageTempBody.Add("saveDate", DateTime.Now);
                pageTempBody.Add("saveUserId", this.CurrentUserId);

                InvokeResult result = chkMgrBll.UpdateCheckOutTime(pageTempBody.Int("pageId"), this.CurrentUserId);
                json.Success = result.Status == Status.Successful;
                json.Message = result.Message;
                if (json.Success && pageTempBody.Text("tempBody") != string.Empty)
                {
                    Yinhoo.Autolink.Business.MissionStatement.PageTempBodyBll pageTempBll = Yinhoo.Autolink.Business.MissionStatement.PageTempBodyBll._();
                    InvokeResult result2 = new InvokeResult();
                    BsonDocument entity = pageTempBll.FindByPageIdUserId(pageTempBody.Int("pageId"), this.CurrentUserId);//已存储的草稿
                    BsonDocument body = PageBodyBll._().FindByPageId(pageTempBody.Int("pageId"));
                    if (entity != null)//已经保存过草稿
                    {
                        if (entity.Text("tempBody") != pageTempBody.Text("tempBody"))//欲保存的内容和已存储的草稿内容不同
                        {
                            if (body != null) //已经保存过正文
                            {
                                if (body.Text("body") != pageTempBody.Text("tempBody"))//和已保存的正文内容不同，则更新
                                {
                                    var updateBson = new BsonDocument();
                                    updateBson.Add("saveDate", DateTime.Now);
                                    updateBson.Add("tempBody", pageTempBody.Text("tempBody"));
                                    result2 = pageTempBll.Update(entity, updateBson);
                                }
                                else
                                {
                                    result2.Status = Status.Successful;
                                }
                            }
                            else //没有保存过正文
                            {
                                var updateBson2 = new BsonDocument();
                                updateBson2.Add("saveDate", DateTime.Now);
                                updateBson2.Add("tempBody", pageTempBody.Text("tempBody"));
                                result2 = pageTempBll.Update(entity, updateBson2);
                            }
                        }
                        else
                        {
                            result2.Status = Status.Successful;
                        }
                    }
                    else
                    {
                        if (body == null)
                        {
                            result2 = pageTempBll.Insert(pageTempBody);
                        }
                        else
                        {
                            if (body.Text("body") != pageTempBody.Text("tempBody"))
                            {
                                result2 = pageTempBll.Insert(pageTempBody);
                            }
                            else
                            {
                                result2.Status = Status.Successful;
                            }
                        }
                    }
                    json.Success = result2.Status == Status.Successful;
                    json.Message = result2.Message;
                }
            }
            json.Message = json.Message == null ? string.Empty : json.Message;
            json.AddInfo("saveTime", DateTime.Now.ToString("yyyy-MM-dd tt HH:mm:ss"));
            return Json(json);
        }

        [NonAction]
        private PageJson IfCanSave(int? pageId)
        {
            PageJson json = new PageJson();
            int status = 0;
            string checkUserName = string.Empty;
            if (pageId == null || pageId == 0)
            {
                json.Success = false;
                json.Message = "参数错误";
                return json;
            }
            FileCheckManageBll chkMgrBll = FileCheckManageBll._();
            BookPageBll bookBll = BookPageBll._();
            BsonDocument pageEntity = bookBll.FindById(pageId ?? 0);
            if (pageEntity == null)
            {
                json.Success = false;
                json.Message = "该页面已被他人删除";
                return json;
            }
            //#region 判断是否嵌入签出
            //if (pageEntity != null)
            //{
            //    BsonDocument bookChk = chkMgrBll.FindByKey(pageEntity.Int("bookId"), FileCheckManage.Task_File_Identify);
            //    if (bookChk != null && bookChk.checkOutUserId != this.CurrentUserId) //检查该页面所在任务书结构的签出状态
            //    {
            //        #region 任务书结构被签出，且签出人不是自己
            //        json.Success = false;
            //        json.Message = "任务书结构已被“" + bookChk.SysUser.Text("name") + "”强制签出";
            //        status = 1;
            //        checkUserName = bookChk.SysUser.Text("name");
            //        #endregion
            //    }
            //    else
            //    {
            //        #region 任务书结构没有被签出，或者被自己签出
            //        FileCheckManage chkEntity = chkMgrBll.FindByKey(pageId ?? 0, FileCheckManage.Page_File_Identify);
            //        if (chkEntity == null)
            //        {
            //            json.Success = false;
            //            json.Message = "页面已被其他人强制签入";
            //            status = 2;
            //        }
            //        else
            //        {
            //            if (chkEntity.checkOutUserId != this.CurrentUserId)
            //            {
            //                json.Success = false;
            //                json.Message = "页面已被其“" + chkEntity.SysUser.Text("name") + "”强制签出";
            //                status = 3;
            //                checkUserName = chkEntity.SysUser.Text("name");
            //            }
            //            else
            //            {
            //                json.Success = true;
            //            }
            //        }
            //        #endregion
            //    }
            //}
            //else
            //{
            //    json.Success = true;
            //}
            //#endregion
            json.Success = true;
            json.AddInfo("status", status.ToString());
            json.AddInfo("checkOutUser", checkUserName);

            return json;
        }

        #endregion

        #region 成果关联预览
        [AcceptVerbs(HttpVerbs.Get)]
        public ViewResult FruitPreview(int? projResId, int? type)
        {
            var projRes = dataOp.FindOneByKeyVal("StandardResult_StandardResult", "retId", projResId.ToString());
            ViewData["projRes"] = projRes;
            ViewData["projResId"] = projResId;
            return View();
        }
        /// <summary>
        /// 成果预览模式选择
        /// </summary>
        /// <param name="projResId"></param>
        /// <param name="viewType"></param>
        /// <returns></returns>
        public ViewResult FruitPreviewMode(int? projResId, int? viewType)
        {
            var projRes = dataOp.FindOneByKeyVal("StandardResult_StandardResult", "retId", projResId.ToString());
            ViewData["projRes"] = projRes;
            ViewData["projResId"] = projResId;

            return View();
        }
        #endregion

        #region 媒体文件管理
        public ViewResult AttachmentManage(int? bookPageId)
        {

            var attachments = dataOp.FindAllByQueryStr("FileRelation", string.Format("tableName=BookPage&keyValue={0}", bookPageId));
            ViewData["attachments"] = attachments;
            return View();
        }

        #endregion

        #region 附件管理

        public ViewResult PageAttachment(int id)
        {
            var attachments = dataOp.FindAllByQueryStr("FileRelation", string.Format("tableName=BookPage&keyValue={0}", id));
            ViewData["attachments"] = attachments;
            return View();
        }


        public ViewResult ChooseAttach(int id /*pageId*/)
        {
            //PageFileBll pageFileBll = PageFileBll._();
            //ViewData["attachments"] = pageFileBll.FindByPageId(id, PageFileType.Attachment).ToList();
            var attachments = dataOp.FindAllByQueryStr("FileRelation", string.Format("tableName=BookPage&keyValue={0}", id));
            ViewData["attachments"] = attachments;
            ViewData["id"] = id;

            return View();
        }
        /// <summary>
        /// 附件引用到任务书内容里面
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public ViewResult ReferAttach(string ids /*,号分隔的id*/)
        {
            var fileIdList = ids.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            var fileList = dataOp.FindAllByKeyValList("FileLibrary", "fileId", fileIdList.ToList());
            ViewData["fileList"] = fileList;

            return View();
        }

        #endregion

        #region 页面模板


        /// <summary>
        /// 模板展示
        /// </summary>
        /// <returns></returns>
        public ViewResult PageTemplates()
        {
            int current = PageReq.GetParamInt("current");
            int pageSize = SysAppConfig.PageSize;
            current = current == 0 ? 1 : current;

            string strKeyWord = PageReq.GetParam("keyWord");
            int parentId = PageReq.GetParamInt("parentId");
            int allCount = 0;
            int stageId = PageReq.GetParamInt("stageId");
            int profId = PageReq.GetParamInt("profId");
            int patternId = PageReq.GetParamInt("patternId");
            BookPageBll bllBookPage = BookPageBll._();
            List<BsonDocument> pageList = new List<BsonDocument>();
            pageList = bllBookPage.Search(current, pageSize, strKeyWord.Trim(), stageId, profId, patternId, 1, out allCount, out parentId);
            ViewData["pageList"] = pageList.Where(m => m.Int("nodePid") != 0).ToList();
            ViewData["strKeyWord"] = strKeyWord;
            ViewData["totalPage"] = allCount % pageSize == 0 ? allCount / pageSize : (allCount / pageSize) + 1;
            ViewData["stageId"] = stageId;
            ViewData["profId"] = profId;
            ViewData["patternId"] = patternId;
            var pageTemplates = bllBookPage.FindAllTemplate().Where(m => m.Int("categoryId") == 1);
            ViewData["subStageList"] = pageTemplates.Where(m => m.Int("sysStageId") != 0).Select(m => m.SourceBson("stageId")).Distinct().ToList();
            ViewData["subProfList"] = pageTemplates.Where(m => m.Int("sysProfId") != 0).Select(m => m.SourceBson("profId")).Distinct().ToList();
            List<BsonDocument> subPatternList = new List<BsonDocument>();
            foreach (var template in pageTemplates)
            {
                subPatternList.AddRange(template.ChildBsonList("BookPagePattern").Select(m => m.SourceBson("patternId")).ToList());
            }
            ViewData["subPatternList"] = subPatternList.Distinct().ToList();
            ViewData["stageList"] = dataOp.FindAll("System_Stage").ToList();
            ViewData["profList"] = dataOp.FindAll("System_Professional").ToList();
            ViewData["patternList"] = dataOp.FindAll("System_Pattern").ToList();
            ViewData["parentId"] = parentId;
            return View();
        }

        /// <summary>
        /// 获取页面内容
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public string GetPageContent(int id)
        {
            BookPageBll bllBookPage = BookPageBll._();
            BsonDocument page = bllBookPage.FindById(id);
            if (page != null && page.ChildBsonList("PageBody").FirstOrDefault() != null)
                return page.ChildBsonList("PageBody").FirstOrDefault().Text("body");
            else
                return "";
        }




        /// <summary>
        /// 获取页面快照路径
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public string GetPageImage(int id)
        {
            BsonDocument page = BookPageBll._().FindById(id);
            string path = "";
            if (page != null && !String.IsNullOrEmpty(page.Text("snapPath")))
                path = page.Text("snapPath");
            return path == "" ? @"/Content/Images/zh-cn/Commitments/nopic.jpg" : path;
        }


        #endregion

        #region 任务书编辑

        #region 任务书与模板


        /// <summary>
        /// 保存任务书模板
        /// </summary>
        /// <param name="bookTask"></param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult SaveBookTaskTemplate()
        {
            PageJson json = new PageJson();
            BookTaskBll bllBookTask = BookTaskBll._();
            InvokeResult result = new InvokeResult();
            BsonDocument bookTask = new BsonDocument();
            var patternIdArray = PageReq.GetFormIntList("patternIdArray");
            var bookId = PageReq.GetFormInt("bookId");
            if (bookId == 0)
            {
                bookTask.Add("refTime", "0");
                bookTask.Add("name", PageReq.GetForm("name"));
                bookTask.Add("categoryId", PageReq.GetForm("categoryId"));
                bookTask.Add("sysStageId", PageReq.GetForm("sysStageId"));
                bookTask.Add("sysProfId", PageReq.GetForm("sysProfId"));
                bookTask.Add("remark", PageReq.GetForm("remark"));
                bookTask.Add("taskType", PageReq.GetForm("taskType"));
                bookTask.Add("type", "1");
                result = bllBookTask.Save(bookTask, new BsonDocument(), patternIdArray);
            }
            else
            {
                var oldBookTask = bllBookTask.FindById(bookId);
                var updateBookTask = new BsonDocument();
                updateBookTask.Add("name", PageReq.GetForm("name"));
                updateBookTask.Add("categoryId", PageReq.GetForm("categoryId"));
                updateBookTask.Add("sysStageId", PageReq.GetForm("sysStageId"));
                updateBookTask.Add("sysProfId", PageReq.GetForm("sysProfId"));
                updateBookTask.Add("remark", PageReq.GetForm("remark"));
                updateBookTask.Add("taskType", PageReq.GetForm("taskType"));
                result = bllBookTask.Save(oldBookTask, updateBookTask, patternIdArray);
            }
            json.Success = result.Status == Status.Successful;
            json.Message = result.Message;
            return this.Json(json);
        }

        ///// <summary>
        ///// 任务书列表
        ///// </summary>
        ///// <returns></returns>
        //public ActionResult BookTasks()
        //{
        //    //int getProjId = this.ProjectId; //设置分项ID

        //    int projId = PageReq.GetParamInt("projId");
        //    var project = dataOp.FindOneByKeyVal("XH_DesignManage_Project", "projId", projId.ToString());
        //    ViewData["project"] = project;

        //    int current = PageReq.GetParamInt("current");
        //    int pageSize = SysAppConfig.PageSize;
        //    current = current == 0 ? 1 : current;

        //    string strKeyWord = PageReq.GetParam("keyWord");
        //    int allCount = 0;
        //    BookTaskBll bllBoolTask = BookTaskBll._();
        //    List<BsonDocument> bookTaskList = new List<BsonDocument>();
        //    int stageId = PageReq.GetParamInt("stageId");
        //    int profId = PageReq.GetParamInt("profId");
        //    int patternId = PageReq.GetParamInt("patternId");

        //    int taskType = PageReq.GetParamInt("taskType");

        //    if (projId > 0)
        //    {
        //        bookTaskList = bllBoolTask.Search(current, pageSize, strKeyWord.Trim(), 0, stageId, profId, patternId, projId, taskType, out allCount);
        //    }
        //    else
        //    {
        //        bookTaskList = bllBoolTask.SearchTemplates(current, pageSize, strKeyWord.Trim(), 1, 0, stageId, profId, patternId, taskType, out allCount);
        //    }
        //    ViewData["bookTaskList"] = bookTaskList;
        //    ViewData["strKeyWord"] = strKeyWord;
        //    ViewData["totalPage"] = allCount % pageSize == 0 ? allCount / pageSize : (allCount / pageSize) + 1;
        //    ViewData["stageId"] = stageId;
        //    ViewData["profId"] = profId;
        //    ViewData["patternId"] = patternId;
        //    IQueryable<BsonDocument> bookTasks = null;
        //    if (projId != 0)
        //    {
        //        bookTasks = bllBoolTask.FindByTypeProjId(0, projId);
        //    }
        //    else
        //    {
        //        bookTasks = bllBoolTask.FindByType(0).Where(m => m.Int("categoryId") == 1);
        //    }

        //    ViewData["stageList"] = bookTasks.Where(m => m.Int("sysStageId") != 0).Select(m => m.SourceBson("stageId")).Distinct().ToList();
        //    ViewData["profList"] = bookTasks.Where(m => m.Int("sysProfId")!=0).Select(m => m.SourceBson("profId")).Distinct().ToList();

        //    List<BsonDocument> patternList = new List<BsonDocument>();
        //    foreach (var bookTask in bookTasks)
        //    {
        //        patternList.AddRange(bookTask.ChildBsonList("BookTaskPattern").Select(m => m.SourceBson("patternId")).ToList());
        //    }
        //    ViewData["patternList"] = patternList.Distinct().ToList();
        //    ViewData["projId"] = projId;
        //    ViewData["taskType"] = taskType;
        //    return View();
        //}




        /// <summary>
        /// 载入模板
        /// </summary>
        /// <param name="pageId"></param>
        /// <param name="type"></param>
        /// <param name="refPageId"></param>
        /// <returns></returns>
        public JsonResult LoadPageTemplate(int pageId, int type, int refPageId)
        {
            PageJson json = new PageJson();
            BookPageBll bllBookPage = BookPageBll._();
            InvokeResult result = bllBookPage.LoadPageTemplate(pageId, type, refPageId, this.CurrentUserId);
            json.Success = result.Status == Status.Successful;
            json.Message = result.Message;
            return this.Json(json);
        }

        /// <summary>
        /// 保存任务书模板
        /// </summary>
        /// <param name="bookTask"></param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult SaveBookTask()
        {
            int projId = PageReq.GetFormInt("projId");
            int taskId = PageReq.GetFormInt("taskId");
            PageJson json = new PageJson();
            BookTaskBll bllBookTask = BookTaskBll._();
            InvokeResult result = new InvokeResult();
            BsonDocument bookTask = new BsonDocument();
            var patternIdArray = PageReq.GetFormIntList("patternIdArray");
            var bookId = PageReq.GetFormInt("bookId");

            if (bookId == 0)
            {
                bookTask.Add("refTime", "0");
                bookTask.Add("name", PageReq.GetForm("name"));
                bookTask.Add("categoryId", PageReq.GetForm("categoryId"));
                bookTask.Add("sysStageId", PageReq.GetForm("sysStageId"));
                bookTask.Add("sysProfId", PageReq.GetForm("sysProfId"));
                bookTask.Add("remark", PageReq.GetForm("remark"));
                bookTask.Add("taskType", PageReq.GetForm("taskType"));
                bookTask.Add("refBookId", PageReq.GetForm("refBookId"));
                result = bllBookTask.Save(bookTask, new BsonDocument(), patternIdArray, projId, taskId);
            }
            else
            {
                var oldBookTask = bllBookTask.FindById(bookId);
                var updateBookTask = new BsonDocument();
                updateBookTask.Add("name", PageReq.GetForm("name"));
                updateBookTask.Add("categoryId", PageReq.GetForm("categoryId"));
                updateBookTask.Add("sysStageId", PageReq.GetForm("sysStageId"));
                updateBookTask.Add("sysProfId", PageReq.GetForm("sysProfId"));
                updateBookTask.Add("remark", PageReq.GetForm("remark"));
                updateBookTask.Add("taskType", PageReq.GetForm("taskType"));
                result = bllBookTask.Save(oldBookTask, updateBookTask, patternIdArray, projId, taskId);
            }
            json.Success = result.Status == Status.Successful;
            json.Message = result.Message;
            return this.Json(json);
        }

        /// <summary>
        /// 任务书或模板删除
        /// </summary>
        /// <param name="bookId"></param>
        public JsonResult DeleteBookTask(int id)
        {
            PageJson json = new PageJson();
            BookTaskBll bllBookTask = BookTaskBll._();
            InvokeResult result = bllBookTask.Delete(id);
            json.Success = result.Status == Status.Successful;
            json.Message = result.Message;
            return this.Json(json);
        }




        #endregion

        /// <summary>
        /// 获取业态json格式
        /// </summary>
        /// <returns></returns>
        public JsonResult GetPatternJson()
        {
            List<BsonDocument> patternList = dataOp.FindAll("System_Pattern").ToList();
            List<Item> itemList = new List<Item>();
            itemList = (from p in patternList
                        select new Item()
                        {
                            id = p.Int("patternId"),
                            name = p.Text("name"),
                            isUse = p.Int("isUse")
                        }).ToList();
            return Json(itemList, JsonRequestBehavior.AllowGet);
        }



        #region 任务书保存


        /// <summary>
        /// 获取草稿
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public JsonResult GetPageTempContent(int id)
        {
            PageTempBodyBll bllTempBody = PageTempBodyBll._();
            var tempBody = bllTempBody.FindByPageIdUserId(id, this.CurrentUserId);
            Item item = new Item()
            {
                id = tempBody != null ? tempBody.Int("tempBodyId") : 0,
                value = tempBody != null ? tempBody.Text("tempBody") : ""
            };
            return this.Json(item);
        }

        /// <summary>
        /// 保存页面内容
        /// </summary>
        /// <param name="pageId"></param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult SavePageContent(int pageId, string content, int refPageId, string strTag)
        {
            PageJson json = new PageJson();
            var retIds = PageReq.GetFormIntList("retIds"); ;//
            List<BsonDocument> tagList = new List<BsonDocument>(); //标签
            //if (strTag.Trim() != "")
            //{
            //    strTag = strTag.Trim();
            //    var arrStrTag = strTag.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            //    foreach (var tag in arrStrTag)
            //    {
            //        if (tagList.Where(m => m.Text("name") == tag).Count() > 0)
            //        {
            //            json.Success = false;
            //            json.Message = "标签名称有重复";
            //            return this.Json(json);
            //        }
            //        ObjectEntityTag tagModel = new ObjectEntityTag()
            //        {
            //            sysObjId = 3,
            //            name = tag,
            //            entityId = pageId,
            //            createUserId = this.CurrentUserId,
            //            createDate = DateTime.Now
            //        };
            //        tagList.Add(tagModel);
            //    }
            //}

            //判断是否可以保存
            PageJson pgJson = this.IfCanSave(pageId);
            if (pgJson.Success)
            {
                PageBodyBll bllBody = PageBodyBll._();
                BsonDocument body = new BsonDocument();
                body.Add("pageId", pageId);
                body.Add("body", content);
                InvokeResult result = bllBody.Save(body, refPageId, tagList, strTag, retIds);

                json.Success = result.Status == Status.Successful;
                json.Message = result.Message;
            }
            else
            {
                json.Success = false;
                json.Message = pgJson.Message;
            }
            return this.Json(json);
        }
        #endregion

        #region 书签
        /// <summary>
        /// 获取书签
        /// </summary>
        /// <returns></returns>
        public JsonResult GetPageMark()
        {
            int id = PageReq.GetParamInt("id");
            var pageMark = PageMarkBll._().FindById(id);
            if (pageMark != null)
            {
                var obj = new
                {
                    markId = pageMark.Text("markId"),
                    name = pageMark.Text("name"),
                    remark = pageMark.Text("remark")
                };
                return this.Json(obj);
            }
            return null;
        }

        /// <summary>
        /// 通过页面ID获取书签
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public JsonResult FindBookMarkByPageId(int id)
        {
            List<BsonDocument> pageMarkList = null;
            string keyWord = PageReq.GetParam("keyWord");
            try
            {
                pageMarkList = PageMarkBll._().FindByPageId(id, keyWord.Trim());
            }
            catch
            {
                pageMarkList = new List<BsonDocument>();
            }
            var jsonList = (from pageMark in pageMarkList
                            select new { markId = pageMark.Int("markId"), name = pageMark.Text("name"), pageId = pageMark.Int("pageId"), pageName = pageMark.SourceBson("pageId").Text("name") }).ToList();
            return this.Json(jsonList);
        }

        /// <summary>
        /// 通过任务书ID获取书签
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public JsonResult FindBookMarkByBookId(int id)
        {
            List<BsonDocument> pageMarkList = null;
            string keyWord = PageReq.GetParam("keyWord");
            try
            {
                pageMarkList = PageMarkBll._().FindByBookId(id, keyWord.Trim());
            }
            catch
            {
                pageMarkList = new List<BsonDocument>();
            }
            var jsonList = (from pageMark in pageMarkList
                            select new { markId = pageMark.Int("markId"), name = pageMark.Text("name"), pageId = pageMark.Int("pageId"), pageName = pageMark.SourceBson("pageId").Text("name") }).ToList();
            return this.Json(jsonList);
        }

        /// <summary>
        /// 书签编辑
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult PageMarkEdit(int? id)
        {
            PageJson json = new PageJson();
            InvokeResult result = new InvokeResult();
            int pageId = PageReq.GetFormInt("pageId");
            string name = PageReq.GetForm("name");
            PageMarkBll bllPageMark = PageMarkBll._();
            if (id.HasValue)
            {
                var oldPageMark = bllPageMark.FindById(id.Value);
                oldPageMark.Add("name", name);
                result = bllPageMark.Update(oldPageMark);
            }
            else
            {
                BsonDocument pageMark = new BsonDocument();
                pageMark.Add("name", name);
                pageMark.Add("pageId", pageId);
                pageMark.Add("markIdentity", name);
                result = bllPageMark.Insert(pageMark);
                id = result.BsonInfo.Int("markId");
            }
            json.Success = result.Status == Status.Successful;
            json.Message = result.Message;
            json.htInfo = new System.Collections.Hashtable();
            json.htInfo.Add("id", id.Value.ToString());
            return this.Json(json);
        }

        /// <summary>
        /// 标签删除
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public JsonResult DelPageMark(int id)
        {
            PageMarkBll bllPageMark = PageMarkBll._();
            InvokeResult result = new InvokeResult();
            result = bllPageMark.Delete(id);
            PageJson json = new PageJson();
            json.Success = result.Status == Status.Successful;
            json.Message = result.Message;
            return this.Json(json);
        }
        #endregion

        #region 目录




        /// <summary>
        /// 编辑页面
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult UpdateBookPage()
        {
            PageJson json = new PageJson();
            int bookId = PageReq.GetFormInt("bookId");
            var bookTask = BookTaskBll._().FindById(bookId);
            int id = PageReq.GetFormInt("id");
            string name = PageReq.GetForm("name");
            string remark = PageReq.GetForm("remark");
            int stageId = PageReq.GetFormInt("stageId");
            int profId = PageReq.GetFormInt("profId");
            string patternIds = PageReq.GetForm("patternIds");

            if (name == "")
            {
                json.Success = false;
                json.Message = "页面名称不能为空!";
                return this.Json(json);
            }
            BookPageBll bllBookPage = BookPageBll._();

            var bookPage = bllBookPage.FindById(id);
            string oldName = bookPage.Text("name");
            var updateBson = new BsonDocument();
            updateBson.Add("name", name);
            updateBson.Add("remark", remark);

            if (stageId != -1 && stageId != 0)
            {
                updateBson.Add("sysStageId", stageId);
            }

            if (profId != -1 && profId != 0)
            {
                updateBson.Add("sysProfId", profId);

            }
            string[] strPatternIds = patternIds.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            List<int> arrPatternIds = new List<int>();
            foreach (var strId in strPatternIds)
                arrPatternIds.Add(Int32.Parse(strId));

            InvokeResult result = bllBookPage.Update(bookPage, updateBson, oldName, bookId, arrPatternIds);
            json.Success = result.Status == Status.Successful;
            json.Message = result.Message;

            return this.Json(json);
        }

        /// <summary>
        /// 删除页面
        /// </summary>
        /// <param name="matCatId"></param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult DelBookPage()
        {
            PageJson json = new PageJson();
            InvokeResult result = new InvokeResult();

            int id = PageReq.GetFormInt("id");

            BookPageBll bookPageBll = BookPageBll._();

            IQueryable<BsonDocument> subList = bookPageBll.FindSubList(id);

            if (subList.Count() > 0)
            {
                json.Success = false;
                json.Message = "当前页面含有子页面,无法删除";
                return Json(json);
            }

            result = bookPageBll.Delete(id);

            json.Success = result.Status == Status.Successful;
            json.Message = result.Message;

            return this.Json(json);
        }
        /// <summary>
        /// 获取任务书
        /// </summary>
        /// <returns></returns>
        public JsonResult GetBookTask()
        {
            int id = PageReq.GetParamInt("id");
            var bookTask = BookTaskBll._().FindById(id);
            if (bookTask != null)
            {
                var patterns = "";
                foreach (var pattern in bookTask.ChildBsonList("BookTaskPattern").ToList())
                {
                    patterns += pattern.Int("patternId") + ":" + pattern.SourceBson("patternId").Text("name") + ",";
                }
                if (patterns != "")
                    patterns = patterns.Substring(0, patterns.Length - 1);
                var obj = new
                {
                    bookId = bookTask.Int("bookId"),
                    name = bookTask.Text("name"),
                    remark = bookTask.Text("remark"),
                    sysProfId = bookTask.Int("sysProfId"),
                    sysStageId = bookTask.Int("sysStageId"),
                    patterns = patterns
                };
                return this.Json(obj);
            }
            return null;
        }

        /// <summary>
        /// 获取页面
        /// </summary>
        /// <returns></returns>
        public JsonResult GetBookPage()
        {
            int id = PageReq.GetParamInt("id");
            var bookPage = BookPageBll._().FindById(id);
            var parentPage = BookPageBll._().FindById(bookPage.Int("nodePid"));
            if (bookPage != null)
            {
                var patterns = "";
                foreach (var pattern in bookPage.ChildBsonList("BookPagePattern").ToList())
                {
                    patterns += pattern.Int("patternId") + ":" + pattern.SourceBson("patternId").Text("name") + ",";
                }
                if (patterns != "")
                    patterns = patterns.Substring(0, patterns.Length - 1);
                var obj = new
                {
                    pageId = bookPage.Int("pageId"),
                    name = bookPage.Text("name"),
                    remark = bookPage.Text("remark"),
                    sysProfId = bookPage.Int("sysProfId"),
                    sysStageId = bookPage.Int("sysStageId"),
                    parentName = parentPage != null ? parentPage.Text("name") : "",
                    patterns = patterns
                };
                return this.Json(obj);
            }
            return null;
        }
        /// <summary>
        /// 移动目录
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult MoveBookPage()
        {
            PageJson json = new PageJson();
            InvokeResult result = new InvokeResult();

            int moveId = PageReq.GetFormInt("moveid");
            int toMoveId = PageReq.GetFormInt("tomoveid");
            string type = PageReq.GetForm("type");

            if (moveId == 0 || toMoveId == 0 || moveId == toMoveId)
            {
                json.Success = false;
                json.Message = "请正确选择移动节点！";
                return Json(json);
            }

            BookPageBll bookPageBll = BookPageBll._();

            BsonDocument move = bookPageBll.FindById(moveId);

            IQueryable<BsonDocument> moveSubList = bookPageBll.FindSubList(moveId);

            if (moveSubList.Where(t => t.Int("pageId") == toMoveId).Count() > 0)
            {
                json.Success = false;
                json.Message = "无法对自己的子节点进行移动操作！";
                return Json(json);
            }

            var toMove = bookPageBll.FindById(toMoveId);

            IQueryable<BsonDocument> toMoveSubList = bookPageBll.FindSubList(toMoveId);

            if (type == "child")
            {
                if (toMoveSubList.Where(t => t.Int("nodeLevel") == toMove.Int("nodeLevel") + 1 && t.Text("name") == move.Text("name")).Count() > 0)
                {
                    json.Success = false;
                    json.Message = "已存在同名页面,不能移动";
                    return Json(json);
                }
            }
            else
            {
                var parent = bookPageBll.FindById(toMove.Int("nodePid"));

                IQueryable<BsonDocument> parentSubList = bookPageBll.FindSubList(toMove.Int("nodePid"));

                if (parentSubList.Where(t => t.Int("nodeLevel") == parent.Int("nodeLevel") + 1 && t.Text("name") == move.Text("name") && t.Int("pageId") != moveId).Count() > 0)
                {
                    json.Success = false;
                    json.Message = "已存在同名页面,不能移动";
                    return Json(json);
                }
            }

            result = bookPageBll.MoveBookPage(moveId, toMoveId, type);

            json.Success = result.Status == Status.Successful;
            json.Message = result.Message;

            return this.Json(json);
        }


        /// <summary>
        /// 复制目录
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult CopyBookPage()
        {
            int bookId = PageReq.GetFormInt("bookId");
            int orginalId = PageReq.GetFormInt("moveid");
            int toCopyId = PageReq.GetFormInt("tomoveid");
            string type = PageReq.GetForm("type");

            PageJson json = new PageJson();
            InvokeResult result = new InvokeResult();
            result = BookPageBll._().CopyBookPage(orginalId, toCopyId, type, this.CurrentUserId, bookId);

            json.Success = result.Status == Status.Successful ? true : false;
            json.Message = result.Message;

            return this.Json(json);
        }
        #endregion

        #region 任务书结构树



        /// <summary>
        /// 获取搜索后结果的人员列表
        /// </summary>
        /// <returns></returns>
        public JsonResult SearchResult()
        {
            int id = PageReq.GetParamInt("id");
            var keyWord = PageReq.GetParam("keyWord");
            var PageBll = BookPageBll._();
            IQueryable<BsonDocument> bookPageList = BookPageBll._().FindByLevelScope(5, id);
            // var SearchAvaiableList = PageBll.SearchFromIndex(keyWord, this.CurrentUserId);
            var query = bookPageList.Where(c => c.Text("name").Contains(keyWord));
            List<Item> ItemList = new List<Item>();
            foreach (var page in query)
            {
                Item item = new Item();
                item.name = page.Text("name");
                item.id = page.Int("pageId");
                item.value = page.Int("nodePid").ToString();
                ItemList.Add(item);
            }
            return Json(ItemList);
        }

        /// <summary>
        /// 类型树数据获取
        /// </summary>
        /// <returns></returns>
        public ActionResult StartBookPageXml()
        {
            int id = PageReq.GetParamInt("id");
            int categoryId = PageReq.GetParamInt("categoryId");
            var query1 = Query.EQ("categoryId", categoryId);
            var query2 = Query.EQ("nodePid", 0);

            List<BsonDocument> allNodeList = new List<BsonDocument>();
            var query = Query.Or(query1, query2);

            if (query != null)
            {
                allNodeList = dataOp.FindAll("BookPage").ToList();
            }
            else
            {
                allNodeList = dataOp.FindAllByQuery("BookPage", query).ToList();
            }

            List<TreeNode> treeList = TreeHelper.GetSingleTreeList(allNodeList);

            return new XmlTree(treeList);
        }

        /// <summary>
        /// 类型树数据获取
        /// </summary>
        /// <returns></returns>
        public ActionResult AsynBookPageXml()
        {
            var idStr = PageReq.GetParam("id");
            return GetSingleSubTreeXML("BookPage", idStr, 0);
        }
        #endregion

        #region 编辑控件图片上传
        ///// <summary>
        ///// 编辑控件图片上传 
        ///// </summary>
        ///// <returns></returns>
        //public string SaveImage()
        //{
        //    // 初始化一大堆变量
        //    int Pageid = PageReq.GetParamInt("Pageid");
        //    string inputname = "filedata";//表单文件域name
        //    string attachdir = "/UploadFiles/MissionStatement";     // 上传文件保存路径，结尾不要带/
        //    int dirtype = 1;                 // 1:按天存入目录 2:按月存入目录 3:按扩展名存目录  建议使用按天存
        //    int maxattachsize = 2097152;     // 最大上传大小，默认是2M
        //    string upext = "txt,rar,zip,jpg,jpeg,gif,png,swf,wmv,avi,wma,mp3,mid";    // 上传扩展名
        //    int msgtype = 2;                 //返回上传参数的格式：1，只返回url，2，返回参数数组
        //    string immediate = Request.QueryString["immediate"];//立即上传模式，仅为演示用
        //    byte[] file;                     // 统一转换为byte数组处理
        //    string localname = "";
        //    string disposition = Request.ServerVariables["HTTP_CONTENT_DISPOSITION"];

        //    string err = "";
        //    string msg = "''";

        //    if (disposition != null)
        //    {
        //        // HTML5上传
        //        file = Request.BinaryRead(Request.TotalBytes);
        //        localname = Regex.Match(disposition, "filename=\"(.+?)\"").Groups[1].Value;// 读取原始文件名
        //    }
        //    else
        //    {
        //        HttpFileCollectionBase filecollection = Request.Files;
        //        HttpPostedFileBase postedfile = filecollection.Get(inputname);

        //        // 读取原始文件名
        //        localname = postedfile.FileName;
        //        // 初始化byte长度.
        //        file = new Byte[postedfile.ContentLength];

        //        // 转换为byte类型
        //        System.IO.Stream stream = postedfile.InputStream;
        //        stream.Read(file, 0, postedfile.ContentLength);
        //        stream.Close();

        //        filecollection = null;
        //    }

        //    if (file.Length == 0) err = "无数据提交";
        //    else
        //    {
        //        if (file.Length > maxattachsize) err = "文件大小超过" + maxattachsize + "字节";
        //        else
        //        {
        //            string attach_dir, attach_subdir, filename, extension, target;

        //            // 取上载文件后缀名
        //            extension = GetFileExt(localname);

        //            if (("," + upext + ",").IndexOf("," + extension + ",") < 0) err = "上传文件扩展名必需为：" + upext;
        //            else
        //            {
        //                switch (dirtype)
        //                {
        //                    case 2:
        //                        attach_subdir = "month_" + DateTime.Now.ToString("yyMM");
        //                        break;
        //                    case 3:
        //                        attach_subdir = "ext_" + extension;
        //                        break;
        //                    default:
        //                        attach_subdir = "day_" + DateTime.Now.ToString("yyMMdd");
        //                        break;
        //                }
        //                attach_dir = attachdir + "/" + attach_subdir + "/";

        //                // 生成随机文件名
        //                Random random = new Random(DateTime.Now.Millisecond);
        //                filename = DateTime.Now.ToString("yyyyMMddhhmmss") + random.Next(10000) + "." + extension;

        //                target = attach_dir + filename;
        //                try
        //                {
        //                    CreateFolder(Server.MapPath(attach_dir));

        //                    System.IO.FileStream fs = new System.IO.FileStream(Server.MapPath(target), System.IO.FileMode.Create, System.IO.FileAccess.Write);
        //                    fs.Write(file, 0, file.Length);
        //                    fs.Flush();
        //                    fs.Close();
        //                    FileLibrary attach = new FileLibrary()
        //                    {
        //                        sysObjId = 3,
        //                        name = filename,
        //                        ext = extension,
        //                        size = StringExtension.ToByteString(file.Length),
        //                        localPath = localname,
        //                        createData = DateTime.Now,
        //                        updateData = DateTime.Now,
        //                        createUserId = this.CurrentUserId,
        //                        updateUserId = this.CurrentUserId,
        //                        webFilePath = target,
        //                        tags = "",
        //                        isTrigger = 1,
        //                        lastVersion = 1
        //                    };
        //                    FileLibraryBll._().Insert(attach);
        //                    PageFile pageFile = new PageFile()
        //                    {
        //                        pageId = Pageid,
        //                        fileId = attach.fileId,
        //                        fileType = 0
        //                    };
        //                    PageFileBll._().Insert(pageFile);
        //                }
        //                catch (Exception ex)
        //                {
        //                    err = ex.Message.ToString();
        //                }

        //                // 立即模式判断
        //                if (immediate == "1") target = "!" + target;
        //                target = jsonString(target);
        //                if (msgtype == 1) msg = "'" + target + "'";
        //                else msg = "{'url':'" + target + "','localname':'" + jsonString(localname) + "','id':'1'}";
        //            }
        //        }
        //    }

        //    file = null;
        //    var str = "{'err':'" + jsonString(err) + "','msg':" + msg + "}";
        //    return str;
        //}

        ///// <summary>
        ///// 编辑控件图片上传 
        ///// </summary>
        ///// <returns></returns>
        //public string SaveImageNew()
        //{
        //    // 初始化一大堆变量
        //    int Pageid = PageReq.GetParamInt("Pageid");
        //    string inputname = "filedata";//表单文件域name
        //    string attachdir = "/UploadFiles/Comment";     // 上传文件保存路径，结尾不要带/
        //    int dirtype = 1;                 // 1:按天存入目录 2:按月存入目录 3:按扩展名存目录  建议使用按天存
        //    int maxattachsize = 2097152;     // 最大上传大小，默认是2M
        //    string upext = "txt,rar,zip,jpg,jpeg,gif,png,swf,wmv,avi,wma,mp3,mid";    // 上传扩展名
        //    int msgtype = 2;                 //返回上传参数的格式：1，只返回url，2，返回参数数组
        //    string immediate = Request.QueryString["immediate"];//立即上传模式，仅为演示用
        //    byte[] file;                     // 统一转换为byte数组处理
        //    string localname = "";
        //    string disposition = Request.ServerVariables["HTTP_CONTENT_DISPOSITION"];

        //    string err = "";
        //    string msg = "''";

        //    if (disposition != null)
        //    {
        //        // HTML5上传
        //        file = Request.BinaryRead(Request.TotalBytes);
        //        localname = Regex.Match(disposition, "filename=\"(.+?)\"").Groups[1].Value;// 读取原始文件名
        //    }
        //    else
        //    {
        //        HttpFileCollectionBase filecollection = Request.Files;
        //        HttpPostedFileBase postedfile = filecollection.Get(inputname);

        //        // 读取原始文件名
        //        localname = postedfile.FileName;
        //        // 初始化byte长度.
        //        file = new Byte[postedfile.ContentLength];

        //        // 转换为byte类型
        //        System.IO.Stream stream = postedfile.InputStream;
        //        stream.Read(file, 0, postedfile.ContentLength);
        //        stream.Close();

        //        filecollection = null;
        //    }

        //    if (file.Length == 0) err = "无数据提交";
        //    else
        //    {
        //        if (file.Length > maxattachsize) err = "文件大小超过" + maxattachsize + "字节";
        //        else
        //        {
        //            string attach_dir, attach_subdir, filename, extension, target;

        //            // 取上载文件后缀名
        //            extension = GetFileExt(localname);

        //            if (("," + upext + ",").IndexOf("," + extension + ",") < 0) err = "上传文件扩展名必需为：" + upext;
        //            else
        //            {
        //                switch (dirtype)
        //                {
        //                    case 2:
        //                        attach_subdir = "month_" + DateTime.Now.ToString("yyMM");
        //                        break;
        //                    case 3:
        //                        attach_subdir = "ext_" + extension;
        //                        break;
        //                    default:
        //                        attach_subdir = "day_" + DateTime.Now.ToString("yyMMdd");
        //                        break;
        //                }
        //                attach_dir = attachdir + "/" + attach_subdir + "/";

        //                // 生成随机文件名
        //                Random random = new Random(DateTime.Now.Millisecond);
        //                filename = DateTime.Now.ToString("yyyyMMddhhmmss") + random.Next(10000) + "." + extension;

        //                target = attach_dir + filename;
        //                try
        //                {
        //                    CreateFolder(Server.MapPath(attach_dir));

        //                    System.IO.FileStream fs = new System.IO.FileStream(Server.MapPath(target), System.IO.FileMode.Create, System.IO.FileAccess.Write);
        //                    fs.Write(file, 0, file.Length);
        //                    fs.Flush();
        //                    fs.Close();
        //                    FileLibrary attach = new FileLibrary()
        //                    {
        //                        sysObjId = 52,
        //                        name = filename,
        //                        ext = extension,
        //                        size = StringExtension.ToByteString(file.Length),
        //                        localPath = localname,
        //                        createData = DateTime.Now,
        //                        updateData = DateTime.Now,
        //                        createUserId = this.CurrentUserId,
        //                        updateUserId = this.CurrentUserId,
        //                        webFilePath = target,
        //                        tags = "",
        //                        isTrigger = 1,
        //                        lastVersion = 1
        //                    };
        //                    FileLibraryBll._().Insert(attach);
        //                    PageFile pageFile = new PageFile()
        //                    {
        //                        pageId = Pageid,
        //                        fileId = attach.fileId,
        //                        fileType = 0
        //                    };
        //                    PageFileBll._().Insert(pageFile);
        //                }
        //                catch (Exception ex)
        //                {
        //                    err = ex.Message.ToString();
        //                }

        //                // 立即模式判断
        //                if (immediate == "1") target = "!" + target;
        //                target = jsonString(target);
        //                if (msgtype == 1) msg = "'" + target + "'";
        //                else msg = "{'url':'" + target + "','localname':'" + jsonString(localname) + "','id':'1'}";
        //            }
        //        }
        //    }

        //    file = null;
        //    var str = "{'err':'" + jsonString(err) + "','msg':" + msg + "}";
        //    return str;
        //}


        string jsonString(string str)
        {
            str = str.Replace("\\", "\\\\");
            str = str.Replace("/", "\\/");
            str = str.Replace("'", "\\'");
            return str;
        }


        string GetFileExt(string FullPath)
        {
            if (FullPath != "") return FullPath.Substring(FullPath.LastIndexOf('.') + 1).ToLower();
            else return "";
        }

        void CreateFolder(string FolderPath)
        {
            if (!System.IO.Directory.Exists(FolderPath)) System.IO.Directory.CreateDirectory(FolderPath);
        }

        #endregion

        #region 侨鑫xhEditor Ajax文件上传
        /// <summary>
        /// 侨鑫xhEditor Ajax文件上传
        /// </summary>
        /// <returns></returns>
        public string SaveImageQX()
        {
            string inputname = "filedata";//表单文件域name
            string attachdir = "/UploadFiles/MissionStatement";     // 上传文件保存路径，结尾不要带/
            if(SysAppConfig.CustomerCode == Yinhe.ProcessingCenter.Common.CustomerCode.ZHHY)
                attachdir = "/UploadFiles/ZHHYMail";
            int dirtype = 1;                 // 1:按天存入目录 2:按月存入目录 3:按扩展名存目录  建议使用按天存
            int maxattachsize = 2097152;     // 最大上传大小，默认是2M
            string upext = "txt,rar,zip,jpg,jpeg,gif,png,swf,wmv,avi,wma,mp3,mid";    // 上传扩展名
            int msgtype = 2;                 //返回上传参数的格式：1，只返回url，2，返回参数数组
            string immediate = Request.QueryString["immediate"];//立即上传模式，仅为演示用
            byte[] file;                     // 统一转换为byte数组处理
            string localname = "";
            string disposition = Request.ServerVariables["HTTP_CONTENT_DISPOSITION"];

            string err = "";
            string msg = "''";

            if (disposition != null)
            {
                // HTML5上传
                file = Request.BinaryRead(Request.TotalBytes);
                localname = Regex.Match(disposition, "filename=\"(.+?)\"").Groups[1].Value;// 读取原始文件名
            }
            else
            {
                HttpFileCollectionBase filecollection = Request.Files;
                HttpPostedFileBase postedfile = filecollection.Get(inputname);

                // 读取原始文件名
                localname = postedfile.FileName;
                // 初始化byte长度.
                file = new Byte[postedfile.ContentLength];

                // 转换为byte类型
                System.IO.Stream stream = postedfile.InputStream;
                stream.Read(file, 0, postedfile.ContentLength);
                stream.Close();

                filecollection = null;
            }

            if (file.Length == 0) err = "无数据提交";
            else
            {
                if (file.Length > maxattachsize) err = "文件大小超过" + maxattachsize + "字节";
                else
                {
                    string attach_dir, attach_subdir, filename, extension, target;

                    // 取上载文件后缀名
                    extension = GetFileExt(localname);

                    if (("," + upext + ",").IndexOf("," + extension + ",") < 0) err = "上传文件扩展名必需为：" + upext;
                    else
                    {
                        switch (dirtype)
                        {
                            case 2:
                                attach_subdir = "month_" + DateTime.Now.ToString("yyMM");
                                break;
                            case 3:
                                attach_subdir = "ext_" + extension;
                                break;
                            default:
                                attach_subdir = "day_" + DateTime.Now.ToString("yyMMdd");
                                break;
                        }
                        attach_dir = attachdir + "/" + attach_subdir + "/";

                        // 生成随机文件名
                        Random random = new Random(DateTime.Now.Millisecond);
                        filename = DateTime.Now.ToString("yyyyMMddhhmmss") + random.Next(10000) + "." + extension;

                        target = attach_dir + filename;
                        try
                        {
                            CreateFolder(Server.MapPath(attach_dir));

                            System.IO.FileStream fs = new System.IO.FileStream(Server.MapPath(target), System.IO.FileMode.Create, System.IO.FileAccess.Write);
                            fs.Write(file, 0, file.Length);
                            fs.Flush();
                            fs.Close();
                        }
                        catch (Exception ex)
                        {
                            err = ex.Message.ToString();
                        }

                        // 立即模式判断
                        if (immediate == "1") target = "!" + target;
                        target = jsonString(target);
                        if (msgtype == 1) msg = "'" + target + "'";
                        else msg = "{'url':'" + target + "','localname':'" + jsonString(localname) + "','id':'1'}";
                    }
                }
            }

            file = null;
            var str = "{'err':'" + jsonString(err) + "','msg':" + msg + "}";
            return str;
        }
        #endregion

        #endregion

       

        #region 任务书展示页面



        #endregion

        #region 成果关联展示


        /// <summary>
        /// 批量删除版本
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult DeleteProjRetRef()
        {
            var Ids = PageReq.GetParamIntList("Ids");
            InvokeResult result = new InvokeResult() { Status = Status.Successful };
            List<int> IdList = new List<int>();
            foreach (var id in Ids)
            {
                if (!IdList.Contains(id))
                {
                    IdList.Add(id);
                }
            }
            if (IdList.Count() > 0)
            {
                PageRefResultBll bodyVersion = PageRefResultBll._();
                result = bodyVersion.Delete(IdList);

            }
            return Json(ConvertToPageJson(result));

        }

        #endregion

        #region 版本管理展示页面


        /// <summary>
        /// 批量删除版本
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult DeleteBodyVersion()
        {
            var Ids = PageReq.GetParamList("Ids");
            InvokeResult result = new InvokeResult() { Status = Status.Successful };
            List<string> IdList = new List<string>();
            foreach (var id in Ids)
            {
                if (!IdList.Contains(id))
                {
                    IdList.Add(id);
                }
            }
            if (IdList.Count() > 0)
            {
                PageBodyVersionBll bodyVersion = PageBodyVersionBll._();
                result = bodyVersion.Delete(IdList);

            }
            return Json(ConvertToPageJson(result));

        }

        /// <summary>
        /// 恢复到某一版本
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult RecoveryBodyVersion()
        {
            var Id = PageReq.GetParamInt("Id");
            InvokeResult result = new InvokeResult() { Status = Status.Successful };
            PageBodyVersionBll bodyVersionBll = PageBodyVersionBll._();
            result = bodyVersionBll.RecoveryBodyVersion(Id, this.CurrentUserId);
            return Json(ConvertToPageJson(result));
        }



        [NonAction]
        public void DownloadFile(string path, string name)
        {
            System.IO.FileInfo file = new System.IO.FileInfo(path);
            name = HttpUtility.UrlEncode(name, System.Text.Encoding.UTF8).Replace("+", "%20");
            Response.Clear();
            Response.ClearHeaders();
            Response.Buffer = false;
            Response.AppendHeader("Content-Disposition", "attachment;filename=" + name);
            Response.AppendHeader("Content-Length", file.Length.ToString());
            Response.ContentType = "application/octet-stream";
            Response.WriteFile(path);
            Response.Flush();
            Response.End();
        }

       

        #endregion

        #region 前端交互接口

        #endregion

        #region 签入签出管理
        public partial class FileCheckManage
        {
            #region 变量
            public const string Task_File_Identify = "bookTask";
            public const string Task_Table_Name = "BookTask";
            public const string Page_File_Identify = "bookPage";
            public const string Page_Table_Name = "BookPage";
            #endregion

            /// <summary>
            /// 文件签入签出状态
            /// </summary>
            public enum FileCheckStatus
            {
                /// <summary>
                /// 未被签出 0
                /// </summary>
                CheckIn,
                /// <summary>
                /// 被签出 1
                /// </summary>
                CheckOut
            }
            /// <summary>
            /// 签入签出记录动作类型
            /// </summary>
            public enum ActionType
            {
                /// <summary>
                /// 正常签入 0
                /// </summary>
                CheckIn,
                /// <summary>
                /// 正常签出 1
                /// </summary>
                CheckOut,
                /// <summary>
                /// 超时签入 2
                /// </summary>
                TimeOutCheckIn,
                /// <summary>
                /// 手动强制签入 3
                /// </summary>
                ForceCheckIn
            }
        }
        #endregion

        /// <summary>
        /// 添加页面
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult AddBookPage()
        {
            string pageNames = PageReq.GetForm("names");
            int pid = PageReq.GetFormInt("Cid");
            int bookId = PageReq.GetFormInt("bookId");
            var bookTask = BookTaskBll._().FindById(bookId);
            int stageId = PageReq.GetFormInt("stageId");
            int profId = PageReq.GetFormInt("profId");
            int categoryId = PageReq.GetFormInt("categoryId");
            string patternIds = PageReq.GetForm("patternIds");
            string remark = PageReq.GetForm("remark");
            PageJson json = new PageJson();
            if (pageNames == "")
            {
                json.Success = false;
                json.Message = "请填写页面名称!";
                return this.Json(json);
            }

            string[] arrName = pageNames.Split('\n');
            List<BsonDocument> bookPageList = new List<BsonDocument>();
            DateTime now = DateTime.Now;
            foreach (string name in arrName)
            {
                if (name.Trim() == "") { continue; }
                if (bookPageList.Where(m => m.Text("name") == name).Count() > 0)
                {
                    json.Success = false;
                    json.Message = "提交内容中有重复部分";
                    return this.Json(json);
                }
                var bookPage = new BsonDocument();
                bookPage.Add("name", name);
                bookPage.Add("nodePid", pid);
                bookPage.Add("remark", remark);
                bookPage.Add("categoryId", categoryId);
                bookPage.Add("IsPageTempate", bookId == 0 ? 1 : 0);

                if (stageId != -1 && stageId != 0)
                {
                    bookPage.Add("sysStageId", stageId);
                }
                if (profId != -1 && profId != 0)
                    bookPage.Add("sysProfId", profId);

                bookPageList.Add(bookPage);
            }
            string[] strPatternIds = patternIds.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            List<int> arrPatternIds = new List<int>();
            foreach (var strId in strPatternIds)
                arrPatternIds.Add(Int32.Parse(strId));
            var result = BookPageBll._().Create(bookPageList, pid, bookId, arrPatternIds);

            json.Success = result.Status == Status.Successful;
            var IdsStr = string.Empty;
            if (json.Success == true && result.Value != null)
            {
                foreach (var _BP in result.Value)
                {
                    IdsStr += _BP.Int("pageId").ToString();
                }
                json.Message = IdsStr;
            }
            else
            {
                json.Message = result.Message;
            }
            return this.Json(json);
        }
        /// <summary>
        /// 页面文件类型
        /// </summary>
        public enum PageFileType
        {
            /// <summary>
            /// 媒体文件 0
            /// </summary>
            Media,
            /// <summary>
            /// 附件 1
            /// </summary>
            Attachment
        }
    }
}
