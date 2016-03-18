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
    /// 流程用户授权处理类
    /// </summary>
    public class BusinessFlowUserGrantBll
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
        private BusinessFlowUserGrantBll() 
        {
            dataOp = new DataOperation();
        }

        private BusinessFlowUserGrantBll(DataOperation _dataOp)
        {
            dataOp = _dataOp;
        }

        public static BusinessFlowUserGrantBll _()
        {
            return new BusinessFlowUserGrantBll();
        }

        public static BusinessFlowUserGrantBll _(DataOperation _dataOp)
        {
            return new BusinessFlowUserGrantBll(_dataOp);
        }
        #endregion
        #region 查询
        /// <summary>
        /// 根据Id查询
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public BsonDocument FindById(int id)
        {
          return dataOp.FindAllByQueryStr("BusinessFlowUserGrant", string.Format("turnId={0}&status={1}", id, 0)).FirstOrDefault();
        }


        /// <summary>
        /// 根据Id查询
        /// </summary>
        /// <param name="flowInstanceId"></param>
        /// <returns></returns>
        public List<BsonDocument> FindByFlowInstanceId(int flowInstanceId)
        {
        return dataOp.FindAllByQueryStr("BusinessFlowUserGrant", string.Format("flowInstanceId={0}&status={1}", flowInstanceId, 0)).ToList();
        }


        /// <summary>
        /// 获取当前是否被转办,没过滤status 应为可能被过滤多次
        /// </summary>
        /// <param name="flowInstanceId"></param>
        /// <param name="givenUserId"></param>
        /// <returns></returns>
        public List<BsonDocument> HasTurnRightQuery(int flowInstanceId, int grantUserId)
        {
            return dataOp.FindAllByQueryStr("BusinessFlowUserGrant", string.Format("flowInstanceId={0}&grantUserId={1}", flowInstanceId, grantUserId)).ToList();
     
        }

        /// <summary>
        /// 获取当前当前用户被给予的权限列表
        /// </summary>
        /// <param name="flowInstanceId"></param>
        /// <param name="givenUserId"></param>
        /// <returns></returns>
        public List<BsonDocument> FindByFlowInstanceId(int flowInstanceId, int givenUserId)
        {
            return dataOp.FindAllByQueryStr("BusinessFlowUserGrant", string.Format("flowInstanceId={0}&givenUserId={1}&status=0", flowInstanceId, givenUserId)).ToList();
        }

        /// <summary>
        /// 获取上步骤的转办给当前人的记录
        /// </summary>
        /// <param name="flowInstanceId"></param>
        /// <param name="stepId"></param>
        /// <param name="curUserId"></param>
        /// <returns></returns>
        public BsonDocument LastUserGrant(string flowInstanceId, string stepId, int curUserId)
        {
            var hitObj = dataOp.FindAllByQueryStr("BusinessFlowUserGrant", string.Format("flowInstanceId={0}&givenUserId={1}&status=0&stepId={2}", flowInstanceId, curUserId,stepId)).OrderByDescending(c=>c.Date("createDate")).FirstOrDefault();
            return hitObj;
        
        }


        /// <summary>
        /// 获取最早上步骤的转办给当前人的记录
        /// </summary>
        /// <param name="flowInstanceId"></param>
        /// <param name="stepId"></param>
        /// <param name="curUserId"></param>
        /// <returns></returns>
        public BsonDocument FirstUserGrant(string flowInstanceId, string stepId, int curUserId)
        {
            var hitObj = dataOp.FindAllByQueryStr("BusinessFlowUserGrant", string.Format("flowInstanceId={0}&givenUserId={1}&stepId={2}", flowInstanceId, curUserId, stepId)).OrderBy(c => c.Date("createDate")).FirstOrDefault();
            return hitObj;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="flowInstanceId"></param>
        /// <param name="grantUserId"></param>
        /// <param name="givenUserId"></param>
        /// <returns></returns>
        public List<BsonDocument> HasExistObj(int flowInstanceId, int grantUserId, int givenUserId,int stepId)
        {
            return dataOp.FindAllByQueryStr("BusinessFlowUserGrant", string.Format("flowInstanceId={0}&givenUserId={1}&grantUserId={2}&status=0&stepId={3}", flowInstanceId, givenUserId, grantUserId, stepId)).ToList();
        }


        /// <summary>
        /// 查找出所有
        /// </summary>
        /// <returns></returns>
        public List<BsonDocument> FindAll()
        {
            return dataOp.FindAll("BusinessFlowUserGrant").ToList();
        }

        
        #endregion

        #region 操作
        /// <summary>
        /// 新增
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public InvokeResult Insert(BsonDocument entity, BsonDocument instance, int userId)
        {
            InvokeResult  result = new InvokeResult ();


            if (entity == null || instance == null) throw new ArgumentNullException();


            //if (HasExistObj(entity.Int("flowInstanceId"), entity.Int("grantUserId"), entity.Int("givenUserId"), entity.Int("stepId")).Count() > 0)
            //{
            //    result.Status = Status.Failed;
            //    result.Message = "不能重复转办给同一个人";
            //    return result;
            //}
            //获取需要删除的列表 将status 设置为1,当“A转给B” “B再转给c”的时候需要将”A转给B“过滤
            var query = Query.And(Query.EQ("givenUserId", entity.Text("grantUserId")), Query.EQ("stepId", entity.Text("stepId")), Query.EQ("status", "0"));

            var hitDisabledQuery = dataOp.FindAllByQuery("BusinessFlowUserGrant", query).OrderByDescending(c => c.Date("createDate"));
           
            using (TransactionScope tran = new TransactionScope())
            {
                try
                {
                    //设置无效转办
                    if (hitDisabledQuery.Count() > 0)
                    {
                     var updateBosn = new BsonDocument().Add("status", "1");
                     var tempResult = dataOp.Update("BusinessFlowUserGrant", query, updateBosn);
                     if (tempResult.Status != Status.Successful)
                     {
                         return tempResult;
                     }
                    }

                    result=dataOp.Insert("BusinessFlowUserGrant", entity);

                     var givenUserName = dataOp.FindOneByKeyVal("SysUser", "userId", entity.Text("givenUserId"));
                     BsonDocument flowTrace = new BsonDocument();
                     flowTrace.Add("flowInstanceId",instance.Int("flowInstanceId"));
                     flowTrace.Add("traceType",6);
                     flowTrace.Add("remark", entity.Text("remark"));
                     flowTrace.Add("preStepId", entity.Text("stepId"));
                     flowTrace.Add("result",string.Format("将操作权转办给了{0}", givenUserName != null ? givenUserName.Text("name") : string.Empty));
                     var traceResult = BusFlowTraceBll._(dataOp).Insert(flowTrace);
                    if (traceResult.Status == Status.Successful)
                    {

                        tran.Complete();
                       
                    }
                    else
                    {
                        result.Status = Status.Failed;
                        result.Message = "传入参数有误";
                    }
                }
                catch (NullReferenceException ex)
                {
                    result.Status = Status.Failed;
                    result.Message = "传入参数有误";
                }
                catch (Exception ex)
                {
                    result.Status = Status.Failed;
                    result.Message = "传入参数有误";
                }

            }

            return result;
        }

       


        /// <summary>
        /// 新增
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public InvokeResult Insert(List<BsonDocument> entityList, BsonDocument instance, int userId)
        {
            InvokeResult  result = new InvokeResult ();
            using (TransactionScope tran = new TransactionScope())
            {
                foreach (var entity in entityList)
                {
                    result = Insert(entity, instance, userId);
                    if (result.Status != Status.Successful)
                    {
                        return result;
                    }
                }
                tran.Complete();
            }
            return result;
        }
        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public InvokeResult Delete(int id)
        {
            InvokeResult result = new InvokeResult();

            try
            {
                var entity = this.FindById(id);

                if (entity == null) throw new ArgumentNullException();
                 result= dataOp.Delete("BusinessFlowUserGrant", string.Format("turnId={0}", id));
                
            }
            catch (Exception ex)
            {
                result.Status = Status.Failed;
                result.Message = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public InvokeResult  Update(BsonDocument entity,BsonDocument UpdateBson)
        {
            InvokeResult result = new InvokeResult();

            try
            {
                var oldEntity = this.FindById(entity.Int("turnId"));
                if (oldEntity == null) throw new ArgumentNullException();
                result = dataOp.Update(oldEntity, UpdateBson);
             }
            catch (Exception ex)
            {
                result.Status = Status.Failed;
                result.Message = ex.Message;
            }

            return result;
        }

        #endregion
    }

}
