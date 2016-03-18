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
    /// 决策日志处理类
    /// </summary>
    public class PolicyLogBll
    {
        
        static DataOperation static_ctx = null;

        static PolicyLogBll()
        {
            static_ctx = new DataOperation();
        }

        /// <summary>
        /// 是否有日志
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="tbKey"></param>
        /// <returns></returns>
        public static bool HasLogs(string tbName,string tbKey,string keyValue)
        {
           // List<BsonDocument> test = static_ctx.FindAllByQuery("PolicyLog", Query.And(Query.EQ("tbName", tbName), Query.EQ(tbKey, keyValue))).ToList;
            return static_ctx.FindAllByQuery("PolicyLog", Query.And(Query.EQ("tbName", tbName), Query.EQ(tbKey, keyValue))).Any();
        }
    }
}
