using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Yinhe.ProcessingCenter
{
    /// <summary>
    /// 计算处理类
    /// </summary>
    public class CalculateHelper
    {
        private static Dictionary<string, int> _operatorLevel;

        /// <summary>
        /// 获取计算值
        /// </summary>
        /// <param name="Expression"></param>
        /// <returns></returns>
        public static double GetCalculateResult(string Expression)
        {
            string source = ConvertToRPN(InsertBlank(Expression));
            return GetResult(source);
        }

        /// <summary>
        /// 为表达式插入空格
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string InsertBlank(string source)
        {
            StringBuilder sb = new StringBuilder();
            var list = source.ToCharArray();
            foreach (var temp in list)
            {
                if (OperatorLevel.ContainsKey(temp.ToString()))
                {
                    sb.Append(" ");
                    sb.Append(temp.ToString());
                    sb.Append(" ");
                }
                else
                {
                    sb.Append(temp);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// 转化为逆波兰表达式
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string ConvertToRPN(string source)
        {
            StringBuilder result = new StringBuilder();
            Stack<string> stack = new Stack<string>();
            string[] list = source.Split(' ');
            for (int i = 0; i < list.Length; i++)
            {
                string current = list[i];
                if (Regex.IsMatch(current, "^([0-9]{1,}){1}"))
                {
                    result.Append(current + " ");
                }
                else if (OperatorLevel.ContainsKey(current))
                {
                    if (stack.Count > 0)
                    {
                        var prev = stack.Peek();
                        if (prev == "(")
                        {
                            stack.Push(current);
                            continue;
                        }
                        if (current == "(")
                        {
                            stack.Push(current);
                            continue;
                        }
                        if (current == ")")
                        {
                            while (stack.Count > 0 && stack.Peek() != "(")
                            {
                                result.Append(stack.Pop() + " ");
                            }
                            //Pop the "("  
                            stack.Pop();
                            continue;
                        }
                        if (OperatorLevel[current] < OperatorLevel[prev])
                        {
                            while (stack.Count > 0)
                            {
                                var top = stack.Pop();
                                if (top != "(" &&
                                    top != ")")
                                {
                                    result.Append(top + " ");
                                }
                                else
                                {
                                    break;
                                }
                            }
                            stack.Push(current);
                        }
                        else
                        {
                            stack.Push(current);
                        }
                    }
                    else
                    {
                        stack.Push(current);
                    }
                }
            }
            if (stack.Count > 0)
            {
                while (stack.Count > 0)
                {
                    var top = stack.Pop();
                    if (top != "(" && top != ")")
                    {
                        result.Append(top + " ");
                    }
                }
            }
            return result.ToString();
        }

        /// <summary>
        /// 根据逆波兰表达式获取结构
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static double GetResult(string source)
        {
            Stack<string> stack = new Stack<string>();
            var list = source.Split(' ');
            for (int i = 0; i < list.Length; i++)
            {
                string current = list[i];
                if (Regex.IsMatch(current, "^([0-9]{1,}){1}"))             // "^(-?[0-9]*[.]*[0-9]{0,3})$"))
                {
                    stack.Push(current);
                }
                else if (OperatorLevel.ContainsKey(current))
                {
                    double right = double.Parse(stack.Pop());
                    double left = double.Parse(stack.Pop());
                    stack.Push(GetValue(left, right, current[0]).ToString());
                }
            }
            return double.Parse(stack.Pop());
        }

        /// <summary>
        /// 简单四则运算
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="_operator"></param>
        /// <returns></returns>
        public static double GetValue(double left, double right, char _operator)
        {
            switch (_operator)
            {
                case '+':
                    return left + right;
                case '-':
                    return left - right;
                case '*':
                    return left * right;
                case '/':
                    return left / right;
            }
            return 0;
        }

        /// <summary>
        /// 运算符优先级
        /// </summary>
        public static Dictionary<string, int> OperatorLevel
        {
            get
            {
                if (_operatorLevel == null)
                {
                    _operatorLevel = new Dictionary<string, int>();
                    _operatorLevel.Add("+", 0);
                    _operatorLevel.Add("-", 0);
                    _operatorLevel.Add("(", 1);
                    _operatorLevel.Add("*", 1);
                    _operatorLevel.Add("/", 1);
                    _operatorLevel.Add(")", 0);
                }
                return _operatorLevel;
            }
        }
    }
}