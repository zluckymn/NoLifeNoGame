using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.Mvc.Html;


namespace System
{
   /// <summary>
   /// 数据类型重载
   /// </summary>
    public static class TypeExtensions
    {
        /// <summary>
        /// 金额的格式展示,去除小数
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static string ToMoney_Z(this decimal val)
        {
            return val.ToString("#,##");
        }

        /// <summary>
        /// 金额的格式展示，保留两位小数
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static string ToMoney(this decimal val)
        {
            return val.ToString("#,##0.00");
        }

        /// <summary>
        /// 金额的格式展示，保留一位小数
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static string ToMoney_O(this decimal val)
        {
            return val.ToString("#,##0.0");
        }

        /// <summary>
        /// 金额的格式展示，保留两位小数
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static string ToMoney_T(this decimal val)
        {
            return val.ToString("#,##0.00");
        }

        public static string ToRate(this decimal val)
        {
            return val.ToString("#0.##%");
        }
    }
}
