using Castle.DynamicProxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace ChangeTracking
{
    public class ChangeTrackingInterceptor<T> : IInterceptor where T : class
    {
        private Dictionary<string, object> _OriginalValueDictionary;
        public event EventHandler StatusChanged = delegate { };
        private static Dictionary<string, PropertyInfo> _Properties;
        public ChangeStatus ChangeTrackingStatus { get; set; }

        static ChangeTrackingInterceptor()
        {
            _Properties = typeof(T).GetProperties().ToDictionary(pi => pi.Name);
        }

        public ChangeTrackingInterceptor(ChangeStatus status)
        {
            _OriginalValueDictionary = new Dictionary<string, object>();
            ChangeTrackingStatus = status;
        }

        public void Intercept(IInvocation invocation)
        {
            if (invocation.Method.IsSetter())
            {
                string propName = invocation.Method.PropertyName();
                if (ChangeTrackingStatus == ChangeStatus.Deleted)
                {
                    throw new InvalidOperationException("Can not modify deleted object");
                }
                if (!_OriginalValueDictionary.ContainsKey(propName))
                {
                    var oldValue = _Properties[propName].GetValue(invocation.InvocationTarget, null);
                    if (!Equals(oldValue, invocation.Arguments[0]))
                    {
                        _OriginalValueDictionary.Add(propName, oldValue);
                        if (ChangeTrackingStatus == ChangeStatus.Unchanged)
                        {
                            ChangeTrackingStatus = ChangeStatus.Changed;
                        }
                        invocation.Proceed();
                        StatusChanged(invocation.InvocationTarget, EventArgs.Empty);
                    }
                    else
                    {
                        invocation.Proceed();
                    }
                }
                else
                {
                    var originalValue = _OriginalValueDictionary[propName];
                    if (Equals(originalValue, invocation.Arguments[0]))
                    {
                        _OriginalValueDictionary.Remove(propName);
                        if (_OriginalValueDictionary.Count == 0)
                        {
                            if (ChangeTrackingStatus == ChangeStatus.Changed)
                            {
                                ChangeTrackingStatus = ChangeStatus.Unchanged;
                            }
                            StatusChanged(invocation.InvocationTarget, EventArgs.Empty);
                        }
                    }
                    invocation.Proceed();
                }
                return;
            }
            else if (invocation.Method.IsGetter())
            {
                string propName = invocation.Method.PropertyName();
                if (propName == "ChangeTrackingStatus")
                {
                    invocation.ReturnValue = ChangeTrackingStatus;
                }
                else
                {
                    invocation.Proceed();
                }
                return;
            }
            switch (invocation.Method.Name)
            {
                case "GetOriginalValue":
                    Type returnType = ((MemberExpression)((LambdaExpression)invocation.Arguments[0]).Body).Type;
                    invocation.ReturnValue = GetType().GetMethod("GetOriginalValue", BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(returnType).Invoke(this, new[] { invocation.Proxy, invocation.Arguments[0] });
                    break;
                case "GetOriginal":
                    invocation.ReturnValue = GetOriginal((T)invocation.Proxy);
                    break;
                case "add_StatusChanged":
                    StatusChanged += (EventHandler)invocation.Arguments[0];
                    break;
                case "remove_StatusChanged":
                    StatusChanged -= (EventHandler)invocation.Arguments[0];
                    break;
                case "Delete":
                    ChangeTrackingStatus = ChangeStatus.Deleted;
                    StatusChanged(invocation.InvocationTarget, EventArgs.Empty);
                    break;
                default:
                    invocation.Proceed();
                    break;
            }

        }

        private TResult GetOriginalValue<TResult>(T target, Expression<Func<T, TResult>> selector)
        {
            if (selector.Body.NodeType != ExpressionType.MemberAccess)
            {
                throw new ArgumentException("A property selector expression has an incorrect format");
            }

            MemberExpression memberAccess = selector.Body as MemberExpression;
            if (memberAccess.Member.MemberType != MemberTypes.Property)
            {
                throw new ArgumentException("A selected member is not a property");
            }

            string propName = memberAccess.Member.Name;

            object value;
            if (_OriginalValueDictionary.TryGetValue(propName, out value))
            {
                return (TResult)value;
            }
            else
            {
                return selector.Compile()(target);
            }
        }

        private T GetOriginal(T target)
        {
            var original = Activator.CreateInstance<T>();
            foreach (var property in _Properties.Values)
            {
                object value;
                property.SetValue(original, _OriginalValueDictionary.TryGetValue(property.Name, out value) ? value : property.GetValue(target, null), null);
            }
            return original;
        }
    }
}
