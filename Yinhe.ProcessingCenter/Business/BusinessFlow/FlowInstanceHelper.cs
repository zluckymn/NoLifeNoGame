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
    /// 流程实例相关处理类
    /// </summary>
    public class FlowInstanceHelper
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
        public FlowInstanceHelper() 
        {
            dataOp = new DataOperation();
        }

        public  FlowInstanceHelper(DataOperation _dataOp)
        {
            dataOp = _dataOp;
        }
        #endregion
        #region 查询
        /// <summary>
        /// 是否存在相同的流程实例
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="referFieldName"></param>
        /// <param name="referFieldValue"></param>
        /// <param name="flowId"></param>
        /// <returns></returns>
        public bool ExistedActvedInstance(string tableName, string referFieldName, string referFieldValue, string flowId)
        {
           var queryStr = string.Format("tableName={0}&referFieldName={1}&referFieldValue={2}&flowId={3}", tableName, referFieldName, referFieldValue, flowId);
           var query = dataOp.FindAllByQueryStr(tableName,queryStr);
           var result = query.Where(c => c.Text("instanceStatus") == "0");
           if (result.Count() > 0)
           {
               return true;

           }
           else { return false; }
        }

        /// <summary>
        /// 判断当前流程实例当前人是否可以操作
        /// </summary>
        /// <param name="flowInstanceId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public bool CanExecute(string flowId, string flowInstanceId, int userId)
        {
            var hasOperateRight = false;
            var flowObj = dataOp.FindOneByKeyVal("BusFlow", "flowId", flowId);
            var curFlowInstance = dataOp.FindOneByKeyVal("BusFlowInstance", "flowInstanceId", flowInstanceId);
            var curStep = curFlowInstance.SourceBson("stepId");
            var stepList = flowObj.ChildBsonList("BusFlowStep").OrderBy(c => c.Int("stepOrder")).ToList();
            var turnRightList = dataOp.FindAllByQueryStr("BusinessFlowTurnRight", string.Format("flowInstanceId={0}&status={1}", flowInstanceId, 0)).ToList();
            var curUserId = userId;
            if (curFlowInstance != null)
            {

                if (curStep == null)
                {
                    curStep = new BsonDocument();
                }
                var query1 = MongoDB.Driver.Builders.Query.EQ("flowId", flowObj.Text("flowId"));
                var query2 = MongoDB.Driver.Builders.Query.EQ("flowInstanceId", curFlowInstance.Text("flowInstanceId"));
                //设置为用户为当前用户
                var query4 = MongoDB.Driver.Builders.Query.EQ("userId", curUserId.ToString());
                //获取会签相关岗位步骤
                if (curStep.Int("actTypeId") == (int)FlowActionType.Countersign)
                {
                    var assoicateStepIds = stepList.Where(c => c.Int("stepOrder") == curStep.Int("stepOrder")).Select(c => c.Text("stepId"));
                    // 获取当前实例权限

                    var query3 = MongoDB.Driver.Builders.Query.In("stepId", TypeConvert.StringListToBsonValueList(assoicateStepIds.ToList()));
                    var query5 = MongoDB.Driver.Builders.Query.In("preStepId", TypeConvert.StringListToBsonValueList(assoicateStepIds.ToList()));//用与过滤已经执行过的
                    var query6 = MongoDB.Driver.Builders.Query.EQ("actionAvaiable", "1");//用户过滤已经执行过的
                    var hitExecStepIds = dataOp.FindAllByQuery("BusFlowTrace", MongoDB.Driver.Builders.Query.And(query2, query5, query6)).Select(c => c.Text("preStepId")).ToList();
                    var query7 = MongoDB.Driver.Builders.Query.NotIn("stepId", TypeConvert.StringListToBsonValueList(hitExecStepIds));//用与过滤已经执行过的

                    //获取可执行的用户列表,可能没权限
                    var hitInstanceActionUser = dataOp.FindAllByQuery("InstanceActionUser", MongoDB.Driver.Builders.Query.And(query1, query2, query3, query7)).Where(c=>c.Int("status")==0);


                    //获取替换过的当前可用人员，
                    var hitInstanceActionUserIds = TurnRightUserList(hitInstanceActionUser.Select(c => c.Int("userId")).ToList(), curFlowInstance.Int("flowInstanceId"));
                    hasOperateRight = hitInstanceActionUserIds.Contains(curUserId);//当前人有权限，或者有可能是别人转办给当前人，导致当前人有权限
                    #region 加入转办判断条件
                    if (hitInstanceActionUserIds != null)//需要获取赋予我当前权限的人，或者是当前人所在的步骤，因为会签中要把当前执行的步骤actionAvaiable设置为1
                    {
                        var orginalAvaiableUserIds = new List<int>();//原先有权限的人
                        var granterUserQuery = turnRightList.Where(c => c.Int("givenUserId") == curUserId);//
                        orginalAvaiableUserIds.Add(curUserId);//没有转办而有权限
                        if (turnRightList.Count() > 0)////有人转办给当前用户，或者当前用户有转办给其他人,  此处本人怎么办, 比如当A，B两个用户，A又转办给B，导致两个会签人都一样
                        {

                            orginalAvaiableUserIds.AddRange(granterUserQuery.Select(c => c.Int("orginalUserId")).ToList());
                            if (turnRightList.Where(c => c.Int("orginalUserId") == curUserId&& c.Int("givenUserId") != curUserId).Count() > 0)//当前人有转办给别人
                            {
                                orginalAvaiableUserIds = orginalAvaiableUserIds.Where(c => c != curUserId).ToList();//去除当前用户
                            }
                        }
                        var curInstanceActionUserObj = hitInstanceActionUser.Where(c => orginalAvaiableUserIds.Contains(c.Int("userId"))).FirstOrDefault();
                        if (curInstanceActionUserObj != null)
                        {

                            curStep = curInstanceActionUserObj.SourceBson("stepId");
                            if (curStep == null)
                            {
                                curStep = new BsonDocument();
                            }
                        }
                        else
                        {
                            hasOperateRight = false;
                        }
                    }
                    #endregion

                }
                else
                {
                    var query3 = MongoDB.Driver.Builders.Query.EQ("stepId", curStep.Text("stepId"));

                    var hitInstanceActionUser = dataOp.FindAllByQuery("InstanceActionUser", MongoDB.Driver.Builders.Query.And(query1, query2, query3, query4)).Where(c=>c.Int("status")==0);
                    if (hitInstanceActionUser.Count() > 0)
                    {
                        hasOperateRight = true;
                    }
                    //当有流程实例判断是否有权限，获取当前步骤真正可执行的人
                    var curAvaibleUserIds = GetFlowInstanceAvaiableStepUser(flowObj.Int("flowId"), curFlowInstance.Int("flowInstanceId"), curStep.Int("stepId"));
                    hasOperateRight = curAvaibleUserIds.Contains(userId);
                   }
              }
            return hasOperateRight;
        }

        /// <summary>
        /// 初始化流程执行情况，用户与流程实例详细页面展示
        /// </summary>
        /// <param name="flowId"></param>
        /// <param name="flowInstanceId"></param>
        /// <param name="userId"></param>
        /// <param name="hasOperateRight"></param>
        /// <param name="curAvaiableUserName"></param>
        /// <param name="curStep"></param>
        /// <returns></returns>
        public BsonDocument InitialExecuteCondition(string flowId, string flowInstanceId, int userId, ref bool hasOperateRight, ref string curAvaiableUserName)
        {
            var curUserId = userId;
            var flowObj = dataOp.FindOneByKeyVal("BusFlow", "flowId", flowId);
            var curFlowInstance = dataOp.FindOneByKeyVal("BusFlowInstance", "flowInstanceId", flowInstanceId);
            var  curStep = curFlowInstance.SourceBson("stepId");
            var stepList = flowObj.ChildBsonList("BusFlowStep").OrderBy(c => c.Int("stepOrder")).ToList();
            var turnRightList = dataOp.FindAllByQueryStr("BusinessFlowTurnRight", string.Format("flowInstanceId={0}&status={1}", flowInstanceId, 0)).ToList();
            if (curFlowInstance != null)
            {

                if (curStep == null)
                {
                    curStep = new BsonDocument();
                }
                var query1 = MongoDB.Driver.Builders.Query.EQ("flowId", flowObj.Text("flowId"));
                var query2 = MongoDB.Driver.Builders.Query.EQ("flowInstanceId", curFlowInstance.Text("flowInstanceId"));
                //设置为用户为当前用户
                var query4 = MongoDB.Driver.Builders.Query.EQ("userId", curUserId.ToString());
                //获取会签相关岗位步骤
                if (curStep.Int("actTypeId") == (int)FlowActionType.Countersign)
                {
                    var assoicateStepIds = stepList.Where(c => c.Int("stepOrder") == curStep.Int("stepOrder")).Select(c => c.Text("stepId"));
                    // 获取当前实例权限

                    var query3 = MongoDB.Driver.Builders.Query.In("stepId", TypeConvert.StringListToBsonValueList(assoicateStepIds.ToList()));
                    var query5 = MongoDB.Driver.Builders.Query.In("preStepId", TypeConvert.StringListToBsonValueList(assoicateStepIds.ToList()));//用与过滤已经执行过的
                    var query6 = MongoDB.Driver.Builders.Query.EQ("actionAvaiable", "1");//用户过滤已经执行过的
                    var hitExecStepIds = dataOp.FindAllByQuery("BusFlowTrace", MongoDB.Driver.Builders.Query.And(query2, query5, query6)).Select(c => c.Text("preStepId")).ToList();
                    var query7 = MongoDB.Driver.Builders.Query.NotIn("stepId", TypeConvert.StringListToBsonValueList(hitExecStepIds));//用与过滤已经执行过的

                    //获取可执行的用户列表,可能没权限
                    var hitInstanceActionUser = dataOp.FindAllByQuery("InstanceActionUser", MongoDB.Driver.Builders.Query.And(query1, query2, query3, query7)).Where(c=>c.Int("status")==0);
                    var tempInstanceActionUser1 = hitInstanceActionUser.FirstOrDefault();
                    if (tempInstanceActionUser1 != null)
                    {

                        var hitCurUserId = TurnRightUserList(tempInstanceActionUser1.Int("userId"), curFlowInstance.Int("flowInstanceId"));
                        if (hitCurUserId != null)
                        {
                            var curAvaiableUser = dataOp.FindOneByKeyVal("SysUser", "userId", hitCurUserId.ToString());
                            if (curAvaiableUser != null)
                            {
                                curAvaiableUserName = curAvaiableUser.Text("name");
                            }
                        }
                        // curAvaiableUserName = tempInstanceActionUser1.SourceBsonField("userId", "name");
                    }

                    //获取替换过的当前可用人员，
                    var hitInstanceActionUserIds = TurnRightUserList(hitInstanceActionUser.Select(c => c.Int("userId")).ToList(), curFlowInstance.Int("flowInstanceId"));
                    hasOperateRight = hitInstanceActionUserIds.Contains(curUserId);//当前人有权限，或者有可能是别人转办给当前人，导致当前人有权限
                    #region 加入转办判断条件
                    if (hitInstanceActionUserIds != null)//需要获取赋予我当前权限的人，或者是当前人所在的步骤，因为会签中要把当前执行的步骤actionAvaiable设置为1
                    {
                        var orginalAvaiableUserIds = new List<int>();//原先有权限的人
                        var granterUserQuery = turnRightList.Where(c => c.Int("givenUserId") == curUserId);//
                        orginalAvaiableUserIds.Add(curUserId);//没有转办而有权限
                        if (turnRightList.Count() > 0)////有人转办给当前用户，或者当前用户有转办给其他人,  此处本人怎么办, 比如当A，B两个用户，A又转办给B，导致两个会签人都一样
                        {

                            orginalAvaiableUserIds.AddRange(granterUserQuery.Select(c => c.Int("orginalUserId")).ToList());
                            if (turnRightList.Where(c => c.Int("orginalUserId") == curUserId&& c.Int("givenUserId") != curUserId).Count() > 0)//当前人有转办给别人
                            {
                                orginalAvaiableUserIds = orginalAvaiableUserIds.Where(c => c != curUserId).ToList();//去除当前用户
                            }
                        }
                        var curInstanceActionUserObj = hitInstanceActionUser.Where(c => orginalAvaiableUserIds.Contains(c.Int("userId"))).FirstOrDefault();
                        if (curInstanceActionUserObj != null)
                        {

                            curStep = curInstanceActionUserObj.SourceBson("stepId");
                            if (curStep == null)
                            {
                                curStep = new BsonDocument();
                            }
                        }
                        else
                        {
                            hasOperateRight = false;
                        }
                    }
                    #endregion

                }
                else
                {
                    var query3 = MongoDB.Driver.Builders.Query.EQ("stepId", curStep.Text("stepId"));
                    //获取可执行的用户列表
                    var tempInstanceActionUser2 = dataOp.FindAllByQuery("InstanceActionUser", MongoDB.Driver.Builders.Query.And(query1, query2, query3)).Where(c=>c.Int("status")==0).FirstOrDefault();
                    if (tempInstanceActionUser2 != null)
                    {
                        curAvaiableUserName = tempInstanceActionUser2.SourceBsonField("userId", "name");
                    }
                    var hitInstanceActionUser = dataOp.FindAllByQuery("InstanceActionUser", MongoDB.Driver.Builders.Query.And(query1, query2, query3, query4)).Where(c=>c.Int("status")==0);
                    if (hitInstanceActionUser.Count() > 0)
                    {
                        hasOperateRight = true;
                    }
                    //当有流程实例判断是否有权限，获取当前步骤真正可执行的人
                    var curAvaibleUserIds = GetFlowInstanceAvaiableStepUser(flowObj.Int("flowId"), curFlowInstance.Int("flowInstanceId"), curStep.Int("stepId"));
                    hasOperateRight = curAvaibleUserIds.Contains(userId);
                    if (curAvaibleUserIds.Count() > 0)
                    {
                        var curAvaiableUser = dataOp.FindOneByKeyVal("SysUser", "userId", curAvaibleUserIds.FirstOrDefault().ToString());
                        if (curAvaiableUser != null)
                        {
                            curAvaiableUserName = curAvaiableUser.Text("name");
                        }
                    }
                }
            }
            return curStep;
        }

        /// <summary>
        /// 初始化流程执行情况，用户与流程实例详细页面展示，新增侨鑫转办功能，被转办人不能进行成功跳往下步操作
        /// </summary>
        /// <param name="flowId">流程Id</param>
        /// <param name="flowInstanceId"></param>
        /// <param name="userId"></param>
        /// <param name="hasEditRight">是否有编辑的权限</param>
        /// <param name="hasOperateRight">是否有所有的权限</param>
        /// <param name="hasOperateRight">是否可以强制结束会签</param>
        /// <param name="curAvaiableUserName"></param>
        /// <returns></returns>
        public BsonDocument InitialExecuteCondition(string flowId, string flowInstanceId, int userId, ref bool hasOperateRight, ref bool hasEditRight,ref bool canForceComplete, ref string curAvaiableUserName)
        {
            var curUserId = userId;
            var flowObj = dataOp.FindOneByKeyVal("BusFlow", "flowId", flowId);
            var curFlowInstance = dataOp.FindOneByKeyVal("BusFlowInstance", "flowInstanceId", flowInstanceId);
            var curStep = curFlowInstance.SourceBson("stepId");
            var stepList = flowObj.ChildBsonList("BusFlowStep").OrderBy(c => c.Int("stepOrder")).ToList();
            var turnRightList = dataOp.FindAllByQueryStr("BusinessFlowTurnRight", string.Format("flowInstanceId={0}&status={1}", flowInstanceId, 0)).ToList();
            if (curFlowInstance != null)
            {

                if (curStep == null)
                {
                    curStep = new BsonDocument();
                }
                var query1 = MongoDB.Driver.Builders.Query.EQ("flowId", flowObj.Text("flowId"));
                var query2 = MongoDB.Driver.Builders.Query.EQ("flowInstanceId", curFlowInstance.Text("flowInstanceId"));
                //设置为用户为当前用户
                var query4 = MongoDB.Driver.Builders.Query.EQ("userId", curUserId.ToString());
                //获取会签相关岗位步骤
                if (curStep.Int("actTypeId") == (int)FlowActionType.Countersign)
                {
                    var assoicateStepIds = stepList.Where(c => c.Int("stepOrder") == curStep.Int("stepOrder")).Select(c => c.Text("stepId"));
                    // 获取当前实例权限
                    //获取上个步骤
                   
                      var curRuery1 = MongoDB.Driver.Builders.Query.EQ("nextStepId", curFlowInstance.Text("stepId"));
                      var curRuery2 = MongoDB.Driver.Builders.Query.In("traceType", "2");//用与过滤已经执行过的
                      var hitObj = dataOp.FindAllByQuery("BusFlowTrace", MongoDB.Driver.Builders.Query.And(curRuery1, curRuery2)).OrderByDescending(c => c.Date("createDate")).FirstOrDefault();
                      if (hitObj != null && hitObj.Int("createUserId") == userId)
                      {
                          canForceComplete = true;
                      }
                 
                    var query3 = MongoDB.Driver.Builders.Query.In("stepId", TypeConvert.StringListToBsonValueList(assoicateStepIds.ToList()));
                    var query5 = MongoDB.Driver.Builders.Query.In("preStepId", TypeConvert.StringListToBsonValueList(assoicateStepIds.ToList()));//用与过滤已经执行过的
                    var query6 = MongoDB.Driver.Builders.Query.EQ("actionAvaiable", "1");//用户过滤已经执行过的
                    var hitExecStepIds = dataOp.FindAllByQuery("BusFlowTrace", MongoDB.Driver.Builders.Query.And(query2, query5, query6)).Select(c => c.Text("preStepId")).ToList();
                    var query7 = MongoDB.Driver.Builders.Query.NotIn("stepId", TypeConvert.StringListToBsonValueList(hitExecStepIds));//用与过滤已经执行过的

                    //获取可执行的用户列表,可能没权限
                    var hitInstanceActionUser = dataOp.FindAllByQuery("InstanceActionUser", MongoDB.Driver.Builders.Query.And(query1, query2, query3, query7)).Where(c => c.Int("status") == 0);
                    //var tempInstanceActionUser1 = hitInstanceActionUser.FirstOrDefault();
                    //if (tempInstanceActionUser1 != null)
                    //{

                    //    var hitCurUserId = TurnRightUserList(tempInstanceActionUser1.Int("userId"), curFlowInstance.Int("flowInstanceId"));
                    //    if (hitCurUserId != null)
                    //    {
                    //        var curAvaiableUser = dataOp.FindOneByKeyVal("SysUser", "userId", hitCurUserId.ToString());
                    //        if (curAvaiableUser != null)
                    //        {
                    //            curAvaiableUserName = curAvaiableUser.Text("name");
                    //        }
                    //    }
                    //    // curAvaiableUserName = tempInstanceActionUser1.SourceBsonField("userId", "name");
                    //}

                    //获取替换过的当前可用人员，
                    var hitInstanceActionUserIds = TurnRightUserList(hitInstanceActionUser.Select(c => c.Int("userId")).ToList(), curFlowInstance.Int("flowInstanceId"));
                    var userQuery = MongoDB.Driver.Builders.Query.In("userId",hitInstanceActionUserIds.Select(c => (BsonValue)c.ToString()));
                    var avaiableUserList = dataOp.FindAllByQuery("SysUser", userQuery).Select(c => c.Text("name")).ToList();
                    if (avaiableUserList.Count() > 0)
                    {
                        curAvaiableUserName = string.Join(",", avaiableUserList);
                    }

                    hasEditRight = hitInstanceActionUserIds.Contains(curUserId);//当前人有权限，或者有可能是别人转办给当前人，导致当前人有权限
                    #region 加入转办判断条件
                    if (hitInstanceActionUserIds != null)//需要获取赋予我当前权限的人，或者是当前人所在的步骤，因为会签中要把当前执行的步骤actionAvaiable设置为1
                    {
                        var orginalAvaiableUserIds = new List<int>();//原先有权限的人
                        var granterUserQuery = turnRightList.Where(c => c.Int("givenUserId") == curUserId);//
                        orginalAvaiableUserIds.Add(curUserId);//没有转办而有权限
                        if (turnRightList.Count() > 0)////有人转办给当前用户，或者当前用户有转办给其他人,  此处本人怎么办, 比如当A，B两个用户，A又转办给B，导致两个会签人都一样
                        {

                            orginalAvaiableUserIds.AddRange(granterUserQuery.Select(c => c.Int("orginalUserId")).ToList());
                            if (turnRightList.Where(c => c.Int("orginalUserId") == curUserId && c.Int("givenUserId") != curUserId).Count() > 0)//当前人有转办给别人
                            {
                                orginalAvaiableUserIds = orginalAvaiableUserIds.Where(c => c != curUserId).ToList();//去除当前用户
                            }
                        }
                        //此处需考虑比如会签步骤 A,B,C 三人会签 ，A转办给B，此时B进入审批列表的情况，暂时不考虑
                        var curInstanceActionUserObj = hitInstanceActionUser.Where(c => orginalAvaiableUserIds.Contains(c.Int("userId"))).FirstOrDefault();
                        if (curInstanceActionUserObj != null)
                        {

                            curStep = curInstanceActionUserObj.SourceBson("stepId");
                            if (curStep == null)
                            {
                                curStep = new BsonDocument();
                            }
                            hasEditRight = true;
                            if (curInstanceActionUserObj.Int("orginalUserId") == 0 || curInstanceActionUserObj.Int("userId") == curUserId)//有权限并且原始人是自己或者为null（没进行过转办）
                            {
                                hasOperateRight = true;
                            }
                        }
                        else
                        {

                            hasEditRight = false;
                            hasOperateRight = false;
                        }
                    }
                    #endregion

                }
                else
                {
                    var query3 = MongoDB.Driver.Builders.Query.EQ("stepId", curStep.Text("stepId"));
                    //获取可执行的用户列表
                    var tempInstanceActionUser2 = dataOp.FindAllByQuery("InstanceActionUser", MongoDB.Driver.Builders.Query.And(query1, query2, query3)).Where(c => c.Int("status") == 0).FirstOrDefault();
                    if (tempInstanceActionUser2 != null)
                    {
                        curAvaiableUserName = tempInstanceActionUser2.SourceBsonField("userId", "name");
                    }
                    var hitInstanceActionUser = dataOp.FindAllByQuery("InstanceActionUser", MongoDB.Driver.Builders.Query.And(query1, query2, query3, query4)).Where(c => c.Int("status") == 0);
                    if (hitInstanceActionUser.Count() > 0)
                    {
                         hasEditRight = true;
                         if (hitInstanceActionUser.Where(c =>c.Int("orginalUserId")==0||c.Int("orginalUserId") == curUserId).Count()>0)//有权限并且原始人是自己或者为null（没进行过转办）
                         {
                             hasOperateRight = true;
                         }
                    }
                    //当有流程实例判断是否有权限，获取当前步骤真正可执行的人
                    var curAvaibleUserIds = GetFlowInstanceAvaiableStepUser(flowObj.Int("flowId"), curFlowInstance.Int("flowInstanceId"), curStep.Int("stepId"));
                   // hasOperateRight = curAvaibleUserIds.Contains(curUserId);//2013.12.18新增的转办没有了转办表可以不用更判断
                    if (curAvaibleUserIds.Count() > 0)
                    {
                        var curAvaiableUser = dataOp.FindOneByKeyVal("SysUser", "userId", curAvaibleUserIds.FirstOrDefault().ToString());
                        if (curAvaiableUser != null)
                        {
                            curAvaiableUserName = curAvaiableUser.Text("name");
                        }
                    }
                }
            }
            return curStep;
        }
         
        #region 2014.1.26 流程重构钱添加转办按钮新增提交和转办下一步操作
        /// <summary>
        /// 获取当前用户对应流程实例的步骤
        /// </summary>
        /// <param name="flowInstanceId"></param>
        /// <param name="curUserId"></param>
        /// <returns></returns>
        public BsonDocument GetCurOrginalActionUser(string flowInstanceId, int curUserId)
        {
            var curFlowInstance = dataOp.FindOneByKeyVal("BusFlowInstance", "flowInstanceId", flowInstanceId);
            //转办动作初始化
            var allInstanceActionUserList = dataOp.FindAllByKeyVal("InstanceActionUser", "flowInstanceId", curFlowInstance.Text("flowInstanceId")).ToList();
            var curActionUser = allInstanceActionUserList.Where(c => c.Int("stepId") == curFlowInstance.Int("stepId")).FirstOrDefault();
            if (curActionUser != null)
            {
                var curStepOrderStepIds = allInstanceActionUserList.Where(c => c.Text("stepOrder") != "" && c.Int("stepOrder") == curActionUser.Int("stepOrder")).Select(c => c.Int("stepId")).ToList();//获取会签步骤的
                curStepOrderStepIds.Add(curFlowInstance.Int("stepId"));//兼容旧数据
                //真正的当前步骤
                var OrginalActionUser = allInstanceActionUserList.Where(c => curStepOrderStepIds.Contains(c.Int("stepId")) && c.Int("userId") == curUserId).FirstOrDefault();//可能会签步骤中有2个部门同一个人
                if (OrginalActionUser != null)
                {
                    return OrginalActionUser;
                }
            }
            return null;
        
        }

        /// <summary>
        /// 获取转办操作上步骤的操作记录
        /// </summary>
        /// <param name="flowInstanceId"></param>
        /// <param name="curUserId"></param>
        /// <returns></returns>
        public BsonDocument GetLastUserGrant(string flowInstanceId, int curUserId )
        {

            var OrginalActionUser = GetCurOrginalActionUser(flowInstanceId, curUserId);
            if (OrginalActionUser != null)
            {
                Yinhe.ProcessingCenter.BusinessFlow.BusinessFlowUserGrantBll userGrantBll = Yinhe.ProcessingCenter.BusinessFlow.BusinessFlowUserGrantBll._();
                var lastUserGrant = userGrantBll.LastUserGrant(flowInstanceId, OrginalActionUser.Text("stepId"), curUserId);
                return lastUserGrant;
            }
            return null;
        }

        /// <summary>
        /// 获取流程步骤转办情况
        /// </summary>
        /// <returns></returns>
        public BsonDocument InitialUserGrantCondition(string flowInstanceId, int stepId, int curUserId, ref bool isOrginalUser, ref int submitUserId)
        {
          
            var allInstanceActionUserList = dataOp.FindAllByKeyVal("InstanceActionUser", "flowInstanceId", flowInstanceId).ToList();
            var curActionUser = allInstanceActionUserList.Where(c => c.Int("stepId") == stepId).FirstOrDefault();
            if (curActionUser != null)
            {
                var curStepOrderStepIds = allInstanceActionUserList.Where(c => c.Text("stepOrder") != "" && c.Int("stepOrder") == curActionUser.Int("stepOrder")).Select(c => c.Int("stepId")).ToList();//获取会签步骤的
                curStepOrderStepIds.Add(stepId);//兼容旧数据

                var query2 = MongoDB.Driver.Builders.Query.EQ("flowInstanceId", flowInstanceId);
                var query5 = MongoDB.Driver.Builders.Query.In("preStepId", TypeConvert.StringListToBsonValueList(curStepOrderStepIds.Select(c=>c.ToString()).ToList()));//用与过滤已经执行过的
                var query6 = MongoDB.Driver.Builders.Query.EQ("actionAvaiable", "1");//用户过滤已经执行过的
                var hitExecStepIds = dataOp.FindAllByQuery("BusFlowTrace", MongoDB.Driver.Builders.Query.And(query2, query5, query6)).Select(c => c.Int("preStepId")).ToList();

                //真正的当前步骤
                var OrginalActionUser = allInstanceActionUserList.Where(c => curStepOrderStepIds.Contains(c.Int("stepId"))&&!hitExecStepIds.Contains(c.Int("stepId")) && c.Int("userId") == curUserId).FirstOrDefault();//可能会签步骤中有2个部门同一个人
                if (OrginalActionUser != null)
                {
                        isOrginalUser = OrginalActionUser.Int("orginalUserId") == 0 || OrginalActionUser.Int("orginalUserId") == curUserId;
                     
                        Yinhe.ProcessingCenter.BusinessFlow.BusinessFlowUserGrantBll userGrantBll = Yinhe.ProcessingCenter.BusinessFlow.BusinessFlowUserGrantBll._();

                        var firstUserGrant = userGrantBll.FirstUserGrant(flowInstanceId, OrginalActionUser.Text("stepId"), curUserId);
                        if (firstUserGrant != null)
                        {
                            submitUserId = firstUserGrant.Int("grantUserId");
                        }
                        var lastUserGrant = userGrantBll.LastUserGrant(flowInstanceId, OrginalActionUser.Text("stepId"), curUserId);
                        if (lastUserGrant != null)
                        {
                          
                            return lastUserGrant;
                        }
                       

       
                }
            }
            return new BsonDocument();

        }
        

        #endregion


        #endregion
        #region 流程启动
        /// <summary>
        /// 创建流程实例
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        public InvokeResult CreateInstance(BsonDocument instance)
        {
            if (instance == null)
            {
                throw new Exception("未实例化对象");
            }
            InvokeResult  result = new InvokeResult () { Status = Status.Failed };
         
            
            #region 判断是否未完成相同的流程实例对象

            if (ExistedActvedInstance(instance.Text("tableName"), instance.Text("referFieldName"), instance.Text("referFieldValue"), instance.Text("flowId")))
            {
                result.Status = Status.Failed;
                result.Message = "该对象已经启动了流程并且还未完成！！";
                return result;
            }
            #endregion

            #region 启动流程
            try
            {
                instance.Add("instanceGuid", System.Guid.NewGuid());
               #region 执行数据操作
                using (TransactionScope tran = new TransactionScope())
                {
                    //记录流程实例
                    InvokeResult resultInstance = dataOp.Insert("BusFlowInstance",instance);
                    //插入是否成功
                    if (resultInstance.Status != Status.Successful)
                    {
                       
                        result.Status = Status.Failed;
                        result.Message = resultInstance.Message;
                        return result;
                    }
                    instance = resultInstance.BsonInfo;

                    //添加第一次实例化跟踪信息
                    BusFlowTraceBll traceBll = BusFlowTraceBll._(dataOp);
                    //创建启动流程跟踪日志
                    traceBll.CreateStartInstanceLog(instance.Int("stepId"), instance.Int("flowInstanceId"), instance.Int("TraceType"));
                    //创建系统自动跳转日志
                    traceBll.CreateSysActionLog(null, instance.Int("stepId"), instance.Int("flowInstanceId"));

                    tran.Complete();

                    result.Status = Status.Successful;
                    result.BsonInfo = resultInstance.BsonInfo;
                }
                #endregion


            }
            catch (Exception ex)
            {
                Yinhoo.Framework.Log.LogWarpper._().PushApplicationException(ex);
            }

            #endregion

            if (result.Status == Status.Failed)
            {
                result.Status = Status.Failed;
                result.Message = "流程启动失败";
                return result;
            }

            return result;

        }

        /// <summary>
        /// 创建流程实例并制定操作人  步骤2|H|1,2,3,4|Y|步骤2|H|1,2,3,4
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        public InvokeResult SaveInstance(BsonDocument instance, string actionUserStr)
        {
            if (instance == null)
            {
                throw new Exception("未实例化对象");
            }
            InvokeResult  result = new InvokeResult { Status = Status.Failed };
          
            #region 判断是否未完成相同的流程实例对象

            if (ExistedActvedInstance(instance.Text("tableName"), instance.Text("referFieldName"), instance.Text("referFieldValue"), instance.Text("flowId")))
            {
                result.Status = Status.Failed;
                result.Message = "该对象已经启动了流程并且还未完成！！";
                return result;
            }
            #endregion

            #region 启动流程
            try
            {
                instance.Add("instanceGuid", System.Guid.NewGuid());

                #region 执行数据操作
                using (TransactionScope tran = new TransactionScope())
                {
                    //记录流程实例
                    InvokeResult resultInstance = dataOp.Insert("BusFlowInstance", instance);
                    //插入是否成功
                    if (resultInstance.Status != Status.Successful)
                    {
                        result.Status = Status.Failed;
                        result.Message = resultInstance.Message;
                        return result;
                    }
                    instance = resultInstance.BsonInfo;
                    //2014.1.14 添加流程实例模板对象，防止流程步骤更改
                    List<BsonDocument> allStepList = dataOp.FindAllByKeyVal("BusFlowStep", "flowId", instance.Text("flowId")).ToList();  //所有步骤
                    dataOp.Delete("InstanceActionUser", Query.EQ("flowInstanceId", instance.Text("flowInstanceId")));
                    if (!String.IsNullOrEmpty(actionUserStr))
                    {
                        List<BsonDocument> actionUserList = new List<BsonDocument>();
                        var arrActionUserStrUserStr = actionUserStr.Split(new string[] { "|H|" }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var userStr in arrActionUserStrUserStr)
                        {
                            var arrUserStr = userStr.Split(new string[] { "|Y|" }, StringSplitOptions.RemoveEmptyEntries);
                            var stepId = Int32.Parse(arrUserStr[0]);
                            var userIds = arrUserStr[1].Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                            var curStepObj = allStepList.Where(c => c.Int("stepId") == stepId).FirstOrDefault();
                            if (curStepObj == null)
                            {
                                continue;
                            }
                            if (userIds.Count() > 0)
                            {
                                foreach (var userId in userIds)
                                {
                                    BsonDocument actionUser = new BsonDocument();
                                    actionUser.Add("flowInstanceId",instance.Text("flowInstanceId"));
                                  
                                    actionUser.Add("actionConditionId",instance.Text("flowInstanceId"));
                                    actionUser.Add("userId",userId);
                                    actionUser.Add("stepId", stepId);
                                    //新增模板属性对象
                                    if (curStepObj != null)
                                    {
                                        //actionUser.Add("flowId", instance.Text("flowId"));
                                        //actionUser.Add("stepOrder", curStepObj.Text("stepOrder"));
                                        //actionUser.Add("flowPosId", curStepObj.Text("flowPosId"));
                                        //actionUser.Add("actTypeId", curStepObj.Text("actTypeId"));
                                        //actionUser.Add("enslavedStepId", curStepObj.Text("enslavedStepId"));
                                        //actionUser.Add("resetCSignStepId", curStepObj.Text("resetCSignStepId"));
                                        //actionUser.Add("completeStepName", curStepObj.Text("completeStepName"));
                                        //actionUser.Add("isFixUser", curStepObj.Text("isFixUser"));
                                        //actionUser.Add("canImComplete", curStepObj.Text("canImComplete"));
                                        //actionUser.Add("ImCompleteName", curStepObj.Text("ImCompleteName"));
                                        //actionUser.Add("refuseStepId", curStepObj.Text("refuseStepId"));
                                        //actionUser.Add("isChecker", curStepObj.Text("isChecker"));
                                        //actionUser.Add("isHideLog", curStepObj.Text("isHideLog"));
                                        CopyFlowStepProperty(actionUser, curStepObj);
                                      
                                    }
                                    actionUserList.Add(actionUser);
                                }
                            }
                        }

                        //添加启动步骤
                        var bootStep = allStepList.Where(c => c.Int("actTypeId") == (int)FlowActionType.Launch).FirstOrDefault();
                        if (bootStep != null && actionUserList.Where(c => c.Int("stepId") == bootStep.Int("stepId")).Count() <= 0)
                        {
                            BsonDocument actionUser = new BsonDocument();
                            actionUser.Add("flowInstanceId", instance.Text("flowInstanceId"));
                            actionUser.Add("stepId", bootStep.Int("stepId"));
                            actionUser.Add("flowId", instance.Text("flowId"));
                            CopyFlowStepProperty(actionUser, bootStep);
                            actionUserList.Add(actionUser);
                        }
                        if (actionUserList.Count > 0)
                        {
                            dataOp.QuickInsert("InstanceActionUser", actionUserList);
                        }
                    }

                    //添加第一次实例化跟踪信息
                    BusFlowTraceBll traceBll = BusFlowTraceBll._(dataOp);
                    //创建启动流程跟踪日志
                    traceBll.CreateStartInstanceLog(instance.Int("stepId"), instance.Int("flowInstanceId"), instance.Int("TraceType"));
                    //创建系统自动跳转日志
                    traceBll.CreateSysActionLog(null, instance.Int("stepId"), instance.Int("flowInstanceId"));

                    tran.Complete();

                    result.Status = Status.Successful;
                    result.BsonInfo = resultInstance.BsonInfo;
                }
                #endregion


            }
            catch (Exception ex)
            {
                Yinhoo.Framework.Log.LogWarpper._().PushApplicationException(ex);
            }

            #endregion

            if (result.Status == Status.Failed)
            {
                result.Status = Status.Failed;
                result.Message = "流程启动失败";
                return result;
            }

            return result;

        }


        /// <summary>
        /// 更新流程实例并制定操作人  步骤2|Y|1,2,3,4|H|步骤2|Y|1,2,3,4，主要用来发起流程并设定流程操作人
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        public InvokeResult UpdateInstance(BsonDocument instance,BsonDocument updateBson, string actionUserStr)
        {
            if (instance == null)
            {
                throw new Exception("未实例化对象");
            }
            InvokeResult result = new InvokeResult { Status = Status.Failed };
         
            #region 启动流程
            try
            {
                

                #region 执行数据操作
                using (TransactionScope tran = new TransactionScope())
                {
                    //记录流程实例
                    InvokeResult resultInstance = dataOp.Update(instance, updateBson);
                    //插入是否成功
                    if (resultInstance.Status != Status.Successful)
                    {
                        result.Status = Status.Failed;
                        result.Message = resultInstance.Message;
                        return result;
                    }
                    instance = resultInstance.BsonInfo;
                    //添加第一次实例化跟踪信息
                    BusFlowTraceBll traceBll = BusFlowTraceBll._(dataOp);
                    //创建启动流程跟踪日志
                    traceBll.CreateStartInstanceLog(instance.Int("stepId"), instance.Int("flowInstanceId"), -1);
                    //创建系统自动跳转日志
                    traceBll.CreateSysActionLog(null, instance.Int("stepId"), instance.Int("flowInstanceId"));

                    //2014.1.14 添加流程实例模板对象，防止流程步骤更改
                    List<BsonDocument> allStepList = dataOp.FindAllByKeyVal("BusFlowStep", "flowId", instance.Text("flowId")).ToList();  //所有步骤
                    dataOp.Delete("InstanceActionUser", Query.EQ("flowInstanceId", instance.Text("flowInstanceId")));
                    if (!String.IsNullOrEmpty(actionUserStr))
                    {
                        List<BsonDocument> actionUserList = new List<BsonDocument>();
                        var arrActionUserStrUserStr = actionUserStr.Split(new string[] { "|H|" }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var userStr in arrActionUserStrUserStr)
                        {
                            var arrUserStr = userStr.Split(new string[] { "|Y|" }, StringSplitOptions.RemoveEmptyEntries);
                            var stepId = Int32.Parse(arrUserStr[0]);
                            var curStepObj = allStepList.Where(c => c.Int("stepId") == stepId).FirstOrDefault();
                            if (curStepObj == null)
                            {
                                continue;
                            }
                            var userArrayIds = arrUserStr[1];
                            var userIds = userArrayIds.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                            if (userIds.Count() > 0)
                            {
                                foreach (var userId in userIds)
                                {
                                    BsonDocument actionUser = new BsonDocument();
                                    actionUser.Add("flowInstanceId", instance.Text("flowInstanceId"));
                                 
                                    actionUser.Add("actionConditionId", instance.Text("flowInstanceId"));
                                    actionUser.Add("userId", userId);
                                    actionUser.Add("stepId", stepId);
                                    //新增模板属性对象
                                    if (curStepObj != null)
                                    {
                                       
                                        actionUser.Add("flowId", instance.Text("flowId"));
                                        //actionUser.Add("stepOrder", curStepObj.Text("stepOrder"));
                                        //actionUser.Add("flowPosId", curStepObj.Text("flowPosId"));
                                        //actionUser.Add("actTypeId", curStepObj.Text("actTypeId"));
                                        //actionUser.Add("enslavedStepId", curStepObj.Text("enslavedStepId"));
                                        //actionUser.Add("resetCSignStepId", curStepObj.Text("resetCSignStepId"));
                                        //actionUser.Add("completeStepName", curStepObj.Text("completeStepName"));
                                        //actionUser.Add("isFixUser", curStepObj.Text("isFixUser"));
                                        //actionUser.Add("canImComplete", curStepObj.Text("canImComplete"));
                                        //actionUser.Add("ImCompleteName", curStepObj.Text("ImCompleteName"));
                                        //actionUser.Add("isChecker", curStepObj.Text("isChecker"));
                                        //actionUser.Add("refuseStepId", curStepObj.Text("refuseStepId"));
                                        //actionUser.Add("isHideLog", curStepObj.Text("isHideLog"));
                                        CopyFlowStepProperty(actionUser, curStepObj);
                                   }
                                    actionUserList.Add(actionUser);
                                }
                            }
                        }
                        //添加启动步骤
                        var bootStep = allStepList.Where(c => c.Int("actTypeId") == (int)FlowActionType.Launch).FirstOrDefault();
                        if (bootStep != null && actionUserList.Where(c => c.Int("stepId") == bootStep.Int("stepId")).Count() <= 0)
                        {
                            BsonDocument actionUser = new BsonDocument();
                            actionUser.Add("flowInstanceId", instance.Text("flowInstanceId"));
                            actionUser.Add("stepId", bootStep.Int("stepId"));
                            actionUser.Add("flowId", instance.Text("flowId"));
                            //actionUser.Add("stepOrder", bootStep.Text("stepOrder"));
                            //actionUser.Add("flowPosId", bootStep.Text("flowPosId"));
                            //actionUser.Add("actTypeId", bootStep.Text("actTypeId"));
                            //actionUser.Add("enslavedStepId", bootStep.Text("enslavedStepId"));
                            //actionUser.Add("resetCSignStepId", bootStep.Text("resetCSignStepId"));
                            //actionUser.Add("completeStepName", bootStep.Text("completeStepName"));
                            //actionUser.Add("isFixUser", bootStep.Text("isFixUser"));
                            //actionUser.Add("canImComplete", bootStep.Text("canImComplete"));
                            //actionUser.Add("ImCompleteName", bootStep.Text("ImCompleteName"));
                            //actionUser.Add("isChecker", bootStep.Text("isChecker"));
                            //actionUser.Add("refuseStepId", bootStep.Text("refuseStepId"));
                            //actionUser.Add("isHideLog", bootStep.Text("isHideLog"));
                            CopyFlowStepProperty(actionUser, bootStep);
                            actionUserList.Add(actionUser);
                        }
                        if (actionUserList.Count > 0)
                        {
                            dataOp.QuickInsert("InstanceActionUser", actionUserList);
                        }
                    }
                    tran.Complete();
                    result.Status = Status.Successful;
                    result.BsonInfo = resultInstance.BsonInfo;
                }
                #endregion


            }
            catch (Exception ex)
            {
                Yinhoo.Framework.Log.LogWarpper._().PushApplicationException(ex);
                result.Status = Status.Failed;
                result.Message = "请联系管理员";
                return result;
            }

            #endregion

            if (result.Status == Status.Failed)
            {
                result.Status = Status.Failed;
                result.Message = "流程启动失败";
                return result;
            }

            return result;

        }

        #endregion
        #region 执行动作
        /// <summary>
        /// 执行流程动作
        /// 1.更新当前流程实例的当前步骤，当前状态
        /// 2.添加跟踪数据
        /// </summary>
        /// <param name="instance">当前流程</param>
        /// <param name="actionId">要执行的动作</param>
        /// <param name="userId">当前用户</param>
        /// <returns></returns>
        public InvokeResult ExecAction(BsonDocument instance, int actionId, int? formId,int? curRealStepId)
        {
            if (instance == null)
                throw new Exception("未实例化对象");
            InvokeResult  Result = new InvokeResult () { Status = Status.Failed };
  
            #region 获取流程实例
            var currentInstance = dataOp.FindOneByKeyVal("BusFlowInstance", "flowInstanceId", instance.Text("flowInstanceId"));
            if (currentInstance == null)
            {
                Result.Status = Status.Failed;
                Result.Message = "该流程实例可能不存在请刷新";
                return Result;
            }
             var curAction=dataOp.FindOneByKeyVal("BusFlowAction","actId",actionId.ToString());   
            if(curAction==null)
            {
                Result.Status = Status.Failed;
                Result.Message = "动作不存在请刷新后重试";
                return Result;
            }

            
            //获取动作执行前步骤
            var preStepId = curRealStepId.HasValue ? curRealStepId.Value : instance.Int("stepId");
            var successStepId = preStepId;
            #endregion
            var stepBll = BusFlowStepBll._(dataOp);
            var instanceUpdate =new BsonDocument();
            //获取下一个步骤
            BsonDocument preStep = dataOp.FindOneByKeyVal("BusFlowStep", "stepId", preStepId.ToString());
            BsonDocument nextStep=new BsonDocument();
            //获取流程当前的用户步骤对象
            var curInstanceActionUserList = dataOp.FindAllByQuery("InstanceActionUser", Query.EQ("flowInstanceId", currentInstance.Text("flowInstanceId"))).ToList();
          
            switch (curAction.Int("type"))
            {
                case 0:
                case 1:
                case 2:
                    //须跳转到对应status！=1 的对应步骤
                    nextStep = stepBll.GetNextStep(instance.Int("flowInstanceId"), instance.Int("flowId"), preStepId);
                     break;
                case 3://普通驳回步骤
                     nextStep = stepBll.GetActionRefuseStep(instance.Int("flowInstanceId"), instance.Int("flowId"), preStepId);
                     break;
                case 4://集团驳回步骤
                     nextStep = stepBll.GetGroupRefuseStep(instance.Int("flowId"), preStepId);
                     break;
                default:
                     nextStep = stepBll.GetNextStep(instance.Int("flowInstanceId"),instance.Int("flowId"), preStepId);
                    break;
            }
            //设置动作执行后，流程实例的步骤，状态
            int skipStepId = nextStep!=null?nextStep.Int("stepId"):0;
            var isComplete = false;
            
            //判断动作执行完要跳转到的步骤
            if (nextStep.Text("stepId") == currentInstance.Text("stepId"))
            {
                Result.Status = Status.Failed;
                Result.Message = "重复提交操作请刷新后重试";
                return Result;
            }
            try
            {
                BusFlowTraceBll traceBll = BusFlowTraceBll._(dataOp);
                using (TransactionScope tran = new TransactionScope())
                {
                    #region 更新流程实例

                    //判断执行动作后的下一个步骤是否还有下一步 或者是否为结束动作此处需要为审批动作，或者会签动作
                    if (nextStep == null || skipStepId == 0 || (preStep != null && preStep.Int("actTypeId") == (int)FlowActionType.Approve && curAction.Int("type") == (int)FlowActionExecType.Approve))
                    {
                        //修改实例状态完成
                        instanceUpdate.Add("instanceStatus", "1");
                        skipStepId = 0;
                        isComplete = true;
                    }
                    #endregion

                    #region 添加流程跟踪日志
                    var flowTrace = new BsonDocument();
                    var remark = string.Empty;
                    flowTrace.Add("preStepId", preStep.Text("stepId"));
                    //此处不能添加重复的step字段
                    flowTrace.Add("nextStepId", nextStep.Text("stepId"));
                    flowTrace.Add("flowInstanceId",currentInstance.Text("flowInstanceId"));
                    flowTrace.Add("traceType","2");
                    flowTrace.Add("actId",actionId);
                    if (formId.HasValue)
                    {
                        flowTrace.Add("formId", formId);
                    }
                    flowTrace.Add("remark", curAction.Text("name"));
                    //判断动作类型
                    //动作类型 0代表发起流程 1 会签确认 2通过 3 驳回
                    switch (curAction.Int("type"))
                    {
                        case (int)FlowActionExecType.Countersign: flowTrace.Add("actionAvaiable", "1");
                            //var query1 = Query.EQ("flowInstanceId", currentInstance.Text("flowInstanceId"));
                            //var query2 = Query.EQ("stepId", preStep.Text("stepId"));
                            //更新模板对象
                            var curInstanceUserObj = curInstanceActionUserList.Where(c => c.Int("stepId") == preStep.Int("stepId")).FirstOrDefault();
                            if (curInstanceUserObj != null)
                            {
                                var updateBson = new BsonDocument().Add("actionAvaiable", "1");
                                dataOp.Update(curInstanceUserObj, updateBson);
                            }
                            break;
                        case (int)FlowActionExecType.Launch:
                            var reSetTraceResult = traceBll.ResetActionUnAvaiable(currentInstance.Int("flowId"), currentInstance.Int("flowInstanceId"));
                             break;
                        case (int)FlowActionExecType.Refuse:
                            #region 更新已经执行动作选项的有效动作为无效动作 设置 actionStatus为0

                            var hitTraceResult = traceBll.ChangeActionUnAvaiable(currentInstance.Int("flowId"), currentInstance.Int("flowInstanceId"), preStepId, nextStep.Int("stepId"));
                            if (nextStep.Int("actTypeId") == (int)FlowActionType.Launch)
                            {
                                instanceUpdate.Add("approvalUserId", "");//驳回发起人要设置为空
                            }
                            else
                            {    //更新侨鑫重置驳回操作
                                if (preStep != null && preStep.Int("refuseStepId") != 0) {

                                    var curRefuseUserObj = curInstanceActionUserList.Where(c => c.Int("stepId") == nextStep.Int("stepId")).FirstOrDefault();
                                    if (curRefuseUserObj != null)
                                    {
                                        var updateBson = new BsonDocument().Add("converseRefuseStepId", preStep.Int("stepId"));
                                        dataOp.Update(curRefuseUserObj, updateBson);
                                    }
                                }
                            }
                            #endregion
                              break;
                        case (int)FlowActionExecType.GroupRefuse://集团驳回步骤
                              #region 更新已经执行动作选项的有效动作为无效动作 设置 actionStatus为0
                              var otherTraceResult = traceBll.ChangeActionUnAvaiable(currentInstance.Int("flowId"), currentInstance.Int("flowInstanceId"), preStepId, nextStep.Int("stepId"));
                              #endregion
                              break;
                        default: break;
                    }
                    traceBll.Insert(flowTrace);
                    #endregion
                    //更新流程实例,判断是否可以跳转到下一步骤 驳回可以任意跳转
                    var canJumpNextStep=traceBll.CanJumpNextStep(currentInstance.Int("flowId"), currentInstance.Int("flowInstanceId"), preStep, curAction.Int("type")) && nextStep != null && !string.IsNullOrEmpty(nextStep.Text("stepId"));
                    if (canJumpNextStep)
                    {
                        instanceUpdate.Add("stepId", nextStep.Text("stepId"));
                    }
                    //else
                    //{
                    //    //获取下一步骤可用的步骤列表
                    //    var avaiableStepIds = traceBll.GetNextAbaiableStepList(currentInstance.Int("flowId"), currentInstance.Int("flowInstanceId"), preStep);
                    //    if (avaiableStepIds.Count() > 0)
                    //    {
                    //        instanceUpdate.Add("stepId", avaiableStepIds.FirstOrDefault());
                    //    }
                    //}
                    
                    Result = dataOp.Update(currentInstance, instanceUpdate);


                    #region 系统自动跳转
                    if (skipStepId == 0)
                    {
                        if (!isComplete)
                        {
                            traceBll.CreateSysActionLog(preStep.Int("stepId"), null, currentInstance.Int("flowInstanceId"));
                        }
                        else {
                            traceBll.CreateCompleteActionLog(preStep.Int("stepId"), currentInstance.Int("flowInstanceId"));
                        
                        }
                    }
                    #endregion

                    #region 执行进入事务
                    DoJumpInTransaction(nextStep, instance);
                    #endregion

                    #region 执行等待事务
                    DoWaitTransaction(preStep, instance);
                    #endregion

                    if (canJumpNextStep)
                    {
                        #region 执行立即执行事务
                        DoImmediatelyTransaction(preStep, instance);
                        #endregion
                    }
                    tran.Complete();
                    successStepId = preStep.Int("stepId");
                    Result.Status = Status.Successful;
                   
                }

            }
            catch (Exception ex)
            {
                Yinhoo.Framework.Log.LogWarpper._().PushApplicationException(ex);
            }
 
            return Result;
        }
 
        /// <summary>
        /// 执行流程动作,自由跳转
        /// 1.更新当前流程实例的当前步骤，当前状态
        /// 2.添加跟踪数据
        /// </summary>
        /// <param name="instance">当前流程</param>
        /// <param name="actionId">要执行的动作</param>
        /// <param name="userId">当前用户</param>
        /// <returns></returns>
        public InvokeResult ExecAction(BsonDocument instance, int actionId, int formId, int? curRealStepId, int jumpStepId)
        {
            if (instance == null)
                throw new Exception("未实例化对象");
            InvokeResult Result = new InvokeResult() { Status = Status.Failed };

            #region 获取流程实例
            var currentInstance = dataOp.FindOneByKeyVal("BusFlowInstance", "flowInstanceId", instance.Text("flowInstanceId"));
            if (currentInstance == null)
            {
                Result.Status = Status.Failed;
                Result.Message = "该流程实例可能不存在请刷新";
                return Result;
            }
            var curAction = dataOp.FindOneByKeyVal("BusFlowAction", "actId", actionId.ToString());
            if (curAction == null)
            {
               curAction=new BsonDocument();
               curAction.Add("type", "5");
               curAction.Add("name", "结束会签");
            }
            //获取动作执行前步骤
            var preStepId = curRealStepId.HasValue ? curRealStepId.Value : instance.Int("stepId");;
            var successStepId = preStepId;
            #endregion
            var stepBll = BusFlowStepBll._(dataOp);
            var instanceUpdate = new BsonDocument();

            //获取流程当前的用户步骤对象
            var curInstanceActionUserList = dataOp.FindAllByQuery("InstanceActionUser", Query.EQ("flowInstanceId", currentInstance.Text("flowInstanceId"))).ToList();
            //获取下一个步骤
            BsonDocument preStep = dataOp.FindOneByKeyVal("BusFlowStep", "stepId", preStepId.ToString());
            BsonDocument nextStep = new BsonDocument();
            if (jumpStepId == 0)
            {
                //if (curAction.Int("type") != 3)
                //{
                //    nextStep = stepBll.GetNextStep(instance.Int("flowId"), preStepId);
                //}
                //else//驳回操作
                //{
                //    nextStep = stepBll.GetStartStep(instance.Int("flowId"));
                //}
                switch (curAction.Int("type"))
                {
                    case (int)FlowActionExecType.Launch:
                    case (int)FlowActionExecType.Countersign:
                    case (int)FlowActionExecType.Approve:
                        nextStep = stepBll.GetNextStep(instance.Int("flowInstanceId"),instance.Int("flowId"), preStepId);
                        break;
                    case (int)FlowActionExecType.Refuse://普通驳回步骤

                        nextStep = stepBll.GetActionRefuseStep(instance.Int("flowInstanceId"), instance.Int("flowId"), preStepId);
                          
                        break;
                    case (int)FlowActionExecType.GroupRefuse://集团驳回步骤
                        nextStep = stepBll.GetGroupRefuseStep(instance.Int("flowId"), preStepId);
                        break;
                    default:
                        nextStep = stepBll.GetNextStep(instance.Int("flowInstanceId"),instance.Int("flowId"), preStepId);
                        break;
                }
            }
            else
            {
                nextStep = stepBll.FindById(jumpStepId.ToString());
            }

            //设置动作执行后，流程实例的步骤，状态
            int skipStepId = nextStep != null ? nextStep.Int("stepId") : 0;


            //动作执行完要跳转到的步骤
            var isComplete = false;
            try
            {
                BusFlowTraceBll traceBll = BusFlowTraceBll._(dataOp);
                using (TransactionScope tran = new TransactionScope())
                {
                    #region 更新流程实例

                    //判断执行动作后的下一个步骤是否还有下一步 或者是否为结束动作此处需要为审批动作，或者会签动作
                    if (nextStep == null || skipStepId == 0 || (preStep != null && preStep.Int("actTypeId") == (int)FlowActionType.Approve && curAction.Int("type") == (int)FlowActionExecType.Approve))
                    {
                        //修改实例状态完成
                        instanceUpdate.Add("instanceStatus", "1");
                        skipStepId = 0;
                        isComplete = true;
                    }
                    #endregion

                    #region 添加流程跟踪日志
                    var flowTrace = new BsonDocument();
                    var remark = string.Empty;
                    flowTrace.Add("preStepId", preStep.Text("stepId"));
                    flowTrace.Add("nextStepId", nextStep.Text("stepId"));
                    flowTrace.Add("flowInstanceId", currentInstance.Text("flowInstanceId"));
                    flowTrace.Add("traceType", "2");
                    flowTrace.Add("actId", actionId);
                    flowTrace.Add("formId", formId);
                    flowTrace.Add("remark", curAction.Text("name"));
                    //判断动作类型
                    //动作类型 0代表发起流程 1 会签确认 2通过 3 驳回， 注意目前驳回到会签步骤可能导致部分会签步骤没有重置actionAvaiable为0，但是使用InstanceActionUser的
                    //actionAvaiable 可解决该问题
                    switch (curAction.Int("type"))
                    {
                        case (int)FlowActionExecType.Countersign: flowTrace.Add("actionAvaiable", "1");
                            //更新模板对象
                            var curInstanceUserObj = curInstanceActionUserList.Where(c => c.Int("stepId") == preStep.Int("stepId")).FirstOrDefault();
                            if (curInstanceUserObj != null)
                            {
                                var updateBson = new BsonDocument().Add("actionAvaiable", "1");
                                dataOp.Update(curInstanceUserObj, updateBson);
                            }
                            break;
                        case (int)FlowActionExecType.Launch:
                            var reSetTraceResult = traceBll.ResetActionUnAvaiable(currentInstance.Int("flowId"), currentInstance.Int("flowInstanceId"));
                            break;
                        case (int)FlowActionExecType.Approve: break;
                        case (int)FlowActionExecType.Refuse:
                            #region 更新已经执行动作选项的有效动作为无效动作 设置 actionStatus为0
                            var hitTraceResult = traceBll.ChangeActionUnAvaiable(currentInstance.Int("flowId"), currentInstance.Int("flowInstanceId"), jumpStepId, currentInstance.Int("stepId"));
                            if (nextStep.Int("actTypeId") == (int)FlowActionType.Launch)
                            {
                                instanceUpdate.Add("approvalUserId", "");//驳回发起人要设置为空
                            }
                            else
                            {    //更新侨鑫重置驳回操作
                                if (preStep != null && preStep.Int("refuseStepId") != 0)
                                {

                                    var curRefuseUserObj = curInstanceActionUserList.Where(c => c.Int("stepId") == nextStep.Int("stepId")).FirstOrDefault();
                                    if (curRefuseUserObj != null)
                                    {
                                        var updateBson = new BsonDocument().Add("converseRefuseStepId", preStep.Int("stepId"));
                                        dataOp.Update(curRefuseUserObj, updateBson);
                                    }
                                }
                            }
                            #endregion
                            break;
                        case (int)FlowActionExecType.GroupRefuse://集团驳回步骤
                            #region 更新已经执行动作选项的有效动作为无效动作 设置 actionStatus为0
                            var otherTraceResult = traceBll.ChangeActionUnAvaiable(currentInstance.Int("flowId"), currentInstance.Int("flowInstanceId"), preStepId, nextStep.Int("stepId"));
                            #endregion
                            break;
                        default:
                            if (nextStep.Int("stepOrder") < preStep.Int("stepOrder"))//类似驳回到前面,流程自动跳转
                            {
                                var defaultTraceResult = traceBll.ChangeActionUnAvaiable(currentInstance.Int("flowId"), currentInstance.Int("flowInstanceId"), jumpStepId, currentInstance.Int("stepId"));
                            }
                            
                            break;
                    }
                    traceBll.Insert(flowTrace);
                    #endregion
                    //更新流程实例,判断是否可以跳转到下一步骤
                    var canJumpNextStep=traceBll.CanJumpNextStep(currentInstance.Int("flowId"), currentInstance.Int("flowInstanceId"), preStep, curAction.Int("type")) && nextStep != null && !string.IsNullOrEmpty(nextStep.Text("stepId"));

                    if (canJumpNextStep)
                    {
                        instanceUpdate.Add("stepId", nextStep.Text("stepId"));
                    }
                    Result = dataOp.Update(currentInstance, instanceUpdate);
                    #region 系统自动跳转
                    if (skipStepId == 0)
                    {
                        if (!isComplete)
                        {
                            traceBll.CreateSysActionLog(preStep.Int("stepId"), null, currentInstance.Int("flowInstanceId"));
                        }
                        else
                        {
                            traceBll.CreateCompleteActionLog(preStep.Int("stepId"), currentInstance.Int("flowInstanceId"));

                        }
                    }
                    #endregion
                    #region 执行进入事务
                    DoJumpInTransaction(nextStep, instance);
                    #endregion
                    #region 执行等待事务
                    DoWaitTransaction(preStep, instance);
                    #endregion
                    if (canJumpNextStep)
                    {
                        #region 执行立即执行事务
                        DoImmediatelyTransaction(preStep, instance);
                        #endregion
                    }
                    tran.Complete();
                    successStepId = preStep.Int("stepId");
                    Result.Status = Status.Successful;

                }

            }
            catch (Exception ex)
            {
                Yinhoo.Framework.Log.LogWarpper._().PushApplicationException(ex);
            }

            return Result;
        }


        /// <summary>
        /// 保存表单并执行动作
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="actionId"></param>
        /// <param name="content"></param>
        /// <param name="jumpStepId"></param>
        /// <returns></returns>
        public InvokeResult SaveFormAndExecAction(BsonDocument instance, int actionId, string content,int? curRealStepId, int? jumpStepId)
        {
              var formBson = new BsonDocument();
              formBson.Add("flowInstanceId",instance.Text("flowInstanceId"));
              formBson.Add("stepId", curRealStepId.HasValue?curRealStepId.Value.ToString():instance.Text("stepId"));
              formBson.Add("actId", actionId);
              formBson.Add("content",content);
              var result=dataOp.Insert("BusFlowFormData",formBson);
                if(result.Status==Status.Successful)
                {
                    var curformBson=result.BsonInfo;
                    if (jumpStepId.HasValue)
                    {
                        return ExecAction(instance, actionId, curformBson.Int("formId"),curRealStepId, jumpStepId.Value);
                    }
                    else
                    {
                        return ExecAction(instance, actionId, curformBson.Int("formId"), curRealStepId);
                    }
                }
            return result;
        }

        /// <summary>
        /// 执行系统事务直接自动跳转动作
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="jumpStepId"></param>
        /// <param name="jumpStepId">是否直接结束</param>
        /// <returns></returns>
        public InvokeResult ExecStepTranSysAction(BsonDocument instance, int jumpStepId)
        {
            if (instance == null)
                throw new Exception("未实例化对象");
            InvokeResult Result = new InvokeResult() { Status = Status.Failed };

            #region 获取流程实例
            var currentInstance = dataOp.FindOneByKeyVal("BusFlowInstance", "flowInstanceId", instance.Text("flowInstanceId"));
            if (currentInstance == null)
            {
                Result.Status = Status.Failed;
                Result.Message = "该流程实例可能不存在请刷新";
                return Result;
            }
            //获取动作执行前步骤
            var preStepId =  instance.Int("stepId"); 
            var successStepId = preStepId;
            #endregion
            var stepBll = BusFlowStepBll._(dataOp);
            var instanceUpdate = new BsonDocument();
            //获取下一个步骤
            BsonDocument preStep = dataOp.FindOneByKeyVal("BusFlowStep", "stepId", preStepId.ToString());
            BsonDocument nextStep = dataOp.FindOneByKeyVal("BusFlowStep", "stepId", jumpStepId.ToString());
            //设置动作执行后，流程实例的步骤，状态
            int skipStepId = nextStep != null ? nextStep.Int("stepId") : 0;


            //动作执行完要跳转到的步骤
            var isComplete = false;
            try
            {
                BusFlowTraceBll traceBll = BusFlowTraceBll._(dataOp);
                using (TransactionScope tran = new TransactionScope())
                {
                    #region 更新流程实例

                    //判断执行动作后的下一个步骤是否还有下一步 或者是否为结束动作此处需要为审批动作，或者会签动作
                    if (jumpStepId==0||nextStep == null || skipStepId == 0 || (preStep != null && preStep.Int("actTypeId") == 4))
                    {
                        //修改实例状态完成
                        instanceUpdate.Add("instanceStatus", "1");
                        skipStepId = 0;
                        isComplete = true;
                    }
                    #endregion

                    #region 添加流程跟踪日志
                   
                    //判断动作类型
                    //动作类型 0代表发起流程 1 会签确认 2通过 3 驳回
                    if (nextStep != null && preStep != null)
                    {
                        if (nextStep.Int("stepOrder") < preStep.Int("stepOrder"))//类似驳回到前面,流程自动跳转
                        {
                            var defaultTraceResult = traceBll.ChangeActionUnAvaiable(currentInstance.Int("flowId"), currentInstance.Int("flowInstanceId"), jumpStepId, currentInstance.Int("stepId"));
                        }
                    }
                    #endregion
                    //更新流程实例,判断是否可以跳转到下一步骤
                    if (nextStep != null && !string.IsNullOrEmpty(nextStep.Text("stepId")))
                    {
                        instanceUpdate.Add("stepId", nextStep.Text("stepId"));
                    }
                    Result = dataOp.Update(currentInstance, instanceUpdate);
                    #region 系统自动跳转
                    if (skipStepId == 0)
                    {
                        if (!isComplete)
                        {
                            traceBll.CreateSysActionLog(preStep.Int("stepId"), null, currentInstance.Int("flowInstanceId"));
                        }
                        else
                        {
                            traceBll.CreateCompleteActionLog(preStep.Int("stepId"), currentInstance.Int("flowInstanceId"));
                        }
                    }
                    #endregion
                    tran.Complete();
                    successStepId = preStep.Int("stepId");
                    Result.Status = Status.Successful;

                }

            }
            catch (Exception ex)
            {
                Yinhoo.Framework.Log.LogWarpper._().PushApplicationException(ex);
            }
            return Result;
           
        }

        #endregion

        #region 人员获取操作

        /// <summary>
        /// 获取当前步骤涉及的人员, 三种情况，一种指定岗位，指定人，2.指定人 3 指定岗位 4.项目角色
        /// 可以有可能返回多个用户列表，可用于启动流程的时候再度筛选人员，没通过转办人员过滤
        /// </summary>
        /// <param name="flowId"></param>
        ///  <param name="flowInstanceId">可传0</param>
        /// <param name="stepId"></param>
        /// <returns></returns>
        public List<int> GetCurStepUser(int flowId, int stepId, string tableName, string referFieldName, string referFieldValue)
        {
            FlowUserHelper flowUserHelper = new FlowUserHelper();
            var userIdList = new List<int>();
            //获取后台设定的项目岗位角色人员
            userIdList = flowUserHelper.GetStepProjRoleUser(flowId, stepId, tableName, referFieldName, referFieldValue);
            if (userIdList.Count() > 0)
            {
                return userIdList;
            }
            //获取后台设定固定人员
            userIdList = flowUserHelper.GetFixStepUser(flowId, stepId);
            if (userIdList.Count() > 0)
            {
                return userIdList;
            }
           
            //获取后台设定的岗位的对应人员
            userIdList = flowUserHelper.GetStepPostUser(flowId, stepId);
            if (userIdList.Count() > 0)
            {
               return  userIdList;
            }

            return userIdList;
        }


        /// <summary>
        /// 获取当前步骤涉及的人员, 三种情况，一种指定岗位，指定人，2.指定人 3 指定岗位 4.项目角色
        /// 可以有可能返回多个用户列表，可用于启动流程的时候再度筛选人员,转办人员过滤
        /// </summary>
        /// <param name="flowId"></param>
        ///  <param name="flowInstanceId">可传0</param>
        /// <param name="stepId"></param>
        /// <returns></returns>
        public List<int> GetCurStepUser(int flowId,int flowInstanceId,int stepId)
        {
            FlowUserHelper flowUserHelper=new FlowUserHelper();
            var userIdList = new List<int>();

            //获取后台设定的项目岗位角色人员
            userIdList = flowUserHelper.GetStepProjRoleUser(flowId, flowInstanceId, stepId);
            if (userIdList.Count() > 0)
            {
                return TurnRightUserList(userIdList.ToList(), flowInstanceId);
            }
            //获取后台设定固定人员
            userIdList = flowUserHelper.GetFixStepUser(flowId,stepId);
            if (userIdList.Count() > 0)
            {
                return TurnRightUserList(userIdList.ToList(),flowInstanceId);
            }
          
            //获取后台设定的岗位的对应人员
            userIdList = flowUserHelper.GetStepPostUser(flowId, stepId);
            if (userIdList.Count() > 0)
            {
                TurnRightUserList(userIdList.ToList(), flowInstanceId);
            }

            return TurnRightUserList(userIdList.ToList(), flowInstanceId);
       }

        ///// <summary>
        ///// 根据多个步骤获取多个人员
        ///// </summary>
        ///// <param name="flowId"></param>
        ///// <param name="flowInstanceId"></param>
        ///// <param name="stepIds"></param>
        ///// <returns></returns>
        //public List<int> GetStepsUserIds(int flowId, int flowInstanceId, IEnumerable<int> stepIds)
        //{
        //    FlowUserHelper flowUserHelper = new FlowUserHelper();
        //    var userIdList = new List<int>();

        //    //获取后台设定的项目岗位角色人员
        //    userIdList = flowUserHelper.GetStepProjRoleUser(flowId, flowInstanceId, stepIds);
        //    if (userIdList.Count() > 0)
        //    {
        //        return TurnRightUserList(userIdList.ToList(), flowInstanceId);
        //    }
        //    //获取后台设定固定人员
        //    userIdList = flowUserHelper.GetFixStepUser(flowId, stepIds);
        //    if (userIdList.Count() > 0)
        //    {
        //        return TurnRightUserList(userIdList.ToList(), flowInstanceId);
        //    }

        //    //获取后台设定的岗位的对应人员
        //    userIdList = flowUserHelper.GetStepPostUser(flowId, stepIds);
        //    if (userIdList.Count() > 0)
        //    {
        //        TurnRightUserList(userIdList.ToList(), flowInstanceId);
        //    }

        //    return TurnRightUserList(userIdList.ToList(), flowInstanceId);
        //}

        /// <summary>
        /// 获取每个步骤对应的人员Id字典
        /// </summary>
        /// <param name="flowId"></param>
        /// <param name="flowInstanceId"></param>
        /// <param name="stepIds"></param>
        /// <returns></returns>
        public Dictionary<int, List<int>> GetStepUserIdsDic(int flowId, int flowInstanceId,IEnumerable<int> stepIds)
        {
            FlowUserHelper flowUserHelper = new FlowUserHelper(); 
            stepIds = stepIds.Distinct();
            Dictionary<int, List<int>> stepUserIdsDic = flowUserHelper.GetStepProjRoleUsersDic(flowId, flowInstanceId, stepIds);
            if (stepUserIdsDic.Values.Any(c => c.Count == 0))
            {
                var stepFixedUsersDic = flowUserHelper.GetFixStepUsersDic(flowId, stepIds);

                foreach (int stepId in stepIds)
                    if (stepUserIdsDic[stepId].Count == 0)
                        stepUserIdsDic[stepId] = stepFixedUsersDic[stepId];
                if (stepUserIdsDic.Values.Any(c => c.Count == 0))
                {
                    var stepPostUsersDic = flowUserHelper.GetStepPostUsersDic(flowId, stepIds);
                    foreach (int stepId in stepIds)
                        if (stepUserIdsDic[stepId].Count == 0)
                            stepUserIdsDic[stepId] = stepPostUsersDic[stepId];
                }
            }
            foreach (int stepId in stepIds)
                stepUserIdsDic[stepId] = TurnRightUserList(stepUserIdsDic[stepId], flowInstanceId);
            return stepUserIdsDic;
        }

        /// <summary>
        /// 进行转办过滤
        /// </summary>
        /// <param name="instanceActionUserId"></param>
        /// <param name="flowInstanceId"></param>
        /// <returns></returns>
        public int TurnRightUserList(int instanceActionUserId, int flowInstanceId)
        {
            var tempList = new List<int>();
            tempList.Add(instanceActionUserId);
            return TurnRightUserList(tempList, flowInstanceId).FirstOrDefault();
        }
        /// <summary>
        /// 进行转办过滤,当前人转办成什么人
        /// </summary>
        /// <param name="instanceActionUser"></param>
        /// <param name="flowInstanceId"></param>
        /// <returns></returns>
        public List<int> TurnRightUserList(List<int> instanceActionUser, int flowInstanceId)
        {
            #region  人员替办替2012.5.14 郑伯锰添加
            var tempList = new List<int>();
            if (instanceActionUser.Count() > 0)
            {
                tempList.AddRange(instanceActionUser.Distinct());
            }
            if (flowInstanceId != 0)
            {
                var trunRightBll = BusinessFlowTurnRightBll._();
                var businessFlowTurnRightQuery = trunRightBll.FindByFlowInstanceId(flowInstanceId).Where(c =>c.Int("status")==0&& tempList.Contains(c.Int("orginalUserId")));
                if (businessFlowTurnRightQuery.Count() > 0)
                {
                    foreach (var turnRightItem in businessFlowTurnRightQuery)
                    {

                        var hasExistObj = tempList.Where(c => c == turnRightItem.Int("givenUserId"));//给了谁
                        var hitObjQuery = tempList.Where(c => c == turnRightItem.Int("orginalUserId"));//原始拥有者人
                        
                        if (hasExistObj.Count() <= 0)//当前人没有在列表中
                        {

                                if (hitObjQuery.Count() > 0)
                                {
                                    var hitObj = hitObjQuery.FirstOrDefault();
                                    tempList.Remove(hitObj);
                                }
                                tempList.Add(turnRightItem.Int("givenUserId"));
                         }//当前人已经在列表中
                        else 
                        {
                            if (turnRightItem.Int("givenUserId") != turnRightItem.Int("orginalUserId"))//防止A->B B->A
                            {
                                if (hitObjQuery.Count() > 0)
                                {
                                    var hitObj = hitObjQuery.FirstOrDefault();
                                    tempList.Remove(hitObj);
                                }
                            }
                         
                        }

                    }
                }
            }
            #endregion
            return tempList;
        }

        /// <summary>
        /// 获取当前步骤真正被允许可执行的一个用户,用于流程图展示
        /// </summary>
        /// <param name="flowId"></param>
        /// <param name="flowInstanceId"></param>
        /// <param name="stepId"></param>
        /// <returns></returns>
        public List<int> GetFlowInstanceAvaiableStepUserForFlowMap(int flowId, int flowInstanceId, int stepId)
        {
            var instanceActionUserQuery = dataOp.FindAllByKeyVal("InstanceActionUser", "flowInstanceId", flowInstanceId.ToString()).ToList();
            var curActionUser = instanceActionUserQuery.Where(c => c.Int("stepId") == stepId).Select(c => c.Int("userId")).ToList();

            return curActionUser;
        }

        /// <summary>
        /// 获取当前步骤真正被允许可执行的一个用户，一般为一个，有可能为多个，当为一个部门多个人审核的时候,
        /// 用于流程启动中判断当前步骤真正可执行的人员,可用于判断是否需要在流程启动的时候过滤人员
        /// </summary>
        /// <param name="flowId"></param>
        ///  <param name="flowInstanceId">可传0</param>
        /// <param name="stepId"></param>
        /// <returns></returns>
        public List<int> GetFlowInstanceAvaiableStepUser(int flowId, int flowInstanceId, int stepId)
        {

            var instanceActionUserQuery = dataOp.FindAllByKeyVal("InstanceActionUser", "flowInstanceId", flowInstanceId.ToString()).ToList();
            var curActionUser = instanceActionUserQuery.Where(c => c.Int("stepId") == stepId).FirstOrDefault();
            if (curActionUser != null)
            {
                var curStepOrderStepIds = instanceActionUserQuery.Where(c => c.Text("stepOrder") != "" && c.Int("stepOrder") == curActionUser.Int("stepOrder")).Select(c => c.Int("stepId")).ToList();//获取会签步骤的
                curStepOrderStepIds.Add(stepId);//兼容旧数据

                var query2 = MongoDB.Driver.Builders.Query.EQ("flowInstanceId", flowInstanceId.ToString());
                var query5 = MongoDB.Driver.Builders.Query.In("preStepId", TypeConvert.StringListToBsonValueList(curStepOrderStepIds.Select(c => c.ToString()).ToList()));//用与过滤已经执行过的
                var query6 = MongoDB.Driver.Builders.Query.EQ("actionAvaiable", "1");//用户过滤已经执行过的
                var hitExecStepIds = dataOp.FindAllByQuery("BusFlowTrace", MongoDB.Driver.Builders.Query.And(query2, query5, query6)).Select(c => c.Int("preStepId")).ToList();

                //真正的当前步骤
                var OrginalActionUser = instanceActionUserQuery.Where(c => curStepOrderStepIds.Contains(c.Int("stepId")) && !hitExecStepIds.Contains(c.Int("stepId"))).ToList();//可能会签步骤中有2个部门同一个人

                if (OrginalActionUser.Count() > 0)//此处条件判断情况 有可能为转办与流程无对应实例情况
                {
                    var instanceActionUser = OrginalActionUser.Where(c => c.Int("status") == 0).Select(c => c.Int("userId"));
                    var hitList = TurnRightUserList(instanceActionUser.ToList(), flowInstanceId);
                    return hitList;
                }
                else
                {
                    return new List<int>();
                }
                
            }
            else
            {
                var userIdList = GetCurStepUser(flowId, flowInstanceId, stepId);
                return userIdList;
            }
        }

        /// <summary>
        /// 获取用户涉及的流程实例列表，一般用于个人工作台
        /// </summary>
        /// <param name="userId">用户Id</param>
        /// <returns></returns>
        public List<BsonDocument> GetUserAssociatedFlowInstance(int userId)
        {
            //获取转办给我的最近新流程,需要过滤我转办给其他人的流程实例
            //var grantUserIds = dataOp.FindAllByQueryStr("BusinessFlowTurnRight", string.Format("givenUserId={0}&status=0", userId)).Select(c => c.Int("orginalUserId")).Distinct().ToList();
            //grantUserIds.Add(userId);
            //var filterFlowInstanceIds = dataOp.FindAllByQueryStr("BusinessFlowTurnRight", string.Format("orginalUserId={0}&status=0", userId)).Where(c => c.Int("givenUserId") != userId).Select(c => c.Int("flowInstanceId")).Distinct().ToList();
            //var flowInstanceQuery=dataOp.FindAllByKeyVal("BusFlowInstance","instanceStatus","0");
            //var flowInstanceIds = flowInstanceQuery.Where(c => !filterFlowInstanceIds.Contains(c.Int("flowInstanceId"))).Select(c => c.Text("flowInstanceId")).ToList();
           
            //var instanceIdList = dataOp.FindAllByKeyValList("InstanceActionUser", "flowInstanceId", flowInstanceIds).Where(c => c.Int("status")==0&&grantUserIds.Contains(c.Int("userId"))).Select(c => c.Int("flowInstanceId")).ToList();
            //var hitFlowInstanceList= flowInstanceQuery.Where(c=>instanceIdList.Contains(c.Int("flowInstanceId"))).ToList();
            //return hitFlowInstanceList;
            return GetUserAssociatedFlowInstance(userId, 0);
        }

        /// <summary>
        /// 取出涉及到我的所有流程  statue==0 进行中的  1 所有的
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public List<BsonDocument> GetUserAssociatedFlowInstance(int userId,int status)
        {
            //获取转办给我的最近新流程,需要过滤我转办给其他人的流程实例
            var grantUserIds = dataOp.FindAllByQueryStr("BusinessFlowTurnRight", string.Format("givenUserId={0}&status=0", userId)).Select(c => c.Int("orginalUserId")).Distinct().ToList();
            grantUserIds.Add(userId);
            var filterFlowInstanceIds = dataOp.FindAllByQueryStr("BusinessFlowTurnRight", string.Format("orginalUserId={0}&status=0", userId)).Where(c => c.Int("givenUserId") != userId).Select(c => c.Int("flowInstanceId")).Distinct().ToList();
            var flowInstanceQuery = dataOp.FindAll("BusFlowInstance").ToList();
            if (status == 0)
            {
                flowInstanceQuery = flowInstanceQuery.Where(t => t.Int("instanceStatus") == 0).ToList();
            }

            var flowInstanceIds = flowInstanceQuery.Where(c => !filterFlowInstanceIds.Contains(c.Int("flowInstanceId"))).Select(c => c.Text("flowInstanceId")).ToList();

            var instanceIdList = dataOp.FindAllByKeyValList("InstanceActionUser", "flowInstanceId", flowInstanceIds).Where(c => c.Int("status") == 0 && grantUserIds.Contains(c.Int("userId"))).Select(c => c.Int("flowInstanceId")).ToList();
            var hitFlowInstanceList = flowInstanceQuery.Where(c => instanceIdList.Contains(c.Int("flowInstanceId"))).ToList();

            
            return hitFlowInstanceList;
        }

        /// <summary>
        /// 获取我审批过后的流程实例
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public List<BsonDocument> GetUserApprovaledFlowInstance(int userId)
        {
            var query1 = Query.EQ("createUserId", userId.ToString());
            // Remark="流程动作类型   -1 --重启流程 0 -- 启动流程1 -- 系统自动执行2 -- 用户执行动作3 -- 回滚4 -- 强制进入下一步骤5-- 废弃流程6 --转办功能7 -- 沟通日志 8传阅功能"
            //获取我涉及的所有流程
            var waitForApprovaleInstanceIds = GetUserWaitForApprovaleFlow(userId).Select(c => c.Int("flowInstanceId")).ToList();
            var flowInstanceIds = dataOp.FindAllByQuery("BusFlowTrace", query1).Where(c => c.Int("traceType") < 6 && !waitForApprovaleInstanceIds.Contains(c.Int("flowInstanceId"))).Select(c => c.Text("flowInstanceId")).Distinct().ToList();

            var finalResult = dataOp.FindAllByKeyValList("BusFlowInstance", "flowInstanceId", flowInstanceIds).ToList();
            return finalResult;

        }

        /// <summary>
        /// 获取轮到当前人审批的流程实例，有实例
        /// </summary>
        /// <param name="userId">用户Id</param>
        /// <returns></returns>
        public List<BsonDocument> GetUserWaitForApprovaleFlow(int userId)
        {
            var businessFlowTurnRight = dataOp.FindAllByQueryStr("BusinessFlowTurnRight", string.Format("givenUserId={0}&status=0", userId)).ToList();
            //获取转办给我的最近新流程
            var grantUserIds = businessFlowTurnRight.Select(c => c.Int("orginalUserId")).Distinct().ToList();
            grantUserIds.Add(userId);
            //转办给别人但最后还没转办给我的
            var filterFlowInstanceIds = dataOp.FindAllByQueryStr("BusinessFlowTurnRight", string.Format("orginalUserId={0}&status=0", userId)).Where(c => c.Int("givenUserId") != userId).Select(c => c.Int("flowInstanceId")).Distinct().ToList();
            var allStepList = dataOp.FindAll("BusFlowStep").ToList();
            var allStepOrderDic = allStepList.ToDictionary(c => c.Int("stepId"), d => d.Int("stepOrder"));
            //获取每个流程实例的当前步骤
            var filterStpList = dataOp.FindAllByKeyVal("BusFlowInstance", "instanceStatus", "0").ToList();
            var filterStepIdList = filterStpList.Select(c => c.Int("stepId")).ToList();
            //获取同会签的步骤
            var counterSignStep = allStepList.Where(c => filterStepIdList.Contains(c.Int("stepId")) && c.Int("actTypeId") == (int)FlowActionType.Countersign).ToList();
            var hitSameCounterSignStepIds = from c in allStepList
                                            join d in counterSignStep on c.Int("flowId") equals d.Int("flowId")
                                            where c.Int("stepOrder") == d.Int("stepOrder") && c.Int("stepId") != d.Int("stepId")
                                            select c.Text("stepId");

            //获取步骤是非发起的步骤
            var launchStepIdStrList = allStepList.Where(c => c.Int("actTypeId") != (int)FlowActionType.Launch && filterStepIdList.Contains(c.Int("stepId"))).Select(c => c.Text("stepId")).ToList();
            launchStepIdStrList.AddRange(hitSameCounterSignStepIds);

            //过滤已执行过的
            var query2 = MongoDB.Driver.Builders.Query.In("flowInstanceId", TypeConvert.StringListToBsonValueList(filterStpList.Select(c => c.Text("flowInstanceId")).ToList()));
            var query5 = MongoDB.Driver.Builders.Query.In("preStepId", TypeConvert.StringListToBsonValueList(launchStepIdStrList));//用与过滤已经执行过的
            var query6 = MongoDB.Driver.Builders.Query.EQ("actionAvaiable", "1");//用户过滤已经执行过的
            var hitExecBusFlowTrace = dataOp.FindAllByQuery("BusFlowTrace", MongoDB.Driver.Builders.Query.And(query2, query5, query6)).ToList();

            //过滤当前用户，获取轮到当前用户审批的流程实例
            var InstanceActionUserList = dataOp.FindAllByKeyValList("InstanceActionUser", "stepId", launchStepIdStrList).Where(c =>  c.Int("status") == 0 && c.Int("userId") == userId).ToList();
            //过滤已经执行过的流程实例与步骤，可能前面执行过，后面又有等待审批的
            var hitExecIds = from c in hitExecBusFlowTrace
                              join d in InstanceActionUserList on c.Int("flowInstanceId") equals d.Int("flowInstanceId")
                              where c.Int("preStepId") == d.Int("stepId")
                              select d.Int("inActId");
            InstanceActionUserList = InstanceActionUserList.Where(c => !hitExecIds.Contains(c.Int("inActId"))).ToList();        

            var hitFlowInstanceIdList = from c in InstanceActionUserList
                                        join e in filterStpList on c.Int("flowInstanceId") equals e.Int("flowInstanceId")
                                        where allStepOrderDic.ContainsKey(c.Int("stepId")) && allStepOrderDic.ContainsKey(e.Int("stepId")) &&
                                              allStepOrderDic[c.Int("stepId")] == allStepOrderDic[e.Int("stepId")]
                                        select e;

            var finalResult = hitFlowInstanceIdList.Distinct().ToList();
            return finalResult;
        }


        /// <summary>
        /// 通用获取轮到当前人审批的流程实例，有实例，2014.1.14更新项目计划模板后最新获取等待用户审批的流程，2014.1.14最新版本发布后更新才可用
        /// </summary>
        /// <param name="userId">用户Id</param>
        /// <returns></returns>
        public List<BsonDocument> GeneralUserWaitForApprovaleFlow(int userId)
        {
         
            //获取每个流程实例的当前步骤
            var filterFlowIstanceList = dataOp.FindAllByKeyVal("BusFlowInstance", "instanceStatus", "0").ToList();
            //获取正在走的流程实例对应步骤人员模板
            var allStepUserList = dataOp.FindAllByKeyValList("InstanceActionUser", "flowInstanceId", filterFlowIstanceList.Select(c=>c.Text("flowInstanceId")).ToList()).ToList();
            var allStepOrderDic = allStepUserList.ToDictionary(c => c.Int("stepId"), d => d.Int("stepOrder"));
            var filterStepIdList = filterFlowIstanceList.Select(c => c.Int("stepId")).ToList();//获取流程当前步骤
            //获取同会签的步骤
            var counterSignStep = allStepUserList.Where(c => filterStepIdList.Contains(c.Int("stepId")) && c.Int("actTypeId") == (int)FlowActionType.Countersign).ToList();
            var hitSameCounterSignStepIds = from c in allStepUserList
                                            join d in counterSignStep on c.Int("flowId") equals d.Int("flowId")
                                            where c.Int("stepOrder") == d.Int("stepOrder") && c.Int("stepId") != d.Int("stepId")
                                            select c.Int("stepId");

            //获取步骤是非发起的步骤
            var launchStepIdStrList = allStepUserList.Where(c => c.Int("actTypeId") != (int)FlowActionType.Launch && filterStepIdList.Contains(c.Int("stepId"))).Select(c => c.Int("stepId")).ToList();
            launchStepIdStrList.AddRange(hitSameCounterSignStepIds);//launchStepIdStrList包含了当前步骤和当前会签步骤对应相同stepOrder的其他步骤

            //此处获取当前步骤是我的而且还未执行过的
            var InstanceActionUserList = allStepUserList.Where(c=>c.Int("userId") == userId&&launchStepIdStrList.Contains(c.Int("stepId"))&&c.Int("status") == 0&&c.Int("actionAvaiable") != 1);
            
            var hitFlowInstanceIdList = from c in InstanceActionUserList
                                        join e in filterFlowIstanceList on c.Int("flowInstanceId") equals e.Int("flowInstanceId")
                                        where allStepOrderDic.ContainsKey(c.Int("stepId")) && allStepOrderDic.ContainsKey(e.Int("stepId")) &&
                                           allStepOrderDic[c.Int("stepId")] == allStepOrderDic[e.Int("stepId")]
                                        select e;

            var finalResult = hitFlowInstanceIdList.Distinct().ToList();
            return finalResult;
        }

        /// <summary>
        /// 获取发起人是当前用户的流程，并且已经与用户关联的流程，没有实例
        /// </summary>
        /// <param name="userId">用户Id</param>
        /// <returns></returns>
        public List<BsonDocument> GetUserWaitForStartFlow(int userId)
        {  //获取转办给我的最近新流程
            var grantUserIds = dataOp.FindAllByQueryStr("BusinessFlowTurnRight", string.Format("givenUserId={0}&status=0", userId)).Select(c => c.Int("orginalUserId")).Distinct().ToList();
            grantUserIds.Add(userId);
            var filterFlowInstanceIds = dataOp.FindAllByQueryStr("BusinessFlowTurnRight", string.Format("orginalUserId={0}&status=0", userId)).Where(c => c.Int("givenUserId") == userId).Select(c => c.String("flowInstanceId")).Distinct().ToList();
                     //获取步骤是发起人的步骤
            var launchStepIdIdList = dataOp.FindAll("BusFlowStep").Where(c => c.Int("actTypeId") == (int)FlowActionType.Launch).Select(c => c.String("stepId")).Distinct().ToList();
            var launchStepIdStrList = launchStepIdIdList.Select(c=>c.ToString()).ToList();
            //var filterTaskIdList = dataOp.FindAllByKeyVal("BusFlowInstance", "instanceStatus", "0").Where(c => !filterFlowInstanceIds.Contains(c.Int("flowInstanceId")) && !launchStepIdIdList.Contains(c.Int("stepId"))).Select(c => c.Int("referFieldValue")).ToList();
           // var filterTaskIdList2 = dataOp.FindAllByKeyVal("BusFlowInstance", "instanceStatus", "1").Select(c => c.Int("referFieldValue")).ToList();
            //filterTaskIdList.AddRange(filterTaskIdList2);
            var noIn1 = Query.NotIn("flowInstanceId", TypeConvert.StringListToBsonValueList(filterFlowInstanceIds));
            var noIn2 = Query.NotIn("stepId", TypeConvert.StringListToBsonValueList(launchStepIdIdList));
            var queryOr = Query.Or(Query.And(Query.EQ("instanceStatus", "0"), noIn2, noIn1), Query.EQ("instanceStatus", "1"));
            var filterTaskIdList = dataOp.FindAllByQuery("BusFlowInstance", queryOr).Select(c => c.Int("referFieldValue")).ToList();
           
          //过滤当前用户
            var hitFlowIdList = dataOp.FindAllByKeyValList("BusFlowStepUserRel", "stepId", launchStepIdStrList).Where(c =>grantUserIds.Contains(c.Int("userId"))).Select(c => c.Text("flowId")).Distinct().ToList();
            //过滤当前流程中与任务关联的任务关联列表
            var hitTaskFlowList = dataOp.FindAllByKeyValList("XH_DesignManage_TaskBusFlow", "flowId", hitFlowIdList).Where(c => !filterTaskIdList.Contains(c.Int("taskId"))).OrderByDescending(x=>x.Date("createDate")).ToList();
            return hitTaskFlowList;
        }


        /// <summary>
        /// 获取发起人是当前用户的流程，并且已经与用户关联的流程，没有实例
        /// </summary>
        /// <param name="userId">用户Id</param>
        /// <returns></returns>
        public List<BsonDocument> GetDesignChangeWaitForStartFlow(int userId)
        {  //获取转办给我的最近新流程
            var grantUserIds = dataOp.FindAllByQueryStr("BusinessFlowTurnRight", string.Format("givenUserId={0}&status=0", userId)).Select(c => c.Int("orginalUserId")).Distinct().ToList();
            grantUserIds.Add(userId);
            var filterFlowInstanceIds = dataOp.FindAllByQueryStr("BusinessFlowTurnRight", string.Format("orginalUserId={0}&status=0", userId)).Where(c => c.Int("givenUserId") == userId).Select(c => c.String("flowInstanceId")).Distinct().ToList();
            //获取步骤是发起人的步骤
            var launchStepIdIdList = dataOp.FindAll("BusFlowStep").Where(c => c.Int("actTypeId") == (int)FlowActionType.Launch).Select(c => c.String("stepId")).Distinct().ToList();
            var launchStepIdStrList = launchStepIdIdList.Select(c => c.ToString()).ToList();
            //var filterTaskIdList = dataOp.FindAllByKeyVal("BusFlowInstance", "instanceStatus", "0").Where(c => !filterFlowInstanceIds.Contains(c.Int("flowInstanceId")) && !launchStepIdIdList.Contains(c.Int("stepId"))).Select(c => c.Int("referFieldValue")).ToList();
            // var filterTaskIdList2 = dataOp.FindAllByKeyVal("BusFlowInstance", "instanceStatus", "1").Select(c => c.Int("referFieldValue")).ToList();
            //filterTaskIdList.AddRange(filterTaskIdList2);
            var noIn1 = Query.NotIn("flowInstanceId", TypeConvert.StringListToBsonValueList(filterFlowInstanceIds));
            var noIn2 = Query.NotIn("stepId", TypeConvert.StringListToBsonValueList(launchStepIdIdList));
            var queryOr = Query.Or(Query.And(Query.EQ("instanceStatus", "0"), noIn2, noIn1), Query.EQ("instanceStatus", "1"));
            var filterTaskIdList = dataOp.FindAllByQuery("BusFlowInstance", queryOr).Select(c => c.Int("referFieldValue")).ToList();

            //过滤当前用户
            var hitFlowIdList = dataOp.FindAllByKeyValList("BusFlowStepUserRel", "stepId", launchStepIdStrList).Where(c => grantUserIds.Contains(c.Int("userId"))).Select(c => c.Text("flowId")).Distinct().ToList();
            //过滤当前流程中与设计变更关联的设计变更关联列表
            var hitTaskFlowList = dataOp.FindAllByKeyValList("DesignChangeBusFlow", "flowId", hitFlowIdList).Where(c => !filterTaskIdList.Contains(c.Int("taskId"))).OrderByDescending(x => x.Date("createDate")).ToList();
            return hitTaskFlowList;
        }


        /// <summary>
        /// 获取流程实例中涉及的所有人
        /// </summary>
        /// <param name="flowId"></param>
        /// <param name="flowInstanceId"></param>
        /// <returns></returns>
        public List<int> GetFlowInstanceAssociateUser(int flowId, int flowInstanceId)
        {

            var hitQuery = string.Format("flowId={0}&flowInstanceId={1}&status=0", flowId, flowInstanceId);
            var instanceActionUser = dataOp.FindAllByQueryStr("InstanceActionUser", hitQuery).Select(c => c.Int("userId"));
            var hitList = TurnRightUserList(instanceActionUser.ToList(), flowInstanceId);
            return hitList;
        }

        #region 执行事务
        /// <summary>
        /// 执行进入事务
        /// </summary>
        /// <param name="nextStep"></param>
        /// <param name="instance"></param>
        public void DoJumpInTransaction(BsonDocument nextStep,BsonDocument instance){
            var nextTranList = new List<BsonDocument>();
            if (nextStep != null)
            {
                nextTranList = nextStep.ChildBsonList("StepTransaction").Where(m => m.Int("type") == 0).ToList();
            }
            if (nextTranList.Count > 0)
                TranFactory.Instance.ExecuteTran(nextTranList, dataOp.GetCurrentUserId(), instance);
        }
        /// <summary>
        /// 执行等待事务
        /// </summary>
        /// <param name="preStep"></param>
        /// <param name="instance"></param>
        public void DoWaitTransaction(BsonDocument preStep, BsonDocument instance)
        {
            var tranList = new List<BsonDocument>();

            if (preStep != null)
            {
                tranList = preStep.ChildBsonList("StepTransaction").Where(m => m.Int("type") == 1).ToList();
            }
            if (tranList.Count > 0)
                TranFactory.Instance.ExecuteTran(tranList, dataOp.GetCurrentUserId(), instance);
        }
        /// <summary>
        /// 立即执行跳出事务
        /// </summary>
        /// <param name="preStep"></param>
        /// <param name="instance"></param>
        public void DoImmediatelyTransaction(BsonDocument preStep,BsonDocument instance)
        {
            var ImmediatelyTranList = new List<BsonDocument>();
            if (preStep != null)
            {
                ImmediatelyTranList = preStep.ChildBsonList("StepTransaction").Where(m => m.Int("type") == 2).ToList();
            }
            if (ImmediatelyTranList.Count > 0)
                TranFactory.Instance.ExecuteTranImmediately(ImmediatelyTranList, dataOp.GetCurrentUserId(), instance);
        }                

        #endregion


        #region 中海宏扬获取要您发起审批
        //待发起计划任务
        public List<BsonDocument> ProjTask_GetUserLaunch(int curUserId)
        {
            var taskManagerRel = dataOp.FindAllByQuery("XH_DesignManage_TaskManager", Query.EQ("userId", curUserId.ToString())
                ).Where(i => i.Int("type") == (int)TaskManagerType.TaskOwner);
            var allTaskFlow = dataOp.FindAllByQuery("XH_DesignManage_TaskBusFlow",
                        Query.In("taskId", taskManagerRel.Select(p => (BsonValue)p.Text("taskId")))
                  ).ToList();
            var allTask = dataOp.FindAllByQuery("XH_DesignManage_Task",
                    Query.In("taskId", allTaskFlow.Select(i => i.GetValue("taskId", string.Empty)))
                ).ToList();
            
            var refTaskIds = dataOp.FindAllByQuery("BusFlowInstance",
                            Query.And(
                                Query.EQ("tableName", "XH_DesignManage_Task"),
                                Query.EQ("referFieldName", "taskId"),
                                Query.In("referFieldValue", allTask.Select(i => i.GetValue("taskId", string.Empty)))
                            )
                        ).Where(i => i.Int("approvalUserId") != 0 || i.Int("instanceStatus") == 1).ToList()
                        .Select(i => i.Int("referFieldValue"));
            allTask = allTask.Where(i => !refTaskIds.Contains(i.Int("taskId"))).ToList();
            //过滤掉已经删除的项目里的任务
            var projIdList = dataOp.FindAllByQuery("XH_DesignManage_Project",
                    Query.In("projId", allTask.Select(i => i.GetValue("projId", string.Empty)).Distinct())
                ).ToList().Select(i => i.Int("projId"));
            var taskIdList = allTask.Where(i => projIdList.Contains(i.Int("projId"))).ToList().Select(i => i.Int("taskId"));
            return allTaskFlow.Where(i => taskIdList.Contains(i.Int("taskId"))).ToList();
            
        }
        //待发起设计变更
        public List<BsonDocument> GetUserLaunchDesignChange(int currentUserId)
        {
            var changeList = dataOp.FindAllByQuery("DesignChange", Query.EQ("drafterId", currentUserId.ToString())).Where(c => c.Int("saveStatus") == 1).OrderByDescending(x => x.Date("createDate")).ToList();
            var refInstanceIds = dataOp.FindAllByQuery("BusFlowInstance",
                    Query.And(
                        Query.EQ("tableName", "DesignChange"),
                        Query.EQ("referFieldName", "designChangeId"),
                        Query.In("referFieldValue", changeList.Select(p => p.GetValue("designChangeId", string.Empty)))
                    )
                ).Where(i => i.Int("approvalUserId") != 0 || (i.Int("instanceStatus") == 1 && i.Int("approvalUserId") != 0))
                .ToList()
                .Select(c => c.Int("referFieldValue")).ToList();
            
            changeList = changeList.Where(p => !refInstanceIds.Contains(p.Int("designChangeId"))).ToList();
            //过滤掉已经删除的项目里的任务
            var projIdList = dataOp.FindAllByQuery("XH_DesignManage_Project",
                    Query.In("projId", changeList.Select(i => i.GetValue("projId", string.Empty)).Distinct())
                ).ToList().Select(i => i.Int("projId"));
            changeList = changeList.Where(i => projIdList.Contains(i.Int("projId"))).ToList();

            return changeList;
        }
        //待发起自由审批
        public List<BsonDocument> FreeTask_GetUserLaunchApp(int curUserId)
        {
            var allFreeApp = dataOp.FindAll("FreeFlow_FreeApproval").Where(i => i.Int("createUserId") == curUserId).ToList();
            var refTaskIds = dataOp.FindAllByQuery("BusFlowInstance",
                            Query.And(
                                Query.EQ("tableName", "FreeFlow_FreeApproval"),
                                Query.EQ("referFieldName", "freeApprovalId"),
                                Query.In("referFieldValue", allFreeApp.Select(i => i.GetValue("freeApprovalId", string.Empty)))
                            )
                        ).Where(i => i.Int("approvalUserId") != 0 || i.Int("instanceStatus") == 1).ToList()
                        .Select(i => i.Int("referFieldValue"));
            allFreeApp = allFreeApp.Where(i => !refTaskIds.Contains(i.Int("freeApprovalId"))).ToList();
            //过滤掉已经删除的项目里的任务
            var projIdList = dataOp.FindAllByQuery("XH_DesignManage_Project",
                    Query.In("projId", allFreeApp.Select(i => i.GetValue("projId", string.Empty)).Distinct())
                ).ToList().Select(i => i.Int("projId"));
            allFreeApp = allFreeApp.Where(i => projIdList.Contains(i.Int("projId"))).ToList();
            return allFreeApp;
        }


        #endregion

        #region 获取流程实例可发起人

        /// <summary>
        /// 获取流程实例可发起人
        /// </summary>
        /// <param name="flowInstanceId"></param>
        /// <returns></returns>
        public List<int> GetCanLaunchUserIds(int flowInstanceId)
        {
            List<int> result = new List<int>();
            if (SysAppConfig.CustomerCode == Yinhe.ProcessingCenter.Common.CustomerCode.ZHHY)
            {
                result = ZHHYGetLaunchUserIds(flowInstanceId);
            }
            return result;
        }

        /// <summary>
        /// ZHHY判断流程的可发起人
        /// </summary>
        /// <param name="flowInstanceId"></param>
        /// <returns></returns>
        public List<int> ZHHYGetLaunchUserIds(int flowInstanceId)
        {
            DataOperation dataOp = new DataOperation();
            List<int> result = new List<int>();
            var curFlowInstance = dataOp.FindOneByQuery("BusFlowInstance", Query.EQ("flowInstanceId", flowInstanceId.ToString()));
            var targetObj = dataOp.FindOneByKeyVal(curFlowInstance.Text("tableName"), curFlowInstance.Text("referFieldName"), curFlowInstance.Text("referFieldValue"));
            switch (curFlowInstance.Text("tableName"))
            {
                case "XH_DesignManage_Task":
                    var task = targetObj;
                    var taskManagerRel = dataOp.FindAllByQuery("XH_DesignManage_TaskManager", Query.EQ("taskId", task.Text("taskId")));
                    if (task.Text("hiddenTask") == "1") //隐藏子任务的任务负责人为主任务的负责人
                    {
                        taskManagerRel = dataOp.FindAllByQuery("XH_DesignManage_TaskManager", Query.EQ("taskId", task.Text("nodePid")));
                    }
                    result = taskManagerRel.Select(i => i.Int("userId")).ToList();
                    break;
                case "FreeFlow_FreeApproval":
                    result = new List<int>() { targetObj.Int("createUserId") };
                    break;
                case "DesignChange":
                    result = new List<int>() { targetObj.Int("drafterId") };
                    break;
                default:
                    break;
            }
            return result;
        }
        #endregion

        #region QX由我发起的
        public List<BsonDocument> QX_GetMyLaunch(int userId,string tableName,string keyName)
        {
            var instanceList = dataOp.FindAll("BusFlowInstance");
            var entityList = dataOp.FindAll(tableName);
            var result = (from instance in instanceList
                          join entity in entityList
                          on instance.Text("referFieldValue") equals entity.Text(keyName)
                          where instance.Int("approvalUserId") == userId
                          select entity)
                          .OrderByDescending(i => i.Date("createDate")).ToList();
            return result;
        }
        #endregion 

        #region 获取流程实例签发人
        /// <summary>
        /// 获取流程实例签发人
        /// </summary>
        /// <param name="flowId"></param>
        /// <param name="flowInstanceId"></param>
        /// <returns></returns>
        public List<BsonDocument> GetFlowInstanceSignUser(int flowId, int flowInstanceId)
        {
            List<string> signStepIdList = BusFlowStepBll._().GetSignStep(flowId)
                .OrderBy(i => i.Int("stepOrder")).Select(i => i.Text("stepId")).ToList();
            List<BsonDocument> signUserList = dataOp.FindAllByKeyValList("InstanceActionUser", "stepId", signStepIdList)
                .Where(i => i.Int("flowInstanceId") == flowInstanceId && i.Int("status") == 0 && i.Int("isSkip") == 0)
                .ToList();
            return signUserList;
        }
        #endregion

        #endregion

        #region 人员表单内容获取
        /// <summary>
        /// 获取当前人最近回复的内容
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public List<string> GetLatestReplyConent(string userId)
        {
             var curUserFlowFormDataList = dataOp.FindAllByKeyVal("BusFlowFormData", "createUserId", userId).Where(c => !string.IsNullOrEmpty(c.Text("content"))).OrderByDescending(c => c.Date("createDate")).Select(c => c.Text("content"));
             return curUserFlowFormDataList.Distinct().Take(5).ToList();
         }
        #endregion

        #region 快捷操作
        /// <summary>
        /// 将步骤额外属性值复制为当前流程模板对象
        /// </summary>
        /// <param name="actionUser"></param>
        /// <param name="curStepObj"></param>
        /// <returns></returns>
        public void CopyFlowStepProperty(BsonDocument actionUser, BsonDocument curStepObj)
        {
            if (actionUser != null && curStepObj != null)
            {
                actionUser.TryAdd("flowId", curStepObj.Text("flowId"));
                actionUser.TryAdd("stepOrder", curStepObj.Text("stepOrder"));//步骤顺序
                actionUser.TryAdd("flowPosId", curStepObj.Text("flowPosId"));//步骤岗位
                actionUser.TryAdd("actTypeId", curStepObj.Text("actTypeId"));//步骤类型
                actionUser.TryAdd("enslavedStepId", curStepObj.Text("enslavedStepId"));//控制会签步骤
                actionUser.TryAdd("resetCSignStepId", curStepObj.Text("resetCSignStepId"));//二次会签步骤
                actionUser.TryAdd("completeStepName", curStepObj.Text("completeStepName"));//结束会签名称
                actionUser.TryAdd("isFixUser", curStepObj.Text("isFixUser"));//是否固定审批人员，用于编辑人员情况下是否可以重新选人，
                actionUser.TryAdd("canImComplete", curStepObj.Text("canImComplete"));//是否可直接结束
                actionUser.TryAdd("ImCompleteName", curStepObj.Text("ImCompleteName"));//结束流程名称
                actionUser.TryAdd("isChecker", curStepObj.Text("isChecker"));//是否复核人
                actionUser.TryAdd("refuseStepId", curStepObj.Text("refuseStepId"));//驳回步骤Id
                actionUser.TryAdd("isHideLog", curStepObj.Text("isHideLog"));//是否隐藏日志
                actionUser.TryAdd("noTurnRight", curStepObj.Text("noTurnRight"));//是否禁用转办
                actionUser.TryAdd("sameUserStepId", curStepObj.Text("sameUserStepId"));//相同审批人员步骤
                actionUser.TryAdd("noRefuseBtn", curStepObj.Text("noRefuseBtn"));//是否禁用驳回按钮
                actionUser.TryAdd("noReplyBtn", curStepObj.Text("noReplyBtn"));//是否禁用回复按钮
            }
        
        }
        #endregion

        #region 获取流程实例实际可能执行的步骤列表
        /// <summary>
        /// 获取流程实例实际可能执行的步骤列表
        /// </summary>
        /// <param name="flowInstanceId">流程实例id</param>
        /// <returns></returns>
        public InvokeResult<IEnumerable<int>> GetInstancePossibleSteps(int flowInstanceId)
        {
            var instance = dataOp.FindAllByQuery("BusFlowInstance", Query.EQ("flowInstanceId", flowInstanceId.ToString()))
                .OrderByDescending(i => i.Date("createDate")).FirstOrDefault();
            //初始步骤取流程模板的发起步骤
            var stepList = dataOp.FindAllByKeyVal("BusFlowStep", "flowId", instance.Text("flowId")).OrderBy(c => c.Int("stepOrder"));
            var firstStep = stepList.Where(c => c.Int("actTypeId") == (int)FlowActionType.Launch).FirstOrDefault();
            List<int> firstStepIds = new List<int>();
            if (!firstStep.IsNullOrEmpty())
            {
                firstStepIds.Add(firstStep.Int("stepId"));
            }
            return GetInstancePossibleSteps(instance.Int("flowId"), instance.Int("flowInstanceId"), firstStepIds, null);
        }
        /// <summary>
        /// 获取流程实例实际可能执行的步骤列表
        /// </summary>
        /// <param name="flowInstanceId"></param>
        /// <param name="money"></param>
        /// <returns></returns>
        public InvokeResult<IEnumerable<int>> GetInstancePossibleSteps(int flowInstanceId, double money)
        {
            var instance = dataOp.FindAllByQuery("BusFlowInstance", Query.EQ("flowInstanceId", flowInstanceId.ToString()))
                .OrderByDescending(i => i.Date("createDate")).FirstOrDefault();
            //初始步骤取流程模板的发起步骤
            var stepList = dataOp.FindAllByKeyVal("BusFlowStep", "flowId", instance.Text("flowId")).OrderBy(c => c.Int("stepOrder"));
            var firstStep = stepList.Where(c => c.Int("actTypeId") == (int)FlowActionType.Launch).FirstOrDefault();
            List<int> firstStepIds = new List<int>();
            if (!firstStep.IsNullOrEmpty())
            {
                firstStepIds.Add(firstStep.Int("stepId"));
            }
            return GetInstancePossibleSteps(instance.Int("flowId"), instance.Int("flowInstanceId"), firstStepIds, money);
        }
        /// <summary>
        /// 获取流程实例实际可能执行的步骤列表
        /// </summary>
        /// <param name="flowId">指定流程模板id</param>
        /// <param name="flowInstanceId">流程实例id</param>
        /// <param name="startStepIds">指定起始步骤id</param>
        /// <param name="money">指定判断金额，为null则由事务自行获取</param>
        /// <returns></returns>
        public InvokeResult<IEnumerable<int>> GetInstancePossibleSteps(int flowId, int flowInstanceId, IEnumerable<int> startStepIds, double? money)
        {
            InvokeResult<IEnumerable<int>> invokeResult = new InvokeResult<IEnumerable<int>>() { Status = Status.Successful };
            var allStepIds = new List<int>();
            var instance = dataOp.FindAllByQuery("BusFlowInstance", Query.EQ("flowInstanceId", flowInstanceId.ToString()))
                .OrderByDescending(i => i.Date("createDate")).FirstOrDefault();
            if (instance.IsNullOrEmpty())
            {
                invokeResult.Status = Status.Failed;
                invokeResult.Message = "无效的流程实例";
                invokeResult.Value = new List<int>();
                return invokeResult;
            }

            //获取流程模板中的步骤
            var allFlowSteps = dataOp.FindAllByKeyVal("BusFlowStep", "flowId", flowId.ToString()).ToList();
            var allFlowStepIds = allFlowSteps
                .Select(i => i.Int("stepId")).ToList();

            var curStep = allFlowSteps.Where(i => i.Int("stepId") == instance.Int("stepId")).FirstOrDefault();
            if (curStep.IsNullOrEmpty())
            {
                invokeResult.Status = Status.Failed;
                invokeResult.Message = "未能获取当前流程实例步骤";
                invokeResult.Value = new List<int>();
                return invokeResult;
            }
            //如果当前步骤为发起步骤，则返回流程模板中的所有步骤
            if (curStep.Int("actTypeId") == (int)FlowActionType.Launch)
            {
                invokeResult.Status = Status.Successful;
                invokeResult.Value = allFlowStepIds;
                return invokeResult;
            }

            //获取流程审批人员并过滤不在以上步骤中的
            var allInstanceUsers = dataOp.FindAllByKeyVal("InstanceActionUser", "flowInstanceId", flowInstanceId.ToString())
                .Where(c => c.Int("status") == 0 && c.Int("isSkip") == 0)
                .Where(i => allFlowStepIds.Contains(i.Int("stepId")))
                .ToList();
            //获取所有步骤的跳出事务关联
            var allStepTrans = dataOp.FindAllByQuery("StepTransaction",
                Query.And(
                    Query.In("stepId", allInstanceUsers.Select(i => i.GetValue("stepId", string.Empty))),
                    Query.EQ("type", "2")
                    )
                ).ToList();
            var allTranIds = allStepTrans.Select(i => i.Int("transactionId"));
            var allTrans = dataOp.FindAll("TransactionStore")
                .Where(i => allTranIds.Contains(i.Int("transactionId"))).ToList();
            Yinhe.ProcessingCenter.BusinessFlow.ApprovaedAmountReDirect approvaedAmountReDirect = new Yinhe.ProcessingCenter.BusinessFlow.ApprovaedAmountReDirect();

            if (startStepIds!=null)
            {
                allStepIds = new List<int>(startStepIds);
            }
            var tempStepIds = allStepIds;//获取当前执行步骤
            var nextStepIds = new List<int>();//当前执行步骤可能的下一步
            int maxLoopCount = 9999;//防止死循环...
            do
            {
                //获取当前流程实例在当前这些步骤下实际可跳转的下一步骤
                {
                    List<BsonDocument> result = new List<BsonDocument>();
                    foreach (var preStepId in tempStepIds)
                    {
                        var curStepUser = allInstanceUsers.Where(i => i.Int("stepId") == preStepId).FirstOrDefault();
                        if (curStepUser.IsNullOrEmpty()) continue;
                        var nextStepUser = new BsonDocument();

                        #region 判断是否有关联跳出事务，有的话则进行判断
                        {
                            var preStepTranRel = allStepTrans.Where(i => i.Int("stepId") == preStepId).ToList();
                            var preStepTranIds = preStepTranRel.Select(i => i.Int("transactionId")).ToList();
                            var preStepTrans = allTrans.Where(i => preStepTranIds.Contains(i.Int("transactionId"))).ToList();
                            bool isDirectEnd = false;
                            foreach (var tranStore in preStepTrans)
                            {

                                #region 根据事务类判断下一步是直接结束还是跳转到其他步骤

                                switch (tranStore.Text("tranClass"))
                                {
                                    case "Yinhe.ProcessingCenter.BusinessFlow.ApprovaedAmountReDirect":
                                        var flowPosId = tranStore.Int("flowPosId");
                                        var checkPropertyArray = approvaedAmountReDirect.checkPropertyArray;
                                        var approvedAmount = approvaedAmountReDirect.GetApprovedAmount(instance);//获取对应审批金额
                                        if (money.HasValue)//参数指定金额
                                        {
                                            approvedAmount = money.Value.ToString();
                                        }
                                        if (approvaedAmountReDirect.canJumpStep(approvedAmount, tranStore) &&
                                            approvaedAmountReDirect.CheckProperty(checkPropertyArray, instance, tranStore))
                                        {
                                            if (flowPosId == -1)// 默认直接结束流程
                                            {
                                                isDirectEnd = true;
                                            }
                                            else
                                            {
                                                var hitStepObj = allFlowSteps.Where(c => c.Int("flowPosId") == flowPosId).FirstOrDefault();
                                                nextStepUser = allInstanceUsers.Where(i => i.Int("stepId") == hitStepObj.Int("stepId")).FirstOrDefault();
                                            }
                                        }
                                        break;
                                    case "Yinhe.ProcessingCenter.BusinessFlow.CompleteInstanceStatus":
                                        var approvalAmount = instance.Double("approvedAmount");
                                        if (money.HasValue)//参数指定金额
                                        {
                                            approvalAmount = money.Value;
                                        }
                                        double approveAmount = 200000;

                                        if (tranStore.Double("approvedAmount") != 0)
                                        {
                                            approveAmount = tranStore.Double("approvedAmount");
                                        }
                                        if (approvalAmount < approveAmount)//小于20w流程将计就计金额数200000
                                        {
                                            isDirectEnd = true;
                                        }
                                        break;
                                    default: break;
                                }

                                #endregion

                            }
                            if (preStepTrans.Count > 0)
                            {
                                if (isDirectEnd)//直接结束或者有跳转步骤
                                {
                                    continue;
                                }
                                else if (!nextStepUser.IsNullOrEmpty())
                                {
                                    result.AddRange(allInstanceUsers.Where(i => i.Int("stepOrder") == nextStepUser.Int("stepOrder")));
                                    continue;
                                }
                            }
                        }
                        #endregion

                        nextStepUser = allInstanceUsers.OrderBy(i => i.Int("stepOrder"))
                            .Where(i => i.Int("stepOrder") > curStepUser.Int("stepOrder")).FirstOrDefault();

                        if (!nextStepUser.IsNullOrEmpty())
                        {
                            result.AddRange(allInstanceUsers.Where(i => i.Int("stepOrder") == nextStepUser.Int("stepOrder")));
                        }
                    }
                    //
                    nextStepIds = result.Select(i => i.Int("stepId")).ToList();
                }

                //从可能执行的下一步中过滤之前已经出现过的步骤，剩下的作为当前执行步骤进入下一次循环
                tempStepIds = nextStepIds.Where(i => !allStepIds.Contains(i)).ToList();
                allStepIds = allStepIds.Union(nextStepIds).ToList();//合并之前出现过的步骤与本次循环中的下一步

            } while (tempStepIds.Count > 0 && maxLoopCount-- > 0);//当前没有可执行的步骤或死循环时跳出...
            if (maxLoopCount <= 0)
            {
                invokeResult.Status = Status.Failed;
                invokeResult.Message = "超出设定最大循环次数";
                invokeResult.Value = new List<int>();
                return invokeResult;
            }

            invokeResult.Status = Status.Successful;
            invokeResult.Value = allStepIds;
            return invokeResult;
        }
        #endregion
    }

}
