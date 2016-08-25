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
        public void GetOriginalValue_Generic_By_Property_Name_Should_Return_Original_Value()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();

            trackable.CustomerNumber = "Test1";

            trackable.CastToIChangeTrackable().GetOriginalValue<string>("CustomerNumber").Should().Be("Test");
        }


        [TestMethod]
        public void GetOriginalValue_By_Property_Name_Should_Return_Original_Value()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();

            trackable.CustomerNumber = "Test1";

            trackable.CastToIChangeTrackable().GetOriginalValue("CustomerNumber").Should().Be("Test");
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

        [TestMethod]
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

        [TestMethod]
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

            trackable.ShouldBeEquivalentTo(Helper.GetOrder().AsTrackable());
        }

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
        public void RejectChanges_Should_RejectChanges_On_Collection_Property()
        {
            var order = Helper.GetOrder();

            var trackable = order.AsTrackable();
            trackable.OrderDetails[0].ItemNo = "ItemTesting";
            var intf = trackable.CastToIChangeTrackable();
            intf.RejectChanges();

            trackable.OrderDetails[0].ShouldBeEquivalentTo(Helper.GetOrder().OrderDetails[0].AsTrackable());
        }

        [TestMethod]
        public void AsTrackable_Should_ComplexProperty_Children_Be_Trackable()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();

            trackable.Address.Should().BeAssignableTo<IChangeTrackable<Address>>();
        }

        [TestMethod]
        public void AsTrackable_When_ComplexProperty_Children_Trackable_Child_Change_Should_Change_Parent()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();

            trackable.Address.AddressId = 999;

            trackable.Address.CastToIChangeTrackable().ChangeTrackingStatus.Should().Be(ChangeStatus.Changed);
            trackable.CastToIChangeTrackable().ChangeTrackingStatus.Should().Be(ChangeStatus.Changed);
        }

        [TestMethod]
        public void AsTrackable_When_ComplexProperty_Children_Trackable_AcceptChanes_Should_Accept_On_Childern()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();

            trackable.Address.AddressId = 999;
            trackable.CastToIChangeTrackable().AcceptChanges();

            trackable.Address.CastToIChangeTrackable().GetOriginalValue(a => a.AddressId).Should().Be(999);
        }

        [TestMethod]
        public void AsTrackable_When_Passed_False_Should_Not_ComplexProperty_Children_Be_Trackable()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable(makeComplexPropertiesTrackable: false);

            trackable.Address.AddressId = 99;
            Action action = () => { var address = (IChangeTrackable<Address>)trackable.Address; };

            action.ShouldThrow<InvalidCastException>();
        }

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
        public void AsTrackable_Should_CollectionProperty_Children_Be_Trackable()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();

            trackable.OrderDetails.Should().BeAssignableTo<IChangeTrackableCollection<OrderDetail>>();
        }

        [TestMethod]
        public void AsTrackable_When_CollectionProperty_Children_Trackable_Child_Change_Property_Should_Change_Parent()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();

            trackable.OrderDetails[0].OrderDetailId = 999;

            trackable.OrderDetails.CastToIChangeTrackableCollection().IsChanged.Should().BeTrue();
            trackable.CastToIChangeTrackable().ChangeTrackingStatus.Should().Be(ChangeStatus.Changed);
        }

        [TestMethod]
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

        [TestMethod]
        public void AsTrackable_When_CollectionProperty_Children_Trackable_AcceptChanes_Should_Accept_On_Childern()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();

            trackable.OrderDetails[0].OrderDetailId = 999;
            trackable.CastToIChangeTrackable().AcceptChanges();

            trackable.OrderDetails[0].CastToIChangeTrackable().GetOriginalValue(o => o.OrderDetailId).Should().Be(999);
        }

        [TestMethod]
        public void AsTrackable_When_Passed_False_Should_Not_CollectionProperty_Children_Be_Trackable()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable(makeCollectionPropertiesTrackable: false);

            Action action = () => { var details = (IChangeTrackableCollection<OrderDetail>)trackable.OrderDetails; };

            action.ShouldThrow<InvalidCastException>();
        }

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
        public void AcceptChanges_Should_Raise_StatusChanged()
        {
            var order = Helper.GetOrder();

            var trackable = order.AsTrackable();
            trackable.Id = 963;
            trackable.MonitorEvents();
            var intf = trackable.CastToIChangeTrackable();
            intf.AcceptChanges();

            trackable.ShouldRaise("StatusChanged");
        }

        [TestMethod]
        public void RejectChanges_Should_Raise_StatusChanged()
        {
            var order = Helper.GetOrder();

            var trackable = order.AsTrackable();
            trackable.Id = 963;
            trackable.MonitorEvents();
            var intf = trackable.CastToIChangeTrackable();
            intf.RejectChanges();

            trackable.ShouldRaise("StatusChanged");
        }

        [TestMethod]
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

        [TestMethod]
        public void When_CollectionProperty_Children_Trackable_Change_Property_On_Item_In_Collection_Should_Raise_StatusChanged_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();
            trackable.MonitorEvents();

            trackable.OrderDetails[0].ItemNo = "Testing";

            trackable.ShouldRaise("StatusChanged");
        }

        [TestMethod]
        public void When_CollectionProperty_Children_Not_Trackable_Change_Property_On_Item_In_Collection_Should_Not_Raise_StatusChanged_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable(makeCollectionPropertiesTrackable: false);
            trackable.MonitorEvents();

            trackable.OrderDetails[0].ItemNo = "Testing";

            trackable.ShouldNotRaise("StatusChanged");
        }

        [TestMethod]
        public void When_CollectionProperty_Children_Trackable_Change_CollectionProperty_Should_Raise_StatusChanged_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();
            trackable.MonitorEvents();

            trackable.OrderDetails.Add(new OrderDetail
            {
                OrderDetailId = 123,
                ItemNo = "Item123"
            });

            trackable.ShouldRaise("StatusChanged");
        }

        [TestMethod]
        public void When_CollectionProperty_Children_Not_Trackable_Change_CollectionProperty_Should_Not_Raise_StatusChanged_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable(makeCollectionPropertiesTrackable: false);
            trackable.MonitorEvents();

            trackable.OrderDetails.Add(new OrderDetail
            {
                OrderDetailId = 123,
                ItemNo = "Item123"
            });

            trackable.ShouldNotRaise("StatusChanged");
        }

        [TestMethod]
        public void When_ComplexProperty_Children_Trackable_Change_Property_On_Complex_Property_Should_Raise_StatusChanged_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();
            trackable.MonitorEvents();

            trackable.Address.City = "Chicago";

            trackable.ShouldRaise("StatusChanged");
        }

        [TestMethod]
        public void When_Not_ComplexProperty_Children_Trackable_Change_Property_On_Complex_Property_Should_Not_Raise_StatusChanged_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable(makeComplexPropertiesTrackable: false);
            trackable.MonitorEvents();

            trackable.Address.City = "Chicago";

            trackable.ShouldNotRaise("StatusChanged");
        }

        [TestMethod]
        public void When_CollectionProperty_Children_Trackable_Set_CollectionProperty_And_Change_Collection_Should_Raise_StatusChanged_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();
            trackable.OrderDetails = new List<OrderDetail>();
            trackable.CastToIChangeTrackable().AcceptChanges();
            trackable.MonitorEvents();

            trackable.OrderDetails.Add(new OrderDetail());

            trackable.ShouldRaise("StatusChanged");
        }

        [TestMethod]
        public void When_CollectionProperty_Children_Trackable_Set_CollectionProperty_And_Change_Collection_Item_Property_Should_Raise_StatusChanged_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();
            trackable.OrderDetails = new List<OrderDetail> { new OrderDetail() };
            trackable.CastToIChangeTrackable().AcceptChanges();
            trackable.MonitorEvents();

            trackable.OrderDetails[0].OrderDetailId = 123;

            trackable.ShouldRaise("StatusChanged");
        }

        [TestMethod]
        public void When_ComplexProperty_Children_Trackable_Set_CoomplexProperty_And_Change_Property_Should_Raise_StatusChanged_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();

            trackable.Address = new Address();
            trackable.CastToIChangeTrackable().AcceptChanges();
            trackable.MonitorEvents();
            trackable.Address.AddressId = 123;

            trackable.ShouldRaise("StatusChanged");
        }
    }
}
