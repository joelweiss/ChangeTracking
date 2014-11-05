using Castle.DynamicProxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace ChangeTracking
{
    internal sealed class ChangeTrackingInterceptor<T> : IInterceptor where T : class
    {
        private Dictionary<string, object> _OriginalValueDictionary;
        public event EventHandler StatusChanged = delegate { };
        private static Dictionary<string, PropertyInfo> _Properties;
        public ChangeStatus ChangeTrackingStatus { get; private set; }

        static ChangeTrackingInterceptor()
        {
            _Properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).ToDictionary(pi => pi.Name);
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
                if (ChangeTrackingStatus == ChangeStatus.Deleted)
                {
                    throw new InvalidOperationException("Can not modify deleted object");
                }
                string propName = invocation.Method.PropertyName();
                if (!_OriginalValueDictionary.ContainsKey(propName))
                {
                    var previousalue = _Properties[propName].GetValue(invocation.InvocationTarget, null);
                    if (!Equals(previousalue, invocation.Arguments[0]))
                    {
                        _OriginalValueDictionary.Add(propName, previousalue);
                        if (ChangeTrackingStatus == ChangeStatus.Unchanged)
                        {
                            ChangeTrackingStatus = ChangeStatus.Changed;
                        }
                        invocation.Proceed();
                        StatusChanged(invocation.Proxy, EventArgs.Empty);
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
                            StatusChanged(invocation.Proxy, EventArgs.Empty);
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
                else if (propName == "IsChanged")
                {
                    invocation.ReturnValue = ChangeTrackingStatus != ChangeStatus.Unchanged;
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
                    invocation.ReturnValue = ((dynamic)this).GetOriginalValue((T)invocation.Proxy, (dynamic)invocation.Arguments[0]);
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
                    invocation.ReturnValue = Delete(invocation.Proxy);
                    break;
                case "UnDelete":
                    invocation.ReturnValue = UnDelete(invocation.Proxy);
                    break;
                case "AcceptChanges":
                    AcceptChanges(invocation.Proxy);
                    break;
                case "RejectChanges":
                    RejectChanges(invocation.Proxy);
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

        private object GetOriginalValue(T target, string propertyName)
        {
            object value;
            if (!_OriginalValueDictionary.TryGetValue(propertyName, out value))
            {
                try
                {
                    value = _Properties[propertyName].GetValue(target, null);
                }
                catch (KeyNotFoundException ex)
                {
                    throw new ArgumentOutOfRangeException(string.Format("\"{0}\" is not a valid property name of type \"{1}\"", propertyName, typeof(T)), ex);
                }
            }
            return value;
        }

        private T GetOriginal(T target)
        {
            if (_OriginalValueDictionary.Count == 0)
            {
                return target;
            }
            // this doesn't handle private fields
            var original = Activator.CreateInstance<T>();
            foreach (var property in _Properties.Values)
            {
                object value;
                property.SetValue(original, _OriginalValueDictionary.TryGetValue(property.Name, out value) ? value : property.GetValue(target, null), null);
            }
            return original;
        }

        private bool Delete(object target)
        {
            if (ChangeTrackingStatus != ChangeStatus.Deleted)
            {
                ChangeTrackingStatus = ChangeStatus.Deleted;
                StatusChanged(target, EventArgs.Empty);
                return true;
            }
            return false;
        }

        private bool UnDelete(object target)
        {
            if (ChangeTrackingStatus == ChangeStatus.Deleted)
            {
                ChangeTrackingStatus = _OriginalValueDictionary.Count > 0 ? ChangeStatus.Changed : ChangeStatus.Unchanged;
                StatusChanged(target, EventArgs.Empty);
                return true;
            }
            return false;
        }

        private void AcceptChanges(object proxy)
        {
            if (ChangeTrackingStatus == ChangeStatus.Deleted)
            {
                throw new InvalidOperationException("Can not call AcceptChanges on deleted object");
            }
            if (_OriginalValueDictionary.Count > 0)
            {
                _OriginalValueDictionary.Clear();
                ChangeTrackingStatus = ChangeStatus.Unchanged;
                StatusChanged(proxy, EventArgs.Empty);
            }
        }

        private void RejectChanges(object proxy)
        {
            if (_OriginalValueDictionary.Count > 0)
            {
                object target = ((IProxyTargetAccessor)proxy).DynProxyGetTarget();
                foreach (var changedProperty in _OriginalValueDictionary)
                {
                    _Properties[changedProperty.Key].SetValue(target, changedProperty.Value, null);
                }
                _OriginalValueDictionary.Clear();
            }
            if (ChangeTrackingStatus == ChangeStatus.Changed || ChangeTrackingStatus == ChangeStatus.Deleted)
            {
                ChangeTrackingStatus = ChangeStatus.Unchanged;
                StatusChanged(proxy, EventArgs.Empty);
            }
        }
    }
}
