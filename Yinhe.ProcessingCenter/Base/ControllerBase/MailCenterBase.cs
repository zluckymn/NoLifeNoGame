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
///<summary>
///后台处理中心
///</summary>
namespace Yinhe.ProcessingCenter
{
    /// <summary>
    /// 工作信函邮件后台基类
    /// </summary>
    public partial class ControllerBase : Controller
    {
        #region ZHTZ获取用户所属城市公司
        /// <summary>
        /// 获取用户所属部门,保存工作函同时保存发文人所属城市公司id
        /// </summary>
        /// <param name="userId">用户id</param>
        /// <param name="nodeLevel">部门层级</param>
        /// <returns></returns>
        public BsonDocument GetUserCom_ZHTZ(string userId)
        {
            DataOperation dataOp = new DataOperation();
            BsonDocument result = new BsonDocument();
            var userObj = dataOp.FindOneByQuery("SysUser", Query.EQ("userId", userId));
            if (userObj == null) return result;
            var userOrgPostObj = dataOp.FindOneByQuery("UserOrgPost", Query.EQ("userId", userId));
            var orgPostObj = dataOp.FindOneByQuery("OrgPost", Query.EQ("postId", userOrgPostObj.Text("postId")));
            var orgObj = dataOp.FindOneByQuery("Organization", Query.EQ("orgId", orgPostObj.Text("orgId")));
            var level = 2;//中海投资的nodelevel=1；城市公司为2
            while (orgObj.Int("nodeLevel") > level)
            {
                var pOrg = dataOp.FindOneByQuery("Organization", Query.EQ("orgId", orgObj.Text("nodePid")));
                if (pOrg.Int("nodeLevel") < orgObj.Int("nodeLevel"))
                {
                    orgObj = pOrg;
                }
                else
                {
                    break;
                }
            }
            var comList = dataOp.FindAll("DesignManage_Company");
            foreach (var com in comList)
            {
                if (orgObj.Text("name").Contains(com.Text("name").Replace("公司", string.Empty)))
                {
                    result = com;
                    break;
                }
            }
            return result;
        }
        #endregion

        #region 保存发文部门
        public void SaveSendOrgIdToMail(BsonDocument doc)
        {
            if (SysAppConfig.CustomerCode == "F8A3250F-A433-42be-9F68-803BBF01ZHHY")
            {
                var curUserId = dataOp.GetCurrentUserId().ToString();
                var curUserPost = dataOp.FindAllByQuery("UserOrgPost", Query.EQ("userId", curUserId));
                var curUserOrgPost = dataOp.FindAllByQuery("OrgPost",
                        Query.In("postId", curUserPost.Select(p => (BsonValue)p.Text("postId")))
                    );
                var curUserOrgList = dataOp.FindAllByQuery("Organization",
                        Query.In("orgId", curUserOrgPost.Select(p => (BsonValue)p.Text("orgId")))
                    );
                doc.Set("isGroup", "0");
                foreach (var org in curUserOrgList)
                {
                    if (org.Int("isGroup") == 1)
                    {
                        doc.Set("isGroup", "1");
                    }
                }
                var userOrgIdList = Session["orgIdList"] as List<int> ?? new List<int>();
                var userComList = dataOp.FindAllByQuery("DesignManage_Company",
                    Query.In("orgId", userOrgIdList.Select(p => (BsonValue)p.ToString())));
                doc.Set("comId", string.Join(",", userComList.Select(p => p.Text("comId"))));
                doc.Set("sendOrgId", string.Join(",", curUserOrgList.Select(p => p.Text("orgId"))));
            }
        }
        #endregion

        #region 设置工作函发文编号
        /// <summary>
        /// 设置工作函发文编号
        /// </summary>
        /// <param name="mail">工作函</param>
        /// <param name="sendUserId">发送用户Id</param>
        /// <returns></returns>
        public InvokeResult SetMailDocNum(BsonDocument mail, string sendUserId)
        {
            InvokeResult result = new InvokeResult() { Status = Status.Successful, BsonInfo = mail };
            if (SysAppConfig.CustomerCode == "F8A3250F-A433-42be-9F68-803BBF01ZHHY")
            {
                //保存工作函获取id后才能保存编号
                BsonDocument senderPost = dataOp.FindOneByQuery("UserOrgPost", Query.EQ("userId", sendUserId));
                string orgCode = string.Empty;
                if (senderPost != null)
                {
                    BsonDocument senderOrg = dataOp.FindOneByQuery("OrgPost", Query.EQ("postId", senderPost.String("postId")));
                    if (senderOrg != null)
                    {
                        orgCode = senderOrg.String("code");
                    }

                }
                var maxNum = 1;
                //生成编号是要进行判断，提前
                BsonDocument sortInfo = dataOp.FindOneByQuery("MailSort", Query.EQ("mailSortId", mail.String("mailSortId")));
                if (string.IsNullOrEmpty(orgCode))
                {
                    orgCode = "ZHHY";
                    var cityObj = GetUserCom_ZHTZ(sendUserId);
                    if(cityObj!=null)
                    {
                        var thisYear = mail.Date("createDate").Year.ToString();
                        var maxNumObj = dataOp.FindOneByQuery("MailMaxNumber", Query.And(
                            Query.EQ("comId", cityObj.String("comId")),
                            Query.EQ("year", thisYear),
                            Query.EQ("mailSortId", sortInfo.String("mailSortId"))
                            ));
                        orgCode += "-" + cityObj.String("shortName");

                        if (mail.String("documentNum") == "")
                        {
                            if (maxNumObj == null)
                            {
                                dataOp.Insert("MailMaxNumber", new BsonDocument().Add("comId", cityObj.String("comId")).Add("year", thisYear).Add("maxNum", maxNum.ToString()).Add("mailSortId", sortInfo.String("mailSortId")));
                            }
                            else
                            {
                                maxNum = maxNumObj.Int("maxNum") + 1;
                                dataOp.Update("MailMaxNumber", Query.EQ("numId", maxNumObj.String("numId")), new BsonDocument().Add("maxNum", maxNum.ToString()));
                            }
                        }
                        else
                        {
                            var documentNum = mail.String("documentNum");
                            var expos = documentNum.LastIndexOf(".");
                            maxNum =int.Parse(documentNum.Substring(expos + 1, 3));
                        }
                    }
                }

                

                //BsonDocument typeInfo = dataOp.FindOneByQuery("MailType", Query.EQ("mailTypeId", mail.String("mailTypeId")));
                //BsonDocument sortInfo = dataOp.FindOneByQuery("MailSort", Query.EQ("mailSortId", mail.String("mailSortId")));
                //string typeCode = string.Empty;
                string sortCode = string.Empty;
                //if (typeInfo != null)
                //{
                //    typeCode = typeInfo.String("code");
                //}
                if (sortInfo != null)
                {
                    sortCode = sortInfo.String("code");
                }
                //string mailCode = orgCode + "-" + sortCode + "-" + typeCode + "-" + mail.Date("createDate").Year + "." + mail.Int("mailId");
                string mailCode = orgCode + "-"+ sortCode + "-" + mail.Date("createDate").Year + "." + maxNum.ToString("000");
                var docNumBson = new BsonDocument().Add("documentNum", mailCode);
                result = dataOp.Save("Mail", Query.EQ("mailId", mail.String("mailId")), docNumBson);

            }
            return result;
        }
        /// <summary>
        /// 设置工作函发文编号 重载（如果工作函的发文类别（sort）改变，需要改变发文编号）
        /// </summary>
        /// <param name="mail">工作函</param>
        /// <param name="sendUserId">发送用户Id</param>
        /// <param name="isChangeSort">是否改变类别</param>
        /// <returns></returns>
        public InvokeResult SetMailDocNum(BsonDocument mail, string sendUserId,bool isChangeSort)
        {
            InvokeResult result = new InvokeResult() { Status = Status.Successful, BsonInfo = mail };
            if (SysAppConfig.CustomerCode == "F8A3250F-A433-42be-9F68-803BBF01ZHHY")
            {
                //保存工作函获取id后才能保存编号
                BsonDocument senderPost = dataOp.FindOneByQuery("UserOrgPost", Query.EQ("userId", sendUserId));
                string orgCode = string.Empty;
                if (senderPost != null)
                {
                    BsonDocument senderOrg = dataOp.FindOneByQuery("OrgPost", Query.EQ("postId", senderPost.String("postId")));
                    if (senderOrg != null)
                    {
                        orgCode = senderOrg.String("code");
                    }

                }
                var maxNum = 1;
                //生成编号是要进行判断，提前
                BsonDocument sortInfo = dataOp.FindOneByQuery("MailSort", Query.EQ("mailSortId", mail.String("mailSortId")));
                if (string.IsNullOrEmpty(orgCode))
                {
                    orgCode = "ZHHY";
                    var cityObj = GetUserCom_ZHTZ(sendUserId);
                    if (cityObj != null)
                    {
                        var thisYear = mail.Date("createDate").Year.ToString();
                        var maxNumObj = dataOp.FindOneByQuery("MailMaxNumber", Query.And(
                            Query.EQ("comId", cityObj.String("comId")),
                            Query.EQ("year", thisYear),
                            Query.EQ("mailSortId", sortInfo.String("mailSortId"))
                            ));
                        orgCode += "-" + cityObj.String("shortName");

                        if (mail.String("documentNum") == "" || isChangeSort)
                        {
                            if (maxNumObj == null)
                            {
                                dataOp.Insert("MailMaxNumber", new BsonDocument().Add("comId", cityObj.String("comId")).Add("year", thisYear).Add("maxNum", maxNum.ToString()).Add("mailSortId", sortInfo.String("mailSortId")));
                            }
                            else
                            {
                                maxNum = maxNumObj.Int("maxNum") + 1;
                                dataOp.Update("MailMaxNumber", Query.EQ("numId", maxNumObj.String("numId")), new BsonDocument().Add("maxNum", maxNum.ToString()));
                            }
                        }
                        else
                        {
                            var documentNum = mail.String("documentNum");
                            var expos = documentNum.LastIndexOf(".");
                            maxNum = int.Parse(documentNum.Substring(expos + 1, 3));
                        }
                    }
                }



                //BsonDocument typeInfo = dataOp.FindOneByQuery("MailType", Query.EQ("mailTypeId", mail.String("mailTypeId")));
                //BsonDocument sortInfo = dataOp.FindOneByQuery("MailSort", Query.EQ("mailSortId", mail.String("mailSortId")));
                //string typeCode = string.Empty;
                string sortCode = string.Empty;
                //if (typeInfo != null)
                //{
                //    typeCode = typeInfo.String("code");
                //}
                if (sortInfo != null)
                {
                    sortCode = sortInfo.String("code");
                }
                //string mailCode = orgCode + "-" + sortCode + "-" + typeCode + "-" + mail.Date("createDate").Year + "." + mail.Int("mailId");
                string mailCode = orgCode + "-" + sortCode + "-" + mail.Date("createDate").Year + "." + maxNum.ToString("000");
                var docNumBson = new BsonDocument().Add("documentNum", mailCode);
                result = dataOp.Save("Mail", Query.EQ("mailId", mail.String("mailId")), docNumBson);

            }
            return result;
        }
        #endregion

        #region 保存工作组人员名单
        /// <summary>
        /// 保存工作组人员名单
        /// </summary>
        [NonAction]
        public InvokeResult SaveMailGroupUserRelation(int groupId, string userIds)
        {
            InvokeResult result = new InvokeResult();
            BsonDocument groupObj = dataOp.FindOneByKeyVal("MailUserGroup", "mailGroupId", groupId.ToString());
            if (groupObj == null) //新的组别创建并获得id之后才保存人员关系
            {
                result.Status = Status.Failed;
                result.Message = "未能找到该工作组";
                return result;
            }
            List<string> userIdList = new List<string>();

            userIdList = userIds.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();
            if (userIdList.Count != 0)
            {
                userIdList = userIdList.Distinct().ToList();
            }

            List<BsonDocument> oldRelList = dataOp.FindAllByKeyVal("MailUserGroupDetail", "mailGroupId", groupObj.String("mailGroupId")).ToList();
            List<StorageData> userDataList = new List<StorageData>();
            foreach (var tempUserId in userIdList)
            {
                BsonDocument tempOld = oldRelList.Where(t => t.String("userId") == tempUserId).FirstOrDefault();    //旧的关联

                if (tempOld == null)    //没有旧的关联则添加
                {
                    StorageData tempData = new StorageData();
                    tempData.Name = "MailUserGroupDetail";
                    tempData.Type = StorageType.Insert;
                    tempData.Document = new BsonDocument();

                    tempData.Document.Add("mailGroupId", groupObj.String("mailGroupId"));

                    tempData.Document.Add("userId", tempUserId);
                    userDataList.Add(tempData);
                }
            }
            foreach (var tempOld in oldRelList)
            {
                if (userIdList.Contains(tempOld.String("userId")) == false) //不在传入的,则删除
                {
                    StorageData tempData = new StorageData();
                    tempData.Name = "MailUserGroupDetail";
                    tempData.Type = StorageType.Delete;
                    tempData.Query = Query.EQ("mailUserId", tempOld.String("mailUserId"));

                    userDataList.Add(tempData);
                }
            }
            try
            {
                result = dataOp.BatchSaveStorageData(userDataList);
            }
            catch (Exception ex)
            {
                result.Status = Status.Failed;
                result.Message = ex.Message;
            }

            return result;
        }
        #endregion

