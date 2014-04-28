using Castle.DynamicProxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChangeTracking
{
    internal sealed class ChangeTrackingCollectionInterceptor<T> : IInterceptor, IList<T>, IChangeTrackableCollection<T> where T : class
    {
        private IList<T> _Target;
        private IList<T> _DeletedItems;

        public ChangeTrackingCollectionInterceptor(IList<T> target)
        {
            for (int i = 0; i < target.Count; i++)
            {
                target[i] = target[i].AsTrackable();
            }
            _Target = target;
            _DeletedItems = new List<T>();
        }

        public void Intercept(IInvocation invocation)
        {
            invocation.ReturnValue = invocation.Method.Invoke(this, invocation.Arguments);
        }

        public int IndexOf(T item)
        {
            return _Target.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            var trackable = item.AsTrackable(ChangeStatus.Added);
            _Target.Insert(index, trackable);
        }

        public void RemoveAt(int index)
        {
            var trackable = this[index];
            DeleteItem(trackable);
            _Target.RemoveAt(index);
        }

        private void DeleteItem(T item)
        {
            var currentStatus = item.CastToIChangeTrackable().ChangeTrackingStatus;
            var manager = (IChangeTrackingManager<T>)item;
            manager.Delete();
            if (currentStatus != ChangeStatus.Added)
            {
            	_DeletedItems.Add(item);
            }
        }

        public T this[int index]
        {
            get
            {
                return _Target[index];
            }
            set
            {
                var trackable = value as IChangeTrackable<T>;
                if (trackable == null)
                {
                    trackable = (IChangeTrackable<T>)value.AsTrackable(ChangeStatus.Added);
                }
                _Target[index] = (T)trackable;
            }
        }

        public void Add(T item)
        {
            var trackable = item.AsTrackable(ChangeStatus.Added);
            _Target.Add(trackable);
        }

        public void Clear()
        {
            foreach (var item in _Target)
            {
                DeleteItem(item);
            }
            _Target.Clear();
        }

        public bool Contains(T item)
        {
            return _Target.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _Target.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _Target.Count; }
        }

        public bool IsReadOnly
        {
            get { return _Target.IsReadOnly; }
        }

        public bool Remove(T item)
        {
            bool removed = _Target.Remove(item);
            if (removed)
            {
            	DeleteItem(item);
            }
            return removed;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _Target.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerable<T> UnchangedItems
        {
            get { return _Target.Cast<IChangeTrackable<T>>().Where(ct => ct.ChangeTrackingStatus == ChangeStatus.Unchanged).Cast<T>(); }
        }

        public IEnumerable<T> AddedItems
        {
            get { return _Target.Cast<IChangeTrackable<T>>().Where(ct => ct.ChangeTrackingStatus == ChangeStatus.Added).Cast<T>(); }
        }

        public IEnumerable<T> ChangedItems
        {
            get { return _Target.Cast<IChangeTrackable<T>>().Where(ct => ct.ChangeTrackingStatus == ChangeStatus.Changed).Cast<T>(); }
        }

        public IEnumerable<T> DeletedItems
        {
            get { return _DeletedItems.Select(i => i); }
        }
    }
}
