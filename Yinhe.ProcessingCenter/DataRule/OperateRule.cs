using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using MongoDB.Bson;
using MongoDB.Driver.Builders;

namespace Yinhe.ProcessingCenter.DataRule
{
    /// <summary>
    /// 操作规则
    /// </summary>
    public class OperateRule
    {
        #region 属性
        /// <summary>
        /// 级联内顶级变量
        /// </summary>
        public List<VarRule> Vars = new List<VarRule>();

        /// <summary>
        /// 级联内顶级循环
        /// </summary>
        public List<ForeachRule> Foreachs = new List<ForeachRule>();

        /// <summary>
        /// 级联内顶级判断
        /// </summary>
        public List<IfRule> Ifs = new List<IfRule>();

        /// <summary>
        /// 级联内顶级操作
        /// </summary>
        public List<StorageRlue> Storages = new List<StorageRlue>();

        #endregion

        #region 构造
        /// <summary>
        /// 默认构造函数
        /// </summary>
        public OperateRule()
        {

        }

        /// <summary>
        /// 初始化内容构造
        /// </summary>
        /// <param name="entityElement"></param>
        public OperateRule(XElement entityElement)
        {
            this.SetOperateRule(entityElement);
        }
        #endregion

        #region 方法
        /// <summary>
        /// 获取顶级操作对象,即获取级联操作父类属性
        /// </summary>
        /// <param name="entityElement"></param>
        /// <returns></returns>
        public void SetOperateRule(XElement entityElement)
        {
            #region 顶级变量
            List<VarRule> vars = new List<VarRule>();

            foreach (var tempElement in entityElement.Elements("Var"))
            {
                VarRule tempVar = new VarRule(tempElement);

                vars.Add(tempVar);
            }

            this.Vars = vars;
            #endregion

            #region 顶级循环
            List<ForeachRule> foreachs = new List<ForeachRule>();

            foreach (var tempElement in entityElement.Elements("Foreach"))
            {
                ForeachRule tempForeach = new ForeachRule(tempElement);

                foreachs.Add(tempForeach);
            }

            this.Foreachs = foreachs;
            #endregion

            #region 顶级判断
            List<IfRule> ifs = new List<IfRule>();

            foreach (var tempElement in entityElement.Elements("If"))
            {
                IfRule tempIf = new IfRule(tempElement);

                ifs.Add(tempIf);
            }

            this.Ifs = ifs;
            #endregion

            #region 顶级操作
            List<StorageRlue> storages = new List<StorageRlue>();

            foreach (var tempElement in entityElement.Elements("Storage"))
            {
                StorageRlue tempStorage = new StorageRlue(tempElement);

                storages.Add(tempStorage);
            }

            this.Storages = storages;
            #endregion

        }

        /// <summary>
        /// 获取顶级存储语句
        /// </summary>
        /// <param name="varList"></param>
        /// <param name="sourceDic"></param>
        /// <returns></returns>
        public List<StorageData> GetStorageDatas(MongoOperation _dbOp, List<VarRule> varList, Dictionary<string, BsonDocument> sourceDic)
        {
            List<StorageData> resultList = new List<StorageData>();

            #region 处理变量
            List<VarRule> allVarList = new List<VarRule>();

            if (varList != null) allVarList.AddRange(varList);

            if (this.Vars != null)
            {
                foreach (var tempVar in this.Vars)
                {
                    if (tempVar.Value.Type == DataObjectType.AValue) //如果是一个值,添加到变量中去
                    {
                        allVarList.Add(tempVar);
                    }
                    else if (tempVar.Value.Type == DataObjectType.ARecord)
                    {
                        BsonDocument tempBson = _dbOp.FindOne(tempVar.Value.TableName, tempVar.Value.GetQuery(allVarList, sourceDic));

                        if (sourceDic == null) sourceDic = new Dictionary<string, BsonDocument>();

                        sourceDic.Add(tempVar.Name, tempBson);
                    }
                }
            }
            #endregion

            #region 处理顶级存储语句
            if (this.Storages != null)
            {
                foreach (var tempStorage in this.Storages)
                {
                    StorageData tempData = new StorageData();

                    tempData.Name = tempStorage.Data.TableName;                                         //操作表名
                    tempData.Query = tempStorage.Data.GetQuery(allVarList, sourceDic);          //定位记录
                    tempData.Type = tempStorage.Type;                                                   //操作类型
                    tempData.Document = tempStorage.Data.GetDataBsonDocument(allVarList, sourceDic);    //操作数据

                    resultList.Add(tempData);
                }
            }
            #endregion

            #region 处理子循环语句
            if (this.Foreachs != null)
            {
                foreach (var tempForeach in this.Foreachs)
                {
                    List<BsonDocument> tempDocList = _dbOp.FindAll(tempForeach.Object.TableName, tempForeach.Object.GetQuery(allVarList, sourceDic)).ToList();

                    foreach (var tempDoc in tempDocList)     //循环处理循环对象
                    {
                        if (sourceDic == null) sourceDic = new Dictionary<string, BsonDocument>();

                        if (sourceDic.ContainsKey(tempForeach.Name))
                        {
                            sourceDic[tempForeach.Name] = tempDoc;
                        }
                        else
                        {
                            sourceDic.Add(tempForeach.Name, tempDoc);
                        }

                        List<StorageData> foreachResult = tempForeach.GetStorageDatas(_dbOp, allVarList, sourceDic);  //获取子循环结果

                        resultList.AddRange(foreachResult);
                    }
                }
            }

            #endregion

            #region 处理子判断语句
            if (this.Ifs != null)
            {
                foreach (var tempIf in this.Ifs)
                {
                    if (tempIf.Condition.GetResult(allVarList,sourceDic))
                    {
                        List<StorageData> ifResult = tempIf.GetStorageDatas(_dbOp, allVarList, sourceDic);  //获取子判断结果

                        resultList.AddRange(ifResult);
                    }
                }
            }
            #endregion

            return resultList;
        }


        #endregion
    }
}
