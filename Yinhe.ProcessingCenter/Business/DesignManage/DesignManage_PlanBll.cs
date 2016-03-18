using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using Yinhe.ProcessingCenter.DataRule;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using MongoDB.Driver;
namespace Yinhe.ProcessingCenter.DesignManage.TaskFormula
{
    /// <summary>
    /// 项目计划处理类
    /// </summary>
    public class DesignManage_PlanBll
    {
        #region 构造函数
        /// <summary>
        /// 类私有变量
        /// </summary>
        private DataOperation dataOp = null;

        private string tableName = "";
        /// <summary>
        /// 封闭当前默认构造函数
        /// </summary>
        private DesignManage_PlanBll()
        {
            dataOp = new DataOperation();
        }

        private DesignManage_PlanBll(DataOperation _dataOp)
        {
            dataOp = _dataOp;
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static DesignManage_PlanBll _()
        {
            return new DesignManage_PlanBll();
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static DesignManage_PlanBll _(DataOperation _dataOp)
        {
            return new DesignManage_PlanBll(_dataOp);
        }

        public bool IgnoreSameNode { get { return SysAppConfig.IsIgnoreName; } }//模板载入的时候是否忽略名称相同节点,
        private int offSetDay = 0;//时间偏移量
        private int offSetMonth = 0;//时间偏移量
        private int offSetYear = 0;//时间偏移量

        private bool isIgnorePeople = false;//是否忽略载入人员
        private bool isIgnoreDate = false;//是否忽略载入时间
        private bool isIgnorePassed = false;//是否忽略跳过任务
        #region  属性
        /// <summary>
        /// 是否载入人员
        /// </summary>
        public bool IsIgnorePeople
        {
            get
            {
                return isIgnorePeople;
            }
            set
            {
                isIgnorePeople = value;
            }
        }

        /// <summary>
        /// 是否载入人员
        /// </summary>
        public bool IsIgnoreDate
        {
            get
            {
                return isIgnoreDate;
            }
            set
            {
                isIgnoreDate = value;
            }
        }

        /// <summary>
        /// 是否忽略跳过任务
        /// </summary>
        public bool IsIgnorePassed
        {
            get
            {
                return isIgnorePassed;
            }
            set
            {
                isIgnorePassed = value;
            }
        }

        /// <summary>
        /// 任务时间偏移量
        /// </summary>
        public int OffSetDay
        {
            get
            {

                return offSetDay;
            }
            set
            {
                offSetDay = value;
            }
        }
        /// <summary>
        /// 任务时间偏移量
        /// </summary>
        public int OffSetMonth
        {
            get
            {

                return offSetMonth;
            }
            set
            {
                offSetMonth = value;
            }
        }
        /// <summary>
        /// 任务时间偏移量
        /// </summary>
        public int OffSetYear
        {
            get
            {

                return offSetYear;
            }
            set
            {
                offSetYear = value;
            }
        }

        #endregion
        #region 查询
        /// <summary>
        /// 获取项目模板
        /// </summary>
        /// <returns></returns>
        public BsonDocument FindGroupExpLib()
        {
            var expLib = dataOp.FindOneByQueryStr("XH_DesignManage_Plan", "isExpTemplate=1&isPrimLib=1");
            return expLib;
        }
        #endregion
        #endregion
        /// <summary>
        /// 赋值辅助计划
        /// </summary>
        /// <param name="copyId">对应对象Id</param>
        /// <param name="srcId">需要初始化的对象</param>
        /// <returns></returns>
        public InvokeResult CopySecPlan(string _tableName, int copyId, int srcId, int userId)
        {
            tableName = _tableName;
            TableRule childTable = new TableRule(tableName);        //获取子表的表结构
            ColumnRule foreignColumn = childTable.ColumnRules.Where(t => t.IsPrimary == true).FirstOrDefault();  //子表指向源表的外键字段
            var keyField = string.Empty;
            if (foreignColumn != null)
            {
                keyField = foreignColumn.Name;
            }
            InvokeResult result = new InvokeResult();
            var copyObj = dataOp.FindOneByKeyVal(tableName, keyField, copyId.ToString());
            var srcObj = dataOp.FindOneByKeyVal(tableName, keyField, srcId.ToString());
            if (srcObj.Int("status") == (int)ProjectPlanStatus.Completed)
            {
                return new InvokeResult() { Status = Status.Failed, Message = "该计划已经完成不能载入" };
            }
            if (copyObj == null || srcObj == null) return new InvokeResult() { Status = Status.Failed, Message = "传入参数有误刷新后重试" };
           // using (TransactionScope tran = new TransactionScope())
            {
                try
                {
                    result = CopyObject(copyObj, srcObj, userId);
                    if (result.Status == Status.Successful)
                    {
                     //   tran.Complete();
                    }
                    else
                    {
                        return result;
                    }

                }
                catch (Exception ex)
                {
                    result.Status = Status.Failed;
                    result.Message = "复制失败，请确认该是否有错误的数据！";
                    return result;
                }

            }
            return result;
        }
        /// <summary>
        /// 赋值计划信息
        /// </summary>
        /// <param name="copyObj"></param>
        /// <param name="srcObj"></param>
        public InvokeResult CopyObject(BsonDocument copyObj, BsonDocument srcObj, int userId)
        {
            Dictionary<string, object> ForeignKey = new Dictionary<string, object>();
            #region 此处未完 用于传入需要设置的其他任务信息
            ForeignKey.Add("IsIgnorePeople", IsIgnorePeople);
            ForeignKey.Add("IsIgnoreDate", IsIgnoreDate);
            ForeignKey.Add("IsIgnorePassed", IsIgnorePassed);
            ForeignKey.Add("offSetYear", offSetYear);
            ForeignKey.Add("offSetMonth", offSetMonth);
            ForeignKey.Add("offSetDay", offSetDay);
            #endregion
            #region 任务赋值
            InvokeResult retCatResult = new InvokeResult();
            retCatResult = CreatePlanTaskFromExpLib(copyObj, srcObj, userId, ForeignKey);
            return retCatResult;
            #endregion

        }

        /// <summary>
        /// 更新计划任务
        /// </summary>
        /// <param name="secPlan"></param>
        /// <param name="managerUserId"></param>
        /// <param name="managerProfId"></param>
        /// <param name="currUserId"></param>
        /// <param name="currUserProfId"></param>
        /// <param name="checkMe"></param>
        /// <returns></returns>
        public InvokeResult Update(BsonDocument secPlan, int managerUserId, int managerProfId, int currUserId, int currUserProfId, bool checkMe)
        {
            InvokeResult result = new InvokeResult();
            try
            {
                using (TransactionScope trans = new TransactionScope())
                {

                    var query = Query.And(
                        Query.NE("planId", secPlan.Text("planId")),
                        Query.EQ("projId", secPlan.Text("projId")),
                        Query.EQ("name", secPlan.Text("name")));
                    var ExistsPlanByName = dataOp.FindAllByQuery("XH_DesignManage_Plan", query);
                    if (ExistsPlanByName.Count() > 0)
                    {
                        throw new Exception("该计划名称已经存在，请更改后重试");
                    }
                    if (string.IsNullOrEmpty(secPlan.Text("planId")))
                    {
                        result = dataOp.Insert("XH_DesignManage_Plan", secPlan);
                        secPlan = result.BsonInfo;
                    }
                    var existManager = dataOp.FindOneByKeyVal("XH_DesignManage_PlanManager", "planId", secPlan.Text("planId"));
                    if (existManager != null)
                    {
                        existManager.Set("userId", managerUserId);
                    }
                    else
                    {
                        //添加计划负责人表中
                        var manager = new BsonDocument();
                        manager.Add("planId", secPlan.Text("planId"));
                        manager.Add("userId", managerUserId);
                        dataOp.Insert("XH_DesignManage_PlanManager", manager);
                    }

                    if (checkMe)//如果勾选了“将我设置为负责人”
                    {
                        //将当前负责人的专业添加到辅助专业
                        var curmanager = new BsonDocument();
                        curmanager.Add("planId", secPlan.Text("planId"));
                        curmanager.Add("userId", currUserId);
                        dataOp.Insert("XH_DesignManage_PlanManager", curmanager);
                    }
                    trans.Complete();
                    result.Status = Status.Successful;
                    result.BsonInfo = secPlan;
                }
            }
            catch (Exception ex)
            {
                result.Status = Status.Failed;
                result.Message = ex.Message;
            }

            return result;
        }


        /// <summary>
        /// 更新计划任务
        /// </summary>
        /// <param name="secPlan"></param>
        /// <param name="managerUserId"></param>
        /// <param name="managerProfId"></param>
        /// <param name="currUserId"></param>
        /// <param name="currUserProfId"></param>
        /// <param name="checkMe"></param>
        /// <returns></returns>
        public InvokeResult Update(BsonDocument secPlan,BsonDocument updateBson, int managerUserId, int managerProfId, int currUserId, int currUserProfId, bool checkMe)
        {
            InvokeResult result = new InvokeResult();
            try
            {
                using (TransactionScope trans = new TransactionScope())
                {

                    

                    var query = Query.And(
                        Query.NE("planId", secPlan.Text("planId")),
                        Query.EQ("projId", secPlan.Text("projId")),
                        Query.EQ("name", secPlan.Text("name")));
                    var ExistsPlanByName = dataOp.FindAllByQuery("XH_DesignManage_Plan", query);
                    if (ExistsPlanByName.Count() > 0)
                    {
                        throw new Exception("该计划名称已经存在，请更改后重试");
                    }
                    if (string.IsNullOrEmpty(secPlan.Text("planId")))
                    {
                        result = dataOp.Insert("XH_DesignManage_Plan", secPlan);
                        secPlan = result.BsonInfo;
                    }
                    else
                    {
                        result = dataOp.Update(secPlan, updateBson);
                        secPlan = result.BsonInfo;
                    }

                    var existManager = dataOp.FindOneByKeyVal("XH_DesignManage_PlanManager", "planId", secPlan.Text("planId"));
                    if (existManager != null)
                    {
                        existManager.Set("userId", managerUserId);
                    }
                    else
                    {
                        //添加计划负责人表中
                        var manager = new BsonDocument();
                        manager.Add("planId", secPlan.Text("planId"));
                        manager.Add("userId", managerUserId);
                        dataOp.Insert("XH_DesignManage_PlanManager", manager);
                    }

                    if (checkMe)//如果勾选了“将我设置为负责人”
                    {
                        //将当前负责人的专业添加到辅助专业
                        var curmanager = new BsonDocument();
                        curmanager.Add("planId", secPlan.Text("planId"));
                        curmanager.Add("userId", currUserId);
                        dataOp.Insert("XH_DesignManage_PlanManager", curmanager);
                    }
                    trans.Complete();
                    result.Status = Status.Successful;
                    result.BsonInfo = secPlan;
                }
            }
            catch (Exception ex)
            {
                result.Status = Status.Failed;
                result.Message = ex.Message;
            }

            return result;
        }


        #region 添加计划负责人
        /// <summary>
        /// 主要用于载入模板设置
        /// </summary>
        /// <param name="secPlan"></param>
        /// <returns></returns>
        public InvokeResult Update(BsonDocument secPlan,BsonDocument updateBson, List<BsonDocument> secPlanManagerList, Dictionary<int, int> projProfIdList)
        {
            InvokeResult result = new InvokeResult();
            var projId = secPlan.Int("projId");
            try
            {
                using (TransactionScope tran = new TransactionScope())
                {
                     result=dataOp.Update(secPlan, updateBson);
                     if (result.Status != Status.Successful)
                     {
                        return result;
                     }

                    var existList = dataOp.FindAllByQueryStr("XH_DesignManage_PlanManager", "planId=" + secPlan.Text("planId"));
                    var userIdList = secPlanManagerList.Select(m => m.Int("userId"));
                    var DelList = existList.Where(m => userIdList.Contains(m.Int("userId")) == false);
                    var ExistsUserList = existList.Select(m => m.Int("userId")).ToList();
                    var AddList = secPlanManagerList.Where(u => ExistsUserList.Contains(u.Int("userId")) == false);

                    if (DelList.Count() > 0)
                    {
                        dataOp.QuickDelete("XH_DesignManage_PlanManager", DelList.ToList());
                    }
                    if (AddList.Count() > 0)
                    {
                        dataOp.QuickInsert("XH_DesignManage_PlanManager", AddList.ToList());
                    }


                    tran.Complete();
                    result.Status = Status.Successful;

                }
            }
            catch (Exception ex)
            {
                result.Status = Status.Failed;
                result.Message = ex.Message;
            }

            return result;
        }


        /// <summary>
        /// 用户初始化对应脉络图节点
        /// </summary>
        /// <param name="addList"></param>
        /// <param name="updateList"></param>
        /// <returns></returns>
        public InvokeResult InititalDiagramTask(BsonDocument plan)
        {
            #region 添加集团模板任务节点
            var InsertResult = new InvokeResult();
            var findAllNode = this.dataOp.FindAll("XH_DesignManage_ContextDiagram");
            var rootNode = dataOp.FindOneByQueryStr("XH_DesignManage_Task", string.Format("planId={0}&nodePid=0", plan.Text("planId")));
            var rootId = "0";
            if (rootNode == null)//当不是新的工作台则必须创建，当时新的工作台，且rootnode=null时候才建
            {
                var docLib = new BsonDocument();
                docLib.Add("name", Guid.NewGuid());
                docLib.Add("planId", plan.Text("planId"));
                docLib.Add("nodePid", "0");
                InsertResult = dataOp.Insert("XH_DesignManage_Task", docLib);
                if (InsertResult.Status == Status.Successful)
                {
                    docLib = InsertResult.BsonInfo;
                    rootId = docLib.Text("taskId");
                }
                else
                {
                    return InsertResult;
                }
            }
            else
            {
                rootId = rootNode.Text("taskId");
            }
            var updateList = new Dictionary<BsonDocument, string>();
            var addList = new List<BsonDocument>();
            foreach (var node in findAllNode)
            {
                var diagramId = node.Text("diagramId");
                var hitObj = this.dataOp.FindOneByQueryStr("XH_DesignManage_Task", string.Format("diagramId={0}&planId={1}", diagramId, plan.Text("planId")));
                if (hitObj != null)
                {
                    updateList.Add(hitObj, string.Format("name={0}&type={1}&keyTask={2}&needApproval={3}", node.Text("name"), node.Text("type"), node.Text("keyTask"), node.Text("needApproval")));
                }
                else
                {
                    hitObj = new BsonDocument();
                    hitObj.Add("name", node.Text("name"));
                    hitObj.Add("type", node.Text("type"));
                    hitObj.Add("keyTask", node.Text("keyTask"));
                    hitObj.Add("needApproval", node.Text("needApproval"));
                    hitObj.Add("planId", plan.Text("planId"));
                    hitObj.Add("nodePid", rootId);
                    hitObj.Add("diagramId", node.Text("diagramId"));
                    addList.Add(hitObj);
                }
            }
            #endregion
            var result = new InvokeResult() { Status = Status.Successful };
            using (TransactionScope tran = new TransactionScope())
            {
                if (addList.Count() > 0)
                {
                    result = this.dataOp.QuickInsert("XH_DesignManage_Task", addList);
                }
                if (updateList.Count() > 0)
                {
                    result = this.dataOp.QuickUpdate("XH_DesignManage_Task", updateList);
                }

                tran.Complete();
            }
            return result;
        }
        #endregion

        #region 根据公司经验创建任务
        /// <summary>
        /// 根据公司经验创建任务
        /// </summary>
        /// <param name="copyObj">经验库对应辅助计划</param>
        /// <param name="srcObj">需要初始化,被复制的辅助计划</param>
        /// <param name="UserId">用户Id</param>
        /// <param name="ForeignKey">传入基础数据参数</param>
        /// <returns></returns>
        public InvokeResult CreatePlanTaskFromExpLib(BsonDocument copyObj, BsonDocument srcObj, int UserId, Dictionary<string, object> ForeignKey)
        {
            #region 判断参数是否正确
            if (copyObj == null || srcObj == null)
            {
                return new InvokeResult() { Status = Status.Failed, Message = "传入参数有误？任务创建失败" };
            }
            var projId = copyObj.Int("projId");

            #endregion

            var ObjIdsList = new Dictionary<int, int>();//用于存储新增的旧keyId 与新keyId对应字典
            var ExistIdsList = new List<int>();

            #region 判断是否为区域模板载入

            var IsTemplate = copyObj.Int("isExpTemplate") == 1 ? true : false;
            #endregion

            #region  参数设定
            var offSetYear = 0;
            var offSetMonth = 0;
            var offSetDay = 0;

            try
            {
                if (ForeignKey.ContainsKey("IsIgnorePeople"))
                {
                    IsIgnorePeople = (bool)ForeignKey["IsIgnorePeople"];
                }
                if (ForeignKey.ContainsKey("IsIgnoreDate"))
                {
                    IsIgnoreDate = (bool)ForeignKey["IsIgnoreDate"];
                }
                if (ForeignKey.ContainsKey("IsIgnorePassed"))
                {
                    IsIgnorePassed = (bool)ForeignKey["IsIgnorePassed"];
                }
                if (ForeignKey.ContainsKey("offSetYear"))
                {
                    offSetYear = int.Parse(ForeignKey["offSetYear"].ToString());
                }
                if (ForeignKey.ContainsKey("offSetMonth"))
                {
                    offSetMonth = int.Parse(ForeignKey["offSetMonth"].ToString());
                }
                if (ForeignKey.ContainsKey("offSetDay"))
                {
                    offSetDay = int.Parse(ForeignKey["offSetDay"].ToString());
                }

            }
            catch (InvalidCastException ex)
            {
                offSetYear = 0;
                offSetMonth = 0;
                offSetDay = 0;
                IsIgnorePeople = false;
                IsIgnoreDate = false;
            }
            catch (Exception ex)
            {
                offSetYear = 0;
                offSetMonth = 0;
                offSetDay = 0;
                IsIgnorePeople = false;
                IsIgnoreDate = false;
            }
            #endregion

            var totalTaskQuery = dataOp.FindAllByKeyVal("XH_DesignManage_Task", "planId", copyObj.Text("planId"));
            var taskQuery = totalTaskQuery.OrderBy(c => c.Text("nodeKey"));
            var existTaskList = dataOp.FindAllByKeyVal("XH_DesignManage_Task", "planId", srcObj.Text("planId")).ToList();
            var rootNode = dataOp.FindOneByQueryStr("XH_DesignManage_Task", string.Format("planId={0}&nodePid=0", srcObj.Text("planId")));
            try
            {
                //using (TransactionScope tran = new TransactionScope())
                {

                    #region 遍历计划任务，创建并初始化关联数据
                    foreach (var taskObj in taskQuery)
                    {
                        var uniqueKey = taskObj.Int("taskId");

                        var nodePid = taskObj.Int("nodePid");   //获取父节点
                        var ParentId = 0;
                        if (nodePid != 0)
                        {
                            if (!ObjIdsList.ContainsKey(nodePid)) continue;
                            ParentId = ObjIdsList[nodePid];
                        }

                        try
                        {
                            #region 任务创建并初始化
                            BsonDocument OldObj = null;
                            if (!IgnoreSameNode)//是否需要重名判断
                            {
                                //根据模板名称树状节点的级别和名称获取是否有同名的目录名称
                                OldObj = existTaskList.Where(c => c.Text("name").Contains(taskObj.Text("name")) && c.Int("nodeLevel") == taskObj.Int("nodeLevel")).FirstOrDefault();
                            }
                            if (IgnoreSameNode == true || OldObj == null)//不存在需要新增 实现增量添加
                            {
                                #region 初始化ProjPlanTask对象
                                var docLib = new BsonDocument();
                                docLib.Add("planId", srcObj.Text("planId"));
                                docLib.Add("projId", srcObj.Text("projId"));
                                docLib.Add("keyTask", taskObj.Text("keyTask"));
                                docLib.Add("curState", taskObj.Text("curState"));
                                if (nodePid == 0 && rootNode==null)//根目录设置
                                {
                                    docLib.Add("name", string.Format("{0}根任务",srcObj.Text("name")));
                                }
                                else
                                {
                                    docLib.Add("name", taskObj.Text("name"));
                                }
                                docLib.Add("nodePid", taskObj.Text("nodePid"));
                                docLib.Add("remark", taskObj.Text("remark"));
                                docLib.Add("type", taskObj.Text("type"));
                                docLib.Add("approvalDepart", taskObj.Text("approvalDepart"));
                                docLib.Add("status", taskObj.Text("status"));
                                //节点类型
                                docLib.Add("nodeTypeId", taskObj.Text("nodeTypeId"));
                                //一二级联动节点
                                docLib.Add("linkId", taskObj.Text("linkId"));
                                //阶段性成果
                                docLib.Add("InitialResultId", taskObj.Text("InitialResultId"));
                                docLib.Add("levelId", taskObj.Text("levelId"));
                                docLib.Add("srcPlanId", taskObj.Text("planId"));
                                docLib.Add("stageId", taskObj.Text("stageId"));
                                docLib.Add("operateStatus", taskObj.Text("operateStatus"));
                                docLib.Add("srcTaskId",  taskObj.Text("taskId"));
                                docLib.Add("srcPrimTaskId", IsTemplate ? taskObj.Text("taskId") : taskObj.Text("srcPrimTaskId"));
                                docLib.Add("groupId", taskObj.Text("groupId"));
                                docLib.Add("diagramId", taskObj.Text("diagramId"));
                                docLib.Add("pointId", taskObj.Text("pointId"));
                                docLib.Add("hasCIList", taskObj.Text("hasCIList"));
                                docLib.Add("hasPush", taskObj.Text("hasPush"));
                                docLib.Add("paymentDisplayName", taskObj.Text("paymentDisplayName"));
                                docLib.Add("designServiceContent", taskObj.Text("designServiceContent"));
                                docLib.Add("designServicesStage", taskObj.Text("designServicesStage"));
                                docLib.Add("contractTypeId", taskObj.Text("contractTypeId"));
                                docLib.Add("contractCatId", taskObj.Text("contractCatId"));
                                docLib.Add("isInitialResult", taskObj.Text("isInitialResult"));
                                docLib.Add("isLink", taskObj.Text("isLink"));
                                docLib.Add("isShowIndex", taskObj.Text("isShowIndex"));
                                docLib.Add("isIndicator", taskObj.Text("isIndicator"));
                                docLib.Add("isMaterial", taskObj.Text("isMaterial"));
                                docLib.Add("isResult", taskObj.Text("isResult"));
                                docLib.Add("indicatorRemark", taskObj.Text("indicatorRemark"));
                                docLib.Add("materialRemark", taskObj.Text("materialRemark"));
                                docLib.Add("resultRemark", taskObj.Text("resultRemark"));
                                docLib.Add("taskClassId", taskObj.Text("taskClassId"));
                                docLib.Add("hiddenTask", taskObj.Text("hiddenTask"));
                                #region 任务状态控制
                                switch (srcObj.Int("status"))
                                {
                                    case (int)ProjectPlanStatus.Unconfirmed:
                                        if (docLib.Int("status") != (int)TaskStatus.ToSplit && docLib.Int("status") != (int)TaskStatus.SplitCompleted)
                                        {
                                            docLib["status"] = (int)TaskStatus.SplitCompleted;
                                        }
                                        break;
                                    case (int)ProjectPlanStatus.Processing:
                                        if (docLib.Int("status") == (int)TaskStatus.ToSplit || docLib.Int("status") == (int)TaskStatus.SplitCompleted)
                                        {
                                            docLib["status"] = (int)TaskStatus.NotStarted;
                                        }
                                        break;
                                    default:
                                        break;
                                }
                                #endregion
                                #region 添加偏移量
                                if (IsIgnoreDate == false)
                                {
                                    if (taskObj.Date("curStartData") != DateTime.MinValue)
                                    {
                                        docLib.Add("curStartData", taskObj.Date("curStartData").AddYears(offSetYear).AddMonths(offSetMonth).AddDays(offSetDay));

                                    }
                                    if (taskObj.Date("curEndData") != DateTime.MinValue)
                                    {
                                        docLib.Add("curEndData", taskObj.Date("curEndData").AddYears(offSetYear).AddMonths(offSetMonth).AddDays(offSetDay));

                                    }
                                  
                                }
                                docLib.Add("period", taskObj.Text("period"));

                                #endregion
                                #endregion

                                if ( nodePid == 0)//此处需要注意任务有个根节点，可能出现多个根节点情况
                                {
                                    if (rootNode == null)//当不是新的工作台则必须创建，当时新的工作台，且rootnode=null时候才建
                                    {
                                        var InsertResult = new InvokeResult();
                                        docLib.Set("nodePid", "0");
                                        InsertResult = dataOp.Insert("XH_DesignManage_Task", docLib);
                                        if (InsertResult.Status == Status.Successful)
                                        {
                                            docLib = InsertResult.BsonInfo;
                                            ObjIdsList.Add(uniqueKey, docLib.Int("taskId"));
                                            if (!ExistIdsList.Contains(docLib.Int("taskId")))
                                            {
                                                ExistIdsList.Add(docLib.Int("taskId"));
                                            }
                                        }
                                        else
                                        {
                                            return InsertResult;
                                        }
                                    }
                                    else//当时新的工作台，且rootnode！=null时候才建
                                    {
                                        ObjIdsList.Add(uniqueKey, rootNode.Int("taskId"));
                                        if (!ExistIdsList.Contains(rootNode.Int("taskId")))
                                        {
                                            ExistIdsList.Add(rootNode.Int("taskId"));
                                        }
                                    }
                                }
                                else
                                {
                                    if (ObjIdsList.ContainsKey(nodePid))//父节点是否已经添加过了
                                    {

                                        docLib.Set("nodePid", ParentId);
                                        var insertResult = dataOp.Insert("XH_DesignManage_Task", docLib);
                                        if (insertResult.Status == Status.Successful)
                                        {
                                            docLib = insertResult.BsonInfo;
                                            ObjIdsList.Add(uniqueKey, docLib.Int("taskId"));
                                        }
                                        else
                                        {
                                            return insertResult;
                                        }
                                    }
                                }
                                //初始化任务关联对象信息
                                //  CopyTaskToTask(copyObj, srcObj, taskObj, docLib, UserId, projFileLibObj.projFileLibId);
                                // TaskManagerList.AddRange(docLib.TaskManagers);
                            }
                            else
                            {
                                if (!ExistIdsList.Contains(taskObj.Int("taskId")))
                                {
                                    ExistIdsList.Add(taskObj.Int("taskId"));
                                }
                                #region 2011.10.25新增复制来源
                                OldObj.Set("srcTaskId", taskObj.Text("taskId"));
                                OldObj.Set("srcSecdPlanId", taskObj.Text("srcSecdPlanId"));
                                OldObj.Set("srcPrimTaskId", IsTemplate ? taskObj.Text("taskId") : taskObj.Text("srcPrimTaskId"));
                                #endregion
                                if (!ObjIdsList.ContainsValue(OldObj.Int("taskId")))
                                {
                                    ObjIdsList.Add(uniqueKey, OldObj.Int("taskId"));
                                }
                                // TaskManagerList.AddRange(OldObj.TaskManagers);
                            }
                            #endregion
                        }
                        catch (Exception ex)
                        {
                            return new InvokeResult() { Status = Status.Failed, Message = "请传入正确的分项Id" };
                        }

                    }
                    #endregion

                    //初始化任务关联对象信息
                    CopyPlanRelateion(copyObj, srcObj, UserId, ObjIdsList, ExistIdsList);

                    ///保存地铁图
                    dataOp.QuickUpdate("XH_DesignManage_Plan", srcObj, "mapId=" + copyObj.Text("mapId"));

                    #region 初始化权限
                    //var initProjRoleResult = taskManagerBll.InitProjectRole(srcObj.projId);
                    //if (initProjRoleResult.Status != Status.Successful)
                    //{
                    //    return new InvokeResult() { Status = Status.Failed, Message = initProjRoleResult.Message };
                    //}
                    #endregion


                    #region 计划运营点创建初始化
                    //2011.5.20wk新增添加功能
                    var operatePointResult = CopyOperatingPointList(copyObj, srcObj, UserId);
                    if (operatePointResult.Status != Status.Successful)
                    {
                        return operatePointResult;
                    }
                    #endregion

                    #region 计划分组创建初始化
                    var taskGroupResult = CopyTaskGroupList(copyObj, srcObj, UserId, ObjIdsList);
                    if (taskGroupResult.Status != Status.Successful)
                    {
                        return taskGroupResult;
                    }
                    #endregion

                    #region 复制公式
                    var taskFormulaResult = CopyTaskFormulaList(copyObj, srcObj, UserId, ObjIdsList);
                    if (taskFormulaResult.Status != Status.Successful)
                    {
                        return taskFormulaResult;
                    }
                    #endregion

                    #region 项目技术关系
                    var taskRelationResult = CopyProjPlanRelation(copyObj, srcObj, UserId, ObjIdsList);
                    if (taskRelationResult.Status != Status.Successful)
                    {
                        return taskRelationResult;
                    }
                    #endregion

                    if (srcObj.Int("projId") != 0)//模板对象计划不用建关系
                    {
                        #region 项目技术关系
                        var taskAcrossPlanRelationResult = CopyAcrossPlanTaskRelation(copyObj, srcObj, UserId, ObjIdsList);
                        if (taskRelationResult.Status != Status.Successful)
                        {
                            return taskAcrossPlanRelationResult;
                        }
                        #endregion
                    }
                   // tran.Complete();
                }
            }
            catch (Exception ex)
            {
                return new InvokeResult() { Status = Status.Failed, Message = "操作失败请刷新重试" };
            }

            return new InvokeResult() { Status = Status.Successful };
        }
        #endregion

        #region  从原计划赋值分项任务


        /// <summary>
        /// 批量复制任务关联
        /// </summary>
        /// <param name="copyObj"></param>
        /// <param name="srcObj"></param>
        /// <param name="UserId"></param>
        /// <param name="projFileLibId"></param>
        /// <param name="ObjIdsList"></param>
        public void CopyPlanRelateion(BsonDocument copyObj, BsonDocument srcObj, int UserId, Dictionary<int, int> ObjIdsList, List<int> ExistIdsList)
        {
            var secdPlanId = srcObj.Int("planId");
            var projId = srcObj.Int("projId");
            var oldTaskIdList = ObjIdsList.Where(d => !ExistIdsList.Contains(d.Key)).Select(c => c.Key.ToString()).ToList();
            #region 人员安排
            if (IsIgnorePeople == false)//是否忽略人员安排
            {
                var taskManagerQuery = dataOp.FindAllByKeyValList("XH_DesignManage_TaskManager", "taskId", oldTaskIdList);
                foreach (var projProfEntity in taskManagerQuery)//遍历旧的taskManager
                {
                    if (!ObjIdsList.ContainsKey(projProfEntity.Int("taskId"))) continue;
                    var maptaskId = ObjIdsList[projProfEntity.Int("taskId")];

                    var newTaskManager = new BsonDocument();
                    newTaskManager.Add("type", projProfEntity.Text("type"));
                    newTaskManager.Add("userId", projProfEntity.Text("userId"));
                    newTaskManager.Add("profId", projProfEntity.Text("profId"));
                    newTaskManager.Add("taskId", maptaskId);
                    dataOp.Insert("XH_DesignManage_TaskManager", newTaskManager);
                }
            }
            #endregion
            #region 添加预定义属性
            var taskFileDefaultValueQuery = dataOp.FindAllByKeyValList("Scheduled_deliver", "taskId", oldTaskIdList);
            foreach (var taskVauleObj in taskFileDefaultValueQuery)  //遍历旧的数据
            {
                if (!ObjIdsList.ContainsKey(taskVauleObj.Int("taskId"))) continue;
                var maptaskId = ObjIdsList[taskVauleObj.Int("taskId")];
                var curTaskFileDefaultValueObj = new BsonDocument();
                curTaskFileDefaultValueObj.Add("profId", taskVauleObj.Text("profId"));
                curTaskFileDefaultValueObj.Add("stageId", taskVauleObj.Text("stageId"));
                curTaskFileDefaultValueObj.Add("fileCatId", taskVauleObj.Text("fileCatId"));
                curTaskFileDefaultValueObj.Add("isForce", taskVauleObj.Text("isForce"));
                curTaskFileDefaultValueObj.Add("projId", projId);
                curTaskFileDefaultValueObj.Add("taskId", maptaskId);
                curTaskFileDefaultValueObj.Add("Remark", taskVauleObj.Text("Remark"));
                curTaskFileDefaultValueObj.Add("EditType","1"); //模板的导入导出，均设置为1，1表示在具体项目中预定义组合不可编辑 0或者空为可编辑
                curTaskFileDefaultValueObj.Add("srcDeliverId", taskVauleObj.Text("deliverId")); //如果是模板导入或者导出的，用此字段记录来源组合Id
                var result1 = dataOp.Insert("Scheduled_deliver", curTaskFileDefaultValueObj);   //插入交付物定义
                if (result1.Status == Status.Successful)
                {
                    #region 添加预定义文档
                    var deliverId = result1.BsonInfo.Text("deliverId");   //获取Insert的交付物定义的Id
                    if (deliverId == null)
                        continue;
                    var taskResultFilesQuery = dataOp.FindAllByQuery("PredefineFile", Query.EQ("deliverId", taskVauleObj.Text("deliverId"))); //找出旧的交付物定义下的预定义交付文档
                    foreach (var prefile in taskResultFilesQuery)
                    {

                        var curBizPredefineFile = new BsonDocument();
                        curBizPredefineFile.Add("taskId", maptaskId);
                        curBizPredefineFile.Add("projId", projId);
                        curBizPredefineFile.Add("name", prefile.Text("name"));
                        curBizPredefineFile.Add("ext", prefile.Text("ext"));
                        curBizPredefineFile.Add("profId", prefile.Text("profId"));
                        curBizPredefineFile.Add("stageId", prefile.Text("stageId"));
                        curBizPredefineFile.Add("fileCatId", prefile.Text("fileCatId"));
                        curBizPredefineFile.Add("deliverId", deliverId);
                        dataOp.Insert("PredefineFile", curBizPredefineFile);
                    }
                    #endregion

                    #region 添加组合备注列表
                    var deliverRemarkQuery = dataOp.FindAllByQuery("PredefineFileRemark", Query.EQ("deliverId", taskVauleObj.Text("deliverId"))); //找出旧的交付物定义下的组合列表
                    foreach (var remark in deliverRemarkQuery)
                    {
                        var curRemark = new BsonDocument();
                        curRemark.Add("taskId", maptaskId);
                        curRemark.Add("name", remark.Text("name"));
                        curRemark.Add("EditType", "1");
                        curRemark.Add("isNeedUpLoad", remark.Text("isNeedUpLoad"));
                        curRemark.Add("srcRemarkId", remark.Text("remarkId"));
                        curRemark.Add("deliverId", deliverId);
                        dataOp.Insert("PredefineFileRemark", curRemark);
                    }
                    #endregion
                }

            }
            
            #endregion

            #region 添加目录文档属性
            List<BsonDocument> taskDocType = dataOp.FindAllByQuery("TaskDocProtery", Query.In("taskId", TypeConvert.StringListToBsonValueList(oldTaskIdList))).ToList();
            foreach (var taskVauleObj in taskDocType)  //遍历旧的数据
            {
                if (!ObjIdsList.ContainsKey(taskVauleObj.Int("taskId"))) continue;
                var maptaskId = ObjIdsList[taskVauleObj.Int("taskId")];
                var newTaskDoc = new BsonDocument();
                newTaskDoc.Add("sysProfId", taskVauleObj.Text("sysProfId"));
                newTaskDoc.Add("stageId", taskVauleObj.Text("stageId"));
                newTaskDoc.Add("fileCatId", taskVauleObj.Text("fileCatId"));
                newTaskDoc.Add("isForce", taskVauleObj.Text("isForce"));
                newTaskDoc.Add("projId", projId);
                newTaskDoc.Add("taskId", maptaskId);
                var result2 = dataOp.Insert("TaskDocProtery", newTaskDoc);   
              

            }

            #endregion
            #region 复制任务关联流程 ProjPlanTaskApproval


            var taskFlowQuery = dataOp.FindAllByKeyValList("XH_DesignManage_TaskBusFlow", "taskId", oldTaskIdList);
            foreach (var tFlow in taskFlowQuery)//遍历旧的taskManager
            {
                if (!ObjIdsList.ContainsKey(tFlow.Int("taskId"))) continue;
                var maptaskId = ObjIdsList[tFlow.Int("taskId")];
                var newTaskFlow = new BsonDocument();
                newTaskFlow.Add("flowId", tFlow.Text("flowId"));
                newTaskFlow.Add("taskId", maptaskId);
                dataOp.Insert("XH_DesignManage_TaskBusFlow", newTaskFlow);
            }
            
            #endregion

            #region 复制指引文档
            //var explibObj = copyObj.ExperienceLibraries.FirstOrDefault();
            //if (explibObj == null || (explibObj != null && explibObj.isPrimLib != 1))
            //{
            //    var taskFileRelationQuery = this._ctx.TaskFileRelations.Where(c => oldTaskIdList.Contains(c.taskId));
            //    foreach (var taskFile in taskFileRelationQuery)
            //    {
            //        var maptaskId = ObjIdsList[taskFile.taskId];
            //        if (maptaskId == null) continue;
            //        TaskFileRelation curTaskFileRelationObj = new TaskFileRelation()//添加ProjPlanTaskApproval
            //        {
            //            createDate = DateTime.Now,
            //            createUserId = UserId,
            //            fileId = taskFile.fileId,
            //            remark = taskFile.remark,
            //            status = taskFile.status,
            //            businessOrgId = taskFile.businessOrgId,
            //            fileType = taskFile.fileType
            //        };

            //        //curNewTask.TaskFileRelations.Add(curTaskFileRelationObj);
            //        if (maptaskId != null)
            //        {
            //            curTaskFileRelationObj.taskId = maptaskId;
            //        }

            //        this._ctx.TaskFileRelations.InsertOnSubmit(curTaskFileRelationObj);
            //    }
            //}
            #endregion

            #region 中海弘扬针对任务下多审批时，隐藏子任务的处理
            var taskClassList = dataOp.FindAllByKeyValList("TaskClass", "taskId", oldTaskIdList); //查找源任务的步骤列表
            foreach (var taskClass in taskClassList)//遍历旧的数据
            {
                if (!ObjIdsList.ContainsKey(taskClass.Int("taskId"))) continue;
                var maptaskId = ObjIdsList[taskClass.Int("taskId")];
                var newTaskFlow = new BsonDocument();
                newTaskFlow.Add("name", taskClass.Text("name"));
                newTaskFlow.Add("status", taskClass.Text("status"));
                newTaskFlow.Add("taskId", maptaskId);
                var result3 = dataOp.Insert("TaskClass", newTaskFlow);
                var curTaskList = dataOp.FindAllByQuery("XH_DesignManage_Task", Query.EQ("taskClassId", taskClass.Text("taskClassId"))).Where(c => !oldTaskIdList.Contains(c.String("taskId")));
                //curTaskObj.Set("taskClassId");
                foreach (var curtask in curTaskList)
                {
                    string updateQuery = "db.XH_DesignManage_Task.distinct('_id',{'taskId':'" + curtask.Text("taskId") + "'})";
                    dataOp.Update("XH_DesignManage_Task", updateQuery, "taskClassId=" + result3.BsonInfo.Text("taskClassId"));
                }
            }
            #endregion

        }


        #endregion



        #region wk 新增复制对象 2011.5.20
        /// <summary>
        /// 复制运营点
        /// </summary>
        /// <param name="copyObj"></param>
        /// <param name="srcObj"></param>
        /// <param name="UserId"></param>
        /// <returns></returns>
        public InvokeResult CopyOperatingPointList(BsonDocument copyObj, BsonDocument srcObj, int userId)
        {

            var pointIdsList = new Dictionary<int, int>();
            InvokeResult result = new InvokeResult() { Status = Status.Failed };
            try
            {
                var operatingQuery = copyObj.ChildBsonList("XH_DesignManage_OperatingPoint").OrderBy(c => c.Text("nodeKey"));
                foreach (var operPoint in operatingQuery)
                {
                    var uniqueKey = operPoint.Int("pointId");
                    var nodePid = operPoint.Int("nodePid");//获取父节点
                    var ParentId = 0;
                    if (nodePid != 0)
                    {
                        if (!pointIdsList.ContainsKey(nodePid)) continue;
                        ParentId = pointIdsList[nodePid];
                    }
                    #region 初始化新对象
                    var newOperatingPoint = new BsonDocument();
                    newOperatingPoint.Add("name", operPoint.Text("name"));
                    newOperatingPoint.Add("projId", srcObj.Text("projId"));
                    newOperatingPoint.Add("planId", srcObj.Text("planId"));
                    newOperatingPoint.Add("expectedEndTime", srcObj.Text("expectedEndTime"));
                    newOperatingPoint.Add("levelId", srcObj.Text("levelId"));
                    newOperatingPoint.Add("remark", srcObj.Text("remark"));
                    newOperatingPoint.Add("nodePid", ParentId);
                    #endregion
                    var queryStr = string.Format("planId={0}&nodeLevel={1}", srcObj.Text("planId"), operPoint.Text("nodeLevel"));
                    var OldObj = dataOp.FindAllByQueryStr("XH_DesignManage_OperatingPoint", queryStr).Where(c => c.Text("name") == operPoint.Text("name")).FirstOrDefault(); //查看同层几下是否有同名名称
                    if (IgnoreSameNode == true || OldObj == null)//不存在需要新增 实现增量添加
                    {

                        var operatingResult = dataOp.Insert("XH_DesignManage_OperatingPoint", newOperatingPoint);
                        if (operatingResult.Status == Status.Successful)
                        {
                            var curOperatingPoint = operatingResult.BsonInfo;
                            if (!pointIdsList.ContainsKey(uniqueKey) && curOperatingPoint != null)
                            {
                                pointIdsList.Add(uniqueKey, curOperatingPoint.Int("pointId"));
                            }
                        }

                    }
                    else
                    {
                        if (!pointIdsList.ContainsKey(uniqueKey))
                        {
                            pointIdsList.Add(uniqueKey, OldObj.Int("pointId"));
                        }
                    }
                }
                result.Status = Status.Successful;
            }
            catch (Exception ex)
            {
                result.Status = Status.Failed;
                result.Message = ex.Message;
                return result;
            }
            return result;
        }

        /// <summary>
        /// 复制分组
        /// </summary>
        /// <param name="copyObj"></param>
        /// <param name="srcObj"></param>
        /// <param name="UserId"></param>
        /// <returns></returns>
        public InvokeResult CopyTaskGroupList(BsonDocument copyObj, BsonDocument srcObj, int userId, Dictionary<int, int> ObjIdsList)
        {

            InvokeResult result = new InvokeResult() { Status = Status.Failed };
            try
            {
                var taskGroupIdList = new Dictionary<int, int>();
                var taskGroupQuery = copyObj.ChildBsonList("XH_DesignManage_TaskGroup").OrderBy(c => c.Text("nodeKey"));
                foreach (var taskGroup in taskGroupQuery)
                {
                    var uniqueKey = taskGroup.Int("groupId");
                    var nodePid = taskGroup.Int("nodePid");//获取父节点
                    var ParentId = 0;
                    if ( nodePid != 0)
                    {
                        if (!taskGroupIdList.ContainsKey(nodePid)) continue;
                        ParentId = taskGroupIdList[nodePid];
                    }
                    #region 初始化新对象
                    var newtaskGroup = new BsonDocument();
                    newtaskGroup.Add("name", taskGroup.Text("name"));
                    newtaskGroup.Add("planId", srcObj.Text("planId"));
                    newtaskGroup.Add("layoutRow", taskGroup.Text("layoutRow"));
                    newtaskGroup.Add("layoutCol", taskGroup.Text("layoutCol"));
                    newtaskGroup.Add("remark", taskGroup.Text("remark"));
                    newtaskGroup.Add("nodePid", ParentId);
                    #endregion
                    #region 创建组对象
                    //var OldObj = null;// taskGroupBll.GetGroup(srcObj.secdPlanId, taskGroup.nodeLevel, taskGroup.name);//查看同层几下是否有同名名称
                    //if (OldObj == null)//不存在需要新增 实现增量添加
                    //{
                    var operatingResult = dataOp.Insert("XH_DesignManage_TaskGroup", newtaskGroup);
                    if (operatingResult.Status == Status.Successful)
                    {
                        var curTaskGroup = operatingResult.BsonInfo;
                        if (!taskGroupIdList.ContainsKey(uniqueKey) && curTaskGroup != null)
                        {
                            taskGroupIdList.Add(uniqueKey, curTaskGroup.Int("groupId"));
                        }
                    }
                    #endregion
                    #region 创建任务组关联对象
                    foreach (var taskGroupItem in taskGroup.ChildBsonList("XH_DesignManage_Task"))
                    {
                        if (ObjIdsList.ContainsKey(taskGroupItem.Int("taskId")) && taskGroupIdList.ContainsKey(uniqueKey))
                        {
                            var mapGroupId = taskGroupIdList[uniqueKey];
                            var mapTaskId = ObjIdsList[taskGroupItem.Int("taskId")];
                            var existItem = dataOp.FindOneByKeyVal("XH_DesignManage_Task", "taskId", mapTaskId.ToString());
                            if (existItem != null)//不存在则添加
                            {
                                dataOp.QuickUpdate("XH_DesignManage_Task", existItem, "groupId=" + mapGroupId);
                            }

                        }
                    }
                    #endregion
                }
                result.Status = Status.Successful;
            }
            catch (Exception ex)
            {
                result.Status = Status.Failed;
                result.Message = ex.Message;
                return result;
            }
            return result;
        }


        /// <summary>
        /// 公式复制
        /// </summary>
        /// <param name="copyObj"></param>
        /// <param name="srcObj"></param>
        /// <param name="userId"></param>
        /// <param name="ObjIdsList"></param>
        /// <returns></returns>
        public InvokeResult CopyTaskFormulaList(BsonDocument copyObj, BsonDocument srcObj, int userId, Dictionary<int, int> ObjIdsList)
        {
            InvokeResult result = new InvokeResult() { Status = Status.Failed };
            try
            {
                #region 创建任务组关联对象

                var taskFormulaQuery = dataOp.FindAllByKeyValList("XH_DesignManage_TaskFormula", "taskId", ObjIdsList.Keys.Select(c => c.ToString()).ToList());
                //var taskFormulaQuery = dataOp.FindAllByKeyVal("XH_DesignManage_TaskFormula", "planId", copyObj.Text("planId"));
                foreach (var taskFormula in taskFormulaQuery)
                {
                    if (ObjIdsList.ContainsKey(taskFormula.Int("taskId")))
                    {
                        var mapTaskId = ObjIdsList[taskFormula.Int("taskId")];
                        var existItem = dataOp.FindOneByKeyVal("XH_DesignManage_TaskFormula", "taskId", mapTaskId.ToString());
                        if (IgnoreSameNode == true || existItem == null)//不存在则添加
                        {
                            var formulaParam = string.Empty;
                            var relTaskId = string.Empty;
                            FormulaObject formulaObj = new FormulaObject(taskFormula.Text("formulaParam"));
                            formulaObj.ConvertExp();
                            formulaObj.ConvertFormulaParam(ObjIdsList, ref formulaParam, ref relTaskId);

                            var taskItem = new BsonDocument();
                            taskItem.Add("formulaClass", taskFormula.Text("formulaClass"));
                            taskItem.Add("formulaParam", formulaParam);
                            taskItem.Add("taskId", mapTaskId);
                            taskItem.Add("relTaskId", relTaskId);
                            dataOp.Insert("XH_DesignManage_TaskFormula", taskItem);

                        }
                        else
                        {
                            //替换对应公式未完2011.5.20
                        }

                    }
                }

                #endregion
                result.Status = Status.Successful;
            }
            catch (Exception ex)
            {
                result.Status = Status.Failed;
                result.Message = ex.Message;
                return result;
            }
            return result;
        }


        #endregion


        #region 项目技术关系
        public InvokeResult CopyProjPlanRelation(BsonDocument copyObj, BsonDocument srcObj, int userId, Dictionary<int, int> ObjIdsList)
        {

            InvokeResult result = new InvokeResult() { Status = Status.Failed };
            try
            {
                #region 创建技术关系对象
                var taskRelationQuery = dataOp.FindAllByKeyVal("XH_DesignManage_TaskRelation", "planId", copyObj.Text("planId"));
                foreach (var projPlanRelation in taskRelationQuery)
                {
                    var preTaskId = projPlanRelation.Int("preTaskId");
                    var sucTaskId = projPlanRelation.Int("sucTaskId");
                    if (ObjIdsList.ContainsKey(preTaskId) && ObjIdsList.ContainsKey(sucTaskId))//确保对应任务存在
                    {
                        var mapPreTaskId = ObjIdsList[preTaskId];
                        var mapSucTaskId = ObjIdsList[sucTaskId];
                        var queryStr = string.Format("planId={0}&preTaskId={1}&sucTaskId={2}", srcObj.Text("planId"), mapPreTaskId, mapSucTaskId);
                        var existItem = dataOp.FindOneByQueryStr("XH_DesignManage_TaskRelation", queryStr);
                        if (existItem == null)//不存在则添加
                        {

                            var taskItem = new BsonDocument();
                            taskItem.Add("referType", projPlanRelation.Text("referType"));
                            taskItem.Add("delayCount", projPlanRelation.Text("delayCount"));
                            taskItem.Add("delayType", projPlanRelation.Text("delayType"));
                            taskItem.Add("order", projPlanRelation.Text("order"));
                            taskItem.Add("preTaskId", mapPreTaskId);
                            taskItem.Add("sucTaskId", mapSucTaskId);
                            taskItem.Add("projId", srcObj.Text("projId"));
                            taskItem.Add("remark", projPlanRelation.Text("remark"));
                            taskItem.Add("planId", srcObj.Text("planId"));
                            taskItem.Add("status", projPlanRelation.Text("status"));
                            taskItem.Add("paymentRatio", projPlanRelation.Text("paymentRatio"));
                            taskItem.Add("paymentAmount", projPlanRelation.Text("paymentAmount"));
                            dataOp.Insert("XH_DesignManage_TaskRelation", taskItem);
                        }
                        else
                        {
                            //替换对应关系
                        }

                    }
                }

                #endregion
                result.Status = Status.Successful;
            }
            catch (Exception ex)
            {
                result.Status = Status.Failed;
                result.Message = ex.Message;
                return result;
            }
            return result;
        }

        #endregion



        #region 跨项目技术关系
        public InvokeResult CopyAcrossPlanTaskRelation(BsonDocument copyObj, BsonDocument srcObj, int userId, Dictionary<int, int> ObjIdsList)
        {
           
            InvokeResult result = new InvokeResult() { Status = Status.Failed };
            try
            {
                //获取一个工程下的对应所有的任务
                var curProj = dataOp.FindOneByKeyVal("XH_DesignManage_Project", "projId", srcObj.Text("projId"));
                if (curProj == null)
                {
                    result.Status = Status.Successful;
                    result.Message = "分项对象不存在无法创建关联";
                    return result;
                }
                var projList = dataOp.FindAllByKeyVal("XH_DesignManage_Project", "engId", curProj.Text("engId")).Select(c => c.Text("projId")).ToList();
                var allTaskList = dataOp.FindAllByKeyValList("XH_DesignManage_Task", "projId", projList);
                #region 创建前置技术关系对象,需要添加已存在的二级任务Id
                var taskRelationQuery = dataOp.FindAllByKeyVal("XH_DesignManage_AcrossPlanTaskRelation", "prePlanId", copyObj.Text("planId"));
                foreach (var projPlanRelation in taskRelationQuery)
                {
                    var preTaskId = projPlanRelation.Int("preTaskId"); //一二级关联关系类型前置模板任务Id
                    var nextTaskId = projPlanRelation.Int("nextTaskId"); //一二级关联关系类型后置模板任务Id
                    if (ObjIdsList.ContainsKey(preTaskId))//确保对应任务存在
                    {
                        var mapPreTaskId = ObjIdsList[preTaskId];//当前计划对应节点Id
                        //curTemplateTaskId复制出来的对应分项里面其他计划的taskId，即 projId 下载非curplanId 计划里面对应的来自curTemplateTaskId的taskId
                        var acrossPlanTaskList = allTaskList.Where(c => c.Int("planId") != srcObj.Int("planId") && c.Int("srcTaskId") == nextTaskId && c.Int("nodeTypeId") == (int)ConcernNodeType.LevelRelate).ToList();
                        if (acrossPlanTaskList == null||acrossPlanTaskList.Count()<=0) continue;
                        foreach (var acrossPlanTask in acrossPlanTaskList)
                        {
                            var mapNextTaskId = acrossPlanTask.Int("taskId");//需要找出由nextTaskId复制出来的对应分项里面其他计划的taskId
                            var queryStr = string.Format("prePlanId={0}&&preTaskId={1}&&nextPlanId={3}&&nextTaskId={2}", srcObj.Text("planId"), mapPreTaskId, mapNextTaskId, acrossPlanTask.Int("planId"));
                            var existItem = dataOp.FindOneByQueryStr("XH_DesignManage_AcrossPlanTaskRelation", queryStr);
                            if (existItem == null)//不存在则添加
                            {
                                var taskItem = new BsonDocument();
                                taskItem.Add("referType", projPlanRelation.Text("referType"));
                                taskItem.Add("preTaskId", mapPreTaskId);
                                taskItem.Add("nextTaskId", mapNextTaskId);
                                taskItem.Add("preProjId", srcObj.Text("projId"));
                                taskItem.Add("nextProjId", acrossPlanTask.Text("projId"));
                                taskItem.Add("prePlanId", srcObj.Text("planId"));
                                taskItem.Add("nextPlanId", acrossPlanTask.Text("planId"));
                                dataOp.Insert("XH_DesignManage_AcrossPlanTaskRelation", taskItem);
                            }
                            else
                            {
                                //替换对应关系
                            }
                        }

                    }
                }
                #endregion
                #region 创建后置技术关系对象
                var nextTaskRelationQuery = dataOp.FindAllByKeyVal("XH_DesignManage_AcrossPlanTaskRelation", "nextPlanId", copyObj.Text("planId"));
                foreach (var projPlanRelation in nextTaskRelationQuery)
                {
                    var preTaskId = projPlanRelation.Int("preTaskId"); //一二级关联关系类型前置模板任务Id
                    var nextTaskId = projPlanRelation.Int("nextTaskId"); //一二级关联关系类型后置模板任务Id
                    if (ObjIdsList.ContainsKey(nextTaskId))//确保对应任务存在
                    {
                        //curTemplateTaskId复制出来的对应分项里面其他计划的taskId，即 projId 下载非curplanId 计划里面对应的来自curTemplateTaskId的taskId
                        var acrossPlanTaskList = allTaskList.Where(c => c.Int("planId") != srcObj.Int("planId") && c.Int("srcTaskId") == preTaskId && c.Int("nodeTypeId") == (int)ConcernNodeType.LevelRelate).ToList();
                        var mapNextTaskId = ObjIdsList[nextTaskId];
                        if (acrossPlanTaskList == null || acrossPlanTaskList.Count() <= 0) continue;
                        foreach (var acrossPlanTask in acrossPlanTaskList)
                        {
                            var mapPreTaskId = acrossPlanTask.Int("taskId");//需要找出由preTaskId复制出来的对应分项里面其他计划的taskId
                            var queryStr = string.Format("prePlanId={0}&&preTaskId={1}&&nextPlanId={3}&&nextTaskId={2}", srcObj.Text("planId"), mapPreTaskId, mapNextTaskId, acrossPlanTask.Int("planId"));
                            var existItem = dataOp.FindOneByQueryStr("XH_DesignManage_AcrossPlanTaskRelation", queryStr);
                            if (existItem == null)//不存在则添加
                            {
                                var taskItem = new BsonDocument();
                                taskItem.Add("referType", projPlanRelation.Text("referType"));
                                taskItem.Add("preTaskId", mapPreTaskId);
                                taskItem.Add("nextTaskId", mapNextTaskId);
                                taskItem.Add("preProjId", acrossPlanTask.Text("projId"));
                                taskItem.Add("nextProjId", srcObj.Text("projId"));
                                taskItem.Add("prePlanId", acrossPlanTask.Text("planId"));
                                taskItem.Add("nextPlanId", srcObj.Text("planId"));
                                dataOp.Insert("XH_DesignManage_AcrossPlanTaskRelation", taskItem);
                            }
                            else
                            {
                                //替换对应关系
                            }
                        }

                    }
                }
                #endregion


                result.Status = Status.Successful;
            }
            catch (Exception ex)
            {
                result.Status = Status.Failed;
                result.Message = ex.Message;
                return result;
            }
            return result;
        }
       

        

        #endregion

        /// <summary>
        /// 获取用户负责的任务列表
        /// </summary>
        /// <returns></returns>
        public List<BsonDocument> GetUserTaskList(string userId)
        {

            var taskManagerList = dataOp.FindAllByQuery("XH_DesignManage_TaskManager", Query.And(
            Query.EQ("type", ((int)TaskManagerType.TaskOwner).ToString()),
            Query.EQ("userId", userId)
           )).Select(c => (BsonValue)c.Text("taskId"));
           var   taskList = dataOp.FindAllByQuery("XH_DesignManage_Task", Query.And(Query.In("taskId", taskManagerList))).ToList();

          return taskList;
        }


      

    }
}
