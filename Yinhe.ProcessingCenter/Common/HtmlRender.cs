using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;

using System.Drawing;
using System.Web;
using System.Text.RegularExpressions;

 
using System.Diagnostics;
using System.Drawing.Printing;
using System.IO;
using WebSupergoo.ABCpdf7;
using WebSupergoo.ABCpdf7.Objects;
using WebSupergoo.ABCpdf7.Operations;

namespace Yinhe.ProcessingCenter
{
    #region 将html页面生成图片
    /// <summary>
    /// 将html页面生成图片
    /// </summary>
    public class HtmlToImg
    {
        #region 常量
        /// <summary>
        /// 默认宽度
        /// </summary>
        const int WIDTH = 1024;
        /// <summary>
        /// 默认高度
        /// </summary>
        const int HEIGHT = 1280;
        #endregion

        #region 成员变量
        /// <summary>
        /// 缩略图宽度
        /// </summary>
        public int Width { get; set; }
        /// <summary>
        /// 缩略图高度
        /// </summary>
        public int Height { get; set; }
        /// <summary>
        /// 将被生成图片的页面Url地址
        /// </summary>
        public string Url { get; set; }
        /// <summary>
        /// 将被生成图片的HTML代码
        /// </summary>
        public string HtmlText { get; set; }
        /// <summary>
        /// 生成的原始大小的图片路径
        /// </summary>
        public string OriginPath { get; set; }
        /// <summary>
        /// 生成的缩略图路径
        /// </summary>
        public string ThumbPath { get; set; }
        #endregion

        #region 构造器
        /// <summary>
        /// 根据页面Url地址生成网页快照
        /// </summary>
        /// <param name="url">页面地址</param>
        /// <param name="originPath">生成的原始大小的图片路径</param>
        /// <param name="thumbPath">生成的缩略图路径</param>
        /// <param name="width">缩略图宽度</param>
        /// <param name="height">缩略图高度</param>
        public HtmlToImg(string url, string originPath, string thumbPath, int width, int height) 
        {
            this.Url = url;
            this.Width = width;
            this.Height = height;
            this.OriginPath = originPath;
            this.ThumbPath = thumbPath;
        }
        /// <summary>
        /// 根据HTML代码生成页面内容快照
        /// </summary>
        /// <param name="htmlText">HTML代码</param>
        /// <param name="originPath">生成的原始大小的图片路径</param>
        /// <param name="thumbPath">生成的缩略图路径</param>
        /// <param name="width">缩略图宽度</param>
        /// <param name="height">缩略图高度</param>
        /// <param name="noUse">此参数暂时没有使用</param>
        public HtmlToImg(string htmlText, string originPath, string thumbPath, int width, int height, bool noUse)
        {
            this.HtmlText = this.ReplaceSrc(htmlText);
            this.Width = width;
            this.Height = height;
            this.OriginPath = originPath;
            this.ThumbPath = thumbPath;
        }

        #endregion

        #region 私有方法
        /// <summary>
        /// 在src属性前加上域名
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        string ReplaceSrc(string source)
        {
            string protocal = System.Web.HttpContext.Current.Request.ServerVariables["SERVER_PROTOCOL"].Split('/')[0].ToLower();
            string httpHost = System.Web.HttpContext.Current.Request.ServerVariables["HTTP_HOST"];
            string prefix = protocal + "://" + httpHost + "/";

            string oldStr = "src=\"/";
            string newStr = "src=\"" + prefix;
            return source.Replace(oldStr, newStr);
        }
        #endregion
    }

    #endregion

    /// <summary>
    /// Html处理类
    /// </summary>
    public class HtmlHelper
    {
             
