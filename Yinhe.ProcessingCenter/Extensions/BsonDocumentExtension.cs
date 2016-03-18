using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver;
using Yinhe.ProcessingCenter.DataRule;
using Yinhe.ProcessingCenter;

///<summary>
///MongoDB基础及其重载
///</summary>
namespace MongoDB.Bson
{
    /// <summary>
    /// BsonDoc方法重载
    /// </summary>
    public static class BsonDocumentExtension
    {
        #region 外键获取
        /// <summary>
        /// 获取字段对应的主表记录
        /// </summary>
        /// <param name="bsonDoc"></param>
        /// <param name="foreignFieldName"> 外键字段名称</param>
        /// <returns></returns>
        public static BsonDocument SourceBson(this BsonDocument bsonDoc, string foreignFieldName)
        {
            BsonDocument sourceDoc = null;

            if (bsonDoc != null && bsonDoc.Contains(foreignFieldName) && bsonDoc.Contains("underTable"))
            {
                string tbName = bsonDoc.GetValue("underTable").ToString();      //当前记录所属表

                TableRule tableEntity = new TableRule(tbName);    //获取表结构

                ColumnRule columnEntity = tableEntity.ColumnRules.Where(t => t.Name == foreignFieldName).FirstOrDefault();  //获取对应字段

                if (columnEntity != null && columnEntity.SourceTable != "" && columnEntity.SourceColumn != "")  //如果字段有标记外键,则返回对应源记录
                {
                    sourceDoc = new DataOperation(columnEntity.SourceTable).FindOneByKeyVal(columnEntity.SourceColumn, bsonDoc.GetValue(foreignFieldName).ToString());
                }
            }

            return sourceDoc;
        }

        /// <summary>
        /// 快捷获取字段对应的主表记录的字段值
        /// </summary>
        /// <param name="bsonDoc"></param>
        /// <param name="foreignFieldName">外键值</param>
        /// <param name="fieldName">外键表要获取的字段</param>
        /// <returns></returns>
        public static string SourceBsonField(this BsonDocument bsonDoc, string foreignFieldName, string fieldName)
        {
            BsonDocument sourceDoc = bsonDoc.SourceBson(foreignFieldName);
            if (sourceDoc!=null)
            {
                return sourceDoc.Text(fieldName);
            }
             return string.Empty;
        }

        /// <summary>
        /// 获取字段对应的主表记录
        /// </summary>
        /// <param name="bsonDoc"></param>
        /// <param name="name">字段名称</param>
        /// <returns></returns>
        public static List<BsonDocument> ChildBsonList(this BsonDocument bsonDoc, string tbName)
        {
            List<BsonDocument> childBsonList = new List<BsonDocument>();
            
            if (bsonDoc != null && bsonDoc.String("underTable") != "")
            {
                string curTbName = bsonDoc.String("underTable");      //当前记录所属表

                TableRule childTable= new TableRule(tbName);        //获取子表的表结构

                ColumnRule foreignColumn = childTable.ColumnRules.Where(t => t.SourceTable == curTbName).FirstOrDefault();  //子表指向源表的外键字段

                if (foreignColumn != null && foreignColumn.SourceTable != "" && foreignColumn.SourceColumn != "")   //外键不为空,且有对应表，对应字段
                {
                    childBsonList = new DataOperation(tbName).FindAllByKeyVal(foreignColumn.Name, bsonDoc.String(foreignColumn.SourceColumn)).ToList();
                }
            }
            return childBsonList;
        }
        #endregion

        #region 通用获取
        /// <summary>
        /// 获取文本值
        /// </summary>
        /// <param name="bsonDoc"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string Text(this BsonDocument bsonDoc, string name)
        {
            return String(bsonDoc, name);
        }
        
        /// <summary>
        ///  获取文本值
        /// </summary>
        /// <param name="bsonDoc"></param>
        /// <param name="name"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static string Text(this BsonDocument bsonDoc, string name, string defaultValue)
        {
            return String(bsonDoc, name, defaultValue);
        }

        /// <summary>
        /// 获取字符串值
        /// </summary>
        /// <param name="bsonDoc"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string String(this BsonDocument bsonDoc, string name)
        {
            return String(bsonDoc, name, "");
        }

        /// <summary>
        /// 向对象新增key，如果key已经存在，则修改原先key对应的值
        /// </summary>
        /// <param name="bsonDoc"></param>
        /// <param name="key">key值</param>
        /// <param name="val">要新增的对象</param>
        public static void TryAdd(this BsonDocument bsonDoc, string key, string val)
        {
            if (bsonDoc.Contains(key))
            {
                bsonDoc[key] = val;
            }
            else
            {
                bsonDoc.Add(key, val);
            }
        }

