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
    public class IRevertibleChangeTrackingTests
    {
        [TestMethod]
        public void AsTrackable_Should_Make_Object_Implement_IRevertibleChangeTracking()
        {
            var order = Helper.GetOrder();

            Order trackable = order.AsTrackable();

            trackable.Should().BeAssignableTo<System.ComponentModel.IRevertibleChangeTracking>();
        }     
    }
}
