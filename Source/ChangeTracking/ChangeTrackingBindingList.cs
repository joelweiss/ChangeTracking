using ChangeTracking.Internal;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace ChangeTracking
{
    internal sealed class ChangeTrackingBindingList<T> : BindingList<T>, INotifyCollectionChanged where T : class
    {
        private readonly Action<T> _ItemCanceled;
        private readonly Action<T> _DeleteItem;
        private readonly ChangeTrackingSettings _ChangeTrackingSettings;
        private readonly Graph _Graph;

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public ChangeTrackingBindingList(IList<T> list, Action<T> deleteItem, Action<T> itemCanceled, ChangeTrackingSettings changeTrackingSettings, Graph graph)
            : base(list)
        {
            _DeleteItem = deleteItem;
            _ItemCanceled = itemCanceled;
            _ChangeTrackingSettings = changeTrackingSettings;
            _Graph = graph;
            var bindingListType = typeof(ChangeTrackingBindingList<T>).BaseType;
            bindingListType.GetField("raiseItemChangedEvents", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(this, true);
            var hookMethod = bindingListType.GetMethod("HookPropertyChanged", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            foreach (var item in list)
            {
                hookMethod.Invoke(this, new object[] { item });
            }
        }

        protected override void InsertItem(int index, T item)
        {
            object trackable = item as IChangeTrackable<T>;
            if (trackable == null)
            {
                trackable = ChangeTrackingFactory.Default.AsTrackable(item, ChangeStatus.Added, _ItemCanceled, _ChangeTrackingSettings, _Graph);
            }
            base.InsertItem(index, (T)trackable);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
        }

        protected override void SetItem(int index, T item)
        {
            object trackable = item as IChangeTrackable<T>;
            if (trackable == null)
            {
                trackable = ChangeTrackingFactory.Default.AsTrackable(item, ChangeStatus.Added, _ItemCanceled, _ChangeTrackingSettings, _Graph);
            }
            T originalItem = this[index];
            base.SetItem(index, (T)trackable);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, item, originalItem, index));
        }

        protected override void RemoveItem(int index)
        {
            T removedItem = this[index];
            _DeleteItem?.Invoke(removedItem);
            base.RemoveItem(index);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItem, index));
        }

        protected override void OnListChanged(ListChangedEventArgs e)
        {
            base.OnListChanged(e);
            switch (e.ListChangedType)
            {
                case ListChangedType.Reset:
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                    break;
                default:
                    return;
            }
        }

        protected override object AddNewCore()
        {
            AddingNewEventArgs e = new AddingNewEventArgs(null);
            OnAddingNew(e);
            T newItem = (T)e.NewObject;

            if (newItem == null)
            {
                newItem = Activator.CreateInstance<T>();
            }

            object trackable = newItem as IChangeTrackable<T>;
            if (trackable == null)
            {
                trackable = ChangeTrackingFactory.Default.AsTrackable(newItem, ChangeStatus.Added, _ItemCanceled, _ChangeTrackingSettings, _Graph);
                var editable = (IEditableObject)trackable;
                editable.BeginEdit();
            }
            Add((T)trackable);

            return trackable;
        }
    }
}
