using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChangeTracking.Tests
{
    [TestClass]
    public class ProxyTests
    {
        public interface IFloor
        {
            string Name { get; set; }
        }

        public interface IBasement : IFloor
        {
            IList<ICompartment> Compartments { get; set; }
        }

        public interface ICompartment
        {
            string Name { get; set; }
        }

        public interface IBuilding
        {
            IList<IFloor> Floors { get; set; }
        }

        public class Building : IBuilding
        {
            public virtual IList<IFloor> Floors { get; set; } = new List<IFloor>();
        }
        
        public class Floor : IFloor {
            public virtual string Name { get; set; }
        }
        
        public class Basement : Floor, IBasement {
            public virtual IList<ICompartment> Compartments { get; set; } = new List<ICompartment>();
        }

        public class Compartment : ICompartment
        {
            public virtual string Name { get; set; }
        }

        [TestMethod]
        public void TracksDerivedInterfaces()
        {
            // Arrange
            var b = new Building
            {
                Floors = new List<IFloor>
                {
                    new Floor {Name = "First floor"},
                    new Basement
                    {
                        Name = "basement",
                        Compartments = new List<ICompartment>
                        {
                            new Compartment{Name = "1A"},
                            new Compartment{Name = "2B"},
                            new Compartment{Name = "3C"},
                        }
                    }
                }
            };

            // Act
            var t = b.AsTrackable();

            // Assert
            Assert.IsTrue(t is IChangeTrackable);
            Assert.IsTrue(t.Floors is IChangeTrackableCollection<IFloor>);
            var tf = t.Floors[0] as IFloor;
            var tb = t.Floors[1] as IBasement;
            Assert.IsTrue(tf is IChangeTrackable);
            Assert.IsTrue(tb is IChangeTrackable);
            Assert.IsTrue(tb.Compartments is IChangeTrackableCollection<ICompartment>);
            Assert.IsTrue(tb.Compartments[0] is IChangeTrackable);
            Assert.IsTrue(tb.Compartments[1] is IChangeTrackable);
            Assert.IsTrue(tb.Compartments[2] is IChangeTrackable);
        }



        [TestMethod]
        public void TracksChangesInDerivedInterface()
        {
            // Arrange
            var b = new Building
            {
                Floors = new List<IFloor>
                {
                    new Floor {Name = "First floor"},
                    new Basement
                    {
                        Name = "basement",
                        Compartments = new List<ICompartment>
                        {
                            new Compartment{Name = "1A"},
                            new Compartment{Name = "2B"},
                            new Compartment{Name = "3C"},
                        }
                    }
                }
            };

            // Act
            var t = b.AsTrackable();

            var basement = (IBasement)t.Floors[1];
            basement.Compartments[0].Name = "11111";

            // Assert
            var c = t.CastToIChangeTrackable();
            Assert.IsTrue(c.IsChanged);
        }
    }
}
