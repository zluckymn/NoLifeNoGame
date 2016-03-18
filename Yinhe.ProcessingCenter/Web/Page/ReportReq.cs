using System;
using System.Web;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace Yinhe.ProcessingCenter
{
    /// <summary>
    /// 报表参数处理类
    /// </summary>
    public class ReportReq
    {
        #region 默认构造函数
        /// <summary>
        /// 构造函数
        /// </summary>
        public ReportReq()
        {
        }
        #endregion



        /// <summary>
        /// 获取设计报表的相关参数
        /// </summary>
        /// <param name="report">报表实体</param>
        /// <returns></returns>
        public  static Dictionary<string, string> GetDesignParam()
        {
            int width = PageReq.GetParamInt("width");
            int height = PageReq.GetParamInt("height");
            string template = PageReq.GetParam("template");
            string chartAreaColorType = PageReq.GetParam("chartAreaColorType");
            string title = PageReq.GetParam("title");
            string reportTypeValueType = PageReq.GetParam("reportTypeValueType");
            bool isExplodedAllPoint = PageReq.GetParamBoolean("isExplodedAllPoint");
            int explodedDistance = PageReq.GetParamInt("explodedDistance");
            string axisYUnit = PageReq.GetParam("axisYUnit");
            Dictionary<string, string> dicParam = new Dictionary<string, string>();
            if (width != 0 && height != 0)
            {
                if (dicParam.ContainsKey("width") == true)
                {
                    dicParam["width"] = width.ToString();
                }
                else
                {
                    dicParam.Add("width", width.ToString());
                }
                if (dicParam.ContainsKey("height") == true)
                {
                    dicParam["height"] = width.ToString();
                }
                else
                {
                    dicParam.Add("height", height.ToString());
                }

            }
            //添加template
            if (!string.IsNullOrEmpty(template))
            {
                if (dicParam.ContainsKey("template") == true)
                {
                    dicParam["template"] = template.ToString();
                }
                else
                {
                    dicParam.Add("template", template.ToString());
                }
            }

            //添加chartarea类型
            if (!string.IsNullOrEmpty(chartAreaColorType))
            {
                if (dicParam.ContainsKey("chartAreaColorType") == true)
                {
                    dicParam["chartAreaColorType"] = chartAreaColorType.ToString();
                }
                else
                {
                    dicParam.Add("chartAreaColorType", chartAreaColorType.ToString());
                }
            }



            //添加chartarea类型
            if (!string.IsNullOrEmpty(title))
            {
                if (dicParam.ContainsKey("title") == true)
                {
                    dicParam["title"] = chartAreaColorType.ToString();
                }
                else
                {
                    dicParam.Add("title", chartAreaColorType.ToString());
                }
            }
            //添加chartarea类型
            if (!string.IsNullOrEmpty(reportTypeValueType))
            {
                if (dicParam.ContainsKey("reportTypeValueType") == true)
                {
                    dicParam["reportTypeValueType"] = chartAreaColorType.ToString();
                }
                else
                {
                    dicParam.Add("reportTypeValueType", chartAreaColorType.ToString());
                }
            }

            if (false != isExplodedAllPoint)
            {
                if (!dicParam.ContainsKey("isExplodedAllPoint"))
                {
                    dicParam.Add("isExplodedAllPoint", isExplodedAllPoint.ToString());
                }
                else
                {
                    dicParam["isExplodedAllPoint"] = isExplodedAllPoint.ToString();
                }
            }

            if (0 != explodedDistance)
            {
                if (!dicParam.ContainsKey("explodedDistance"))
                {
                    dicParam.Add("explodedDistance", explodedDistance.ToString());
                }
                else
                {
                    dicParam["explodedDistance"] = explodedDistance.ToString();
                }
            }

            if (string.IsNullOrEmpty(axisYUnit))
            {
                if (!dicParam.ContainsKey("axisYUnit"))
                {
                    dicParam.Add("axisYUnit", axisYUnit);
                }
                else
                {
                    dicParam["axisYUnit"] = axisYUnit;
                }
            }
            return dicParam;
        }


        public static string GetDateFilterCondition()
        {
            string startStrDate = PageReq.GetParam("st");
            string endStrEnd = PageReq.GetParam("ed");
            string fieldName = PageReq.GetParam("fld");
            StringBuilder sbFilter = new StringBuilder();
            if (String.IsNullOrEmpty(fieldName) == true)
            {
                return sbFilter.ToString();
            }
            if (String.IsNullOrEmpty(startStrDate) == false)
            {
                sbFilter.AppendFormat("{0}>='{1}'", fieldName, startStrDate);
            }
            if (String.IsNullOrEmpty(endStrEnd) == false)
            {
                if (sbFilter.Length > 0)
                {
                    sbFilter.Append(" and ");
                }
                sbFilter.AppendFormat("{0}<='{1}'", fieldName, endStrEnd);
            }

            string strWhere = PageReq.GetParam("whr");
            var dateFilter = sbFilter.ToString();
            if (string.IsNullOrEmpty(dateFilter) == false)
            {
                if (string.IsNullOrEmpty(strWhere) == false)
                {
                    strWhere = " and " + dateFilter;
                }
                else
                {
                    strWhere = dateFilter;
                }
            }
            return strWhere;
        }

    }
}
