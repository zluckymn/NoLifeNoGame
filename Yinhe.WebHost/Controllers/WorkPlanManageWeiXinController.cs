using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Senparc.Weixin.MP;
using Senparc.Weixin.MP.Entities.Request;
using Senparc.Weixin.MP.MvcExtension;
using Senparc.Weixin.MP.Sample.CommonService.CustomMessageHandler;
using NLog;

namespace WorkPlanManageWeiXin.Controllers
{
    public class WeiXinController : Controller
    {

        public readonly string Token = "WPM";
        public readonly string EncodingAESKey = "KuDFPA3nYYcugADDSh7eyqZqSJfpoRLhOBgp7wFmK2p";
        public readonly string AppId = "wx7466f132d2d58755";
        Logger log = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// 微信后台验证地址（使用Get），微信后台的“接口配置信息”的Url填写如：http://weixin.senparc.com/weixin
        /// </summary>
        [HttpGet]
        [ActionName("Index")]
        public ActionResult Get(string signature, string timestamp, string nonce, string echostr)
        {
            if (CheckSignature.Check(signature, timestamp, nonce, Token))
            {
                return Content(echostr); //返回随机字符串则表示验证通过
            }
            else
            {
                return Content("failed:" + signature + "," + Senparc.Weixin.MP.CheckSignature.GetSignature(timestamp, nonce, Token) + "。如果您在浏览器中看到这条信息，表明此Url可以填入微信后台。");
            }
        }

        [HttpPost]
        [ActionName("Index")]
        public ActionResult Post(PostModel postModel)
        {
          
           // log.Info("开始check");
           
            if (!CheckSignature.Check(postModel.Signature, postModel.Timestamp, postModel.Nonce, Token))
            {
                return Content("参数错误！");
            }

            postModel.Token = Token;
            postModel.EncodingAESKey = EncodingAESKey;//根据自己后台的设置保持一致
            postModel.AppId = AppId;//根据自己后台的设置保持一致

           // log.Info("开始CustomMessageHandler");
            var messageHandler = new CustomMessageHandler(Request.InputStream, postModel);//接收消息
           // log.Info("开始Execute");
            messageHandler.Execute();//执行微信处理过程
           // log.Info("return WeixinResult");
            return new WeixinResult(messageHandler);//返回结果
        }


    
        
    }
}
