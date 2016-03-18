using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Reflection;
using System.IO;

namespace Yinhe.WebHost
{
    public class AssemblyResourceVirtualFile : System.Web.Hosting.VirtualFile
    {
        /// <summary>
        /// 路径信息,构造后具有虚拟路径
        /// </summary>
        private string path;

        /// <summary>
        /// 程序集中的虚拟文件信息
        /// </summary>
        /// <param name="virtualPath"></param>
        public AssemblyResourceVirtualFile(string virtualPath)
            : base(virtualPath)
        {
            path = VirtualPathUtility.ToAppRelative(virtualPath);
        }

        /// <summary>
        /// 重写System.Web.Hosting.VirtualFile.Open,在派生类中重写时，返回到虚拟资源的只读流
        /// </summary>
        /// <returns></returns>
        public override Stream Open()
        {
            string[] parts = path.Split('/');
            string assemblyName = parts[2];
            string resourceName = parts[3];

            assemblyName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins", assemblyName); 
            byte[] assemblyBytes = File.ReadAllBytes(assemblyName);
            Assembly assembly = Assembly.Load(assemblyBytes);

            if (assembly != null)
                return assembly.GetManifestResourceStream(resourceName);

            return null;
        }
    }
}