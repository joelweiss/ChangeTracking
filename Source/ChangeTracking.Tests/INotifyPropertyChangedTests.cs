using FluentAssertions;
using Xunit;

namespace ChangeTracking.Tests
{
    public class INotifyPropertyChangedTests
    {
#if NET452
        [Fact]
        public void AsTrackable_Should_Make_Object_Implement_INotifyPropertyChanged()
        {
            var order = Helper.GetOrder();

            Order trackable = order.AsTrackable();

            trackable.Should().BeAssignableTo<System.ComponentModel.INotifyPropertyChanged>();
        }

        [Fact]
        public void Change_Property_Should_Raise_PropertyChanged_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();
            trackable.MonitorEvents();

            trackable.CustomerNumber = "Test1";

            trackable.ShouldRaise("PropertyChanged");
        }

        [Fact]
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

        [Fact]
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

        [Fact]
        public void When_CollectionProperty_Children_Trackable_Change_Property_On_Item_In_Collection_Should_Raise_PropertyChanged_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();
            trackable.MonitorEvents();

            trackable.OrderDetails[0].ItemNo = "Testing";

            trackable.ShouldRaise("PropertyChanged");
        }

        [Fact]
        public void When_CollectionProperty_Children_Not_Trackable_Change_Property_On_Item_In_Collection_Should_Not_Raise_PropertyChanged_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable(makeCollectionPropertiesTrackable: false);
            trackable.MonitorEvents();

            trackable.OrderDetails[0].ItemNo = "Testing";

            trackable.ShouldNotRaise("PropertyChanged");
        }

        [Fact]
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

        [Fact]
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

        [Fact]
        public void When_ComplexProperty_Children_Trackable_Change_Property_On_Complex_Property_Should_Raise_PropertyChanged_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();
            trackable.MonitorEvents();

            trackable.Address.City = "Chicago";

            trackable.ShouldRaise("PropertyChanged");
        }

        [Fact]
        public void When_Not_ComplexProperty_Children_Trackable_Change_Property_On_Complex_Property_Should_Not_Raise_PropertyChanged_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable(makeComplexPropertiesTrackable: false);
            trackable.MonitorEvents();

            trackable.Address.City = "Chicago";

            trackable.ShouldNotRaise("PropertyChanged");
        }

        [Fact]
        public void Change_Property_Should_Raise_PropertyChanged_On_ChangeTrackingStatus_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();
            trackable.MonitorEvents();

            trackable.CustomerNumber = "Test1";
            IChangeTrackable<Order> changeTrackable = trackable.CastToIChangeTrackable();

            changeTrackable.ShouldRaisePropertyChangeFor(ct => ct.ChangeTrackingStatus);
        }
#endif
    }
}
