using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using Yinhe.ProcessingCenter.DataRule;
using MongoDB.Bson;
using Yinhe.MessageSender;
using Yinhe.ProcessingCenter.Common;
using Yinhe.ProcessingCenter.BusinessFlow;
using NLog;
using System.Threading;
using System.Web;
using MongoDB.Driver.Builders;

namespace Yinhe.ProcessingCenter.BusinessFlow
{
    public class SendApprovalMailNotice:IExecuteTran
    {

        #region IExecuteTran 成员

        public void ExecuteTran(int UserId, BsonDocument instance, BsonDocument stepTran)
        {
            if (SysAppConfig.CustomerCode == CustomerCode.ZHHY)
            {
                ZHHYSendNextAppNotice(instance.Int("flowInstanceId"));
            }
        }

        #endregion

        #region ZHHY给流程审批人员发送邮件提醒
        public void ZHHYSendTurnNotice(int flowInstanceId, List<int> userIds)
        {
            DataOperation dataOp = new DataOperation();
            Logger log = LogManager.GetCurrentClassLogger();
            var userIdListInt = new List<int>();
            var helper = new FlowInstanceHelper();
            int noticeType = 0;//0:普通下一步审批提醒 1：驳回提醒 2：通过提醒
            var curFlowInstance = dataOp.FindOneByKeyVal("BusFlowInstance", "flowInstanceId", flowInstanceId.ToString());

            //获取当前步骤
            var curStepObj = dataOp.FindOneByKeyVal("BusFlowStep", "stepId", curFlowInstance.Text("stepId"));
            //当前流程动作类型
            var curActTypeObj = dataOp.FindOneByKeyVal("BusFlowActionType", "actTypeId", curStepObj.Text("actTypeId"));

            List<string> userIdList = new List<string>();
            userIdList = (from t in userIds
                          select t.ToString()).Distinct().ToList();
            var userList = dataOp.FindAllByKeyValList("SysUser", "userId", userIdList); //获取流程涉及的所有人员

            //获取当前流程模板
            var busflow = dataOp.FindOneByQuery("BusFlow", Query.EQ("flowId", curFlowInstance.Text("flowId")));


            var curTaskObj = dataOp.FindOneByKeyVal(curFlowInstance.Text("tableName"), curFlowInstance.Text("referFieldName"), curFlowInstance.Text("referFieldValue"));

            //设置链接跳转地址
            string taskLinkStr = string.Empty;
            switch (curFlowInstance.Text("tableName"))
            {
                case "XH_DesignManage_Task":
                    taskLinkStr = string.Format("{0}{1}{2}", SysAppConfig.MailHostDomain, "/DesignManage/NewTaskWorkFlowInfo/", curFlowInstance.Text("referFieldValue"));
                    break;
                case "FreeFlow_FreeApproval":
                    taskLinkStr = string.Format("{0}{1}{2}", SysAppConfig.MailHostDomain, "/WorkFlowInfoCenter/FreeTaskWorkFlowInfo/", curFlowInstance.Text("referFieldValue"));
                    break;
                case "DesignChange":
                    taskLinkStr = SysAppConfig.MailHostDomain + "/DesignManage/DesignChangeWorkFlowInfo?dngChangeId=" + curFlowInstance.Text("referFieldValue") + "&typeStr=1";
                    break;
                default:
                    break;
            }
            var body = new StringBuilder();
            body.Append("此邮件来自于宏洋设计ERP系统，请勿回复，谢谢！<br />");
            body.Append("您有一条待审批流程需要进入系统处理 <br />");

            body.AppendFormat("流程名称：{0} <br />", busflow.Text("name"));
            //设置邮件内容是否为html格式
            bool isBodyHtml = true;

            log.Info("开始给 {0} 发送流程审批 转办 邮件提醒", string.Join(",", userList.Select(i => i.Text("name"))));
            string logInfo = string.Format("流程名称：{0} 流程ID：{1} 动作：{2} 动作ID：{3} 动作顺序：{4}",
                     curFlowInstance.Text("instanceName"),
                     curFlowInstance.Int("flowInstanceId"),
                     curActTypeObj.Text("name"),
                     curStepObj.Int("stepId"),
                     curStepObj.Int("stepOrder")
                     );
            log.Info(logInfo);
            //异步发送邮件
            foreach (var user in userList)
            {
                //邮件主题 
                var subject = string.Format("尊敬的{0}，您好，从设计ERP系统推送过来一条待审批流程，请处理", user.Text("name"));
                //通过用户名直接登录
                var loginCheckUrl = SysAppConfig.MailHostDomain + "/Account/Mail_Login?name=";
                //发送成功后的log
                var successInfo = string.Format("给 {0} 发送邮件提醒成功 流程名称：{1}", user.Text("name"), curFlowInstance.Text("instanceName"));

                var nameByte = Encoding.Unicode.GetBytes(user.Text("name"));
                loginCheckUrl += Convert.ToBase64String(nameByte);
                loginCheckUrl += "&ReturnUrl=" + HttpUtility.UrlEncode(taskLinkStr);
                string emailAddr = user.Text("emailAddr");//收件人邮箱

                StringBuilder tempBody = new StringBuilder(body.ToString());
                tempBody.AppendFormat("当前轮到您审批，请<a href='{0}'>点击</a>此链接进入", loginCheckUrl);
                MailSender sender = new MailSender(
                    emailAddr,
                    subject,
                    tempBody.ToString(),
                    string.Empty,
                    isBodyHtml,
                    successInfo);
                sender.SendAsync();
            }
        }

