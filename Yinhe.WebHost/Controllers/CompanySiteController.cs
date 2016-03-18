using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text.RegularExpressions;
using Yinhe.ProcessingCenter;
using MongoDB.Bson;
using System.IO;

namespace Yinhe.WebHost.Controllers
{
    public class CompanySiteController : Controller
    {
        #region 展示页
      
        /// <summary>
        /// 通用在线编辑页
        /// </summary>
        /// <returns></returns>
        public ActionResult ComXheditor()
        {
            return View();
        }
        //
        // GET: /WorkFlowCenter/
        public ActionResult Index()
        {
            return View();
        }
        /// <summary>
        /// 站点管理首页
        /// </summary>
        /// <returns></returns>
        public ActionResult SiteManage()
        {
            return View();
        }

        

        /// <summary>
        /// 公司列表页面
        /// </summary>
        /// <returns></returns>
        public ActionResult CompanyList()
        {
            return View();
        }
        /// <summary>
        /// 合作客户列表
        /// </summary>
        /// <returns></returns>
        public ActionResult CooperationClientList()
        {
            return View();
        }
        /// <summary>
        /// 荣誉列表
        /// </summary>
        /// <returns></returns>
        public ActionResult HonorList()
        {
            return View();
        }

        /// <summary>
        /// 人物专访
        /// </summary>
        /// <returns></returns>
        public ActionResult ClientInterviewList()
        {
            return View();
        }

        /// <summary>
        /// 招聘
        /// </summary>
        /// <returns></returns>
        public ActionResult RecruitmentList()
        {
            return View();
        }


        /// <summary>
        /// 公司列表页面
        /// </summary>
        /// <returns></returns>
        public ActionResult CompanyEdit()
        {
            return View();
        }
        /// <summary>
        /// 合作客户列表
        /// </summary>
        /// <returns></returns>
        public ActionResult CooperationClientEdit()
        {
            return View();
        }
        /// <summary>
        /// 荣誉列表
        /// </summary>
        /// <returns></returns>
        public ActionResult HonorEdit()
        {
            return View();
        }

        /// <summary>
        /// 人物专访
        /// </summary>
        /// <returns></returns>
        public ActionResult ClientInterviewEdit()
        {
            return View();
        }

        /// <summary>
        /// 招聘
        /// </summary>
        /// <returns></returns>
        public ActionResult RecruitmentEdit()
        {
            return View();
        }
        #endregion
        #region 站点展示页
        /// <summary>
        /// 关于我们
        /// </summary>
        /// <returns></returns>
        public ActionResult About()
        {
            return View();
        }
        /// <summary>
        /// 合作客户
        /// </summary>
        /// <returns></returns>
        public ActionResult Client()
        {
            return View();
        }
        /// <summary>
        /// 联系我们
        /// </summary>
        /// <returns></returns>
        public ActionResult Contact()
        {
            return View();
        }
        /// <summary>
        /// 招聘信息
        /// </summary>
        /// <returns></returns>
        public ActionResult Recruitment()
        {
            return View();
        }
         /// <summary>
        /// 成功实践
        /// </summary>
        /// <returns></returns>
        public ActionResult Verification()
        {
            return View();
        }
        /// <summary>
        /// 银禾专访
        /// </summary>
        /// <returns></returns>
        public ActionResult Interview()
        {
            return View();
        }
         /// <summary>
        /// 银禾客户实践
        /// </summary>
        /// <returns></returns>
        public ActionResult CooperationClientPracticeEdit()
        {
            return View();
        }
            /// <summary>
        /// 银禾客户详细
        /// </summary>
        /// <returns></returns>
        public ActionResult ClientDetail()
        {
            return View();
        }

        /// <summary>
        /// 银禾采访详细
        /// </summary>
        /// <returns></returns>
        public ActionResult InterviewDetail()
        {
            return View();
        }

        /// <summary>
        /// 银禾相册编辑
        /// </summary>
        /// <returns></returns>
        public ActionResult AlbumEdit()
        {
            return View();
        }
        /// <summary>
        /// 银禾相册文档上传
        /// </summary>
        /// <returns></returns>
        public ActionResult AlbumFileEdit()
        {
            return View();
        }
        /// <summary>
        /// 银禾相册列表
        /// </summary>
        /// <returns></returns>
        public ActionResult AlbumList()
        {
            return View();
        }
        
        #endregion

