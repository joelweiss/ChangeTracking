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
    public class IChangeTrackableCollectionTests
    {
        [TestMethod]
        public void AsTrackable_On_Collection_Should_Make_Object_Implement_IChangeTrackableCollection()
        {
            var orders = Helper.GetOrdersIList();

            ICollection<Order> trackable = orders.AsTrackable();

            trackable.Should().BeAssignableTo<IChangeTrackableCollection<Order>>();
        }

        [TestMethod]
        public void When_AsTrackable_On_Collection_CastToIChangeTrackableCollection_Should_Not_Throw_InvalidCastException()
        {
            var orders = Helper.GetOrdersIList();

            IList<Order> trackable = orders.AsTrackable();

            trackable.Invoking(o => o.CastToIChangeTrackableCollection()).ShouldNotThrow<InvalidCastException>();
        }

        [TestMethod]
        public void When_Not_AsTrackable_On_Collection_CastToIChangeTrackableCollection_Should_Throw_InvalidCastException()
        {
            var orders = Helper.GetOrdersIList();

            orders.Invoking(o => o.CastToIChangeTrackableCollection()).ShouldThrow<InvalidCastException>();
        }

        [TestMethod]
        public void When_Calling_AsTrackable_On_Collection_Already_Tracking_Should_Throw()
        {
            var orders = Helper.GetOrdersIList();

            orders[0] = orders[0].AsTrackable();
            orders[0].CustumerNumber = "Test1";

            orders.Invoking(list => list.AsTrackable()).ShouldThrow<InvalidOperationException>();
        }

        [TestMethod]
        public void When_Calling_AsTrackable_On_Collection_All_Items_Should_Become_Trackable()
        {
            var orders = Helper.GetOrdersIList();

            orders.AsTrackable();

            orders.Should().ContainItemsAssignableTo<IChangeTrackable<Order>>();
        }

        [TestMethod]
        public void When_Adding_To_Colletion_Should_Be_IChangeTracableTrackable()
        {
            var orders = Helper.GetOrdersIList();

            var trackable = orders.AsTrackable();
            trackable.Add(new Order { Id = 999999999, CustumerNumber = "Custumer" });

            trackable.Single(o => o.Id == 999999999).Should().BeAssignableTo<IChangeTrackable<Order>>();
        }

        [TestMethod]
        public void When_Adding_To_Colletion_Status_Should_Be_Added()
        {
            var orders = Helper.GetOrdersIList();

            var trackable = orders.AsTrackable();
            trackable.Add(new Order { Id = 999999999, CustumerNumber = "Custumer" });

            trackable.Single(o => o.Id == 999999999).CastToIChangeTrackable().ChangeTrackingStatus.Should().Be(ChangeStatus.Added);
        }

        [TestMethod]
        public void When_Adding_To_Colletion_Via_Indexer_Status_Should_Be_Added()
        {
            IList<Order> list = new List<Order>();

            var trackable = list.AsTrackable();
            trackable.Add(new Order { Id = 999999999, CustumerNumber = "Custumer" });

            trackable.Single(o => o.Id == 999999999).CastToIChangeTrackable().ChangeTrackingStatus.Should().Be(ChangeStatus.Added);
        }

        [TestMethod]
        public void When_Deleting_From_Colletion_Status_Should_Be_Deleted()
        {
            var orders = Helper.GetOrdersIList();

            var trackable = orders.AsTrackable();
            var first = trackable.First();
            trackable.Remove(first);

            first.CastToIChangeTrackable().ChangeTrackingStatus.Should().Be(ChangeStatus.Deleted);
        }

        [TestMethod]
        public void When_Deleting_From_Colletion_Should_Be_Added_To_DeletedItems()
        {
            var orders = Helper.GetOrdersIList();

            var trackable = orders.AsTrackable();
            var first = trackable.First();
            trackable.Remove(first);

            trackable.CastToIChangeTrackableCollection().DeletedItems.Should().HaveCount(1)
                .And.OnlyContain(o => o.Id == first.Id && o.CustumerNumber == first.CustumerNumber);
        }

        [TestMethod]
        public void When_Deleting_From_Colletion_Item_That_Status_Is_Added_Should_Not_Be_Added_To_DeletedItems()
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
                .And.OnlyContain(o => o.Id == first.Id && o.CustumerNumber == first.CustumerNumber);
        }

        [TestMethod]
        public void When_Using_Not_On_List_Of_T_Or_Collection_Of_T_Should_Throw()
        {
            var orders = Helper.GetOrdersIList().ToArray();

            orders.Invoking(o => o.AsTrackable()).ShouldThrow<InvalidOperationException>();
        }

        [TestMethod]
        public void AsTrackable_On_Collection_Should_Make_It_IRevertibleChangeTracking()
        {
            var orders = Helper.GetOrdersIList();

            var trackable = orders.AsTrackable();

            trackable.Should().BeAssignableTo<System.ComponentModel.IRevertibleChangeTracking>();
        }

        [TestMethod]
        public void AcceptChanges_On_Collection_Should_All_Items_Status_Be_Unchanged()
        {
            var orders = Helper.GetOrdersIList();

            var trackable = orders.AsTrackable();
            var first = trackable.First();
            first.Id = 963;
            first.CustumerNumber = "Testing";
            var collectionintf = trackable.CastToIChangeTrackableCollection();
            collectionintf.AcceptChanges();

            trackable.All(o => o.CastToIChangeTrackable().ChangeTrackingStatus == ChangeStatus.Unchanged).Should().BeTrue();
        }

        [TestMethod]
        public void AcceptChanges_On_Collection_Should_AcceptChanges()
        {
            var orders = Helper.GetOrdersIList();

            var trackable = orders.AsTrackable();
            var first = trackable.First();
            first.Id = 963;
            first.CustumerNumber = "Testing";
            var itemIntf = first.CastToIChangeTrackable();
            var collectionintf = trackable.CastToIChangeTrackableCollection();
            int oldChangeStatusCount = collectionintf.ChangedItems.Count();
            collectionintf.AcceptChanges();

            itemIntf.GetOriginalValue(c => c.CustumerNumber).Should().Be("Testing");
            itemIntf.GetOriginalValue(c => c.Id).Should().Be(963);
            oldChangeStatusCount.Should().Be(1);
            collectionintf.ChangedItems.Count().Should().Be(0);
        }

        [TestMethod]
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

        [TestMethod]
        public void RejectChanges_On_Collection_Should_All_Items_Status_Be_Unchanged()
        {
            var orders = Helper.GetOrdersIList();
            var trackable = orders.AsTrackable();

            var first = trackable.First();
            first.Id = 963;
            first.CustumerNumber = "Testing";
            var intf = trackable.CastToIChangeTrackableCollection();
            var oldAnythingUnchanged = intf.ChangedItems.Any();
            intf.RejectChanges();

            oldAnythingUnchanged.Should().BeTrue();
            intf.ChangedItems.Count().Should().Be(0);
        }

        [TestMethod]
        public void RejectChanges_On_Collection_Should_RejectChanges()
        {
            var orders = Helper.GetOrdersIList();
            var trackable = orders.AsTrackable();

            var first = trackable.First();
            first.Id = 963;
            first.CustumerNumber = "Testing";
            var newOrder = Helper.GetOrder();
            newOrder.Id = 999;
            trackable.Add(newOrder);
            trackable.RemoveAt(5);
            var intf = trackable.CastToIChangeTrackableCollection();
            intf.RejectChanges();
            var ordersToMatch = Helper.GetOrdersIList().AsTrackable();

            intf.UnchangedItems.Should().Contain(i => ordersToMatch.SingleOrDefault(o =>
                o.Id == i.Id &&
                i.CustumerNumber == o.CustumerNumber &&
                i.CastToIChangeTrackable().ChangeTrackingStatus == o.CastToIChangeTrackable().ChangeTrackingStatus) != null);
            intf.UnchangedItems.Count().Should().Be(ordersToMatch.Count);
        }

        [TestMethod]
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

        [TestMethod]
        public void RejectChanges_On_Collection_Should_RejectChanges_Only_After_Last_AcceptChanges()
        {
            var orders = Helper.GetOrdersIList();
            var trackable = orders.AsTrackable();

            var first = orders.First();
            first.Id = 963;
            first.CustumerNumber = "Testing";
            var collectionIntf = trackable.CastToIChangeTrackableCollection();
            collectionIntf.AcceptChanges();
            first.Id = 999;
            first.CustumerNumber = "Testing 123";
            collectionIntf.RejectChanges();
            var intf = first.CastToIChangeTrackable();
            var orderToMatch = new Order { Id = 963, CustumerNumber = "Testing" };

            intf.GetOriginal().ShouldBeEquivalentTo(orderToMatch);
            intf.GetOriginalValue(o => o.Id).Should().Be(963);
            first.ShouldBeEquivalentTo(orderToMatch);
        }
    }
}
