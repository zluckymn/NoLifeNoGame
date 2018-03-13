/*----------------------------------------------------------------
    Copyright (C) 2015 Senparc
    
    文件名：CustomMessageHandler.cs
    文件功能描述：自定义MessageHandler
    
    
    创建标识：Senparc - 20150312
----------------------------------------------------------------*/

using System;
using System.Configuration;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Configuration;
using Senparc.Weixin.MP.Agent;
using Senparc.Weixin.Context;
using Senparc.Weixin.MP.Entities;
using Senparc.Weixin.MP.Entities.Request;
using Senparc.Weixin.MP.MessageHandlers;
using Senparc.Weixin.MP.Helpers;
using Senparc.Weixin.MP.Sample.CommonService.Utilities;
using NLog;
using Yinhe.ProcessingCenter;
using Yinhe.ProcessingCenter.DesignManage.TaskFormula;
using Yinhe.ProcessingCenter.Administration;
using MongoDB.Bson;
using System.Linq;
using System.Collections.Generic;
using Senparc.Weixin.MP.Sample.CommonService.MessageHandlers.CustomMessageHandler;
using System.Net;
using System.Threading;
using System.Runtime.Serialization.Json;
using Yinhe.ProcessingCenter.LifeDay;
namespace Senparc.Weixin.MP.Sample.CommonService.CustomMessageHandler
{
    /// <summary>
    /// 自定义MessageHandler
    /// 把MessageHandler作为基类，重写对应请求的处理方法
    /// </summary>
    public partial class CustomMessageHandler : MessageHandler<CustomMessageContext>
    {
        /*
         * 重要提示：v1.5起，MessageHandler提供了一个DefaultResponseMessage的抽象方法，
         * DefaultResponseMessage必须在子类中重写，用于返回没有处理过的消息类型（也可以用于默认消息，如帮助信息等）；
         * 其中所有原OnXX的抽象方法已经都改为虚方法，可以不必每个都重写。若不重写，默认返回DefaultResponseMessage方法中的结果。
         */

       
        #if DEBUG
        string agentUrl = "http://localhost:12222/App/Weixin/4";
        string agentToken = "27C455F496044A87";
        string wiweihiKey = "CNadjJuWzyX5bz5Gn+/XoyqiqMa5DjXQ";
        #else
        //下面的Url和Token可以用其他平台的消息，或者到www.weiweihi.com注册微信用户，将自动在“微信营销工具”下得到
        private string agentUrl = WebConfigurationManager.AppSettings["WeixinAgentUrl"];//这里使用了www.weiweihi.com微信自动托管平台
        private string agentToken = WebConfigurationManager.AppSettings["WeixinAgentToken"];//Token
        private string wiweihiKey = WebConfigurationManager.AppSettings["WeixinAgentWeiweihiKey"];//WeiweihiKey专门用于对接www.Weiweihi.com平台，获取方式见：http://www.weiweihi.com/ApiDocuments/Item/25#51
#endif

        private string appId = WebConfigurationManager.AppSettings["WeixinAppId"];
        private string appSecret = WebConfigurationManager.AppSettings["WeixinAppSecret"];
        private string WorkPlanManageConnectionString = WebConfigurationManager.AppSettings["WorkPlanManageConnectionString"];
        private string DayLifeUrl = WebConfigurationManager.AppSettings["DayLifeUrl"] == null ? "" : WebConfigurationManager.AppSettings["DayLifeBakUrl"];//;//日常地址
        private string DayLifeBakUrl = WebConfigurationManager.AppSettings["DayLifeBakUrl"] == null ? "http://59.61.72.34:8025/" : WebConfigurationManager.AppSettings["DayLifeBakUrl"];//日常地址
        private string MeetingAppUrl = WebConfigurationManager.AppSettings["MeetingAppUrl"] == null ? "http://api.meng-zheng.com.cn/MeetingApp/MeetingLogin" : WebConfigurationManager.AppSettings["MeetingAppUrl"];//会议地址
        private static DataOperation dataop = null;
        private static List<UserInfo> UserModeInfoList = new List<UserInfo>();//记录用户的动作模式
      


        Logger log = LogManager.GetCurrentClassLogger();

        public CustomMessageHandler(Stream inputStream, PostModel postModel, int maxRecordCount = 0)
            : base(inputStream, postModel, maxRecordCount)
        {
            //这里设置仅用于测试，实际开发可以在外部更全局的地方设置，
            //比如MessageHandler<MessageContext>.GlobalWeixinContext.ExpireMinutes = 3。
            WeixinContext.ExpireMinutes = 3;
        }

        public override void OnExecuting()
        {
            //测试MessageContext.StorageData
            if (CurrentMessageContext.StorageData == null)
            {
                CurrentMessageContext.StorageData = 0;
            }
            base.OnExecuting();
        }

        public override void OnExecuted()
        {
            base.OnExecuted();
            CurrentMessageContext.StorageData = ((int)CurrentMessageContext.StorageData) + 1;
        }

