using Castle.DynamicProxy;
using System;

namespace ChangeTracking
{
    internal class ChangeTrackingInterceptorSelector : IInterceptorSelector
    {
        IInterceptor[] IInterceptorSelector.SelectInterceptors(Type type, System.Reflection.MethodInfo method, IInterceptor[] interceptors) => interceptors;
    }
}
