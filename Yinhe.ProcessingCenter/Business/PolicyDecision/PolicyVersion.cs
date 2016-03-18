using System;
using System.Collections.Generic;
using System.Linq;
using Yinhe.ProcessingCenter.DataRule;
using MongoDB.Bson;
using MongoDB.Driver.Builders;

namespace Yinhe.ProcessingCenter.Business.PolicyDecision
{
    /// <summary>
    /// 决策版本处理类
    /// </summary>
    public class PolicyDept
    {
        public const string ProjNode = "ProjNode";//项目节点
        public const string MarketingArea = "MarketingArea";//市场面积
        public const string DesignArea = "DesignArea";//设计面积
        public const string Finance = "Finance";//财务数据
        public const string Cost = "Cost";//成本数据

        public static readonly Dictionary<string, string> costTypeDict = new Dictionary<string, string> {
            {"0","节点支付"},
            {"1","按月均摊"},
            {"2","比例分配"}
        };

        public static readonly List<object> BelongDepts = new List<object>(){
            new{id="0",name="成本部门"},
            new{id="1",name="财务部门"},
        };

        public static readonly List<object> CostTypes = new List<object>() {
            new{id="-1",name="无"},
            new{id="0",name="节点支付"},
            new{id="1",name="按月均摊"},
            new{id="2",name="比例分配"}
        };

        public static readonly List<object> SubjectTypes = new List<object>() {
            new{id="0",name="支出"},
            new{id="1",name="收入"}
        };

        public static readonly List<object> Units = new List<object>() {
            new{id="0",name="㎡"},
            new{id="1",name="元"},
            new{id="2",name="个"}
        };

        public static readonly List<object> RelTable = new List<object>() {
            new{id="",name="无"},
            new{id="MarketingArea",name="营销部"},
            new{id="DesignArea",name="设计部"},
            //new{id="Finance",name="财务部"}
        };

        public static readonly Dictionary<string, string> DeptDict = new Dictionary<string, string> {
        {"MarketingArea","营销部"},
        {"DesignArea","设计部"},
        //{"Finance","财务部"}
        };

    }

    /// <summary>
    /// 决策版本管理
    /// </summary>
    public class PolicyVersionBll
    {

        /// <summary>
        /// 获取项目的最早最迟时间
        /// </summary>
        /// <param name="projNodeList"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public static void CalcStartEnd(List<BsonDocument> projNodeList, ref DateTime start, ref DateTime end)
        {
            projNodeList = projNodeList.Where(s => s.String("completeDate") != "").ToList();
            if (!projNodeList.Any()) return;
            start = projNodeList.Select(s => s.Date("completeDate")).Min();
            end = projNodeList.Select(s => s.Date("completeDate")).Max();
        }

        /// <summary>
        /// 获取项目的开始结束时间
        /// </summary>
        /// <param name="versionId"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public void GetStartEndDate(string versionId, out DateTime start, out DateTime end)
        {
            var projNodeList = GetTableDirsByVersionId(PolicyDept.ProjNode, versionId).Where(s => !string.IsNullOrEmpty(s.String("completeDate")));
            if (projNodeList.Any())
            {
                start = projNodeList.Select(s => s.Date("completeDate")).Min();
                end = projNodeList.Select(s => s.Date("completeDate")).Max();
            }
            else
            {
                start = default(DateTime);
                end = start;
            }
        }



        private static readonly object ObjPad = new object();
        private const string PolicyVersion = "PolicyVersion";//决策版本

        #region 构造函数
        /// <summary>
        /// 类私有变量
        /// </summary>
        private readonly DataOperation _ctx = null;

        /// <summary>
        /// 封闭当前默认构造函数
        /// </summary>
        public PolicyVersionBll()
        {
            _ctx = new DataOperation();
        }

