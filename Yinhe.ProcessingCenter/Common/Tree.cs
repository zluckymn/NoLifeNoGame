using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yinhe.ProcessingCenter.Common
{
    /// <summary>
    /// 树状操作类
    /// </summary>
    public class Tree
    {

        public string Id { get; set; }

        public string Name { get; set; }

        public string Pid { get; set; }

        public int Level { get; set; }

        public string Key { get; set; }

        private bool _IsChild = false;
        public bool IsChild
        {
            get
            {
                return this._IsChild;
            }
            set
            {
                this._IsChild = value;
            }
        }

        private string _NodeEvent = "javascript:void(0);";
        public string NodeEvent
        {
            get
            {
                return this._NodeEvent;
            }
            set
            {
                this._NodeEvent = value;
            }
        }

        private int _type = 0;
        /// <summary>
        /// 节点类型（0-主表节点，1-从表节点）
        /// </summary>
        public int Type
        {
            get
            {
                return this._type;
            }
            set
            {
                this._type = value;
            }
        }
        /// <summary>
        /// 是否为根节点
        /// </summary>
        public bool IsRoot { get; set; }

        /// <summary>
        /// 节点的操作动作（节点旁边的html代码）
        /// </summary>
        public string NodeAction { get; set; }
    }
}
