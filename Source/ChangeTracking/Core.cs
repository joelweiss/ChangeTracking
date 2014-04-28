using Castle.DynamicProxy;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChangeTracking
{
    public static class Core
    {
        private static ProxyGenerator _ProxyGenerator;
        private static ProxyGenerationOptions _Options;

        static Core()
        {
            _ProxyGenerator = new ProxyGenerator();

            _Options = new ProxyGenerationOptions
            {
                Hook = new ChangeTrackingProxyGenerationHook(),
                Selector = new ChangeTrackingInterceptorSelector()
            };
        }

        public static T AsTrackable<T>(this T target, ChangeStatus status = ChangeStatus.Unchanged) where T : class
        {
            object proxy = _ProxyGenerator.CreateClassProxyWithTarget(typeof(T), new[] { typeof(IChangeTrackable<T>), typeof(IChangeTrackingManager<T>) }, target, _Options, new ChangeTrackingInterceptor<T>(status));
            return (T)proxy;
        }

        public static IChangeTrackable<T> CastToIChangeTrackable<T>(this T target) where T : class
        {
            return (IChangeTrackable<T>)target;
        }

        public static ICollection<T> AsTrackable<T>(this ICollection<T> target) where T : class
        {
            var list = target as IList<T>;
            if (target == null)
            {
                list = target.ToList();
            }
            return list.AsTrackable();
        }

        public static IList<T> AsTrackable<T>(this IList<T> target) where T : class
        {
            if (target.OfType<IChangeTrackable<T>>().Where(ct => ct.ChangeTrackingStatus != ChangeStatus.Unchanged).Any())
            {
                throw new InvalidOperationException("some items in the collection are already being tracked");
            }
            object proxy = _ProxyGenerator.CreateInterfaceProxyWithTarget(typeof(IList<T>), new[] { typeof(IChangeTrackableCollection<T>) }, target, _Options, new ChangeTrackingCollectionInterceptor<T>(target));
            return (IList<T>)proxy;
        }

        public static IChangeTrackableCollection<T> CastToIChangeTrackable<T>(this ICollection<T> target) where T : class
        {
            return (IChangeTrackableCollection<T>)target;
        }

        public static IChangeTrackableCollection<T> CastToIChangeTrackable<T>(this IList<T> target) where T : class
        {
            return (IChangeTrackableCollection<T>)target;
        }
    }
}
