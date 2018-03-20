using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace ChangeTracking
{
    public static class Extensions
    {
        public static IChangeTrackable<T> CastToIChangeTrackable<T>(this T target) where T : class
        {
            return (IChangeTrackable<T>)target;
        }

        public static IChangeTrackableCollection<T> CastToIChangeTrackableCollection<T>(this ICollection<T> target) where T : class
        {
            return (IChangeTrackableCollection<T>)target;
        }

        public static IChangeTrackableCollection<T> CastToIChangeTrackableCollection<T>(this IList<T> target) where T : class
        {
            return (IChangeTrackableCollection<T>)target;
        }

        public static IChangeTrackableCollection<T> CastToIChangeTrackableCollection<T>(this IList target) where T : class
        {
            return (IChangeTrackableCollection<T>)target;
        }

        public static IChangeTrackableCollection<T> CastToIChangeTrackableCollection<T>(this IBindingList target) where T : class
        {
            return (IChangeTrackableCollection<T>)target;
        }
    }
}
