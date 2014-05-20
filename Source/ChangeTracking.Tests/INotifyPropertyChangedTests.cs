using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;

namespace ChangeTracking.Tests
{
    [TestClass]
    public class INotifyPropertyChangedTests
    {
        [TestMethod]
        public void AsTrackable_Should_Make_Object_Implement_INotifyPropertyChanged()
        {
            var order = Helper.GetOrder();

            Order trackable = order.AsTrackable();

            trackable.Should().BeAssignableTo<System.ComponentModel.INotifyPropertyChanged>();
        }

        [TestMethod]
        public void Change_Property_Should_Raise_PropertyChanged_Event()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();
            trackable.MonitorEvents();

            trackable.CustumerNumber = "Test1";

            trackable.ShouldRaise("PropertyChanged");
        }

        [TestMethod]
        public void When_Raised_Property_Should_Be_Changed()
        {
            var order = Helper.GetOrder();
            var trackable = order.AsTrackable();
            var inpc = (System.ComponentModel.INotifyPropertyChanged)trackable;
            int newValue = 0;
            inpc.PropertyChanged += (o, e) => newValue = order.Id;

            trackable.Id = 1234;

            newValue.Should().Be(1234);
        }
    }
}
