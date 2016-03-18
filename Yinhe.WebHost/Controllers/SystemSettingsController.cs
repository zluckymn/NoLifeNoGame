using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text;
using System.Diagnostics;
using MongoDB.Bson;
using Yinhe.ProcessingCenter;
using Yinhe.ProcessingCenter.Common;
using Yinhe.ProcessingCenter.DataRule;
using MongoDB.Driver.Builders;

namespace Plugin_Administration_SS.Controllers
{
    public class SystemSettingsController : Yinhe.ProcessingCenter.ControllerBase //Controller
    {
        //
        // GET: /SystemSettings/

        public ActionResult Index()
        {
            return View() ;
        }

        public ActionResult SystemSettingsPage()
        {
            return View();
        }
        public ActionResult ComSysSettingPage()
        {
            return View();
        }
        public ActionResult SysMainPage()
        {
            return View();
        }

        public ActionResult LeftSideBar()
        {
            return View();
        }

        public ActionResult SysRoleDetail()
        {
            return View();
        }

        public ActionResult SysRoleManage()
        {
            return View();
        }
        public ActionResult SysMiddlePage()
        {
            return View();
        }

        public ActionResult SysModulRightSetting()
        {
            return View();
        }

        public ActionResult ItemAddControl()
        {
            return View();
        }
        public ActionResult SysRoleProjManage()
        {
            return View();
        }

        public ActionResult SysRoleProjManageSN()
        {
            return View();
        }

        public ActionResult HeadContent()
        {
            return View();
        }

        public ActionResult AreaProjectRole()
        {
            return View();
        }

        public ActionResult AreaProjectRoleRight()
        {
            return View();
        }

        public ActionResult ComSysRoleProjSetting()
        {
            return View();
        }

        /// <summary>
        /// 查看用户所拥有的权限
        /// </summary>
        /// <returns></returns>
        public ActionResult RightsOfUser()
        {
            return View();
        }

        /// <summary>
        /// 查看用户所拥有的权限
        /// </summary>
        /// <returns></returns>
        public ActionResult UserRightsOfProject()
        {
            return View();
        }

        /// <summary>
        /// 查看用户所拥有的权限
        /// </summary>
        /// <returns></returns>
        public ActionResult RightsOfProject()
        {
            return View();
        }
        public ActionResult SysRoleProductManage()
        {
            return View();
        }

