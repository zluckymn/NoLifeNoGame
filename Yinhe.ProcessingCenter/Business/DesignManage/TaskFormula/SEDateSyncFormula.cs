using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using Yinhe.ProcessingCenter.DataRule;
using MongoDB.Driver.Builders;

namespace Yinhe.ProcessingCenter.DesignManage.TaskFormula
{
    /// <summary>
    /// 同步任务的开始时间和结束时间的公式类
    /// </summary>
    public class SEDateSyncFormula : ITaskFormula
    {
        #region 常量
        private static readonly DateTime MinDate = new DateTime(1900, 1, 1);
        private List<BsonDocument> taskList = new List<BsonDocument>();
        private List<BsonDocument> taskFormualList = new List<BsonDocument>();
        private List<StorageData> updateBsonStorage = new List<StorageData>();
        #endregion

        #region 构造函数
        /// <summary>
        /// 类私有变量
        /// </summary>
        private DataOperation dataOp = new DataOperation();

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public SEDateSyncFormula()
        {
           
        }
        public SEDateSyncFormula(List<BsonDocument> _taskList)
        {
            taskList = _taskList;
            taskFormualList = dataOp.FindAllByKeyValList("XH_DesignManage_TaskFormula", "taskId", _taskList.Select(c => c.Text("taskId")).ToList()).ToList();
        }
        

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static SEDateSyncFormula _()
        {
            return new SEDateSyncFormula();
        }

        #endregion

        #region 解析接口
        /// <summary>
        /// 解析公式
        /// </summary>
        /// <param name="taskId"></param>
        /// <param name="relTaskId"></param>
        /// <param name="formulaParam"></param>
        /// <returns></returns>
        public string AnalysisFormula(int taskId, string formulaParam)
        {
            try
            {
                var strOperate = "";
                if (!String.IsNullOrEmpty(formulaParam))
                {
                
                    var desTask = dataOp.FindOneByKeyVal("XH_DesignManage_Task","taskId",taskId.ToString());

                    if (desTask != null)
                    {
                        var formulaObj = new FormulaObject(formulaParam.ToUpper());
                        formulaObj.ConvertExp();
                        double value = formulaObj.CalcValue();
                        DateTime tempDateTime = formulaObj.date.HasValue ? formulaObj.date.Value : new DateTime(1900, 1, 1);
                        tempDateTime = tempDateTime.AddDays(value);
                        switch (formulaObj.OperDateType.ToLower())
                        {
                            case "s":
                                dataOp.Update("XH_DesignManage_Task", "db.XH_DesignManage_Task.findOne({'taskId':'" + taskId + "'})", "curStartData=" + tempDateTime.ToString("yyyy-MM-dd"));
                                break;
                            case "e":
                                dataOp.Update("XH_DesignManage_Task", "db.XH_DesignManage_Task.findOne({'taskId':'" + taskId + "'})", "curEndData=" + tempDateTime.ToString("yyyy-MM-dd"));
                                break;
                        }
                        strOperate = formulaObj.OperDateType;
                    }
                }
                return strOperate;
            }
            catch(Exception ex)
            {
                return "";
            }
        }


        #region 批量设置时间公式 2013.10.21
        /// <summary>
        /// 开始批量添加
        /// </summary>
        /// <param name="bllFormula"></param>
        /// <param name="strOperate"></param>
        /// <param name="projId"></param>
        public void BeginSyncFormulaDate(string strOperate, string projId)
        {
            List<BsonDocument> formulaList = taskFormualList.Where(m => m.String("relTaskId").Contains(strOperate)).ToList();
            foreach (var formula in formulaList)
            {
                if (!String.IsNullOrEmpty(formula.String("formulaClass")))
                {
                       var strResult = PatchAnalysisFormula(formula.Int("taskId"), formula.String("formulaParam"));
                        if (!String.IsNullOrEmpty(strResult))
                        {
                            BeginSyncFormulaDate(strResult + formula.String("taskId"), projId);
                        }
                }
            }
        }


