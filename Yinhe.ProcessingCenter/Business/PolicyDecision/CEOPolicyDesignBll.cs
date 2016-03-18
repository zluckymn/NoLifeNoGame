using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Yinhe.ProcessingCenter.DataRule;
using MongoDB.Bson;
using Yinhe.ProcessingCenter;
using MongoDB.Driver.Builders;
using MongoDB.Driver;

namespace Yinhe.ProcessingCenter.Business.PolicyDecision
{
    /// <summary>
    /// CEO决策设计
    /// </summary>
    public class CEOPolicyDesignBll
    {
        #region 构造函数
        /// <summary>
        /// 类私有变量
        /// </summary>
        private readonly DataOperation _ctx = null;

        /// <summary>
        /// 封闭当前默认构造函数
        /// </summary>
        public CEOPolicyDesignBll()
        {
            _ctx = new DataOperation();
        }

        public CEOPolicyDesignBll(DataOperation ctx)
        {
            _ctx = ctx;
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static CEOPolicyDesignBll _()
        {
            return new CEOPolicyDesignBll();
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static CEOPolicyDesignBll _(DataOperation ctx)
        {
            return new CEOPolicyDesignBll(ctx);
        }
        #endregion
    }
}