        [HttpPost]
        public void SysRoleProjManageGet()
        {
            string data = PageReq.GetParam("data");
            string roleId = PageReq.GetParam("roleId");
            int flag = PageReq.GetParamInt("flag");
            string first = "";
            string last = "";
            string[] str = new string[5];

            if (string.IsNullOrEmpty(data) == false)
            {
                string[] strs = data.Split(new char[] { ',' });
                foreach (string s in strs)
                {
                    if (string.IsNullOrEmpty(s) == false)
                    {
                        str = s.Split(new char[] { '|' });
                        first = str[0];
                        last = str[1];
                    }

                    switch (first)
                    {
                        case "area":
                            dataOp.Delete("DataScope", Query.And(Query.EQ("dataTableName", "XH_ProductDev_Area"), Query.EQ("dataId", last.ToString()), Query.EQ("roleId", roleId)));
                            if (flag == 1)
                            {
                                BsonDocument bson = new BsonDocument { 
                {"roleCategoryId",1},
                {"roleId",roleId},
                {"dataTableName", "XH_ProductDev_Area"},
                {"dataFeiIdName",""},
                {"dataId",last},
                {"status",0},
                {"remark","区域权限"},
                };
                                dataOp.Insert("DataScope", bson);
                            }
                            break;
                        case "city":
                            dataOp.Delete("DataScope", Query.And(Query.EQ("dataTableName", "XH_ProductDev_City"), Query.EQ("dataId", last.ToString()), Query.EQ("roleId", roleId)));
                            if (flag == 1)
                            {
                                BsonDocument bson1 = new BsonDocument { 
                {"roleCategoryId",1},
                {"roleId",roleId},
                {"dataTableName", "XH_ProductDev_City"},
                {"dataFeiIdName",""},
                {"dataId",last},
                {"status",0},
                {"remark","区域权限"},
                };
                                dataOp.Insert("DataScope", bson1);
                            }
                            break;
                        case "land":
                            dataOp.Delete("DataScope", Query.And(Query.EQ("dataTableName", "XH_DesignManage_Engineering"), Query.EQ("dataId", last.ToString()), Query.EQ("roleId", roleId)));
                            if (flag == 1)
                            {
                                BsonDocument bson2 = new BsonDocument { 
                {"roleCategoryId",1},
                {"roleId",roleId},
                {"dataTableName", "XH_DesignManage_Engineering"},
                {"dataFeiIdName",""},
                {"dataId",last},
                {"status",0},
                {"remark","区域权限"},
                };
                                dataOp.Insert("DataScope", bson2);
                            }
                            break;
                        case "proj":

                            dataOp.Delete("DataScope", Query.And(Query.EQ("dataTableName", "XH_DesignManage_Project"), Query.EQ("dataId", last.ToString()), Query.EQ("roleId", roleId)));
                            if (flag == 1)
                            {
                                BsonDocument bson3 = new BsonDocument { 
                {"roleCategoryId",1},
                {"roleId",roleId},
                {"dataTableName", "XH_DesignManage_Project"},
                {"dataFeiIdName",""},
                {"dataId",last},
                {"status",0},
                {"remark","区域权限"},
                };
                                dataOp.Insert("DataScope", bson3);
                            }
                            break;
                        case "role":
                            dataOp.Delete("SysRoleRight", Query.And(Query.EQ("roleId", roleId), Query.EQ("modulId", str[2]), Query.EQ("code", str[3]), Query.EQ("dataObjId", str[4])));
                            if (flag == 1)
                            {
                                BsonDocument bson4 = new BsonDocument
                    {
                        {"roleId",roleId.ToString()},
                        {"modulId",str[2]},
                        {"code",str[3]},
                        {"dataObjId",str[4]}
                    };
                                dataOp.Insert("SysRoleRight", bson4);
                            }
                            break;
                        default: break;
                    }
                }
            }
        }



        [HttpPost]
        public void SysRoleProjManageSNGet()
        {

            string data = PageReq.GetParam("data");
            string roleId = PageReq.GetParam("roleId");
            int flag = PageReq.GetParamInt("flag");
            string first = "";
            string last = "";
            string[] str = new string[5];
            if (string.IsNullOrEmpty(data) == false)
            {
                str = data.Split(new char[] { '|' });
                first = str[0];
                last = str[1];
            }

            switch (first)
            {
                case "com":
                    dataOp.Delete("DataScope", Query.And(Query.EQ("dataTableName", "DesignManage_Company"), Query.EQ("dataId", last.ToString()), Query.EQ("roleId", roleId)));
                    if (flag == 1)
                    {
                        BsonDocument bson = new BsonDocument { 
                {"roleCategoryId",1},
                {"roleId",roleId},
                {"dataTableName", "DesignManage_Company"},
                {"dataFeiIdName",""},
                {"dataId",last},
                {"status",0},
                {"remark","公司权限"},
                };
                        dataOp.Insert("DataScope", bson);
                    } break;
                case "eng":
                    dataOp.Delete("DataScope", Query.And(Query.EQ("dataTableName", "XH_DesignManage_Engineering"), Query.EQ("dataId", last), Query.EQ("roleId", roleId)));
                    if (flag == 1)
                    {
                        BsonDocument bson1 = new BsonDocument { 
                {"roleCategoryId",1},
                {"roleId",roleId},
                {"dataTableName","XH_DesignManage_Engineering"},
                {"dataFeiIdName",""},
                {"dataId",last},
                {"status",0},
                {"remark","地块权限"},
                };
                        dataOp.Insert("DataScope", bson1);
                    }
                    break;
                case "proj":
                    dataOp.Delete("DataScope", Query.And(Query.EQ("dataTableName", "XH_DesignManage_Project"), Query.EQ("dataId", last), Query.EQ("roleId", roleId)));
                    if (flag == 1)
                    {
                        BsonDocument bson2 = new BsonDocument { 
                {"roleCategoryId",1},
                {"roleId",roleId},
                {"dataTableName","XH_DesignManage_Project"},
                {"dataFeiIdName",""},
                {"dataId",last},
                {"status",0},
                {"remark","项目权限"},
                };
                        dataOp.Insert("DataScope", bson2);
                    } break;
                case "role":
                    dataOp.Delete("SysRoleRight", Query.And(Query.EQ("roleId", roleId), Query.EQ("modulId", str[2]), Query.EQ("code", str[3]), Query.EQ("dataObjId", str[4])));
                    if (flag == 1)
                    {
                        BsonDocument bson3 = new BsonDocument
                    {
                        {"roleId",roleId.ToString()},
                        {"modulId",str[2]},
                        {"code",str[3]},
                        {"dataObjId",str[4]}
                    };
                        dataOp.Insert("SysRoleRight", bson3);
                    } break;

                default: break;
            }
        }



