using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using Yinhe.ProcessingCenter.DataRule;
using MongoDB.Bson;
///<summary>
///项目计划管理
///</summary>
namespace Yinhe.ProcessingCenter.DesignManage
{
    /// <summary>
    /// 脉络图处理相关类
    /// </summary>
    public class ContextDiagramBll
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
        private ContextDiagramBll() 
        {
            dataOp = new DataOperation();
        }

        private ContextDiagramBll(DataOperation _dataOp)
        {
            dataOp = _dataOp;
        }
 
        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static ContextDiagramBll _() 
        {
            return new ContextDiagramBll(); 
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static ContextDiagramBll _(DataOperation _dataOp)
        {
            return new ContextDiagramBll(_dataOp);
        }
        public bool IgnoreSameNode { get {return  SysAppConfig.IsIgnoreName;}}//模板载入的时候是否忽略名称相同节点,
        private int offSetDay = 0;//时间偏移量
        private int offSetMonth = 0;//时间偏移量
        private int offSetYear = 0;//时间偏移量

        private bool isIgnorePeople = false;//是否忽略载入人员
        private bool isIgnoreDate = false;//是否忽略载入时间
        private bool isIgnorePassed = false;//是否忽略跳过任务
        #region  属性
        /// <summary>
        /// 是否载入人员
        /// </summary>
        public   bool IsIgnorePeople
        {
            get
            {
                return isIgnorePeople;
            }
            set
            {
                isIgnorePeople = value;
            }
        }

        /// <summary>
        /// 是否载入人员
        /// </summary>
        public bool IsIgnoreDate
        {
            get
            {
                return isIgnoreDate;
            }
            set
            {
                isIgnoreDate = value;
            }
        }

        /// <summary>
        /// 是否忽略跳过任务
        /// </summary>
        public bool IsIgnorePassed
        {
            get
            {
                return isIgnorePassed;
            }
            set
            {
                isIgnorePassed = value;
            }
        }
      
        /// <summary>
        /// 任务时间偏移量
        /// </summary>
        public int OffSetDay
        {
            get
            {

                return offSetDay;
            }
            set
            {
                offSetDay = value;
            }
        }
        /// <summary>
        /// 任务时间偏移量
        /// </summary>
        public int OffSetMonth
        {
            get
            {

                return offSetMonth;
            }
            set
            {
                offSetMonth = value;
            }
        }
        /// <summary>
        /// 任务时间偏移量
        /// </summary>
        public int OffSetYear
        {
            get
            {

                return offSetYear;
            }
            set
            {
                offSetYear = value;
            }
        }

        #endregion

        #endregion
        #region  操作
        public InvokeResult Update(string tableName,List<BsonDocument>addList,Dictionary<BsonDocument,string> updateList) 
        {
            var result = new InvokeResult() {Status= Status.Successful };
               using (TransactionScope tran = new TransactionScope())
               {
                   if (addList.Count() > 0)
                   {
                     var insertResult= dataOp.QuickInsert(tableName, addList);
                     if (insertResult.Status != Status.Successful)
                     {
                         return insertResult;
                     }
                   }
                   if (updateList.Count() > 0)
                   {

                       var updateResult = dataOp.QuickUpdate(tableName, updateList);
                       if (updateResult.Status != Status.Successful)
                       {
                           return updateResult;
                       }
                   }
                 

                   tran.Complete();
               }
               return result;
        }
        #endregion
 
    }

}
