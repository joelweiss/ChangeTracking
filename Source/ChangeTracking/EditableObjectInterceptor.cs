using Castle.DynamicProxy;
using ChangeTracking.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ChangeTracking
{
    internal sealed class EditableObjectInterceptor<T> : IInterceptor, IInterceptorSettings
    {
        private static Dictionary<string, PropertyInfo> _Properties;
        private readonly Dictionary<string, object> _BeforeEditValues;
        private readonly Action<T> _NotifyParentItemCanceled;
        private bool _IsEditing;

        public bool IsInitialized { get; set; }

        static EditableObjectInterceptor()
        {
            _Properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).ToDictionary(pi => pi.Name);
        }

        internal EditableObjectInterceptor()
        {
            _BeforeEditValues = new Dictionary<string, object>();
        }

        internal EditableObjectInterceptor(Action<T> notifyParentItemCanceled)
            : this()
        {
            _NotifyParentItemCanceled = notifyParentItemCanceled;
        }


        public void Intercept(IInvocation invocation)
        {
            if (!IsInitialized)
            {
                return;
            }
            if (_IsEditing == true)
            {
                if (invocation.Method.IsSetter())
                {
                    string propName = invocation.Method.PropertyName();
                    if (!_BeforeEditValues.ContainsKey(propName))
                    {
                        var oldValue = _Properties[propName].GetValue(invocation.Proxy, null);
                        invocation.Proceed();
                        var newValue = _Properties[propName].GetValue(invocation.Proxy, null);
                        if (!Equals(oldValue, newValue))
                        {
                            _BeforeEditValues.Add(propName, oldValue);
                        }
                    }
                    else
                    {
                        var originalValue = _BeforeEditValues[propName];
                        invocation.Proceed();
                        var newValue = _Properties[propName].GetValue(invocation.Proxy, null);
                        if (Equals(originalValue, newValue))
                        {
                            _BeforeEditValues.Remove(propName);
                        }
                    }
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
                    BeginEdit(invocation.Proxy, invocation.Arguments.Length == 0 ? null : (List<object>)invocation.Arguments[0]);
                    break;
                case "CancelEdit":
                    CancelEdit(invocation.Proxy, invocation.Arguments.Length == 0 ? null : (List<object>)invocation.Arguments[0]);
                    break;
                case "EndEdit":
                    EndEdit(invocation.Proxy, invocation.Arguments.Length == 0 ? null : (List<object>)invocation.Arguments[0]);
                    break;
                default:
                    invocation.Proceed();
                    break;
            }
        }

        private void BeginEdit(object proxy, List<object> parents)
        {
            parents = parents ?? new List<object>(20) { proxy };
            foreach (var child in Utils.GetChildren<IEditableObjectInternal>(proxy, parents))
            {
                child.BeginEdit(parents);
            }
            _IsEditing = true;
        }

        private void CancelEdit(object proxy, List<object> parents)
        {
            parents = parents ?? new List<object>(20) { proxy };
            foreach (var child in Utils.GetChildren<IEditableObjectInternal>(proxy, parents))
            {
                child.CancelEdit(parents);
            }
            if (_IsEditing)
            {
                _IsEditing = false;
                foreach (var oldValue in _BeforeEditValues)
                {
                    _Properties[oldValue.Key].SetValue(proxy, oldValue.Value, null);
                }
                _NotifyParentItemCanceled?.Invoke((T)proxy);
            }
        }

        private void EndEdit(object proxy, List<object> parents)
        {
            parents = parents ?? new List<object>(20) { proxy };
            foreach (var child in Utils.GetChildren<IEditableObjectInternal>(proxy, parents))
            {
                child.EndEdit(parents);
            }
            if (_IsEditing == true)
            {
                _IsEditing = false;
                _BeforeEditValues.Clear();
            }
        }
    }
}
