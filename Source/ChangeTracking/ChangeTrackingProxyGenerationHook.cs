using Castle.DynamicProxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ChangeTracking.Internal;

namespace ChangeTracking
{
    internal class ChangeTrackingProxyGenerationHook : IProxyGenerationHook
    {
        private HashSet<string> _MethodsToSkip;
        private readonly Type _Type;
        private HashSet<MethodInfo> _InstanceMethodsToSkip;

        public ChangeTrackingProxyGenerationHook(Type type, HashSet<string> methodsToSkip)
        {
            _Type = type ?? throw new ArgumentNullException(nameof(type));
            _MethodsToSkip = methodsToSkip ?? throw new ArgumentNullException(nameof(methodsToSkip));
            const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            _InstanceMethodsToSkip = new HashSet<MethodInfo>(type
                .GetMethods(bindingFlags)
                .Where(mi => !mi.IsSpecialName || (mi.IsProperty() && type.GetProperty(mi.PropertyName(), bindingFlags) is PropertyInfo pi && (!pi.CanWrite || Utils.IsMarkedDoNotTrack(pi)))));
        }

        public void MethodsInspected() { }

        public void NonProxyableMemberNotification(Type type, MemberInfo memberInfo)
        {
            if (memberInfo is MethodInfo methodInfo && methodInfo.IsProperty() && !_InstanceMethodsToSkip.Contains(methodInfo))
            {
                throw new InvalidOperationException($"Property {methodInfo.Name.Substring("set_".Length)} is not virtual. Can't track classes with non-virtual properties.");
            }
        }

        public bool ShouldInterceptMethod(Type type, System.Reflection.MethodInfo methodInfo) => !_MethodsToSkip.Contains(methodInfo.Name) && !_InstanceMethodsToSkip.Contains(methodInfo);

        public override bool Equals(object obj) => (obj as ChangeTrackingProxyGenerationHook)?._Type == _Type;

        public override int GetHashCode() => _Type.GetHashCode();
    }
}
