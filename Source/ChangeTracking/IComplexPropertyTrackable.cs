using System.Collections.Generic;

namespace ChangeTracking
{
    internal interface IComplexPropertyTrackable
    {
        IEnumerable<object> ComplexPropertyTrackables { get; }
    }
}
