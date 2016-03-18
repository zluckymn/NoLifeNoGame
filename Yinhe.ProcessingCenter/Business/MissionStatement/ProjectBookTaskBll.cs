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
    /// 分项任务书关联处理类
    /// </summary>
    public class ProjectBookTaskBll
    {
        #region 构造函数
        /// <summary>
        /// 类私有变量
        /// </summary>
        private DataOperation _ctx = null;

        /// <summary>
        /// 封闭当前默认构造函数
        /// </summary>
        private ProjectBookTaskBll()
        {
            _ctx = new DataOperation();
        }

        private ProjectBookTaskBll(DataOperation ctx)
        {
            _ctx = ctx;
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static ProjectBookTaskBll _()
        {
            return new ProjectBookTaskBll();
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static ProjectBookTaskBll _(DataOperation ctx)
        {
            return new ProjectBookTaskBll(ctx);
        }
        #endregion

        #region 查找
        /// <summary>
        /// 通过主键查找
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public BsonDocument FindById(int id)
        { 
            var hitObj = this._ctx.FindOneByKeyVal("ProjectBookTask", "projBookTaskId", id.ToString());
            return hitObj;
        }

        #endregion
    }
}
