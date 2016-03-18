using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;

using MongoDB.Bson;
using Yinhe.ProcessingCenter;


namespace System.Web.Mvc.Html
{
   /// <summary>
   /// Html重载
   /// </summary>
    public static class HtmlExtensions
    {
        /// <summary>
        /// 获取下拉列表中的选择项名称
        /// </summary>
        /// <param name="htmlHelper"></param>
        /// <param name="selectList"></param>
        /// <param name="selectVal"></param>
        /// <returns></returns>
        public static string ShowSelect(this HtmlHelper htmlHelper, IEnumerable<SelectListItem> selectList, object selectVal)
        {
            var result = selectList.FirstOrDefault(s => s.Value == selectVal.ToString());
            return result != null ? result.Text : string.Empty;
        }

        /// <summary>
        /// bson枚举生成selectList对象
        /// </summary>
        /// <param name="htmlHelper"></param>
        /// <param name="bsonList">bson枚举列表</param>
        /// <param name="id">下拉列表Id项的key值</param>
        /// <param name="name">下拉列表name项的key值</param>
        /// <returns></returns>
        public static SelectList BsonSelectList(this HtmlHelper htmlHelper,IEnumerable<BsonDocument> bsonList,string id,string name)
        {
            var objList = bsonList.Select(s => new { id = s.String(id), name = s.String(name) });
            return new SelectList(objList, "id", "name");
        }

        /// <summary>
        /// bson枚举生成selectList对象
        /// </summary>
        /// <param name="htmlHelper"></param>
        /// <param name="bsonList">bson枚举列表</param>
        /// <param name="id">下拉列表Id项的key值</param>
        /// <param name="name">下拉列表name项的key值</param>
        /// <param name="selectValue">选择项</param>
        /// <returns></returns>
        public static SelectList BsonSelectList(this HtmlHelper htmlHelper, IEnumerable<BsonDocument> bsonList, string id, string name,object selectValue)
        {
            var objList = bsonList.Select(s => new { id = s.String(id), name = s.String(name) });
            return new SelectList(objList, "id", "name", selectValue);
        }

        /// <summary>
        /// 产品决策，正式数据展示。正式数据返回格式为金额
        /// </summary>
        /// <param name="htmlHelper"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string DiffDiv(this HtmlHelper htmlHelper, string str)
        {
            decimal value;
            if (!string.IsNullOrEmpty(str) && decimal.TryParse(str, out value))
            {
                return string.Format(@"<div class='D_imp'><label>{0}</label></div>", value.ToMoney());
            }
            else
            {
                return @"<div class='D_imp'><label>&nbsp;</label></div>";
            }
        }

        /// <summary>
        /// 产品决策，正式数据展示。正式数据返回格式为十进制数
        /// </summary>
        /// <param name="htmlHelper"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string DiffDivDecimal(this HtmlHelper htmlHelper, string str)
        {
            decimal value;
            if (!string.IsNullOrEmpty(str) && decimal.TryParse(str, out value))
            {
                return string.Format(@"<div class='D_imp'><label>{0}</label></div>", value);
            }
            else
            {
                return @"<div class='D_imp'><label>&nbsp;</label></div>";
            }
        }

        /// <summary>
        /// 产品决策，正式数据展示。正式数据返回格式为时间
        /// </summary>
        /// <param name="htmlHelper"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string DiffDivDate(this HtmlHelper htmlHelper, string str)
        {
            DateTime value;
            if (!string.IsNullOrEmpty(str) && DateTime.TryParse(str, out value))
            {
                return string.Format(@"<div class='D_imp'><label>{0}</label></div>", value.ToString("yyyy-MM-dd"));
            }
            else
            {
                return @"<div class='D_imp'><label>&nbsp;</label></div>";
            }
        }

        /// <summary>
        /// 情报库、产品定位，正式数据展示。正式数据字符串格式
        /// </summary>
        /// <param name="htmlHelper"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string DiffDivStr(this HtmlHelper htmlHelper, string str)
        {
            if (!string.IsNullOrEmpty(str) )
            {
                return string.Format(@"<div class='D_imp'><label>{0}</label></div>", str);
            }
            else
            {
                return @"<div class='D_imp'><label>&nbsp;</label></div>";
            }
        }
        /// <summary>
        /// 情报库、产品定位，正式数据展示。正式数据字符串格式
        /// </summary>
        /// <param name="htmlHelper"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string DiffDivStr(this HtmlHelper htmlHelper, string str,string classStr)
        {
            if (!string.IsNullOrEmpty(str))
            {
                return string.Format(@"<div class='{1}'><label>{0}</label></div>", str,classStr);
            }
            else
            {
                return string.Format(@"<div class='{0}'><label></label></div>", classStr);
            }
        }
        /// <summary>
        /// 情报库、产品定位，正式数据展示。正式数据字符串textArea格式
        /// </summary>
        /// <param name="htmlHelper"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string DiffDivTextStr(this HtmlHelper htmlHelper, string str)
        {
            if (!string.IsNullOrEmpty(str))
            {
                return string.Format(@"<div class='D_imp'><label>{0}</label></div>", str);
            }
            else
            {
                return @"<div class='D_imp'><label>&nbsp;</label></div>";
            }
        }
        /// <summary>
        /// 情报库、产品定位，正式数据展示。正式数据字符串textArea格式
        /// </summary>
        /// <param name="htmlHelper"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string FormLogHtml(this HtmlHelper htmlHelper, int status,bool verfirm,bool enirty)
        {
            var logHtml = "&nbsp;&nbsp;";
            if (status == 1 && (verfirm || enirty))
            {
                logHtml += "<a class=\"ablock ablue\" href=\"javascript:void(0);\">";
                if (verfirm)
                {
                    logHtml += FormStatus.DSH;
                }
                else
                {
                    logHtml += FormStatus.SHZ;
                }
                logHtml += "</a>&nbsp;&nbsp;";
            }
            logHtml += "<a href=\"javascript:void(0);\" onclick=\"showLog(this)\">";
            logHtml += "<img src=\"";
            logHtml += SysAppConfig.HostDomain;
            logHtml += "/Content/images/icon/icos0059.png\" title=\"数据修改审核日志\" />";
            logHtml += "</a>";
            return logHtml;
        }



    }
}
