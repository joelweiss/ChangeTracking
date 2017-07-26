﻿using Castle.DynamicProxy;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ChangeTracking
{
    internal class ComplexPropertyInterceptor<T> : IInterceptor, IInterceptorSettings
    {
        private static readonly List<PropertyInfo> _Properties;
        private static Dictionary<string, Action<IInvocation, Dictionary<string, object>, bool, bool>> _Actions;
        private readonly Dictionary<string, object> _Trackables;
        private readonly bool _MakeComplexPropertiesTrackable;
        private readonly bool _MakeCollectionPropertiesTrackable;
        private bool _AreAllPropertesTrackable;

        private const string GET = "get_";
        private const string SET = "set_";

        public bool IsInitialized { get; set; }

        static ComplexPropertyInterceptor()
        {
            _Actions = new Dictionary<string, Action<IInvocation, Dictionary<string, object>, bool, bool>>();
            _Properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();
            var getters = _Properties.Where(pi => pi.CanRead).Select(pi => new KeyValuePair<string, Action<IInvocation, Dictionary<string, object>, bool, bool>>(pi.Name, GetGetterAction(pi)));
            foreach (var getter in getters)
            {
                _Actions.Add(GET + getter.Key, getter.Value);
            }
            var setters = _Properties.Where(pi => pi.CanWrite).Select(pi => new KeyValuePair<string, Action<IInvocation, Dictionary<string, object>, bool, bool>>(pi.Name, GetSetterAction(pi)));
            foreach (var setter in setters)
            {
                _Actions.Add(SET + setter.Key, setter.Value);
            }
        }

        internal ComplexPropertyInterceptor(bool makeComplexPropertiesTrackable, bool makeCollectionPropertiesTrackable)
        {
            _MakeComplexPropertiesTrackable = makeComplexPropertiesTrackable;
            _MakeCollectionPropertiesTrackable = makeCollectionPropertiesTrackable;
            _Trackables = new Dictionary<string, object>();
        }

        private static Action<IInvocation, Dictionary<string, object>, bool, bool> GetGetterAction(PropertyInfo propertyInfo)
        {
            if (CanComplexPropertyBeTrackable(propertyInfo))
            {
                return (invocation, trackables, makeComplexPropertiesTrackable, makeCollectionPropertiesTrackable) =>
                {
                    string propertyName = invocation.GetPropertyName();
                    if (!trackables.ContainsKey(propertyName))
                    {
                        object childTarget = propertyInfo.GetValue(invocation.InvocationTarget, invocation.GetParameter());
                        if (childTarget == null)
                        {
                            return;
                        }
                        trackables.Add(propertyName, Core.AsTrackableChild(propertyInfo.PropertyType, childTarget, null, makeComplexPropertiesTrackable, makeCollectionPropertiesTrackable));
                    }
                    invocation.ReturnValue = trackables[propertyName];
                };
            }
            return (invocation, trackables, makeComplexPropertiesTrackable, makeCollectionPropertiesTrackable) =>
            {
                invocation.Proceed();
            };
        }

        private static Action<IInvocation, Dictionary<string, object>, bool, bool> GetSetterAction(PropertyInfo propertyInfo)
        {
            if (CanComplexPropertyBeTrackable(propertyInfo))
            {
                return (invocation, trackables, makeComplexPropertiesTrackable, makeCollectionPropertiesTrackable) =>
                {
                    string parentPropertyName = invocation.GetPropertyName();
                    invocation.Proceed();

                    object childTarget = invocation.Arguments[0];
                    object newValue;
                    if (invocation.Arguments[0] == null)
                    {
                        newValue = null;
                    }
                    if (childTarget.GetType().GetInterfaces().FirstOrDefault(t => t == typeof(IChangeTrackable<>)) != null)
                    {
                        newValue = invocation.Arguments[0];
                    }
                    else
                    {
                        newValue = Core.AsTrackableChild(propertyInfo.PropertyType, childTarget, null, makeComplexPropertiesTrackable, makeCollectionPropertiesTrackable);
                    }
                    trackables[parentPropertyName] = newValue;
                };
            }
            return (invocation, trackables, makeComplexPropertiesTrackable, makeCollectionPropertiesTrackable) =>
            {
                invocation.Proceed();
            };
        }

        private static bool CanComplexPropertyBeTrackable(PropertyInfo propertyInfo)
        {
            Type propertyType = propertyInfo.PropertyType;
            bool ignored = propertyInfo.GetCustomAttributes(typeof(IgnoreAttribute), true).Any();
            if (ignored)
                return false;

            return 
                // allow user to define properties that have an interface as property type
                // to avoid conflicts with the CollectionPropertyInterceptor, 
                // we exclude all properties whose type implements the "System.Collections.IEnumerable" interface
                ((propertyType.IsInterface && !typeof(IEnumerable).IsAssignableFrom(propertyType)) 
                || 
                (propertyType.IsClass &&
                    !propertyType.IsSealed &&
                    propertyType.GetConstructor(Type.EmptyTypes) != null &&
                    propertyType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Any(pi => pi.CanRead && pi.CanWrite) &&
                    propertyType.GetProperties(BindingFlags.Public | BindingFlags.Instance).All(pi => pi.CanRead && pi.CanWrite && pi.GetAccessors()[0].IsVirtual)));
        }

        public void Intercept(IInvocation invocation)
        {
            if (!IsInitialized)
            {
                return;
            }
            if (invocation.Method.Name == "get_ComplexPropertyTrackables")
            {
                invocation.ReturnValue = ComplexPropertyTrackables(invocation.Proxy);
                return;
            }
            Action<IInvocation, Dictionary<string, object>, bool, bool> action;
            if (_MakeComplexPropertiesTrackable && _Actions.TryGetValue(invocation.Method.Name, out action))
            {
                action(invocation, _Trackables, _MakeComplexPropertiesTrackable, _MakeCollectionPropertiesTrackable);
            }
            else
            {
                invocation.Proceed();
            }
        }

        private IEnumerable<object> ComplexPropertyTrackables(object proxy)
        {
            if (!_MakeComplexPropertiesTrackable)
            {
                return Enumerable.Empty<object>();
            }
            if (!_AreAllPropertesTrackable)
            {
                MakeAllPropertiesTrackable(proxy);
            }
            return _Trackables.Values;
        }

        private void MakeAllPropertiesTrackable(object proxy)
        {
            var notTrackedProperties = _Properties.Where(pi => !_Trackables.ContainsKey(pi.Name) && CanComplexPropertyBeTrackable(pi));
            foreach (var property in notTrackedProperties)
            {
                property.GetValue(proxy, null);
            }
            _AreAllPropertesTrackable = true;
        }
    }
}
