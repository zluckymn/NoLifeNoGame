using System;
using System.Web;
using System.IO;
using System.Collections.Generic;

namespace Yinhe.ProcessingCenter
{
    /// <summary>
    ///  页面请求参数处理类
    /// </summary>
    public class PageReq
    {
        #region 默认构造函数
        /// <summary>
        /// 构造函数
        /// </summary>
        public PageReq()
        {
        }
        #endregion

        
        public static string GetString(string name)
        {
            HttpRequest request  = HttpContext.Current.Request;
            string type = request.RequestType;
            if(string.Compare(type,"GET",true) == 0 && request.QueryString[name] != null)//GET方法
            {
                return request.QueryString[name] ?? string.Empty;
            }
            else if(string.Compare(type,"POST",true) == 0 && request.Form[name] != null) //POST方法
            {
                return request.Form[name] ?? string.Empty;
            }
            return string.Empty;
        }

        public static decimal GetDecimal(string name)
        {
            string str = GetString(name);
            decimal val;
            decimal.TryParse(str,out val);
            return val;
        }

        public static double GetDouble(string name)
        {
            string str = GetString(name);
            double val;
            double.TryParse(str,out val);
            return val;
        }

        public static int GetInt(string name)
        {
            string str = GetString(name);
            int val;
            int.TryParse(str,out val);
            return val;
        }

        public static bool GetBoolean(string name)
        {
            string str = GetString(name);
            bool val;
            bool.TryParse(str, out val);
            return val;
        }



        #region 获取GET方法的参数
        /// <summary>
        /// 获取url参数
        /// </summary>
        /// <param name="paramName">参数名称</param>
        /// <returns></returns>
        public static string GetParam(string paramName)
        {
            if (HttpContext.Current.Request.QueryString[paramName] != null)
            {
                return HttpContext.Current.Request.QueryString[paramName].ToString();
            }
            else
            {
                return "";
            }
        }

      

        /// <summary>
        /// 将字符串转换为int类型，转换失败返回0
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static int ToInt(string str)
        {
            int value;
            if (!int.TryParse(str, out value)) value = 0;
            return value;
        }

        /// <summary>
        /// 将字符串转换为float类型，转换失败返回0.0
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static float ToFloat(string str)
        {
            float value;
            if (!float.TryParse(str, out value)) value = 0.0f;
            return value;
        }

        /// <summary>
        /// 将字符串转换为bool类型，转换失败返回false
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static bool ToBoolean(string str)
        {
            bool value;
            bool.TryParse(str, out value);
            return value;
        }

        /// <summary>
        /// 将字符串转换为decimal类型，转换失败返回0m
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static decimal ToDecimal(string str)
        {
            decimal value;
            if (!decimal.TryParse(str, out value)) value = 0m;
            return value;
        }

        /// <summary>
        /// 获取url浮点型参数
        /// </summary>
        /// <param name="paramName"></param>
        /// <returns></returns>
        public static decimal GetParamDecimal(string paramName)
        {
            string strParam = GetParam(paramName);
            return ToDecimal(strParam);
        }

        /// <summary>
        /// 获取布尔型的参数
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool GetParamBoolean(string key)
        {
            string str = GetParam(key);
            return ToBoolean(str);
        }

        /// <summary>
        /// 获取url整型参数
        /// </summary>
        /// <param name="paramName">参数名称</param>
        /// <returns></returns>
        public static int GetParamInt(string paramName)
        {
            string strParam = GetParam(paramName);
            return ToInt(strParam);
        }

        /// <summary>
        /// 获取url参数列表,参数值以逗号分隔
        /// </summary>
        /// <param name="paramName"></param>
        /// <returns></returns>
        public static string[] GetParamList(string paramName)
        {
            string paramValue = GetParam(paramName);
            string[] result = new string[0];
            if (!string.IsNullOrEmpty(paramValue))
            {
                result = paramValue.Split(',');
            }
            return result;
        }