        /// <summary>
        /// 去除HTML标记
        /// </summary>
        /// <param name="Htmlstring"></param>
        /// <returns></returns>
        public static string RemoveHTML(string Htmlstring)
        {
            Htmlstring = HttpUtility.UrlDecode(Htmlstring,Encoding.UTF8 );
            //删除脚本
            Htmlstring = Regex.Replace(Htmlstring, @"<script[^>]*?>.*?</script>", "", RegexOptions.IgnoreCase);

            //删除HTML

            Htmlstring = Regex.Replace(Htmlstring, @"<(.[^>]*)>", "", RegexOptions.IgnoreCase);

            Htmlstring = Regex.Replace(Htmlstring, @"([\r\n])[\s]+", "", RegexOptions.IgnoreCase);

            Htmlstring = Regex.Replace(Htmlstring, @"-->", "", RegexOptions.IgnoreCase);

            Htmlstring = Regex.Replace(Htmlstring, @"<!--.*", "", RegexOptions.IgnoreCase);

            Htmlstring = Regex.Replace(Htmlstring, @"&(quot|#34);", "\"", RegexOptions.IgnoreCase);

            Htmlstring = Regex.Replace(Htmlstring, @"&(amp|#38);", "&", RegexOptions.IgnoreCase);

            Htmlstring = Regex.Replace(Htmlstring, @"&(lt|#60);", "<", RegexOptions.IgnoreCase);

            Htmlstring = Regex.Replace(Htmlstring, @"&(gt|#62);", ">", RegexOptions.IgnoreCase);

            Htmlstring = Regex.Replace(Htmlstring, @"&(nbsp|#160);", " ", RegexOptions.IgnoreCase);

            Htmlstring = Regex.Replace(Htmlstring, @"&(iexcl|#161);", "\xa1", RegexOptions.IgnoreCase);

            Htmlstring = Regex.Replace(Htmlstring, @"&(cent|#162);", "\xa2", RegexOptions.IgnoreCase);

            Htmlstring = Regex.Replace(Htmlstring, @"&(pound|#163);", "\xa3", RegexOptions.IgnoreCase);

            Htmlstring = Regex.Replace(Htmlstring, @"&(copy|#169);", "\xa9", RegexOptions.IgnoreCase);

            Htmlstring = Regex.Replace(Htmlstring, @"&#(\d+);", "", RegexOptions.IgnoreCase);

            Htmlstring.Replace("<", "");

            Htmlstring.Replace(">", "");

            Htmlstring.Replace("\r\n", "");
        
    
            return Htmlstring;
        }

        /// <summary>
        /// 将html页面生成pdf文档，异步方式
        /// </summary>
        /// <param name="url"></param>
        /// <param name="savePath"></param>
        /// <returns></returns>
        public static bool Html2Img(string url, string originPath)
        {
            Thread newThread = new Thread(delegate()
            {
                try
                {
                    Doc doc = new Doc();
                    doc.HtmlOptions.Timeout = 30 * 1000;
                    doc.HtmlOptions.UseScript = true;
                    doc.Rect.Inset(36.0, 72.0);
                    doc.Page = doc.AddPage();
                    int num = doc.AddImageUrl(url);
                    while (true)
                    {
                        doc.FrameRect();
                        if (!doc.Chainable(num))
                        {
                            break;
                        }
                        doc.Page = doc.AddPage();
                        num = doc.AddImageToChain(num);
                    }
                    for (int i = 1; i <= doc.PageCount; i++)
                    {
                        doc.PageNumber = i;
                        doc.Flatten();
                    }
                    doc.Save(originPath);
                    doc.Clear();
                }
                catch (Exception ex)
                {
                    LogError.ReportErrors(ex.Message);
                }

            });
            newThread.Start();

            return true;
        }

