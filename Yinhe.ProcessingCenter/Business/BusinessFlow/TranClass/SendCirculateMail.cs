using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using MongoDB.Bson;
using Yinhe.MessageSender;
using MongoDB.Driver.Builders;

namespace Yinhe.ProcessingCenter.BusinessFlow
{
    /// <summary>
    /// 流程传阅通知事务操作
    /// </summary>
    public class SendCirculateMail:IExecuteTran
    {
        
        public void ExecuteTran(int UserId, BsonDocument instance, BsonDocument stepTran)
        {

            DataOperation dataOp = new DataOperation();
            if (instance == null) return;
            var instanceId = instance.Text("flowInstanceId");

            var toUserIds = GetToUserIds(instance);
            var curFlowInstance = dataOp.FindOneByQuery("BusFlowInstance", Query.EQ("flowInstanceId", instanceId));
            if (curFlowInstance == null)
            {
                return;
            }
            var isBodyHtml = false;
            var mailBody = new StringBuilder();
            var tableName = curFlowInstance.Text("tableName");
            var linkUrl = string.Empty;
            if (tableName == "XH_DesignManage_Task")
            {
                var mailTitle = string.Format("中海宏洋ERP系统审批业务事项提醒通知--您有一个传阅的审批任务");
                var taskObj = dataOp.FindOneByKeyVal(tableName, curFlowInstance.Text("referFieldName"), curFlowInstance.Text("referFieldValue"));
                if (taskObj == null)
                {
                    return;
                }
                var projObj = dataOp.FindOneByQuery("XH_DesignManage_Project", Query.EQ("projId", taskObj.Text("projId")));
                if (projObj == null)
                {
                    return;
                }
                linkUrl = SysAppConfig.MailHostDomain + "/DesignManage/NewTaskWorkFlowInfo/" + taskObj.Text("taskId");
                mailBody.AppendFormat("您好,{0}项目的任务{1}，流程审批经系统传阅给您，请登录系统进行查阅；链接为： {2}", projObj.Text("name"), taskObj.Text("name"), linkUrl);
                
               SendMail(toUserIds, mailTitle, mailBody.ToString(), string.Empty, isBodyHtml);
            }
        }
        /// <summary>
        /// 获取邮件接收者ids
        /// </summary>
        /// <returns></returns>
        public List<string> GetToUserIds(BsonDocument instance)
        {
            DataOperation dataOp = new DataOperation();
            var query = Query.And(Query.EQ("flowId", instance.Text("flowId")), Query.EQ("stepId", instance.Text("stepId")), Query.EQ("unAvaiable","0"));
            var allInstanceStepUsers = dataOp.FindAllByQuery("StepCirculation", query);
            var resultIds = new List<int>();
            if (allInstanceStepUsers != null && allInstanceStepUsers.Count() != 0)
            {
                resultIds = allInstanceStepUsers.Select(p => p.Int("userId")).ToList();
            }
            #region 添加流程岗位人员,2013.7.23
            var allInstanceStepPosUsers = dataOp.FindAllByQuery("StepCirculationFlowPosition", query).Select(c => c.Int("flowPosId")).ToList();
            FlowUserHelper helper = new FlowUserHelper();
            foreach (var flowPosId in allInstanceStepPosUsers)
            {
                var curUserIds = helper.GetStepProjRoleUserByFlowPostId(instance.Int("flowId"), instance.Int("flowInstanceId"), flowPosId);
                if (curUserIds.Count() > 0)
                {
                    resultIds.AddRange(curUserIds);
                }
            }
            #endregion
            return resultIds.Distinct().Select(c=>c.ToString()).ToList();
        }

        #region 发送邮件提醒
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
            DataOperation dataOp = new DataOperation();
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
                string toAddress = string.Join(",", toList.Select(p => p.String("emailAddr")));
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
            idStrList=idList.Select(p=>p.ToString()).Distinct().ToList();
            result = SendMail(idStrList, subject, body, fileName, isBodyHtml);
            return result;
        }
        #endregion
    }
}
