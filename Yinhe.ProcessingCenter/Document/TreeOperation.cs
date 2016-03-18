using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
namespace Yinhe.ProcessingCenter.Document
{
    /// <summary>
    /// Created By Robin  2012年5月20日 
    /// 1 先将文件列表转化成单节点树
    /// 2 将树集合进行排序  取节点最深的为主树
    /// 3 将其他树与主树合并
    /// </summary>
    public class TreeOperation
    {
        public List<string> _fileList = new List<string>();

        /// <summary>
        /// 文件夹扫描 用于测试
        /// </summary>
        /// <param name="rootPath"></param>
        /// <returns></returns>
        public TreeNode ScanFolder(string rootPath)
        {
            int fileCount = 0;
            int dirCount = 0;
            TreeNode node = new TreeNode();
            DirectoryInfo dir = new DirectoryInfo(rootPath);

            var fileList = new List<string>();
            foreach (var file in dir.GetFiles())
            {
                ++fileCount;
                fileList.Add(file.FullName);
                _fileList.Add(file.FullName);
                //Console.WriteLine(file.FullName);
            }
            var dirList = new List<TreeNode>();
            foreach (var subdir in dir.GetDirectories())
            {
                var subNode = ScanFolder(subdir.FullName);
                dirList.Add(subNode);
                ++dirCount;
                dirCount = dirCount + subNode.SubDirCount;
                fileCount = fileCount + subNode.SubFileCount;
            }
            node.Name = dir.FullName;
            node.FileList = fileList;
            node.SubNode = dirList;
            node.SubFileCount = fileCount;
            node.SubDirCount = dirCount;
            return node;
        }

        /// <summary>
        /// 树合并
        /// </summary>
        /// <param name="root"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        public static TreeNode TreeCombine(string root, List<string> list)
        {
            var abc = ListAnalyse(root, list);
            var motherNodeDic = abc.FirstOrDefault();
            TreeNode motherNode = motherNodeDic.Key;
            foreach (var item in motherNodeDic.Value)
            {
                SetNode(motherNode, item);
            }
            return motherNode;
        }

        /// <summary>
        /// 列表分析
        /// </summary>
        /// <param name="root"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        public static Dictionary<TreeNode, List<Dictionary<int, TreeDic>>> ListAnalyse(string root, List<string> list)
        {
            Dictionary<TreeNode, List<Dictionary<int, TreeDic>>> dic = new Dictionary<TreeNode, List<Dictionary<int, TreeDic>>>();
            List<Dictionary<int, TreeDic>> subDicList = new List<Dictionary<int, TreeDic>>();
            TreeNode node = new TreeNode();
            int max = 0;
            foreach (var item in list)
            {
                var array = ArrayGenerate(root, item);
                var tree = TreeGenerate(root, item);
                int level = TreeLevel(tree)+1;
                if (level > max)
                {
                    max = level;
                    node = tree;
                }
                subDicList.Add(TreeToDic(tree, array));
            }
            dic.Add(node, subDicList);
            return dic;

        }

        /// <summary>
        /// 树遍历  打印
        /// </summary>
        /// <param name="node"></param>
        public static void TreePrint(TreeNode node)
        {
            TreeNode subnode = new TreeNode();
            subnode = node;
            //Console.WriteLine(string.Format("Level:{0}  Name:{1}", subnode.Level, subnode.Name));
            if (subnode.SubNode != null)
            {
                foreach (var item in subnode.SubNode)
                {
                    TreePrint(item);
                    subnode = item;
                }
            }
        }


        public static List<string> CutRoot(string root, List<string> list)
        {
            List<string> newlist = new List<string>();

            foreach (var str in list)
            {
                string tempStr = "";
                int index = root.ToLower().Length;
                tempStr = str.Substring(index);
                // tempStr = tempStr.Substring(0, tempStr.LastIndexOf("\\")+1);
                newlist.Add(tempStr);
            }
            return newlist;
        }

        /// <summary>
        /// 将地址转化成数组
        /// </summary>
        /// <param name="root"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string[] ArrayGenerate(string root, string path)
        {
            string tempStr = "";
            int index = root.ToLower().Length;
            tempStr = path.Substring(index);
            TreeNode node = new TreeNode();
            int lastIndex = tempStr.LastIndexOf('\\');
            string fileName = tempStr.Substring(lastIndex + 1);//获取文件名
            string dirString = tempStr.Substring(0, lastIndex);//获取目录路径

            //  dirString = dirString.Substring(1);

            string[] dirArray = dirString.Split(new string[] { "\\" }, StringSplitOptions.RemoveEmptyEntries);
            return dirArray;
        }

        /// <summary>
        /// 将文件路径装换成树形 单节点
        /// </summary>
        /// <param name="root">跟目录</param>
        /// <param name="path">完整路径</param>
        /// <returns></returns>
        public static TreeNode TreeGenerate(string root, string path)
        {
            return ArrayMulAnalyze(ArrayGenerate(root, path), 0, path); ;
        }

