using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yinhe.ProcessingCenter.Permissions
{
    /// <summary>
    /// 角色数据权限实体
    /// </summary>
    public class DataScopeEntity
    {
        /// <summary>
        /// 数据范围Id
        /// </summary>
        public int ScopeId { get; set; }
        /// <summary>
        /// 角色Id
        /// </summary>
        public int RoleId { get; set; }
        /// <summary>
        /// 角色数据权限类别Id
        /// </summary>
        public int RoleCategoryId { get; set; }
        /// <summary>
        /// 数据对应表
        /// </summary>
        public string DataTableName { get; set; }
        /// <summary>
        /// 数据对应表主键字段
        /// </summary>
        public string DataFeildName { get; set; }
        /// <summary>
        /// 数据实体主键Id
        /// </summary>
        public int DataId { get; set; }
        /// <summary>
        /// 创建用户
        /// </summary>
        public int CreateUserId { get; set; }
        /// <summary>
        /// 更新用户
        /// </summary>
        public int UpdateUserId { get; set; }
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateDate { get; set; }
        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdateDate { get; set; }
        /// <summary>
        /// 状态
        /// </summary>
        public int Status { get; set; }
        /// <summary>
        /// 备注说明
        /// </summary>
        public string remark { get; set; }
        /// <summary>
        /// 排序
        /// </summary>
        public int order { get; set; }

        /// <summary>
        /// 数据范围权限列表
        /// </summary>
        private List<RoleRightEntity> _RoleRightEntities;
        /// <summary>
        /// 数据范围权限列表
        /// </summary>
        public List<RoleRightEntity> RoleRightEntities
        {
            get {
                if (this._RoleRightEntities == null)
                {
                    this._RoleRightEntities = new List<RoleRightEntity>();
                }
                return _RoleRightEntities;
            }
            set {
                this._RoleRightEntities = value;
            }
        }
    }
}
