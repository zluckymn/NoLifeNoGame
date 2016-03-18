using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Yinhe.ProcessingCenter;
using MongoDB.Driver;
using MongoDB.Bson;
using Yinhe.ProcessingCenter.Common;

namespace Yinhe.ProcessingCenter.Permissions
{

  /// <summary>
  /// 系统冻结处理类
  /// </summary>
    public class FreezeStrategy
    {
        private static DateTime curBeginTime = DateTime.Now;
        private static bool isFreeze = false;
        private static int BeginMintue =0;
        /// <summary>
        /// 固定时间开启冻结模式
        /// </summary>
        /// <returns></returns>
        public static bool IsFreezeSystem()
        {
            if (SysAppConfig.CustomerCode == CustomerCode.SN && SysAppConfig.IsFreezeZone)
            {
                var curHour = DateTime.Now.Hour;//小时
                var curMintue = DateTime.Now.Minute;//时间
                if (curHour > SysAppConfig.FreezeBegHour && curHour <= SysAppConfig.FreezeEndHour)//进入间隔区域
                {
                    if ((curHour * 60 + SysAppConfig.FreezePeriod) % SysAppConfig.FreezePeriod == 0)
                    {
                        //随机有段时间不能用
                        if (isFreeze == false)//开始能用
                        {
                            isFreeze = true;
                            curBeginTime = DateTime.Now;
                            return true;
                        }
                        else
                        {
                            if ((DateTime.Now - curBeginTime).TotalMinutes <= SysAppConfig.FreezeDuration)//持续时间
                            {

                                return true;
                            }
                        }
                    }
                }
                isFreeze = false;
            }
            return false;
        }


        /// <summary>
        /// 随机时间开启冻结模式
        /// </summary>
        /// <returns></returns>
        public static bool IsRandomFreezeSystem()
        {
            if (SysAppConfig.CustomerCode == CustomerCode.SN && SysAppConfig.IsFreezeZone)
            {
                var curHour = DateTime.Now.Hour;//小时
                var curMintue = DateTime.Now.Minute;//时间
                if (curHour > SysAppConfig.FreezeBegHour && curHour <= SysAppConfig.FreezeEndHour)//进入间隔区域
                {
                    if (isFreeze && (DateTime.Now - curBeginTime).TotalMinutes <= SysAppConfig.FreezeDuration) return true;
                    if ((curHour * 60 + SysAppConfig.FreezePeriod) % SysAppConfig.FreezePeriod == 0)
                    {
                        //随机有段时间不能用
                        if (isFreeze == false)//开始能用
                        {
                            if (BeginMintue == 0)
                            {
                                var maxValue = Math.Abs(60 - SysAppConfig.FreezeDuration);
                                BeginMintue = new Random(1).Next(1, maxValue <= 1 ? 1 : maxValue);
                            }
                            if (curMintue >= BeginMintue)
                            {
                                isFreeze = true;
                                curBeginTime = DateTime.Now;
                                return true;
                            }
                        }
                        else
                        {
                            if ((DateTime.Now - curBeginTime).TotalMinutes <= SysAppConfig.FreezeDuration)//持续时间
                            {

                                return true;
                            }
                        }
                    }
                }
                isFreeze = false;
                BeginMintue = 0;
            }
            return false;
        }
    }
}