        #region 保存工作组
        /// <summary>
        /// 保存工作组
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SaveMailGroupAndUserRelation(FormCollection saveForm)
        {
            string userIds = PageReq.GetForm("userIds");
            string queryStr = PageReq.GetForm("queryStr");
            InvokeResult result = new InvokeResult();

            BsonDocument dataBson = new BsonDocument();
            foreach (var tempKey in saveForm.AllKeys)
            {
                if (tempKey == "tbName" || tempKey == "queryStr" || tempKey.Contains("fileList[") || tempKey.Contains("param.") || tempKey == "userIds") continue;
                dataBson.Add(tempKey, PageReq.GetForm(tempKey));
            }
            using (TransactionScope scope = new TransactionScope())
            {
                try
                {
                    result = dataOp.Save("MailUserGroup", TypeConvert.NativeQueryToQuery(queryStr), dataBson);
                    if (result.Status == Status.Successful)
                    {
                        int tempId = result.BsonInfo.Int("mailGroupId");
                        InvokeResult relationResult = SaveMailGroupUserRelation(tempId, userIds);
                        if (relationResult.Status == Status.Failed)
                        {
                            result.Status = Status.Failed;
                            result.Message = relationResult.Message;
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.Status = Status.Failed;
                    result.Message = ex.Message;
                }
                scope.Complete();
            }

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        #endregion

        #region 保存工作函以及相关信息

        /// <summary>
        /// 保存工作信函
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SaveMailAndRelations(FormCollection saveForm)
        {
            //收件人id列表
            string attentionIds = PageReq.GetForm("AttentionIdList");
            //发件人id
            string sendUserid = PageReq.GetForm("sendUserId");
            //审核人id
            string approvalId = string.Empty;
            //审核人id
            string signUserId = string.Empty;
            //抄报人id列表
            string reportUsers = string.Empty;
            //抄送人id列表
            string ccUsers = string.Empty;
            string queryStr = PageReq.GetForm("queryStr");
            //判断保存草稿或是发送 发送的话draftStatus=""
            string draftStatus = PageReq.GetParam("draft");

            //收文人群组id列表
            string attentionGroupIds = string.Empty;
            //抄送人群组id列表
            string ccGroupIds = string.Empty;
            //抄报人群组id列表
            string reportGroupIds = string.Empty;
            InvokeResult result = new InvokeResult();
            BsonDocument dataBson = new BsonDocument();
            foreach (var tempKey in saveForm.AllKeys)
            {
                //AttentionIdList:收件人id列表
                if (tempKey == "tbName" || tempKey == "queryStr" || tempKey == "AttentionIdList" || tempKey == "sendUserId") continue;
                if (tempKey == "Approval")
                {
                    approvalId = PageReq.GetForm(tempKey);//保存审核人id
                    continue;
                }
                if (tempKey == "Signer")
                {
                    signUserId = PageReq.GetForm(tempKey);//保存审核人id
                    continue;
                }
                if (tempKey == "ReportUsers")
                {
                    reportUsers = PageReq.GetForm("ReportUsers");
                    continue;
                }
                if (tempKey == "CCUsers")
                {
                    ccUsers = PageReq.GetForm("CCUsers");
                    continue;
                }
                if (tempKey == "attentionGroupIds")
                {
                    attentionGroupIds = PageReq.GetForm("attentionGroupIds");
                    continue;
                }
                if (tempKey == "ccGroupIds")
                {
                    ccGroupIds = PageReq.GetForm("ccGroupIds");
                    continue;
                }
                if (tempKey == "reportGroupIds")
                {
                    reportGroupIds = PageReq.GetForm("reportGroupIds");
                    continue;
                }
                dataBson.Add(tempKey, PageReq.GetForm(tempKey));
            }
            if (String.IsNullOrEmpty(draftStatus))
            {
                dataBson.Add("draftStatus", "0");//非草稿
                dataBson.Add("sendStatus", "1");//审核中
            }
            else
            {
                dataBson.Add("draftStatus", "1");//草稿状态
                dataBson.Add("sendStatus", "0");//发送暂时设置为0
            }

            //发文人对应城市公司id，如果不属于城市公司则id为0
            var comId = GetUserCom_ZHTZ(sendUserid).Text("comId");
            if (SysAppConfig.CustomerCode == "73345DB5-DFE5-41F8-B37E-7D83335AZHTZ")
            {
                dataBson.Set("comId", comId);
            }
            SaveSendOrgIdToMail(dataBson);


            int mailId = -1;

            using (TransactionScope scope = new TransactionScope())
            {
                //用于判断信函的类别是否改变
                bool isChangeSort = false;
                if (!string.IsNullOrEmpty(queryStr))
                {
                   var oldMail = dataOp.FindOneByQuery("Mail", TypeConvert.NativeQueryToQuery(queryStr));
                   if (oldMail == null)
                       oldMail = new BsonDocument();
                   if (oldMail.String("mailSortId") != saveForm["mailSortId"])
                       isChangeSort = true;
                }

                result = dataOp.Save("Mail", TypeConvert.NativeQueryToQuery(queryStr), dataBson);
                if (result.Status == Status.Successful)
                {
                    mailId = result.BsonInfo.Int("mailId");
                }
                //增加档案编号保存
                if (result.Status == Status.Successful)
                {
                    result = SetMailDocNum(result.BsonInfo, sendUserid, isChangeSort);
                }
                if (result.Status == Status.Successful)
                {
                    #region 保存审核人
                    if (!String.IsNullOrEmpty(approvalId))
                    {
                        try
                        {
                            //保存审核人
                            InvokeResult appResult = SaveApprovalUser(result.BsonInfo.Int("mailId"), int.Parse(approvalId));
                        }
                        catch (ArgumentNullException ex)
                        {
                            result.Status = Status.Failed;
                            result.Message = ex.Message;
                            return Json(TypeConvert.InvokeResultToPageJson(result));
                        }
                        catch (Exception ex)
                        {
                            result.Status = Status.Failed;
                            result.Message = ex.Message;
                            return Json(TypeConvert.InvokeResultToPageJson(result));
                        }
                    }
                    #endregion
                    #region 保存会签人
                    if (!String.IsNullOrEmpty(signUserId))
                    {
                        try
                        {
                            //保存会签人
                            InvokeResult appResult = SaveSignUser(result.BsonInfo.Int("mailId"), int.Parse(signUserId));
                        }
                        catch (ArgumentNullException ex)
                        {
                            result.Status = Status.Failed;
                            result.Message = ex.Message;
                            return Json(TypeConvert.InvokeResultToPageJson(result));
                        }
                        catch (Exception ex)
                        {
                            result.Status = Status.Failed;
                            result.Message = ex.Message;
                            return Json(TypeConvert.InvokeResultToPageJson(result));
                        }
                    }
                    #endregion
                    #region 保存邮件群组关联
                    InvokeResult groupResult = new InvokeResult();
                    //groupType  1:收文人群组 2:抄送人群组 3:抄报人群组
                    groupResult = SaveMailRefGroup(mailId, attentionGroupIds, 1);
                    if (groupResult.Status == Status.Failed)
                    {
                        result.Status = Status.Failed;
                        result.Message = groupResult.Message;
                        return Json(TypeConvert.InvokeResultToPageJson(result));
                    }
                    groupResult = SaveMailRefGroup(mailId, ccGroupIds, 2);
                    if (groupResult.Status == Status.Failed)
                    {
                        result.Status = Status.Failed;
                        result.Message = groupResult.Message;
                        return Json(TypeConvert.InvokeResultToPageJson(result));
                    }
                    groupResult = SaveMailRefGroup(mailId, reportGroupIds, 3);
                    if (groupResult.Status == Status.Failed)
                    {
                        result.Status = Status.Failed;
                        result.Message = groupResult.Message;
                        return Json(TypeConvert.InvokeResultToPageJson(result));
                    }
                    #endregion

                    #region 保存邮件关联人


                    InvokeResult refUserResult = new InvokeResult();

                    refUserResult = SaveMailRefUserByType(mailId, attentionIds, attentionGroupIds, 1);//收文人
                    if (refUserResult.Status == Status.Successful)
                    {
                        refUserResult = SaveMailRefUserByType(mailId, sendUserid, string.Empty, 0);//发件人
                    }
                    if (refUserResult.Status == Status.Successful)
                    {
                        refUserResult = SaveMailRefUserByType(mailId, ccUsers, ccGroupIds, 2);//抄送人
                    }
                    if (refUserResult.Status == Status.Successful)
                    {
                        refUserResult = SaveMailRefUserByType(mailId, reportUsers, reportGroupIds, 3);//抄报人
                    }
                    if (refUserResult.Status == Status.Failed)
                    {
                        result.Status = Status.Failed;
                        result.Message = refUserResult.Message;
                        return Json(TypeConvert.InvokeResultToPageJson(result));
                    }

                    #endregion

                }
                scope.Complete();
            }

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        #endregion

        #region 保存回复的工作函
        /// <summary>
        /// 保存回复的邮件
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        public ActionResult SaveSendBack(FormCollection saveForm)
        {
            InvokeResult result = new InvokeResult();
            BsonDocument dataBson = new BsonDocument();
            string tbName = "Mail";
            string queryStr = "";

            int firstMailId = PageReq.GetParamInt("firstMailId");
            var firstMailObj = dataOp.FindOneByQuery("Mail", Query.EQ("mailId", firstMailId.ToString())); //查找第一封邮件
            foreach (var tempKey in saveForm.AllKeys)
            {
                if (tempKey == "Approval") continue;
                if (tempKey == "Signer") continue;
                if (tempKey == "title" && PageReq.GetForm(tempKey)=="")
                {
                    var newMailTitle = string.Format("{0}(回复)", firstMailObj.Text("title"));
                    //firstMailObj.Text("MailContent") + "--回复--" + PageReq.GetForm(tempKey);
                    dataBson.Add(tempKey, newMailTitle);
                    continue;}
                if (tempKey == "MailContent") //回复内容  (之前的内容+最新的内容)
                {
                    var newMailContent = string.Format("{0}：{1}<br/>--回复内容--<br/>{2}", firstMailObj.Text("title"), firstMailObj.Text("MailContent"), PageReq.GetForm(tempKey));
                        //firstMailObj.Text("MailContent") + "--回复--" + PageReq.GetForm(tempKey);
                    dataBson.Add(tempKey, newMailContent);
                    continue;
                }

                dataBson.Add(tempKey, PageReq.GetForm(tempKey));
            }
            var firstMailSendUserId = dataOp.FindOneByQuery("MailRefUser", Query.And(Query.EQ("mailId", firstMailId.ToString()), Query.EQ("userType", "0"))).Int("userId");  //第一封邮件的发送人
            var Approval = PageReq.GetForm("Approval");
            var Signer = PageReq.GetForm("Signer");
            dataBson.Add("mailSortId", firstMailObj.Text("mailSortId"));
            dataBson.Add("mailTypeId", firstMailObj.Text("mailTypeId"));
            dataBson.Add("type", "1");
            dataBson.Add("firstMailId", firstMailId.ToString());
            dataBson.Add("draftStatus", "0");
            dataBson.Add("sendStatus", "4");  //会签通过，带查看
            dataBson.Add("Page", firstMailObj.Text("Page"));

            //发文人对应城市公司id，如果不属于城市公司则id为0
            var comId = GetUserCom_ZHTZ(CurrentUserId.ToString()).Text("comId");
            if (SysAppConfig.CustomerCode == "73345DB5-DFE5-41F8-B37E-7D83335AZHTZ")
            {
                dataBson.Set("comId", comId);
            }
            SaveSendOrgIdToMail(dataBson);
            int mailId = -1;

            result = dataOp.Save(tbName, queryStr != "" ? TypeConvert.NativeQueryToQuery(queryStr) : Query.Null, dataBson); //保存邮件
            if (result.Status == Status.Successful)
            {
                mailId = result.BsonInfo.Int("mailId");

            }
            //设置邮件编号
            if (result.Status == Status.Successful)
            {
                SetMailDocNum(result.BsonInfo, dataOp.GetCurrentUserId().ToString());
            }

            if (result.Status == Status.Successful)
            {
                BsonDocument dataBson1 = new BsonDocument();
                dataBson1.Add("mailId", mailId.ToString());
                dataBson1.Add("userType", "1");
                dataBson1.Add("userId", firstMailSendUserId.ToString());
                dataBson1.Add("deleteStatus", "0");
                dataBson1.Add("readStatus", "0");
                InvokeResult result1 = dataOp.Save("MailRefUser", queryStr != "" ? TypeConvert.NativeQueryToQuery(queryStr) : Query.Null, dataBson1); //保存邮件关联人 收件人
                if (result1.Status == Status.Failed)
                {
                    result.Status = Status.Failed;
                    result.Message = result1.Message;
                    return Json(TypeConvert.InvokeResultToPageJson(result));
                }

                BsonDocument dataBson2 = new BsonDocument();
                dataBson2.Add("mailId", mailId.ToString());
                dataBson2.Add("userType", "0");
                dataBson2.Add("userId", CurrentUserId.ToString());
                dataBson2.Add("deleteStatus", "0");
                dataBson2.Add("readStatus", "0");
                InvokeResult result2 = dataOp.Save("MailRefUser", queryStr != "" ? TypeConvert.NativeQueryToQuery(queryStr) : Query.Null, dataBson2); //保存邮件关联人 发件人
                if (result2.Status == Status.Failed)
                {
                    result.Status = Status.Failed;
                    result.Message = result2.Message;
                    return Json(TypeConvert.InvokeResultToPageJson(result));
                }

                //BsonDocument dataBson3 = new BsonDocument();
                //dataBson3.Add("mailId", mailId.ToString());
                //dataBson3.Add("userId", Approval);
                //BsonDocument newApprovalUser = dataOp.FindOneByKeyVal("SysUser", "userId", Approval);
                //dataBson3.Add("name", newApprovalUser.Text("name"));
                //InvokeResult result3 = dataOp.Save("MailApprovalUser", queryStr != "" ? TypeConvert.NativeQueryToQuery(queryStr) : Query.Null, dataBson3); //保存审核人
                //if (result3.Status == Status.Failed)
                //{
                //    result.Status = Status.Failed;
                //    result.Message = result3.Message;
                //    return Json(TypeConvert.InvokeResultToPageJson(result));
                //}

                //BsonDocument dataBson4 = new BsonDocument();
                //dataBson4.Add("mailId", mailId.ToString());
                //dataBson4.Add("userId", Approval);
                //BsonDocument newSignUser = dataOp.FindOneByKeyVal("SysUser", "userId", Approval);
                //dataBson4.Add("name", newApprovalUser.Text("name"));
                //InvokeResult result4 = dataOp.Save("MailSignUser", queryStr != "" ? TypeConvert.NativeQueryToQuery(queryStr) : Query.Null, dataBson4); //保存审核人
                //if (result4.Status == Status.Failed)
                //{
                //    result.Status = Status.Failed;
                //    result.Message = result4.Message;
                //    return Json(TypeConvert.InvokeResultToPageJson(result));
                //}
            }
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }
        #endregion

        #region 保存转发工作函
        /// <summary>
        /// 保存转发的邮件
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        public ActionResult SaveReSendBack(FormCollection saveForm)
        {
            InvokeResult result = new InvokeResult();
            BsonDocument dataBson = new BsonDocument();
            string tbName = "Mail";
            string queryStr = PageReq.GetForm("queryStr");


            string attentionIds = PageReq.GetForm("AttentionIdList");
            //收件人id
            string sendUserid = CurrentUserId.ToString();
            //审核人id
            string approvalId = "";
            //会签人id
            string signUserId = "";
            //抄报人id列表
            string reportUsers = "";
            //抄送人id列表
            string ccUsers = "";
            //工作组
            //收文人群组id列表
            string attentionGroupIds = string.Empty;
            //抄送人群组id列表
            string ccGroupIds = string.Empty;
            //抄报人群组id列表
            string reportGroupIds = string.Empty;
            int firstMailId = PageReq.GetParamInt("firstMailId");
            var firstMailObj = dataOp.FindOneByQuery("Mail", Query.EQ("mailId", firstMailId.ToString())); //查找第一封邮件
            List<BsonDocument> fileList = dataOp.FindAllByQueryStr("FileRelation", "tableName=Mail&fileObjId=60&keyValue=" + firstMailId).OrderByDescending(t => DateTime.Parse(t.Text("createDate"))).ToList(); //查找第一封邮件下的附件
            foreach (var tempKey in saveForm.AllKeys)
            {
                //AttentionIdList:收件人id列表
                if (tempKey == "tbName" || tempKey == "queryStr" || tempKey == "AttentionIdList" || tempKey == "sendUserId") continue;
                if (tempKey == "Approval")
                {
                    approvalId = PageReq.GetForm(tempKey);//保存审核人id
                    continue;
                }
                if (tempKey == "Signer")
                {
                    signUserId = PageReq.GetForm(tempKey);//保存审核人id
                    continue;
                }
                if (tempKey == "ReportUsers")
                {
                    reportUsers = PageReq.GetForm("ReportUsers");
                    continue;
                }
                if (tempKey == "CCUsers")
                {
                    ccUsers = PageReq.GetForm("CCUsers");
                    continue;
                }

                if (tempKey == "MailContent") //回复内容  (之前的内容+最新的内容)
                {
                    var newMailContent = PageReq.GetForm(tempKey);
                    dataBson.Add(tempKey, newMailContent);
                    continue;
                }
                if (tempKey == "attentionGroupIds")
                {
                    attentionGroupIds = PageReq.GetForm("attentionGroupIds");
                    continue;
                }
                if (tempKey == "ccGroupIds")
                {
                    ccGroupIds = PageReq.GetForm("ccGroupIds");
                    continue;
                }
                if (tempKey == "reportGroupIds")
                {
                    reportGroupIds = PageReq.GetForm("reportGroupIds");
                    continue;
                }
                dataBson.Add(tempKey, PageReq.GetForm(tempKey));
            }


            var firstMailSendUserId = dataOp.FindOneByQuery("MailRefUser", Query.And(Query.EQ("mailId", firstMailId.ToString()), Query.EQ("userType", "0"))).Int("userId");  //第一封邮件的发送人
            var Approval = PageReq.GetForm("Approval");
            dataBson.Add("mailSortId", firstMailObj.Text("mailSortId"));
            dataBson.Add("mailTypeId", firstMailObj.Text("mailTypeId"));
            dataBson.Add("type", "2");
            dataBson.Add("firstMailId", firstMailId.ToString());
            dataBson.Add("fax", firstMailObj.Text("fax"));
            dataBson.Add("Page", firstMailObj.Text("Page"));

            //发文人对应城市公司id，如果不属于城市公司则id为0
            var comId = GetUserCom_ZHTZ(CurrentUserId.ToString()).Text("comId");
            if (SysAppConfig.CustomerCode == "73345DB5-DFE5-41F8-B37E-7D83335AZHTZ")
            {
                dataBson.Set("comId", comId);
            }
            SaveSendOrgIdToMail(dataBson);

            result = dataOp.Save(tbName, queryStr != "" ? TypeConvert.NativeQueryToQuery(queryStr) : Query.Null, dataBson); //保存邮件
            int mailId = result.BsonInfo.Int("mailId");

            if (result.Status == Status.Successful)
            {
                //设置邮件编号
                SetMailDocNum(result.BsonInfo, dataOp.GetCurrentUserId().ToString());
            }

            if (result.Status == Status.Successful)
            {

                if (result.Status == Status.Successful && !String.IsNullOrEmpty(approvalId))
                {
                    try
                    {
                        //保存审核人
                        InvokeResult appResult = SaveApprovalUser(result.BsonInfo.Int("mailId"), int.Parse(approvalId));
                    }
                    catch (ArgumentNullException ex)
                    {
                        result.Status = Status.Failed;
                        result.Message = ex.Message;
                        return Json(TypeConvert.InvokeResultToPageJson(result));
                    }
                    catch (Exception ex)
                    {
                        result.Status = Status.Failed;
                        result.Message = ex.Message;
                        return Json(TypeConvert.InvokeResultToPageJson(result));
                    }

                }
                if (result.Status == Status.Successful && !String.IsNullOrEmpty(signUserId))
                {
                    try
                    {
                        //保存会签人
                        InvokeResult appResult = SaveSignUser(result.BsonInfo.Int("mailId"), int.Parse(signUserId));
                    }
                    catch (ArgumentNullException ex)
                    {
                        result.Status = Status.Failed;
                        result.Message = ex.Message;
                        return Json(TypeConvert.InvokeResultToPageJson(result));
                    }
                    catch (Exception ex)
                    {
                        result.Status = Status.Failed;
                        result.Message = ex.Message;
                        return Json(TypeConvert.InvokeResultToPageJson(result));
                    }

                }
                if (result.Status == Status.Successful)
                {
                    InvokeResult groupResult = new InvokeResult();

                    groupResult = SaveMailRefGroup(mailId, attentionGroupIds, 1);
                    if (groupResult.Status == Status.Failed)
                    {
                        result.Status = Status.Failed;
                        result.Message = groupResult.Message;
                        return Json(TypeConvert.InvokeResultToPageJson(result));
                    }
                    //保存抄送，抄报分组
                    groupResult = SaveMailRefGroup(mailId, ccGroupIds, 2);
                    if (groupResult.Status == Status.Failed)
                    {
                        result.Status = Status.Failed;
                        result.Message = groupResult.Message;
                        return Json(TypeConvert.InvokeResultToPageJson(result));
                    }
                    groupResult = SaveMailRefGroup(mailId, reportGroupIds, 3);
                    if (groupResult.Status == Status.Failed)
                    {
                        result.Status = Status.Failed;
                        result.Message = groupResult.Message;
                        return Json(TypeConvert.InvokeResultToPageJson(result));
                    }
                }


                if (result.Status == Status.Successful)
                {
                    InvokeResult refUserResult = new InvokeResult();
                    refUserResult = SaveMailRefUserByType(mailId, attentionIds, attentionGroupIds, 1);//收文人
                    if (refUserResult.Status == Status.Successful)
                    {
                        refUserResult = SaveMailRefUserByType(mailId, sendUserid, string.Empty, 0);//发件人
                    }
                    if (refUserResult.Status == Status.Successful)
                    {
                        refUserResult = SaveMailRefUserByType(mailId, ccUsers, ccGroupIds, 2);//抄送人
                    }
                    if (refUserResult.Status == Status.Successful)
                    {
                        refUserResult = SaveMailRefUserByType(mailId, reportUsers, reportGroupIds, 3);//抄报人
                    }
                    if (refUserResult.Status == Status.Failed)
                    {
                        result.Status = Status.Failed;
                        result.Message = refUserResult.Message;
                        return Json(TypeConvert.InvokeResultToPageJson(result));
                    }

                }

                foreach (var file in fileList)
                {
                    BsonDocument newFileRelInfo = new BsonDocument();
                    newFileRelInfo.Add("fileId", file.Text("fileId"));
                    newFileRelInfo.Add("fileObjId", "60");
                    newFileRelInfo.Add("tableName", "Mail");
                    newFileRelInfo.Add("keyName", "mailId");
                    newFileRelInfo.Add("keyValue", mailId);
                    newFileRelInfo.Add("isPreDefine", "False");
                    newFileRelInfo.Add("isCover", "False");
                    newFileRelInfo.Add("version", "");
                    newFileRelInfo.Add("uploadType", "0");
                    InvokeResult Fileresult = dataOp.Insert("FileRelation", newFileRelInfo);
                    if (Fileresult.Status == Status.Failed)
                    {
                        result.Status = Status.Failed;
                        result.Message = result.Message;
                        return Json(TypeConvert.InvokeResultToPageJson(result));
                    }
                }

            }
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }
        #endregion

        #region 复制工作信函
        /// <summary>
        /// 工作信函复制
        /// </summary>
        /// <returns></returns>
        public ActionResult CopyMail()
        {
            InvokeResult result = new InvokeResult() { Status = Status.Successful };
            int oldMailId = PageReq.GetParamInt("mailId");
            var curMailObj = dataOp.FindOneByQuery("Mail", Query.EQ("mailId", oldMailId.ToString()));
            if (curMailObj == null)
            {
                result.Status = Status.Failed;
                result.Message = "复制邮件时出错:未找到原始邮件";
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            BsonDocument newMail = new BsonDocument();
            newMail.Add("mailSortId", curMailObj.Text("mailSortId"));
            newMail.Add("mailTypeId", curMailObj.Text("mailTypeId"));
            newMail.Add("title", curMailObj.Text("title"));
            newMail.Add("fax", curMailObj.Text("fax"));
            newMail.Add("Receivefax", curMailObj.Text("Receivefax"));
            newMail.Add("mailDate", curMailObj.Text("mailDate"));
            newMail.Add("sendModeId", curMailObj.Text("sendModeId"));
            newMail.Add("sendPhone", curMailObj.Text("sendPhone"));
            newMail.Add("toContent", curMailObj.Text("toContent"));
            newMail.Add("documentNum", curMailObj.Text("documentNum"));
            newMail.Add("Page", curMailObj.Text("Page"));
            newMail.Add("MailContent", curMailObj.Text("MailContent"));
            newMail.Add("draftStatus", "1");
            newMail.Add("sendStatus", "0");
            newMail.Add("type", curMailObj.Text("type"));
            newMail.Add("firstMailId", curMailObj.Text("firstMailId"));
            newMail.Add("manualAttentionNames", curMailObj.Text("manualAttentionNames"));
            //发文人对应城市公司id，如果不属于城市公司则id为0
            var comId = GetUserCom_ZHTZ(CurrentUserId.ToString()).Text("comId");
            if (SysAppConfig.CustomerCode == "73345DB5-DFE5-41F8-B37E-7D83335AZHTZ")
            {
                newMail.Set("comId", comId);
            }
            SaveSendOrgIdToMail(newMail);

            var newMailId = 0;
            using (TransactionScope tran = new TransactionScope())
            {
                result = dataOp.Save("Mail", Query.Null, newMail);
                if (result.Status == Status.Successful)
                {
                    newMailId = result.BsonInfo.Int("mailId");
                }

                if (result.Status == Status.Successful)
                {
                    //设置邮件编号
                    SetMailDocNum(result.BsonInfo, dataOp.GetCurrentUserId().ToString());
                }

                if (result.Status == Status.Successful)
                {
                    #region 保存审核人
                    var oldApprovalUser = dataOp.FindOneByQuery("MailApprovalUser", Query.EQ("mailId", oldMailId.ToString()));
                    if (oldApprovalUser != null)
                    {
                        try
                        {
                            //保存审核人
                            InvokeResult appResult = SaveApprovalUser(newMailId, oldApprovalUser.Int("userId"));
                        }
                        catch (ArgumentNullException ex)
                        {
                            result.Status = Status.Failed;
                            result.Message = ex.Message;
                            return Json(TypeConvert.InvokeResultToPageJson(result));
                        }
                        catch (Exception ex)
                        {
                            result.Status = Status.Failed;
                            result.Message = ex.Message;
                            return Json(TypeConvert.InvokeResultToPageJson(result));
                        }
                    }
                    #endregion
                    #region 保存会签人
                    var oldSignUser = dataOp.FindOneByQuery("MailSignUser", Query.EQ("mailId", oldMailId.ToString()));
                    if (oldSignUser != null)
                    {
                        try
                        {
                            //保存会签人
                            InvokeResult appResult = SaveSignUser(newMailId, oldSignUser.Int("userId"));
                        }
                        catch (ArgumentNullException ex)
                        {
                            result.Status = Status.Failed;
                            result.Message = ex.Message;
                            return Json(TypeConvert.InvokeResultToPageJson(result));
                        }
                        catch (Exception ex)
                        {
                            result.Status = Status.Failed;
                            result.Message = ex.Message;
                            return Json(TypeConvert.InvokeResultToPageJson(result));
                        }
                    }
                    #endregion
                    #region 保存邮件群组关联
                    InvokeResult groupResult = new InvokeResult();
                    //groupType  1:收文人群组 2:抄送人群组 3:抄报人群组
                    var attentionGroup = dataOp.FindAllByQuery("MailRefGroup", Query.And(Query.EQ("mailId", oldMailId.ToString()), Query.EQ("mailRefGroupType", "1")));
                    var attentionGroupIds = string.Join(",", attentionGroup.Select(p => p.Text("mailGroupId")).Distinct());
                    groupResult = SaveMailRefGroup(newMailId, attentionGroupIds, 1);
                    if (groupResult.Status == Status.Failed)
                    {
                        result.Status = Status.Failed;
                        result.Message = groupResult.Message;
                        return Json(TypeConvert.InvokeResultToPageJson(result));
                    }
                    var ccGroup = dataOp.FindAllByQuery("MailRefGroup", Query.And(Query.EQ("mailId", oldMailId.ToString()), Query.EQ("mailRefGroupType", "2")));
                    var ccGroupIds = string.Join(",", ccGroup.Select(p => p.Text("mailGroupId")).Distinct());
                    groupResult = SaveMailRefGroup(newMailId, ccGroupIds, 2);
                    if (groupResult.Status == Status.Failed)
                    {
                        result.Status = Status.Failed;
                        result.Message = groupResult.Message;
                        return Json(TypeConvert.InvokeResultToPageJson(result));
                    }
                    var reportGroup = dataOp.FindAllByQuery("MailRefGroup", Query.And(Query.EQ("mailId", oldMailId.ToString()), Query.EQ("mailRefGroupType", "3")));
                    var reportGroupIds = string.Join(",", reportGroup.Select(p => p.Text("mailGroupId")).Distinct());
                    groupResult = SaveMailRefGroup(newMailId, reportGroupIds, 3);
                    if (groupResult.Status == Status.Failed)
                    {
                        result.Status = Status.Failed;
                        result.Message = groupResult.Message;
                        return Json(TypeConvert.InvokeResultToPageJson(result));
                    }
                    #endregion
                    #region 保存邮件关联人
                    //获取所有选择的工作组成员
                    var attention = dataOp.FindAllByQuery("MailRefUser",
                        Query.And(Query.EQ("mailId", oldMailId.ToString()), Query.EQ("userType", "1")));
                    var attentionIds = string.Join(",", attention.Select(p => p.Text("userId")).Distinct());
                    var ccUsers = dataOp.FindAllByQuery("MailRefUser",
                        Query.And(Query.EQ("mailId", oldMailId.ToString()), Query.EQ("userType", "2")));
                    var ccUserIds = string.Join(",", ccUsers.Select(p => p.Text("userId")).Distinct());
                    var reportUsers = dataOp.FindAllByQuery("MailRefUser",
                        Query.And(Query.EQ("mailId", oldMailId.ToString()), Query.EQ("userType", "3")));
                    var reportUserIds = string.Join(",", reportUsers.Select(p => p.Text("userId")).Distinct());
                    var sendUser = dataOp.FindAllByQuery("MailRefUser",
                        Query.And(Query.EQ("mailId", oldMailId.ToString()), Query.EQ("userType", "0")));
                    var sendUserId = string.Join(",", sendUser.Select(p => p.Text("userId")).Distinct());
                    InvokeResult refUserResult = new InvokeResult();
                    refUserResult = SaveMailRefUserByType(newMailId, attentionIds, attentionGroupIds, 1);//收文人
                    if (refUserResult.Status == Status.Successful)
                    {
                        refUserResult = SaveMailRefUserByType(newMailId, dataOp.GetCurrentUserId().ToString(), string.Empty, 0);//发件人
                    }
                    if (refUserResult.Status == Status.Successful)
                    {
                        refUserResult = SaveMailRefUserByType(newMailId, ccUserIds, ccGroupIds, 2);//抄送人
                    }
                    if (refUserResult.Status == Status.Successful)
                    {
                        refUserResult = SaveMailRefUserByType(newMailId, reportUserIds, reportGroupIds, 3);//抄报人
                    }
                    if (refUserResult.Status == Status.Failed)
                    {
                        result.Status = Status.Failed;
                        result.Message = refUserResult.Message;
                        return Json(TypeConvert.InvokeResultToPageJson(result));
                    }
                    #endregion
                    #region 保存原有附件关联
                    List<BsonDocument> fileList = dataOp.FindAllByQueryStr("FileRelation", "tableName=Mail&fileObjId=60&keyValue=" + oldMailId).ToList();
                    foreach (var file in fileList)
                    {
                        BsonDocument newfile = new BsonDocument();
                        newfile.Add("fileId", file.Text("fileId"));
                        newfile.Add("fileObjId", "60");
                        newfile.Add("keyValue", newMailId.ToString());
                        newfile.Add("tableName", "Mail");
                        newfile.Add("keyName", "mailId");
                        newfile.Add("isPreDefine", file.Text("isPreDefine"));
                        newfile.Add("isCover", file.Text("isCover"));
                        newfile.Add("version", file.Text("version"));
                        dataOp.Insert("FileRelation", newfile);
                    }
                    #endregion
                }
                tran.Complete();
            }

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }
        #endregion

        #region 保存工作函关联人列表
        /// <summary>
        /// 通过人员类别分别保存分组外人员和组内人员
        /// </summary>
        /// <param name="mailId"></param>
        /// <param name="userIds"></param>
        /// <param name="userGroupIds"></param>
        /// <param name="userType">userType= 0：发件人1：收件人 2：抄送人 3:抄报人 4:被传阅人</param>
        /// <returns></returns>
        public InvokeResult SaveMailRefUserByType(int mailId, string userIds, string userGroupIds, int userType)
        {

            InvokeResult<string> result = GetRefGroupUsers(mailId, userGroupIds);//获取所有选择的工作组成员
            string groupUserIds = String.Empty;
            List<string> userIds_NoGroup = new List<string>();
            List<string> userIds_Group = new List<string>();
            if (result.Status == Status.Successful)
            {
                groupUserIds = result.Value;
                userIds_Group = groupUserIds.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();
            }
            userIds_NoGroup = userIds.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).Distinct().ToList();
            userIds_Group = userIds_Group.Except(userIds_NoGroup).Distinct().ToList();
            InvokeResult refUserResult = new InvokeResult();

            refUserResult = SaveMailRefUser(mailId, userIds_NoGroup, userType, false);
            if (refUserResult.Status == Status.Successful)
            {
                refUserResult = SaveMailRefUser(mailId, userIds_Group, userType, true);
            }
            return refUserResult;
        }


        /// <summary>
        /// 保存邮件关联人列表
        /// </summary>
        /// <param name="mailId"></param>
        /// <param name="userIds"></param>
        /// <param name="userType">userType= 0：发件人1：收件人 2：抄送人 3:抄报人 4:被传阅人</param>
        /// <param name="isFromGroup">是否是从群组中获取</param>
        /// <returns></returns>
        [NonAction]
        public InvokeResult SaveMailRefUser(int mailId, string userIds, int userType, bool isFromGroup)
        {
            //userType= 0：发件人1：收件人 2：抄送人 3:抄报人
            InvokeResult result = new InvokeResult();
            BsonDocument mailObj = dataOp.FindOneByKeyVal("Mail", "mailId", mailId.ToString());
            if (mailObj == null)
            {
                result.Status = Status.Failed;
                result.Message = "未能找到该工作函";
                return result;
            }
            List<string> userIdList = userIds.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();
            if (userIdList.Count != 0)
            {
                //如果是合并后的名单可能会出现重复id
                userIdList = userIdList.Distinct().ToList();
            }
            //List<BsonDocument> oldRelList = dataOp.FindAllByQuery("MailRefUser", Query.And(Query.EQ("mailId", mailObj.String("mailId")), Query.EQ("userType", userType.ToString()))).ToList();
            List<BsonDocument> oldRelList = dataOp.FindAllByQuery("MailRefUser",
                    Query.And(
                        Query.EQ("mailId", mailObj.Text("mailId")),
                        Query.EQ("userType", userType.ToString()),
                        Query.EQ("isFromGroup", isFromGroup ? "1" : "0")
                    )
                ).ToList();


            List<StorageData> userDataList = new List<StorageData>();
            foreach (var tempUserId in userIdList)
            {
                BsonDocument tempOld = oldRelList.Where(t => t.String("userId") == tempUserId).FirstOrDefault();    //旧的关联

                if (tempOld == null)    //没有旧的关联则添加
                {
                    StorageData tempData = new StorageData();
                    tempData.Name = "MailRefUser";
                    tempData.Type = StorageType.Insert;
                    tempData.Document = new BsonDocument();

                    tempData.Document.Add("mailId", mailObj.String("mailId"));
                    tempData.Document.Add("userType", userType.ToString());
                    tempData.Document.Add("userId", tempUserId);
                    tempData.Document.Add("deleteStatus", "0");
                    tempData.Document.Add("readStatus", "0");
                    tempData.Document.Add("readTime", string.Empty);
                    tempData.Document.Add("isFromGroup", isFromGroup ? "1" : "0");
                    userDataList.Add(tempData);
                }
            }
            foreach (var tempOld in oldRelList)
            {
                if (userIdList.Contains(tempOld.String("userId")) == false) //不在传入的,则删除
                {
                    StorageData tempData = new StorageData();
                    tempData.Name = "MailRefUser";
                    tempData.Type = StorageType.Delete;
                    tempData.Query = Query.EQ("mailRefUserId", tempOld.String("mailRefUserId"));

                    userDataList.Add(tempData);
                }
            }
            try
            {
                result = dataOp.BatchSaveStorageData(userDataList);
            }
            catch (Exception ex)
            {
                result.Status = Status.Failed;
                result.Message = ex.Message;
            }
            return result;
        }

        [NonAction]
        public InvokeResult SaveMailRefUser(int mailId, List<string> userIds, int userType, bool isFromGroup)
        {
            return SaveMailRefUser(mailId, string.Join(",", userIds), userType, isFromGroup);
        }


        #endregion

        #region 保存工作函关联工作组
        /// <summary>
        /// 保存邮件关联组
        /// </summary>
        /// <param name="mailId"></param>
        /// <param name="groupIds"></param>
        /// <param name="groupType"></param>
        /// <returns></returns>
        [NonAction]
        public InvokeResult SaveMailRefGroup(int mailId, string groupIds, int groupType)
        {
            //groupType  1:收件人群组  2:抄送人群组  3:抄报人群组
            InvokeResult result = new InvokeResult();
            BsonDocument dataBson = new BsonDocument();
            List<string> groupIdList = groupIds.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();
            List<BsonDocument> oldRelList = dataOp.FindAllByQuery("MailRefGroup", Query.And(Query.EQ("mailId", mailId.ToString()), Query.EQ("mailRefGroupType", groupType.ToString()))).ToList();
            List<StorageData> userDataList = new List<StorageData>();
            foreach (var group in groupIdList)
            {
                BsonDocument tempOld = oldRelList.Where(t => t.String("mailGroupId") == group).FirstOrDefault();
                if (tempOld == null)    //没有旧的关联则添加
                {
                    StorageData tempData = new StorageData();
                    tempData.Name = "MailRefGroup";
                    tempData.Type = StorageType.Insert;
                    tempData.Document = new BsonDocument();

                    tempData.Document.Add("mailGroupId", group);
                    tempData.Document.Add("mailId", mailId.ToString());
                    tempData.Document.Add("mailRefGroupType", groupType.ToString());
                    userDataList.Add(tempData);
                }
            }
            foreach (var tempOld in oldRelList)
            {
                if (groupIdList.Contains(tempOld.String("mailGroupId")) == false) //不在传入的,则删除
                {
                    StorageData tempData = new StorageData();
                    tempData.Name = "MailRefGroup";
                    tempData.Type = StorageType.Delete;
                    tempData.Query = Query.EQ("mailRefGroupId", tempOld.String("mailRefGroupId"));

                    userDataList.Add(tempData);
                }
            }
            try
            {
                result = dataOp.BatchSaveStorageData(userDataList);
            }
            catch (Exception ex)
            {
                result.Status = Status.Failed;
                result.Message = ex.Message;
            }

            return result;
        }
        #endregion

        #region 提取出组id列表对应的人员名单
        /// <summary>
        /// 提取出组id列表对应的人员名单
        /// </summary>
        /// <param name="mailId"></param>
        /// <param name="groupIds"></param>
        /// <returns></returns>
        [NonAction]
        public InvokeResult<string> GetRefGroupUsers(int mailId, string groupIds)
        {
            InvokeResult<string> result = new InvokeResult<string>() { Status = Status.Successful };
            List<string> groupIdList = new List<string>();
            try
            {
                groupIdList = groupIds.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();
                if (groupIdList.Count != 0)
                {
                    groupIdList = groupIdList.Distinct().ToList();
                }
                string userStr = string.Empty;
                List<string> tempStr = dataOp.FindAllByKeyValList("MailUserGroupDetail", "mailGroupId", groupIdList).Select(c => c.Text("userId")).Distinct().ToList();
                if (tempStr.Count != 0)
                {
                    userStr = string.Join(",", tempStr);
                }
                result.Status = Status.Successful;
                result.Value = userStr;
            }
            catch (ArgumentException ex)
            {
                result.Status = Status.Failed;
                result.Message = ex.Message;
            }
            return result;
        }
        #endregion

        #region 保存审核人
        /// <summary>
        /// 单独保存工作信函审核人
        /// </summary>
        /// <param name="mailId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        [NonAction]
        public InvokeResult SaveApprovalUser(int mailId, int userId)
        {
            /*审核人单选*/
            InvokeResult result = new InvokeResult();
            BsonDocument curApproval = dataOp.FindOneByQuery("MailApprovalUser", Query.And(Query.EQ("mailId", mailId.ToString())));
            if (curApproval == null)
            {
                try
                {
                    BsonDocument newApproval = new BsonDocument();
                    newApproval.Add("mailId", mailId.ToString());
                    newApproval.Add("userId", userId.ToString());
                    BsonDocument newApprovalUser = dataOp.FindOneByKeyVal("SysUser", "userId", userId.ToString());
                    newApproval.Add("name", newApprovalUser.Text("name"));
                    result = dataOp.Save("MailApprovalUser", Query.Null, newApproval);
                }
                catch (Exception ex)
                {
                    result.Status = Status.Failed;
                    result.Message = ex.Message;
                }

                return result;
            }
            if (curApproval.Int("userId") == userId)
            {
                result.Status = Status.Successful;
                return result;
            }
            else
            {
                try
                {
                    BsonDocument newApproval = new BsonDocument();
                    newApproval.Add("mailId", mailId.ToString());
                    newApproval.Add("userId", userId.ToString());
                    BsonDocument newApprovalUser = dataOp.FindOneByKeyVal("SysUser", "userId", userId.ToString());
                    newApproval.Add("name", newApprovalUser.Text("name"));
                    result = dataOp.Save("MailApprovalUser", Query.EQ("mailApprovalUserId", curApproval.Text("mailApprovalUserId")), newApproval);
                }
                catch (Exception ex)
                {
                    result.Status = Status.Failed;
                    result.Message = ex.Message;
                }

                return result;
            }
        }
        #endregion

        #region 保存会签人
        /// <summary>
        /// 单独保存工作信函会签人
        /// </summary>
        /// <param name="mailId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        [NonAction]
        public InvokeResult SaveSignUser(int mailId, int userId)
        {
            /*会签人单选*/
            InvokeResult result = new InvokeResult();
            BsonDocument curApproval = dataOp.FindOneByQuery("MailSignUser", Query.And(Query.EQ("mailId", mailId.ToString())));
            if (curApproval == null)
            {
                try
                {
                    BsonDocument newApproval = new BsonDocument();
                    newApproval.Add("mailId", mailId.ToString());
                    newApproval.Add("userId", userId.ToString());
                    BsonDocument newApprovalUser = dataOp.FindOneByKeyVal("SysUser", "userId", userId.ToString());
                    newApproval.Add("name", newApprovalUser.Text("name"));
                    result = dataOp.Save("MailSignUser", Query.Null, newApproval);
                }
                catch (Exception ex)
                {
                    result.Status = Status.Failed;
                    result.Message = ex.Message;
                }

                return result;
            }
            if (curApproval.Int("userId") == userId)
            {
                result.Status = Status.Successful;
                return result;
            }
            else
            {
                try
                {
                    BsonDocument newApproval = new BsonDocument();
                    newApproval.Add("mailId", mailId.ToString());
                    newApproval.Add("userId", userId.ToString());
                    BsonDocument newApprovalUser = dataOp.FindOneByKeyVal("SysUser", "userId", userId.ToString());
                    newApproval.Add("name", newApprovalUser.Text("name"));
                    result = dataOp.Save("MailSignUser", Query.EQ("mailSignUserId", curApproval.Text("mailSignUserId")), newApproval);
                }
                catch (Exception ex)
                {
                    result.Status = Status.Failed;
                    result.Message = ex.Message;
                }

                return result;
            }
        }
        #endregion

        #region 保存工作信函通过审核或会签时间
        /// <summary>
        /// 保存工作信函通过审核或会签时间
        /// </summary>
        /// <returns></returns>
        public ActionResult SaveMailTime()
        {
            InvokeResult result = new InvokeResult();
            var mailId = PageReq.GetParam("mailId");
            var type = PageReq.GetParam("type");//type="approval" or "sign"

            var mailObj = dataOp.FindOneByQuery("Mail", Query.EQ("mailId", mailId));
            if (mailObj == null)
            {
                result.Status = Status.Failed;
                result.Message = "未找到工作函";
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            var tbName = "Mail";
            var queryStr = "db.Mail.distinct('_id',{'mailId':'" + mailId + "'})";
            var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            BsonDocument dataBson = new BsonDocument();
            switch (type)
            {
                case "approval":
                    dataBson.Add("passApprovalDate", time); break;
                case "sign":
                    dataBson.Add("passSignDate", time); break;
                default: break;
            }
            result = dataOp.Save(tbName, Query.EQ("mailId", mailId), dataBson);
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }
        #endregion

        #region 保存工作函传阅人
        public ActionResult SaveMailCirculate()
        {
            var mailId = PageReq.GetForm("mailId");
            var userIds = PageReq.GetForm("userIds");
            //userType= 0：发件人1：收件人 2：抄送人 3:抄报人 4:被传阅人
            PageJson json = new PageJson();
            BsonDocument mailObj = dataOp.FindOneByKeyVal("Mail", "mailId", mailId.ToString());
            if (mailObj.IsNullOrEmpty())
            {
                json.Success = false;
                json.Message = "未能找到该工作函";
                return Json(json);
            }
            List<string> userIdList = userIds.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();
            if (userIdList.Count != 0)
            {
                userIdList = userIdList.Distinct().ToList();
            }
            List<BsonDocument> oldRelList = dataOp.FindAllByQuery("MailRefUser",
                    Query.And(
                        Query.EQ("mailId", mailObj.Text("mailId")),
                        Query.EQ("userType", "4")
                    )
                ).ToList();

            List<StorageData> userDataList = new List<StorageData>();
            foreach (var tempUserId in userIdList)
            {
                BsonDocument tempOld = oldRelList.Where(t => t.String("userId") == tempUserId).FirstOrDefault();    //旧的关联

                if (tempOld.IsNullOrEmpty())    //没有旧的关联则添加
                {
                    StorageData tempData = new StorageData();
                    tempData.Name = "MailRefUser";
                    tempData.Type = StorageType.Insert;
                    tempData.Document = new BsonDocument();

                    tempData.Document.Add("mailId", mailObj.String("mailId"));
                    tempData.Document.Add("userType", "4");
                    tempData.Document.Add("userId", tempUserId);
                    tempData.Document.Add("deleteStatus", "0");
                    tempData.Document.Add("readStatus", "0");
                    tempData.Document.Add("readTime", string.Empty);
                    tempData.Document.Add("isFromGroup", "0");
                    userDataList.Add(tempData);
                }
            }
            var result = dataOp.BatchSaveStorageData(userDataList);
            if (result.Status == Status.Failed)
            {
                json.Success = false;
                json.Message = result.Message;
                return Json(json);
            }
            json.Success = true;
            return Json(json);
        }
        #endregion

        #region 工作函PDF导出

        /// <summary>
        /// PDF导出
        /// </summary>
        public void MailTOPDF()
        {
            try
            {
                var mailId = PageReq.GetParam("mailId");
                var mailObj = dataOp.FindOneByQuery("Mail", Query.EQ("mailId", mailId));
                var url = string.Format("{0}/MailCenter/MailPDF?mailId={1}", SysAppConfig.PDFDomain, mailId);
                string pdfUrl = string.Format("{0}/Account/PDF_Login?ReturnUrl={1}", SysAppConfig.PDFDomain, url);
                string tmpName = mailObj.Text("title").CutStr(8, "") + ".pdf";
                string tmpName1 = HttpUtility.UrlEncode(tmpName, System.Text.Encoding.UTF8).Replace("+", "%20"); //主要为了解决包含非英文/数字名称的问题
                string savePath = Server.MapPath("/UploadFiles/temp");
                if (!System.IO.Directory.Exists(savePath))
                {
                    System.IO.Directory.CreateDirectory(savePath);
                }
                savePath = System.IO.Path.Combine(savePath, tmpName1);
                Process p = System.Diagnostics.Process.Start(@"C:\wkhtmltopdf\wkhtmltopdf.exe", @"" + pdfUrl + " " + savePath);
                p.WaitForExit();
                DownloadFileZHTZ(savePath, tmpName);


            }
            catch (Exception ex)
            {

            }


        }
         /// <summary>
        ///不登陆 PDF导出
        /// </summary>
        public void MailTOPDFNoLogin()
        {
            try
            {
                var mailId = PageReq.GetParam("mailId");
                var mailObj = dataOp.FindOneByQuery("Mail", Query.EQ("mailId", mailId));
                var url = string.Format("{0}/MailCenter/MailPDF?mailId={1}", SysAppConfig.PDFDomain, mailId);
                string tmpName = mailObj.Text("title").CutStr(8, "") + ".pdf";
                string tmpName1 = HttpUtility.UrlEncode(tmpName, System.Text.Encoding.UTF8).Replace("+", "%20"); //主要为了解决包含非英文/数字名称的问题
                string savePath = Server.MapPath("/UploadFiles/temp");
                if (!System.IO.Directory.Exists(savePath))
                {
                    System.IO.Directory.CreateDirectory(savePath);
                }
                savePath = System.IO.Path.Combine(savePath, tmpName1);
                Process p = System.Diagnostics.Process.Start(@"C:\wkhtmltopdf\wkhtmltopdf.exe", @"" + url + " " + savePath);
                p.WaitForExit();
                DownloadFileZHTZ(savePath, tmpName);


            }
            catch (Exception ex)
            {

            }
        }
        #endregion

        #region 任务系统的邮件提醒
        /// <summary>
        /// 根据工作信函id发送邮件给收文人、抄报和抄送人
        /// </summary>
        /// <returns></returns>
        public ActionResult SendMailForTask()
        {
            InvokeResult result = new InvokeResult() { Status = Status.Successful };
            List<string> addr = new List<string>();
            var systaskId = PageReq.GetParam("systaskId");
            var type = PageReq.GetParam("type");
            //type  1:分配任务  2：执行任务并提交  3：任务完成  4:任务驳回 
            var curTaskObj = dataOp.FindOneByKeyVal("SysTask", "systaskId", systaskId);
            if (curTaskObj == null)
            {
                curTaskObj = new BsonDocument();
                result.Status = Status.Failed;
                result.Message = "未找到任务";
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            try
            {
                //设置邮件标题
                var subject = string.Empty;
                //设置邮件内容
                StringBuilder body = new StringBuilder();
                //收件人id
                var toUserList = new List<string>();
                //通过用户名直接登录
                var loginCheckUrl = SysAppConfig.MailHostDomain + "/Account/Mail_Login?name=";
                #region 根据type获取收件人id
                if (type == "1")//任务创建者分配任务
                {
                    var DoTaskUser = dataOp.FindOneByKeyVal("SysUser", "userId", curTaskObj.Text("manber"));

                    if (DoTaskUser == null)
                    {
                        result.Message = "该邮件没有设置任务执行人";
                        result.Status = Status.Failed;
                        return Json(TypeConvert.InvokeResultToPageJson(result));
                    }
                    toUserList.Add(DoTaskUser.Text("userId"));
                    //邮件标题
                    subject = "中海宏洋ERP系统业务事项提醒通知-您有新的任务！";

                    body.Append("<p style='text-indent: 2em'>您好，这条消息来自于ERP系统提醒推送。</p>");
                    body.AppendFormat("<p style='text-indent: 2em'><strong>{0}</strong> 您收到一个新任务，请及时登录ERP系统处理，谢谢！</p>", curTaskObj.Text("taskTitle"));
                    string linkStr = SysAppConfig.MailHostDomain + "/TaskManage/Index";
                    var nameByte = Encoding.Unicode.GetBytes(DoTaskUser.Text("name"));
                    loginCheckUrl += Convert.ToBase64String(nameByte);
                    loginCheckUrl += "&ReturnUrl=" + Url.Encode(linkStr);
                    body.AppendFormat("<p style='text-indent: 2em'>链接为：<a href='{0}'>{0}</a></p>", loginCheckUrl);
                }
                else if (type == "2")//任务接收者执行了任务
                {
                    var CreTaskUser = dataOp.FindOneByKeyVal("SysUser", "userId", curTaskObj.Text("createUserId"));
                    if (CreTaskUser == null)
                    {
                        result.Message = "该任务没有创建人";
                        result.Status = Status.Failed;
                        return Json(TypeConvert.InvokeResultToPageJson(result));
                    }
                    toUserList.Add(CreTaskUser.Text("userId"));
                    subject = "中海宏洋ERP系统业务事项提醒通知-您分配出的任务已得到反馈！";

                    body.Append("<p style='text-indent: 2em'>您好，这条消息来自于ERP系统提醒推送。</p>");
                    body.AppendFormat("<p style='text-indent: 2em'><strong>{0}</strong> 您分配出的任务已得到任务执行者反馈，请及时登录ERP系统处理，谢谢！</p>", curTaskObj.Text("taskTitle"));
                    string linkStr = SysAppConfig.MailHostDomain + "/TaskManage/Index";
                    var nameByte = Encoding.Unicode.GetBytes(CreTaskUser.Text("name"));
                    loginCheckUrl += Convert.ToBase64String(nameByte);
                    loginCheckUrl += "&ReturnUrl=" + Url.Encode(linkStr);
                    body.AppendFormat("<p style='text-indent: 2em'>链接为：<a href='{0}'>{0}</a></p>", loginCheckUrl);
                }
                else if (type == "3")//任务创建者确认任务完成
                {
                    var DoTaskUser = dataOp.FindOneByKeyVal("SysUser", "userId", curTaskObj.Text("manber"));

                    if (DoTaskUser == null)
                    {
                        result.Message = "该邮件没有设置任务执行人";
                        result.Status = Status.Failed;
                        return Json(TypeConvert.InvokeResultToPageJson(result));
                    }
                    toUserList.Add(DoTaskUser.Text("userId"));
                    //邮件标题
                    subject = "中海宏洋ERP系统业务事项提醒通知-您有一条任务完成提示！";

                    body.Append("<p style='text-indent: 2em'>您好，这条消息来自于ERP系统提醒推送。</p>");
                    body.AppendFormat("<p style='text-indent: 2em'><strong>{0}</strong> 这个任务您已顺利完成，请登录ERP系统查看领导批示，谢谢！</p>", curTaskObj.Text("taskTitle"));
                    string linkStr = SysAppConfig.MailHostDomain + "/TaskManage/Index";
                    var nameByte = Encoding.Unicode.GetBytes(DoTaskUser.Text("name"));
                    loginCheckUrl += Convert.ToBase64String(nameByte);
                    loginCheckUrl += "&ReturnUrl=" + Url.Encode(linkStr);
                    body.AppendFormat("<p style='text-indent: 2em'>链接为：<a href='{0}'>{0}</a></p>", loginCheckUrl);
                }
                else if (type == "4")
                {
                    var DoTaskUser = dataOp.FindOneByKeyVal("SysUser", "userId", curTaskObj.Text("manber"));

                    if (DoTaskUser == null)
                    {
                        result.Message = "该邮件没有设置任务执行人";
                        result.Status = Status.Failed;
                        return Json(TypeConvert.InvokeResultToPageJson(result));
                    }
                    toUserList.Add(DoTaskUser.Text("userId"));
                    //邮件标题
                    subject = "中海宏洋ERP系统业务事项提醒通知-您提交的任务已被驳回！";

                    body.Append("<p style='text-indent: 2em'>您好，这条消息来自于ERP系统提醒推送。</p>");
                    body.AppendFormat("<p style='text-indent: 2em'><strong>{0}</strong> 这个任务未按照要求完成，请登录ERP系统重新执行任务，谢谢！</p>", curTaskObj.Text("taskTitle"));
                    string linkStr = SysAppConfig.MailHostDomain + "/TaskManage/Index";
                    var nameByte = Encoding.Unicode.GetBytes(DoTaskUser.Text("name"));
                    loginCheckUrl += Convert.ToBase64String(nameByte);
                    loginCheckUrl += "&ReturnUrl=" + Url.Encode(linkStr);
                    body.AppendFormat("<p style='text-indent: 2em'>链接为：<a href='{0}'>{0}</a></p>", loginCheckUrl);
                }
                #endregion


                body = body.Replace("yinhooleft", "<").Replace("yinhooright", ">");
                //设置邮件内容是否是html格式
                bool isBodyHtml = true;
                result = SendMail(toUserList, subject, body.ToString(), string.Empty, isBodyHtml);
            }
            catch (Exception ex)
            {
                result.Status = Status.Failed;
                result.Message = ex.Message;
            }

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }
        #endregion

        #region ZHTZ给任务审批流程下一步人员发送邮件提醒
        public ActionResult SendMailToFlowUser()
        {
            if (SysAppConfig.CustomerCode == CustomerCode.ZHHY)
            {
                return SendMailToFlowUserZHHY();
            }
            InvokeResult result = new InvokeResult() { Status = Status.Successful };
            var flowInstanceId = PageReq.GetParam("instanceId");
            var type = PageReq.GetParam("type").Trim();//type  0:推送给发起人(暂时未用到)  1：推送给会签或审批人(有实例)
            var curAvaUserList = new List<int>();
            var helper = new Yinhe.ProcessingCenter.BusinessFlow.FlowInstanceHelper();
            if (type.Equals("1"))
            {
                var curFlowInstance = dataOp.FindOneByKeyVal("BusFlowInstance", "flowInstanceId", flowInstanceId);
                if (curFlowInstance == null)
                {
                    curFlowInstance = new BsonDocument();
                    result.Status = Status.Failed;
                    result.Message = "未能找到该流程实例";
                    return Json(TypeConvert.InvokeResultToPageJson(result));
                }
                if (curFlowInstance.Int("instanceStatus") == 1)
                {
                    result.Message = "该流程实例已审批结束，下一步为发起人";
                    return Json(TypeConvert.InvokeResultToPageJson(result));
                }
                //获取当前步骤
                var curStepObj = dataOp.FindOneByKeyVal("BusFlowStep", "stepId", curFlowInstance.Text("stepId"));
                //当前流程动作类型
                var curActTypeObj = dataOp.FindOneByKeyVal("BusFlowActionType", "actTypeId", curStepObj.Text("actTypeId"));
                var curTaskObj = dataOp.FindOneByKeyVal(curFlowInstance.Text("tableName"), curFlowInstance.Text("referFieldName"), curFlowInstance.Text("referFieldValue"));
                //获取当前流程可执行人
                curAvaUserList = helper.GetFlowInstanceAvaiableStepUser(curFlowInstance.Int("flowId"), curFlowInstance.Int("flowInstanceId"), curFlowInstance.Int("stepId"));
                if (curStepObj.Int("actTypeId") == (int)FlowActionType.Launch)
                {
                    curAvaUserList = new List<int> { curFlowInstance.Int("approvalUserId") };
                }
                //邮件主题 
                var subject = "中海投资ERP系统审批业务事项提醒通知-您有新的审批任务！";
                var body = new StringBuilder();
                //设置链接跳转地址
                string taskLinkStr = string.Format("{0}{1}{2}", SysAppConfig.MailHostDomain, "/DesignManage/NewTaskWorkFlowInfo/", curFlowInstance.Text("referFieldValue"));
                //设置邮件内容
                body.Append("<p style='text-indent: 2em'>您好，这条消息来自于ERP审批事务提醒推送。</p>");
                body.AppendFormat("<p style='text-indent: 2em'><strong>{0}</strong>项目的任务<strong>{1}</strong>当前流程流转到您<strong>{2}</strong>，请及时登录ERP系统处理，谢谢！</p>", curTaskObj.SourceBsonField("projId", "name"), curTaskObj.Text("name"), curActTypeObj.Text("name"));
                //设置邮件内容是否为html格式
                bool isBodyHtml = true;
                //异步发送邮件
                foreach (var id in curAvaUserList)
                {
                    //通过用户名直接登录
                    var loginCheckUrl = SysAppConfig.MailHostDomain + "/Account/Mail_Login?name=";
                    var userObj = dataOp.FindOneByQuery("SysUser", Query.EQ("userId", id.ToString()));
                    if (userObj == null) continue;
                    var nameByte = Encoding.Unicode.GetBytes(userObj.Text("name"));
                    loginCheckUrl += Convert.ToBase64String(nameByte);
                    loginCheckUrl += "&ReturnUrl=" + Url.Encode(taskLinkStr);
                    string recvUserStr = id.ToString();
                    StringBuilder tempBody = new StringBuilder(body.ToString());
                    //发送成功时记录的日志
                    string logInfo = string.Format("收件人：{0} 流程名称：{1} 流程ID：{2} 动作：{3}",
                        userObj.Text("name"), curFlowInstance.Text("instanceName"), curFlowInstance.Int("flowInstanceId"), curActTypeObj.Text("name"));
                    tempBody.AppendFormat("<p style='text-indent: 2em'>链接为：<a href='{0}'>{1}</a></p>", loginCheckUrl, loginCheckUrl);
                    SendMail(recvUserStr, subject, tempBody.ToString(), string.Empty, isBodyHtml, logInfo);
                }
            }
            if (type.Equals("0"))
            {
                var flowId = PageReq.GetParamInt("flowId");
                var taskId = PageReq.GetParam("taskId");
                var curTaskObj = dataOp.FindOneByQuery("XH_DesignManage_Task", Query.EQ("taskId", taskId));
                if (curTaskObj == null)
                {
                    curTaskObj = dataOp.FindOneByQuery("XH_DesignManage_Task", Query.EQ("taskId", PageReq.GetParam("taskId")));
                }
                if (curTaskObj == null)
                {
                    result.Status = Status.Failed;
                    result.Message = "未能找到相关任务";
                    return Json(TypeConvert.InvokeResultToPageJson(result));
                }
                List<BsonDocument> allStepList = dataOp.FindAllByKeyVal("BusFlowStep", "flowId", flowId.ToString()).ToList();
                var launchStep = allStepList.Where(p => p.Int("actTypeId") == (int)FlowActionType.Launch).FirstOrDefault();
                if (launchStep == null)
                {
                    result.Status = Status.Failed;
                    result.Message = "该流程未设置发起步骤";
                    return Json(TypeConvert.InvokeResultToPageJson(result));
                }
                var tempUserIds = helper.GetCurStepUser(flowId, launchStep.Int("stepId"), "XH_DesignManage_Task", "taskId", taskId);
                var subject = "中海投资ERP系统审批业务事项提醒通知-您有新的审批任务！";
                var body = new StringBuilder();
                //设置链接跳转地址
                string taskLinkStr = string.Format("{0}{1}{2}", SysAppConfig.MailHostDomain, "/DesignManage/ProjTaskInfo/", taskId);
                //设置邮件内容
                body.Append("<p style='text-indent: 2em'>您好，这条消息来自于ERP审批事务提醒推送。</p>");
                body.AppendFormat("<p style='text-indent: 2em'><strong>{0}</strong>项目的任务<strong>{1}</strong>当前流程流转到您<strong>{2}</strong>，请及时登录ERP系统处理，谢谢！</p>", curTaskObj.SourceBsonField("projId", "name"), curTaskObj.Text("name"), "发起");
                body.AppendFormat("<p style='text-indent: 2em'>链接为：<a href='{0}'>{1}</a></p>", taskLinkStr, taskLinkStr);
                //设置邮件内容是否为html格式
                bool isBodyHtml = true;
                //异步发送邮件
                //SendMail(tempUserIds, subject, body.ToString(), string.Empty, isBodyHtml);
                foreach (var id in tempUserIds)
                {
                    //通过用户名直接登录
                    var loginCheckUrl = SysAppConfig.MailHostDomain + "/Account/Mail_Login?name=";
                    var userObj = dataOp.FindOneByQuery("SysUser", Query.EQ("userId", id.ToString()));
                    if (userObj == null) continue;
                    var nameByte = Encoding.Unicode.GetBytes(userObj.Text("name"));
                    loginCheckUrl += Convert.ToBase64String(nameByte);
                    loginCheckUrl += "&ReturnUrl=" + Url.Encode(taskLinkStr);
                    var receiverIdList = new List<int>();
                    receiverIdList.Add(id);
                    StringBuilder tempBody = new StringBuilder(body.ToString());
                    tempBody.AppendFormat("<p style='text-indent: 2em'>链接为：<a href='{0}'>{1}</a></p>", loginCheckUrl, loginCheckUrl);
                    SendMail(receiverIdList, subject, tempBody.ToString(), string.Empty, isBodyHtml);
                }
            }
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }
        #endregion

        #region ZHHY给审批流程下一步人员发送邮件提醒
        public ActionResult SendMailToFlowUserZHHY()
        {
            InvokeResult result = new InvokeResult() { Status = Status.Successful };
            var flowInstanceId = PageReq.GetParamInt("instanceId");
            var type = PageReq.GetParamInt("type");// 1:正常通过驳回  2：转办
            var userIds = PageReq.GetParamIntList("userId");
            var instance = dataOp.FindOneByQuery("BusFlowInstance", Query.EQ("flowInstanceId", flowInstanceId.ToString()));
            //通过前后stepId判断是否需要发送，当前stepId与操作前preStepId一致则不发送
            var preStepId = PageReq.GetParamInt("preStepId");
            var notice=new Yinhe.ProcessingCenter.BusinessFlow.SendApprovalMailNotice();
            if (type == 1)
            {
                if(instance.Int("instanceStatus")==1 || preStepId!=instance.Int("stepId")){
                    notice.ZHHYSendNextAppNotice(flowInstanceId);
                }
            }else{
                notice.ZHHYSendTurnNotice(flowInstanceId,userIds.ToList());
            }
            
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }
        #endregion

        #region ZHTZ工作函发送邮件提醒

        /// <summary>
        /// 根据工作信函id发送邮件给收文人、抄报和抄送人
        /// </summary>
        /// <returns></returns>
        public ActionResult SendMailToMailUser()
        {
            if (SysAppConfig.CustomerCode == CustomerCode.ZHHY)
            {
                return SendMailToMailUserZHHY();
            }
            InvokeResult result = new InvokeResult() { Status = Status.Successful };
            List<string> addr = new List<string>();
            var mailId = PageReq.GetParam("mailId");
            var type = PageReq.GetParam("type");
            //type  1:发送给审核人  2：发送给会签人  3：发送给收文、抄报、抄送人  4:工作信函驳回提醒 5:工作信函审批通过
            var curMailObj = dataOp.FindOneByKeyVal("Mail", "mailId", mailId);
            if (curMailObj == null)
            {
                curMailObj = new BsonDocument();
                result.Status = Status.Failed;
                result.Message = "未能找到该工作信函";
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            try
            {
                //设置邮件标题
                var subject = string.Empty;
                //设置邮件内容
                StringBuilder body = new StringBuilder();
                //收件人id
                var toUserList = new List<string>();
                //通过用户名直接登录
                var loginCheckUrl = SysAppConfig.MailHostDomain + "/Account/Mail_Login?name=";
                string logInfo = string.Format("工作函ID：{0} 主题：{1}", mailId, curMailObj.Text("title"));
                #region 根据type获取收件人id
                if (type == "1")//工作信函发送后给审核人发送提醒邮件
                {
                    var approvalUser = dataOp.FindOneByKeyVal("MailApprovalUser", "mailId", mailId);

                    if (approvalUser == null)
                    {
                        result.Message = "该邮件没有设置审核人";
                        result.Status = Status.Failed;
                        return Json(TypeConvert.InvokeResultToPageJson(result));
                    }
                    var approvalUserObj = dataOp.FindOneByQuery("SysUser", Query.EQ("userId", approvalUser.Text("userId")));
                    toUserList.Add(approvalUser.Text("userId"));
                    //邮件标题
                    subject = "中海投资ERP系统审批业务事项提醒通知-您有新的工作信函审批！";

                    body.Append("<p style='text-indent: 2em'>您好，这条消息来自于ERP系统提醒推送。</p>");
                    body.AppendFormat("<p style='text-indent: 2em'><strong>{0}</strong> 工作信函当前流程流转到您审批的步骤，请及时登录ERP系统处理，谢谢！</p>", curMailObj.Text("title"));
                    string linkStr = SysAppConfig.MailHostDomain + "/MailCenter/Index?tagName=MailApproval&ClassType=1";
                    var nameByte = Encoding.Unicode.GetBytes(approvalUserObj.Text("name"));
                    loginCheckUrl += Convert.ToBase64String(nameByte);
                    loginCheckUrl += "&ReturnUrl=" + Url.Encode(linkStr);
                    body.AppendFormat("<p style='text-indent: 2em'>链接为：<a href='{0}'>{0}</a></p>", loginCheckUrl);
                }
                else if (type == "2")//审核通过后发送提醒邮件给签发人
                {
                    var signUser = dataOp.FindOneByKeyVal("MailSignUser", "mailId", mailId);
                    if (signUser == null)
                    {
                        result.Message = "该邮件没有设置签发人";
                        result.Status = Status.Failed;
                        return Json(TypeConvert.InvokeResultToPageJson(result));
                    }
                    var signUserObj = dataOp.FindOneByQuery("SysUser", Query.EQ("userId", signUser.Text("userId")));
                    toUserList.Add(signUser.Text("userId"));
                    subject = "中海投资ERP系统审批业务事项提醒通知-您有新的工作信函需要签发！";

                    body.Append("<p style='text-indent: 2em'>您好，这条消息来自于ERP系统提醒推送。</p>");
                    body.AppendFormat("<p style='text-indent: 2em'><strong>{0}</strong> 工作信函当前流程流转到您签发的步骤，请及时登录ERP系统处理，谢谢！</p>", curMailObj.Text("title"));
                    string linkStr = SysAppConfig.MailHostDomain + "/MailCenter/Index?tagName=MailSign&ClassType=1";
                    var nameByte = Encoding.Unicode.GetBytes(signUserObj.Text("name"));
                    loginCheckUrl += Convert.ToBase64String(nameByte);
                    loginCheckUrl += "&ReturnUrl=" + Url.Encode(linkStr);
                    body.AppendFormat("<p style='text-indent: 2em'>链接为：<a href='{0}'>{0}</a></p>", loginCheckUrl);
                }
                else if (type == "4")//审核或签发驳回发送提醒给工作信函发件人
                {
                    //获取工作信函发件人
                    var ownerUser = dataOp.FindOneByQuery("MailRefUser", Query.And(Query.EQ("mailId", mailId), Query.EQ("userType", "0")));
                    if (ownerUser == null)
                    {
                        result.Message = "未找到该工作信函发件人";
                        result.Status = Status.Failed;
                        return Json(TypeConvert.InvokeResultToPageJson(result));
                    }
                    var ownerUserObj = dataOp.FindOneByQuery("SysUser", Query.EQ("userId", ownerUser.Text("userId")));
                    toUserList.Add(ownerUser.Text("userId"));
                    subject = "中海投资ERP系统审批业务事项提醒通知-您有新的工作信函审批被驳回！";

                    body.Append("<p style='text-indent: 2em'>您好，这条消息来自于ERP系统提醒推送。</p>");
                    if (curMailObj.Text("sendStatus") == "5")
                    {
                        body.AppendFormat("<p style='text-indent: 2em'><strong>{0}</strong> 工作信函没有通过签发，已被驳回给您，请及时登录ERP系统处理，谢谢！</p>", curMailObj.Text("title"));
                    }
                    else if (curMailObj.Text("sendStatus") == "3")
                    {
                        body.AppendFormat("<p style='text-indent: 2em'><strong>{0}</strong> 工作信函没有通过审核，已被驳回给您，请及时登录ERP系统处理，谢谢！</p>", curMailObj.Text("title"));
                    }


                    string linkStr = string.Empty;//设置跳转链接地址
                    if (curMailObj.Text("sendStatus") == "5")//5:会签驳回  3：审核驳回
                    {
                        linkStr = SysAppConfig.MailHostDomain + "/MailCenter/Index?tagName=MailSend&ClassType=6";
                    }
                    else if (curMailObj.Text("sendStatus") == "3")
                    {
                        linkStr = SysAppConfig.MailHostDomain + "/MailCenter/Index?tagName=MailSend&ClassType=4";
                    }

                    var nameByte = Encoding.Unicode.GetBytes(ownerUserObj.Text("name"));
                    loginCheckUrl += Convert.ToBase64String(nameByte);
                    loginCheckUrl += "&ReturnUrl=" + Url.Encode(linkStr);
                    body.AppendFormat("<p style='text-indent: 2em'>链接为：<a href='{0}'>{0}</a></p>", loginCheckUrl);
                }
                else if (type == "5")
                {
                    //获取工作信函发件人
                    var ownerUser = dataOp.FindOneByQuery("MailRefUser", Query.And(Query.EQ("mailId", mailId), Query.EQ("userType", "0")));
                    if (ownerUser == null)
                    {
                        result.Message = "未找到该工作信函发件人";
                        result.Status = Status.Failed;
                        return Json(TypeConvert.InvokeResultToPageJson(result));
                    }
                    var ownerUserObj = dataOp.FindOneByQuery("SysUser", Query.EQ("userId", ownerUser.Text("userId")));
                    toUserList.Add(ownerUser.Text("userId"));
                    subject = "中海投资ERP系统审批业务事项提醒通知-您有新的工作信函通过审批！";

                    body.Append("<p style='text-indent: 2em'>您好，这条消息来自于ERP系统提醒推送。</p>");
                    body.AppendFormat("<p style='text-indent: 2em'><strong>{0}</strong> 工作信函已经通过审核，请注意登录ERP系统查看，谢谢！</p>", curMailObj.Text("title"));

                    string linkStr = string.Empty;//设置跳转链接地址

                    linkStr = SysAppConfig.MailHostDomain + "/MailCenter/Index?tagName=MailSend&ClassType=3";

                    var nameByte = Encoding.Unicode.GetBytes(ownerUserObj.Text("name"));
                    loginCheckUrl += Convert.ToBase64String(nameByte);
                    loginCheckUrl += "&ReturnUrl=" + Url.Encode(linkStr);
                    body.AppendFormat("<p style='text-indent: 2em'>链接为：<a href='{0}'>{0}</a></p>", loginCheckUrl);
                }
                else if (type == "3")//审核以及会签通过后发送提醒邮件给接收人
                {
                    List<string> userType = new List<string>() { "1", "2", "3" };//userType:0：发件人1：收件人 2：抄送人 3:抄报人  4:被传阅人
                    var queryStr = Query.And(Query.EQ("mailId", mailId), Query.In("userType", TypeConvert.StringListToBsonValueList(userType)));
                    toUserList = dataOp.FindAllByQuery("MailRefUser", queryStr).Select(p => p.Text("userId")).ToList();
                    subject = "中海投资ERP系统工作信函提醒通知-您收到新的工作信函！";
                    string linkStr = SysAppConfig.MailHostDomain + "/MailCenter/Index?tagName=MailReceive&ClassType=2";
                    foreach (var userId in toUserList)
                    {
                        var receiverObj = dataOp.FindOneByQuery("SysUser", Query.EQ("userId", userId));
                        var nameByte = Encoding.Unicode.GetBytes(receiverObj.Text("name"));
                        loginCheckUrl += Convert.ToBase64String(nameByte);
                        loginCheckUrl += "&ReturnUrl=" + Url.Encode(linkStr);
                        body.Clear();
                        body.Append("<p style='text-indent: 2em'>您好，这条消息来自于ERP系统即时推送提醒信息。</p>");
                        body.AppendFormat("<p style='text-indent: 2em'>您收到一份新的工作信函(主题为：<strong>{0}</strong>)，请您登录ERP系统查看，谢谢！</p>", curMailObj.Text("title"));
                        body.AppendFormat("<p style='text-indent: 2em'>链接为：<a href='{0}'>{0}</a></p>", loginCheckUrl);

                        SendMail(userId, subject, body.ToString(), string.Empty, true, logInfo);
                    }
                }

                #endregion

                body = body.Replace("yinhooleft", "<").Replace("yinhooright", ">");
                //设置邮件内容是否是html格式
                bool isBodyHtml = true;
                if (type != "3")
                {
                    result = SendMail(string.Join(",", toUserList), subject, body.ToString(), string.Empty, isBodyHtml, logInfo);
                }
            }
            catch (Exception ex)
            {
                result.Status = Status.Failed;
                result.Message = ex.Message;
            }

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        #endregion

        #region ZHHY工作函发送邮件提醒

        /// <summary>
        /// 根据工作信函id发送邮件给收文人、抄报和抄送人
        /// </summary>
        /// <returns></returns>
        public ActionResult SendMailToMailUserZHHY()
        {
            InvokeResult result = new InvokeResult() { Status = Status.Successful };
            List<string> addr = new List<string>();
            var mailId = PageReq.GetParam("mailId");
            var type = PageReq.GetParam("type");
            //type  1:发送给审核人  2：发送给会签人  3：发送给收文、抄报、抄送人  4:工作信函驳回提醒 5:工作信函审批通过
            var curMailObj = dataOp.FindOneByKeyVal("Mail", "mailId", mailId);
            if (curMailObj.IsNullOrEmpty())
            {
                result.Status = Status.Failed;
                result.Message = "未能找到该工作信函";
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            try
            {
                //设置邮件内容
                StringBuilder body = new StringBuilder();
                //收件人id
                var toUserList = new List<string>();
                //通过用户名直接登录
                var loginCheckUrl = SysAppConfig.MailHostDomain + "/Account/Mail_Login?name=";
                string logInfo = string.Format("工作函ID：{0} 主题：{1}", mailId, curMailObj.Text("title"));
                #region 根据type获取收件人id
                if (type == "1")//工作信函发送后给审核人发送提醒邮件
                {
                    var approvalUser = dataOp.FindOneByKeyVal("MailApprovalUser", "mailId", mailId);

                    if (approvalUser == null)
                    {
                        result.Message = "该邮件没有设置审核人";
                        result.Status = Status.Failed;
                        return Json(TypeConvert.InvokeResultToPageJson(result));
                    }
                    var approvalUserObj = dataOp.FindOneByQuery("SysUser", Query.EQ("userId", approvalUser.Text("userId")));

                    string linkStr = SysAppConfig.MailHostDomain + "/MailCenter/Index?tagName=MailApproval&ClassType=1";
                    var nameByte = Encoding.Unicode.GetBytes(approvalUserObj.Text("name"));
                    loginCheckUrl += Convert.ToBase64String(nameByte);
                    loginCheckUrl += "&ReturnUrl=" + Url.Encode(linkStr);
                    //邮件主题 
                    var subject = string.Format("尊敬的{0}，您好，从设计ERP系统推送过来一封待审核工作函，请处理", approvalUserObj.Text("name"));
                    body.Clear();
                    body.Append("此邮件来自于宏洋设计ERP系统，请勿回复，谢谢！<br />");
                    body.Append("您有一封待审核工作函需要进入系统处理 <br />");
                    body.AppendFormat("工作函标题：{0} <br />", curMailObj.Text("title"));
                    body.AppendFormat("当前轮到您审核，请<a href='{0}'>点击</a>此链接进入", loginCheckUrl);
                    //设置邮件内容是否是html格式
                    bool isBodyHtml = true;
                    result = SendMail(approvalUser.Text("userId"), subject, body.ToString(), string.Empty, isBodyHtml, logInfo);
                }
                else if (type == "2")//审核通过后发送提醒邮件给签发人
                {
                    var signUser = dataOp.FindOneByKeyVal("MailSignUser", "mailId", mailId);
                    if (signUser == null)
                    {
                        result.Message = "该邮件没有设置签发人";
                        result.Status = Status.Failed;
                        return Json(TypeConvert.InvokeResultToPageJson(result));
                    }
                    var signUserObj = dataOp.FindOneByQuery("SysUser", Query.EQ("userId", signUser.Text("userId")));
                    
                    string linkStr = SysAppConfig.MailHostDomain + "/MailCenter/Index?tagName=MailSign&ClassType=1";
                    var nameByte = Encoding.Unicode.GetBytes(signUserObj.Text("name"));
                    loginCheckUrl += Convert.ToBase64String(nameByte);
                    loginCheckUrl += "&ReturnUrl=" + Url.Encode(linkStr);

                    //邮件主题 
                    var subject = string.Format("尊敬的{0}，您好，从设计ERP系统推送过来一封待签发工作函，请处理", signUserObj.Text("name"));
                    body.Clear();
                    body.Append("此邮件来自于宏洋设计ERP系统，请勿回复，谢谢！<br />");
                    body.Append("您有一封待签发工作函需要进入系统处理 <br />");
                    body.AppendFormat("工作函标题：{0} <br />", curMailObj.Text("title"));
                    body.AppendFormat("当前轮到您签发，请<a href='{0}'>点击</a>此链接进入", loginCheckUrl);
                    //设置邮件内容是否是html格式
                    bool isBodyHtml = true;
                    result = SendMail(signUser.Text("userId"), subject, body.ToString(), string.Empty, isBodyHtml, logInfo);
                }
                else if (type == "4")//审核或签发驳回发送提醒给工作信函发件人
                {
                    //获取工作信函发件人
                    var ownerUser = dataOp.FindOneByQuery("MailRefUser", Query.And(Query.EQ("mailId", mailId), Query.EQ("userType", "0")));
                    if (ownerUser == null)
                    {
                        result.Message = "未找到该工作信函发件人";
                        result.Status = Status.Failed;
                        return Json(TypeConvert.InvokeResultToPageJson(result));
                    }
                    var ownerUserObj = dataOp.FindOneByQuery("SysUser", Query.EQ("userId", ownerUser.Text("userId")));
                    toUserList.Add(ownerUser.Text("userId"));
                    string linkStr = string.Empty;//设置跳转链接地址
                    if (curMailObj.Text("sendStatus") == "5")//5:会签驳回  3：审核驳回
                    {
                        linkStr = SysAppConfig.MailHostDomain + "/MailCenter/Index?tagName=MailSend&ClassType=6";
                    }
                    else if (curMailObj.Text("sendStatus") == "3")
                    {
                        linkStr = SysAppConfig.MailHostDomain + "/MailCenter/Index?tagName=MailSend&ClassType=4";
                    }
                    var nameByte = Encoding.Unicode.GetBytes(ownerUserObj.Text("name"));
                    loginCheckUrl += Convert.ToBase64String(nameByte);
                    loginCheckUrl += "&ReturnUrl=" + Url.Encode(linkStr);


                    //邮件主题 
                    var subject = string.Format("尊敬的{0}，您好，从设计ERP系统推送过来一条被驳回工作函，请处理", ownerUserObj.Text("name"));
                    body.Clear();
                    body.Append("此邮件来自于宏洋设计ERP系统，请勿回复，谢谢！<br />");
                    if (curMailObj.Text("sendStatus") == "5")
                    {
                        body.Append("您有一封工作函没有通过签发,需要进入系统处理 <br />");
                        body.AppendFormat("工作函标题：{0} <br />", curMailObj.Text("title"));
                    }
                    else if (curMailObj.Text("sendStatus") == "3")
                    {
                        body.Append("您有一封工作函没有通过审核,需要进入系统处理 <br />");
                        body.AppendFormat("工作函标题：{0} <br />", curMailObj.Text("title"));
                    }
                    body.AppendFormat("请<a href='{0}'>点击</a>此链接进入", loginCheckUrl);
                    //设置邮件内容是否是html格式
                    bool isBodyHtml = true;
                    result = SendMail(string.Join(",", toUserList), subject, body.ToString(), string.Empty, isBodyHtml, logInfo);
                }
                else if (type == "5")
                {
                    //获取工作信函发件人
                    var ownerUser = dataOp.FindOneByQuery("MailRefUser", Query.And(Query.EQ("mailId", mailId), Query.EQ("userType", "0")));
                    if (ownerUser == null)
                    {
                        result.Message = "未找到该工作信函发件人";
                        result.Status = Status.Failed;
                        return Json(TypeConvert.InvokeResultToPageJson(result));
                    }
                    var ownerUserObj = dataOp.FindOneByQuery("SysUser", Query.EQ("userId", ownerUser.Text("userId")));

                    //设置跳转链接地址
                    string linkStr = string.Empty;
                    linkStr = SysAppConfig.MailHostDomain + "/MailCenter/Index?tagName=MailSend&ClassType=3";
                    var nameByte = Encoding.Unicode.GetBytes(ownerUserObj.Text("name"));
                    loginCheckUrl += Convert.ToBase64String(nameByte);
                    loginCheckUrl += "&ReturnUrl=" + Url.Encode(linkStr);

                    //邮件主题
                    var subject = string.Format("尊敬的{0}，您好，从设计ERP系统推送过来一条已通过审核的工作函", ownerUserObj.Text("name"));
                    body.Clear();
                    body.Append("此邮件来自于宏洋设计ERP系统，请勿回复，谢谢！<br />");
                    body.Append("您有一封工作函已通过审核 <br />");
                    body.AppendFormat("工作函标题：{0} <br />", curMailObj.Text("title"));
                    body.AppendFormat("请<a href='{0}'>点击</a>此链接查看", loginCheckUrl);
                    //设置邮件内容是否是html格式
                    bool isBodyHtml = true;
                    result = SendMail(ownerUser.Text("userId"), subject, body.ToString(), string.Empty, isBodyHtml, logInfo);
                }
                else if (type == "3")//审核以及会签通过后发送提醒邮件给接收人
                {

                    List<string> userType = new List<string>() { "1", "2", "3" };//userType:0：发件人1：收件人 2：抄送人 3:抄报人  4:被传阅人
                    var queryStr = Query.And(Query.EQ("mailId", mailId), Query.In("userType", TypeConvert.StringListToBsonValueList(userType)));
                    toUserList = dataOp.FindAllByQuery("MailRefUser", queryStr).Select(p => p.Text("userId")).ToList();
                  
                    string linkStr = SysAppConfig.MailHostDomain + "/MailCenter/Index?tagName=MailReceive&ClassType=2";
                    foreach (var userId in toUserList)
                    {
                        var receiverObj = dataOp.FindOneByQuery("SysUser", Query.EQ("userId", userId));
                        //邮件主题 
                        var subject = string.Format("尊敬的{0}，您好，从设计ERP系统推送过来一条新收到的工作函，请处理", receiverObj.Text("name"));
                        var nameByte = Encoding.Unicode.GetBytes(receiverObj.Text("name"));
                        loginCheckUrl += Convert.ToBase64String(nameByte);
                        loginCheckUrl += "&ReturnUrl=" + Url.Encode(linkStr);
                        body.Clear();
                        body.Append("此邮件来自于宏洋设计ERP系统，请勿回复，谢谢！<br />");
                        body.Append("您收到一份新的工作信函，请您登录ERP系统查看，谢谢！<br />");
                        body.AppendFormat("工作函标题：{0} <br />", curMailObj.Text("title"));
                        body.AppendFormat("请<a href='{0}'>点击</a>此链接查看", loginCheckUrl);
                        SendMail(userId, subject, body.ToString(), string.Empty, true, logInfo);
                    }
                }

                #endregion

            }
            catch (Exception ex)
            {
                result.Status = Status.Failed;
                result.Message = ex.Message;
            }

            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        
        #endregion

        #region 邮件提醒发送方法
        /// <summary>
        /// 异步发送邮件
        /// </summary>
        /// <param name="idList">收件人id</param>
        /// <param name="subject">邮件主题</param>
        /// <param name="body">邮件内容</param>
        /// <param name="fileName">邮件附件</param>
        /// <param name="isBodyHtml">邮件内容是否html格式</param>
        /// <returns></returns>
        public InvokeResult SendMail(List<string> idList, string subject, string body, string fileName, bool isBodyHtml)
        {
            InvokeResult result = new InvokeResult() { Status = Status.Successful };
            int mailCount = 0;
            try
            {
                var toList = new List<BsonDocument>();
                foreach (var item in idList)
                {
                    var userObj = dataOp.FindOneByKeyVal("SysUser", "userId", item);
                    if (userObj != null)
                    {
                        toList.Add(userObj);
                        mailCount++;
                    }
                }
                toList = toList.Where(p => !String.IsNullOrEmpty(p.String("emailAddr"))).ToList();
                string toAddress = string.Join(",", toList.Select(p => p.String("emailAddr").Trim()));
                if (!string.IsNullOrEmpty(toAddress))
                {
                    MailSender sender = new MailSender(toAddress, subject, body, fileName, isBodyHtml);
                    sender.SendAsync();
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
        /// 异步发送邮件
        /// </summary>
        /// <param name="idList">收件人id</param>
        /// <param name="subject">邮件主题</param>
        /// <param name="body">邮件内容</param>
        /// <param name="fileName">邮件附件</param>
        /// <param name="isBodyHtml">邮件内容是否html格式</param>
        /// <returns></returns>
        public InvokeResult SendMail(List<int> idList, string subject, string body, string fileName, bool isBodyHtml)
        {
            InvokeResult result = new InvokeResult() { Status = Status.Successful };
            List<string> idStrList = new List<string>();

            foreach (var item in idList)
            {
                idStrList.Add(item.ToString());
            }
            result = SendMail(idStrList, subject, body, fileName, isBodyHtml);
            return result;
        }
        public InvokeResult SendMail(string idListStr, string subject, string body, string fileName, bool isBodyHtml,string logInfo)
        {
            InvokeResult result = new InvokeResult() { Status = Status.Successful };
            List<string> toUserIdList = idListStr.Split(new string[] { ",", "，" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            List<BsonDocument> toUserList = dataOp.FindAllByQuery("SysUser", Query.In("userId", toUserIdList.Select(i => (BsonValue)i))).ToList();
            toUserList = toUserList.Where(p => !String.IsNullOrEmpty(p.String("emailAddr"))).ToList();
            string toAddress = string.Join(",", toUserList.Select(p => p.String("emailAddr").Trim()));
            try
            {
                if (!string.IsNullOrEmpty(toAddress))
                {
                    MailSender sender = new MailSender(toAddress, subject, body, fileName, isBodyHtml,logInfo);
                    sender.SendAsync();
                }
            }
            catch (Exception ex)
            {
                result.Status = Status.Failed;
                result.Message = ex.Message;
            }
            return result;
        }
        #endregion 

        #region 设置调度中心定时发送邮件
        public void InitTestMail()
        {
            PageJson json = new PageJson();
            try
            {
                string cronExStr = "0 * * * * ?";//每天上午9点触发一次      0 0 9 * * ?
                if (cronExStr.Trim() != "")
                {
                    SystemMsg msg = new SystemMsg();
                    bool isSuccess = Yinhe.WebReference.YinheSchdeuler.SendTaskNoticeOnce(Yinhe.WebReference.Schdeuler.Group.Msg_WF_Action_Normal,
                        Guid.NewGuid().ToString(), cronExStr);


                    if (!isSuccess)
                    {
                        throw new Exception("注册服务失败");
                    }

                }
                else
                {
                    throw new Exception("未正确设置发送时间!");
                }

                json.Success = true;
            }
            catch (Exception ex)
            {
                json.Success = false;
                json.Message = ex.Message;
            }

            if (json.Success == true)
            {
                Response.Write("<script>alert('消息发送任务注册成功!');</script>");
            }
            else
            {
                Response.Write(string.Format("<script>alert('失败,{0}');</script>", json.Message));
            }

            return;
        }
        #endregion

        #region 设置调度中心定时删除zhtz长时间未审批设计供应商
        public void InitDelSup()
        {
            PageJson json = new PageJson();
            try
            {
                string cronExStr = "0 0 20 * * ?";//每天晚上8点触发一次      0 0 20 * * ?
                if (cronExStr.Trim() != "")
                {
                    SystemMsg msg = new SystemMsg();
                    bool isSuccess = Yinhe.WebReference.YinheSchdeuler.DeleteDesignSupplier(Yinhe.WebReference.Schdeuler.Group.Msg_ZHTZ_DeleteDesignSupplier,
                        Guid.NewGuid().ToString(), cronExStr);


                    if (!isSuccess)
                    {
                        throw new Exception("注册服务失败");
                    }

                }
                else
                {
                    throw new Exception("未正确设置发送时间!");
                }

                json.Success = true;
            }
            catch (Exception ex)
            {
                json.Success = false;
                json.Message = ex.Message;
            }

            if (json.Success == true)
            {
                Response.Write("<script>alert('消息发送任务注册成功!');</script>");
            }
            else
            {
                Response.Write(string.Format("<script>alert('失败,{0}');</script>", json.Message));
            }

            return;
        }
        #endregion

        #region 设置调度中心定时发送邮件
        public void InitUnDoInstanceMail()
        {
            PageJson json = new PageJson();
            try
            {
                string cronExStr = "0 * * * * ?";//每天早上9点触发一次      0 0 9 * * ?
                if (cronExStr.Trim() != "")
                {
                    SystemMsg msg = new SystemMsg();
                    bool isSuccess = Yinhe.WebReference.YinheSchdeuler.SendInstanceApproverNotice(Yinhe.WebReference.Schdeuler.Group.Msg_WF_Action_Normal,
                        Guid.NewGuid().ToString(), cronExStr);


                    if (!isSuccess)
                    {
                        throw new Exception("注册服务失败");
                    }

                }
                else
                {
                    throw new Exception("未正确设置发送时间!");
                }

                json.Success = true;
            }
            catch (Exception ex)
            {
                json.Success = false;
                json.Message = ex.Message;
            }

            if (json.Success == true)
            {
                Response.Write("<script>alert('消息发送任务注册成功!');</script>");
            }
            else
            {
                Response.Write(string.Format("<script>alert('失败,{0}');</script>", json.Message));
            }

            return;
        }
        #endregion



    }
}
