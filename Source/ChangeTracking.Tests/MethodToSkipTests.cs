using FluentAssertions;
using Xunit;

namespace ChangeTracking.Tests
{
    public class MethodToSkipTests
    {
        [Fact]
        public void SkipMethods()
        {
            ChangeTrackingFactory.Default.MethodsToSkip.Remove(nameof(Equals));
            ChangeTrackingFactory.Default.MethodsToSkip.Remove(nameof(GetHashCode));

            var order = Helper.GetOrder();
            Order trackable = order.AsTrackable();
            trackable.Should().Be(order);
            trackable.GetHashCode().Should().Be(order.GetHashCode());
        }
    }
}