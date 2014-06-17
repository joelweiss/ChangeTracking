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
    public class IChangeTrackableTests
    {
        [TestMethod]
        public void AsTrackable_Should_Make_Object_Implement_IChangeTrackable()
        {
            var order = Helper.GetOrder();

            Order trackable = order.AsTrackable();

            trackable.Should().BeAssignableTo<IChangeTrackable<Order>>();
        }

        [TestMethod]
        public void When_AsTrackable_CastToIChangeTrackable_Should_Not_Throw_InvalidCastException()
        {
            var order = Helper.GetOrder();

            Order trackable = order.AsTrackable();

            trackable.Invoking(o => o.CastToIChangeTrackable()).ShouldNotThrow<InvalidCastException>();
        }

        [TestMethod]
        public void When_Not_AsTrackable_CastToIChangeTrackable_Should_Throw_InvalidCastException()
        {
            var order = Helper.GetOrder();

            order.Invoking(o => o.CastToIChangeTrackable()).ShouldThrow<InvalidCastException>();
        }

        [TestMethod]
        public void Change_Property_Should_Raise_StatusChanged_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();
            trackable.MonitorEvents();

            trackable.CustomerNumber = "Test1";

            trackable.ShouldRaise("StatusChanged");
        }

        [TestMethod]
        public void Change_Property_To_Same_Value_Should_Not_Raise_StatusChanged_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();
            trackable.MonitorEvents();

            trackable.CustomerNumber = "Test";

            trackable.ShouldNotRaise("StatusChanged");
        }

        [TestMethod]
        public void Change_Property_From_Null_To_Value_Should_Not_Throw()
        {
            var trackable = new Order { Id = 321, CustomerNumber = null }.AsTrackable();

            trackable.Invoking(o => o.CustomerNumber = "Test").ShouldNotThrow<NullReferenceException>();
        }

        [TestMethod]
        public void GetOriginalValue_Should_Return_Original_Value()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();

            trackable.CustomerNumber = "Test1";

            trackable.CastToIChangeTrackable().GetOriginalValue(o => o.CustomerNumber).Should().Be("Test");
        }

        [TestMethod]
        public void GetOriginal_Should_Return_Original()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();

            trackable.Id = 124;
            trackable.CustomerNumber = "Test1";

            var original = trackable.CastToIChangeTrackable().GetOriginal();
            var newOne = Helper.GetOrder();
            original.ShouldBeEquivalentTo(newOne);
        }

        [TestMethod]
        public void When_Setting_Status_Should_Be_That_Status()
        {
            var order = Helper.GetOrder();

            var trackable = order.AsTrackable(ChangeStatus.Added);

            trackable.CastToIChangeTrackable().ChangeTrackingStatus.Should().Be(ChangeStatus.Added);
        }

        [TestMethod]
        public void When_Status_Added_And_Change_Value_Status_Should_Stil_Be_Added()
        {
            var order = Helper.GetOrder();

            var trackable = order.AsTrackable(ChangeStatus.Added);
            trackable.CustomerNumber = "Test1";

            trackable.CastToIChangeTrackable().ChangeTrackingStatus.Should().Be(ChangeStatus.Added);
        }

        [TestMethod]
        public void When_Status_Is_Deleted_And_Change_Value_Should_Throw()
        {
            var order = Helper.GetOrder();

            var trackable = order.AsTrackable(ChangeStatus.Deleted);

            trackable.Invoking(o => o.CustomerNumber = "Test1").ShouldThrow<InvalidOperationException>();
        }
    }
}
