using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using MongoDB.Driver;
using System.Configuration;

namespace Yinhe.ProcessingCenter.DesignManage
{
    /// <summary>
    /// 设计变更处理类
    /// </summary>
    public class DesignChangeBll
    {
        #region 构造函数
        private DataOperation dataOp = null;
        private DesignChangeBll()
        {
            dataOp = new DataOperation();
        }
        private DesignChangeBll(DataOperation _dataOp)
        {
            this.dataOp = _dataOp;
        }
        public static DesignChangeBll _()
        {
            return new DesignChangeBll();
        }
        public static DesignChangeBll _(DataOperation _dataOp)
        {
            return new DesignChangeBll(_dataOp);
        }
        #endregion

        #region 公共查询方法

        #region 获取所有已通过审批的变更单的指令单 IEnumerable<BsonDocument> GetAllChangeCmd()
        /// <summary>
        /// 获取所有已通过审批的变更单的指令单
        /// </summary>
        /// <returns></returns>
        public IEnumerable<BsonDocument> GetAllChangeCmd()
        {
            var allCompleteInstances = dataOp.FindAllByQuery("BusFlowInstance",
                    Query.And(
                        Query.EQ("tableName", "DesignChange"),
                        Query.EQ("referFieldName", "designChangeId"),
                        Query.EQ("instanceStatus", "1")
                    )
                ).Where(i => i.Int("approvalUserId") != 0);
            var allDesignChange = dataOp.FindAllByQuery("DesignChange",
                    Query.In("designChangeId", allCompleteInstances.Select(i => i.GetValue("referFieldValue", string.Empty)))
                );
            var allDesignChangeCmds = dataOp.FindAllByQuery("DesignChangeCommand",
                    Query.In("designChangeId", allDesignChange.Select(i => i.GetValue("designChangeId", string.Empty)))
                );
            return allDesignChangeCmds;
        }
        #endregion

        #region 获取指令单所有的操作记录 IEnumerable<BsonDocument> GetAllChangeCmdLogs(IEnumerable<int> cmdIdList)
        /// <summary>
        /// 获取指令单所有的操作记录
        /// </summary>
        /// <param name="cmdIdList">指令单id列表</param>
        /// <returns></returns>
        public IEnumerable<BsonDocument> GetAllChangeCmdLogs(IEnumerable<int> cmdIdList)
        {
            var allLogs = dataOp.FindAllByQuery("DesignChangeCommandLog",
                    Query.In("changeCommandId", cmdIdList.Select(i=>(BsonValue)(i.ToString())))
                );
            return allLogs;
        }
        #endregion

        #region 获取所有需要指定人员签发的指令单 IEnumerable<BsonDocument> GetAllSignOutChangeCmd(int userId)
        /// <summary>
        /// 获取所有需要指定人员签发的指令单
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public IEnumerable<BsonDocument> GetAllSignOutChangeCmd(int userId)
        {
            var allChangeCmds = GetAllChangeCmd();
            //获取签发人为当前指定用户的指令单
            allChangeCmds = allChangeCmds.Where(i => i.Text("signerId").SplitToIntList(",").Contains(userId));
            return allChangeCmds;
        }
        #endregion

        #region 获取待签发的指令单 IEnumerable<BsonDocument> GetNotSignOutChangeCmd(int userId)
        /// <summary>
        /// 获取未签发的指令单
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public IEnumerable<BsonDocument> GetNotSignOutChangeCmd(int userId)
        {
            var allChangeCmds = GetAllSignOutChangeCmd(userId);
            var allNoSignIdCmds = allChangeCmds.Where(i => i.Int("status") == (int)DesignChangeCmdStatus.NotIssue);
            return allNoSignIdCmds;
        }
        #endregion

        #region 获取所有需要指定人员签收的指令单 IEnumerable<BsonDocument> GetAllSignInChangeCmd(int userId)
        /// <summary>
        /// 获取所有需要指定人员签收的指令单
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public IEnumerable<BsonDocument> GetAllSignInChangeCmd(int userId)
        {
            var allChangeCmds = GetAllChangeCmd();
            //获取签收人当前指定用户的指令单
            allChangeCmds = allChangeCmds.Where(i => i.Text("signInId").SplitToIntList(",").Contains(userId));
            return allChangeCmds;
        }
        #endregion

        #region 获取未签收的指令单 IEnumerable<BsonDocument> GetNotSignInChangeCmd(int userId)
        /// <summary>
        /// 获取未签发的指令单
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public IEnumerable<BsonDocument> GetNotSignInChangeCmd(int userId)
        {
            var allChangeCmds = GetAllSignInChangeCmd(userId)
                .Where(i => i.Int("status") == (int)DesignChangeCmdStatus.NotSee);

            var allCmdLogs = GetAllChangeCmdLogs(allChangeCmds.Select(i => i.Int("changeCommandId")));

            var allNoSignIdCmds = allChangeCmds.Where(i =>
                    allCmdLogs.Where(u => i.Int("changeCommandId") == u.Int("changeCommandId"))
                    .Where(u => u.Int("actionAvailable") != 1)
                    .Where(u => u.Int("userId") == userId)
                    .Where(u => u.Text("actionName").Trim() == "read")
                    .Count() <= 0
                );
            return allNoSignIdCmds;
        }
        #endregion

