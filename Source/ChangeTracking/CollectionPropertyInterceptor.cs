using Castle.DynamicProxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ChangeTracking
{
    internal class CollectionPropertyInterceptor<T> : IInterceptor, IInterceptorSettings
    {
        private static readonly List<PropertyInfo> _Properties;
        private static Dictionary<string, Action<IInvocation, Dictionary<string, object>, bool, bool>> _Actions;
        private readonly Dictionary<string, object> _Trackables;
        private readonly bool _MakeComplexPropertiesTrackable;
        private readonly bool _MakeCollectionPropertiesTrackable;
        private bool _AreAllPropertesTrackable;

        public bool IsInitialized { get; set; }

        static CollectionPropertyInterceptor()
        {
            _Actions = new Dictionary<string, Action<IInvocation, Dictionary<string, object>, bool, bool>>();
            _Properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();
            var getters = _Properties.Where(pi => pi.CanRead).Select(pi => new KeyValuePair<string, Action<IInvocation, Dictionary<string, object>, bool, bool>>(pi.Name, GetGetterAction(pi)));
            foreach (var getter in getters)
            {
                _Actions.Add("get_" + getter.Key, getter.Value);
            }
            var setters = _Properties.Where(pi => pi.CanWrite).Select(pi => new KeyValuePair<string, Action<IInvocation, Dictionary<string, object>, bool, bool>>(pi.Name, GetSetterAction(pi)));
            foreach (var setter in setters)
            {
                _Actions.Add("set_" + setter.Key, setter.Value);
            }
        }

        public CollectionPropertyInterceptor(bool makeComplexPropertiesTrackable, bool makeCollectionPropertiesTrackable)
        {
            _MakeComplexPropertiesTrackable = makeComplexPropertiesTrackable;
            _MakeCollectionPropertiesTrackable = makeCollectionPropertiesTrackable;
            _Trackables = new Dictionary<string, object>();
        }

        private static Action<IInvocation, Dictionary<string, object>, bool, bool> GetGetterAction(PropertyInfo propertyInfo)
        {
            if (CanCollectionBeTrackable(propertyInfo))
            {
                return (invocation, trackables, makeComplexPropertiesTrackable, makeCollectionPropertiesTrackable) =>
                {
                    string propertyName = invocation.Method.PropertyName();
                    if (!trackables.ContainsKey(propertyName))
                    {
                        object childTarget = propertyInfo.GetValue(invocation.InvocationTarget, null);
                        if (childTarget == null)
                        {
                            return;
                        }
                        trackables.Add(propertyName, Core.AsTrackableCollectionChild(propertyInfo.PropertyType, childTarget, makeComplexPropertiesTrackable, makeCollectionPropertiesTrackable));
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
            if (CanCollectionBeTrackable(propertyInfo))
            {
                return (invocation, trackables, makeComplexPropertiesTrackable, makeCollectionPropertiesTrackable) =>
                {
                    string parentPropertyName = invocation.Method.PropertyName();
                    invocation.Proceed();

                    object childTarget = invocation.Arguments[0];
                    object newValue;
                    if (invocation.Arguments[0] == null)
                    {
                        newValue = null;
                    }
                    else if (childTarget.GetType().GetInterfaces().FirstOrDefault(t => t == typeof(IChangeTrackableCollection<>)) != null)
                    {
                        newValue = invocation.Arguments[0];
                    }
                    else
                    {
                        newValue = Core.AsTrackableCollectionChild(propertyInfo.PropertyType, childTarget, makeComplexPropertiesTrackable, makeCollectionPropertiesTrackable);
                    }
                    trackables[parentPropertyName] = newValue;
                };
            }
            return (invocation, trackables, makeComplexPropertiesTrackable, makeCollectionPropertiesTrackable) =>
            {
                invocation.Proceed();
            };
        }

        private static bool CanCollectionBeTrackable(PropertyInfo propertyInfo)
        {
            Type propertyType = propertyInfo.PropertyType;
            Type genericCollectionArgumenType = propertyType.GetGenericArguments().FirstOrDefault();
            return genericCollectionArgumenType != null && propertyType.IsInterface && typeof(ICollection<>).MakeGenericType(genericCollectionArgumenType).IsAssignableFrom(propertyType);
        }

        public void Intercept(IInvocation invocation)
        {
            if (!IsInitialized)
            {
                return;
            }
            if (invocation.Method.Name == "get_CollectionPropertyTrackables")
            {
                invocation.ReturnValue = CollectionPropertyTrackables(invocation.Proxy);
                return;
            }
            Action<IInvocation, Dictionary<string, object>, bool, bool> action;
            if (_MakeCollectionPropertiesTrackable && _Actions.TryGetValue(invocation.Method.Name, out action))
            {
                action(invocation, _Trackables, _MakeComplexPropertiesTrackable, _MakeCollectionPropertiesTrackable);
            }
            else
            {
                invocation.Proceed();
            }
        }

        private IEnumerable<object> CollectionPropertyTrackables(object proxy)
        {
            if (!_MakeCollectionPropertiesTrackable)
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
            var notTrackedProperties = _Properties.Where(pi => !_Trackables.ContainsKey(pi.Name) && CanCollectionBeTrackable(pi));
            foreach (var property in notTrackedProperties)
            {
                property.GetValue(proxy, null);
            }
            _AreAllPropertesTrackable = true;
        }
    }
}
