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
            string Name { get; set; }
            bool IsLocked { get; set; }
        }

        public class Door : IDoor
        {
            public virtual string Name { get; set; }
            public virtual bool IsLocked { get; set; }
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

        [TestMethod]
        public void CanHandleInterfaceLists_WhenAddingProxy()
        {
            // Arrange
            var b = new Building
            {
                Floors = new List<IFloor>
                {
                    
                }
            };
            var bTrackable = b.AsTrackable();
            var f = new Floor();
            var fTrackable = f.AsTrackable();

            // Act
            bTrackable.Floors.Add(fTrackable);

            // Assert
        }

        [TestMethod]
        public void CanGetCurrentModel_Unproxied_WithoutHavingToAcceptChanges()
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
            var bTrackable = b.AsTrackable();
            bTrackable.Floors[0].Doors[0].IsLocked = false;

            // Act
            var b2 = bTrackable.CastToIChangeTrackable().GetCurrent();

            // Assert
            Assert.IsFalse(bTrackable.Floors[0].Doors[0].IsLocked);
            Assert.AreEqual(bTrackable.Floors[0].Doors[0].IsLocked, b2.Floors[0].Doors[0].IsLocked);
        }

        [TestMethod]
        public void CanGetCurrentModel_Unproxied_WithoutHavingToAcceptChanges_WhenEditingChildCollection()
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
                                Name = "1A",
                                IsLocked = true,
                            },
                            new Door()
                            {
                                Name = "1B",
                                IsLocked = false,
                            }
                        }
                    }
                }
            };
            var bTrackable = b.AsTrackable();
            bTrackable.Floors[0].Doors[0].IsLocked = false;
            bTrackable.Floors[0].Doors.RemoveAt(1);
            var fTrackable = new Floor().AsTrackable();
            bTrackable.Floors.Add(fTrackable);
            var dTrackable = new Door().AsTrackable();
            dTrackable.Name = "2A";
            dTrackable.IsLocked = true;
            fTrackable.Doors.Add(dTrackable);
            
            // Act
            var b2 = bTrackable.CastToIChangeTrackable().GetCurrent();

            // Assert
            Assert.AreEqual(2, b2.Floors.Count);

            var f1 = b2.Floors[0];
            Assert.AreEqual("1A",f1.Doors[0].Name);
            Assert.IsFalse(f1.Doors[0].IsLocked);
            Assert.AreEqual(1, f1.Doors.Count);

            var f2 = b2.Floors[1];
            Assert.AreEqual("2A", f2.Doors[0].Name);
            Assert.IsTrue(f2.Doors[0].IsLocked);
            Assert.AreEqual(1, f2.Doors.Count);
        }
    }
}
