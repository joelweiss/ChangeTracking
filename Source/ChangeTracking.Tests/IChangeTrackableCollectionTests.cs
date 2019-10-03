using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace ChangeTracking.Tests
{
    public class IChangeTrackableCollectionTests
    {
        [Fact]
        public void AsTrackable_On_Collection_Should_Make_Object_Implement_IChangeTrackableCollection()
        {
            var orders = Helper.GetOrdersIList();

            ICollection<Order> trackable = orders.AsTrackable();

            trackable.Should().BeAssignableTo<IChangeTrackableCollection<Order>>();
        }

        [Fact]
        public void When_AsTrackable_On_Collection_CastToIChangeTrackableCollection_Should_Not_Throw_InvalidCastException()
        {
            var orders = Helper.GetOrdersIList();

            IList<Order> trackable = orders.AsTrackable();

            trackable.Invoking(o => o.CastToIChangeTrackableCollection()).Should().NotThrow<InvalidCastException>();
        }

        [Fact]
        public void When_Not_AsTrackable_On_Collection_CastToIChangeTrackableCollection_Should_Throw_InvalidCastException()
        {
            var orders = Helper.GetOrdersIList();

            orders.Invoking(o => o.CastToIChangeTrackableCollection()).Should().Throw<InvalidCastException>();
        }

        [Fact]
        public void When_Calling_AsTrackable_On_Collection_Already_Tracking_Should_Throw()
        {
            var orders = Helper.GetOrdersIList();

            orders[0] = orders[0].AsTrackable();
            orders[0].CustomerNumber = "Test1";

            orders.Invoking(list => list.AsTrackable()).Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void When_Calling_AsTrackable_On_Collection_All_Items_Should_Become_Trackable()
        {
            var orders = Helper.GetOrdersIList();

            orders.AsTrackable();

            orders.Should().ContainItemsAssignableTo<IChangeTrackable<Order>>();
        }

        [Fact]
        public void When_Adding_To_Collection_Should_Be_IChangeTracableTrackable()
        {
            var orders = Helper.GetOrdersIList();

            var trackable = orders.AsTrackable();
            trackable.Add(new Order { Id = 999999999, CustomerNumber = "Customer" });

            trackable.Single(o => o.Id == 999999999).Should().BeAssignableTo<IChangeTrackable<Order>>();
        }

        [Fact]
        public void When_Adding_To_Collection_Status_Should_Be_Added()
        {
            var orders = Helper.GetOrdersIList();

            var trackable = orders.AsTrackable();
            trackable.Add(new Order { Id = 999999999, CustomerNumber = "Customer" });

            trackable.Single(o => o.Id == 999999999).CastToIChangeTrackable().ChangeTrackingStatus.Should().Be(ChangeStatus.Added);
        }

        [Fact]
        public void When_Adding_To_Collection_Via_Indexer_Status_Should_Be_Added()
        {
            IList<Order> list = Helper.GetOrdersIList();

            var trackable = list.AsTrackable();
            trackable[0] = new Order { Id = 999999999, CustomerNumber = "Customer" };

            trackable.Single(o => o.Id == 999999999).CastToIChangeTrackable().ChangeTrackingStatus.Should().Be(ChangeStatus.Added);
        }

        [Fact]
        public void When_Deleting_From_Collection_Status_Should_Be_Deleted()
        {
            var orders = Helper.GetOrdersIList();

            var trackable = orders.AsTrackable();
            var first = trackable.First();
            trackable.Remove(first);

            first.CastToIChangeTrackable().ChangeTrackingStatus.Should().Be(ChangeStatus.Deleted);
        }

        [Fact]
        public void When_Deleting_From_Collection_Should_Be_Added_To_DeletedItems()
        {
            var orders = Helper.GetOrdersIList();

            var trackable = orders.AsTrackable();
            var first = trackable.First();
            trackable.Remove(first);

            trackable.CastToIChangeTrackableCollection().DeletedItems.Should().HaveCount(1)
                .And.OnlyContain(o => o.Id == first.Id && o.CustomerNumber == first.CustomerNumber);
        }

        [Fact]
        public void When_Deleting_From_Collection_Item_That_Status_Is_Added_Should_Not_Be_Added_To_DeletedItems()
        {
            var orders = Helper.GetOrdersIList();

            var trackable = orders.AsTrackable();
            var first = trackable.First();
            trackable.Remove(first);
            var order = Helper.GetOrder();
            order.Id = 999;
            trackable.Add(order);
            trackable.Remove(trackable.Single(o => o.Id == 999));

            trackable.CastToIChangeTrackableCollection().DeletedItems.Should().HaveCount(1)
                .And.OnlyContain(o => o.Id == first.Id && o.CustomerNumber == first.CustomerNumber);
        }

        [Fact]
        public void When_Using_Not_On_List_Of_T_Or_Collection_Of_T_Should_Throw()
        {
            var orders = Helper.GetOrdersIList().ToArray();

            orders.Invoking(o => o.AsTrackable()).Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void AsTrackable_On_Collection_Should_Make_It_IRevertibleChangeTracking()
        {
            var orders = Helper.GetOrdersIList();

            var trackable = orders.AsTrackable();

            trackable.Should().BeAssignableTo<System.ComponentModel.IRevertibleChangeTracking>();
        }

        [Fact]
        public void AcceptChanges_On_Collection_Should_All_Items_Status_Be_Unchanged()
        {
            var orders = Helper.GetOrdersIList();

            var trackable = orders.AsTrackable();
            var first = trackable.First();
            first.Id = 963;
            first.CustomerNumber = "Testing";
            var collectionintf = trackable.CastToIChangeTrackableCollection();
            collectionintf.AcceptChanges();

            trackable.All(o => o.CastToIChangeTrackable().ChangeTrackingStatus == ChangeStatus.Unchanged).Should().BeTrue();
        }

        [Fact]
        public void AcceptChanges_On_Collection_Should_AcceptChanges()
        {
            var orders = Helper.GetOrdersIList();

            var trackable = orders.AsTrackable();
            var first = trackable.First();
            first.Id = 963;
            first.CustomerNumber = "Testing";
            var itemIntf = first.CastToIChangeTrackable();
            var collectionintf = trackable.CastToIChangeTrackableCollection();
            int oldChangeStatusCount = collectionintf.ChangedItems.Count();
            collectionintf.AcceptChanges();

            itemIntf.GetOriginalValue(c => c.CustomerNumber).Should().Be("Testing");
            itemIntf.GetOriginalValue(c => c.Id).Should().Be(963);
            oldChangeStatusCount.Should().Be(1);
            collectionintf.ChangedItems.Count().Should().Be(0);
        }

        [Fact]
        public void AcceptChanges_On_Collection_Should_Clear_DeletedItems()
        {
            var orders = Helper.GetOrdersIList();

            var trackable = orders.AsTrackable();
            var first = trackable.First();
            trackable.Remove(first);
            var intf = trackable.CastToIChangeTrackableCollection();
            int oldDeleteStatusCount = intf.DeletedItems.Count();
            intf.AcceptChanges();


            oldDeleteStatusCount.Should().Be(1);
            intf.DeletedItems.Count().Should().Be(0);
            trackable.All(o => o.CastToIChangeTrackable().ChangeTrackingStatus == ChangeStatus.Unchanged).Should().BeTrue();
        }

        [Fact]
        public void RejectChanges_On_Collection_Should_All_Items_Status_Be_Unchanged()
        {
            var orders = Helper.GetOrdersIList();
            var trackable = orders.AsTrackable();

            var first = trackable.First();
            first.Id = 963;
            first.CustomerNumber = "Testing";
            var intf = trackable.CastToIChangeTrackableCollection();
            var oldAnythingUnchanged = intf.ChangedItems.Any();
            intf.RejectChanges();

            oldAnythingUnchanged.Should().BeTrue();
            intf.ChangedItems.Count().Should().Be(0);
        }

        [Fact]
        public void RejectChanges_On_Collection_Should_RejectChanges()
        {
            var orders = Helper.GetOrdersIList();
            var trackable = orders.AsTrackable();

            var first = trackable.First();
            first.Id = 963;
            first.CustomerNumber = "Testing";
            var newOrder = Helper.GetOrder();
            newOrder.Id = 999;
            trackable.Add(newOrder);
            trackable.RemoveAt(5);
            var intf = trackable.CastToIChangeTrackableCollection();
            intf.RejectChanges();
            var ordersToMatch = Helper.GetOrdersIList().AsTrackable();

            intf.UnchangedItems.Should().Contain(i => ordersToMatch.SingleOrDefault(o =>
                o.Id == i.Id &&
                i.CustomerNumber == o.CustomerNumber &&
                i.CastToIChangeTrackable().ChangeTrackingStatus == o.CastToIChangeTrackable().ChangeTrackingStatus) != null);
            intf.UnchangedItems.Count().Should().Be(ordersToMatch.Count);
            intf.UnchangedItems.Count().Should().Be(intf.Count());
        }

        [Fact]
        public void RejectChanges_On_Collection_Should_Move_DeletedItems_Back_To_Unchanged()
        {
            var orders = Helper.GetOrdersIList();
            var trackable = orders.AsTrackable();

            var first = orders.First();
            trackable.Remove(first);
            var intf = trackable.CastToIChangeTrackableCollection();
            intf.RejectChanges();

            trackable.Count.Should().Be(10);
            intf.UnchangedItems.Count().Should().Be(10);
        }

        [Fact]
        public void RejectChanges_On_Collection_Should_RejectChanges_Only_After_Last_AcceptChanges()
        {
            var orders = Helper.GetOrdersIList();
            var trackable = orders.AsTrackable();

            var first = orders.First();
            first.Id = 963;
            first.CustomerNumber = "Testing";
            var collectionIntf = trackable.CastToIChangeTrackableCollection();
            collectionIntf.AcceptChanges();
            first.Id = 999;
            first.CustomerNumber = "Testing 123";
            collectionIntf.RejectChanges();
            var intf = first.CastToIChangeTrackable();
            var orderToMatch = Helper.GetOrder();
            orderToMatch.Id = 963;
            orderToMatch.CustomerNumber = "Testing";
            Order originalOrder = intf.GetOriginal();

            originalOrder.Should().BeEquivalentTo(orderToMatch, options => options.IgnoringCyclicReferences());
            intf.GetOriginalValue(o => o.Id).Should().Be(963);
        }

        [Fact]
        public void UnDelete_Should_Move_Back_Item_From_DeletedItems_And_Change_Back_Status()
        {
            var orders = Helper.GetOrdersIList();
            var trackable = orders.AsTrackable();

            Order first = trackable.First();
            trackable.Remove(first);
            trackable.CastToIChangeTrackableCollection().UnDelete(first);

            trackable.Should().Contain(first);
            trackable.CastToIChangeTrackableCollection().DeletedItems.Should().NotContain(first).And.BeEmpty();
            first.CastToIChangeTrackable().ChangeTrackingStatus.Should().Be(ChangeStatus.Unchanged);
        }

        [Fact]
        public void Can_Enumerate_IChangeTrackableCollection()
        {
            var orders = Helper.GetOrdersIList();
            var trackable = orders.AsTrackable();

            trackable.Invoking(t => t.FirstOrDefault()).Should().NotThrow();
        }

        [Fact]
        public void AsTrackable_On_ICollection_Should_Convert_ToIList_Internally()
        {
            IList<Order> orders = Helper.GetOrdersIList();
            ICollection<Order> ordersSet = new System.Collections.ObjectModel.Collection<Order>(orders);

            ICollection<Order> trackable = ordersSet.AsTrackable();

            trackable.Should().BeAssignableTo<IChangeTrackableCollection<Order>>();
        }

        [Fact]
        public void AsTrackable_Should_Take_MakeCollectionPropertiesTrackable_From_Default()
        {
            ChangeTrackingFactory.Default.MakeCollectionPropertiesTrackable = false;
            Order order = Helper.GetOrder();

            Order trackable = order.AsTrackable();

            trackable.Should().BeAssignableTo<IChangeTrackable<Order>>();
            trackable.OrderDetails.Should().NotBeAssignableTo<IChangeTrackableCollection<OrderDetail>>();
            ChangeTrackingFactory.Default.MakeCollectionPropertiesTrackable = true;
        }

        [Fact]
        public void AsTrackable_Should_Take_MakeComplexPropertiesTrackable_From_Default()
        {
            ChangeTrackingFactory.Default.MakeComplexPropertiesTrackable = false;
            Order order = Helper.GetOrder();

            Order trackable = order.AsTrackable();

            trackable.Should().BeAssignableTo<IChangeTrackable<Order>>();
            trackable.Address.Should().NotBeAssignableTo<IChangeTrackable<Address>>();
            ChangeTrackingFactory.Default.MakeComplexPropertiesTrackable = true;
        }
               
        [Fact]
        public void When_Clear_Collection_Should_Work()
        {
            IList<Order> orders = Helper.GetOrdersIList();
            IList<Order> trackable = orders.AsTrackable();

            trackable.Clear();

            IChangeTrackableCollection<Order> trackableCollection = trackable.CastToIChangeTrackableCollection();
            trackableCollection.IsChanged.Should().BeTrue();
            trackableCollection.DeletedItems.Should().NotBeEmpty();
            trackableCollection.UnchangedItems.Should().BeEmpty();
        }
    }
}
