using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
namespace Yinhe.ProcessingCenter.Common
{
    /// <summary>
    /// 系统Item返回类型
    /// </summary>
    public class Item
    {
        public int id { get; set; }

        public string name { get; set; }

        private string _type = "";
        public string type
        {
            get { return this._type; }
            set { this._type = value; }
        }

        public string value { get; set; }

        public double Counter { get; set; }

        /// <summary>
        /// 用于标识属性处理
        /// </summary>
        public bool IsFlag { get; set; }

        /// <summary>
        /// 其他参数
        /// </summary>
        public string otherParam { get; set; }


        /// <summary>
        /// 参数
        /// </summary>
        public int userId { get; set; }
        /// <summary>
        /// 标志使用状态
        /// </summary>
        public int isUse { get; set; }
    }

    /// <summary>
    /// 模块操作业务对象实体
    /// </summary>
    public class ModulOper
    {
        /// <summary>
        /// 模块id
        /// </summary>
        public int ModulId { get; set; }

        /// <summary>
        /// 操作id
        /// </summary>
        public int OperId { get; set; }

        /// <summary>
        /// 业务对象id
        /// </summary>
        public int dataObjId { get; set; }

        /// <summary>
        /// 权限代码
        /// </summary>
        public string code { get; set; }

    }

    /// <summary>
    /// 任务分项工程名称实体
    /// </summary>
    public class TaskIdClass
    {
        public int taskId { get; set; }
        public int projId { get; set; }
        public int engId { get; set; }
        public int comId { get; set; }

        public string taskName { get; set; }
        public string projName { get; set; }
        public string engName { get; set; }
        public string comName { get; set; }

    }
}
