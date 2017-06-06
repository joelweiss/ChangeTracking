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

        public interface IFloor
        {
            IList<IDoor> Doors { get; set; }
        }

        public class Floor : IFloor
        {
            public virtual IList<IDoor> Doors { get; set; } = new List<IDoor>();
        }

        public interface IDoor
        {
            bool IsLocked { get; set; }
        }

        public class Door : IDoor
        {
            public bool IsLocked { get; set; }
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

            Assert.IsTrue(bTrackable.Floors[0] is IFloor);
            Assert.IsTrue(bTrackable.Floors[0] is Floor);
        }

        [TestMethod]
        public void CanHandleInterfaceLists_Nested()
        {
            // Arrange
            var b = new Building
            {
                Floors = new List<IFloor>
                {
                    new Floor
                    {
                        Doors = new List<IDoor>
                        {
                            new Door()
                            {
                                IsLocked = true,
                            }
                        }
                    }
                }
            };

            var t = b.AsTrackable();
            
            // Assert
            Assert.IsTrue(t is IChangeTrackable);
            Assert.IsTrue(t.Floors[0] is IChangeTrackable);
            Assert.IsTrue(t.Floors[0].Doors[0] is IChangeTrackable);
        }
    }
}
