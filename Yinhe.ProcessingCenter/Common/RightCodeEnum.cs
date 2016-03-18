using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yinhe.ProcessingCenter.Common
{
    /// <summary>
    /// 权限操作项
    /// </summary>
    public enum RightCodeEnum
    {
        /// <summary>
        /// 查看
        /// </summary>
        [EnumDescription("查看")]
        VIEW=1,
        /// <summary>
        /// 下载
        /// </summary>
        [EnumDescription("下载")]
        DOWNLOAD=2,
        /// <summary>
        /// 新增
        /// </summary>
        [EnumDescription("新增")]
        ADD=3,
        /// <summary>
        /// 更新
        /// </summary>
        [EnumDescription("更新")]
        UPDATE=4,
        /// <summary>
        /// 更新所有
        /// </summary>
        [EnumDescription("更新所有")]
        UPDATEALL=5,
        /// <summary>
        /// 维护
        /// </summary>
        [EnumDescription("维护")]
        ADMIN=6
    }
}