        /// <summary>
        /// 解析公式
        /// </summary>
        /// <param name="taskId"></param>
        /// <param name="relTaskId"></param>
        /// <param name="formulaParam"></param>
        /// <returns></returns>
        public string PatchAnalysisFormula(int taskId, string formulaParam)
        {
            try
            {
                var strOperate = "";
                if (!String.IsNullOrEmpty(formulaParam))
                {
                    //var desTask = dataOp.FindOneByKeyVal("XH_DesignManage_Task", "taskId", taskId.ToString());
                    var desTask = taskList.Where(c => c.Int("taskId") == taskId).FirstOrDefault();
                    if (desTask != null)
                    {
                        var formulaObj = new FormulaObject(formulaParam.ToUpper());
                        formulaObj.taskList = taskList;
                        formulaObj.ConvertExp();
                        double value = formulaObj.CalcValue();
                        DateTime tempDateTime = formulaObj.date.HasValue ? formulaObj.date.Value : new DateTime(1900, 1, 1);
                        tempDateTime = tempDateTime.AddDays(value);
                        switch (formulaObj.OperDateType.ToLower())
                        {
                            case "s":
                               // dataOp.Update("XH_DesignManage_Task", "db.XH_DesignManage_Task.findOne({'taskId':'" + taskId + "'})", "curStartData=" + tempDateTime.ToString("yyyy-MM-dd"));
                                desTask.Set("curStartData", tempDateTime.ToString("yyyy-MM-dd"));
                                var updateValueBson = new BsonDocument();
                                updateValueBson.Add("curStartData", tempDateTime.ToString("yyyy-MM-dd"));
                                var udpateStartDate = new StorageData() { Document = updateValueBson, Type = StorageType.Update, Name = "XH_DesignManage_Task", Query = Query.EQ("taskId", taskId.ToString()) };
                                updateBsonStorage.Add(udpateStartDate);
                                break;
                            case "e":
                                //  dataOp.Update("XH_DesignManage_Task", "db.XH_DesignManage_Task.findOne({'taskId':'" + taskId + "'})", "curEndData=" + tempDateTime.ToString("yyyy-MM-dd"));
                                desTask.Set("curEndData",tempDateTime.ToString("yyyy-MM-dd"));
                                var updateBson = new BsonDocument();
                                updateBson.Add("curEndData", tempDateTime.ToString("yyyy-MM-dd"));
                                var udpateEndDate = new StorageData() { Document = updateBson, Type = StorageType.Update, Name = "XH_DesignManage_Task", Query = Query.EQ("taskId", taskId.ToString()) };
                                updateBsonStorage.Add(udpateEndDate);
                                break;
                        }
                        strOperate = formulaObj.OperDateType;
                    }
                }
                return strOperate;
            }
            catch (Exception ex)
            {
                return "";
            }
        }

 
        /// <summary>
        /// 提交修改更新
        /// </summary>
        /// <returns></returns>
        public InvokeResult  ChangeSubmit()
        {
            var result = new InvokeResult() { Status=Status.Successful};
            if (updateBsonStorage.Count() > 0)
            {
                result = dataOp.BatchSaveStorageData(updateBsonStorage);
                //MongoOperation mongoOp = new MongoOperation();
            }
            return result;
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
           // List<BsonDocument> formulaList = dataOp.FindAll("XH_DesignManage_TaskFormula").Where(t => t.String("relTaskId").Contains(strOperate)).ToList();
            List<BsonDocument> formulaList = taskFormualList.Where(m => m.String("relTaskId").Contains(strOperate)).ToList();
            foreach (var formula in formulaList)
            {
                if (!String.IsNullOrEmpty(formula.String("formulaParam")))
                {
                    FormulaObject formulaObj = new FormulaObject(formula.String("formulaParam"));
                    formulaObj.taskList = this.taskList;
                    formulaObj.ConvertExp();
                    if (!String.IsNullOrEmpty(formulaObj.OperDateType) && formulaObj.TaskId != 0)
                    {
                        strOperateList.Add(formulaObj.OperDateType + formulaObj.TaskId);
                        CheckClosedLoop(formulaObj.OperDateType.ToString() + formulaObj.TaskId + ",", ref strOperateList);
                    }
                }
            }
        }

        #endregion
        #endregion
    }

    #region 公式对象
    /// <summary>
    /// 公式对象
    /// </summary>
    public class FormulaObject
    {
        #region 字段
        /// <summary>
        /// 关联的日期
        /// </summary>
        public DateTime? date
        {
            get;
            set;
        }

        /// <summary>
        /// 操作的日期类型
        /// </summary>
        public string OperDateType
        {
            get;
            set;
        }

        /// <summary>
        /// 获取的日期类型
        /// </summary>
        public string ReadDateType
        {
            get;
            set;
        }

        /// <summary>
        /// 被关联的任务Id
        /// </summary>
        public string StrRelTaskId
        {
            get;
            set;
        }

        /// <summary>
        /// 表达式
        /// </summary>
        public string Expression
        {
            get;
            set;
        }

        /// <summary>
        /// 转化后的表达式
        /// </summary>
        public string ConvertedExpression
        {
            get;
            set;
        }

        public int TaskId
        {
            get;
            set;
        }

        public int RelTaskId
        {
            get;
            set;
        }

        public int PeriodTaskId
        {
            get;
            set;
        }
        private List<BsonDocument> _taskList = null;

