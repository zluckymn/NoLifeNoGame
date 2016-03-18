using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Yinhe.ProcessingCenter;
using System.IO;
using Microsoft.Build.Evaluation;
using MongoDB.Bson;
using System.Transactions;
using MongoDB.Bson.IO;
using Yinhe.ProcessingCenter.DataRule;
using MongoDB.Driver.Builders;
using System.Collections;
using System.Text;
using System.DirectoryServices;
using MongoDB.Driver;

namespace Yinhe.WebHost.Controllers
{
    public class HomeController : Yinhe.ProcessingCenter.ControllerBase
    {
        //
        // GET: /Home/

        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        ///  错误信息页
        /// </summary>
        /// <returns></returns>
        public ActionResult ErrorInfo()
        {
            return View();
        }

        public ActionResult AlertInfo()
        {
            return View();
        }

        

        /// <summary>
        /// 文件统计
        /// </summary>
        /// <returns></returns>
        public ActionResult FileNumReCount()
        {

            //var LIST = dataOp.FindAll("CCCCC");
            //foreach (var item in LIST)
            //{
            //    BsonDocument userd = dataOp.FindOneByKeyVal("SysUser", "guid", item.Text("guid"));
            //    if (string.IsNullOrEmpty(userd.Text("guid")) || string.IsNullOrEmpty(item.Text("loginName")) || userd == null || userd.Text("loginPwd") != "8888") continue;
            //    var ret = dataOp.Update("SysUser", "db.SysUser.distinct('_id',{'guid':'" + item.Text("guid") + "'})", "loginPwd=" + item.Text("loginPwd"));
            //}

            return View();
        }

        public ActionResult ResetThumbPicPath()
        {
            return View();
        }

