using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System.IO;
using System.Web.Script.Serialization;
using Yinhe.ProcessingCenter.DataRule;

namespace Yinhe.ProcessingCenter.Document
{
    /// <summary>
    /// 文件处理类
    /// </summary>
    public class FileOperationHelper
    {
        private DataOperation _dataOp = null;
        public FileOperationHelper()
        {
            if (_dataOp == null)
            {
                _dataOp = new DataOperation();
            }
        }

        #region 查询
        /// <summary>
        /// 获得目录下的文件列表
        /// </summary>
        /// <param name="structId"></param>
        /// <returns></returns>
        public List<BsonDocument> FindFileListByStructId(int structId)
        {
            List<BsonDocument> docList = new List<BsonDocument>();
            var fileList = _dataOp.FindAllByKeyVal("FileLibrary", "structId", structId.ToString());
            docList.AddRange(fileList);

            var subStructList = _dataOp.FindAllByKeyVal("FileStructure", "nodePid", structId.ToString());
            foreach (var item in subStructList)
            {
                int subId = item.Int("structId");
                docList.AddRange(FindFileListByStructId(subId));
            }
            return docList;
        }
        #endregion

        #region 文件操作

        #region  新增

        /// <summary>
        /// 上传单文件
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public InvokeResult<FileUploadSaveResult> UploadSingleFile(FileUploadObject obj)
        {
            InvokeResult<FileUploadSaveResult> ret = new InvokeResult<FileUploadSaveResult>();
            InvokeResult result = new InvokeResult();
            FileUploadSaveResult state = new FileUploadSaveResult();
            state.param = "sysObject";
            state.localPath = obj.localPath;
            try
            {
                #region 文档表数据
                BsonDocument fileLib = new BsonDocument();
                string fileName = Path.GetFileName(obj.localPath);
                if (obj.fileTypeId != 0)
                {
                    fileLib.Add("fileTypeId", obj.fileTypeId.ToString());
                }
                if (obj.fileObjId != 0)
                {
                    fileLib.Add("fileObjId", obj.fileObjId.ToString());
                }

                fileLib.Add("name", Path.GetFileNameWithoutExtension(fileName));
                fileLib.Add("ext", Path.GetExtension(fileName));
                fileLib.Add("localPath", obj.localPath);
                fileLib.Add("version", "1");
                if (string.IsNullOrEmpty(obj.subMapParam) == false)
                {
                    fileLib.Add("subMapParam", obj.subMapParam);
                }
                if (string.IsNullOrEmpty(obj.guid2d) == false)
                {
                    fileLib.Add("guid2d", obj.guid2d);
                }
                string thumbPicPath = GetImgPath(Path.GetExtension(fileName));
                if (!string.IsNullOrEmpty(thumbPicPath))
                {
                    fileLib.Add("thumbPicPath", thumbPicPath);
                }
                if (obj.propvalueDic != null)
                {
                    foreach (var dic in obj.propvalueDic)
                    {
                        fileLib.Add(dic.Key, dic.Value);
                    }
                }
                result = _dataOp.Insert("FileLibrary", fileLib);
                if (result.Status == Status.Successful)
                {
                    state.fileId = result.BsonInfo.Int("fileId");
                }
                else
                {
                    throw new Exception(result.Message);
                }
                #endregion

                #region 文档版本表数据
                BsonDocument fileLibVer = new BsonDocument();
                fileLibVer.Add("fileId", result.BsonInfo.Text("fileId"));
                fileLibVer.Add("name", result.BsonInfo.Text("name"));
                fileLibVer.Add("ext", result.BsonInfo.Text("ext"));
                fileLibVer.Add("localPath", result.BsonInfo.Text("localPath"));
                fileLibVer.Add("version", result.BsonInfo.Text("version"));
                if (string.IsNullOrEmpty(obj.subMapParam) == false)
                {
                    fileLibVer.Add("subMapParam", obj.subMapParam);
                }
                if (string.IsNullOrEmpty(obj.guid2d) == false)
                {
                    fileLibVer.Add("guid2d", obj.guid2d);
                }
                #region 版本属性
                if (result.BsonInfo.Int("version") == 1)
                {
                    if (obj.propvalueDic != null)
                    {
                        foreach (var dic in obj.propvalueDic)
                        {
                            fileLibVer.Add(dic.Key, dic.Value);
                        }
                    }
                }
                #endregion

                result = _dataOp.Insert("FileLibVersion", fileLibVer);
                if (result.Status == Status.Successful)
                {
                    state.fileVerId = result.BsonInfo.Int("fileVerId");
                    state.ext = Path.GetExtension(obj.localPath);
                }
                else
                {
                    throw new Exception(result.Message);
                }
                #endregion

                #region 文档关联表数据
                BsonDocument fileRle = new BsonDocument();
                fileRle.Add("fileId", result.BsonInfo.Text("fileId"));
                fileRle.Add("fileObjId", obj.fileObjId.ToString());
                fileRle.Add("version", result.BsonInfo.Text("version"));
                fileRle.Add("tableName", obj.tableName);
                fileRle.Add("keyName", obj.keyName);
                fileRle.Add("keyValue", obj.keyValue);
                fileRle.Add("uploadType", obj.uploadType.ToString());
                fileRle.Add("isPreDefine", obj.isPreDefine.ToString());
                fileRle.Add("isCover", obj.isCover.ToString());

                result = _dataOp.Insert("FileRelation", fileRle);

                if (result.Status == Status.Successful)
                {
                    state.fileRelId = result.BsonInfo.Int("fileRelId");
                    if (obj.fileRel_fileCatId != "0" && obj.fileRel_profId != "0" && obj.fileRel_stageId != "0") 
                    {
                        BsonDocument fileRle_property = new BsonDocument();
                        fileRle_property.Add("profId", obj.fileRel_profId);
                        fileRle_property.Add("stageId", obj.fileRel_stageId);
                        fileRle_property.Add("fileCatId", obj.fileRel_fileCatId);
                        fileRle_property.Add("fileRelId", state.fileRelId);
                        this._dataOp.Insert("FileRelProperty", fileRle_property);
                    }
                }
                else
                {
                    throw new Exception(result.Message);
                }
                #endregion
            }
            catch (Exception ex)
            {
                result.Status = Status.Failed;
                result.Message = ex.Message;

            }
            finally
            {
                ret.Status = result.Status;
                ret.Value = state;
                ret.Message = result.Message;
            }
            return ret;
        }