        /// <summary>
        ///  获取字符串值
        /// </summary>
        /// <param name="bsonDoc"></param>
        /// <param name="name"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static string String(this BsonDocument bsonDoc, string name, string defaultValue)
        {
            if (bsonDoc != null && bsonDoc.Contains(name))
            {
                return bsonDoc.GetValue(name).ToString();
            }

            return defaultValue;
        }

        /// <summary>
        /// 获取整形值
        /// </summary>
        /// <param name="bsonDoc"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static int Int(this BsonDocument bsonDoc, string name)
        {
            return Int(bsonDoc, name, 0);
        }

        /// <summary>
        /// 获取整形值
        /// </summary>
        /// <param name="bsonDoc"></param>
        /// <param name="name"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static int Int(this BsonDocument bsonDoc, string name, int defaultValue)
        {
            if (bsonDoc != null && bsonDoc.Contains(name))
            {
                int temp = new int();

                if (int.TryParse(bsonDoc.GetValue(name).ToString(), out temp))
                {
                    return bsonDoc.GetValue(name).ToInt32();
                }
            }

            return defaultValue;
        }

        /// <summary>
        /// 货币的格式展示
        /// </summary>
        /// <param name="bsonDoc"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string Money(this BsonDocument bsonDoc, string name)
        {
            if (bsonDoc != null && bsonDoc.Contains(name))
            {
                decimal value;
                string str = bsonDoc.GetValue(name).ToString();
                if (!string.IsNullOrEmpty(str) && decimal.TryParse(str, out value))
                {
                    return value.ToMoney();
                }
            }
            return string.Empty;
            
        }

        /// <summary>
        /// 获取日期值,无值则返回DateTime.MinValue
        /// </summary>
        /// <param name="bsonDoc"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static DateTime Date(this BsonDocument bsonDoc, string name)
        {
            return Date(bsonDoc, name, default(DateTime));
        }

        /// <summary>
        /// 获取日期值
        /// </summary>
        /// <param name="bsonDoc"></param>
        /// <param name="name"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static DateTime Date(this BsonDocument bsonDoc, string name, DateTime defaultValue)
        {
            if (bsonDoc != null && bsonDoc.Contains(name))
            {
                DateTime temp;

                if (DateTime.TryParse(bsonDoc.GetValue(name).ToString(), out temp))
                {
                    return temp;
                }
                else
                {
                    return defaultValue;
                }
            }

            return defaultValue;
        }

        /// <summary>
        /// 获取浮点值
        /// </summary>
        /// <param name="bsonDoc"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static double Double(this BsonDocument bsonDoc, string name)
        {
            return Double(bsonDoc, name, 0);
        }

        /// <summary>
        /// 获取浮点值
        /// </summary>
        /// <param name="bsonDoc"></param>
        /// <param name="name"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static double Double(this BsonDocument bsonDoc, string name, double defaultValue)
        {
            if (bsonDoc != null && bsonDoc.Contains(name))
            {
                double temp = new double();

                if (double.TryParse(bsonDoc.GetValue(name).ToString(), out temp))
                {
                    return temp;
                }
            }

            return defaultValue;
        }

        /// <summary>
        /// 获取浮点值
        /// </summary>
        /// <param name="bsonDoc"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static decimal Decimal(this BsonDocument bsonDoc, string name)
        {
            return Decimal(bsonDoc, name, 0);
        }

        /// <summary>
        /// 获取浮点值
        /// </summary>
        /// <param name="bsonDoc"></param>
        /// <param name="name"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static decimal Decimal(this BsonDocument bsonDoc, string name, decimal defaultValue)
        {
            if (bsonDoc != null && bsonDoc.Contains(name))
            {
                decimal temp = new decimal();

                if (decimal.TryParse(bsonDoc.GetValue(name).ToString(), out temp))
                {
                    return temp;
                }
            }

            return defaultValue;
        }


        /// <summary>
        /// 获取当前记录的是否包含列
        /// </summary>
        /// <returns></returns>
        public static bool ContainsColumn(this BsonDocument bsonDoc, string name)
        {
            return bsonDoc.Elements.Where(c => c.Name.Trim()==name.Trim()).Count()>0;
        }

        /// <summary>
        /// 获取当前记录的是否包含列
        /// </summary>
        /// <returns></returns>
        public static int  ColumnCount(this BsonDocument bsonDoc)
        {
            return bsonDoc.Elements.Count();
        }


