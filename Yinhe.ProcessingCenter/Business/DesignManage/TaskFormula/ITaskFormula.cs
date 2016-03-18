using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

///<summary>
///计划任务公式计算
///</summary>
namespace Yinhe.ProcessingCenter.DesignManage.TaskFormula
{
    /// <summary>
    /// 公式计算接口
    /// </summary>
    public interface ITaskFormula
    {
        /// <summary>
        /// 公式计算接口
        /// </summary>
        /// <param name="plItemId"></param>
        /// <param name="responseTitl"></param>
        string AnalysisFormula(int taskId, string formulaParam);
    }

    /// <summary>
    /// 公式工厂
    /// </summary>
    public class TaskFormulaFactory
    {
        private static TaskFormulaFactory _instance = new TaskFormulaFactory();
        /// <summary>
        /// 返回工厂实例
        /// </summary>
        public static TaskFormulaFactory Instance
        {
            get { return _instance; }
        }

        /// <summary>
        /// 创建具体公式解析类
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public ITaskFormula Create(string Name)
        {
            ITaskFormula myTaskFormula = null;
            try
            {
                Type type = Type.GetType(Name, true);
                myTaskFormula = (ITaskFormula)Activator.CreateInstance(type);
            }
            catch (Exception ex)
            {

            }
            return myTaskFormula;
        }
    }
}
