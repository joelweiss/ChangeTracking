
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeTracking.Tests
{
    internal static class Helper
    {
        internal static Order GetOrder(int id = 1, string custumerNumber = "Test")
        {
            return new Order
            {
                Id = 1,
                CustomerNumber = "Test"
            };
        }

        internal static IList<Order> GetOrdersIList()
        {
            return Enumerable.Range(0, 10).Select(i =>
            {
                var order = GetOrder();
                order.Id = i;
                return order;
            }).ToList();
        }
    }
}
