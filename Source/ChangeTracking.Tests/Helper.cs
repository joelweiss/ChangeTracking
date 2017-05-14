using System.Collections.Generic;
using System.Linq;

namespace ChangeTracking.Tests
{
    internal static class Helper
    {
        internal static Order GetOrder(int id = 1, string custumerNumber = "Test")
        {
            return new Order
            {
                Id = 1,
                CustomerNumber = "Test",
                Address = new Address
                {
                    AddressId = 1,
                    City = "New York"
                },
                OrderDetails = new List<OrderDetail>
                {
                    new OrderDetail
                    {
                        OrderDetailId = 1,
                        ItemNo = "Item123"
                    },
                    new OrderDetail
                    {
                        OrderDetailId = 2,
                        ItemNo = "Item369"
                    }
                }
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
