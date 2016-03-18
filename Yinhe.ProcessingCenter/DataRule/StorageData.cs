using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Driver.Builders;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Yinhe.ProcessingCenter.DataRule
{
    /// <summary>
    /// 数据存储类
    /// </summary>
    public class StorageData
    {
        /// <summary>
        /// 表名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 操作类型
        /// </summary>
        public StorageType Type { get; set; }

        /// <summary>
        /// 定位关键字
        /// </summary>
        public IMongoQuery Query { get; set; }

        /// <summary>
        /// 保存文档
        /// </summary>
        public BsonDocument Document { get; set; }
    }
}
