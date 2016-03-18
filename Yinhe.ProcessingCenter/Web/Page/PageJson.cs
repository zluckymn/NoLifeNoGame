using System;
using System.Data;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace Yinhe.ProcessingCenter
{
    /// <summary>
    /// 提交处理结果返回信息
    /// </summary>
    public class PageJson
    {
        #region 私有变量
        private StringBuilder sbResult;
        #endregion

        #region  公共变量
        /// <summary>
        /// 返回状态
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 返回错误信息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 文件信息
        /// </summary>
        public string FileInfo { get; set; }
        /// <summary>
        /// 附加信息的键值对表格
        /// </summary>
        public Hashtable htInfo;
        #endregion

        #region 构造函数
        /// <summary>
        /// 默认构造函数
        /// </summary>
        public PageJson()
        {
            sbResult = new StringBuilder();
        }

        /// <summary>
        /// 附带状态构造函数
        /// </summary>
        /// <param name="isSuccess"></param>
        public PageJson(bool isSuccess)
        {
            sbResult = new StringBuilder();

            this.Success = isSuccess;
        }

        /// <summary>
        /// 附带状态/错误信息构造函数
        /// </summary>
        /// <param name="isSuccess"></param>
        /// <param name="errMsg"></param>
        public PageJson(bool isSuccess, string msg)
        {
            sbResult = new StringBuilder();

            this.Success = isSuccess;
            this.Message = msg;
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 添加额外信息
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void AddInfo(string key, string value)
        {
            if (this.htInfo == null)
            {
                this.htInfo = new Hashtable();
            }
            htInfo.Add(key, value);
        }

        /// <summary>
        /// 返回标准Json字符串
        /// </summary>
        /// <returns></returns>
        public string ToJsonString()
        {
            string isTrue = (Success == true ? "true" : "false");
            string str = string.Format("\"Success\":{0},\"Message\":\"{1}\"", isTrue, Message);
            str = "{" + str + "}";
            return str;
        }

        /// <summary>
        /// 构建返回的JSON字符串
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            sbResult.Append("{");
            sbResult.Append(" success:");
            if (this.Success)
            {
                sbResult.Append("true,");
                sbResult.Append("errors:");
                sbResult.Append("{msg:\"ok\"}");
            }
            else
            {
                sbResult.Append("false,");
                sbResult.Append("errors:");
                sbResult.Append("{msg:\"" + this.Message + "\"}");
            }
            sbResult.Append(this.GetOtherInfo());
            sbResult.Append("}");
            return sbResult.ToString();
        }

        /// <summary>
        /// 传值信息
        /// </summary>
        /// <returns></returns>
        public string ToTransmitString()
        {
            sbResult.Append("{");
            if (this.htInfo != null)
            {
                IDictionaryEnumerator infotor = htInfo.GetEnumerator();
                while (infotor.MoveNext())
                {
                    sbResult.Append(infotor.Key.ToString());
                    sbResult.Append(":");
                    sbResult.AppendFormat("\"{0}\"", infotor.Value.ToString());
                    sbResult.Append(",");
                }

                sbResult.Remove(sbResult.Length - 1, 1);
            }
            sbResult.Append("}");
            return sbResult.ToString();
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 获取额外信息
        /// </summary>
        /// <returns></returns>
        private string GetOtherInfo()
        {
            StringBuilder sbInfo = new StringBuilder();
            if (this.htInfo != null)
            {
                IDictionaryEnumerator infotor = htInfo.GetEnumerator();
                while (infotor.MoveNext())
                {
                    sbInfo.AppendFormat(",");
                    sbInfo.Append(infotor.Key.ToString());
                    sbInfo.Append(":");
                    sbInfo.AppendFormat("\"{0}\"", infotor.Value.ToString());
                }

            }
            return sbInfo.ToString();
        }
        #endregion
    }
}
