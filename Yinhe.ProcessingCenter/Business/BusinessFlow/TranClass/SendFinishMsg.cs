using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using Yinhe.ProcessingCenter.DataRule;
using MongoDB.Bson;
using Yinhe.MessageSender;

namespace Yinhe.ProcessingCenter.BusinessFlow
{

    /// <summary>
    /// 更新计划状态为进行中
    /// </summary>
    public class SendFinishMsg : IExecuteTran
    {
        Yinhe.ProcessingCenter.CommonLog _log = new CommonLog();
        /// <summary>
        /// 对所有流程人员发送消息
        /// </summary>
        /// <param name="UserId"></param>
        /// <param name="instance"></param>
        /// <param name="stepTran"></param>
        public void ExecuteTran(int UserId, BsonDocument instance, BsonDocument stepTran)
        {
            var DataOp = new DataOperation();
            if (instance != null)
            {
                FlowInstanceHelper helper = new FlowInstanceHelper();


                var currentInstrance = DataOp.FindOneByKeyVal("BusFlowInstance", "flowInstanceId", instance.String("flowInstanceId"));
                var stepObj = currentInstrance.SourceBson("stepId");
                var userIdListInt = new List<int>();
                if (stepObj != null && stepObj.Int("actTypeId") == (int)FlowActionType.Launch)
                {
                    userIdListInt.Add(currentInstrance.Int("approvalUserId"));
                }
                else
                {
                    userIdListInt = helper.GetFlowInstanceAvaiableStepUser(instance.Int("flowId"), instance.Int("flowInstanceId"), currentInstrance.Int("stepId"));
                }
                List<string> userIdList = new List<string>();
                userIdList = (from t in userIdListInt
                              select t.ToString()).Distinct().ToList();
                //userIdList.Add(instance.Text("approvalUserId"));
                var userList = DataOp.FindAllByKeyValList("SysUser", "userId", userIdList); //获取流程涉及的所有人员


                //string strPre = "参与的";
                bool isSend = true;
                string smsTemple = "请审批{0}提交的流程：{1}项目 {2}任务";//"您{4}流程{3}已被{0}审批通过： {1}项目 {2}任务";
                BsonDocument task = DataOp.FindOneByKeyVal("XH_DesignManage_Task", "taskId", instance.Text("referFieldValue"));
                BsonDocument project = DataOp.FindOneByKeyVal("XH_DesignManage_Project", "projId", task.Text("projId"));
                BsonDocument user = DataOp.FindOneByKeyVal("SysUser", "userId", UserId.ToString());
                string content = string.Format(smsTemple, user.Text("name"), project.Text("name"), task.Text("name"), instance.Text("instanceName"));
                BsonDocument trace = DataOp.FindAllByKeyVal("BusFlowTrace", "flowInstanceId", instance.Text("flowInstanceId")).OrderByDescending(t => t.Date("createDate")).FirstOrDefault();
                string actId = trace != null ? trace.Text("actId") : "0";
                List<string> numList = new List<string>();
                List<SmsClass> smsSendList = new List<SmsClass>();

                if (trace.Int("traceType") == 0 || trace.Int("traceType") == -1)  // 发起审批
                {
                    smsTemple = "请审批{0}提交的流程：{1}项目 {2}任务";
                    content = string.Format(smsTemple, user.Text("name"), project.Text("name"), task.Text("name"), instance.Text("instanceName"));
                    isSend = false; //不对发起人发送发起短信

                }
                else
                {
                    if (actId != "0")
                    {
                        BsonDocument action = DataOp.FindOneByKeyVal("BusFlowAction", "actId", actId); //驳回审批
                        if (action.Int("type") == 3)
                        {
                            smsTemple = "您正在处理的流程已被{0}驳回： {1}项目 {2}任务";// "您{4}流程{3}已被{0}审批驳回： {1}项目 {2}任务";
                            content = string.Format(smsTemple, user.Text("name"), project.Text("name"), task.Text("name"), instance.Text("instanceName"));
                        }
                    }
                }
                foreach (var item in userList)
                {
                    SmsClass smsc = new SmsClass();
                    if (!string.IsNullOrEmpty(item.Text("mobileNumber")))
                    {
                        numList.Add(item.Text("mobileNumber"));
                        smsc.mobileNumber = item.Text("mobileNumber");
                        smsc.content = content;
                        smsSendList.Add(smsc);
                    }
                }

                //if (isSend)
                //{
                //    strPre = "发起的";
                //    string apprvoeContent = string.Format(smsTemple, user.Text("name"), project.Text("name"), task.Text("name"), instance.Text("instanceName"), strPre);
                //    var apprUser = userList.Where(t => t.Int("userId") == instance.Int("approvalUserId")).FirstOrDefault();
                //    if (!string.IsNullOrEmpty(apprUser.Text("mobileNumber")))
                //    {
                //        SmsClass smsc = new SmsClass();
                //        smsc.mobileNumber = apprUser.Text("mobileNumber");
                //        smsc.content = apprvoeContent;
                //        smsSendList.Add(smsc);

                //    }
                //}

                Send1(smsSendList);
            }
        }
        #region
        /// <summary>
        /// 短信发送
        /// </summary>
        /// <param name="phoneNumber"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        private delegate Yinhe.MessageSender.InvokeResult MyMethod(List<string> phoneNumber, string content);

