using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChangeTracking.Tests
{
    public class Order
    {
        public virtual int Id { get; set; }
        public virtual string CustomerNumber { get; set; }
        public virtual Address  Address { get; set; }
        public virtual IList<OrderDetail> OrderDetails { get; set; }
        public virtual int OrderDetailsCount
        {
            get
            {
                return OrderDetails!=null ? OrderDetails.Count : 0;
            }
        }

        public Order CreateOrder()
        {
            return new Order();
        }
    }
}