        public PolicyVersionBll(DataOperation ctx)
        {
            _ctx = ctx;
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static PolicyVersionBll _()
        {
            return new PolicyVersionBll();
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static PolicyVersionBll _(DataOperation ctx)
        {
            return new PolicyVersionBll(ctx);
        }
        #endregion

        /// <summary>
        /// 创建决策版本
        /// </summary>
        /// <param name="projId"></param>
        /// <param name="type">1:引用模板创建新版本 0或2：从旧数据中创建新模板</param>
        /// <returns></returns>
        public void CreateVersion(string projId, string remark,int type)
        {
            //ProjNode MarketingArea  DesignArea Finance Cost
            lock (ObjPad)
            {
                var versionId = string.Empty;
                try
                {
                    var count = _ctx.FindAllByQuery(PolicyVersion, Query.EQ("projId", projId)).Count();
                    var costBll = CostBll._(_ctx);
                    var latestVersionId = GetLatestVersionId(projId);
                    var version = CreateOriginalVersion(projId, remark, (count + 1).ToString());
                    versionId = version.String("versionId");
                    if ((count == 0 && latestVersionId == 0)||type==1)
                    {
                        CopyPolicyDateFromTemplate(PolicyDept.ProjNode, "项目节点", projId, versionId, new string[] { "name" });
                        CopyPolicyDateFromTemplate(PolicyDept.MarketingArea, "营销面积", projId, versionId, new string[] { "name", "unit" });
                        CopyPolicyDateFromTemplate(PolicyDept.DesignArea, "设计面积", projId, versionId, new string[] { "name", "unit" });
                        CreateFinance(projId, versionId);
                        costBll.CopyFromTemplate(PolicyDept.Cost, "成本科目", projId, versionId, new string[] { "name", "quota", "costType", "rate", "remark", "unit", "subType", "belong" });

                        if (type == 1)
                        {
                            var preVerStr = latestVersionId.ToString();
                            CopyDataFromOldVersion(PolicyDept.ProjNode, projId, preVerStr, versionId, new string[] { "completeDate" });//项目节点
                            CopyDataFromOldVersion(PolicyDept.MarketingArea, projId, preVerStr, versionId, new string[] { "areaValue", "maxVal", "minVal", "commonVal",  });//营销面积
                            CopyDataFromOldVersion(PolicyDept.DesignArea, projId, preVerStr, versionId, new string[] { "areaValue", });//设计面积
                            CopyDataFromOldVersion(PolicyDept.Cost, projId, preVerStr, versionId, new string[] { "quota", "rate", "amount" });//成本科目
                            CopyCostRate(projId, preVerStr, versionId, "srcId", false);
                            CopyRepayDetails(preVerStr, versionId);//还款计划
                            CopySellDetails(preVerStr, versionId);//销售回款计划
                        }
                        else 
                        {
                            CopyCostRate(projId, "", versionId, "templateId", true);
                        }
                    }
                    else
                    {
                        var preVerStr = latestVersionId.ToString();
                        CreateVersionFromPre(PolicyDept.ProjNode, "项目节点", projId, preVerStr, versionId, new string[] { "name", "completeDate", "templateId" });
                        CreateVersionFromPre(PolicyDept.MarketingArea, "营销面积", projId, preVerStr, versionId, new string[] { "name", "areaValue", "unit", "maxVal", "minVal", "commonVal", "templateId" });
                        CreateVersionFromPre(PolicyDept.DesignArea, "设计面积", projId, preVerStr, versionId, new string[] { "name", "areaValue", "unit", "templateId" });
                        CopyFinance(projId, preVerStr, versionId);//拷贝财务数据
                        costBll.CopyFromPreVersion(PolicyDept.Cost, "成本科目", projId, preVerStr, versionId, new string[] { "name", "quota", "costType", "amount", "rate", "remark", "unit", "subType", "belong", "templateId" });
                        CopyCostRate(projId, preVerStr, versionId, "srcId", false);
                        CopyRepayDetails(preVerStr, versionId);//还款计划
                        CopySellDetails(preVerStr, versionId);//销售回款计划

                        //有新模板时但不使用新模板加提醒
                        var tempNotice = _ctx.FindOneByQuery("ProjVersionNotice",Query.And( Query.EQ("projId", projId), Query.EQ("versionId",latestVersionId.ToString())));
                        if (tempNotice != null) 
                        {
                            tempNotice["versionId"] = versionId;
                            tempNotice["versionOrder"] = version.String("versionOrder");
                            tempNotice["remark"] = tempNotice["remark"] + ",再次通知";
                            _ctx.Update("ProjVersionNotice", Query.EQ("projVerNoticeId", tempNotice.String("projVerNoticeId")), tempNotice);
                        }

                    }
                    _ctx.Insert("ConflictInfo", new BsonDocument { { "versionId", versionId } });//初始化一个冲突决策表单信息
                    _ctx.Insert("CashDoc", new BsonDocument { { "versionId", versionId }, { "projId", projId } });//现金流文件
                    _ctx.Insert("ConflictDoc", new BsonDocument { { "versionId", versionId }, { "projId", projId } });//冲突决策文件
                    _ctx.Insert("WeightsDoc", new BsonDocument { { "versionId", versionId }, { "projId", projId } });//权重决策文件
                    _ctx.Insert("CEOPolicyDesign", new BsonDocument { { "versionId", versionId }, { "projId", projId } });//权重决策文件

                }
                catch
                {
                    if (!string.IsNullOrEmpty(versionId))
                    {
                        _ctx.Delete("PolicyVersion", Query.EQ("versionId", versionId));
                        Rollback(versionId);
                    }
                    throw;
                }
            }
        }

        /// <summary>
        /// 删除项目下的产品决策信息
        /// </summary>
        /// <param name="projId"></param>
        /// <returns></returns>
        public InvokeResult DeletePolicy(string projId)
        {
            var result = new InvokeResult();
            try
            {
                var versionIds = _ctx.FindAllByQuery("PolicyVersion", Query.EQ("projId", projId)).Select(s => s.String("versionId"));
                foreach (var versionId in versionIds)
                {
                    Rollback(versionId);
                }
                result.Status = Status.Successful;
            }
            catch (Exception)
            {
                result.Status = Status.Failed;
            }
            return result;
        }

        /// <summary>
        /// 创建新的决策版本失败，回滚数据，删除5个部门的目录数据，成本部门的科目比例,融资渠道
        /// </summary>
        /// <param name="projId">项目Id</param>
        /// <param name="versionId">版本Id</param>
        public void Rollback(string versionId)
        {
            int version;
            if (int.TryParse(versionId, out version) && version > 0)
            {
                var tempVer=_ctx.FindOneByQuery("PolicyVersion",Query.EQ("versionId",versionId));
                DeleteFinanceMode(versionId);
                DeletePolicyDeptDate(PolicyDept.ProjNode, versionId);
                DeletePolicyDeptDate(PolicyDept.MarketingArea, versionId);
                DeletePolicyDeptDate(PolicyDept.DesignArea, versionId);
                DeletePolicyDeptDate(PolicyDept.Finance, versionId);
                DeletePolicyDeptDate(PolicyDept.Cost, versionId);
                DeleteCostRate(versionId);
                _ctx.Delete("RepayDetail", Query.EQ("versionId", versionId));
                _ctx.Delete("SellDetail", Query.EQ("versionId", versionId));
                _ctx.Delete("PolicyVersion", Query.EQ("versionId", versionId));
                _ctx.Delete("ConflictInfo", Query.EQ("versionId", versionId));

                _ctx.Delete("CashDoc", Query.EQ("versionId", versionId));//现金流文件
                _ctx.Delete("ConflictDoc", Query.EQ("versionId", versionId));//冲突决策文件
                _ctx.Delete("WeightsDoc", Query.EQ("versionId", versionId));//权重决策文件
                _ctx.Delete("CEOPolicyDesign", Query.EQ("versionId", versionId));//权重决策文件
                _ctx.Delete("OAMessage", Query.EQ("versionId", versionId));//删除审核
                //增加删除版本后查看是否有需要通知模板已经更新通知
                var tempNotice = _ctx.FindOneByQuery("ProjVersionNotice", Query.EQ("versionId", versionId));
                if (tempNotice != null)
                {
                    var query = _ctx.FindAllByQuery("PolicyVersion", Query.EQ("projId", tempVer.String("projId"))).ToList();
                    var preVersionId = 0;
                    if (query.Any())
                    {
                        preVersionId = query.Select(s => s.Int("versionId")).Max();
                        var last = query.Where(x => x.String("versionId") == preVersionId.ToString()).FirstOrDefault();
                        tempNotice["versionId"] = last.String("versionId");
                        tempNotice["versionOrder"] = last.String("versionOrder");
                        tempNotice["remark"] = tempNotice["remark"]+",删除新版本";
                        _ctx.Update("ProjVersionNotice", Query.EQ("projVerNoticeId", tempNotice.String("projVerNoticeId")), tempNotice);
                    }
                }
            }
        }

        /// <summary>
        /// 删除成本科目比率
        /// </summary>
        /// <param name="projId"></param>
        /// <param name="versionId"></param>
        private void DeleteCostRate(string versionId)
        {
            int version, costRateForm;
            var form = _ctx.FindOneByQuery("CostRateForm", Query.EQ("versionId", versionId));
            var costRateFormId = form.String("costRateFormId");
            if (int.TryParse(versionId, out version) && version > 0 && int.TryParse(costRateFormId, out costRateForm) && costRateForm > 0)
            {
                var costRates = _ctx.FindAllByQuery("CostRate", Query.EQ("costRateFormId", costRateFormId)).Where(s => s.Int("isTemplate") == 0).Select(s => s.String("_id")).ToList();
                _ctx.Delete("CostRateForm", Query.EQ("costRateFormId", costRateFormId));
                _ctx.Delete("CostRate", Query.In("_id", TypeConvert.ToObjectIdList(costRates)));
            }
        }

        /// <summary>
        /// 删除融资渠道
        /// </summary>
        /// <param name="projId"></param>
        /// <param name="versionId"></param>
        private void DeleteFinanceMode(string versionId)
        {
            var financeId = _ctx.FindOneByQuery("Finance", Query.EQ("versionId", versionId)).String("FinanceId");
            var costRates = _ctx.FindAllByQuery("FinanceMode", Query.EQ("FinanceId", financeId)).Select(s => s.String("_id")).ToList();
            _ctx.Delete("FinanceMode", Query.In("_id", TypeConvert.ToObjectIdList(costRates)));
        }

        /// <summary>
        /// 删除部门目录数据
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="projId"></param>
        /// <param name="versionId"></param>
        private void DeletePolicyDeptDate(string tbName, string versionId)
        {
            var dirs = new List<BsonDocument>();
            if (!string.IsNullOrEmpty(tbName))
            {
                var dirName = tbName + "Dir";
                var tbRule = new TableRule(tbName);
                var primaryKey = tbRule.PrimaryKey;
                var dirRule = new TableRule(dirName);
                var dirKey = dirRule.PrimaryKey;
                //找出对应从模板拷贝的目录信息
                var dirParent = _ctx.FindOneByQuery(tbName, Query.EQ("versionId", versionId));
                _ctx.Delete(tbName, Query.EQ("_id", TypeConvert.ToObjectId(dirParent.String("_id"))));//删除
                var dirIds = _ctx.FindAllByQuery(dirName, Query.EQ(primaryKey, dirParent.String(primaryKey))).Select(s => s.String("_id")).ToList();
                _ctx.Delete(dirName, Query.In("_id", TypeConvert.ToObjectIdList(dirIds)));//删除目录
            }
        }

        /// <summary>
        /// 获取最新的决策版本Id
        /// </summary>
        /// <param name="projId">项目Id</param>
        /// <returns></returns>
        public int GetLatestVersionId(string projId)
        {
            var query = _ctx.FindAllByQuery(PolicyVersion, Query.And(Query.EQ("projId", projId)));
            var preVersionId = 0;
            if (query.Any())
            {
                preVersionId = _ctx.FindAllByQuery(PolicyVersion, Query.And(Query.EQ("projId", projId))).Select(s => s.Int("versionId")).Max();
            }
            return preVersionId;
        }

        /// <summary>
        /// 获取输入版本的上一个版本号
        /// </summary>
        /// <param name="projIdStr">项目Id</param>
        /// <param name="curVersionIdStr">版本Id</param>
        /// <returns></returns>
        public int GetPreVersionId(string curVersionIdStr)
        {
            int projId, curVersionId;
            var preVersionId = 0;
            if (int.TryParse(curVersionIdStr, out curVersionId) && curVersionId > 0)
            {
                projId = _ctx.FindOneByQuery(PolicyVersion, Query.EQ("versionId", curVersionIdStr)).Int("projId");
                if (projId > 0)
                {
                    var query = _ctx.FindAllByQuery(PolicyVersion, Query.And(Query.EQ("projId", projId.ToString()))).Where(s => s.Int("versionId") < curVersionId);
                    if (query.Any())
                    {
                        preVersionId = query.Select(s => s.Int("versionId")).Max();
                    }
                }
            }
            return preVersionId;
        }

        /// <summary>
        /// 判断产品决策数据是否产生变化
        /// </summary>
        /// <param name="curList">当前列表</param>
        /// <param name="preList">对应的上一个版本列表</param>
        /// <returns></returns>
        public void IsChange(IEnumerable<BsonDocument> curList, IEnumerable<BsonDocument> preList, params string[] keys)
        {
            if (preList.Any() && curList.Any())
            {
                foreach (var item in curList)
                {
                    var pre = preList.SingleOrDefault(s => s.Int("templateId") == item.Int("templateId"));
                    if (pre != null)
                    {
                        foreach (var key in keys)
                        {
                            if (key != "completeDate")
                            {
                                if (item.Decimal(key) != pre.Decimal(key))//判断同上一个版本是否产生变化
                                {
                                    item.Add(key + "_change", true);
                                }
                            }
                            else
                            {
                                if (item.String(key) != pre.String(key))//判断同上一个版本是否产生变化
                                {
                                    item.Add(key + "_change", true);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 销售计划，还款计划，变更判断
        /// </summary>
        /// <param name="curList"></param>
        /// <param name="preList"></param>
        /// <returns></returns>
        public void IsDetailChange(IEnumerable<BsonDocument> curList, IEnumerable<BsonDocument> preList, params string[] keys)
        {
            if (preList.Any() && curList.Any())
            {
                foreach (var item in curList)
                {
                    var pre = preList.SingleOrDefault(s => s.Date("date") == item.Date("date"));
                    if (pre != null)
                    {
                        foreach (var key in keys)
                        {
                            if (item.Decimal(key) != pre.Decimal(key))//判断同上一个版本是否产生变化
                            {
                                item.Add(key + "_change", true);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 复制还款计划信息
        /// </summary>
        /// <param name="preVersionId">上一个版本Id</param>
        /// <param name="curVersionId">当前版本Id</param>
        public void CopyRepayDetails(string preVersionId, string curVersionId)
        {
            const string tbName = "RepayDetail";
            var sellDetails = _ctx.FindAllByQuery(tbName, Query.EQ("versionId", preVersionId)).ToList();//上一个版本的销售计划
            var copyBll = CopyBsonHandle._(_ctx);
            var keyValDict = new Dictionary<string, string> { { "versionId", curVersionId } };
            copyBll.CopyBsons(tbName, sellDetails, "detailId", new string[] { "date", "interest", "repayVal", "loan", "minInterest", "minRepayVal", "minLoan", "comInterest", "comRepayVal", "comLoan" }, keyValDict);
        }

        /// <summary>
        /// 复制销售回款信息
        /// </summary>
        /// <param name="preVersionId">上一个版本Id</param>
        /// <param name="curVersionId">当前版本Id</param>
        public void CopySellDetails(string preVersionId, string curVersionId)
        {
            const string tbName = "SellDetail";
            var sellDetails = _ctx.FindAllByQuery(tbName, Query.EQ("versionId", preVersionId)).ToList();//上一个版本的销售计划
            var copyBll = CopyBsonHandle._(_ctx);
            var keyValDict = new Dictionary<string, string> { { "versionId", curVersionId } };
            copyBll.CopyBsons(tbName, sellDetails, "detailId", new string[] { "date", "salesVal", "minSalesVal", "comSalesVal" }, keyValDict);
        }

        /// <summary>
        /// 创建财务数据
        /// </summary>
        /// <param name="projId"></param>
        /// <param name="versionId"></param>
        private void CreateFinance(string projId, string versionId)
        {
            var finance = new BsonDocument { 
                {"name","财务数据"},{"projId",projId},{"versionId",versionId}
            };
            _ctx.Insert(PolicyDept.Finance, finance);
        }

        /// <summary>
        /// 复制财务数据以及融资渠道
        /// </summary>
        private void CopyFinance(string projId, string preVersionId, string versionId)
        {
            var preFinance = _ctx.FindOneByQuery(PolicyDept.Finance, Query.And(Query.EQ("projId", projId), Query.EQ("versionId", preVersionId)));//上一个版本的财务数据
            var FinanceId = preFinance.String("FinanceId");
            var preFinanceModes = _ctx.FindAllByQuery("FinanceMode", Query.EQ("FinanceId", FinanceId)).ToList();//上个版本的融资渠道

            var copyBll = CopyBsonHandle._(_ctx);
            var keyValueDict = new Dictionary<string, string> { { "projId", projId }, { "versionId", versionId } };
            var newFinance = copyBll.CopyBson(PolicyDept.Finance, preFinance, "FinanceId", new string[] { "landMoney", "otherMoney", "name" }, keyValueDict);
            var newFinanceId = newFinance.String("FinanceId");
            var keyValueDict2 = new Dictionary<string, string> { { "FinanceId", newFinanceId } };
            copyBll.CopyBsons("FinanceMode", preFinanceModes, "modeId", new string[] { "bank", "money", "cycle", "rate" }, keyValueDict2);
        }

        /// <summary>
        /// 复制节点支付科目的支付比例
        /// </summary>
        /// <param name="projId"></param>
        /// <param name="preVersionId"></param>
        /// <param name="versionId"></param>
        /// <param name="compareKey"></param>
        /// <param name="isFromTemplate">是否从模板拷贝</param>
        private void CopyCostRate(string projId, string preVersionId, string versionId, string compareKey, bool isFromTemplate)
        {
            List<BsonDocument> preCostRates = null;
            if (isFromTemplate)
            {
                preCostRates = _ctx.FindAllByQuery("CostRate", Query.EQ("isTemplate", "1")).ToList();//模板的支付比例
            }
            else
            {
                var costRateForm = _ctx.FindOneByQuery("CostRateForm", Query.And(Query.EQ("projId", projId), Query.EQ("versionId", preVersionId)));
                var preCostRateFormId = costRateForm.String("costRateFormId");
                preCostRates = _ctx.FindAllByQuery("CostRate", Query.EQ("costRateFormId", preCostRateFormId)).ToList();//上一版本节点支付比例
            }

            var projNodes = GetTableDirs(PolicyDept.ProjNode, projId, versionId);
            var costDirs = GetTableDirs(PolicyDept.Cost, projId, versionId);

            var rateForm = new BsonDocument { { "projId", projId }, { "versionId", versionId } };
            var result = _ctx.Insert("CostRateForm", rateForm);
            var newCostRateFormId = result.BsonInfo.String("costRateFormId");

            foreach (var preRate in preCostRates)
            {
                var costDirId = preRate.String("costDirId");
                var projNodeId = preRate.String("projNodeId");

                //获取当前版本对应的项目节点和成本Id
                var curCostDir = costDirs.FirstOrDefault(s => s.String(compareKey) == costDirId);
                var curProjNode = projNodes.FirstOrDefault(s => s.String(compareKey) == projNodeId);
                if (curCostDir != null && curProjNode != null)
                {

                    var costRate = new BsonDocument {
                       {"costDirId",curCostDir.String("CostDirId")},
                       {"projNodeId",curProjNode.String("projNodeDirId")},
                       {"costRateFormId",newCostRateFormId},
                       {"rate",preRate.String("rate")},
                       {"isTemplate","0"}
                    };
                    _ctx.Insert("CostRate", costRate);
                }
            }
        }

        /// <summary>
        /// 创建决策初始的版本，从模板中拷贝信息
        /// </summary>
        /// <param name="projId"></param>
        private BsonDocument CreateOriginalVersion(string projId, string remark, string versionOrder)
        {
            var version = new BsonDocument { { "projId", projId }, { "remark", remark }, { "versionOrder", versionOrder } };
            var result = _ctx.Insert(PolicyVersion, version);
            return result.BsonInfo;
        }


        /// <summary>
        /// 从上一个版本拷贝数据
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="name"></param>
        /// <param name="projId"></param>
        /// <param name="preVersionId"></param>
        /// <param name="versionId"></param>
        /// <param name="fields"></param>
        private void CreateVersionFromPre(string tableName, string name, string projId, string preVersionId, string versionId, string[] fields)
        {
            var tableRule = new TableRule(tableName);
            var primaryKey = tableRule.PrimaryKey;
            var preNode = _ctx.FindOneByQuery(tableName, Query.And(Query.EQ("versionId", preVersionId), Query.EQ("projId", projId)));//上一个版本的数据
            var dirParent = new BsonDocument { { "versionId", versionId }, { "name", name }, { "projId", projId } };//挂载目录的
            var projNodeResult = _ctx.Insert(tableName, dirParent);
            var templateDirs = _ctx.FindAllByQuery(tableName + "Dir", Query.EQ(primaryKey, preNode.String(primaryKey))).ToList();//获取上一个节点的目录结构
            var keyValueDict = new Dictionary<string, string> { { primaryKey, projNodeResult.BsonInfo.String(primaryKey) } };
            CopyTreeTable(tableName + "Dir", templateDirs, fields, keyValueDict, true);//拷贝目录信息
        }

        /// <summary>
        /// 从模板中拷贝目录结构
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="name"></param>
        /// <param name="versionId"></param>
        /// <param name="fields"></param>
        /// <param name="keyValueDict"></param>
        private void CopyPolicyDateFromTemplate(string tableName, string name, string projId, string versionId, string[] fields)
        {
            var tableRule = new TableRule(tableName);
            var primaryKey = tableRule.PrimaryKey;
            var template = _ctx.FindOneByQuery(tableName, Query.EQ("isTemplate", "1"));//找出系统模板
            var dirParent = new BsonDocument { { "versionId", versionId }, { "name", name }, { "projId", projId } };//挂载目录的
            var projNodeResult = _ctx.Insert(tableName, dirParent);
            var templateDirs = _ctx.FindAllByQuery(tableName + "Dir", Query.EQ(primaryKey, template.String(primaryKey))).ToList();
            var keyValueDict = new Dictionary<string, string> { { primaryKey, projNodeResult.BsonInfo.String(primaryKey) } };
            CopyTreeTable(tableName + "Dir", templateDirs, fields, keyValueDict, false);//拷贝目录信息
        }



        /// <summary>
        /// 从模板中复制科目节点支付比例
        /// </summary>
        private void CopyCostRateFromTemplate()
        {
            var costRates = _ctx.FindAllByQuery("CostRate", Query.EQ("isTemplate", "1")).ToList();
        }

        /// <summary>
        /// 复制树形结构目录
        /// </summary>
        /// <param name="tableName">树形结构的表名称</param>
        /// <param name="dataList">被复制的树型结构列表</param>
        /// <param name="fields">要复制的字段</param>
        /// <param name="keyValueDict">固定加入的值：比如树形目录挂在项目下可能就要加入项目的主键Id</param>
        /// <returns></returns>
        public List<BsonDocument> CopyTreeTable(string tableName, List<BsonDocument> dataList, string[] fields, Dictionary<string, string> keyValueDict, bool hasTemplateId)
        {
            var tableRule = new TableRule(tableName);
            var primaryKey = tableRule.PrimaryKey;

            var count = dataList.Count;
            var datas = new List<BsonDocument>(count);
            var keyDict = new Dictionary<string, string>();
            foreach (var d in dataList.OrderBy(s => s.String("nodeKey")))
            {
                var record = new BsonDocument();
                foreach (var s in fields)
                {
                    record.Add(s, d.String(s));
                    record.Add(s+"_bak", d.String(s));//初始化暂存区数据
                }

                foreach (var dict in keyValueDict)
                {
                    record.Add(dict.Key, dict.Value);
                }

                var keyValue = d.String(primaryKey);
                record.Add("srcId", keyValue);//记录被复制节点的主键值
                if (!hasTemplateId)//没有传入拷贝templateId字段，则表示要记录拷贝的Id为模板Id
                {
                    record.Add("templateId", keyValue);//记录模板的字段值
                }
                var oldNodePid = d.String("nodePid");//获取被复制节点的父节点id；
                var nodePid = "0";

                if (keyDict.ContainsKey(oldNodePid))
                {
                    nodePid = keyDict[oldNodePid];
                }
                record.Add("nodePid", nodePid);
                var result = _ctx.Insert(tableName, record);
                if (result.Status == Status.Successful)
                {
                    keyDict.Add(d.String(primaryKey), result.BsonInfo.String(primaryKey));//记录树形对应节点的主键对
                }
                datas.Add(record);
            }
            return datas;
        }
        

        /// <summary>
        /// 通过表名，项目id，版本id获取对应表的目录信息,
        /// </summary>
        /// <param name="tbName">表名</param>
        /// <param name="projId"></param>
        /// <param name="versionId"></param>
        /// <returns></returns>
        public List<BsonDocument> GetTableDirs(string tbName, string projId, string versionId)
        {
            var dirs = new List<BsonDocument>();
            if (!string.IsNullOrEmpty(tbName))
            {
                var dirName = tbName + "Dir";
                var tbRule = new TableRule(tbName);
                var primaryKey = tbRule.ColumnRules.Single(s => s.IsPrimary).Name;

                var dirRule = new TableRule(dirName);
                var dirKey = dirRule.ColumnRules.Single(s => s.IsPrimary).Name;
                //找出对应从模板拷贝的目录信息
                var dirParent = _ctx.FindOneByQuery(tbName, Query.And(Query.EQ("versionId", versionId), Query.EQ("projId", projId)));
                dirs = _ctx.FindAllByQuery(dirName, Query.EQ(primaryKey, dirParent.String(primaryKey))).ToList();
            }
            return dirs;
        }


        /// <summary>
        /// 通过表名，项目id，版本id获取对应表的目录信息,
        /// </summary>
        /// <param name="tbName">表名</param>
        /// <param name="projId"></param>
        /// <param name="versionId"></param>
        /// <returns></returns>
        public List<BsonDocument> GetTableDirsByVersionId(string tbName, string versionId)
        {
            var dirs = new List<BsonDocument>();
            if (!string.IsNullOrEmpty(tbName))
            {
                var tbRule = new TableRule(tbName);
                var primaryKey = tbRule.PrimaryKey;

                var dirName = tbName + "Dir";//目录表
                var dirRule = new TableRule(dirName);
                var dirKey = dirRule.PrimaryKey;//目录主键
                //找出对应从模板拷贝的目录信息
                var dirParent = _ctx.FindOneByQuery(tbName, Query.EQ("versionId", versionId));
                dirs = _ctx.FindAllByQuery(dirName, Query.EQ(primaryKey, dirParent.String(primaryKey))).ToList();
            }
            return dirs;
        }
        /// <summary>
        /// 从上一个版本拷贝数据到新版本（从模板复制的新版本）
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="projId"></param>
        /// <param name="oldVersionId"></param>
        /// <param name="preVersionId"></param>
        /// <param name="fields"></param>
        public void CopyDataFromOldVersion(string tableName, string projId, string oldVersionId, string preVersionId, string[] fields) 
        {
            var tableRule = new TableRule(tableName);
            var primaryKey = tableRule.PrimaryKey;
            var dirRule = new TableRule(tableName+"Dir");
             var dirKey = dirRule.PrimaryKey;//目录主键
            var oldNode = _ctx.FindOneByQuery(tableName, Query.And(Query.EQ("versionId", oldVersionId), Query.EQ("projId", projId)));//上一个版本的数据
            var oldDirs = _ctx.FindAllByQuery(tableName + "Dir", Query.EQ(primaryKey, oldNode.String(primaryKey))).ToList();//获取上一个节点的目录结构
            var curNode = _ctx.FindOneByQuery(tableName, Query.And(Query.EQ("versionId", preVersionId), Query.EQ("projId", projId)));//最新版本的数据
            var curDirs = _ctx.FindAllByQuery(tableName + "Dir", Query.EQ(primaryKey, curNode.String(primaryKey))).ToList();//获取当前最新版本的目录结构
            Dictionary<string, string> preOldKey = new Dictionary<string, string>();
            var curParent = curDirs.Where(x => x.Int("nodePid") == 0).FirstOrDefault();
            var oldParent = oldDirs.Where(x => x.Int("nodePid") == 0).FirstOrDefault();
            if (curParent.String("name").Trim() == curParent.String("name").Trim()) 
            {
                foreach (var s in fields)
                {
                    if (curParent.ContainsColumn(s))
                    {
                        curParent[s] = oldParent.String(s);
                    }
                    else
                    {
                        curParent.Add(s, oldParent.String(s));
                    }
                    if (curParent.ContainsColumn(s + "_bak"))
                    {
                        curParent[s + "_bak"] = oldParent.String(s + "_bak");
                    }
                    else
                    {
                        curParent.Add(s + "_bak", oldParent.String(s + "_bak"));
                    }
                }
                var result = _ctx.Update(tableName + "Dir", Query.EQ(dirKey, curParent.String(dirKey)), curParent);
                preOldKey.Add(curParent.String(dirKey), oldParent.String(dirKey));
                foreach (var tempDir in curDirs.Where(s => s.Int("nodePid") != 0).OrderBy(x => x.String("nodeKey")))
                {
                    if (!preOldKey.ContainsKey(tempDir.String("nodePid")))
                    {
                        continue;
                    }
                    BsonDocument tempOld = oldDirs.Where(x => x.String("name").Trim() == tempDir.String("name").Trim() && x.Int("nodeLevel") == tempDir.Int("nodeLevel") && x.String("nodePid") == preOldKey[tempDir.String("nodePid")]).FirstOrDefault();
                    if (tempOld == null)
                    {
                        continue;
                    }
                    else
                    {
                        preOldKey.Add(tempDir.String(dirKey), tempOld.String(dirKey));
                    }
                    foreach (var s in fields)
                    {
                        if (tempDir.ContainsColumn(s))
                        {
                            tempDir[s] = tempOld.String(s);
                        }
                        else
                        {
                            tempDir.Add(s, tempOld.String(s));
                        }
                        if (tempDir.ContainsColumn(s + "_bak"))
                        {
                            tempDir[s + "_bak"] = tempOld.String(s + "_bak");
                        }
                        else
                        {
                            tempDir.Add(s + "_bak", tempOld.String(s + "_bak"));
                        }
                    }
                    result = _ctx.Update(tableName+"Dir", Query.EQ(dirKey, tempDir.String(dirKey)), tempDir);
                }
            }
        
        }


        /// <summary>
        /// 决策数据确认
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="versionId"></param>
        public void ConfirmPolicyData(string tbName, string versionId)
        {
            switch (tbName)
            {
                case PolicyDept.ProjNode:
                case PolicyDept.MarketingArea:
                case PolicyDept.DesignArea:
                    ConfirmCommonData(tbName, versionId);
                    break;
                case PolicyDept.Finance:
                    ConfirmFinanceData(tbName, versionId);
                    break;
                case PolicyDept.Cost:
                    ConfirmCommonData(tbName, versionId, 0);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 确认总办，市场，设计数据
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="versionId"></param>
        private void ConfirmCommonData(string tbName, string versionId, int belong = -1)
        {
            IList<string> keys = PolicyDataInfo.PolicyDataDict[tbName];
            IList<BsonDocument> dirList = GetTableDirsByVersionId(tbName, versionId);//获取目录名称
            if (belong > -1)
            {
                dirList = dirList.Where(s => s.Int("belong") == belong).ToList();
            }
            TableRule dirRule = new TableRule(tbName + "Dir");
            ConfirmData(dirRule, dirList, keys);
        }

        /// <summary>
        /// 确认财务数据
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="versionId"></param>
        private void ConfirmFinanceData(string tbName, string versionId)
        {
            ConfirmCommonData(PolicyDept.Cost, versionId, 1);//更新财务成本数据


            var repayDetails = _ctx.FindAllByQuery("RepayDetail", Query.EQ("versionId", versionId));
            var sellDetails = _ctx.FindAllByQuery("SellDetail", Query.EQ("versionId", versionId));
            ConfirmDetail("RepayDetail", versionId, repayDetails, "loan", "repayVal", "interest");
            ConfirmDetail("SellDetail", versionId, sellDetails, "salesVal");
        }

        /// <summary>
        /// 确认还款计划，销售回款计划
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="versionId"></param>
        /// <param name="details"></param>
        /// <param name="keys"></param>
        private void ConfirmDetail(string tbName, string versionId, IEnumerable<BsonDocument> details, params string[] keys)
        {
            int tempVersion;
            if (int.TryParse(versionId, out tempVersion) && tempVersion > 0)
            {
                foreach (var item in details)
                {
                    BsonDocument update = new BsonDocument();
                    foreach (var key in keys)
                    {
                        string bakVal = item.String(key + "_bak");
                        if (!string.IsNullOrEmpty(bakVal)) update.Add(key, bakVal);
                    }
                    _ctx.Update(tbName, Query.And(Query.EQ("date", item.String("date")), Query.EQ("versionId", versionId)), update);
                }
            }
        }

     

        /// <summary>
        /// 确认决策数据
        /// </summary>
        /// <param name="rule"></param>
        /// <param name="dirList"></param>
        /// <param name="keys"></param>
        private void ConfirmData(TableRule rule, IEnumerable<BsonDocument> dirList, IEnumerable<string> keys)
        {
            foreach (var dir in dirList)
            {
                BsonDocument update = new BsonDocument();
                foreach (var key in keys)
                {
                    string bakVal = dir.String(key + "_bak");
                    if (!string.IsNullOrEmpty(bakVal)) update.Add(key, bakVal);
                }
                _ctx.Update(rule.Name, Query.EQ(rule.PrimaryKey, dir.String(rule.PrimaryKey)), update);
            }
        }


    }


    public abstract class PolicyDataInfo
    {
        public static readonly Dictionary<string, List<string>> PolicyDataDict;
        static PolicyDataInfo()
        {
            PolicyDataDict = new Dictionary<string, List<string>>();
            PolicyDataDict.Add(PolicyDept.ProjNode, new List<string> { "completeDate" });
            PolicyDataDict.Add(PolicyDept.MarketingArea, new List<string> { "areaValue", "maxVal", "minVal", "commonVal" });
            PolicyDataDict.Add(PolicyDept.DesignArea, new List<string> { "areaValue" });
            PolicyDataDict.Add(PolicyDept.Finance, new List<string> { "areaValue" });
            PolicyDataDict.Add(PolicyDept.Cost, new List<string> { "rate", "amount", "quota"});
        }
    }
}
