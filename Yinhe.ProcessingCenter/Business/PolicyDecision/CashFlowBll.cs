using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Yinhe.ProcessingCenter.DataRule;
using MongoDB.Bson;
using Yinhe.ProcessingCenter;
using MongoDB.Driver.Builders;
using MongoDB.Driver;

///<summary>
///决策管理
///</summary>
namespace Yinhe.ProcessingCenter.Business.PolicyDecision
{
    /// <summary>
    /// 现金流处理类
    /// </summary>
    public class CashFlowBll
    {
        #region 构造函数
        /// <summary>
        /// 类私有变量
        /// </summary>
        private readonly DataOperation _ctx = null;

        /// <summary>
        /// 封闭当前默认构造函数
        /// </summary>
        public CashFlowBll()
        {
            _ctx = new DataOperation();
        }

        public CashFlowBll(DataOperation ctx)
        {
            _ctx = ctx;
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static CashFlowBll _()
        {
            return new CashFlowBll();
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static CashFlowBll _(DataOperation ctx)
        {
            return new CashFlowBll(ctx);
        }
        #endregion

        /// <summary>
        /// 初始化现金流的相关信息
        /// </summary>
        /// <param name="projId">项目Id</param>
        /// <param name="versionId">版本Id</param>
        /// <returns></returns>
        public BsonDocument InitCashFlowInfo(string projId,string versionId)
        {
            var costBll = CostBll._(_ctx);
            var cashInfo = new BsonDocument();
            var costAccounts = GetCostDirList(projId, versionId);//获取所有成本科目Id
            costBll.Init(costAccounts, projId, versionId);//初始化
            //cashInfo.Add("costAccounts", costAccounts);
            return cashInfo;
        }

        /// <summary>
        /// 获取所有科目
        /// </summary>
        /// <param name="projId"></param>
        /// <param name="versionId"></param>
        /// <returns></returns>
        public List<BsonDocument> GetCostDirList(string projId, string versionId)
        {
            var parent = _ctx.FindOneByQuery("Cost", Query.And(Query.EQ("projId", projId), Query.EQ("versionId", versionId)));
            return _ctx.FindAllByQuery("CostDir", Query.EQ("CostId", parent.String("CostId"))).ToList();
        }

        /// <summary>
        /// 计算成本科目每个月份要支付的额度
        /// </summary>
        /// <param name="costAccounts">成本科目</param>
        /// <param name="months">所有月份</param>
        /// <param name="yearNodes">每个月份所对应的项目节点字典</param>
        /// <param name="costRates">科目对应节点支付比例</param>
        public void CalcNodePay(List<BsonDocument> costAccounts,List<string> months, Dictionary<string, List<BsonDocument>> yearNodes, List<BsonDocument> costRates,string versionId)
        {
            string tbName = "SellDetail";
            List<BsonDocument> sellDetails = _ctx.FindAllByQuery(tbName,Query.EQ("versionId", versionId)).ToList();//总销售收入
            foreach (var costAccount in costAccounts)
            {
                var costDirId = costAccount.String("CostDirId");//科目Id
                var costType = costAccount.Int("costType");//支付类型
                foreach (var month in months)
                {
                    decimal result = 0;
                    decimal minresult = 0;
                    decimal comresult = 0;
                    var baseRate = 0.01m;
                    if (costType == 0)//节点支付
                    {
                        if (yearNodes.ContainsKey(month))
                        {
                            foreach (var node in yearNodes[month])
                            {
                                var projNodeId = node.String("projNodeDirId");//节点Id
                                var total = costAccount.Decimal("value");
                                var mintotal = costAccount.Decimal("minvalue");
                                var comtotal = costAccount.Decimal("comvalue");
                                //获取科目节点所对应的比例值
                                var rate = costRates.SingleOrDefault(s => (s.String("costDirId") == costDirId) && (s.String("projNodeId") == projNodeId));
                                if (rate.Decimal("rate") > 0)
                                {
                                    var nodeRate = rate.Decimal("rate");
                                    result += (total * nodeRate * baseRate);
                                    minresult += (mintotal * nodeRate * baseRate);
                                    comresult += (comtotal * nodeRate * baseRate);
                                }
                            }
                            costAccount.Add(month, result.ToString());
                            costAccount.Add(month+"_min", minresult.ToString());
                            costAccount.Add(month+"_com", comresult.ToString());
                        }
                    }
                    else if(costType == 1)//按月均摊
                    {
                        var totalPay = costAccount.Decimal("value");
                        var pay = 0m;
                        pay = (totalPay / months.Count);
                        costAccount.Add(month, pay.ToString());
                        //增加悲观 客观
                        var mintotalPay = costAccount.Decimal("minvalue");
                        var minpay = 0m;
                        minpay = (mintotalPay / months.Count);
                        costAccount.Add(month + "_min", minpay.ToString());
                        var comtotalPay = costAccount.Decimal("comvalue");
                        var compay = 0m;
                        compay = (comtotalPay / months.Count);
                        costAccount.Add(month+"_com", compay.ToString());
                    }
                    else if (costType == 2)//销售税费
                    {
                        var rate = costAccount.Decimal("rate");//比率
                        var sellInfo = sellDetails.SingleOrDefault(s => s.String("date") == month);
                        var pay = rate * sellInfo.Decimal("salesVal");
                        costAccount.Add(month, pay.ToString());
                        var minpay = rate * sellInfo.Decimal("minSalesVal");
                        costAccount.Add(month+"_min", minpay.ToString());
                        var compay = rate * sellInfo.Decimal("comSalesVal");
                        costAccount.Add(month + "_com", compay.ToString());
                    }
                }
            }
            CalcParentMonthCost(costAccounts, months);
        }

        /// <summary>
        /// 计算父节点每个月份要支付的成本
        /// </summary>
        /// <param name="costAccounts"></param>
        /// <param name="months"></param>
        private void CalcParentMonthCost(List<BsonDocument> costAccounts, List<string> months)
        {
            CostBll costBll = CostBll._(_ctx);
            foreach (var month in months)
            {
               costBll.CalcParentValue(costAccounts, month);
            }
        }


        /// <summary>
        /// 计算成本科目每个季度要支付的额度
        /// </summary>
        /// <param name="costAccounts">成本科目</param>
        /// <param name="months">所有月份</param>
        /// <param name="yearNodes">每个月份所对应的项目节点字典</param>
        /// <param name="costRates">科目对应节点支付比例</param>
        public void CalcSeasonPay(List<BsonDocument> costAccounts, Season season, Dictionary<string, List<BsonDocument>> seasonNodes, List<BsonDocument> costRates,string versionId)
        {
            string tbName = "SellDetail";
            List<BsonDocument> sellDetails = _ctx.FindAllByQuery(tbName, Query.EQ("versionId", versionId)).ToList();//总销售收入

            var monthCount = season.monthCount;
            foreach (var costAccount in costAccounts)
            {
                var costDirId = costAccount.String("CostDirId");//科目Id
                var costType = costAccount.Int("costType");//支付类型
                foreach (var seasonMonth in season.SeasonMonth)//遍历季度
                {
                    var key = seasonMonth.key;
                        decimal result = 0;
                        decimal minresult = 0;//悲观结果
                        decimal comresult = 0;//客观结果
                        var baseRate = 0.01m;
                        if (costType == 0)//节点支付
                        {
                            if (seasonNodes.ContainsKey(key))
                            {
                                foreach (var node in seasonNodes[key])
                                {
                                    var projNodeId = node.String("projNodeDirId");//节点Id
                                    var total = costAccount.Decimal("value");
                                    var mintotal = costAccount.Decimal("minvalue");
                                    var comtotal = costAccount.Decimal("comvalue");
                                    //获取科目节点所对应的比例值
                                    var rate = costRates.FirstOrDefault(s => (s.String("costDirId") == costDirId) && (s.String("projNodeId") == projNodeId));
                                    if (rate.Decimal("rate") > 0)
                                    {
                                        var nodeRate = rate.Decimal("rate");
                                        result += (total * nodeRate * baseRate);
                                        minresult += (mintotal * nodeRate * baseRate);
                                        comresult += (comtotal * nodeRate * baseRate);
                                    }
                                }
                                costAccount.Add(key, result.ToString());
                                costAccount.Add(key+"_min", minresult.ToString());//悲观
                                costAccount.Add(key+"_com", comresult.ToString());//客观
                            }
                        }
                        else if (costType == 1)//按月均摊
                        {
                            var totalPay = costAccount.Decimal("value");
                            var mintotalPay = costAccount.Decimal("minvalue");
                            var comtotalPay = costAccount.Decimal("comvalue");
                            var pay = 0m;
                            var minpay = 0m;
                            var compay = 0m;
                            pay = (totalPay / monthCount);
                            minpay = (mintotalPay / monthCount);
                            compay = (comtotalPay / monthCount);
                            var seasonPay = pay * seasonMonth.months.Count;
                            var minseasonPay = minpay * seasonMonth.months.Count;
                            var comseasonPay = compay * seasonMonth.months.Count;
                            costAccount.Add(key, seasonPay.ToString());
                            costAccount.Add(key+"_min", minseasonPay.ToString());
                            costAccount.Add(key+"_com", comseasonPay.ToString());
                        }
                        else if (costType == 2)
                        {
                            var totalPay = 0m;
                            var mintotalPay = 0m;
                            var comtotalPay = 0m;
                            var rate = costAccount.Decimal("rate");
                            foreach (var date in seasonMonth.months)
                            {
                                string month = date.Month.ToString().PadLeft(2, '0');
                                string dateStr = date.Year + "-" + month;//年月
                                totalPay += sellDetails.SingleOrDefault(s => s.String("date") == dateStr).Decimal("salesVal");
                                mintotalPay += sellDetails.SingleOrDefault(s => s.String("date") == dateStr).Decimal("minSalesVal");
                                comtotalPay += sellDetails.SingleOrDefault(s => s.String("date") == dateStr).Decimal("comSalesVal");
                            }

                            costAccount.Add(key, (totalPay * rate).ToString());
                            costAccount.Add(key+"_min", (mintotalPay * rate).ToString());
                            costAccount.Add(key+"_com", (comtotalPay * rate).ToString());
                        }
                }
            }
            CalcParentSeasonCost(costAccounts,season);
        }

        /// <summary>
        /// 计算父节点每个季度份要支付的成本
        /// </summary>
        /// <param name="costAccounts"></param>
        /// <param name="months"></param>
        private void CalcParentSeasonCost(List<BsonDocument> costAccounts, Season season)
        {
            CostBll costBll = CostBll._(_ctx);
            foreach (var seasonMonth in season.SeasonMonth)//遍历季度
            {
                string key = seasonMonth.key;
                costBll.CalcParentValue(costAccounts, key);
            }
        }

        /// <summary>
        /// 销售权重决策
        /// </summary>
        /// <param name="marketDatas"></param>
        public void CalcMarketData(IEnumerable<BsonDocument> marketDatas)
        {
            foreach (var data in marketDatas)
            {
                var areaVal = data.Decimal("areaValue");
                var maxVal = data.Decimal("maxVal");//乐观售价
                var minVal = data.Decimal("minVal");//悲观售价
                var comVal = data.Decimal("commonVal");//客观售价

                var maxTotal = areaVal * maxVal;
                var minTotal = areaVal * minVal;
                var comTotal = areaVal * comVal;
                var difference = maxTotal - minTotal;//差额
                var comdifference = maxTotal - comTotal;//乐观与客观差额
                var mindifference = comTotal - minTotal;//客观与悲观差额
                data.Add("maxTotal", maxTotal.ToString());
                data.Add("minTotal", minTotal.ToString());
                data.Add("comTotal", comTotal.ToString());
                data.Add("difference", difference.ToString());//乐观悲观差额
                data.Add("differencexc", comdifference.ToString());//乐观与客观差额
                data.Add("differencecn", mindifference.ToString());//客观与悲观差额
            }
        }


       /// <summary>
       /// 月份现金流图表信息
       /// </summary>
       /// <param name="costList"></param>
       /// <param name="yearNodes"></param>
       /// <param name="costRates"></param>
       /// <param name="versionId"></param>
       /// <param name="months"></param>
        public void CalcCashFlowInfo(IEnumerable<BsonDocument> costList, string versionId, List<string> months, out string cashOut, out string cashIn, out string cashPure,out string cashAddup,out string ticks ,out decimal max, out decimal min)
        {
            min = max = default(decimal);
            string SellDetail = "SellDetail";
            List<BsonDocument> sellDetails = _ctx.FindAllByQuery(SellDetail, Query.EQ("versionId", versionId)).ToList();//销售信息

            string RepayDetail = "RepayDetail";
            List<BsonDocument> repayDetails = _ctx.FindAllByQuery(RepayDetail, Query.EQ("versionId", versionId)).ToList();//融资信息信息

            StringBuilder sb = new StringBuilder();
            StringBuilder sbOut = new StringBuilder();
            StringBuilder sbIn = new StringBuilder();
            StringBuilder sbKey = new StringBuilder();
            StringBuilder sbAdd = new StringBuilder();
            sb.Append("[");
            sbOut.Append("[");
            sbIn.Append("[");
            sbKey.Append("[");
            sbAdd.Append("[");
            int index = 1;
           
            var preValue = 0m;
            foreach (var month in months)
            {
                var cost = costList.LeafNode().Sum(s => s.Decimal(month));//成本支出
                var sell = sellDetails.Where(s => DateTimeHelper.CompareYM(s.String("date"), month)).Sum(s => s.Decimal("salesVal"));
                var loan = repayDetails.Where(s => DateTimeHelper.CompareYM(s.String("date"), month)).Sum(s => s.Decimal("loan"));//借款
                var repayVal = repayDetails.Where(s => DateTimeHelper.CompareYM(s.String("date"), month)).Sum(s => s.Decimal("repayVal"));//归还借款
                var interest = repayDetails.Where(s => DateTimeHelper.CompareYM(s.String("date"), month)).Sum(s => s.Decimal("interest"));//利息

                string key = index.ToString();
                MonthKey(sbKey, key, month);
                
                //净现金流
                var tempTotal = sell + loan - repayVal - interest - cost;
                sb.Append("{");
                sb.Append("\"year\":\"" + key + "\",");
                sb.Append("\"value\":\""+tempTotal.ToString()+"\"");
                sb.Append("},");

                //现金流出
                var totalOut = -(repayVal + interest + cost);
                sbOut.Append("{");
                sbOut.Append("\"year\":\"" + key + "\",");
                sbOut.Append("\"value\":\"" + totalOut.ToString() + "\"");
                sbOut.Append("},");

                //现金流入
                var totalIn = sell + loan;
                sbIn.Append("{");
                sbIn.Append("\"year\":\"" + key + "\",");
                sbIn.Append("\"value\":\"" + totalIn.ToString() + "\"");
                sbIn.Append("},");

                //累计现金流
                var addup = preValue + tempTotal;
                sbAdd.Append("{");
                sbAdd.Append("\"year\":\"" + key + "\",");
                sbAdd.Append("\"value\":\"" + addup.ToString() + "\"");
                sbAdd.Append("},");

                preValue = addup;
                index++;

                max = Max(tempTotal, totalOut, totalIn,addup, max);
                min = Min(tempTotal, totalOut, totalIn,addup, min);
            }
            
            cashPure = sb.ToString().TrimEnd(',') + "]";
            cashOut = sbOut.ToString().TrimEnd(',') + "]";
            cashIn = sbIn.ToString().TrimEnd(',') + "]";
            cashAddup = sbAdd.ToString().TrimEnd(',') + "]";
            ticks = sbKey.ToString().TrimEnd(',') + "]";

            max *= 1.2m;
            min *= 1.2m;
        }


        /// <summary>
        /// 计算现金流信息
        /// </summary>
        /// <param name="costList">成本科目列表</param>
        /// <param name="versionId">版本Id</param>
        /// <param name="months">月份信息</param>
        /// <param name="cashPure">净现金流</param>
        /// <param name="cashOut">现金流出</param>
        /// <param name="cashIn">现金流入</param>
        /// <param name="cashAddup">累计现金流</param>
        public void CalcCashFlowInfo(IEnumerable<BsonDocument> costList, string versionId, List<string> months, BsonDocument cashPure, BsonDocument cashOut, BsonDocument cashIn, BsonDocument cashAddup)
        {
            string SellDetail = "SellDetail";
            List<BsonDocument> sellDetails = _ctx.FindAllByQuery(SellDetail, Query.EQ("versionId", versionId)).ToList();//销售信息

            string RepayDetail = "RepayDetail";
            List<BsonDocument> repayDetails = _ctx.FindAllByQuery(RepayDetail, Query.EQ("versionId", versionId)).ToList();//融资信息信息

            string preKey = string.Empty;
            foreach (var month in months)
            {
                var cost = costList.LeafNode().Sum(s => s.Decimal(month));//成本支出
                var sell = sellDetails.Where(s => DateTimeHelper.CompareYM(s.String("date"), month)).Sum(s => s.Decimal("salesVal"));
                var loan = repayDetails.Where(s => DateTimeHelper.CompareYM(s.String("date"), month)).Sum(s => s.Decimal("loan"));//借款
                var repayVal = repayDetails.Where(s => DateTimeHelper.CompareYM(s.String("date"), month)).Sum(s => s.Decimal("repayVal"));//归还借款
                var interest = repayDetails.Where(s => DateTimeHelper.CompareYM(s.String("date"), month)).Sum(s => s.Decimal("interest"));//利息
                //悲观
                var mincost = costList.LeafNode().Sum(s => s.Decimal(month+"_min"));//成本支出
                var minsell = sellDetails.Where(s => DateTimeHelper.CompareYM(s.String("date"), month)).Sum(s => s.Decimal("minSalesVal"));
                var minloan = repayDetails.Where(s => DateTimeHelper.CompareYM(s.String("date"), month)).Sum(s => s.Decimal("minLoan"));//借款
                var minrepayVal = repayDetails.Where(s => DateTimeHelper.CompareYM(s.String("date"), month)).Sum(s => s.Decimal("minRepayVal"));//归还借款
                var mininterest = repayDetails.Where(s => DateTimeHelper.CompareYM(s.String("date"), month)).Sum(s => s.Decimal("minInterest"));//利息
                //客观
                var comcost = costList.LeafNode().Sum(s => s.Decimal(month+"_com"));//成本支出
                var comsell = sellDetails.Where(s => DateTimeHelper.CompareYM(s.String("date"), month)).Sum(s => s.Decimal("comSalesVal"));
                var comloan = repayDetails.Where(s => DateTimeHelper.CompareYM(s.String("date"), month)).Sum(s => s.Decimal("comLoan"));//借款
                var comrepayVal = repayDetails.Where(s => DateTimeHelper.CompareYM(s.String("date"), month)).Sum(s => s.Decimal("comRepayVal"));//归还借款
                var cominterest = repayDetails.Where(s => DateTimeHelper.CompareYM(s.String("date"), month)).Sum(s => s.Decimal("comInterest"));//利息





                //净现金流
                var tempTotal = sell + loan - repayVal - interest - cost;
                cashPure.Add(month, tempTotal.ToMoney());
                var mintempTotal = minsell + minloan - minrepayVal - mininterest - mincost;
                cashPure.Add(month+"_min", mintempTotal.ToMoney());
                var comtempTotal = comsell + comloan - comrepayVal - cominterest - comcost;
                cashPure.Add(month + "_com", comtempTotal.ToMoney());


                //现金流出
                var totalOut = -(repayVal + interest + cost);
                cashOut.Add(month, totalOut.ToMoney());
                var mintotalOut = -(minrepayVal + mininterest + mincost);
                cashOut.Add(month + "_min", mintotalOut.ToMoney());
                var comtotalOut = -(comrepayVal + cominterest + comcost);
                cashOut.Add(month + "_com", comtotalOut.ToMoney());

                //现金流入
                var totalIn = sell + loan;
                cashIn.Add(month, totalIn.ToMoney());
                var mintotalIn = minsell + minloan;
                cashIn.Add(month + "_min", mintotalIn.ToMoney());
                var comtotalIn = comsell + comloan;
                cashIn.Add(month + "_com", comtotalIn.ToMoney());

                //累计现金流
                var addup = cashAddup.Decimal(preKey) + cashPure.Decimal(month);
                cashAddup.Add(month, addup.ToMoney());
                var minaddup = cashAddup.Decimal(preKey+"_min") + cashPure.Decimal(month+"_min");
                cashAddup.Add(month + "_min", minaddup.ToMoney());
                var comaddup = cashAddup.Decimal(preKey + "_com") + cashPure.Decimal(month + "_com");
                cashAddup.Add(month+"_com", comaddup.ToMoney());
               
                preKey = month;
            }
        }

        /// <summary>
        /// 季度现金流图形报表信息
        /// </summary>
        /// <param name="costList"></param>
        /// <param name="versionId"></param>
        /// <param name="season">季度</param>
        /// <param name="cashOut"></param>
        /// <param name="cashIn"></param>
        /// <param name="cashPure"></param>
        /// <param name="cashAddup"></param>
        /// <param name="seasonKey">季度的key值</param>
        /// <param name="max"></param>
        /// <param name="min"></param>
        public void CalcSeasonCashFlowInfo(IEnumerable<BsonDocument> costList, string versionId, Season season, out string cashOut, out string cashIn, out string cashPure,out string cashAddup,out string seasonKey,out decimal max,out decimal min)
        {
            max = min = default(decimal);

            string SellDetail = "SellDetail";
            List<BsonDocument> sellDetails = _ctx.FindAllByQuery(SellDetail, Query.EQ("versionId", versionId)).ToList();//销售信息

            string RepayDetail = "RepayDetail";
            List<BsonDocument> repayDetails = _ctx.FindAllByQuery(RepayDetail, Query.EQ("versionId", versionId)).ToList();//融资信息信息

            StringBuilder sb = new StringBuilder();
            StringBuilder sbOut = new StringBuilder();
            StringBuilder sbIn = new StringBuilder();
            StringBuilder sbKey = new StringBuilder();
            StringBuilder sbAdd = new StringBuilder();
            sbKey.Append("[");
            sb.Append("[");
            sbOut.Append("[");
            sbIn.Append("[");
            sbAdd.Append("[");
            int index = 1;
            var preValue = 0m;
            foreach (var seasonMonth in season.SeasonMonth)
            {
                string key = seasonMonth.key;
                var cost = costList.LeafNode().Sum(s => s.Decimal(key));//成本支出
                var sell = sellDetails.Where(s => isInSeason(seasonMonth.months,s.Date("date"))).Sum(s => s.Decimal("salesVal"));
                var loan = repayDetails.Where(s => isInSeason(seasonMonth.months, s.Date("date"))).Sum(s => s.Decimal("loan"));//借款
                var repayVal = repayDetails.Where(s => isInSeason(seasonMonth.months, s.Date("date"))).Sum(s => s.Decimal("repayVal"));//归还借款
                var interest = repayDetails.Where(s => isInSeason(seasonMonth.months, s.Date("date"))).Sum(s => s.Decimal("interest"));//利息


                key = index.ToString();
                SeasonKey(sbKey, key, seasonMonth.year, seasonMonth.seasonId);
                //净现金流
                var tempTotal = sell + loan - repayVal - interest - cost;
                sb.Append("{");
                sb.Append("\"year\":\"" + key + "\",");
                sb.Append("\"value\":\"" + tempTotal.ToString() + "\"");
                sb.Append("},");

                //现金流出
                var totalOut = -(repayVal + interest + cost);
                sbOut.Append("{");
                sbOut.Append("\"year\":\"" + key + "\",");
                sbOut.Append("\"value\":\"" + totalOut.ToString() + "\"");
                sbOut.Append("},");

                //现金流入
                var totalIn = sell + loan;
                sbIn.Append("{");
                sbIn.Append("\"year\":\"" + key + "\",");
                sbIn.Append("\"value\":\"" + totalIn.ToString() + "\"");
                sbIn.Append("},");

                //累计现金流
                var addup = preValue + tempTotal;
                sbAdd.Append("{");
                sbAdd.Append("\"year\":\"" + key + "\",");
                sbAdd.Append("\"value\":\"" + addup.ToString() + "\"");
                sbAdd.Append("},");
                preValue = addup;
                index++;

                max = Max(tempTotal, totalOut, totalIn, addup, max);
                min = Min(tempTotal, totalOut, totalIn, addup, min);
            }
            
            cashPure = sb.ToString().TrimEnd(',') + "]";
            cashOut = sbOut.ToString().TrimEnd(',') + "]";
            cashIn = sbIn.ToString().TrimEnd(',') + "]";
            cashAddup = sbAdd.ToString().TrimEnd(',') + "]";
            seasonKey = sbKey.ToString().TrimEnd(',') + "]";

            max *= 1.2m;
            min *= 1.2m;
        }

        /// <summary>
        /// 计算季度现金流信息
        /// </summary>
        /// <param name="costList"></param>
        /// <param name="versionId"></param>
        /// <param name="season"></param>
        /// <param name="cashPure"></param>
        /// <param name="cashOut"></param>
        /// <param name="cashIn"></param>
        public void CalcSeasonCashFlowInfo(IEnumerable<BsonDocument> costList, string versionId, Season season, BsonDocument cashPure, BsonDocument cashOut,BsonDocument cashIn,BsonDocument cashAddup)
        {
            string SellDetail = "SellDetail";
            List<BsonDocument> sellDetails = _ctx.FindAllByQuery(SellDetail, Query.EQ("versionId", versionId)).ToList();//销售信息

            string RepayDetail = "RepayDetail";
            List<BsonDocument> repayDetails = _ctx.FindAllByQuery(RepayDetail, Query.EQ("versionId", versionId)).ToList();//融资信息信息

            string preKey = string.Empty;
            int index = 0;
            foreach (var seasonMonth in season.SeasonMonth)
            {
                string key = seasonMonth.key;
                var cost = costList.LeafNode().Sum(s => s.Decimal(key));//成本支出
                var sell = sellDetails.Where(s => isInSeason(seasonMonth.months, s.Date("date"))).Sum(s => s.Decimal("salesVal"));
                var loan = repayDetails.Where(s => isInSeason(seasonMonth.months, s.Date("date"))).Sum(s => s.Decimal("loan"));//借款
                var repayVal = repayDetails.Where(s => isInSeason(seasonMonth.months, s.Date("date"))).Sum(s => s.Decimal("repayVal"));//归还借款
                var interest = repayDetails.Where(s => isInSeason(seasonMonth.months, s.Date("date"))).Sum(s => s.Decimal("interest"));//利息
                //悲观
                var mincost = costList.LeafNode().Sum(s => s.Decimal(key+"_min"));//成本支出
                var minsell = sellDetails.Where(s => isInSeason(seasonMonth.months, s.Date("date"))).Sum(s => s.Decimal("minSalesVal"));
                var minloan = repayDetails.Where(s => isInSeason(seasonMonth.months, s.Date("date"))).Sum(s => s.Decimal("minLoan"));//借款
                var minrepayVal = repayDetails.Where(s => isInSeason(seasonMonth.months, s.Date("date"))).Sum(s => s.Decimal("minRepayVal"));//归还借款
                var mininterest = repayDetails.Where(s => isInSeason(seasonMonth.months, s.Date("date"))).Sum(s => s.Decimal("minInterest"));//利息
                //客观
                var comcost = costList.LeafNode().Sum(s => s.Decimal(key + "_com"));//成本支出
                var comsell = sellDetails.Where(s => isInSeason(seasonMonth.months, s.Date("date"))).Sum(s => s.Decimal("comSalesVal"));
                var comloan = repayDetails.Where(s => isInSeason(seasonMonth.months, s.Date("date"))).Sum(s => s.Decimal("comLoan"));//借款
                var comrepayVal = repayDetails.Where(s => isInSeason(seasonMonth.months, s.Date("date"))).Sum(s => s.Decimal("comRepayVal"));//归还借款
                var cominterest = repayDetails.Where(s => isInSeason(seasonMonth.months, s.Date("date"))).Sum(s => s.Decimal("comInterest"));//利息


                //净现金流
                var tempTotal = sell + loan - repayVal - interest - cost;
                cashPure.Add(key, tempTotal.ToMoney());
                var mintempTotal = minsell + minloan - minrepayVal - mininterest - mincost;
                cashPure.Add(key + "_min", mintempTotal.ToMoney());
                var comtempTotal = comsell + comloan - comrepayVal - cominterest - comcost;
                cashPure.Add(key + "_com", comtempTotal.ToMoney());
                //现金流出
                var totalOut = -(repayVal + interest + cost);
                cashOut.Add(key, totalOut.ToMoney());
                var mintotalOut = -(minrepayVal + mininterest + mincost);
                cashOut.Add(key + "_min", mintotalOut.ToMoney());
                var comtotalOut = -(comrepayVal + cominterest + comcost);
                cashOut.Add(key + "_com", comtotalOut.ToMoney());
                //现金流入
                var totalIn = sell + loan;
                cashIn.Add(key, totalIn.ToMoney());
                var mintotalIn = minsell + minloan;
                cashIn.Add(key+"_min", mintotalIn.ToMoney());
                var comtotalIn = comsell + comloan;
                cashIn.Add(key+"_com", comtotalIn.ToMoney());
               
                //累计现金流
                var addup = cashAddup.Decimal(preKey) + cashPure.Decimal(key);
                cashAddup.Add(key, addup.ToMoney());
                var minaddup = cashAddup.Decimal(preKey + "_min") + cashPure.Decimal(key + "_min");
                cashAddup.Add(key + "_min", minaddup.ToMoney());
                var comaddup = cashAddup.Decimal(preKey + "_com") + cashPure.Decimal(key + "_com");
                cashAddup.Add(key+"_com", comaddup.ToMoney());
                
                preKey = key;
            }
        }

        /// <summary>
        /// 求最大值
        /// </summary>
        /// <param name="nums"></param>
        /// <returns></returns>
        private decimal Max(params decimal[] nums)
        {
            return nums.Max();
        }

        /// <summary>
        /// 求最小值
        /// </summary>
        /// <param name="nums"></param>
        /// <returns></returns>
        private decimal Min(params decimal[] nums)
        {
            return nums.Min();
        }

        /// <summary>
        /// 获取季度的key值
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="index"></param>
        /// <param name="year"></param>
        /// <param name="seasonId"></param>
        private void SeasonKey(StringBuilder sb,string index,int year,int seasonId)
        {
            string seasonName = year.ToString() + "年" + seasonId.ToString() + "季度";
            sb.Append("[");
            sb.Append(index+",\"");
            sb.Append(seasonName);
            sb.Append("\"],");
        }

        /// <summary>
        /// 获取月份的key值
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="index"></param>
        /// <param name="month"></param>
        private void MonthKey(StringBuilder sb, string index, string month)
        {
            var dt = DateTime.Parse(month);
            string monthName = dt.Year.ToString() + "-" + dt.Month.ToString();
            sb.Append("[");
            sb.Append(index + ",\"");
            sb.Append(monthName);
            sb.Append("\"],");
        }

        /// <summary>
        /// 判断某个时间是否在季度中
        /// </summary>
        /// <param name="dts"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        private bool isInSeason(List<DateTime> dts, DateTime date)
        {
            foreach (var dt in dts)
            {
                if ((dt.Year == date.Year) && (dt.Month == date.Month)) return true;
            }
            return false;
        }

        /// <summary>
        /// 计算总利息
        /// </summary>
        /// <param name="versionId"></param>
        /// <returns></returns>
        public decimal CalcTotalInterest(string versionId)
        {
            return 0m;
        }

        /// <summary>
        /// 计算中的支出：
        /// </summary>
        /// <param name="versionId"></param>
        /// <returns></returns>
        public decimal CalcTotalExpenses(string versionId)
        {
            return 0m;
        }

        /// <summary>
        /// 计算中收入
        /// </summary>
        /// <param name="versonId"></param>
        /// <returns></returns>
        public decimal CalcTotalIncom(string versonId)
        {
            return 0m;
        }


        /// <summary>
        /// 季度现金流图形报表信息
        /// </summary>
        /// <param name="costList"></param>
        /// <param name="versionId"></param>
        /// <param name="season">季度</param>
        /// <param name="cashOut"></param>
        /// <param name="cashIn"></param>
        /// <param name="cashPure"></param>
        /// <param name="cashAddup"></param>
        /// <param name="seasonKey">季度的key值</param>
        /// <param name="max"></param>
        /// <param name="min"></param>
        ///  <param name="type">类型1：乐观 2 悲观 3 客观</param>
        public void CalcSeasonCashFlowInfo(IEnumerable<BsonDocument> costList, string versionId, Season season, out string cashOut, out string cashIn, out string cashPure, out string cashAddup, out string seasonKey, out decimal max, out decimal min,int type)
        {
            string loanstr = "loan";
            string repayValstr = "repayVal";
            string intereststr = "interest";
            string salesValstr = "salesVal";
            if (type == 2)
            {
                loanstr = "minLoan";
                repayValstr = "minRepayVal";
                intereststr = "minInterest";
                salesValstr = "minSalesVal";

            }
            else if (type == 3)
            {
                loanstr = "comLoan";
                repayValstr = "comRepayVal";
                intereststr = "comInterest";
                salesValstr = "comSalesVal";
            }
            max = min = default(decimal);

            string SellDetail = "SellDetail";
            List<BsonDocument> sellDetails = _ctx.FindAllByQuery(SellDetail, Query.EQ("versionId", versionId)).ToList();//销售信息

            string RepayDetail = "RepayDetail";
            List<BsonDocument> repayDetails = _ctx.FindAllByQuery(RepayDetail, Query.EQ("versionId", versionId)).ToList();//融资信息信息

            StringBuilder sb = new StringBuilder();
            StringBuilder sbOut = new StringBuilder();
            StringBuilder sbIn = new StringBuilder();
            StringBuilder sbKey = new StringBuilder();
            StringBuilder sbAdd = new StringBuilder();
            sbKey.Append("[");
            sb.Append("[");
            sbOut.Append("[");
            sbIn.Append("[");
            sbAdd.Append("[");
            int index = 1;
            var preValue = 0m;
            foreach (var seasonMonth in season.SeasonMonth)
            {
                string key = seasonMonth.key;
               
                var cost = costList.LeafNode().Sum(s => s.Decimal(key));//成本支出
                if (type == 2)
                {
                    cost = costList.LeafNode().Sum(s => s.Decimal(key+"_min"));//成本支出
                }
                else if (type == 3)
                {
                    cost = costList.LeafNode().Sum(s => s.Decimal(key + "_com"));//成本支出 ;
                }
                var sell = sellDetails.Where(s => isInSeason(seasonMonth.months, s.Date("date"))).Sum(s => s.Decimal(salesValstr));
                var loan = repayDetails.Where(s => isInSeason(seasonMonth.months, s.Date("date"))).Sum(s => s.Decimal(loanstr));//借款
                var repayVal = repayDetails.Where(s => isInSeason(seasonMonth.months, s.Date("date"))).Sum(s => s.Decimal(repayValstr));//归还借款
                var interest = repayDetails.Where(s => isInSeason(seasonMonth.months, s.Date("date"))).Sum(s => s.Decimal(intereststr));//利息


                key = index.ToString();
                SeasonKey(sbKey, key, seasonMonth.year, seasonMonth.seasonId);
                //净现金流
                var tempTotal = sell + loan - repayVal - interest - cost;
                sb.Append("{");
                sb.Append("\"year\":\"" + key + "\",");
                sb.Append("\"value\":\"" + tempTotal.ToString() + "\"");
                sb.Append("},");

                //现金流出
                var totalOut = -(repayVal + interest + cost);
                sbOut.Append("{");
                sbOut.Append("\"year\":\"" + key + "\",");
                sbOut.Append("\"value\":\"" + totalOut.ToString() + "\"");
                sbOut.Append("},");

                //现金流入
                var totalIn = sell + loan;
                sbIn.Append("{");
                sbIn.Append("\"year\":\"" + key + "\",");
                sbIn.Append("\"value\":\"" + totalIn.ToString() + "\"");
                sbIn.Append("},");

                //累计现金流
                var addup = preValue + tempTotal;
                sbAdd.Append("{");
                sbAdd.Append("\"year\":\"" + key + "\",");
                sbAdd.Append("\"value\":\"" + addup.ToString() + "\"");
                sbAdd.Append("},");
                preValue = addup;
                index++;

                max = Max(tempTotal, totalOut, totalIn, addup, max);
                min = Min(tempTotal, totalOut, totalIn, addup, min);
            }

            cashPure = sb.ToString().TrimEnd(',') + "]";
            cashOut = sbOut.ToString().TrimEnd(',') + "]";
            cashIn = sbIn.ToString().TrimEnd(',') + "]";
            cashAddup = sbAdd.ToString().TrimEnd(',') + "]";
            seasonKey = sbKey.ToString().TrimEnd(',') + "]";

            max *= 1.2m;
            min *= 1.2m;
        }

        /// <summary>
        /// 月份现金流图表信息
        /// </summary>
        /// <param name="costList"></param>
        /// <param name="yearNodes"></param>
        /// <param name="costRates"></param>
        /// <param name="versionId"></param>
        /// <param name="months"></param>
        /// <param name="type">类型1：乐观 2 悲观 3 客观</param>
        public void CalcCashFlowInfo(IEnumerable<BsonDocument> costList, string versionId, List<string> months, out string cashOut, out string cashIn, out string cashPure, out string cashAddup, out string ticks, out decimal max, out decimal min,int type)
        {
            string loanstr = "loan";
            string repayValstr = "repayVal";
            string intereststr = "interest";
            string salesValstr = "salesVal";
            if (type == 2)
            {
                loanstr = "minLoan";
                repayValstr = "minRepayVal";
                intereststr = "minInterest";
                salesValstr = "minSalesVal";
            }
            else if (type == 3)
            {
                loanstr = "comLoan";
                repayValstr = "comRepayVal";
                intereststr = "comInterest";
                salesValstr = "comSalesVal";
            }

            min = max = default(decimal);
            string SellDetail = "SellDetail";
            List<BsonDocument> sellDetails = _ctx.FindAllByQuery(SellDetail, Query.EQ("versionId", versionId)).ToList();//销售信息

            string RepayDetail = "RepayDetail";
            List<BsonDocument> repayDetails = _ctx.FindAllByQuery(RepayDetail, Query.EQ("versionId", versionId)).ToList();//融资信息信息

            StringBuilder sb = new StringBuilder();
            StringBuilder sbOut = new StringBuilder();
            StringBuilder sbIn = new StringBuilder();
            StringBuilder sbKey = new StringBuilder();
            StringBuilder sbAdd = new StringBuilder();
            sb.Append("[");
            sbOut.Append("[");
            sbIn.Append("[");
            sbKey.Append("[");
            sbAdd.Append("[");
            int index = 1;

            var preValue = 0m;
            foreach (var month in months)
            {
                var cost = costList.LeafNode().Sum(s => s.Decimal(month));//成本支出
                var sell = sellDetails.Where(s => DateTimeHelper.CompareYM(s.String("date"), month)).Sum(s => s.Decimal(salesValstr));
                var loan = repayDetails.Where(s => DateTimeHelper.CompareYM(s.String("date"), month)).Sum(s => s.Decimal(loanstr));//借款
                var repayVal = repayDetails.Where(s => DateTimeHelper.CompareYM(s.String("date"), month)).Sum(s => s.Decimal(repayValstr));//归还借款
                var interest = repayDetails.Where(s => DateTimeHelper.CompareYM(s.String("date"), month)).Sum(s => s.Decimal(intereststr));//利息

                string key = index.ToString();
                MonthKey(sbKey, key, month);

                //净现金流
                var tempTotal = sell + loan - repayVal - interest - cost;
                sb.Append("{");
                sb.Append("\"year\":\"" + key + "\",");
                sb.Append("\"value\":\"" + tempTotal.ToString() + "\"");
                sb.Append("},");

                //现金流出
                var totalOut = -(repayVal + interest + cost);
                sbOut.Append("{");
                sbOut.Append("\"year\":\"" + key + "\",");
                sbOut.Append("\"value\":\"" + totalOut.ToString() + "\"");
                sbOut.Append("},");

                //现金流入
                var totalIn = sell + loan;
                sbIn.Append("{");
                sbIn.Append("\"year\":\"" + key + "\",");
                sbIn.Append("\"value\":\"" + totalIn.ToString() + "\"");
                sbIn.Append("},");

                //累计现金流
                var addup = preValue + tempTotal;
                sbAdd.Append("{");
                sbAdd.Append("\"year\":\"" + key + "\",");
                sbAdd.Append("\"value\":\"" + addup.ToString() + "\"");
                sbAdd.Append("},");

                preValue = addup;
                index++;

                max = Max(tempTotal, totalOut, totalIn, addup, max);
                min = Min(tempTotal, totalOut, totalIn, addup, min);
            }

            cashPure = sb.ToString().TrimEnd(',') + "]";
            cashOut = sbOut.ToString().TrimEnd(',') + "]";
            cashIn = sbIn.ToString().TrimEnd(',') + "]";
            cashAddup = sbAdd.ToString().TrimEnd(',') + "]";
            ticks = sbKey.ToString().TrimEnd(',') + "]";

            max *= 1.2m;
            min *= 1.2m;
        }



    }
}
