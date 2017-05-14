using System.Linq;
using FluentAssertions;
using Xunit;

namespace ChangeTracking.Tests
{
    public class IBindingListTests
    {
#if NET452
        [Fact]
        public void AsTrackable_On_Collection_Should_Make_It_ICancelAddNew()
        {
            var orders = Helper.GetOrdersIList();

            var trackable = orders.AsTrackable();

            trackable.Should().BeAssignableTo<System.ComponentModel.ICancelAddNew>();
        }

        [Fact]
        public void AsTrackable_On_Collection_Should_Make_It_IBindingList()
        {
            var orders = Helper.GetOrdersIList();

            var trackable = orders.AsTrackable();

            trackable.Should().BeAssignableTo<System.ComponentModel.IBindingList>();
        }

        [Fact]
        public void AsTrackable_On_Collection_AddNew_Should_Raise_ListChanged()
        {
            var orders = Helper.GetOrdersIList();

            var trackable = orders.AsTrackable();
            var bindingList = (System.ComponentModel.IBindingList)trackable;

            bindingList.MonitorEvents();
            bindingList.AddNew();

            bindingList.ShouldRaise("ListChanged");
        }

        [Fact]
        public void AsTrackable_On_Collection_Remove_Should_Raise_ListChanged()
        {
            var orders = Helper.GetOrdersIList();

            var trackable = orders.AsTrackable();
            var bindingList = (System.ComponentModel.IBindingList)trackable;

            bindingList.MonitorEvents();
            trackable.Remove(trackable[0]);

            bindingList.ShouldRaise("ListChanged");
        }

        [Fact]
        public void CancelEdit_On_Item_Should_Remove_From_Collection()
        {
            var orders = Helper.GetOrdersIList();

            var trackable = orders.AsTrackable();
            var bindingList = (System.ComponentModel.IBindingList)trackable;

            bindingList.AddNew();
            var withAddedCount = bindingList.Count;
            var addedItem = bindingList.Cast<Order>().Single(o => o.CustomerNumber == null);
            var editableObject = (System.ComponentModel.IEditableObject)addedItem;
            editableObject.CancelEdit();

            bindingList.Count.Should().Be(withAddedCount - 1, because: "item was canceled");
        }
        
        [Fact]
        public void Change_Property_On_Item_That_Implements_INotifyPropertyChanged_In_Collection_Should_Raise_ListChanged()
        {
            var orders = Helper.GetOrdersIList();

            var trackable = orders.AsTrackable();
            var bindingList = (System.ComponentModel.IBindingList)trackable;

            bindingList.MonitorEvents();
            ((Order)bindingList[0]).Id = 123;

            bindingList.ShouldRaise("ListChanged");
        }        

        [Fact]
        public void AcceptChanges_On_Collection_Should_Raise_ListChanged()
        {
            var orders = Helper.GetOrdersIList();
            var trackable = orders.AsTrackable();

            var first = trackable.First();
            var bl = trackable as System.ComponentModel.IBindingList;
            bl.ListChanged += (o, e) =>
            {
                ;
            };
            first.Id = 963;


            trackable.MonitorEvents();
            trackable.CastToIChangeTrackableCollection().AcceptChanges();            

            trackable.ShouldRaise("ListChanged");
        }

        [Fact]
        public void AcceptChanges_On_Collection_If_No_Changes_Should_Not_Raise_ListChanged()
        {
            var orders = Helper.GetOrdersIList();
            var trackable = orders.AsTrackable();

            trackable.MonitorEvents();
            trackable.CastToIChangeTrackableCollection().AcceptChanges();

            trackable.ShouldNotRaise("ListChanged");
        }

        [Fact]
        public void RejectChanges_On_Collection_Should_Raise_ListChanged()
        {
            var orders = Helper.GetOrdersIList();
            var trackable = orders.AsTrackable();

            var first = trackable.First();
            first.Id = 963;

            trackable.MonitorEvents();


            trackable.CastToIChangeTrackableCollection().RejectChanges();

            trackable.ShouldRaise("ListChanged");
        }

        [Fact]
        public void RejectChanges_On_Collection_If_No_Changes_Should_Not_Raise_ListChanged()
        {
            var orders = Helper.GetOrdersIList();
            var trackable = orders.AsTrackable();

            trackable.MonitorEvents();
            trackable.CastToIChangeTrackableCollection().RejectChanges();

            trackable.ShouldNotRaise("ListChanged");
        }
#endif
    }
}
