using Castle.DynamicProxy;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;

namespace ChangeTracking
{
    internal sealed class ChangeTrackingCollectionInterceptor<T> : IInterceptor, IChangeTrackableCollection<T>, IInterceptorSettings where T : class
    {
        private readonly ChangeTrackingBindingList<T> _WrappedTarget;
        internal readonly List<IndexedItem<T>> _DeletedItems;
        internal readonly List<IndexedItem<T>> _AddedItems;
        internal readonly List<T> _ChangedItems;
        internal readonly List<T> _UnchangedItems;
        private static HashSet<string> _ImplementedMethods;
        private static HashSet<string> _BindingListImplementedMethods;
        private static HashSet<string> _IBindingListImplementedMethods;
        private static HashSet<string> _INotifyCollectionChangedImplementedMethods;
        private readonly bool _MakeComplexPropertiesTrackable;
        private readonly bool _MakeCollectionPropertiesTrackable;
        private bool _reverting;
        private bool _undeleting;

        public bool IsInitialized { get; set; }

        static ChangeTrackingCollectionInterceptor()
        {
            _ImplementedMethods = new HashSet<string>(typeof(ChangeTrackingCollectionInterceptor<T>).GetMethods(BindingFlags.Instance | BindingFlags.Public).Select(m => m.Name));
            _BindingListImplementedMethods = new HashSet<string>(typeof(ChangeTrackingBindingList<T>).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy).Select(m => m.Name));
            _IBindingListImplementedMethods = new HashSet<string>(typeof(ChangeTrackingBindingList<T>).GetInterfaceMap(typeof(System.ComponentModel.IBindingList)).TargetMethods.Where(mi => mi.IsPrivate).Select(mi => mi.Name.Substring(mi.Name.LastIndexOf('.') + 1)));
            _INotifyCollectionChangedImplementedMethods = new HashSet<string>(typeof(INotifyCollectionChanged).GetMethods(BindingFlags.Instance | BindingFlags.Public).Select(m => m.Name));
        }

        internal ChangeTrackingCollectionInterceptor(IList<T> target, bool makeComplexPropertiesTrackable, bool makeCollectionPropertiesTrackable)
        {
            _MakeComplexPropertiesTrackable = makeComplexPropertiesTrackable;
            _MakeCollectionPropertiesTrackable = makeCollectionPropertiesTrackable;
            _DeletedItems = new List<IndexedItem<T>>();
            _AddedItems = new List<IndexedItem<T>>();
            _ChangedItems = new List<T>();
            _UnchangedItems = new List<T>();

            for (int i = 0; i < target.Count; i++)
            {
                var newItem = target[i].AsTrackable(ChangeStatus.Unchanged, ItemCanceled, _MakeComplexPropertiesTrackable, _MakeCollectionPropertiesTrackable);
                target[i] = newItem;
                _UnchangedItems.Add(newItem);
            }

            _WrappedTarget = new ChangeTrackingBindingList<T>(target, InsertingItem, DeleteItem, ItemCanceled, ChildStatusChanged, _MakeComplexPropertiesTrackable, _MakeCollectionPropertiesTrackable);
        }

        public void Intercept(IInvocation invocation)
        {
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
            invocation.Proceed();
        }

        private void DeleteItem(T item, int index)
        {
            if (_reverting)
                return;

            var currentStatus = item.CastToIChangeTrackable().ChangeTrackingStatus;
            var manager = (IChangeTrackingManager) item;
            bool deleteSuccess = manager.Delete();
            if (deleteSuccess && currentStatus != ChangeStatus.Added)
            {
                _DeletedItems.Add(new IndexedItem<T>(item, index, currentStatus));
            }
            else if (deleteSuccess && currentStatus == ChangeStatus.Added)
            {
                var added = _AddedItems.FirstOrDefault(a => a.Item == item);
                if (added != null)
                {
                    _AddedItems.Remove(added);
                }
            }
        }

        private T InsertingItem(int index, T item)
        {
            if (_reverting || _undeleting)
            {
                if (_undeleting && item is IChangeTrackable<T> trackable)
                {
                    if (trackable.ChangeTrackingStatus == ChangeStatus.Unchanged)
                        _UnchangedItems.Add(item);
                    else if (trackable.ChangeTrackingStatus == ChangeStatus.Changed)
                        _ChangedItems.Add(item);
                }

                return item;
            }

            var deletedItem = _DeletedItems.FirstOrDefault(d => d.Item == item);
            if (deletedItem != null)
            {
                _DeletedItems.Remove(deletedItem);
                
                var manager = (IChangeTrackingManager) item;
                manager.UpdateStatus();
                return item;
            }
            else if (item is IChangeTrackable<T> ctr && ctr.ChangeTrackingStatus == ChangeStatus.Deleted)
            {
                _AddedItems.Add(new IndexedItem<T>(item, index, ChangeStatus.Deleted));

                var manager = (IChangeTrackingManager) item;
                manager.SetAdded();
                return item;
            }
            else
            {
                object trackable = item as IChangeTrackable<T>;
                if (trackable == null)
                {
                    trackable = item.AsTrackable(ChangeStatus.Added, ItemCanceled, _MakeComplexPropertiesTrackable, _MakeCollectionPropertiesTrackable);
                }

                _AddedItems.Add(new IndexedItem<T>((T)trackable, index, ChangeStatus.Unchanged));
                var manager = (IChangeTrackingManager)trackable;
                manager.SetAdded();

                return (T)trackable;
            }
        }