        /// <summary>
        /// 缩略图地址批量替换
        /// </summary>
        /// <returns></returns>
        public JsonResult ResetThumbPic()
        {
            string preText = PageReq.GetParam("preText");
            string aftText = PageReq.GetParam("aftText");
            InvokeResult result = new InvokeResult();
            var list = dataOp.FindAll("FileLibrary").Where(t => t.Text("thumbPicPath").Contains(preText)).ToList();

            foreach (var item in list)
            {
                BsonDocument doc = new BsonDocument();
                doc.Add("thumbPicPath", item.Text("thumbPicPath").Replace(preText, aftText));
                dataOp.Update("FileLibrary", Query.EQ("fileId", item.Text("fileId")), doc);

            }
            //string preText = PageReq.GetParam("preText");
            //string aftText = PageReq.GetParam("aftText");
            //var list = dataOp.FindAll("FileLibrary").Where(t => t.Text("thumbPicPath").Contains(preText)).ToList();
            //try
            //{
            //    foreach (var item in list)
            //    {
            //        BsonDocument doc = new BsonDocument();
            //        doc.Add("thumbPicPath", item.Text("thumbPicPath").Replace(preText, aftText));
            //        dataOp.Update("FileLibrary", Query.EQ("fileId", item.Text("fileId")), doc);
            //    }

            //}
            //catch (Exception ex)
            //{
            //    result.Status = Status.Failed;
            //    result.Message = ex.Message;
            //}
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 同步主键
        /// 当主键表中所存的的值小于对应表中实际最大主键值时
        /// 修改主键表中的值为该最大值
        /// </summary>
        /// <returns></returns>
        public JsonResult SyncPK()
        {
            InvokeResult result = new InvokeResult();
            List<StorageData> dataList = new List<StorageData>();
            var pkCounter = dataOp.FindAll("TablePKCounter").ToList();
            var allRules = TableRule.GetAllTables();
            var allTempRules = allRules.Select(i => new
            {
                tbName = i.Name,
                key = i.GetPrimaryKey()
            }).ToList();
            foreach (var counter in pkCounter)
            {
                var tbName = counter.Text("tbName");
                var curMax = counter.Int("count");//当前主键表所存主键值
                int factMax = 0;
                var rule = allTempRules.Where(i => i.tbName == tbName).FirstOrDefault();
                string key = string.Empty;
                if (rule != null)
                {
                    key = rule.key ?? string.Empty;
                }
                string current = string.Empty;
                
                if (!string.IsNullOrWhiteSpace(key))
                {
                    var allDatas = dataOp.FindAll(tbName).SetFields(key).ToList();
                    if (allDatas.Count > 0)
                    {
                        factMax = allDatas.Max(i => i.Int(key));
                        if (curMax < factMax)
                        {
                            StorageData data = new StorageData();
                            data.Name = "TablePKCounter";
                            data.Query = Query.And(Query.EQ("tbName", tbName), Query.EQ("count", curMax.ToString()));
                            data.Type = StorageType.Update;
                            data.Document = new BsonDocument().Add("count", factMax.ToString());
                            dataList.Add(data);
                        }
                    }
                }
            }
            result = dataOp.BatchSaveStorageData(dataList);
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        #region 插件信息
        /// <summary>
        ///  错误信息页
        /// </summary>
        /// <returns></returns>
        public ActionResult PublishPlugins()
        {
            return View();
        }

        /// <summary>
        /// 编译方法
        /// </summary>
        /// <param name="csprojFile">项目文件完整路径</param>
        /// <param name="buildLogFile">日志文件完整路径</param>
        /// <returns></returns>
        private bool ComplieProgramme(string csprojFile, string buildLogFile)
        {
            ProjectCollection projects = new ProjectCollection(ToolsetDefinitionLocations.Registry);

            Project project = projects.LoadProject(csprojFile);

            project.SetGlobalProperty("Configuration", "Release");  //设置编译方式：Debug或Release

            Microsoft.Build.BuildEngine.FileLogger logger = new Microsoft.Build.BuildEngine.FileLogger();
            logger.Parameters = @"logfile=" + buildLogFile;

            bool success = project.Build(logger);

            #region
            //project.SetGlobalProperty("OutputPath", @"c:\aaa\"); //设置编译输出路径
            //project.SetGlobalProperty("AssemblySearchPaths", @"c:\windows\microsoft.net\framework\v4.0.30319");  //设置引用程序集查找目录

            //ICollection<ProjectItem> projectReferenceProjectItems = project.GetItems("ProjectReference");
            //foreach (ProjectItem pi in projectReferenceProjectItems)
            //{
            //    String sRefName = pi.GetMetadataValue("Name");
            //    String sAssemblyName = sRefName.ToLower() + ".dll";
            //    String sRefAssemblyFileName = "C:/Temp/" + sAssemblyName;  //获取程序集的文件路径
            //    Dictionary<string, string> refAssemblyMetadata = new Dictionary<string, string>();
            //    refAssemblyMetadata.Add("HintPath", sRefAssemblyFileName);
            //    refAssemblyMetadata.Add("Private", "False");
            //    project.AddItem("Reference", sRefName, refAssemblyMetadata);
            //}
            //project.RemoveItems(projectReferenceProjectItems);

            //Microsoft.Build.BuildEngine.Engine engine = new Microsoft.Build.BuildEngine.Engine();
            //engine.BinPath = @"c:\windows\microsoft.net\framework\v4.0.30319";
            //Microsoft.Build.BuildEngine.FileLogger logger = new Microsoft.Build.BuildEngine.FileLogger();
            //logger.Parameters = @"logfile=" + buildLogFile;
            //engine.RegisterLogger(logger);
            //bool success = engine.BuildProjectFile(csprojFile);
            //engine.UnregisterAllLoggers();
            #endregion

            return success;
        }

        /// <summary>
        /// 编译插件
        /// </summary>
        /// <param name="dirUrl">插件目录</param>
        /// <param name="projName">插件名称</param>
        /// <returns></returns>
        public ActionResult CompiledPlugin(string pluginDir, string pluginName)
        {
            PageJson json = new PageJson();

            try
            {
                json.Success = ComplieProgramme(Path.Combine(pluginDir, pluginName + ".csproj"), Path.Combine(pluginDir, "BuildLog.txt"));
            }
            catch (Exception e)
            {
                json.Success = false;
                json.Message = e.Message;
            }

            return Json(json);
        }

        /// <summary>
        /// 将插件拷贝到Host下
        /// </summary>
        /// <param name="pluginDir">插件目录</param>
        /// <param name="pluginName">插件名称</param>
        /// <returns></returns>
        public ActionResult CopyPluginDLLToHost(string pluginDir, string pluginName)
        {
            PageJson json = new PageJson();

            try
            {
                //编译当前插件
                ComplieProgramme(Path.Combine(pluginDir, pluginName + ".csproj"), Path.Combine(pluginDir, "BuildLog.txt"));

                string sourceFile = Path.Combine(pluginDir, "bin", pluginName + ".dll");        //插件dll

                String targetFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins", pluginName + ".dll");

                System.IO.File.Copy(sourceFile, targetFile, true);

                //编译HOST
                //ComplieProgramme(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Yinhe.WebHost.csproj"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BuildLog.txt"));

                json.Success = true;
            }
            catch (Exception e)
            {
                json.Success = false;
                json.Message = e.Message;
            }

            return Json(json);
        }

        /// <summary>
        /// 下载插件
        /// </summary>
        /// <param name="pluginDir"></param>
        /// <param name="pluginName"></param>
        public void DownLoadPluginDLL(string pluginDir, string pluginName)
        {
            try
            {
                //编译当前插件
                ComplieProgramme(Path.Combine(pluginDir, pluginName + ".csproj"), Path.Combine(pluginDir, "BuildLog.txt"));

                string filePath = Path.Combine(pluginDir, "bin", pluginName + ".dll");        //插件地址

                Response.ClearHeaders();
                Response.Clear();
                Response.Expires = 0;
                Response.Buffer = true;
                Response.AddHeader("Accept-Language", "zh-cn");
                Response.Charset = "GB2312";
                string name = System.IO.Path.GetFileName(filePath);
                System.IO.FileStream files = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                byte[] byteFile = null;
                if (files.Length == 0)
                {
                    byteFile = new byte[1];
                }
                else
                {
                    byteFile = new byte[files.Length];
                }
                files.Read(byteFile, 0, (int)byteFile.Length);
                files.Close();

                Response.AddHeader("Content-Disposition", "attachment;filename=" + HttpUtility.UrlEncode(name, System.Text.Encoding.UTF8));
                Response.ContentType = "application/octet-stream;";
                Response.BinaryWrite(byteFile);
                Response.End();

            }
            catch (Exception e)
            {
                Response.Write("<script>alert('" + e.Message + "'); </script>");
            }
        }

        /// <summary>
        /// 发布客户所有插件到Host
        /// </summary>
        /// <param name="A3Dir"></param>
        /// <param name="clientCode"></param>
        /// <returns></returns>
        public ActionResult PublishClientToHost(string A3Dir, string clientCode)
        {
            PageJson json = new PageJson();

            if (Directory.Exists(A3Dir) && clientCode.TrimStart() != "")
            {
                try
                {
                    #region 清空当前HOST下的所有插件

                    foreach (var tempFile in Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins")))
                    {
                        System.IO.File.Delete(tempFile);
                    }
                    #endregion

                    #region 发布需要发布的插件
                    string pluginDir = Path.Combine(A3Dir, "Plugin_" + clientCode);

                    List<string> pluginList = Directory.GetDirectories(pluginDir).ToList();

                    foreach (var tempPlugin in pluginList.Where(t => t.Replace(pluginDir, "").TrimStart('\\').StartsWith("Plugin_")))
                    {
                        string pluginName = tempPlugin.Replace(pluginDir, "").TrimStart('\\');       //插件名称

                        bool flag = false;      //是否需要发布

                        #region 判断插件是否需要发布
                        if (pluginName.Split(new string[] { "_" }, StringSplitOptions.RemoveEmptyEntries).Count() == 2) //系统通用插件
                        {
                            flag = true;
                        }
                        else if (pluginName.Split(new string[] { "_" }, StringSplitOptions.RemoveEmptyEntries).Count() == 3)
                        {
                            if (pluginName.Split(new string[] { "_" }, StringSplitOptions.RemoveEmptyEntries)[2] == clientCode)
                            {
                                flag = true;
                            }
                        }
                        #endregion

                        if (flag == true)
                        {
                            ComplieProgramme(Path.Combine(tempPlugin, pluginName + ".csproj"), Path.Combine(pluginDir, "BuildLog.txt"));//编译当前插件

                            string sourceFile = Path.Combine(tempPlugin, "bin", pluginName + ".dll");        //插件dll

                            String targetFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins", pluginName + ".dll");

                            System.IO.File.Copy(sourceFile, targetFile, true);  //拷贝插件
                        }
                    }
                    #endregion

                    json.Success = true;
                }
                catch (Exception e)
                {
                    json.Success = false;
                    json.Message = e.Message;
                }
            }
            else
            {
                json.Success = false;
                json.Message = "传入参数有误";
            }

            return Json(json);
        }

        #endregion

        #region 管理员操作控件
        public ActionResult DataEdit()
        {
            return View();
        }
        public ActionResult DataDelete()
        {
            return View();
        }
        public ActionResult DataImport()
        {
            return View();
        }
        public ActionResult DataStatistics()
        {
            return View();
        }
        public ActionResult DataToString()
        {
            return View();
        }
        public ActionResult ReSetLogTime()
        {
            return View();
        }
        public ActionResult ReSetTreeKey()
        {
            return View();
        }
        public ActionResult SendMessage()
        {
            return View();
        }
        public ActionResult DataExport()
        {
            return View();
        }
        public ActionResult FileStatistics()
        {
            return View();
        }
        public ActionResult FileNoSizeList()
        {
            return View();
        }
        public ActionResult PKCounterStatistics()
        {
            return View();
        }
        #endregion

        #region 客户端调用页面
        public ActionResult TZFrmResultEdit()
        {
            return View();
        }

        #endregion

        #region 快速创建插件站点 2013.1.31 郑伯锰

        /// <summary>
        /// 
        /// </summary>
        /// <param name="installPath">"C:\\Program Files\\MyWeb"</param>
        /// <param name="IISVirtualDirectory">"MyWeb";</param>
        private void CreateWebSite(string installPath, string IISVirtualDirectory)
        {
            try
            {
                var root = new DirectoryEntry("IIS://localhost/W3SVC/1/ROOT");
                foreach (DirectoryEntry directoryEntry in root.Children)
                {
                    if (directoryEntry.Name == IISVirtualDirectory)
                    {
                        try
                        {
                            root.Invoke("Delete", new[] { directoryEntry.SchemaClassName, IISVirtualDirectory });
                            root.CommitChanges();
                        }
                        catch (Exception)
                        {
                        }
                    }
                }

                DirectoryEntry de = root.Children.Add(IISVirtualDirectory, "IIsWebVirtualDir");
                de.Properties["Path"][0] = installPath + @"\MyWebSite";
                de.Invoke("AppCreate", true);
                de.Properties["AppFriendlyName"][0] = IISVirtualDirectory;

                //IIS下，将Framework自动对应到4.0版本。
                Object[] mappings = (Object[])de.InvokeGet("ScriptMaps");

                StringBuilder sb = new StringBuilder();
                foreach (var a in mappings)
                {
                    sb.Append(a + "\r\n");
                }

                ArrayList list = AddScriptArray();
                de.CommitChanges();
            }
            catch
            {
            }
        }

        /// <summary>
        /// IIS下，将Framework自动对应到4.0版本。
        /// </summary>
        /// <returns></returns>
        private ArrayList AddScriptArray()
        {
            string specialFolder = "c:";
            try
            {
                specialFolder = Environment.GetFolderPath(Environment.SpecialFolder.System).Substring(0, 2);
            }
            catch
            {

            }
            ArrayList list = new ArrayList();
            list.Add(".asp," + specialFolder + @"\WINDOWS\system32\inetsrv\asp.dll,5,GET,HEAD,POST,TRACE");
            list.Add(".cer," + specialFolder + @"\WINDOWS\system32\inetsrv\asp.dll,5,GET,HEAD,POST,TRACE");
            list.Add(".cdx," + specialFolder + @"\WINDOWS\system32\inetsrv\asp.dll,5,GET,HEAD,POST,TRACE");
            list.Add(".asa," + specialFolder + @"\WINDOWS\system32\inetsrv\asp.dll,5,GET,HEAD,POST,TRACE");
            list.Add(".idc," + specialFolder + @"\WINDOWS\system32\inetsrv\httpodbc.dll,5,OPTIONS,GET,HEAD,POST,PUT,DELETE,TRACE");
            list.Add(".shtm," + specialFolder + @"\WINDOWS\system32\inetsrv\ssinc.dll,5,GET,POST");
            list.Add(".shtml," + specialFolder + @"\WINDOWS\system32\inetsrv\ssinc.dll,5,GET,POST");
            list.Add(".stm," + specialFolder + @"\WINDOWS\system32\inetsrv\ssinc.dll,5,GET,POST");
            list.Add(".asax," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG");
            list.Add(".ascx," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG");

            list.Add(".ashx," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,1,GET,HEAD,POST,DEBUG");
            list.Add(".asmx," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,1,GET,HEAD,POST,DEBUG");
            list.Add(".aspx," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,1,GET,HEAD,POST,DEBUG");
            list.Add(".axd," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,1,GET,HEAD,POST,DEBUG");
            list.Add(".vsdisco," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,1,GET,HEAD,POST,DEBUG");
            list.Add(".rem," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,1,GET,HEAD,POST,DEBUG");
            list.Add(".soap," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,1,GET,HEAD,POST,DEBUG");
            list.Add(".config," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG");
            list.Add(".cs," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG");
            list.Add(".csproj," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG");

            list.Add(".vb," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG");
            list.Add(".vbproj," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG");
            list.Add(".webinfo," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG");
            list.Add(".licx," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG");
            list.Add(".resx," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG");
            list.Add(".resources," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG");
            list.Add(".master," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG");
            list.Add(".skin," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG");
            list.Add(".compiled," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG");
            list.Add(".browser," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG");

            list.Add(".mdb," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG");
            list.Add(".jsl," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG");
            list.Add(".vjsproj," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG");
            list.Add(".sitemap," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG");
            list.Add(".msgx," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,1,GET,HEAD,POST,DEBUG");
            list.Add(".ad," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG");
            list.Add(".dd," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG");
            list.Add(".ldd," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG");
            list.Add(".sd," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG");
            list.Add(".cd," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG");

            list.Add(".adprototype," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG");
            list.Add(".lddprototype," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG");
            list.Add(".sdm," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG");
            list.Add(".sdmDocument," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG");
            list.Add(".ldb," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG");
            list.Add(".mdf," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG");
            list.Add(".ldf," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG");
            list.Add(".java," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG");
            list.Add(".exclude," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG");
            list.Add(".refresh," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG");

            list.Add(".xamlx," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,1,GET,HEAD,POST,DEBUG");
            list.Add(".aspq," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG");
            list.Add(".cshtm," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG");
            list.Add(".cshtml," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,5");
            list.Add(".vbhtm," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG");
            list.Add(".vbhtml," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG");
            list.Add(".svc," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,1");
            list.Add(".xoml," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,1");
            list.Add(".rules," + specialFolder + @"\windows\microsoft.net\framework\v4.0.30319\aspnet_isapi.dll,5");

            return list;
        }
        #endregion

        /// <summary>
        /// 三盛OA验证列表
        /// </summary>
        /// <returns></returns>
        public ActionResult VerifyList() 
        {
            return View();
        }
        /// <summary>
        /// FileRelation表的冗余信息
        /// </summary>
        /// <returns></returns>
        public ActionResult FileRelRedundancy()
        {
            return View();
        }
        public ActionResult CommonTableEdit()
        {
            return View();
        }

        public ActionResult CommonTableManage()
        {
            return View();
        }
        public ActionResult CommonTablePhotoEdit()
        {
            return View();
        }
    }
}
