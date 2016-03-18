using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Yinhe.ProcessingCenter.DataRule
{
    /// <summary>
    /// 字段规则
    /// </summary>
    public class ColumnRule
    {
        #region 属性
        /// <summary>
        /// 字段间的唯一标示
        /// </summary>
        public string Name = "";

        /// <summary>
        /// 字段类型
        /// </summary>
        public string Type = "";

        /// <summary>
        /// 是否主键
        /// </summary>
        public bool IsPrimary = false;

        /// <summary>
        /// 是否自增
        /// </summary>
        public bool IsIdentity = false;

        /// <summary>
        /// 外键源表
        /// </summary>
        public string SourceTable = "";

        /// <summary>
        /// 外键源字段
        /// </summary>
        public string SourceColumn = "";

        /// <summary>
        /// 值,字段有的默认值或设置值
        /// </summary>
        public string Value = "";

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
        public ColumnRule(XElement entityElement)
        {
            this.SetColumnRule(entityElement);
        }
        #endregion

        #region 方法
        /// <summary>
        /// 根据XML获取对应字段实体
        /// </summary>
        /// <param name="entityElement"></param>
        /// <returns></returns>
        public void SetColumnRule(XElement entityElement)
        {
            this.Name = entityElement.Attribute("Name") != null ? entityElement.Attribute("Name").Value : "";
            this.Type = entityElement.Attribute("Type") != null ? entityElement.Attribute("Type").Value : "";
            this.IsIdentity = (entityElement.Attribute("IsIdentity") != null && entityElement.Attribute("IsIdentity").Value == "true") ? true : false;
            this.IsPrimary = (entityElement.Attribute("IsPrimary") != null && entityElement.Attribute("IsPrimary").Value == "true") ? true : false;
            this.SourceTable = entityElement.Attribute("SourceTable") != null ? entityElement.Attribute("SourceTable").Value : "";
            this.SourceColumn = entityElement.Attribute("SourceColumn") != null ? entityElement.Attribute("SourceColumn").Value : "";
            this.Value = entityElement.Attribute("Value") != null ? entityElement.Attribute("Value").Value : "";
            this.Remark = entityElement.Attribute("Remark") != null ? entityElement.Attribute("Remark").Value : "";
        }

        #endregion
    }
}
