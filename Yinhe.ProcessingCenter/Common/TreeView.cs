using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yinhe.ProcessingCenter.Common
{
    /// <summary>
    /// 树状视图处理类
    /// </summary>
    public class TreeView
    {
        /// <summary>
        /// 树的所有节点
        /// </summary>
        public List<Tree> TreeList { get; set; }
        /// <summary>
        /// 根结点
        /// </summary>
        public List<Tree> RootTreeList { get; set; }

        private string _TreeType;
        public string TreeType
        {
            get
            {
                return this._TreeType;
            }
            set
            {
                this._TreeType = value;
            }
        }
        public TreeView()
        {
        }
        public TreeView(List<Tree> treeList)
        {
            this.TreeList = treeList;
            this.RootTreeList = new List<Tree>();
        }


        /// <summary>
        /// 输出树html
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (TreeList == null)
            {
                return "没有数据";
            }
            StringBuilder sbTree = new StringBuilder();

            List<Tree> rootTreeList = null;
            if (this.RootTreeList.Count <= 0)
            {
                rootTreeList = this.GetRootTreeList();
            }
            else
            {
                rootTreeList = this.RootTreeList;
            }

            foreach (var root in rootTreeList)
            {
                sbTree.Append(this.GetChildTree(root));
            }

            return sbTree.ToString();
        }

        public string NoParentToString()
        {
            if (TreeList == null)
            {
                return "没有数据";
            }
            StringBuilder sbTree = new StringBuilder();
            foreach (var root in TreeList)
            {
                sbTree.Append(this.GetChildTree(root));
            }
            return sbTree.ToString();
        }
        #region 私有函数

        /// <summary>
        /// 获取某一节点的子节点
        /// </summary>
        /// <param name="tree"></param>
        /// <returns></returns>
        private string GetChildTree(Tree tree)
        {
            StringBuilder sbHtml = new StringBuilder();

            sbHtml.Append("<li>\r\n");

            sbHtml.AppendFormat("  <span class=\"folder\" nodeid=\"{3}\"><span name=\"treex\" onclick=\"{0}\">&nbsp;{1}</span>    {2}</span>\r\n", tree.NodeEvent, tree.Name, tree.NodeAction, tree.Id);

            sbHtml.Append(this.GetChildTree(this.TreeList.Where(t => t.Pid == tree.Id).OrderBy(t => t.Type).ToList()));

            sbHtml.Append("</li>\r\n");

            return sbHtml.ToString();
        }

        private string GetChildTree(List<Tree> childTreeList)
        {
            StringBuilder sbHtml = new StringBuilder();
            int count = childTreeList.Count;
            if (count <= 0) { return ""; }

            sbHtml.Append("  <ul>\r\n");

            foreach (var tree in childTreeList)
            {
                sbHtml.Append("    <li>\r\n");

                switch (tree.Type)
                {
                    case 0:
                        if (this.IsLeaf(tree.Id) == true)
                        {
                            //sbHtml.AppendFormat("      <span class=\"file\"><span onclick=\"{0}\">{1}</span></span>\r\n", tree.NodeEvent, tree.Name);
                            sbHtml.AppendFormat("      <span class=\"folder\" nodeid=\"{3}\"><span name=\"treex\" onclick=\"{0}\">&nbsp;{1}</span>   {2}</span>\r\n", tree.NodeEvent, tree.Name, tree.NodeAction, tree.Id);
                        }
                        else
                        {
                            sbHtml.AppendFormat("      <span class=\"folder\" nodeid=\"{3}\"><span name=\"treex\" onclick=\"{0}\">&nbsp;{1}</span>   {2}</span>\r\n", tree.NodeEvent, tree.Name, tree.NodeAction, tree.Id);
                            sbHtml.Append(this.GetChildTree(this.TreeList.Where(t => t.Pid == tree.Id).OrderBy(t => t.Type).ToList()));
                        }
                        break;
                    case 1:
                        sbHtml.AppendFormat("      <span class=\"file\" nodeid=\"{3}\"><span name=\"treex\" onclick=\"{0}\">&nbsp;{1}</span>   {2}</span>\r\n", tree.NodeEvent, tree.Name, tree.NodeAction, tree.Id);
                        break;
                    default:
                        sbHtml.AppendFormat("      <span class=\"file\" nodeid=\"{3}\"><span name=\"treex\" onclick=\"{0}\">&nbsp;{1}</span>   {2}</span>\r\n", tree.NodeEvent, tree.Name, tree.NodeAction, tree.Id);
                        break;
                }

                sbHtml.Append("    </li>\r\n");
            }

            sbHtml.Append("  </ul>");

            return sbHtml.ToString();
        }

        /// <summary>
        /// 判断是否为叶子节点（没有子节点）
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private bool IsLeaf(string id)
        {
            int count = this.TreeList.Where(t => t.Pid == id).Count();

            return (count <= 0);
        }
        /// <summary>
        /// 获取根节点
        /// </summary>
        private List<Tree> GetRootTreeList()
        {
            List<Tree> rootTreeList = null;
            rootTreeList = this.TreeList.Where(t => t.IsRoot == true).ToList();
            if (rootTreeList.Count <= 0)
            {
                rootTreeList = this.TreeList.Where(t => t.Level == 1).ToList();
            }
            return rootTreeList;
        }
        #endregion
    }
}
