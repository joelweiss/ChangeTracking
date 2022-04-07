using ChangeTracking.Internal;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ChangeTracking
{
    public static class Core
    {
        public static T AsTrackable<T>(this T target) where T : class
        {
            return ChangeTrackingFactory.Default.AsTrackable(target);
        }

        public static T AsTrackable<T>(this T target, ChangeStatus status = ChangeStatus.Unchanged, bool makeComplexPropertiesTrackable = true, bool makeCollectionPropertiesTrackable = true) where T : class
        {
            return ChangeTrackingFactory.Default.AsTrackable(target, new ChangeTrackingSettings(makeComplexPropertiesTrackable, makeCollectionPropertiesTrackable), status);
        }

        public static ICollection<T> AsTrackable<T>(this Collection<T> target) where T : class
        {
            return ChangeTrackingFactory.Default.AsTrackableCollection(target);
        }

        public static ICollection<T> AsTrackable<T>(this Collection<T> target, bool makeComplexPropertiesTrackable = true, bool makeCollectionPropertiesTrackable = true) where T : class
        {
            return ChangeTrackingFactory.Default.AsTrackableCollection(target, new ChangeTrackingSettings(makeComplexPropertiesTrackable, makeCollectionPropertiesTrackable));
        }

        public static ICollection<T> AsTrackable<T>(this ICollection<T> target) where T : class
        {
            return ChangeTrackingFactory.Default.AsTrackableCollection(target);
        }

        public static ICollection<T> AsTrackable<T>(this ICollection<T> target, bool makeComplexPropertiesTrackable, bool makeCollectionPropertiesTrackable) where T : class
        {
            return ChangeTrackingFactory.Default.AsTrackableCollection(target, new ChangeTrackingSettings(makeComplexPropertiesTrackable, makeCollectionPropertiesTrackable));
        }

        public static IList<T> AsTrackable<T>(this List<T> target) where T : class
        {
            return (IList<T>)ChangeTrackingFactory.Default.AsTrackableCollection(target);
        }

        public static IList<T> AsTrackable<T>(this List<T> target, bool makeComplexPropertiesTrackable, bool makeCollectionPropertiesTrackable) where T : class
        {
            return (IList<T>)ChangeTrackingFactory.Default.AsTrackableCollection(target, new ChangeTrackingSettings(makeComplexPropertiesTrackable, makeCollectionPropertiesTrackable));
        }

        public static IList<T> AsTrackable<T>(this IList<T> target) where T : class
        {
            return (IList<T>)ChangeTrackingFactory.Default.AsTrackableCollection(target);
        }

        public static IList<T> AsTrackable<T>(this IList<T> target, bool makeComplexPropertiesTrackable, bool makeCollectionPropertiesTrackable) where T : class
        {
            return (IList<T>)ChangeTrackingFactory.Default.AsTrackableCollection(target, new ChangeTrackingSettings(makeComplexPropertiesTrackable, makeCollectionPropertiesTrackable));
        }
    }
}
