using FluentAssertions;
using Xunit;

namespace ChangeTracking.Tests
{
    public class DoNoTrackTests
    {
        
        [Fact]
        public void AsTrackable_Should_Not_Track_Properties_That_Are_Marked_DoNotTrack_And_Be_Ok_That_It_Is_Not_Virtual()
        {
            var order = Helper.GetOrder();

            Order trackable = order.AsTrackable();
            trackable.LeadId = 123;

            trackable.CastToIChangeTrackable().ChangeTrackingStatus.Should().Be(ChangeStatus.Unchanged);
            trackable.CastToIChangeTrackable().ChangedProperties.Should().NotContain(nameof(Order.LeadId));
        }
        
        [Fact]
        public void AsTrackable_Should_Not_Track_ComplexProperties_That_The_PropertyType_Is_Marked_DoNotTrack()
        {
            var order = Helper.GetOrder();

            Order trackable = order.AsTrackable();

            trackable.Lead.Should().NotBeAssignableTo<IChangeTrackable<Lead>>();
        }

        [Fact]
        public void AsTrackable_Should_Not_Track_ComplexProperties_That_Are_Marked_DoNotTrack()
        {
            var order = Helper.GetOrder();

            Order trackable = order.AsTrackable();

            trackable.DoNotTrackAddress.Should().NotBeAssignableTo<IChangeTrackable<Address>>();
        }
        
        [Fact]
        public void AsTrackable_Should_Not_Track_CollectionProperties_That_The_PropertyType_Is_Marked_DoNotTrack()
        {
            var order = Helper.GetOrder();

            Order trackable = order.AsTrackable();

            trackable.Leads.Should().NotBeAssignableTo<IChangeTrackableCollection<Lead>>();
        }

        [Fact]
        public void AsTrackable_Should_Not_Track_CollectionProperties_That_Are_Marked_DoNotTrack()
        {
            var order = Helper.GetOrder();

            Order trackable = order.AsTrackable();
            trackable.LeadId = 123;

            trackable.CastToIChangeTrackable().ChangeTrackingStatus.Should().Be(ChangeStatus.Unchanged);
            trackable.CastToIChangeTrackable().ChangedProperties.Should().NotContain(nameof(Order.LeadId));
        }
        
        [Fact]
        public void AsTrackable_Properties_That_Are_DoNotTrack_Should_Still_Be_Copied_To_Trackable()
        {
            var order = Helper.GetOrder();

            Order trackable = order.AsTrackable();

            trackable.LeadId.Should().Be(order.LeadId);
            trackable.Lead.Should().Be(order.Lead);
            trackable.DoNotTrackAddress.Should().Be(order.DoNotTrackAddress);
            trackable.DoNotTrackOrderDetails.Should().BeEquivalentTo(order.DoNotTrackOrderDetails);
            trackable.Leads.Should().BeEquivalentTo(order.Leads);
        }
    }
}