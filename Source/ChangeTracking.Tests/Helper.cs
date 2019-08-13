using System.Collections.Generic;
using System.Linq;

namespace ChangeTracking.Tests
{
    internal static class Helper
    {
        internal static Order GetOrder(int? orderId = null)
        {
            int id = orderId ?? 1;
            Order order = new Order
            {
                Id = id,
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
                },
                DoNotTrackOrderDetails = new List<OrderDetail>
                {
                    new OrderDetail
                    {
                        OrderDetailId = 20,
                        ItemNo = "Item20"
                    }
                },
                LeadId = id + 500,
                Lead = new Lead
                {
                    LeadId = id + 600
                },
                Leads = new List<Lead>
                {
                    new Lead
                    {
                        LeadId = id + 700
                    }
                },
                DoNotTrackAddress = new Address
                {
                    AddressId = id + 800,
                    City = "London"
                }
            };
            Order linkedOrder = new Order
            {
                Id = id + 1000,
                LinkedToOrder = order
            };
            order.LinkedOrder = linkedOrder;

            foreach (OrderDetail orderDetail in order.OrderDetails)
            {
                orderDetail.Order = order;
            }
            return order;
        }

        internal static IList<Order> GetOrdersIList() => Enumerable.Range(1, 10).Select(i => GetOrder(i)).ToList();
    }
}
