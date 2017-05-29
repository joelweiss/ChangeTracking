using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChangeTracking.Tests
{
    [TestClass]
    public class InterfaceListsTest
    {
        public class Building
        {
            public virtual IList<IFloor> Floors { get; set; } = new List<IFloor>();
        }

        public interface IFloor { }

        public class Floor : IFloor
        {
        }

        [TestMethod]
        public void CanHandleInterfaceLists()
        {
            // Arrange
            var b = new Building();
            var bTrackable = b.AsTrackable();

            Exception exception = null;
            try
            {
                // Act
                bTrackable.Floors.Add(new Floor());
            }
            catch (Exception x)
            {
                exception = x;
            }

            // Assert
            Assert.IsNull(exception, "collections of interfaces are not supported?");

            var bChanges = bTrackable.CastToIChangeTrackable();
            Assert.IsTrue(bChanges.IsChanged);
        }
    }
}
