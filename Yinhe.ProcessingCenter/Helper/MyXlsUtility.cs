using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Web;

using org.in2bits.MyXls;

namespace Yinhe.ProcessingCenter
{
    public class MyXlsUtility
    {
        /// <summary>
        /// 将生成的Excel表格发送到客户端
        /// </summary>
        /// <param name="xls">Excel文档</param>
        /// <param name="fileName">文件名</param>
        public static void ExportByWeb(XlsDocument xlsDoc, string fileName)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                HttpContext context = HttpContext.Current;
                context.Response.ContentType = "application/vnd.ms-excel";
                context.Response.ContentEncoding = Encoding.UTF8;
                context.Response.Charset = "";
                context.Response.AppendHeader("Content-Disposition", "attachment;filename=" + HttpUtility.UrlEncode(fileName, Encoding.UTF8) + ".xls");
                xlsDoc.Save(ms);
                ms.Flush();
                ms.Position = 0;
                context.Response.BinaryWrite(ms.GetBuffer());
                context.Response.End();
            }
        }
    }
}
