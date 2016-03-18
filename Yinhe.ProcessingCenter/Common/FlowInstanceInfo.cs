using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;

namespace Yinhe.ProcessingCenter
{
    /// <summary>
    /// 流程实例相关信息处理类
    /// </summary>
    public class FlowInstanceInfo
    {
        public FlowInstanceInfo()
        {
         
        }
        /// <summary>
        /// 是否拥有按钮操作权限，同意驳回等按钮
        /// </summary>
        public bool hasOperateRight { get; set; }
        /// <summary>
        /// 是否拥有编辑流程对象的权限，如编辑设计变更，添加合同，上传文件，
        /// </summary>
        public bool hasEditRight { get; set; }
        /// <summary>
        /// 是否拥有直接结束会签的权限
        /// </summary>
        public bool canForceCompleteAssign { get; set; }
        /// <summary>
        /// 当前可操作的用户名
        /// </summary>
        public string curAvaiableUserName { get; set; }
        /// <summary>
        /// 当前可操作的用户Id
        /// </summary>
        public int curUserId { get; set; }
        /// <summary>
        /// 当前可操作的用户Id列表
        /// </summary>
        public int curUserIds { get; set; }
        /// <summary>
        /// 当前步骤对象，此处可分为，真正可执行的步骤对象，否则为流程实例当前的流程步骤对象
        /// </summary>
        public BsonDocument curStep { get; set; }
        /// <summary>
        /// 当前步骤Id
        /// </summary>
        public int stepId { get; set; }
        /// <summary>
        /// 当前流程实例对象
        /// </summary>
        public BsonDocument curFlowInstance { get; set; }
        /// <summary>
        /// 当前流程实例对象Id
        /// </summary>
        public int flowInstanceId { get; set; }
        /// <summary>
        /// 当前步骤的转办列表
        /// </summary>
        public List<BsonDocument> turnRightList { get; set; }
        /// <summary>
        /// 当前步骤的动作列表
        /// </summary>
        public List<BsonDocument> actionList { get; set; }
        /// <summary>
        /// 获取动作别名列表
        /// </summary>
        public List<BsonDocument> curActionNameList { get; set; }
        /// <summary>
        /// 获取可更改的字段列表
        /// </summary>
        public List<BsonDocument> referFiledList { get; set; }
        /// <summary>
        /// 当前流程实例对象
        /// </summary>
        public List<BsonDocument> allInstanceActionUser { get; set; }
        /// <summary>
        /// 当前步骤可控制会签的列表
        /// </summary>
        public List<BsonDocument> hitEnslavedStepOrder { get; set; }
        /// <summary>
        /// 获取二次会签可控制的会签步骤列表
        /// </summary>
        public List<BsonDocument> hitResetCSignStepOrder { get; set; }
        /// <summary>
        //获取可驳回重申的步骤
        /// </summary>
        public List<BsonDocument> hitRefuseStepObj { get; set; }
        /// <summary>
        /// 是否初始用户，转办之后非初始用户
        /// </summary>
        public bool isOrginalUser { get; set; }
        /// <summary>
        /// 提交用户Id，既谁转办给我
        /// </summary>
        public int submitUserId { get; set; }
        /// <summary>
        /// 转办备注
        /// </summary>
        public string grantRemark { get; set; }
        /// <summary>
        /// 是否可以强制结束
        /// </summary>
        public bool canImComplete { get; set; }
        /// <summary>
        /// 是否有转办按钮，后台控制是否有转办的权限
        /// </summary>
        public bool hasTurnRightBtn { get; set; }
     
        
    }

}
