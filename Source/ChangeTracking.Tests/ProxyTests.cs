using System.Collections.Generic;
using Xunit;

namespace ChangeTracking.Tests
{
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

        [Fact]
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
            Assert.True(t is IChangeTrackable);
            Assert.True(t.Floors is IChangeTrackableCollection<IFloor>);
            var tf = t.Floors[0] as IFloor;
            var tb = t.Floors[1] as IBasement;
            Assert.True(tf is IChangeTrackable);
            Assert.True(tb is IChangeTrackable);
            Assert.True(tb.Compartments is IChangeTrackableCollection<ICompartment>);
            Assert.True(tb.Compartments[0] is IChangeTrackable);
            Assert.True(tb.Compartments[1] is IChangeTrackable);
            Assert.True(tb.Compartments[2] is IChangeTrackable);
        }

        [Fact]
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
            Assert.True(c.IsChanged);
        }

        [Fact]
        public void TracksChangesInDerivedInterface_CanCastToIChangeTrackable()
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
            var t = b.AsTrackable();
            var basement = (Basement)t.Floors[1];
            var compartment = (Compartment)basement.Compartments[0];

            // Act
            var tb = basement.CastToIChangeTrackable();
            var tc = compartment.CastToIChangeTrackable();
            basement.Compartments[0].Name = "noodle";

            // Assert
            Assert.True(tc.IsChanged);
        }
    }
}
