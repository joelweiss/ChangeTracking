using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeTracking.Tests
{
    public class OrderDetail
    {
        public virtual int OrderDetailId { get; set; }
        public virtual string ItemNo { get; set; }
    }
}
