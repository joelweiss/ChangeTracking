using Castle.DynamicProxy;
using ChangeTracking.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace ChangeTracking
{
    internal sealed class ChangeTrackingInterceptor<T> : IInterceptor, IInterceptorSettings where T : class
    {
        private static readonly Dictionary<string, PropertyInfo> _Properties;
        private readonly Dictionary<string, object> _OriginalValueDictionary;
        private readonly HashSet<string> _ChangedComplexOrCollectionProperties;
        private readonly Dictionary<string, Delegate> _StatusChangedEventHandlers;
        private readonly object _StatusChangedEventHandlersLock;
        private ChangeStatus _ChangeTrackingStatus;
        private bool _InRejectChanges;
        internal EventHandler _StatusChanged = delegate { };
        internal EventHandler _ChangedPropertiesChanged = delegate { };

        public bool IsInitialized { get; set; }

        static ChangeTrackingInterceptor()
        {
            _Properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanWrite).ToDictionary(pi => pi.Name);
        }

        internal ChangeTrackingInterceptor(ChangeStatus status)
        {
            _OriginalValueDictionary = new Dictionary<string, object>();
            _ChangedComplexOrCollectionProperties = new HashSet<string>();
            _StatusChangedEventHandlers = new Dictionary<string, Delegate>();
            _StatusChangedEventHandlersLock = new object();
            _ChangeTrackingStatus = status;
        }

        public void Intercept(IInvocation invocation)
        {
            if (!IsInitialized)
            {
                return;
            }
            if (invocation.Method.IsSetter() && !_InRejectChanges)
            {
                if (_ChangeTrackingStatus == ChangeStatus.Deleted)
                {
                    throw new InvalidOperationException("Can not modify deleted object");
                }
                string propertyName = invocation.Method.PropertyName();

                object previousValue = _Properties[propertyName].GetValue(invocation.Proxy, null);

                invocation.Proceed();
                object newValue = _Properties[propertyName].GetValue(invocation.Proxy, null);

                if (!ReferenceEquals(previousValue, newValue))
                {
                    UnsubscribeFromChildStatusChanged(propertyName, previousValue);
                    SubscribeToChildStatusChanged(invocation.Proxy, propertyName, newValue);
                }
                bool originalValueFound = _OriginalValueDictionary.TryGetValue(propertyName, out object originalValue);
                if (!originalValueFound && !Equals(previousValue, newValue))
                {
                    _OriginalValueDictionary.Add(propertyName, previousValue);
                    RaiseChangePropertiesChanged(invocation.Proxy);
                    SetAndRaiseStatusChanged(invocation.Proxy, parents: new List<object> { invocation.Proxy }, setStatusEvenIfStatsAddedOrDeleted: false);
                }
                else if (originalValueFound && Equals(originalValue, newValue))
                {
                    _OriginalValueDictionary.Remove(propertyName);
                    RaiseChangePropertiesChanged(invocation.Proxy);
                    SetAndRaiseStatusChanged(invocation.Proxy, parents: new List<object> { invocation.Proxy }, setStatusEvenIfStatsAddedOrDeleted: false);
                }
                return;
            }
            else if (invocation.Method.IsGetter())
            {
                string propertyName = invocation.Method.PropertyName();
                if (propertyName == nameof(IChangeTrackable.ChangeTrackingStatus))
                {
                    invocation.ReturnValue = _ChangeTrackingStatus;
                }
                else if (propertyName == nameof(System.ComponentModel.IChangeTracking.IsChanged))
                {
                    invocation.ReturnValue = _ChangeTrackingStatus != ChangeStatus.Unchanged;
                }
                else if (propertyName == nameof(IChangeTrackable.ChangedProperties))
                {
                    invocation.ReturnValue = GetChangedProperties();
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
                case nameof(IChangeTrackable<object>.GetOriginalValue):
                    invocation.ReturnValue = ((dynamic)this).GetOriginalValue((T)invocation.Proxy, (dynamic)invocation.Arguments[0]);
                    break;
                case nameof(IChangeTrackable<object>.GetOriginal):
                case nameof(IChangeTrackable<object>.GetCurrent):
                    object poco;
                    Func<PropertyInfo, object, object> getPropertyProxy;
                    Func<IChangeTrackableInternal, UnrollGraph, object> getPropertyPoco;
                    if (invocation.Method.Name == nameof(IChangeTrackable<object>.GetOriginal))
                    {
                        getPropertyProxy = (property, proxy) => _OriginalValueDictionary.TryGetValue(property.Name, out object value) ? value : property.GetValue(proxy, null);
                        getPropertyPoco = (proxy, g) => proxy.GetOriginal(g);
                    }
                    else
                    {
                        getPropertyProxy = (property, proxy) => property.GetValue(proxy, null);
                        getPropertyPoco = (proxy, g) => proxy.GetCurrent(g);
                    }

                    bool isRootCall = invocation.Arguments.Length == 0;
                    if (isRootCall)
                    {
                        UnrollGraph unrollGraph = new UnrollGraph();
                        poco = GetPoco((T)invocation.Proxy, unrollGraph, getPropertyProxy, getPropertyPoco);
                        unrollGraph.FinishWireUp();
                    }
                    else
                    {
                        poco = GetPoco((T)invocation.Proxy, (UnrollGraph)invocation.Arguments[0], getPropertyProxy, getPropertyPoco);
                    }
                    invocation.ReturnValue = poco;
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
                case nameof(IRevertibleChangeTrackingInternal.AcceptChanges):
                    AcceptChanges(invocation.Proxy, invocation.Arguments.Length == 0 ? null : (List<object>)invocation.Arguments[0]);
                    break;
                case nameof(IRevertibleChangeTrackingInternal.RejectChanges):
                    RejectChanges(invocation.Proxy, invocation.Arguments.Length == 0 ? null : (List<object>)invocation.Arguments[0]);
                    break;
                case nameof(IRevertibleChangeTrackingInternal.IsChanged):
                    invocation.ReturnValue = GetNewChangeStatus(invocation.Proxy, (List<object>)invocation.Arguments[0]) != ChangeStatus.Unchanged;
                    break;
                default:
                    invocation.Proceed();
                    break;
            }
        }

        private object GetOriginalValue(T target, string propertyName)
        {
            if (!_OriginalValueDictionary.TryGetValue(propertyName, out object value))
            {
                try
                {
                    value = _Properties[propertyName].GetValue(target, null);
                }
                catch (KeyNotFoundException ex)
                {
                    throw new ArgumentOutOfRangeException($"\"{propertyName}\" is not a valid property name of type \"{typeof(T)}\"", ex);
                }
            }
            return value;
        }

        private TResult GetOriginalValue<TResult>(T target, Expression<Func<T, TResult>> selector)
        {
            var propertyInfo = GetPropertyInfo(selector);

            string propName = propertyInfo.Name;
            TResult originalValue = _OriginalValueDictionary.TryGetValue(propName, out object value) ?
                (TResult)value :
                selector.Compile()(target);
            if (originalValue is IChangeTrackableInternal trackable)
            {
                UnrollGraph unrollGraph = new UnrollGraph();
                originalValue = (TResult)trackable.GetOriginal(unrollGraph);
                unrollGraph.FinishWireUp();
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

        private T GetPoco(T proxy, UnrollGraph unrollGraph, Func<PropertyInfo, object, object> getPropertyProxy, Func<IChangeTrackableInternal, UnrollGraph, object> getPropertyPoco)
        {
            T original = (T)Activator.CreateInstance(typeof(T), nonPublic: true);
            foreach (var property in _Properties.Values)
            {
                object oldPropertyProxyValue = getPropertyProxy(property, proxy);
                if (oldPropertyProxyValue != null)
                {
                    if (oldPropertyProxyValue is IChangeTrackableInternal trackable)
                    {
                        if (unrollGraph.RegisterOrScheduleAssignPocoInsteadOfProxy(trackable, pocoValue => property.SetValue(original, pocoValue, null)))
                        {
                            object newPropertyValue;
                            newPropertyValue = getPropertyPoco(trackable, unrollGraph);
                            unrollGraph.AddMap(new ProxyTargetMap(newPropertyValue, trackable));
                            property.SetValue(original, newPropertyValue, null);
                        }
                    }
                    else if (oldPropertyProxyValue.GetType().GetInterface("IChangeTrackableCollection`1") != null)
                    {
                        if (unrollGraph.RegisterOrScheduleAssignPocoInsteadOfProxy(oldPropertyProxyValue, pocoValue => property.SetValue(original, pocoValue, null)))
                        {
                            System.Collections.IEnumerable originalPropertyValueEnumerable = (System.Collections.IEnumerable)oldPropertyProxyValue;
                            int listCount = originalPropertyValueEnumerable is IEnumerable<object> e ? e.Count() : originalPropertyValueEnumerable.Cast<object>().Count();
                            var list = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(property.PropertyType.GetGenericArguments().First()), listCount);

                            foreach (var proxyListItem in originalPropertyValueEnumerable)
                            {
                                if (unrollGraph.RegisterOrScheduleAssignPocoInsteadOfProxy(proxyListItem, pocoSetter: null))
                                {
                                    object listItem = getPropertyPoco((IChangeTrackableInternal)proxyListItem, unrollGraph);
                                    unrollGraph.AddMap(new ProxyTargetMap(listItem, proxyListItem));
                                }
                            }

                            unrollGraph.AddListSetter(map =>
                            {
                                foreach (var proxyListItem in originalPropertyValueEnumerable)
                                {
                                    list.Add(map(proxyListItem));
                                }
                            });
                            unrollGraph.AddMap(new ProxyTargetMap(list, oldPropertyProxyValue));
                            property.SetValue(original, list, null);
                        }
                    }
                    else
                    {
                        property.SetValue(original, oldPropertyProxyValue, null);
                    }
                }
            }
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
                SetAndRaiseStatusChanged(proxy, parents: new List<object> { proxy }, setStatusEvenIfStatsAddedOrDeleted: true);
                return true;
            }
            return false;
        }

        private void AcceptChanges(object proxy, List<object> parents)
        {
            if (_ChangeTrackingStatus == ChangeStatus.Deleted)
            {
                throw new InvalidOperationException("Can not call AcceptChanges on deleted object");
            }
            ChangeStatus changeTrackingStatusWhenStarted = _ChangeTrackingStatus;
            parents = parents ?? new List<object>(20) { proxy };
            foreach (var child in Utils.GetChildren<System.ComponentModel.IRevertibleChangeTracking>(proxy, parents))
            {
                if (child is IRevertibleChangeTrackingInternal childInternal)
                {
                    childInternal.AcceptChanges(parents);
                }
                else
                {
                    child.AcceptChanges();
                }
            }
            _OriginalValueDictionary.Clear();
            SetAndRaiseStatusChanged(proxy, parents, setStatusEvenIfStatsAddedOrDeleted: true);
            bool anythingChanged = changeTrackingStatusWhenStarted != _ChangeTrackingStatus;
            if (anythingChanged)
            {
                RaiseChangePropertiesChanged(proxy);
            }
        }

        private void RejectChanges(object proxy, List<object> parents)
        {
            ChangeStatus changeTrackingStatusWhenStarted = _ChangeTrackingStatus;
            parents = parents ?? new List<object>(20) { proxy };
            foreach (var child in Utils.GetChildren<System.ComponentModel.IRevertibleChangeTracking>(proxy, parents))
            {
                if (child is IRevertibleChangeTrackingInternal childInternal)
                {
                    childInternal.RejectChanges(parents);
                }
                else
                {
                    child.RejectChanges();
                }
            }
            if (_OriginalValueDictionary.Count > 0)
            {
                _InRejectChanges = true;
                foreach (var changedProperty in _OriginalValueDictionary)
                {
                    _Properties[changedProperty.Key].SetValue(proxy, changedProperty.Value, null);
                }
                _OriginalValueDictionary.Clear();
                _InRejectChanges = false;
            }
            SetAndRaiseStatusChanged(proxy, parents, setStatusEvenIfStatsAddedOrDeleted: true);
            bool anythingChanged = changeTrackingStatusWhenStarted != _ChangeTrackingStatus;
            if (anythingChanged)
            {
                RaiseChangePropertiesChanged(proxy);
            }
        }

        private void UnsubscribeFromChildStatusChanged(string propertyName, object oldChild)
        {
            lock (_StatusChangedEventHandlersLock)
            {
                if (_StatusChangedEventHandlers.TryGetValue(propertyName, out Delegate handler))
                {
                    if (oldChild is IChangeTrackable trackable)
                    {
                        trackable.StatusChanged -= (EventHandler)handler;
                        _StatusChangedEventHandlers.Remove(propertyName);
                        return;
                    }
                    if (oldChild is System.ComponentModel.IBindingList collectionTrackable)
                    {
                        collectionTrackable.ListChanged -= (System.ComponentModel.ListChangedEventHandler)handler;
                        _StatusChangedEventHandlers.Remove(propertyName);
                    }
                }
            }
        }

        private void SubscribeToChildStatusChanged(object proxy, string propertyName, object newValue)
        {
            void Handler(object sender)
            {
                SetAndRaiseStatusChanged(proxy, parents: new List<object> { proxy }, setStatusEvenIfStatsAddedOrDeleted: false);
                if (sender is IRevertibleChangeTrackingInternal trackable && trackable.IsChanged(new List<object> { proxy }) is bool isChanged && (isChanged && _ChangedComplexOrCollectionProperties.Add(propertyName) || !isChanged && _ChangedComplexOrCollectionProperties.Remove(propertyName)))
                {
                    RaiseChangePropertiesChanged(proxy);
                }
            }

            lock (_StatusChangedEventHandlersLock)
            {
                if (!_StatusChangedEventHandlers.ContainsKey(propertyName))
                {
                    if (newValue is IChangeTrackable newChild)
                    {
                        EventHandler newHandler = (sender, e) => Handler(newValue);
                        newChild.StatusChanged += newHandler;
                        _StatusChangedEventHandlers.Add(propertyName, newHandler);
                        return;
                    }
                    if (newValue is System.ComponentModel.IBindingList newCollectionChild)
                    {
                        System.ComponentModel.ListChangedEventHandler newHandler = (sender, e) => Handler(newValue);
                        newCollectionChild.ListChanged += newHandler;
                        _StatusChangedEventHandlers.Add(propertyName, newHandler);
                    }
                }
            }
        }

        private void SetAndRaiseStatusChanged(object proxy, List<object> parents, bool setStatusEvenIfStatsAddedOrDeleted)
        {
            if (_ChangeTrackingStatus == ChangeStatus.Changed || _ChangeTrackingStatus == ChangeStatus.Unchanged || setStatusEvenIfStatsAddedOrDeleted)
            {
                var newChangeStatus = GetNewChangeStatus(proxy, parents);
                if (_ChangeTrackingStatus != newChangeStatus)
                {
                    _ChangeTrackingStatus = newChangeStatus;
                    _StatusChanged(proxy, EventArgs.Empty);
                }
            }
        }

        private ChangeStatus GetNewChangeStatus(object proxy, List<object> parents) => _OriginalValueDictionary.Count == 0 && Utils.GetChildren<IRevertibleChangeTrackingInternal>(proxy, parents).All(c => !c.IsChanged(parents)) ? ChangeStatus.Unchanged : ChangeStatus.Changed;

        private IEnumerable<string> GetChangedProperties()
        {
            switch (_ChangeTrackingStatus)
            {
                case ChangeStatus.Unchanged:
                    return Enumerable.Empty<string>();
                case ChangeStatus.Added:
                case ChangeStatus.Deleted:
                    return _Properties.Keys;
                case ChangeStatus.Changed:
                    return _Properties.Keys.Where(propertyName => _OriginalValueDictionary.Keys.Concat(_ChangedComplexOrCollectionProperties).Contains(propertyName));
                default: throw null;
            }
        }

        private void RaiseChangePropertiesChanged(object sender) => _ChangedPropertiesChanged(sender, EventArgs.Empty);
    }
}
