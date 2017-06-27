using Castle.DynamicProxy;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace ChangeTracking
{
    internal sealed class ChangeTrackingInterceptor<T> : IInterceptor, IInterceptorSettings where T : class
    {
        private Dictionary<string, object> _OriginalValueDictionary;
        internal EventHandler _StatusChanged = delegate { };
        private static Dictionary<string, PropertyInfo> _Properties;
        private ChangeStatus _ChangeTrackingStatus;
        private readonly Dictionary<string, Delegate> _StatusChangedEventHandlers;
        private bool _InRejectChanges;
        private static readonly PropertyInfo _DynamicProperty;
        private static readonly HashSet<string> _IgnoredProperties;

        public bool IsInitialized { get; set; }

        static ChangeTrackingInterceptor()
        {
            _Properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanWrite).ToDictionary(pi => pi.Name);
            _IgnoredProperties = new HashSet<string>(_Properties.Values.Where(p => p.GetCustomAttributes(typeof(IgnoreAttribute), true).Any())
                .Select(p => p.Name)
                .ToArray());
            // in case an object implements something like
            // public virtual string this[string key]{get;set;}
            // this is the only type of property in C# that has a separate parameter for getter and setter methods
            // and can be used to hold dynamic properties (which are not known at compile time)
            // this is often used in conjunction with JSON
            _DynamicProperty = _Properties.ExtractIndexerProperty();
        }

        internal ChangeTrackingInterceptor(ChangeStatus status)
        {
            _OriginalValueDictionary = new Dictionary<string, object>();
            _StatusChangedEventHandlers = new Dictionary<string, Delegate>();
            _ChangeTrackingStatus = status;
        }

        public void Intercept(IInvocation invocation)
        {
            if (!IsInitialized)
            {
                return;
            }
            if (invocation.Method.IsSetter() && !_InRejectChanges && !_IgnoredProperties.Contains(invocation.GetPropertyName()))
            {
                if (_ChangeTrackingStatus == ChangeStatus.Deleted)
                {
                    throw new InvalidOperationException("Can not modify deleted object");
                }
                string propertyName = invocation.GetPropertyName();
                bool noOriginalValueFound = !_OriginalValueDictionary.ContainsKey(propertyName);

                object originalValue = noOriginalValueFound ? GetProperty(propertyName).GetValue(invocation.Proxy, invocation.GetParameter()) : _OriginalValueDictionary[propertyName];
                invocation.Proceed();
                object newValue = GetProperty(propertyName).GetValue(invocation.Proxy, invocation.GetParameter());

                if (!ReferenceEquals(originalValue, newValue))
                {
                    UnsubscribeFromChildStatusChanged(propertyName, originalValue);
                    SubscribeToChildStatusChanged(invocation.Proxy, propertyName, newValue);
                }
                if (noOriginalValueFound && !Equals(originalValue, newValue))
                {
                    _OriginalValueDictionary.Add(propertyName, originalValue);
                    SetAndRaiseStatusChanged(invocation.Proxy, false);
                }
                else if (!noOriginalValueFound && Equals(originalValue, newValue))
                {
                    _OriginalValueDictionary.Remove(propertyName);
                    SetAndRaiseStatusChanged(invocation.Proxy, false);
                }
                return;
            }
            else if (invocation.Method.IsGetter() && !_IgnoredProperties.Contains(invocation.GetPropertyName()))
            {
                string propertyName = invocation.GetPropertyName();
                if (propertyName == "ChangeTrackingStatus")
                {
                    invocation.ReturnValue = _ChangeTrackingStatus;
                }
                else if (propertyName == "IsChanged")
                {
                    invocation.ReturnValue = _ChangeTrackingStatus != ChangeStatus.Unchanged;
                }
                else
                {
                    invocation.Proceed();
                    SubscribeToChildStatusChanged(invocation.Proxy, propertyName, invocation.ReturnValue);
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
                    _StatusChanged += (EventHandler)invocation.Arguments[0];
                    break;
                case "remove_StatusChanged":
                    _StatusChanged -= (EventHandler)invocation.Arguments[0];
                    break;
                case "Delete":
                    invocation.ReturnValue = Delete(invocation.Proxy);
                    break;
                case "UnDelete":
                    invocation.ReturnValue = UnDelete(invocation.Proxy);
                    break;
                case "UpdateStatus":
                    UpdateStatus(invocation.Proxy);
                    break;
                case "SetAdded":
                    SetAdded(invocation.Proxy);
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

        private object GetOriginalValue(T target, string propertyName)
        {
            object value;
            if (!_OriginalValueDictionary.TryGetValue(propertyName, out value))
            {
                try
                {
                    value = GetProperty(propertyName).GetValue(target, null);
                }
                catch (KeyNotFoundException ex)
                {
                    throw new ArgumentOutOfRangeException(string.Format("\"{0}\" is not a valid property name of type \"{1}\"", propertyName, typeof(T)), ex);
                }
            }
            return value;
        }

        private TResult GetOriginalValue<TResult>(T target, Expression<Func<T, TResult>> selector)
        {
            var propertyInfo = GetPropertyInfo(selector);

            string propName = propertyInfo.Name;

            object value;
            TResult originalValue;
            if (_OriginalValueDictionary.TryGetValue(propName, out value))
            {
                originalValue = (TResult)value;
            }
            else
            {
                originalValue = selector.Compile()(target);
            }
            var trackable = originalValue as IChangeTrackableInternal;
            if (trackable != null)
            {
                originalValue = (TResult)trackable.GetOriginal();
            }
            return originalValue;
        }

        public PropertyInfo GetPropertyInfo<TSource, TResult>(Expression<Func<TSource, TResult>> propertyLambda)
        {
            MemberExpression member = propertyLambda.Body as MemberExpression;
            if (member == null)
            {
                throw new ArgumentException(string.Format("Expression '{0}' refers to a method, not a property.", propertyLambda));
            }

            PropertyInfo propInfo = member.Member as PropertyInfo;
            if (propInfo == null)
            {
                throw new ArgumentException(string.Format("Expression '{0}' refers to a field, not a property.", propertyLambda));
            }

            Type type = typeof(TSource);
            if (type != propInfo.ReflectedType && !type.IsSubclassOf(propInfo.ReflectedType))
            {
                throw new ArgumentException(string.Format("Expression '{0}' refers to a property that is not from type {1}.", propertyLambda, type));
            }
            return propInfo;
        }

        private T GetOriginal(T proxy)
        {
            var original = Activator.CreateInstance<T>();

            foreach (var propertyKey in _Properties)
            {
                var property = propertyKey.Value;
                object value;
                object originalValue = _OriginalValueDictionary.TryGetValue(property.Name, out value) ? value : property.GetValue(proxy, null);
                if (originalValue != null)
                {
                    var trackable = originalValue as IChangeTrackableInternal;
                    if (trackable != null)
                    {
                        originalValue = trackable.GetOriginal();
                    }
                    else if (originalValue.GetType().GetInterface("IChangeTrackableCollection`1") != null)
                    {
                        IEnumerable<object> originalValues = ((System.Collections.IEnumerable)originalValue).Cast<IChangeTrackableInternal>().Select(v => v.GetOriginal());
                        var list = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(property.PropertyType.GetGenericArguments().First()));
                        foreach (var item in originalValues)
                        {
                            list.Add(item);
                        }
                        originalValue = list;
                    }
                }
                var index = property.Name == propertyKey.Key ? null : new object[]{propertyKey.Key};
                property.SetValue(original, originalValue, index);
            }

            // for the edge case that the objects implement IDictionary<,>
            proxy.TryCopyDictionary(original);

            return original;
        }

        private bool Delete(object proxy)
        {
            if (_ChangeTrackingStatus != ChangeStatus.Deleted)
            {
                _ChangeTrackingStatus = ChangeStatus.Deleted;
                _StatusChanged(proxy, EventArgs.Empty);
                return true;
            }
            return false;
        }

        private bool UnDelete(object proxy)
        {
            if (_ChangeTrackingStatus == ChangeStatus.Deleted)
            {
                SetAndRaiseStatusChanged(proxy, true);
                return true;
            }
            return false;
        }

        private void UpdateStatus(object proxy)
        {
            SetAndRaiseStatusChanged(proxy, true);
        }

        private void SetAdded(object proxy)
        {
            if (_ChangeTrackingStatus == ChangeStatus.Deleted)
            {
                var newChangeStatus = ChangeStatus.Added;
                if (_ChangeTrackingStatus != newChangeStatus)
                {
                    _ChangeTrackingStatus = newChangeStatus;
                    _StatusChanged(proxy, EventArgs.Empty);
                }
            }
        }

        private void AcceptChanges(object proxy)
        {
            if (_ChangeTrackingStatus == ChangeStatus.Deleted)
            {
                throw new InvalidOperationException("Can not call AcceptChanges on deleted object");
            }
            foreach (var child in GetChildren(proxy))
            {
                child.AcceptChanges();
            }
            _OriginalValueDictionary.Clear();
            SetAndRaiseStatusChanged(proxy, true);
        }

        private void RejectChanges(object proxy)
        {
            foreach (var child in GetChildren(proxy))
            {
                child.RejectChanges();
            }
            if (_OriginalValueDictionary.Count > 0)
            {
                _InRejectChanges = true;
                foreach (var changedProperty in _OriginalValueDictionary)
                {
                    var property = GetProperty(changedProperty.Key);

                    var index = property.Name == changedProperty.Key ? null : new object[] { changedProperty.Key };
                    property.SetValue(proxy, changedProperty.Value, index);
                }
                _OriginalValueDictionary.Clear();
                _InRejectChanges = false;
            }
            SetAndRaiseStatusChanged(proxy, true);
        }

        private static IEnumerable<System.ComponentModel.IRevertibleChangeTracking> GetChildren(object proxy)
        {
            return ((ICollectionPropertyTrackable)proxy).CollectionPropertyTrackables
                .Concat(((IComplexPropertyTrackable)proxy).ComplexPropertyTrackables)
                .OfType<System.ComponentModel.IRevertibleChangeTracking>();
        }
        
        private void UnsubscribeFromChildStatusChanged(string propertyName, object oldChild)
        {
            Delegate handler;
            if (_StatusChangedEventHandlers.TryGetValue(propertyName, out handler))
            {
                var trackable = oldChild as IChangeTrackable;
                if (trackable != null)
                {
                    trackable.StatusChanged -= (EventHandler)handler;
                    _StatusChangedEventHandlers.Remove(propertyName);
                    return;
                }
                var collectionTrackable = oldChild as System.ComponentModel.IBindingList;
                if (collectionTrackable != null)
                {
                    collectionTrackable.ListChanged -= (System.ComponentModel.ListChangedEventHandler)handler;
                    _StatusChangedEventHandlers.Remove(propertyName);
                }
            }
        }

        private void SubscribeToChildStatusChanged(object proxy, string propertyName, object newValue)
        {
            if (!_StatusChangedEventHandlers.ContainsKey(propertyName))
            {
                var newChild = newValue as IChangeTrackable;
                if (newChild != null)
                {
                    EventHandler newHandler = (sender, e) => SetAndRaiseStatusChanged(proxy, false);
                    newChild.StatusChanged += newHandler;
                    _StatusChangedEventHandlers.Add(propertyName, newHandler);
                    return;
                }
                var newCollectionChild = newValue as System.ComponentModel.IBindingList;
                if (newCollectionChild != null)
                {
                    System.ComponentModel.ListChangedEventHandler newHandler = (sender, e) => SetAndRaiseStatusChanged(proxy, false);
                    newCollectionChild.ListChanged += newHandler;
                    _StatusChangedEventHandlers.Add(propertyName, newHandler);
                }
            }
        }

        private void SetAndRaiseStatusChanged(object proxy, bool setStatusEvenIfStatsAddedOrDeleted)
        {
            if (_ChangeTrackingStatus == ChangeStatus.Changed || _ChangeTrackingStatus == ChangeStatus.Unchanged || setStatusEvenIfStatsAddedOrDeleted)
            {
                var newChangeStatus = GetNewChangeStatus(proxy);
                if (_ChangeTrackingStatus != newChangeStatus)
                {
                    _ChangeTrackingStatus = newChangeStatus;
                    _StatusChanged(proxy, EventArgs.Empty);
                }
            }
        }

        private ChangeStatus GetNewChangeStatus(object sender)
        {
            return _OriginalValueDictionary.Count == 0 && GetChildren(sender).All(c => !c.IsChanged) ? ChangeStatus.Unchanged : ChangeStatus.Changed;
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