        /// <summary>
        /// 将html页面生成pdf文档，同步方式
        /// </summary>
        /// <param name="url"></param>
        /// <param name="savePath"></param>
        /// <returns></returns>
        public static bool Html2PDFSynch(string url, string savePath, string customName)
        {

            Doc doc = new Doc();
            try
            {
                //url = "http://localhost:8018/PDF?returnUrl=http://localhost:8018/Material/MatList/exporttopdf?matListId=167";
                doc.HtmlOptions.UseNoCache = true;
                doc.HtmlOptions.PageCacheEnabled = false;
                doc.HtmlOptions.PageCacheClear();
                doc.HtmlOptions.Timeout = 30 * 1000;
                doc.HtmlOptions.UseScript = true;
                doc.HtmlOptions.UseActiveX = true;
                doc.Rect.Inset(36.0, 72.0);
                doc.Page = doc.AddPage();
                int num = doc.AddImageUrl(url);
                while (true)
                {
                    //doc.FrameRect();//添加黑色边框
                    if (!doc.Chainable(num))
                    {
                        break;
                    }
                    doc.Page = doc.AddPage();
                    num = doc.AddImageToChain(num);
                }
                for (int i = 1; i <= doc.PageCount; i++)
                {
                    doc.PageNumber = i;
                    
                    doc.Color.String = "0 0 0"; //黑色
                    doc.AddLine(24, 750, 588, 750); //画一条分隔线

                    doc.Rect.String = "24 12 588 40";
                    doc.HPos = 0;
                    doc.VPos = 0.5;
                    doc.Font = doc.AddFont("宋体", "ChineseS");
                    doc.TextStyle.Italic = true;
                    doc.AddHtml(" <font color=\"#cccccc\">" + customName + "</font>");
                    doc.TextStyle.Italic = false;

                    doc.Rect.String = "24 12 588 40";
                    doc.HPos = 1.0;
                    doc.VPos = 0.5;
                    doc.Color.String = "0 0 0"; //黑色
                    doc.AddHtml("page " + i.ToString() + "/" + doc.PageCount.ToString());
                    doc.AddLine(24, 40, 588, 40);

                    doc.Flatten();
                }
                if (!savePath.ToLower().EndsWith(".pdf"))
                {
                    savePath += ".pdf";
                }
                doc.Save(savePath);
                doc.Clear();
                return true;
            }
            catch (Exception ex)
            {
                LogError.ReportErrors(ex.Message);
                
                return false;
            }
            finally 
            {
                doc.Clear();
            }

            return true;
        }

        //public static  bool PageToPdfByteArray(string url, string path, Encoding encoe)
        //{
        //    byte[] pdfBuf;
        //    bool ret = false;
        //    try
        //    {
        //        GlobalConfig gc = new GlobalConfig();
              
        //        // set it up using fluent notation
        //        gc.SetMargins(new Margins(0, 0, 0, 0))
        //          .SetDocumentTitle("Test document")
        //          .SetPaperSize(PaperKind.A4);
        //        //... etc

        //        // create converter
        //        IPechkin pechkin = new SynchronizedPechkin(gc);
                
        //        // subscribe to events
        //        //pechkin.Begin += OnBegin;
        //        //pechkin.Error += OnError;
        //        //pechkin.Warning += OnWarning;
        //        //pechkin.PhaseChanged += OnPhase;
        //        //pechkin.ProgressChanged += OnProgress;
        //        //pechkin.Finished += OnFinished;

        //        // create document configuration object
        //        ObjectConfig oc = new ObjectConfig();

        //        // and set it up using fluent notation too
        //        oc.SetCreateExternalLinks(false)
        //        .SetFallbackEncoding(encoe)
        //        .SetLoadImages(true)
        //        .SetPageUri(url);
        //        //... etc

        //        // convert document
        //        pdfBuf = pechkin.Convert(oc);
                
        //        FileStream fs = new FileStream(path, FileMode.Create);
        //        fs.Write(pdfBuf, 0, pdfBuf.Length);
        //        fs.Close();
        //        ret = true;
        //    }
        //    catch (Exception ex)
        //    {

        //    }

        //    return ret;
        //}
        
