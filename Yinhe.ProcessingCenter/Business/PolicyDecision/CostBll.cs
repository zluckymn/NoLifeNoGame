using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Yinhe.ProcessingCenter.DataRule;
using MongoDB.Bson;
using Yinhe.ProcessingCenter;
using MongoDB.Driver.Builders;
using MongoDB.Driver;

namespace Yinhe.ProcessingCenter.Business.PolicyDecision
{
    /// <summary>
    /// 成本计算处理类
    /// </summary>
    public class CostBll
    {
        #region 构造函数
        /// <summary>
        /// 类私有变量
        /// </summary>
        private DataOperation _ctx = null;

        /// <summary>
        /// 封闭当前默认构造函数
        /// </summary>
        public CostBll()
        {
            _ctx = new DataOperation();
        }

        public CostBll(DataOperation ctx)
        {
            _ctx = ctx;
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static CostBll _()
        {
            return new CostBll();
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static CostBll _(DataOperation ctx)
        {
            return new CostBll(ctx);
        }
        #endregion

        /// <summary>
        /// 计算出成本科目的工程量的值，及估算总值((比例支付的基数为成本科目中的收入科目总额))
        /// </summary>
        /// <param name="costDirList"></param>
        /// <param name="projId"></param>
        /// <param name="versionId"></param>
        public void Init(List<BsonDocument> costDirList ,string projId,string versionId)
        {
            Dictionary<string, List<BsonDocument>> tbDirDict = new Dictionary<string, List<BsonDocument>>();

            string areaValue = "areaValue";
            string amount = "amount";
            string quota = "quota";
            string value = "value";
            string minamount = "minamount";
            string minquota = "minquota";
            string minvalue = "minvalue";
            string comamount = "comamount";
            string comquota = "comquota";
            string comvalue = "comvalue";

            foreach (var dir in costDirList.Where(s=>s.Int("subType") == 1))//成本收入
            {
                string relTable = dir.String("relTable");
                if (!string.IsNullOrEmpty(relTable))//引用其它部门数据的工程量
                {

                    BsonDocument relData = GetRelInfo(tbDirDict, relTable, versionId, areaValue, dir.Int("relId"));//获取工程量
                    dir.TryAdd(amount, relData.String(areaValue));
                    dir.TryAdd(minamount, relData.String(areaValue));
                    dir.TryAdd(comamount, relData.String(areaValue));
                    dir.TryAdd(quota, relData.String("maxVal"));
                    dir.TryAdd(minquota, relData.String("minVal"));
                    dir.TryAdd(comquota, relData.String("commonVal"));
                }
                decimal val = dir.Decimal(amount) * dir.Decimal(quota);//乐观成本
                dir.Add(value, val.ToString());
                decimal minval = dir.Decimal(amount) * dir.Decimal(minquota);//悲观成本
                dir.Add(minvalue, minval.ToString());
                decimal comval = dir.Decimal(amount) * dir.Decimal(comquota);//客观成本
                dir.Add(comvalue, comval.ToString());
            }

            var totalIn = costDirList.Where(s => s.Int("subType") == 1).LeafNode().Sum(s => s.Decimal(value));//总收入
            var mintotalIn = costDirList.Where(s => s.Int("subType") == 1).LeafNode().Sum(s => s.Decimal(minvalue));//悲观总收入
            var comtotalIn = costDirList.Where(s => s.Int("subType") == 1).LeafNode().Sum(s => s.Decimal(comvalue));//客观总收入

            foreach (var dir in costDirList.Where(s=>s.Int("subType") == 0))//成本支出科目
            {
                string relTable = dir.String("relTable");
                if (!string.IsNullOrEmpty(relTable))//引用其它部门数据的工程量
                {
                    BsonDocument relData = GetRelInfo(tbDirDict, relTable, versionId, areaValue, dir.Int("relId"));//获取工程量
                    dir.TryAdd(amount,relData.String(areaValue));
                    dir.TryAdd(minamount, relData.String(areaValue));
                    dir.TryAdd(comamount, relData.String(areaValue));
                    decimal val = dir.Decimal(amount) * dir.Decimal(quota);
                    dir.Add(value, val.ToString());
                    dir.Add(minvalue, val.ToString());
                    dir.Add(comvalue, val.ToString());

                    dir.Add(minquota, dir.String(quota));//估算指标一样
                    dir.Add(comquota, dir.String(quota));
                }
                else if ((dir.Int("costType") == 2))//比例,基数为成本科目的总收入
                {
                    var rate = dir.Decimal(quota);
                    dir.TryAdd(amount, totalIn.ToString());
                    dir.TryAdd(minamount, mintotalIn.ToString());
                    dir.TryAdd(comamount, comtotalIn.ToString());
                    var totalValue = rate * totalIn;
                    dir.Add(value, totalValue.ToString());
                    var mintotalValue = rate * mintotalIn;
                    dir.Add(minvalue, mintotalValue.ToString());
                    var comtotalValue = rate * comtotalIn;
                    dir.Add(comvalue, comtotalValue.ToString());

                    dir.Add(minquota, dir.String(quota));//估算指标一样
                    dir.Add(comquota, dir.String(quota));
                }
                else//手动填写工程量和估算指标
                {
                    decimal val = dir.Decimal(amount) * dir.Decimal(quota);
                    dir.Add(value, val.ToString());//三种状态的值都一样
                    dir.Add(minvalue, val.ToString());
                    dir.Add(comvalue, val.ToString());
                    dir.Add(minamount, dir.String(amount));
                    dir.Add(comamount, dir.String(amount));
                    dir.Add(minquota, dir.String(quota));
                    dir.Add(comquota, dir.String(quota));
                }
            }
            CalcParentValue(costDirList, value);
            CalcParentValue(costDirList, minvalue);
            CalcParentValue(costDirList, comvalue);
        }


        /// <summary>
        /// 计算出成本科目的工程量的值，及估算总值(比例支付的基数为销售回款总额)
        /// </summary>
        /// <param name="costDirList">成本科目</param>
        /// <param name="projId">项目Id</param>
        /// <param name="versionId">版本Id</param>
        public void CalcTotalCost(List<BsonDocument> costDirList, string projId, string versionId)
        {
            Dictionary<string, List<BsonDocument>> tbDirDict = new Dictionary<string, List<BsonDocument>>();

            string tbName = "SellDetail";
            List<BsonDocument> sellDetails = _ctx.FindAllByQuery(tbName, Query.EQ("versionId", versionId)).ToList();//销售
            string areaValue = "areaValue";

            //
            foreach (var dir in costDirList.LeafNode().Where(s => s.Int("subType") == 1))//成本收入
            {
                string relTable = dir.String("relTable");
                if (!string.IsNullOrEmpty(relTable))//引用其它部门数据的工程量
                {
                    BsonDocument relData = GetRelInfo(tbDirDict, relTable, versionId, areaValue, dir.Int("relId"));//获取工程量
                    dir.TryAdd("amount", relData.String(areaValue));
                    dir.TryAdd("quota", relData.String("maxVal"));
                    dir.TryAdd("minamount", relData.String(areaValue));
                    dir.TryAdd("minquota", relData.String("minVal"));
                    dir.TryAdd("comamount", relData.String(areaValue));
                    dir.TryAdd("comquota", relData.String("commonVal"));
                }
                decimal value = dir.Decimal("amount") * dir.Decimal("quota");//成本
                dir.Add("value", value.ToString());
                decimal minvalue = dir.Decimal("minamount") * dir.Decimal("minquota");//悲观成本
                dir.Add("minvalue", minvalue.ToString());
                decimal comvalue = dir.Decimal("comamount") * dir.Decimal("comquota");//客观成本
                dir.Add("comvalue", comvalue.ToString());

            }

            var totalIn = 0m;//总收入
            var mintotalIn = 0m;//悲观总收入
            var comtotalIn = 0m;//客观总收入
            if (sellDetails.Any())
            {
                totalIn = sellDetails.Sum(s => s.Decimal("salesVal"));
                mintotalIn = sellDetails.Sum(s => s.Decimal("minSalesVal"));
                comtotalIn = sellDetails.Sum(s => s.Decimal("comSalesVal"));
            }

            foreach (var dir in costDirList.Where(s => s.Int("subType") == 0))//成本支出科目
            {
                string relTable = dir.String("relTable");
                if (!string.IsNullOrEmpty(relTable))//引用其它部门数据的工程量
                {
                    BsonDocument relData = GetRelInfo(tbDirDict, relTable, versionId, areaValue, dir.Int("relId"));//获取工程量
                    dir.TryAdd("amount", relData.String(areaValue));
                    dir.TryAdd("minamount", relData.String(areaValue));
                    dir.TryAdd("comamount", relData.String(areaValue));
                    decimal value = dir.Decimal("amount") * dir.Decimal("quota");
                    dir.Add("value", value.ToString());
                    decimal minvalue = dir.Decimal("minamount") * dir.Decimal("quota");
                    dir.Add("minvalue", minvalue.ToString());//悲观值
                    decimal comvalue = dir.Decimal("comamount") * dir.Decimal("quota");
                    dir.Add("comvalue", comvalue.ToString());//客观值


                    dir.Add("minquota", dir.String("quota"));//估算指标一样
                    dir.Add("comquota", dir.String("quota"));

                }
                else if ((dir.Int("costType") == 2))//比例,基数为成本科目的总收入
                {
                    var rate = dir.Decimal("quota");
                    var totalValue = rate * totalIn;
                    dir.Add("value", totalValue.ToString());
                    var mintotalValue = rate * mintotalIn;
                    dir.Add("minvalue", mintotalValue.ToString());
                    var comtotalValue = rate * comtotalIn;
                    dir.Add("comvalue", comtotalValue.ToString());

                    dir.Add("minquota", dir.String("quota"));//估算指标一样
                    dir.Add("comquota", dir.String("quota"));
                }
                else//手动填写工程量和比例
                {
                    decimal value = dir.Decimal("amount") * dir.Decimal("quota");
                    dir.Add("value", value.ToString());//三种状态的值都一样
                    dir.Add("minvalue", value.ToString());
                    dir.Add("comvalue", value.ToString());
                    dir.Add("minamount", dir.String("amount"));
                    dir.Add("comamount", dir.String("amount"));
                    dir.Add("minquota", dir.String("quota"));
                    dir.Add("comquota", dir.String("quota"));
                }
            }
            CalcParentValue(costDirList, "value");
            CalcParentValue(costDirList, "minvalue");
            CalcParentValue(costDirList, "comvalue");
        }




        /// <summary>
        /// 获取关联信息
        /// </summary>
        /// <param name="tbDirDict"></param>
        /// <param name="relTable"></param>
        /// <param name="versionId"></param>
        /// <param name="key"></param>
        /// <param name="relId"></param>
        /// <returns></returns>
        private BsonDocument GetRelInfo(Dictionary<string, List<BsonDocument>> tbDirDict, string relTable, string versionId, string key,int relId)
        {
            if (!tbDirDict.ContainsKey(relTable))
            {
                tbDirDict.Add(relTable, GetPolicyDirs(relTable, versionId, key));
            }
            TableRule dirRule = new TableRule(relTable + "Dir");
            string dirKey = dirRule.PrimaryKey;
            return tbDirDict[relTable].SingleOrDefault(s => s.Int(dirKey) == relId);//获取工程量
            
        }


        /// <summary>
        /// 计算所有成本科目的成本
        /// </summary>
        /// <param name="costList"></param>
        //public decimal CalcCost(List<BsonDocument> costList,string projId,string versionId)
        //{
        //   // PolicyVersionBll versionBll = PolicyVersionBll._(_ctx);
        //    TreeHandle.CalcLeafNode(costList);
        //    Init(costList, projId, versionId);
        //    return costList.Where(s=>s.Int("subType") == 0).LeafNode().Sum(s => s.Decimal("value"));
        //}




        /// <summary>
        /// 计算比例
        /// </summary>
        /// <param name="dirList"></param>
        /// <param name="calcKey"></param>
        /// <param name="rateKey"></param>
        /// <param name="decimals"></param>
        public void CalcRateOfTotal(IEnumerable<BsonDocument> dirList,string calcKey,string rateKey,int decimals = 2)
        {
            decimal totalCost = dirList.Sum(s => s.Decimal(calcKey));
            foreach (var dir in dirList)
            {
                decimal rate = default(decimal);
                decimal value = dir.Decimal(calcKey);
                if (totalCost > 0m)
                {
                    rate = value / totalCost;
                }
                dir.Add(rateKey, rate.ToString());
            }
        }

        /// <summary>
        /// 计算父节点的科目支出总值，传入的科目需要已经计算出叶子节点
        /// </summary>
        /// <param name="costDirList">科目列表</param>
        private void CalcParentCost(List<BsonDocument> costDirList)
        {
            foreach (var dir in costDirList.OrderBy(s=>s.String("nodeKey")))
            {
                if (dir.Int("isLeaf") != 1)//非叶子节点
                {
                    var parentKey = dir.String(TreeHandle.NodeKey) + ".";
                    var leafChilds = costDirList.Where(s => s.String(TreeHandle.NodeKey).StartsWith(parentKey) && s.Int("isLeaf") == 1);
                    var total = leafChilds.Sum(s => s.Decimal("value"));
                    dir["value"] = total.ToString();
                }
            }
        }

        


        /// <summary>
        /// 从模板拷贝产品决策中部门数据信息
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="name"></param>
        /// <param name="projId">项目Id</param>
        /// <param name="versionId"></param>
        /// <param name="fields">要拷贝的字段数组</param>
        public void CopyFromTemplate(string tableName, string name, string projId, string versionId, string[] fields)
        {
            TableRule tableRule = new TableRule(tableName);
            string primaryKey = tableRule.ColumnRules.Where(t => t.IsPrimary == true).First().Name;
            BsonDocument template = _ctx.FindOneByQuery(tableName, Query.EQ("isTemplate", "1"));//找出系统模板
            BsonDocument dirParent = new BsonDocument { { "versionId", versionId }, { "name", name }, { "projId", projId } };//挂载目录的
            var projNodeResult = _ctx.Insert(tableName, dirParent);
            List<BsonDocument> templateDirs = _ctx.FindAllByQuery(tableName + "Dir", Query.EQ(primaryKey, template.String(primaryKey))).ToList();
            Dictionary<string, string> keyValueDict = new Dictionary<string, string>();
            keyValueDict.Add(primaryKey, projNodeResult.BsonInfo.String(primaryKey));
            CopyTreeTable(tableName + "Dir", templateDirs,projId,versionId, fields, keyValueDict, false);//拷贝目录信息
        }

        /// <summary>
        /// 上一个版本拷贝
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="name"></param>
        /// <param name="projId"></param>
        /// <param name="preVersionId">上一个版本Id</param>
        /// <param name="versionId"></param>
        /// <param name="fields">要拷贝的字段数组</param>
        public void CopyFromPreVersion(string tableName, string name, string projId, string preVersionId, string versionId, string[] fields)
        {
            TableRule tableRule = new TableRule(tableName);
            string primaryKey = tableRule.ColumnRules.Where(t => t.IsPrimary == true).First().Name;
            BsonDocument preNode = _ctx.FindOneByQuery(tableName, Query.And(Query.EQ("versionId", preVersionId), Query.EQ("projId", projId)));//上一个版本的数据
            BsonDocument dirParent = new BsonDocument { { "versionId", versionId }, { "name", name }, { "projId", projId } };//挂载目录的
            var projNodeResult = _ctx.Insert(tableName, dirParent);
            List<BsonDocument> templateDirs = _ctx.FindAllByQuery(tableName + "Dir", Query.EQ(primaryKey, preNode.String(primaryKey))).ToList();//获取上一个节点的目录结构
            Dictionary<string, string> keyValueDict = new Dictionary<string, string>();
            keyValueDict.Add(primaryKey, projNodeResult.BsonInfo.String(primaryKey));
            CopyTreeTable(tableName + "Dir", templateDirs, projId, versionId, fields, keyValueDict, true);//拷贝目录信息
        }



        /// <summary>
        /// 从模板中拷贝目录结构
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="name"></param>
        /// <param name="versionId"></param>
        /// <param name="fields"></param>
        private void CopyPolicyDateFromTemplate(string tableName, string name, string projId, string versionId, string[] fields)
        {
            TableRule tableRule = new TableRule(tableName);
            string primaryKey = tableRule.ColumnRules.Where(t => t.IsPrimary == true).First().Name;
            BsonDocument template = _ctx.FindOneByQuery(tableName, Query.EQ("isTemplate", "1"));//找出系统模板
            BsonDocument dirParent = new BsonDocument { { "versionId", versionId }, { "name", name }, { "projId", projId } };//挂载目录的
            var projNodeResult = _ctx.Insert(tableName, dirParent);
            List<BsonDocument> templateDirs = _ctx.FindAllByQuery(tableName + "Dir", Query.EQ(primaryKey, template.String(primaryKey))).ToList();
            Dictionary<string, string> keyValueDict = new Dictionary<string, string>();
            keyValueDict.Add(primaryKey, projNodeResult.BsonInfo.String(primaryKey));
            CopyTreeTable(tableName + "Dir", templateDirs,projId,versionId, fields, keyValueDict, false);//拷贝目录信息
        }

        /// <summary>
        /// 复制树形结构目录
        /// </summary>
        /// <param name="tableName">树形结构的表名称</param>
        /// <param name="dataList">被复制的树型结构列表</param>
        /// <param name="fields">要复制的字段</param>
        /// <param name="keyValueDict">固定加入的值：比如树形目录挂在项目下可能就要加入项目的主键Id</param>
        /// <returns></returns>
        public List<BsonDocument> CopyTreeTable(string tableName, List<BsonDocument> dataList,string projId,string versionId, string[] fields, Dictionary<string, string> keyValueDict, bool hasTemplateId)
        {
            TableRule tableRule = new TableRule(tableName);
            string primaryKey = tableRule.ColumnRules.Where(t => t.IsPrimary == true).First().Name;

            int count = dataList.Count;
            List<BsonDocument> datas = new List<BsonDocument>(count);
            Dictionary<string, string> keyDict = new Dictionary<string, string>();
            Dictionary<string, List<BsonDocument>> tbDirDict = new Dictionary<string, List<BsonDocument>>();

            foreach (var d in dataList.OrderBy(s => s.String("nodeKey")))
            {
                BsonDocument record = new BsonDocument();
                foreach (var s in fields)
                {
                    record.Add(s, d.String(s));
                    record.Add(s + "_bak", d.String(s));//初始化暂存区数据
                }

                foreach (var dict in keyValueDict)
                {
                    record.Add(dict.Key, dict.Value);
                }

                string keyValue = d.String(primaryKey);
                record.Add("srcId", keyValue);//记录被复制节点的主键值
                string copyKey = "srcId";
                if (hasTemplateId)//非模板拷贝,传入拷贝"templateId"
                {
                    copyKey = "srcId";//模板
                }
                else//模板拷贝
                {
                    record.Add("templateId", keyValue);//记录模板的字段值
                    copyKey = "templateId";//模板
                }
                CalcRelId(tbDirDict, d, record, projId, versionId, copyKey);//计算工程量关联的信息
                string oldNodePid = d.String("nodePid");//获取被复制节点的父节点id；
                string nodePid = "0";

                //获取对应父节点
                if (keyDict.ContainsKey(oldNodePid))
                {
                    nodePid = keyDict[oldNodePid];
                }
                record.Add("nodePid", nodePid);

                var result = _ctx.Insert(tableName, record);
                if (result.Status == Status.Successful)
                {
                    keyDict.Add(d.String(primaryKey), result.BsonInfo.String(primaryKey));//记录树形对应节点的主键对
                }
                datas.Add(record);
            }
            return datas;
        }

        /// <summary>
        /// 成本科目计算工程量的关联数据
        /// </summary>
        /// <param name="tbNameDict">其它部门的同版本的目录数据字典</param>
        /// <param name="template">被拷贝的科目信息</param>
        /// <param name="cost">新建的当前版本的科目信息</param>
        /// <param name="projId">项目Id</param>
        /// <param name="versionId"></param>
        /// <param name="copyKey">relId所要拷贝的Id（如果是拷贝自模板则传入templateId,项目则是srcId）</param>
        public void CalcRelId(Dictionary<string, List<BsonDocument>> tbNameDict,BsonDocument template,BsonDocument cost,string projId,string versionId,string copyKey)
        {
            string tbName = template.String("relTable");
            if (!string.IsNullOrEmpty(tbName))
            {
                string dirName = tbName + "Dir";
                TableRule dirRule = new TableRule (dirName);
                string dirKey = dirRule.ColumnRules.Single(s => s.IsPrimary).Name;

                if (!tbNameDict.ContainsKey(tbName))
                {
                    tbNameDict.Add(tbName, GetCostDirs(tbName, projId, versionId));
                }

                //获取模板Id为对应RelId的数据
                BsonDocument targetData = tbNameDict[tbName].Where(s => s.Int(copyKey) == template.Int("relId")).FirstOrDefault();
                cost.Add("relId", targetData.String(dirKey));
                cost.Add("relTable", tbName);
            }
        }

        /// <summary>
        /// 通过表名，项目id，版本id获取对应表的目录信息
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="projId"></param>
        /// <param name="versionId"></param>
        /// <returns></returns>
        private List<BsonDocument> GetCostDirs(string tbName, string projId, string versionId)
        {
            List<BsonDocument> dirs = new List<BsonDocument>();
            if (!string.IsNullOrEmpty(tbName))
            {
                string dirName = tbName + "Dir";
                TableRule tbRule = new TableRule(tbName);
                string primaryKey = tbRule.ColumnRules.Single(s => s.IsPrimary).Name;

                TableRule dirRule = new TableRule(dirName);
                string dirKey = dirRule.ColumnRules.Single(s => s.IsPrimary).Name;
                //找出对应从模板拷贝的目录信息
                BsonDocument dirParent = _ctx.FindOneByQuery(tbName, Query.And(Query.EQ("versionId", versionId), Query.EQ("projId", projId)));
                dirs = _ctx.FindAllByQuery(dirName, Query.EQ(primaryKey, dirParent.String(primaryKey))).ToList();
            }
            return dirs;
        }

       /// <summary>
       /// 
       /// </summary>
       /// <param name="tbName">部门表名：树形表为其后面+Dir</param>
       /// <param name="versionId">版本Id</param>
       /// <param name="sumKey">汇总Key</param>
       /// <returns></returns>
        private List<BsonDocument> GetPolicyDirs(string tbName, string versionId,string sumKey)
        {
             List<BsonDocument> dirs = new List<BsonDocument>();
            if (!string.IsNullOrEmpty(tbName))
            {
                string dirName = tbName + "Dir";
                TableRule tbRule = new TableRule(tbName);
                string primaryKey = tbRule.ColumnRules.Single(s => s.IsPrimary).Name;

                TableRule dirRule = new TableRule(dirName);
                string dirKey = dirRule.ColumnRules.Single(s => s.IsPrimary).Name;
                //找出对应从模板拷贝的目录信息
                BsonDocument dirParent = _ctx.FindOneByQuery(tbName, Query.EQ("versionId", versionId));
                dirs = _ctx.FindAllByQuery(dirName, Query.EQ(primaryKey, dirParent.String(primaryKey))).ToList();
            }
            TreeHandle.CalcLeafNode(dirs);
            CalcParentValue(dirs, sumKey);//计算面积
            return dirs;
        }

        /// <summary>
        /// 汇总树形结构父节点的值
        /// 传入的树形结构需要已经计算出叶子节点
        /// </summary>
        /// <param name="treeList">树形结构列表</param>
        /// <param name="sumKey">要汇总的值的Key</param>
        public void CalcParentValue(IEnumerable<BsonDocument> treeList,string sumKey)
        {
            List<string> shieldStr = new List<string>{"Value","value"};//屏蔽包含有这个单词
            bool flag =shieldStr.Contains(sumKey);
            foreach (var dir in treeList.OrderBy(s => s.String(TreeHandle.NodeKey)))
            {
                if (dir.Int("isLeaf") != 1)//非叶子节点
                {
                    var parentKey = dir.String(TreeHandle.NodeKey) + ".";
                    var leafChilds = treeList.Where(s => s.String(TreeHandle.NodeKey).StartsWith(parentKey) && s.Int("isLeaf") == 1);
                    var total = leafChilds.Sum(s => s.Decimal(sumKey));
                   
                    if (dir.Contains(sumKey)) 
                    { 
                        dir[sumKey] = total.ToString();
                    }
                    else {
                        dir.Add(sumKey, total.ToString());
                    }
                    if (flag == false)
                    {
                        var mintotal = leafChilds.Sum(s => s.Decimal(sumKey + "_min"));
                        var comtotal = leafChilds.Sum(s => s.Decimal(sumKey + "_com"));
                        if (dir.Contains(sumKey + "_min"))
                        {
                            dir[sumKey + "_min"] = mintotal.ToString();
                        }
                        else
                        {
                            dir.Add(sumKey + "_min", mintotal.ToString());
                        }
                        if (dir.Contains(sumKey + "_com"))
                        {
                            dir[sumKey + "_com"] = comtotal.ToString();
                        }
                        else
                        {
                            dir.Add(sumKey + "_com", comtotal.ToString());
                        }
                    }
                   
                }
            }
        }

    }
}
