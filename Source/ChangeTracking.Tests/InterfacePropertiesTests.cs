using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChangeTracking.Tests
{
    [TestClass]
    public class InterfacePropertiesTest
    {
        public class Building
        {
            public virtual IAddress Address { get; set; }
        }

        public interface IAddress
        {
            string Street { get; set; }
            IZipCode ZipCode { get; set; }
        }

        public class Address : IAddress
        {
            public virtual string Street { get; set; }
            public virtual IZipCode ZipCode { get; set; }
        }

        public interface IZipCode
        {
            int Code { get; set; }
        }

        public class ZipCode : IZipCode
        {
            public virtual int Code { get; set; }
        }

        [TestMethod]
        public void CanDetectChangesOfInterfaceProperty()
        {
            // Arrange
            var b = new Building()
            {
                Address = new Address()
            };
            var bTrackable = b.AsTrackable();

            // Act
            bTrackable.Address.Street = "Duckstreet";

            // Assert
            var bChanges = bTrackable.CastToIChangeTrackable();
            Assert.IsTrue(bChanges.IsChanged);
        }

        [TestMethod]
        public void CanDetectChangesOfInterfaceProperty_InDeepHierarchy()
        {
            // Arrange
            var b = new Building();
            var bTrackable = b.AsTrackable();
            bTrackable.Address = new Address() { Street = "Duckstreet 33", ZipCode = new ZipCode()};
            bTrackable.CastToIChangeTrackable().AcceptChanges();

            Exception exception = null;
            try
            {
                // Act
                bTrackable.Address.ZipCode.Code = 1232;
            }
            catch (Exception x)
            {
                exception = x;
            }

            // Assert
            Assert.IsNull(exception, "interface properties are not supported?");
            var bChanges = bTrackable.CastToIChangeTrackable();
            var aChanges = bTrackable.Address.CastToIChangeTrackable();
            var zChanges = bTrackable.Address.ZipCode.CastToIChangeTrackable();
            Assert.IsTrue(bChanges.IsChanged);
            Assert.IsTrue(aChanges.IsChanged);
            Assert.IsTrue(zChanges.IsChanged);
        }
    }
}