        /// <summary>
        /// 根据页面url，按节点目录生成pdf和对应书签(空页面不生成)
        /// </summary>
        /// <param name="docList"></param>
        /// <param name="savePath"></param>
        /// <returns></returns>
        public static bool MergePdf(List<PdfDoc> docList, string savePath) 
        {
            if (docList.Count == 0) return false;
            Doc doc = new Doc();
            doc.HtmlOptions.Timeout = 30 * 1000;
            doc.HtmlOptions.UseScript = true;
            doc.Rect.Inset(36.0, 72.0);//Rect默认是文档整个页面大小, 这里的Inset表示将Rect左右留出36的空白,上下留出72的空白

            string emptyMarkPath = null;
            bool emptyMarkExp = false;
            try
            {
                foreach (PdfDoc pd in docList)
                {
                    if (pd.NodePid != 0) //0为根节点，没有页面
                    {
                        if (string.IsNullOrEmpty(pd.Url))
                        {
                            /**
                             * 有些目录可能没有页面内容，这里则先将目录的bookmark路径保存；
                             * **/
                            emptyMarkPath = GetPath(docList, pd).TrimEnd('\\');
                            emptyMarkExp = pd.Expended;
                        }
                        else
                        {
                            doc.Page = doc.AddPage();
                            if (emptyMarkPath != null) 
                            {
                                doc.AddBookmark(emptyMarkPath, emptyMarkExp);//让空目录指定到其第一个子页面
                                emptyMarkPath = null;//添加之后置空
                            }
                            doc.AddBookmark(GetPath(docList, pd).TrimEnd('\\'), pd.Expended);

                            int num = doc.AddImageUrl(pd.Url);
                            while (true)
                            {
                                //doc.FrameRect();//给内容区域添加黑色边框
                                if (!doc.Chainable(num))
                                {
                                    break;
                                }
                                doc.Page = doc.AddPage();
                                num = doc.AddImageToChain(num);
                            }
                            
                        }
                    }
                }
                for (int i = 1; i <= doc.PageCount; i++)
                {
                    doc.PageNumber = i;
                    doc.Flatten();
                }
                if (!savePath.ToLower().EndsWith(".pdf"))
                {
                    savePath += ".pdf";
                }
                doc.Save(savePath);
            }
            catch (Exception ex)
            {
                LogError.ReportErrors(ex.Message);
                return false;
            }
            finally 
            {
                doc.Clear();
                doc.Dispose();
            }

            return true;
        }
        /// <summary>
        /// 根据页面url，按节点目录生成pdf和对应书签(生成所有页面)
        /// </summary>
        /// <param name="docList"></param>
        /// <param name="savePath"></param>
        /// <returns></returns>
        public static bool MergePdf2(List<PdfDoc> docList, string savePath, string customerName, string bookTaskUrl) 
        {
            if (docList.Count == 0) return false;
            Doc doc = new Doc();
            doc.HtmlOptions.Timeout = 30 * 1000;
            doc.HtmlOptions.UseScript = true;
            doc.HtmlOptions.UseNoCache = true;
            doc.HtmlOptions.PageCacheEnabled = false;
            doc.HtmlOptions.PageCacheClear();
            doc.Rect.Inset(36.0, 72.0);
            
            try
            {
                Dictionary<int, string> titleDic = new Dictionary<int, string>();
                foreach (PdfDoc pd in docList)
                {
                    if (pd.NodePid == 0) //0为根节点，没有页面
                        continue;
                    doc.Page = doc.AddPage();
                    doc.AddBookmark(GetPath(docList, pd).TrimEnd('\\'), pd.Expended);
                    titleDic.Add(doc.PageCount, pd.Name);

                    if (pd.Url == null)
                        continue;
                    int num = doc.AddImageUrl(pd.Url);
                    
                    while (true)
                    {
                        //doc.FrameRect();//给内容区域添加黑色边框
                        if (!doc.Chainable(num))
                        {
                            break;
                        }
                        doc.Page = doc.AddPage();
                        num = doc.AddImageToChain(num);
                        titleDic.Add(doc.PageCount, pd.Name);
                    }
                    
                }
                #region 添加页眉和页脚

                AddHeader(ref doc, titleDic, customerName, bookTaskUrl);
                for (int i = 1; i <= doc.PageCount; i++)
                {
                    doc.PageNumber = i;
                    //压缩输出
                    doc.Flatten();
                }
                #endregion
                if (!savePath.ToLower().EndsWith(".pdf"))
                {
                    savePath += ".pdf";
                }
                doc.Save(savePath);
            }
            catch (Exception ex)
            {
                LogError.ReportErrors(ex.Message);
                return false;
            }
            finally
            {
                doc.Clear();
                doc.Dispose();
            }

            return true;
        }
        /// <summary>
        /// 整个任务书展示页面生成pdf，同时计算页码，生成书签(此方法不完善，书签定位不正确)
        /// </summary>
        /// <param name="docList"></param>
        /// <param name="url"></param>
        /// <param name="savePath"></param>
        /// <param name="customName"></param>
        /// <param name="tops"></param>
        /// <returns></returns>
        public static bool MergePdf3(List<PdfDoc> docList,string url,string savePath, string customName, Dictionary<int, int> tops)
        {
            Doc doc = new Doc();
            doc.HtmlOptions.Timeout = 30 * 1000;
            doc.HtmlOptions.UseScript = true;
            doc.HtmlOptions.UseNoCache = true;
            doc.HtmlOptions.PageCacheEnabled = false;
            doc.HtmlOptions.PageCacheClear();

            int num = doc.AddImageUrl(url);
            while (true)
            {
                if (!doc.Chainable(num))
                {
                    break;
                }
                doc.Page = doc.AddPage();
                num = doc.AddImageToChain(num);
            }
            for (int i = 0; i < tops.Count; i++)
            {
                int minusOffset = tops.Values.ToList<int>()[0] * i;
                double offset = 0.0;
                doc.PageNumber = CalculatePageNum(doc, tops.Values.ToList<int>()[i], out offset, minusOffset);
                int id = doc.AddBookmark(GetPath(docList, tops.Keys.ToList<int>()[i]).TrimEnd('\\'), true);
                doc.SetInfo(id, "/Dest:Del", "");
                doc.SetInfo(id, "/Dest[]:Ref", doc.Page.ToString());
                doc.SetInfo(id, "/Dest[]:Name", "XYZ");
                doc.SetInfo(id, "/Dest[]:Num", "0");
                doc.SetInfo(id, "/Dest[]:Num", offset.ToString());
                doc.SetInfo(id, "/Dest[]", "null");

            }

            for (int i = 1; i <= doc.PageCount; i++)
            {
                doc.PageNumber = i;
                doc.Flatten();
            }
            doc.Save(savePath);
            doc.Clear();

            return true;
        }
        /// <summary>
        /// 按一级目录请求页面,二级和以后的目录全部不生成，书签只能定位到一级目录
        /// </summary>
        /// <param name="docList"></param>
        /// <param name="savePath"></param>
        /// <param name="customerName"></param>
        /// <param name="bookTaskUrl">为a标签包含的链接</param>
        /// <returns></returns>
        public static bool MergePdf4(List<PdfDoc> docList, string savePath, string customerName,string bookTaskUrl)
        {
            if (docList.Count == 0)
            {
                return false;
            }
            Doc doc = new Doc();
            doc.HtmlOptions.Timeout = 30 * 1000;
            doc.HtmlOptions.UseScript = true;
            doc.HtmlOptions.UseNoCache = true;
            doc.HtmlOptions.PageCacheEnabled = false;
            doc.HtmlOptions.PageCacheClear();
            doc.Rect.Inset(52.0, 100.0);
            
            try
            {
                Dictionary<int, string> titleDic = new Dictionary<int, string>();
                foreach (PdfDoc pd in docList)
                {
                    if (pd.NodePid == 0)//0为根节点，可能为封面，单独处理
                        continue;

                    doc.Page = doc.AddPage();
                    doc.AddBookmark(GetPath(docList, pd).TrimEnd('\\'), pd.Expended);
                    titleDic.Add(doc.PageCount, pd.Name);

                    if (pd.Url == null)
                    {
                        continue;
                    }
                    int num = doc.AddImageUrl(pd.Url);

                    while (true)
                    {
                        if (!doc.Chainable(num))
                        {
                            break;
                        }
                        doc.Page = doc.AddPage();
                        num = doc.AddImageToChain(num);
                        titleDic.Add(doc.PageCount, pd.Name);
                    }

                }
                #region 添加页眉和页脚

                AddHeader(ref doc, titleDic, customerName, bookTaskUrl);
                for (int i = 1; i <= doc.PageCount; i++)
                {
                    doc.PageNumber = i;
                    //压缩输出
                    doc.Flatten();
                }
                #endregion
                if (!savePath.ToLower().EndsWith(".pdf"))
                {
                    savePath += ".pdf";
                }
                doc.Save(savePath);
            }
            catch (Exception ex)
            {
                LogError.ReportErrors(ex.Message);
                return false;
            }
            finally
            {
                doc.Clear();
                doc.Dispose();
            }

            return true;
        }
        /// <summary>
        /// 添加页眉和页脚
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="titleDic"></param>
        /// <param name="customName"></param>
        /// <param name="bookTaskUrl"></param>
        private static void AddHeader(ref Doc doc, Dictionary<int, string> titleDic, string customName, string bookTaskUrl) 
        {
            for (int i = 1; i <= doc.PageCount; i++)
            {
                doc.PageNumber = i;

                doc.Rect.String = "52 722 568 750";  //"left bottom right top". 直接通过这种方式指定表头输出区域
                doc.HPos = 0; //0.5代表居中, 0代表居左, 1代表居右
                doc.VPos = 1; //0.5代表居中, 0代表靠上, 1代表靠下
                doc.Color.String = "0 0 0";
                doc.Font = doc.AddFont("宋体", "ChineseS");
                doc.AddHtml( bookTaskUrl);
                
                doc.Rect.String = "52 722 568 750";  //"left bottom right top". 
                doc.HPos = 1; //0.5代表居中, 0代表居左, 1代表居右
                doc.VPos = 1; //0.5代表居中, 0代表靠上, 1代表靠下
                doc.Color.String = "0 0 0"; //红色255 0 0
                doc.Font = doc.AddFont("宋体", "ChineseS");
                doc.AddHtml(" <font color=\"#333333\">" + titleDic[i] + "</font>");

                doc.Color.String = "0 0 0"; 
                doc.AddLine(52, 720, 568, 720); //画一条页眉分隔线

                doc.Rect.String = "52 12 568 53";
                doc.HPos = 0;
                doc.VPos = 0;
                doc.TextStyle.Italic = true;
                doc.AddHtml(" <font color=\"#666666\">" + customName + "</font>");
                doc.TextStyle.Italic = false;

                doc.Rect.String = "52 12 568 53";
                doc.HPos = 1.0;
                doc.VPos = 0;
                doc.Color.String = "0 0 0"; //黑色
                doc.AddHtml("page " + i.ToString() + "/" + doc.PageCount.ToString());
                doc.Color.String = "0 0 0"; 
                doc.AddLine(52, 55, 568, 55);//画一条页脚分隔线
                doc.Color.String = "0 0 0"; 
            }
        }