        #endregion

        #region 快捷获取
        /// <summary>
        /// 获取当前记录的时间
        /// </summary>
        /// <returns></returns>
        public static string CreateDate(this BsonDocument bsonDoc)
        {
            return bsonDoc.Date("createDate").ToString("yyyy-MM-dd");
        }

        /// <summary>
        /// 获取当前记录的创建人
        /// </summary>
        /// <returns></returns>
        public static string UpdateDate(this BsonDocument bsonDoc)
        {
            return bsonDoc.Date("updateDate").ToString("yyyy-MM-dd");
        }

        /// <summary>
        /// 获取当前记录的创建人
        /// </summary>
        /// <returns></returns>
        public static string CreateUserName(this BsonDocument bsonDoc)
        {
            DataOperation dataop = new DataOperation();
            var createUser = dataop.FindOneByKeyVal("SysUser", "userId", bsonDoc.Text("createUserId"));
            if (createUser != null)
            {
                return createUser.Text("name");
            }
            return string.Empty;
        }

        /// <summary>
        /// 获取当前记录的创建人
        /// </summary>
        /// <returns></returns>
        public static string UpdateUserName(this BsonDocument bsonDoc)
        {
            DataOperation dataop = new DataOperation();
            var createUser = dataop.FindOneByKeyVal("SysUser", "userId", bsonDoc.Text("updateUserId"));
            if (createUser != null)
            {
                return createUser.Text("name");
            }
            return string.Empty;
        }

        /// <summary>
        /// 获取日期值,无值则返回DateTime.MinValue
        /// </summary>
        /// <param name="bsonDoc"></param>
        /// <param name="name">字段名</param>
        /// <param name="formate">例如"yyyy-MM-dd"</param>
        /// <returns></returns>
        public static string DateFormat(this BsonDocument bsonDoc, string name, string formate)
        {
            return bsonDoc.Date(name) != DateTime.MinValue ? bsonDoc.Date(name).ToString(formate) : "";
        }

        /// <summary>
        /// 获取日期值,无值则返回DateTime.MinValue
        /// </summary>
        /// <param name="bsonDoc"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string ShortDate(this BsonDocument bsonDoc, string name)
        {
            var formate = "yyyy-MM-dd";
            return bsonDoc.DateFormat(name, formate);
        }

        /// <summary>
        /// 获取主键字段名
        /// </summary>
        /// <param name="?"></param>
        /// <returns></returns>
        public static string KeyField(this BsonDocument bsonDoc)
        {
            var tbName = bsonDoc.TableName();
            var keyField=string.Empty;
             TableRule childTable = new TableRule(tbName);        //获取子表的表结构
            ColumnRule foreignColumn = childTable.ColumnRules.Where(t => t.IsPrimary == true).FirstOrDefault();  //子表指向源表的外键字段
            if (foreignColumn != null)
            {
                keyField = foreignColumn.Name;
            }
            return keyField;
        }

        /// <summary>
        /// 获取主键字段名
        /// </summary>
        /// <param name="?"></param>
        /// <returns></returns>
        public static string  TableName(this BsonDocument bsonDoc)
        {
            var tbName = string.Empty;
            if (bsonDoc.Contains("underTable"))
            {
                tbName = bsonDoc.GetValue("underTable").ToString();      //当前记录所属表
            }

            return tbName;
        }

        /// <summary>
        /// 获取主键字段
        /// </summary>
        /// <param name="?"></param>
        /// <returns></returns>
        public static string KeyValue(this BsonDocument bsonDoc)
        {
           return bsonDoc.Text(bsonDoc.KeyField());
        }
        #endregion

        #region 扩展方法
        /// <summary>
        /// 判断一个BsonDocument是否为空或为NULL
        /// </summary>
        /// <param name="bsonDoc"></param>
        /// <returns></returns>
        public static bool IsNullOrEmpty(this BsonDocument bsonDoc)
        {
            if (bsonDoc == null) return true;

            if (bsonDoc.Elements.Count() == 0) return true;

            return false;
        }

        /// <summary>
        /// 通用设置接口
        /// </summary>
        /// <param name="bsonDoc"></param>
        /// <param name="fieldName"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static void SetValue(BsonDocument bsonDoc, string fieldName, string obj)
        {
            if (bsonDoc.Contains(fieldName))
            {
                bsonDoc[fieldName] = obj;
            }
            else
            {
                bsonDoc.Add(fieldName,obj);
            }
        }
        #endregion

    }
}
