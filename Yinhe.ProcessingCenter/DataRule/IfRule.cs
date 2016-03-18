using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Yinhe.ProcessingCenter.DataRule
{
    /// <summary>
    /// 判断规则,继承基础操作规则
    /// </summary>
    public class IfRule : OperateRule
    {
        #region 属性
        /// <summary>
        /// 判断条件
        /// </summary>
        public ConditionRule Condition = null;

        /// <summary>
        /// 备注
        /// </summary>
        public string Remark = "";

        #endregion

        #region 构造
        /// <summary>
        /// 初始化内容构造
        /// </summary>
        /// <param name="entityElement"></param>
        public IfRule(XElement entityElement)
        {
            this.SetIfRule(entityElement);
        }
        #endregion

        #region 方法
        /// <summary>
        /// 根据XML获取对应的判断规则
        /// </summary>
        /// <param name="entityElement"></param>
        /// <returns></returns>
        public void SetIfRule(XElement entityElement)
        {
            this.Condition = new ConditionRule(entityElement.Attribute("Condition") != null ? entityElement.Attribute("Condition").Value : "");

            this.SetOperateRule(entityElement);
        }
        #endregion
    }
}
