using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;

namespace Yinhe.ProcessingCenter.DataRule
{
    /// <summary>
    /// 规则条件处理类
    /// </summary>
    public class ConditionRule
    {
        #region 属性
        /// <summary>
        /// 条件字符串
        /// </summary>
        public string ConditionStr = "";

        /// <summary>
        /// 备注
        /// </summary>
        public string Remark = "";

        #endregion

        #region 构造
        /// <summary>
        /// 带条件字符串的构造函数
        /// </summary>
        /// <param name="conditionStr"></param>
        public ConditionRule(string conditionStr)
        {
            this.SetConditionRule(conditionStr);
        }
        #endregion

        #region 方法
        /// <summary>
        /// 设置对象值
        /// </summary>
        /// <param name="conditionStr"></param>
        public void SetConditionRule(string conditionStr)
        {
            this.ConditionStr = conditionStr;
        }

        /// <summary>
        /// 获取条件判断结构
        /// </summary>
        /// <param name="varList"></param>
        /// <param name="sourceDic"></param>
        /// <returns></returns>
        public bool GetResult(List<VarRule> varList, Dictionary<string, BsonDocument> sourceDic)
        {
            bool resultBool = true;

            return resultBool;
        }

        #endregion
    }
}
