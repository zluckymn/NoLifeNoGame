using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yinhe.ProcessingCenter
{
    /// <summary>
    /// 分页类实现
    /// </summary>
    public class Pagination
    {
        public int curIndex { get; set; }
        public int totalPages { get; set; }
        public long totalCount { get; set; }
        public int preIndex { get; set; }
        public int nextIndex { get; set; }
        public int pageSize { get; set; }
        public int skipCount { get; set; }

        /// <summary>
        /// 分页类构造函数:某人分页20
        /// </summary>
        /// <param name="totalCount"></param>
        /// <param name="curIndex"></param>
        public Pagination(long totalCount, int curIndex) : this(totalCount, curIndex, 20) { }
       

        /// <summary>
        /// 分页类构造函数
        /// </summary>
        /// <param name="totalCount"></param>
        /// <param name="curIndex"></param>
        /// <param name="pageSize"></param>
        public Pagination(long totalCount, int curIndex, int pageSize)
        {
            this.totalCount = totalCount;
            this.curIndex = curIndex;
            this.pageSize = pageSize;
            Init();
        }

        /// <summary>
        /// 初始化数据
        /// </summary>
        private void Init()
        {
            if (curIndex < 1) curIndex = 1;
            totalPages = (int)Math.Ceiling(totalCount / (pageSize * 1.0));
            preIndex = curIndex > 1 ? curIndex - 1 : 1;
            nextIndex = curIndex < totalPages ? curIndex + 1:totalPages;
            skipCount = (curIndex - 1) * pageSize;
        }
    }
}
