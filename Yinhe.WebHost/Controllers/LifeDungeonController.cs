using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MongoDB.Bson;
using Yinhe.ProcessingCenter;
using System.Web.Security;

namespace Yinhe.WebHost.Controllers
{
    public class LifeDungeonController : Yinhe.ProcessingCenter.ControllerBase
    {
        //
        // GET: /Home/

        public ActionResult Index()
        {

            return View();
        }
       
 

    }
}
