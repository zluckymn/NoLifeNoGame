using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using Yinhe.ProcessingCenter.DataRule;
using MongoDB.Bson;
using System.Threading;
namespace Yinhe.ProcessingCenter.BusinessFlow
{
    /// <summary>
    /// 事务执行接口，不同的事务对象都继承该接口
    /// </summary>
    public interface IExecuteTran
    {
        /// <summary>
        /// 执行事务
        /// </summary>
        /// <param name="tran"></param>
        /// <param name="UserId"></param>
        /// <param name="instance"></param>
        void ExecuteTran(int UserId, BsonDocument instance, BsonDocument stepTran);
    }

    /// <summary>
    /// 事务对象工厂
    /// </summary>
    public class TranFactory
    {
        private static TranFactory _instance = new TranFactory();
        /// <summary>
        /// 返回工厂实例
        /// </summary>
        public static TranFactory Instance
        {
            get { return _instance; }
        }

        /// <summary>
        /// 创建具体事务对象
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public IExecuteTran Create(string Name)
        {
            IExecuteTran myExecuteTran = null;
            try
            {
                Type type = Type.GetType(Name, true);
                myExecuteTran = (IExecuteTran)Activator.CreateInstance(type);
            }
            catch (TypeLoadException e)
            {

            }
            return myExecuteTran;
        }

        /// <summary>
        /// 新建线程，批量执行事务
        /// </summary>
        /// <param name="tranList"></param>
        /// <param name="userId"></param>
        /// <param name="instance"></param>
        public void ExecuteTran(List<BsonDocument> tranList, int userId, BsonDocument instance)
        {
            Thread thread = new Thread(new ThreadStart(delegate()
            {
                foreach (var stepTran in tranList)
                {
                    try
                    {
                        var transactionStore = stepTran.SourceBsonField("transactionId", "tranClass");
                        if (!String.IsNullOrEmpty(transactionStore))
                        {
                            var tranClass = Create(transactionStore);
                            if (tranClass != null)
                                tranClass.ExecuteTran(userId, instance, stepTran);
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }));
            thread.Start();
        }

        /// <summary>
        ///  立即批量执行事务
        /// </summary>
        /// <param name="tranList"></param>
        /// <param name="userId"></param>
        /// <param name="instance"></param>
        public void ExecuteTranImmediately(List<BsonDocument> tranList, int userId, BsonDocument instance)
        {

            foreach (var stepTran in tranList)
            {
                try
                {
                    var transactionStore = stepTran.SourceBsonField("transactionId", "tranClass");
                    if (!String.IsNullOrEmpty(transactionStore))
                    {
                        var tranClass = Create(transactionStore);
                        if (tranClass != null)
                            tranClass.ExecuteTran(userId, instance, stepTran);
                    }
                }
                catch (Exception ex)
                {
                }
            }
        }
      

    }

}
