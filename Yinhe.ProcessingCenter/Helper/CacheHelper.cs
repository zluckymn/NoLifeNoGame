using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Caching;

namespace Yinhe.ProcessingCenter
{
    /// <summary>
    /// 缓存处理类
    /// </summary>
    public class CacheHelper
    {
        /// <summary>
        /// 添加缓存
        /// </summary>
        /// <param name="key"></param>
        /// <param name="obj"></param>
        /// <param name="cachedep"></param>
        /// <param name="overDueTime"></param>
        public static void SetCache(string key, object obj, CacheDependency cachedep, DateTime overDueTime)
        {
            if (HttpRuntime.Cache[key] == null)
            {
                HttpRuntime.Cache.Add(key, obj, cachedep, overDueTime, Cache.NoSlidingExpiration, CacheItemPriority.High, null);
            }
            else
            {
                HttpRuntime.Cache.Remove(key);
                HttpRuntime.Cache.Add(key, obj, cachedep, overDueTime, Cache.NoSlidingExpiration, CacheItemPriority.High, null);
            }
        }

        /// <summary>
        /// 获取缓存
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static Object GetCache(string key)
        {
            return HttpRuntime.Cache[key];
        }

        /// <summary>
        /// 清除缓存
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static void RemoveCache(string key)
        {
            if (HttpRuntime.Cache[key] != null)
            {
                HttpRuntime.Cache.Remove(key);
            }
        }
    }
}
