using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yinhe.ProcessingCenter.Business
{
  /// <summary>
  /// 推送消息致远OA基类
  /// </summary>
    public class PushToSeeyon
    {
        /// <summary>
        /// 推送消息到致远OA
        /// </summary>
        /// <param name="loginNames">用户登录名数组</param>
        /// <param name="content">推送内容</param>
        /// <param name="url">推送内容url</param>
        public void PushTodoInfo(string[] loginNames,string content,string url)
        {
            try
            {
               
                
                AuthorityService.authorityService auth = new AuthorityService.authorityService();
                AuthorityService.UserToken token = auth.authenticate("service-admin", "123456");
                
                MessageService.messageService ms = new MessageService.messageService();
                var urls = GetUrls(loginNames, url);
               
              
                ms.sendMessageByLoginName(token.id, loginNames, content+ " -- 产品管理系统", urls);
            }catch(Exception ex)
            {
                CommonLog log = new CommonLog();
                log.Info(ex.Message);
            }
        }

        /// <summary>
        /// 获取用户获取的链接
        /// </summary>
        /// <param name="loginNames"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public string[] GetUrls(string[] loginNames, string url)
        {
            var urls = new List<string>();
            
            url = Yinhe.ProcessingCenter.Common.Base64.EncodeBase64(System.Text.Encoding.GetEncoding("utf-8"), url);
            foreach (var item in loginNames)
            {
                url = string.Format("{0}{1}{2}", SysAppConfig.Domain, "/Account/Login_SSSSO?ReturnUrl=", url);

                var OaParam = Yinhe.ProcessingCenter.Common.Base64.EncodeBase64(System.Text.Encoding.GetEncoding("utf-8"), "from=oa&username=" + item + "&time=" + DateTime.Now);
                var tempUrl = url + "&OaParam=" + OaParam;
                urls.Add(tempUrl);
            }
            return urls.ToArray();
        }
    }
}
