using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI.DataVisualization.Charting;
using System.Drawing;

namespace Yinhe.ProcessingCenter.Reports
{
    /// <summary>
    /// 报表serie处理类
    /// </summary>
    public static class SerieHelper
    {
        public static List<Color> colors= new List<Color>{ 
                                        Color.FromArgb(65,150,174),
                                        Color.FromArgb(83,184,210),
                                        Color.FromArgb(180,218,234),
                                        Color.FromArgb(67,174,148),
                                        Color.FromArgb(91,207,179),
                                        Color.FromArgb(129,240,213),
                                        Color.FromArgb(58,81,171),
                                        Color.FromArgb(87,115,216),
                                        Color.FromArgb(145,166,241)
                                       };

        public static void FilterPieLabelLine(this Chart chart, double minValue)
        {
            foreach (var serie in chart.Series)
            {
                foreach (var point in serie.Points)
                {
                    var yValue = point.YValues.FirstOrDefault();
                    if (yValue < minValue)
                    {
                        point["PieLineColor"] = "Black";
                    }
                }
            }
        }
        
        public static void CollectPieSlices(this Chart chart,double liminalValue)
        {
            if (chart.Series.Count > 0)
            {
                foreach (var serie in chart.Series)
                {
                    var collectedPoints = serie.Points.Where(c => c.YValues.FirstOrDefault() < liminalValue).ToList();
                    double collectedSum = collectedPoints.Sum(c => c.YValues.FirstOrDefault());
                    //List<DataPoint> newDataPoints = new List<DataPoint>();
                    foreach (var collectedPoint in collectedPoints)
                    {
                        serie.Points.Remove(collectedPoint);
                    }
                    if (collectedSum >=1)
                    {
                        serie.Points.AddXY("其他", collectedSum);
                    }
                }
                chart.FilterPieChartColor();
            }
        }

        public static void FilterPieChartColor(this Chart chart)
        {
            if (chart.Series.Count > 0)
            {
                var slices = chart.Series[0].Points;
                for (int i = 0; i < slices.Count; i++)
                {
                    if (i < colors.Count())
                    {
                        slices[i].Color = colors[i];
                    }
                }
            }
        }

        public static void CopySerieStyleFrom(this Series newSerie, Series oSerie)
        {
            newSerie.CustomProperties = oSerie.CustomProperties;
            newSerie.Legend = oSerie.Legend;
            newSerie.ChartArea = oSerie.ChartArea;
            newSerie.LabelFormat = oSerie.LabelFormat;
            newSerie.IsValueShownAsLabel = oSerie.IsValueShownAsLabel;
            newSerie.BorderWidth = oSerie.BorderWidth;
        }

        public static void FilterSeriesWidth(this Chart chart)
        {
            int serieCount = chart.Series.Count;
            if (serieCount>1)
            {
                //int maxColumnCount = chart.Series.Max(c => c.Points.Count);
                //int chartWidth = (int)Math.Ceiling(chart.Width.Value);
                int curColumnWidth = 0;
                if (!int.TryParse(chart.Series[0]["PixelPointWidth"], out curColumnWidth))
                {
                    curColumnWidth = 20;
                }
                //int pixelPointWidth = chart.Series.Count > 1 ? curColumnWidth * chart.Series.Count * 2 / 3 : curColumnWidth;
                int pixelPointWidth =serieCount * curColumnWidth * 2 / 3;
                //int interval = 5;//假设每个柱形的间隙至少为10px;
                //if ((pixelPointWidth + interval) * maxColumnCount >= chartWidth * 0.7)
                //{
                //    pixelPointWidth = (int)Math.Floor(chartWidth * 0.7 / maxColumnCount - interval);
                //}
                foreach (var serie in chart.Series)
                {
                    serie["PixelPointWidth"] = pixelPointWidth.ToString();
                }    
            }
            var maxValue = (from s in chart.Series
                            from p in s.Points
                            select p).Max(c => c.YValues.FirstOrDefault());
            if (maxValue < 1000)
            {
                var chartArea = chart.ChartAreas.FirstOrDefault();
                if (chartArea != null)
                    chartArea.AxisY.LabelStyle.Format = "";
            }
                         

            //判断所有label文字的长度；如果label过长，则调整label的角度；
            //chart.ChartAreas[0].AxisX.LabelStyle.Angle = 30;如何判断是个问题；     
        }
    }
}
