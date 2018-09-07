using Castle.DynamicProxy;
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

        private static void CopyFields<T>(T source, object target) => CopyFields(typeof(T), source, target);

        private static void CopyFields(Type type, object source, object target)
        {
            Action<object, object> copier = _FieldCopiers.GetOrAdd(type, typeCopying =>
            {
                List<FieldInfo> fieldInfosToCopy = typeCopying
                    .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(fi => !(fi.Name.StartsWith("<") && fi.Name.EndsWith(">k__BackingField")))
                    .ToList();
                if (fieldInfosToCopy.Count == 0)
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
                BlockExpression block = Expression.Block(setFieldsExpressions);

                return Expression.Lambda<Action<object, object>>(block, sourceParameter, targetParameter).Compile();
            });
            copier?.Invoke(source, target);
        }

        internal static object AsTrackableCollectionChild(Type type, object target, bool makeComplexPropertiesTrackable, bool makeCollectionPropertiesTrackable)
        {
            Type genericArgument = type.GetGenericArguments().First();
            return _ProxyGenerator.CreateInterfaceProxyWithTarget(typeof(IList<>).MakeGenericType(genericArgument),
                        new[] { typeof(IChangeTrackableCollection<>).MakeGenericType(genericArgument), typeof(IBindingList), typeof(ICancelAddNew), typeof(INotifyCollectionChanged) },
                        target,
                        GetOptions(genericArgument),
                        (IInterceptor)CreateInstance(typeof(ChangeTrackingCollectionInterceptor<>).MakeGenericType(genericArgument), target, makeComplexPropertiesTrackable, makeCollectionPropertiesTrackable));
        }

        internal static object AsTrackableChild(Type type, object target, Action<object> notifyParentItemCanceled, bool makeComplexPropertiesTrackable, bool makeCollectionPropertiesTrackable)
        {
            var changeTrackingInterceptor = CreateInstance(typeof(ChangeTrackingInterceptor<>).MakeGenericType(type), ChangeStatus.Unchanged);
            var notifyPropertyChangedInterceptor = CreateInstance(typeof(NotifyPropertyChangedInterceptor<>).MakeGenericType(type), changeTrackingInterceptor);
            var editableObjectInterceptor = CreateInstance(typeof(EditableObjectInterceptor<>).MakeGenericType(type), notifyParentItemCanceled);
            var complexPropertyInterceptor = CreateInstance(typeof(ComplexPropertyInterceptor<>).MakeGenericType(type), makeComplexPropertiesTrackable, makeCollectionPropertiesTrackable);
            var collectionPropertyInterceptor = CreateInstance(typeof(CollectionPropertyInterceptor<>).MakeGenericType(type), makeComplexPropertiesTrackable, makeCollectionPropertiesTrackable);
            object proxy = _ProxyGenerator.CreateClassProxyWithTarget(type,
                         new[] { typeof(IChangeTrackableInternal), typeof(IChangeTrackable<>).MakeGenericType(type), typeof(IChangeTrackingManager), typeof(IComplexPropertyTrackable), typeof(ICollectionPropertyTrackable), typeof(IEditableObject), typeof(System.ComponentModel.INotifyPropertyChanged) },
                         target,
                         GetOptions(type),
                         (IInterceptor)notifyPropertyChangedInterceptor,
                         (IInterceptor)changeTrackingInterceptor,
                         (IInterceptor)editableObjectInterceptor,
                         (IInterceptor)complexPropertyInterceptor,
                         (IInterceptor)collectionPropertyInterceptor);
            CopyFields(type: type, source: target, target: proxy);
            ((IInterceptorSettings)notifyPropertyChangedInterceptor).IsInitialized = true;
            ((IInterceptorSettings)changeTrackingInterceptor).IsInitialized = true;
            ((IInterceptorSettings)editableObjectInterceptor).IsInitialized = true;
            ((IInterceptorSettings)complexPropertyInterceptor).IsInitialized = true;
            ((IInterceptorSettings)collectionPropertyInterceptor).IsInitialized = true;
            return proxy;
        }

        private static object CreateInstance(Type type, params object[] args)
        {
            return Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, args, null);
        }

        public static T AsTrackable<T>(this T target, ChangeStatus status = ChangeStatus.Unchanged, bool makeComplexPropertiesTrackable = true, bool makeCollectionPropertiesTrackable = true) where T : class
        {
            return AsTrackable(target, status, null, makeComplexPropertiesTrackable, makeCollectionPropertiesTrackable);
        }

        internal static T AsTrackable<T>(this T target, ChangeStatus status, Action<T> notifyParentListItemCanceled, bool makeComplexPropertiesTrackable, bool makeCollectionPropertiesTrackable) where T : class
        {
            //if T was ICollection<T> it would of gone to one of the other overloads
            if (target as ICollection != null)
            {
                throw new InvalidOperationException("Only IList<T>, List<T> and ICollection<T> are supported");
            }

            var changeTrackingInterceptor = new ChangeTrackingInterceptor<T>(status);
            var notifyPropertyChangedInterceptor = new NotifyPropertyChangedInterceptor<T>(changeTrackingInterceptor);
            var editableObjectInterceptor = new EditableObjectInterceptor<T>(notifyParentListItemCanceled);
            var complexPropertyInterceptor = new ComplexPropertyInterceptor<T>(makeComplexPropertiesTrackable, makeCollectionPropertiesTrackable);
            var collectionPropertyInterceptor = new CollectionPropertyInterceptor<T>(makeComplexPropertiesTrackable, makeCollectionPropertiesTrackable);
            object proxy = _ProxyGenerator.CreateClassProxyWithTarget(typeof(T),
                new[] { typeof(IChangeTrackableInternal), typeof(IChangeTrackable<T>), typeof(IChangeTrackingManager), typeof(IComplexPropertyTrackable), typeof(ICollectionPropertyTrackable), typeof(IEditableObject), typeof(System.ComponentModel.INotifyPropertyChanged) },
                target,
                GetOptions(typeof(T)),
                notifyPropertyChangedInterceptor,
                changeTrackingInterceptor,
                editableObjectInterceptor,
                complexPropertyInterceptor,
                collectionPropertyInterceptor);
            CopyFields(source: target, target: proxy);
            notifyPropertyChangedInterceptor.IsInitialized = true;
            changeTrackingInterceptor.IsInitialized = true;
            editableObjectInterceptor.IsInitialized = true;
            complexPropertyInterceptor.IsInitialized = true;
            collectionPropertyInterceptor.IsInitialized = true;
            return (T)proxy;
        }

        public static ICollection<T> AsTrackable<T>(this System.Collections.ObjectModel.Collection<T> target) where T : class
        {
            return AsTrackable(target, true, true);
        }

        public static ICollection<T> AsTrackable<T>(this System.Collections.ObjectModel.Collection<T> target, bool makeComplexPropertiesTrackable = true, bool makeCollectionPropertiesTrackable = true) where T : class
        {
            return ((ICollection<T>)target).AsTrackable(makeComplexPropertiesTrackable, makeCollectionPropertiesTrackable);
        }

        public static ICollection<T> AsTrackable<T>(this ICollection<T> target) where T : class
        {
            return AsTrackable(target, true, true);
        }

        public static ICollection<T> AsTrackable<T>(this ICollection<T> target, bool makeComplexPropertiesTrackable, bool makeCollectionPropertiesTrackable) where T : class
        {
            var list = target as IList<T>;
            if (list == null)
            {
                list = target.ToList();
            }
            return list.AsTrackable(makeComplexPropertiesTrackable, makeCollectionPropertiesTrackable);
        }

        public static IList<T> AsTrackable<T>(this List<T> target) where T : class
        {
            return AsTrackable(target, true, true);
        }

        public static IList<T> AsTrackable<T>(this List<T> target, bool makeComplexPropertiesTrackable, bool makeCollectionPropertiesTrackable) where T : class
        {
            return ((IList<T>)target).AsTrackable(makeComplexPropertiesTrackable, makeCollectionPropertiesTrackable);
        }

        public static IList<T> AsTrackable<T>(this IList<T> target) where T : class
        {
            return AsTrackable(target, true, true);
        }

        public static IList<T> AsTrackable<T>(this IList<T> target, bool makeComplexPropertiesTrackable, bool makeCollectionPropertiesTrackable) where T : class
        {
            if (target.OfType<IChangeTrackable<T>>().Any(ct => ct.ChangeTrackingStatus != ChangeStatus.Unchanged))
            {
                throw new InvalidOperationException("some items in the collection are already being tracked");
            }
            object proxy = _ProxyGenerator.CreateInterfaceProxyWithTarget(typeof(IList<T>),
                new[] { typeof(IChangeTrackableCollection<T>), typeof(IBindingList), typeof(ICancelAddNew), typeof(INotifyCollectionChanged) }, target, GetOptions(typeof(T)), new ChangeTrackingCollectionInterceptor<T>(target, makeComplexPropertiesTrackable, makeCollectionPropertiesTrackable));
            return (IList<T>)proxy;
        }
    }
}