        [HttpPost]
        public void SysRoleProjManageSSGet()
        {
            string data = PageReq.GetParam("data");
            string roleId = PageReq.GetParam("roleId");
            int flag = PageReq.GetParamInt("flag");
            string first = "";
            string last = "";
            string[] str = new string[5];

            if (string.IsNullOrEmpty(data) == false)
            {
                string[] strs = data.Split(new char[] { ',' });
                foreach (string s in strs)
                {
                    if (string.IsNullOrEmpty(s) == false)
                    {
                        str = s.Split(new char[] { '|' });
                        first = str[0];
                        last = str[1];
                    }

                    switch (first)
                    {
                        case "area":
                            dataOp.Delete("DataScope", Query.And(Query.EQ("dataTableName", "LandArea"), Query.EQ("dataId", last.ToString()), Query.EQ("roleId", roleId)));
                            if (flag == 1)
                            {
                                BsonDocument bson = new BsonDocument { 
                {"roleCategoryId",1},
                {"roleId",roleId},
                {"dataTableName", "LandArea"},
                {"dataFeiIdName",""},
                {"dataId",last},
                {"status",0},
                {"remark","区域权限"},
                };
                                dataOp.Insert("DataScope", bson);
                            }
                            break;
                        case "city":
                            dataOp.Delete("DataScope", Query.And(Query.EQ("dataTableName", "LandCity"), Query.EQ("dataId", last.ToString()), Query.EQ("roleId", roleId)));
                            if (flag == 1)
                            {
                                BsonDocument bson1 = new BsonDocument { 
                {"roleCategoryId",1},
                {"roleId",roleId},
                {"dataTableName", "LandCity"},
                {"dataFeiIdName",""},
                {"dataId",last},
                {"status",0},
                {"remark","城市权限"},
                };
                                dataOp.Insert("DataScope", bson1);
                            }
                            break;
                        case "land":
                            dataOp.Delete("DataScope", Query.And(Query.EQ("dataTableName", "Land"), Query.EQ("dataId", last.ToString()), Query.EQ("roleId", roleId)));
                            if (flag == 1)
                            {
                                BsonDocument bson2 = new BsonDocument { 
                {"roleCategoryId",1},
                {"roleId",roleId},
                {"dataTableName", "Land"},
                {"dataFeiIdName",""},
                {"dataId",last},
                {"status",0},
                {"remark","土地权限"},
                };
                                dataOp.Insert("DataScope", bson2);
                            }
                            break;
                        case "proj":

                            dataOp.Delete("DataScope", Query.And(Query.EQ("dataTableName", "Project"), Query.EQ("dataId", last.ToString()), Query.EQ("roleId", roleId)));
                            if (flag == 1)
                            {
                                BsonDocument bson3 = new BsonDocument { 
                {"roleCategoryId",1},
                {"roleId",roleId},
                {"dataTableName", "Project"},
                {"dataFeiIdName",""},
                {"dataId",last},
                {"status",0},
                {"remark","项目权限"},
                };
                                dataOp.Insert("DataScope", bson3);
                            }
                            break;
                        case "role":
                            dataOp.Delete("SysRoleRight", Query.And(Query.EQ("roleId", roleId), Query.EQ("modulId", str[2]), Query.EQ("code", str[3]), Query.EQ("dataObjId", str[4])));
                            if (flag == 1)
                            {
                                BsonDocument bson4 = new BsonDocument
                    {
                        {"roleId",roleId.ToString()},
                        {"modulId",str[2]},
                        {"code",str[3]},
                        {"dataObjId",str[4]}
                    };
                                dataOp.Insert("SysRoleRight", bson4);
                            }
                            break;
                        default: break;
                    }
                }
            }
        }