        #region 侨鑫xhEditor Ajax文件上传
        /// <summary>
        /// 侨鑫xhEditor Ajax文件上传
        /// </summary>
        /// <returns></returns>
        public string SaveImageCompanySite()
        {
            string inputname = "filedata";//表单文件域name
            string attachdir = "/UploadFiles/CompanySite";     // 上传文件保存路径，结尾不要带/
       
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


        /// <summary>
        /// 通用ajax图片上传
        /// </summary>
        /// <returns></returns>
        public string SaveAjaxImage()
        {
            var dataOp=new DataOperation();
            string msg = "";
            var keyValue = PageReq.GetForm("keyValue");//主键ID
            var keyField = PageReq.GetForm("keyField");//主键字段名
            var fkeyValue = PageReq.GetForm("fkeyValue");//外键主键ID
            var fKeyField = PageReq.GetForm("fKeyField");//外键字段名
            var tableName = PageReq.GetForm("tableName");//表名
            var fieldName = PageReq.GetForm("fieldName");//对象图片存储名
            var needCreate = PageReq.GetForm("needCreate");//当对象不存在时候是否创建对象
            if (string.IsNullOrEmpty(fieldName))
            { 
             fieldName="companyImage";
            }
            var relativePath = string.Format("/UploadFiles/CompanySite/{0}",tableName);
            
             
           
            if (Request.Files.Count > 0)
            {
               
                string path = Server.MapPath(relativePath);
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                var file = Request.Files[0];

                string fileName;

                if (Request.Browser.Browser.ToUpper() == "IE")
                {
                    string[] files = file.FileName.Split(new char[] { '\\' });
                    fileName = files[files.Length - 1];
                }
                else
                {
                    fileName = file.FileName;
                }
               



                string strFileName = fileName;
                if (string.IsNullOrEmpty(strFileName))
                {
                    msg = "{";
                    msg += string.Format("error:'{0}',\n", "请选择文件！");
                    msg += string.Format("msg:'{0}'\n", string.Empty);
                    msg += "}";
                }
                else
                {
                    var relativeFileName = Path.Combine(relativePath, fileName);
                    fileName = Path.Combine(path, fileName);
                    file.SaveAs(fileName);
                    ///更新图片字段
                    var curObj = dataOp.FindOneByKeyVal(tableName, keyField, keyValue);
                    if (curObj == null || string.IsNullOrEmpty(curObj.Text(keyField)))
                    {
                        if (string.IsNullOrEmpty(needCreate))
                        {
                            msg = "{";
                            msg += string.Format("error:'{0}',\n", "传入参数有误，请刷新后重试");
                            msg += string.Format("msg:'{0}'\n", string.Empty);
                            msg += "}";
                            return msg;
                        }
                        else
                        {

                            var result = dataOp.Insert(tableName, new BsonDocument().Add("fileName", Path.GetFileNameWithoutExtension(fileName)).Add(fieldName, relativeFileName).Add(fKeyField, fkeyValue));
                            if (result.Status == Status.Successful)
                            {
                                curObj = result.BsonInfo;
                            }
                            else
                            {
                                msg = "{";
                                msg += string.Format("error:'{0}',\n", "对象不存在且无法创建，请刷新后重试");
                                msg += string.Format("msg:'{0}'\n", string.Empty);
                                msg += "}";
                                return msg;
                            }

                        }
                    }
                    else
                    {
                        dataOp.Update(curObj, new BsonDocument().Add(fieldName, relativeFileName));

                    }
                    msg = "{";
                    msg += string.Format("error:'{0}',\n", string.Empty);
                    msg += string.Format("msg:'{0}'\n", "上传成功");
                    msg += "}";
                }
                return msg;


            }
            else
            {
                msg = "{";
                msg += string.Format("error:'{0}',\n", string.Empty);
                msg += string.Format("msg:'{0}'\n", "请先选择图片");
                msg += "}";
                return msg;
            }
        }

        public bool IsReusable
        {
            get
            {
                return true;
            }
        }
    
        #endregion

        string GetFileExt(string FullPath)
        {
            if (FullPath != "") return FullPath.Substring(FullPath.LastIndexOf('.') + 1).ToLower();
            else return "";
        }

        void CreateFolder(string FolderPath)
        {
            if (!System.IO.Directory.Exists(FolderPath)) System.IO.Directory.CreateDirectory(FolderPath);
        }

        string jsonString(string str)
        {
            str = str.Replace("\\", "\\\\");
            str = str.Replace("/", "\\/");
            str = str.Replace("'", "\\'");
            return str;
        }


       
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SaveCompanyInfo()
        {
            var tableName = PageReq.GetForm("tableName");
            var keyField = PageReq.GetForm("keyField");
            var keyValue = PageReq.GetForm("keyValue");
            var htmlEditField = PageReq.GetForm("htmlEditField");
            var htmlEditValue =Server.UrlDecode(PageReq.GetForm("htmlEditValue"));
            var dataOp = new DataOperation();
            var curObj = dataOp.FindOneByKeyVal(tableName, keyField, keyValue);
            var updateBson=new BsonDocument().Add(htmlEditField,htmlEditValue);
            InvokeResult result=new InvokeResult();
            if (curObj != null)
            {
                result = dataOp.Update(curObj, updateBson);
            }
          
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }
    }
}
