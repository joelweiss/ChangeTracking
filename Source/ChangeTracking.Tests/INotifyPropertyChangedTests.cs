using FluentAssertions;
using System.ComponentModel;
using Xunit;

namespace ChangeTracking.Tests
{
    public class INotifyPropertyChangedTests
    {
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
            ((INotifyPropertyChanged)trackable).MonitorEvents();

            trackable.CustomerNumber = "Test1";

            trackable.ShouldRaisePropertyChangeFor(o => o.CustomerNumber);
        }

        [Fact]
        public void RejectChanges_Should_Raise_PropertyChanged()
        {
            var order = Helper.GetOrder();

            var trackable = order.AsTrackable();
            trackable.Id = 963;
            ((INotifyPropertyChanged)trackable).MonitorEvents();
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
            ((INotifyPropertyChanged)trackable).MonitorEvents();

            trackable.OrderDetails[0].ItemNo = "Testing";

            trackable.ShouldRaisePropertyChangeFor(o => o.OrderDetails);
        }

        [Fact]
        public void When_CollectionProperty_Children_Not_Trackable_Change_Property_On_Item_In_Collection_Should_Not_Raise_PropertyChanged_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable(makeCollectionPropertiesTrackable: false);
            ((INotifyPropertyChanged)trackable).MonitorEvents();

            trackable.OrderDetails[0].ItemNo = "Testing";

            trackable.ShouldNotRaisePropertyChangeFor(o => o.OrderDetails);
        }

        [Fact]
        public void When_CollectionProperty_Children_Trackable_Change_CollectionProperty_Should_Raise_PropertyChanged_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();
            ((INotifyPropertyChanged)trackable).MonitorEvents();

            trackable.OrderDetails.Add(new OrderDetail
            {
                OrderDetailId = 123,
                ItemNo = "Item123"
            });

            trackable.ShouldRaisePropertyChangeFor(o => o.OrderDetails);
        }

        [Fact]
        public void When_CollectionProperty_Children_Not_Trackable_Change_CollectionProperty_Should_Not_Raise_PropertyChanged_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable(makeCollectionPropertiesTrackable: false);
            ((INotifyPropertyChanged)trackable).MonitorEvents();

            trackable.OrderDetails.Add(new OrderDetail
            {
                OrderDetailId = 123,
                ItemNo = "Item123"
            });

            trackable.ShouldNotRaisePropertyChangeFor(o => o.OrderDetails);
        }

        [Fact]
        public void When_ComplexProperty_Children_Trackable_Change_Property_On_Complex_Property_Should_Raise_PropertyChanged_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();
            ((INotifyPropertyChanged)trackable).MonitorEvents();

            trackable.Address.City = "Chicago";

            trackable.ShouldRaisePropertyChangeFor(o => o.Address);
        }

        [Fact]
        public void When_Not_ComplexProperty_Children_Trackable_Change_Property_On_Complex_Property_Should_Not_Raise_PropertyChanged_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable(makeComplexPropertiesTrackable: false);
            ((INotifyPropertyChanged)trackable).MonitorEvents();

            trackable.Address.City = "Chicago";

            trackable.ShouldNotRaisePropertyChangeFor(o => o.Address);
        }

        [Fact]
        public void Change_Property_Should_Raise_PropertyChanged_On_ChangeTrackingStatus_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();
            ((INotifyPropertyChanged)trackable).MonitorEvents();

            trackable.CustomerNumber = "Test1";
            IChangeTrackable<Order> changeTrackable = trackable.CastToIChangeTrackable();

            changeTrackable.ShouldRaisePropertyChangeFor(ct => ct.ChangeTrackingStatus);
        }

        [Fact]
        public void Change_Property_Should_Raise_PropertyChanged_On_ChangedProperties()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();
            ((INotifyPropertyChanged)trackable).MonitorEvents();

            trackable.CustomerNumber = "Test1";
            IChangeTrackable<Order> changeTrackable = trackable.CastToIChangeTrackable();

            changeTrackable.ShouldRaisePropertyChangeFor(ct => ct.ChangedProperties);
        }

        [Fact]
        public void Change_Property_From_Value_To_Null_Should_Stop_Notification()
        {
            Order trackable = new Order { Id = 321, Address = new Address { AddressId = 0 } }.AsTrackable();
            Address trackableAddress = trackable.Address;
            trackable.Address = null;

            trackable.CastToIChangeTrackable().MonitorEvents();
            trackableAddress.AddressId = 2;

            trackable.ShouldNotRaisePropertyChangeFor(o => o.Address);
        }
        
        [Fact]
        public void Change_Property_Should_Raise_PropertyChanged_Event_if_non_virtual_method()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();
            ((INotifyPropertyChanged)trackable).MonitorEvents();

            trackable.NonVirtualModifier();

            trackable.ShouldRaisePropertyChangeFor(o => o.CustomerNumber);
        }

        // Todo: It currently prevents the IDictionary support.
        //[Fact]
        public void Change_Property_Should_Raise_PropertyChanged_Event_if_method_virtual()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();
            ((INotifyPropertyChanged)trackable).MonitorEvents();

            trackable.VirtualModifier();

            trackable.ShouldRaisePropertyChangeFor(o => o.CustomerNumber);
        }
    }
}
