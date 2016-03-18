using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace Yinhe.ProcessingCenter.SynAD
{
    /// <summary>
    /// 同步数据获取接口
    /// </summary>
    public interface IGetDataList
    {
         List<BsonDocument> GetBsonDocumentDataList(string connStr, string commandText);
    }
}
