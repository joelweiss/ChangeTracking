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
    public class INotifyPropertyChangedTests
    {
        [TestMethod]
        public void AsTrackable_Should_Make_Object_Implement_INotifyPropertyChanged()
        {
            var order = Helper.GetOrder();

            Order trackable = order.AsTrackable();

            trackable.Should().BeAssignableTo<System.ComponentModel.INotifyPropertyChanged>();
        }

        [TestMethod]
        public void Change_Property_Should_Raise_PropertyChanged_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();
            trackable.MonitorEvents();

            trackable.CustomerNumber = "Test1";

            trackable.ShouldRaise("PropertyChanged");
        }

        [TestMethod]
        public void RejectChanges_Should_Raise_PropertyChanged()
        {
            var order = Helper.GetOrder();

            var trackable = order.AsTrackable();
            trackable.Id = 963;
            trackable.MonitorEvents();
            var intf = trackable.CastToIChangeTrackable();
            intf.RejectChanges();

            trackable.ShouldRaisePropertyChangeFor(o => o.Id);
        }

        [TestMethod]
        public void When_PropertyChanged_Raised_Property_Should_Be_Changed()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();
            var inpc = (System.ComponentModel.INotifyPropertyChanged)trackable;
            int newValue = 0;
            inpc.PropertyChanged += (o, e) => newValue = order.Id;

            trackable.Id = 1234;

            newValue.Should().Be(1234);
        }

        [TestMethod]
        public void When_CollectionProperty_Children_Trackable_Change_Property_On_Item_In_Collection_Should_Raise_PropertyChanged_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();
            trackable.MonitorEvents();

            trackable.OrderDetails[0].ItemNo = "Testing";

            trackable.ShouldRaise("PropertyChanged");
        }

        [TestMethod]
        public void When_CollectionProperty_Children_Not_Trackable_Change_Property_On_Item_In_Collection_Should_Not_Raise_PropertyChanged_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable(makeCollectionPropertiesTrackable: false);
            trackable.MonitorEvents();

            trackable.OrderDetails[0].ItemNo = "Testing";

            trackable.ShouldNotRaise("PropertyChanged");
        }

        [TestMethod]
        public void When_CollectionProperty_Children_Trackable_Change_CollectionProperty_Should_Raise_PropertyChanged_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();
            trackable.MonitorEvents();

            trackable.OrderDetails.Add(new OrderDetail
                  {
                      OrderDetailId = 123,
                      ItemNo = "Item123"
                  });

            trackable.ShouldRaise("PropertyChanged");
        }

        [TestMethod]
        public void When_CollectionProperty_Children_Not_Trackable_Change_CollectionProperty_Should_Not_Raise_PropertyChanged_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable(makeCollectionPropertiesTrackable: false);
            trackable.MonitorEvents();

            trackable.OrderDetails.Add(new OrderDetail
                  {
                      OrderDetailId = 123,
                      ItemNo = "Item123"
                  });

            trackable.ShouldNotRaise("PropertyChanged");
        }

        [TestMethod]
        public void When_ComplexProperty_Children_Trackable_Change_Property_On_Complex_Property_Should_Raise_PropertyChanged_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();
            trackable.MonitorEvents();

            trackable.Address.City = "Chicago";

            trackable.ShouldRaise("PropertyChanged");
        }

        [TestMethod]
        public void When_Not_ComplexProperty_Children_Trackable_Change_Property_On_Complex_Property_Should_Not_Raise_PropertyChanged_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable(makeComplexPropertiesTrackable: false);
            trackable.MonitorEvents();

            trackable.Address.City = "Chicago";

            trackable.ShouldNotRaise("PropertyChanged");
        }
    }
}
