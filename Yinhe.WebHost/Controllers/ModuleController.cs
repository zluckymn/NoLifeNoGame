using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using Yinhe.ProcessingCenter;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace Yinhe.WebHost.Controllers
{
    public class ModuleController : Yinhe.ProcessingCenter.ControllerBase
    {
        //
        // GET: /Module/

        public ActionResult Index()
        {
            return View();
        }
        public ActionResult HeadContent()
        {
            return View();
        }


        public ActionResult ModulList(string id)
        {
            ViewData["id"] = id;
            return View();
        }

        public ActionResult EditModul()
        {
            return View();
        }

        /// <summary>
        /// 新增保存模块
        /// </summary>
        /// <param name="id"></param>
        /// <param name="selectOp"></param>
        /// <param name="name"></param>
        /// <param name="code"></param>
        /// <param name="dataObjId"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult EditModul(string id, string nodePid, string[] selectOp, string name, string code, string dataObjId)
        {
            int modulId;
            BsonDocument module = new BsonDocument { { "name", name }, { "code", code }, { "dataObjId", dataObjId } };
            if (CheckModuleCode(id, code))
            {
                if (int.TryParse(id, out modulId) && modulId > 0)//编辑模块
                {
                    dataOp.Update("SysModule", Query.EQ("modulId", id), module);
                    var selectOperating = dataOp.FindAllByQuery("SysModulFunction", Query.EQ("modulId", id));

                    UpdateOp(selectOp, code, id);
                }
                else//新增模块
                {
                    module.TryAdd("nodePid", nodePid);
                    InvokeResult result = dataOp.Insert("SysModule", module);
                    id = result.BsonInfo.String("modulId");
                    UpdateOp(selectOp, code, id);
                }

                return RedirectToAction("ModulList", new { id = id });
            }
            else
            {
                return View();
            }


        }

        public ActionResult DeleteModule(string id)
        {
            int modulId;
            if (int.TryParse(id, out modulId) && modulId > 0)//编辑模块
            {
                dataOp.Delete("SysModule", Query.EQ("modulId", id));
                dataOp.Delete("SysModulFunction", Query.EQ("modulId", id));
                dataOp.Delete("SysRoleRight", Query.EQ("modulId", id));//删除角色所对对应模块所选的权限
            }
            return RedirectToAction("ModulList");
        }

        public ActionResult MoveModule()
        {
            return View();
        }


        /// <summary>
        /// 判断是否存在相同code的模块
        /// </summary>
        /// <param name="moduleId"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        public bool CheckModuleCode(string moduleId, string code)
        {
            IMongoQuery query = Query.And(Query.EQ("code", code), Query.NE("modulId", moduleId));
            var modules = dataOp.FindAllByQuery("SysModule", query);
            return !modules.Any();
        }

        /// <summary>
        /// 保存模块的操作项
        /// </summary>
        /// <param name="selectOp">模块选择的操作项Id</param>
        /// <param name="code"></param>
        /// <param name="modulId"></param>
        private void UpdateOp(string[] selectOp, string code, string modulId)
        {
            if (selectOp != null && selectOp.Count() > 0)
            {
                var operatingList = dataOp.FindAll("SysOperating");//系统所有操作项

                dataOp.Delete("SysModulFunction", Query.EQ("modulId", modulId));//删除原先的所有操作项
                foreach (var opId in selectOp)
                {
                    var op = operatingList.Single(s => s.String("operatingId") == opId);
                    string opCode = op.String("code");
                    string opName = op.String("name");
                    string opOrder = op.String("opOrder");
                    dataOp.Insert("SysModulFunction", new BsonDocument{{"operatingId",opId},{"modulId",modulId},
                                                                    {"code",code+"_"+opCode},{"name",opName},{"opOrder",opOrder}});
                }
            }
            else
            {
                dataOp.Delete("SysModulFunction", Query.EQ("modulId", modulId));//删除原先的所有操作项
            }
        }

        #region 操作项管理
        /// <summary>
        /// 操作项管理
        /// </summary>
        /// <returns></returns>
        public ActionResult OperatingManage()
        {
            return View();
        }
        /// <summary>
        /// 操作项编辑
        /// </summary>
        /// <returns></returns>
        public ActionResult OperatingEdit()
        {
            return View();
        }

        /// <summary>
        /// 编辑
        /// </summary>
        /// <param name="moduleId"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult OperatingEdit(string id, string name, string code, string opOrder, string status, string remark)
        {
            int opId;
            IMongoQuery query1 = Query.And(Query.EQ("code", code), Query.NE("operatingId", id));
            var modules = dataOp.FindAllByQuery("SysOperating", query1);
            if (!modules.Any())
            {
                BsonDocument operationg = new BsonDocument { {"name",name },{"code",code },{"opOrder",opOrder },
                                                                            {"status",status },{"remark",remark} };
                if (int.TryParse(id, out opId) && opId > 0)//编辑操作
                {
                    IMongoQuery query = Query.EQ("operatingId", id);
                    dataOp.Update("SysOperating", query, operationg);
                    UpdateSysModulFunction(id, opOrder);
                }
                else//更新操作
                {
                    dataOp.Insert("SysOperating", operationg);
                }
            }
            return Json(new { success = true });
        }

        /// <summary>
        /// 修改系统操作后，同步修改模块所选择的操作代码
        /// </summary>
        /// <param name="operatingId"></param>
        /// <param name="code"></param>
        private void UpdateSysModulFunction(string operatingId, string opOrder)
        {
            var opfuncs = dataOp.FindAllByQuery("SysModulFunction", Query.EQ("operatingId", operatingId));
            foreach (var item in opfuncs)
            {
                string moduleFunctionId = item.String("moduleFunctionId");
                dataOp.Update("SysModulFunction", Query.EQ("moduleFunctionId", moduleFunctionId), new BsonDocument { { "opOrder", opOrder } });
            }
        }

        #endregion

        #region 系统模块
        /// <summary>
        /// 系统模块管理
        /// </summary>
        /// <returns></returns>
        public ActionResult ModulManage()
        {
            return View();
        }
        /// <summary>
        /// 系统模块编辑
        /// </summary>
        /// <returns></returns>
        public ActionResult ModuleEdit()
        {
            return View();
        }

        #endregion

        #region 系统模块操作项
        /// <summary>
        /// 系统功能模块
        /// </summary>
        /// <returns></returns>
        public ActionResult ModuleFunctionManage()
        {
            return View();
        }
        /// <summary>
        /// 系统模块操作项编辑
        /// </summary>
        /// <returns></returns>
        public ActionResult ModuleFunctionEdit()
        {
            return View();
        }
        #endregion

        #region 系统对象管理

        /// <summary>
        /// 数据对象管理
        /// </summary>
        /// <returns></returns>
        public ActionResult DataObjectManage()
        {
            return View();
        }
        /// <summary>
        /// 数据对象编辑
        /// </summary>
        /// <returns></returns>
        public ActionResult DataObjectEdit()
        {
            return View();
        }
        #endregion

    }
}