        private static int CalculatePageNum(Doc doc,int top, out double offset, int minusOffset) 
        {
            int topPt = top * 3 / 4;
            int pageNum = (int)Math.Ceiling((double)topPt / doc.Rect.Height);
            offset = doc.Rect.Height - (double)topPt % doc.Rect.Height - minusOffset;

            return pageNum == 0 ? 1 : pageNum;
        }

        /// <summary>
        /// 获取abcpdf的bookmark层级目录的路径，需要查看ABCpdf的Api文档
        /// </summary>
        /// <param name="pageList"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        static string GetPath(List<PdfDoc> pageList, PdfDoc page)
        {
            foreach (PdfDoc p in pageList)
            {
                if (p.Id == page.NodePid)
                {
                    if (page.NodeLevel == 1)
                    {
                        return page.Name + "\\";
                    }
                    return GetPath(pageList, p) + page.Name + "\\";
                }
            }
            return page.Name + "\\";
        }
        /// <summary>
        /// 递归方法，获取当前页面的路径
        /// </summary>
        /// <param name="pageList"></param>
        /// <param name="pageId"></param>
        /// <returns></returns>
        static string GetPath(List<PdfDoc> pageList, int pageId) 
        {
            foreach (PdfDoc p in pageList)
            {
                if (p.Id == pageId)
                    return GetPath(pageList, p);
                
            }
            return string.Empty;
        }
    }
    /// <summary>
    /// 辅助实体类，将被生成PDF页面的对象
    /// </summary>
    public class PdfDoc
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int NodePid { get; set; }
        public int NodeLevel { get; set; }
        public int NodeOrder { get; set; }
        public string NodeKey { get; set; }
        /// <summary>
        /// 是否展开节点
        /// </summary>
        public bool Expended { get; set; }
        public string DocPath { get; set; }
        public string Url { get; set; }
        public string Html { get;set; }
        
    }
    /// <summary>
    /// 记录日志
    /// </summary>
    public class LogError 
    {
        private static object log_locker = new object();//日志锁
        
        public static string ReportErrors(string msgError)
        {
            try
            {
                var ExceptionMessage = string.Format("{2}客户Ip:{0}:发生了如下错误:{1}", GetCustomerIP(), msgError, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                string Dir = HttpContext.Current.Server.MapPath("/UploadFiles/MissionStatement/Logs");
                if (!Directory.Exists(Dir))
                {
                    Directory.CreateDirectory(Dir);

                }

                string Path = Dir + @"\" + DateTime.Now.ToString("yyyy-MM-dd") + ".log";
                lock (log_locker)
                {
                    WriteFile(ExceptionMessage, Path);
                }
            }
            catch (Exception ex)
            {

            }
            return "";

        }

        public static void Message(string msg) 
        {
            try
            {
                var ExceptionMessage = string.Format("{2}客户Ip:{0}:发送了如下消息:{1}", GetCustomerIP(), msg, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                string Dir = HttpContext.Current.Server.MapPath("/UploadFiles/MissionStatement/Logs");
                if (!Directory.Exists(Dir))
                {
                    Directory.CreateDirectory(Dir);

                }

                string Path = Dir + @"\" + DateTime.Now.ToString("yyyy-MM-dd") + ".log";
                lock (log_locker)
                {
                    WriteFile(ExceptionMessage, Path);
                }
            }
            catch (Exception ex)
            {

            }
        }

        public static void Message(string msg, string logPath)
        {
            try
            {
                var ExceptionMessage = string.Format("{2}客户Ip:{0}:发送了如下消息:{1}", GetCustomerIP(), msg, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                string Dir = logPath; ;
                if (!Directory.Exists(Dir))
                {
                    Directory.CreateDirectory(Dir);

                }

                string Path = Dir + @"\" + DateTime.Now.ToString("yyyy-MM-dd") + ".log";
                lock (log_locker)
                {
                    WriteFile(ExceptionMessage, Path);
                }
            }
            catch (Exception ex)
            {

            }
        }

        private static string ReportErrors(string msgError, string Method)
        {
            try
            {
                var ExceptionMessage = string.Format("{2}客户Ip:{0}:发生了如下错误:{1} 方法：{3}", GetCustomerIP(), msgError, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Method);
                string Dir = HttpContext.Current.Server.MapPath("/UploadFiles/MissionStatement/Logs");
                if (!Directory.Exists(Dir))
                {
                    Directory.CreateDirectory(Dir);

                }

                string Path = Dir + @"\" + DateTime.Now.ToString("yyyy-MM-dd") + ".log";
                lock (log_locker)
                {
                    WriteFile(ExceptionMessage, Path);
                }
            }
            catch (Exception ex)
            {

            }
            return "";

        }

        private static void WriteFile(string content, string filename)
        {

            System.IO.StreamWriter sw = null;
            if (File.Exists(filename))
            {
                sw = new System.IO.StreamWriter(filename, true);
            }
            else
            {
                sw = new System.IO.StreamWriter(filename);
            }
            sw.WriteLine(content);
            sw.Close();

        }

        private static string GetCustomerIP()
        {

            string CustomerIP = "";
            if (HttpContext.Current != null && HttpContext.Current.Request != null)
            {
                if (HttpContext.Current.Request.ServerVariables["HTTP_VIA"] != null)
                {
                    if (HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"] != null)
                        CustomerIP = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"].ToString();

                }
                else
                {
                    if (HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"] != null)
                        CustomerIP = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"].ToString();
                }
            }

            return CustomerIP;


        }
    }

}
