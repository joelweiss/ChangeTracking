using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace ChangeTracking.Tests
{
    public class IChangeTrackableTests
    {
        [Fact]
        public void AsTrackable_Should_Make_Object_Implement_IChangeTrackable()
        {
            var order = Helper.GetOrder();

            Order trackable = order.AsTrackable();

            trackable.Should().BeAssignableTo<IChangeTrackable<Order>>();
        }

        [Fact]
        public void When_AsTrackable_CastToIChangeTrackable_Should_Not_Throw_InvalidCastException()
        {
            var order = Helper.GetOrder();

            Order trackable = order.AsTrackable();

            trackable.Invoking(o => o.CastToIChangeTrackable()).ShouldNotThrow<InvalidCastException>();
        }

        [Fact]
        public void When_Not_AsTrackable_CastToIChangeTrackable_Should_Throw_InvalidCastException()
        {
            var order = Helper.GetOrder();

            order.Invoking(o => o.CastToIChangeTrackable()).ShouldThrow<InvalidCastException>();
        }

        [Fact]
        public void Change_Property_Should_Raise_StatusChanged_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();
            EventMonitor monitor = trackable.MonitorStatusChanged();

            trackable.CustomerNumber = "Test1";

            monitor.WasRaised.Should().BeTrue();
        }

        [Fact]
        public void Change_Property_To_Same_Value_Should_Not_Raise_StatusChanged_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();
            EventMonitor monitor = trackable.MonitorStatusChanged();

            trackable.CustomerNumber = "Test";

            monitor.WasRaised.Should().BeFalse();
        }

        [Fact]
        public void Change_Property_From_Null_To_Value_Should_Not_Throw()
        {
            var trackable = new Order { Id = 321, CustomerNumber = null }.AsTrackable();

            trackable.Invoking(o => o.CustomerNumber = "Test").ShouldNotThrow<NullReferenceException>();
        }

        [Fact]
        public void GetOriginalValue_Should_Return_Original_Value()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();

            trackable.CustomerNumber = "Test1";

            trackable.CastToIChangeTrackable().GetOriginalValue(o => o.CustomerNumber).Should().Be("Test");
        }

        [Fact]
        public void GetOriginalValue_Generic_By_Property_Name_Should_Return_Original_Value()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();

            trackable.CustomerNumber = "Test1";

            trackable.CastToIChangeTrackable().GetOriginalValue<string>("CustomerNumber").Should().Be("Test");
        }


        [Fact]
        public void GetOriginalValue_By_Property_Name_Should_Return_Original_Value()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();

            trackable.CustomerNumber = "Test1";

            trackable.CastToIChangeTrackable().GetOriginalValue("CustomerNumber").Should().Be("Test");
        }

        [Fact]
        public void GetOriginal_Should_Return_Original()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();

            trackable.Id = 124;
            trackable.CustomerNumber = "Test1";

            var original = trackable.CastToIChangeTrackable().GetOriginal();
            var newOne = Helper.GetOrder();
            original.ShouldBeEquivalentTo(newOne);
            (original is IChangeTrackable).Should().BeFalse();
        }

        [Fact]
        public void When_Setting_Status_Should_Be_That_Status()
        {
            var order = Helper.GetOrder();

            var trackable = order.AsTrackable(ChangeStatus.Added);

            trackable.CastToIChangeTrackable().ChangeTrackingStatus.Should().Be(ChangeStatus.Added);
        }

        [Fact]
        public void When_Status_Added_And_Change_Value_Status_Should_Still_Be_Added()
        {
            var order = Helper.GetOrder();

            var trackable = order.AsTrackable(ChangeStatus.Added);
            trackable.CustomerNumber = "Test1";

            trackable.CastToIChangeTrackable().ChangeTrackingStatus.Should().Be(ChangeStatus.Added);
        }

        [Fact]
        public void When_Status_Is_Deleted_And_Change_Value_Should_Throw()
        {
            var order = Helper.GetOrder();

            var trackable = order.AsTrackable(ChangeStatus.Deleted);

            trackable.Invoking(o => o.CustomerNumber = "Test1").ShouldThrow<InvalidOperationException>();
        }

        [Fact]
        public void AcceptChanges_Should_Status_Be_Unchanged()
        {
            var order = Helper.GetOrder();

            var trackable = order.AsTrackable();
            trackable.Id = 963;
            trackable.CustomerNumber = "Testing";
            var intf = trackable.CastToIChangeTrackable();

            var oldChangeStatus = intf.ChangeTrackingStatus;
            intf.AcceptChanges();

            oldChangeStatus.Should().Be(ChangeStatus.Changed);
            intf.ChangeTrackingStatus.Should().Be(ChangeStatus.Unchanged);
        }

        [Fact]
        public void AcceptChanges_Should_AcceptChanges()
        {
            var order = Helper.GetOrder();

            var trackable = order.AsTrackable();
            trackable.Id = 963;
            trackable.CustomerNumber = "Testing";
            var intf = trackable.CastToIChangeTrackable();
            intf.AcceptChanges();

            intf.ShouldBeEquivalentTo(intf.GetOriginal().AsTrackable());
            intf.GetOriginalValue(o => o.Id).Should().Be(963);
        }

        [Fact]
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

        [Fact]
        public void RejectChanges_Should_RejectChanges()
        {
            var order = Helper.GetOrder();

            var trackable = order.AsTrackable();
            trackable.Id = 963;
            trackable.CustomerNumber = "Testing";
            var intf = trackable.CastToIChangeTrackable();
            intf.RejectChanges();

            trackable.ShouldBeEquivalentTo(Helper.GetOrder().AsTrackable());
        }

        [Fact]
        public void AcceptChanges_Should_AcceptChanges_On_Complex_Property()
        {
            var order = Helper.GetOrder();

            var trackable = order.AsTrackable();
            trackable.Address.AddressId = 963;
            trackable.Address.City = "Chicago";
            var intf = trackable.CastToIChangeTrackable();
            intf.AcceptChanges();

            trackable.Address.ShouldBeEquivalentTo(trackable.Address.CastToIChangeTrackable().GetOriginal().AsTrackable());
        }

        [Fact]
        public void RejectChanges_Should_Status_Be_Unchanged_On_Complex_Property()
        {
            var order = Helper.GetOrder();

            var trackable = order.AsTrackable();
            trackable.Address.AddressId = 963;
            trackable.Address.City = "Chicago";
            var oldChangeStatus = trackable.Address.CastToIChangeTrackable().ChangeTrackingStatus;
            trackable.CastToIChangeTrackable().RejectChanges();

            oldChangeStatus.Should().Be(ChangeStatus.Changed);
            trackable.Address.CastToIChangeTrackable().ChangeTrackingStatus.Should().Be(ChangeStatus.Unchanged);
        }

        [Fact]
        public void RejectChanges_Should_RejectChanges_On_Complex_Property()
        {
            var order = Helper.GetOrder();

            var trackable = order.AsTrackable();
            trackable.Address.AddressId = 963;
            trackable.Address.City = "Chicago";
            var intf = trackable.CastToIChangeTrackable();
            intf.RejectChanges();

            trackable.Address.ShouldBeEquivalentTo(Helper.GetOrder().AsTrackable().Address);
        }

        [Fact]
        public void AcceptChanges_Should_AcceptChanges_On_Collection_Property()
        {
            var order = Helper.GetOrder();

            var trackable = order.AsTrackable();
            trackable.OrderDetails[0].ItemNo = "ItemTesting";
            var intf = trackable.CastToIChangeTrackable();
            intf.AcceptChanges();

            trackable.OrderDetails[0].ItemNo.Should().Be("ItemTesting");
            trackable.OrderDetails[0].CastToIChangeTrackable().GetOriginalValue(i => i.ItemNo).Should().Be("ItemTesting");
        }

        [Fact]
        public void RejectChanges_Should_Status_Be_Unchanged_On_Collection_Property()
        {
            var order = Helper.GetOrder();

            var trackable = order.AsTrackable();
            trackable.OrderDetails[0].ItemNo = "ItemTesting";
            var intf = trackable.CastToIChangeTrackable();
            var oldChangeStatus = trackable.OrderDetails[0].CastToIChangeTrackable().ChangeTrackingStatus;
            intf.RejectChanges();

            oldChangeStatus.Should().Be(ChangeStatus.Changed);
            trackable.OrderDetails[0].CastToIChangeTrackable().ChangeTrackingStatus.Should().Be(ChangeStatus.Unchanged);
        }

        [Fact]
        public void RejectChanges_Should_RejectChanges_On_Collection_Property()
        {
            var order = Helper.GetOrder();

            var trackable = order.AsTrackable();
            trackable.OrderDetails[0].ItemNo = "ItemTesting";
            var intf = trackable.CastToIChangeTrackable();
            intf.RejectChanges();

            trackable.OrderDetails[0].ShouldBeEquivalentTo(Helper.GetOrder().OrderDetails[0].AsTrackable());
        }

        [Fact]
        public void AsTrackable_Should_ComplexProperty_Children_Be_Trackable()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();

            trackable.Address.Should().BeAssignableTo<IChangeTrackable<Address>>();
        }

        [Fact]
        public void AsTrackable_When_ComplexProperty_Children_Trackable_Child_Change_Should_Change_Parent()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();

            trackable.Address.AddressId = 999;

            trackable.Address.CastToIChangeTrackable().ChangeTrackingStatus.Should().Be(ChangeStatus.Changed);
            trackable.CastToIChangeTrackable().ChangeTrackingStatus.Should().Be(ChangeStatus.Changed);
        }

        [Fact]
        public void AsTrackable_When_ComplexProperty_Children_Trackable_AcceptChanges_Should_Accept_On_Children()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();

            trackable.Address.AddressId = 999;
            trackable.CastToIChangeTrackable().AcceptChanges();

            trackable.Address.CastToIChangeTrackable().GetOriginalValue(a => a.AddressId).Should().Be(999);
        }

        [Fact]
        public void AsTrackable_When_Passed_False_Should_Not_ComplexProperty_Children_Be_Trackable()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable(makeComplexPropertiesTrackable: false);

            trackable.Address.AddressId = 99;
            Action action = () => { var address = (IChangeTrackable<Address>)trackable.Address; };

            action.ShouldThrow<InvalidCastException>();
        }

        [Fact]
        public void AsTrackable_When_Not_ComplexProperty_Children_Trackable_AcceptChanges_Should_Work()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable(makeComplexPropertiesTrackable: false);

            trackable.Id = 999;
            trackable.Address.AddressId = 999;
            trackable.CastToIChangeTrackable().AcceptChanges();
            var intf = trackable.CastToIChangeTrackable();

            intf.GetOriginalValue(o => o.Id).Should().Be(999);
        }

        [Fact]
        public void AsTrackable_When_Not_ComplexProperty_Children_Trackable_RejectChanges_Should_Work()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable(makeComplexPropertiesTrackable: false);

            trackable.Id = 999;
            trackable.Address.AddressId = 999;
            trackable.CastToIChangeTrackable().RejectChanges();
            var intf = trackable.CastToIChangeTrackable();

            intf.GetOriginalValue(o => o.Id).Should().Be(1);
            trackable.Address.AddressId.Should().Be(999);
        }

        [Fact]
        public void AsTrackable_Should_CollectionProperty_Children_Be_Trackable()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();

            trackable.OrderDetails.Should().BeAssignableTo<IChangeTrackableCollection<OrderDetail>>();
        }

        [Fact]
        public void AsTrackable_When_CollectionProperty_Children_Trackable_Child_Change_Property_Should_Change_Parent()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();

            trackable.OrderDetails[0].OrderDetailId = 999;

            trackable.OrderDetails.CastToIChangeTrackableCollection().IsChanged.Should().BeTrue();
            trackable.CastToIChangeTrackable().ChangeTrackingStatus.Should().Be(ChangeStatus.Changed);
        }

        [Fact]
        public void AsTrackable_When_CollectionProperty_Children_Trackable_Child_Change_Collection_Should_Change_Parent()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();

            trackable.OrderDetails.Add(new OrderDetail
            {
                OrderDetailId = 123,
                ItemNo = "Item123"
            });

            trackable.OrderDetails.CastToIChangeTrackableCollection().IsChanged.Should().BeTrue();
            trackable.CastToIChangeTrackable().ChangeTrackingStatus.Should().Be(ChangeStatus.Changed);
        }

        [Fact]
        public void AsTrackable_When_CollectionProperty_Children_Trackable_AcceptChanges_Should_Accept_On_Children()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();

            trackable.OrderDetails[0].OrderDetailId = 999;
            trackable.CastToIChangeTrackable().AcceptChanges();

            trackable.OrderDetails[0].CastToIChangeTrackable().GetOriginalValue(o => o.OrderDetailId).Should().Be(999);
        }

        [Fact]
        public void AsTrackable_When_Passed_False_Should_Not_CollectionProperty_Children_Be_Trackable()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable(makeCollectionPropertiesTrackable: false);

            Action action = () => { var details = (IChangeTrackableCollection<OrderDetail>)trackable.OrderDetails; };

            action.ShouldThrow<InvalidCastException>();
        }

        [Fact]
        public void AsTrackable_When_Not_CollectionProperty_Children_Trackable_AcceptChanges_Should_Work()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable(makeCollectionPropertiesTrackable: false);

            trackable.Id = 999;
            trackable.OrderDetails[0].OrderDetailId = 999;
            trackable.CastToIChangeTrackable().AcceptChanges();
            var intf = trackable.CastToIChangeTrackable();

            intf.GetOriginalValue(o => o.Id).Should().Be(999);
        }

        [Fact]
        public void AsTrackable_When_Not_CollectionProperty_Children_Trackable_RejectChanges_Should_Work()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable(makeCollectionPropertiesTrackable: false);

            trackable.Id = 999;
            trackable.OrderDetails[0].OrderDetailId = 999;
            trackable.CastToIChangeTrackable().RejectChanges();
            var intf = trackable.CastToIChangeTrackable();

            intf.GetOriginalValue(o => o.Id).Should().Be(1);
            trackable.OrderDetails[0].OrderDetailId.Should().Be(999);
        }

        [Fact]
        public void AcceptChanges_Should_Raise_StatusChanged()
        {
            var order = Helper.GetOrder();

            var trackable = order.AsTrackable();
            trackable.Id = 963;
            EventMonitor monitor = trackable.MonitorStatusChanged();
            var intf = trackable.CastToIChangeTrackable();
            intf.AcceptChanges();

            monitor.WasRaised.Should().BeTrue();
        }

        [Fact]
        public void RejectChanges_Should_Raise_StatusChanged()
        {
            var order = Helper.GetOrder();

            var trackable = order.AsTrackable();
            trackable.Id = 963;
            EventMonitor monitor = trackable.MonitorStatusChanged();
            var intf = trackable.CastToIChangeTrackable();
            intf.RejectChanges();

            monitor.WasRaised.Should().BeTrue();
        }

        [Fact]
        public void When_StatusChanged_Raised_Property_Should_Be_Changed()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();
            var intf = trackable.CastToIChangeTrackable();
            int newValue = 0;
            intf.StatusChanged += (o, e) => newValue = order.Id;

            trackable.Id = 1234;

            newValue.Should().Be(1234);
        }

        [Fact]
        public void When_CollectionProperty_Children_Trackable_Change_Property_On_Item_In_Collection_Should_Raise_StatusChanged_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();
            EventMonitor monitor = trackable.MonitorStatusChanged();

            trackable.OrderDetails[0].ItemNo = "Testing";

            monitor.WasRaised.Should().BeTrue();
        }

        [Fact]
        public void When_CollectionProperty_Children_Not_Trackable_Change_Property_On_Item_In_Collection_Should_Not_Raise_StatusChanged_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable(makeCollectionPropertiesTrackable: false);
            EventMonitor monitor = trackable.MonitorStatusChanged();

            trackable.OrderDetails[0].ItemNo = "Testing";

            monitor.WasRaised.Should().BeFalse();
        }

        [Fact]
        public void When_CollectionProperty_Children_Trackable_Change_CollectionProperty_Should_Raise_StatusChanged_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();
            EventMonitor monitor = trackable.MonitorStatusChanged();

            trackable.OrderDetails.Add(new OrderDetail
            {
                OrderDetailId = 123,
                ItemNo = "Item123"
            });

            monitor.WasRaised.Should().BeTrue();
        }

        [Fact]
        public void When_CollectionProperty_Children_Not_Trackable_Change_CollectionProperty_Should_Not_Raise_StatusChanged_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable(makeCollectionPropertiesTrackable: false);
            EventMonitor monitor = trackable.MonitorStatusChanged();

            trackable.OrderDetails.Add(new OrderDetail
            {
                OrderDetailId = 123,
                ItemNo = "Item123"
            });

            monitor.WasRaised.Should().BeFalse();
        }

        [Fact]
        public void When_ComplexProperty_Children_Trackable_Change_Property_On_Complex_Property_Should_Raise_StatusChanged_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();
            EventMonitor monitor = trackable.MonitorStatusChanged();

            trackable.Address.City = "Chicago";

            monitor.WasRaised.Should().BeTrue();
        }

        [Fact]
        public void When_Not_ComplexProperty_Children_Trackable_Change_Property_On_Complex_Property_Should_Not_Raise_StatusChanged_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable(makeComplexPropertiesTrackable: false);
            EventMonitor monitor = trackable.MonitorStatusChanged();

            trackable.Address.City = "Chicago";

            monitor.WasRaised.Should().BeFalse();
        }

        [Fact]
        public void When_CollectionProperty_Children_Trackable_Set_CollectionProperty_And_Change_Collection_Should_Raise_StatusChanged_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();
            trackable.OrderDetails = new List<OrderDetail>();
            trackable.CastToIChangeTrackable().AcceptChanges();
            EventMonitor monitor = trackable.MonitorStatusChanged();

            trackable.OrderDetails.Add(new OrderDetail());

            monitor.WasRaised.Should().BeTrue();
        }

        [Fact]
        public void When_CollectionProperty_Children_Trackable_Set_CollectionProperty_And_Change_Collection_Item_Property_Should_Raise_StatusChanged_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();
            trackable.OrderDetails = new List<OrderDetail> { new OrderDetail() };
            trackable.CastToIChangeTrackable().AcceptChanges();
            EventMonitor monitor = trackable.MonitorStatusChanged();

            trackable.OrderDetails[0].OrderDetailId = 123;

            monitor.WasRaised.Should().BeTrue();
        }

        [Fact]
        public void When_ComplexProperty_Children_Trackable_Set_ComplexProperty_And_Change_Property_Should_Raise_StatusChanged_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();

            trackable.Address = new Address();
            trackable.CastToIChangeTrackable().AcceptChanges();
            EventMonitor monitor = trackable.MonitorStatusChanged();
            trackable.Address.AddressId = 123;

            monitor.WasRaised.Should().BeTrue();
        }

        [Fact]
        public void When_Nothing_Is_Changed_Should_Be_Unchanged()
        {
            Order order = Helper.GetOrder();
            var trackable = order.AsTrackable();

            trackable.CastToIChangeTrackable().IsChanged.Should().BeFalse();
            trackable.CastToIChangeTrackable().ChangeTrackingStatus.Should().Be(ChangeStatus.Unchanged);
        }



        [Fact]
        public void When_Nothing_Is_Changed_ChangedProperties_Should_BeEmpty()
        {
            Order order = Helper.GetOrder();
            var trackable = order.AsTrackable();

            trackable.CastToIChangeTrackable().ChangedProperties.Should().BeEmpty();
        }

        [Fact]
        public void When_Changed_ChangedProperties_Should_ReturnChangedProperties()
        {
            Order order = Helper.GetOrder();
            var trackable = order.AsTrackable();
            trackable.CustomerNumber = "Change";

            trackable.CastToIChangeTrackable().ChangedProperties.Should().BeEquivalentTo(nameof(Order.CustomerNumber));
        }

        [Fact]
        public void Change_Property_From_Null_To_Null_Should_Not_Throw()
        {
            var trackable = new Order { Id = 321, }.AsTrackable();

            trackable.Invoking(o => o.Address = null).ShouldNotThrow<NullReferenceException>();
        }

        [Fact]
        public void Change_Property_From_Value_To_Null_Should_Not_Throw()
        {
            var trackable = new Order { Id = 321, Address = new Address() }.AsTrackable();

            trackable.Invoking(o => o.Address = null).ShouldNotThrow<NullReferenceException>();
        }

        [Fact]
        public void GetCurrent_Should_Return_Original()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();

            trackable.Id = 124;
            trackable.CustomerNumber = "Test1";

            var current = trackable.CastToIChangeTrackable().GetCurrent();

            current.ShouldBeEquivalentTo(trackable);
            (current is IChangeTrackable).Should().BeFalse();
        }

        [Fact]
        public void Insert_On_Child_Collection_Should_Be_Intercepted()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();

            EventMonitor monitor = trackable.MonitorStatusChanged();

            trackable.OrderDetails.Insert(0, new OrderDetail());

            monitor.WasRaised.Should().BeTrue();
        }

        [Fact]
        public void Complex_Property_Should_Be_Trackable_Even_Not_All_Properties_Are_Read_Write()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();

            trackable.Address.Should().BeAssignableTo<IChangeTrackable<Address>>();
        }
    }
}
