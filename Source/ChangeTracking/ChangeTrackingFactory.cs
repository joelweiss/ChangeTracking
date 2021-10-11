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
    public class ChangeTrackingFactory : IChangeTrackingFactory
    {
        private readonly ProxyGenerator _ProxyGenerator;
        private readonly IInterceptorSelector _Selector;
        private readonly ConcurrentDictionary<Type, ProxyGenerationOptions> _Options;
        private readonly MethodInfo _SetValueMethodInfo;
        private readonly ConcurrentDictionary<Type, Action<object, object>> _FieldCopiers;

        static ChangeTrackingFactory() => Default = new ChangeTrackingFactory();

        public static ChangeTrackingFactory Default { get; }

        public ChangeTrackingFactory() : this(makeComplexPropertiesTrackable: true, makeCollectionPropertiesTrackable: true) { }

        public ChangeTrackingFactory(bool makeComplexPropertiesTrackable, bool makeCollectionPropertiesTrackable)
        {
            MakeComplexPropertiesTrackable = makeComplexPropertiesTrackable;
            MakeCollectionPropertiesTrackable = makeCollectionPropertiesTrackable;

            _ProxyGenerator = new ProxyGenerator();
            _Selector = new ChangeTrackingInterceptorSelector();
            _Options = new ConcurrentDictionary<Type, ProxyGenerationOptions>();
            _SetValueMethodInfo = typeof(FieldInfo).GetMethod(nameof(FieldInfo.SetValue), new[] { typeof(object), typeof(object) });
            _FieldCopiers = new ConcurrentDictionary<Type, Action<object, object>>();
        }

        public T AsTrackable<T>(T target, ChangeStatus status = ChangeStatus.Unchanged) where T : class
        {
            return AsTrackable(target, new ChangeTrackingSettings(MakeComplexPropertiesTrackable, MakeCollectionPropertiesTrackable), status);
        }

        public ICollection<T> AsTrackableCollection<T>(ICollection<T> target) where T : class
        {
            return AsTrackableCollection(target, new ChangeTrackingSettings(MakeComplexPropertiesTrackable, MakeCollectionPropertiesTrackable));
        }

        public bool MakeComplexPropertiesTrackable { get; set; }
        public bool MakeCollectionPropertiesTrackable { get; set; }

        internal T AsTrackable<T>(T target, ChangeStatus status, Action<T> notifyParentListItemCanceled, ChangeTrackingSettings changeTrackingSettings, Graph graph) where T : class
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

            //var changeTrackingInterceptor = new ChangeTrackingInterceptor<T>(status);
            var changeTrackingInterceptor = (IInterceptor)Activator.CreateInstance(
                GetGenericType(typeof(ChangeTrackingInterceptor<>), target.GetType()), 
                status);

            //var notifyPropertyChangedInterceptor = new NotifyPropertyChangedInterceptor<T>(changeTrackingInterceptor);
            var notifyPropertyChangedInterceptor = (IInterceptor)Activator.CreateInstance(
                GetGenericType(typeof(NotifyPropertyChangedInterceptor<>), target.GetType()), 
                changeTrackingInterceptor);

            //var editableObjectInterceptor = new EditableObjectInterceptor<T>(notifyParentListItemCanceled);
            var editableObjectInterceptor = (IInterceptor)Activator.CreateInstance(
                GetGenericType(typeof(EditableObjectInterceptor<>), target.GetType()),
                notifyParentListItemCanceled);

            //var complexPropertyInterceptor = new ComplexPropertyInterceptor<T>(changeTrackingSettings, graph);
            var complexPropertyInterceptor = (IInterceptor)Activator.CreateInstance(
                GetGenericType(typeof(ComplexPropertyInterceptor<>), target.GetType()),
                changeTrackingSettings, graph);

            //var collectionPropertyInterceptor = new CollectionPropertyInterceptor<T>(changeTrackingSettings, graph);
            var collectionPropertyInterceptor = (IInterceptor)Activator.CreateInstance(
                GetGenericType(typeof(CollectionPropertyInterceptor<>), target.GetType()),
                changeTrackingSettings, graph);

            object proxy = _ProxyGenerator.CreateClassProxyWithTarget(target.GetType(),
                new[] { 
                    typeof(IChangeTrackableInternal), 
                    typeof(IRevertibleChangeTrackingInternal), 
                    //typeof(IChangeTrackable<T>), 
                    GetGenericType(typeof(IChangeTrackable<>), target.GetType()),
                    typeof(IChangeTrackingManager), 
                    typeof(IComplexPropertyTrackable), 
                    typeof(ICollectionPropertyTrackable), 
                    typeof(IEditableObjectInternal), 
                    typeof(INotifyPropertyChanged) },
                target,
                GetOptions(target.GetType()),
                notifyPropertyChangedInterceptor,
                changeTrackingInterceptor,
                editableObjectInterceptor,
                complexPropertyInterceptor,
                collectionPropertyInterceptor);
            CopyFieldsAndProperties(source: target, target: proxy);
            (notifyPropertyChangedInterceptor as IInterceptorSettings).IsInitialized = true;
            (changeTrackingInterceptor as IInterceptorSettings).IsInitialized = true;
            (editableObjectInterceptor as IInterceptorSettings).IsInitialized = true;
            (complexPropertyInterceptor as IInterceptorSettings).IsInitialized = true;
            (collectionPropertyInterceptor as IInterceptorSettings).IsInitialized = true;
            graph.Add(new ProxyWeakTargetMap(target, proxy));
            return (T)proxy;
        }

        private Type GetGenericType(Type generic, Type genericTypeParameter)
        {
            return generic.MakeGenericType(genericTypeParameter);
        }

        internal ICollection<T> AsTrackableCollection<T>(ICollection<T> target, ChangeTrackingSettings changeTrackingSettings) where T : class
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

        internal T AsTrackable<T>(T target, ChangeTrackingSettings changeTrackingSettings, ChangeStatus status = ChangeStatus.Unchanged) where T : class
        {
            return AsTrackable(target, status, null, changeTrackingSettings, new Graph());
        }

        private ProxyGenerationOptions GetOptions(Type type)
        {
            ProxyGenerationOptions CreateOptions(Type createOptionsForType) => new ProxyGenerationOptions
            {
                Hook = new ChangeTrackingProxyGenerationHook(createOptionsForType),
                Selector = _Selector
            };
            return _Options.GetOrAdd(type, CreateOptions);
        }

        private void CopyFieldsAndProperties<T>(T source, object target) => CopyFieldsAndProperties(typeof(T), source, target);

        private void CopyFieldsAndProperties(Type type, object source, object target)
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

        internal object AsTrackableCollectionChild(Type type, object target, ChangeTrackingSettings changeTrackingSettings, Graph graph)
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
                        CreateInterceptor(typeof(ChangeTrackingCollectionInterceptor<>).MakeGenericType(genericArgument), target, changeTrackingSettings, graph));
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

        internal object AsTrackableChild(Type type, object target, Action<object> notifyParentItemCanceled, ChangeTrackingSettings changeTrackingSettings, Internal.Graph graph)
        {
            ThrowIfTargetIsProxy(target);
            ProxyWeakTargetMap existing = graph.GetExistingProxyForTarget(target);
            if (existing != null)
            {
                return existing.Proxy;
            }
            IInterceptor changeTrackingInterceptor = CreateInterceptor(typeof(ChangeTrackingInterceptor<>).MakeGenericType(type), ChangeStatus.Unchanged);
            IInterceptor notifyPropertyChangedInterceptor = CreateInterceptor(typeof(NotifyPropertyChangedInterceptor<>).MakeGenericType(type), changeTrackingInterceptor);
            IInterceptor editableObjectInterceptor = CreateInterceptor(typeof(EditableObjectInterceptor<>).MakeGenericType(type), notifyParentItemCanceled);
            IInterceptor complexPropertyInterceptor = CreateInterceptor(typeof(ComplexPropertyInterceptor<>).MakeGenericType(type), changeTrackingSettings, graph);
            IInterceptor collectionPropertyInterceptor = CreateInterceptor(typeof(CollectionPropertyInterceptor<>).MakeGenericType(type), changeTrackingSettings, graph);
            object proxy = _ProxyGenerator.CreateClassProxyWithTarget(type,
                         new[] { typeof(IChangeTrackableInternal), typeof(IRevertibleChangeTrackingInternal), typeof(IChangeTrackable<>).MakeGenericType(type), typeof(IChangeTrackingManager), typeof(IComplexPropertyTrackable), typeof(ICollectionPropertyTrackable), typeof(IEditableObjectInternal), typeof(INotifyPropertyChanged) },
                         target,
                         GetOptions(type),
                         notifyPropertyChangedInterceptor,
                         changeTrackingInterceptor,
                         editableObjectInterceptor,
                         complexPropertyInterceptor,
                         collectionPropertyInterceptor);
            CopyFieldsAndProperties(type: type, source: target, target: proxy);
            ((IInterceptorSettings)notifyPropertyChangedInterceptor).IsInitialized = true;
            ((IInterceptorSettings)changeTrackingInterceptor).IsInitialized = true;
            ((IInterceptorSettings)editableObjectInterceptor).IsInitialized = true;
            ((IInterceptorSettings)complexPropertyInterceptor).IsInitialized = true;
            ((IInterceptorSettings)collectionPropertyInterceptor).IsInitialized = true;
            graph.Add(new ProxyWeakTargetMap(target, proxy));
            return proxy;
        }

        private static IInterceptor CreateInterceptor(Type type, params object[] args)
        {
            return (IInterceptor)Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, args, null);
        }
    }
}