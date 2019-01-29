using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace ChangeTracking.Internal
{
    internal class ProxyWeakTargetMap
    {
        private readonly WeakReference _Target;

        public ProxyWeakTargetMap(object target, object proxy)
        {
            _Target = new WeakReference(target);
            Proxy = proxy;
        }

        public object Target => _Target.Target;

        public object Proxy { get; }
    }

    internal class Graph : Collection<ProxyWeakTargetMap>
    {
        private readonly ReaderWriterLockSlim _ClearLock;
        private int _InvokeCount;

        public Graph()
        {
            _ClearLock = new ReaderWriterLockSlim();
        }

        internal ProxyWeakTargetMap GetExistingProxyForTarget(object target)
        {
            Interlocked.Increment(ref _InvokeCount);
            int invokeCount = Interlocked.CompareExchange(ref _InvokeCount, 0, 100);
            if (invokeCount == 100)
            {
                _ClearLock.EnterWriteLock();
                try
                {
                    for (int i = Count - 1; i >= 0; i--)
                    {
                        if (this[i].Target is null)
                        {
                            RemoveAt(i);
                        }
                    }
                }
                finally
                {
                    _ClearLock.ExitWriteLock();
                }
            }
            _ClearLock.EnterReadLock();
            try
            {
                return Items.FirstOrDefault(t => ReferenceEquals(target, t.Target));
            }
            finally
            {
                _ClearLock.ExitReadLock();
            }
        }
    }

    internal class ProxyTargetMap
    {
        public ProxyTargetMap(object target, object proxy)
        {
            Target = target;
            Proxy = proxy;
        }

        public object Target { get; }
        public object Proxy { get; }
    }

    internal class UnrollGraph
    {
        private readonly List<ProxyTargetMap> _ProxyTargetMaps;
        private readonly List<object> _RegisteredProxies;
        private readonly List<Action> _PocoSetters;
        private readonly List<Action<Func<object, object>>> _ListSetters;

        public UnrollGraph()
        {
            _ProxyTargetMaps = new List<ProxyTargetMap>();
            _RegisteredProxies = new List<object>();
            _PocoSetters = new List<Action>();
            _ListSetters = new List<Action<Func<object, object>>>();
        }

        internal bool RegisterOrScheduleAssignPocoInsteadOfProxy(object proxy, Action<object> pocoSetter)
        {
            object map = _RegisteredProxies.FirstOrDefault(p => ReferenceEquals(p, proxy)) ?? _ProxyTargetMaps.FirstOrDefault(m => ReferenceEquals(m.Proxy, proxy));
            if (map is null)
            {
                _RegisteredProxies.Add(proxy);
                return true;
            }
            if (pocoSetter != null)
            {
                _PocoSetters.Add(() => pocoSetter(_ProxyTargetMaps.First(m => ReferenceEquals(m.Proxy, proxy)).Target)); 
            }
            return false;
        }

        internal void AddMap(ProxyTargetMap map)
        {
            _RegisteredProxies.Remove(map.Proxy);
            _ProxyTargetMaps.Add(map);
        }

        internal void AddListSetter(Action<Func<object, object>> listSetter) => _ListSetters.Add(listSetter);

        internal void FinishWireUp()
        {
            foreach (Action action in _PocoSetters)
            {
                action();
            }
            foreach (Action<Func<object, object>> action in _ListSetters)
            {
                action(proxy => _ProxyTargetMaps.First(m => ReferenceEquals(m.Proxy, proxy)).Target);
            }
        }
    }
}