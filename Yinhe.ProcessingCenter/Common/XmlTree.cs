using System;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Text;

using Yinhe.ProcessingCenter;
using System.Web;
namespace Yinhe.ProcessingCenter.Common
{
    /// <summary>
    /// 树转化xml处理类
    /// </summary>
    public class XmlTree : ActionResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TreeNodesXmlResult"/> class.
        /// </summary>
        /// <param name="data">The data.</param>
        public XmlTree(List<TreeNode> data) { Data = data; }

        public XmlTree(List<TreeNode> data, string param)
        {
            Data = data;
            this.TreeParam = param;
        }

        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        /// <value>The data.</value>
        public List<TreeNode> Data { get; set; }

        public int SetRetCountFlag { get; set; }

        public String TreeParam { get; set; }

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

            sbXml.AppendFormat("\t<item{0} id=\"{1}\" name=\"{2}\" lv=\"{0}\" isfolder=\"{3}\" param=\"{4}\">", node.Lv, node.Id, HttpUtility.HtmlEncode(node.Name), node.IsLeaf.ToString(), String.IsNullOrEmpty(node.Param) == false ? node.Param : this.TreeParam);

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
    }
    /// <summary>
    /// 动态树生成处理
    /// </summary>
    public class DynamicXmlTree : ActionResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TreeNodesXmlResult"/> class.
        /// </summary>
        /// <param name="data">The data.</param>
        public DynamicXmlTree(List<TreeNode> data) { Data = data; }

        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        /// <value>The data.</value>
        public List<TreeNode> Data { get; set; }

        private string TreeType;

        private int counter;
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
                this.TreeType = entity.TreeType;
                this.counter++;
                resultBuilder.Append(this.GetXmlNodeString(entity, 0));
            }

            resultBuilder.Append("</root>");

            response.Write(resultBuilder.ToString());
        }

        private string GetXmlNodeString(TreeNode node, int ilevel)
        {
            StringBuilder sbXml = new StringBuilder();
            sbXml.AppendFormat("\t<item{0} id=\"{4}_{1}_{5}\" name=\"{2}\" lv=\"{0}\" isfolder=\"{3}\" param=\"{6}\">", node.Lv + ilevel, node.Id, HttpUtility.HtmlEncode(node.Name), node.IsLeaf.ToString(), node.TreeType, this.counter, node.Param);
            if (node.SubNodes != null)
            {
                foreach (var child in node.SubNodes)
                {
                    if (this.TreeType != child.TreeType)
                    {

                        this.TreeType = child.TreeType;
                    }
                    this.counter++;
                    sbXml.Append(this.GetXmlNodeString(child, ilevel + 1));
                }
            }
            sbXml.AppendFormat("</item{0}>\r\n", node.Lv + ilevel);
            return sbXml.ToString();
        }
    }
}
