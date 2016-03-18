using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Collections.Specialized;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Web;
using Yinhe.ProcessingCenter.MvcFilters;
using Yinhe.ProcessingCenter.Document;
using System.Text.RegularExpressions;
using Yinhe.ProcessingCenter.DataRule;
using Yinhe.ProcessingCenter;
using Yinhe.ProcessingCenter.Common;
using Yinhe.ProcessingCenter.DesignManage.TaskFormula;
using MongoDB.Driver.Builders;
using System.Transactions;
using Yinhe.ProcessingCenter.DesignManage;
using System.Xml.Linq;
using Yinhe.ProcessingCenter.BusinessFlow;
using MongoDB.Bson.IO;
using System.Collections;
using System.Xml;
using Yinhe.WebReference.Schdeuler;
using Yinhe.MessageSender;
using Yinhe.WebReference;
using System.Diagnostics;
using System.IO;
namespace Yinhe.ProcessingCenter
{
    /// <summary>
    /// 月计划后台管理基类
    /// </summary>
    public partial class ControllerBase : Controller
    {

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult saveMonthPlanInfo(FormCollection saveForm)
        {
            InvokeResult result = new InvokeResult();
            var queryStr = PageReq.GetForm("queryStr");
            var mPlanId = PageReq.GetFormInt("mPlanId");
            var orgId = PageReq.GetForm("orgId");
            var yearMonth = PageReq.GetForm("yearMonth");
            var curStartDate = Convert.ToDateTime(yearMonth + "-01");
            var curPlan = dataOp.FindOneByQuery("MonthlyPlan", Query.EQ("mPlanId", mPlanId.ToString()));
            var checkPlan = dataOp.FindAllByQuery("MonthlyPlan", Query.And(Query.EQ("year", curStartDate.Year.ToString()), Query.EQ("month", curStartDate.Month.ToString()),Query.EQ("orgId",orgId))).ToList();
            if (curPlan == null && checkPlan.Count > 0)
            {
                result.Message = "已经存在当前月份的计划";
                result.Status = Status.Failed;
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            var allKey = saveForm.AllKeys;
            var newBsonData = new BsonDocument().Add("planStatus", MonthlyPlanStatus.NotPublished).Add("year",curStartDate.Year.ToString()).Add("month",curStartDate.Month.ToString());
            if (curPlan.String("planStatus") != "0")
                newBsonData.Set("planStatus", curPlan.Text("planStatus"));

            foreach (var key in allKey)
            {
                if (key == "queryStr" || key == "mPlanId" || key == "yearMonth") continue;
                newBsonData.Add(key, saveForm[key]);
            }

            result = dataOp.Save("MonthlyPlan", TypeConvert.NativeQueryToQuery(queryStr), newBsonData);
            
            var order = 0;
            if (result.Status == Status.Successful && curPlan == null)
            {
                List<StorageData> storageList = new List<StorageData>();

                
                DateTime curEndDate = curStartDate.AddMonths(1);

                //计划任务
                #region 1.获取该月份1号前为开始和进行中的任务2.排除计划任务开始时间>该月最后一天


                var comId = dataOp.FindOneByKeyVal("DesignManage_Company", "orgId", orgId).String("comId");
                var engs = dataOp.FindAllByKeyVal("XH_DesignManage_Engineering", "comId", comId).ToList();
                var projs = dataOp.FindAllByKeyValList("XH_DesignManage_Project", "engId", engs.Select(t=>t.String("engId"))).ToList();
                var taskList = dataOp.FindAllByKeyValList("XH_DesignManage_Task", "projId", projs.Select(t=>t.String("projId"))).Where(t => t.SourceBson("planId").Int("isPayHiddenPlan") != 1).Where(t => t.Int("status") != (int)TaskStatus.Completed &&
                    //如果没有计划开始时间，则取计划结束时间，再次没有去掉
                    Convert.ToDateTime(
                    string.IsNullOrEmpty(t.String("curStartData")) ? (string.IsNullOrEmpty(t.String("curEndData")) ? curEndDate.ToShortDateString() : t.String("curEndData")) : t.String("curStartData")
                    ) < curEndDate
                    && t.Int("nodePid") != 0).ToList();
                var taskMangers = dataOp.FindAllByKeyValList("XH_DesignManage_TaskManager", "taskId", taskList.Select(t => t.String("taskId"))).ToList();
                if (taskList.Count > 0)
                {
                    
                    foreach (var task in taskList.OrderBy(t=>t.Int("projId")).ToList())
                    {
                        order++;
                        var engId = projs.Find(t => t.String("projId") == task.String("projId")).String("engId");
                        //var engId = dataOp.FindOneByKeyVal("XH_DesignManage_Project", "projId", task.String("projId")).String("engId");
                        var tempData = new StorageData();
                        var curTaskStatus = task.Int("status", 2);
                        var curStatusName = EnumDescription.GetFieldText((TaskStatus)task.Int("status"));

                        tempData.Name = "DetailTaskNode";
                        var owerUserId = taskMangers.Find(t=>t.String("taskId")==task.String("taskId") && t.String("type")=="1").String("userId");
                        //var owerUserId = dataOp.FindOneByQuery("XH_DesignManage_TaskManager", Query.And(
                        //    Query.EQ("taskId", task.String("taskId"))
                        //    , Query.EQ("type", "1"))).String("userId");
                        var otherPersonelList = taskMangers.Where(t => t.String("taskId") == task.String("taskId") && t.String("type") == "3").Select(t => t.String("name")).ToList();
                        //var otherPersonelList = dataOp.FindAllByQuery("XH_DesignManage_TaskManager", Query.And(
                        //    Query.EQ("taskId", task.String("taskId"))
                        //    , Query.EQ("type", "3"))).Select(t => t.String("name")).ToList();
                        var otherPersonel = "";
                        if (otherPersonelList.Count > 0)
                        {
                            foreach (var person in otherPersonelList)
                            {
                                otherPersonel += person + ",";
                            }
                        }


                        tempData.Document = new BsonDocument().Add("mPlanId", result.BsonInfo.String("mPlanId")).Add("name", task.String("name"))
                            .Add("projId", task.String("projId")).Add("engId", engId).Add("nodeType", TaskNodeType.DesignTask).Add("taskId", task.String("taskId")).Add("ownerUserId", owerUserId)
                            .Add("otherPersonel", otherPersonel).Add("curStartDate", task.String("curStartData"))
                            .Add("curEndDate", task.String("curEndData")).Add("status", "0").Add("taskStatus", "0").Add("delayStatus", "0").Add("order",order);
                        tempData.Type = StorageType.Insert;

                        storageList.Add(tempData);

                    }
                }
                # endregion
                //系统任务
                #region 1.获取该月份1号前为开始和进行中的任务2.排除计划开始时间>该月最后一天
                //根据创建人的公司来确定系统任务的选择
                var allOrgList = dataOp.FindAll("Organization").ToList();
                var curOrgObj=allOrgList.Where(c=>c.Text("orgId")==orgId).FirstOrDefault();
                if(curOrgObj!=null)
                {
                    //获取子公司列表
                    var childOrgList = allOrgList.Where(c => c.Text("nodeKey").StartsWith(curOrgObj.Text("nodeKey"))).Select(c => c.Text("orgId")).ToList();
                    var postIdList = dataOp.FindAllByKeyValList("OrgPost", "orgId", childOrgList).Select(t => t.String("postId")).Distinct().ToList();
                    var userIdList = dataOp.FindAllByKeyValList("UserOrgPost", "postId", postIdList).Select(t => t.String("userId")).Distinct().ToList();
                    var sysTaskList = dataOp.FindAllByKeyValList("SysTask", "manber", userIdList).Where(t => t.String("state") != "3" &&
                         Convert.ToDateTime(t.ShortDate("createDate")) < curEndDate).ToList();
                    if (sysTaskList.Count > 0)
                    {
                        foreach (var task in sysTaskList)
                        {
                            order++;
                            var tempData = new StorageData();

                            var owerUserId = task.String("manber");
                            tempData.Name = "DetailTaskNode";
                         
                            tempData.Document = new BsonDocument().Add("mPlanId", result.BsonInfo.String("mPlanId")).Add("name", task.String("taskTitle")).Add("ownerUserId",task.String("manber"))
                                .Add("nodeType", TaskNodeType.SystemTask).Add("systaskId", task.String("systaskId")).Add("curStartDate", task.String("createDate"))
                                .Add("curEndDate", task.String("endTime")).Add("status", "0").Add("taskStatus", "0").Add("delayStatus", "0").Add("projId", "").Add("engId", "").Add("order",order);
                            tempData.Type = StorageType.Insert;
                            storageList.Add(tempData);

                        }
                    }
                }
                #endregion
                //上个月未完成任务
                #region 获取上个月计划里面未完成的任务，复制过来，然后状态为已延迟
                var mPlanList = dataOp.FindAllByKeyVal("MonthlyPlan","orgId",orgId).Where(t=>t.Int("mPlanId")!=result.BsonInfo.Int("mPlanId")).ToList();
                BsonDocument oldmPlan = new BsonDocument(); 
                if (mPlanList.Count > 0)
                {
                    var yearToLastMonth = mPlanList.Where(t => t.Int("year") <= curStartDate.Year).Select(t => t.Int("year")).Max();
                    if (yearToLastMonth == curStartDate.Year)
                    {

                        mPlanList = mPlanList.Where(t => t.Int("year") == yearToLastMonth && t.Int("month") < curStartDate.Month).ToList();
                        if (mPlanList.Count > 0)
                        {
                            var monthToLastMonth = mPlanList.Select(t => t.Int("month")).Max();
                            oldmPlan = mPlanList.FirstOrDefault(t => t.Int("month") == monthToLastMonth);

                        }

                    }
                    else {
                        mPlanList = mPlanList.Where(t => t.Int("year") == yearToLastMonth).ToList();
                        if (mPlanList.Count > 0)
                        {
                            var monthToLastMonth = mPlanList.Select(t => t.Int("month")).Max();
                            oldmPlan = mPlanList.FirstOrDefault(t => t.Int("month") == monthToLastMonth);
 
                        }
                    }
                }

                //var mPlanList = dataOp.FindAll("MonthlyPlan").OrderByDescending(t=>Convert.ToDateTime(t.String("year")+"-"+t.String("month"))).ToList();
                //BsonDocument oldmPlan = new BsonDocument();
                //if (mPlanList.Count > 0)
                //{
                //    oldmPlan = mPlanList.FirstOrDefault(t=>(lastMonth - Convert.ToDateTime(t.String("year")+"-"+t.String("month"))).Days > 0);
                //}
                if (oldmPlan != null && oldmPlan.String("mPlanId") != "")
                {
                    if (oldmPlan.Int("planStatus") != (int)MonthlyPlanStatus.Filed)
                    {
                        var planStorage = new StorageData();
                        planStorage.Name = "MonthlyPlan";
                        planStorage.Document = new BsonDocument().Add("planStatus", MonthlyPlanStatus.Filed).Add("placeEndDate", DateTime.Now);
                        planStorage.Query = Query.EQ("mPlanId", oldmPlan.String("mPlanId"));
                        planStorage.Type = StorageType.Update;

                        storageList.Add(planStorage);
                    }
                    var taskNodeList = dataOp.FindAllByKeyVal("DetailTaskNode", "mPlanId", oldmPlan.String("mPlanId")).Where(t => t.Int("nodeType") != (int)TaskNodeType.DesignTask
                        && t.Int("nodeType") != (int)TaskNodeType.SystemTask && t.Int("taskStatus") != (int)MonthlyTaskStatus.Completed && t.String("nodePid") != "0"
                        ).ToList();

                    if (taskNodeList.Count > 0)
                    {
                        foreach (var node in taskNodeList.OrderBy(t => t.Int("projId")).ThenBy(t => t.Int("professionId")).ToList())
                        {
                            var tempData = new StorageData();

                            //如果任务是自定义的
                            if (node.Int("nodeType") == (int)TaskNodeType.SelfDefinedTask)
                            {
                                if (Convert.ToDateTime(node.String("curEndDate")) > curStartDate)
                                {
                                    tempData.Name = "DetailTaskNode";
                                    tempData.Document = new BsonDocument().Add("mPlanId", result.BsonInfo.String("mPlanId")).Add("name", node.String("name")).Add("nodeType", node.String("nodeType"))
                                        .Add("projId", node.String("projId")).Add("engId", node.String("engId")).Add("ownerUserId", node.String("ownerUserId")).Add("otherPersonel", node.String("otherPersonel"))
                                        .Add("remark", node.String("remark")).Add("curStartDate", node.String("curStartDate")).Add("curEndDate", node.String("curEndDate")).Add("status", node.String("status"))
                                        .Add("cityId", node.String("cityId")).Add("professionId", node.String("professionId"))
                                        .Add("taskStatus", node.String("taskStatus")).Add("delayStatus", node.String("delayStatus")).Add("order",++order);
                                    tempData.Type = StorageType.Insert;
                                    storageList.Add(tempData);
                                }
                                else
                                {
                                    tempData.Name = "DetailTaskNode";
                                    tempData.Document = new BsonDocument().Add("mPlanId", result.BsonInfo.String("mPlanId")).Add("name", node.String("name")).Add("nodeType", TaskNodeType.LastMonthUnfinishedTask)
                                        .Add("delayStatus", TaskDelayStatus.Delayed).Add("relNodeId", node.String("taskNodeId")).Add("projId", node.String("projId"))
                                        .Add("engId", node.String("engId")).Add("ownerUserId", node.String("ownerUserId")).Add("otherPersonel", node.String("otherPersonel"))
                                        .Add("remark", node.String("remark")).Add("curStartDate", node.String("curStartDate")).Add("curEndDate", node.String("curEndDate")).Add("status", node.String("status"))
                                        .Add("cityId", node.String("cityId")).Add("professionId", node.String("professionId"))
                                        .Add("taskStatus", MonthlyTaskStatus.Delayed).Add("order", ++order);
                                    tempData.Type = StorageType.Insert;
                                    storageList.Add(tempData);
                                }
                            }
                            //如果是延迟任务中的自定义任务 延迟任务中的项目计划任务和领导分配任务已经在前面选取
                            else
                            {
                                if (string.IsNullOrEmpty(node.String("taskId")) && string.IsNullOrEmpty(node.String("systaskId")))
                                {
                                    tempData.Name = "DetailTaskNode";
                                    tempData.Document = new BsonDocument().Add("mPlanId", result.BsonInfo.String("mPlanId")).Add("name", node.String("name")).Add("nodeType", TaskNodeType.LastMonthUnfinishedTask)
                                        .Add("delayStatus", TaskDelayStatus.Delayed).Add("relNodeId", node.String("relNodeId")).Add("projId", node.String("projId"))
                                        .Add("engId", node.String("engId")).Add("ownerUserId", node.String("ownerUserId")).Add("otherPersonel", node.String("otherPersonel"))
                                        .Add("remark", node.String("remark")).Add("curStartDate", node.String("curStartDate")).Add("curEndDate", node.String("curEndDate")).Add("status", node.String("status"))
                                        .Add("cityId", node.String("cityId")).Add("professionId", node.String("professionId"))
                                        .Add("taskStatus", MonthlyTaskStatus.Delayed).Add("order",++order);
                                    tempData.Type = StorageType.Insert;
                                    storageList.Add(tempData);
                                }
                            }
                        }
                    }
                }

                #endregion
                dataOp.BatchSaveStorageData(storageList);
            }

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        public ActionResult monthPlanUpdate(string mPlanId)
        {
            InvokeResult result = new InvokeResult();
            //var mPlanId = PageReq.GetParam("mPlanId");
            if (mPlanId == "")
            {
                result.Message = "没有选择月计划";
                result.Status = Status.Failed;
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            var mPlanObj = dataOp.FindOneByKeyVal("MonthlyPlan", "mPlanId", mPlanId);
            if (mPlanObj == null)
            {
                result.Message = "不存在此月计划";
                result.Status = Status.Failed;
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            var taskNodeList = dataOp.FindAllByKeyVal("DetailTaskNode", "mPlanId", mPlanId).ToList();
            var DesignTaskNodeList = taskNodeList.Where(t=>t.Int("nodeType")==(int)TaskNodeType.DesignTask).ToList();
            var SysTaskNodeList = taskNodeList.Where(t=> t.Int("nodeType")==(int)TaskNodeType.SystemTask).ToList();
            List<StorageData> storageList = new List<StorageData>();
            if (mPlanObj.String("year") == "" || mPlanObj.String("month") == "")
            {
                result.Message = "发生未知错误，请联系管理员";
                result.Status = Status.Failed;
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            var curEndDate = Convert.ToDateTime(mPlanObj.String("year") + "-" + mPlanObj.String("month") +"-01").AddMonths(1);
            //计划任务
            
            //去除计划任务中，已经完成的任务
            var taskIdList = DesignTaskNodeList.Select(t => t.String("taskId")).ToList();
            var hitTaskList = dataOp.FindAllByKeyValList("XH_DesignManage_Task", "taskId", taskIdList).ToList();
            foreach (var task in DesignTaskNodeList)
            {
                var taskObj = hitTaskList.Find(t => t.String("taskId") == task.String("taskId"));
                if (taskObj == null)
                {
                    taskObj = new BsonDocument();
                }
                if (taskObj.Int("status") == (int)TaskStatus.Completed)
                {
                    var tempData = new StorageData();
                    tempData.Name = "DetailTaskNode";
                    tempData.Query = Query.EQ("taskNodeId", task.String("taskNodeId"));
                    tempData.Type = StorageType.Delete;

                    storageList.Add(tempData);
                }
            }
            #region 1.获取该月份1号前为开始和进行中的任务2.排除计划任务开始时间>该月最后一天
            
            var comId = dataOp.FindOneByKeyVal("DesignManage_Company", "orgId", mPlanObj.String("orgId")).String("comId");
            var engs = dataOp.FindAllByKeyVal("XH_DesignManage_Engineering", "comId", comId).ToList();
            var projs = dataOp.FindAllByKeyValList("XH_DesignManage_Project", "engId", engs.Select(t => t.String("engId"))).ToList();
            var taskList = dataOp.FindAllByKeyValList("XH_DesignManage_Task", "projId", projs.Select(t => t.String("projId"))).Where(t => t.SourceBson("planId").Int("isPayHiddenPlan") != 1).Where(t => t.Int("status") != (int)TaskStatus.Completed &&
                //如果没有计划开始时间，则取计划结束时间，再次没有去掉
                Convert.ToDateTime(
                string.IsNullOrEmpty(t.String("curStartData")) ? (string.IsNullOrEmpty(t.String("curEndData")) ? curEndDate.ToShortDateString() : t.String("curEndData")) : t.String("curStartData")
                ) < curEndDate
                && t.Int("nodePid") != 0).ToList();
            var taskManagers = dataOp.FindAllByKeyValList("XH_DesignManage_TaskManager","taskId",taskList.Select(t=>t.String("taskId"))).ToList();
            if (taskList.Count > 0)
            {
                taskList = taskList.Where(t => DesignTaskNodeList.Exists(p => t.String("taskId") == p.String("taskId")) == false).ToList();
            }


            if (taskList.Count > 0)
            {
                foreach (var task in taskList)
                {
                    //var engId = dataOp.FindOneByKeyVal("XH_DesignManage_Project", "projId", task.String("projId")).String("engId");
                    var engId = projs.Find(t => t.String("porjId") == task.String("projId")).String("engId");
                    var tempData = new StorageData();
                    var curTaskStatus = task.Int("status", 2);
                    var curStatusName = EnumDescription.GetFieldText((TaskStatus)task.Int("status"));

                    tempData.Name = "DetailTaskNode";
                    //var owerUserId = dataOp.FindOneByQuery("XH_DesignManage_TaskManager", Query.And(
                    //    Query.EQ("taskId", task.String("taskId"))
                    //    , Query.EQ("type", "1"))).String("userId");

                    //var otherPersonelList = dataOp.FindAllByQuery("XH_DesignManage_TaskManager", Query.And(
                    //    Query.EQ("taskId", task.String("taskId"))
                    //    , Query.EQ("type", "3"))).Select(t => t.String("name")).ToList();
                    var owerUserId = taskManagers.Find(t => t.String("taskId") == task.String("taskId") && t.Int("type") == 1).String("userId");
                    var otherPersonelList = taskManagers.Where(t => t.String("taskId") == task.String("taskId") && t.Int("type") == 3).Select(t => t.String("name")).ToList();
                    var otherPersonel = "";
                    if (otherPersonelList.Count > 0)
                    {
                        foreach (var person in otherPersonelList)
                        {
                            otherPersonel += person + ",";
                        }
                    }


                    tempData.Document = new BsonDocument().Add("mPlanId", mPlanObj.String("mPlanId")).Add("name", task.String("name"))
                        .Add("projId", task.String("projId")).Add("engId", engId).Add("nodeType", TaskNodeType.DesignTask).Add("taskId", task.String("taskId")).Add("ownerUserId", owerUserId)
                        .Add("otherPersonel", otherPersonel).Add("curStartDate", task.String("curStartData"))
                        .Add("curEndDate", task.String("curEndData")).Add("status", "0").Add("taskStatus", "0").Add("delayStatus", "0");
                    tempData.Type = StorageType.Insert;

                    storageList.Add(tempData);

                }
            }
            # endregion
            //系统任务
            #region 1.获取该月份1号前为开始和进行中的任务2.排除计划开始时间>该月最后一天
            //去掉已经完成的任务
            var sysIdList = SysTaskNodeList.Select(t => t.String("systaskId")).ToList();
            var hitSysTaskList = dataOp.FindAllByKeyValList("SysTask", "systaskId", sysIdList).ToList();
            foreach (var task in SysTaskNodeList)
            {
                var taskObj = hitSysTaskList.Find(t => t.String("systaskId") == task.String("systaskId"));
                if (taskObj == null)
                    taskObj = new BsonDocument();
                if (taskObj.Int("state") == 3)
                {
                    var tempData = new StorageData();
                    tempData.Name = "DetailTaskNode";
                    tempData.Query = Query.EQ("taskNodeId", task.String("taskNodeId"));
                    tempData.Type = StorageType.Delete;

                    storageList.Add(tempData);
                }
            }

            //根据创建人的公司来确定系统任务的选择
            var allOrgList = dataOp.FindAll("Organization").ToList();
            var curOrgObj = allOrgList.Where(c => c.Text("orgId") == mPlanObj.Text("orgId")).FirstOrDefault();
            if (curOrgObj != null)
            {
                //获取子公司列表
                var childOrgList = allOrgList.Where(c => c.Text("nodeKey").StartsWith(curOrgObj.Text("nodeKey"))).Select(c => c.Text("orgId")).ToList();
                var postIdList = dataOp.FindAllByKeyValList("OrgPost", "orgId", childOrgList).Select(t => t.String("postId")).Distinct().ToList();
                var userIdList = dataOp.FindAllByKeyValList("UserOrgPost", "postId", postIdList).Select(t => t.String("userId")).Distinct().ToList();
                var sysTaskList = dataOp.FindAllByKeyValList("SysTask", "manber", userIdList).Where(t => t.String("state") != "3" &&
                     Convert.ToDateTime(t.ShortDate("createDate")) < curEndDate).ToList();
                if (sysTaskList.Count > 0)
                {
                    sysTaskList = sysTaskList.Where(t => SysTaskNodeList.Exists(p => p.String("systaskId") == t.String("systaskId")) == false).ToList();
                }
                if (sysTaskList.Count > 0)
                {
                    foreach (var task in sysTaskList)
                    {
                        var tempData = new StorageData();

                        var owerUserId = task.String("manber");
                        tempData.Name = "DetailTaskNode";

                        tempData.Document = new BsonDocument().Add("mPlanId", mPlanObj.String("mPlanId")).Add("name", task.String("taskTitle")).Add("ownerUserId", task.String("manber"))
                            .Add("nodeType", TaskNodeType.SystemTask).Add("systaskId", task.String("systaskId")).Add("curStartDate", task.String("createDate"))
                            .Add("curEndDate", task.String("endTime")).Add("status", "0").Add("taskStatus", "0").Add("delayStatus", "0").Add("projId", "").Add("engId", "");
                        tempData.Type = StorageType.Insert;
                        storageList.Add(tempData);

                    }
                }
            }
            #endregion
            dataOp.BatchSaveStorageData(storageList);

            //更新序号
            var order = 0;
            var detailNodeList = dataOp.FindAllByKeyVal("DetailTaskNode", "mPlanId", mPlanId).ToList();

            storageList = new List<StorageData>();
            if (detailNodeList.Count() > 0)
            {
                var designTask = detailNodeList.Where(t => t.Int("nodeType") == 1).ToList();
                if (designTask.Count() > 0)
                {
                    foreach (var task in designTask.OrderBy(t=>t.Int("projId")).ToList())
                    {
                        var tempData = new StorageData();

                        tempData.Name = "DetailTaskNode";

                        tempData.Document = new BsonDocument().Add("order", ++order);
                        tempData.Query = Query.EQ("taskNodeId", task.String("taskNodeId"));
                        tempData.Type = StorageType.Update;
                        storageList.Add(tempData);
                    }
                }
                var sysTask = detailNodeList.Where(t => t.Int("nodeType") == 2).ToList();
                if (sysTask.Count() > 0)
                {
                    foreach (var task in sysTask)
                    {
                        var tempData = new StorageData();
                        tempData.Name = "DetailTaskNode";

                        tempData.Document = new BsonDocument().Add("order", ++order);
                        tempData.Query = Query.EQ("taskNodeId", task.String("taskNodeId"));
                        tempData.Type = StorageType.Update;
                        storageList.Add(tempData);
                    }
                }
                var selDefTask = detailNodeList.Where(t => t.Int("nodeType") == 0 || t.Int("nodeType") == 3).ToList();
                if (selDefTask.Count > 0)
                {
                    foreach (var task in selDefTask.OrderBy(t => t.Int("projId")).ThenBy(t => t.Int("professionId")).ToList())
                    {
                        if (task.Int("taskStatus") == (int)TaskStatus.Completed)
                        {
                            var tempData = new StorageData();
                            tempData.Name = "DetailTaskNode";
                            tempData.Query = Query.EQ("taskNodeId", task.String("taskNodeId"));
                            tempData.Type = StorageType.Delete;

                            storageList.Add(tempData);
                        }
                        else
                        {
                            var tempData = new StorageData();
                            tempData.Name = "DetailTaskNode";

                            tempData.Document = new BsonDocument().Add("order", ++order);
                            tempData.Query = Query.EQ("taskNodeId", task.String("taskNodeId"));
                            tempData.Type = StorageType.Update;
                            storageList.Add(tempData);
                        }
                    }
                }
            }
            dataOp.BatchSaveStorageData(storageList);
            result.Status = Status.Successful;
            result.Message = "更新成功";
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        public ActionResult ChangeOrder(string souId,string desId,string type)
        {
            //var souId = PageReq.GetParam("souId");
            //var desId = PageReq.GetParam("desId");
            //var type = PageReq.GetParam("type");
            InvokeResult result = new InvokeResult();
            if (string.IsNullOrEmpty(souId) || string.IsNullOrEmpty(desId) || string.IsNullOrEmpty(type))
            {
                result.Message = "传入参数有误";
                result.Status = Status.Failed;
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            var souObj = dataOp.FindOneByKeyVal("DetailTaskNode", "taskNodeId", souId);
            var desObj = dataOp.FindOneByKeyVal("DetailTaskNode", "taskNodeId", desId);

            if (souObj == null || desObj == null)
            {
                result.Message = "传入参数有误";
                result.Status = Status.Failed;
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }

            if (souObj.Int("nodeType") == (int)TaskNodeType.SelfDefinedTask || souObj.Int("nodeType") == (int)TaskNodeType.LastMonthUnfinishedTask)
            {
                if (desObj.Int("nodeType") != (int)TaskNodeType.SelfDefinedTask && desObj.Int("nodeType") != (int)TaskNodeType.LastMonthUnfinishedTask)
                {
                    result.Message = "任务类型不一致，请重新选择";
                    result.Status = Status.Failed;
                    return Json(TypeConvert.InvokeResultToPageJson(result));
                }
            }
            else if (souObj.Int("nodeType") != desObj.Int("nodeType"))
            {
                    result.Message = "任务类型不一致，请重新选择";
                    result.Status = Status.Failed;
                    return Json(TypeConvert.InvokeResultToPageJson(result));
            }

            result = dataOp.Move("DetailTaskNode", souId, desId, type);
            if (result.Status == Status.Successful)
                result.Message = "移动成功";
            else
                result.Message = "移动失败";
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }
    }
}
