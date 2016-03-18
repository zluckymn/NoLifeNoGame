using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Yinhe.ProcessingCenter.DataRule
{
    /// <summary>
    /// 变量规则,定义级联操作中的变量
    /// </summary>
    public class VarRule
    {
        #region 属性
        /// <summary>
        /// 变量名称
        /// </summary>
        public string Name = "";

        /// <summary>
        /// 变量值
        /// </summary>
        public DataObject Value = null;

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
        public VarRule(XElement entityElement)
        {
            this.SetVarRule(entityElement);
        }
        #endregion

        #region 方法
        /// <summary>
        /// 根据XML获取对应的变量规则
        /// </summary>
        /// <param name="entityElement"></param>
        /// <returns></returns>
        public void SetVarRule(XElement entityElement)
        {
            this.Name = entityElement.Attribute("Name") != null ? entityElement.Attribute("Name").Value : "";
            this.Value = new DataObject(entityElement.Attribute("Value") != null ? entityElement.Attribute("Value").Value : "");
            this.Remark = entityElement.Attribute("Remark") != null ? entityElement.Attribute("Remark").Value : "";
        }

        #endregion
    }
}
