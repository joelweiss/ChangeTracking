using System.Collections.Generic;
using System.Linq;

namespace ChangeTracking.Internal
{
    internal static class Utils
    {
        internal static IEnumerable<TResult> GetChildren<TResult>(object proxy, List<object> parents = null)
        {
            IEnumerable<object> children = ((ICollectionPropertyTrackable)proxy).CollectionPropertyTrackables
                .Concat(((IComplexPropertyTrackable)proxy).ComplexPropertyTrackables);
            List<TResult> result;
            if (parents is null)
            {
                result = children.OfType<TResult>().ToList();
            }
            else
            {
                result = children.Except(parents).OfType<TResult>().ToList();
                parents.AddRange(result.Cast<object>());
            }
            return result;
        }
    }
}
