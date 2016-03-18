using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System.Data.SqlClient;
//using Ci.Log;

namespace Yinhe.ProcessingCenter.SynAD
{
    /// <summary>
    /// AD数据获取处理
    /// </summary>
    public class SQLGetDataList : IGetDataList
    {
        private CommonLog _log = new CommonLog();

        //private string _connStr;
        //private string _commandText;

        //public SQLGetDataList(string connStr, string commandText)
        //{
        //    _connStr = connStr;
        //    _commandText = commandText;
        //}


        /// <summary>
        /// 获取bson数据列表
        /// </summary>
        /// <param name="connStr"></param>
        /// <param name="commandText"></param>
        /// <returns></returns>
        public List<BsonDocument> GetBsonDocumentDataList(string connStr, string commandText)
        {
            List<BsonDocument> list = new List<BsonDocument>();
            SqlDataReader dr = null;
            //string guid = Guid.NewGuid().ToString();
            try
            {
                //Data Source=192.168.1.250;Initial Catalog=CI;Integrated Security=True
                List<string> colList = new List<string>();
                dr = SqlHelper.ExecuteReader(connStr, System.Data.CommandType.Text, commandText);
                while (dr.Read())
                {
                    BsonDocument doc = new BsonDocument();
                    if (colList.Count == 0)
                    {
                        for (int i = 0; i < dr.FieldCount; i++)
                        {
                            string colName = dr.GetName(i);
                            if (!colList.Contains(colName))
                            {
                                colList.Add(colName);
                                //Console.WriteLine(colName);
                            }
                        }
                    }
                    foreach (var item in colList)
                    {
                        doc.Add(item, dr[item].ToString());
                        //Console.WriteLine(dr[item].ToString());
                        //_log.Info(dr[item].ToString());
                    }
                   // doc.Add("guid", guid);
                   // doc.Add("sqlCommand", commandText);

                    list.Add(doc);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
            finally
            {
                if (dr != null)
                {
                    dr.Dispose();
                    dr.Close();
                }
            }
            return list;

        }


        /// <summary>
        /// 清空数据库
        /// </summary>
        /// <param name="connStr"></param>
        /// <param name="commandText"></param>
        /// <returns></returns>
        public void DeleteData(string connStr, string commandText)
        {
            SqlDataReader dr = null;
            try
            {
                dr = SqlHelper.ExecuteReader(connStr, System.Data.CommandType.Text, commandText);
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
            finally
            {
                if (dr != null)
                {
                    dr.Dispose();
                    dr.Close();
                }
            }

        }


        /// <summary>
        /// 插入数据
        /// </summary>
        /// <param name="connStr"></param>
        /// <param name="commandText"></param>
        /// <returns></returns>
        public void InsertData(string connStr, string commandText)
        {
            SqlDataReader dr = null;
            try
            {
                dr = SqlHelper.ExecuteReader(connStr, System.Data.CommandType.Text, commandText);
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
            finally
            {
                if (dr != null)
                {
                    dr.Dispose();
                    dr.Close();
                }
            }

        }
    }
}
