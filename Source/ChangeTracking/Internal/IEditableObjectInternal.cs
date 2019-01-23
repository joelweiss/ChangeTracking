using System.Collections.Generic;
using System.ComponentModel;

namespace ChangeTracking.Internal
{
    internal interface IEditableObjectInternal : IEditableObject
    {
        void BeginEdit(List<object> parents);
        void CancelEdit(List<object> parents);
        void EndEdit(List<object> parents);
    }
}