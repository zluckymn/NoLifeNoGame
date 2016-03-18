using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Collections.Specialized;
using System.Web.Script.Serialization;
using System.Web.UI.HtmlControls;
using MongoDB.Bson;
using Yinhe.ProcessingCenter;
using MongoDB.Driver.Builders;
using MongoDB.Driver;

namespace Yinhe.WebHost.Controllers
{
    public class LogReportController : Yinhe.ProcessingCenter.ControllerBase
    {
        //
        // GET: /LogReport/

        public ActionResult Index()
        {
            return View();
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SaveCommenInfoTest(MutualData main, List<MutualData> subList)
        {
            JavaScriptSerializer jsonSerializer = new JavaScriptSerializer();


            string abc = jsonSerializer.Serialize(HttpContext.Request.Form);

            string dddd = "{ \"name\" : \"123\", \"hijs\" : [{ \"id\" : 0, \"type\" : 1 }, { \"id\" : 1, \"type\" : 2 }, { \"id\" : 2, \"type\" : 3 }, { \"id\" : 3, \"type\" : 4 }] } ";

            string ccc = "{ 'name' : '123', 'hijs' : [{ 'id' : 0, 'type' : 1 }, { 'id' : 1, 'type' : 2 }, { 'id' : 2, 'type' : 3 }, { 'id' : 3, 'type' : 4 }] } ";

            MongoDB.Bson.IO.BsonReader bsonReader = MongoDB.Bson.IO.BsonReader.Create(ccc);

            BsonDocument tempBson = BsonDocument.ReadFrom(bsonReader);

            MongoOperation monOp = new MongoOperation();

            monOp.Save("TestRelation", tempBson);



            return View();
        }

        public ActionResult XHLogIndex()
        {
            return View();
        }

        public ActionResult LogInfoList()
        {
            string stStr = PageReq.GetParam("stStr");       //日志开始时间
            string edStr = PageReq.GetParam("edStr");       //日志结束时间

            DateTime stTime, edTime;

            var stQuery = Query.Null;
            var edQuery = Query.Null;

            if (DateTime.TryParse(stStr, out stTime)) stQuery = Query.GTE("timeSort", stTime.ToString("yyyyMMddHHmmss"));
            if (DateTime.TryParse(edStr, out edTime)) edQuery = Query.LTE("timeSort", edTime.ToString("yyyyMMddHHmmss"));

            var logQuery = dataOp.FindAllByQuery("SysBehaviorLog", Query.And(stQuery, edQuery));

            List<BsonDocument> logList = logQuery.OrderByDescending(t => t.String("timeSort")).ToList();

            string logUserId = PageReq.GetParam("userId").TrimEnd(',');       //日志用户Id

            var userQuery = Query.Null;

            if (logUserId != "" && logUserId != "0") userQuery = Query.Or(Query.EQ("logUserId", logUserId.ToString()), Query.EQ("userId", logUserId));

            logQuery = dataOp.FindAllByQuery("SysBehaviorLog", Query.And(stQuery, edQuery, userQuery));

            logList = logQuery.OrderByDescending(t => t.String("timeSort")).ToList();

            string resolveIds = PageReq.GetParam("resolveIds");     //获取需要解析的类型Id列表

            List<string> resolveIdList = resolveIds.SplitParam(",").ToList();

            List<BsonDocument> resolveList = new List<BsonDocument>();

            if (resolveIdList.Count > 0)
            {
                resolveList = dataOp.FindAllByKeyValList("SysLogResolve", "resolveId", resolveIdList).ToList();  //所有解析类型
            }
            else
            {
                resolveList = dataOp.FindAll("SysLogResolve").ToList();
            }

            var resolveQuery = Query.Null;

            foreach (var tempResolve in resolveList)
            {
                QueryDocument tempQuery = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<QueryDocument>(tempResolve.String("query"));
                resolveQuery = Query.Or(resolveQuery, tempQuery);
            }

            logQuery = dataOp.FindAllByQuery("SysBehaviorLog", Query.And(stQuery, edQuery, userQuery, resolveQuery));

            logList = logQuery.OrderByDescending(t => t.String("timeSort")).ToList();

            return View();
        }
    }
}
