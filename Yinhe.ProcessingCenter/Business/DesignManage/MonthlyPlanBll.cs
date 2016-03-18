using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using Yinhe.ProcessingCenter.DataRule;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using MongoDB.Driver;
namespace Yinhe.ProcessingCenter.DesignManage
{
    /// <summary>
    /// 月度计划处理类
    /// </summary>
    public class MonthlyPlanBll
    {
        #region 构造函数
        /// <summary>
        /// 类私有变量
        /// </summary>
        private DataOperation dataOp = null;

        private string tableName = "";
        /// <summary>
        /// 封闭当前默认构造函数
        /// </summary>
        private MonthlyPlanBll()
        {
            dataOp = new DataOperation();
        }

        private MonthlyPlanBll(DataOperation _dataOp)
        {
            dataOp = _dataOp;
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static MonthlyPlanBll _()
        {
            return new MonthlyPlanBll();
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static MonthlyPlanBll _(DataOperation _dataOp)
        {
            return new MonthlyPlanBll(_dataOp);
        }

         
        
         
        #endregion
        /// <summary>
        /// 赋值辅助计划
        /// </summary>
        /// <param name="copyId">对应对象Id</param>
        /// <param name="srcId">需要初始化的对象</param>
        /// <returns></returns>
       

        public CommonTaskStatus GetTaskStatusByBson(BsonDocument task,List<BsonDocument> designTasks, List<BsonDocument> sysTasks)
        {
            var taskstatus = CommonTaskStatus.NotStarted;
            switch (task.Int("nodeType"))
            {
                case (int)TaskNodeType.SelfDefinedTask:
                case (int)TaskNodeType.LastMonthUnfinishedTask://自定义与上个月未完成任务
                    Enum.TryParse(((MonthlyTaskStatus)task.Int("taskStatus")).ToString(), out taskstatus);
                    break;
                case (int)TaskNodeType.DesignTask://项目设计计划
                    var designTask = designTasks.Where(c => c.String("taskId") == task.String("taskId")).FirstOrDefault();
                    if (designTask != null)
                        Enum.TryParse(((TaskStatus)designTask.Int("status")).ToString(), out taskstatus);
                    break;
                case (int)TaskNodeType.SystemTask://系统设计计划
                    var sysTask = sysTasks.Where(c => c.String("systaskId") == task.String("systaskId")).FirstOrDefault();
                    if (sysTask != null)
                        Enum.TryParse(((SysTaskStatus)sysTask.Int("state")).ToString(), out taskstatus);
                    break;
            }
            return taskstatus;
        }

        /// <summary>
        /// 获取任务状态
        /// </summary>
        /// <returns></returns>
        public string GetTaskNodeStatus(BsonDocument task, List<BsonDocument> designTasks, List<BsonDocument> sysTasks)
        {
            var taskstatus = CommonTaskStatus.NotStarted;
            var curStatusName=EnumDescription.GetFieldText(taskstatus);
            switch (task.Int("nodeType"))
            {
                case (int)TaskNodeType.SelfDefinedTask:
                case (int)TaskNodeType.LastMonthUnfinishedTask://自定义与上个月未完成任务
                    curStatusName=  EnumDescription.GetFieldText((MonthlyTaskStatus)task.Int("taskStatus"));
                    break;
                case (int)TaskNodeType.DesignTask://项目设计计划
                    var designTask = designTasks.Where(c => c.String("taskId") == task.String("taskId")).FirstOrDefault();
                    if (designTask != null)
                    {
                        var curTaskStatus = designTask.Int("status", 2);
                        if (curTaskStatus < (int)TaskStatus.NotStarted)
                        {
                            curTaskStatus = (int)TaskStatus.NotStarted;
                        }
                        curStatusName = EnumDescription.GetFieldText((TaskStatus)curTaskStatus);
                        double delayDay = 0;
                        if (curTaskStatus < (int)TaskStatus.Completed && designTask.Date("curEndData") != DateTime.MinValue && designTask.Date("curEndData") < DateTime.Now)//延迟结束
                        {
                            curTaskStatus = -1;
                            curStatusName = "已延迟";
                            delayDay = (DateTime.Now - task.Date("curEndData")).TotalDays;
                        }
                    }
                     break;
                case (int)TaskNodeType.SystemTask://系统设计计划
                    var sysTask = sysTasks.Where(c => c.String("systaskId") == task.String("systaskId")).FirstOrDefault();
                    if (sysTask != null)
                      curStatusName=  EnumDescription.GetFieldText((SysTaskStatus)sysTask.Int("state"));
                    break;
             }
            return curStatusName;
         }


        public void GetWeekStartEndDate(int year, int month, int week, out DateTime weekStartDate, out DateTime weekEndDate)
        {
            weekEndDate = weekStartDate = DateTime.MinValue;
            if (week < 1 || week > 6)
                return;
            DateTime monthStart = new DateTime(year, month, 1);
            DateTime monthEnd = monthStart.AddMonths(1);
            int monthFirstDayOfWeek = GetDayOfWeek(monthStart.DayOfWeek);
            DateTime mondayOfMonthFirstWeek = monthStart.AddDays(1 - monthFirstDayOfWeek); //每月第一天所在的星期的星期一；
            weekEndDate = mondayOfMonthFirstWeek.AddDays(week * 7);
            weekStartDate = weekEndDate.AddDays(-7);
            if (weekEndDate > monthEnd)
                weekEndDate = monthEnd;
            if (weekStartDate < monthStart)
                weekStartDate = monthStart;
        }

        public int GetDayOfWeek(DayOfWeek date)
        {
            int day = (int)date;
            if (day == 0)
                day = 7;
            return day;
        }
    }
}
