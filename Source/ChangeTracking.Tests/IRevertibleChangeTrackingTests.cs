using FluentAssertions;
using Xunit;

namespace ChangeTracking.Tests
{
    public class IRevertibleChangeTrackingTests
    {
        [Fact]
        public void AsTrackable_Should_Make_Object_Implement_IRevertibleChangeTracking()
        {
            var order = Helper.GetOrder();

            Order trackable = order.AsTrackable();

            trackable.Should().BeAssignableTo<System.ComponentModel.IRevertibleChangeTracking>();
        }     
    }
}