        /// <summary>
        /// 获取url浮点型参数列表,参数值以逗号分隔
        /// </summary>
        /// <param name="paramName"></param>
        /// <returns></returns>
        public static decimal[] GetParamDecimalList(string paramName)
        {
            string[] items = GetParamList(paramName);

            List<decimal> list = new List<decimal>();
            foreach (string s in items)
            {
                decimal theItem = decimal.Zero;
                decimal.TryParse(s, out theItem);
                list.Add(theItem);
            }

            return list.ToArray();
        }

        /// <summary>
        /// 获取url整型参数列表,参数值以逗号分隔
        /// </summary>
        /// <param name="paramName"></param>
        /// <returns></returns>
        public static int[] GetParamIntList(string paramName)
        {
            string[] items = GetParamList(paramName);

            List<int> list = new List<int>();
            foreach (string s in items)
            {
                if (!string.IsNullOrEmpty(s))
                {
                    int theItem = -1;
                    int.TryParse(s, out theItem);
                    list.Add(theItem);
                }
            }

            return list.ToArray();
        }

        #endregion

        #region 获取POST方式的参数
        /// <summary>
        /// 获取Post提交的参数
        /// </summary>
        /// <param name="paramName">参数名称</param>
        /// <returns></returns>
        public static string GetForm(string paramName)
        {
            if (HttpContext.Current.Request.Form[paramName] != null)
            {
                return HttpContext.Current.Request.Form[paramName].ToString();
            }
            else
            {
                return "";
            }
        }

        /// <summary>
        /// 获取Post提交的浮点型参数
        /// </summary>
        /// <param name="paramName"></param>
        /// <returns></returns>
        public static decimal GetFormDecimal(string paramName)
        {
            string strParam = GetForm(paramName);
            return ToDecimal(strParam);
        }

        /// <summary>
        /// 获取Post提交的整型参数
        /// </summary>
        /// <param name="paramName">参数名称</param>
        /// <returns></returns>
        public static int GetFormInt(string paramName)
        {
            string strParam = GetForm(paramName);
            return ToInt(strParam);
        }

        /// <summary>
        /// 获取Post提交的整型参数
        /// </summary>
        /// <param name="paramName">参数名称</param>
        /// <returns></returns>
        public static bool GetFormBoolean(string paramName)
        {
            string strParam = GetForm(paramName);
            return ToBoolean(strParam);
        }

        /// <summary>
        /// 获取Post提交的参数列表,参数以逗号隔开
        /// </summary>
        /// <param name="paramName"></param>
        /// <returns></returns>
        public static string[] GetFormList(string paramName)
        {
            string paramValue = GetForm(paramName);
            string[] result = new string[0];
            if (!string.IsNullOrEmpty(paramValue))
            {
                result = paramValue.Split(',');
            }
            return result;
        }

        /// <summary>
        /// 获取Post提交的浮点型参数列表,参数以逗号隔开
        /// </summary>
        /// <param name="paramName"></param>
        /// <returns></returns>
        public static decimal[] GetFormDecimalList(string paramName)
        {
            string[] items = GetFormList(paramName);

            List<decimal> list = new List<decimal>();
            foreach (string s in items)
            {
                decimal theItem = decimal.Zero;
                decimal.TryParse(s, out theItem);
                list.Add(theItem);
            }

            return list.ToArray();
        }

        /// <summary>
        /// 获取Post提交的整型参数列表,参数以逗号隔开
        /// </summary>
        /// <param name="paramName"></param>
        /// <returns></returns>
        public static int[] GetFormIntList(string paramName)
        {
            string[] items = GetFormList(paramName);

            List<int> list = new List<int>();
            foreach (string s in items)
            {
                if (!string.IsNullOrEmpty(s))
                {
                    int theItem = -1;
                    int.TryParse(s, out theItem);
                    list.Add(theItem);
                }
            }

            return list.ToArray();
        }

        #endregion

        #region Session相关操作
        /// <summary>
        /// 获取对应关键字,Session的值,无此关键字,返回空
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string GetSession(string key)
        {
            if (HttpContext.Current.Session[key] != null)
            {
                return HttpContext.Current.Session[key].ToString();
            }
            else
            {
                return "";
            }
        }

       

        /// <summary>
        /// 将值保存到Session中去
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void SetSession(string key, string value)
        {
            HttpContext.Current.Session[key] = value;
        }

