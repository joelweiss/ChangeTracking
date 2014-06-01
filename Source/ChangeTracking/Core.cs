using Castle.DynamicProxy;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
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

        internal static object AsTrackableObject(Type type, object target, ChangeStatus status = ChangeStatus.Unchanged, Action<object> notifyParentItemCanceled = null)
        {
            if (type == typeof(ICollection))
            {
                throw new InvalidOperationException("Only IList<T> and ICollection<T> are supported");
            }
            Type T = type.GetGenericArguments().FirstOrDefault();
            if (T != null && typeof(ICollection<>).MakeGenericType(T).IsAssignableFrom(type))
            {
                object proxy = _ProxyGenerator.CreateInterfaceProxyWithTarget(typeof(IList<>).MakeGenericType(T),
                                   new[] { typeof(IChangeTrackableCollection<>).MakeGenericType(T), typeof(IBindingList) },
                                   target,
                                   _Options,
                                   (IInterceptor)Activator.CreateInstance(typeof(ChangeTrackingCollectionInterceptor<>).MakeGenericType(T), target));
                return proxy;
            }
            return _ProxyGenerator.CreateClassProxyWithTarget(type,
                         new[] { typeof(IChangeTrackable<>).MakeGenericType(type), typeof(IChangeTrackingManager<>).MakeGenericType(type), typeof(IEditableObject), typeof(System.ComponentModel.INotifyPropertyChanged) },
                         target,
                         _Options,
                         (IInterceptor)Activator.CreateInstance(typeof(ChangeTrackingInterceptor<>).MakeGenericType(type), status),
                         (IInterceptor)Activator.CreateInstance(typeof(EditableObjectInterceptor<>).MakeGenericType(type), notifyParentItemCanceled),
                         (IInterceptor)Activator.CreateInstance(typeof(NotifyPropertyChangedInterceptor<>).MakeGenericType(type)));

        }

        public static T AsTrackable<T>(this T target, ChangeStatus status = ChangeStatus.Unchanged, Action<T> notifyParentItemCanceled = null) where T : class
        {
            if (target as ICollection != null)
            {
                throw new InvalidOperationException("Only IList<T>, List<T> and ICollection<T> are supported");
            }

            object proxy = _ProxyGenerator.CreateClassProxyWithTarget(typeof(T),
                new[] { typeof(IChangeTrackable<T>), typeof(IChangeTrackingManager<T>), typeof(IEditableObject), typeof(System.ComponentModel.INotifyPropertyChanged) },
                target, _Options, new ChangeTrackingInterceptor<T>(status), new EditableObjectInterceptor<T>(notifyParentItemCanceled), new NotifyPropertyChangedInterceptor<T>());
            return (T)proxy;
        }

        public static ICollection<T> AsTrackable<T>(this System.Collections.ObjectModel.Collection<T> target) where T : class
        {
            return ((ICollection<T>)target).AsTrackable();
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

        public static IList<T> AsTrackable<T>(this List<T> target) where T : class
        {
            return ((IList<T>)target).AsTrackable();
        }

        public static IList<T> AsTrackable<T>(this IList<T> target) where T : class
        {
            if (target.OfType<IChangeTrackable<T>>().Any(ct => ct.ChangeTrackingStatus != ChangeStatus.Unchanged))
            {
                throw new InvalidOperationException("some items in the collection are already being tracked");
            }
            object proxy = _ProxyGenerator.CreateInterfaceProxyWithTarget(typeof(IList<T>),
                new[] { typeof(IChangeTrackableCollection<T>), typeof(IBindingList), typeof(ICancelAddNew) }, target, _Options, new ChangeTrackingCollectionInterceptor<T>(target));
            return (IList<T>)proxy;
        }
    }
}
