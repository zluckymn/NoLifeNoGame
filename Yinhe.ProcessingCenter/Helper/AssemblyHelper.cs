using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;

namespace Yinhe.ProcessingCenter
{
    /// <summary>
    /// 插件处理类
    /// </summary>
    public class AssemblyHelper
    {
        /// <summary>
        /// 获取目录下的所有程序集
        /// </summary>
        /// <param name="dirPath"></param>
        /// <returns></returns>
        public static List<Assembly> GetDirAssemblies(string dirPath)
        {
            List<Assembly> allAssembly = new List<Assembly>();

            DirectoryInfo theFolder = new DirectoryInfo(dirPath);

            foreach (FileInfo file in theFolder.GetFiles())
            {
                if (file.Extension.ToLower() == ".dll")
                {
                    byte[] assemblyBytes = System.IO.File.ReadAllBytes(file.FullName);

                    if (assemblyBytes != null)
                    {
                        Assembly assembly = Assembly.Load(assemblyBytes);
                        allAssembly.Add(assembly);
                    }
                }
            }

            return allAssembly;
        }

        /// <summary>
        /// 获取包含插件目录的所有程序集
        /// </summary>
        /// <returns></returns>
        public static List<Assembly> GetAllAssemblies()
        {
            List<Assembly> allAssemblies = new List<Assembly>();

            string pluginUrl = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");

            if (Directory.Exists(pluginUrl) == true)
            {
                allAssemblies.AddRange(GetDirAssemblies(pluginUrl));     //插件目录所有dll
            }

            allAssemblies.AddRange(AppDomain.CurrentDomain.GetAssemblies());    //站点下所有dll

            return allAssemblies;
        }

        /// <summary>
        /// 获取所有插件程序集
        /// </summary>
        /// <returns></returns>
        public static List<Assembly> GetPluginAssemblies()
        {
            List<Assembly> allAssemblies = GetAllAssemblies();

            IEnumerable<Assembly> pluginAssemblies = allAssemblies.Where(a => a.GetCustomAttributes(typeof(PluginViewLocations), false).Count() > 0).AsEnumerable();    //有插件标记的程序集

            List<Assembly> distinctAssemblies = pluginAssemblies.Distinct(new DLLComparer()).ToList();

            if (allAssemblies.Where(t => t.ManifestModule.ScopeName == "Yinhe.ProcessingCenter.dll").FirstOrDefault() != null)
            {
                distinctAssemblies.Add(allAssemblies.Where(t => t.ManifestModule.ScopeName == "Yinhe.ProcessingCenter.dll").FirstOrDefault());
            }

            return distinctAssemblies;
        }

    }

    /// <summary>
    /// DLL比较器
    /// </summary>
    public class DLLComparer : IEqualityComparer<Assembly>
    {
        #region IEqualityComparer<Assembly> Members

        public bool Equals(Assembly x, Assembly y)
        {
            return x.ManifestModule.ScopeName == y.ManifestModule.ScopeName;
        }

        public int GetHashCode(Assembly obj)
        {
            return obj.ManifestModule.ScopeName.GetHashCode();
        }

        #endregion
    }
}
