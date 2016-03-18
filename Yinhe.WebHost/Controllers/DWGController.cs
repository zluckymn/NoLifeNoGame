using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Yinhe.ProcessingCenter;

namespace Yinhe.WebHost.Controllers
{
    public class DWGController : Yinhe.ProcessingCenter.ControllerBase
    {
        //
        // GET: /DWG/

        public ActionResult ViewDwgFile()
        {
            string filePath = PageReq.GetParam("filePath");
            ViewData["filePath"] = filePath;
            
            return View();
        }

    }
}
