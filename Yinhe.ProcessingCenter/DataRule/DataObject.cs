using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System.Text.RegularExpressions;

namespace Yinhe.ProcessingCenter.DataRule
{
    /// <summary>
    /// 数据对象
    /// </summary>
    public class DataObject
    {
        #region 属性
        /// <summary>
        /// 表名
        /// </summary>
        public string TableName = "";

        /// <summary>
        /// 查询字符串
        /// </summary>
        public string QueryStr = "";

        /// <summary>
        /// 数据字符串
        /// </summary>
        public string DataStr = "";

        /// <summary>
        /// 普通值字符串
        /// </summary>
        public string ValueStr = "";

        /// <summary>
        /// 对象类型
        /// </summary>
        public DataObjectType Type;

        /// <summary>
        /// 备注
        /// </summary>
        public string Remark = "";


        #endregion

        #region 构造
        /// <summary>
        /// 带对象字符串的构造函数
        /// </summary>
        /// <param name="objectStr"></param>
        public DataObject(string objectStr)
        {
            this.SetDataObject(objectStr);
        }
        #endregion

        #region 方法
        /// <summary>
        /// 设置对象值
        /// </summary>
        /// <param name="objectStr"></param>
        public void SetDataObject(string objectStr)
        {
            if (objectStr.Contains("|A3|"))
            {
                string[] tempArray = objectStr.Split(new string[] { "|A3|" }, StringSplitOptions.None);

                if (tempArray.Count() == 2)             //如果含有表名加查询
                {
                    this.TableName = tempArray[0];

                    if (string.IsNullOrEmpty(tempArray[1]))
                    {
                        this.Type = DataObjectType.ATable;          //查询为空,则为表
                    }
                    else
                    {
                        this.QueryStr = tempArray[1];

                        if (tempArray[0].Contains(".findOne("))         //findOne查询为单条记录,可为源记录
                        {
                            this.Type = DataObjectType.ARecord;
                        }
                        else if (tempArray[0].Contains(".distinct("))   //distinct查询为多条记录
                        {
                            this.Type = DataObjectType.MultipleRecords;
                        }
                    }
                }

                if (tempArray.Count() == 3)
                {
                    this.TableName = tempArray[0];
                    this.QueryStr = tempArray[1];
                    this.DataStr = tempArray[2];

                    this.Type = DataObjectType.AStorage;        //一个存储对象
                }
            }
            else
            {
                this.TableName = null;
                this.QueryStr = null;
                this.ValueStr = objectStr;

                this.Type = DataObjectType.AValue;        //一个值对象
            }
        }

        /// <summary>
        /// 获取数据对象中的查询
        /// </summary>
        /// <param name="varList"></param>
        /// <param name="sourceDic"></param>
        /// <returns></returns>
        public IMongoQuery GetQuery(List<VarRule> varList, Dictionary<string, BsonDocument> sourceDic)
        {
            var resultQuery = Query.Null;

            if (this.QueryStr != null)
            {
                string tempStr = this.ReplaceBracket(this.QueryStr, varList, sourceDic);    //替换查询语句中的中括号

                resultQuery = TypeConvert.NativeQueryToQuery(tempStr);           //将查询语句转换为Query并返回
            }

            return resultQuery;
        }

        /// <summary>
        /// 获取数据对象中的数据文档
        /// </summary>
        /// <param name="varList"></param>
        /// <param name="sourceDic"></param>
        /// <returns></returns>
        public BsonDocument GetDataBsonDocument(List<VarRule> varList, Dictionary<string, BsonDocument> sourceDic)
        {
            BsonDocument resultBson = null;

            if (this.DataStr != null)
            {
                string tempStr = this.ReplaceBracket(this.DataStr, varList, sourceDic); //替换数据中的中括号

                resultBson = TypeConvert.ParamStrToBsonDocument(tempStr);         //将字符串转换为bson文档
            }

            return resultBson;
        }

        /// <summary>
        /// 获取数据对象中的值
        /// </summary>
        /// <param name="varList"></param>
        /// <param name="sourceDic"></param>
        /// <returns></returns>
        public string GetValue(List<VarRule> varList, Dictionary<string, BsonDocument> sourceDic)
        {
            string resultVal = null;

            if (this.ValueStr != null)
            {
                string tempStr = this.ReplaceBracket(this.ValueStr, varList, sourceDic); //替换中括号

                if (tempStr.StartsWith("db."))      //如果是一个数据库查询,则进入数据查询具体值
                {
                    resultVal = TypeConvert.NativeQueryToResultValue(tempStr);
                }
                else
                {
                    resultVal = tempStr;
                }
            }

            return resultVal;
        }

