using Castle.DynamicProxy;
using ChangeTracking.Internal;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ChangeTracking
{
    public static class Core
    {
        private static readonly ProxyGenerator _ProxyGenerator;
        private static readonly IInterceptorSelector _Selector;
        private static readonly ConcurrentDictionary<Type, ProxyGenerationOptions> _Options;
        private static readonly MethodInfo _SetValueMethodInfo;
        private static readonly ConcurrentDictionary<Type, Action<object, object>> _FieldCopiers;

        static Core()
        {
            _ProxyGenerator = new ProxyGenerator();
            _Selector = new ChangeTrackingInterceptorSelector();
            _Options = new ConcurrentDictionary<Type, ProxyGenerationOptions>();
            _SetValueMethodInfo = typeof(FieldInfo).GetMethod(nameof(FieldInfo.SetValue), new[] { typeof(object), typeof(object) });
            _FieldCopiers = new ConcurrentDictionary<Type, Action<object, object>>();
        }

        private static ProxyGenerationOptions CreateOptions(Type type) => new ProxyGenerationOptions
        {
            Hook = new ChangeTrackingProxyGenerationHook(type),
            Selector = _Selector
        };

        private static ProxyGenerationOptions GetOptions(Type type) => _Options.GetOrAdd(type, CreateOptions);

        private static void CopyFieldsAndProperties<T>(T source, object target) => CopyFieldsAndProperties(typeof(T), source, target);

        private static void CopyFieldsAndProperties(Type type, object source, object target)
        {
            Action<object, object> copier = _FieldCopiers.GetOrAdd(type, typeCopying =>
            {
                List<FieldInfo> fieldInfosToCopy = typeCopying
                    .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(fi => !(fi.Name.StartsWith("<") && fi.Name.EndsWith(">k__BackingField")))
                    .ToList();
                List<PropertyInfo> propertyInfosToCopy = typeCopying
                   .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                   .Where(pi => pi.CanRead && pi.CanWrite && Utils.IsMarkedDoNotTrack(pi))
                   .ToList();
                if (fieldInfosToCopy.Count == 0 && propertyInfosToCopy.Count == 0)
                {
                    return null;
                }

                ParameterExpression sourceParameter = Expression.Parameter(typeof(object), "source");
                ParameterExpression targetParameter = Expression.Parameter(typeof(object), "target");
                UnaryExpression sourceAsType = Expression.Convert(sourceParameter, typeCopying);
                UnaryExpression targetAsType = Expression.Convert(targetParameter, typeCopying);

                IEnumerable<Expression> setFieldsExpressions = fieldInfosToCopy.Select<FieldInfo, Expression>(fi =>
                {
                    if (fi.IsInitOnly)
                    {
                        return Expression.Call(Expression.Constant(fi), _SetValueMethodInfo, targetAsType, Expression.Field(sourceAsType, fi));
                    }
                    return Expression.Assign(Expression.Field(targetAsType, fi), Expression.Field(sourceAsType, fi));
                });
                IEnumerable<Expression> setPropertiesExpressions = propertyInfosToCopy.Select<PropertyInfo, Expression>(pi =>
                {
                    return Expression.Assign(Expression.Property(targetAsType, pi), Expression.Property(sourceAsType, pi));
                });
                BlockExpression block = Expression.Block(setFieldsExpressions.Concat(setPropertiesExpressions));

                return Expression.Lambda<Action<object, object>>(block, sourceParameter, targetParameter).Compile();
            });
            copier?.Invoke(source, target);
        }

        internal static object AsTrackableCollectionChild(Type type, object target, ChangeTrackingSettings changeTrackingSettings, Graph graph)
        {
            ThrowIfTargetIsProxy(target);
            ProxyWeakTargetMap existing = graph.GetExistingProxyForTarget(target);
            if (existing != null)
            {
                return existing.Proxy;
            }
            Type genericArgument = type.GetGenericArguments().First();
            object proxy = _ProxyGenerator.CreateInterfaceProxyWithTarget(typeof(IList<>).MakeGenericType(genericArgument),
                        new[] { typeof(IChangeTrackableCollection<>).MakeGenericType(genericArgument), typeof(IRevertibleChangeTrackingInternal), typeof(IBindingList), typeof(ICancelAddNew), typeof(INotifyCollectionChanged) },
                        target,
                        GetOptions(genericArgument),
                        (IInterceptor)CreateInstance(typeof(ChangeTrackingCollectionInterceptor<>).MakeGenericType(genericArgument), target, changeTrackingSettings, graph));
            graph.Add(new ProxyWeakTargetMap(target, proxy));
            return proxy;
        }

        private static void ThrowIfTargetIsProxy(object target)
        {
            if (target is IRevertibleChangeTrackingInternal)
            {
                throw new InvalidOperationException("The target is already a Trackable Proxy");
            }
        }

        internal static object AsTrackableChild(Type type, object target, Action<object> notifyParentItemCanceled, ChangeTrackingSettings changeTrackingSettings, Internal.Graph graph)
        {
            ThrowIfTargetIsProxy(target);
            ProxyWeakTargetMap existing = graph.GetExistingProxyForTarget(target);
            if (existing != null)
            {
                return existing.Proxy;
            }
            var changeTrackingInterceptor = CreateInstance(typeof(ChangeTrackingInterceptor<>).MakeGenericType(type), ChangeStatus.Unchanged);
            var notifyPropertyChangedInterceptor = CreateInstance(typeof(NotifyPropertyChangedInterceptor<>).MakeGenericType(type), changeTrackingInterceptor);
            var editableObjectInterceptor = CreateInstance(typeof(EditableObjectInterceptor<>).MakeGenericType(type), notifyParentItemCanceled);
            var complexPropertyInterceptor = CreateInstance(typeof(ComplexPropertyInterceptor<>).MakeGenericType(type), changeTrackingSettings, graph);
            var collectionPropertyInterceptor = CreateInstance(typeof(CollectionPropertyInterceptor<>).MakeGenericType(type), changeTrackingSettings, graph);
            object proxy = _ProxyGenerator.CreateClassProxyWithTarget(type,
                         new[] { typeof(IChangeTrackableInternal), typeof(IRevertibleChangeTrackingInternal), typeof(IChangeTrackable<>).MakeGenericType(type), typeof(IChangeTrackingManager), typeof(IComplexPropertyTrackable), typeof(ICollectionPropertyTrackable), typeof(IEditableObjectInternal), typeof(INotifyPropertyChanged) },
                         target,
                         GetOptions(type),
                         (IInterceptor)notifyPropertyChangedInterceptor,
                         (IInterceptor)changeTrackingInterceptor,
                         (IInterceptor)editableObjectInterceptor,
                         (IInterceptor)complexPropertyInterceptor,
                         (IInterceptor)collectionPropertyInterceptor);
            CopyFieldsAndProperties(type: type, source: target, target: proxy);
            ((IInterceptorSettings)notifyPropertyChangedInterceptor).IsInitialized = true;
            ((IInterceptorSettings)changeTrackingInterceptor).IsInitialized = true;
            ((IInterceptorSettings)editableObjectInterceptor).IsInitialized = true;
            ((IInterceptorSettings)complexPropertyInterceptor).IsInitialized = true;
            ((IInterceptorSettings)collectionPropertyInterceptor).IsInitialized = true;
            graph.Add(new ProxyWeakTargetMap(target, proxy));
            return proxy;
        }

        private static object CreateInstance(Type type, params object[] args)
        {
            return Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, args, null);
        }

        public static T AsTrackable<T>(this T target) where T : class
        {
            return ChangeTrackingFactory.Default.AsTrackable(target);
        }

        public static T AsTrackable<T>(this T target, ChangeStatus status = ChangeStatus.Unchanged, bool makeComplexPropertiesTrackable = true, bool makeCollectionPropertiesTrackable = true) where T : class
        {
            return ChangeTrackingFactory.Default.AsTrackable(target, new ChangeTrackingSettings(makeComplexPropertiesTrackable, makeCollectionPropertiesTrackable), status);
        }

        internal static T AsTrackable<T>(this T target, ChangeStatus status, Action<T> notifyParentListItemCanceled, ChangeTrackingSettings changeTrackingSettings, Graph graph) where T : class
        {
            ThrowIfTargetIsProxy(target);
            ProxyWeakTargetMap existing = graph.GetExistingProxyForTarget(target);
            if (existing != null)
            {
                return (T)existing.Proxy;
            }

            //if T was ICollection<T> it would of gone to one of the other overloads
            if (target as ICollection != null)
            {
                throw new InvalidOperationException("Only IList<T>, List<T> and ICollection<T> are supported");
            }

            var changeTrackingInterceptor = new ChangeTrackingInterceptor<T>(status);
            var notifyPropertyChangedInterceptor = new NotifyPropertyChangedInterceptor<T>(changeTrackingInterceptor);
            var editableObjectInterceptor = new EditableObjectInterceptor<T>(notifyParentListItemCanceled);
            var complexPropertyInterceptor = new ComplexPropertyInterceptor<T>(changeTrackingSettings, graph);
            var collectionPropertyInterceptor = new CollectionPropertyInterceptor<T>(changeTrackingSettings, graph);
            object proxy = _ProxyGenerator.CreateClassProxyWithTarget(typeof(T),
                new[] { typeof(IChangeTrackableInternal), typeof(IRevertibleChangeTrackingInternal), typeof(IChangeTrackable<T>), typeof(IChangeTrackingManager), typeof(IComplexPropertyTrackable), typeof(ICollectionPropertyTrackable), typeof(IEditableObjectInternal), typeof(INotifyPropertyChanged) },
                target,
                GetOptions(typeof(T)),
                notifyPropertyChangedInterceptor,
                changeTrackingInterceptor,
                editableObjectInterceptor,
                complexPropertyInterceptor,
                collectionPropertyInterceptor);
            CopyFieldsAndProperties(source: target, target: proxy);
            notifyPropertyChangedInterceptor.IsInitialized = true;
            changeTrackingInterceptor.IsInitialized = true;
            editableObjectInterceptor.IsInitialized = true;
            complexPropertyInterceptor.IsInitialized = true;
            collectionPropertyInterceptor.IsInitialized = true;
            graph.Add(new ProxyWeakTargetMap(target, proxy));
            return (T)proxy;
        }

        internal static ICollection<T> AsTrackableCollection<T>(ICollection<T> target, ChangeTrackingSettings changeTrackingSettings) where T : class
        {
            if (!(target is IList<T> list))
            {
                list = target.ToList();
            }
            ThrowIfTargetIsProxy(list);
            if (list.OfType<IChangeTrackable<T>>().Any(ct => ct.ChangeTrackingStatus != ChangeStatus.Unchanged))
            {
                throw new InvalidOperationException("some items in the collection are already being tracked");
            }

            Graph graph = new Graph();
            object proxy = _ProxyGenerator.CreateInterfaceProxyWithTarget(typeof(IList<T>),
                new[] { typeof(IChangeTrackableCollection<T>), typeof(IRevertibleChangeTrackingInternal), typeof(IBindingList), typeof(ICancelAddNew), typeof(INotifyCollectionChanged) }, list, GetOptions(typeof(T)), new ChangeTrackingCollectionInterceptor<T>(list, changeTrackingSettings, graph));
            graph.Add(new ProxyWeakTargetMap(list, proxy));
            return (ICollection<T>)proxy;
        }

        public static ICollection<T> AsTrackable<T>(this System.Collections.ObjectModel.Collection<T> target) where T : class
        {
            return ChangeTrackingFactory.Default.AsTrackableCollection(target);
        }

        public static ICollection<T> AsTrackable<T>(this System.Collections.ObjectModel.Collection<T> target, bool makeComplexPropertiesTrackable = true, bool makeCollectionPropertiesTrackable = true) where T : class
        {
            return ChangeTrackingFactory.Default.AsTrackableCollection(target, new ChangeTrackingSettings(makeComplexPropertiesTrackable, makeCollectionPropertiesTrackable));
        }

        public static ICollection<T> AsTrackable<T>(this ICollection<T> target) where T : class
        {
            return ChangeTrackingFactory.Default.AsTrackableCollection(target);
        }

        public static ICollection<T> AsTrackable<T>(this ICollection<T> target, bool makeComplexPropertiesTrackable, bool makeCollectionPropertiesTrackable) where T : class
        {
            return ChangeTrackingFactory.Default.AsTrackableCollection(target, new ChangeTrackingSettings(makeComplexPropertiesTrackable, makeCollectionPropertiesTrackable));
        }

        public static IList<T> AsTrackable<T>(this List<T> target) where T : class
        {
            return (IList<T>)ChangeTrackingFactory.Default.AsTrackableCollection(target);
        }

        public static IList<T> AsTrackable<T>(this List<T> target, bool makeComplexPropertiesTrackable, bool makeCollectionPropertiesTrackable) where T : class
        {
            return (IList<T>)ChangeTrackingFactory.Default.AsTrackableCollection(target, new ChangeTrackingSettings(makeComplexPropertiesTrackable, makeCollectionPropertiesTrackable));
        }

        public static IList<T> AsTrackable<T>(this IList<T> target) where T : class
        {
            return (IList<T>)ChangeTrackingFactory.Default.AsTrackableCollection(target);
        }

        public static IList<T> AsTrackable<T>(this IList<T> target, bool makeComplexPropertiesTrackable, bool makeCollectionPropertiesTrackable) where T : class
        {
            return (IList<T>)ChangeTrackingFactory.Default.AsTrackableCollection(target, new ChangeTrackingSettings(makeComplexPropertiesTrackable, makeCollectionPropertiesTrackable));
        }
    }
}
