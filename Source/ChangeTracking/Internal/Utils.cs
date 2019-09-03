using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ChangeTracking.Internal
{
    internal static class Utils
    {
        internal static IEnumerable<TResult> GetChildren<TResult>(object proxy, List<object> parents)
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

        internal static bool IsMarkedDoNotTrack(PropertyInfo propertyInfo)
        {
            Type doNoTrackAttribute = typeof(DoNoTrackAttribute);
            return propertyInfo.GetCustomAttribute(doNoTrackAttribute) != null
                ? true
                : IsMarkedDoNotTrack(propertyInfo.PropertyType);
        }

        internal static bool IsMarkedDoNotTrack(Type type)
        {
            Type doNoTrackAttribute = typeof(DoNoTrackAttribute);
            if (type.GetCustomAttribute(doNoTrackAttribute) != null)
            {
                return true;
            }
            if (type.IsInterface && type.GetGenericArguments().FirstOrDefault() is Type genericCollectionArgumentType && typeof(ICollection<>).MakeGenericType(genericCollectionArgumentType).IsAssignableFrom(type) && genericCollectionArgumentType.GetCustomAttribute(doNoTrackAttribute) != null)
            {
                return true;
            }

            return false;
        }
    }
}
