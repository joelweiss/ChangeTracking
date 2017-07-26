using Castle.DynamicProxy;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace ChangeTracking
{
    internal sealed class ChangeTrackingBindingList<T> : BindingList<T> where T : class
    {
        private readonly Action<T> _ItemCanceled;
        private readonly Action<StatusChangedEventArgs> _childStatusChanged;
        private readonly Func<int, T, T> _insertingItem;
        private readonly Action<T, int> _DeleteItem;
        private readonly bool _MakeComplexPropertiesTrackable;
        private readonly bool _MakeCollectionPropertiesTrackable;

        public ChangeTrackingBindingList(IList<T> list, Func<int, T, T> inseringItem, Action<T, int> deleteItem, Action<T> itemCanceled, Action<StatusChangedEventArgs> childStatusChanged, bool makeComplexPropertiesTrackable, bool makeCollectionPropertiesTrackable)
            : base(list)
        {
            if (inseringItem == null) throw new ArgumentNullException(nameof(inseringItem));
            if (deleteItem == null) throw new ArgumentNullException(nameof(deleteItem));
            if (itemCanceled == null) throw new ArgumentNullException(nameof(itemCanceled));
            if (childStatusChanged == null) throw new ArgumentNullException(nameof(childStatusChanged));
            if (deleteItem == null) throw new ArgumentNullException(nameof(deleteItem));



            _insertingItem = inseringItem;
            _DeleteItem = deleteItem;
            _ItemCanceled = itemCanceled;
            _childStatusChanged = childStatusChanged;
            _MakeComplexPropertiesTrackable = makeComplexPropertiesTrackable;
            _MakeCollectionPropertiesTrackable = makeCollectionPropertiesTrackable;
            var bindingListType = typeof(ChangeTrackingBindingList<T>).BaseType;
            bindingListType.GetField("raiseItemChangedEvents", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(this, true);
            var hookMethod = bindingListType.GetMethod("HookPropertyChanged", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            foreach (var item in list)
            {
                hookMethod.Invoke(this, new object[] { item });

                if (item is IChangeTrackable<T> trackable)
                {
                    trackable.StatusChanged -= Trackable_StatusChanged;
                    trackable.StatusChanged += Trackable_StatusChanged;
                }
            }
        }

        protected override void InsertItem(int index, T item)
        {
            var i = _insertingItem?.Invoke(index, item) ?? item;

            IChangeTrackable<T> trackable = i as IChangeTrackable<T>;
            if (trackable == null)
            {
                trackable = (IChangeTrackable<T>)i.AsTrackable(ChangeStatus.Added, _ItemCanceled, _MakeComplexPropertiesTrackable,
                    _MakeCollectionPropertiesTrackable);
            }

            trackable.StatusChanged -= Trackable_StatusChanged;
            trackable.StatusChanged += Trackable_StatusChanged;
            
            base.InsertItem(index, (T) trackable);
        }

        protected override void SetItem(int index, T item)
        {
            IChangeTrackable<T> trackable = item as IChangeTrackable<T>;
            if (trackable == null)
            {
                trackable = (IChangeTrackable<T>)item.AsTrackable(ChangeStatus.Added, _ItemCanceled, _MakeComplexPropertiesTrackable, _MakeCollectionPropertiesTrackable);
            }

            trackable.StatusChanged -= Trackable_StatusChanged;
            trackable.StatusChanged += Trackable_StatusChanged;

            base.SetItem(index, (T)trackable);
            _insertingItem?.Invoke(index, (T)trackable);
        }

        private void Trackable_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            _childStatusChanged(e);
        }

        protected override void RemoveItem(int index)
        {
            var item = this[index];
            _DeleteItem(item, index);

            if (item is IChangeTrackable<T> trackable)
            {
                trackable.StatusChanged -= Trackable_StatusChanged;
            }

            base.RemoveItem(index);
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

            IChangeTrackable<T> trackable = newItem as IChangeTrackable<T>;
            if (trackable == null)
            {
                trackable = (IChangeTrackable<T>)newItem.AsTrackable(ChangeStatus.Added, _ItemCanceled, _MakeComplexPropertiesTrackable, _MakeCollectionPropertiesTrackable);
                var editable = (IEditableObject)trackable;
                editable.BeginEdit();
            }

            trackable.StatusChanged -= Trackable_StatusChanged;
            trackable.StatusChanged += Trackable_StatusChanged;

            Add((T)trackable);

            return trackable;
        }
    }
}
