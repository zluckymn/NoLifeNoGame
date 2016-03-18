using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public class VoiceTextDateTimeHelper
{
    public VoiceTextDateTimeHelper()
    {

    }
    /// <summary>
    /// 第一个有效的日期字符串
    /// </summary>
    public string validDateStr { get; set; }
    /// <summary>
    /// 第一个有效的时间字符串
    /// </summary>
    public string validTimeStr { get; set; }

    /// <summary>
    /// 获取用于提醒的时间，即时间一般都要往后延迟而非往前
    /// </summary>
    /// <param name="_needDealStr"></param>
    /// <returns></returns>
    public string GetDateTimeStrForAlert(string _needDealStr)
    {
        DateTime tempDate = DateTime.Now;
        var curDate = DateTime.Now;
        var hitDateStr = GetDateTimeStr(_needDealStr);
        if (DateTime.TryParse(hitDateStr, out tempDate))
        {

            if (tempDate < curDate)//小于当前时间
            {
                if ((tempDate - curDate).Days > 0)//代表日期不一致一致
                {
                    tempDate = DateTime.Parse(string.Format("{0}{1}", curDate.ToString("yyyy-MM-dd"), tempDate.ToString("HH:mm:ss")));
                }
                var resultDate = tempDate;
                //在这之前保证了TempDate CurDate 日期一致但时间不一致
                if (tempDate < curDate && tempDate.Hour < 12)
                {
                    var hitObjCount = ChineseTimeClsDic.Where(c => _needDealStr.Contains(c.name)).Count();
                    if (hitObjCount > 0)//此处表示经过”早上“的区域端的时间修正过后，时间还是小于当前时间，这表示需要延后到明天
                    {
                        tempDate = tempDate.AddDays(1);//如下午的时候说早上6点
                    }
                    else
                    {
                        tempDate = tempDate.AddHours(12);//尝试改成下午,因为可能语意中忘了提醒下午4点，二十直接下午四点
                    }

                }
                if (tempDate < curDate)//改了时间，也还是小与当前时间
                {
                    resultDate = resultDate.AddDays(1);//修改+12小时前的时间
                    return resultDate.ToString("yyyy-MM-dd HH:mm:ss");
                }


            }

            return tempDate.ToString("yyyy-MM-dd HH:mm:ss");
        }
        else
        {
            return string.Empty;
        }

    }

    public string GetDateTimeStr(string _needDealStr, string regexStr = "")
    {
        DateTime tempDate = DateTime.Now;
        //进行时间语意提前读取
        if (DateTimeAdvance(ref tempDate, _needDealStr))
        {
            return tempDate.ToString("yyyy-MM-dd HH:mm:ss");
        }
        var result = PickUpDateTimeStr(_needDealStr, regexStr);

        var needDefaultTime = false;//是否需要默认延迟
        if (!string.IsNullOrEmpty(result))
        {
            if (!DateTime.TryParse(result, out tempDate))
            {
                tempDate = DateTime.Now;
                needDefaultTime = true;
            }
        }
        else
        {
            tempDate = DateTime.Now;
            needDefaultTime = true;
        }

        if (needDefaultTime)
        {
            var curDate = DateTime.Now.AddHours(1);//默认一个小时
            var hitObjListCount = ChineseTimeClsDic.Where(c => _needDealStr.Contains(c.name)).Count();
            if (hitObjListCount > 0)
            {
                return string.Format("{0} {1}", curDate.ToString("yyyy-MM-dd"), ChineseTimeFix(_needDealStr));
            }
            else
            {
                return string.Format("{0} {1}", curDate.ToString("yyyy-MM-dd"), ChineseTimeFix(_needDealStr, curDate));
            }

        }
        else
        {
            return tempDate.ToString("yyyy-MM-dd HH:mm:ss");
        }


    }

    private string PickUpDateTimeStr(string _needDealStr, string regexStr = "")
    {
        try
        {

            //中文处理
            var needDealStr = DateChineseToNum(_needDealStr);
            ///冒号处理
            needDealStr = DateTimeHourseWhiteSpanFilterDeal(needDealStr);
            ///空格缩进
            needDealStr = DateTimeWhiteSpanFilterDeal(needDealStr);


            if (string.IsNullOrEmpty(regexStr))
            {
                regexStr = DateTimeFilter;
            }
            if (!string.IsNullOrEmpty(needDealStr) && !string.IsNullOrEmpty(regexStr))
            {
                Regex reg = new Regex(string.Format(@"{0}", regexStr));
                var matches = reg.Matches(needDealStr);
                foreach (var m in matches)
                {
                    if (!string.IsNullOrEmpty(m.ToString()))
                    {
                        //将时间转化为- - -模式
                        var curDateFormat = DateToFormat(m.ToString());
                        curDateFormat = DateTimeStrFix(curDateFormat);
                        if (!string.IsNullOrEmpty(curDateFormat))
                        {
                            DateTime date;
                            if (DateTime.TryParse(curDateFormat, out date))
                            {
                                if (string.IsNullOrEmpty(validDateStr))
                                {
                                    validDateStr = date.ToString(" yyyy-MM-dd ");
                                }


                                if (!curDateFormat.Contains(":") || date.Hour <= 0)
                                {
                                    //查看matches中是否有符合的时间
                                    if (!string.IsNullOrEmpty(validTimeStr))
                                    {
                                        validTimeStr = DateToFormat(validTimeStr.ToString());
                                        validTimeStr = TimeStrTryFix(validTimeStr);
                                        var tempCurDateFormat = string.Format("{0} {1}", date.ToString("yyyy-MM-dd"), validTimeStr);
                                        DateTime tempDate;
                                        if (DateTime.TryParse(tempCurDateFormat, out tempDate))
                                        {
                                            return string.Format("{0} {1}", date.ToString("yyyy-MM-dd"), ChineseTimeFix(_needDealStr, tempDate));
                                        }
                                    }
                                    return string.Format("{0} {1}", date.ToString("yyyy-MM-dd"), ChineseTimeFix(_needDealStr, date));
                                }
                                else
                                {
                                    return string.Format("{0} {1}", date.ToString("yyyy-MM-dd"), ChineseTimeFix(_needDealStr, date));//第一个匹配即返回
                                }

                            }
                            else
                            {
                                return string.Empty;
                            }
                        }

                    }

                }

            }
        }
        catch (InvalidCastException ex)
        { }
        catch (Exception ex)
        { }


        ///默认情况
        return string.Empty;
    }



    #region 语音字符串 语意日期提取 中文转化数字，零散时间补齐 正则时间提取
    private const string chineseWord = "零一二三四五六七八九1234567890";

    /// <summary>
    ///转化时间字符串钱空格过滤正则表达式，
    /// </summary>
    private const string dateTimeWhiteSpanFilter = @"(\w*[年,-,/])|(\w*[月,-,/])|(\w*[日,-,/, 号])|(\w*[:,点])|(\w*[:,分])|(\w*[秒])";
    //用于小时前面:的空格生成
    private const string dateTimeHourseWhiteSpanFilter = @"\s*((\d{1,2})|[一,二,三,四,五,六,七,八,九,十]{2,3})[:,点,时][\d{1,2}]*[:,分]?";
    /// <summary>
    /// 时间提取正则表达式
    /// </summary>
    private const string DateTimeFilter = @"(\d{2,4}[-,年,/,\.])?(\d{1,2}[-,/,月,\.])?(\d{1,2}[-,/,日,号]?)?\s*(\d{1,2}[:,点])?(\d{1,2}[:,分]?)?(\d{1,2})?|(\d{2,4}[-,年,/,\.])?(\d{1,2}[-,/,月,\.])?(\d{1,2}[-,/,日,号]?)?";

    private const string DateFilter = @"(\d{2,4}[-,年,/,\.])?(\d{1,2}[-,/,月,\.])?(\d{1,2}[-,/,日,号]?)?";

    private Dictionary<string, string> CNStrDic = new Dictionary<string, string>() {
        //{ "十", "1" } ,{ "二十", "2" },{ "三十", "3" },
        //{ "四十", "4" }, { "五十", "5" },{ "六十", "6" },
        { "零", "0" }, { "一", "1" }, { "二", "2" },{ "两", "2" }, { "三", "3" }, { "四", "4" },
            { "五", "5" }, { "六", "6" }, { "七", "7" }, { "八", "8" },
            { "九", "9" }, { "半小时后", "30分后" },{ "半年后", "6个月后" }
        };



    /// <summary>
    /// 几十几 十几 过滤
    /// </summary>
    private List<CTNCls> CTNStrList = new List<CTNCls>() {
      
        //只有整十 ，二十 三十 ....
        new CTNCls() { key="十",ruler=string.Format("[{0}]十[^{0}]",chineseWord),value="0"},
        //只有N十N ，二十一 二十二 ....
        new CTNCls() { key="十",ruler=string.Format("[{0}]十[{0}]",chineseWord),value=""},
        //只有十N ，十一 十二 ....
        new CTNCls() { key="十",ruler=string.Format("[^{0}]?十[{0}]",chineseWord),value="1"},
        // 只有一个十
        new CTNCls() { key="十",ruler=string.Format("[^{0}]?十[^{0}]?",chineseWord),value="10"},

        new CTNCls() { key="半",ruler=string.Format("[{0}]点半",chineseWord),value="30"},

         };

    private class CTNCls
    {
        public string key { get; set; }
        public string value { get; set; }
        public string ruler { get; set; }
    }

    /// <summary>
    /// 时间格式化
    /// </summary>
    private Dictionary<string, string> CNDateStrDic = new Dictionary<string, string>() {
            { "号", "日" },
            { "点", ":" }, { "时", ":" },{ "分", ":" },{ "秒", "" },

        };
    private string[] DateStrFormat = new string[] { "年", "月", "日", "-", "/", "." };

    /// <summary>
    /// 正则过滤十
    /// </summary>
    /// <param name="needDealStr"></param>
    /// <returns></returns>
    private string ReplaceTenStr(string needDealStr)
    {

        foreach (var dic in CTNStrList)
        {
            Regex reg = new Regex(string.Format(@"{0}", dic.ruler));
            foreach (var result in reg.Matches(needDealStr))
            {
                var otherResult = result.ToString().Replace(dic.key, dic.value);
                needDealStr = needDealStr.Replace(result.ToString(), otherResult);
            }

        }
        return needDealStr;

    }

    /// <summary>
    /// 转换时间为可以直接parse的时间
    /// </summary>
    /// <param name="dateStr"></param>
    /// <returns></returns>
    private string ReplaceDateStr(Dictionary<string, string> replaceDic, string needDealStr)
    {

        needDealStr = ReplaceTenStr(needDealStr);//整十过滤
        foreach (var dic in replaceDic)
        {
            if (needDealStr.Contains(dic.Key))
            {
                needDealStr = needDealStr.Replace(dic.Key, dic.Value);
            }

        }

        return needDealStr.Trim('-').Trim(':');
    }
    /// <summary>
    /// 时间格式化
    /// </summary>
    /// <param name="needDealStr"></param>
    /// <returns></returns>
    public string DateToFormat(string needDealStr)
    {
        //最后返回可能有：
        return ReplaceDateStr(CNDateStrDic, needDealStr).Trim(':');
    }

    /// <summary>
    /// 中文数字格式化
    /// </summary>
    /// <param name="needDealStr"></param>
    /// <returns></returns>
    public string DateChineseToNum(string needDealStr)
    {
        needDealStr = ChineseDateFix(needDealStr);
        //  ReplaceDateStr(CNStrDic, needDealStr);
        return ReplaceDateStr(CNStrDic, needDealStr);
    }


    /// <summary>
    /// 转化时间前进行冒号空格处理，进行空格缩进 冒号前置空格
    /// </summary>
    /// <param name="needDealStr"></param>
    /// <param name="regexStr"></param>
    /// <returns></returns>
    public string DateTimeWhiteSpanFilterDeal(string needDealStr)
    {
        //if (!string.IsNullOrEmpty(needDealStr) && !string.IsNullOrEmpty(dateTimeWhiteSpanFilter))
        //{
        //    Regex reg = new Regex(string.Format(@"{0}", dateTimeWhiteSpanFilter));
        //    var match = reg.Matches(needDealStr);
        //    foreach (var m in match)
        //    {
        //        var temp = m.ToString().Trim().Replace(" ","");
        //        needDealStr = needDealStr.Replace(m.ToString(), temp);
        //    }
        //    return needDealStr;
        //}
        //else
        //{
        //    return needDealStr;
        //}
        return needDealStr.Replace(" ", "").Replace("|Y|", " ");

    }

    /// <summary>
    ///  冒号前置空格
    /// </summary>
    /// <param name="needDealStr"></param>
    /// <param name="regexStr"></param>
    /// <returns></returns>
    public string DateTimeHourseWhiteSpanFilterDeal(string needDealStr, string splitStr = "")
    {
        if (string.IsNullOrEmpty(splitStr))
        {
            splitStr = "|Y|";
        }
        if (!string.IsNullOrEmpty(needDealStr) && !string.IsNullOrEmpty(dateTimeHourseWhiteSpanFilter))
        {
            Regex reg = new Regex(string.Format(@"{0}", dateTimeHourseWhiteSpanFilter));
            var match = reg.Matches(needDealStr);
            foreach (var m in match)
            {
                var temp = splitStr + m.ToString().Trim();
                needDealStr = needDealStr.Replace(m.ToString(), temp);
                if (string.IsNullOrEmpty(validTimeStr))
                {
                    validTimeStr = m.ToString();
                }
            }
            return needDealStr;
        }
        else
        {
            return needDealStr;
        }
    }



    /// <summary>
    /// 将残缺的日期进行修正展示
    /// </summary>
    /// <param name="dtStr"></param>
    /// <returns></returns>
    private string DateTimeStrFix(string dtStr)
    {
        // 传入的对象需要处理11年1日 12:20（可用）日期与时间拆分
        // 传入的对象需要处理        12:20（可用）日期与时间拆分
        // 传入的对象需要处理11年1日 12 （不可用）日期与时间拆分,需要加载对应的值
        // 10号只有该字段可能导致DateTime.TryParse转化为2015/1月10号
        try
        {
            DateTime date;
            Regex reg = new Regex(@"^\d{1,2}[号,日]\s*(\d{1,2}[:,点]?)?");//不足月份
            if (reg.IsMatch(dtStr))
            {
                dtStr = string.Format("{0}月{1}", DateTime.Now.Month, dtStr);
            }

            if (DateTime.TryParse(dtStr, out date))//转化成功
            {
                return date.ToString();
            }
            else//转化失败
            {
                var curNewDTStr = string.Empty;
                var DateStrArray = dtStr.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (DateStrArray.Count() >= 2)
                {
                    var dateStr = DateStrArray[0];
                    if (!string.IsNullOrEmpty(dateStr))
                    {
                        curNewDTStr += DateStrTryFix(dateStr);

                    }
                    var timeStr = DateStrArray[1];
                    if (!string.IsNullOrEmpty(timeStr))
                    {
                        curNewDTStr += string.Format(" {0}", TimeStrTryFix(timeStr));
                    }
                    if (DateTime.TryParse(curNewDTStr, out date))//转化成功
                    {
                        return date.ToString();
                    }
                }
                else if (DateStrArray.Count() == 1)//单个需要翻译为 10点小事
                {
                    if (DateStrFormat.Any(c => dtStr.Contains(c)))
                    {
                        curNewDTStr += DateStrTryFix(dtStr);
                    }
                    else
                    {
                        curNewDTStr += TimeStrTryFix(dtStr);

                    }
                    if (DateTime.TryParse(curNewDTStr, out date))//转化成功
                    {
                        return date.ToString();
                    }

                }


                return string.Empty;
            }
        }
        catch (Exception ex)
        {
            return string.Empty;
        }

    }
    /// <summary>
    /// 尝试日期修复 2010-10-10  10-10 10 
    /// </summary>
    /// <returns></returns>
    private string DateStrTryFix(string dateStr)
    {
        DateTime tempDate;
        //只提取日期后续字母过滤此处有月份有日 8月21日6的情况多个六
        var newReg = new Regex(DateFilter);
        var result = newReg.Match(dateStr);//只提取日期
        if (result != null)
        {
            dateStr = result.ToString();
        }
        //Regex reg = new Regex(@"^\d{1,2}[号,日]$");
        Regex reg = new Regex(@"^\d{1,2}[号,日]");//没有月份 可能导致转化的时候出现默认为1月份
        if (reg.IsMatch(dateStr))
        {
            dateStr = string.Format("{ 0}月{1}", DateTime.Now.Month, dateStr.ToString());
        }



        if (DateTime.TryParse(dateStr, out tempDate))//转化成功 17号
        {
            return tempDate.ToString("yyyy-MM-dd");//返回正确格式测试用1.1 12:20
        }
        else//失败一般为 如下10-10系统无法识别需要补齐,原则为由后往前补齐 15-12 需要识别为年而非月日
        {
            var temDateStr = dateStr;
            foreach (var format in DateStrFormat.Where(c => c != "-"))
            {
                temDateStr = temDateStr.Replace(format, "-");
            }
            var splitArray = temDateStr.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
            if (splitArray.Count() == 3)//年月日
            {
                var fullDateStr = string.Format("{0}-{1}-{2}", splitArray[0], splitArray[1], splitArray[2]);
                if (DateTime.TryParse(fullDateStr, out tempDate))//转化成功
                {
                    return tempDate.ToString("yyyy-MM-dd");//返回正确格式测试用1.1 12:20
                }
            }
            else if (splitArray.Count() == 2)//可能出错因为年2-4位 月12 日30最高
            {
                try
                {
                    //年月 //月日 此处只考虑中国情况按循序年月日
                    var firstNum = int.Parse(splitArray[0]);
                    var secdNum = int.Parse(splitArray[1]);
                    var MonthDayStr = string.Format("{0}-{1}-{2}", DateTime.Now.Year, firstNum, secdNum);

                    if (firstNum <= 12 && DateTime.TryParse(MonthDayStr, out tempDate))//月日优先
                    {
                        return tempDate.ToString("yyyy-MM-dd");//返回正确格式测试用1.1 12:20
                    }
                    else//年月
                    {
                        MonthDayStr = string.Format("{0}-{1}-{2}", firstNum, secdNum, DateTime.Now.Day);
                        if (DateTime.TryParse(MonthDayStr, out tempDate))
                        {
                            return tempDate.ToString("yyyy-MM-dd");//返回正确格式测试用1.1 12:20
                        }
                    }
                }
                catch (InvalidCastException ex)
                {
                    return string.Empty;
                }
                catch (Exception ex)
                {
                    return string.Empty;
                }

            }

            return string.Empty;
        }

    }

    /// <summary>
    /// 尝试日期修复 12点需要转化成12:00
    /// </summary>
    /// <returns></returns>
    private string TimeStrTryFix(string timeStr)
    {
        DateTime tempDate;
        if (DateTime.TryParse(timeStr, out tempDate))//转化成功
        {
            return tempDate.ToString("HH:mm:ss");//返回正确格式测试用1.1 12:20
        }
        else//失败一般为单个 补齐即可
        {
            if (!timeStr.Contains(":"))
            {
                timeStr = string.Format("{0}:00", timeStr);
                if (DateTime.TryParse(timeStr, out tempDate))//转化成功
                {
                    return tempDate.ToString("HH:mm:ss");//返回正确格式测试用1.1 12:20
                }
            }

            return string.Empty;
        }

    }

    #endregion

    #region 中文语意时间替换 早上 中午 下午 晚上 凌晨 提前N天 早N天 N天后
    //1-5点是凌晨，
    //5-8点是早晨，
    //9-13点是中午，
    //14-18点是下午，
    //19-24点是晚上。
    /// <summary>
    /// 获取周一至周五对应天数
    /// </summary>
    /// <param name="week"></param>
    /// <param name="now"></param>
    /// <returns></returns>
    public string CurWeekDay(DayOfWeek week, DateTime now, int Round)
    {
        var curWeekDay = DateTime.Parse(string.Format("{0}-{1}-{2}", now.Year, now.Month, now.Day));

        if (Round == 0 && (int)week >= (int)now.DayOfWeek)
        {
            var daySpan = Math.Abs((int)now.DayOfWeek - (int)week);
            if (daySpan > 0)
            {
                curWeekDay = curWeekDay.AddDays(daySpan);
            }
        }
        else
        {
            var curWeekValue = (int)now.DayOfWeek;
            if (now.DayOfWeek == DayOfWeek.Sunday)
            {
                curWeekValue = 7;
            }
            var daySpan = ((int)week + 7 * Round) - curWeekValue;
            curWeekDay = curWeekDay.AddDays(daySpan);
        }
        //用于alret使用时间提前 周一可能获取到这周一当前时间
        if (curWeekDay < DateTime.Now)
        {
            return CurWeekDay(week, now, ++Round);
        }
        return curWeekDay.ToString(" yyyy-MM-dd ");
    }
    /// <summary>
    /// 中文语意替换
    /// </summary>
    /// <returns></returns>
    public string ChineseDateFix(string dealStrl)
    {
        var curDateTime = DateTime.Now;
        var year = curDateTime.Year;
        var month = curDateTime.Month;
        var day = curDateTime.Day;

        //中文反转,用于防止出现周1的情况
        foreach (var dic in CNStrDic)
        {
            dealStrl = dealStrl.Replace("周" + dic.Value, "周" + dic.Key);
        }
        //周一至周末日数统计

        Dictionary<string, string> ChineseTimeStrDic = new Dictionary<string, string>()
            {
                 { "今年",string.Format("{0}年",year) }, { "本年",string.Format("{0}年",year) }, { "该年",string.Format("{0}年",year) },{ "这一年",string.Format("{0}年",year) },
                 { "下年",string.Format("{0}年",year+1) }, { "明年",string.Format("{0}年",year+1) }, { "下一年",string.Format("{0}年",year+1) },
                 { "后年",string.Format("{0}年",year+2) }, { "后一年",string.Format("{0}年",year+2) }, { "大后年",string.Format("{0}年",year+3) },
                 { "这个月",string.Format("{0}月",month) }, { "本月",string.Format("{0}月",month) }, { "该月",string.Format("{0}月",month) },
                 { "下个月",string.Format("{0}月",month+1) }, { "下月",string.Format("{0}月",month+1) },{ "下一月",string.Format("{0}月",month+1) },
                 { "今天",string.Format("{0}日",day) },  { "今早",string.Format("{0}日",day) }, { "今晚",string.Format("{0}日",day) },
                 { "明天",string.Format("{0}日",day+1) },  { "明早",string.Format("{0}日",day+1) }, { "明晚",string.Format("{0}日",day+1) },
                 { "大后天",string.Format("{0}日",day+3) },{ "后天",string.Format("{0}日",day+2) },
                  {"星期","周" },
                 { "下下周一",string.Format("{0}", CurWeekDay(DayOfWeek.Monday,curDateTime,2))},{ "下周一",string.Format("{0}", CurWeekDay(DayOfWeek.Monday,curDateTime,1))},{ "周一",string.Format("{0}", CurWeekDay(DayOfWeek.Monday,curDateTime,0))},
                 { "下下周二",string.Format("{0}", CurWeekDay(DayOfWeek.Tuesday,curDateTime,2))},{ "下周二",string.Format("{0}", CurWeekDay(DayOfWeek.Tuesday,curDateTime,1))},{ "周二",string.Format("{0}", CurWeekDay(DayOfWeek.Tuesday,curDateTime,0))},
                 { "下下周三",string.Format("{0}", CurWeekDay(DayOfWeek.Wednesday,curDateTime,2))},{ "下周三",string.Format("{0}", CurWeekDay(DayOfWeek.Wednesday,curDateTime,1))},{ "周三",string.Format("{0}", CurWeekDay(DayOfWeek.Wednesday,curDateTime,0))},
                 { "下下周四",string.Format("{0}", CurWeekDay(DayOfWeek.Thursday,curDateTime,2))},{ "下周四",string.Format("{0}", CurWeekDay(DayOfWeek.Tuesday,curDateTime,1))},{ "周四",string.Format("{0}", CurWeekDay(DayOfWeek.Tuesday,curDateTime,0))},
                 { "下下周五",string.Format("{0}", CurWeekDay(DayOfWeek.Friday,curDateTime,2))},{ "下周五",string.Format("{0}", CurWeekDay(DayOfWeek.Friday,curDateTime,1))},{ "周五",string.Format("{0}", CurWeekDay(DayOfWeek.Friday,curDateTime,0))},
                 { "下下周六",string.Format("{0}", CurWeekDay(DayOfWeek.Saturday,curDateTime,2))},{ "下周六",string.Format("{0}", CurWeekDay(DayOfWeek.Saturday,curDateTime,1))},{ "周六",string.Format("{0}", CurWeekDay(DayOfWeek.Saturday,curDateTime,0))},
                 { "下下周日",string.Format("{0}", CurWeekDay(DayOfWeek.Sunday,curDateTime,3))},{ "下周日",string.Format("{0}", CurWeekDay(DayOfWeek.Sunday,curDateTime,2))},{ "周日",string.Format("{0}", CurWeekDay(DayOfWeek.Sunday,curDateTime,1))},

            };
        return ReplaceDateStr(ChineseTimeStrDic, dealStrl);



    }

    List<ChineseTimeCls> ChineseTimeClsDic = new List<ChineseTimeCls>() {
        new ChineseTimeCls() {  name="凌晨",minHour=1,maxHour=5},
        new ChineseTimeCls() {  name="早晨",minHour=5,maxHour=12},
          new ChineseTimeCls(){ name="早上",minHour=6,maxHour=12},
        new ChineseTimeCls() {  name="中午",minHour=12,maxHour=13},
        new ChineseTimeCls() {  name="下午",minHour=14,maxHour=18},
        new ChineseTimeCls() {  name="傍晚",minHour=16,maxHour=18},
        new ChineseTimeCls() {  name="晚上",minHour=19,maxHour=24},
        new ChineseTimeCls() {  name="下班",minHour=18,maxHour=20},
        new ChineseTimeCls() {  name="上班",minHour=9,maxHour=20},
    };

    /// <summary>
    /// 模糊时间获取 下午提醒我干嘛，明天早上提醒我 主要用来控制小时分钟,
    /// </summary>
    /// <param name="dealStrl"></param>
    /// <returns></returns>
    public string ChineseTimeFix(string dealStrl)
    {
        //中文反转,用于防止出现周1的情况
        foreach (var dic in CNStrDic)
        {
            dealStrl = dealStrl.Replace(dic.Value, dic.Key);
        }
        var hitObjList = ChineseTimeClsDic.Where(c => dealStrl.Contains(c.name)).ToList();
        //if (tempDate < curDate && tempDate.Hour < 12)
        var curHour = DateTime.Now.Hour;
        foreach (var ctc in hitObjList)
        {

            var rand = new Random();
            var hour = 0;
            if (ctc.maxHour < curHour)
            {
                hour = rand.Next(ctc.minHour, ctc.maxHour);
            }
            else
            {
                var minHour = ctc.minHour > curHour ? ctc.minHour : curHour;
                hour = rand.Next(minHour, ctc.maxHour);
            }

            var minRand = new Random().Next(0, 59);
            return string.Format(" {0}:{1}:00 ", hour, minRand);
        }
        ///默认早上执行
        return string.Format(" 9:00:00 ");
    }

    /// <summary>
    /// 模糊时间获取 下午提醒我干嘛，明天早上提醒我 主要用来控制小时分钟,主要用来修复晚上八点 为20：00
    /// </summary>
    /// <param name="dealStrl"></param>
    /// <param name="date"></param>
    /// <param name="isAlertMode">提醒模式会自动</param>
    /// <returns></returns>
    public string ChineseTimeFix(string dealStrl, DateTime date)
    {
        if (date.Hour == 0 && string.IsNullOrEmpty(validTimeStr))
        {
            return ChineseTimeFix(dealStrl);
        }
        var hitObjList = ChineseTimeClsDic.Where(c => dealStrl.Contains(c.name)).ToList();
        foreach (var ctc in hitObjList)
        {
            if (date.Hour < ctc.minHour)//少于区域段
            {
                return string.Format(" {0}:{1}:{2} ", date.Hour + 12, date.Minute, date.Second);

            }
            if (date.Hour > ctc.maxHour)
            {

                return string.Format(" {0}:{1}:{2} ", date.Hour - 12, date.Minute, date.Second);
            }

        }

        ///默认早上执行
        return date.ToString(" HH:mm:ss ");
    }

    #endregion

    #region 提前 延迟  时间 提取 时间提取


    /// <summary>
    /// 提前3天 延迟3天 30分钟后 3天后 一个月后 一年后 
    /// </summary>
    private List<DateTimeAdvanceCls> DateTimeAdvanceList = new List<DateTimeAdvanceCls>() {
        //只有一个十
        new DateTimeAdvanceCls() { ruler=string.Format(@"\d*秒之?[后,候]"),type=1},
        new DateTimeAdvanceCls() { ruler=string.Format(@"\d*分之?[后,候]"),type=2},
        new DateTimeAdvanceCls() { ruler=string.Format(@"\d*分钟之?[后,候]"),type=2},
        new DateTimeAdvanceCls() { ruler=string.Format(@"\d*个?小时之?[后,候]"),type=3},
        new DateTimeAdvanceCls() { ruler=string.Format(@"\d*时之?[后,候]"),type=3},
        new DateTimeAdvanceCls() { ruler=string.Format(@"\d*天之?[后,候]"),type=4},
        new DateTimeAdvanceCls() { ruler=string.Format(@"\d*月之?[后,候]"),type=5},
        new DateTimeAdvanceCls() { ruler=string.Format(@"\d*个月之?[后,候]"),type=5},
        new DateTimeAdvanceCls() { ruler=string.Format(@"\d*年之?[后,候]"),type=6},
         };


    /// <summary>
    /// 延迟时间提取
    /// </summary>
    /// <param name="needDealStr"></param>
    /// <returns></returns>
    private bool DateTimeAdvance(ref DateTime curDate, string needDealStr)
    {
        needDealStr = DateChineseToNum(needDealStr);//语意替换
        foreach (var dic in DateTimeAdvanceList)
        {
            Regex reg = new Regex(string.Format(@"{0}", dic.ruler));
            foreach (var result in reg.Matches(needDealStr))
            {
                var numReg = new Regex(@"\d*");
                var hitNumStr = numReg.Match(result.ToString());
                int parseNum = 0;
                if (int.TryParse(hitNumStr.ToString(), out parseNum))
                {
                    switch (dic.type)
                    {
                        case 1: curDate = curDate.AddSeconds(parseNum); break;
                        case 2: curDate = curDate.AddMinutes(parseNum); break;
                        case 3: curDate = curDate.AddHours(parseNum); break;
                        case 4: curDate = curDate.AddDays(parseNum); break;
                        case 5: curDate = curDate.AddMonths(parseNum); break;
                        case 6: curDate = curDate.AddYears(parseNum); break;
                        default: break;

                    }
                    return true;//第一个转化成功就退出
                }

            }

        }
        return false;

    }
    #endregion


    private class DateTimeAdvanceCls
    {
        /// <summary>
        /// 规则
        /// </summary>  
        public string ruler { get; set; }


        /// <summary>
        /// 类型
        /// </summary>
        public int type { get; set; }

    }

    private class ChineseTimeCls
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// 最大
        /// </summary>
        public int minHour { get; set; }

        /// <summary>
        /// 最小
        /// </summary>
        public int maxHour { get; set; }
    }
}
