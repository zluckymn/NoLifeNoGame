using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Collections.Specialized;
using MongoDB.Bson;
using System.Web;
using Yinhe.ProcessingCenter.MvcFilters;
using Yinhe.ProcessingCenter.Document;
using System.Text.RegularExpressions;
using Yinhe.ProcessingCenter.DataRule;
using Yinhe.ProcessingCenter.DesignManage.TaskFormula;
using MongoDB.Driver.Builders;
using System.Transactions;
using Yinhe.ProcessingCenter.DesignManage;
using System.Xml.Linq;
using System.Collections;
using Yinhe.ProcessingCenter.Common;
using Yinhe.ProcessingCenter.Business.PolicyDecision;
using MongoDB.Driver;
using Yinhe.ProcessingCenter.Permissions;

using Yinhe.ProcessingCenter.Business;

namespace Yinhe.ProcessingCenter
{
    /// <summary>
    ///  现金流管理基类
    /// </summary>
    public class PolicyFormInfo
    {
        public string tbName { get; set; }
        public string dirName { get; set; }
        public string verifyKey { get; set; }
        public string name { get; set; }
    }

    public partial class ControllerBase : Controller
    {

        private static readonly object objPad = new object();
        //HashSet<string> tableSet = new HashSet<string> { "ProjNodeDir", "MarketingAreaDir", "DesignAreaDir", "CostDir" };
        //public static readonly IDictionary<string, string> FormVerifyDict =
        //    new Dictionary<string, string> { { "ProjNode", "ZB_VERIFY" }, { "MarketingArea", "YX_VERIFY" }, { "DesignArea", "SJ_VERIFY" }, 
        //                                    { "Finance", "CW_VERIFY" }, { "Cost", "CB_VERIFY" } };

        /// <summary>
        /// 产品决策各部门数据表及相关信息
        /// </summary>
        public static readonly IDictionary<string, PolicyFormInfo> FormInfoDict =
            new Dictionary<string, PolicyFormInfo> { { "ProjNode",new PolicyFormInfo{verifyKey = "ZB_VERIFY",tbName="ProjNode",name="总办部门",dirName="ProjNodeDir"} }, 
                                            { "MarketingArea",new PolicyFormInfo{verifyKey = "YX_VERIFY",tbName="MarketingArea",name="营销部门",dirName="MarketingAreaDir"} },
                                            { "DesignArea", new PolicyFormInfo{verifyKey ="SJ_VERIFY",tbName="DesignArea",name="设计部门",dirName="DesignAreaDir"} }, 
                                            { "Finance", new PolicyFormInfo{verifyKey = "CW_VERIFY",tbName="Finance",name="财务部门",dirName="FinanceDir"} },
                                            { "Cost",  new PolicyFormInfo{verifyKey ="CB_VERIFY",tbName="Cost",name="成本部门",dirName="CostDir"} } };

        public static readonly IDictionary<string, string> FormDict =
            new Dictionary<string, string> { { "ProjNode", "ProjNodeDir" }, { "MarketingArea", "MarketingAreaDir" }, { "DesignArea", "DesignAreaDir" }, 
                                            { "Finance", "FinanceDir" }, { "Cost", "CostDir" } };

         public static readonly IDictionary<string, string> FormDirDict =
            new Dictionary<string, string> { { "ProjNodeDir", "ProjNode" }, { "MarketingAreaDir", "MarketingArea" }, { "DesignAreaDir", "DesignArea" }, 
                                            { "FinanceDir", "Finance" }, { "CostDir", "Cost" } };

        #region 决策模板相关操作

        /// <summary>
        /// 产品配置业态的信息
        /// </summary>
        /// <param name="projId">项目Id</param>
        /// <param name="treeId">配置Id</param>
        /// <returns></returns>
        public JsonResult GetPropertyInfos(string projId, string treeId)
        {
            IMongoQuery query = Query.And(Query.EQ("projId", projId), Query.EQ("treeId", treeId));
            var list = dataOp.FindAllByQuery("ProjectCIlist", query);//项目的产品配置清单
            var propertyIdList = list.Select(x => x.String("propertyId"));//业态
            var propertyList = dataOp.FindAllByKeyValList("SystemProperty", "propertyId", propertyIdList);
            return Json(propertyList.Select(s => new { id = s.String("propertyId"), name = s.String("name") }));
        }

