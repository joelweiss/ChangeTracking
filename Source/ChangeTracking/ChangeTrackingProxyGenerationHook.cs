using Castle.DynamicProxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ChangeTracking
{
    public class ChangeTrackingProxyGenerationHook : IProxyGenerationHook
    {
        private static HashSet<string> _MethodsToSkip;
        private readonly Type _Type;
        //private HashSet<MethodInfo> _InstanceMethodsOnClass;

        static ChangeTrackingProxyGenerationHook()
        {
            _MethodsToSkip = new HashSet<string> {"Equals", "GetType", "ToString", "GetHashCode"};
        }

        public ChangeTrackingProxyGenerationHook(Type type)
        {
            _Type = type;

            // Todo: It currently prevents the IDictionary support.
            // _InstanceMethodsOnClass = new HashSet<MethodInfo>(type
            //    .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            //    .Where(mi => !mi.IsSpecialName));
        }

        public void MethodsInspected() { }

        public void NonProxyableMemberNotification(Type type, MemberInfo memberInfo)
        {
            if (memberInfo is MethodInfo methodInfo && methodInfo.IsProperty() && type.GetProperty(methodInfo.PropertyName())?.GetCustomAttributes(typeof(ChangeTracking.IgnoreAttribute), true).Any() == false)
            {
                throw new InvalidOperationException($"Property {methodInfo.Name.Substring("set_".Length)} is not virtual. Can't track classes with non-virtual properties.");
            }
        }

        public bool ShouldInterceptMethod(Type type, System.Reflection.MethodInfo methodInfo) => !_MethodsToSkip.Contains(methodInfo.Name) /*&& !_InstanceMethodsOnClass.Contains(methodInfo)*/;

        public override bool Equals(object obj) => (obj as ChangeTrackingProxyGenerationHook)?._Type == _Type;

        public override int GetHashCode() => _Type.GetHashCode();
    }
}
