using System;
using System.Collections.Generic;
using Xunit;

namespace ChangeTracking.Tests
{
    public class IgnoreAttributeTests
    {
        [Fact]
        public void CanIgnoreChanges()
        {
            // Arrange
            var b = new Building();
            var bTrackable = b.AsTrackable();

            // Act
            bTrackable.Address = "duck street";

            // Assert
            var bChanges = bTrackable.CastToIChangeTrackable();
            Assert.False(bChanges.IsChanged);

            Assert.Equal("duck street", bTrackable.Address);
            var orig = bChanges.GetOriginal();
            Assert.Equal("duck street", orig.Address);
        }

        [Fact]
        public void CanIgnoreChangesOfComplexProperty()
        {
            // Arrange
            var b = new Building02();
            var bTrackable = b.AsTrackable();

            // Act
            bTrackable.Address = new Address02{Street = "duck street", ZipCode = 999};

            // Assert
            var bChanges = bTrackable.CastToIChangeTrackable();
            Assert.False(bChanges.IsChanged);

            var a = bTrackable.Address;
            Assert.NotNull(a);
            Assert.Equal("duck street", a.Street);
            Assert.Equal(999, a.ZipCode);

            var orig = bChanges.GetOriginal();
            a = orig.Address;
            Assert.NotNull(a);
            Assert.Equal("duck street", a.Street);
            Assert.Equal(999, a.ZipCode);
        }

        [Fact]
        public void CanIgnoreChangesOfCollectionProperty()
        {
            // Arrange
            var b = new Building03();
            var bTrackable = b.AsTrackable();

            // Act
            bTrackable.Floors.Add(new Floor03{ Name = "first floor"});

            // Assert
            var bChanges = bTrackable.CastToIChangeTrackable();
            Assert.False(bChanges.IsChanged);
            var bOrig = bChanges.GetOriginal();

            var floor = bTrackable.Floors[0];
            Assert.NotNull(floor);
            Assert.Equal("first floor", floor.Name);

            floor = bOrig.Floors[0];
            Assert.NotNull(floor);
            Assert.Equal("first floor", floor.Name);
        }

        [Fact]
        public void WhenPropertiesAreNonVirtual_ButIgnored_DoesNotThrow()
        {
            // Arrange
            var b = new Building04();
            Building04 proxy = null;

            Exception exception = null;
            try
            {
                // Act
                proxy = b.AsTrackable();
                proxy.Name = "hans";
            }
            catch (Exception x)
            {
                exception = x;
            }

            // Assert
            Assert.Null(exception);
            var trackable = proxy.CastToIChangeTrackable();
            Assert.True(trackable.IsChanged);
        }

        public class Building
        {
            [ChangeTracking.Ignore]
            public virtual string Address { get; set; }
        }

        public class Building02
        {
            [ChangeTracking.Ignore]
            public virtual Address02 Address { get; set; }
        }
        public class Address02
        {
            public virtual string Street { get; set; }
            public virtual int ZipCode { get; set; }
        }

        public class Building03
        {
            [ChangeTracking.Ignore]
            public virtual IList<Floor03> Floors { get; set; } = new List<Floor03>();
        }
        public class Floor03
        {
            public virtual string Name { get; set; }
        }

        public class Building04
        {
            public virtual string Name { get; set; }
            [ChangeTracking.Ignore]
            public string Owner { get; set; }
            [ChangeTracking.Ignore]
            public Address02 Address { get; set; }
            [ChangeTracking.Ignore]
            public IList<Floor03> Floors { get; set; } = new List<Floor03>();
        }
    }
}