        public  List<BsonDocument> taskList{
            get
            {
            return _taskList;
            }
                
            set{

                _taskList = value;
            }
        }

        private DataOperation dataOp = new DataOperation();
        #endregion

        #region 构造函数
        public FormulaObject(string exp)
        {
            this.Expression = exp.ToUpper();
            if (!IsWright(this.Expression))
            {
                throw new Exception("公式格式不正确");
            }
        }
        #endregion

        #region 解析表达式
        /// <summary>
        /// 转化表达式
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public void ConvertExp()
        {
            string expression = this.Expression;
            var equalIndex = expression.IndexOf("=");
            var strOperDateType = expression.Substring(0, 1);
            this.OperDateType = strOperDateType;
            var strTaskId = expression.Substring(1, equalIndex - 1);
            int taskId = 0;
            if (Int32.TryParse(strTaskId,out taskId))
            {
                this.TaskId = taskId;
            }
            expression = expression.Substring(equalIndex + 1);
            Stack<char> operators = new Stack<char>();
            StringBuilder result = new StringBuilder();
            string tempOpnd = "";
            for (int i = 0; i < expression.Length; i++)
            {
                char ch = expression[i];
                if (String.IsNullOrEmpty(ch.ToString().Trim())) continue;
                switch (ch)
                {
                    case '+':
                    case '-':
                        ConvertOpnd(ref tempOpnd, ref result);
                        while (operators.Count > 0)
                        {
                            char c = operators.Pop();   //pop Operator
                            if (c == '(')
                            {
                                operators.Push(c);      //push Operator
                                break;
                            }
                            else
                            {
                                result.Append(c);
                            }
                        }
                        operators.Push(ch);
                        break;
                    case '*':
                    case '/':
                        ConvertOpnd(ref tempOpnd, ref result);
                        while (operators.Count > 0)
                        {
                            char c = operators.Pop();
                            if (c == '(')
                            {
                                operators.Push(c);
                                break;
                            }
                            else
                            {
                                if (c == '+' || c == '-')
                                {
                                    operators.Push(c);
                                    break;
                                }
                                else
                                {
                                    result.Append(c);
                                }
                            }
                        }
                        operators.Push(ch);
                        break;
                    case '(':
                        ConvertOpnd(ref tempOpnd, ref result);
                        operators.Push(ch);
                        break;
                    case ')':
                        ConvertOpnd(ref tempOpnd, ref result);
                        while (operators.Count > 0)
                        {
                            char c = operators.Pop();
                            if (c == '(')
                            {
                                break;
                            }
                            else
                            {
                                result.Append(c);
                            }
                        }
                        break;
                    default:
                        tempOpnd += expression[i];
                        break;
                }
            }
            if (!String.IsNullOrEmpty(tempOpnd))
            {
                ConvertOpnd(ref tempOpnd, ref result);
            }
            while (operators.Count > 0)
            {
                result.Append(operators.Pop()); //pop All Operator
            }
            this.ConvertedExpression = result.ToString();
        }

        /// <summary>
        /// 转换操作数
        /// </summary>
        /// <param name="strOpnd"></param>
        /// <param name="result"></param>
        public void ConvertOpnd(ref string strOpnd, ref StringBuilder result)
        {
            if (!String.IsNullOrEmpty(strOpnd))
            {
                result.Append(" ");
                if (IsDouble(strOpnd))
                {
                    var opnd = Double.Parse(strOpnd);
                    result.Append(opnd);
                }
                else if (strOpnd.ToLower().StartsWith("s") || strOpnd.ToLower().StartsWith("e"))
                {
                    var readDateType = strOpnd.Substring(0, 1);
                    this.ReadDateType = readDateType;
                    var relTaskId = strOpnd.Substring(1);
                    var taskId = 0;
                    if (Int32.TryParse(relTaskId, out taskId))
                    {
                        BsonDocument  oriTask=null;
                        if (_taskList != null)
                        {
                         oriTask = _taskList.Where(c => c.Int("taskId") == taskId).FirstOrDefault();
                        }else
                        {
                         oriTask = dataOp.FindOneByKeyVal("XH_DesignManage_Task", "taskId", taskId.ToString());
                        }
                        if (oriTask != null)
                        {
                            if (strOpnd.ToLower().StartsWith("s") && oriTask.Date("curStartData") != DateTime.MinValue)
                            {
                                date = oriTask.Date("curStartData");
                            }
                            if (strOpnd.ToLower().StartsWith("e") && oriTask.Date("curEndData") != DateTime.MinValue)
                            {
                                date = oriTask.Date("curEndData");
                            }
                            StrRelTaskId += strOpnd + ",";
                            this.RelTaskId = taskId;
                        }
                    }
                    result.Append(0);
                }
                else if (strOpnd.ToLower().StartsWith("g"))
                {
                    var relTaskId = strOpnd.Substring(1);

                    var taskId = 0;
                    if (Int32.TryParse(relTaskId, out taskId))
                    {
                        BsonDocument oriTask = null;
                        if (_taskList != null)
                        {
                            oriTask = _taskList.Where(c => c.Int("taskId") == taskId).FirstOrDefault();
                        }
                        else
                        {
                            oriTask = dataOp.FindOneByKeyVal("XH_DesignManage_Task", "taskId", taskId.ToString());
                        }
                        if (oriTask != null)
                        {
                            result.Append(oriTask.String("period", "0"));

                            StrRelTaskId += "G" + taskId + ",";
                            this.PeriodTaskId = taskId;
                        }
                    }
                }
                strOpnd = "";
            }
        }

