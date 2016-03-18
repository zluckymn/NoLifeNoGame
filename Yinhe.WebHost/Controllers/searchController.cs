using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Yinhe.ProcessingCenter;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using Yinhe.ProcessingCenter.Document;

namespace Yinhe.WebHost.Controllers
{
    public class searchController : Yinhe.ProcessingCenter.ControllerBase
    {
        //
        // GET: /search/

        public ActionResult Index()
        {
            return View();
        }
        public ActionResult QRCodeViewFile()
        {
            return View();
        }
        public ActionResult FLCodeViewFile()
        {
            return View();
        }

        public ActionResult FLFileDetail()
        {
            return View();
        }

        public string addViewFile()
        {
            string fileId = PageReq.GetParam("fileId");
            DataOperation dataOp = new DataOperation();
            dataOp.Insert("HDSpecFileView", "fileId=" + fileId + "&isView=0");
            return "ok";
        }

        public string ChangeFileStatus()
        {
            string fileId = PageReq.GetParam("fileId");
            DataOperation dataOp = new DataOperation();
            string updateQuery = "db.FileLibrary.findOne({'fileId':'" + fileId+ "'})";
            dataOp.Update("FileLibrary", updateQuery, "confirmed=1");
            return "ok";
        }

        public string findIileToView()
        {
            DataOperation dataOp = new DataOperation();
            BsonDocument file = dataOp.FindOneByKeyVal("HDSpecFileView", "isView", "0");
            if (file != null)
            {
                file = dataOp.FindOneByKeyVal("FileLibrary", "fileId", file.Text("fileId"));
                string query = "db.HDSpecFileView.findOne({'fileId':'" + file.Text("fileId") + "'});";
                dataOp.Delete("HDSpecFileView", query);
                return FileCommonOperation.GetClientOnlineRead(file);
            }
            else {
                return "false";
            }
        }

    }
}
