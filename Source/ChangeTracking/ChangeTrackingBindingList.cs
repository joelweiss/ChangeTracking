using Castle.DynamicProxy;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace ChangeTracking
{
    internal sealed class ChangeTrackingBindingList<T> : BindingList<T>
    {
        private readonly Action<T> _ItemCanceled;
        private IList<T> target;
        private Action<T> _DeleteItem;

        public ChangeTrackingBindingList(IList<T> list, Action<T> deleteItem, Action<T> itemCanceled)
            : base(list)
        {
            _DeleteItem = deleteItem;
            _ItemCanceled = itemCanceled;
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
                //refactor out and handle if its not a class but a int for instance
                trackable = Core.AsTrackableObject(typeof(T), item, ChangeStatus.Added, v => _ItemCanceled((T)v));
                //trackable = typeof(Core).GetMethods().First(m => m.Name == "AsTrackable").MakeGenericMethod(typeof(T)).Invoke(null, new object[] { item, ChangeStatus.Added, _ItemCanceled });
            }
            base.InsertItem(index, (T)trackable);
        }

        protected override void SetItem(int index, T item)
        {
            object trackable = item as IChangeTrackable<T>;
            if (trackable == null)
            {
                //refactor out and handle if its not a class but a int for instance
                //trackable = typeof(Core).GetMethods().First(m => m.Name == "AsTrackable").MakeGenericMethod(typeof(T)).Invoke(null, new object[] { item, ChangeStatus.Added, _ItemCanceled });
                trackable = Core.AsTrackableObject(typeof(T), item, ChangeStatus.Added, v => _ItemCanceled((T)v));
            }
            base.SetItem(index, (T)trackable);
        }

        protected override void RemoveItem(int index)
        {
            if (_DeleteItem != null)
            {
                _DeleteItem(this[index]);
            }
            base.RemoveItem(index);
        }

        protected override object AddNewCore()
        {
            AddingNewEventArgs e = new AddingNewEventArgs(null);
            OnAddingNew(e);
            object newItem = e.NewObject;

            if (newItem == null)
            {
                newItem = Activator.CreateInstance<T>();
            }

            object trackable = newItem as IChangeTrackable<T>;
            if (trackable == null)
            {
                //refactor out and handle if its not a class but a int for instance
                //trackable = typeof(Core).GetMethods().First(m => m.Name == "AsTrackable").MakeGenericMethod(typeof(T)).Invoke(null, new object[] { newItem, ChangeStatus.Added, _ItemCanceled });
                trackable = Core.AsTrackableObject(typeof(T), newItem, ChangeStatus.Added, v => _ItemCanceled((T)v));
                var editable = (IEditableObject)trackable;
                editable.BeginEdit();
            }
            Add((T)trackable);

            return trackable;
        }
    }
}
