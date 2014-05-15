using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;

namespace ChangeTracking.Tests
{
    [TestClass]
    public class IBindingListTests
    {
        [TestMethod]
        public void AsTrackable_On_Collection_Should_Make_It_ICancelAddNew()
        {
            var orders = Helper.GetOrdersIList();

            var trackable = orders.AsTrackable();

            trackable.Should().BeAssignableTo<System.ComponentModel.ICancelAddNew>();
        }

        [TestMethod]
        public void AsTrackable_On_Collection_Should_Make_It_IBindingList()
        {
            var orders = Helper.GetOrdersIList();

            var trackable = orders.AsTrackable();

            trackable.Should().BeAssignableTo<System.ComponentModel.IBindingList>();
        }

        [TestMethod]
        public void AsTrackable_On_Collection_AddNew_Should_Raise_ListChanged()
        {
            var orders = Helper.GetOrdersIList();

            var trackable = orders.AsTrackable();
            var bindingList = (System.ComponentModel.IBindingList)trackable;

            bindingList.MonitorEvents();
            bindingList.AddNew();

            bindingList.ShouldRaise("ListChanged");
        }

        [TestMethod]
        public void CancelEdit_On_Item_Should_Remove_From_Collection()
        {
            var orders = Helper.GetOrdersIList();

            var trackable = orders.AsTrackable();
            var bindingList = (System.ComponentModel.IBindingList)trackable;

            bindingList.AddNew();
            var withAddedCount = bindingList.Count;
            var addedItem = bindingList.Cast<Order>().Single(o => o.CustumerNumber == null);
            var editableObject = (System.ComponentModel.IEditableObject)addedItem;
            editableObject.CancelEdit();

            bindingList.Count.Should().Be(withAddedCount - 1, because: "item was canceled");
        }
    }
}
