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
        private readonly Action<int, T> _insertItem;
        private Action<T, int> _DeleteItem;
        private readonly bool _MakeComplexPropertiesTrackable;
        private readonly bool _MakeCollectionPropertiesTrackable;

        public ChangeTrackingBindingList(IList<T> list, Action<int, T> insertItem, Action<T, int> deleteItem, Action<T> itemCanceled, bool makeComplexPropertiesTrackable, bool makeCollectionPropertiesTrackable)
            : base(list)
        {
            _insertItem = insertItem;
            _DeleteItem = deleteItem;
            _ItemCanceled = itemCanceled;
            _MakeComplexPropertiesTrackable = makeComplexPropertiesTrackable;
            _MakeCollectionPropertiesTrackable = makeCollectionPropertiesTrackable;
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
                trackable = item.AsTrackable(ChangeStatus.Added, _ItemCanceled, _MakeComplexPropertiesTrackable,
                    _MakeCollectionPropertiesTrackable);
            }
            base.InsertItem(index, (T) trackable);
            _insertItem?.Invoke(index, (T)trackable);
        }

        protected override void SetItem(int index, T item)
        {
            object trackable = item as IChangeTrackable<T>;
            if (trackable == null)
            {
                trackable = item.AsTrackable(ChangeStatus.Added, _ItemCanceled, _MakeComplexPropertiesTrackable, _MakeCollectionPropertiesTrackable);
            }
            base.SetItem(index, (T)trackable);
            _insertItem?.Invoke(index, (T)trackable);
        }

        protected override void RemoveItem(int index)
        {
            if (_DeleteItem != null)
            {
                var item = this[index];
                _DeleteItem(item, index);
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

            object trackable = newItem as IChangeTrackable<T>;
            if (trackable == null)
            {
                trackable = newItem.AsTrackable(ChangeStatus.Added, _ItemCanceled, _MakeComplexPropertiesTrackable, _MakeCollectionPropertiesTrackable);
                var editable = (IEditableObject)trackable;
                editable.BeginEdit();
            }
            Add((T)trackable);

            return trackable;
        }
    }
}