        /// <summary>
        /// 数组分析
        /// </summary>
        /// <param name="dirArray"></param>
        /// <param name="currentIndex"></param>
        /// <param name="localPath"></param>
        /// <returns></returns>
        public static TreeNode ArrayAnalyze(string[] dirArray, int currentIndex, string localPath)
        {
            TreeNode node = new TreeNode();
            node.Name = dirArray[currentIndex];
            if (currentIndex == 1)
            {
                node.pName = "";
            }
            else
            {
                node.pName = dirArray[currentIndex - 1];
            }
            node.Level = currentIndex;
            if (currentIndex == dirArray.Length - 1)//最后一级目录挂文件
            {
                node.SubSingleFile = localPath;
            }
            ++currentIndex;

            if (currentIndex < dirArray.Length)
            {
                TreeNode subnode = new TreeNode();

                subnode = ArrayAnalyze(dirArray, currentIndex, localPath);
                node.SubSingleNode = subnode;
            }
            return node;
        }

        /// <summary>
        /// 数组分析
        /// </summary>
        /// <param name="dirArray"></param>
        /// <param name="currentIndex"></param>
        /// <param name="localPath"></param>
        /// <returns></returns>
        public static TreeNode ArrayMulAnalyze(string[] dirArray, int currentIndex, string localPath)
        {
            TreeNode node = new TreeNode();
            node.Name = dirArray[currentIndex];
            node.Level = currentIndex;
            if (currentIndex == 0)
            {
                node.pName = "";
            }
            else
            {
                node.pName = dirArray[currentIndex - 1];
            }
            if (currentIndex == dirArray.Length - 1)//最后一级目录挂文件
            {
                node.FileList = new List<string> { localPath };
            }
            ++currentIndex;

            if (currentIndex < dirArray.Length)
            {
                TreeNode subnode = new TreeNode();

                subnode = ArrayMulAnalyze(dirArray, currentIndex, localPath);
                node.SubNode = new List<TreeNode> { subnode };
            }
            return node;
        }

        ///// <summary>
        ///// 树合并
        ///// </summary>
        ///// <param name="treeList"></param>
        ///// <returns></returns>
        //public static TreeNode TreeCombine(List<TreeNode> treeList)
        //{
        //    TreeNode node = new TreeNode();//根

        //    foreach (var item in treeList)
        //    {

        //    }


        //    return node;
        //}

        /// <summary>
        /// 树合并
        /// </summary>
        /// <param name="motherNode">母树</param>
        /// <param name="dic"></param>
        /// <returns></returns>
        public static void SetNode(TreeNode motherNode, Dictionary<int, TreeDic> dic)
        {

            TreeNode subnode = new TreeNode();
            subnode = motherNode;
            if (subnode.SubNode == null)
            {
                subnode.SubNode = new List<TreeNode>();
            }
            if (dic.Count == 1) //根目录文件列表赋值
            {
                if (subnode.FileList == null)
                {
                    subnode.FileList = new List<string>();
                }
                if (!string.IsNullOrEmpty(dic.FirstOrDefault().Value.LocalPath))
                {
                    subnode.FileList.Add(dic.FirstOrDefault().Value.LocalPath);
                }

            }
            // 

            //Console.WriteLine(string.Format("Level:{0}  Name:{1}", subnode.Level, subnode.Name));
            var temp = dic.Where(t => t.Key == subnode.Level + 1).FirstOrDefault();
            var temp2 = dic.Where(t => t.Key == subnode.Level).FirstOrDefault();
            List<TreeNode> oldList = new List<TreeNode>();
            List<string> oldStrList = new List<string>();
            if (temp.Value != null)
            {
                if (subnode.SubNode != null)
                {
                    if (subnode.Name == temp.Value.pName && subnode.SubNode.Where(t => t.Name == temp.Value.Name && t.pName == temp.Value.pName).Count() <= 0) //如果subnode子节点没有这个节点 则新加入
                    {
                        TreeNode node = new TreeNode();
                        node.Name = temp.Value.Name;
                        node.Level = temp.Key;
                        node.pName = temp.Value.pName;
                        if (node.FileList == null)
                        {
                            node.FileList = new List<string>();
                        }
                        if (!string.IsNullOrEmpty(temp.Value.LocalPath))
                        {
                            if (!node.FileList.Contains(temp.Value.LocalPath))
                            {
                                node.FileList.Add(temp.Value.LocalPath);
                            }
                            if (!oldStrList.Contains(temp.Value.LocalPath))
                            {
                                oldStrList.Add(temp.Value.LocalPath);
                            }

                        }
                        subnode.SubNode.Add(node);

                        oldList.AddRange(subnode.SubNode);

                    }
                    else
                    {
                        var nodes = subnode.SubNode.Where(t => t.Name == temp.Value.Name && t.pName == temp.Value.pName).FirstOrDefault();
                        if (nodes != null)
                        {
                            if (nodes.FileList == null)
                            {
                                nodes.FileList = new List<string>();
                            }
                            if (!string.IsNullOrEmpty(temp.Value.LocalPath))
                            {
                                if (!nodes.FileList.Contains(temp.Value.LocalPath))
                                {
                                    nodes.FileList.Add(temp.Value.LocalPath);
                                }
                            }
                        }
                    }
                }

                if (subnode.SubNode != null)
                {

                    foreach (var item in subnode.SubNode.Where(t => t.Name == temp.Value.Name&&t.pName==temp.Value.pName))
                    {
                        subnode = item;
                        SetNode(item, dic);

                    }
                }
            }


        }