        /// <summary>
        /// 处理文字请求
        /// </summary>
        /// <returns></returns>
        public override IResponseMessageBase OnTextRequest(RequestMessageText requestMessage)
        {
            log.Info("OnTextRequest");
            //TODO:这里的逻辑可以交给Service处理具体信息，参考OnLocationRequest方法或/Service/LocationSercice.cs

            //方法一（v0.1），此方法调用太过繁琐，已过时（但仍是所有方法的核心基础），建议使用方法二到四
            //var responseMessage =
            //    ResponseMessageBase.CreateFromRequestMessage(RequestMessage, ResponseMsgType.Text) as
            //    ResponseMessageText;

            //方法二（v0.4）
            //var responseMessage = ResponseMessageBase.CreateFromRequestMessage<ResponseMessageText>(RequestMessage);

            //方法三（v0.4），扩展方法，需要using Senparc.Weixin.MP.Helpers;
            //var responseMessage = RequestMessage.CreateResponseMessage<ResponseMessageText>();

            //方法四（v0.6+），仅适合在HandlerMessage内部使用，本质上是对方法三的封装
            //注意：下面泛型ResponseMessageText即返回给客户端的类型，可以根据自己的需要填写ResponseMessageNews等不同类型。

            log.Info("CreateResponseMessage");
            var responseMessage = base.CreateResponseMessage<ResponseMessageText>();

            if (requestMessage.Content == null)
            {

            }
            else if (requestMessage.Content == "约束")
            {
                responseMessage.Content =
                    "<a href=\"http://weixin.senparc.com/FilterTest/\">点击这里</a>进行客户端约束测试（地址：http://weixin.senparc.com/FilterTest/）。";
            }
            else if (requestMessage.Content == "托管" || requestMessage.Content == "代理")
            {
                #region 代理
                //开始用代理托管，把请求转到其他服务器上去，然后拿回结果
                //甚至也可以将所有请求在DefaultResponseMessage()中托管到外部。

                DateTime dt1 = DateTime.Now; //计时开始

                var responseXml = MessageAgent.RequestXml(this, agentUrl, agentToken, RequestDocument.ToString());
                //获取返回的XML
                //上面的方法也可以使用扩展方法：this.RequestResponseMessage(this,agentUrl, agentToken, RequestDocument.ToString());

                /* 如果有WeiweihiKey，可以直接使用下面的这个MessageAgent.RequestWeiweihiXml()方法。
                 * WeiweihiKey专门用于对接www.weiweihi.com平台，获取方式见：http://www.weiweihi.com/ApiDocuments/Item/25#51
                 */
                //var responseXml = MessageAgent.RequestWeiweihiXml(weiweihiKey, RequestDocument.ToString());//获取Weiweihi返回的XML

                DateTime dt2 = DateTime.Now; //计时结束

                //转成实体。
                /* 如果要写成一行，可以直接用：
                 * responseMessage = MessageAgent.RequestResponseMessage(agentUrl, agentToken, RequestDocument.ToString());
                 * 或
                 * 
                 */
                #endregion
                responseMessage = responseXml.CreateResponseMessage() as ResponseMessageText;

                responseMessage.Content += string.Format("\r\n\r\n代理过程总耗时：{0}毫秒", (dt2 - dt1).Milliseconds);
            }
            else if (requestMessage.Content == "测试" || requestMessage.Content == "退出测试")
            {
               
            }
            else if (requestMessage.Content == "AsyncTest")
            {
                //异步并发测试（提供给单元测试使用）
                DateTime begin = DateTime.Now;
                int t1, t2, t3;
                System.Threading.ThreadPool.GetAvailableThreads(out t1, out t3);
                System.Threading.ThreadPool.GetMaxThreads(out t2, out t3);
                System.Threading.Thread.Sleep(TimeSpan.FromSeconds(4));
                DateTime end = DateTime.Now;
                var thread = System.Threading.Thread.CurrentThread;
                responseMessage.Content = string.Format("TId:{0}\tApp:{1}\tBegin:{2:mm:ss,ffff}\tEnd:{3:mm:ss,ffff}\tTPool：{4}",
                        thread.ManagedThreadId,
                        HttpContext.Current != null ? HttpContext.Current.ApplicationInstance.GetHashCode() : -1,
                        begin,
                        end,
                        t2 - t1
                        );
            }
            else
            {


                try
                {
                    var result = new StringBuilder();

                    #region 样例
                    //result.AppendFormat("您刚才发送了文字信息：{0}\r\n\r\n", requestMessage.Content);

                    //if (CurrentMessageContext.RequestMessages.Count > 1)
                    //{
                    //    result.AppendFormat("您刚才还发送了如下消息（{0}/{1}）：\r\n", CurrentMessageContext.RequestMessages.Count,
                    //        CurrentMessageContext.StorageData);
                    //    for (int i = CurrentMessageContext.RequestMessages.Count - 2; i >= 0; i--)
                    //    {
                    //        var historyMessage = CurrentMessageContext.RequestMessages[i];
                    //        result.AppendFormat("{0} 【{1}】{2}\r\n",
                    //            historyMessage.CreateTime.ToShortTimeString(),
                    //            historyMessage.MsgType.ToString(),
                    //            (historyMessage is RequestMessageText)
                    //                ? (historyMessage as RequestMessageText).Content
                    //                : "[非文字类型]"
                    //            );
                    //    }
                    //    result.AppendLine("\r\n");
                    //}

                    //result.AppendFormat("如果您在{0}分钟内连续发送消息，记录将被自动保留（当前设置：最多记录{1}条）。过期后记录将会自动清除。\r\n",
                    //    WeixinContext.ExpireMinutes, WeixinContext.MaxRecordCount);
                    //result.AppendLine("\r\n");
                    //result.AppendLine(
                    //    "您还可以发送【位置】【图片】【语音】【视频】等类型的信息，查看不同格式的回复。\r\n");
                    #endregion
                    if (dataop == null)
                    {
                        dataop = new DataOperation(WorkPlanManageConnectionString, true);
                    }
                    DesignManage_PlanBll planBll = DesignManage_PlanBll._(dataop);
                    SysUserBll userBll = SysUserBll._(dataop);
                    var curUserWeiXinCode = requestMessage.FromUserName;//当前账户名
                    var curUser = userBll.FindUsersByWeiXin(curUserWeiXinCode);
                    log.Info(string.Format("{0}刚才发送了文字信息{1}", curUser != null ? curUser.Text("name") : "匿名用户", requestMessage.Content));
                    #region 2次动作查询
                    var ActionMode = "0";
                    var curUserMode=UserModeInfoList.Where(c=>c.code==curUserWeiXinCode).FirstOrDefault();
                    if(curUserMode==null)
                    {
                        curUserMode=new UserInfo(){ code=curUserWeiXinCode, actionMode="0"};
                        UserModeInfoList.Add(curUserMode);
                    }
              
                    if (requestMessage.Content=="0"|| requestMessage.Content=="退出")
                    {
                      curUserMode.actionMode = "0";
                      curUserMode.MessageQueue.Clear();//清空邮件队列
                    }
                    ActionMode = curUserMode.actionMode;
                    log.Info("动作模式" + ActionMode);
                    switch (ActionMode)
                    {
                       
                        case "2": //微信绑定模式
                        case "绑定":
                        case "微信绑定":
                            if (!String.IsNullOrEmpty(requestMessage.Content.Trim()))
                            {
                                log.Info("微信绑定申请:" + curUserWeiXinCode + requestMessage.Content);
                                var curApplyUser = userBll.FindUsersByWeiXinName(requestMessage.Content.Trim());
                                if (curApplyUser != null && !curApplyUser.IsBsonNull)
                                {
                                    var updateBson = new BsonDocument();
                                    updateBson.Add("weixin", curUserWeiXinCode);
                                    var execResult = dataop.Update("SysUser", MongoDB.Driver.Builders.Query.EQ("userId", curApplyUser.Text("userId")), updateBson);
                                    if (execResult.Status == Status.Successful)
                                    {
                                        result.AppendLine(curApplyUser.Text("name") + "链接成功,欢迎回来");
                                    }
                                }
                                else
                                {
                                    //自动创建账号curUserWeiXinCode
                                    if (!string.IsNullOrEmpty(curUserWeiXinCode) && !string.IsNullOrEmpty(requestMessage.Content) && requestMessage.Content.Length>=2)
                                    {
                                        LifeDayHelper lifeDayHelper = new LifeDayHelper(dataop);
                                        curUser= lifeDayHelper.InitialUserInfo(curUserWeiXinCode, requestMessage.Content);
                                        if (curUser!=null)
                                        {
                                          result.AppendLine(curUser.Text("name") + "链接成功,欢迎回来");
                                        }
                                        else
                                        {
                                            result.AppendLine("请求已发送请等待管理员审核!");
                                        }
                                    }
                                   
                                }
                            }
                            curUserMode.actionMode = "0";
                           // ActionMode = 0;
                            break;
                        case "3": //电话查询模式
                        case "通讯录":
                        case "电话":
                            if (!String.IsNullOrEmpty(requestMessage.Content.Trim()))
                            {

                                var userList = userBll.GetSearchPhoneResult(requestMessage.Content);
                                if (userList.Count() > 0)
                                {
                                    foreach (var user in userList)
                                    {
                                        result.AppendFormat("【{0} {1}】\r\n", user.Text("name"), !string.IsNullOrEmpty(user.Text("phoneNumber")) ? user.Text("phoneNumber") : "未登记");
                                    }
                                    result.AppendLine("退出请输【0】或【退出】");
                                }
                                else
                                {
                                    result.AppendLine("暂无登记的电话！\n\r退出请输【0】或【退出】");
                                }

                            }
                            curUserMode.actionMode = "3";
                            //ActionMode = 0;
                            break;
                        case "4":
                        case "MN":
                        case "机器人":
                            #region 代理

                            curUserMode.actionMode = "4";
                            //result.AppendLine(GetTulingMessage(requestMessage.Content.Trim()));
                            //result.AppendLine("\n\r退出请输【0】或【退出】");

                            var cuResponse = GetTulingRichResponse(requestMessage.Content.Trim());
                            return cuResponse;
                            //  responseMessage.Content += result.ToString();
                            // return responseMessage;
                           #endregion
                        case "5":
                        case "alert":
                        case "闹钟":
                        case "提醒":
                            #region 代理
                              curUserMode.actionMode = "0";
                              var alertResponse = GetAlertResponse(requestMessage.Content.Trim(), curUser,false);
                              return alertResponse;
                             #endregion 
                        case "-5"://邮件发送确认,添加到队列
                              if (curUserMode.MessageQueue.Count() > 0)
                              { 
                                  var cmd=requestMessage.Content.Trim().ToLower();
                                  if (cmd == "ok" || cmd == "1" || cmd == "确定" || cmd == "好")
                                  {
                                      MessagePushQueueHelper pushQueueHelper = new MessagePushQueueHelper(dataop);
                                      foreach (var message in curUserMode.MessageQueue)
                                      {
                                          pushQueueHelper.PushMessage(message);
                                      }
                                      curUserMode.MessageQueue.Clear();//清空否则重复添加
                                      result.AppendLine("提醒登记成功，系统将会在设定时间给予提醒");
                                  }
                                  else
                                  {
                                      curUserMode.MessageQueue.Clear();//清空邮件队列
                                      result.AppendLine("取消成功！");
                                  }
                                
                              }
                              curUserMode.actionMode = "0";//退出模式
                              break;
                        default:
                            //ActionMode = 0;
                            #region 消息接口
                            switch (requestMessage.Content.Trim())
                            {
                                case "1":
                                case "任务":
                                case "我的任务":
                                    {
                                        #region 任务查询


                                        log.Info(curUserWeiXinCode + requestMessage.Content);

                                        try
                                        {


                                            if (curUser != null&& curUser.Int("status")!=2)
                                            {
                                                #region 内容查找
                                                log.Info(curUser.Text("name"));
                                                var taskList = planBll.GetUserTaskList(curUser.Text("userId"));
                                                //延迟开始的任务
                                                var delayDoingTaskList = taskList.Where(t => t.Int("status", 2) <= (int)TaskStatus.NotStarted && t.String("curStartData") != "" && t.Date("curStartData") < DateTime.Now).ToList();
                                                //延迟结束的任务
                                                var delayDoneTaskList = taskList.Where(t => t.Int("status", 2) == (int)TaskStatus.Processing && t.Int("status", 2) != (int)TaskStatus.Completed && t.String("curEndData") != "" && t.Date("curEndData") < DateTime.Now).ToList();
                                                //log.Info(string.Format("延迟开始个数{0}延迟完成个数{1}", delayDoingTaskList.Count(), delayDoneTaskList.Count()));
                                                if (delayDoingTaskList.Count() != 0 || delayDoneTaskList.Count() != 0)
                                                {
                                                    if (delayDoingTaskList.Count() != 0)
                                                    {
                                                        result.AppendFormat("您有以下延迟开始的任务\r\n");
                                                        foreach (var delayTask in delayDoingTaskList)
                                                        {
                                                            result.AppendFormat("【{0}】\r\n", delayTask.Text("name"));
                                                        }
                                                    }
                                                    if (delayDoneTaskList.Count() != 0)
                                                    {
                                                        result.AppendFormat("您有以下延迟完成的任务\r\n");
                                                        foreach (var delayTask in delayDoneTaskList)
                                                        {
                                                            result.AppendFormat("【{0}】\r\n", delayTask.Text("name"));
                                                        }
                                                    }
                                                }


                                                var doingTaskList = taskList.Where(t => t.Int("status", 2) != (int)TaskStatus.Completed && t.Date("curEndData") >= DateTime.Now && t.Date("curEndData") <= DateTime.Now.AddDays(3)).ToList();
                                                //log.Info(string.Format("您有3天内后即将完成任务{0}", doingTaskList.Count()));
                                                if (doingTaskList.Count() != 0)
                                                {
                                                    result.AppendFormat("您有3天内后即将完成任务\r\n");
                                                    foreach (var delayTask in doingTaskList)
                                                    {
                                                        result.AppendFormat("【{0}】\r\n", delayTask.Text("name"));
                                                    }
                                                }

                                                #endregion
                                            }
                                            else
                                            {
                                                responseMessage.Content = "请先输入【2】进行绑定微信号操作";
                                                return responseMessage;
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            log.Info(ex.Message);
                                        }
                                        //log.Info(result.ToString());
                                        // responseMessage.Content = result.ToString();
                                        #endregion
                                        curUserMode.actionMode = "0";
                                    }
                                    break;
                                case "2":
                                case "绑定":
                                case "微信绑定":
                                    {
                                        if (curUser == null)
                                        {
                                            result.AppendLine("请输入您的计划系统帐户名或者姓名:");
                                            //responseMessage.Content = "请输入您的计划系统帐户名或者姓名:";
                                          //  ActionMode = 2;//进入微信绑定模式
                                            curUserMode.actionMode = "2";
                                        }
                                        else
                                        {
                                            result.AppendFormat("{0}您好,您已成功绑定,请输入【1】,【任务】进行任务查询!", curUser.Text("name"));
                                            // responseMessage.Content = result.ToString();
                                           // ActionMode = 0;
                                            curUserMode.actionMode = "0";
                                        }
                                    }
                                    break;
                                case "3":
                                case "通讯录":
                                case "电话":
                                    {
                                        if (curUser == null || curUser.Int("status")==2)
                                        {

                                            result.AppendLine("您的帐号还没登陆无法查询！其先输入【2】进行绑定");
                                          //  ActionMode = 2;//进入微信绑定模式
                                        }
                                        else
                                        {
                                            result.AppendFormat("{0}您好,请输入您要查询的姓名 如：XXX", curUser.Text("name"));
                                            //responseMessage.Content = result.ToString();
                                           // ActionMode = 3;
                                            curUserMode.actionMode = "3";
                                        }
                                    }
                                    break;
                                case "4":
                                case "机器人":
                                case "MN":
                                    
                                    {
                                        //图灵机器人介入
                                        result.AppendLine("您好,欢迎进入管理员sama激情互动模式!");
                                        result.AppendLine("管理员sama可以给你汇报【厦门明天天气】\n\r【厦门北京今天航班】\n\r【车次】【星座】【笑话】【xx的图片】【体育新闻】\n\r【娱乐新闻】【成语接龙】\n\r【菜谱】【故事】【顺丰快递】，快来激情互动吧");
                                        curUserMode.actionMode = "4";
                                        break;
                                    }
                                case "5":
                                case "alert":
                                case "闹钟":
                                case "提醒":
                                    {

                                        //进入提醒模式
                                        result.AppendLine("您好,欢迎进入快速提醒模式");
                                        result.AppendLine("管理员sama根据您输入的【提醒我晚上6点10分进行购物】识别时间发送邮件，如需短信提醒请注册对应.139邮箱邮件推送！");
                                        curUserMode.actionMode = "5";
                                      
                                    }  break;
                                case "6":
                                case "我的alert":
                                case "我的闹钟":
                                case "查看提醒":
                                case "我的提醒":
                                    {
                                        if (curUser == null)
                                        {  
                                            result.AppendLine("您的帐号还没登陆无法查询！其先输入【2】进行绑定");
                                        }
                                        else
                                        {
                                            if (curUserMode.MessageQueue!=null&&curUserMode.MessageQueue.Count() > 0)
                                            curUserMode.MessageQueue.Clear();
                                            var messageResponse = GetMessageQueueRichResponse(curUser);
                                            return messageResponse;
                                        }
                                        curUserMode.actionMode = "0";
                                     
                                    }
                                    break;
                                case "-6":
                                case "取消提醒":
                                case "取消闹钟":
                                   {
                                        if (curUser == null)
                                        {
                                            result.AppendLine("您的帐号还没登陆无法查询！其先输入【2】进行绑定");
                                        }
                                        else
                                        {
                                            MessagePushQueueHelper pushQueueHelper = new MessagePushQueueHelper(dataop);
                                            if (pushQueueHelper.CancelMyNeedSendMessage(curUser.Text("userId")).Status == Status.Successful)
                                            {
                                                result.AppendLine("取消成功！您可以成功输入【6】进行添加提醒");
                                            }
                                            else
                                            {
                                                result.AppendLine("取消失败请联系管理员sama！输入【-6】重试");
                                            }
                                        }
                                        curUserMode.actionMode = "0";

                                    }
                                    break;
                                case "7": 
                                  
                                            var richResponseMessage = CreateResponseMessage<ResponseMessageNews>();
                                            if (!string.IsNullOrEmpty(DayLifeUrl))
                                            {
                                                richResponseMessage.Articles.Add(new Article()
                                                {
                                                    Title = "我的任务日常",
                                                    Description = "模拟我的地下城日常",
                                                    PicUrl = string.Format("{0}/Content/flat-ui/img/icons/png/Retina-Ready.png", DayLifeUrl),
                                                    Url = string.Format("{0}/LifeDay/NoLifeNoGame?weixin={1}&missionType=0", DayLifeUrl, curUserWeiXinCode)

                                                });
                                            }
                                            if (!string.IsNullOrEmpty(DayLifeBakUrl))
                                            {
                                                richResponseMessage.Articles.Add(new Article()
                                                {
                                                    Title = "我的任务日常_备用地址",
                                                    Description = "模拟我的life日常",
                                                    PicUrl = string.Format("{0}/Content/flat-ui/img/icons/png/Retina-Ready.png", DayLifeBakUrl),
                                                    Url = string.Format("{0}/LifeDay/NoLifeNoGame?weixin={1}&missionType=0", DayLifeBakUrl, curUserWeiXinCode)

                                                });
                                            }

                                            curUserMode.actionMode = "0";
                                            return richResponseMessage;
                                      
                                     
                                    break;

                                case "退出":
                                case "0":
                                default:
                                    {

                                        if (curUser != null)
                                        {
                                            result.AppendFormat("{0}您好,欢迎回来! 由于管理员sama目前还在学习目前只支持如下功能:\n\r", curUser.Text("name"));
                                           
                                        }
                                        result.AppendLine("输入如:【1】,【任务】,任务查询 注:(需在计划系统中有对应帐号并等待通过审核)");
                                        result.AppendLine("输入如:【2】,【绑定】, 进行计划任务帐号绑定");
                                        result.AppendLine("输入如:【3】,【电话】,【通讯录】, 进行通讯录查询");
                                        result.AppendLine("输入如:【4】,【机器人】,【MN】, 进行管理员聊天模式");
                                        result.AppendLine("输入如:【5】,【闹钟】,【alert】,【提醒】 进行闹钟提醒");
                                        result.AppendLine("输入如:【6】,【我的闹钟】,【我的闹钟】,【我的闹钟】 我添加的提醒展示");
                                        result.AppendLine("输入如:【-6】,【取消闹钟】,【取消提醒】, 进行取消提醒操作");
                                        result.AppendLine("输入如:【7】,【日常】, 进行地下城日常（beta1）");
                                        curUserMode.actionMode = "0";
                                   }
                                    break;
                            }
                            #endregion
                            break;


                    }
                    #endregion
                    responseMessage.Content = result.ToString();
                }
                catch (Exception ex)
                {
                    log.Info(ex.Message);
                }
            }

            if (string.IsNullOrEmpty(responseMessage.Content))
            {
                responseMessage.Content = "暂无查询到任何任务数据";
            }
            
            return responseMessage;
        }

        /// <summary>
        /// 处理位置请求
        /// </summary>
        /// <param name="requestMessage"></param>
        /// <returns></returns>
        public override IResponseMessageBase OnLocationRequest(RequestMessageLocation requestMessage)
        {
            var locationService = new LocationService();
            var responseMessage = locationService.GetResponseMessage(requestMessage as RequestMessageLocation);
            return responseMessage;
        }

        public override IResponseMessageBase OnShortVideoRequest(RequestMessageShortVideo requestMessage)
        {
            var responseMessage = this.CreateResponseMessage<ResponseMessageText>();
            responseMessage.Content = "您刚才发送的是小视频";
            return responseMessage;
        }

        /// <summary>
        /// 处理图片请求
        /// </summary>
        /// <param name="requestMessage"></param>
        /// <returns></returns>
        public override IResponseMessageBase OnImageRequest(RequestMessageImage requestMessage)
        {
            log.Info("OnImageRequest");
            var responseMessage = CreateResponseMessage<ResponseMessageNews>();
            responseMessage.Articles.Add(new Article()
            {
                Title = "您刚才发送了图片信息",
                Description = "您发送的图片将会显示在边上",
                PicUrl = requestMessage.PicUrl,
                Url = "http://59.61.72.34:8023/WorkPlan/ProjectProgressIndex"
            });
            responseMessage.Articles.Add(new Article()
            {
                Title = "第二条",
                Description = "第二条带连接的内容",
                PicUrl = requestMessage.PicUrl,
                Url = "http://59.61.72.34:8023/WorkPlan/ProjectProgressIndex"
            });

            return responseMessage;
        }

        /// <summary>
        /// 处理语音请求
        /// </summary>
        /// <param name="requestMessage"></param>
        /// <returns></returns>
        public override IResponseMessageBase OnVoiceRequest(RequestMessageVoice requestMessage)
        {
            var logStr=string.Empty;
         
            try
            {

               
                var accessToken = CommonAPIs.AccessTokenContainer.TryGetToken(appId, appSecret);
                 logStr = string.Format("MediaId：{0}  appId：{1} appSecret:{2} accessToken：{3}", requestMessage.MediaId, appId, appSecret, accessToken);
                 log.Info(logStr);
                //
              
                ////上传缩略图
               
                // //var uploadResult = AdvancedAPIs.Media.MediaApi.UploadTemporaryMedia(accessToken, UploadMediaFileType.image,
                // //                                             Server.GetMapPath("~/Images/Logo.jpg"));

                ////设置音乐信息
                //responseMessage.Music.Title = "天籁之音";
                //responseMessage.Music.Description = "播放您上传的语音";
                //responseMessage.Music.MusicUrl = "http://weixin.senparc.com/Media/GetVoice?mediaId=" + requestMessage.MediaId;
                //responseMessage.Music.HQMusicUrl = "http://weixin.senparc.com/Media/GetVoice?mediaId=" + requestMessage.MediaId;
                //log.Info(responseMessage.Music.MusicUrl);
                 log.Info("语音识别结果开始");
                 if (!string.IsNullOrEmpty(requestMessage.Recognition.Trim()))
                 {
                     log.Info("语音识别结果："+requestMessage.Recognition.Trim());
                     if (requestMessage.Recognition.Contains("提醒"))
                     {
                         if (dataop == null)
                         {
                             dataop = new DataOperation(WorkPlanManageConnectionString, true);
                         }
                         SysUserBll userBll = SysUserBll._(dataop);
                         var curUserWeiXinCode = requestMessage.FromUserName;//当前账户名
                         var curUser = new BsonDocument();
                         if (requestMessage != null && curUserWeiXinCode != null)
                         {
                             curUser = userBll.FindUsersByWeiXin(curUserWeiXinCode);
                             var cuResponse = GetAlertResponse(requestMessage.Recognition.Trim(), curUser, true);
                             return cuResponse;
                         }
                         else
                         { 
                            log.Info("语音识别结果："+requestMessage.Recognition.Trim());
                         }
                    
                     }
                     else //聊天
                     {
                         var cuResponse = GetTulingRichResponse(requestMessage.Recognition.Trim());
                         return cuResponse;
                     }
                 }
                 else
                 {
                   
                 }
            }
             catch (Exception ex)
            {
                logStr=string.Format("{0}",ex.Message);
                log.Info(logStr);
            }
            log.Info(logStr);
            var textResponseMessage = this.CreateResponseMessage<ResponseMessageText>();
            textResponseMessage.Content = "无法识别您说的什么！请说普通话好吗！";
            log.Info("方法结束");

            return textResponseMessage;
        }
        /// <summary>
        /// 处理视频请求
        /// </summary>
        /// <param name="requestMessage"></param>
        /// <returns></returns>
        public override IResponseMessageBase OnVideoRequest(RequestMessageVideo requestMessage)
        {
            var responseMessage = CreateResponseMessage<ResponseMessageText>();
            responseMessage.Content = "您发送了一条视频信息，ID：" + requestMessage.MediaId;
            return responseMessage;
        }

        /// <summary>
        /// 处理链接消息请求
        /// </summary>
        /// <param name="requestMessage"></param>
        /// <returns></returns>
        public override IResponseMessageBase OnLinkRequest(RequestMessageLink requestMessage)
        {
            var responseMessage = ResponseMessageBase.CreateFromRequestMessage<ResponseMessageText>(requestMessage);
            responseMessage.Content = string.Format(@"您发送了一条连接信息：
Title：{0}
Description:{1}
Url:{2}", requestMessage.Title, requestMessage.Description, requestMessage.Url);
            return responseMessage;
        }

        /// <summary>
        /// 处理事件请求（这个方法一般不用重写，这里仅作为示例出现。除非需要在判断具体Event类型以外对Event信息进行统一操作
        /// </summary>
        /// <param name="requestMessage"></param>
        /// <returns></returns>
        public override IResponseMessageBase OnEventRequest(IRequestMessageEventBase requestMessage)
        {
            var eventResponseMessage = base.OnEventRequest(requestMessage);//对于Event下属分类的重写方法，见：CustomerMessageHandler_Events.cs
            //TODO: 对Event信息进行统一操作
            return eventResponseMessage;
        }

        public override IResponseMessageBase DefaultResponseMessage(IRequestMessageBase requestMessage)
        {
            /* 所有没有被处理的消息会默认返回这里的结果，
             * 因此，如果想把整个微信请求委托出去（例如需要使用分布式或从其他服务器获取请求），
             * 只需要在这里统一发出委托请求，如：
             * var responseMessage = MessageAgent.RequestResponseMessage(agentUrl, agentToken, RequestDocument.ToString());
             * return responseMessage;
             */
           
            return null;
        }


        /// <summary>
        /// 富文本类型
        /// 
        /// </summary>
        public class RichTextMessage
        {
            /// <summary>
            /// 地址
            /// </summary>
            public string detailurl { get; set; }
            /// <summary>
            /// 图片
            /// </summary>
            public string icon { get; set; }

            /// <summary>
            /// 开始时间
            /// </summary>
            public string starttime { get; set; }
            /// <summary>
            /// 结束时间
            /// </summary>
            public string endtime { get; set; }

            ///-----------------新闻类
            /// <summary>
            /// 主题
            /// </summary>
            public string article { get; set; }
            /// <summary>
            /// 来源
            /// </summary>
            public string source { get; set; }
           

            ///-----------------列车类
            /// <summary>
            /// 车次
            /// </summary>
            public string trainnum { get; set; }
            /// <summary>
            /// 起点
            /// </summary>
            public string start { get; set; }
            /// <summary>
            /// 终点
            /// </summary>
            public string terminal { get; set; }
           

            ///-----------------列车类
            /// <summary>
            /// 航班
            /// </summary>
            public string flight { get; set; }
            /// <summary>
            /// 路由
            /// </summary>
            public string route { get; set; }
            
            ///-----------------菜谱类code	 状态码
           /// <summary>
            /// 菜谱名
            /// </summary>
            public string name { get; set; }
            /// <summary>
            /// 信息
            /// </summary>
            public string info { get; set; }
        }

        public class TulingMessage
        {
            /// <summary>
            /// 代码100000	 文本类数据
                    //305000	 列车
                    //306000	 航班
                    //200000	 网址类数据
                    //302000	 新闻
                    //308000	 菜谱、视频、小说
                    //40001	 key的长度错误（32位）
                    //40002	 请求内容为空
                    //40003	 key错误或帐号未激活
                    //40004	 当天请求次数已用完
                    //40005	 暂不支持该功能
                    //40006	 服务器升级中
                    //40007	 服务器数据格式异常
            /// </summary>
            public string code { get; set; }
            /// <summary>
            /// 文本
            /// </summary>
            public string text { get; set; }
            /// <summary>
            /// url
            /// </summary>
            public string url { get; set; }

            /// <summary>
            /// 列表,新闻类，航班，菜谱
            /// </summary>
            public List<RichTextMessage> list { get; set; }
 
        }

        /// <summary>
        /// 解析图灵并回复富文本反馈
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private IResponseMessageBase GetTulingRichResponse(string info)
        {
            var sn = GetTulingObj(info);
            var retString = string.Empty;
            var richResponseMessage = CreateResponseMessage<ResponseMessageNews>();
            if (sn != null)
            {
                switch (sn.code)
                {
                    //普通文本
                    case "100000": retString = sn.text; break;
                    //链接类
                    case "200000":
                    //航班类
                    case "306000":
                        retString = string.Format("{0}查看地址为{1}", sn.text, sn.url); break;
                    //新闻
                    case "302000":  
                        
                          foreach (var newInfo in sn.list)
                          {
                              richResponseMessage.Articles.Add(new Article()
                              {
                                  Title = newInfo.article,
                                  Description = newInfo.source,
                                  PicUrl = newInfo.icon,
                                  Url = newInfo.detailurl
                              });
                          }
                          return richResponseMessage;
                    break;
                    //菜谱视频小说
                    case "308000":  
                         
                          foreach (var newInfo in sn.list)
                          {
                              richResponseMessage.Articles.Add(new Article()
                              {
                                  Title = newInfo.name,
                                  Description = newInfo.info,
                                  PicUrl = newInfo.icon,
                                  Url = newInfo.detailurl
                              });
                          }
                          return richResponseMessage;
                    break;
                    // 列车
                    case "305000": retString = sn.text;
                    
                       
                          foreach (var newInfo in sn.list)
                          {
                              richResponseMessage.Articles.Add(new Article()
                              {
                                  Title = string.Format("车次：{0}", newInfo.trainnum),
                                  Description = string.Format("{0}至{1} {2}至{3}", newInfo.starttime, newInfo.endtime, newInfo.start, newInfo.terminal),
                                  PicUrl = newInfo.icon,
                                  Url = newInfo.detailurl
                              });
                          }
                          return richResponseMessage;
                    break;
                    default: retString = sn.text; break;
                }
 
            }
            var responseMessage = base.CreateResponseMessage<ResponseMessageText>();
            responseMessage.Content = retString+"\n\r退出请输【0】或【退出】";
            log.Info(retString.ToString());
            return responseMessage;
           
        }


        
        /// <summary>
        /// 返回消息提醒列表
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private IResponseMessageBase GetMessageQueueRichResponse(BsonDocument userInfo)
        {
            if (userInfo != null)
            {
                var retString = string.Empty;
                var richResponseMessage = CreateResponseMessage<ResponseMessageNews>();
                MessagePushQueueHelper pushQueueHelper = new MessagePushQueueHelper(dataop);
                var myAlterList = pushQueueHelper.FindMyNeedSendMessage(userInfo.Text("userId"));
                if (myAlterList.Count() > 0)
                {
                  //  var index = 1;
                    foreach (var message in myAlterList)
                    {
                        var detailContent = string.Format("{0}\n{1}", message.Text("title"), message.DateFormat("sendDate", "yyyy-MM-dd HH:mm"));
                        var lingUrl = string.Format("/WorkPlan/CommonTableManage/?tableName=SystemMessagePushQueue&orderByName=publishDate&orderByMode=1&condition=sendUserId@{0}|sendStatus@null|deleteStatus@null", userInfo.Text("userId"));
                        richResponseMessage.Articles.Add(new Article()
                        {
                            Title = detailContent,
                            Description = detailContent,
                            PicUrl = "http://http://59.61.72.34:8023/Content/images/icon/msg_icon.png",
                            Url = string.Format("http://59.61.72.34:8023/Account/YHSSOLogin/?userName={0}&returnUrl={1}", userInfo.Text("loginName"), System.Web.HttpUtility.UrlEncode(lingUrl))


                        });
                    }
                    return richResponseMessage;
                }
                else {
                    var responseMessage = base.CreateResponseMessage<ResponseMessageText>();
                    responseMessage.Content = "您暂无需要发送的提醒";
                    return responseMessage;
                }
            }
            else
            {
                var responseMessage = base.CreateResponseMessage<ResponseMessageText>();
                responseMessage.Content = "请先输入【2】进行微信绑定";
             
                return responseMessage;
            }
 
             
          
         
           
        }

        


        /// <summary>
        /// 解析语音命令，并添加到对应的邮件推送提醒列表
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private IResponseMessageBase GetAlertResponse(string info,BsonDocument curUser,bool needConfirm)
        {
            //TODO:日期识别
            //2015年8月19日晚上提醒我进行XXX操作
            //明天提醒我进行XXX操作
            //今天完成进行XXX
            //晚上6点
            var resultSB = new StringBuilder();
            var responseMessage = base.CreateResponseMessage<ResponseMessageText>();
             
            if (!string.IsNullOrEmpty(info))
            {
                VoiceTextDateTimeHelper voiceTextHelper = new VoiceTextDateTimeHelper();
                //var curDateStr = voiceTextHelper.GetDateTimeStr(info);
                var curDateStr = voiceTextHelper.GetDateTimeStrForAlert(info);//提小于当前时提醒专用,当时间小于当前时间会按一定算法进行提前，或者用户漏输入早上晚上
                
                if (string.IsNullOrEmpty(curDateStr))
                {
                    curDateStr = string.Format("{0} 09:00:00",DateTime.Now.AddDays(1).ToString("yyyy-MM-dd"));
                }
                log.Info(string.Format("识别时间为：{0}",curDateStr));
                var curUserMode = UserModeInfoList.Where(c => c.code == RequestMessage.FromUserName).FirstOrDefault();
                if (curUserMode == null)
                {
                    curUserMode = new UserInfo() { code = RequestMessage.FromUserName, actionMode = "6" };
                    UserModeInfoList.Add(curUserMode);
                }
                if (curUser == null)
                    curUser = new BsonDocument();
                var userId = curUser.Text("userId");

                resultSB.AppendLine("请核对您所输入的内容:");
                resultSB.AppendLine(string.Format("【{0} {1}】", info, curDateStr));
                var pwdTip = new MessagePushEntity()
                {
                    content = string.Format("{0}你好！{1}\n\r  时间：{2} 请注意:管理员sama发现您有一封定时消息提醒，该邮件如需进行手机短信推送，请先设定手机绑定。~喵", curUser.Text("name"), info, curDateStr),
                    arrivedUserIds = userId,
                    title = string.Format("{0},提醒时间：{1}", info, curDateStr),
                    sendDate = curDateStr,
                    sendUserId = userId,
                    type = "3",//提醒
                    addDayLifeTask=true
                };
                if (curUserMode != null && !string.IsNullOrEmpty(userId))
                {
                    if (needConfirm)
                    {
                        curUserMode.MessageQueue.Add(pwdTip);
                        curUserMode.actionMode = "-5";//语音模式邮件确认模式
                        resultSB.AppendLine("语音输入需要您请输入【1】【ok】进行确认保存！输入任意其他字符进行取消操作呦! ");

                    }
                    else {

                        MessagePushQueueHelper pushQueueHelper = new MessagePushQueueHelper(dataop);
                        pushQueueHelper.PushMessage(pwdTip);
                        resultSB.AppendLine("提醒成功,您可以输入【5】继续添加提醒【6】查看现有提醒！如要取消当前所有提醒【-6】或【取消】命令");
                        curUserMode.actionMode = "0";//进入提醒模式，退出提醒模式防止重复添加
                    }
                  
                    responseMessage.Content = resultSB.ToString();
                }
                else5
                {
                    resultSB.AppendLine("由于您未绑定系统管理员不知道你是谁无法发送邮件");
                    responseMessage.Content = resultSB.ToString();
                }

                log.Info(info.ToString());
              
            }
            else
            {
                responseMessage.Content = "未识别内容" + "\n退出请输【0】或【退出】";
            }
          
            return responseMessage;
           
        }
        



       /// <summary>
       /// 返回图灵信息序列化对象
       /// </summary>
       /// <param name="info"></param>
       /// <returns></returns>
        private TulingMessage GetTulingObj(string info)
        {
            var Url = string.Format("http://www.tuling123.com/openapi/api?key={0}&info={1}&userid=96266", "7d9bcbeb4a3fe11ccc35c755480fb50a", info);
        
            try
            {
                var returnInfo = getPageInfo(Url);
                if (returnInfo != null)
                {
                    var ser = new DataContractJsonSerializer(typeof(TulingMessage));
                    var ms = new MemoryStream(Encoding.UTF8.GetBytes(returnInfo));
                    TulingMessage sn = (TulingMessage)ser.ReadObject(ms);
                    if (sn != null)
                    {

                        return sn;

                    }
                }
            }
            catch (ThreadInterruptedException ex)
            {
                // ReportErrors(ex.Message, "PushQueue");
                return null;
            }
            catch (TimeoutException ex)
            {
                // ReportErrors(ex.Message, "PushQueue");
                return null;
            }
            catch (HttpListenerException ex)
            {
                // ReportErrors(ex.Message, "PushQueue");
                return null;
            }
            catch (Exception ex)
            {
                //  Yinhoo.Framework.Log.LogWarpper._().PushApplicationException(ex);
                //ReportErrors(ex.Message, "PushQueue");
                return null;
            }
            return null;
        }

        /// <summary>
        /// 获取普通文本反馈
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        private string GetTulingMessage(string info)
        {
            var sn = GetTulingObj(info);
            var retString = string.Empty;
            if (sn != null)
            {
                switch (sn.code)
                {
                    //普通文本
                    case "100000": retString = sn.text; break;
                    //链接类
                    case "200000":
                    //航班类
                    case "306000":
                        retString = string.Format("{0}查看地址为{1}", sn.text, sn.url); break;
                    //新闻
                    case "302000": retString = sn.text; break;
                    //菜谱视频小说
                    case "308000": retString = sn.text; break;
                    // 列车
                    case "305000": retString = sn.text; break;
                    default: retString = sn.text; break;
                }
            }
             ///记录回复信息
            log.Info(retString.ToString());
            return retString;
        }
        /// <summary>
        /// 获取url数据
        /// </summary>
        /// <param name="strUrl"></param>
        /// <returns></returns>
        public static string getPageInfo(string strUrl)
        {
            // 构建一个请求
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(strUrl);
            // 请求的方式
            request.Method = "GET";

            // 请求的响应
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            // 响应的流
            Stream responseStream = response.GetResponseStream();

            // 字符编码
            Encoding enc = Encoding.GetEncoding("utf-8");

            // 读取流
            StreamReader readResponseStream = new StreamReader(responseStream, enc);

            // 请求的结果
            string result = readResponseStream.ReadToEnd();

            // 关闭流,响应,释放资源
            readResponseStream.Close();
            response.Close();

            return result;

        }
    }
}
