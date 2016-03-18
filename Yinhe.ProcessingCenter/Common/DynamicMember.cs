#region --- Namespace scope ---

using System;
using System.Reflection;

#endregion

namespace Yinhe.ProcessingCenter
{
    /// <summary>
    /// 动态属性操作类
    /// </summary>
	public static class DynamicMember{
		/// <summary>
		/// 动态获取任一对象的属性值.
		/// </summary>
		/// <typeparam name="T">需要返回的参数类型</typeparam>
		/// <param name="obj">需要查看的对象</param>
		/// <param name="propertyName">对象的某属性名称</param>
		/// <returns>属性所拥有的值或对象</returns>
		public static T GetDynamicProperty< T >( this Object obj , String propertyName ){
			var type = obj.GetType();

			var result = type.InvokeMember( propertyName ,
			                                BindingFlags.GetField | BindingFlags.GetProperty ,
			                                null ,
			                                obj ,
			                                new object[]{} );

			return ( T ) result;
		}

        /// <summary>
        /// 动态获取任一对象的属性值字符串.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static string GetDynamicProperty(this Object obj, String propertyName) {
            var type = obj.GetType();

            var result = type.InvokeMember(propertyName,
                                    BindingFlags.GetField | BindingFlags.GetProperty,
                                    null,
                                    obj,
                                    new object[] { });
            return String.Format("{0}", result);
        }

        /// <summary>
        /// 动态获取任一对象的属性值.
        /// </summary>
        /// <typeparam name="T">需要返回的参数类型</typeparam>
        /// <param name="obj">需要查看的对象</param>
        /// <param name="propertyName">对象的某属性名称</param>
        /// <returns>属性所拥有的值或对象</returns>
        public static string GetDynamicPropertyString(this Object obj, String propertyName)
        {
            var type = obj.GetType();

            var result = type.InvokeMember(propertyName,
                                            BindingFlags.GetField | BindingFlags.GetProperty,
                                            null,
                                            obj,
                                            new object[] { });

            return result.ToString();
        }

        /// <summary>
        /// 动态获取任一对象的属性值.
        /// </summary>
        /// <typeparam name="T">需要返回的参数类型</typeparam>
        /// <param name="obj">需要查看的对象</param>
        /// <param name="propertyName">对象的某属性名称</param>
        /// <returns>属性所拥有的值或对象</returns>
        public static int GetDynamicPropertyInt(this Object obj, String propertyName)
        {
            var type = obj.GetType();

            var result = type.InvokeMember(propertyName,
                                            BindingFlags.GetField | BindingFlags.GetProperty,
                                            null,
                                            obj,
                                            new object[] { });

            return (int)result;
        }


		/// <summary>
		/// 动态设置一个对象的属性值.
		/// </summary>
		/// <param name="obj">需要查看的对象</param>
		/// <param name="propertyName">对象的某属性名称</param>
		/// <param name="newValue">给此属性的新数值或对象</param>
		public static void SetDynamicProperty( this Object obj , String propertyName , object newValue ){
			var type = obj.GetType();
            var result = type.GetProperty(propertyName);
            if (propertyName == "updateDate")
            {
                type.GetProperty("updateData").SetValue(obj, newValue, null);
            }
            else
            {
                type.GetProperty(propertyName).SetValue(obj, newValue, null);
            }
		}

        /// <summary>
        /// 在给定的数组中获取对象的相对应属性，并给其赋值
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="propertyNames"></param>
        /// <returns></returns>
        public static void SetPropertyValueByNames(this Object obj, string[] propertyNames, object newValue)
        {
            Type type = obj.GetType();
            PropertyInfo property = null;
            foreach (string s in propertyNames)
            {
                property = type.GetProperty(s);
                if (property != null)
                {
                    break;
                }
            }
            if (property != null)
            {
                property.SetValue(obj, newValue, null);
            }
        }
	}
}