        /// <summary>
        /// 上传多文件
        /// </summary>
        /// <param name="objList"></param>
        /// <returns></returns>
        public List<InvokeResult<FileUploadSaveResult>> UploadMultipleFiles(List<FileUploadObject> objList, UploadType type)
        {
            InvokeResult result = new InvokeResult();
            List<InvokeResult<FileUploadSaveResult>> retList = new List<InvokeResult<FileUploadSaveResult>>();
            InvokeResult<FileUploadSaveResult> itemState = new InvokeResult<FileUploadSaveResult>();
            switch (type)
            {
                case UploadType.File: //文件上传
                    foreach (var item in objList)
                    {
                        itemState = UploadSingleFile(item);
                        retList.Add(itemState);
                    }
                    break;

                case UploadType.Package://文件包上传 单目录
                    retList.AddRange(UploadMultipleFilesPackage(objList, type));
                    break;

                case UploadType.Folder://文件夹上传   多目录
                    var query = objList.GroupBy(c => c.fileObjId).Select(c => c.Key).ToList();
                    foreach(var key in query)
                    {
                        var hitObjList=objList.Where(c=>c.fileObjId==key).ToList();
                      retList.AddRange(UploadMultipleFilesTree(hitObjList, type));
                    }
                    break;
            }

            return retList;
        }

        /// <summary>
        /// 上传新版本
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public InvokeResult<FileUploadSaveResult> UploadNewVersion(FileUploadVersionObject obj)
        {
            InvokeResult<FileUploadSaveResult> ret = new InvokeResult<FileUploadSaveResult>();
            InvokeResult result = new InvokeResult();
            InvokeResult gresult = new InvokeResult();
            BsonDocument fileLib = _dataOp.FindOneByKeyVal("FileLibrary", "fileId", obj.fileId.ToString());
            FileUploadSaveResult state = new FileUploadSaveResult();
            state.param = "sysObject";
            state.fileId = obj.fileId;
            state.localPath = obj.localPath;
            try
            {
                if (fileLib != null)
                {
                    BsonDocument fileLibVer = new BsonDocument();
                    int version = fileLib.Int("version") + 1;
                    string fileName = Path.GetFileName(obj.localPath);
                    fileLibVer.Add("fileId", obj.fileId.ToString());
                    fileLibVer.Add("name", Path.GetFileNameWithoutExtension(fileName));
                    fileLibVer.Add("ext", Path.GetExtension(fileName));
                    fileLibVer.Add("localPath", obj.localPath);
                    fileLibVer.Add("version", version.ToString());

                    #region 版本属性
                    if (obj.propvalueDic != null)
                    {
                        foreach (var dic in obj.propvalueDic)
                        {
                            fileLibVer.Add(dic.Key, dic.Value);
                        }
                    }

                    #endregion

                    result = _dataOp.Insert("FileLibVersion", fileLibVer);
                    state.ext = Path.GetExtension(fileName);
                    if (result.Status == Status.Successful)
                    {
                        state.fileVerId = result.BsonInfo.Int("fileVerId");

                    }

                    result = _dataOp.Update("FileLibrary", "db.FileLibrary.distinct('_id',{'fileId':'" + obj.fileId + "'})", "version=" + version.ToString() + "&localPath=" + obj.localPath + "&ext=" + Path.GetExtension(Path.GetFileName(obj.localPath)) + "&name=" + Path.GetFileNameWithoutExtension(Path.GetFileName(obj.localPath)));
                    if (result.Status == Status.Successful)
                    {
                        result = _dataOp.Update("FileRelation", "db.FileRelation.distinct('_id',{'fileId':'" + obj.fileId + "'})", "version=" + version.ToString());
                        state.fileRelId = result.BsonInfo.Int("fileRelId");
                    }

                }
                else
                {
                    result.Status = Status.Failed;
                    result.Message = "未找到filelibrary对象";
                }
            }
            catch (Exception ex)
            {
                result.Status = Status.Failed;
                result.Message = ex.Message;
            }
            finally
            {
                ret.Status = result.Status;
                ret.Value = state;
                ret.Message = result.Message;
            }
            return ret;

        }

