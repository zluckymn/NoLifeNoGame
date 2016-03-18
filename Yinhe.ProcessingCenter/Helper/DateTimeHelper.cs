using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yinhe.ProcessingCenter
{
    /// <summary>
    /// 时间处理类
    /// </summary>
    public class Season
    {
        public readonly Dictionary<int, int> monthSeasonDict = new Dictionary<int, int> { 
            { 1, 1} ,{2,1},{3,1},
            {4,2},{5,2},{6,2},
            {7,3},{8,3},{9,3},
            {10,4},{11,4},{12,4}
        };

        private DateTime _start;
        private DateTime _end;
        private int _seasonCount;
        public int monthCount { get; set; }
        private List<SeasonMonth> _seasonMonth = null;

        public List<SeasonMonth> SeasonMonth
        {
            get
            {
                return _seasonMonth;
            }
        }

        public Season(DateTime start,DateTime end)
        {
            _start = start;
            _end = end;
            _seasonMonth = new List<ProcessingCenter.SeasonMonth>();
            Init(_start,_end);
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        private void Init(DateTime start, DateTime end)
        {
            monthCount = DateTimeHelper.CalcMonthCount(start, end);
            int count = 0;
            int flag = 0;//标记最新记录的季度
            SeasonMonth seasonMonth;
            for (int i = 0; i < monthCount; i++)
            {
                int month = start.Month;
                int curSeason = monthSeasonDict[month];

                if (flag != curSeason)//如果当前月份所在的季度和最新纪录的不一样，则季度数加1
                {
                    count++;
                    flag = curSeason;//修改哨兵
                    
                    seasonMonth = new SeasonMonth { year = start.Year, seasonId = curSeason };
                    seasonMonth.months = new List<DateTime>();
                    seasonMonth.months.Add(start);//加入
                    _seasonMonth.Add(seasonMonth);
                }
                else
                {
                    SeasonMonth temp = _seasonMonth.Single(s => s.year == start.Year && s.seasonId == curSeason);
                    temp.months.Add(start);
                }

                start = start.AddMonths(1);
            }
            _seasonCount = count;
        }

        private void CalcSeasonMonth()
        {

        }

        /// <summary>
        /// 计算传入的两个时间存在几个季度
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public  int CalcSeasonCount(DateTime start, DateTime end)
        {
            int allMonths = DateTimeHelper.CalcMonthCount(start, end);
            int count = 1;
            int flag = monthSeasonDict[start.Month];//标记最新记录的季度
            for (int i = 0; i < allMonths; i++)
            {
                int month = start.Month;
                int curSeason = monthSeasonDict[month];
                if (flag != curSeason)//如果当前月份所在的季度和最新纪录的不一样，则季度数加1
                {
                    count++;
                    flag = curSeason;
                }
                start = start.AddMonths(1);
            }
            return count;
        }

        public List<string> GetSeasonNames()
        {
            List<string> seasonNames = new List<string>();
            for (int i = 0; i < _seasonCount; i++)
            {
                int month = _start.Month;
            }
            return seasonNames;
        }
    }

    /// <summary>
    /// 节点对应月份信息
    /// </summary>
    public sealed class SeasonMonth
    {
        public int year { get; set; }
        public int seasonId { get; set; }
        public string key
        {
            get
            {
                return string.Format("{0}-{1}", year, seasonId);
            }
        }
        public List<DateTime> months { get; set; }
    }
    /// <summary>
    /// 时间操作相关
    /// </summary>
    public abstract class DateTimeHelper
    {
        /// <summary>
        /// 给定开始和结束时间，遍历其所有月份，返回格式为：2013-07
        /// </summary>
        /// <param name="start">开始时间</param>
        /// <param name="end">结束时间</param>
        /// <returns></returns>
        public static List<string> GetMonths(DateTime start, DateTime end)
        {
            int allMonths = CalcMonthCount(start, end);
            List<string> months = new List<string>(allMonths);
            for (int i = 0; i < allMonths; i++)
            {
                string month = start.Month.ToString().PadLeft(2, '0');
                string colName = start.Year + "-" + month;

                start = start.AddMonths(1);
                months.Add(colName);
            }
            return months;
        }

        /// <summary>
        /// 计算两个时间月份数
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static int CalcMonthCount(DateTime start, DateTime end)
        {
            return (end.Year - start.Year) * 12 + (end.Month - start.Month) + 1;
        }

       

        /// <summary>
        /// 遍历年份
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static IEnumerable GetYears(DateTime start, DateTime end)
        {
            for (int i = start.Year; i <= end.Year; i++)
            {
                yield return i;
            }
        }

        /// <summary>
        /// 比较两个时间是否同年同月
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static bool CompareYM(string start, string end)
        {
            bool token = false;
            DateTime dt1, dt2;
            if (!string.IsNullOrEmpty(start) && !string.IsNullOrEmpty(end))
            {
                if (DateTime.TryParse(start, out dt1) && DateTime.TryParse(end, out dt2))
                {
                    if ((dt1.Year == dt2.Year) && (dt1.Month == dt2.Month))
                    {
                        token = true;
                    }
                }
            }
            return token;
        }

   

        /// <summary>
        /// 比较start是否小于等于end只比较月份
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static bool LTE(DateTime start, DateTime end)
        {
           

            if (end.Year > start.Year)
            {
                return true;
            }
            else if (end.Year == start.Year && end.Month >= start.Month)
            {
                return true;
            }
            else
            {
                return false;
            }
            
        }

        public static IEnumerable GetSeasons(DateTime start, DateTime end)
        {
            int allMonths = (end.Year - start.Year) * 12 + (end.Month - start.Month) + 1;
            for (int i = 0; i < allMonths; i++)
            {
                string month = start.Month.ToString().PadLeft(2, '0');
                string colName = start.Year + "-" + month;

                start = start.AddMonths(1);
                yield return colName;
            }
        }
    }
}
