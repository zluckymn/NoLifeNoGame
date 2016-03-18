using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
///<summary>
///设备管理
///</summary>
namespace Yinhe.ProcessingCenter.Business.Device
{
    /// <summary>
    /// 设备属性类型处理类
    /// </summary>
    public class AttrTypeBll
    {
        private DataOperation dataOp = null;
        private string tableName = "Device_attrType";
        #region 构造函数
        /// <summary>
        /// 类私有变量
        /// </summary>
        
        /// <summary>
        /// 封闭当前默认构造函数
        /// </summary>
        public AttrTypeBll()
        {
            dataOp = new DataOperation();
        }
        /// <summary>
        /// 封闭当前默认构造函数
        /// </summary>
        public AttrTypeBll(string tbName)
        {
            dataOp = new DataOperation();
            tableName = tbName;
        }
        /// <summary>
        /// 封闭当前默认构造函数
        /// </summary>
        private AttrTypeBll(DataOperation ctx)
        {
            dataOp = ctx;
        }


        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static AttrTypeBll _()
        {
            return new AttrTypeBll();
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static AttrTypeBll _(DataOperation ctx)
        {
            return new AttrTypeBll(ctx);
        }
        #endregion

        #region 获取属性类型字典  key:Id value:名称 +Dictionary<string, string> GetIdNameDic()
        /// <summary>
        /// 获取品牌或者属性类型字典  key:Id value:名称
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public Dictionary<string, string> GetIdNameDic()
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            List<BsonDocument> attrTypeList = dataOp.FindAll(tableName).ToList();
            foreach (BsonDocument attrType in attrTypeList)
            {
                dic.Add(attrType.String("typeId"), attrType.String("name"));
            }
            return dic;
        }
        #endregion
    }
}
