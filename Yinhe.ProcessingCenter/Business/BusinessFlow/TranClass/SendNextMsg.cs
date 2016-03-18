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
    /// 为下一步流程人员发送消息
    /// </summary>
    class SendNextMsg : IExecuteTran
    {
        /// <summary>
        /// 修改计划状态为进行中
        /// </summary>
        /// <param name="UserId"></param>
        /// <param name="instance"></param>
        /// <param name="stepTran"></param>
        public void ExecuteTran(int UserId, BsonDocument instance, BsonDocument stepTran)
        {
                var  DataOp=new DataOperation();
                if(instance!=null)
                {
                    FlowInstanceHelper helper = new FlowInstanceHelper();
                    SendFinishMsg msg = new SendFinishMsg();

                    var currentInstrance = DataOp.FindOneByKeyVal("BusFlowInstance", "flowInstanceId", instance.String("flowInstanceId"));
                    var userIdListInt = helper.GetFlowInstanceAssociateUser(instance.Int("flowId"), instance.Int("flowInstanceId"));
                    userIdListInt.Add(instance.Int("approvalUserId"));
                    List<string> userIdList = (from t in userIdListInt
                                               select t.ToString()).Distinct().ToList();
                    var userList = DataOp.FindAllByKeyValList("SysUser", "userId", userIdList); //获取流程涉及的所有人员

                    string smsTemple = "您{0}的流程已被审批通过： {1}项目 {2}任务";
                    BsonDocument task = DataOp.FindOneByKeyVal("XH_DesignManage_Task", "taskId", instance.Text("referFieldValue"));
                    BsonDocument project = DataOp.FindOneByKeyVal("XH_DesignManage_Project", "projId", task.Text("projId"));
                    BsonDocument user = DataOp.FindOneByKeyVal("SysUser", "userId", UserId.ToString());
                    BsonDocument trace = DataOp.FindAllByKeyVal("BusFlowTrace", "flowInstanceId", instance.Text("flowInstanceId")).OrderByDescending(t => t.Date("createDate")).FirstOrDefault();
                    List<SmsClass> smsSendList = new List<SmsClass>();
                    foreach (var item in userList)
                    {
                        SmsClass smsc = new SmsClass();
                        if (!string.IsNullOrEmpty(item.Text("mobileNumber")))
                        {
                            smsc.mobileNumber = item.Text("mobileNumber");
                            if (item.Int("userId") == instance.Int("approvalUserId"))
                            {
                                smsc.content = string.Format(smsTemple,"发起",project.Text("name"),task.Text("name"));
                            }
                            else
                            {
                                smsc.content = string.Format(smsTemple, "参与", project.Text("name"), task.Text("name")); ;
                            }
                           
                            smsSendList.Add(smsc);
                        }
                    }

                    msg.Send1(smsSendList);
                }
     
        }
    }

}
