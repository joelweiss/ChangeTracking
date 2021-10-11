﻿using Castle.DynamicProxy;
using ChangeTracking.Internal;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace ChangeTracking
{
    internal class NotifyPropertyChangedInterceptor<T> : IInterceptor, IInterceptorSettings where T : class
    {
        private static Dictionary<string, PropertyInfo> _Properties;
        private readonly Dictionary<string, PropertyChangedEventHandler> _PropertyChangedEventHandlers;
        private readonly object _PropertyChangedEventHandlersLock;
        private readonly Dictionary<string, ListChangedEventHandler> _ListChangedEventHandlers;
        private readonly object _ListChangedEventHandlersLock;
        private readonly HashSet<string> _CurrentlyExecutingPropertyChangedEvents;
        private readonly object _CurrentlyExecutingPropertyChangedEventsLock;

        public bool IsInitialized { get; set; }

        static NotifyPropertyChangedInterceptor()
        {
            _Properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).ToDictionary(pi => pi.Name);
        }

        public NotifyPropertyChangedInterceptor(ChangeTrackingInterceptor<T> changeTrackingInterceptor)
        {
            _PropertyChangedEventHandlers = new Dictionary<string, PropertyChangedEventHandler>();
            _PropertyChangedEventHandlersLock = new object();
            _ListChangedEventHandlers = new Dictionary<string, ListChangedEventHandler>();
            _ListChangedEventHandlersLock = new object();
            _CurrentlyExecutingPropertyChangedEvents = new HashSet<string>();
            _CurrentlyExecutingPropertyChangedEventsLock = new object();
            changeTrackingInterceptor._StatusChanged += (o, e) => RaisePropertyChanged(o, nameof(IChangeTrackable.ChangeTrackingStatus));
            changeTrackingInterceptor._ChangedPropertiesChanged += (o, e) => RaisePropertyChanged(o, nameof(IChangeTrackable.ChangedProperties));
            PropertyChanged += delegate { };
        }

        public void Intercept(IInvocation invocation)
        {
            if (!IsInitialized)
            {
                return;
            }
            if (invocation.Method.IsSetter())
            {
                string propertyName = invocation.Method.PropertyName();
                var previousValue = _Properties[propertyName].GetValue(invocation.Proxy, null);
                invocation.Proceed();
                var newValue = _Properties[propertyName].GetValue(invocation.Proxy, null);
                if (!Equals(previousValue, newValue))
                {
                    RaisePropertyChanged(invocation.Proxy, propertyName);
                    RaisePropertyChanged(invocation.Proxy, nameof(IChangeTrackable.ChangedProperties));
                }
                if (!ReferenceEquals(previousValue, newValue))
                {
                    UnsubscribeFromChildPropertyChanged(propertyName, previousValue);
                    SubscribeToChildPropertyChanged(invocation, propertyName, newValue);
                    UnsubscribeFromChildListChanged(propertyName, previousValue);
                    SubscribeToChildListChanged(invocation, propertyName, newValue);
                }
                return;
            }
            if (invocation.Method.IsGetter())
            {
                invocation.Proceed();
                string propertyName = invocation.Method.PropertyName();
                SubscribeToChildPropertyChanged(invocation, propertyName, invocation.ReturnValue);
                SubscribeToChildListChanged(invocation, propertyName, invocation.ReturnValue);
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

        private void UnsubscribeFromChildPropertyChanged(string propertyName, object oldChild)
        {
            if (oldChild is INotifyPropertyChanged trackable)
            {
                lock (_PropertyChangedEventHandlersLock)
                {
                    if (_PropertyChangedEventHandlers.TryGetValue(propertyName, out PropertyChangedEventHandler handler))
                    {
                        trackable.PropertyChanged -= handler;
                        _PropertyChangedEventHandlers.Remove(propertyName);
                    }
                }
            }
        }

        private void SubscribeToChildPropertyChanged(IInvocation invocation, string propertyName, object newValue)
        {
            if (newValue is INotifyPropertyChanged newChild)
            {
                lock (_PropertyChangedEventHandlersLock)
                {
                    if (!_PropertyChangedEventHandlers.ContainsKey(propertyName))
                    {
                        void newHandler(object sender, PropertyChangedEventArgs e) => RaisePropertyChanged(invocation.Proxy, propertyName);
                        newChild.PropertyChanged += newHandler;
                        _PropertyChangedEventHandlers.Add(propertyName, newHandler);
                    }
                }
            }
        }

        private void UnsubscribeFromChildListChanged(string propertyName, object oldChild)
        {
            if (oldChild is IBindingList trackable)
            {
                lock (_ListChangedEventHandlersLock)
                {
                    if (_ListChangedEventHandlers.TryGetValue(propertyName, out ListChangedEventHandler handler))
                    {
                        trackable.ListChanged -= handler;
                        _ListChangedEventHandlers.Remove(propertyName);
                    }
                }
            }
        }

        private void SubscribeToChildListChanged(IInvocation invocation, string propertyName, object newValue)
        {
            if (newValue is IBindingList newChild)
            {
                lock (_ListChangedEventHandlersLock)
                {
                    if (!_ListChangedEventHandlers.ContainsKey(propertyName))
                    {
                        void newHandler(object sender, ListChangedEventArgs e) => RaisePropertyChanged(invocation.Proxy, propertyName);
                        newChild.ListChanged += newHandler;
                        _ListChangedEventHandlers.Add(propertyName, newHandler);
                    }
                }
            }
        }

        private event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(object proxy, string propertyName)
        {
            lock (_CurrentlyExecutingPropertyChangedEventsLock)
            {
                if (!_CurrentlyExecutingPropertyChangedEvents.Add(propertyName))
                {
                    return;
                }
            }
            try
            {
                PropertyChanged(proxy, new PropertyChangedEventArgs(propertyName));
            }
            finally
            {
                lock (_CurrentlyExecutingPropertyChangedEventsLock)
                {
                    _CurrentlyExecutingPropertyChangedEvents.Remove(propertyName);
                }
            }
        }
    }
}
