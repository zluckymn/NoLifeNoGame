using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using Yinhe.ProcessingCenter.DataRule;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
namespace Yinhe.ProcessingCenter.BusinessFlow
{
    /// <summary>
    /// 流程步骤岗位人员获取处理类
    /// </summary>
    public class FlowUserHelper
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
        public FlowUserHelper() 
        {
            dataOp = new DataOperation();
        }

        public FlowUserHelper(DataOperation _dataOp)
        {
            dataOp = _dataOp;
        }
        #endregion

        #region 人员获取操作
        /// <summary>
        /// 获取当前步骤设定固定的人员
        /// </summary>
        /// <param name="flowId"></param>
        /// <param name="stepId"></param>
        /// <returns></returns>
        public List<int> GetFixStepUser(int flowId,int stepId)
        {
            var userIdList = new List<int>();
            var  hitQuery=dataOp.FindAllByQueryStr("BusFlowStepUserRel",string.Format("flowId={0}&&stepId={1}",flowId,stepId)).Select(c=>c.Int("userId"));
            ///当有人的时候取人，没人通过项目角色取人，没项目角色取人
            if(hitQuery.Count()>0)
            {
                return hitQuery.ToList();
            }
             return userIdList;
        }

        /// <summary>
        /// 获取每个步骤对应设定固定的人员字典
        /// </summary>
        /// <param name="flowId"></param>
        /// <param name="stepIds"></param>
        /// <returns></returns>
        public Dictionary<int, List<int>> GetFixStepUsersDic(int flowId, IEnumerable<int> stepIds)
        {
            Dictionary<int, List<int>> stepUsersDic = new Dictionary<int, List<int>>();
            stepIds = stepIds.Distinct();
            var stepUserRels = dataOp.FindAllByQuery("BusFlowStepUserRel", Query.And(
                    Query.EQ("flowId", flowId.ToString()),
                    Query.In("stepId",TypeConvert.StringListToBsonValueList(stepIds.Select(c=>c.ToString())))
                )).ToList();
            foreach (int stepId in stepIds)
            {
                var curUserIds = stepUserRels.Where(c => c.String("stepId") == stepId.ToString()).Select(c=>c.Int("userId")).ToList();
                stepUsersDic.Add(stepId, curUserIds);
            }
            return stepUsersDic;
        }

        //public List<int> GetFixStepUser(int flowId, IEnumerable<int> stepIds)
        //{
        //    var userIdList = dataOp.FindAllByQuery("BusFlowStepUserRel", Query.And(
        //        Query.EQ("flowId", flowId.ToString()),
        //        Query.In("stepId", stepIds.Select(c => (BsonValue)c))
        //        )).Select(c => c.Int("userId")).ToList();
        //    return userIdList;
        //}
        /// <summary>
        /// 获取当前步骤设定项目角色人员
        /// </summary>
        /// <param name="flowId"></param>
        /// <param name="stepId"></param>
        /// <returns></returns>
        public List<int> GetStepProjRoleUser(int flowId, int flowInstanceId, int stepId)
        {
            var userIdList = new List<int>();
            var stepObj = dataOp.FindOneByKeyVal("BusFlowStep", "stepId", stepId.ToString());
            var instance = dataOp.FindOneByKeyVal("BusFlowInstance", "flowInstanceId", flowInstanceId.ToString());
            if (instance != null && stepObj != null)
            {
                var  tableName = instance.Text("tableName");
                var referFieldName = instance.Text("referFieldName");
                var  referFieldValue= instance.Text("referFieldValue");
                var flowPosId = stepObj.Text("flowPosId");//流程岗位
                var entityObj = dataOp.FindOneByKeyVal(tableName, referFieldName,referFieldValue);//获取当前实体对象
                if (entityObj!=null)
                {  var projId=entityObj.Text("projId");
                   var hitQuery=string.Format("projId={0}&flowPosId={1}",projId,flowPosId);
                   var projFlowPositionUser = dataOp.FindAllByQueryStr("XH_DesignManage_ProjFlowPositionUser", hitQuery).Select(c=>c.Int("userId"));
                   if (projFlowPositionUser != null && projFlowPositionUser.Count() > 0)
                   {
                       userIdList.AddRange(projFlowPositionUser);
                   }
                 }
            }
            return userIdList;
        }

