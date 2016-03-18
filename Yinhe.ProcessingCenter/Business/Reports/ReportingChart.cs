using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Web.UI;
using System.Web.UI.DataVisualization.Charting;
using System.Drawing;
using Yinhoo.Utilities.Core.Extensions;
using Yinhe.ProcessingCenter.DataRule;
using MongoDB.Bson;
using Yinhe.ProcessingCenter;
using MongoDB.Driver.Builders;

namespace Yinhe.ProcessingCenter.Reports
{
    /// <summary>
    /// 报表实例处理类
    /// </summary>
    public class ReportingChart : ReportingDisplay, IReportingDisplay
    {
        public const int Width = 412;
        public const int Height = 296;

        #region 构造函数
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="tableName">表名数据源</param>
        /// <param name="data"></param>
        /// <param name="prm"></param>
        public ReportingChart(List<ReportingData> data, Dictionary<string, string> prm)
        {
            this.Init(data, prm);
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="tableName">表名数据源</param>
        /// <param name="data"></param>
        /// <param name="prm"></param>
        public ReportingChart(List<ReportingSerie> series, Dictionary<string, string> prm)
        {
            this.Init(series, prm);
        }
        #endregion

        #region 常量
        /// <summary>
        /// 默认的ChartArea
        /// </summary>
        public const string DefaultChartAreaName = "DefaultArea";
        /// <summary>
        /// 默认的Serie
        /// </summary>
        public const string DefaultSerieName = "DefaultSeries";
        /// <summary>
        /// 默认Legend
        /// </summary>
        public const string DefaultLegendName = "DefaultLegend";
        #endregion
        /// <summary>
        /// 检测报表是否有效
        /// </summary>
        public virtual bool ValidataReporting { 
            get {
                return this.CheckValidateSql();
            } 
        }
        /// <summary>
        /// 初始化默认样式
        /// </summary>
        /// <param name="chart"></param>
        private void LoadDefaultStyle()
        {
            chart.BackGradientStyle = GradientStyle.TopBottom;
            chart.BorderlineDashStyle = ChartDashStyle.Solid;
            chart.BorderlineWidth = 1;
            chart.BorderlineColor = Color.Transparent;
            chart.Palette = ChartColorPalette.BrightPastel;
            // chart.BackColor = System.Drawing.Color.FromArgb(243, 223, 193);
            chart.BackColor = System.Drawing.Color.FromArgb(64, 165, 191, 228);
            chart.BackSecondaryColor = Color.White;
            chart.BorderSkin.SkinStyle = BorderSkinStyle.None;
        
        }

        public Chart chart;

        /// <summary>
        /// 初始化图表控件的部分参数
        /// </summary>
        protected virtual void InitChart() {
            chart = new Chart();
            #region url样式参数设定
            int fontSize = 14;
            string chartAreaColorType =string.Empty;
            string template = string.Empty;
            int Width = 0;
            int Height = 0;
            var title = string.Empty;
            string reportTypeValueType = "Pie";
            if (this.dicPrm.ContainsKey("template") == true)
            {
                template =  this.dicPrm["template"].ToString().Trim() ;
            }
           
            if (this.dicPrm.ContainsKey("chartAreaColorType") == true)
            {
                chartAreaColorType =this.dicPrm["chartAreaColorType"].ToString().Trim() ;
            }
            if (this.dicPrm.ContainsKey("title") == true)
             {
                 title = this.dicPrm["title"].ToString().Trim();
             }
            if (this.dicPrm.ContainsKey("reportTypeValueType") == true)
            {
                reportTypeValueType = this.dicPrm["reportTypeValueType"].ToString().Trim();
            }

             //高度宽度设置
             if (this.dicPrm.ContainsKey("width") == true)
             {
                  Width = int.Parse(this.dicPrm["width"].ToString());
                 fontSize = int.Parse(this.dicPrm["width"].ToString()) * 14 / 412;
             }
             else
             {
                  Width = 412;
             }
             if (this.dicPrm.ContainsKey("height") == true)
             {
                 Height = int.Parse(this.dicPrm["height"].ToString());
                 int size = int.Parse(this.dicPrm["height"].ToString()) * 14 / 296;
                 fontSize = size > fontSize ? fontSize : size;
             }
             else
             {
                  Height = 296;
             }

            #endregion
            chart.RenderType = RenderType.ImageTag;
            chart.ImageLocation = "..\\..\\TempImages\\ChartPic_#SEQ(200,30)";
            //BorderDashStyle="Solid" BackGradientStyle="TopBottom" BorderWidth="2" BorderColor="181, 64, 1"
            //Palette="BrightPastel" BackColor="#F3DFC1"

            var WebPath = System.AppDomain.CurrentDomain.BaseDirectory+"Content\\ChartTemplates\\";//获取模版路径
            var TempCharXML = WebPath + template+".xml";
            if (!string.IsNullOrEmpty(template)&&System.IO.File.Exists(TempCharXML))//从模版载入样式
            {
                var Stream = System.IO.File.Open(TempCharXML, System.IO.FileMode.Open);
                try
                {
                    chart.Serializer.Content = SerializationContents.Appearance;
                    chart.Serializer.SerializableContent = "*.*";
                    //chart.Serializer.IsUnknownAttributeIgnored = true; 
                    chart.Serializer.Load(Stream);
                    //初始化配置文件中的颜色
                    var colors = (from s in chart.Series
                                  from p in s.Points
                                  select p.Color).ToList();
                    if (colors.Count > 0)
                    {
                        SerieHelper.colors = colors;
                        foreach (var serie in chart.Series)
                        {
                            serie.Points.Clear();
                        }
                    }
                    Stream.Close();
                    //chart.LoadTemplate(TempCharXML);
                }
                catch (Exception ex)
                {
                    if (Stream != null)
                    {
                        Stream.Close();
                    }
                    LoadDefaultStyle();
                }

                #region 初始化series
                foreach (var series in this.reportingSeries)
                {
                    var serieName = string.IsNullOrEmpty(series.name) ? ReportingChart.DefaultSerieName : series.name;
                    var curChartObj = chart.Series.Where(c => c.Name.Trim() == serieName).FirstOrDefault();      
                    if (curChartObj == null)
                    {
                        curChartObj = chart.Series.Add(serieName);
                    }
                    curChartObj.Name = serieName;
                    string[] keyStyles = { "PieDrawingStyle", "AreaDrawingStyle" };
                    foreach (var keyStyle in keyStyles)
                    {
                        if (this.dicPrm.ContainsKey(keyStyle))
                        {
                            curChartObj[keyStyle] = this.dicPrm[keyStyle];
                        }
                    }
                    if (!string.IsNullOrEmpty(series.chartType))
                    {
                        curChartObj.ChartType = (SeriesChartType)Enum.Parse(typeof(SeriesChartType), series.chartType, true);
                    }
                    curChartObj.CopySerieStyleFrom(chart.Series[0]);
                }
                #endregion
                if (chart.ChartAreas.Count != 0)
                {
                    chart.ChartAreas[0].Name = ReportingChart.DefaultChartAreaName;
                }
                if (chart.Legends.Count != 0)
                {
                    chart.Legends[0].Name=ReportingChart.DefaultLegendName;
                 
                }  
            }
            else
            {
                #region Chart样式

                LoadDefaultStyle();
                #endregion
             }
             #region 设置默认值

            if (chart.Series.Count == 0)
            {
                chart.Series.Add(ReportingChart.DefaultSerieName);
             
            }
            if (chart.ChartAreas.Count == 0)
            {
                chart.ChartAreas.Add(ReportingChart.DefaultChartAreaName);
            }
            if (chart.Legends.Count == 0)
            {
                chart.Legends.Add(ReportingChart.DefaultLegendName);
            }
            if (chart.Titles.Count == 0)
            {

                Title titleObj = new Title(title, Docking.Top, new System.Drawing.Font("Trebuchet MS", fontSize, System.Drawing.FontStyle.Bold), System.Drawing.Color.FromArgb(26, 59, 105));
                titleObj.Alignment = ContentAlignment.TopLeft;
                chart.Titles.Add(titleObj);
             }
            #endregion

            #region 设定图表类型
            SeriesChartType chartType;
            try
            {
                if (!string.IsNullOrEmpty(reportTypeValueType))
                {
                    chartType = (SeriesChartType)Enum.Parse(typeof(SeriesChartType), reportTypeValueType, true);
                    foreach (var serie in chart.Series)
                    {
                        serie.ChartType = chartType;//全部改成统一类型后期可能更改
                    }
                }
            }
            catch (InvalidCastException ex)
            {

            }
            catch (Exception ex)
            {
            }
            #endregion
          
            #region chartArea 样式设定
            if (!string.IsNullOrEmpty(chartAreaColorType))
            {
                Color BackColor;
                Color BackSecondaryColor;
                switch (chartAreaColorType.Trim())
                {
                    case "0":
                        {
                            BackColor = Color.FromKnownColor(KnownColor.White);
                            BackSecondaryColor = Color.FromKnownColor(KnownColor.White);
                            break;
                        }
                    case "1": 
                        {
                            BackColor = Color.FromKnownColor(KnownColor.WhiteSmoke);
                            BackSecondaryColor = Color.FromKnownColor(KnownColor.White);
                        break;
                        }
                    case "2":
                        {
                           
                            BackColor = Color.FromArgb(64, 165, 191, 228);
                            BackSecondaryColor = Color.FromKnownColor(KnownColor.White);
                            break;
                       
                        }
                    default:
                        {
                            BackColor= Color.FromKnownColor(KnownColor.OldLace);
                            BackSecondaryColor = Color.FromKnownColor(KnownColor.White);
                            break;
                        }
                 
                }
                #region chartarea 默认样式 
                chart.ChartAreas[ReportingChart.DefaultChartAreaName].BackSecondaryColor =BackColor;
                chart.ChartAreas[ReportingChart.DefaultChartAreaName].BackColor = BackSecondaryColor;
                #endregion
            }
            else
            {
                if (string.IsNullOrEmpty(template)|| !System.IO.File.Exists(TempCharXML))//当模版载入出错
                {
                    chart.ChartAreas[ReportingChart.DefaultChartAreaName].BackSecondaryColor = Color.FromKnownColor(KnownColor.White);
                    chart.ChartAreas[ReportingChart.DefaultChartAreaName].BackColor = Color.FromKnownColor(KnownColor.OldLace);
                    this.chart.Series[ReportingChart.DefaultSerieName].IsValueShownAsLabel = true;
                 
                    chart.ChartAreas[ReportingChart.DefaultChartAreaName].ShadowColor = Color.FromKnownColor(KnownColor.Transparent);
                    chart.ChartAreas[ReportingChart.DefaultChartAreaName].BackGradientStyle = GradientStyle.TopBottom;
                    chart.Legends[ReportingChart.DefaultLegendName].Enabled = true;
                    chart.Legends[ReportingChart.DefaultLegendName].BackColor = System.Drawing.Color.FromKnownColor(KnownColor.Transparent);
                }
                
            }
            #endregion


            if (chart.Titles.Count != 0)
            {
                chart.Titles[0].Text =  title;
                chart.Titles[0].Name  = title;
            }

            chart.Width = Width;
            chart.Height = Height;
           
          
         }


        /// <summary>
        /// 图表数据绑定
        /// </summary>
        protected virtual void ChartDataBind() {
            List<ReportingData> tblReport = this.reportData;
            
            int rowCount = tblReport.Count;
            #region 判断最大值
            double maxValue = 0;
            //bool checkConvertNumeric = this.CheckConvertNumeric(tblReport, 0, out maxValue);
            bool checkConvertNumeric = false;
            string strUnit = string.Empty;
            int maxLength = NumericExtension.GetValueLength(maxValue);
            if (checkConvertNumeric == true)
            {
                strUnit = NumericExtension.ConvertUnit(maxValue);
                this.chart.ChartAreas[ReportingChart.DefaultChartAreaName].AxisY.Title = "数量级单位：" + strUnit;
                this.chart.ChartAreas[ReportingChart.DefaultChartAreaName].AxisY.TitleAlignment = System.Drawing.StringAlignment.Center;
                this.chart.ChartAreas[ReportingChart.DefaultChartAreaName].AxisY.TextOrientation = TextOrientation.Stacked;
            }
            #endregion
            var RowCount = rowCount;
           
           // var MarkCount = this.reportData.MarkFieldList.Count();
            var deleteList = new List<Series>();
            double axisYValueDivisor = 0;//除数
            if (dicPrm.ContainsKey("axisYValueDivisor"))
                double.TryParse(dicPrm["axisYValueDivisor"], out axisYValueDivisor);
            foreach (var curSeries in chart.Series)
            {
                var seriesObj = this.reportingSeries.Where(c => c.name.Trim() == curSeries.Name.Trim()).FirstOrDefault();
                if (seriesObj != null)
                {
                    tblReport = seriesObj.reportingData;
                    //curSeries.ToolTip = "所属名称:  #VALX\n 值: #VALY{C}" + strUnit;
                    if (dicPrm.ContainsKey("toolTip"))
                    {
                        curSeries.ToolTip = dicPrm["toolTip"];
                    }
                    else {
                        curSeries.ToolTip = curSeries.Name + "&" + "#VALX&#VALY";
                    }
                    foreach (var obj in tblReport)
                    {
                        double yVaule = 0;
                        yVaule = obj.statistics;
                        if (axisYValueDivisor != 0)
                            yVaule = yVaule / axisYValueDivisor;
                        //yVaule = NumericExtension.ConvertNumeric(yVaule, maxLength);
                        curSeries.Points.AddXY(obj.groupByName, yVaule);
                    }
                }
                else
                {
                   deleteList.Add(curSeries);
                }
            }
            foreach (var delObj in deleteList)
            {
                chart.Series.Remove(delObj);
            }
                    
            //是否分离饼图的分块
            if (this.dicPrm.ContainsKey("isExplodedAllPoint"))
            {
                bool isExplodedAllPoint = false;
                bool.TryParse(dicPrm["isExplodedAllPoint"], out isExplodedAllPoint);
                foreach (var serie in chart.Series)
                {
                    int explodeDistance=5;//默认分离5个像素
                    if (dicPrm.ContainsKey("explodedDistance"))
                        int.TryParse(dicPrm["explodedDistance"],out explodeDistance);
                    if (serie.Points.Count > 1)
                    {
                        serie.BorderColor = chart.ChartAreas.FirstOrDefault().BackColor;
                        serie.BorderDashStyle = ChartDashStyle.Solid;
                        serie.BorderWidth = explodeDistance;
                    }
                }
                //当Point的数目大于4时，将label放到图外面；
            }

            //更改横坐标的角度 
            if (this.dicPrm.ContainsKey("axisXLabelAngle"))
            {
                int axisXLabelAngle = 0;
                int.TryParse(dicPrm["axisXLabelAngle"], out axisXLabelAngle);
                foreach (var chartArea in chart.ChartAreas)
                {
                    chartArea.AxisX.IsLabelAutoFit=false;
                    chartArea.AxisX.LabelStyle.Angle = axisXLabelAngle;
                }
            }

            //添加单位
            if (dicPrm.ContainsKey("axisYUnit"))
            {
                string axisXUnit = dicPrm["axisYUnit"];
                var chartArea = chart.ChartAreas.FirstOrDefault();
                if (chartArea != null)
                {
                    chartArea.AxisY.Title = axisXUnit;
                    chartArea.AxisY.TextOrientation = TextOrientation.Rotated270;
                }
            }
            //设置Y轴间隔
            if (dicPrm.ContainsKey("axisYInterval"))
            {
                double axisYInterval = 1;
                var chartArea = chart.ChartAreas.FirstOrDefault();
                if (chartArea != null && double.TryParse(dicPrm["axisYInterval"], out axisYInterval))
                {
                    chartArea.AxisY.Interval = axisYInterval;
                }
            }

            //去除无用的legend
            if (chart.Series.Count == 1//只有一个Series
                && (chart.Series[0].ChartType != SeriesChartType.Pie&&chart.Series[0].ChartType != SeriesChartType.Doughnut)//不为饼图
                && chart.Series[0].Name == ReportingChart.DefaultSerieName && chart.Legends.Count > 0)//名字为DefaultSeries，不显示legend;
                chart.Series[0].IsVisibleInLegend = false;
            
            //设置饼图或甜甜圈的颜色
            if (chart.Series.Count == 1 && (chart.Series[0].ChartType == SeriesChartType.Pie || chart.Series[0].ChartType == SeriesChartType.Doughnut))//设置饼图的颜色
            {
                double invalidValue = 0;
                if (dicPrm.ContainsKey("collectedPercentage"))
                {
                    double.TryParse(dicPrm["collectedPercentage"], out invalidValue);
                }
                //去除无效数据
                foreach (var serie in chart.Series)
                {
                    List<DataPoint> voidPoints = serie.Points.Where(c => c.YValues.FirstOrDefault() <=invalidValue).ToList();
                    foreach (var voidPoint in voidPoints)
                    {
                        serie.Points.Remove(voidPoint);
                    }
                }
                chart.FilterPieChartColor();//更改饼图中的颜色
            }

            //设置曲线图中值为0的显示
            foreach (var serie in chart.Series)
            {
                if (serie.ChartType == SeriesChartType.Line || serie.ChartType == SeriesChartType.Spline || serie.ChartType == SeriesChartType.StepLine)
                {
                    var emptyPoints = serie.Points.Where(c => c.YValues.FirstOrDefault() == 0).ToList();
                    foreach (var emptypoint in emptyPoints)
                    {
                        emptypoint.IsEmpty = true;
                    }
                }
            }

            //不显示值为0的点的label
            var points = from s in chart.Series
                         from p in s.Points
                         where p.YValues[0]<=0
                         select p;
            foreach (var point in points)
            {
                point.IsEmpty = true;
            }

            //调整柱状图的每个柱体的宽度
            if(chart.Series.Count>0&&chart.Series.All(c=>c.ChartType==SeriesChartType.Column))
                chart.FilterSeriesWidth();
            
            //合并饼图中较小的值
            if (chart.Series.All(c => c.ChartType == SeriesChartType.Pie || c.ChartType == SeriesChartType.Doughnut) && dicPrm.ContainsKey("CollectedThreshold"))
            {
                double collectedThreshold = 1.0;
                double.TryParse(dicPrm["CollectedThreshold"], out collectedThreshold);
                chart.CollectPieSlices(collectedThreshold);
            }

            if (dicPrm.ContainsKey("PieLabelLineLiminalValue"))
            {
                double liminalValue = 100.0;
                double.TryParse(dicPrm["PieLabelLineLiminalValue"], out liminalValue);
                chart.FilterPieLabelLine(liminalValue);
            }
        }

        /// <summary>
        /// 判断是否要转数值
        /// </summary>
        /// <param name="tblReport"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        protected virtual bool CheckConvertNumeric(DataTable tblReport,int index,out double maxValue)
        {
            bool bResult = false;
            maxValue =0;
            var rows = from r in tblReport.Select("1=1")
                       select r;
            if (rows.Count() > 0)
            {
                if (rows.Where(r => r[index] != null).Count() > 0)
                {
                    maxValue = rows.Where(r => r[index] != System.DBNull.Value ).Max(r => Convert.ToDouble(r[index]));
                    if (NumericExtension.ConvertTimes(maxValue) > 1)
                    {
                        bResult = true;
                    }
                }
            }
            return bResult;

                     
        }

       
        protected List<string> GroupFieldList;
        protected int maxFieldCount = 2;

    
        #region 私有函数

        /// <summary>
        /// 检测是否有效的报表
        /// </summary>
        /// <returns></returns>
        protected bool CheckValidata()
        {
            #region 检测数据

            if (this.reportData == null)
            {
                this.Message = "没有有效的数据源";
                return false;
            }

            #endregion

            return true;
        }

        #endregion

        #region IReportingDisplay Members

        /// <summary>
        /// 输出报表
        /// </summary>
        /// <param name="page"></param>
        public virtual void RenderHtml(System.Web.UI.Page page)
        {
            this.ChartDataBind();
            this.chart.Page = page;
            HtmlTextWriter writer = new HtmlTextWriter(page.Response.Output);
            chart.RenderControl(writer);
        }
        #endregion
    }

   
}