        #region 获取所有抄报给指定人员的指令单 IEnumerable<BsonDocument> GetAllReportChangeCmd(int userId)
        /// <summary>
        /// 获取所有抄报给指定人员的指令单
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public IEnumerable<BsonDocument> GetAllReportChangeCmd(int userId)
        {
            var allChangeCmds = GetAllChangeCmd()
                .Where(i => i.Int("status") != (int)DesignChangeCmdStatus.NotIssue);
            var allReportUserRels = dataOp.FindAll("CommandCopeMan");
            return from cmd in allChangeCmds
                   join userRel in allReportUserRels
                   on cmd.Int("changeCommandId") equals userRel.Int("changeCommandId")
                   where userRel.Int("sendManId") == userId
                   select cmd;
        }
        #endregion

        #region 获取所有抄报给指定人员且已经阅读的指令单 IEnumerable<BsonDocument> GetReportReadChangeCmd(int userId)
        /// <summary>
        /// 获取所有抄报给指定人员且已经阅读的指令单
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public IEnumerable<BsonDocument> GetReportReadChangeCmd(int userId)
        {
            var allChangeCmds = GetAllReportChangeCmd(userId);
            var allCmdLogs = dataOp.FindAll("DesignChangeCommandLog");
            return from cmd in allChangeCmds
                   join log in allCmdLogs on cmd.Int("changeCommandId") equals log.Int("changeCommandId")
                   where log.String("actionName", string.Empty) == "reportRead"
                   where log.Int("userId") == userId
                   where log.Int("actionAvailable") == 0
                   select cmd;
        }
        #endregion

        #region 获取所有抄报给指定人员且未阅读的指令单 IEnumerable<BsonDocument> GetNotReportReadChangeCmd(int userId)
        /// <summary>
        /// 获取所有抄报给指定人员且已经阅读的指令单
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public IEnumerable<BsonDocument> GetNotReportReadChangeCmd(int userId)
        {
            var allChangeCmds = GetAllReportChangeCmd(userId);
            var allReportReadCmds = GetReportReadChangeCmd(userId);
            return allChangeCmds.Except(allReportReadCmds);
        }
        #endregion

        #region 获取指定人员所有签发历史对应指令单 IEnumerable<BsonDocument> GetAllSignOutCmdHistory(int userId)
        /// <summary>
        /// 获取指定人员所有签发历史对应指令单,返回DesignChangeCommand实例
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public IEnumerable<BsonDocument> GetAllSignOutCmdHistory(int userId)
        {
            var allCmdLogs = dataOp.FindAllByQuery("DesignChangeCommandLog", Query.EQ("actionName", "issue"))
                .Where(i => i.Int("createUserId") == userId);
            var allChangeCmds = dataOp.FindAllByQuery("DesignChangeCommand",
                    Query.In("changeCommandId", allCmdLogs.Select(i => (BsonValue)i.Text("changeCommandId")))
                );
            return allChangeCmds;
        }
        #endregion

        #region 获取指定人员所有签收历史对应指令单 IEnumerable<BsonDocument> GetAllReadCmdHistory(int userId)
        /// <summary>
        /// 获取指定人员所有签收历史对应指令单,返回DesignChangeCommand实例
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public IEnumerable<BsonDocument> GetAllReadCmdHistory(int userId)
        {
            var allCmdLogs = dataOp.FindAllByQuery("DesignChangeCommandLog", Query.EQ("actionName", "read"))
                .Where(i => i.Int("createUserId") == userId);
            
            var allChangeCmds=dataOp.FindAll("DesignChangeCommand");
            return from log in allCmdLogs
                   join cmd in allChangeCmds on log.Int("changeCommandId") equals cmd.Int("changeCommandId")
                   select cmd;

        }
        #endregion

        #endregion

        #region 公共操作方法

        #region 新增抄报人阅读记录 AddReportReadChangeCmdLog(int userId,int commandId)
        /// <summary>
        /// 新增抄报人阅读记录
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public InvokeResult AddReportReadChangeCmdLog(int userId,int commandId)
        {
            string cmdTb = "DesignChangeCommand";
            InvokeResult result = new InvokeResult();
            //获取对应指令单
            var command = dataOp.FindOneByQuery(cmdTb, Query.EQ("changeCommandId", commandId.ToString()));
            if (command.IsNullOrEmpty())
            {
                result.Status = Status.Failed;
                result.Message = "未能找到变更指令单";
                return result;
            }
            if (command.Int("status") == (int)DesignChangeCmdStatus.NotIssue)
            {
                result.Status = Status.Failed;
                result.Message = "指令单未签发";
                return result;
            }
            //判断该用户是否是抄报人之一
            var reportUserRel = dataOp.FindAll("CommandCopeMan")
                .Where(i => i.Int("sendManId") == userId)
                .Where(i => i.Int("changeCommandId") == commandId)
                .FirstOrDefault();
            if (reportUserRel.IsNullOrEmpty())
            {
                result.Status = Status.Failed;
                result.Message = "不是该指令单的抄报人";
                return result;
            }
            //如果已经阅读过则不添加
            var reportReadLog = dataOp.FindAll("DesignChangeCommandLog")
                .Where(i => i.Int("changeCommandId") == commandId)
                .Where(i => i.Int("userId") == userId)
                .Where(i => i.String("actionName", string.Empty) == "reportRead")
                .Where(i => i.Int("actionAvailable") == 0)
                .FirstOrDefault();
            if (!reportReadLog.IsNullOrEmpty())
            {
                result.Status = Status.Failed;
                result.Message = "该抄报人已阅读此指令单";
                return result;
            }

            var newLog = new BsonDocument(){
                {"changeCommandId",commandId.ToString()},
                {"userId",userId.ToString()},
                {"actionName","reportRead"},
                {"actionAvailable","0"}
            };
            return dataOp.Insert("DesignChangeCommandLog", newLog);
        }
        #endregion

        #endregion
    }
}
