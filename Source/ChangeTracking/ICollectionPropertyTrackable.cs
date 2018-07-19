using System.Collections.Generic;

namespace ChangeTracking
{
    internal interface ICollectionPropertyTrackable
    {
        IEnumerable<object> CollectionPropertyTrackables { get; }
    }
}