using FluentAssertions;
using System.Collections.Generic;
using Xunit;

namespace ChangeTracking.Tests
{
    public class InternalTests
    {
        public class Home
        {
            internal Home()
            {
                Town = new Town
                {
                    Name = "Prague"
                };
                Rooms = new List<Room>
                {
                    new Room
                    {
                       Name = "Dining"
                    }
                };
            }

            public virtual ICollection<Room> Rooms { get; set; }
            public virtual Town Town { get; set; }
        }

        public class Room
        {
            internal Room() { }
            public virtual string Name { get; set; }
        }

        public class Town
        {
            internal Town() { }
            public virtual string Name { get; set; }
        }

        [Fact]
        public void GetOriginal_OnInternal_Should_Not_Throw()
        {
            Home home = new Home();

            Home trackable = home.AsTrackable();

            trackable.CastToIChangeTrackable().Invoking(t => t.GetOriginal()).Should().NotThrow();
        }

        [Fact]
        public void GetCurrent_OnInternal_Should_Not_Throw()
        {
            Home home = new Home();

            Home trackable = home.AsTrackable();

            trackable.CastToIChangeTrackable().Invoking(t => t.GetCurrent()).Should().NotThrow();
        }

        [Fact]
        public void Internal_ComplexProperty_Should_Be_Trackable()
        {
            Home home = new Home();

            Home trackable = home.AsTrackable();

            trackable.Town.Should().BeAssignableTo<IChangeTrackable<Town>>();
        }

        [Fact]
        public void BindingList_AddNew_With_Items_With_Internal_Constructor_Should_Not_Throw()
        {
            Home home = new Home();

            Home trackable = home.AsTrackable();
            var bindingList = (System.ComponentModel.IBindingList)trackable.Rooms;
            
            bindingList.Invoking(bl => bl.AddNew()).Should().NotThrow();
        }
    }
}