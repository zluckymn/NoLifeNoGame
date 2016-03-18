using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using MongoDB.Bson;
using System.Web;
using System.Text.RegularExpressions;
using MongoDB.Driver.Builders;
using System.Data.OleDb;
using System.Data;
using MongoDB.Driver;
using System.IO;
using Yinhe.ProcessingCenter.DataRule;
using Yinhe.ProcessingCenter.Reports;
using Yinhe.ProcessingCenter.Document;
using System.Web.Script.Serialization;
using Yinhe.ProcessingCenter.BusinessFlow;
namespace Yinhe.ProcessingCenter
{
    /// <summary>
    /// 方案评审管理基类
    /// </summary>
    public partial class ControllerBase : Controller
    {
        #region QX设计变更PDF导出

        /// <summary>
        /// 设计变更PDF导出
        /// </summary>
        public void ProgrammeEvaluationTOPDF()
        {
            try
            {
                var proEvalId = PageReq.GetParam("proEvalId");
                var ProgrammeEvaluationObj = dataOp.FindOneByQuery("ProgrammeEvaluation", Query.EQ("proEvalId", proEvalId));
                var url = string.Format("{0}/ProgrammeEvaluation/EvaluationWorkFlowInfoTable?proEvalId={1}", SysAppConfig.PDFDomain, proEvalId);
                string pdfUrl = string.Format("{0}/Account/PDF_Login?ReturnUrl={1}", SysAppConfig.PDFDomain, url);
                string tmpName = ProgrammeEvaluationObj.Text("name") + ".pdf";
                string tmpName1 = HttpUtility.UrlEncode(tmpName, System.Text.Encoding.UTF8).Replace("+", "%20"); //主要为了解决包含非英文/数字名称的问题
                string savePath = Server.MapPath("/UploadFiles/temp");
                if (!System.IO.Directory.Exists(savePath))
                {
                    System.IO.Directory.CreateDirectory(savePath);
                }
                savePath = System.IO.Path.Combine(savePath, tmpName1);
                System.Diagnostics.Process p = System.Diagnostics.Process.Start(@"C:\wkhtmltopdf\wkhtmltopdf.exe", @"" + pdfUrl + " " + savePath);
                p.WaitForExit();
                DownloadFileZHTZ(savePath, tmpName);

            }
            catch (Exception ex)
            {

            }
        }
        #endregion
        #region 保存变更指令单
       
        #endregion

