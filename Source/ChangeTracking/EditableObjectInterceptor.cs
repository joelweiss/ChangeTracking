using Castle.DynamicProxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ChangeTracking
{
    internal sealed class EditableObjectInterceptor<T> : IInterceptor
    {
        private static Dictionary<string, PropertyInfo> _Properties;
        private readonly Dictionary<string, object> _BeforeEditValues;
        private readonly Action<T> _NotifyParentItemCanceled;
        private bool _IsEditing;

        static EditableObjectInterceptor()
        {
            _Properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).ToDictionary(pi => pi.Name);
        }

        public EditableObjectInterceptor()
        {
            _BeforeEditValues = new Dictionary<string, object>();
        }

        public EditableObjectInterceptor(Action<T> notifyParentItemCanceled)
            : this()
        {
            _NotifyParentItemCanceled = notifyParentItemCanceled;
        }


        public void Intercept(IInvocation invocation)
        {
            if (_IsEditing == true)
            {
                if (invocation.Method.IsSetter())
                {
                    string propName = invocation.Method.PropertyName();
                    if (!_BeforeEditValues.ContainsKey(propName))
                    {
                        var oldValue = _Properties[propName].GetValue(invocation.InvocationTarget, null);
                        if (!Equals(oldValue, invocation.Arguments[0]))
                        {
                            _BeforeEditValues.Add(propName, oldValue);
                        }
                    }
                    else
                    {
                        var originalValue = _BeforeEditValues[propName];
                        if (Equals(originalValue, invocation.Arguments[0]))
                        {
                            _BeforeEditValues.Remove(propName);
                        }
                    }
                    invocation.Proceed();
                    return;
                }
                else if (invocation.Method.IsGetter())
                {
                    invocation.Proceed();
                    return;
                }
            }
            switch (invocation.Method.Name)
            {
                case "BeginEdit":
                    BeginEdit();
                    break;
                case "CancelEdit":
                    CancelEdit(invocation.Proxy);
                    break;
                case "EndEdit":
                    EndEdit();
                    break;
                default:
                    invocation.Proceed();
                    break;
            }
        }

        private void BeginEdit()
        {
            _IsEditing = true;
        }

        private void CancelEdit(object proxy)
        {
            if (_IsEditing)
            {
                _IsEditing = false;
                foreach (var oldValue in _BeforeEditValues)
                {
                    _Properties[oldValue.Key].SetValue(proxy, oldValue.Value, null);
                }
                if (_NotifyParentItemCanceled != null)
                {
                    _NotifyParentItemCanceled((T)proxy);
                }
            }
        }

        private void EndEdit()
        {
            if (_IsEditing == true)
            {
                _IsEditing = false;
                _BeforeEditValues.Clear();
            }
        }
    }
}