        #region 文件夹上传
        /// <summary>
        /// 文件保存方法
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public InvokeResult<FileUploadSaveResult> SaveSingleFile(FileUploadObject obj)
        {
            InvokeResult<FileUploadSaveResult> ret = new InvokeResult<FileUploadSaveResult>();
            InvokeResult result = new InvokeResult();
            FileUploadSaveResult state = new FileUploadSaveResult();
            state.param = "sysObject";
            state.localPath = obj.localPath;
            try
            {
                #region 文档表数据
                BsonDocument fileLib = new BsonDocument();
                string fileName = Path.GetFileName(obj.localPath);
                if (obj.fileTypeId != 0)
                {
                    fileLib.Add("fileTypeId", obj.fileTypeId.ToString());
                }

                if (obj.fileObjId != 0)
                {
                    fileLib.Add("fileObjId", obj.fileObjId.ToString());
                }

                if (obj.structId != 0)
                {
                    fileLib.Add("structId", obj.structId.ToString());
                }

                fileLib.Add("name", Path.GetFileNameWithoutExtension(fileName));
                fileLib.Add("ext", Path.GetExtension(fileName));
                fileLib.Add("localPath", obj.localPath);
                fileLib.Add("version", "1");
                string thumbPicPath = GetImgPath(Path.GetExtension(fileName));
                if (!string.IsNullOrEmpty(thumbPicPath))
                {
                    fileLib.Add("thumbPicPath", thumbPicPath);
                }
                //if (string.IsNullOrEmpty(obj.fileRel_fileCatId) == false)
                //{
                //    fileLib.Add("thumbPicPath", thumbPicPath);
                //}
                if (obj.propvalueDic != null)
                {
                    foreach (var dic in obj.propvalueDic)
                    {
                        fileLib.Add(dic.Key, dic.Value);
                    }
                }
                result = _dataOp.Insert("FileLibrary", fileLib);
                if (result.Status == Status.Successful)
                {
                    state.fileId = result.BsonInfo.Int("fileId");
                }
                #endregion

                #region 文档版本表数据
                BsonDocument fileLibVer = new BsonDocument();
                fileLibVer.Add("fileId", result.BsonInfo.Text("fileId"));
                fileLibVer.Add("name", result.BsonInfo.Text("name"));
                fileLibVer.Add("ext", result.BsonInfo.Text("ext"));
                fileLibVer.Add("localPath", result.BsonInfo.Text("localPath"));
                fileLibVer.Add("version", result.BsonInfo.Text("version"));
   

                #region 版本属性
                if (result.BsonInfo.Int("version") == 1)
                {
                    if (obj.propvalueDic != null)
                    {
                        foreach (var dic in obj.propvalueDic)
                        {
                            fileLibVer.Add(dic.Key, dic.Value);
                        }
                    }
                }
                #endregion

                result = _dataOp.Insert("FileLibVersion", fileLibVer);
                if (result.Status == Status.Successful)
                {
                    state.fileVerId = result.BsonInfo.Int("fileVerId");
                    state.ext = Path.GetExtension(obj.localPath);
                }
                #endregion

                #region 文档关联表数据
                BsonDocument fileRle = new BsonDocument();
                fileRle.Add("fileId", state.fileId);
                fileRle.Add("fileObjId", obj.fileObjId.ToString());
                fileRle.Add("version", result.BsonInfo.Text("version"));
                fileRle.Add("tableName", obj.tableName);
                fileRle.Add("keyName", obj.keyName);
                fileRle.Add("keyValue", obj.keyValue);
                fileRle.Add("uploadType", obj.uploadType.ToString());
                fileRle.Add("isPreDefine", obj.isPreDefine.ToString());
                fileRle.Add("isCover", obj.isCover.ToString());

                result = _dataOp.Insert("FileRelation", fileRle);

                if (result.Status == Status.Successful)
                {
                    state.fileRelId = result.BsonInfo.Int("fileRelId");
                    if (obj.fileRel_fileCatId != "0" && obj.fileRel_profId != "0" && obj.fileRel_stageId != "0")
                    {
                        BsonDocument fileRle_property = new BsonDocument();
                        fileRle_property.Add("profId", obj.fileRel_profId);
                        fileRle_property.Add("stageId", obj.fileRel_stageId);
                        fileRle_property.Add("fileCatId", obj.fileRel_fileCatId);
                        fileRle_property.Add("fileRelId", state.fileRelId);
                        this._dataOp.Insert("FileRelProperty", fileRle_property);
                    }
                }
                else
                {
                    throw new Exception(result.Message);
                }
                #endregion

            }
            catch (Exception ex)
            {
                result.Status = Status.Failed;
                result.Message = ex.Message;

            }
            finally
            {
                ret.Status = result.Status;
                ret.Value = state;
                ret.Message = result.Message;
            }
            return ret;
        }

        #region 多文件上传方法
        /// <summary>
        /// 上传多文件
        /// </summary>
        /// <param name="objList"></param>
        /// <returns></returns>
        public List<InvokeResult<FileUploadSaveResult>> UploadMultipleFilesTree(List<FileUploadObject> objList, UploadType type)
        {
            InvokeResult result = new InvokeResult();
            List<InvokeResult<FileUploadSaveResult>> retList = new List<InvokeResult<FileUploadSaveResult>>();
            InvokeResult<FileUploadSaveResult> itemState = new InvokeResult<FileUploadSaveResult>();
            //文件夹上传
            Dictionary<string, List<FileUploadObject>> dic = new Dictionary<string, List<FileUploadObject>>();
            var dirList = objList.Select(t => t.rootDir).Distinct().ToList();//根据目录分类
            foreach (var item in dirList)
            {
                dic.Add(item, objList.Where(t => t.rootDir == item).ToList());
            }

            if (type == UploadType.Folder)
            {
                foreach (var entity in dic)
                {
                    var fileList = entity.Value.Select(t => t.localPath).ToList();
                    var tree = TreeOperation.TreeCombine(entity.Key, fileList);
                    var newList = FolderTreeInsert(tree, null);
                    if (newList != null)
                    {
                        var subResult = FolderTreeFileInsert(newList, objList, type);
                        retList.AddRange(subResult);
                    }
                }

            }

            return retList;
        }

