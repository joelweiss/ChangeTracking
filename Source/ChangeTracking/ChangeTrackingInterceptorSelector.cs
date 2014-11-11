using Castle.DynamicProxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChangeTracking
{
    internal class ChangeTrackingInterceptorSelector : IInterceptorSelector
    {
        IInterceptor[] IInterceptorSelector.SelectInterceptors(Type type, System.Reflection.MethodInfo method, IInterceptor[] interceptors)
        {
            return interceptors;
        }
    }
}
