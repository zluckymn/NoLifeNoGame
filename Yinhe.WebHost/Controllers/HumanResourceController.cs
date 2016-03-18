using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Yinhe.ProcessingCenter;

namespace Yinhe.WebHost.Controllers
{
    public class HumanResourceController : Yinhe.ProcessingCenter.ControllerBase
    {
        //
        // GET: /HumanResource/

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult HeadContent()
        {
            return View();
        }
        public ActionResult OrgManage()
        {
            return View();
        }

        public ActionResult OrgEdit()
        {
            return View();
        }

        public ActionResult OrgPostEdit()
        {
            return View();
        }

        public ActionResult ComPostManage()
        {
            return View();
        }

        public ActionResult ComPostEdit()
        {
            return View();
        }

        public ActionResult UserManage()
        {
            return View();
        }

        public ActionResult UserList()
        {
            return View();
        }

        public ActionResult UserEdit()
        {
            return View();
        }

        public ActionResult OrgPostTree()
        {
            return View();
        }
        public ActionResult CommonPostTree()
        {
            return View();
        }
        public ActionResult SearchTree()
        {
            return View();
        }
        public ActionResult UserPhoto()
        {
            return View();
        }
        public ActionResult UserView()
        {
            return View();
        }
        public ActionResult OrgOrderManager()
        {
            return View();
        }
        public ActionResult AddressBookManage()
        {
            return View();
        }
        public ActionResult PostShowOrder()
        {
            return View();
        }
        /// <summary>
        /// 首页用户头像
        /// </summary>
        /// <returns></returns>
        public ActionResult UserPortrait()
        {
            return View();
        }
    }
}
