using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Collections.Specialized;
using MongoDB.Bson;
using System.Web;
using Yinhe.ProcessingCenter.MvcFilters;
using Yinhe.ProcessingCenter.Document;
using System.Text.RegularExpressions;
using Yinhe.ProcessingCenter.DataRule;
using System.Web.Security;
using MongoDB.Driver.Builders;
using System.Collections;
using Yinhe.ProcessingCenter.Permissions;
using System.IO;
using MongoDB.Driver;
using MongoDB.Bson.IO;
using Yinhe.ProcessingCenter.Common;
using Yinhe.WebReference.Schdeuler;
using System.Transactions;
using System.Data.SqlClient;
using System.Data; 
namespace Yinhe.ProcessingCenter
{
    /// <summary>
    /// MVC Controller的AD同步通用基类
    /// </summary>
    public partial class ControllerBase : Controller
    {

        /// <summary>
        /// 连接数据库
        /// </summary>
        /// <returns></returns>
        public static SqlConnection GetCon()
        {
            SqlConnection cn = new SqlConnection("server=192.168.1.200;uid = sa;pwd =dba;database=Autolink2.0");
            
            return cn; 
        
        }
        /// <summary>
        /// 获取DataSet数据集
        /// </summary>
        /// <param name="sqlStr">查询语句</param>
        /// <param name="tableName">表名</param>
        /// <returns></returns>
        public static DataSet GetData(string sqlStr,string tableName) {
            SqlConnection cn1 = GetCon();
            cn1.Open();
            SqlDataAdapter ada = new SqlDataAdapter(sqlStr, cn1);
            DataSet ds = new DataSet();
            ada.Fill(ds, tableName);
            cn1.Close();
            return ds;  
        }
        /// <summary>
        /// 操作数据，插入Mongo中
        /// </summary>
        public void DataOperate() {
            string a = "select * from SysUser";
            DataSet a1 = GetData(a,"SysUser");
            DataTable b1=a1.Tables["SysUser"];
            DataRow c1=b1.Rows[0];
            string d=c1["userId"].ToString();

        }



    }

    


}