        #region zhhy保存项目角色权限
        [HttpPost]
        public void SysRoleProjManageZHHYGet()
        {

            string data = PageReq.GetParam("data");
            string roleId = PageReq.GetParam("roleId");
            int flag = PageReq.GetParamInt("flag");
            string first = "";
            string last = "";
            string[] str = new string[5];
            if (string.IsNullOrEmpty(data) == false)
            {
                string[] strs = data.Split(new char[] { ',' });
                foreach (string s in strs)
                {
                    if (string.IsNullOrEmpty(s) == false)
                    {
                        str = s.Split(new char[] { '|' });
                        first = str[0];
                        last = str[1];
                    }
                    switch (first)
                    {
                        case "com":
                            dataOp.Delete("DataScope", Query.And(Query.EQ("dataTableName", "DesignManage_Company"), Query.EQ("dataId", last.ToString()), Query.EQ("roleId", roleId)));
                            if (flag == 1)
                            {
                                BsonDocument bson = new BsonDocument { 
                                    {"roleCategoryId",1},
                                    {"roleId",roleId},
                                    {"dataTableName", "DesignManage_Company"},
                                    {"dataFeiIdName",""},
                                    {"dataId",last},
                                    {"status",0},
                                    {"remark","公司权限"}
                                };
                                dataOp.Insert("DataScope", bson);
                            } break;
                        case "eng":
                            dataOp.Delete("DataScope", Query.And(Query.EQ("dataTableName", "XH_DesignManage_Engineering"), Query.EQ("dataId", last), Query.EQ("roleId", roleId)));
                            if (flag == 1)
                            {
                                BsonDocument bson1 = new BsonDocument { 
                                    {"roleCategoryId",1},
                                    {"roleId",roleId},
                                    {"dataTableName","XH_DesignManage_Engineering"},
                                    {"dataFeiIdName",""},
                                    {"dataId",last},
                                    {"status",0},
                                    {"remark","地块权限"}
                                };
                                dataOp.Insert("DataScope", bson1);
                            }
                            break;
                        case "proj":
                            dataOp.Delete("DataScope", Query.And(Query.EQ("dataTableName", "XH_DesignManage_Project"), Query.EQ("dataId", last), Query.EQ("roleId", roleId)));
                            if (flag == 1)
                            {
                                BsonDocument bson2 = new BsonDocument { 
                                    {"roleCategoryId",1},
                                    {"roleId",roleId},
                                    {"dataTableName","XH_DesignManage_Project"},
                                    {"dataFeiIdName",""},
                                    {"dataId",last},
                                    {"status",0},
                                    {"remark","项目权限"}
                                };
                                dataOp.Insert("DataScope", bson2);
                            } break;
                        case "role":
                            dataOp.Delete("SysRoleRight", Query.And(Query.EQ("roleId", roleId), Query.EQ("modulId", str[2]), Query.EQ("code", str[3]), Query.EQ("dataObjId", str[4])));
                            if (flag == 1)
                            {
                                BsonDocument bson3 = new BsonDocument
                                {
                                    {"roleId",roleId.ToString()},
                                    {"modulId",str[2]},
                                    {"code",str[3]},
                                    {"dataObjId",str[4]}
                                };
                                dataOp.Insert("SysRoleRight", bson3);
                            } break;

                        default: break;
                    }
                }
            }
        }

        #endregion