        /// <summary>
        /// 获取每个步骤对应设定项目角色人员字典
        /// </summary>
        /// <param name="flowId"></param>
        /// <param name="flowInstanceId"></param>
        /// <param name="stepIds"></param>
        /// <returns></returns>
        public Dictionary<int, List<int>> GetStepProjRoleUsersDic(int flowId, int flowInstanceId, IEnumerable<int> stepIds)
        {
            Dictionary<int, List<int>> stepUsersDic = new Dictionary<int, List<int>>();
            stepIds = stepIds.Distinct();
            var steps = dataOp.FindAllByKeyValList("BusFlowStep", "stepId", stepIds.Select(c => c.ToString())).ToList();
            var instance = dataOp.FindOneByKeyVal("BusFlowInstance", "flowInstanceId", flowInstanceId.ToString());
            //先初始化
            foreach (int stepId in stepIds)
                stepUsersDic.Add(stepId, new List<int>());
            if (instance != null && steps.Count > 0)
            {
                var tableName = instance.Text("tableName");
                var referFieldName = instance.Text("referFieldName");
                var referFieldValue = instance.Text("referFieldValue");
                //var flowPosId = stepObj.Text("flowPosId");//流程岗位
                var flowPosIds = steps.Select(c => c.GetValue("flowPosId"));
                var entityObj = dataOp.FindOneByKeyVal(tableName, referFieldName, referFieldValue);//获取当前实体对象
                if (entityObj != null)
                {
                    var projId = entityObj.Text("projId");
                    var allprojFlowPosition = dataOp.FindAllByQuery("XH_DesignManage_ProjFlowPositionUser",
                        Query.And(
                            Query.EQ("projId", projId),
                            Query.In("flowPosId", flowPosIds)
                        )).ToList();  //跟原先比多占点空间；
                    foreach (var step in steps)
                    {
                        var curUserIds = allprojFlowPosition.Where(c => c.String("flowPosId") == step.String("flowPosId")).Select(c => c.Int("userId")).ToList();
                        stepUsersDic[step.Int("stepId")] = curUserIds;
                    }
                }
            }
            return stepUsersDic;
        }
        ///// <summary>
        ///// 获取多个步骤设定项目角色人员
        ///// </summary>
        ///// <param name="flowId"></param>
        ///// <param name="flowInstanceId"></param>
        ///// <param name="stepIds"></param>
        ///// <returns></returns>
        //public List<int> GetStepProjRoleUser(int flowId, int flowInstanceId, IEnumerable<int> stepIds)
        //{
        //    var userIdList = new List<int>();
        //    var steps = dataOp.FindAllByQuery("BusFlowStep", Query.In("stepId", stepIds.Select(c => (BsonValue)c))).ToList();
        //    var instance = dataOp.FindOneByKeyVal("BusFlowInstance", "flowInstanceId", flowInstanceId.ToString());
        //    if (instance != null && steps.Count>0)
        //    {
        //        var tableName = instance.Text("tableName");
        //        var referFieldName = instance.Text("referFieldName");
        //        var referFieldValue = instance.Text("referFieldValue");
        //        //var flowPosId = stepObj.Text("flowPosId");//流程岗位
        //        var flowPosIds = steps.Select(c => c.GetValue("flowPosId"));
        //        var entityObj = dataOp.FindOneByKeyVal(tableName, referFieldName, referFieldValue);//获取当前实体对象
        //        if (entityObj != null)
        //        {
        //            var projId = entityObj.Text("projId");
        //            //var hitQuery = string.Format("projId={0}&flowPosId={1}", projId, flowPosId);
        //            //var projFlowPositionUser = dataOp.FindAllByQueryStr("XH_DesignManage_ProjFlowPositionUser", hitQuery).Select(c => c.Int("userId"));
        //            var projFlowPositionUser = dataOp.FindAllByQuery("XH_DesignManage_ProjFlowPositionUser", 
        //                Query.And(
        //                    Query.EQ("projId", projId),
        //                    Query.In("flowPosId", flowPosIds)
        //                )).Select(c => c.Int("userId"));
        //            if (projFlowPositionUser != null && projFlowPositionUser.Count() > 0)
        //            {
        //                userIdList.AddRange(projFlowPositionUser);
        //            }
        //        }
        //    }
        //    return userIdList;
        //}

