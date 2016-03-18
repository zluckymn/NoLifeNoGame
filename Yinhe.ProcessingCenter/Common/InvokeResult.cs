using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;

namespace Yinhe.ProcessingCenter
{
    /// <summary>
    /// 业务层调用结果类型.
    /// </summary>
    public class InvokeResult
    {
        /// <summary>
        /// 获取或设置当期异常
        /// </summary>
        /// <value>The exception.</value>
        public Exception Exception { get; set; }

        /// <summary>
        /// 获取或设置当前状态
        /// </summary>
        /// <value>The status.</value>
        public Status Status { get; set; }

        /// <summary>
        /// 获取或设置Key名称
        /// </summary>
        /// <value>The key.</value>
        public String Key { get; set; }

        /// <summary>
        /// 获取或设置消息文本
        /// </summary>
        /// <value>The message.</value>
        public String Message { get; set; }

        /// <summary>
        /// 文件信息
        /// </summary>
        public String FileInfo { get; set; }

        /// <summary>
        /// 获取结果信息
        /// </summary>
        public BsonDocument BsonInfo { get; set; }
    }

    /// <summary>
    /// 附带值类型的结果类型
    /// </summary>
    /// <typeparam name="TType"></typeparam>
    public class InvokeResult<TType> : InvokeResult
    {
        /// <summary>
        /// 返回泛型的结果值
        /// </summary>
        /// <value>The value.</value>
        public TType Value { get; set; }
    }

    /// <summary>
    /// 结果状态枚举
    /// </summary>
    public enum Status
    {
        /// <summary>
        /// 成功
        /// </summary>
        Successful,

        /// <summary>
        /// 失败
        /// </summary>
        Failed,

        /// <summary>
        /// 挂起
        /// </summary>
        Suspend,

        /// <summary>
        /// 无效的
        /// </summary>
        Invaild,

        /// <summary>
        /// 特定的状态,需要参考Message属性.
        /// </summary>
        Specific,
    }
}
