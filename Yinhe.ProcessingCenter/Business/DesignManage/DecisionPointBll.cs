using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using Yinhe.ProcessingCenter.DataRule;
using MongoDB.Bson;
namespace Yinhe.ProcessingCenter.DesignManage
{
    /// <summary>
    /// 决策点处理相关页
    /// </summary>
    public class DecisionPointBll
    {
        #region 构造函数
        /// <summary>
        /// 类私有变量
        /// </summary>
        private DataOperation dataOp = null;

        private string tableName = "";
        /// <summary>
        /// 封闭当前默认构造函数
        /// </summary>
        private DecisionPointBll() 
        {
            dataOp = new DataOperation();
        }

        private DecisionPointBll(DataOperation _dataOp)
        {
            dataOp = _dataOp;
        }
 
        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static DecisionPointBll _() 
        {
            return new DecisionPointBll(); 
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static DecisionPointBll _(DataOperation _dataOp)
        {
            return new DecisionPointBll(_dataOp);
        }
        
        

        #endregion

        #region 查询
        #region 查询
        /// <summary>
        /// 根据Id查询
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public BsonDocument FindById(string id)
        {
            return dataOp.FindOneByKeyVal("XH_DesignManage_DecisionPoint", "pointId", id);
        }

        public BsonDocument FindByTextId(string textId)
        {
            return dataOp.FindOneByKeyVal("XH_DesignManage_DecisionPoint", "textId", textId);
         }
        public BsonDocument FindByTextPId(string textPId)
        {
            return dataOp.FindOneByKeyVal("XH_DesignManage_DecisionPoint", "textPId", textPId);
        }
        /// <summary>
        /// 查找出所有
        /// </summary>
        /// <returns></returns>
        public List<BsonDocument> FindAll()
        {
            return  dataOp.FindAll("XH_DesignManage_DecisionPoint").ToList();
        }

        /// <summary>
        /// 根据Id列表查询
        /// </summary>
        /// <param name="idList"></param>
        /// <returns></returns>
        public List<BsonDocument> FindByIdList(List<string> idList)
        {
            return dataOp.FindAllByKeyValList("XH_DesignManage_DecisionPoint", "pointId", idList).ToList();
        }
        /// <summary>
        /// 是否存在重名
        /// </summary>
        /// <param name="idList"></param>
        /// <returns></returns>
        public bool HasExist(string textId, string name)
        {
            var query = FindAll().Where(t => t.Text("textId").Trim() == textId.Trim() && t.Text("name").Trim() == name.Trim());
            if (query.Count() > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }


       
        #endregion
        #endregion

        #region 地铁图 批量更新

        /// <summary>
        /// 地铁图编辑保存接口,先保存插入决策点
        /// </summary>
        /// <param name="mapId">地铁图Id</param>
        /// <param name="userId">当前用户</param>
        /// <param name="decisionPointList">决策点列表</param>
        /// <returns></returns>
        public InvokeResult PatchUpdate(string mapId, BsonDocument updateBson, List<BsonDocument> decisionPointList)
        {
           
            InvokeResult result = new InvokeResult();
            var updateList = new List<BsonDocument>();
            var addList = new List<BsonDocument>();
             using (var tran = new TransactionScope())
            {
                try
                {
                    #region 创建默认地铁图
                    var newMapObj = dataOp.FindOneByKeyVal("XH_DesignManage_SubwayMap", "mapId", mapId);
                    if (newMapObj == null)
                    {
                        var mapObj = new BsonDocument();
                        if (string.IsNullOrEmpty(updateBson.Text("imagePath")))
                        {
                            if (updateBson.Contains("imagePath"))
                            { 
                              updateBson["imagePath"]=string.Format(@"\Content\SubwayMap\subway.jpg");
                            }
                         }

                        var mapResult = dataOp.Insert("XH_DesignManage_SubwayMap", updateBson);
                        if (mapResult.Status == Status.Successful)
                        {
                            mapObj = mapResult.BsonInfo;
                            mapId = mapObj.Text("mapId");

                        }
                        else
                        {
                            return mapResult;
                        }
                    }
                    else
                    {

                        var mapResult = dataOp.Update(newMapObj, updateBson);
                        if (mapResult.Status != Status.Successful)
                        {
                            return mapResult;
                        }
                    }
                    #endregion

                    foreach (var decisionPointObj in decisionPointList)
                    {
                        var pointId = decisionPointObj.Text("pointId");
                        var oldDecisionPointObj = FindByTextId(decisionPointObj.Text("textId"));
                        var DecPUpdateBson = new BsonDocument();
                        if (oldDecisionPointObj != null)
                        {
                             DecPUpdateBson.Add("name",decisionPointObj.Text("name"));
                             DecPUpdateBson.Add("textPId", decisionPointObj.Text("textPId"));
                             var decsionResult= dataOp.Update(oldDecisionPointObj, DecPUpdateBson);
                             pointId = oldDecisionPointObj.Text("pointId");
                        }
                        else
                        {
                            if (!HasExist(decisionPointObj.Text("textId"), decisionPointObj.Text("name")))
                            {
                                var insertResult = dataOp.Insert("XH_DesignManage_DecisionPoint", decisionPointObj);
                               if(insertResult.Status==Status.Successful)
                               {
                                   var addDecisionPointObj=insertResult.BsonInfo;
                                   if(addDecisionPointObj!=null)
                                   {
                                    pointId = addDecisionPointObj.Text("pointId");
                                   }
                               }
                               else
                               {
                               return insertResult;
                               }
                            }
                        }
                        //添加地铁图决策点关联
                        var hitObj = dataOp.FindOneByQueryStr("XH_DesignManage_MapPointRelation",string.Format("mapId={0}&pointId={1}",mapId,pointId));
                        if (hitObj == null &&!string.IsNullOrEmpty(pointId))
                        {
                            var newSubwayMapPoint = new BsonDocument();
                             newSubwayMapPoint.Add("mapId",mapId);
                             newSubwayMapPoint.Add("pointId",pointId);
                             newSubwayMapPoint.Add("type",0);
                             var relationResult= dataOp.Insert("XH_DesignManage_MapPointRelation",newSubwayMapPoint);
                             if (relationResult.Status != Status.Successful)
                             {
                                    return relationResult;
                              }
                        }

                    }
                   
                    tran.Complete();
                }
                catch (Exception ex)
                {
                    result.Status = Status.Failed;
                    result.Message = ex.Message;
                }
            }

            return result;
        }


        #endregion
 
    }

}
