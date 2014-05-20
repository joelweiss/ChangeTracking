using Castle.DynamicProxy;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ChangeTracking
{
    internal class NotifyPropertyChangedInterceptor<T> : IInterceptor
    {
        private static Dictionary<string, System.Reflection.PropertyInfo> _Properties;

        static NotifyPropertyChangedInterceptor()
        {
            _Properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).ToDictionary(pi => pi.Name);
        }

        public NotifyPropertyChangedInterceptor()
        {
            PropertyChanged += delegate { };
        }

        public void Intercept(IInvocation invocation)
        {
            if (invocation.Method.IsSetter())
            {
                string propName = invocation.Method.PropertyName();
                var previousalue = _Properties[propName].GetValue(invocation.InvocationTarget, null);
                if (!Equals(previousalue, invocation.Arguments[0]))
                {
                    PropertyChanged(invocation.Proxy, new System.ComponentModel.PropertyChangedEventArgs(propName));
                }
                invocation.Proceed();
                return;
            }
            switch (invocation.Method.Name)
            {
                case "add_PropertyChanged":
                    PropertyChanged += (PropertyChangedEventHandler)invocation.Arguments[0];
                    break;
                case "remove_PropertyChanged":
                    PropertyChanged -= (PropertyChangedEventHandler)invocation.Arguments[0];
                    break;
                default:
                    invocation.Proceed();
                    break;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
