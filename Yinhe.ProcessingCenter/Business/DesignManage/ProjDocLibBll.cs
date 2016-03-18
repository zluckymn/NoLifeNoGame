using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using Yinhe.ProcessingCenter.DataRule;
using MongoDB.Bson;
using Yinhe.ProcessingCenter;
using MongoDB.Driver.Builders;
///<summary>
///分项资料库管理
///</summary>
namespace Yinhe.ProcessingCenter.Business.DesignManage
{
    /// <summary>
    /// 分项资料库处理类
    /// </summary>
    public class ProjDocLibBll
    {
    
        #region 构造函数
        /// <summary>
        /// 类私有变量
        /// </summary>
        private DataOperation _ctx = null;

        /// <summary>
        /// 封闭当前默认构造函数
        /// </summary>
        public ProjDocLibBll()
        {
            _ctx = new DataOperation();
        }

        public ProjDocLibBll(DataOperation ctx)
        {
            _ctx = ctx;
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static ProjDocLibBll _()
        {
            return new ProjDocLibBll();
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static ProjDocLibBll _(DataOperation ctx)
        {
            return new ProjDocLibBll(ctx);
        }
        #endregion
        #region 操作
        /// <summary>
        /// 检查重名
        /// </summary>
        /// <param name="docLibId"></param>
        /// <param name="docCatId"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool CheckName(int docLibId, int nodePid, int docCatId, string name)
        {
            BsonDocument tempData=this._ctx.FindOneByQuery("ProjDoctationCatProtery",Query.And(Query.EQ("projDoctionId",docLibId.ToString()),Query.EQ("nodePid",nodePid.ToString()),Query.EQ("projDocCatId",docCatId.ToString()),Query.EQ("name",name)));

            if (tempData!=null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion


    }
}