        /// <summary>
        /// 获取当前步骤设定项目角色人员
        /// </summary>
        /// <param name="flowId"></param>
        /// <param name="stepId"></param>
        /// <returns></returns>
        public List<int> GetStepProjRoleUserByFlowPostId(int flowId, int flowInstanceId, int flowPosId)
        {
            var userIdList = new List<int>();
          
            var instance = dataOp.FindOneByKeyVal("BusFlowInstance", "flowInstanceId", flowInstanceId.ToString());
            if (instance != null  )
            {
                var tableName = instance.Text("tableName");
                var referFieldName = instance.Text("referFieldName");
                var referFieldValue = instance.Text("referFieldValue");
               
                var entityObj = dataOp.FindOneByKeyVal(tableName, referFieldName, referFieldValue);//获取当前实体对象
                if (entityObj != null)
                {
                    var projId = entityObj.Text("projId");
                    var hitQuery = string.Format("projId={0}&flowPosId={1}", projId, flowPosId);
                    var projFlowPositionUser = dataOp.FindAllByQueryStr("XH_DesignManage_ProjFlowPositionUser", hitQuery).Select(c => c.Int("userId"));
                    if (projFlowPositionUser != null && projFlowPositionUser.Count() > 0)
                    {
                        userIdList.AddRange(projFlowPositionUser);
                    }
                }
            }
            return userIdList;
        }


        /// <summary>
        /// 获取当前步骤设定项目角色人员
        /// </summary>
        /// <param name="flowId"></param>
        /// <param name="stepId"></param>
        /// <returns></returns>
        public List<int> GetStepProjRoleUser(int flowId, int stepId, string tableName, string referFieldName, string referFieldValue)
        {
            var userIdList = new List<int>();
            var stepObj = dataOp.FindOneByKeyVal("BusFlowStep", "stepId", stepId.ToString());
            if (stepObj != null)
            {
                var flowPosId = stepObj.Text("flowPosId");//流程岗位
                var entityObj = dataOp.FindOneByKeyVal(tableName, referFieldName, referFieldValue);//获取当前实体对象
                if (entityObj != null)
                {
                    var projId = entityObj.Text("projId");
                    var hitQuery = string.Format("projId={0}&flowPosId={1}", projId, flowPosId);
                    var projFlowPositionUser = dataOp.FindAllByQueryStr("XH_DesignManage_ProjFlowPositionUser", hitQuery).Select(c => c.Int("userId"));
                    if (projFlowPositionUser != null && projFlowPositionUser.Count() > 0)
                    {
                        userIdList.AddRange(projFlowPositionUser);
                    }
                }
            }
            return userIdList;
        }

