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
    }
}