        /// <summary>
        /// 表达式求值
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public double CalcValue()
        {
            Stack<double> results = new Stack<double>();
            double x, y;
            for (int i = 0; i < this.ConvertedExpression.Length; i++)
            {
                char ch = this.ConvertedExpression[i];
                if (String.IsNullOrEmpty(ch.ToString().Trim())) continue;
                switch (ch)
                {
                    case '+':
                        y = results.Pop();
                        x = results.Pop();
                        results.Push(x + y);
                        break;
                    case '-':
                        y = results.Pop();
                        x = results.Pop();
                        results.Push(x - y);
                        break;
                    case '*':
                        y = results.Pop();
                        x = results.Pop();
                        results.Push(x * y);
                        break;
                    case '/':
                        y = results.Pop();
                        x = results.Pop();
                        results.Push(x / y);
                        break;
                    default:
                        int pos = i;
                        StringBuilder operand = new StringBuilder();
                        do
                        {
                            operand.Append(this.ConvertedExpression[pos]);
                            pos++;
                        } while (pos < this.ConvertedExpression.Length && (char.IsDigit(this.ConvertedExpression[pos]) || this.ConvertedExpression[pos] == '.'));
                        i = --pos;
                        results.Push(double.Parse(operand.ToString()));
                        break;
                }
            }
            return results.Peek();
        }

        #endregion

        #region 辅助函数
        /// <summary>
        /// 判断是否为整数
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsDouble(string str)
        {
            double result = 0;
            return double.TryParse(str, out result);
        }

        /// <summary>
        /// 检验公式格式是否正确
        /// </summary>
        public static bool IsWright(string expression)
        {
            return true;
        }

        /// <summary>
        /// 拼接关联任务Id
        /// </summary>
        /// <param name="idList"></param>
        /// <returns></returns>
        public static string GetRelTaskIdStr(List<int> idList)
        {
            var str = new StringBuilder();
            foreach (var id in idList)
            {
                str.Append("," + id);
            }
            if (!String.IsNullOrEmpty(str.ToString()))
            {
                str.Append(",");
            }
            return str.ToString();
        }

        /// <summary>
        /// 转换表达式（导入模板使用）
        /// </summary>
        /// <param name="taskIdDic"></param>
        /// <param name="formulaParam"></param>
        /// <returns></returns>
        public void ConvertFormulaParam(Dictionary<int, int> taskIdDic, ref string convertedExp, ref string convertedRelTaskId)
        {
            convertedExp = this.Expression;
            convertedRelTaskId = "";
            var newTaskId=0;
            var newRelTaskId=0;
            var newPeriodTaskId=0;
            if (this.TaskId != 0 && taskIdDic.ContainsKey(this.TaskId))
            {
                newTaskId = taskIdDic[this.TaskId];
                if (newTaskId != 0)
                {
                    convertedExp = convertedExp.Replace(this.OperDateType + this.TaskId, this.OperDateType + newTaskId);
                }
            }
            if (this.RelTaskId != 0 && taskIdDic.ContainsKey(this.RelTaskId))
            {
                newRelTaskId = taskIdDic[this.RelTaskId];
                if (newRelTaskId != 0)
                {
                    convertedExp = convertedExp.Replace(this.ReadDateType + this.RelTaskId, this.ReadDateType + newRelTaskId);
                    convertedRelTaskId += this.ReadDateType + newRelTaskId + ",";
                }

            }
            if (this.PeriodTaskId != 0 && taskIdDic.ContainsKey(this.PeriodTaskId))
            {
                newPeriodTaskId = taskIdDic[this.PeriodTaskId];
                if (newPeriodTaskId != 0)
                {
                    convertedExp = convertedExp.Replace("G" + this.PeriodTaskId, "G" + newPeriodTaskId);
                    convertedRelTaskId += "G" + newPeriodTaskId + ",";
                }
            }
        }
        #endregion
    }
    #endregion
}
