using Castle.DynamicProxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChangeTracking
{
    public class ChangeTrackingInterceptorSelector : IInterceptorSelector
    {
        public IInterceptor[] SelectInterceptors(Type type, System.Reflection.MethodInfo method, IInterceptor[] interceptors)
        {
            return interceptors;
        }
    }
}
