using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yinhe.ProcessingCenter
{
    /// <summary>
    /// 树节点操作类
    /// </summary>
    public class TreeNode
    {
        public TreeNode()
        {
            this.SubNodes = new List<TreeNode>();
        }

        /// <summary>
        /// 当前节点ID
        /// </summary>
        /// <value>The id.</value>
        public Int32 Id { get; set; }

        /// <summary>
        /// 当前节点显示的名称
        /// </summary>
        /// <value>The name.</value>
        public String Name { get; set; }

        /// <summary>
        /// 当前节点所在的层级数.
        /// </summary>
        /// <value>The lv.</value>
        public Int32 Lv { get; set; }

        /// <summary>
        /// 当前节点的父节点id
        /// </summary>
        public Int32 Pid { get; set; }

        /// <summary>
        /// 同一父节点序号
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// 当前节点所属表
        /// </summary>
        public string underTable { get; set; }

        /// <summary>
        /// 树类型，主要用于组合树
        /// </summary>
        public string TreeType { get; set; }

        /// <summary>
        /// 当前节点带的参数（用|分割）
        /// </summary>
        public string Param { get; set; }

        /// <summary>
        /// 当前节点的子节点集合
        /// </summary>
        /// <value>The sub node.</value>
        public List<TreeNode> SubNodes { get; set; }

        /// <summary>
        /// 是否是叶子节点 0 --非叶子节点,即有子节点，1--叶子节点,无子节点
        /// </summary>
        public Int32 IsLeaf { get; set; }

        /// <summary>
        /// 叶子节点个数
        /// </summary>
        public Int32 LeafCount { get; set; }

        /// <summary>
        /// 当前节点下所有子节点(包含本节点)的统计个数
        /// </summary>
        public Int32 childCount { get; set; }
    }

}
