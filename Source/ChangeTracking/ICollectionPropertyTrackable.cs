using Castle.DynamicProxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ChangeTracking
{
    internal interface ICollectionPropertyTrackable
    {
        IEnumerable<object> CollectionPropertyTrackables { get; }
    }
}