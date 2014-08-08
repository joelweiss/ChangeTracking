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
    public class IEditableObjectTests
    {
        [TestMethod]
        public void AsTrackable_Should_Make_Object_Implement_IEditableObject()
        {
            var order = Helper.GetOrder();

            Order trackable = order.AsTrackable();

            trackable.Should().BeAssignableTo<System.ComponentModel.IEditableObject>();
        }

        [TestMethod]
        public void CancelEdit_On_Item_Should_Revert_Changes()
        {
            var order = Helper.GetOrder();

            var trackable = order.AsTrackable();
            var editableObject = (System.ComponentModel.IEditableObject)trackable;

            editableObject.BeginEdit();
            trackable.CustomerNumber = "Testing";
            editableObject.CancelEdit();

            trackable.CustomerNumber.Should().Be("Test", because: "item was canceled");
        }

        [TestMethod]
        public void CancelEdit_On_Item_After_EndEdit_Should_Not_Revert_Changes()
        {
            var order = Helper.GetOrder();

            var trackable = order.AsTrackable();
            var editableObject = (System.ComponentModel.IEditableObject)trackable;

            editableObject.BeginEdit();
            trackable.CustomerNumber = "Testing";
            editableObject.EndEdit();
            editableObject.CancelEdit();

            trackable.CustomerNumber.Should().Be("Testing", because: "item was canceled after calling EndEdit");
        }

        [TestMethod]
        public void With_Out_BeginEdit_CancelEdit_Should_Do_Nothing()
        {
            var order = Helper.GetOrder();

            var trackable = order.AsTrackable();
            var editableObject = (System.ComponentModel.IEditableObject)trackable;

            trackable.CustomerNumber = "Testing";
            editableObject.CancelEdit();

            trackable.CustomerNumber.Should().Be("Testing", because: "item was canceled after calling EndEdit");
        }

        [TestMethod]
        public void AcceptChanges_Should_Status_Be_Unchanged()
        {
            var order = Helper.GetOrder();

            var trackable = order.AsTrackable();
            trackable.Id = 963;
            trackable.CustomerNumber = "Testing";
            var intf = trackable.CastToIChangeTrackable();

            var oldChangeStatus = intf.ChangeTrackingStatus;
            intf.RejectChanges();

            oldChangeStatus.Should().Be(ChangeStatus.Changed);
            intf.ChangeTrackingStatus.Should().Be(ChangeStatus.Unchanged);
        }

        [TestMethod]
        public void AcceptChanges_Should_AcceptChanges()
        {
            var order = Helper.GetOrder();

            var trackable = order.AsTrackable();
            trackable.Id = 963;
            trackable.CustomerNumber = "Testing";
            var intf = trackable.CastToIChangeTrackable();
            intf.AcceptChanges();

            intf.GetOriginal().ShouldBeEquivalentTo(intf.GetOriginal());
            intf.GetOriginalValue(o => o.Id).Should().Be(963);
        }

        [TestMethod]
        public void RejectChanges_Should_Status_Be_Unchanged()
        {
            var order = Helper.GetOrder();

            var trackable = order.AsTrackable();
            trackable.Id = 963;
            trackable.CustomerNumber = "Testing";
            var intf = trackable.CastToIChangeTrackable();
            var oldChangeStatus = intf.ChangeTrackingStatus;
            intf.RejectChanges();

            oldChangeStatus.Should().Be(ChangeStatus.Changed);
            intf.ChangeTrackingStatus.Should().Be(ChangeStatus.Unchanged);
        }

        [TestMethod]
        public void RejectChanges_Should_RejectChanges()
        {
            var order = Helper.GetOrder();

            var trackable = order.AsTrackable();
            trackable.Id = 963;
            trackable.CustomerNumber = "Testing";
            var intf = trackable.CastToIChangeTrackable();
            intf.RejectChanges();

            trackable.ShouldBeEquivalentTo(Helper.GetOrder());
        }

        [TestMethod]
        public void RejectChanges_Should_AcceptChanges_Only_After_Last_AcceptChanges()
        {
            var order = Helper.GetOrder();

            var trackable = order.AsTrackable();
            trackable.Id = 963;
            trackable.CustomerNumber = "Testing";
            var intf = trackable.CastToIChangeTrackable();
            intf.AcceptChanges();

            intf.GetOriginal().ShouldBeEquivalentTo(intf.GetOriginal());
            intf.GetOriginalValue(o => o.Id).Should().Be(963);
        }
    }
}
