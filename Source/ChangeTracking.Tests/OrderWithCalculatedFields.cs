using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeTracking.Tests
{
    public class OrderWithCalculatedFields
    {
        public virtual int Id { get; set; }
        public virtual string CustomerNumber { get; set; }
        public virtual Address Address { get; set; }
        public virtual IList<OrderDetail> OrderDetails { get; set; }
        public virtual string FormattedId
        {
            get { return CustomerNumber != null ? CustomerNumber + "-" + Id.ToString() : null; }
        }

        public Order CreateOrder()
        {
            return new Order();
        }
    }
}
