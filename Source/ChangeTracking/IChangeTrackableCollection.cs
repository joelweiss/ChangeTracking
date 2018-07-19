using System.Collections.Generic;

namespace ChangeTracking
{
    public interface IChangeTrackableCollection<T> : System.ComponentModel.IRevertibleChangeTracking, IEnumerable<T>
    {
        IEnumerable<T> UnchangedItems { get; }        
        IEnumerable<T> AddedItems { get; }
        IEnumerable<T> ChangedItems { get; }
        IEnumerable<T> DeletedItems { get; }

        bool UnDelete(T item);
    }
}
