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

        public virtual void VirtualModifier()
        {
            CustomerNumber = "ChangedInVirtualModifier";
        }

        public void NonVirtualModifier()
        {
            CustomerNumber = "ChangedInNonVirtualModifier";
        }

        private string _name;
        public virtual void SetName(string name)
        {
            _name = name;
        }

        public virtual string GetName()
        {
            return _name;
        }
    }
}
