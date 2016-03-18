using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using org.in2bits.MyXls;

namespace Yinhe.ProcessingCenter.Common
{
    /// <summary>
    /// excel处理类
    /// </summary>
    public class ExcelWriter
    {
        /// <summary>
        /// 合并单元格的信息，目前只支持行合并或者列合并，尚不支持同时合并行和列
        /// </summary>
        private class MergeInfo
        {
            public string MergeType { get; set; }
            public int MergeSpan { get; set; }
        }
        XlsDocument xls = new XlsDocument();
        Worksheet worksheet;
        int rowIndex = 1;
        int colIndex = 1;
        List<MergeArea> mergeAreaList = new List<MergeArea>();  //表格合并单元格的信息列表
        XF cellStyle;
        int columnCount; //表格最大列索引（从1开始）

        #region +ExcelWriter(string sheetName) 构造函数
        /// <summary>
        ///  构造函数
        /// </summary>
        /// <param name="sheetName"></param>
        public ExcelWriter(string sheetName)
        {
            if (string.IsNullOrEmpty(sheetName))
                sheetName = "sheet";
            worksheet = xls.Workbook.Worksheets.Add(sheetName);
            cellStyle = GetCellStyle(false, HorizontalAlignments.Centered);
        }
        #endregion

        #region +SaveAsFile(string filename) 保存到文件
        /// <summary>
        /// 保存到文件中
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public string SaveAsFile(string filePath, string fileName)
        {
            if (!Directory.Exists(filePath))
                Directory.CreateDirectory(filePath);
            if (string.IsNullOrEmpty(fileName))
            {
                Random random = new Random();
                fileName = random.Next(9999999).ToString() + ".xls";
            }
            if (!fileName.Contains(".xls"))
                fileName = fileName + ".xls";
            string fullFileName = Path.Combine(filePath, fileName);
            xls.FileName = Path.GetFileNameWithoutExtension(fileName);
            xls.Save(filePath, true);
            return fullFileName;
        }
        #endregion

        #region -bool WithInMergeArea(int rowIndex, int colIndex) 判断某一个单元格是否在合并的单元格内
        /// <summary>
        /// 判断某一个单元格是否在合并的单元格内
        /// </summary>
        /// <param name="rowIndex"></param>
        /// <param name="colIndex"></param>
        /// <returns></returns>
        private bool WithinMergeArea(int rowIndex, int colIndex)
        {
            var query = from p in mergeAreaList where rowIndex >= p.RowMin && rowIndex <= p.RowMax && colIndex >= p.ColMin && colIndex <= p.ColMax select p;
            return query.Count() > 0;
        }
        #endregion

        #region -void AddCellData(string data, MergeInfo mergeInfo) 添加单元数据
        /// <summary>
        /// 添加单元格数据
        /// </summary>
        /// <param name="data">数据值</param>
        /// <param name="mergeInfo">单元格合并信息</param>
        private void AddCellData(string data)
        {
            if (WithinMergeArea(rowIndex, colIndex))
            {
                colIndex++;
                AddCellData(data);
            }
            else
            {
                MergeInfo mergeInfo = GetMergeInfo(data);
                SetCellStyleByCellData(data);
                data = Regex.Replace(data, @"\(\?!.+?\)", "");
                double dataDou;
                if (double.TryParse(data, out dataDou))
                {
                    Cell cell = worksheet.Cells.Add(rowIndex, colIndex, dataDou, cellStyle);
                }
                else
                    worksheet.Cells.Add(rowIndex, colIndex, data, cellStyle);
                if (mergeInfo != null)
                {
                    MergeArea mergeArea;
                    if (string.Equals(mergeInfo.MergeType, "colspan", StringComparison.InvariantCultureIgnoreCase))
                    {
                        mergeArea = new MergeArea() { RowMin = (ushort)rowIndex, RowMax = (ushort)rowIndex, ColMin = (ushort)colIndex, ColMax = (ushort)(colIndex + mergeInfo.MergeSpan - 1) };
                    }
                    else
                    {
                        mergeArea = new MergeArea() { RowMin = (ushort)rowIndex, RowMax = (ushort)(rowIndex + mergeInfo.MergeSpan - 1), ColMin = (ushort)colIndex, ColMax = (ushort)colIndex };
                    }
                    worksheet.AddMergeArea(mergeArea);
                    mergeAreaList.Add(mergeArea);
                }
            }
        }
        #endregion