        /// <summary>
        /// 将树形结构插入数据库 并对主键赋值  
        /// </summary>
        /// <param name="node"></param>
        public List<TreeNode> FolderTreeInsert(TreeNode node, TreeNode pnode)
        {
            List<TreeNode> nodeList = new List<TreeNode>();
            TreeNode retNode = new TreeNode();
            TreeNode subnode = new TreeNode();
            InvokeResult result = new InvokeResult();
            subnode = node;
            retNode.Name = subnode.Name;
            retNode.Level = subnode.Level;
            retNode.FileList = subnode.FileList;
            int pid = 0;

            if (pnode == null)
            {
                pid = 0;
            }
            else
            {
                pid = pnode.StructId;
            }
            BsonDocument structModel = new BsonDocument();
            structModel.Add("name", subnode.Name);
            structModel.Add("nodePid", pid.ToString());
            result = _dataOp.Insert("FileStructure", structModel);

            if (result.Status == Status.Successful)
            {
                subnode.StructId = result.BsonInfo.Int("structId");
                retNode.StructId = subnode.StructId;

                //
            }
            nodeList.Add(retNode);
            // Console.WriteLine(string.Format("Level:{0}  Name:{1}", subnode.Level, subnode.Name));

            if (subnode.SubNode != null)
            {
                foreach (var item in subnode.SubNode)
                {
                    nodeList.AddRange(FolderTreeInsert(item, subnode));
                }
            }
            return nodeList;
        }

        public List<InvokeResult<FileUploadSaveResult>> FolderTreeFileInsert(List<TreeNode> list, List<FileUploadObject> objList, UploadType type)
        {
            if (type == UploadType.Folder)
            {
                var obj = objList.FirstOrDefault();
                InvokeResult result = new InvokeResult();
                if (obj != null)
                {
                    BsonDocument fileRle = new BsonDocument();
                    var node = list.Where(t => t.Level == 0).FirstOrDefault();
                    fileRle.Add("structId", node != null ? node.StructId.ToString() : "0");
                    fileRle.Add("fileId", "0");
                    fileRle.Add("fileObjId", obj.fileObjId.ToString());
                    fileRle.Add("version", "1");
                    fileRle.Add("tableName", obj.tableName);
                    fileRle.Add("keyName", obj.keyName);
                    fileRle.Add("keyValue", obj.keyValue);
                    fileRle.Add("uploadType", type);
                    fileRle.Add("isPreDefine", obj.isPreDefine.ToString());
                    fileRle.Add("isCover", obj.isCover.ToString());
                     result = _dataOp.Insert("FileRelation", fileRle);
                }
            }
            List<InvokeResult<FileUploadSaveResult>> retList = new List<InvokeResult<FileUploadSaveResult>>();
            Dictionary<string, int> dic = new Dictionary<string, int>();
            foreach (var item in list)
            {
                if (item.FileList != null)
                {
                    foreach (var subItem in item.FileList)
                    {
                        if (!dic.Keys.Contains(subItem))
                        {
                            dic.Add(subItem, item.StructId);
                        }
                    }
                }
            }
            foreach (var item in objList)
            {
                InvokeResult<FileUploadSaveResult> result = new InvokeResult<FileUploadSaveResult>();
                var keyValue = dic.Where(t => t.Key == item.localPath).FirstOrDefault();
                int structId = keyValue.Value;
                item.structId = structId;
                result = SaveSingleFile(item);
                retList.Add(result);
            }
            return retList;
        }

        #endregion

        #endregion

        #region 文件包上传
        /// <summary>
        /// 文件包上传
        /// </summary>
        /// <param name="objList">文件列表</param>
        /// <param name="type"></param>
        /// <returns></returns>
        public List<InvokeResult<FileUploadSaveResult>> UploadMultipleFilesPackage(List<FileUploadObject> objList, UploadType type)
        {
            List<InvokeResult<FileUploadSaveResult>> retList = new List<InvokeResult<FileUploadSaveResult>>();
            InvokeResult result = new InvokeResult();
            try
            {
                FileUploadObject obj = objList.FirstOrDefault();
                string path = obj.localPath;
                string rootDir = obj.rootDir;
                int index = rootDir.Length;
                path = path.Substring(index);
                int index2 = path.IndexOf('\\');
                path = path.Substring(0, index2);
                BsonDocument structModel = new BsonDocument();
                structModel.Add("name", path);
                structModel.Add("nodePid", "0");
                result = _dataOp.Insert("FileStructure", structModel);
                if (result.Status == Status.Successful)
                {
                    int structId = result.BsonInfo.Int("structId");
                    BsonDocument fileRle = new BsonDocument();
                    fileRle.Add("structId", structId.ToString());
                    fileRle.Add("fileId", "0");
                    fileRle.Add("fileObjId", obj.fileObjId.ToString());
                    fileRle.Add("version", "1");
                    fileRle.Add("tableName", obj.tableName);
                    fileRle.Add("keyName", obj.keyName);
                    fileRle.Add("keyValue", obj.keyValue);
                    fileRle.Add("uploadType", type);
                    fileRle.Add("isPreDefine", obj.isPreDefine.ToString());
                    fileRle.Add("isCover", obj.isCover.ToString());
                    result = _dataOp.Insert("FileRelation", fileRle);

                    foreach (var item in objList)
                    {
                        item.structId = structId;
                        var ret = SaveSingleFile(item);
                        retList.Add(ret);
                    }

                }

            }
            catch (Exception ex)
            {
                return retList;
            }
            return retList;
        }
        #endregion

        #endregion

