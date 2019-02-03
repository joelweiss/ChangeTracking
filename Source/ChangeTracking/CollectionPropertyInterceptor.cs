using Castle.DynamicProxy;
using ChangeTracking.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace ChangeTracking
{
    internal class CollectionPropertyInterceptor<T> : IInterceptor, IInterceptorSettings
    {
        private static readonly List<PropertyInfo> _Properties;
        private static Dictionary<string, Action<IInvocation, Dictionary<string, object>, object, bool, bool, Graph>> _Actions;
        private readonly Dictionary<string, object> _Trackables;
        private readonly object _TrackablesLock;
        private readonly bool _MakeComplexPropertiesTrackable;
        private readonly bool _MakeCollectionPropertiesTrackable;
        private readonly Graph _Graph;
        private bool _AreAllPropertiesTrackable;

        public bool IsInitialized { get; set; }

        static CollectionPropertyInterceptor()
        {
            _Actions = new Dictionary<string, Action<IInvocation, Dictionary<string, object>, object, bool, bool, Graph>>();
            _Properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();
            var getters = _Properties.Where(pi => pi.CanRead).Select(pi => new KeyValuePair<string, Action<IInvocation, Dictionary<string, object>, object, bool, bool, Graph>>(pi.Name, GetGetterAction(pi)));
            foreach (var getter in getters)
            {
                _Actions.Add("get_" + getter.Key, getter.Value);
            }
            var setters = _Properties.Where(pi => pi.CanWrite).Select(pi => new KeyValuePair<string, Action<IInvocation, Dictionary<string, object>, object, bool, bool, Graph>>(pi.Name, GetSetterAction(pi)));
            foreach (var setter in setters)
            {
                _Actions.Add("set_" + setter.Key, setter.Value);
            }
        }

        public CollectionPropertyInterceptor(bool makeComplexPropertiesTrackable, bool makeCollectionPropertiesTrackable, Graph graph)
        {
            _MakeComplexPropertiesTrackable = makeComplexPropertiesTrackable;
            _MakeCollectionPropertiesTrackable = makeCollectionPropertiesTrackable;
            _Graph = graph;
            _Trackables = new Dictionary<string, object>();
            _TrackablesLock = new object();
        }

        private static Action<IInvocation, Dictionary<string, object>, object, bool, bool, Graph> GetGetterAction(PropertyInfo propertyInfo)
        {
            if (CanCollectionBeTrackable(propertyInfo))
            {
                return (invocation, trackables, trackablesLock, makeComplexPropertiesTrackable, makeCollectionPropertiesTrackable, graph) =>
                {
                    string propertyName = invocation.Method.PropertyName();
                    lock (trackablesLock)
                    {
                        if (!trackables.ContainsKey(propertyName))
                        {
                            object childTarget = propertyInfo.GetValue(invocation.InvocationTarget, null);
                            if (childTarget == null)
                            {
                                return;
                            }
                            trackables.Add(propertyName, Core.AsTrackableCollectionChild(propertyInfo.PropertyType, childTarget, makeComplexPropertiesTrackable, makeCollectionPropertiesTrackable, graph));
                        }
                        invocation.ReturnValue = trackables[propertyName];
                    }
                };
            }
            return (invocation, _, __, ___, ____, _____) =>
            {
                invocation.Proceed();
            };
        }

        private static Action<IInvocation, Dictionary<string, object>, object, bool, bool, Graph> GetSetterAction(PropertyInfo propertyInfo)
        {
            if (CanCollectionBeTrackable(propertyInfo))
            {
                return (invocation, trackables, trackablesLock, makeComplexPropertiesTrackable, makeCollectionPropertiesTrackable, graph) =>
                {
                    string parentPropertyName = invocation.Method.PropertyName();
                    invocation.Proceed();

                    bool lockWasTaken = false;
                    try
                    {
                        object childTarget = invocation.Arguments[0];
                        object newValue;
                        if (invocation.Arguments[0] == null)
                        {
                            newValue = null;
                        }
                        else if (childTarget is IProxyTargetAccessor && childTarget.GetType().GetInterfaces().FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IChangeTrackableCollection<>)) != null)
                        {
                            newValue = invocation.Arguments[0];
                        }
                        else
                        {
                            Monitor.Enter(trackablesLock, ref lockWasTaken);
                            newValue = Core.AsTrackableCollectionChild(propertyInfo.PropertyType, childTarget, makeComplexPropertiesTrackable, makeCollectionPropertiesTrackable, graph);
                        }
                        if (!lockWasTaken)
                        {
                            Monitor.Enter(trackablesLock, ref lockWasTaken);
                        }
                        trackables[parentPropertyName] = newValue;
                    }
                    finally
                    {
                        if (lockWasTaken)
                        {
                            Monitor.Exit(trackablesLock);
                        }
                    }
                };
            }
            return (invocation, _, __, ___, ____, _____) =>
            {
                invocation.Proceed();
            };
        }

        private static bool CanCollectionBeTrackable(PropertyInfo propertyInfo)
        {
            Type propertyType = propertyInfo.PropertyType;
            Type genericCollectionArgumentType = propertyType.GetGenericArguments().FirstOrDefault();
            return genericCollectionArgumentType != null && propertyType.IsInterface && typeof(ICollection<>).MakeGenericType(genericCollectionArgumentType).IsAssignableFrom(propertyType);
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
            if (_MakeCollectionPropertiesTrackable && _Actions.TryGetValue(invocation.Method.Name, out Action<IInvocation, Dictionary<string, object>, object, bool, bool, Graph> action))
            {
                action(invocation, _Trackables, _TrackablesLock, _MakeComplexPropertiesTrackable, _MakeCollectionPropertiesTrackable, _Graph);
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
            if (!_AreAllPropertiesTrackable)
            {
                MakeAllPropertiesTrackable(proxy);
            }
            lock (_TrackablesLock)
            {
                return _Trackables.Values.ToArray();
            }
        }

        private void MakeAllPropertiesTrackable(object proxy)
        {
            var notTrackedProperties = _Properties.Where(pi => !_Trackables.ContainsKey(pi.Name) && CanCollectionBeTrackable(pi));
            foreach (var property in notTrackedProperties)
            {
                property.GetValue(proxy, null);
            }
            _AreAllPropertiesTrackable = true;
        }
    }
}
