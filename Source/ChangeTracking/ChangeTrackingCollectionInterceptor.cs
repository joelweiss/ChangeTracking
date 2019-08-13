using Castle.DynamicProxy;
using ChangeTracking.Internal;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;

namespace ChangeTracking
{
    internal sealed class ChangeTrackingCollectionInterceptor<T> : IInterceptor, IChangeTrackableCollection<T>, IInterceptorSettings where T : class
    {
        private ChangeTrackingBindingList<T> _WrappedTarget;
        private IList<T> _DeletedItems;
        private readonly static HashSet<string> _ImplementedMethods;
        private readonly static HashSet<string> _BindingListImplementedMethods;
        private readonly static HashSet<string> _IBindingListImplementedMethods;
        private readonly static HashSet<string> _INotifyCollectionChangedImplementedMethods;
        private readonly ChangeTrackingSettings _ChangeTrackingSettings;
        private readonly Graph _Graph;

        public bool IsInitialized { get; set; }

        static ChangeTrackingCollectionInterceptor()
        {
            _ImplementedMethods = new HashSet<string>(typeof(ChangeTrackingCollectionInterceptor<T>).GetMethods(BindingFlags.Instance | BindingFlags.Public).Select(m => m.Name));
            _BindingListImplementedMethods = new HashSet<string>(typeof(ChangeTrackingBindingList<T>).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy).Select(m => m.Name));
            _IBindingListImplementedMethods = new HashSet<string>(typeof(ChangeTrackingBindingList<T>).GetInterfaceMap(typeof(System.ComponentModel.IBindingList)).TargetMethods.Where(mi => mi.IsPrivate).Select(mi => mi.Name.Substring(mi.Name.LastIndexOf('.') + 1)));
            _INotifyCollectionChangedImplementedMethods = new HashSet<string>(typeof(INotifyCollectionChanged).GetMethods(BindingFlags.Instance | BindingFlags.Public).Select(m => m.Name));
        }

        internal ChangeTrackingCollectionInterceptor(IList<T> target, ChangeTrackingSettings changeTrackingSettings, Graph graph)
        {
            _ChangeTrackingSettings = changeTrackingSettings;
            _Graph = graph;
            for (int i = 0; i < target.Count; i++)
            {
                target[i] = target[i].AsTrackable(ChangeStatus.Unchanged, ItemCanceled, _ChangeTrackingSettings, _Graph);
            }
            _WrappedTarget = new ChangeTrackingBindingList<T>(target, DeleteItem, ItemCanceled, _ChangeTrackingSettings, _Graph);
            _DeletedItems = new List<T>();
        }

        public void Intercept(IInvocation invocation)
        {
            switch (invocation.Method.Name)
            {
                case nameof(IRevertibleChangeTrackingInternal.AcceptChanges):
                    AcceptChanges(invocation.Proxy, invocation.Arguments.Length == 0 ? null : (List<object>)invocation.Arguments[0]);
                    break;
                case nameof(IRevertibleChangeTrackingInternal.RejectChanges):
                    RejectChanges(invocation.Proxy, invocation.Arguments.Length == 0 ? null : (List<object>)invocation.Arguments[0]);
                    break;
                default:
                    if (_ImplementedMethods.Contains(invocation.Method.Name))
                    {
                        invocation.ReturnValue = invocation.Method.Invoke(this, invocation.Arguments);
                        return;
                    }
                    if (_BindingListImplementedMethods.Contains(invocation.Method.Name))
                    {
                        invocation.ReturnValue = invocation.Method.Invoke(_WrappedTarget, invocation.Arguments);
                        return;
                    }
                    if (_IBindingListImplementedMethods.Contains(invocation.Method.Name))
                    {
                        invocation.ReturnValue = invocation.Method.Invoke(_WrappedTarget, invocation.Arguments);
                        return;
                    }
                    if (_INotifyCollectionChangedImplementedMethods.Contains(invocation.Method.Name))
                    {
                        invocation.ReturnValue = invocation.Method.Invoke(_WrappedTarget, invocation.Arguments);
                        return;
                    }
                    if (invocation.Method.Name == nameof(IRevertibleChangeTrackingInternal.IsChanged))
                    {
                        invocation.ReturnValue = IsChanged;
                        return;
                    }
                    invocation.Proceed();
                    break;
            }
        }

        private void DeleteItem(T item)
        {
            var currentStatus = item.CastToIChangeTrackable().ChangeTrackingStatus;
            var manager = (IChangeTrackingManager)item;
            bool deleteSuccess = manager.Delete();
            if (deleteSuccess && currentStatus != ChangeStatus.Added)
            {
                _DeletedItems.Add(item);
            }
        }

        private void ItemCanceled(T item) => _WrappedTarget.CancelNew(_WrappedTarget.IndexOf(item));

        public IEnumerable<T> UnchangedItems => _WrappedTarget.Cast<IChangeTrackable<T>>().Where(ct => ct.ChangeTrackingStatus == ChangeStatus.Unchanged).Cast<T>();

        public IEnumerable<T> AddedItems => _WrappedTarget.Cast<IChangeTrackable<T>>().Where(ct => ct.ChangeTrackingStatus == ChangeStatus.Added).Cast<T>();

        public IEnumerable<T> ChangedItems => _WrappedTarget.Cast<IChangeTrackable<T>>().Where(ct => ct.ChangeTrackingStatus == ChangeStatus.Changed).Cast<T>();

        public IEnumerable<T> DeletedItems => _DeletedItems.Select(i => i);

        public bool UnDelete(T item)
        {
            var manager = (IChangeTrackingManager)item;
            bool unDeleteSuccess = manager.UnDelete();
            if (unDeleteSuccess)
            {
                bool removeSuccess = _DeletedItems.Remove(item);
                if (removeSuccess)
                {
                    _WrappedTarget.Add(item);
                    return true;
                }
            }
            return false;
        }

        public void AcceptChanges() => throw new System.InvalidOperationException("Invalid call you must call the overload with proxy and parents arguments");

        private void AcceptChanges(object proxy, List<object> parents)
        {
            parents = parents ?? new List<object>(20) { proxy };
            foreach (var item in _WrappedTarget.Cast<IRevertibleChangeTrackingInternal>())
            {
                item.AcceptChanges(parents);
                if (item is IEditableObjectInternal editable)
                {
                    editable.EndEdit(parents);
                }
            }
            _DeletedItems.Clear();
        }

        public void RejectChanges() => throw new System.InvalidOperationException("Invalid call you must call the overload with proxy and parents arguments");

        private void RejectChanges(object proxy, List<object> parents)
        {
            AddedItems.ToList().ForEach(i => _WrappedTarget.Remove(i));
            parents = parents ?? new List<object>(20) { proxy };
            foreach (var item in _WrappedTarget.Cast<IRevertibleChangeTrackingInternal>())
            {
                item.RejectChanges(parents);
            }
            foreach (var item in _DeletedItems)
            {
                ((IRevertibleChangeTrackingInternal)item).RejectChanges(parents);
                _WrappedTarget.Add(item);
            }
            _DeletedItems.Clear();
        }

        public bool IsChanged => ChangedItems.Any() || AddedItems.Any() || DeletedItems.Any();

        public IEnumerator<T> GetEnumerator() => _WrappedTarget.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
