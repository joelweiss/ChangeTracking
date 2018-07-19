using Castle.DynamicProxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ChangeTracking
{
    public static class Utils
    {
        public static bool IsSetter(this MethodInfo method)
        {
            return method.IsSpecialName && method.Name.StartsWith("set_", StringComparison.Ordinal);
        }

        public static bool IsGetter(this MethodInfo method)
        {
            return method.IsSpecialName && method.Name.StartsWith("get_", StringComparison.Ordinal);
        }

        public static bool IsProperty(this MethodInfo method)
        {
            return method.IsSpecialName && (method.Name.StartsWith("get_", StringComparison.Ordinal) || method.Name.StartsWith("set_", StringComparison.Ordinal));
        }

        private static readonly int prefixLength = "set_".Length;
        public static string PropertyName(this MethodInfo method)
        {
            return method.Name.Substring(prefixLength);
            //return method.Name.StartsWith("set_") ? method.Name.Substring("set_".Length) : method.Name.Substring("get_".Length);
        }

        public static object[] GetParameter(this IInvocation invocation)
        {
            return invocation.Arguments.Length == 2 ? new object[] { invocation.Arguments[0] } : null;
        }

        public static string GetPropertyName(this IInvocation invocation)
        {
            //return invocation.Method.PropertyName();
            if (invocation.Arguments.Length == 2)
                return (string) invocation.Arguments[0];
                    
            return invocation.Method.PropertyName();
        }

        /// <summary>
        /// in case an object implements something like
        /// public virtual string this[string key]{get;set;}
        /// this is the only type of property in C# that has a separate parameter for getter and setter methods
        /// and can be used to hold dynamic properties (which are not known at compile time)
        /// this is often used in conjunction with JSON
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        public static PropertyInfo ExtractIndexerProperty(this IDictionary<string, PropertyInfo> properties)
        {
            var property = properties.Where(
                p => p.Value.Name == "Item" && p.Value.GetGetMethod().GetParameters().Length == 1 &&
                     p.Value.GetSetMethod().GetParameters().Length == 2).Select(p => p.Value).FirstOrDefault();

            // the dynamic property needs to be ignored by the default logic, 
            // because its setter and getter has an additional parameter which would cause errors
            if (property != null)
                properties.Remove(property.Name);

            return property;
        }
       
        public static void TryCopyDictionary(this object source, object target)
        {
            var sourceDict = source as IDictionary<string, object>;
            var targetDict = target as IDictionary<string, object>;
            if ( sourceDict != null &&
                 targetDict != null)
            {
                foreach (var kvp in sourceDict)
                {
                    targetDict.Add(kvp);
                }
            }
        }
    }
}
