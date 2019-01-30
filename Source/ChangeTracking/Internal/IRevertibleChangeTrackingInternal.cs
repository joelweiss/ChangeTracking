using System.Collections.Generic;

namespace ChangeTracking.Internal
{
    internal interface IRevertibleChangeTrackingInternal
    {
        void AcceptChanges(List<object> parents);
        void RejectChanges(List<object> parents);
        bool IsChanged(List<object> parents);
    }
}