        /// <summary>
        /// 递归处理级联语句中的中括号
        /// </summary>
        /// <param name="sourceStr"></param>
        /// <param name="sourceDic"></param>
        /// <returns></returns>
        public string ReplaceBracket(string sourceStr, List<VarRule> varList, Dictionary<string, BsonDocument> sourceDic)
        {
            string resultStr = sourceStr;

            if (sourceStr.StartsWith("[") && sourceStr.EndsWith("]"))
            {
                resultStr = sourceStr.TrimStart('[').TrimEnd(']');       //去掉源字符串的开头中括号和结尾中括号
            }

            if (resultStr.Contains("[") && resultStr.Contains("]"))         //如果源字串含有子中括号
            {
                Regex rgxBracket = new Regex(@"\[[^\[\]]*(((?'Open'\[)[^\[\]]*)+((?'-Open'\])[^\[\]]*)+)*(?(Open)(?!))\]");
                MatchCollection matchBracket = rgxBracket.Matches(resultStr);   //正则匹配所有最外层的中括号

                if (matchBracket.Count > 0)
                {
                    foreach (var mat in matchBracket)
                    {
                        string tempResult = this.ReplaceBracket(mat.ToString(), varList, sourceDic); //递归解析中括号的值

                        resultStr = resultStr.Replace(mat.ToString(), tempResult);       //替换中括号的值
                    }
                }
            }
            else            //如果不含中括号
            {
                resultStr = this.ResolveBracket(sourceStr, varList, sourceDic);      //解析中括号值
            }

            return resultStr;
        }

        /// <summary>
        /// 解析级联操作中中括号括号的值
        /// </summary>
        /// <param name="sourceStr"></param>
        /// <param name="varList"></param>
        /// <param name="sourceDic"></param>
        /// <returns></returns>
        public string ResolveBracket(string sourceStr, List<VarRule> varList, Dictionary<string, BsonDocument> sourceDic)
        {
            string resultStr = sourceStr;

            if (sourceStr.StartsWith("[") && sourceStr.EndsWith("]"))
            {
                resultStr = sourceStr.TrimStart('[').TrimEnd(']');

                #region 判断是否为一个数组
                if (resultStr.Contains(","))
                {
                    bool flag = false;

                    string[] tempArray = resultStr.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);

                    if (tempArray.Count() > 0)
                    {
                        #region 判断是不是每一段都是被 " " 包裹
                        if (flag == false)
                        {
                            foreach (var temp in tempArray)
                            {
                                if (temp.StartsWith("\"") && temp.EndsWith("\"")) flag = true;
                                else                //如果不是被 "" 包裹,返回标记为假
                                {
                                    flag = false; break;
                                }
                            }
                        }
                        #endregion

                        #region 判断是不是每一段都是被 '' 包裹
                        if (flag == false)
                        {
                            foreach (var temp in tempArray)
                            {
                                if (temp.StartsWith("'") && temp.EndsWith("'")) flag = true;
                                else                //如果不是被 '' 包裹,返回标记为假
                                {
                                    flag = false; break;
                                }
                            }
                        }
                        #endregion

                        #region 判断是不是每一段都没有被任何引号包裹
                        if (flag == false)
                        {
                            foreach (var temp in tempArray)
                            {
                                if (temp.StartsWith("'") == false && temp.StartsWith("\"") == false && temp.EndsWith("'") && temp.EndsWith("\"")) flag = true;
                                else                //如果有 " 或 ' 开头或结束 ,返回标记为假
                                {
                                    flag = false; break;
                                }
                            }
                        }
                        #endregion

                    }

                    if (flag == true) return sourceStr; //如果是一个数组,返回原字符串
                }
                #endregion

                #region 判断是否为一个字符串
                if ((resultStr.StartsWith("'") && resultStr.EndsWith("'")) || (resultStr.StartsWith("\"") && resultStr.EndsWith("\"")))
                {
                    return resultStr.TrimStart('\'').TrimStart('"').TrimEnd('\'').TrimEnd('"'); //如果是字符串,返回去掉引号后的值
                }
                #endregion

                #region 判断是否是 *.* 结构
                if (resultStr.Contains("."))       //如果是{*.*}类型,从源集合中取值,或直接数据库查询
                {
                    string[] tempArray = resultStr.Split(new string[] { "." }, StringSplitOptions.None);

                    #region 如果是 [this.column] , [var.column] 类型
                    if (tempArray.Count() == 2)
                    {
                        string tempBsonKey = tempArray[0];      //引用变量名

                        string tempColumnKey = tempArray[1];    //字段名

                        BsonDocument tempBson = (sourceDic != null && sourceDic.ContainsKey(tempBsonKey)) ? sourceDic[tempBsonKey] : null;  //对应变量BSON

                        resultStr = (tempBson != null && tempBson.Contains(tempColumnKey)) ? tempBson[tempColumnKey].ToString() : "";       //获取对应字段值
                    }
                    #endregion

                    #region 如果是 [db.*.findOne(..).column] 类型
                    if (tempArray.Count() == 4) resultStr = TypeConvert.NativeQueryToResultValue(resultStr);//执行原生查询
                    #endregion

                    return resultStr;
                }
                #endregion

                #region 判断是否是变量
                VarRule tempVar = varList.Where(t => t.Name == resultStr).FirstOrDefault();

                if (tempVar != null)
                {
                    resultStr = tempVar.Value.GetValue(varList, sourceDic);
                }
                #endregion
            }

            return resultStr;
        }



        #endregion
    }
}