        private void ItemCanceled(T item)
        {
            _WrappedTarget.CancelNew(_WrappedTarget.IndexOf(item));
        }

        private void ChildStatusChanged(StatusChangedEventArgs args)
        {
            var item = (T) args.Proxy;
            if (args.OldStatus == ChangeStatus.Unchanged)
            {
                _UnchangedItems.Remove(item);
            }
            if (args.OldStatus == ChangeStatus.Changed)
            {
                _ChangedItems.Remove(item);
            }


            if (args.NewStatus == ChangeStatus.Unchanged)
            {
                if(!_UnchangedItems.Contains(item))
                    _UnchangedItems.Add(item);
            }
            if (args.NewStatus == ChangeStatus.Changed)
            {
                if (!_ChangedItems.Contains(item))
                    _ChangedItems.Add(item);
            }
        }

        public IEnumerable<T> UnchangedItems
        {
            get
            {
                return _UnchangedItems;
                //_WrappedTarget.Cast<IChangeTrackable<T>>().Where(ct => ct.ChangeTrackingStatus == ChangeStatus.Unchanged).Cast<T>(); 
            }
        }

        public IEnumerable<T> AddedItems
        {
            get { return _AddedItems.Select(i => i.Item); }
        }

        public IEnumerable<T> ChangedItems
        {
            get {
                return _ChangedItems;
                //_WrappedTarget.Cast<IChangeTrackable<T>>().Where(ct => ct.ChangeTrackingStatus == ChangeStatus.Changed).Cast<T>();
            }
        }

        public IEnumerable<T> DeletedItems
        {
            get { return _DeletedItems.Select(i => i.Item); }
        }

        public bool UnDelete(T item)
        {
            try
            {
                _undeleting = true;
                var manager = (IChangeTrackingManager) item;
                bool unDeleteSuccess = manager.UnDelete();
                if (unDeleteSuccess)
                {
                    var deletedItem = _DeletedItems.Single(d => d.Item == item);
                    bool removeSuccess = _DeletedItems.Remove(deletedItem);
                    if (removeSuccess)
                    {
                        _WrappedTarget.Insert(deletedItem.Index, deletedItem.Item);
                        return true;
                    }
                }
                return false;
            }
            finally
            {
                _undeleting = false;
            }
        }

        public void AcceptChanges()
        {
            var allChangedItems = _AddedItems.Select(a => a.Item).Concat(_DeletedItems.Select(a => a.Item))
                .Concat(_ChangedItems).Cast<IChangeTrackable<T>>().ToArray();
            
            foreach (var item in allChangedItems)
            {
                if ((item.ChangeTrackingStatus == ChangeStatus.Added ||
                     item.ChangeTrackingStatus == ChangeStatus.Changed) &&
                    !_UnchangedItems.Contains((T) item))
                {
                    _UnchangedItems.Add((T)item);

                    item.AcceptChanges();
                }

                if (item is System.ComponentModel.IEditableObject editable)
                {
                    editable.EndEdit();
                }
            }
            _DeletedItems.Clear();
            _AddedItems.Clear();
            _ChangedItems.Clear();
        }

        public void RejectChanges()
        {
            try
            {
                _reverting = true;

                foreach (var revertedItem in _DeletedItems.Select(a => a.Item).Concat(_ChangedItems))
                {
                    if (!_UnchangedItems.Contains(revertedItem))
                        _UnchangedItems.Add(revertedItem);
                }

                AddedItems.ToList().ForEach(i => _WrappedTarget.Remove(i));
                _AddedItems.Clear();
                foreach (var item in _WrappedTarget.Cast<IChangeTrackable<T>>())
                {
                    item.RejectChanges();
                }

                // If someone removes two objects from a collection and reverts it then the order of the deleted objects is inverted
                // For example:
                //      collection.RemoveAt(0);
                //      collection.RemoveAt(0);
                //      trackable.RejectChanges();
                //
                _DeletedItems.Reverse();

                foreach (var item in _DeletedItems.OrderBy(i => i.Index))
                {
                    ((System.ComponentModel.IRevertibleChangeTracking) item.Item).RejectChanges();
                    _WrappedTarget.Insert(item.Index, item.Item);
                }
                _DeletedItems.Clear();
            }
            finally
            {
                _reverting = false;
            }
        }

        public bool IsChanged => _ChangedItems.Count != 0 || _AddedItems.Count != 0 || _DeletedItems.Count != 0;

        public IEnumerator<T> GetEnumerator() => _WrappedTarget.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

        internal class IndexedItem<TT>
        {
            public IndexedItem(TT item, int index, ChangeStatus previousStatus)
            {
                Item = item;
                Index = index;
                PreviousStatus = previousStatus;
            }
            public TT Item { get; set; }
            public int Index { get; set; }
            public ChangeStatus PreviousStatus { get; set; }
        }
    }
}
