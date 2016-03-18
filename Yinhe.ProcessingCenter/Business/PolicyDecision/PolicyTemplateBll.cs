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
    /// 决策模板处理类
    /// </summary>
    public class PolicyTemplateBll
    {
        #region 构造函数
        /// <summary>
        /// 类私有变量
        /// </summary>
        private readonly DataOperation _ctx = null;

        /// <summary>
        /// 封闭当前默认构造函数
        /// </summary>
        public PolicyTemplateBll()
        {
            _ctx = new DataOperation();
        }

        public PolicyTemplateBll(DataOperation ctx)
        {
            _ctx = ctx;
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static PolicyTemplateBll _()
        {
            return new PolicyTemplateBll();
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static PolicyTemplateBll _(DataOperation ctx)
        {
            return new PolicyTemplateBll(ctx);
        }
        #endregion

        public Dictionary<string, List<BsonDocument>> GetDeptDirDict()
        {
            var deptDirDict = new Dictionary<string, List<BsonDocument>>();
            deptDirDict.Add(PolicyDept.MarketingArea, GetTemplateDirs(PolicyDept.MarketingArea));
            deptDirDict.Add(PolicyDept.DesignArea, GetTemplateDirs(PolicyDept.DesignArea));
            return deptDirDict;
        }

        /// <summary>
        /// 获取模板的目录信息
        /// </summary>
        /// <param name="dept"></param>
        /// <returns></returns>
        public List<BsonDocument> GetTemplateDirs(string dept)
        {
            var tableRule = new TableRule(dept);
            var primaryKey = tableRule.ColumnRules.Single(s => s.IsPrimary).Name;

            var dirTable = dept + "Dir";

            var priKeyValue = _ctx.FindOneByQuery(dept, Query.EQ("isTemplate", "1")).String(primaryKey);
            var dirs = _ctx.FindAllByQuery(dirTable, Query.EQ(primaryKey, priKeyValue)).ToList();
            return dirs;
        }

        /// <summary>
        /// 获取部门目录字典
        /// </summary>
        /// <param name="versionId"></param>
        /// <returns></returns>
        public Dictionary<string, List<BsonDocument>> GetDeptDirDict(string versionId)
        {
            PolicyVersionBll versionBll = PolicyVersionBll._(_ctx);
            var deptDirDict = new Dictionary<string, List<BsonDocument>>();
            deptDirDict.Add(PolicyDept.MarketingArea, versionBll.GetTableDirsByVersionId(PolicyDept.MarketingArea,versionId));
            deptDirDict.Add(PolicyDept.DesignArea, versionBll.GetTableDirsByVersionId(PolicyDept.DesignArea, versionId));
            return deptDirDict;
        }
    }
}