        #region +WriteExcelFile(string htmlCode) 将html写入excel文件中
        /// <summary>
        /// 将html写入excel文件中
        /// </summary>
        /// <param name="htmlCode"></param>
        public void WriteData(string htmlCode)
        {
            string dataStr = FilterTable(htmlCode);
            string[] lines = Regex.Split(dataStr, @"\(\?!lineEnd\)");
            foreach (string line in lines)
            {
                colIndex = 1;
                string[] cellDatas = Regex.Split(line, @"\(\?!inlineItem\)");
                if (columnCount < cellDatas.Length) //写文件时候获取表格的最大列数;
                    columnCount = cellDatas.Length;
                foreach (string cellData in cellDatas)
                {
                    AddCellData(cellData);
                    colIndex++;
                }
                rowIndex++;
            }
            AdjustColumnWidth();
        }
        #endregion

        #region -void AdjustColumnWidth() 调整表格各列的宽度
        /// <summary>
        /// 调整表格各列的宽度
        /// </summary>
        private void AdjustColumnWidth()
        {
            for (ushort i = 1; i <= columnCount; i++)
            {
                int width = GetMaxColumnWidth(i);
                ColumnInfo columnInfo = new ColumnInfo(xls, worksheet);
                columnInfo.ColumnIndexStart = (ushort)(i - 1); //索引从0开始
                columnInfo.ColumnIndexEnd = (ushort)(i - 1);//索引从0开始
                columnInfo.Width = (ushort)((width + 2) * 256);
                worksheet.AddColumnInfo(columnInfo);
            };
        }
        #endregion

        #region -XF GetCellStyle(bool isBold) 获取单元格的格式
        /// <summary>
        ///  获取单元格的格式
        /// </summary>
        /// <param name="isBold">isBold是否粗体</param>
        /// <returns></returns>
        private XF GetCellStyle(bool isBold)
        {
            XF xf = xls.NewXF();
            xf.HorizontalAlignment = HorizontalAlignments.Centered;
            xf.VerticalAlignment = VerticalAlignments.Centered;
            xf.Format = StandardFormats.General;
            xf.Font.Bold = isBold;
            xf.TextWrapRight = true;
            return xf;
        }

        private XF GetCellStyle(bool isBold, HorizontalAlignments align)
        {
            XF xf = xls.NewXF();
            xf.HorizontalAlignment = align;
            xf.VerticalAlignment = VerticalAlignments.Centered;
            xf.Format = StandardFormats.General;
            xf.Font.Bold = isBold;
            xf.TextWrapRight = true;
            return xf;
        }
        #endregion

        #region -int GetMaxColumnWidth(int colIndex) 获取worksheet中某一列的最大宽度
        /// <summary>
        /// 获取表格某一列的最大宽度
        /// </summary>
        /// <param name="colIndex"></param>
        /// <returns></returns>
        private int GetMaxColumnWidth(ushort colIndex)
        {
            int maxLength = 0;
            int rowCount = worksheet.Rows.Count;
            for (ushort i = 1; i <= rowCount; i++)
            {
                if (!worksheet.Rows[i].CellExists(colIndex))
                    continue;
                Cell cell = worksheet.Rows[i].CellAtCol(colIndex);
                string[] valueArr = cell.Value.ToString().Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                int strLength = valueArr.Length > 0 ? valueArr.Max(c => GetStrLength(c)) : GetStrLength(cell.Value.ToString());
                if (maxLength < strLength)
                    maxLength = strLength;
            }
            return maxLength;
        }
        #endregion