        /// <summary>
        /// 创建一棵空树  单节点
        /// </summary>
        /// <param name="treeLevel"></param>
        /// <param name="currentLevel"></param>
        /// <returns></returns>
        public static TreeNode CreateEmptyTree(int treeLevel, int currentLevel)
        {
            TreeNode node = new TreeNode();
            node.Level = currentLevel;
            if (currentLevel < treeLevel)
            {
                ++currentLevel;
                TreeNode subnode = CreateEmptyTree(treeLevel, currentLevel);
                node.SubSingleNode = subnode;
            }
            return node;

        }

        /// <summary>
        /// 将树转化为字典
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static Dictionary<int, TreeDic> TreeToDic(TreeNode node, string[] array)
        {
            TreeNode subnode = new TreeNode();
            int treeLevel = TreeLevel(node);
            Dictionary<int, TreeDic> dic = new Dictionary<int, TreeDic>();
            TreeDic dicmod = new TreeDic();
            subnode = node;
            dicmod.Name = node.Name;
            dicmod.pName = node.Level <= 0 ? "" : array[node.Level - 1];
            dicmod.LocalPath = node.FileList != null ? node.FileList.FirstOrDefault() : "";
            dic.Add(node.Level, dicmod);
            int level = 0;
            while (subnode.SubNode != null)
            {

                subnode = subnode.SubNode.FirstOrDefault();
                TreeDic subdicmod = new TreeDic();
                if (subnode != null)
                {
                    ++level;
                    subdicmod.Name = subnode.Name;
                    subdicmod.pName = level <= 0 ? "" : array[level - 1];
                    if (level == treeLevel)
                    {
                        subdicmod.LocalPath = subnode.FileList != null ? subnode.FileList.FirstOrDefault() : "";
                    }
                    dic.Add(level, subdicmod);
                }


            }

            return dic;
        }

        /// <summary>
        /// 计算树深度
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static int TreeLevel(TreeNode node)
        {
            int level = 0;
            TreeNode subNode = new TreeNode();
            subNode = node;
            int max = 0;
            if (subNode.SubNode != null)
            {
                ++level;

                foreach (var item in subNode.SubNode)
                {
                    int sublevel = TreeLevel(item);
                    max = sublevel > max ? sublevel : max;
                }
            }
            level += max;
            return level;
        }

    }

    /// <summary>
    /// 树实体
    /// </summary>
    public class TreeNode
    {

        private int _level;

        public int Level
        {
            get { return _level; }
            set { _level = value; }
        }
        private string _name;

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        private string _pname;

        public string pName
        {
            get { return _pname; }
            set { _pname = value; }
        }

        private string _localPath;

        public string LocalPath
        {
            get { return _localPath; }
            set { _localPath = value; }
        }

        private int _subFileCount;

        public int SubFileCount
        {
            get { return _subFileCount; }
            set { _subFileCount = value; }
        }

        private int _subDirCount;

        public int SubDirCount
        {
            get { return _subDirCount; }
            set { _subDirCount = value; }
        }

        private List<string> _fileList;

        public List<string> FileList
        {
            get { return _fileList; }
            set { _fileList = value; }
        }
        private List<TreeNode> _subNode;

        public List<TreeNode> SubNode
        {
            get { return _subNode; }
            set { _subNode = value; }
        }

        private TreeNode _subSingleNode;

        public TreeNode SubSingleNode
        {
            get { return _subSingleNode; }
            set { _subSingleNode = value; }
        }


        private string _subSingleFile;

        public string SubSingleFile
        {
            get { return _subSingleFile; }
            set { _subSingleFile = value; }
        }

        private int _structId;

        public int StructId
        {
            get { return _structId; }
            set { _structId = value; }
        }

    }
    /// <summary>
    /// 树字典
    /// </summary>
    public class TreeDic
    {
        private string _name;

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        private string _pname;

        public string pName
        {
            get { return _pname; }
            set { _pname = value; }
        }

        private string _localPath;

        public string LocalPath
        {
            get { return _localPath; }
            set { _localPath = value; }
        }
    }

}
