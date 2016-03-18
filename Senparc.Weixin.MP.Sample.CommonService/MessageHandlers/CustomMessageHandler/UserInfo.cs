using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Yinhe.ProcessingCenter;

namespace Senparc.Weixin.MP.Sample.CommonService.MessageHandlers.CustomMessageHandler
{
     public class UserInfo
    {
         //用户代码
         public string code { get;set;}
         //用户当前动作模式
         public string actionMode { get;set;}

         private List<MessagePushEntity> _MessageQueue = new List<MessagePushEntity>();
         /// <summary>
         /// 消息推送队列
         /// </summary>
         public  List<MessagePushEntity> MessageQueue{
             get
             {
                 return _MessageQueue;
             }

             set
              {
                  _MessageQueue = value;
             }
         }
    }
}
