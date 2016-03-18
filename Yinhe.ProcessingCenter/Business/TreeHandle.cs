using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;

namespace Yinhe.ProcessingCenter
{
    /// <summary>
    /// 树形结构操作基类
    /// </summary>
    public abstract class TreeHandle
    {
        public  const string NodeKey = "nodeKey";
        public  const string NodeLevel = "nodeLevel";
        public  const string NodePid = "nodePid";
        public  const string NodeOrder = "nodeOrder";
        /// <summary>
        /// 对传入的数据结构进行计算，哪些事叶子节点，每个节点需要占据的行数、列数
        /// </summary>
        /// <param name="treeNodes">树形结构数据</param>
        /// <param name="maxLevel">传入结构的最大深度</param>
        public static void InitTableNode(List<BsonDocument> treeNodes, int maxLevel)
        {
            //计算叶子节点
            foreach (var node in treeNodes)
            {
                var nextLevel = node.Int("nodeLevel") + 1;
                var parentKey = node.String("nodeKey");
                var childCount = treeNodes.Count(s => s.Int("nodeLevel") == nextLevel && s.String("nodeKey").StartsWith(parentKey));
                var colSpan = 1;
                if (childCount == 0)
                {
                    node.Add("isLeaf", "1");//代表为叶子节点(没有子节点)
                    colSpan = maxLevel - node.Int("nodeLevel") + 1;
                }
                node.Add("colSpan", colSpan);
            }

            //计算每个节点要占的行数
            foreach (var node in treeNodes)
            {
                var parentKey = node.String("nodeKey") + ".";
                var rowSpan = 1;
                var leafCount = treeNodes.Count(s => s.String("nodeKey").StartsWith(parentKey) && s.String("isLeaf") == "1");//叶子节点数量
                if (leafCount > 1)
                {
                    rowSpan = leafCount;
                }
                node.Add("rowSpan", rowSpan.ToString());
            }
        }

        /// <summary>
        /// 计算叶子节点，如果为叶子节点则isLeaf为1
        /// </summary>
        /// <param name="treeNodes"></param>
        public static void CalcLeafNode(List<BsonDocument> treeNodes)
        {
            //计算叶子节点
            foreach (var node in treeNodes)
            {
                var nextLevel = node.Int("nodeLevel") + 1;
                var parentKey = node.String("nodeKey");
                var childCount = treeNodes.Count(s => s.Int("nodeLevel") == nextLevel && s.String("nodeKey").StartsWith(parentKey));
                if (childCount == 0)
                {
                    node.Add("isLeaf", "1");//代表为叶子节点(没有子节点)
                }
            }
        }



        /// <summary>
        /// 对传入的数据结构进行计算，哪些是叶子节点，每个节点需要占据的行数、列数,剩余列数放在第几级
        /// </summary>
        /// <param name="treeNodes">树形结构数据</param>
        /// <param name="maxLevel">传入结构的最大深度</param>
        /// <param name="colLevel">剩余列数所在级</param>
        public static void InitTableNode(List<BsonDocument> treeNodes, int maxLevel,int colLevel)
        {
            if (colLevel > maxLevel) { colLevel = maxLevel; }
            //计算叶子节点
            foreach (var node in treeNodes)
            {
                var nextLevel = node.Int("nodeLevel") + 1;
                var parentKey = node.String("nodeKey");
                var childCount = treeNodes.Count(s => s.Int("nodeLevel") == nextLevel && s.String("nodeKey").StartsWith(parentKey));
                var colSpan = 1;
                if (childCount == 0)
                {
                    node.Add("isLeaf", "1");//代表为叶子节点(没有子节点)
                   var colSpanLeft = maxLevel - node.Int("nodeLevel") + 1;
                   if (colSpanLeft > 1) 
                   {
                       var parentNode = treeNodes.FirstOrDefault(x => node.String("nodeKey").IndexOf(x.String("nodeKey")) == 0&x.Int("nodeLevel")==colLevel);
                       if (parentNode != null) {
                           parentNode.Set("colSpan", colSpanLeft);
                       }
                   }
                }
                if (!node.ContainsColumn("colSpan"))
                {
                    node.Add("colSpan", colSpan);
                }
            }

            //计算每个节点要占的行数
            foreach (var node in treeNodes)
            {
                var parentKey = node.String("nodeKey") + ".";
                var rowSpan = 1;
                var leafCount = treeNodes.Count(s => s.String("nodeKey").StartsWith(parentKey) && s.String("isLeaf") == "1");//叶子节点数量
                if (leafCount > 1)
                {
                    rowSpan = leafCount;
                }
                node.Add("rowSpan", rowSpan.ToString());
            }
        }
    }

    /// <summary>
    /// 树形借款扩展方法
    /// </summary>
    public static class TreeExtensions
    {
        /// <summary>
        /// 获取叶子节点
        /// </summary>
        /// <param name="treeList"></param>
        /// <returns></returns>
        public static IEnumerable<BsonDocument> LeafNode(this IEnumerable<BsonDocument> treeList)
        {
            return treeList.Where(s => s.Int("isLeaf") == 1);
        }
    }

}
