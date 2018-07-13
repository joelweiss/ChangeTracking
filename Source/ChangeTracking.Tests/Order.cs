using System.Collections.Generic;

namespace ChangeTracking.Tests
{
    public class Order
    {
        public Order()
        {
            OrderDetails = new List<OrderDetail>();
        }

        public virtual int Id { get; set; }
        public virtual string CustomerNumber { get; set; }
        public virtual Address Address { get; set; }
        public virtual IList<OrderDetail> OrderDetails { get; set; }
        public virtual int OrderDetailsCount => OrderDetails != null ? OrderDetails.Count : 0;

        public Order CreateOrder()
        {
            return new Order();
        }
    }
}
