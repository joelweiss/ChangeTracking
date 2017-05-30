using Castle.DynamicProxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ChangeTracking
{
    internal sealed class EditableObjectInterceptor<T> : IInterceptor, IInterceptorSettings
    {
        private static Dictionary<string, PropertyInfo> _Properties;
        private readonly Dictionary<string, object> _BeforeEditValues;
        private readonly Action<T> _NotifyParentItemCanceled;
        private bool _IsEditing;
        private static readonly PropertyInfo _DynamicProperty;

        public bool IsInitialized { get; set; }

        static EditableObjectInterceptor()
        {
            _Properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).ToDictionary(pi => pi.Name);

            // in case an object implements something like
            // public virtual string this[string key]{get;set;}
            // this is the only type of property in C# that has a separate parameter for getter and setter methods
            // and can be used to hold dynamic properties (which are not known at compile time)
            // this is often used in conjunction with JSON
            _DynamicProperty = _Properties.ExtractIndexerProperty();
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
                    string propName = invocation.GetPropertyName();
                    if (!_BeforeEditValues.ContainsKey(propName))
                    {
                        var oldValue = GetProperty(propName).GetValue(invocation.Proxy, invocation.GetParameter());
                        invocation.Proceed();
                        var newValue = GetProperty(propName).GetValue(invocation.Proxy, invocation.GetParameter());
                        if (!Equals(oldValue, newValue))
                        {
                            _BeforeEditValues.Add(propName, oldValue);
                        }
                    }
                    else
                    {
                        var originalValue = _BeforeEditValues[propName];
                        invocation.Proceed();
                        var newValue = GetProperty(propName).GetValue(invocation.Proxy, invocation.GetParameter());
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
                    BeginEdit(invocation.Proxy);
                    break;
                case "CancelEdit":
                    CancelEdit(invocation.Proxy);
                    break;
                case "EndEdit":
                    EndEdit(invocation.Proxy);
                    break;
                default:
                    invocation.Proceed();
                    break;
            }
        }

        private void BeginEdit(object proxy)
        {
            foreach (var child in GetChildren(proxy))
            {
                child.BeginEdit();
            }
            _IsEditing = true;
        }

        private void CancelEdit(object proxy)
        {
            foreach (var child in GetChildren(proxy))
            {
                child.CancelEdit();
            }
            if (_IsEditing)
            {
                _IsEditing = false;
                foreach (var oldValue in _BeforeEditValues)
                {
                    var property = GetProperty(oldValue.Key);
                    var index = property.Name == oldValue.Key ? null : new object[] { oldValue.Key };
                    property.SetValue(proxy, oldValue.Value, index);
                }
                if (_NotifyParentItemCanceled != null)
                {
                    _NotifyParentItemCanceled((T)proxy);
                }
            }
        }

        private void EndEdit(object proxy)
        {
            foreach (var child in GetChildren(proxy))
            {
                child.EndEdit();
            }
            if (_IsEditing == true)
            {
                _IsEditing = false;
                _BeforeEditValues.Clear();
            }
        }

        private static IEnumerable<System.ComponentModel.IEditableObject> GetChildren(object proxy)
        {
            return ((ICollectionPropertyTrackable)proxy).CollectionPropertyTrackables
                .Concat(((IComplexPropertyTrackable)proxy).ComplexPropertyTrackables)
                .OfType<System.ComponentModel.IEditableObject>();
        }
        internal static PropertyInfo GetProperty(string propertyName)
        {
            PropertyInfo property;
            if (_Properties.TryGetValue(propertyName, out property))
            {
                return property;
            }

            if (_DynamicProperty != null)
                return _DynamicProperty;

            throw new InvalidOperationException($"The type '{typeof(T).FullName}' has no property named '{propertyName}'");
        }
    }
}
