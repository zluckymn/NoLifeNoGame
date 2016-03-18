using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

///<summary>
///A3规则相关
///</summary>
namespace Yinhe.ProcessingCenter.DataRule
{
    /// <summary>
    /// 级联规则,继承基础操作规则
    /// </summary>
    public class CascadeRule : OperateRule
    {
        #region 属性
        /// <summary>
        /// 级联触发事件类型
        /// </summary>
        public StorageType Event = StorageType.None;

        /// <summary>
        /// 级联触发时间时间
        /// </summary>
        public CascadeTimeType Time = CascadeTimeType.None;

        /// <summary>
        /// 触发条件
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
        public CascadeRule(XElement entityElement)
        {
            this.SetCascadeRule(entityElement);
        }
        #endregion

        #region 方法
        /// <summary>
        /// 根据XML获取对应级联规则
        /// </summary>
        /// <param name="entityElement"></param>
        /// <returns></returns>
        public void SetCascadeRule(XElement entityElement)
        {
            if (entityElement.Attribute("Event") != null)
            {
                switch (entityElement.Attribute("Event").Value)
                {
                    case "Insert": this.Event = StorageType.Insert; break;
                    case "Update": this.Event = StorageType.Update; break;
                    case "Delete": this.Event = StorageType.Delete; break;
                    default: this.Event = StorageType.None; break;
                }
            }

            if (entityElement.Attribute("Time") != null)
            {
                switch (entityElement.Attribute("Time").Value)
                {
                    case "Before": this.Time = CascadeTimeType.Before; break;
                    case "After": this.Time = CascadeTimeType.After; break;
                    default: this.Time = CascadeTimeType.None; break;
                }
            }

            this.Condition = new ConditionRule(entityElement.Attribute("Condition") != null ? entityElement.Attribute("Condition").Value : "");
            this.Remark = entityElement.Attribute("Remark") != null ? entityElement.Attribute("Remark").Value : "";

            this.SetOperateRule(entityElement);
        }

        #endregion
    }
}