        public JsonResult GetLastPropertys(string projId, string treeId)
        {
            IMongoQuery query = Query.And(Query.EQ("projId", projId), Query.EQ("treeId", treeId));
            var list = dataOp.FindAllByQuery("ProjectCIlist", query);//项目的产品配置清单
            var propertyIdList = list.Select(x => x.String("propertyId"));//业态
            var propertyList = dataOp.FindAllByQuery("SystemProperty", Query.NotIn("propertyId", TypeConvert.StringListToBsonValueList(propertyIdList)));
            return Json(propertyList.Select(s => new { id = s.String("propertyId"), name = s.String("name") }));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyId"></param>
        /// <param name="listId"></param>
        /// <returns></returns>
        public JsonResult SaveProperty(string propertyId, string listId)
        {
            var result = dataOp.Update("ProjectCIlist", Query.EQ("listId", listId), new BsonDocument { { "propertyId", propertyId } });
            return Json(ConvertToPageJson(result));
        }


        /// <summary>
        /// 是否确认暂存数据
        /// </summary>
        /// <param name="versionId"></param>
        /// <returns></returns>
        private bool isConfirm(string versionId)
        {
            int confirm = PageReq.GetInt("confirm");
            if (confirm == 1)
                return true;
            else
                return false;
        }

        /// <summary>
        /// 获取决策意见信息
        /// </summary>
        /// <param name="typeId"></param>
        /// <param name="versionId"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult GetCEOComment(int typeId, int versionId)
        {
            BsonDocument comment = dataOp.FindOneByQuery("CEOPolicyDesign", Query.EQ("versionId", versionId));
            int pdId = comment.Int("pdId");
            List<BsonDocument> comments = dataOp.FindAllByQuery("CEOComment", Query.And(Query.EQ("pdId", pdId), Query.EQ("typeId", typeId))).ToList();
            object json = comments.Select(s => new { title = s.String("content").CutStr(10, "..."), date = s.ShortDate("createDate") }).ToList();
            return Json(json);
        }

        /// <summary>
        /// 新增总裁意见
        /// </summary>
        /// <param name="typeId"></param>
        /// <param name="versionId"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult InsertCEOComment(int pdId, int typeId, string content)
        {
            object json = null;
            try
            {
                dataOp.Insert("CEOComment", new BsonDocument { { "pdId", pdId }, { "typeId", typeId }, { "content", content } });
                json = new { success = true };
            }
            catch
            {
                json = new { success = false };
            }
            return Json(json);
        }

        [HttpPost]
        public JsonResult DeleteCEOComment(int commentId)
        {

            object json = null;
            try
            {
                dataOp.Delete("CEOComment", Query.EQ("commentId", commentId.ToString()));
                json = new { success = true };
            }
            catch
            {
                json = new { success = false };
            }
            return Json(json);
        }

        [HttpPost]
        public JsonResult GetFileTableInfo(string tbName, string versionId)
        {
            var tbRule = new TableRule(tbName);
            var keyName = tbRule.PrimaryKey;
            const string versionIdStr = "versionId";
            var keyValue = dataOp.FindOneByQuery(tbName, Query.EQ(versionIdStr, versionId)).String(keyName);//获取主键值
            //object json = new object ;
            return Json(new { tbName = tbName, keyName = keyName, keyValue = keyValue });
        }

        /// <summary>
        /// 保存冲突决策信息
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public JsonResult SaveConflictPolicyInfo()
        {
            lock (objPad)
            {
                PageJson json = new PageJson();
                try
                {
                    string opinionInfos = PageReq.GetString("opinionInfos");
                    string costInfos = PageReq.GetString("costInfos");//主材造价
                    string versionId = PageReq.GetString("versionId");
                    string[] temps = opinionInfos.SplitParam("|Y|");
                    BsonDocument feedback = new BsonDocument();
                    foreach (var s in temps)//决策意见
                    {
                        string[] infos = s.SplitParam(StringSplitOptions.None, "|$|");
                        if (infos.Length > 1)
                        {
                            string key = infos[0];
                            string value = infos[1];
                            feedback.Add(key, value);
                        }
                    }

                    string[] costTemps = costInfos.SplitParam("|Y|");
                    foreach (var s in costTemps)
                    {
                        string[] infos = s.SplitParam(StringSplitOptions.None, "|$|");
                        if (infos.Length > 1)
                        {
                            string key = infos[0];
                            string value = infos[1];
                            feedback.Add(key, value);
                        }
                    }

                    string tbName = "ConflictInfo";
                    BsonDocument old = dataOp.FindOneByQuery(tbName, Query.EQ("versionId", versionId));
                    if (old != null)//更新
                    {
                        dataOp.Update(tbName, Query.EQ("versionId", versionId), feedback);
                    }
                    else
                    {
                        feedback.Add("versionId", versionId);
                        dataOp.Insert(tbName, feedback);
                    }
                    json.Success = true;
                }
                catch
                {
                    json.Success = false;
                }
                return Json(json);
            }
        }


        [HttpPost]
        public JsonResult SaveConflictStatus(string key, string versionId, string status)
        {
            PageJson json = new PageJson();

            try
            {
                if (status == "1") status = "0";
                else status = "1";
                dataOp.Update("ConflictInfo", Query.EQ("versionId", versionId), new BsonDocument { { key, status } });
                json.Success = true;
            }
            catch
            {
                json.Success = false;
            }
            return Json(json);
        }



        /// <summary>
        /// 保存销售计划
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public JsonResult SaveSellDetails()
        {
            lock (objPad)
            {
                string versionId = PageReq.GetForm("versionId");
                string details = PageReq.GetForm("details");//
                string[] temps = details.SplitParam("|$|");
                string tbName = "SellDetail";
                PageJson json = new PageJson();
                try
                {
                    foreach (string temp in temps)
                    {

                        string[] detail = temp.SplitParam(StringSplitOptions.None, "|Y|");
                        if (detail.Count() > 2)
                        {
                            string date = detail[1];
                            BsonDocument newDetail = new BsonDocument { { "date", detail[1] }, { "salesVal", detail[2] } };

                            BsonDocument sellDetail = dataOp.FindOneByQuery(tbName, Query.And(Query.EQ("date", date), Query.EQ("versionId", versionId)));
                            if (sellDetail != null)
                            {
                                string detailId = sellDetail.String("detailId");
                                dataOp.Update(tbName, Query.EQ("detailId", detailId), newDetail);
                            }
                            else
                            {
                                newDetail.Add("versionId", versionId);
                                dataOp.Insert(tbName, newDetail);
                            }
                        }
                    }
                    json.Success = true;
                }
                catch
                {
                    json.Success = false;
                }
                return Json(json);
            }
        }

        /// <summary>
        /// 保存还款计划信息
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public JsonResult SaveReapyDetails()
        {
            lock (objPad)
            {

                string versionId = PageReq.GetForm("versionId");
                string details = PageReq.GetForm("details");//
                string[] temps = details.SplitParam("|$|");
                string tbName = "RepayDetail";
                bool confirm = isConfirm(versionId);
                PageJson json = new PageJson();
                try
                {
                    //还款计划
                    foreach (string temp in temps)
                    {
                        string[] detail = temp.SplitParam(StringSplitOptions.None, "|Y|");
                        if (detail.Count() >10)
                        {
                            string date = detail[1];//日期
                            //BsonDocument newDetail = new BsonDocument { { "date", detail[1] }, { "repayVal", detail[2] }, { "interest", detail[3] }, { "loan", detail[4] } };
                            BsonDocument newDetail = null;
                            if (!confirm)
                            {   //暂存数据
                                newDetail = new BsonDocument { 
                                        { "date", detail[1] }, { "repayVal_bak", detail[2] }, { "interest_bak", detail[3] }, { "loan_bak", detail[4] },{ "minRepayVal_bak", detail[5] }, { "minInterest_bak", detail[6] }, { "minLoan_bak", detail[7] },{ "comRepayVal_bak", detail[8] }, { "comInterest_bak", detail[9] }, { "comLoan_bak", detail[10] }};
                            }
                            else
                            {   //确认数据,如果是确认人直接修改数据，需要把确认后的数据回写到暂存区
                                newDetail = new BsonDocument { 
                                        { "date", detail[1] }, { "repayVal_bak", detail[2] }, { "interest_bak", detail[3] }, { "loan_bak", detail[4] },{ "minRepayVal_bak", detail[5] }, { "minInterest_bak", detail[6] }, { "minLoan_bak", detail[7] },{ "comRepayVal_bak", detail[8] }, { "comInterest_bak", detail[9] }, { "comLoan_bak", detail[10] },
                                                                { "repayVal", detail[2] }, { "interest", detail[3] }, { "loan", detail[4] }
                                ,{ "minRepayVal", detail[5] }, { "minInterest", detail[6] }, { "minLoan", detail[7] },{ "comRepayVal", detail[8] }, { "comInterest", detail[9] }, { "comLoan", detail[10] }};
                            }

                            BsonDocument reapyDetail = dataOp.FindOneByQuery(tbName, Query.And(Query.EQ("date", date), Query.EQ("versionId", versionId)));
                            if (reapyDetail != null)
                            {
                                string detailId = reapyDetail.String("detailId");
                                InvokeResult result =
                                    dataOp.Update(tbName, Query.EQ("detailId", detailId), newDetail);
                            }
                            else
                            {
                                newDetail.Add("versionId", versionId);
                                dataOp.Insert(tbName, newDetail);
                            }
                        }
                    }

                    //保存销售计划信息
                    string sellDetails = PageReq.GetString("sellDetails");
                    temps = sellDetails.SplitParam("|$|");
                    tbName = "SellDetail";

                    foreach (string temp in temps)
                    {
                        string[] detail = temp.SplitParam(StringSplitOptions.None, "|Y|");
                        if (detail.Count() > 4)
                        {
                            string date = detail[1];
                            //BsonDocument newDetail = new BsonDocument { { "date", detail[1] }, { "salesVal", detail[2] } };
                            BsonDocument newDetail = null;
                            if (!confirm)
                            {
                                //暂存数据
                                newDetail = new BsonDocument { { "date", detail[1] }, { "salesVal_bak", detail[2] }, { "minSalesVal_bak", detail[3] }, { "comSalesVal_bak", detail[4] } };
                            }
                            else
                            {
                                //确认数据
                                newDetail = new BsonDocument { { "date", detail[1] }, { "salesVal", detail[2] }, { "minSalesVal", detail[3] }, { "comSalesVal", detail[4] }, { "salesVal_bak", detail[2] }, { "minSalesVal_bak", detail[3] }, { "comSalesVal_bak", detail[4] } };
                            }

                            BsonDocument sellDetail = dataOp.FindOneByQuery(tbName, Query.And(Query.EQ("date", date), Query.EQ("versionId", versionId)));
                            if (sellDetail != null)
                            {
                                string detailId = sellDetail.String("detailId");
                                dataOp.Update(tbName, Query.EQ("detailId", detailId), newDetail);
                            }
                            else
                            {
                                newDetail.Add("versionId", versionId);
                                dataOp.Insert(tbName, newDetail);
                            }
                        }
                    }

                    //保存成本科目比例
                    string rates = PageReq.GetString("rates");
                    temps = rates.SplitParam("|$|");
                    tbName = "CostDir";
                    foreach (string temp in temps)
                    {
                        string[] detail = temp.SplitParam(StringSplitOptions.None, "|Y|");
                        if (detail.Count() > 1)
                        {
                            string dirId = detail[0];
                            if (!confirm)
                            {
                                dataOp.Update(tbName, Query.EQ("CostDirId", dirId), new BsonDocument { { "rate_bak", detail[1] } });
                            }
                            else
                            {
                                dataOp.Update(tbName, Query.EQ("CostDirId", dirId), new BsonDocument { { "rate_bak", detail[1] }, { "rate", detail[1] } });
                            }
                        }
                    }
                    json.Success = true;
                }
                catch
                {
                    json.Success = false;
                }
                return Json(json);
            }
        }

        /// <summary>
        /// 删除版本
        /// </summary>
        /// <param name="versionId"></param>
        /// <returns></returns>
        public JsonResult DeleteVersion(string versionId)
        {
            PolicyVersionBll versionBll = PolicyVersionBll._(dataOp);
            PageJson json = new PageJson();
            try
            {
                versionBll.Rollback(versionId);
                json.Success = true;
            }
            catch
            {
                json.Success = false;
            }
            return Json(json);
        }

        /// <summary>
        /// 决策数据模板目录
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult GetTemplateDir(string tableName)
        {
            string dirTable = tableName + "Dir";//目录表名称

            TableRule dirRule = new TableRule(dirTable);
            TableRule tableEntity = new TableRule(tableName);
            string primaryKey = tableEntity.ColumnRules.Where(t => t.IsPrimary == true).FirstOrDefault().Name;
            string dirKey = dirRule.ColumnRules.Where(t => t.IsPrimary == true).FirstOrDefault().Name;//目录主键名称

            BsonDocument temp = dataOp.FindOneByQuery(tableName, Query.EQ("isTemplate", "1"));//找出模板
            List<BsonDocument> dirs = dataOp.FindAllByQuery(dirTable, Query.EQ(primaryKey, temp.String(primaryKey))).ToList();
            var json = dirs.Where(s => s.Int("nodeLevel") > 1).Select(s => new { id = s.String(dirKey), name = s.String("name") });
            return Json(json);
        }

        /// <summary>
        /// 创建决策版本
        /// </summary>
        /// <param name="isOriginal">是否初始版本</param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult CreateVersion(string projId, string remark,int type)
        {
            PageJson json = new PageJson();
            string versionId = string.Empty;
            try
            {
                PolicyVersionBll versionBll = PolicyVersionBll._(dataOp);
                versionBll.CreateVersion(projId, remark,type);
                json.Success = true;
            }
            catch
            {
                json.Success = false;
            }

            return Json(json);
        }

        [HttpPost]
        public JsonResult SaveRate()
        {
            lock (objPad)
            {
                PageJson json = new PageJson();
                string dataStr = PageReq.GetForm("dataStr");
                string costRateFormId = PageReq.GetForm("costRateFormId");
                bool isProjRate;
                bool.TryParse(PageReq.GetForm("isProjRate"), out isProjRate);
                string[] temp = dataStr.Split('|');//科目Id，节点Id，值
                try
                {
                    foreach (var s in temp)
                    {
                        string[] rateInfo = s.Split(',');
                        if (rateInfo.Length > 2)
                        {
                            string costDirId = rateInfo[0];
                            string nodeId = rateInfo[1];
                            string rate = rateInfo[2];

                            BsonDocument rateBson = dataOp.FindOneByQuery("CostRate", Query.And(Query.EQ("costDirId", costDirId), Query.EQ("projNodeId", nodeId)));
                            if (rateBson != null)
                            {
                                dataOp.Update("CostRate", Query.EQ("CostRateId", rateBson.String("CostRateId")), new BsonDocument { { "rate", rate } });
                            }
                            else
                            {
                                rateBson = new BsonDocument();
                                if (!isProjRate)
                                    rateBson.Add("isTemplate", "1");//模板
                                else
                                    rateBson.Add("isTemplate", "0");
                                rateBson.Add("costDirId", costDirId);
                                rateBson.Add("projNodeId", nodeId);
                                rateBson.Add("rate", rate);
                                rateBson.Add("costRateFormId", costRateFormId);
                                dataOp.Insert("CostRate", rateBson);
                            }
                        }
                    }
                    json.Success = true;
                }
                catch
                {
                    json.Success = false;
                }

                return Json(json);
            }
        }

        /// <summary>
        /// 保存财务融资信息
        /// </summary>
        /// <returns></returns>
        public JsonResult UpdateFinance()
        {
            //landMoney: landMoney, financeModes: financeModes
            //bool cofirm = isConfirm(versionId);//保存还是提交数据
            PageJson json = new PageJson();
            int confirm = PageReq.GetInt("confirm");
            string versionId = PageReq.GetString("versionId");
            string projId = PageReq.GetString("projId");
            bool isSubmit = PageReq.GetBoolean("isSubmit");
            try
            {
                string FinanceId = string.Empty;
                string landMoney = PageReq.GetForm("landMoney");
                string financeModes = PageReq.GetForm("financeModes");

                string[] temp1 = landMoney.SplitParam("|");
                BsonDocument finance = new BsonDocument();
                int index1 = 0;
                foreach (var s in temp1)
                {
                    string[] temp2 = s.Split('_');
                    if (index1++ == 0)
                    {
                        FinanceId = temp2[1];
                    }
                    else
                    {
                        finance.Add(temp2[0], temp2[1]);
                    }
                }
                dataOp.Update("Finance", Query.EQ("FinanceId", FinanceId), finance);

                string[] modeStrs = financeModes.SplitParam("|Y|");
                foreach (var s in modeStrs)
                {
                    string[] modeStr = s.SplitParam("|");
                    string modeId = string.Empty;
                    BsonDocument mode = new BsonDocument();
                    int index = 0;
                    foreach (var m in modeStr)
                    {
                        string[] keyValue = m.Split('_');
                        if (index++ == 0)
                        {
                            modeId = keyValue[1];
                        }
                        else
                        {
                            mode.Add(keyValue[0], keyValue[1]);
                        }
                    }
                    dataOp.Update("FinanceMode", Query.EQ("modeId", modeId), mode);
                }
                if (confirm == 1)
                {
                    string dirName = "FinanceDir";
                    ChangeFormStatus(dirName,projId,versionId,"0");
                   // AddOAPolicyMsg(dirName, projId, versionId);
                    UpdateOAPolicyMsg(dirName,projId,versionId);
                    LogPolicyVerify("Finance", projId, versionId, "通过", "");
                }
                if (isSubmit)
                {
                    SubVerify("FinanceDir", projId, versionId);
                }
                json.Success = true;
            }
            catch (Exception ex)
            {
                json.Success = false;
            }

            return Json(json);
        }


        public ActionResult PolicyDirTree(string tbName, string moveId)
        {
            string dirName = tbName + "Dir";
            TableRule tbRule = new TableRule(tbName);
            TableRule dirRule = new TableRule(dirName);

            string tbKey = tbRule.PrimaryKey;
            string dirKey = dirRule.PrimaryKey;

            BsonDocument parent = dataOp.FindOneByQuery(tbName, Query.EQ("isTemplate", "1"));
            List<BsonDocument> dirList = dataOp.FindAllByQuery(dirName, Query.EQ(tbKey, parent.String(tbKey))).ToList();//获取模板下的目录
            List<TreeNode> treeList = TreeHelper.GetSingleTreeList(dirList);

            return new XmlTree(treeList);
        }

        public ActionResult SysModuleTree()
        {
            List<BsonDocument> dirList = dataOp.FindAll("SysModule").ToList();//获取模板下的目录
            List<TreeNode> treeList = TreeHelper.GetSingleTreeList(dirList);

            return new XmlTree(treeList);
        }

        /// <summary>
        /// 获取目录树形
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="moveId"></param>
        /// <param name="ddirId">目录树Id</param>
        /// <returns></returns>
        public ActionResult GetDirTree(string tbName, string moveId, string ddirId)
        {
            string dirName = tbName + "Dir";
            TableRule tbRule = new TableRule(tbName);
            TableRule dirRule = new TableRule(dirName);

            string tbKey = tbRule.PrimaryKey;
            string dirKey = dirRule.PrimaryKey;

            BsonDocument parent = dataOp.FindOneByQuery(tbName, Query.EQ(tbKey, ddirId));
            List<BsonDocument> dirList = dataOp.FindAllByQuery(dirName, Query.EQ(tbKey, parent.String(tbKey))).ToList();//获取模板下的目录
            List<TreeNode> treeList = TreeHelper.GetSingleTreeList(dirList);

            return new XmlTree(treeList);
        }

        /// <summary>
        /// 保存决策意见
        /// </summary>
        /// <param name="cellOpinions">细项意见</param>
        /// <param name="formOptions">表单意见</param>
        /// <param name="versionId">版本号</param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult SaveOpinion(string cellOpinions, string formOptions, string versionId)
        {
            PageJson json = new PageJson();
            try
            {
                //保存单元格意见
                string[] cellStrs = cellOpinions.SplitParam("|");
                foreach (var cells in cellStrs)
                {
                    string[] opinionInfos = cells.SplitParam(",");
                    if (opinionInfos.Length > 3)
                    {
                        string tbName = opinionInfos[0];
                        string pkName = opinionInfos[1];
                        string pkValue = opinionInfos[2];
                        string opinion = opinionInfos[3];
                        dataOp.Update(tbName, Query.EQ(pkName, pkValue), new BsonDocument { { "opinion", opinion } });
                    }
                }
                //保存表单的意见
                string[] formStrs = formOptions.SplitParam("|");
                foreach (var forms in formStrs)
                {
                    string[] opinionInfos = forms.SplitParam(",");
                    if (opinionInfos.Length > 1)
                    {
                        string tbName = opinionInfos[0];
                        string opinion = opinionInfos[1];
                        BsonDocument feedback = dataOp.FindOneByQuery("Feedback", Query.And(Query.EQ("tbName", tbName), Query.EQ("versionId", versionId)));
                        if (feedback != null)
                        {
                            dataOp.Update("Feedback", Query.EQ("feedbackId", feedback.String("feedbackId")), new BsonDocument { { "opinion", opinion } });
                        }
                        else
                        {
                            dataOp.Insert("Feedback", new BsonDocument { { "opinion", opinion }, { "tbName", tbName }, { "versionId", versionId }, { "type", "0" } });
                        }
                    }
                }

                json.Success = true;
            }
            catch
            {
                json.Success = false;
            }
            return Json(json);
        }

        /// <summary>
        /// 保存决策意见
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="pkName"></param>
        /// <param name="pkValue"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public JsonResult SaveFeedback(string tbName, string pkName, string pkValue, string status)
        {
            PageJson json = new PageJson();

            try
            {
                if (status == "1") status = "0";
                else status = "1";
                dataOp.Update(tbName, Query.EQ(pkName, pkValue), new BsonDocument { { "feedback", status } });
                json.Success = true;
            }
            catch
            {
                json.Success = false;
            }
            return Json(json);
        }

        ///// <summary>
        ///// 表单数据提交审核
        ///// </summary>
        ///// <param name="dirName">目录表名称</param>
        ///// <param name="projId">项目Id</param>
        ///// <param name="versionId">版本Id</param>
        ///// <returns></returns>
        //public JsonResult SubVerify(string dirName, string projId, string versionId)
        //{
        //    var result = ChangeFormStatus(dirName, projId, versionId, "1");//改为审核状态
        //    if (result.Status == Status.Successful)
        //    {
        //        LogPolicyVerify(FormDirDict[dirName], projId, versionId,"提交","");//记录日志
        //        AddOAPolicyMsg(dirName, projId, versionId);
        //    }
        //    return Json(ConvertToPageJson(result));
        //}


        /// <summary>
        /// 表单数据提交审核
        /// </summary>
        /// <param name="dirName">目录表名称</param>
        /// <param name="projId">项目Id</param>
        /// <param name="versionId">版本Id</param>
        /// <returns></returns>
        public void SubVerify(string dirName, string projId, string versionId)
        {
            var result = ChangeFormStatus(dirName, projId, versionId, "1");//改为审核状态
            if (result.Status == Status.Successful)
            {
                LogPolicyVerify(FormDirDict[dirName], projId, versionId, "提交", "");//记录日志
                AddOAPolicyMsg(dirName, projId, versionId);
            }
            //return Json(ConvertToPageJson(result));
        }


        /// <summary>
        /// 批量保存产品决策数据
        /// </summary>
        /// <param name="tbName">目录表名</param>
        /// <param name="dataSource">数据格式 </param>
        /// <returns></returns>
        public JsonResult BatchSavePolicyData(string tbName, string dataSource, string versionId,string projId,bool isSubmit)
        {
            PageJson json = new PageJson();
            InvokeResult result = new InvokeResult();
            /*字符串数据源 数据格式：
                目录id_#目录id值|#字段名_#字段值|#字段名_#字段值*#
                目录id_#目录id值|#字段名_#字段值|#字段名_#字段值
            其中每条数据*#结束，每个字段以|#结束，字段名与对应的字段值以_#连接
            表名主键传入名称统一用dirId
             * */
            bool cofirm = isConfirm(versionId);//保存还是提交数据
            List<StorageData> dataList = new List<StorageData>();
            TableRule table = new TableRule(tbName);
            #region 增加记录联动日志
            List<StorageData> oldLinkLog = SaveLinkLog(tbName, table.PrimaryKey);
            int oldLinkFlag = 0;//标志是否执行 0表示执行
            #endregion
            string[] item = dataSource.SplitParam(StringSplitOptions.RemoveEmptyEntries, "*#");//dataSource.Split('*');//每条数据对象字符串
            foreach (var tempItem in item)
            {
                BsonDocument tempEntity = new BsonDocument();
                StorageData tempStorageData = new StorageData();

                string[] property = tempItem.SplitParam(StringSplitOptions.RemoveEmptyEntries, "|#"); // tempItem.Split('|#');//单条数据所有属性字符串
                foreach (var tempProperty in property)
                {
                    string[] propertyItem = tempProperty.SplitParam(StringSplitOptions.None, "_#");//tempProperty.Split('_#');//单个属性对应值
                    if (propertyItem.Length == 2)
                    {
                        if (propertyItem[0] == table.PrimaryKey)
                        {
                            tempEntity.Add(propertyItem[0], propertyItem[1]);
                        }
                        else
                        {
                            if (!cofirm)
                            {
                                tempEntity.Add(propertyItem[0] + "_bak", propertyItem[1]);//没有实时保存产品决策数据，确认人确认之后才会复制变更。
                            }
                            else
                            {
                                tempEntity.Add(propertyItem[0], propertyItem[1]);//确认数据
                                tempEntity.Add(propertyItem[0] + "_bak", propertyItem[1]);//回写暂存区
                            }
                        }
                    }
                    else
                    {
                        json.Message = "数据源格式不正确！请重新确定数据源格式！";
                        json.Success = false;
                        return Json(json);
                    }
                }
                tempStorageData.Document = tempEntity;
                tempStorageData.Query = Query.EQ(table.PrimaryKey, tempEntity.String(table.PrimaryKey));
                tempStorageData.Name = tbName;
                tempStorageData.Type = StorageType.Update;
                dataList.Add(tempStorageData);
                if (cofirm)//保存正式数据
                {
                    //newData.Add(tempEntity);
                    if (oldLinkFlag == 0) 
                    {
                        dataList.AddRange(oldLinkLog);
                        oldLinkFlag = 1;
                    }
                    SaveLog(tbName, table.PrimaryKey, tempStorageData,projId,versionId);//保存日志
                }
            }
            result = dataOp.BatchSaveStorageData(dataList);
            if (result.Status == Status.Successful)
            {
                if (cofirm)
                {
                    ChangeFormStatus(tbName, projId, versionId, "0");//将表单置成录入状态
                    LogPolicyVerify(FormDirDict[tbName], projId, versionId, "通过", "");
                    UpdateOAPolicyMsg(tbName, projId, versionId);
                }

                if (isSubmit)
                {
                    SubVerify(tbName, projId, versionId);//提交
                }
            }

            

            json = TypeConvert.InvokeResultToPageJson(result);

            return this.Json(json);
        }



        /// <summary>
        /// 修改表单状态
        /// </summary>
        /// <param name="dirName">表单下目录Id</param>
        /// <param name="projId">项目Id</param>
        /// <param name="versionId">版本Id</param>
        /// <param name="status">要修改的标准状态</param>
        public InvokeResult ChangeFormStatus(string dirName,string projId,string versionId,string status)
        {
           string tbName = FormDirDict[dirName];//获取目录载的表单
           return dataOp.Update(tbName, Query.And(Query.EQ("projId", projId), Query.EQ("versionId", versionId)), new BsonDocument { { "status", status } });
           
        }

        /// <summary>
        /// 添加推送消息
        /// </summary>
        /// <param name="dirName"></param>
        /// <param name="projId"></param>
        /// <param name="versionId"></param>
        /// <returns></returns>
        public InvokeResult AddOAPolicyMsg(string dirName, string projId, string versionId)
        {
            var user = dataOp.FindOneByKeyVal("SysUser", "userId", this.CurrentUserId.ToString());
            string tableName = FormDirDict[dirName];
            BsonDocument msg = new BsonDocument { { "formName", tableName }, { "projId", projId }, { "versionId", versionId } };
            Authentication auth = new Authentication();
            PolicyFormInfo formInfo = FormInfoDict[tableName];
            var users = auth.GetUserOfProjectRight(AreaRoleType.Project, projId, formInfo.verifyKey);

            var proj = dataOp.FindOneByKeyVal("Project", "projId", projId);
            var land = dataOp.FindOneByKeyVal("Land", "landId", proj.String("landId"));

            int versionOrder = GetVersionOrder(projId, versionId);
            string title = string.Format("{0}-{1}-{2}V{3}版本-{4}", land.String("name"), proj.String("name"), "产品决策", versionOrder, formInfo.name);
            
            string url = string.Format("/PolicyDecision/PolicyMain/?projId={0}&versionId={1}&formName={2}", projId, versionId, formInfo.tbName);
            string OAMessage = "OAMessage";
            foreach (var item in users)
            {
                string acceptName = item.String("loginName");
                IMongoQuery query = Query.And(Query.EQ("projId", projId), Query.EQ("versionId", versionId), Query.EQ("acceptName", acceptName), Query.EQ("auditStatus", "0"), Query.EQ("formName", formInfo.tbName));
                if (!dataOp.FindAllByQuery(OAMessage, query).Any())
                {
                    dataOp.Insert(OAMessage, new BsonDocument{{"typeId","3"},{"projId",projId},{"versionId",versionId},{"formName",formInfo.tbName},
                            {"title",title},{"url",url},{"acceptName",item.String("loginName")},{"subUserId",user.String("userId")},{"subName",user.String("loginName")},{"auditStatus","0"}});
                }
                else
                {
                    dataOp.Update(OAMessage, query, new BsonDocument { { "createDate", DateTime.Now } });
                }
            }
            PushToSeeyon pushToSeeyon = new PushToSeeyon();
            string[] loginNames = users.Where(s => !string.IsNullOrEmpty(s.String("loginName"))).Select(s => s.String("loginName")).ToArray();

            CommonLog log = new CommonLog();
            string info = string.Join(",", loginNames);
            log.Info(info);

            pushToSeeyon.PushTodoInfo(loginNames,title,url);
            
            return new InvokeResult();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="projId"></param>
        /// <param name="versionId"></param>
        /// <returns></returns>
        public int GetVersionOrder(string projId, string versionId)
        {
            var query = dataOp.FindAllByKeyVal("PolicyVersion", "projId", projId).ToList();
            int tempId = int.Parse(versionId);

            return query.Where(s => s.Int("versionId") <= tempId).Count();
        }

        /// <summary>
        /// 修改成已经审核
        /// </summary>
        /// <param name="dirName"></param>
        /// <param name="projId"></param>
        /// <param name="versionId"></param>
        /// <returns></returns>
        public InvokeResult UpdateOAPolicyMsg(string dirName, string projId, string versionId)
        {
            string tableName = FormDirDict[dirName];
            PolicyFormInfo formInfo = FormInfoDict[tableName];
            IMongoQuery query = Query.And(Query.EQ("projId", projId), Query.EQ("versionId", versionId), Query.EQ("formName", formInfo.tbName));
            return dataOp.Update("OAMessage", query, new BsonDocument { { "auditStatus","1" } });

        }

        /// <summary>
        /// 记录决策审核信息
        /// </summary>
        /// <returns></returns>
        public InvokeResult LogPolicyVerify(string tableName,string projId,string versionId,string action,string content)
        {
            BsonDocument log = new BsonDocument{{"formName",tableName},{"projId",projId},{"versionId",versionId},{"action",action},
                                                {"content",content}};
            dataOp.Insert("PolicyVerifyLog",log);
            return new InvokeResult();
        }

        /// <summary>
        /// 保存决策日志日志
        /// </summary>
        /// <param name="tbName">表名称</param>
        /// <param name="tbKey">主键名称</param>
        /// <param name="storage"></param>
        public void SaveLog(string tbName, string tbKey, StorageData storage,string projId,string versionId)
        {
            HashSet<string> tableSet = new HashSet<string> { "ProjNodeDir", "MarketingAreaDir", "DesignAreaDir", "CostDir" };
            if (tableSet.Contains(tbName))
            {
                BsonDocument old = dataOp.FindOneByQuery(storage.Name, storage.Query);
                if (old != null)
                {
                    BsonDocument doc = new BsonDocument { { "tbName", tbName }, { "tbKey", tbKey } };//表名主键名
                    BsonDocument update = storage.Document;
                    BsonDocument firstLog = dataOp.FindOneByQuery("PolicyLog", Query.And(Query.EQ("tbName", tbName), Query.EQ(tbKey, old.String(tbKey))));//是否已经存在记录
                    bool tag = false;
                    foreach (var key in update.Elements.Select(s => s.Name))
                    {
                        if (!tag && old.String(key) != update.String(key))
                        {
                            tag = true;
                        }
                        doc.Add(key, old.String(key));
                    }
                    bool canLog = CanLog(old, update, tbKey, firstLog != null);
                    if (tag && canLog) {
                        var tempResult= dataOp.Insert("PolicyLog", doc);//所有属性不全部为空，且有变化才保存
                        BsonDocument tempLog=tempResult.BsonInfo;
                        BsonDocument linkLog = new BsonDocument { { "tbName", tbName }, { "keyName", tbKey },{"keyValue",old.String(tbKey)},{"projId",projId},{"versionId",versionId},{"logId",tempLog.String("logId")},{"status","1"} };//表名主键名
                         dataOp.Insert("ProjVersionLinkLog", linkLog);//插入联动日志
                    }
                }
            }
        }
        /// <summary>
        /// 修改决策联动日志 将旧数据状态值置为2
        /// </summary>
        /// <param name="tbName">表名称</param>
        /// <param name="tbKey">主键名称</param>
        /// <param name="storage"></param>
        public List<StorageData> SaveLinkLog(string tbName, string tbKey)
        {
            HashSet<string> tableSet = new HashSet<string> { "ProjNodeDir", "MarketingAreaDir", "DesignAreaDir", "CostDir" };
            List<StorageData> returnData=new List<StorageData>();
            if (tableSet.Contains(tbName))
            {
                //处理上一次审核的数据日志记录
                List<BsonDocument> oldLog = dataOp.FindAllByQuery("ProjVersionLinkLog", Query.And(Query.EQ("tbName", tbName), Query.EQ("keyName", tbKey), Query.EQ("status", "1"))).ToList();
                for(int i=0;i<oldLog.Count();i++){
                    BsonDocument tempOldLog=oldLog[i];
                    tempOldLog["status"]="2";
                    StorageData tempReturn=new StorageData();
                    tempReturn.Name = "ProjVersionLinkLog";
                    tempReturn.Document=tempOldLog;
                    tempReturn.Type=StorageType.Update;
                    tempReturn.Query=Query.EQ("linkLogId",tempOldLog.String("linkLogId"));
                    returnData.Add(tempReturn);
                }
            }
            return returnData;
        }
        /// <summary>
        /// 判断是保存日志
        /// </summary>
        /// <param name="old"></param>
        /// <param name="update"></param>
        /// <param name="tbKey"></param>
        /// <param name="hasLog"></param>
        /// <returns></returns>
        public bool CanLog(BsonDocument old, BsonDocument update, string tbKey, bool hasLog)
        {
            bool canLog = false;
            if (!hasLog)
            {
                foreach (var key in update.Elements.Select(s => s.Name))
                {
                    if (key != tbKey)
                    {
                        if (!string.IsNullOrEmpty(old.String(key)))
                        {
                            canLog = true;
                            break;
                        }
                    }
                }
            }
            else
            {
                canLog = true;
            }
            return canLog;
        }

        [HttpPost]
        public JsonResult ConfirmPolicy(string tbName, string versionId)
        {
            PageJson json = new PageJson { Success = false };
            int tempVersionId;
            if (int.TryParse(versionId, out tempVersionId) && tempVersionId > 0)
            {
                try
                {
                    PolicyVersionBll versionBll = PolicyVersionBll._(dataOp);
                    versionBll.ConfirmPolicyData(tbName, versionId);
                    json.Success = true;
                }
                catch
                {
                    json.Success = false;
                }
            }
            return Json(json);
        }

        #endregion


        /// <summary>
        /// 通知已有项目模板有改动
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public JsonResult IssuseTemplateVersion(string remark)
        {
            PageJson json = new PageJson();
            try
            {
                /*通知机制 通知表中一个项目永远只记录一条数据 */
                List<BsonDocument> projList = dataOp.FindAll("Project").ToList();
                PolicyVersionBll versionBll=PolicyVersionBll._();
                List<StorageData> data = new List<StorageData>();
                var query = dataOp.FindAll("PolicyVersion").ToList();
                foreach (var tempProj in projList) {
                    List<string> value = GetProjVersionIdAndOrder(query,tempProj.String("projId"));
                    if (value.Count() > 1) 
                    {
                        BsonDocument tempInfo = dataOp.FindOneByQuery("ProjVersionNotice",Query.EQ("projId", tempProj.String("projId")));
                        if (tempInfo != null)
                        {
                            tempInfo["versionId"] = value[0];
                            tempInfo["versionOrder"] = value[1];
                            tempInfo["remark"] = remark;
                            var result = dataOp.Update("ProjVersionNotice", Query.EQ("projVerNoticeId", tempInfo.String("projVerNoticeId")), tempInfo);
                        }
                        else
                        {
                            tempInfo = new BsonDocument().Add("projId", tempProj.String("projId")).Add("versionOrder", value[1]).Add("remark", remark).Add("versionId", value[0]);
                            var result = dataOp.Insert("ProjVersionNotice", tempInfo);
                        }
                    }
                }
                json.Success = true;
            }
            catch
            {
                json.Success = false;
            }
            return Json(json);
        }
        /// <summary>
        /// 获取最新版本及版本序号
        /// </summary>
        /// <param name="versionList"></param>
        /// <param name="projId"></param>
        /// <returns></returns>
        public List<string> GetProjVersionIdAndOrder(List<BsonDocument> versionList,string projId) 
        {
            List<string> value = new List<string>();
            var query = versionList.Where(x => x.String("projId") == projId).ToList();
            var preVersionId = 0;
            if (query.Any())
            {
               preVersionId =query.Select(s => s.Int("versionId")).Max();
               var last = query.Where(x => x.String("versionId") == preVersionId.ToString()).FirstOrDefault();
               value.Add(preVersionId.ToString());
               value.Add(last.String("versionOrder"));
            }
            return value;
        }


        /// <summary>
        /// 批量更新目录属性值
        /// </summary>
        /// <returns></returns>
        public JsonResult BatchSaveDir()
        {
            PageJson json = new PageJson();
            InvokeResult result = new InvokeResult();
            string tbName = PageReq.GetParam("tbName");//目录项表名
            string tbRootName = tbName.Replace("Dir", "");//表单根的表名
            int confirmType = PageReq.GetFormInt("confirmType");//1:确认 0或其他：保存
            int type = PageReq.GetFormInt("type");//type 0:保存 1 提交审核 2 通过审核  3驳回审核
            int ddirId = PageReq.GetFormInt("ddirId");//表单根的Id
            int typeId = PageReq.GetFormInt("typeId");//消息类型Id：1情报库，2产品定位，3产品决策
            //字符串数据源 数据格式：目录id_#目录id值|#字段名_#字段值|#字段名_#字段值*#目录id_#目录id值|#字段名_#字段值|#字段名_#字段值
            //其中每条数据*#结束，每个字段以|#结束，字段名与对应的字段值以_#连接
            //表名主键传入名称统一用dirId
            string dataSource = PageReq.GetForm("dataSource");
            List<StorageData> dataList = new List<StorageData>();
            TableRule table = new TableRule(tbName);
            string primaryKey = string.Empty;
            primaryKey = table.GetPrimaryKey();
            #region 处理数据源


            //string[] item = Regex.Split(dataSource, @"\*\#", RegexOptions.IgnoreCase);//dataSource.Split('*');//每条数据对象字符串
            string[] item = dataSource.SplitParam(StringSplitOptions.RemoveEmptyEntries, "*#");//dataSource.Split('*');//每条数据对象字符串
            foreach (var tempItem in item)
            {
                BsonDocument tempEntity = new BsonDocument();
                StorageData tempStorageData = new StorageData();

                string[] property = Regex.Split(tempItem, @"\|\#", RegexOptions.IgnoreCase); // tempItem.Split('|#');//单条数据所有属性字符串
                foreach (var tempProperty in property)
                {
                    string[] propertyItem = Regex.Split(tempProperty, @"_\#", RegexOptions.IgnoreCase);//tempProperty.Split('_#');//单个属性对应值
                    if (propertyItem.Length == 2)
                    {
                        if (propertyItem[0] == "dirId")
                        {
                            tempEntity.Add(primaryKey, propertyItem[1]);
                        }
                        else
                        {
                            if (confirmType != 1)
                            {
                                tempEntity.Add(propertyItem[0] + "_bak", propertyItem[1]);
                            }
                            else
                            {
                                tempEntity.Add(propertyItem[0], propertyItem[1]);
                                tempEntity.Add(propertyItem[0] + "_bak", propertyItem[1]);//回写暂存区
                            }
                        }
                    }
                    else
                    {
                        json.Message = "数据源格式不正确！请重新确定数据源格式！";
                        json.Success = false;
                    }
                }

                tempStorageData.Document = tempEntity;
                tempStorageData.Query = Query.EQ(primaryKey, tempEntity.String(primaryKey));
                tempStorageData.Name = tbName;
                tempStorageData.Type = StorageType.Update;
                dataList.Add(tempStorageData);
            }
            if (type == 1)
            {
                //提交审核加入审核信息及记录

                #region 获取地块 项目信息
                int landId = 0;//地块Id
                int projId = 0;//项目Id
                BsonDocument projInfo = new BsonDocument();//项目信息
                if (typeId == 1)
                {
                    landId = PageReq.GetFormInt("landOrProjId");
                }
                else if (typeId == 2)
                {
                    projId = PageReq.GetFormInt("landOrProjId");
                    projInfo = dataOp.FindOneByQuery("Project", Query.EQ("projId", projId.ToString()));
                    landId = projInfo.Int("landId");

                }
                BsonDocument landInfo = dataOp.FindOneByQuery("Land", Query.EQ("landId", landId.ToString()));//地块信息
                #endregion

                TableRule table1 = new TableRule(tbRootName);
                string keyName1 = table1.PrimaryKey;
                BsonDocument entity = dataOp.FindOneByQuery(tbRootName, Query.EQ(keyName1, ddirId.ToString()));
                dataList.AddRange(GetSubmitFormAuditData(tbRootName, typeId, landInfo, projInfo, entity));
            }
            else if (type == 2) 
            {
                dataList.AddRange(GetAuditFormTableData(ddirId,tbRootName,1));
            }
            result = dataOp.BatchSaveStorageData(dataList);
            json = TypeConvert.InvokeResultToPageJson(result);
            #endregion
            return this.Json(json);
        }

        /// <summary>
        /// 批量更新竞品楼盘属性值
        /// </summary>
        /// <returns></returns>
        public JsonResult BatchSaveHouseAttr()
        {
            PageJson json = new PageJson();
            InvokeResult result = new InvokeResult();
            string tbName = PageReq.GetParam("tbName");//属性保存表名
            string tbRootName = string.Empty;//表单根的表名
            int confirmType = PageReq.GetFormInt("confirmType");//1:确认 0或其他：保存
            int type = PageReq.GetFormInt("type");//type 0:保存 1 提交审核 2 通过审核  3驳回审核
            int ddirId = PageReq.GetFormInt("ddirId");//表单根的Id
            int typeId = PageReq.GetFormInt("typeId");//消息类型Id：1情报库，2产品定位，3产品决策
            //dataSource 
            //字符串数据源 数据格式：目录id_#目录id值|#楼盘Id#楼盘Id值|#字段名_#字段值*#目录id_#目录id值|#楼盘Id#楼盘Id值|#字段名_#字段值
            //其中每条数据*#结束，每个字段以|#结束，字段名与对应的字段值以_#连接
            //表名主键传入名称统一用dirId
            string dataSource = PageReq.GetForm("dataSource");
            string houseIdStr = PageReq.GetForm("houseIdStr");
            List<StorageData> dataList = new List<StorageData>();
            string ddirIdName = string.Empty;//楼盘目录id字段名
            string houseIdName = string.Empty;//楼盘实体主键字段名
            //TableRule table = new TableRule(tbName);
            //string primaryKey = string.Empty;
            //primaryKey = table.GetPrimaryKey();
            //primaryKey = "comHouseDirId";
            if (tbName == "HouseAttr")
            {
                ddirIdName = "comHouseDirId";
                houseIdName = "houseEntityId";
                tbRootName = "CompetitiveHouse";
            }
            else if (tbName == "ProjHouseAttr")
            {
                ddirIdName = "correctDirId";
                houseIdName = "projHouseId";
                tbRootName = "ExternalCorrect";
            }
            #region 处理楼盘Id及查找所有相关属性值
            List<string> houseIdList = new List<string>();
            if (!string.IsNullOrEmpty(houseIdStr))
            {
                houseIdList = houseIdStr.Split(',').ToList();
            }
            List<BsonDocument> exitHouseAttr = new List<BsonDocument>();//已经存在的楼盘属性值
            exitHouseAttr = dataOp.FindAllByQuery(tbName, Query.In(houseIdName, TypeConvert.StringListToBsonValueList(houseIdList))).ToList();
            #endregion


            #region 处理数据源
            string[] item = Regex.Split(dataSource, @"\*\#", RegexOptions.IgnoreCase);//dataSource.Split('*');//每条数据对象字符串
            foreach (var tempItem in item)
            {
                BsonDocument tempEntity = new BsonDocument();
                StorageData tempStorageData = new StorageData();
                if (!string.IsNullOrEmpty(tempItem))
                {
                    string[] property = Regex.Split(tempItem, @"\|\#", RegexOptions.IgnoreCase); // tempItem.Split('|#');//单条数据所有属性字符串
                    foreach (var tempProperty in property)
                    {
                        string[] propertyItem = Regex.Split(tempProperty, @"_\#", RegexOptions.IgnoreCase);//tempProperty.Split('_#');//单个属性对应值
                        if (propertyItem.Length == 2)
                        {
                            if (propertyItem[0] == "dirId")
                            {
                                tempEntity.Add(ddirIdName, propertyItem[1]);
                            }
                            else
                            {
                                if (propertyItem[0] != ddirIdName && propertyItem[0] != houseIdName)
                                {
                                    if (confirmType != 1)
                                    {
                                        tempEntity.Add(propertyItem[0] + "_bak", propertyItem[1]);//保存到缓存区域数据
                                    }
                                    else
                                    {
                                        tempEntity.Add(propertyItem[0], propertyItem[1]);
                                        tempEntity.Add(propertyItem[0] + "_bak", propertyItem[1]);//回写到缓存区域
                                    }
                                }
                                else
                                {
                                    tempEntity.Add(propertyItem[0], propertyItem[1]);
                                }
                            }
                        }
                        else
                        {
                            json.Message = "数据源格式不正确！请重新确定数据源格式！";
                            json.Success = false;
                        }
                    }

                    var isExit = exitHouseAttr.Where(x => x.String(houseIdName) == tempEntity.String(houseIdName) && x.String(ddirIdName) == tempEntity.String(ddirIdName)).FirstOrDefault();
                    if (isExit != null)
                    {

                        if (!tempEntity.ContainsColumn("name"))
                        {
                            if (!isExit.ContainsColumn("name"))
                            {
                                tempEntity.Add("name", isExit.String("name"));
                            }
                        }

                        if (tbName == "ProjHouseAttr")
                        {

                            if (!tempEntity.ContainsColumn("mark"))
                            {
                                if (!isExit.ContainsColumn("mark"))
                                {
                                    tempEntity.Add("mark", isExit.String("mark"));
                                }
                            }
                        }

                        tempStorageData.Document = tempEntity;
                        tempStorageData.Query = Query.And(Query.EQ(ddirIdName, tempEntity.String(ddirIdName)), Query.EQ(houseIdName, tempEntity.String(houseIdName)));
                        tempStorageData.Name = tbName;
                        tempStorageData.Type = StorageType.Update;
                        dataList.Add(tempStorageData);
                        exitHouseAttr.Remove(isExit);
                    }
                    else
                    {
                        tempStorageData.Document = tempEntity;
                        tempStorageData.Query = Query.And(Query.EQ(ddirIdName, tempEntity.String(ddirIdName)), Query.EQ(houseIdName, tempEntity.String(houseIdName)));
                        tempStorageData.Name = tbName;
                        tempStorageData.Type = StorageType.Insert;
                        dataList.Add(tempStorageData);
                    }

                }
            }
            foreach (var delAttr in exitHouseAttr)
            {
                StorageData tempStorageData = new StorageData();
                tempStorageData.Document = delAttr;
                tempStorageData.Query = Query.And(Query.EQ(ddirIdName, delAttr.String(ddirIdName)), Query.EQ(houseIdName, delAttr.String(houseIdName)));
                tempStorageData.Name = tbName;
                tempStorageData.Type = StorageType.Delete;
                dataList.Add(tempStorageData);
            }
            if (type == 1)
            {
                //提交审核加入审核信息及记录

                #region 获取地块 项目信息
                int landId = 0;//地块Id
                int projId = 0;//项目Id
                BsonDocument projInfo = new BsonDocument();//项目信息
                if (typeId == 1)
                {
                    landId = PageReq.GetFormInt("landOrProjId");
                }
                else if (typeId == 2)
                {
                    projId = PageReq.GetFormInt("landOrProjId");
                    projInfo = dataOp.FindOneByQuery("Project", Query.EQ("projId", projId.ToString()));
                    landId = projInfo.Int("landId");

                }
                BsonDocument landInfo = dataOp.FindOneByQuery("Land", Query.EQ("landId", landId.ToString()));//地块信息
                #endregion

                TableRule table1 = new TableRule(tbRootName);
                string keyName1 = table1.PrimaryKey;
                BsonDocument entity = dataOp.FindOneByQuery(tbRootName, Query.EQ(keyName1, ddirId.ToString()));
                dataList.AddRange(GetSubmitFormAuditData(tbRootName, typeId, landInfo, projInfo, entity));
            }
            else if (type == 2)
            {
                dataList.AddRange(GetAuditFormTableData(ddirId, tbRootName, 1));
            }
            result = dataOp.BatchSaveStorageData(dataList);
            json = TypeConvert.InvokeResultToPageJson(result);
            #endregion
            return this.Json(json);
        }
        /// <summary>
        /// 删除楼盘及楼盘属性值
        /// </summary>
        /// <returns></returns>
        public JsonResult deleteHouse()
        {
            PageJson json = new PageJson();
            InvokeResult result = new InvokeResult();
            int houseId = PageReq.GetParamInt("houseId");
            string tbName = PageReq.GetParam("tbName");
            var entity = dataOp.FindOneByQuery("HouseEntity", Query.EQ("houseEntityId", houseId.ToString()));
            if (entity == null)
            {
                json.Message = "楼盘实体不存在，请联系管理员！";
                json.Success = false;
                return this.Json(json);
            }
            List<StorageData> storageDataList = new List<StorageData>();
            StorageData storageData = new StorageData();
            storageData.Document = entity;
            storageData.Type = StorageType.Delete;
            storageData.Name = "HouseEntity";
            storageData.Query = Query.EQ("houseEntity", houseId.ToString());
            storageDataList.Add(storageData);
            List<BsonDocument> entityAttr = new List<BsonDocument>();
            entityAttr = dataOp.FindAllByQuery("HouseAttr", Query.EQ("houseEntityId", houseId.ToString())).ToList();
            if (entityAttr.Count() > 0)
            {
                foreach (var attr in entityAttr)
                {
                    StorageData tempAttr = new StorageData();
                    tempAttr.Document = attr;
                    tempAttr.Name = "HouseAttr";
                    tempAttr.Type = StorageType.Delete;
                    tempAttr.Query = Query.EQ("houseAttrId", attr.String("houseAttrId"));
                    storageDataList.Add(tempAttr);
                }
            }
            result = dataOp.BatchSaveStorageData(storageDataList);
            json = TypeConvert.InvokeResultToPageJson(result);

            return this.Json(json);
        }
        /// <summary>
        /// 获取业态json格式
        /// </summary>
        /// <returns></returns>
        public JsonResult GetPropertyJson()
        {
            List<BsonDocument> propertyList = dataOp.FindAll("SystemProperty").ToList();
            List<Item> itemList = new List<Item>();
            itemList = (from p in propertyList
                        select new Item()
                        {
                            id = p.Int("propertyId"),
                            name = p.Text("name"),
                            isUse = p.Int("isUse")
                        }).ToList();
            return Json(itemList, JsonRequestBehavior.AllowGet);
        }
        /// <summary>
        ///保存项目业态json格式
        /// </summary>
        /// <returns></returns>
        public JsonResult SavePropertyJson()
        {
            PageJson json = new PageJson();
            InvokeResult result = new InvokeResult();
            string propertyStr = PageReq.GetParam("ids");
            int projId = PageReq.GetParamInt("projId");
            List<string> propertyIdList = new List<string>();
            List<string> newPropertyIdList = new List<string>();
            if (!string.IsNullOrEmpty(propertyStr))
            {
                propertyIdList = propertyStr.Split(',').ToList();
            }
            List<BsonDocument> propertyList = dataOp.FindAllByQuery("SystemProperty", Query.In("propertyId", TypeConvert.StringListToBsonValueList(propertyIdList))).ToList();
            var exitProperty = dataOp.FindAllByQuery("ProjectProperty", Query.EQ("projId", projId.ToString())).ToList();
            List<StorageData> dataList = new List<StorageData>();
            foreach (var tempProperty in propertyIdList)
            {
                var temp = exitProperty.Where(x => x.String("propertyId") == tempProperty).FirstOrDefault();

                if (temp != null) { exitProperty.Remove(temp); }
                else
                {
                    BsonDocument tempProper = new BsonDocument();
                    tempProper.Add("projId", projId.ToString());
                    tempProper.Add("propertyId", tempProperty);
                    StorageData tempData = new StorageData();
                    tempData.Name = "ProjectProperty";
                    tempData.Type = StorageType.Insert;
                    tempData.Document = tempProper;
                    dataList.Add(tempData);
                    newPropertyIdList.Add(tempProperty);
                }
            }
            //同步删除已经存在但没有被选中的
            List<string> delPropertyId = new List<string>();
            if (exitProperty.Count() > 0)
            {
                foreach (var tempProperty in exitProperty)
                {
                    StorageData tempData = new StorageData();
                    tempData.Name = "ProjectProperty";
                    tempData.Type = StorageType.Delete;
                    tempData.Query = Query.EQ("projPropertyId", tempProperty.String("projPropertyId"));
                    tempData.Document = tempProperty;
                    dataList.Add(tempData);
                    delPropertyId.Add(tempProperty.String("propertyId"));
                }
            }
            result = dataOp.BatchSaveStorageData(dataList);
            if (result.Status == Status.Successful)
            {
                List<BsonDocument> newProperty = dataOp.FindAllByQuery("ProjectProperty", Query.And(Query.EQ("projId", projId.ToString()), Query.In("propertyId", TypeConvert.StringListToBsonValueList(newPropertyIdList)))).ToList();
                List<object> tempNewProperty = new List<object>();
                foreach (var tempP in newProperty)
                {
                    object tempObj = new
                    {
                        projId = tempP.String("projId"),
                        propertyId = tempP.String("propertyId"),
                        projPropertyId = tempP.String("projPropertyId"),
                        name = propertyList.Where(x => x.String("propertyId") == tempP.String("propertyId")).FirstOrDefault().String("name"),
                    };
                    tempNewProperty.Add(tempObj);
                }
                json.htInfo = new System.Collections.Hashtable();
                json.htInfo.Add("newProperty", tempNewProperty);
                json.htInfo.Add("delProperty", delPropertyId);
                json.Success = true;
            }
            else
            {
                json = TypeConvert.InvokeResultToPageJson(result);
            }

            return Json(json);
        }
        /// <summary>
        /// 保存项目定价建议内容
        /// </summary>
        /// <returns></returns>
        public JsonResult BatchSaveProjectPrice()
        {
            PageJson json = new PageJson();
            InvokeResult result = new InvokeResult();
            string tbName = PageReq.GetParam("tbName");//属性保存表名
            string tbRootName = string.Empty;//表单根的表名
            int confirmType = PageReq.GetFormInt("confirmType");//1:确认 0或其他：保存
            int type = PageReq.GetFormInt("type");//type 0:保存 1 提交审核 2 通过审核  3驳回审核
            int ddirId = PageReq.GetFormInt("ddirId");//表单根的Id
            int typeId = PageReq.GetFormInt("typeId");//消息类型Id：1情报库，2产品定位，3产品决策
            //dataSource 
            //字符串数据源 数据格式：目录id_#目录id值|#楼盘Id#楼盘Id值|#字段名_#字段值*#目录id_#目录id值|#楼盘Id#楼盘Id值|#字段名_#字段值
            //其中每条数据*#结束，每个字段以|#结束，字段名与对应的字段值以_#连接
            //表名主键传入名称统一用dirId
            string dataSource = PageReq.GetForm("dataSource");
            string houseIdStr = PageReq.GetForm("houseIdStr");
            //数据格式： 定价建议Id_#定义建议Id值|#建议值字段名_#建议值字段值*#定价建议Id_#定义建议Id值|#建议值字段名_#建议值字段值
            string priceStr = PageReq.GetForm("priceStr");
            List<StorageData> dataList = new List<StorageData>();
            string ddirIdName = string.Empty;//楼盘目录id字段名
            string houseIdName = string.Empty;//楼盘实体主键字段名
            //TableRule table = new TableRule(tbName);
            //string primaryKey = string.Empty;
            //primaryKey = table.GetPrimaryKey();
            //primaryKey = "comHouseDirId";
            if (tbName == "HouseAttr")
            {
                ddirIdName = "comHouseDirId";
                houseIdName = "houseEntityId";
                tbRootName = "ExternalCorrect";
            }
            else if (tbName == "ProjHouseAttr")
            {
                ddirIdName = "correctDirId";
                houseIdName = "projHouseId";
                tbRootName = "ExternalCorrect";
            }
            #region 处理楼盘Id及查找所有相关属性值
            List<string> houseIdList = new List<string>();
            if (!string.IsNullOrEmpty(houseIdStr))
            {
                houseIdList = houseIdStr.Split(',').ToList();
            }
            List<BsonDocument> exitHouseAttr = new List<BsonDocument>();//已经存在的楼盘属性值
            exitHouseAttr = dataOp.FindAllByQuery(tbName, Query.In(houseIdName, TypeConvert.StringListToBsonValueList(houseIdList))).ToList();
            #endregion


            #region 处理数据源

            //定价建议
            string[] priceItem = Regex.Split(priceStr, @"\*\#", RegexOptions.IgnorePatternWhitespace);
            foreach (var tempPriceItem in priceItem)
            {
                if (!string.IsNullOrEmpty(tempPriceItem))
                {
                    BsonDocument tempEntity = new BsonDocument();
                    StorageData tempStorageData = new StorageData();

                    string[] singlePriceItem = Regex.Split(tempPriceItem, @"\|\#", RegexOptions.IgnorePatternWhitespace);//单条数据所有属性字符串
                    foreach (var tempPrice in singlePriceItem)
                    {
                        string[] tempSinglePriceItem = Regex.Split(tempPrice, @"_\#", RegexOptions.IgnorePatternWhitespace);//单个属性对应值
                        if (tempSinglePriceItem.Length == 2)
                        {
                            if (tempSinglePriceItem[0] == "dirId")
                            {
                                tempEntity.Add(ddirIdName, tempSinglePriceItem[1]);
                            }
                            else
                            {
                                tempEntity.Add(tempSinglePriceItem[0], tempSinglePriceItem[1]);
                            }
                        }
                        else
                        {
                            json.Message = "数据源格式不正确！请重新确定数据源格式！";
                            json.Success = false;
                        }
                    }
                    tempStorageData.Document = tempEntity;
                    tempStorageData.Query = Query.EQ("projPropertyId", tempEntity.String("projPropertyId"));
                    tempStorageData.Name = "ProjectProperty";
                    tempStorageData.Type = StorageType.Update;
                    dataList.Add(tempStorageData);
                }
            }

            //外部系数
            string[] item = Regex.Split(dataSource, @"\*\#", RegexOptions.IgnorePatternWhitespace);//dataSource.Split('*');//每条数据对象字符串
            foreach (var tempItem in item)
            {
                BsonDocument tempEntity = new BsonDocument();
                StorageData tempStorageData = new StorageData();
                if (!string.IsNullOrEmpty(tempItem))
                {
                    string[] property = Regex.Split(tempItem, @"\|\#", RegexOptions.IgnorePatternWhitespace); // tempItem.Split('|#');//单条数据所有属性字符串
                    foreach (var tempProperty in property)
                    {
                        string[] propertyItem = Regex.Split(tempProperty, @"_\#", RegexOptions.IgnorePatternWhitespace);//tempProperty.Split('_#');//单个属性对应值
                        if (propertyItem.Length == 2)
                        {
                            if (propertyItem[0] == "dirId")
                            {
                                tempEntity.Add(ddirIdName, propertyItem[1]);
                            }
                            else
                            {
                                if (propertyItem[0] != houseIdName && propertyItem[0] != ddirIdName)
                                {
                                    if (confirmType != 1)
                                    {
                                        tempEntity.Add(propertyItem[0] + "_bak", propertyItem[1]);
                                    }
                                    else
                                    {
                                        tempEntity.Add(propertyItem[0], propertyItem[1]);
                                        tempEntity.Add(propertyItem[0] + "_bak", propertyItem[1]);//返写回缓存数据区
                                    }
                                }
                                else
                                {
                                    tempEntity.Add(propertyItem[0], propertyItem[1]);
                                }
                            }
                        }
                        else
                        {
                            json.Message = "数据源格式不正确！请重新确定数据源格式！";
                            json.Success = false;
                        }
                    }

                    var isExit = exitHouseAttr.Where(x => x.String(houseIdName) == tempEntity.String(houseIdName) && x.String(ddirIdName) == tempEntity.String(ddirIdName)).FirstOrDefault();
                    if (isExit != null)
                    {
                        if (!tempEntity.ContainsColumn("name"))
                        {
                            if (isExit.ContainsColumn("name"))
                            {
                                tempEntity.Add("name", isExit.String("name"));
                            }
                        }

                        if (tbName == "ProjHouseAttr")
                        {

                            if (isExit.ContainsColumn("mark"))
                            {
                                if (!tempEntity.ContainsColumn("mark"))
                                {
                                    tempEntity.Add("mark", isExit.String("mark"));
                                }
                            }
                        }
                        tempStorageData.Document = tempEntity;
                        tempStorageData.Query = Query.And(Query.EQ(ddirIdName, tempEntity.String(ddirIdName)), Query.EQ(houseIdName, tempEntity.String(houseIdName)));
                        tempStorageData.Name = tbName;
                        tempStorageData.Type = StorageType.Update;
                        dataList.Add(tempStorageData);
                        exitHouseAttr.Remove(isExit);
                    }
                    else
                    {
                        tempStorageData.Document = tempEntity;
                        tempStorageData.Query = Query.And(Query.EQ(ddirIdName, tempEntity.String(ddirIdName)), Query.EQ(houseIdName, tempEntity.String(houseIdName)));
                        tempStorageData.Name = tbName;
                        tempStorageData.Type = StorageType.Insert;
                        dataList.Add(tempStorageData);
                    }

                }
            }
            foreach (var delAttr in exitHouseAttr)
            {
                StorageData tempStorageData = new StorageData();
                tempStorageData.Document = delAttr;
                tempStorageData.Query = Query.And(Query.EQ(ddirIdName, delAttr.String(ddirIdName)), Query.EQ(houseIdName, delAttr.String(houseIdName)));
                tempStorageData.Name = tbName;
                tempStorageData.Type = StorageType.Delete;
                dataList.Add(tempStorageData);
            }

            if (type == 1)
            {
                //提交审核加入审核信息及记录

                #region 获取地块 项目信息
                int landId = 0;//地块Id
                int projId = 0;//项目Id
                BsonDocument projInfo = new BsonDocument();//项目信息
                if (typeId == 1)
                {
                    landId = PageReq.GetFormInt("landOrProjId");
                }
                else if (typeId == 2)
                {
                    projId = PageReq.GetFormInt("landOrProjId");
                    projInfo = dataOp.FindOneByQuery("Project", Query.EQ("projId", projId.ToString()));
                    landId = projInfo.Int("landId");

                }
                BsonDocument landInfo = dataOp.FindOneByQuery("Land", Query.EQ("landId", landId.ToString()));//地块信息
                #endregion

                TableRule table1 = new TableRule(tbRootName);
                string keyName1 = table1.PrimaryKey;
                BsonDocument entity = dataOp.FindOneByQuery(tbRootName, Query.EQ(keyName1, ddirId.ToString()));
                dataList.AddRange(GetSubmitFormAuditData(tbRootName, typeId, landInfo, projInfo, entity));
            }
            else if (type == 2)
            {
                dataList.AddRange(GetAuditFormTableData(ddirId, tbRootName, 1));
            }


            result = dataOp.BatchSaveStorageData(dataList);
            json = TypeConvert.InvokeResultToPageJson(result);
            #endregion
            return this.Json(json);
        }
        /// <summary>
        /// 保存土地或项目的目录
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult SaveGeneralDir()
        {
            PageJson json = new PageJson();
            InvokeResult result = new InvokeResult();
            string tbName = PageReq.GetParam("tbName");//表名
            int belongSource = PageReq.GetParamInt("belongSource");//判断挂在项目下还是地块下1：地块下 2：项目下
            int belongValue = PageReq.GetParamInt("belongValue");//项目或地块的id
            int templateId = PageReq.GetParamInt("templateId");//复制模板的Id
            TableRule tableEntity = new TableRule(tbName);
            string primaryKey = tableEntity.GetPrimaryKey();
            BsonDocument entity = new BsonDocument();
            BsonDocument templateInfo = dataOp.FindOneByQuery(tbName, Query.EQ(primaryKey, templateId.ToString()));
            List<BsonDocument> dataSubList = new List<BsonDocument>();//实体详细目录
            List<BsonDocument> templateDataSubList = new List<BsonDocument>();//模板详细目录
            if (templateInfo == null)
            {
                templateInfo = new BsonDocument();
                templateInfo = dataOp.FindOneByQuery(tbName, Query.EQ("isTemplate", "1"));
            }
            if (templateInfo != null)
            {
                templateId = templateInfo.Int(primaryKey);
                if (belongSource == 1)
                {
                    BsonDocument land = new BsonDocument();//地块信息
                    land = dataOp.FindOneByQuery("Land", Query.EQ("landId", belongValue.ToString()));
                    //string landName = string.Empty;
                    //if (land != null) { landName = land.String("name"); }
                    entity = dataOp.FindOneByQuery(tbName, Query.EQ("landId", belongValue.ToString()));
                    if (entity == null)
                    {
                        entity = new BsonDocument();
                        entity.Add("name", land != null ? land.String("name") : string.Empty + "目录");
                        entity.Add("landId", belongValue.ToString());
                        entity.Add("isTemplate", "0");
                        entity.Add("srcId", templateId.ToString());
                        result = dataOp.Insert(tbName, entity);
                        entity = result.BsonInfo;
                    }
                    dataSubList = dataOp.FindAllByQuery(tbName + "Dir", Query.EQ(primaryKey, entity.String(primaryKey))).ToList();
                    if (dataSubList.Count() == 0)
                    {
                        templateDataSubList = dataOp.FindAllByQuery(tbName + "Dir", Query.EQ(primaryKey, templateId.ToString())).ToList();
                        TemplateDirBll tdBll = TemplateDirBll._();
                        List<BsonDocument> newDir = new List<BsonDocument>();
                        tdBll.CopyGeneralDir(newDir, templateDataSubList, 0, null, tbName, entity);
                    }

                }
                else if (belongSource == 2)
                {
                    BsonDocument project = new BsonDocument();//项目信息
                    project = dataOp.FindOneByQuery("Project", Query.EQ("projId", belongValue.ToString()));
                    entity = dataOp.FindOneByQuery(tbName, Query.EQ("projId", belongValue.ToString()));
                    if (entity == null)
                    {
                        entity = new BsonDocument();
                        entity.Add("name", project != null ? project.String("name") : "" + "目录");
                        entity.Add("projId", belongValue.ToString());
                        entity.Add("isTemplate", "0");
                        entity.Add("srcId", templateId.ToString());
                        result = dataOp.Insert(tbName, entity);
                        entity = result.BsonInfo;
                    }
                    dataSubList = dataOp.FindAllByQuery(tbName + "Dir", Query.EQ(primaryKey, entity.String(primaryKey))).ToList();
                    if (dataSubList.Count() == 0)
                    {
                        templateDataSubList = dataOp.FindAllByQuery(tbName + "Dir", Query.EQ(primaryKey, templateId.ToString())).ToList();
                        TemplateDirBll tdBll = TemplateDirBll._();
                        List<BsonDocument> newDir = new List<BsonDocument>();
                        tdBll.CopyGeneralDir(newDir, templateDataSubList, 0, null, tbName, entity);
                    }
                }
            }
            json.Message = result.Message;
            json.Success = result.Status == Status.Successful;
            return Json(json);
        }
        /// <summary>
        /// 保存版本
        /// </summary>
        /// <param name="saveFormCollection"></param>
        /// <returns></returns>
        public ActionResult SaveVersion(FormCollection saveFormCollection)
        {
            PageJson json = new PageJson();
            InvokeResult result = new InvokeResult();
            string verTbName = PageReq.GetForm("tbName");//复制表名
            string queryStr = PageReq.GetForm("queryStr");
            string sourceTbName = PageReq.GetForm("tableName");//被复制表名
            TableRule table = new TableRule(verTbName);
            string primaryKey = table.GetPrimaryKey();
            TableRule sourceTable = new TableRule(sourceTbName);
            string sourcePrimaryKey = sourceTable.GetPrimaryKey();
            int ddirId = PageReq.GetFormInt(sourcePrimaryKey);//来源Id
            BsonDocument entity = new BsonDocument();
            if (string.IsNullOrEmpty(queryStr))
            {
                foreach (var keyName in saveFormCollection.AllKeys)
                {
                    if (keyName == "tbName" || keyName == "queryStr") continue;
                    entity.Add(keyName, PageReq.GetForm(keyName));
                }
                result = dataOp.Insert(verTbName, entity);
                entity = result.BsonInfo;
                List<BsonDocument> sourceSubList = new List<BsonDocument>();
                if (result.Status == Status.Successful)
                {
                    sourceSubList = dataOp.FindAllByQuery(sourceTbName + "Dir", Query.EQ(sourcePrimaryKey, ddirId.ToString())).ToList();
                    TemplateDirBll tdBll = TemplateDirBll._();
                    List<BsonDocument> newDir = new List<BsonDocument>();
                    List<string> filed = new List<string> { "content", "overIndicator", "highRise", "uniteRow", "classSingle", "foreignHouse", "basement" };
                    tdBll.SaveVerDir(newDir, sourceSubList, 0, null, verTbName, entity, sourceTbName + "Dir", filed);
                }

            }


            json.Message = result.Message;
            json.Success = result.Status == Status.Successful;
            return Json(json);
        }
        /// <summary>
        /// 保存项目复制目录信息及内容
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        public ActionResult SaveProjCopyDir(FormCollection saveForm)
        {
            InvokeResult result = new InvokeResult();
            PageJson json = new PageJson();
            string tbName = PageReq.GetForm("tbName");
            string queryStr = PageReq.GetForm("queryStr");
            string srcId = PageReq.GetForm("srcId");
            string filedStr = PageReq.GetForm("fileds");//需复制内容的字段名
            string[] filedArr = filedStr.Split(new string[] { "_" }, StringSplitOptions.RemoveEmptyEntries);
            TableRule tableEntity = new TableRule(tbName);
            string primaryKey = tableEntity.GetPrimaryKey();
            BsonDocument entity = new BsonDocument();
            BsonDocument srcInfo = dataOp.FindOneByQuery(tbName, Query.EQ(primaryKey, srcId));
            if (srcInfo == null && string.IsNullOrEmpty(queryStr))
            {
                json.Message = "复制源信息不存在!请刷新后重试!";
                json.Success = false;
                return Json(json);
            }

            foreach (var tempKey in saveForm.AllKeys)
            {
                if (tempKey == "tbName" || tempKey == "queryStr" || tempKey.Contains("fileList[") || tempKey.Contains("param.")) continue;

                entity.Add(tempKey, PageReq.GetForm(tempKey));
            }
            #region 保存数据
            result = dataOp.Save(tbName, queryStr != "" ? TypeConvert.NativeQueryToQuery(queryStr) : Query.Null, entity);
            if (result.Status == Status.Successful)
            {
                entity = result.BsonInfo;
                json.Success = true;
                json.Message = "保存成功!";
                json.htInfo = new System.Collections.Hashtable();
                json.htInfo.Add("landInfoId", entity.String("landInfoId"));
            }
            else
            {
                json.Message = "保存失败!请重试";
                json.Success = false;
                return Json(json);
            }
            #endregion

            try
            {
                if (string.IsNullOrEmpty(queryStr))
                {
                    List<BsonDocument> dataSubList = new List<BsonDocument>();//实体详细目录
                    List<BsonDocument> templateDataSubList = new List<BsonDocument>();//模板详细目录
                    Dictionary<string, string> dc = new Dictionary<string, string>();
                    dc.Add("projId", entity.String("projId"));
                    dc.Add(primaryKey, entity.String(primaryKey));
                    dataSubList = dataOp.FindAllByQuery(tbName + "Dir", Query.EQ(primaryKey, entity.String(primaryKey))).ToList();
                    if (dataSubList.Count() > 0)
                    {
                        dataOp.Delete(tbName + "Dir", Query.EQ(primaryKey, entity.String(primaryKey)));
                    }
                    if (dataSubList.Count() == 0)
                    {
                        templateDataSubList = dataOp.FindAllByQuery(tbName + "Dir", Query.EQ(primaryKey, srcId)).ToList();
                        TemplateDirBll tdBll = TemplateDirBll._();
                        List<BsonDocument> newDir = new List<BsonDocument>();
                        tdBll.CopyTreeTable(tbName + "Dir", templateDataSubList, filedArr, dc);
                    }
                }
            }
            catch (Exception ex)
            {
                dataOp.Delete(tbName, Query.EQ(primaryKey, entity.String(primaryKey)));
                dataOp.Delete(tbName + "Dir", Query.EQ(primaryKey, entity.String(primaryKey)));
                json.Success = false;
                json.Message = "创建失败";
            }

            return Json(json);
        }
        /// <summary>
        /// 获取文件个数
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult GetFileRelCount()
        {
            PageJson json = new PageJson();
            string tbName = PageReq.GetParam("tbName");
            TableRule table = new TableRule(tbName);
            string keyName = table.GetPrimaryKey();
            string keyValue = PageReq.GetParam("keyValue");
            string fileObjId = PageReq.GetParam("fileObjId");
            if (string.IsNullOrEmpty(tbName) || string.IsNullOrEmpty(keyName) || string.IsNullOrEmpty(fileObjId))
            {
                json.Success = false;
                json.Message = "传入参数错误！请重试或联系管理员！";
                return Json(json);
            }
            List<BsonDocument> fileRel = dataOp.FindAllByQuery("FileRelation", Query.And(Query.EQ("tableName", tbName), Query.EQ("fileObjId", fileObjId), Query.EQ("keyName", keyName))).ToList();
            if (!string.IsNullOrEmpty(keyValue))
            {
                if (fileRel.Count() > 0)
                {
                    fileRel = fileRel.Where(x => x.String("keyValue") == keyValue).ToList();
                }
            }
            int count = fileRel.Count();
            json.Success = true;
            json.htInfo = new System.Collections.Hashtable();
            json.htInfo.Add("fileNum", count);
            return Json(json);
        }
        public JsonResult CheckPropertyIsUse()
        {
            PageJson json = new PageJson();
            string propertyId = PageReq.GetForm("propertyId");
            //地块是否引用
            List<BsonDocument> landPropertyList = dataOp.FindAllByQuery("LandProperty", Query.EQ("propertyId", propertyId)).ToList();
            //项目定价建议物业类型
            List<BsonDocument> projPropertyList = dataOp.FindAllByQuery("ProjectProperty", Query.EQ("propertyId", propertyId)).ToList();
            //项目基本信息是否引用
            List<BsonDocument> projBasePropertyList = dataOp.FindAllByQuery("ProjectBaseProperty", Query.EQ("propertyId", propertyId)).ToList();
            //外部项目库是否引用此业态
            List<BsonDocument> outProjPropertyList = dataOp.FindAllByQuery("OutProjectProperty", Query.EQ("propertyId", propertyId)).ToList();
            int landCount = landPropertyList.Count();
            int projCount = projPropertyList.Count();
            int projBaseCount = projBasePropertyList.Count();
            int outProjCount = outProjPropertyList.Count();
            int allCount = landCount + projCount + projBaseCount + outProjCount;
            if (allCount > 0)
            {
                json.Success = false;
                json.Message = "此类型已被" + allCount + "处引用";
            }
            else
            {
                json.Success = true;
                json.Message = "此类型没被引用";
            }

            return Json(json);
        }
        /// <summary>
        /// 复制备份字段
        /// </summary>
        /// <returns></returns>
        public JsonResult CopyBakData()
        {
            string tableName = PageReq.GetParam("tbName");
            TableRule table = new TableRule(tableName);
            string primaryKey = table.GetPrimaryKey();
            string colStr = PageReq.GetParam("colName");//列名，格式：colName+*|+colName+……
            string[] item = Regex.Split(colStr, @"\*\|", RegexOptions.IgnoreCase);//分解列名
            List<BsonDocument> allData = dataOp.FindAll(tableName).ToList();
            List<StorageData> updateData = new List<StorageData>();
            foreach (var tempData in allData)
            {
                BsonDocument tempDoc = new BsonDocument();
                tempDoc = tempData;
                foreach (var tempItem in item)
                {
                    if (!tempDoc.ContainsColumn(tempItem + "_bak"))
                    {
                        if (tempDoc.ContainsColumn(tempItem))
                        {
                            tempDoc.Add(tempItem + "_bak", tempDoc[tempItem]);
                        }
                    }
                }
                StorageData tempStorageData = new StorageData();
                tempStorageData.Document = tempDoc;
                tempStorageData.Name = tableName;
                tempStorageData.Type = StorageType.Update;
                tempStorageData.Query = Query.EQ(primaryKey, tempDoc.String(primaryKey));
                updateData.Add(tempStorageData);
            }
            var result = dataOp.BatchSaveStorageData(updateData);
            PageJson json = new PageJson();
            json.Success = result.Status == Status.Successful;
            json.Message = result.Message;
            return Json(json, JsonRequestBehavior.AllowGet);


        }

        #region 审核表单

        /// <summary>
        /// 提交产品定位 情报库的表单审核
        /// </summary>
        /// <returns></returns>
        public JsonResult SubmitFormAudit()
        {
            PageJson json = new PageJson();
            InvokeResult result = new InvokeResult();
            string tableName = PageReq.GetParam("tableName");
            int ddirId = PageReq.GetParamInt("ddirId");
            int typeId = PageReq.GetParamInt("typeId");//消息类型Id：1情报库，2产品定位，3产品决策

            #region 获取地块 项目信息

            int landId = 0;//地块Id
            int projId = 0;//项目Id
            BsonDocument projInfo = new BsonDocument();//项目信息
            if (typeId == 1)
            {
                landId = PageReq.GetParamInt("landOrProjId");
            }
            else if (typeId == 2)
            {
                projId = PageReq.GetParamInt("landOrProjId");
                projInfo = dataOp.FindOneByQuery("Project", Query.EQ("projId", projId.ToString()));
                landId = projInfo.Int("landId");

            }
            BsonDocument landInfo = dataOp.FindOneByQuery("Land", Query.EQ("landId", landId.ToString()));//地块信息
            #endregion

            #region 判断信息是否正常
            if (string.IsNullOrEmpty(tableName) || ddirId == 0 || landInfo == null)
            {
                json.Message = "传入参数有误,请刷新后重试!";
                json.Success = false;
                return Json(json);
            }
            TableRule table = new TableRule(tableName);
            string keyName = table.PrimaryKey;
            BsonDocument entity = dataOp.FindOneByQuery(tableName, Query.EQ(keyName, ddirId.ToString()));
            if (entity == null)
            {
                json.Message = "传入参数有误,请刷新后重试!";
                json.Success = false;
                return Json(json);
            }
            #endregion

            #region 更新信息
            List<StorageData> storageData = new List<StorageData>();//需更新/插入的数据
            StorageData tempData = new StorageData();//临时插入/更新的信息
            int currentUserId = this.CurrentUserId;//提交人
            BsonDocument currentUserInfo = dataOp.FindOneByQuery("SysUser", Query.EQ("userId", currentUserId.ToString()));

            //更新提交表单的审核状态  1：审核中 0：已审核或未提交 2 已审核
            if (entity.ContainsColumn("sumbitStatus"))
            {
                entity["sumbitStatus"] = "1";
            }
            else
            {
                entity.Add("sumbitStatus", "1");
            }
            tempData.Name = tableName;
            tempData.Document = entity;
            tempData.Type = StorageType.Update;
            tempData.Query = Query.EQ(keyName, ddirId);
            storageData.Add(tempData);


            #region 表单权限代码关联字段
            Dictionary<string, List<string>> formCode = new Dictionary<string, List<string>>();//表单权限代码关联字段
            formCode.Add(FormTableName.ParcelCirInfo, new List<string> { "SZPT_VERIFY", "地块周边市政配套设施" });
            formCode.Add(FormTableName.AdjacentParcelInfo, new List<string> { "XLDK_VERIFY", "相邻地块情况" });
            formCode.Add(FormTableName.GovRequest, new List<string> { "ZFBMYQ_VERIFY", "政府部门要求" });
            formCode.Add(FormTableName.DesignFileApproval, new List<string> { "SJWJ_VERIFY", "设计文件报批标准指引" });
            formCode.Add(FormTableName.EngineeringFileApproval, new List<string> { "GCWJ_VERIFY", "工程文件报批标准指引" });
            formCode.Add(FormTableName.CompetitiveHouse, new List<string> { "JPLP_VERIFY", "竞品楼盘考察表" });
            formCode.Add(FormTableName.LocationProposal, new List<string> { "ZTDW_VERIFY", "项目整体定位建议" });
            formCode.Add(FormTableName.TypeMatchProposal, new List<string> { "HXPB_VERIFY", "户型类型、配比建议 " });
            formCode.Add(FormTableName.SaleOfficeProposal, new List<string> { "SLYB_VERIFY", "售楼处、样板房建议 " });
            formCode.Add(FormTableName.ExternalCorrect, new List<string> { "XMDJ_VERIFY", "项目定价建议 " });
            formCode.Add(FormTableName.CostEcoIndicator, new List<string> { "CBJJ_VERIFY", "成本经济技术指标表" });
            formCode.Add(FormTableName.KeyTechEcoIndicator, new List<string> { "GJJS_VERIFY", "项目关键技术经济指标" });
            formCode.Add(FormTableName.LandInfo, new List<string> { "XMTD_VERIFY", "项目土地信息" });
            formCode.Add(FormTableName.LandInSide, new List<string> { "TDQK_VERIFY", "土地内部情况查看" });
            #endregion

            string title = landInfo.String("name");//提交的表单的名称
            if (typeId == 1)
            {
                title = string.Format("{0}情报库{1}", title, formCode[tableName][1]);
            }
            else if (typeId == 2)
            {
                title = string.Format("{0}{1}产品定位{2}", title, projInfo.String("name"), formCode[tableName][1]);
            }

            BsonDocument logInfo = new BsonDocument();//日志
            logInfo.Add("subUserId", currentUserId.ToString());
            logInfo.Add("auditStatus", "0");
            logInfo.Add("actionType", "1");
            logInfo.Add("formId", ddirId.ToString());
            logInfo.Add("formName", tableName);
            logInfo.Add("projId", projId.ToString());
            logInfo.Add("landId", landId.ToString());
            logInfo.Add("typeId", typeId.ToString());
            logInfo.Add("content", title);
            tempData = new StorageData();
            tempData.Document = logInfo;
            tempData.Name = "FormAuditLog";
            tempData.Type = StorageType.Insert;
            storageData.Add(tempData);//添加日志


            #region  推送消息
            List<BsonDocument> auditUserList = new List<BsonDocument>();//推送消息人员名单
            Authentication auth = new Authentication();
            if (typeId == 1)
            {
                auditUserList = auth.GetUserOfProjectRight(AreaRoleType.Land, landId.ToString(), formCode[tableName][0]);
            }
            else if (typeId == 2)
            {
                auditUserList = auth.GetUserOfProjectRight(AreaRoleType.Project, landId.ToString(), formCode[tableName][1]);
            }

            BsonDocument oaMessage = new BsonDocument();
            string url = string.Empty;
            if (typeId == 1)
            {
                url = GetAuditUrl(typeId, tableName, landId, ddirId);
            }
            else if (typeId == 2)
            {
                url = GetAuditUrl(typeId, tableName, projId, ddirId);
            }
            foreach (var tempUser in auditUserList)
            {
                oaMessage = new BsonDocument();
                oaMessage.Add("typeId", typeId.ToString());
                oaMessage.Add("landId", landId.ToString());
                oaMessage.Add("projId", projId.ToString());
                oaMessage.Add("versionId", "0");
                oaMessage.Add("formName", tableName);
                oaMessage.Add("formId", ddirId.ToString());
                oaMessage.Add("title", title);
                oaMessage.Add("url", url);
                oaMessage.Add("subUserId", currentUserId.ToString());
                oaMessage.Add("subName", currentUserInfo.String("loginName"));
                oaMessage.Add("acceptUserId", tempUser.String("userId"));
                oaMessage.Add("acceptName", tempUser.String("loginName"));
                oaMessage.Add("auditStatus", "0");
                tempData = new StorageData();
                tempData.Document = oaMessage;
                tempData.Name = "OAMessage";
                tempData.Type = StorageType.Insert;
                storageData.Add(tempData);//添加OA消息
            }
            #endregion

            #endregion

            try
            {
                result = dataOp.BatchSaveStorageData(storageData);
                //if (result.Status == Status.Successful)
                //{
                //    PushToSeeyon pushToSeeyon = new PushToSeeyon();
                //    string[] loginNames = auditUserList.Where(x => !string.IsNullOrEmpty(x.String("loginName"))).Select(x => x.String("loginName")).ToArray();
                //    pushToSeeyon.PushTodoInfo(loginNames, title, url);
                //}
            }
            catch (Exception ex)
            {

            }
            if (result.Status == Status.Successful)
            {
                json.Message = "已提交审核!";
                json.Success = true;
            }
            else
            {
                json.Message = "提交失败,请刷新后重试!";
                json.Success = false;
            }
            return Json(json);
        }
        /// <summary>
        /// 返回提审数据
        /// </summary>
        /// <param name="tableName">表单名</param>
        /// <param name="typeId">//消息类型Id：1情报库，2产品定位，3产品决策</param>
        /// <param name="landInfo">地块信息</param>
        /// <param name="projInfo">项目信息</param>
        /// <param name="entity">表单数据</param>
        /// <returns></returns>
        public List<StorageData> GetSubmitFormAuditData(string tableName, int typeId, BsonDocument landInfo, BsonDocument projInfo, BsonDocument entity)
        {
            TableRule table = new TableRule(tableName);
            string keyName = table.PrimaryKey;
            int projId = projInfo.Int("projId");
            int landId = landInfo.Int("landId");
            int ddirId = entity.Int(keyName);
            // 更新信息
            List<StorageData> storageData = new List<StorageData>();//需更新/插入的数据
            StorageData tempData = new StorageData();//临时插入/更新的信息
            int currentUserId = this.CurrentUserId;//提交人
            BsonDocument currentUserInfo = dataOp.FindOneByQuery("SysUser", Query.EQ("userId", currentUserId.ToString()));

            //更新提交表单的审核状态  1：审核中 0：已审核或未提交 2 已审核
            if (entity.ContainsColumn("sumbitStatus"))
            {
                entity["sumbitStatus"] = "1";
            }
            else
            {
                entity.Add("sumbitStatus", "1");
            }
            tempData.Name = tableName;
            tempData.Document = entity;
            tempData.Type = StorageType.Update;
            tempData.Query = Query.EQ(keyName, entity.String(keyName));
            storageData.Add(tempData);


            #region 表单权限代码关联字段
            Dictionary<string, List<string>> formCode = new Dictionary<string, List<string>>();//表单权限代码关联字段
            formCode.Add(FormTableName.ParcelCirInfo, new List<string> { "SZPT_VERIFY", "地块周边市政配套设施" });
            formCode.Add(FormTableName.AdjacentParcelInfo, new List<string> { "XLDK_VERIFY", "相邻地块情况" });
            formCode.Add(FormTableName.GovRequest, new List<string> { "ZFBMYQ_VERIFY", "政府部门要求" });
            formCode.Add(FormTableName.DesignFileApproval, new List<string> { "SJWJ_VERIFY", "设计文件报批标准指引" });
            formCode.Add(FormTableName.EngineeringFileApproval, new List<string> { "GCWJ_VERIFY", "工程文件报批标准指引" });
            formCode.Add(FormTableName.CompetitiveHouse, new List<string> { "JPLP_VERIFY", "竞品楼盘考察表" });
            formCode.Add(FormTableName.LocationProposal, new List<string> { "ZTDW_VERIFY", "项目整体定位建议" });
            formCode.Add(FormTableName.TypeMatchProposal, new List<string> { "HXPB_VERIFY", "户型类型、配比建议 " });
            formCode.Add(FormTableName.SaleOfficeProposal, new List<string> { "SLYB_VERIFY", "售楼处、样板房建议 " });
            formCode.Add(FormTableName.ExternalCorrect, new List<string> { "XMDJ_VERIFY", "项目定价建议 " });
            formCode.Add(FormTableName.CostEcoIndicator, new List<string> { "CBJJ_VERIFY", "成本经济技术指标表" });
            formCode.Add(FormTableName.KeyTechEcoIndicator, new List<string> { "GJJS_VERIFY", "项目关键技术经济指标" });
            formCode.Add(FormTableName.LandInfo, new List<string> { "XMTD_VERIFY", "项目土地信息" });
            formCode.Add(FormTableName.LandInSide, new List<string> { "TDQK_VERIFY", "土地内部情况查看" });
            #endregion

            string title = landInfo.String("name");//提交的表单的名称
            if (typeId == 1)
            {
                title = string.Format("{0}-情报库-{1}", title, formCode[tableName][1]);
            }
            else if (typeId == 2)
            {
                title = string.Format("{0}-{1}-产品定位-{2}", title, projInfo.String("name"), formCode[tableName][1]);
            }

            BsonDocument logInfo = new BsonDocument();//日志
            logInfo.Add("subUserId", currentUserId.ToString());
            logInfo.Add("auditStatus", "0");
            logInfo.Add("actionType", "1");
            logInfo.Add("formId", ddirId.ToString());
            logInfo.Add("formName", tableName);
            logInfo.Add("projId", projId.ToString());
            logInfo.Add("landId", landId.ToString());
            logInfo.Add("typeId", typeId.ToString());
            logInfo.Add("content", title);
            tempData = new StorageData();
            tempData.Document = logInfo;
            tempData.Name = "FormAuditLog";
            tempData.Type = StorageType.Insert;
            storageData.Add(tempData);//添加日志


            #region  推送消息
            List<BsonDocument> auditUserList = new List<BsonDocument>();//推送消息人员名单
            Authentication auth = new Authentication();
            if (typeId == 1)
            {
                auditUserList = auth.GetUserOfProjectRight(AreaRoleType.Land, landId.ToString(), formCode[tableName][0]);
            }
            else if (typeId == 2)
            {
                auditUserList = auth.GetUserOfProjectRight(AreaRoleType.Project, projId.ToString(), formCode[tableName][0]);
            }

            BsonDocument oaMessage = new BsonDocument();
            string url = string.Empty;
            if (typeId == 1)
            {
                url = GetAuditUrl(typeId, tableName, landId, ddirId);
            }
            else if (typeId == 2)
            {
                url = GetAuditUrl(typeId, tableName, projId, ddirId);
            }
            foreach (var tempUser in auditUserList)
            {
                oaMessage = new BsonDocument();
                oaMessage.Add("typeId", typeId.ToString());
                oaMessage.Add("landId", landId.ToString());
                oaMessage.Add("projId", projId.ToString());
                oaMessage.Add("versionId", "0");
                oaMessage.Add("formName", tableName);
                oaMessage.Add("formId", ddirId.ToString());
                oaMessage.Add("title", title);
                oaMessage.Add("url", url);
                oaMessage.Add("subUserId", currentUserId.ToString());
                oaMessage.Add("subName", currentUserInfo.String("loginName"));
                oaMessage.Add("acceptUserId", tempUser.String("userId"));
                oaMessage.Add("acceptName", tempUser.String("loginName"));
                oaMessage.Add("auditStatus", "0");
                tempData = new StorageData();
                tempData.Document = oaMessage;
                tempData.Name = "OAMessage";
                tempData.Type = StorageType.Insert;
                storageData.Add(tempData);//添加OA消息
            }
            PushToSeeyon pushToSeeyon = new PushToSeeyon();
            string[] loginNames = auditUserList.Where(x => !string.IsNullOrEmpty(x.String("loginName"))).Select(x => x.String("loginName")).ToArray();
            pushToSeeyon.PushTodoInfo(loginNames, title, url);
            #endregion
            return storageData;
        }



        /// <summary>
        /// 审核表单
        /// </summary>
        /// <returns></returns>
        public JsonResult AuditFromTable()
        {
            PageJson json = new PageJson();
            InvokeResult result = new InvokeResult();
            int ddirId = PageReq.GetParamInt("ddirId");
            string tableName = PageReq.GetParam("tableName");
            int auditStatus = PageReq.GetParamInt("status");//审核状态 1：通过 2 未通过
            if (ddirId == 0 || string.IsNullOrEmpty(tableName))
            {
                json.Message = "传入参数有误,请刷新后重试!";
                json.Success = false;
                return Json(json);
            }
            int currentUserId = this.CurrentUserId;
            BsonDocument curUserInfo = dataOp.FindOneByQuery("SysUser", Query.EQ("userId", currentUserId.ToString()));
            List<BsonDocument> oaMessageList = dataOp.FindAllByQuery("OAMessage", Query.And(Query.EQ("formName", tableName), Query.EQ("formId", ddirId.ToString()), Query.EQ("auditStatus", "0"))).ToList();
            if (oaMessageList.Count() == 0)
            {
                json.Message = "该表单已被审核过!";
                json.Success = false;
                return Json(json);
            }
            List<StorageData> storageData = new List<StorageData>();
            StorageData tempData = new StorageData();
            for (int i = 0; i < oaMessageList.Count(); i++)
            {
                tempData = new StorageData();
                oaMessageList[i]["auditStatus"] = "1";
                tempData.Document = oaMessageList[i];
                tempData.Name = "OAMessage";
                tempData.Type = StorageType.Update;
                tempData.Query = Query.EQ("msgId", oaMessageList[i].String("msgId"));
                storageData.Add(tempData);
            }
            BsonDocument oaMessage = oaMessageList.FirstOrDefault();
            BsonDocument logInfo = new BsonDocument();
            logInfo.Add("subUserId", currentUserId.ToString());
            logInfo.Add("auditStatus", auditStatus.ToString());
            logInfo.Add("actionType", "2");
            logInfo.Add("formId", ddirId.ToString());
            logInfo.Add("formName", tableName);
            logInfo.Add("projId", oaMessage.String("projId"));
            logInfo.Add("landId", oaMessage.String("landId"));
            logInfo.Add("typeId", oaMessage.String("typeId"));
            logInfo.Add("content", oaMessage.String("title"));
            logInfo.Add("advice", PageReq.GetParam("advice"));
            tempData = new StorageData();
            tempData.Document = logInfo;
            tempData.Name = "FormAuditLog";
            tempData.Type = StorageType.Insert;
            storageData.Add(tempData);

            TableRule table = new TableRule(tableName);
            string keyName = table.PrimaryKey;
            BsonDocument entity = dataOp.FindOneByQuery(tableName, Query.EQ(keyName, ddirId.ToString()));
            //更新提交表单的审核状态  1：审核中 0：已审核或未提交 2 已审核
            if (entity.ContainsColumn("sumbitStatus"))
            {
                entity["sumbitStatus"] = "2";
            }
            else
            {
                entity.Add("sumbitStatus", "2");
            }
            tempData = new StorageData();
            tempData.Name = tableName;
            tempData.Document = entity;
            tempData.Type = StorageType.Update;
            tempData.Query = Query.EQ(keyName, ddirId.ToString());
            storageData.Add(tempData);

            try
            {
                result = dataOp.BatchSaveStorageData(storageData);
            }
            catch (Exception ex)
            {

            }
            if (result.Status == Status.Successful)
            {
                json.Message = "审核成功!";
                json.Success = true;
            }
            else
            {
                json.Message = "审核失败,请刷新后重试!";
                json.Success = false;
            }


            return Json(json);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ddirId"></param>
        /// <param name="tableName"></param>
        /// <param name="auditStatus">审核状态 1：通过 2 未通过</param>
        /// <returns></returns>
        public List<StorageData> GetAuditFormTableData(int ddirId, string tableName, int auditStatus) 
        {
            List<StorageData> storageData = new List<StorageData>();
            int currentUserId = this.CurrentUserId;
            BsonDocument curUserInfo = dataOp.FindOneByQuery("SysUser", Query.EQ("userId", currentUserId.ToString()));
            List<BsonDocument> oaMessageList = dataOp.FindAllByQuery("OAMessage", Query.And(Query.EQ("formName", tableName), Query.EQ("formId", ddirId.ToString()), Query.EQ("auditStatus", "0"))).ToList();
            if (oaMessageList.Count() != 0)
            {


               
                StorageData tempData = new StorageData();
                for (int i = 0; i < oaMessageList.Count(); i++)
                {
                    tempData = new StorageData();
                    oaMessageList[i]["auditStatus"] = "1";
                    tempData.Document = oaMessageList[i];
                    tempData.Name = "OAMessage";
                    tempData.Type = StorageType.Update;
                    tempData.Query = Query.EQ("msgId", oaMessageList[i].String("msgId"));
                    storageData.Add(tempData);
                }
                BsonDocument oaMessage = oaMessageList.FirstOrDefault();
                BsonDocument logInfo = new BsonDocument();
                logInfo.Add("subUserId", currentUserId.ToString());
                logInfo.Add("auditStatus", auditStatus.ToString());
                logInfo.Add("actionType", "2");
                logInfo.Add("formId", ddirId.ToString());
                logInfo.Add("formName", tableName);
                logInfo.Add("projId", oaMessage.String("projId"));
                logInfo.Add("landId", oaMessage.String("landId"));
                logInfo.Add("typeId", oaMessage.String("typeId"));
                logInfo.Add("content", oaMessage.String("title"));
                logInfo.Add("advice", PageReq.GetParam("advice"));
                tempData = new StorageData();
                tempData.Document = logInfo;
                tempData.Name = "FormAuditLog";
                tempData.Type = StorageType.Insert;
                storageData.Add(tempData);

                TableRule table = new TableRule(tableName);
                string keyName = table.PrimaryKey;
                BsonDocument entity = dataOp.FindOneByQuery(tableName, Query.EQ(keyName, ddirId.ToString()));
                //更新提交表单的审核状态  1：审核中 0：已审核或未提交 2 已审核
                if (entity.ContainsColumn("sumbitStatus"))
                {
                    entity["sumbitStatus"] = "2";
                }
                else
                {
                    entity.Add("sumbitStatus", "2");
                }
                tempData = new StorageData();
                tempData.Name = tableName;
                tempData.Document = entity;
                tempData.Type = StorageType.Update;
                tempData.Query = Query.EQ(keyName, ddirId.ToString());
                storageData.Add(tempData);
            }
            return storageData;
        }

        /// <summary>
        /// 返回审核地址
        /// </summary>
        /// <param name="typeId">审核类型 1：地块审核 2 项目审核 3：产品决策审核</param>
        /// <param name="tableName">审核表单的名称</param>
        /// <param name="keyValue">地块或项目的Id</param>
        /// <param name="ddirId">表单Id</param>
        /// <returns></returns>
        public string GetAuditUrl(int typeId, string tableName, int keyValue, int ddirId)
        {
            string url = string.Empty;
            if (typeId == 1)
            {
                url += "/InformationBank/DevelopmentDepartment?landId=" + keyValue ;
            }
            else if (typeId == 2)
            {
                url += "/InformationBank/ProductPositionManage/?projId=" + keyValue;
            }
            Dictionary<string, string> urlDC = new Dictionary<string, string>();
            urlDC.Add(FormTableName.ParcelCirInfo, "#1_1_1E");
            urlDC.Add(FormTableName.AdjacentParcelInfo, "#1_1_2E");
            urlDC.Add(FormTableName.GovRequest, "#1_2_1E");
            urlDC.Add(FormTableName.DesignFileApproval, "#1_2_2E");
            urlDC.Add(FormTableName.EngineeringFileApproval, "#1_2_3E");
            urlDC.Add(FormTableName.CompetitiveHouse, "#2_1_1E");
            urlDC.Add(FormTableName.LocationProposal, "#1_1E");
            urlDC.Add(FormTableName.TypeMatchProposal, "#1_2E");
            urlDC.Add(FormTableName.SaleOfficeProposal, "#1_3E");
            urlDC.Add(FormTableName.ExternalCorrect,"#1_4E");
            urlDC.Add(FormTableName.CostEcoIndicator, "#2_1E");
            urlDC.Add(FormTableName.KeyTechEcoIndicator, "#3_1E");
            urlDC.Add(FormTableName.LandInfo, "&landInfoId="+ddirId + "#4_1E_" + ddirId);
            urlDC.Add(FormTableName.LandInSide, "#4_2E");
            return url + urlDC[tableName];
        }
        /// <summary>
        /// 地块表单名称
        /// </summary>
        public static class FormTableName
        {
            /// <summary>
            /// 地块周边市政配套设施 
            /// </summary>
            public static string ParcelCirInfo = "ParcelCirInfo";
            /// <summary>
            /// 相邻地块情况 
            /// </summary>
            public static string AdjacentParcelInfo = "AdjacentParcelInfo";
            /// <summary>
            /// 政府部门要求 
            /// </summary>
            public static string GovRequest = "GovRequest";
            /// <summary>
            /// 设计文件报批标准指引 
            /// </summary>
            public static string DesignFileApproval = "DesignFileApproval";
            /// <summary>
            /// 工程文件报批标准指引 
            /// </summary>
            public static string EngineeringFileApproval = "EngineeringFileApproval";
            /// <summary>
            /// 竞品楼盘考察表
            /// </summary>
            public static string CompetitiveHouse = "CompetitiveHouse";
            /// <summary>
            /// 项目整体定位建议 
            /// </summary>
            public static string LocationProposal = "LocationProposal";
            /// <summary>
            /// 户型类型、配比建议 
            /// </summary>
            public static string TypeMatchProposal = "TypeMatchProposal";
            /// <summary>
            /// 售楼处、样板房建议 
            /// </summary>
            public static string SaleOfficeProposal = "SaleOfficeProposal";
            /// <summary>
            /// 项目定价建议 
            /// </summary>
            public static string ExternalCorrect = "ExternalCorrect";
            /// <summary>
            /// 成本经济技术指标表
            /// </summary>
            public static string CostEcoIndicator = "CostEcoIndicator";
            /// <summary>
            /// 项目关键技术经济指标
            /// </summary>
            public static string KeyTechEcoIndicator = "KeyTechEcoIndicator";
            /// <summary>
            /// 项目土地信息 
            /// </summary>
            public static string LandInfo = "LandInfo";
            /// <summary>
            /// 土地内部情况查看 
            /// </summary>
            public static string LandInSide = "LandInSide";

        }

        #endregion
    }
}
