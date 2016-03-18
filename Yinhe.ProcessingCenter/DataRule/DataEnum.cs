using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yinhe.ProcessingCenter.DataRule
{
    /// <summary>
    /// 存储类型枚举
    /// </summary>
    public enum StorageType
    {
        /// <summary>
        /// 无操作
        /// </summary>
        [EnumDescription("无操作")]
        None = 0,

        /// <summary>
        /// 插入
        /// </summary>
        [EnumDescription("插入")]
        Insert = 1,

        /// <summary>
        /// 更新
        /// </summary>
        [EnumDescription("更新")]
        Update = 2,

        /// <summary>
        /// 删除
        /// </summary>
        [EnumDescription("删除")]
        Delete = 3
    }

    /// <summary>
    /// 级联操作触发时间
    /// </summary>
    public enum CascadeTimeType
    {
        /// <summary>
        /// 无操作
        /// </summary>
        [EnumDescription("无操作")]
        None,

        /// <summary>
        /// 操作前
        /// </summary>
        [EnumDescription("操作前")]
        Before,

        /// <summary>
        /// 操作后
        /// </summary>
        [EnumDescription("操作后")]
        After

        
    }

    /// <summary>
    /// 数据对象类型
    /// </summary>
    public enum DataObjectType
    {
        /// <summary>
        /// 一个值
        /// </summary>
        [EnumDescription("一个值")]
        AValue = 0,

        /// <summary>
        /// 一条记录
        /// </summary>
        [EnumDescription("一条记录")]
        ARecord =1,

        /// <summary>
        /// 多条记录
        /// </summary>
        [EnumDescription("多条记录")]
        MultipleRecords = 2,

        /// <summary>
        /// 一张表
        /// </summary>
        [EnumDescription("一张表")]
        ATable = 3,

        /// <summary>
        /// 一个操作
        /// </summary>
        [EnumDescription("一个操作")]
        AStorage = 4

    }
}
