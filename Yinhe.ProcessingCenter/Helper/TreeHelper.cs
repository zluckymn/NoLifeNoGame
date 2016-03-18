using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using MongoDB.Bson;
using Yinhe.ProcessingCenter.DataRule;
using System.Web;

namespace Yinhe.ProcessingCenter
{
    /// <summary>
    /// 树状操作类
    /// </summary>
    public class TreeHelper
    {
        /// <summary>
        /// 获取单表树列表
        /// </summary>
        /// <param name="dataList"></param>
        public static List<TreeNode> GetSingleTreeList(List<BsonDocument> dataList)
        {
            List<TreeNode> treeList = new List<TreeNode>();

            if (dataList.Count > 0)
            {
                int minLevel = dataList.Min(t => t.Int("nodeLevel"));   //最小层级,即根级

                List<BsonDocument> rootList = dataList.Where(t => t.Int("nodeLevel") == minLevel).ToList(); //根列表

                TableRule tableEntity = new TableRule(rootList[0].String("underTable"));

                string primaryKey = tableEntity.ColumnRules.Where(t => t.IsPrimary == true).FirstOrDefault().Name;      //获取主键

                int nodePid = rootList[0].Int("nodePid");

                int allCount = 0;
                int leafCount = 0;

                treeList = GetSingleTreeList(dataList, primaryKey, nodePid, 1, ref allCount, ref leafCount);
            }

            return treeList;
        }

        /// <summary>
        /// 获取子树列表
        /// </summary>
        /// <param name="dataList"></param>
        /// <param name="primaryKey"></param>
        /// <param name="nodePid"></param>
        /// <param name="nodeLevel"></param>
        /// <param name="childCount"></param>
        /// <param name="leafCount"></param>
        /// <returns></returns>
        public static List<TreeNode> GetSingleTreeList(List<BsonDocument> dataList, string primaryKey, int nodePid, int nodeLevel, ref int childCount, ref int leafCount)
        {
            List<BsonDocument> subList = dataList.Where(t => t.Int("nodePid") == nodePid).ToList(); //子级列表

            List<TreeNode> treeList = new List<TreeNode>();

            int order = 1;

            foreach (var subNode in subList.OrderBy(t => t.Int("nodeOrder")))      //循环子集列表,赋值
            {
                int child = 0;  //子孙节点数
                int leaf = 0;   //叶子节点数
                TreeNode node = new TreeNode();

                node.Id = subNode.Int(primaryKey);
                node.Name = subNode.String("name");
                node.Lv = nodeLevel;
                node.Pid = nodePid;
                node.Order = order;
                node.underTable = subNode.String("underTable");
                node.TreeType = "";
                node.Param = "";
                node.SubNodes = GetSingleTreeList(dataList, primaryKey, node.Id, nodeLevel + 1, ref child, ref leaf);    //获取子节点列表,同时统计子孙节点数和叶子节点数
                node.IsLeaf = node.SubNodes.Count > 0 ? 0 : 1;
                node.childCount = child;
                node.LeafCount = node.SubNodes.Count > 0 ? leaf : 0;

                childCount = child + 1;
                leafCount = node.SubNodes.Count > 0 ? leaf : leafCount + 1;

                treeList.Add(node);

                order++;
            }

            return treeList;
        }

    }

    /// <summary>
    /// 将树形列表输出为XML
    /// </summary>
    public class XmlTree : ActionResult
    {
        #region 参数
        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        /// <value>The data.</value>
        public List<TreeNode> Data { get; set; }

        /// <summary>
        /// 默认参数
        /// </summary>
        public String TreeParam { get; set; }
        #endregion

        #region 构造
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="data">输出数据</param>
        public XmlTree(List<TreeNode> data)
        {
            Data = data;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="data">输出数据</param>
        /// <param name="param">默认参数</param>
        public XmlTree(List<TreeNode> data, string param)
        {
            Data = data;
            this.TreeParam = param;
        }
        #endregion

        #region 方法
        /// <summary>
        /// Enables processing of the result of an action method by a custom type that inherits from <see cref="T:System.Web.Mvc.ActionResult"/>.
        /// </summary>
        /// <param name="context">The context within which the result is executed.</param>
        public override void ExecuteResult(ControllerContext context)
        {
            if (context == null) throw new ArgumentNullException();

            var resultBuilder = new System.Text.StringBuilder();

            var response = context.HttpContext.Response;

            /// set HTTP Header's ContentType 
            response.ContentType = "text/xml";

            /// set the xml's header.
            resultBuilder.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n");
            resultBuilder.Append("<root>\r\n");

            foreach (var entity in Data)
            {
                resultBuilder.Append(this.GetXmlNodeString(entity));
            }

            resultBuilder.Append("</root>");

            response.Write(resultBuilder.ToString());
        }

        private string GetXmlNodeString(TreeNode node)
        {
            StringBuilder sbXml = new StringBuilder();

            sbXml.AppendFormat("\t<item{0} id=\"{1}\" name=\"{2}\" lv=\"{0}\" isfolder=\"{3}\" param=\"{4}\" childcount=\"{5}\" leafcount=\"{6}\" >", node.Lv, node.Id, HttpUtility.HtmlEncode(node.Name), node.IsLeaf.ToString(), String.IsNullOrEmpty(node.Param) == false ? node.Param : this.TreeParam,node.childCount,node.LeafCount);

            if (node.SubNodes != null)
            {
                foreach (var child in node.SubNodes)
                {
                    sbXml.Append(this.GetXmlNodeString(child));
                }
            }
            sbXml.AppendFormat("</item{0}>\r\n", node.Lv);
            return sbXml.ToString();
        }

        #endregion
    }

}
