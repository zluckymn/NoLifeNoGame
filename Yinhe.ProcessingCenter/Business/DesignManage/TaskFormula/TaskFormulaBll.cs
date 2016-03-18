using System;
using System.Linq;
using System.Transactions;
using Yinhoo.Framework.Configuration;
using System.Collections.Generic;
using MongoDB.Bson;
using Yinhe.ProcessingCenter.DesignManage.TaskFormula;
using Yinhe.ProcessingCenter;

namespace Yinhe.ProcessingCenter.DesignManage.TaskFormula
{
    /// <summary>
    /// 任务公式
    /// </summary>
    public class TaskFormulaBll
    {
        /// <summary>
        /// 类私有变量
        /// </summary>
        private DataOperation dataOp = new DataOperation();

        /// <summary>
        /// 封闭当前默认构造函数
        /// </summary>
        private TaskFormulaBll()
        {
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static TaskFormulaBll _()
        {
            return new TaskFormulaBll();
        }

        /// <summary>
        /// 检查是否有闭环
        /// </summary>
        /// <param name="formulaObj"></param>
        /// <returns></returns>
        public bool IsClosedLoop(FormulaObject formulaObj)
        {
            List<string> strOperateList = new List<string>();
            CheckClosedLoop(formulaObj.OperDateType.ToString() + formulaObj.TaskId + ",", ref strOperateList);
            if (strOperateList.Contains(formulaObj.ReadDateType.ToString() + formulaObj.RelTaskId))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// 递归检测循环
        /// </summary>
        /// <param name="strOperate"></param>
        /// <param name="strOperateList"></param>
        private void CheckClosedLoop(string strOperate, ref List<string> strOperateList)
        {
            List<BsonDocument> formulaList = dataOp.FindAll("XH_DesignManage_TaskFormula").Where(t => t.String("relTaskId").Contains(strOperate)).ToList();

            foreach (var formula in formulaList)
            {
                if (!String.IsNullOrEmpty(formula.String("formulaParam")))
                {
                    FormulaObject formulaObj = new FormulaObject(formula.String("formulaParam"));
                    formulaObj.ConvertExp();
                    if (!String.IsNullOrEmpty(formulaObj.OperDateType) && formulaObj.TaskId != 0)
                    {
                        strOperateList.Add(formulaObj.OperDateType + formulaObj.TaskId);
                        CheckClosedLoop(formulaObj.OperDateType.ToString() + formulaObj.TaskId + ",", ref strOperateList);
                    }
                }
            }
        }
    }
}



