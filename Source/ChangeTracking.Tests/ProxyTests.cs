using FluentAssertions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xunit;

namespace ChangeTracking.Tests
{
    public class ProxyTests
    {
        [Fact]
        public void Change_Property_Should_Raise_PropertyChanged_Event_if_non_virtual_method()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();
            ((INotifyPropertyChanged)trackable).MonitorEvents();

            trackable.NonVirtualModifier();

            trackable.ShouldRaisePropertyChangeFor(o => o.CustomerNumber);
        }

        [Fact]
        public void Change_Property_Should_Raise_PropertyChanged_Event_if_method_virtual()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();
            ((INotifyPropertyChanged)trackable).MonitorEvents();

            trackable.VirtualModifier();

            trackable.ShouldRaisePropertyChangeFor(o => o.CustomerNumber);
        }

        [Fact]
        public void Set_On_Field_Virtual_Should_Return_Correct_Value()
        {
            var order = Helper.GetOrder();
            order.SetNameVirtual("MyName");
            var trackable = order.AsTrackable();

            var name = trackable.GetNameVirtual();

            name.Should().Be("MyName");
        }

        [Fact]
        public void Set_On_Field_Virtual_When_In_Collection_Should_Return_Correct_Value()
        {
            var order = Helper.GetOrder();
            order.SetNameVirtual("MyName");
            var list = new List<Order> { order };
            var trackableList = list.AsTrackable();

            var name = trackableList[0].GetNameVirtual();

            name.Should().Be("MyName");
        }


        [Fact]
        public void Set_On_Field_NonVirtual_Should_Return_Correct_Value()
        {
            var order = Helper.GetOrder();
            order.SetNameNonVirtual("MyName");
            var trackable = order.AsTrackable();

            var name = trackable.GetNameNonVirtual();

            name.Should().Be("MyName");
        }

        [Fact]
        public void Set_On_Field_NonVirtual_When_In_Collection_Should_Return_Correct_Value()
        {
            var order = Helper.GetOrder();
            order.SetNameNonVirtual("MyName");
            var list = new List<Order> { order };
            var trackableList = list.AsTrackable();

            var name = trackableList[0].GetNameNonVirtual();

            name.Should().Be("MyName");
        }

        [Fact]
        public void Fields_Should_Copy_Over()
        {
            Game game = new Game("ReadOnly")
            {
                Property = "Test",
                _Int = 333,
                _String = "Testing",
                _ListInt = new List<int> { 1, 2, 3, 4 },
                _CustomStruct = new CustomStruct
                {
                    _Int = 30,
                    _String = "StructString"
                }
            };

            Game trackableGame = game.AsTrackable();            

            trackableGame._ReadOnly.Should().Be("ReadOnly");
            trackableGame.Property.Should().Be("Test");
            trackableGame._Int.Should().Be(333);
            trackableGame._String.Should().Be("Testing");
            trackableGame._ListInt.Should().BeEquivalentTo(new List<int> { 1, 2, 3, 4 });
            trackableGame._CustomStruct.Should().Be(new CustomStruct
            {
                _Int = 30,
                _String = "StructString"
            });
        }

        [Fact]
        public void Complex_PropertyWith_Field_Should_Copy_Over()
        {
            Game game = new Game("ReadOnly")
            {
                Player = new Player()
            };
            game.Player.SetName("PlayerName");

            Game trackableGame = game.AsTrackable();

            trackableGame.Player.GetName().Should().Be("PlayerName");
        }

        [Fact]
        public void Events_Should_Copy_Over()
        {
            Game game = new Game("ReadOnly");
            bool raised = false;
            game.OnClicked += (o, ef) => raised = true;

            Game trackableGame = game.AsTrackable();

            trackableGame.Raise();

            raised.Should().BeTrue();
        }

        public class Game
        {
            public Game(string readOnly) => _ReadOnly = readOnly;
            public Game() { }

            public virtual string Property { get; set; }
            public virtual Player Player { get; set; }
            public readonly string _ReadOnly;
            public string _String;
            public int _Int;
            public List<int> _ListInt;
            public CustomStruct _CustomStruct;
            public event EventHandler OnClicked;

            public void Raise() => OnClicked?.Invoke(this, EventArgs.Empty);
        }

        public struct CustomStruct
        {
            public int _Int;
            public string _String;
        }

        public class Player
        {
            // this property is here to make the class proxyable
            public virtual int UserId { get; set; }
            private string _Name;
            public virtual void SetName(string name) => _Name = name;
            public virtual string GetName() => _Name;
        }
    }
}
