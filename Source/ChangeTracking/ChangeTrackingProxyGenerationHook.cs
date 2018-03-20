using Castle.DynamicProxy;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace ChangeTracking
{
    public class ChangeTrackingProxyGenerationHook : IProxyGenerationHook
    {
        private static HashSet<string> _MethodsToSkip;

        static ChangeTrackingProxyGenerationHook()
        {
            _MethodsToSkip = new HashSet<string> { "Equals", "GetType", "ToString", "GetHashCode" };
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

        public bool ShouldInterceptMethod(Type type, System.Reflection.MethodInfo methodInfo) => !_MethodsToSkip.Contains(methodInfo.Name);

        public override bool Equals(object obj) => obj as ChangeTrackingProxyGenerationHook != null;

        public override int GetHashCode() => 0;
    }
}
