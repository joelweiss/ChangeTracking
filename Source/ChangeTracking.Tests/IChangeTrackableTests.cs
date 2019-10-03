using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

            trackable.Invoking(o => o.CastToIChangeTrackable()).Should().NotThrow<InvalidCastException>();
        }

        [Fact]
        public void When_Not_AsTrackable_CastToIChangeTrackable_Should_Throw_InvalidCastException()
        {
            var order = Helper.GetOrder();

            order.Invoking(o => o.CastToIChangeTrackable()).Should().Throw<InvalidCastException>();
        }

        [Fact]
        public void Change_Property_Should_Raise_StatusChanged_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();
            var monitor = ((IChangeTrackable)trackable).Monitor();

            trackable.CustomerNumber = "Test1";

            monitor.Should().Raise(nameof(IChangeTrackable.StatusChanged));
        }

        [Fact]
        public void Change_Property_To_Same_Value_Should_Not_Raise_StatusChanged_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();
            var monitor = ((IChangeTrackable)trackable).Monitor();

            trackable.CustomerNumber = "Test";

            monitor.Should().NotRaise(nameof(IChangeTrackable.StatusChanged));
        }

        [Fact]
        public void Change_Property_From_Null_To_Value_Should_Not_Throw()
        {
            var trackable = new Order { Id = 321, CustomerNumber = null }.AsTrackable();

            trackable.Invoking(o => o.CustomerNumber = "Test").Should().NotThrow<NullReferenceException>();
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
            original.Should().BeEquivalentTo(newOne, options => options.IgnoringCyclicReferences());
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

            trackable.Invoking(o => o.CustomerNumber = "Test1").Should().Throw<InvalidOperationException>();
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

            intf.Should().BeEquivalentTo(intf.GetOriginal().AsTrackable(), options => options.IgnoringCyclicReferences());
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

            trackable.Should().BeEquivalentTo(Helper.GetOrder().AsTrackable(), options => options.IgnoringCyclicReferences());
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

            trackable.Address.Should().BeEquivalentTo(trackable.Address.CastToIChangeTrackable().GetOriginal().AsTrackable());
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

            trackable.Address.Should().BeEquivalentTo(Helper.GetOrder().AsTrackable().Address);
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

            trackable.OrderDetails[0].Should().BeEquivalentTo(Helper.GetOrder().OrderDetails[0].AsTrackable(), options => options.IgnoringCyclicReferences());
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
            Action action = () => { _ = (IChangeTrackable<Address>)trackable.Address; };

            action.Should().Throw<InvalidCastException>();
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

            Action action = () => { _ = (IChangeTrackableCollection<OrderDetail>)trackable.OrderDetails; };

            action.Should().Throw<InvalidCastException>();
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
            var monitor = ((IChangeTrackable)trackable).Monitor();
            var intf = trackable.CastToIChangeTrackable();
            intf.AcceptChanges();

            monitor.Should().Raise(nameof(IChangeTrackable.StatusChanged));
        }

        [Fact]
        public void RejectChanges_Should_Raise_StatusChanged()
        {
            var order = Helper.GetOrder();

            var trackable = order.AsTrackable();
            trackable.Id = 963;
            var monitor = ((IChangeTrackable)trackable).Monitor();
            var intf = trackable.CastToIChangeTrackable();

            intf.RejectChanges();

            monitor.Should().Raise(nameof(IChangeTrackable.StatusChanged));
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
            var monitor = ((IChangeTrackable)trackable).Monitor();

            trackable.OrderDetails[0].ItemNo = "Testing";

            monitor.Should().Raise(nameof(IChangeTrackable.StatusChanged));
        }

        [Fact]
        public void When_CollectionProperty_Children_Not_Trackable_Change_Property_On_Item_In_Collection_Should_Not_Raise_StatusChanged_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable(makeCollectionPropertiesTrackable: false);
            var monitor = ((IChangeTrackable)trackable).Monitor();

            trackable.OrderDetails[0].ItemNo = "Testing";

            monitor.Should().NotRaise(nameof(IChangeTrackable.StatusChanged));
        }

        [Fact]
        public void When_CollectionProperty_Children_Trackable_Change_CollectionProperty_Should_Raise_StatusChanged_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();
            var monitor = ((IChangeTrackable)trackable).Monitor();

            trackable.OrderDetails.Add(new OrderDetail
            {
                OrderDetailId = 123,
                ItemNo = "Item123"
            });

            monitor.Should().Raise(nameof(IChangeTrackable.StatusChanged));
        }

        [Fact]
        public void When_CollectionProperty_Children_Not_Trackable_Change_CollectionProperty_Should_Not_Raise_StatusChanged_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable(makeCollectionPropertiesTrackable: false);
            var monitor = ((IChangeTrackable)trackable).Monitor();

            trackable.OrderDetails.Add(new OrderDetail
            {
                OrderDetailId = 123,
                ItemNo = "Item123"
            });

            monitor.Should().NotRaise(nameof(IChangeTrackable.StatusChanged));
        }

        [Fact]
        public void When_ComplexProperty_Children_Trackable_Change_Property_On_Complex_Property_Should_Raise_StatusChanged_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();
            var monitor = ((IChangeTrackable)trackable).Monitor();

            trackable.Address.City = "Chicago";

            monitor.Should().Raise(nameof(IChangeTrackable.StatusChanged));
        }

        [Fact]
        public void When_Not_ComplexProperty_Children_Trackable_Change_Property_On_Complex_Property_Should_Not_Raise_StatusChanged_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable(makeComplexPropertiesTrackable: false);
            var monitor = ((IChangeTrackable)trackable).Monitor();

            trackable.Address.City = "Chicago";

            monitor.Should().NotRaise(nameof(IChangeTrackable.StatusChanged));
        }

        [Fact]
        public void When_CollectionProperty_Children_Trackable_Set_CollectionProperty_And_Change_Collection_Should_Raise_StatusChanged_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();
            trackable.OrderDetails = new List<OrderDetail>();
            trackable.CastToIChangeTrackable().AcceptChanges();
            var monitor = ((IChangeTrackable)trackable).Monitor();

            trackable.OrderDetails.Add(new OrderDetail());

            monitor.Should().Raise(nameof(IChangeTrackable.StatusChanged));
        }

        [Fact]
        public void When_CollectionProperty_Children_Trackable_Set_CollectionProperty_And_Change_Collection_Item_Property_Should_Raise_StatusChanged_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();
            trackable.OrderDetails = new List<OrderDetail> { new OrderDetail() };
            trackable.CastToIChangeTrackable().AcceptChanges();
            var monitor = ((IChangeTrackable)trackable).Monitor();

            trackable.OrderDetails[0].OrderDetailId = 123;

            monitor.Should().Raise(nameof(IChangeTrackable.StatusChanged));
        }

        [Fact]
        public void When_ComplexProperty_Children_Trackable_Set_ComplexProperty_And_Change_Property_Should_Raise_StatusChanged_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();

            trackable.Address = new Address();
            trackable.CastToIChangeTrackable().AcceptChanges();
            var monitor = ((IChangeTrackable)trackable).Monitor();
            trackable.Address.AddressId = 123;

            monitor.Should().Raise(nameof(IChangeTrackable.StatusChanged));
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
        public void When_Changed_ComplexProperty_ChangedProperties_Should_ReturnChangedProperties()
        {
            Order order = Helper.GetOrder();
            var trackable = order.AsTrackable();
            trackable.Address.City = "Hanoi";

            trackable.CastToIChangeTrackable().ChangedProperties.Should().BeEquivalentTo(nameof(Order.Address));
        }

        [Fact]
        public void When_Changed_CollectionProperty_ChangedProperties_Should_ReturnChangedProperties()
        {
            Order order = Helper.GetOrder();
            var trackable = order.AsTrackable();
            trackable.OrderDetails.Clear();

            trackable.CastToIChangeTrackable().ChangedProperties.Should().BeEquivalentTo(nameof(Order.OrderDetails));
        }

        [Fact]
        public void Change_Property_From_Null_To_Null_Should_Not_Throw()
        {
            var trackable = new Order { Id = 321, }.AsTrackable();

            trackable.Invoking(o => o.Address = null).Should().NotThrow<NullReferenceException>();
        }

        [Fact]
        public void Change_Property_From_Value_To_Null_Should_Not_Throw()
        {
            var trackable = new Order { Id = 321, Address = new Address() }.AsTrackable();

            trackable.Invoking(o => o.Address = null).Should().NotThrow<NullReferenceException>();
        }

        [Fact]
        public void GetCurrent_Should_Return_Original()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();

            trackable.Id = 124;
            trackable.CustomerNumber = "Test1";

            var current = trackable.CastToIChangeTrackable().GetCurrent();

            current.Should().BeEquivalentTo(trackable, options => options.IgnoringCyclicReferences());
            (current is IChangeTrackable).Should().BeFalse();
        }

        [Fact]
        public void Insert_On_Child_Collection_Should_Be_Intercepted()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();

            var monitor = ((IChangeTrackable)trackable).Monitor();

            trackable.OrderDetails.Insert(0, new OrderDetail());

            monitor.Should().Raise(nameof(IChangeTrackable.StatusChanged));
        }

        [Fact]
        public void Complex_Property_Should_Be_Trackable_Even_Not_All_Properties_Are_Read_Write()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();

            trackable.Address.Should().BeAssignableTo<IChangeTrackable<Address>>();
        }

        [Fact]
        public async Task Concurrent_Access_To_ComplexProperty_Should_Not_Throw()
        {
            Order order = Helper.GetOrder();
            Order trackable = order.AsTrackable();

            int count = 2;
            Address[] addresses = new Address[count];
            Task[] tasks = new Task[count];

            for (int i = 0; i < count; i++)
            {
                int index = i;
                tasks[index] = Task.Run(() =>
                {
                    addresses[index] = trackable.Address;
                });
            }
            await Task.WhenAll(tasks);

            Address firstAddress = addresses[0];
            addresses.All(a => a != null && ReferenceEquals(firstAddress, a)).Should().BeTrue();
        }

        [Fact]
        public async Task Concurrent_Access_To_CollectionProperty_Should_Not_Throw()
        {
            Order order = Helper.GetOrder();
            Order trackable = order.AsTrackable();

            int count = 2;
            IList<OrderDetail>[] orderDetails = new IList<OrderDetail>[count];
            Task[] tasks = new Task[count];

            for (int i = 0; i < count; i++)
            {
                int index = i;
                tasks[index] = Task.Run(() =>
                {
                    orderDetails[index] = trackable.OrderDetails;
                });
            }
            await Task.WhenAll(tasks);

            IList<OrderDetail> firstOrderDetails = orderDetails[0];
            orderDetails.All(od => od != null && ReferenceEquals(firstOrderDetails, od)).Should().BeTrue();
        }

        [Fact]
        public void AcceptChanges_On_Circular_Reference_Should_Not_Throw_OverflowException()
        {
            InventoryUpdate update = GetObjectGraph();

            var trackable = update.AsTrackable();
            trackable.InventoryUpdateId = 3;

            trackable.Invoking(t => t.CastToIChangeTrackable().AcceptChanges()).Should().NotThrow<OverflowException>();
        }

        [Fact]
        public void RejectChanges_On_Circular_Reference_Should_Not_Throw_OverflowException()
        {
            InventoryUpdate update = GetObjectGraph();

            var trackable = update.AsTrackable();
            trackable.InventoryUpdateId = 3;

            trackable.Invoking(t => t.CastToIChangeTrackable().RejectChanges()).Should().NotThrow<OverflowException>();
        }

        [Fact]
        public void Circular_Reference_Should_Be_Same_Reference()
        {
            InventoryUpdate update = GetObjectGraph();

            var trackable = update.AsTrackable();

            trackable.Should().BeSameAs(trackable.LinkedInventoryUpdate.LinkedToInventoryUpdate);
            trackable.LinkedInventoryUpdate.Should().BeSameAs(trackable.LinkedInventoryUpdate.LinkedToInventoryUpdate.LinkedInventoryUpdate);
        }

        private static InventoryUpdate GetObjectGraph()
        {
            var update0 = new InventoryUpdate
            {
                InventoryUpdateId = 0,
                UpdateInfos = new List<UpdateInfo>
                {
                    new UpdateInfo
                    {
                        UpdateInfoId = 1
                    }
                }
            };
            update0.UpdateInfos[0].InventoryUpdate = update0;
            var update1 = new InventoryUpdate
            {
                InventoryUpdateId = 1,
                LinkedToInventoryUpdate = update0
            };
            update0.LinkedInventoryUpdate = update1;
            return update0;
        }

        [Fact]
        public void When_Changed_Back_Should_Be_Unchanged()
        {
            Order order = Helper.GetOrder();
            var trackable = order.AsTrackable();

            trackable.Id++;
            trackable.Id--;
            trackable.LinkedOrder.Id++;
            trackable.LinkedOrder.Id--;

            trackable.CastToIChangeTrackable().IsChanged.Should().BeFalse();
            trackable.CastToIChangeTrackable().ChangeTrackingStatus.Should().Be(ChangeStatus.Unchanged);
        }

        [Fact]
        public void Call_AsTrackable_On_Trackable_Should_Throw_InvalidOperationException()
        {
            IList<Order> orders = Helper.GetOrdersIList();
            IList<Order> trackable = orders.AsTrackable();

            trackable.Invoking(t => t.AsTrackable()).Should().Throw<InvalidOperationException>();
        }

        public class Phone
        {
            public virtual CallerId CallerId { get; set; }
            public virtual CallerId TheCallerId => CallerId;
        }

        public class CallerId
        {
            public virtual string Name { get; set; }
        }

        [Fact]
        public void ReadOnly_Property_Should_Not_Be_Intercepted()
        {
            Phone phone = new Phone
            {
                CallerId = new CallerId
                {
                    Name = "Caller"
                }
            };
            Phone trackable = phone.AsTrackable();

            trackable.TheCallerId.Name = "ChangedCaller";

            trackable.CallerId.CastToIChangeTrackable().IsChanged.Should().BeTrue();
            trackable.TheCallerId.Should().BeSameAs(trackable.CallerId);
        }

        [Fact]
        public void When_Setting_All_Back_Status_IsChanged_Should_Be_False()
        {
            var update0 = new InventoryUpdate
            {
                InventoryUpdateId = 0
            };

            var update1 = new InventoryUpdate
            {
                InventoryUpdateId = 1,
                LinkedToInventoryUpdateId = update0.InventoryUpdateId,
                LinkedToInventoryUpdate = update0
            };
            update0.LinkedInventoryUpdate = update1;

            var update3 = new InventoryUpdate
            {
                InventoryUpdateId = 3,
                LinkedToInventoryUpdateId = update1.InventoryUpdateId,
                LinkedToInventoryUpdate = update1
            };
            update1.LinkedInventoryUpdate = update3;

            var update2 = new InventoryUpdate
            {
                InventoryUpdateId = 2
            };

            IList<InventoryUpdate> updates = new List<InventoryUpdate>
            {
                update0,
                update1,
                update3,
                update2
            };

            IList<InventoryUpdate> trackableUpdates = updates.AsTrackable();

            List<InventoryUpdate> updatesToLink = trackableUpdates.OrderBy(iu => iu.InventoryUpdateId).ToList();
            InventoryUpdate linkedToInventoryUpdate = null;
            foreach (InventoryUpdate inventoryUpdate in updatesToLink)
            {
                inventoryUpdate.LinkedToInventoryUpdateId = linkedToInventoryUpdate?.InventoryUpdateId;
                inventoryUpdate.LinkedToInventoryUpdate = linkedToInventoryUpdate;
                inventoryUpdate.LinkedInventoryUpdate = null;
                if (linkedToInventoryUpdate != null)
                {
                    linkedToInventoryUpdate.LinkedInventoryUpdate = inventoryUpdate;
                }
                linkedToInventoryUpdate = inventoryUpdate;
            }

            InventoryUpdate inventoryUpdateToUnlink = trackableUpdates[3];
            linkedToInventoryUpdate = inventoryUpdateToUnlink.LinkedToInventoryUpdate;
            InventoryUpdate linkedInventoryUpdate = inventoryUpdateToUnlink.LinkedInventoryUpdate;

            inventoryUpdateToUnlink.LinkedToInventoryUpdateId = null;
            inventoryUpdateToUnlink.LinkedToInventoryUpdate = null;
            inventoryUpdateToUnlink.LinkedInventoryUpdate = null;

            if (linkedToInventoryUpdate != null)
            {
                linkedToInventoryUpdate.LinkedInventoryUpdate = linkedInventoryUpdate;
            }
            if (linkedInventoryUpdate != null)
            {
                linkedInventoryUpdate.LinkedToInventoryUpdateId = linkedToInventoryUpdate?.InventoryUpdateId;
                linkedInventoryUpdate.LinkedToInventoryUpdate = linkedToInventoryUpdate;
            }

            trackableUpdates.CastToIChangeTrackableCollection().IsChanged.Should().BeFalse();
            trackableUpdates[0].CastToIChangeTrackable().IsChanged.Should().BeFalse();
            trackableUpdates[1].CastToIChangeTrackable().IsChanged.Should().BeFalse();
            trackableUpdates[2].CastToIChangeTrackable().IsChanged.Should().BeFalse();
            trackableUpdates[3].CastToIChangeTrackable().IsChanged.Should().BeFalse();
        }
    }
}