        public Dictionary<int,List<int>> GetStepProjRoleUsersDic(int flowId,IEnumerable<int> stepIds, string tableName, string referFieldName, string referFieldValue)
        {
            Dictionary<int, List<int>> stepUsersDic = new Dictionary<int, List<int>>();
            stepIds = stepIds.Distinct();
            foreach (int stepId in stepIds)
                stepUsersDic.Add(stepId, new List<int>());
            var steps = dataOp.FindAllByKeyValList("BusFlowStep","stepId", stepIds.Select(c => c.ToString())).ToList();
            if (steps.Count>0)
            {
                var entityObj = dataOp.FindOneByKeyVal(tableName, referFieldName, referFieldValue);//获取当前实体对象
                if (entityObj != null)
                {
                    var projFlowPositions = dataOp.FindAllByQuery("XH_DesignManage_ProjFlowPositionUser", Query.And(
                            Query.EQ("projId", entityObj.Text("projId")),
                            Query.In("flowPosId", steps.Select(c => c.GetValue("flowPosId")))
                        )).ToList();
                    foreach (var step in steps)
                    {
                        var curUserIds = projFlowPositions.Where(c => c.String("flowPosId") == step.String("flowPosId")).Select(c => c.Int("userId")).Distinct().ToList();
                        stepUsersDic[step.Int("stepId")]= curUserIds;
                    }
                }
            }
            return stepUsersDic;
        } 

        /// <summary>
        /// 获取当前岗位下人员
        /// </summary>
        /// <param name="flowId"></param>
        /// <param name="flowInstanceId"></param>
        /// <param name="stepId"></param>
        /// <returns></returns>
        public List<int> GetStepPostUser(int flowId, int stepId)
        {
            var userIdList = new List<int>();
            var stepObj = dataOp.FindOneByKeyVal("BusFlowStep", "stepId", stepId.ToString());
            if (stepObj != null && !string.IsNullOrEmpty(stepObj.Text("postId")))
            {
                var hitResult = dataOp.FindAllByQueryStr("UserOrgPost", string.Format("postId={0}", stepObj.Text("postId"))).Select(c=>c.Int("userId"));
                userIdList.AddRange(hitResult);
            }
            return userIdList;
        }
        #endregion

        /// <summary>
        /// 获取每个步骤对应岗位下人员字典
        /// </summary>
        /// <param name="flowId"></param>
        /// <param name="stepIds"></param>
        /// <returns></returns>
        public Dictionary<int, List<int>> GetStepPostUsersDic(int flowId, IEnumerable<int> stepIds)
        {
            Dictionary<int, List<int>> stepUsersDic = new Dictionary<int, List<int>>();
            stepIds = stepIds.Distinct();
            foreach (int stepId in stepIds)
                stepUsersDic.Add(stepId, new List<int>());
            var steps = dataOp.FindAllByKeyValList("BusFlowStep", "stepId", stepIds.Select(c => c.ToString())).ToList();
            var postIds = steps.Where(c => !string.IsNullOrEmpty(c.String("postId"))).Select(c => c.GetValue("postId")).Distinct();
            var hitResult = dataOp.FindAllByQuery("UserOrgPost", Query.In("postId",postIds)).ToList();
            foreach (var step in steps)
            {
                var curUsers = hitResult.Where(c => c.String("postId") == step.String("postId")).Select(c => c.Int("userId")).ToList();
                stepUsersDic[step.Int("stepId")]= curUsers;
            }
            return stepUsersDic;
        }

        ///// <summary>
        ///// 获取当前岗位下人员
        ///// </summary>
        ///// <param name="flowId"></param>
        ///// <param name="flowInstanceId"></param>
        ///// <param name="stepId"></param>
        ///// <returns></returns>
        //public List<int> GetStepPostUser(int flowId, IEnumerable<int> stepIds)
        //{
        //    var userIdList = new List<int>();
        //    //var stepObj = dataOp.FindOneByKeyVal("BusFlowStep", "stepId", stepId.ToString());
        //    var steps = dataOp.FindAllByQuery("BusFlowStep", Query.In("stepId", stepIds.Select(c => (BsonValue)c))).ToList();
        //    steps.RemoveAll(c => string.IsNullOrEmpty(c.String("postId")));
        //    if (steps.Count>0)
        //    {
        //        var hitResult = dataOp.FindAllByQuery("UserOrgPost", Query.In("post", steps.Select(c => c.GetValue("postId")))).Select(c => c.Int("userId"));
        //        userIdList.AddRange(hitResult);
        //    }
        //    return userIdList;
        //}
    }

}
