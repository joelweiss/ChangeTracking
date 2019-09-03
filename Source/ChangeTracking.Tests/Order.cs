using System.Collections.Generic;
using System.Linq;

namespace ChangeTracking.Tests
{
    public class Order
    {
        public Order()
        {
            OrderDetails = new List<OrderDetail>();
            Leads = new List<Lead>();
        }

        public virtual int Id { get; set; }
        public virtual string CustomerNumber { get; set; }
        public virtual Address Address { get; set; }
        public virtual IList<OrderDetail> OrderDetails { get; set; }
        public virtual int OrderDetailsCount => OrderDetails != null ? OrderDetails.Count : 0;
        public virtual OrderDetail OrderDetail => OrderDetails?.FirstOrDefault(od => od.OrderDetailId == Id);
        public virtual Order LinkedToOrder { get; set; }
        public virtual Order LinkedOrder { get; set; }
        [DoNoTrack]
        public int LeadId { get; set; }
        [DoNoTrack]
        public virtual IList<OrderDetail> DoNotTrackOrderDetails { get; set; }
        [DoNoTrack]
        public virtual Address DoNotTrackAddress { get; set; }
        public virtual Lead Lead { get; set; }
        public virtual IList<Lead> Leads { get; set; }

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

        private string _Name;
        public virtual void SetNameVirtual(string name) => _Name = name;
        public virtual string GetNameVirtual() => _Name;
        public void SetNameNonVirtual(string name) => _Name = name;
        public string GetNameNonVirtual() => _Name;
    }
}
