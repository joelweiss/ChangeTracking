using Castle.DynamicProxy;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;

namespace ChangeTracking
{
    public class ChangeTrackingProxyGenerationHook : IProxyGenerationHook
    {
        private readonly static HashSet<string> _MethodsToSkip;
        private readonly static HashSet<Type> _TypesToSkip;

        static ChangeTrackingProxyGenerationHook()
        {
            _MethodsToSkip = new HashSet<string> { "Equals", "GetType", "ToString", "GetHashCode" };
            _TypesToSkip = new HashSet<Type> { typeof(DynamicObject) };
        }

        public void MethodsInspected() { }

        public void NonProxyableMemberNotification(Type type, MemberInfo memberInfo)
        {
            var method = memberInfo as MethodInfo;
            if (method != null && method.IsProperty())
            {
                throw new InvalidOperationException(string.Format("Property {0} is not virtual. Can't track classes with non-virtual properties.", method.Name.Substring("set_".Length)));
            }
        }

        public bool ShouldInterceptMethod(Type type, MethodInfo methodInfo)
        {
            return !_TypesToSkip.Contains(methodInfo.DeclaringType)
                && !_TypesToSkip.Contains(methodInfo.GetBaseDefinition().DeclaringType)
                && !_MethodsToSkip.Contains(methodInfo.Name);
        }

        public override bool Equals(object obj)
        {
            return obj as ChangeTrackingProxyGenerationHook != null;
        }

        public override int GetHashCode()
        {
            return 0;
        }
    }
}