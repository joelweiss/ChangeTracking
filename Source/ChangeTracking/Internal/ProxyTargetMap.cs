using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace ChangeTracking.Internal
{
    internal class ProxyTargetMap
    {
        private readonly WeakReference _Target;

        public ProxyTargetMap(object target, object proxy)
        {
            _Target = new WeakReference(target);
            Proxy = proxy;
        }

        public object Target => _Target.Target;

        public object Proxy { get; }
    }

    internal class Graph : Collection<ProxyTargetMap>
    {
        private readonly ReaderWriterLockSlim _ClearLock;
        private int _InvokeCount;

        public Graph()
        {
            _ClearLock = new ReaderWriterLockSlim();
        }

        internal ProxyTargetMap GetExistingProxyForTarget(object target)
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
}