using System.Collections.Generic;

namespace ChangeTracking
{
    public interface ICollectionPropertyTrackable
    {
        /// <summary>
        /// Gets the list of all trackable collection properties.
        /// </summary>
        IEnumerable<object> CollectionPropertyTrackables { get; }
    }
}
