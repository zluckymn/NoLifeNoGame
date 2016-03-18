using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;

namespace Yinhe.ProcessingCenter.Business.Device
{
    /// <summary>
    /// 设备品牌处理类
    /// </summary>
    public class BrandBll
    {
        #region 构造函数
        /// <summary>
        /// 类私有变量
        /// </summary>
        private DataOperation dataOp = null;
        private string tableName = "Device_brand";
        /// <summary>
        /// 封闭当前默认构造函数
        /// </summary>
        public BrandBll()
        {
            dataOp = new DataOperation();
        }

        /// <summary>
        /// 封闭当前默认构造函数
        /// </summary>
        private BrandBll(DataOperation ctx)
        {
            dataOp = ctx;
        }


        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static BrandBll _()
        {
            return new BrandBll();
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static BrandBll _(DataOperation ctx)
        {
            return new BrandBll(ctx);
        }
        #endregion

        #region 获取品牌字典  key:Id value:名称 +Dictionary<string, string> GetIdNameDic()
        /// <summary>
        /// 获取品牌或者属性类型字典  key:Id value:名称
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public Dictionary<string, string> GetIdNameDic()
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            List<BsonDocument> brandList = dataOp.FindAll(tableName).ToList();
            foreach (BsonDocument brand in brandList)
            {
                dic.Add(brand.String("brandId"), brand.String("name"));
            }       
            return dic;
        }
        #endregion
    }
}
