using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yinhe.ProcessingCenter.Document
{
    /// <summary>
    /// 文件通用删除处理类
    /// </summary>
    public class FileDeleteOperation : IFileDeleteInderface
    {
        public InvokeResult DeleteFileFromFileServer(string guid)
        {
            InvokeResult result = new InvokeResult();
            try
            {

            }
            catch (Exception ex)
            {
                result.Status = Status.Failed;
                result.Message = ex.Message;
            }
            return result;
        }
    }
}
