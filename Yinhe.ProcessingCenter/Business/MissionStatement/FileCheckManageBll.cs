using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using Yinhe.ProcessingCenter.DataRule;
using MongoDB.Bson;
using Yinhe.ProcessingCenter;
namespace Yinhoo.Autolink.Business.MissionStatement
{
    /// <summary>
    /// 文件签入签出处理类
    /// </summary>
    public class FileCheckManageBll 
    {
        #region 构造函数
        /// <summary>
        /// 类私有变量
        /// </summary>
        private DataOperation _ctx = null;

        /// <summary>
        /// 封闭当前默认构造函数
        /// </summary>
        public FileCheckManageBll()
        {
            _ctx = new DataOperation();
        }

        /// <summary>
        /// 封闭当前默认构造函数
        /// </summary>
        private FileCheckManageBll(DataOperation ctx)
        {
            _ctx = ctx;
        }


        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static FileCheckManageBll _()
        {
            return new FileCheckManageBll();
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static FileCheckManageBll _(DataOperation ctx)
        {
            return new FileCheckManageBll(ctx);
        }
        #endregion


        #region 查询
        /// <summary>
        /// 判断是否可签入签出
        /// </summary>
        /// <param name="pageId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public InvokeResult UpdateCheckOutTime(int pageId,int userId)
        {
            return new InvokeResult() { Status=Status.Successful };
        }
        #endregion


    }
}