        public static List<List<string[]>> GetSessionObj(string key)
        {
            if (HttpContext.Current.Session[key] != null)
            {
                return (List<List<string[]>>)HttpContext.Current.Session[key];
            }
            else
            {
                return null;
            }
        }

         

        public static void SetSessionObj(string key, List<List<string[]>> value)
        {
            HttpContext.Current.Session[key] = value;
        }

        /// <summary>
        /// 获取对应关键字,Session的值,无此关键字,返回null
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static object GetObjSession(string key)
        {
            if (HttpContext.Current.Session[key] != null)
            {
                return HttpContext.Current.Session[key];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 将值保存到Session中去
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void SetObjSession(string key, object value)
        {
            HttpContext.Current.Session[key] = value;
        }

        #endregion

        #region 文件相关操作
        /// <summary>
        /// 以文件相对路径获取其绝对路径
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static string GetPhyPath(string filePath)
        {
            return HttpContext.Current.Server.MapPath(filePath);
        }

        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="clientFileName">保存到客户端的文件名称</param>
        /// <param name="filePath">下载的文件物理路径</param>
        public static void DownLoadFile(string clientFileName, string filePath)
        {
            FileInfo fi = new FileInfo(filePath);
            if (string.IsNullOrEmpty(clientFileName))
            {
                clientFileName = fi.Name;
            }
            clientFileName = HttpUtility.UrlEncode(clientFileName, System.Text.Encoding.UTF8);
            clientFileName = clientFileName.Replace("+", "%20");
            if (!fi.Exists)
            {
                HttpContext.Current.Response.Write("下载的文档不存在，或者已经被删除。");
                HttpContext.Current.Response.End();
                return;
            }

            HttpContext.Current.Response.Clear();
            HttpContext.Current.Response.ClearHeaders();
            HttpContext.Current.Response.Buffer = false;
            HttpContext.Current.Response.AppendHeader("Content-Disposition", "attachment;filename=" + clientFileName);

            HttpContext.Current.Response.AppendHeader("Content-Length", fi.Length.ToString());
            HttpContext.Current.Response.ContentType = "application/octet-stream";
            HttpContext.Current.Response.WriteFile(filePath);
            HttpContext.Current.Response.Flush();
            HttpContext.Current.Response.End();
        }
        #endregion

        #region Cookie相关操作
        /// <summary>
        /// 读取 cookie值
        /// </summary>
        /// <param name="strKey"></param>
        /// <returns></returns>
        public static string GetCookieValue(string cookieKey)
        {
            HttpCookie cookie = HttpContext.Current.Request.Cookies["Yinhoo"];
            string cookieValue = null;
            if (cookie != null)
            {
                if (cookie.Values[cookieKey] != null)
                {
                    cookieValue = cookie.Values[cookieKey].ToString();
                }
            }
            return cookieValue;
        }

        /// <summary>
        /// 读取 cookie值
        /// </summary>
        /// <param name="strKey"></param>
        /// <returns></returns>
        public static int GetCookieValueInt(string cookieKey)
        {
            HttpCookie cookie = HttpContext.Current.Request.Cookies["Yinhoo"];
            int cookieValueInt = 0;
            if (cookie != null)
            {
                if (cookie.Values[cookieKey] != null)
                {
                    cookieValueInt = int.Parse(cookie.Values[cookieKey].ToString());
                }
            }
            return cookieValueInt;
        }

        /// <summary>
        /// 设置cookie
        /// </summary>
        /// <param name="cookieKey"></param>
        /// <param name="cookieValue"></param>
        /// <param name="expiresDt"></param>
        public static void SetCookie(string cookieKey, string cookieValue, DateTime expiresDt)
        {
            HttpCookie cookie = HttpContext.Current.Request.Cookies["Yinhoo"];
            if (cookie == null)
            {
                cookie = new HttpCookie("Yinhoo");
            }

            cookie.Values[cookieKey] = cookieValue;
            cookie.Expires = expiresDt;

            HttpContext.Current.Response.Cookies.Add(cookie);
        }
        #endregion

       



    }
}
