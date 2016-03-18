using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Yinhe.WebReference.Schdeuler;

namespace Yinhe.WebReference
{
    public class YinheSchdeuler
    {
        private static JobRegister register = new JobRegister();
        private static string YinheServiceClientUrl = System.Configuration.ConfigurationManager.AppSettings["YinheServiceClientUrl"].Trim();

        private static void Init()
        {
            register.Url = YinheServiceClientUrl;
        }

        /// <summary>
        /// 注册消息
        /// </summary>
        /// <param name="group">组名</param>
        /// <param name="jobName">作业名，无特别要求的使用Guid</param>
        /// <param name="sendTime">发送时间</param>
        /// <param name="msg">消息详细</param>
        /// <returns></returns>
        public static bool SendSysMessage(Group group, string jobName, DateTime sendTime, SystemMsg msg)
        {
            try
            {
                Init();
                register.SendSysMessageAsync(group, jobName, sendTime, msg);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        /// <summary>
        /// 注册任务提醒
        /// </summary>
        /// <param name="group">组名</param>
        /// <param name="jobName">作业名，无特别要求的使用Guid</param>
        /// <param name="cronEx">间隔时间表达式</param>
        /// <returns></returns>
        public static bool SendTaskNoticeOnce(Group group, string jobName, string cronEX)
        {
            try
            {
                Init();
                register.SendTaskNoticeOnceAsync(group, jobName, cronEX);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// 注册任务提醒
        /// </summary>
        /// <param name="group">组名</param>
        /// <param name="jobName">作业名，无特别要求的使用Guid</param>
        /// <param name="cronEx">间隔时间表达式</param>
        /// <returns></returns>
        public static bool DeleteDesignSupplier(Group group, string jobName, string cronEX)
        {
            try
            {
                Init();
                register.DeleteDesignSupplierAsync(group, jobName, cronEX);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }


        /// <summary>
        /// 注册任务提醒
        /// </summary>
        /// <param name="group">组名</param>
        /// <param name="jobName">作业名，无特别要求的使用Guid</param>
        /// <param name="cronEx">间隔时间表达式</param>
        /// <returns></returns>
        public static bool SendInstanceApproverNotice(Group group, string jobName, string cronEX)
        {
            try
            {
                Init();
                register.SendInstanceApproverNotice(group, jobName, cronEX);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// 删除已经注册的job
        /// </summary>
        /// <param name="group"></param>
        /// <param name="jobName"></param>
        public static bool RemoveJob(Group group, string jobName)
        {
            try
            {
                Init();
                register.RemoveJob(group, jobName);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