        public void ZHHYSendNextAppNotice(int flowInstanceId)
        {
            DataOperation dataOp = new DataOperation();
            Logger log = LogManager.GetCurrentClassLogger();
            var userIdListInt = new List<int>();
            var helper = new FlowInstanceHelper();
            int noticeType = 0;//0:普通下一步审批提醒 1：驳回提醒 2：通过提醒
            var curFlowInstance = dataOp.FindOneByKeyVal("BusFlowInstance", "flowInstanceId", flowInstanceId.ToString());
            if (curFlowInstance.Int("instanceStatus") == 1)
            {
                noticeType = 2;
            }
            else
            {
                BsonDocument trace = dataOp.FindAllByKeyVal("BusFlowTrace", "flowInstanceId", curFlowInstance.Text("flowInstanceId")).OrderByDescending(t => t.Date("createDate")).FirstOrDefault();
                string actId = trace != null ? trace.Text("actId") : "0";
                if (actId != "0")
                {
                    BsonDocument action = dataOp.FindOneByKeyVal("BusFlowAction", "actId", actId); //驳回审批
                    if (action.Int("type") == 3)
                    {
                        noticeType = 1;
                    }
                }
            }
            //获取当前步骤
            var curStepObj = dataOp.FindOneByKeyVal("BusFlowStep", "stepId", curFlowInstance.Text("stepId"));
            //当前流程动作类型
            var curActTypeObj = dataOp.FindOneByKeyVal("BusFlowActionType", "actTypeId", curStepObj.Text("actTypeId"));

            if (curFlowInstance.Int("instanceStatus") == 1)
            {
                userIdListInt.Add(curFlowInstance.Int("approvalUserId"));
            }
            else
            {
                if (curStepObj.Int("actTypeId") == (int)FlowActionType.Launch)
                {
                    userIdListInt = helper.GetCanLaunchUserIds(flowInstanceId);
                }
                else
                {
                    userIdListInt = helper.GetFlowInstanceAvaiableStepUser(curFlowInstance.Int("flowId"), curFlowInstance.Int("flowInstanceId"), curFlowInstance.Int("stepId"));
                }
            }


            List<string> userIdList = new List<string>();
            userIdList = (from t in userIdListInt
                          select t.ToString()).Distinct().ToList();
            var userList = dataOp.FindAllByKeyValList("SysUser", "userId", userIdList); //获取流程涉及的所有人员




            //获取当前流程模板
            var busflow = dataOp.FindOneByQuery("BusFlow", Query.EQ("flowId", curFlowInstance.Text("flowId")));


            var curTaskObj = dataOp.FindOneByKeyVal(curFlowInstance.Text("tableName"), curFlowInstance.Text("referFieldName"), curFlowInstance.Text("referFieldValue"));

            //设置链接跳转地址
            string taskLinkStr = string.Empty;
            switch (curFlowInstance.Text("tableName"))
            {
                case "XH_DesignManage_Task":
                    taskLinkStr = string.Format("{0}{1}{2}", SysAppConfig.MailHostDomain, "/DesignManage/NewTaskWorkFlowInfo/", curFlowInstance.Text("referFieldValue"));
                    break;
                case "FreeFlow_FreeApproval":
                    taskLinkStr = string.Format("{0}{1}{2}", SysAppConfig.MailHostDomain, "/WorkFlowInfoCenter/FreeTaskWorkFlowInfo/", curFlowInstance.Text("referFieldValue"));
                    break;
                case "DesignChange":
                    taskLinkStr = SysAppConfig.MailHostDomain + "/DesignManage/DesignChangeWorkFlowInfo?dngChangeId=" + curFlowInstance.Text("referFieldValue") + "&typeStr=1";
                    break;
                default:
                    break;
            }

            //设置邮件内容
            //此邮件来自于宏洋设计ERP系统，请勿回复，谢谢！
            //您有一条待审批流程需要进入系统处理。
            //流程名称：XXXX。（取自我们系统的流程名称）
            //当前轮到您审批，请点击此链接进入。（“链接"加上我们系统对应页面的链接，用户可以点击跳转到对应的页面）

            //顺祝商祺！
            var body = new StringBuilder();
            body.Append("此邮件来自于宏洋设计ERP系统，请勿回复，谢谢！<br />");
            if (noticeType == 0)
            {
                body.Append("您有一条待审批流程需要进入系统处理 <br />");
            }
            else if (noticeType == 1)
            {
                body.Append("您有一条审批流程被驳回需要进入系统处理 <br />");
            }
            else if (noticeType == 2)
            {
                body.Append("您有一条审批流程通过审批请进入系统查看 <br />");
            }

            body.AppendFormat("流程名称：{0} <br />", busflow.Text("name"));
            //设置邮件内容是否为html格式
            bool isBodyHtml = true;

            log.Info("开始给 {0} 发送流程审批邮件提醒", string.Join(",", userList.Select(i => i.Text("name"))));
            string logInfo = string.Format("流程名称：{0} 流程ID：{1} 动作：{2} 动作ID：{3} 动作顺序：{4}",
                     curFlowInstance.Text("instanceName"),
                     curFlowInstance.Int("flowInstanceId"),
                     curActTypeObj.Text("name"),
                     curStepObj.Int("stepId"),
                     curStepObj.Int("stepOrder")
                     );
            log.Info(logInfo);
            //异步发送邮件
            foreach (var user in userList)
            {
                //邮件主题 
                var subject = string.Format("尊敬的{0}，您好，从设计ERP系统推送过来一条待审批流程，请处理", user.Text("name"));
                //通过用户名直接登录
                var loginCheckUrl = SysAppConfig.MailHostDomain + "/Account/Mail_Login?name=";
                //发送成功后的log
                var successInfo = string.Format("给 {0} 发送邮件提醒成功 流程名称：{1}", user.Text("name"), curFlowInstance.Text("instanceName"));

                var nameByte = Encoding.Unicode.GetBytes(user.Text("name"));
                loginCheckUrl += Convert.ToBase64String(nameByte);
                loginCheckUrl += "&ReturnUrl=" + HttpUtility.UrlEncode(taskLinkStr);
                string emailAddr = user.Text("emailAddr");//收件人邮箱

                StringBuilder tempBody = new StringBuilder(body.ToString());
                tempBody.AppendFormat("当前轮到您审批，请<a href='{0}'>点击</a>此链接进入", loginCheckUrl);
                MailSender sender = new MailSender(
                    emailAddr,
                    subject,
                    tempBody.ToString(),
                    string.Empty,
                    isBodyHtml,
                    successInfo);
                sender.SendAsync();
            }
        }

        #endregion 
    }
}
