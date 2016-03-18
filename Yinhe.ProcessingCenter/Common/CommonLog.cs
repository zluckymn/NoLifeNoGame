using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;

namespace Yinhe.ProcessingCenter
{
    /// <summary>
    /// 通用日志处理类
    /// </summary>
    public class CommonLog : LogFactory
    {
        private Logger _logger = null;

        /// <summary>
        /// 
        /// </summary>
        public CommonLog()
        {
            _logger = LogManager.GetCurrentClassLogger();
        }

        /// <summary>
        /// 
        /// </summary>
        public CommonLog(Type type)
        {
            _logger = LogManager.GetCurrentClassLogger(type);
        }

        /// <summary>
        /// 最常见的记录信息，一般用于普通输出
        /// </summary>
        /// <param name="msg"></param>
        public void Trace(string msg)
        {
            _logger.Trace(msg);
        }

        /// <summary>
        /// 调试程序
        /// </summary>
        /// <param name="msg"></param>
        public void Debug(string msg)
        {
            _logger.Debug(msg);
        }

        /// <summary>
        /// 信息类型的消息
        /// </summary>
        /// <param name="msg"></param>
        public void Info(string msg)
        {
             _logger.Info(msg);
        }

        /// <summary>
        /// 警告信息
        /// </summary>
        /// <param name="msg"></param>
        public void Warn(string msg)
        {
            _logger.Warn(msg);
        }

        /// <summary>
        /// 错误信息
        /// </summary>
        /// <param name="msg"></param>
        public void Error(string msg)
        {
            _logger.Error(msg);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        public void Fatal(string msg)
        {
             _logger.Fatal(msg);

        }

        public void LogInfo(string title, string msg)
        {
            LogManager.GetLogger(title).Info(msg);
        }
        /// <summary>
        /// Gets the log warpper.
        /// </summary>
        /// <returns></returns>
        public static CommonLog _()
        {
            return new CommonLog();
        }

    }
}