        #region 保存发起审批时间
        /// <summary>
        /// 保存变更单发起审批时间
        /// </summary>
        /// <param name="designChangeId">变更单ID</param>
        /// <returns></returns>
        public ActionResult SaveProgrammeEvaluationStartTime(int proEvalId)
        {
            InvokeResult result = new InvokeResult() { Status = Status.Successful };
            var designChange = dataOp.FindOneByQuery("ProgrammeEvaluation", Query.EQ("proEvalId", proEvalId.ToString()));
            if (BsonDocumentExtension.IsNullOrEmpty(designChange))
            {
                result.Status = Status.Failed;
                result.Message = "未找到该方案评审单";
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            var timeFormat = "yyyy-MM-dd HH:mm:ss";
            BsonDocument doc = new BsonDocument().Add("startTime", DateTime.Now.ToString(timeFormat));
            result = dataOp.Save("ProgrammeEvaluation", Query.EQ("proEvalId", designChange.Text("proEvalId")), doc);
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }
        #endregion

        #region 文件编码自动生成

        public string CreateChangeNumber(string projId, string engId)
        {
            var changNum = "FAPS-" + engId + "-" + projId + "-";
            var month = DateTime.Now.Month;
            var day = DateTime.Now.Day;
            if (month < 10)
            {
                changNum += "0" + month.ToString();
            }
            else {
                changNum += month.ToString();
            }

            if (day < 10)
            {
                changNum += "0" + day.ToString();
            }
            else
            {
                changNum += day.ToString();
            }
            var EvalList = dataOp.FindAll("ProgrammeEvaluation").Where(t => t.String("changeNum").Contains(changNum)).ToList();
            if (EvalList.Count() == 0)
            {
                changNum += "-1";
                return changNum;
            }
            else { 
                var maxOrder = EvalList.Select(t=>t.Int("order")).Max();
                var lastEval = EvalList.Where(t => t.Int("order") == maxOrder).FirstOrDefault();
                var hitNum = lastEval.String("changeNum");
                var expos = hitNum.LastIndexOf('-');
                var maxNum = hitNum.Substring(expos + 1, hitNum.Length-expos-1);
                var newNum = int.Parse(maxNum) + 1;
                changNum +="-"+ newNum.ToString();
                return changNum;
            }
        }

        #endregion

        #region 保存方案评审
        public ActionResult saveProgEval(FormCollection saveForm)
        {
            InvokeResult result = new InvokeResult();

            #region 构建数据
            string tbName = PageReq.GetForm("tbName");
            string queryStr = PageReq.GetForm("queryStr");
            string dataStr = PageReq.GetForm("dataStr");
            int saveStatus = PageReq.GetFormInt("saveStatus");//0:保存  1：提交
            List<string> filterStrList = new List<string>() { "tbName", "queryStr", "actionUserStr" ,"flowId","stepIds",
            "fileTypeId","fileObjId","tableName","keyName","keyValue","delFileRelIds","uploadFileList","fileSaveType","skipStepIds"
            };
            BsonDocument dataBson = new BsonDocument();
            var allKeys = saveForm.AllKeys.Where(i => !filterStrList.Contains(i));
            if (dataStr.Trim() == "")
            {
                foreach (var tempKey in allKeys)
                {
                    if (tempKey == "tbName" || tempKey == "queryStr" || tempKey.Contains("fileList[") || tempKey.Contains("param.")) continue;

                    dataBson.Add(tempKey, PageReq.GetForm(tempKey));
                }
            }
            else
            {
                dataBson = TypeConvert.ParamStrToBsonDocument(dataStr);
            }
            #endregion

            #region 验证参数

            string flowId = PageReq.GetForm("flowId");
            BsonDocument flowObj = dataOp.FindOneByQuery("BusFlow", Query.EQ("flowId", flowId));
            if (flowObj.IsNullOrEmpty())
            {
                result.Message = "无效的流程模板";
                result.Status = Status.Failed;
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }

            var stepList = dataOp.FindAllByKeyVal("BusFlowStep", "flowId", flowId).OrderBy(c => c.Int("stepOrder")).ToList();
            BsonDocument bootStep = stepList.Where(c => c.Int("actTypeId") == (int)FlowActionType.Launch).FirstOrDefault();
            if (saveStatus == 1 && bootStep.IsNullOrEmpty())//提交时才判断
            {
                result.Message = "该流程缺少发起步骤";
                result.Status = Status.Failed;
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            var activeStepIdList = PageReq.GetFormIntList("stepIds");
            List<int> hitEnslavedStepOrder = dataOp.FindAllByKeyVal("BusFlowStep", "enslavedStepId", bootStep.Text("stepId")).OrderBy(c => c.Int("stepOrder")).Select(c => c.Int("stepOrder")).Distinct().ToList();
            List<int> hitStepIds = stepList.Where(c => hitEnslavedStepOrder.Contains(c.Int("stepOrder"))).Select(c => c.Int("stepId")).ToList();
            if (saveStatus == 1 && activeStepIdList.Count() <= 0 && hitEnslavedStepOrder.Count() > 0)//提交时才判断
            {
                result.Status = Status.Failed;
                result.Message = "请先选定会签部门";
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            #endregion

            TableRule rule = new TableRule(tbName);

            ColumnRule columnRule = rule.ColumnRules.Where(t => t.IsPrimary == true).FirstOrDefault();
            string keyName = columnRule != null ? columnRule.Name : "";

            #region 验证重名
            string newName = PageReq.GetForm("name").Trim();
            BsonDocument curChange = dataOp.FindOneByQuery(tbName, TypeConvert.NativeQueryToQuery(queryStr));
            BsonDocument oldChange = dataOp.FindOneByQuery(tbName, Query.EQ("name", newName));
            if (!oldChange.IsNullOrEmpty() && oldChange.Int(keyName) != curChange.Int(keyName))
            {
                result.Message = "已经存在该名称的方案评审";
                result.Status = Status.Failed;
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            #endregion

            #region 保存数据
            result = dataOp.Save(tbName, queryStr != "" ? TypeConvert.NativeQueryToQuery(queryStr) : Query.Null, dataBson);
            if (result.Status == Status.Failed)
            {
                result.Message = "保存方案评审失败";
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            #endregion

            #region 文件上传
            int primaryKey = 0;

            if (!string.IsNullOrEmpty(queryStr))
            {
                var query = TypeConvert.NativeQueryToQuery(queryStr);
                var recordDoc = dataOp.FindOneByQuery(tbName, query);
                saveForm["keyValue"] = result.BsonInfo.Text(keyName);
                if (recordDoc != null)
                {
                    primaryKey = recordDoc.Int(keyName);
                }
            }

            if (primaryKey == 0)//新建
            {
                if (saveForm["tableName"] != null)
                {
                    saveForm["keyValue"] = result.BsonInfo.Text(keyName);

                }
            }
            else//编辑
            {
                #region 删除文件
                string delFileRelIds = saveForm["delFileRelIds"] != null ? saveForm["delFileRelIds"] : "";
                if (!string.IsNullOrEmpty(delFileRelIds))
                {
                    FileOperationHelper opHelper = new FileOperationHelper();
                    try
                    {
                        string[] fileArray;
                        if (delFileRelIds.Length > 0)
                        {
                            fileArray = delFileRelIds.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                            if (fileArray.Length > 0)
                            {
                                foreach (var item in fileArray)
                                {
                                    var result1 = opHelper.DeleteFileByRelId(int.Parse(item));
                                    if (result1.Status == Status.Failed)
                                    {
                                        break;
                                    }
                                }
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        result.Status = Status.Failed;
                        result.Message = ex.Message;
                        return Json(TypeConvert.InvokeResultToPageJson(result));
                    }
                }
                #endregion

                saveForm["keyValue"] = primaryKey.ToString();
            }
            result.FileInfo = SaveMultipleUploadFiles(saveForm);
            #endregion

            #region 保存审批人员
            InvokeResult tempResult = new InvokeResult();

            int proEvalId = result.BsonInfo.Int(keyName);
            BsonDocument proEvalObj = result.BsonInfo;
            PageJson json = new PageJson();
            json.AddInfo("proEvalId", proEvalId.ToString());

            var actionUserStr = PageReq.GetForm("actionUserStr");

            #region 查找方案评审流程模板关联，没有则添加
            BsonDocument proEvalFlowRel = dataOp.FindOneByQuery("ProgrammeEvaluationBusFlow", Query.EQ("proEvalId", proEvalId.ToString()));
            if (proEvalFlowRel.IsNullOrEmpty())
            {
                tempResult = dataOp.Insert("ProgrammeEvaluationBusFlow", "proEvalId=" + proEvalId.ToString() + "&flowId=" + flowId);
                if (tempResult.Status == Status.Failed)
                {
                    json.Success = false;
                    json.Message = "插入流程关联失败";
                    return Json(json);
                }
                else
                {
                    proEvalFlowRel = tempResult.BsonInfo;
                }
            }
            #endregion

            #region 初始化流程实例
            var helper = new Yinhe.ProcessingCenter.BusinessFlow.FlowInstanceHelper(dataOp);
            var flowUserHelper = new Yinhe.ProcessingCenter.BusinessFlow.FlowUserHelper(dataOp);

            //当前步骤
            BsonDocument curStep = null;
            var hasOperateRight = false;//是否可以跳转步骤
            var hasEditRight = false;//是否可以编辑表单
            var canForceComplete = false;//是否可以强制结束当前步骤
            string curAvaiableUserName = string.Empty;//当前可执行人
            BsonDocument curFlowInstance = dataOp.FindAllByQuery("BusFlowInstance",
                    Query.And(
                        Query.EQ("tableName", "ProgrammeEvaluation"),
                        Query.EQ("referFieldName", "proEvalId"),
                        Query.EQ("referFieldValue", proEvalId.ToString())
                    )
                ).OrderByDescending(i => i.Date("createDate")).FirstOrDefault();
            if (curFlowInstance.IsNullOrEmpty() == false)
            {
                //初始化流程状态
                curStep = helper.InitialExecuteCondition(flowObj.Text("flowId"), curFlowInstance.Text("flowInstanceId"), dataOp.GetCurrentUserId(), ref hasOperateRight, ref hasEditRight, ref canForceComplete, ref curAvaiableUserName);
                if (curStep == null)
                {
                    curStep = curFlowInstance.SourceBson("stepId");
                }
            }
            else
            {
                curStep = bootStep;
                //初始化流程实例
                if (flowObj != null && curStep != null)
                {
                    curFlowInstance = new BsonDocument();
                    curFlowInstance.Add("flowId", flowObj.Text("flowId"));
                    curFlowInstance.Add("stepId", curStep.Text("stepId"));
                    curFlowInstance.Add("tableName", "ProgrammeEvaluation");
                    curFlowInstance.Add("referFieldName", "proEvalId");
                    curFlowInstance.Add("referFieldValue", proEvalId);
                    curFlowInstance.Add("instanceStatus", "0");
                    curFlowInstance.Add("instanceName", proEvalObj.Text("name"));
                    tempResult = helper.CreateInstance(curFlowInstance);
                    if (tempResult.Status == Status.Successful)
                    {
                        curFlowInstance = tempResult.BsonInfo;
                    }
                    else
                    {
                        json.Success = false;
                        json.Message = "创建流程实例失败:" + tempResult.Message;
                        return Json(json);
                    }
                    helper.InitialExecuteCondition(flowObj.Text("flowId"), curFlowInstance.Text("flowInstanceId"), dataOp.GetCurrentUserId(), ref hasOperateRight, ref hasEditRight, ref canForceComplete, ref curAvaiableUserName);
                }
                if (curStep == null)
                {
                    curStep = stepList.FirstOrDefault();
                }
            }
            #endregion

            #region 保存流程实例步骤人员

            List<BsonDocument> allStepList = dataOp.FindAllByKeyVal("BusFlowStep", "flowId", flowId).ToList();  //所有步骤

            //获取可控制的会签步骤
            string curStepId = curStep.Text("stepId");

            var oldRelList = dataOp.FindAllByKeyVal("InstanceActionUser", "flowInstanceId", curFlowInstance.Text("flowInstanceId")).ToList();  //所有的审批人
            //stepId + "|Y|" + uid +"|N|"+ status + "|H|";
            var arrActionUserStrUserStr = actionUserStr.Split(new string[] { "|H|" }, StringSplitOptions.RemoveEmptyEntries);
            var storageList = new List<StorageData>();
            //不需要审批的所有步骤的id--袁辉
            var skipStepIds = PageReq.GetForm("skipStepIds");
            var flowHelper = new FlowInstanceHelper();
            foreach (var userStr in arrActionUserStrUserStr)
            {
                var arrUserStatusStr = userStr.Split(new string[] { "|N|" }, StringSplitOptions.None);
                if (arrUserStatusStr.Length <= 1)
                    continue;
                string status = arrUserStatusStr[1];//该流程步骤人员是否有效 0：有效 1：无效
                var arrUserStr = arrUserStatusStr[0].Split(new string[] { "|Y|" }, StringSplitOptions.RemoveEmptyEntries);
                var stepId = int.Parse(arrUserStr[0]);
                var curStepObj = allStepList.Where(c => c.Int("stepId") == stepId).FirstOrDefault();
                if (curStepObj == null)
                {
                    continue;
                }
                if (arrUserStr.Length <= 1)
                {
                    //如果被跳过的审批没有选择人员，则在这里进行保存
                    var oldRels = oldRelList.Where(t => t.Int("stepId") == stepId).ToList();
                    if (oldRels.Count > 0)
                    {
                        var skipStr = "1";
                        if (skipStepIds.Contains(stepId.ToString()))
                        {
                            oldRels = oldRels.Where(t => t.Int("isSkip") == 0).ToList();
                        }
                        else
                        {
                            oldRels = oldRels.Where(t => t.Int("isSkip") == 1).ToList();
                            skipStr = "0";
                        }
                        if (oldRels.Count > 0)
                        {
                            foreach (var oldRel in oldRels)
                            {
                                var tempData = new StorageData();
                                tempData.Name = "InstanceActionUser";
                                tempData.Type = StorageType.Update;
                                tempData.Query = Query.EQ("inActId", oldRel.Text("inActId"));
                                tempData.Document = new BsonDocument().Add("isSkip", skipStr);

                                storageList.Add(tempData);
                            }
                        }
                    }
                    else if(skipStepIds.Contains(stepId.ToString()))
                    {
                        var tempData = new StorageData();
                        tempData.Name = "InstanceActionUser";
                        tempData.Type = StorageType.Insert;

                        BsonDocument actionUser = new BsonDocument();
                        actionUser.Add("flowInstanceId", curFlowInstance.Text("flowInstanceId"));
                        actionUser.Add("actionConditionId", curFlowInstance.Text("flowInstanceId"));
                        actionUser.Add("userId", "");
                        actionUser.Add("stepId", stepId);
                        actionUser.Add("isSkip", "1");
                        //新增模板属性对象
                        flowHelper.CopyFlowStepProperty(actionUser, curStepObj);
                        tempData.Document = actionUser;
                        storageList.Add(tempData);
                    }
                    continue;
                }
                var userArrayIds = arrUserStr[1];
                var userIds = userArrayIds.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var userId in userIds)
                {
                    var oldRel = oldRelList.FirstOrDefault(i => i.Int("stepId") == stepId && i.Text("userId") == userId);
                    if (oldRel.IsNullOrEmpty())
                    {
                        var tempData = new StorageData();
                        tempData.Name = "InstanceActionUser";
                        tempData.Type = StorageType.Insert;

                        BsonDocument actionUser = new BsonDocument();
                        actionUser.Add("flowInstanceId", curFlowInstance.Text("flowInstanceId"));
                        actionUser.Add("actionConditionId", curFlowInstance.Text("flowInstanceId"));
                        actionUser.Add("userId", userId);
                        actionUser.Add("stepId", stepId);
                        //新增模板属性对象
                        flowHelper.CopyFlowStepProperty(actionUser, curStepObj);
                        if (curStepObj.Int("actTypeId") == 2)//如果是会签步骤
                        {
                            actionUser.Set("status", status);
                        }
                        //判断步骤是否跳过审批--袁辉
                        if (skipStepIds.Contains(stepId.ToString()))
                        {
                            actionUser.Add("isSkip", "1");
                        }
                        else
                        {
                            actionUser.Add("isSkip", "0");
                        }
                        tempData.Document = actionUser;
                        storageList.Add(tempData);
                    }
                    else
                    {
                        var tempData = new StorageData();
                        tempData.Name = "InstanceActionUser";
                        tempData.Type = StorageType.Update;
                        tempData.Query = Query.EQ("inActId", oldRel.Text("inActId"));
                        BsonDocument actionUser = new BsonDocument();
                        if (hitStepIds.Contains(stepId))
                        {
                            actionUser.Add("status", status);
                        }
                        actionUser.Add("converseRefuseStepId", "");
                        actionUser.Add("actionAvaiable", "");
                        flowHelper.CopyFlowStepProperty(actionUser, curStepObj);

                        //判断步骤是否跳过审批--袁辉
                        if (skipStepIds.Contains(stepId.ToString()))
                        {
                            actionUser.Add("isSkip", "1");
                        }
                        else
                        {
                            actionUser.Add("isSkip", "0");
                        }
                        tempData.Document = actionUser;
                        storageList.Add(tempData);
                       
                        oldRelList.Remove(oldRel);
                    }
                }
            }
            foreach (var oldRel in oldRelList)
            {
                var tempData = new StorageData();
                tempData.Name = "InstanceActionUser";
                tempData.Type = StorageType.Delete;
                tempData.Query = Query.EQ("inActId", oldRel.Text("inActId"));
                storageList.Add(tempData);
            }

            tempResult = dataOp.BatchSaveStorageData(storageList);
            if (tempResult.Status == Status.Failed)
            {
                json.Success = false;
                json.Message = "保存审批人员失败";
                return Json(json);
            }
            #endregion

            #endregion

            #region 提交时保存提交信息并跳转
            if (saveStatus == 1)//提交时直接发起
            {
                //保存发起人
                BsonDocument tempData = new BsonDocument().Add("approvalUserId", dataOp.GetCurrentUserId().ToString()).Add("instanceStatus", "0");
                tempResult = dataOp.Save("BusFlowInstance", Query.EQ("flowInstanceId", curFlowInstance.Text("flowInstanceId")), tempData);
                if (tempResult.Status == Status.Failed)
                {
                    json.Success = false;
                    json.Message = "保存发起人失败";
                    return Json(json);
                }
                //保存发起时间
                var timeFormat = "yyyy-MM-dd HH:mm:ss";
                tempData = new BsonDocument(){
                        {"startTime", DateTime.Now.ToString(timeFormat)}
                    };
                tempResult = dataOp.Save("ProgrammeEvaluation", Query.EQ("proEvalId", proEvalId.ToString()), tempData);
                if (tempResult.Status == Status.Failed)
                {
                    json.Success = false;
                    json.Message = "保存发起时间失败";
                    return Json(json);
                }
                //跳转步骤
                BsonDocument act = dataOp.FindAllByKeyVal("BusFlowAction", "type", "0").FirstOrDefault();
                tempResult = helper.ExecAction(curFlowInstance, act.Int("actId"), null, bootStep.Int("stepId"));
                if (tempResult.Status == Status.Failed)
                {
                    json.Success = false;
                    json.Message = "流程跳转失败：" + tempResult.Message;
                    return Json(json);
                }
            }
            #endregion

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }
        #endregion
    }
}
