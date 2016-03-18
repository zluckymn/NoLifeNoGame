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
    /// 计算冲突决策处理类
    /// </summary>
    public class ConflictFeedbackBll
    {
        #region 构造函数
        /// <summary>
        /// 类私有变量
        /// </summary>
        private readonly DataOperation _ctx = null;

        /// <summary>
        /// 封闭当前默认构造函数
        /// </summary>
        public ConflictFeedbackBll()
        {
            _ctx = new DataOperation();
        }

        public ConflictFeedbackBll(DataOperation ctx)
        {
            _ctx = ctx;
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static ConflictFeedbackBll _()
        {
            return new ConflictFeedbackBll();
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static ConflictFeedbackBll _(DataOperation ctx)
        {
            return new ConflictFeedbackBll(ctx);
        }
        #endregion


        /// <summary>
        /// 计算冲突决策所需要的值
        /// </summary>
        /// <param name="projId"></param>
        /// <returns></returns>
        public BsonDocument CalcConflictData(string projId,string versionId)
        {
            //项目节点
            PolicyVersionBll versionBll = PolicyVersionBll._(_ctx);
            CostBll costBll = CostBll._(_ctx);
            decimal projCycle = 0m;
            DateTime start, end;//项目开始结束时间
            versionBll.GetStartEndDate(versionId,out start,out end);
            projCycle = (end - start).Days / 30;
           
            var costDirs = versionBll.GetTableDirs(PolicyDept.Cost, projId, versionId);//成本科目
            TreeHandle.CalcLeafNode(costDirs);
            costBll.CalcTotalCost(costDirs, projId, versionId);
            //decimal totalCost = costBll.CalcCost(costDirs,projId,versionId);//计算成本总值
            decimal totalCost = costDirs.LeafNode().Where(s => s.Int("subType") == 0).Sum(s => s.Decimal("value"));
            decimal mintotalCost = costDirs.LeafNode().Where(s => s.Int("subType") == 0).Sum(s => s.Decimal("minvalue"));//悲观情况
            decimal comtotalCost = costDirs.LeafNode().Where(s => s.Int("subType") == 0).Sum(s => s.Decimal("comvalue"));//客观情况

            //融资周期、资本利息、销售周期、销售费用
            var repayList = _ctx.FindAllByQuery("RepayDetail", Query.EQ("versionId", versionId)).ToList();//获取还款计划
            var sellList = _ctx.FindAllByQuery("SellDetail", Query.EQ("versionId", versionId)).ToList();//销售计划

            repayList = repayList.Where(s => DateTimeHelper.LTE(start, s.Date("date")) && DateTimeHelper.LTE(s.Date("date"), end)).ToList();
            sellList = sellList.Where(s => DateTimeHelper.LTE(start, s.Date("date")) && DateTimeHelper.LTE(s.Date("date"), end)).ToList();

            var financeList = repayList.Where(s => s.Decimal("loan") > 0m || s.Decimal("repayVal") > 0m || s.Decimal("interest") > 0m);//融资周期
            var financeCycle = financeList.Count();
            //悲观融资周期
            var minfinanceList = repayList.Where(s => s.Decimal("minLoan") > 0m || s.Decimal("minRepayVal") > 0m || s.Decimal("minInterest") > 0m);//融资周期
            var minfinanceCycle = minfinanceList.Count();
            //客观融资周期
            var comfinanceList = repayList.Where(s => s.Decimal("comLoan") > 0m || s.Decimal("comRepayVal") > 0m || s.Decimal("comInterest") > 0m);//融资周期
            var comfinanceCycle = comfinanceList.Count();

            var sells = sellList.Where(s => s.Decimal("salesVal") > 0m).ToList();//销售周期
            var sellCycle = sells.Count;
            //悲观销售周期
            var minsells = sellList.Where(s => s.Decimal("minSalesVal") > 0m).ToList();//销售周期
            var minsellCycle = minsells.Count;
            //客观销售周期
            var comsells = sellList.Where(s => s.Decimal("comSalesVal") > 0m).ToList();//销售周期
            var comsellCycle = comsells.Count;

            var totalInterest = repayList.Sum(s => s.Decimal("interest"));//总利息
            //悲观总利息
            var mintotalInterest = repayList.Sum(s => s.Decimal("minInterest"));//总利息
            //客观总利息
            var comtotalInterest = repayList.Sum(s => s.Decimal("comInterest"));//总利息


            //var totalSell = sellList.Sum(s => s.Decimal("salesVal"));//总销售额
            var totalIn = costDirs.LeafNode().Where(s => s.Int("subType") == 1).Sum(s=>s.Decimal("value"));//总收入
            var mintotalIn = costDirs.LeafNode().Where(s => s.Int("subType") == 1).Sum(s => s.Decimal("minvalue"));//悲观总收入
            var comtotalIn = costDirs.LeafNode().Where(s => s.Int("subType") == 1).Sum(s => s.Decimal("comvalue"));//客观总收入
           // List<BsonDocument> ttt = costDirs.Where(s => s.String("销售费用") == "销售费用").ToList();
            var sellCost = costDirs.FirstOrDefault(s => s.String("name") == "销售费用").Decimal("value");
            var minsellCost = costDirs.FirstOrDefault(s => s.String("name") == "销售费用").Decimal("minvalue");//悲观销售费用
            var comsellCost = costDirs.FirstOrDefault(s => s.String("name") == "销售费用").Decimal("comvalue");//悲观销售费用

            //利润率
            var profitRate = 0m;
            var totalFee = totalCost;//总支出
            if (totalIn > 0m) profitRate = (totalIn - totalFee) / totalIn;//净利润率 = 利润/总投资收入
            //悲观利润率
            var minprofitRate = 0m;
            var mintotalFee = mintotalCost;//总支出
            if (mintotalIn > 0m) minprofitRate = (mintotalIn - mintotalFee) / mintotalIn;//净利润率 = 利润/总投资收入
            //客观利润率
            var comprofitRate = 0m;
            var comtotalFee = comtotalCost;//总支出
            if (comtotalIn > 0m) comprofitRate = (comtotalIn - comtotalFee) / comtotalIn;//净利润率 = 利润/总投资收入




            //面积相关
            var decisionList = versionBll.GetTableDirsByVersionId(PolicyDept.DesignArea, versionId);//设计面积
            var marketingList = versionBll.GetTableDirsByVersionId(PolicyDept.MarketingArea, versionId);//市场面积

            var totalArea = decisionList.Where(s => s.String("name") == "总建筑面积").OrderBy(s=>s.String("nodeKey")).FirstOrDefault().Decimal("areaValue");//总建筑面积
            var upArea = decisionList.Where(s => s.String("name") == "地上建筑面积").OrderBy(s => s.String("nodeKey")).FirstOrDefault().Decimal("areaValue");
            var downArea = decisionList.Where(s => s.String("name") == "地下建筑面积").OrderBy(s => s.String("nodeKey")).FirstOrDefault().Decimal("areaValue");
            var jirongArea = decisionList.Where(s => s.String("name") == "计容建筑面积").OrderBy(s => s.String("nodeKey")).FirstOrDefault().Decimal("areaValue");
            var bujirongArea = totalArea - jirongArea;

            var sellArea = marketingList.SingleOrDefault(s => s.String("name") == "总销售面积").Decimal("areaValue");//可售面积
            var noSellArea = totalArea - sellArea;


            //产品配置价格
            var jingguang = costDirs.FirstOrDefault(s => s.String("name") == "景观工程").Decimal("quota");//景观工程
            var gonggong = costDirs.FirstOrDefault(s => s.String("name") == "公共区域装修工程").Decimal("quota");//公共区域装修工程
            var wailimian = costDirs.FirstOrDefault(s => s.String("name").Trim() == "外墙装修（专业）").Decimal("quota");//公共区域装修工程

            return new BsonDocument { { "projCycle", projCycle.ToString() }, { "totalCost", totalCost.ToString() }, { "financeCycle", financeCycle.ToString() },{ "minfinanceCycle", minfinanceCycle.ToString() },{ "comfinanceCycle", comfinanceCycle.ToString() },
                                    {"sellCycle",sellCycle.ToString()},{"minsellCycle",minsellCycle.ToString()},{"comsellCycle",comsellCycle.ToString()},
                                    {"sellCost",sellCost.ToString()},{"minsellCost",minsellCost.ToString()},{"comsellCost",comsellCost.ToString()},
                                    {"upArea",upArea.ToString()},{"downArea",downArea.ToString()},{"totalArea",totalArea.ToString()},{"jirongArea",jirongArea.ToString()},
                                    {"sellArea",sellArea.ToString()},{"noSellArea",noSellArea.ToString()},{"bujirongArea",bujirongArea.ToString()},
                                    {"profitRate",profitRate.ToString()},{"minprofitRate",minprofitRate.ToString()},{"comprofitRate",comprofitRate.ToString()},
                                    {"totalInterest",totalInterest.ToString()},{"mintotalInterest",mintotalInterest.ToString()},{"comtotalInterest",comtotalInterest.ToString()},
                                    {"jingguang",jingguang.ToString()},{"gonggong",gonggong.ToString()},{"wailimian",wailimian.ToString()}

            };
        }

        /// <summary>
        /// 计算最大最小时间差距
        /// </summary>
        /// <param name="list">列表</param>
        /// <param name="dateKey">时间</param>
        /// <returns></returns>
        public int CalcDateRange(IEnumerable<BsonDocument> list, string dateKey)
        {
            DateTime start = default(DateTime);
            DateTime end = start;
            if (list.Any())
            {
                start = list.Min(s => s.Date(dateKey));
                end = list.Max(s => s.Date(dateKey));
                return (end - start).Days;
            }
            return 0;
        }
    }
}