        #region zhtz保存项目角色权限
        [HttpPost]
        public void SysRoleProjManageZHTZGet()
        {

            string data = PageReq.GetParam("data");
            string roleId = PageReq.GetParam("roleId");
            int flag = PageReq.GetParamInt("flag");
            string first = "";
            string last = "";
            string[] str = new string[5];
            if (string.IsNullOrEmpty(data) == false)
            {
                string[] strs = data.Split(new char[] { ',' });
                foreach (string s in strs)
                {
                    if (string.IsNullOrEmpty(s) == false)
                    {
                        str = s.Split(new char[] { '|' });
                        first = str[0];
                        last = str[1];
                    }
                    switch (first)
                    {
                        case "com":
                            dataOp.Delete("DataScope", Query.And(Query.EQ("dataTableName", "DesignManage_Company"), Query.EQ("dataId", last.ToString()), Query.EQ("roleId", roleId)));
                            if (flag == 1)
                            {
                                BsonDocument bson = new BsonDocument { 
                {"roleCategoryId",1},
                {"roleId",roleId},
                {"dataTableName", "DesignManage_Company"},
                {"dataFeiIdName",""},
                {"dataId",last},
                {"status",0},
                {"remark","公司权限"},
                };
                                dataOp.Insert("DataScope", bson);
                            } break;
                        case "eng":
                            dataOp.Delete("DataScope", Query.And(Query.EQ("dataTableName", "XH_DesignManage_Engineering"), Query.EQ("dataId", last), Query.EQ("roleId", roleId)));
                            if (flag == 1)
                            {
                                BsonDocument bson1 = new BsonDocument { 
                {"roleCategoryId",1},
                {"roleId",roleId},
                {"dataTableName","XH_DesignManage_Engineering"},
                {"dataFeiIdName",""},
                {"dataId",last},
                {"status",0},
                {"remark","地块权限"},
                };
                                dataOp.Insert("DataScope", bson1);
                            }
                            break;
                        case "proj":
                            dataOp.Delete("DataScope", Query.And(Query.EQ("dataTableName", "XH_DesignManage_Project"), Query.EQ("dataId", last), Query.EQ("roleId", roleId)));
                            if (flag == 1)
                            {
                                BsonDocument bson2 = new BsonDocument { 
                {"roleCategoryId",1},
                {"roleId",roleId},
                {"dataTableName","XH_DesignManage_Project"},
                {"dataFeiIdName",""},
                {"dataId",last},
                {"status",0},
                {"remark","项目权限"},
                };
                                dataOp.Insert("DataScope", bson2);
                            } break;
                        case "role":
                            dataOp.Delete("SysRoleRight", Query.And(Query.EQ("roleId", roleId), Query.EQ("modulId", str[2]), Query.EQ("code", str[3]), Query.EQ("dataObjId", str[4])));
                            if (flag == 1)
                            {
                                BsonDocument bson3 = new BsonDocument
                    {
                        {"roleId",roleId.ToString()},
                        {"modulId",str[2]},
                        {"code",str[3]},
                        {"dataObjId",str[4]}
                    };
                                dataOp.Insert("SysRoleRight", bson3);
                            } break;

                        default: break;
                    }
                }
            }
        }
        #endregion

