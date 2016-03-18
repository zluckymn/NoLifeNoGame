using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using Yinhe.ProcessingCenter.DataRule;
using MongoDB.Bson;
namespace Yinhe.ProcessingCenter.BusinessFlow
{
    /// <summary>
    /// 流程传阅人员处理类
    /// </summary>
    public class BusinessFlowCirculationBll
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
        private BusinessFlowCirculationBll() 
        {
            dataOp = new DataOperation();
        }

        private BusinessFlowCirculationBll(DataOperation _dataOp)
        {
            dataOp = _dataOp;
        }

        public static BusinessFlowCirculationBll _()
        {
            return new BusinessFlowCirculationBll();
        }

        public static BusinessFlowCirculationBll _(DataOperation _dataOp)
        {
            return new BusinessFlowCirculationBll(_dataOp);
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
          return dataOp.FindAllByQueryStr("BusinessFlowCirculation", string.Format("circulatId={0}", id, 0)).FirstOrDefault();
        }


        /// <summary>
        /// 根据Id查询
        /// </summary>
        /// <param name="flowInstanceId"></param>
        /// <returns></returns>
        public List<BsonDocument> FindByFlowInstanceId(int flowInstanceId)
        {
        return dataOp.FindAllByQueryStr("BusinessFlowCirculation", string.Format("flowInstanceId={0}", flowInstanceId, 0)).ToList();
        }


        /// <summary>
        /// 获取当前是否被传阅,没过滤status 应为可能被过滤多次
        /// </summary>
        /// <param name="flowInstanceId"></param>
        /// <param name="givenUserId"></param>
        /// <returns></returns>
        public List<BsonDocument> HasTurnRightQuery(int flowInstanceId, int grantUserId)
        {
            return dataOp.FindAllByQueryStr("BusinessFlowCirculation", string.Format("flowInstanceId={0}&grantUserId={1}", flowInstanceId, grantUserId)).ToList();
     
        }

      

        /// <summary>
        /// 获取当前当前用户被给予的权限列表
        /// </summary>
        /// <param name="flowInstanceId"></param>
        /// <param name="givenUserId"></param>
        /// <returns></returns>
        public List<BsonDocument> FindByFlowInstanceId(int flowInstanceId, int givenUserId,int stepId)
        {
            return dataOp.FindAllByQueryStr("BusinessFlowCirculation", string.Format("flowInstanceId={0}&givenUserId={1}&stepId={2}", flowInstanceId, givenUserId,stepId)).ToList();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="flowInstanceId"></param>
        /// <param name="grantUserId"></param>
        /// <param name="givenUserId"></param>
        /// <returns></returns>
        public List<BsonDocument> HasExistObj(int flowInstanceId, int stepId,int grantUserId, int givenUserId)
        {
            return dataOp.FindAllByQueryStr("BusinessFlowCirculation", string.Format("flowInstanceId={0}&stepId={1}&givenUserId={2}&grantUserId={3}", flowInstanceId,stepId, givenUserId, grantUserId)).ToList();
        }


        /// <summary>
        /// 查找出所有
        /// </summary>
        /// <returns></returns>
        public List<BsonDocument> FindAll()
        {
            return dataOp.FindAll("BusinessFlowCirculation").ToList();
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


            if (HasExistObj(entity.Int("flowInstanceId"),entity.Int("stepId"), entity.Int("grantUserId"), entity.Int("givenUserId")).Count() > 0)
            {
                result.Status = Status.Failed;
                result.Message = "不能重复传阅给同一个人";
                return result;
            }
         
            var hitDisabledQuery = FindAll().Where(c => c.Int("givenUserId") == entity.Int("grantUserId")).OrderByDescending(c => c.Date("createDate"));
           
            using (TransactionScope tran = new TransactionScope())
            {
                try
                {
                   

                    result=dataOp.Insert("BusinessFlowCirculation", entity);

                    var givenUserName = dataOp.FindOneByKeyVal("SysUser", "userId", entity.Text("givenUserId"));
                     BsonDocument flowTrace = new BsonDocument();
                     flowTrace.Add("flowInstanceId",instance.Int("flowInstanceId"));
                     flowTrace.Add("traceType",8);
                     flowTrace.Add("remark", entity.Text("remark"));
                     flowTrace.Add("preStepId", instance.Int("stepId"));
                     flowTrace.Add("result",string.Format("将操作权传阅给了{0}", givenUserName != null ? givenUserName.Text("name") : string.Empty));
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
                 result= dataOp.Delete("BusinessFlowCirculation", string.Format("circulatId={0}", id));
                
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
                var oldEntity = this.FindById(entity.Int("circulatId"));
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



        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public InvokeResult CirculateComplete(int flowInstanceId,int userId,int stepId,string remark)
        {
            InvokeResult result = new InvokeResult();

            try
            {
                var updateQuery = string.Format("flowInstanceId={0}&givenUserId={1}&status=0", flowInstanceId, userId);
                var bsonList = dataOp.FindAllByQueryStr("BusinessFlowCirculation", updateQuery).ToList();
                if (bsonList.Count() > 0)
                {
                  result = dataOp.QuickUpdate("BusinessFlowCirculation", bsonList, "status=1");
                }
                if (result.Status == Status.Successful)
                {
                    BsonDocument flowTrace = new BsonDocument();
                    flowTrace.Add("flowInstanceId", flowInstanceId);
                    flowTrace.Add("traceType", 8);
                    flowTrace.Add("remark",remark);
                    flowTrace.Add("preStepId", stepId);
                    flowTrace.Add("result", string.Format("进行了阅毕确认"));
                    var traceResult = BusFlowTraceBll._(dataOp).Insert(flowTrace);
                }
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
