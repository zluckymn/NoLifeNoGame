using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yinhe.ProcessingCenter.Document
{
    /// <summary>
    /// 文件删除接口 日后进行扩展  删除业务数据的同时删除文件服务器上的文件
    /// </summary>
    interface IFileDeleteInderface
    {
        InvokeResult DeleteFileFromFileServer(string guid);
    }
}
