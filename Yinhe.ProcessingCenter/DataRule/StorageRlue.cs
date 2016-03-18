using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Yinhe.ProcessingCenter.DataRule
{
    /// <summary>
    /// 存储规则,定义级联操作中的数据存储
    /// </summary>
    public class StorageRlue
    {
        #region 属性
        /// <summary>
        /// 操作类型
        /// </summary>
        public StorageType Type = StorageType.None;

        /// <summary>
        /// 操作数据
        /// </summary>
        public DataObject Data = null;

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
        public StorageRlue(XElement entityElement)
        {
            this.SetStorageRlue(entityElement);
        }
        #endregion

        #region 方法
        /// <summary>
        /// 根据XML获取对应插入规则
        /// </summary>
        /// <param name="entityElement"></param>
        /// <returns></returns>
        public void SetStorageRlue(XElement entityElement)
        {
            if (entityElement.Attribute("Type") != null)
            {
                switch (entityElement.Attribute("Type").Value)
                {
                    case "Insert": this.Type = StorageType.Insert; break;
                    case "Update": this.Type = StorageType.Update; break;
                    case "Delete": this.Type = StorageType.Delete; break;
                    default: this.Type = StorageType.Insert; break;
                }
            }

            this.Data = new DataObject(entityElement.Attribute("Data") != null ? entityElement.Attribute("Data").Value : "");
            this.Remark = entityElement.Attribute("Remark") != null ? entityElement.Attribute("Remark").Value : "";
        }

        #endregion
    }
}
