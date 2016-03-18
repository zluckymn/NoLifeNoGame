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
    /// 页面内容
    /// </summary>
    public class PageFileBll  
    {
        #region 构造函数
        /// <summary>
        /// 类私有变量
        /// </summary>
        private DataOperation _ctx = null;

        /// <summary>
        /// 封闭当前默认构造函数
        /// </summary>
        public  PageFileBll()
        {
            _ctx = new DataOperation();
        }

        private PageFileBll(DataOperation ctx)
        {
            _ctx = ctx;
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static PageFileBll _()
        {
            return new PageFileBll();
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static PageFileBll _(DataOperation ctx)
        {
            return new PageFileBll(ctx);
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
           // return this._ctx.PageFiles.Where(m => m.pageFileId == id).FirstOrDefault();
            var hitObj = this._ctx.FindOneByKeyVal("PageFile", "pageFileId", id.ToString());
            return hitObj;
        }

        #region 用于全文搜索
        /// <summary>
        /// 重载的方法 用于与系统图档库关联
        /// </summary>
        /// <param name="fileId"></param>
        /// <returns></returns>
        public string GetBelongKey(int fileId)
        {
            //var query= this._ctx.PageFiles.Where(m => m.fileId == fileId).FirstOrDefault();
            var query = this._ctx.FindAllByKeyVal("PageFile", "fileId", fileId.ToString()).FirstOrDefault();
            if (query!=null)
            {
                return query.Int("pageId").ToString();
            }
            return string.Empty;
        }

        /// <summary>
        /// 用于全文搜索展示列表索需要的信息
        /// </summary>
        /// <param name="key"></param>
        /// <param name="fileVersion"></param>
        /// <returns></returns>
        public BsonDocument GetDetailObjectInfo(int key, int fileVersion) 
        {
            var Result = new BsonDocument();
            
            Result.BelongToObject = "任务书";
           // var query = this._ctx.PageFiles.Where(m => m.fileId == key).FirstOrDefault();
            var query = this._ctx.FindOneByKeyVal("PageFile", "fileId", key.ToString());
            if (query != null)
            {
                Result.BelongToFileTableName = "FileLibVersion";
                Result.BelongToObjectId = query.Int("pageId").ToString();
               // Result.BelongToObjectName = query.Int("BookPage")!=null?query.BookPage.name:"";
                Result.BelongToObjectName = query.Int("BookPage") != null ? query.SourceBson("pageId").Text("name") : "";
                //string filePath = CommonBll.GetSysObjDir(SysObject.name, SysObject.sysObjId, FileLibrary.createData);//搜索页面可以搜出
                //ImgPath = filePath+CommonBll.GetSysObjDocImg(FileLibrary.fileId, hash, docVer.fileVersion, "m");
                //var book=query.BookPage.BookTaskPages.FirstOrDefault();
                var book = query.SourceBson("pageId").SourceBson("taskPageId").FirstOrDefault();
               Result.LinkObj = string.Format("/MissionStatement/Home/BookPage/{0}", book != null && book.bookId .HasValue? book.bookId: 0);
               
               
            }
            return Result;
        }
        #endregion
        /// <summary>
        /// 查找所有
        /// </summary>
        /// <returns></returns>
        public IQueryable<BsonDocument> FindAll()
        {
            //return this._ctx.PageFiles;
            return this._ctx.FindAll("PageFile").AsQueryable();
        }
        /// <summary>
        ///  返回page的附件列表
        /// </summary>
        /// <param name="bookPageId"></param>
        /// <returns></returns>
        public List<BsonDocument> FindByPageId(int bookPageId, PageFileType type) 
        {
            //var q = this._ctx.PageFiles.Where(r=>r.pageId == bookPageId && r.fileType == (int)type);
            var a = string.Format("pageId[0]&fileTyep[1]", bookPageId, type);
            var q = this._ctx.FindAllByQueryStr("PageFile", a);

            var files = new List<BsonDocument>();
            foreach (var file in q) 
            {
                //var f = this._ctx.FileLibraries.Where(r => r.fileId == file.fileId).FirstOrDefault();
                var f = this._ctx.FindOneByKeyVal("FileLibrary", "fileId", bookPageId.ToString());
                if (f != null) { files.Add(f); }
            }
            return files.OrderByDescending(r => r.Date("updateData")).ToList();
        }

        #endregion

        #region 操作

        /// <summary>
        /// 批量上传
        /// </summary>
        /// <param name="supplierId"></param>
        /// <param name="filePath"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public InvokeResult<string> BatchUpload(int pageId, List<string> filePath, int userId)
        {
            InvokeResult<string> result = new InvokeResult<string>();

            try
            {
                List<FileParam> fileParams = new List<FileParam>();

                using (TransactionScope trans = new TransactionScope())
                {

                    foreach (var file in filePath)
                    {
                        #region 附件及版本
                        FileLibrary fileLib = new FileLibrary
                        {
                            createData = DateTime.Now,
                            createUserId = userId,
                            name = System.IO.Path.GetFileNameWithoutExtension(file),
                            ext = System.IO.Path.GetExtension(file),
                            sysObjId = 3,//已定义好的系统对象
                            localPath = file,
                            tags = string.Empty,
                            lastVersion = 1
                        };


                        _ctx.FileLibraries.InsertOnSubmit(fileLib);
                        _ctx.SubmitChanges();
                        FileLibVersion version = new FileLibVersion()
                        {
                            fileId = fileLib.fileId,
                            fileName = fileLib.name,
                            localPath = fileLib.localPath,
                            createData = DateTime.Now,
                            createUserId = fileLib.createUserId,
                            fileVersion = 1,
                            ext = fileLib.ext
                        };
                        this._ctx.FileLibVersions.InsertOnSubmit(version);
                        this._ctx.SubmitChanges();

                        #endregion

                        //添加关联
                        PageFile pageFile = new PageFile()
                        {
                            pageId = pageId,
                            fileId = fileLib.fileId,
                            fileType = 1
                        };

                        this._ctx.PageFiles.InsertOnSubmit(pageFile);
                        this._ctx.SubmitChanges();

                        //生成参数
                        FileParam fp = new FileParam();
                        fp.docId = fileLib.fileId;
                        fp.lastVersion = version.fileVersion;
                        fp.path = fileLib.localPath;
                        fp.ext = fileLib.ext;
                        fp.strParam = "sysObject@" + fileLib.fileId + "-" + version.verId;

                        fileParams.Add(fp);
                    }

                    trans.Complete();

                    System.Web.Script.Serialization.JavaScriptSerializer script = new System.Web.Script.Serialization.JavaScriptSerializer();
                    string strJson = script.Serialize(fileParams);
                    result.Value = strJson;
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
        /// 批量复制分项资料库图文档，作为任务书附件（没有历史版本）
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="pageId"></param>
        /// <param name="arrId"></param>
        /// <returns></returns>
        public InvokeResult<List<int>> BatchCopyProjectDoc(int userId, int pageId, int[] arrId,List<string> thumbsImages)
        {
            InvokeResult<List<int>> res = new InvokeResult<List<int>>();
            List<int> retIds = new List<int>();

            try
            {
                using (TransactionScope trans = new TransactionScope())
                {
                    StringBuilder thumbStr = new StringBuilder();

                    //thumbStr.Append("<root>");

                    foreach (int fileId in arrId)
                    {
                        var oldFile = this._ctx.FileLibraries.FirstOrDefault(r => r.fileId == fileId);
                        if (oldFile != null)
                        {
                            #region 复制文件信息
                            FileLibrary fileLib = new FileLibrary
                            {
                                createData = DateTime.Now,
                                createUserId = userId,
                                name = oldFile.name,
                                ext = oldFile.ext,
                                sysObjId = 3,//已定义好的系统对象
                                localPath = oldFile.localPath,
                                tags = string.Empty,
                                lastVersion = 1,
                                size = oldFile.size,
                                hash = oldFile.hash

                            };

                            _ctx.FileLibraries.InsertOnSubmit(fileLib);
                            _ctx.SubmitChanges();
                            #endregion

                            #region 复制版本信息
                            FileLibVersion version = new FileLibVersion()
                            {
                                fileId = fileLib.fileId,
                                fileName = fileLib.name,
                                localPath = fileLib.localPath,
                                createData = DateTime.Now,
                                createUserId = fileLib.createUserId,
                                fileVersion = 1,
                                ext = fileLib.ext,
                                hash = fileLib.hash

                            };
                            this._ctx.FileLibVersions.InsertOnSubmit(version);
                            this._ctx.SubmitChanges();
                            #endregion

                            #region 添加关联信息
                            PageFile pageFile = new PageFile()
                            {
                                pageId = pageId,
                                fileId = fileLib.fileId,
                                fileType = 1,
                                bizFileLibId = oldFile.fileId
                            };

                            this._ctx.PageFiles.InsertOnSubmit(pageFile);
                            this._ctx.SubmitChanges();
                            #endregion
                            #region 拷贝文件
                            string appPath = AppDomain.CurrentDomain.BaseDirectory;
                            string filePath = CommonBll.GetSysObjDir(oldFile.sysObjId.Value.ToString(), oldFile.sysObjId.Value, oldFile.createData);
                            string oldFileName = CommonBll.GetSysObjDocImg(oldFile.fileId, oldFile.hash, oldFile.lastVersion, "m");

                            string descPath = CommonBll.GetSysObjDir(fileLib.sysObjId.Value.ToString(), fileLib.sysObjId.Value, fileLib.createData);
                            string descPhyPath = FileExtension.CreateFolder(string.Format("{0}{1}", appPath, descPath));
                            string descFileName =CommonBll.GetSysObjDocImg(fileLib.fileId, fileLib.hash, fileLib.lastVersion, "m");
                            foreach (var s in thumbsImages)
                            {
                                string sourcePhyFileName = string.Format("{0}{1}{2}", appPath, filePath, oldFileName.Replace("_m.", "_" + s + "."));
                                string descPhyFileName = string.Format("{0}{1}", descPhyPath, descFileName.Replace("_m.", "_" + s + "."));
                                if (File.Exists(sourcePhyFileName) == true)
                                {
                                    File.Copy(sourcePhyFileName, descPhyFileName);
                                }
                            }
                            #endregion
                            retIds.Add(fileLib.fileId);

                            //thumbStr.AppendFormat("<file hash=\"{0}\" size=\"{1}\" param=\"sysObject@{2}-{3}\"  />", fileLib.hash, fileLib.size, fileLib.fileId, version.verId);
                        }
                    }
                    //thumbStr.Append("</root>");

                    trans.Complete();
                    res.Status = Status.Successful;
                    res.Message = thumbStr.ToString();
                    res.Value = retIds;
                }
            }
            catch (Exception ex)
            {
                res.Message = ex.Message;
                res.Status = Status.Failed;
            }

            return res;
        }

        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public InvokeResult<PageFile> Update(PageFile entity)
        {
            InvokeResult<PageFile> result = new InvokeResult<PageFile>();
            try
            {
                this._ctx.SubmitChanges();
                result.Status = Status.Successful;
            }
            catch(Exception ex)
            {
                result.Status = Status.Failed;
                result.Message = ex.Message;
            }

            result.Value = entity;

            return result;
        }

        /// <summary>
        /// 新增
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public InvokeResult<PageFile> Insert(PageFile entity)
        {
            if (entity == null) throw new ArgumentNullException();
            InvokeResult<PageFile> result = new InvokeResult<PageFile>();
            try
            {
                this._ctx.PageFiles.InsertOnSubmit(entity);

                this._ctx.SubmitChanges();

                return new InvokeResult<PageFile> { Status = Status.Successful, Value = entity };
            }
            catch(Exception ex)
            {
                result.Status = Status.Failed;
                result.Message = ex.Message;
            }
            result.Value = entity;
            return result;
        }

        /// <summary>
        /// 根据系统文件Id删除
        /// </summary>
        /// <param name="fileId"></param>
        /// <returns></returns>
        public InvokeResult DeleteByFileId(int bookPageId, int fileId)
        {
            InvokeResult result = new InvokeResult() { Status = Status.Successful };
            try
            {
                #region 判断当前附件是否为模板附件,如果是模板附件,则只删除关联记录,不删除文件本身
                bool isTempate = false; //标记 , 是否为模板

                BookPage pageEntity = this._ctx.BookPages.Where(t => t.pageId == bookPageId).FirstOrDefault();  //获取页面实体

                if (pageEntity.IsPageTempate == 1)      //如果是页面模板则标记为模板
                {
                    isTempate = true;
                }
                else                                    //如果不是页面模板,则判断其所属任务书是否为模板
                {
                    BookTaskPage pageRel = pageEntity.BookTaskPages.FirstOrDefault();   //获取页面与任务书的关联

                    if (pageRel != null)
                    {
                        BookTask bookEntity = pageRel.BookTask;         //通过关联获取任务书实体

                        if (bookEntity.type == 1)               //如果任务书是模板,则标记为模板
                        {
                            isTempate = true;
                        }
                    }
                }
                #endregion

                var entity = this._ctx.PageFiles.Where(r => r.pageId == bookPageId && r.fileId == fileId).FirstOrDefault(); //要删除的文档关联信息
                if (entity == null) throw new Exception("参数错误");    //文档关联为空,抛出异常

                string physicPath = System.Web.HttpContext.Current.Server.MapPath(entity.FileLibrary.webFilePath);  //文档的物理路径

                FileLibrary physicFile = entity.FileLibrary;    //文档的实际记录

                int usingCount = this._ctx.PageFiles.Where(t => t.fileId == fileId).Count();    //该文档被其他任务书使用的关联数,为1则只被当前任务书引用

                #region 删除数据库记录
                using (TransactionScope tran = new TransactionScope())
                {
                    this._ctx.PageFiles.DeleteOnSubmit(entity);//删关联

                    if (isTempate == false &&　usingCount == 1)     //如果不是模板的附件且只被当前任务书引用,则把文件记录一起删除
                    {
                        this._ctx.FileLibVersions.DeleteAllOnSubmit(entity.FileLibrary.FileLibVersions);//删版本

                        this._ctx.FileLibraries.DeleteOnSubmit(physicFile);//删文件记录
                    }

                    this._ctx.SubmitChanges();
                    tran.Complete();
                }
                #endregion

                #region 删除文档物理文件
                if (isTempate == false && usingCount == 1)     //如果不是模板附件且没有被其他任务书引用,则删除文件
                {
                    //删文件
                    if (System.IO.File.Exists(physicPath))
                    {
                        System.IO.File.Delete(physicPath);
                    }
                }
                #endregion
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
