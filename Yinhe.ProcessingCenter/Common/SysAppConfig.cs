using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace Yinhe.ProcessingCenter
{
    /// <summary>
    /// 系统配置项处理类
    /// </summary>
    public class SysAppConfig
    {
        /// <summary>
        /// 是否是插件项目
        /// </summary>
        public static bool IsPlugIn
        {
            get
            {
                if (ConfigurationManager.AppSettings["IsPlugIn"] != null)
                {
                    return ConfigurationManager.AppSettings["IsPlugIn"] == "1";
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// 数据库连接串
        /// </summary>
        public static string DataBaseConnectionString
        {
            get
            {
                if (ConfigurationManager.AppSettings["DataBaseConnectionString"] != null)
                {
                    return ConfigurationManager.AppSettings["DataBaseConnectionString"];
                }
                else
                {
                    return "";
                }
            }
        }

        /// <summary>
        /// 数据库连接串
        /// </summary>
        public static string GlobalCss
        {
            get
            {
                if (ConfigurationManager.AppSettings["GlobalCss"] != null)
                {
                    return ConfigurationManager.AppSettings["GlobalCss"];
                }
                else
                {
                    return "/Content/css/client/xuhui/xuhui.css";
                }
            }
        }

        /// <summary>
        /// HOST站点的地址
        /// </summary>
        public static string HostDomain
        {
            get
            {
                if (ConfigurationManager.AppSettings["HostDomain"] != null)
                {
                    return ConfigurationManager.AppSettings["HostDomain"];
                }
                else
                {
                    return "";
                }
            }
        }

        /// <summary>
        /// HOST站点的地址
        /// </summary>
        public static string Domain
        {
            get
            {
                if (ConfigurationManager.AppSettings["Domain"] != null)
                {
                    return ConfigurationManager.AppSettings["Domain"];
                }
                else
                {
                    return "";
                }
            }
        }

        /// <summary>
        /// PDF站点的地址
        /// </summary>
        public static string PDFDomain
        {
            get
            {
                if (ConfigurationManager.AppSettings["PDFDomain"] != null)
                {
                    return ConfigurationManager.AppSettings["PDFDomain"];
                }
                else
                {
                    return "";
                }
            }
        }
        /// <summary>
        ///WKhtmltopdf.exe的地址
        /// </summary>
        public static string WKhtmltopdfUrl
        {
            get
            {
                if (ConfigurationManager.AppSettings["WKhtmltopdfUrl"] != null)
                {
                    return ConfigurationManager.AppSettings["WKhtmltopdfUrl"];
                }
                else
                {
                    return "";
                }
            }
        }
        /// <summary>
        /// 邮件站点的地址
        /// </summary>
        public static string MailHostDomain
        {
            get
            {
                if (ConfigurationManager.AppSettings["MailHostDomain"] != null)
                {
                    return ConfigurationManager.AppSettings["MailHostDomain"];
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// 是否显示多余用户及组织
        /// </summary>
        public static string IsShowUO
        {
            get
            {
                if (ConfigurationManager.AppSettings["IsShowUO"] != null)
                {
                    return ConfigurationManager.AppSettings["IsShowUO"];
                }
                else
                {
                    return "1";
                }
            }
        }

        /// <summary>
        /// HOST站点的地址
        /// </summary>
        public static bool IsPublish
        {
            get
            {
                if (ConfigurationManager.AppSettings["IsPublish"] != null)
                {
                    return ConfigurationManager.AppSettings["IsPublish"].ToLower() == "true";
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// 每页大小
        /// </summary>
        public static int PageSize
        {
            get
            {
                if (ConfigurationManager.AppSettings["PageSize"] != null)
                {
                    return int.Parse(ConfigurationManager.AppSettings["PageSize"]);
                }
                else
                {
                    return 15;
                }
            }
        }

        /// <summary>
        /// 复制对象时候是否忽略重名判断
        /// </summary>
        public static bool IsIgnoreName
        {
            get
            {
                if (ConfigurationManager.AppSettings["IsIgnoreName"] != null)
                {
                    return ConfigurationManager.AppSettings["IsIgnoreName"].ToString() == "1";
                }
                else
                {
                    return true;
                }
            }
        }

     

        /// <summary>
        /// 客户服务代码
        /// </summary>
        public static string CustomerCode
        {
            get
            {
                if (ConfigurationManager.AppSettings["CustomerCode"] != null)
                {
                    return ConfigurationManager.AppSettings["CustomerCode"].ToString();
                }
                else
                {
                    return "";
                }
            }
        }

        /// <summary>
        /// 主控地址
        /// </summary>
        public static string MasterServerAddress
        {
            get
            {
                if (ConfigurationManager.AppSettings["MasterServerAddress"] != null)
                {
                    return ConfigurationManager.AppSettings["MasterServerAddress"].ToString();
                }
                else
                {
                    return "";
                }
            }
        }

        /// <summary>
        /// 登陆页地址
        /// </summary>
        public static string LoginUrl
        {
            get
            {
                if (ConfigurationManager.AppSettings["LoginUrl"] != null && ConfigurationManager.AppSettings["LoginUrl"].Trim() != "")
                {
                    return ConfigurationManager.AppSettings["LoginUrl"].ToString();
                }
                else
                {
                    return "/Account/Login";
                }
            }
        }

        public static string CustomerNameInPDF
        {
            get
            {
                if (ConfigurationManager.AppSettings["CustomerNameInPDF"] != null && ConfigurationManager.AppSettings["CustomerNameInPDF"].Trim() != "")
                {
                    return ConfigurationManager.AppSettings["CustomerNameInPDF"].ToString();
                }
                else
                {
                    return "";
                }
            }
        }

        /// <summary>
        /// 首页地址
        /// </summary>
        public static string IndexUrl
        {
            get
            {
                if (ConfigurationManager.AppSettings["IndexUrl"] != null && ConfigurationManager.AppSettings["IndexUrl"].Trim() != "")
                {
                    return ConfigurationManager.AppSettings["IndexUrl"].ToString();
                }
                else
                {
                    return "";
                }
            }
        }

        /// <summary>
        /// 首页地址
        /// </summary>
        public static string MenuJS
        {
            get
            {
                if (ConfigurationManager.AppSettings["MenuJS"] != null && ConfigurationManager.AppSettings["MenuJS"].Trim() != "")
                {
                    return ConfigurationManager.AppSettings["MenuJS"].ToString();
                }
                else
                {
                    return "SysMenu.js";
                }
            }
        }
        public static string MenuRight
        {
            get
            {
                if (ConfigurationManager.AppSettings["MenuRight"] != null && ConfigurationManager.AppSettings["MenuRight"].Trim() != "")
                {
                    return ConfigurationManager.AppSettings["MenuRight"].ToString();
                }
                else
                {
                    return "MenuRight";
                }
            }
        }
        
        /// <summary>
        /// ADName
        /// </summary>
        public static string ADName
        {
            get
            {
                if (ConfigurationManager.AppSettings["ADName"] != null)
                {
                    return ConfigurationManager.AppSettings["ADName"].ToString();
                }
                else
                {
                    return "";
                }
            }
        }

        /// <summary>
        /// LDAPName
        /// </summary>
        public static string LDAPName
        {
            get
            {
                if (ConfigurationManager.AppSettings["LDAPName"] != null)
                {
                    return ConfigurationManager.AppSettings["LDAPName"].ToString();
                }
                else
                {
                    return "";
                }
            }
        }

        public static string ExpireTime
        {
            get
            {
                if (ConfigurationManager.AppSettings["ExpireTime"] != null)
                {
                    return ConfigurationManager.AppSettings["ExpireTime"].ToString();
                }
                else
                {
                    return "2013-10-28";
                }
            }
        }

        /// <summary>
        /// XHaccountID
        /// </summary>
        public static string XHaccountID
        {
            get
            {
                if (ConfigurationManager.AppSettings["XHaccountID"] != null)
                {
                    return ConfigurationManager.AppSettings["XHaccountID"].ToString();
                }
                else
                {
                    return "";
                }
            }
        }
        /// <summary>
        /// XHenterpriseID
        /// </summary>
        public static string XHenterpriseID
        {
            get
            {
                if (ConfigurationManager.AppSettings["XHenterpriseID"] != null)
                {
                    return ConfigurationManager.AppSettings["XHenterpriseID"].ToString();
                }
                else
                {
                    return "";
                }
            }
        }
        /// <summary>
        /// XHPassword
        /// </summary>
        public static string XHPassword
        {
            get
            {
                if (ConfigurationManager.AppSettings["XHPassword"] != null)
                {
                    return ConfigurationManager.AppSettings["XHPassword"].ToString();
                }
                else
                {
                    return "";
                }
            }
        }

        /// <summary>
        /// SNADDomain
        /// </summary>
        public static string SNADDomain
        {
            get
            {
                if (ConfigurationManager.AppSettings["SNADDomain"] != null)
                {
                    return ConfigurationManager.AppSettings["SNADDomain"].ToString();
                }
                else
                {
                    return "21.226.103.84;sn-oa;oa.suning.com.cn;10.0.0.8;EWorker.suning.com.cn;localhost";
                }
            }
        }

        /// <summary>
        /// SenderClass
        /// </summary>
        public static string SenderClass
        {
            get
            {
                if (ConfigurationManager.AppSettings["SenderClass"] != null)
                {
                    return ConfigurationManager.AppSettings["SenderClass"].ToString();
                }
                else
                {
                    return "";
                }
            }
        }

        /// <summary>
        /// SNADDomain
        /// </summary>
        public static bool IsSendSms
        {
            get
            {
                if (ConfigurationManager.AppSettings["IsSendSms"] != null)
                {
                    return bool.Parse(ConfigurationManager.AppSettings["IsSendSms"].ToString());
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// IsBelongOneOrg
        /// </summary>
        public static bool IsBelongOneOrg
        {
            get
            {
                if (ConfigurationManager.AppSettings["IsBelongOneOrg"] != null)
                {
                    return bool.Parse(ConfigurationManager.AppSettings["IsBelongOneOrg"].ToString());
                }
                else
                {
                    return false;
                }
            }
        }

        #region 冻结
        /// <summary>
        /// 是否开启
        /// </summary>
        public static bool IsFreezeZone
        {
            get
            {
                if (ConfigurationManager.AppSettings["IsFreezeZone"] != null)
                {
                    return bool.Parse(ConfigurationManager.AppSettings["IsFreezeZone"].ToString());
                }
                else
                {
                    return true;
                }
            }
        
        }

        /// <summary>
        /// 开始
        /// </summary>
        public static int FreezeBegHour
        {
            get
            {
                if (ConfigurationManager.AppSettings["FreezeBegHour"] != null)
                {
                    return int.Parse(ConfigurationManager.AppSettings["FreezeBegHour"].ToString());
                }
                else
                {
                    return 0;
                }
            }

        }

        /// <summary>
        /// 结束
        /// </summary>
        public static int FreezeEndHour
        {
            get
            {
                if (ConfigurationManager.AppSettings["FreezeEndHour"] != null)
                {
                    return int.Parse(ConfigurationManager.AppSettings["FreezeEndHour"].ToString());
                }
                else
                {
                    return 17;
                }
            }

        }
        /// <summary>
        /// 间隔分钟
        /// </summary>
        public static int FreezePeriod
        {
            get
            {
                if (ConfigurationManager.AppSettings["FreezePeriod"] != null)
                {
                    return int.Parse(ConfigurationManager.AppSettings["FreezePeriod"].ToString());
                }
                else
                {
                    return 180;
                }
            }
        }

        /// <summary>
        /// 持续分钟
        /// </summary>
        public static int FreezeDuration
        {
            get
            {
                if (ConfigurationManager.AppSettings["FreezeBT"] != null)
                {
                    return int.Parse(ConfigurationManager.AppSettings["FreezeBT"].ToString());
                }
                else
                {
                    return 20;
                }
            }
        }
        #endregion

        /// <summary>
        /// SSSSOValidateUrl
        /// </summary>
        public static string SSSSOValidateUrl
        {
            get
            {
                if (ConfigurationManager.AppSettings["SSSSOValidateUrl"] != null)
                {
                    return ConfigurationManager.AppSettings["SSSSOValidateUrl"].ToString();
                }
                else
                {
                    return "http://27.151.122.65/seeyon/thirdparty.do?ticket=";
                }
            }
        }

        /// <summary>
        /// SSSSOLoginOutUrl
        /// </summary>
        public static string SSSSOLoginOutUrl
        {
            get
            {
                if (ConfigurationManager.AppSettings["SSSSOLoginOutUrl"] != null)
                {
                    return ConfigurationManager.AppSettings["SSSSOLoginOutUrl"].ToString();
                }
                else
                {
                    return "http://27.151.122.65/seeyon/thirdparty.do?method=logoutNotify&ticket=";
                }
            }
        }

        /// <summary>
        /// QXOrgUrl
        /// </summary>
        public static string QXOrgUrl
        {
            get
            {
                if (ConfigurationManager.AppSettings["QXOrgUrl"] != null)
                {
                    return ConfigurationManager.AppSettings["QXOrgUrl"].ToString();
                }
                else
                {
                    return "http://172.16.71.28/portal5.0/getOrg?type=deptInfo";
                }
            }
        }

        /// <summary>
        /// QXUserUrl
        /// </summary>
        public static string QXUserUrl
        {
            get
            {
                if (ConfigurationManager.AppSettings["QXUserUrl"] != null)
                {
                    return ConfigurationManager.AppSettings["QXUserUrl"].ToString();
                }
                else
                {
                    return "http://172.16.71.28/portal5.0/getOrg?type=userInfo";
                }
            }
        }
        /// <summary>
        /// QXOrgUrl
        /// </summary>
        public static string QXOrgXMLPath
        {
            get
            {
                if (ConfigurationManager.AppSettings["QXOrgXMLPath"] != null)
                {
                    return ConfigurationManager.AppSettings["QXOrgXMLPath"].ToString();
                }
                else
                {
                    return @"D:\yinhe\QXORGAndUserAd\org.xml";
                }
            }
        }
        /// <summary>
        /// QXOrgUrl
        /// </summary>
        public static string QXUserXMLPath
        {
            get
            {
                if (ConfigurationManager.AppSettings["QXUserXMLPath"] != null)
                {
                    return ConfigurationManager.AppSettings["QXUserXMLPath"].ToString();
                }
                else
                {
                    return @"D:\yinhe\QXORGAndUserAd\user.xml";
                }
            }
        }
        /// <summary>
        /// 侨鑫市调模块domain
        /// </summary>
        public static string QXSDDomain
        {
            get {
                if (ConfigurationManager.AppSettings["QXSDDomain"] != null)
                {
                    return ConfigurationManager.AppSettings["QXSDDomain"].ToString();
                }
                else
                {
                    return "http://172.16.62.11:8010";
                }
            }
        }
        /// <summary>
        /// 站点logo url
        /// </summary>
        public static string SiteLogoUrl
        {
            get
            {
                if (ConfigurationManager.AppSettings["SiteLogoUrl"] != null)
                {
                    return ConfigurationManager.AppSettings["SiteLogoUrl"].ToString();
                }
                else
                {
                    return "";
                }
            }
        }
        
        /// <summary>
        /// 经办部门签发步骤ID
        /// </summary>
        public static string SignStepId
        {
            get
            {
                if (ConfigurationManager.AppSettings["SignStepId"] != null)
                {
                    return ConfigurationManager.AppSettings["SignStepId"].ToString();
                }
                else
                {
                    return "";
                }
            }
        }
        /// <summary>
        /// 是否自动更新OA待办已办
        /// </summary>
        public static bool IsUpdateOAToDo
        {
            get
            {
                if (ConfigurationManager.AppSettings["IsUpdateOAToDo"] != null)
                {
                    return ConfigurationManager.AppSettings["IsUpdateOAToDo"].ToString().Trim() == "1";
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
