using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yinhe.ProcessingCenter.Permissions
{
    /// <summary>
    /// 角色权限实体
    /// </summary>
    public class RoleRightEntity
    {
        /// <summary>
        /// 模块Id
        /// </summary>
        public int ModuleId { get; set; }
        /// <summary>
        /// 权限项Id
        /// </summary>
        public int OperatingId { get; set; }
        /// <summary>
        /// 资源Id
        /// </summary>
        public Nullable<int> DataObjId { get; set; }

        /// <summary>
        /// 数据对象实例数据Id
        /// </summary>
        public Nullable<int> DataId { get; set; }

        /// <summary>
        /// 模块权限项唯一代码
        /// </summary>
        public string Code { get; set; }

        #region 模块代码
        /// <summary>
        /// 模块代码
        /// </summary>
        private string _ModuleCode;
        /// <summary>
        /// 模块代码
        /// </summary>
        public string ModuleCode
        {
            get
            {
                if (this._ModuleCode == null)
                {
                    string[] arrCode = this.Code.Split(new string[] { "_" }, StringSplitOptions.RemoveEmptyEntries);
                    if (arrCode.Length > 0)
                    {
                        this._ModuleCode = arrCode[0];
                    }
                    else
                    {
                        this._ModuleCode = string.Empty;
                    }
                }
                return this._ModuleCode;
            }
        }
        #endregion

        #region 操作项代码
        /// <summary>
        /// 操作项代码
        /// </summary>
        private string _OperatingCode;
        /// <summary>
        /// 操作项代码
        /// </summary>
        public string OperatingCode
        {
            get
            {
                if (this._OperatingCode == null)
                {
                    string[] arrCode = this.Code.Split(new string[] { "_" }, StringSplitOptions.RemoveEmptyEntries);
                    if (arrCode.Length >= 2)
                    {
                        this._OperatingCode = arrCode[1];
                    }
                    else
                    {
                        this._OperatingCode = string.Empty;
                    }
                }
                return this._ModuleCode;
            }
        }
        #endregion

        #region 数据对象代码
        /// <summary>
        /// 数据对象代码
        /// </summary>
        private string _DataObjectCode;
        /// <summary>
        /// 资源模块代码
        /// </summary>
        public string DataObjectCode
        {
            get
            {

                if (this._DataObjectCode == null)
                {
                    this._OperatingCode = string.Empty;
                    if (this.DataObjId.HasValue == true)
                    {
                        string[] arrCode = this.Code.Split(new string[] { "_" }, StringSplitOptions.RemoveEmptyEntries);
                        if (arrCode.Length >= 3)
                        {
                            this._OperatingCode = arrCode[2];
                        }
                    }
                }
                return this._ModuleCode;
            }
        }

        #endregion
    }
}
