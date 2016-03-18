using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Yinhe.ProcessingCenter.DataRule
{
    /// <summary>
    /// 循环规则,继承基础操作规则
    /// </summary>
    public class ForeachRule : OperateRule
    {
        #region 属性
        /// <summary>
        /// 循环对象的引用名称
        /// </summary>
        public string Name = "";

        /// <summary>
        /// 循环的对象
        /// </summary>
        public DataObject Object = null;

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
        public ForeachRule(XElement entityElement)
        {
            this.SetForeachRule(entityElement);
        }
        #endregion

        #region 方法
        /// <summary>
        /// 根据XML获取对应的循环规则
        /// </summary>
        /// <param name="entityElement"></param>
        /// <returns></returns>
        public void SetForeachRule(XElement entityElement)
        {
            this.Name = entityElement.Attribute("Name") != null ? entityElement.Attribute("Name").Value : "";
            this.Object = new DataObject(entityElement.Attribute("Object") != null ? entityElement.Attribute("Object").Value : "");

            this.SetOperateRule(entityElement);
        }

        #endregion
    }
}
