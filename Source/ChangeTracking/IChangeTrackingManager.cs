using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChangeTracking
{
    internal interface IChangeTrackingManager<T> where T : class
    {
        T Delete();
    }
}