        #region -GetMergeInfo(string data):MergeInfo 获取单元格合并的额外信息
        /// <summary>
        /// 获取单元格合并的额外信息
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private MergeInfo GetMergeInfo(string data)
        {
            MergeInfo mergeInfo = null;
            Match info = Regex.Match(data, @"\(\?!(?<mergeType>\w+)\s*=\s*[""']?(?<mergeSpan>\d+)[""']?\)");
            string mergeType = string.Empty;
            int mergeSpan;
            if (info.Success)
            {
                mergeType = info.Groups["mergeType"].Value.ToString();
                mergeSpan = Convert.ToInt32(info.Groups["mergeSpan"].Value);
                mergeInfo = new MergeInfo() { MergeType = mergeType, MergeSpan = mergeSpan };
            }
            return mergeInfo;
        }
        #endregion

        #region -string FilterTable(string srcStr):string  将html的table标签内的加工成有效数据
        /// <summary>
        /// 将html的table标签内的加工成有效数据
        /// </summary>
        /// <param name="srcStr"></param>
        /// <returns></returns>
        private static string FilterTable(string srcStr)
        {
            string[] patterns ={
                                @"\s+(?=<[^<>]+>)|(?<=<[^<>]+>)\s+",//一，替换无效空格()，换行符为空
                                @"<\s*/(?:td|th)\s*>\s*<\s*/tr\s*>",    //二，替换</td></tr>标签为换行
                                //@"<\s*/(?:th|td)\s*>\(\?!lineEnd\)", //三，将每行最后一个单元格的</td>或</th>标签替换为空
                                @"<\s*/(td|th)\s*>",   //四，替换</td></th>标签为\t
                                @"(?:colspan|rowspan)=[""']?\d+[""']?", //五，获取colspan和rowspan信息
                                @"(?:align)=[""']?(left|right|center)[""']?",
                                @"<\s*th[^<>]*>", //六，获取<th>替换为<>(!th)<>
                                "<[^<>]*>",     //六，将无效的标签替换为空
                                //@"(?<=\d),(?=\d)", //七，数字逗号分隔符中的逗号替换为空
                                @"\(\?!lineEnd\)$"  //八，去除最后一个\n
                              };
            string[] replaceMent = { "", "(?!lineEnd)", "(?!inlineItem)", ">(?!$0)<", ">(?!$1)<", "<>(?!bold)<>", "","" }; //要替换的字符数组
            if (!Regex.IsMatch(srcStr, @"<\s*table[^<>]*>.*?(?:<\s*/table\s*>)", RegexOptions.IgnoreCase | RegexOptions.Singleline))
                return "";
            for (int i = 0; i < patterns.Length; i++)
            {
                RegexOptions options = RegexOptions.IgnoreCase;
                //if (i == 2)  //第三部时候要开启多行模式
                //    options = RegexOptions.IgnoreCase | RegexOptions.Multiline;
                srcStr = Regex.Replace(srcStr, patterns[i], replaceMent[i], options);
            }
            return srcStr;
        }
        #endregion

        #region +int GetStrLength(string str) 获取字符串的长度,中文字符占两个字节
        /// <summary>
        /// 获取字符串的长度,中文字符占两个字节
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private int GetStrLength(string str)
        {
            int length = str.Length;
            foreach (char ch in str)
                if ((int)ch > 128)
                    length++;
            return length;
        }
        #endregion

        #region 设置单元格的样式 -private void SetCellStyleByCellData(string data)
        /// <summary>
        /// 设置单元格的样式
        /// </summary>
        /// <param name="data"></param>
        private void SetCellStyleByCellData(string data)
        {
            if (data.Contains("(?!bold)"))
            {
                this.cellStyle.Font.Bold = true;
            }
            else
            {
                this.cellStyle.Font.Bold = false;
            }
            this.cellStyle.HorizontalAlignment = HorizontalAlignments.Centered;
            Match match = Regex.Match(data, @"\(\?!(?<align>left|right|center)\)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                string align = match.Groups["align"].Value.ToString().ToLower();
                HorizontalAlignments hAlign = (align == "left" ? HorizontalAlignments.Left : (align == "right" ? HorizontalAlignments.Right : HorizontalAlignments.Centered));
                cellStyle.HorizontalAlignment = hAlign;
            }
        }
        #endregion
    }
}
