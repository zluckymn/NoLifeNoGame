using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Yinhe.ProcessingCenter;
using System.IO;
using Microsoft.Build.Evaluation;
using MongoDB.Bson;
using System.Transactions;
using MongoDB.Bson.IO;
using Yinhe.ProcessingCenter.DataRule;
using MongoDB.Driver.Builders;
using System.Collections;
using System.Text;
using System.DirectoryServices;
using MongoDB.Driver;

namespace Yinhe.WebHost.Controllers
{
    public class QuestionAnswerController : Yinhe.ProcessingCenter.ControllerBase
    {
        //
        // GET: /Home/

        public ActionResult Index()
        {
            return View();
        }


        public ActionResult QuestionListDetail()
        {
            return View();
        }

        public ActionResult QuestionTest()
        {
            return View();
        }
    }
}
