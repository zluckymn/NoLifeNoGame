using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace Yinhe.ProcessingCenter.SynAD
{
    /// <summary>
    /// 旭辉手机同步
    /// </summary>
   public class XHMobileSynchronization
    {
        private CommonLog _log = new CommonLog();

        private string _connString = "Data Source=192.168.8.26;Initial Catalog=ekp;User ID=ekpyh;Password=ekpyh;";

        public List<BsonDocument> GetDataList()
        {
            List<BsonDocument> list = new List<BsonDocument>();
            SQLGetDataList bll = new SQLGetDataList();
            try
            {
                list = bll.GetBsonDocumentDataList(_connString, "select * from person_mobile_no where fd_mobile_no is not null and fd_mobile_no <>''");
               
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
            return list;
        }


        public InvokeResult MobileSyn(List<BsonDocument> list)
        {
            InvokeResult result = new InvokeResult();
            DataOperation dataOp = new DataOperation();
            try
            {
                foreach (var item in list)
                {
                    string  loginName = item.Text("fd_login_name");
                    var user = dataOp.FindOneByKeyVal("SysUser", "loginName", loginName);
                    if (user != null)
                    {
                        Console.WriteLine(string.Format("{0} {1}", loginName, item.Text("fd_mobile_no")));
                        BsonDocument doc = new BsonDocument();
                        doc.Add("mobileNumber", item.Text("fd_mobile_no"));
                        var query = Query.EQ("loginName", loginName);
                        result = dataOp.Update("SysUser", query, doc);
                    }
                    else
                    {
                        Console.WriteLine("用户不存在");
                    }
                }
            }
            catch (Exception ex)
            {
                result.Status = Status.Failed;
                result.Message = ex.Message;
                _log.Error(ex.Message);
            }

            return result;
        }
    }
}
