using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System.IO;
using System.Web.Script.Serialization;

///<summary>
///A3文档Document相关
///</summary>
namespace Yinhe.ProcessingCenter.Document
{
    /// <summary>
    /// 文件通用处理类
    /// </summary>
    public class FileCommonOperation
    {
        private static DataOperation _dataOp = new DataOperation();

        #region 缩略图操作

       /// <summary>
        ///  获取缩略图
       /// </summary>
       /// <param name="doc"></param>
       /// <param name="spec"></param>
       /// <returns></returns>
        public static string GetThumbImg(int fileId, string spec)
        {
            var doc = _dataOp.FindOneByKeyVal("FileLibrary", "fileId", fileId.ToString());
            return doc!=null?GetThumbImg(doc, spec):"";
        }

        /// <summary>
        /// 获取缩略图
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="spec"></param>
        /// <returns></returns>
        public static string GetThumbImg(BsonDocument doc, string spec)
        {
            return StringReplace(doc, "m.jpg", string.Format("{0}.jpg", spec));
        }

        #endregion

        /// <summary>
        /// 字符串替换
        /// </summary>
        /// <param name="fileId">文件ID</param>
        /// <param name="sourSring">原字符串</param>
        /// <param name="replaceString">替换字符串</param>
        /// <returns></returns>
        public static string StringReplace(BsonDocument doc, string sourSring, string replaceString)
        {
            string retStr = string.Empty;
            if (doc != null)
            {
                retStr = doc.Text("thumbPicPath");
                if (!string.IsNullOrEmpty(replaceString))
                {
                    retStr = StringReplace(retStr,sourSring, replaceString);
                }
            }

            return retStr;
        }

        /// <summary>
        /// 在线查看方法
        /// </summary>
        /// <param name="fileId"></param>
        /// <returns></returns>r
        public static string GetClientOnlineRead(int fileId)
        {
           var doc = _dataOp.FindOneByKeyVal("FileLibrary", "fileId", fileId.ToString());
           return doc!=null?GetClientOnlineRead(doc):"";
        }

        /// <summary>
        /// 在线查看方法
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public static string GetClientOnlineRead(BsonDocument doc)
        {
            string jsEvent = string.Empty;
            string thumbPicPath = doc.Text("thumbPicPath");
            string swfPath = string.IsNullOrEmpty(thumbPicPath) == false ? thumbPicPath.Replace("_m.jpg", "_sup.swf") : string.Empty;
            string supPath = string.IsNullOrEmpty(thumbPicPath) == false ? thumbPicPath.Replace("_m.", "_sup.") : string.Empty;
            jsEvent = "cwf.readOnline({\"guid\":\"" + doc.Text("guid") + "\""
            + ", \"name\":\"" + doc.Text("name") + "\""
            + ", \"ext\":\"" + doc.Text("ext") + "\""
            + ", \"id\":" + doc.Int("fileId")
            + ", \"type\":0"
            + ",\"ver\":" + doc.Int("version")
            + ", \"swfUrl\":\"" + swfPath + "\""
            + ",\"imgUrl\":\"" + supPath + "\"});";

            return jsEvent;
        }

        /// <summary>
        /// 下载方法
        /// </summary>
        /// <returns></returns>
        public static string GetClientDownLoad(int fileId)
        {
            var doc = _dataOp.FindOneByKeyVal("FileLibrary", "fileId", fileId.ToString());
            return doc!=null?GetClientDownLoad(doc):"";
        }

        /// <summary>
        /// 下载方法
        /// </summary>
        /// <returns></returns>
        public static string GetClientDownLoad(BsonDocument doc)
        {
            string jsEvent = string.Empty;
            jsEvent = string.Format("cwf.downLoad(\"{0}\",\"{1}{3}\",\"{2}\",0)", doc.Text("guid"), doc.Text("name"), doc.Int("fileId"), doc.Text("ext"));
            return jsEvent;
        }

        #region 公共操作
        public static string StringReplace(string tobeReplace,string sourSring, string replaceString)
        {
            string retStr = string.Empty;
            retStr = tobeReplace.Replace(sourSring, replaceString);
            return retStr;
        }
        #endregion

    }
}