        #region 文件删除

        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="fileRelId">关联表ID</param>
        /// <returns></returns>
        public InvokeResult DeleteFileByRelId(int fileRelId)
        {
            InvokeResult result = new InvokeResult();
            try
            {
                BsonDocument doc = new BsonDocument();
                doc = _dataOp.FindOneByKeyVal("FileRelation", "fileRelId", fileRelId.ToString());
                if (doc != null)
                {
                    int fileId = doc.Int("fileId");

                    result = _dataOp.Delete("FileLibVersion", "db.FileLibVersion.distinct('_id',{'fileId':'" + fileId + "'})");

                    if (result.Status == Status.Successful)
                    {
                        result = _dataOp.Delete("FileRelation", "db.FileRelation.distinct('_id',{'fileRelId':'" + fileRelId + "'})");
                    }
                    if (result.Status == Status.Successful)
                    {
                        result = _dataOp.Delete("FileLibrary", "db.FileLibrary.distinct('_id',{'fileId':'" + fileId + "'})");
                    }
                    if (result.Status == Status.Successful)
                    {
                        int structId = doc.Int("structId");
                        if (structId != 0)
                        {
                            result = DeleteFolder(structId);
                        }
                        return result;
                    }
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
        /// 删除文件
        /// </summary>
        /// <param name="fileRelId">关联表ID</param>
        /// <returns></returns>
        public InvokeResult DeleteFileByFileId(int fileId)
        {
            InvokeResult result = new InvokeResult();
            try
            {
                result = _dataOp.Delete("FileLibVersion", "db.FileLibVersion.distinct('_id',{'fileId':'" + fileId + "'})");

                if (result.Status == Status.Successful)
                {
                    result = _dataOp.Delete("FileRelation", "db.FileRelation.distinct('_id',{'fileId':'" + fileId + "'})");
                }
                if (result.Status == Status.Successful)
                {
                    result = _dataOp.Delete("FileLibrary", "db.FileLibrary.distinct('_id',{'fileId':'" + fileId + "'})");
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
        /// 删除文件文件版本
        /// </summary>
        /// <param name="fileRelId">关联表ID</param>
        /// <returns></returns>
        public InvokeResult DeleteFileByFileVerId(int fileVerId)
        {
            InvokeResult result = new InvokeResult();
            try
            {
                var fileVerModel = _dataOp.FindOneByKeyVal("FileLibVersion", "fileVerId", fileVerId.ToString());
                int fileId = fileVerModel != null ? fileVerModel.Int("fileId") : 0;
                result = _dataOp.Delete("FileLibVersion", "db.FileLibVersion.distinct('_id',{'fileVerId':'" + fileVerId + "'})");


                int verCount = _dataOp.FindAllByKeyVal("FileLibVersion", "fileId", fileId.ToString()).ToList().Count();
                if (verCount == 0)
                {
                    result = _dataOp.Delete("FileLibrary", "db.FileLibrary.distinct('_id',{'fileId':'" + fileId + "'})");
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
        /// 文件删除
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="keyName"></param>
        /// <param name="keyValue"></param>
        /// <returns></returns>
        public InvokeResult DeleteFile(string tableName, string keyName, string keyValue)
        {
            InvokeResult result = new InvokeResult();
            try
            {

                var delQuery = _dataOp.FindAllByQuery("FileRelation", Query.And(
                    Query.EQ("tableName", tableName),
                    Query.EQ("keyName", keyName),
                    Query.EQ("keyValue", keyValue))).ToList();

                List<string> fileIdList = delQuery.Select(t => t.String("fileId")).ToList();

                List<int> structIdList = delQuery.Select(t => t.Int("structId")).ToList();

                result = _dataOp.Delete("FileLibrary", Query.In("fileId", TypeConvert.StringListToBsonValueList(fileIdList)));

                if (result.Status == Status.Successful)
                {
                    foreach (var structId in structIdList)
                    {
                        if (structId != 0)
                        {
                            result = DeleteFolder(structId);
                        }
                    }
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
        /// 删除该文件夹下的所有子节点及其文件
        /// </summary>
        /// <param name="structId"></param>
        /// <returns></returns>
        public InvokeResult DeleteFolder(int structId)
        {
            InvokeResult result = new InvokeResult();

            #region 删除文件
            List<BsonDocument> docList = new List<BsonDocument>();
            try
            {
                docList = _dataOp.FindAllByKeyVal("FileLibrary", "structId", structId.ToString()).ToList();

                foreach (var item in docList)
                {
                    int fileId = item.Int("fileId");
                    IFileDeleteInderface fileDelete = new FileDeleteOperation();

                    result = _dataOp.Delete("FileLibVersion", "db.FileLibVersion.distinct('_id',{'fileId':'" + fileId + "'})"); //删版本

                    if (result.Status == Status.Successful)
                    {
                        result = _dataOp.Delete("FileLibrary", "db.FileLibrary.distinct('_id',{'fileId':'" + fileId + "'})"); //删文件
                    }
                }
                List<BsonDocument> structList = new List<BsonDocument>();
                structList = _dataOp.FindAllByKeyVal("FileStructure", "nodePid", structId.ToString()).ToList();

                foreach (var item in structList)//递归删除子节点
                {
                    result = DeleteFolder(item.Int("structId"));
                    if (result.Status == Status.Failed)
                    {
                        return result;

                    }
                }
                if (result.Status == Status.Successful)
                {
                    result = _dataOp.Delete("FileStructure", "db.FileStructure.distinct('_id',{'structId':'" + structId.ToString() + "'})");
                    result = _dataOp.Delete("FileRelation", "db.FileRelation.distinct('_id',{'structId':'" + structId.ToString() + "'})");
                }
                //删除自身

            #endregion
            }
            catch (Exception ex)
            {
                result.Status = Status.Failed;
                result.Message = ex.Message;
                return result;
            }
            return result;
        }

        #endregion

        #region 封面图相关

        public InvokeResult SetCoverImage(int fileRelId)
        {
            InvokeResult result = new InvokeResult();
            BsonDocument fileRel = new BsonDocument();

            var doc = _dataOp.FindOneByKeyVal("FileRelation", "fileRelId", fileRelId.ToString());

            var query = Query.And(
                Query.EQ("fileObjId", doc.Text("fileObjId")),
                Query.EQ("tableName", doc.Text("tableName")),
                Query.EQ("keyName", doc.Text("keyName")),
                Query.EQ("keyValue", doc.Text("keyValue")));

            result = _dataOp.Update("FileRelation", query, new BsonDocument().Add("isCover", "false"));

            if (result.Status == Status.Successful)
            {
                result = _dataOp.Update("FileRelation", "db.FileRelation.distinct('_id',{'fileRelId':'" + fileRelId + "'})", "isCover=true");
            }



            return result;
        }
        /// <summary>
        /// 未实时保存时设置封面图
        /// </summary>
        /// <param name="fileObjId"></param>
        /// <param name="tableName"></param>
        /// <param name="keyName"></param>
        /// <param name="keyValue"></param>
        /// <returns></returns>
        public InvokeResult SetCoverImageNew(string fileObjId,string tableName,string keyName,string keyValue)
        {
            InvokeResult result = new InvokeResult();
            BsonDocument fileRel = new BsonDocument();
            var query = Query.And(
                Query.EQ("fileObjId", fileObjId),
                Query.EQ("tableName", tableName),
                Query.EQ("keyName", keyName),
                Query.EQ("keyValue", keyValue));
            result = _dataOp.Update("FileRelation", query, new BsonDocument().Add("isCover", "false"));
            return result;
        }


        public InvokeResult SetBrainCoverImage(int fileRelId)
        {
            InvokeResult result = new InvokeResult();
            BsonDocument fileRel = new BsonDocument();

            var doc = _dataOp.FindOneByKeyVal("FileRelation", "fileRelId", fileRelId.ToString());

            var query = Query.And(
                Query.EQ("fileObjId", doc.Text("fileObjId")),
                Query.EQ("tableName", doc.Text("tableName")),
                Query.EQ("keyName", doc.Text("keyName")),
                Query.EQ("keyValue", doc.Text("keyValue")));

            result = _dataOp.Update("FileRelation", query, new BsonDocument().Add("isUse", "false"));

            if (result.Status == Status.Successful)
            {
                result = _dataOp.Update("FileRelation", "db.FileRelation.distinct('_id',{'fileRelId':'" + fileRelId + "'})", "isUse=true");
            }



            return result;
        }

        #endregion

        #region 设置首页推送

        public InvokeResult SetIndexPush(int fileRelId,bool isPush)
        {
            InvokeResult result = new InvokeResult();
            try
            {
                result = _dataOp.Update("FileRelation", "db.FileRelation.distinct('_id',{'fileRelId':'" + fileRelId + "'})", "isPush=" + isPush.ToString().ToLower() + "");
            }
            catch (Exception ex)
            {
                result.Status = Status.Failed;
                result.Message = ex.Message;
            }

            return result;
        }
        #endregion

        #region  文档描述

        /// <summary>
        /// 文档描述
        /// </summary>
        /// <param name="propDic"></param>
        /// <returns></returns>
        public InvokeResult SetFileDescription(Dictionary<int, Dictionary<string, string>> propDic)
        {
            InvokeResult result = new InvokeResult();

            foreach (var item in propDic)
            {
                var fileDes = _dataOp.FindOneByKeyVal("FileDescription", "fileId", item.Key.ToString());
                if (fileDes == null)
                {
                    BsonDocument file = new BsonDocument();
                    file.Add("fileId", item.Key.ToString());

                    foreach (var prop in item.Value)
                    {
                        file.Add(prop.Key, prop.Value);
                    }

                    result = _dataOp.Insert("FileDescription", file);
                    if (result.Status == Status.Failed)
                    {
                        return result;
                    }
                }
                else
                {
                    result = UpdateFileDescription(item.Key, item.Value);
                }
            }
            return result;
        }

        /// <summary>
        /// 更新文件描述
        /// </summary>
        /// <param name="fileId"></param>
        /// <param name="propDic"></param>
        /// <returns></returns>
        public InvokeResult UpdateFileDescription(int fileId, Dictionary<string, string> propDic)
        {
            InvokeResult result = new InvokeResult();
            try
            {
                BsonDocument file = new BsonDocument();
                foreach (var prop in propDic)
                {
                    file.Add(prop.Key, prop.Value);
                }
                var query = Query.EQ("fileId", fileId.ToString());

                result = _dataOp.Update("FileDescription", query, file);
            }
            catch (Exception ex)
            {
                result.Status = Status.Failed;
                result.Message = ex.Message;
            }
            return result;
        }


        #endregion


        #region  文档复制操作
        public InvokeResult CopyFileRelation(List<BsonDocument> fileRelationList, string tableName, string keyName, string keyValue)
        {
            InvokeResult result = new InvokeResult() { Status = Status.Successful };
            foreach (var fileRel in fileRelationList)
            {
                var obj = fileRel.SourceBson("fileId");
                if (obj != null)
                {
                    #region 文档表数据
                    BsonDocument fileLib = new BsonDocument();
                    fileLib.Add("structId", obj.Text("structId"));
                    fileLib.Add("fileTypeId", obj.Text("fileTypeId"));
                    fileLib.Add("fileObjId", obj.Text("fileObjId"));
                    fileLib.Add("name", obj.Text("name"));
                    fileLib.Add("ext", obj.Text("ext"));
                    fileLib.Add("localPath", obj.Text("localPath"));
                    fileLib.Add("version", obj.Text("version"));
                    fileLib.Add("thumbPicPath", obj.Text("thumbPicPath"));
                    fileLib.Add("tags", obj.Text("tags"));
                    fileLib.Add("state", obj.Text("state"));
                    fileLib.Add("guid", obj.Text("guid"));
                    result = _dataOp.Insert("FileLibrary", fileLib);
                    if (result.Status == Status.Successful)
                    {
                        fileLib = result.BsonInfo;
                    }
                    else
                    {
                        return result;
                    }
                    #endregion

                    #region 文档版本表数据
                    BsonDocument fileLibVer = new BsonDocument();
                    fileLibVer.Add("fileId", result.BsonInfo.Text("fileId"));
                    fileLibVer.Add("name", result.BsonInfo.Text("name"));
                    fileLibVer.Add("ext", result.BsonInfo.Text("ext"));
                    fileLibVer.Add("localPath", result.BsonInfo.Text("localPath"));
                    fileLibVer.Add("version", result.BsonInfo.Text("version"));
                    fileLibVer.Add("hash", result.BsonInfo.Text("hash"));
                    fileLibVer.Add("tags", result.BsonInfo.Text("tags"));
                    fileLibVer.Add("guid", result.BsonInfo.Text("guid"));
                    fileLibVer.Add("thumbPicPath", result.BsonInfo.Text("thumbPicPath"));
                    result = _dataOp.Insert("FileLibVersion", fileLibVer);
                    if (result.Status == Status.Successful)
                    {
                        fileLibVer = result.BsonInfo;
                    }
                    #endregion

                    #region 文档关联表数据
                    BsonDocument fileRle = new BsonDocument();
                    fileRle.Add("fileId", result.BsonInfo.Text("fileId"));
                    fileRle.Add("fileObjId", fileRel.Text("fileObjId"));
                    fileRle.Add("structId", fileRel.Text("structId"));
                    fileRle.Add("version", result.BsonInfo.Text("version"));
                    fileRle.Add("tableName", tableName);
                    fileRle.Add("keyName", keyName);
                    fileRle.Add("keyValue", keyValue);
                    fileRle.Add("uploadType", fileRel.Text("uploadType"));
                    fileRle.Add("isPreDefine", fileRel.Text("isPreDefine"));
                    fileRle.Add("isCover", fileRel.Text("isCover"));
                    fileRle.Add("isFolde", fileRel.Text("isFolde"));
                    result = _dataOp.Insert("FileRelation", fileRle);

                    if (result.Status != Status.Successful)
                    {
                        return result;
                    }

                    #endregion
                }
            }
            return result;
        }
        #endregion

        #endregion

        #region 文档对象属性操作


        #endregion

        #region 公共操作

        ///// <summary>
        ///// 对象转换 将文件上传结果对象转成字符串 
        ///// 参数格式 param 前置参数@关联表id fileRetId-文件id fileId-版本id fileVerId 
        ///// </summary>
        ///// <param name="result"></param>
        ///// <returns></returns>
        //public InvokeResult<string> ResultConver(InvokeResult<FileUploadSaveResult> result)
        //{
        //    InvokeResult<string> ret = new InvokeResult<string>();
        //    try
        //    {
        //        if (result.Value != null)
        //        {
        //            FileUploadSaveResult retModel = new FileUploadSaveResult();
        //            JavaScriptSerializer script = new JavaScriptSerializer();
        //            retModel = result.Value;
        //            ret.Status = result.Status;
        //            FileParam fp = new FileParam();
        //            fp.path = result.Value.localPath;
        //            fp.ext = result.Value.ext;
        //            fp.strParam = string.Format("{0}@{1}-{2}-{3}", retModel.param, retModel.fileRelId, retModel.fileVerId);
        //            string strJson = script.Serialize(fp);
        //            ret.Value = strJson;
        //        }
        //        else
        //        {
        //            ret.Status = Status.Failed;
        //            ret.Message = "参数错误";
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        ret.Status = Status.Failed;
        //        ret.Message = ex.Message;
        //    }
        //    return ret;
        //}

        public InvokeResult<string> ResultConver(InvokeResult<FileUploadSaveResult> result)
        {
            List<InvokeResult<FileUploadSaveResult>> results = new List<InvokeResult<FileUploadSaveResult>>();
            results.Add(result);
            return ResultConver(results);
        }

        /// <summary>
        /// 对象转换 将文件上传结果对象转成字符串 
        /// 参数格式 param 前置参数@关联表id fileRetId-文件id fileId-版本id fileVerId 
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public InvokeResult<string> ResultConver(List<InvokeResult<FileUploadSaveResult>> results)
        {
            InvokeResult<string> ret = new InvokeResult<string>();
            JavaScriptSerializer script = new JavaScriptSerializer();
            try
            {
                List<FileParam> paramList = new List<FileParam>();
                foreach (var result in results)
                {
                    if (result.Value != null)
                    {
                        FileUploadSaveResult retModel = new FileUploadSaveResult();

                        retModel = result.Value;
                        ret.Status = result.Status;
                        FileParam fp = new FileParam();
                        fp.path = result.Value.localPath;
                        fp.ext = result.Value.ext;
                        fp.strParam = string.Format("{0}@{1}-{2}-{3}", retModel.param, retModel.fileRelId, retModel.fileId, retModel.fileVerId);
                        paramList.Add(fp);

                    }
                    else
                    {
                        ret.Status = Status.Failed;
                        ret.Message = "参数错误";
                    }
                }
                string strJson = script.Serialize(paramList);
                ret.Value = strJson;
                ret.Status = Status.Successful;
            }
            catch (Exception ex)
            {
                ret.Status = Status.Failed;
                ret.Message = ex.Message;
            }
            return ret;
        }

        /// <summary>
        /// 获取默认缩略图路径
        /// </summary>
        /// <param name="ext"></param>
        /// <returns></returns>
        public string GetImgPath(string ext)
        {
            string imgPath = "/Content/images/Docutype/default_m.png";
            switch (ext.ToLower())
            {
                case ".mdb":
                case ".accdb":
                    imgPath = "/Content/images/Docutype/access_m.png";
                    break;
                case ".doc":
                case ".docx":
                    imgPath = "/Content/images/Docutype/word_m.png";
                    break;
                case ".xls":
                case ".xlsx":
                    imgPath = "/Content/images/Docutype/excel_m.png";
                    break;
                case ".bmp":
                    imgPath = "/Content/images/Docutype/bmp_m.png";
                    break;
                case ".dwg":
                    imgPath = "/Content/images/Docutype/dwg_m.png";
                    break;
                case ".pdf":
                    imgPath = "/Content/images/Docutype/pdf_m.png";
                    break;
                case ".gif":
                    imgPath = "/Content/images/Docutype/gif_m.png";
                    break;
                case ".html":
                case ".htm":
                    imgPath = "/Content/images/Docutype/html_m.png";
                    break;
                case ".ini":
                    imgPath = "/Content/images/Docutype/ini_m.png";
                    break;
                case ".jpg":
                case ".jpeg":
                    imgPath = "/Content/images/Docutype/jpg_m.png";
                    break;
                case ".psd":
                    imgPath = "/Content/images/Docutype/psd_m.png";
                    break;
                case ".ppt":
                case ".pptx":
                    imgPath = "/Content/images/Docutype/ppt_m.png";
                    break;
                case ".rtf":
                    imgPath = "/Content/images/Docutype/rtf_m.png";
                    break;
                case ".mpp":
                    imgPath = "/Content/images/Docutype/mpp_m.png";
                    break;
                case ".tiff":
                case ".tif":
                    imgPath = "/Content/images/Docutype/tiff_m.png";
                    break;
                case ".txt":
                    imgPath = "/Content/images/Docutype/txt_m.png";
                    break;
                case ".wav":
                    imgPath = "/Content/images/Docutype/txt_m.png";
                    break;
                case ".result":
                    imgPath = "/Content/images/Docutype/result_m.png";
                    break;
                case ".rar":
                case ".7z":
                case ".zip":
                case ".bz2":
                case ".tar":
                case ".gz":
                case ".iso":
                case ".bin":
                case ".cue":
                    imgPath = "/Content/images/Docutype/rar_m.png";
                    break;
                default:
                    imgPath = "/Content/images/Docutype/default_m.png";
                    break;
            }
            return imgPath;
        }
        #endregion


    }

    /// <summary>
    /// 文件上传类型枚举
    /// </summary>
    public enum UploadType
    {
        /// <summary>
        /// 普通文件
        /// </summary>
        File = 0,
        /// <summary>
        /// 文件包
        /// </summary>
        Package = 1,
        /// <summary>
        /// 文件夹
        /// </summary>
        Folder = 2
    }

    /// <summary>
    /// A2系统文件参数对象 新旧系统能够同一 避免接口修改
    /// </summary>
    public class FileParam
    {
        public int docId { get; set; }
        public int lastVersion { get; set; }
        public string path { get; set; }
        public string strParam { get; set; }
        public string ext { get; set; }
        public int? ProfId { get; set; }
        public int? PhareId { get; set; }
        public int? FileCatId { get; set; }
        public int? PreFileId { get; set; }
        public int? FileId { get; set; }
        public int? BizFileId { get; set; }
        public int Counter { get; set; }
        public string StrHash { get; set; }
    }

    /// <summary>
    /// 文件上传对象
    /// </summary>
    public class FileUploadObject
    {
        /// <summary>
        /// 所属类型
        /// </summary>
        public int fileTypeId { get; set; }
        /// <summary>
        /// 所属对象
        /// </summary>
        public int fileObjId { get; set; }
        /// <summary>
        /// 本地路径
        /// </summary>
        public string localPath { get; set; }
        /// <summary>
        /// 关联表名
        /// </summary>
        public string tableName { get; set; }
        /// <summary>
        /// 关联表主键名
        /// </summary>
        public string keyName { get; set; }
        /// <summary>
        /// 关联表主键值
        /// </summary>
        public string keyValue { get; set; }
        /// <summary>
        /// 是否文件夹
        /// </summary>
        public int uploadType { get; set; }
        /// <summary>
        /// 是否预定义文档
        /// </summary>
        public bool isPreDefine { get; set; }
        /// <summary>
        /// 是否封面图
        /// </summary>
        public bool isCover { get; set; }
        /// <summary>
        /// 文档属性字典
        /// </summary>
        public Dictionary<string, string> propvalueDic { get; set; }
        /// <summary>
        /// 根目录
        /// </summary>
        public string rootDir { get; set; }

        /// <summary>
        /// 所属目录
        /// </summary>
        public int structId { get; set; }

        /// <summary>
        /// 文件子图参数
        /// </summary>
        public string subMapParam { get; set; }
        /// <summary>
        /// 二维码字符串
        /// </summary>
        public string guid2d { get; set; }
        /// <summary>
        /// 文档关联表专业属性值
        /// </summary>
        public string fileRel_profId { get; set; }
        /// <summary>
        /// 文档关联表阶段属性值
        /// </summary>
        public string fileRel_stageId { get; set; }
        /// <summary>
        ///  文档关联表类别属性值
        /// </summary>
        public string fileRel_fileCatId { get; set; }

    }

    /// <summary>
    /// 文件保存结果
    /// </summary>
    public class FileUploadSaveResult
    {
        /// <summary>
        /// 前置参数
        /// </summary>
        public string param { get; set; }
        /// <summary>
        /// 关联表ID
        /// </summary>
        public int fileRelId { get; set; }
        /// <summary>
        /// 文件id
        /// </summary>
        public int fileId { get; set; }
        /// <summary>
        /// 文件版本
        /// </summary>
        public int fileVerId { get; set; }
        /// <summary>
        /// 本地路径
        /// </summary>
        public string localPath { get; set; }
        /// <summary>
        ///  扩展名
        /// </summary>
        public string ext { get; set; }

    }

    /// <summary>
    /// 版本上传对象
    /// </summary>
    public class FileUploadVersionObject
    {
        /// <summary>
        /// 主键值
        /// </summary>
        public int fileId { get; set; }

        /// <summary>
        /// 本地路径
        /// </summary>
        public string localPath { get; set; }

        /// <summary>
        /// 文档属性字典
        /// </summary>
        public Dictionary<string, string> propvalueDic { get; set; }

    }


}