        #region 新增地块或项目时自动勾选
        /// <summary>
        /// 新增地块或项目时，如果某些项目角色已经勾选了改地块或项目所属公司，
        /// 则这些角色的新增地块或项目自动勾选
        /// ZHHY、ZHTZ使用
        /// </summary>
        [HttpPost]
        public JsonResult AutoCheckEngAndProjZHHY()
        {
            var scopetype = PageReq.GetForm("scopetype");
            var value = PageReq.GetForm("value");
            InvokeResult result = new InvokeResult() { Status = Status.Successful };

            #region 获取所有需要自动勾选的角色id
            var dataTableName = string.Empty;
            var keyName = string.Empty;
            BsonDocument projObj = null;
            BsonDocument engObj = null;
            BsonDocument comObj = null;
            switch (scopetype)
            {
                case "eng":
                    engObj = dataOp.FindOneByQuery("XH_DesignManage_Engineering", Query.EQ("engId", value));
                    comObj = dataOp.FindOneByQuery("DesignManage_Company", Query.EQ("comId", engObj.Text("comId")));
                    break;
                case "proj":
                    projObj = dataOp.FindOneByQuery("XH_DesignManage_Project", Query.EQ("projId", value));
                    engObj = dataOp.FindOneByQuery("XH_DesignManage_Engineering", Query.EQ("engId", projObj.Text("engId")));
                    comObj = dataOp.FindOneByQuery("DesignManage_Company", Query.EQ("comId", engObj.Text("comId")));
                    break;
                default:
                    break;
            }
            if (comObj == null || string.IsNullOrEmpty(comObj.Text("comId")))
            {
                result.Status = Status.Failed;
                result.Message = "未能找到该项目或工程对应的公司";
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            var dataScopeList = dataOp.FindAllByQuery("DataScope",
                    Query.And(
                        Query.EQ("dataTableName", "DesignManage_Company"),
                        Query.EQ("dataId", comObj.Text("comId"))
                    )
                ) as IEnumerable<BsonDocument>;

            var sysRoleList = dataOp.FindAllByQuery("SysRole",
                Query.In("roleId", dataScopeList.Select(p => (BsonValue)p.Text("roleId")))).Select(p => p.Text("roleId"));

            dataScopeList = dataScopeList.Where(p => sysRoleList.Contains(p.Text("roleId")));

            var roleIdList = dataScopeList.Select(p => p.Text("roleId")).Distinct();
            if (roleIdList.Count() == 0)
            {
                result.Status = Status.Successful;
                result.Message = "没有需要自动勾选的角色";
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            #endregion

            #region 插入角色区域权限

            List<StorageData> saveList = new List<StorageData>();
            switch (scopetype)
            {
                case "eng":
                    dataOp.Delete("DataScope", Query.And(Query.EQ("dataTableName", "XH_DesignManage_Engineering"), Query.EQ("dataId", value)));

                    foreach (var roleId in roleIdList)
                    {
                        StorageData tempData = new StorageData();
                        tempData.Name = "DataScope";
                        tempData.Document = new BsonDocument { 
                                    {"roleCategoryId",1},
                                    {"roleId",roleId},
                                    {"dataTableName","XH_DesignManage_Engineering"},
                                    {"dataFeiIdName",""},
                                    {"dataId",value},
                                    {"status",0},
                                    {"remark","地块权限"}
                                };
                        tempData.Type = StorageType.Insert;
                        saveList.Add(tempData);
                    }
                    break;
                case "proj":
                    dataOp.Delete("DataScope", Query.And(Query.EQ("dataTableName", "XH_DesignManage_Project"), Query.EQ("dataId", value)));
                    foreach (var roleId in roleIdList)
                    {
                        StorageData tempData = new StorageData();
                        tempData.Name = "DataScope";
                        tempData.Document = new BsonDocument { 
                                    {"roleCategoryId",1},
                                    {"roleId",roleId},
                                    {"dataTableName","XH_DesignManage_Project"},
                                    {"dataFeiIdName",""},
                                    {"dataId",value},
                                    {"status",0},
                                    {"remark","项目权限"}
                                };
                        tempData.Type = StorageType.Insert;
                        saveList.Add(tempData);
                    }
                    break;
                default:
                    break;
            }
            result = dataOp.BatchSaveStorageData(saveList);
            return Json(TypeConvert.InvokeResultToPageJson(result));

            #endregion

        }
        #endregion

        #region QX保存项目角色权限
        [HttpPost]
        public void SysRoleProjManageQXGet()
        {
            string data = PageReq.GetParam("data");
            string roleId = PageReq.GetParam("roleId");
            int flag = PageReq.GetParamInt("flag");
            string first = "";
            string last = "";
            string[] str = new string[5];
            if (string.IsNullOrEmpty(data) == false)
            {
                string[] strs = data.Split(new char[] { ',' });
                foreach (string s in strs)
                {
                    if (string.IsNullOrEmpty(s) == false)
                    {
                        str = s.Split(new char[] { '|' });
                        first = str[0];
                        last = str[1];
                    }
                    switch (first)
                    {
                        case "area":
                            dataOp.Delete("DataScope", Query.And(Query.EQ("dataTableName", "XH_ProductDev_Area"), Query.EQ("dataId", last.ToString()), Query.EQ("roleId", roleId)));
                            if (flag == 1)
                            {
                                BsonDocument bson = new BsonDocument { 
                {"roleCategoryId",1},
                {"roleId",roleId},
                {"dataTableName", "XH_ProductDev_Area"},
                {"dataFeiIdName",""},
                {"dataId",last},
                {"status",0},
                {"remark","区域权限"},
                };
                                dataOp.Insert("DataScope", bson);
                            }
                            break;
                        case "city":
                            dataOp.Delete("DataScope", Query.And(Query.EQ("dataTableName", "XH_ProductDev_City"), Query.EQ("dataId", last.ToString()), Query.EQ("roleId", roleId)));
                            if (flag == 1)
                            {
                                BsonDocument bson = new BsonDocument { 
                {"roleCategoryId",1},
                {"roleId",roleId},
                {"dataTableName", "XH_ProductDev_City"},
                {"dataFeiIdName",""},
                {"dataId",last},
                {"status",0},
                {"remark","公司权限"},
                };
                                dataOp.Insert("DataScope", bson);
                            } break;
                        case "land":
                            dataOp.Delete("DataScope", Query.And(Query.EQ("dataTableName", "XH_DesignManage_Engineering"), Query.EQ("dataId", last), Query.EQ("roleId", roleId)));
                            if (flag == 1)
                            {
                                BsonDocument bson1 = new BsonDocument { 
                {"roleCategoryId",1},
                {"roleId",roleId},
                {"dataTableName","XH_DesignManage_Engineering"},
                {"dataFeiIdName",""},
                {"dataId",last},
                {"status",0},
                {"remark","地块权限"},
                };
                                dataOp.Insert("DataScope", bson1);
                            }
                            break;
                        case "proj":
                            dataOp.Delete("DataScope", Query.And(Query.EQ("dataTableName", "XH_DesignManage_Project"), Query.EQ("dataId", last), Query.EQ("roleId", roleId)));
                            if (flag == 1)
                            {
                                BsonDocument bson2 = new BsonDocument { 
                {"roleCategoryId",1},
                {"roleId",roleId},
                {"dataTableName","XH_DesignManage_Project"},
                {"dataFeiIdName",""},
                {"dataId",last},
                {"status",0},
                {"remark","项目权限"},
                };
                                dataOp.Insert("DataScope", bson2);
                            } break;
                        case "role":
                            dataOp.Delete("SysRoleRight", Query.And(Query.EQ("roleId", roleId), Query.EQ("modulId", str[2]), Query.EQ("code", str[3]), Query.EQ("dataObjId", str[4])));
                            if (flag == 1)
                            {
                                BsonDocument bson3 = new BsonDocument
                    {
                        {"roleId",roleId.ToString()},
                        {"modulId",str[2]},
                        {"code",str[3]},
                        {"dataObjId",str[4]}
                    };
                                dataOp.Insert("SysRoleRight", bson3);
                            } break;

                        default: break;
                    }
                }
            }
        }
        #endregion

        #region QX保存产品系列角色权限
        [HttpPost]
        public void SysRoleProductManageQXGet()
        {
            string data = PageReq.GetParam("data");
            string roleId = PageReq.GetParam("roleId");
            int flag = PageReq.GetParamInt("flag");
            string first = "";
            string last = "";
            string[] str = new string[5];
            if (string.IsNullOrEmpty(data) == false)
            {
                string[] strs = data.Split(new char[] { ',' });
                foreach (string s in strs)
                {
                    if (string.IsNullOrEmpty(s) == false)
                    {
                        str = s.Split(new char[] { '|' });
                        first = str[0];
                        last = str[1];
                    }
                    switch (first)
                    {
                        case "product":
                            dataOp.Delete("DataScope", Query.And(Query.EQ("dataTableName", "ProductSeries"), Query.EQ("dataId", last.ToString()), Query.EQ("roleId", roleId)));
                            if (flag == 1)
                            {
                                BsonDocument bson = new BsonDocument { 
                {"roleCategoryId",1},
                {"roleId",roleId},
                {"dataTableName", "ProductSeries"},
                {"dataFeiIdName",""},
                {"dataId",last},
                {"status",0},
                {"remark","产品系列权限"},
                };
                                dataOp.Insert("DataScope", bson);
                            }
                            break;
                        case "role":
                            dataOp.Delete("SysRoleRight", Query.And(Query.EQ("roleId", roleId), Query.EQ("modulId", str[2]), Query.EQ("code", str[3]), Query.EQ("dataObjId", str[4])));
                            if (flag == 1)
                            {
                                BsonDocument bson3 = new BsonDocument
                    {
                        {"roleId",roleId.ToString()},
                        {"modulId",str[2]},
                        {"code",str[3]},
                        {"dataObjId",str[4]}
                    };
                                dataOp.Insert("SysRoleRight", bson3);
                            } break;

                        default: break;
                    }
                }
            }
        }
        #endregion

    }
}