        private Yinhe.MessageSender.InvokeResult method(List<string> phoneNumber, string content)
        {
            Yinhe.MessageSender.InvokeResult result = new Yinhe.MessageSender.InvokeResult();
            try
            {
                ISmsSender sender = null; // new SmsSenderXH();
                sender = (ISmsSender)Activator.CreateInstance("Yinhe.MessageSender", SysAppConfig.SenderClass).Unwrap();//"Yinhe.ProcessingCenter.SynAD.PathAnalyseXH"
                if (SysAppConfig.IsSendSms)
                {
                    result = sender.SendSMSByNumberList(phoneNumber, content);
                }
            }
            catch (Exception ex)
            {
                result.Status = MessageSender.Status.Failed;
                result.Message = ex.Message;
            }
            return result;
        }

        public void Send(List<string> phoneNumber, string content)
        {
            AsyncCallback cb = new AsyncCallback(MethodCompleted);
            MyMethod my = new MyMethod(method);
            _log.Info("短信发送日志： " + string.Join(",", phoneNumber) + "-------" + content);
            IAsyncResult asyncResult = my.BeginInvoke(phoneNumber, content, cb, my);
        }

        /// <summary>
        /// 异步回调函数
        /// </summary>
        /// <param name="asyncResult"></param>
        private void MethodCompleted(IAsyncResult asyncResult)
        {
            try
            {
                Yinhe.MessageSender.InvokeResult retDic = new Yinhe.MessageSender.InvokeResult();
                if (asyncResult == null)
                    return;
                retDic = (asyncResult.AsyncState as MyMethod).EndInvoke(asyncResult);

                _log.Info(retDic.Status.ToString() + retDic.Message);
            }
            catch (Exception ex)
            {

            }
        }

        #endregion

        #region

        private delegate Yinhe.MessageSender.InvokeResult MyMethod1(List<SmsClass> list);

        private Yinhe.MessageSender.InvokeResult method1(List<SmsClass> list)
        {
            Yinhe.MessageSender.InvokeResult result = new Yinhe.MessageSender.InvokeResult();
            try
            {
                ISmsSender sender = null; // new SmsSenderXH();
                sender = (ISmsSender)Activator.CreateInstance("Yinhe.MessageSender", SysAppConfig.SenderClass).Unwrap();//"Yinhe.ProcessingCenter.SynAD.PathAnalyseXH"
                if (SysAppConfig.IsSendSms)
                {
                    result = sender.SendSMSByClassList(list);
                }
            }
            catch (Exception ex)
            {
                result.Status = MessageSender.Status.Failed;
                result.Message = ex.Message;
            }
            return result;
        }

        public void Send1(List<SmsClass> list)
        {
            AsyncCallback cb = new AsyncCallback(MethodCompleted1);
            MyMethod1 my = new MyMethod1(method1);
            foreach (var item in list)
            {
                _log.Info("短信发送日志： " + string.Join(",", item.mobileNumber) + "-------" + item.content);
            }

            IAsyncResult asyncResult = my.BeginInvoke(list, cb, my);
        }

        /// <summary>
        /// 异步回调函数
        /// </summary>
        /// <param name="asyncResult"></param>
        private void MethodCompleted1(IAsyncResult asyncResult)
        {
            try
            {
                Yinhe.MessageSender.InvokeResult retDic = new Yinhe.MessageSender.InvokeResult();
                if (asyncResult == null)
                    return;
                retDic = (asyncResult.AsyncState as MyMethod1).EndInvoke(asyncResult);

                _log.Info(retDic.Status.ToString() + retDic.Message);
            }
            catch (Exception ex)
            {

            }
        }
        #endregion
    }

}
