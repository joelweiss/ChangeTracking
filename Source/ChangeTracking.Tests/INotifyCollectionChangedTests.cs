using FluentAssertions;
using System.Collections.Generic;
using System.Collections.Specialized;
using Xunit;

namespace ChangeTracking.Tests
{
    public class INotifyCollectionChangedTests
    {
        [Fact]
        public void AsTrackable_On_Collection_Should_Make_It_INotifyCollectionChangedTests()
        {
            var orders = Helper.GetOrdersIList();

            var trackable = orders.AsTrackable();

            trackable.Should().BeAssignableTo<INotifyCollectionChanged>();
        }

        [Fact]
        public void AsTrackable_On_Collection_Add_Should_Raise_CollectionChanged()
        {
            IList<Order> orders = Helper.GetOrdersIList();

            IList<Order> trackable = orders.AsTrackable();
            INotifyCollectionChanged collection = (INotifyCollectionChanged)trackable;

            EventMonitor monitor = trackable.MonitorCollectionChangedChanged();
            trackable.Add(new Order());

            monitor.WasRaised.Should().BeTrue();
        }
    }
}
