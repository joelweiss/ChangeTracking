using ChangeTracking.Internal;
using System.Collections.Generic;

namespace ChangeTracking
{
    public class ChangeTrackingFactory : IChangeTrackingFactory
    {
        static ChangeTrackingFactory() => Default = new ChangeTrackingFactory();
        public static ChangeTrackingFactory Default { get; }

        public ChangeTrackingFactory() : this(makeComplexPropertiesTrackable: true, makeCollectionPropertiesTrackable: true)
        {

        }

        public ChangeTrackingFactory(bool makeComplexPropertiesTrackable, bool makeCollectionPropertiesTrackable)
        {
            MakeComplexPropertiesTrackable = makeComplexPropertiesTrackable;
            MakeCollectionPropertiesTrackable = makeCollectionPropertiesTrackable;
        }

        public T AsTrackable<T>(T target, ChangeStatus status = ChangeStatus.Unchanged) where T : class
        {
            return AsTrackable(target, new ChangeTrackingSettings(MakeComplexPropertiesTrackable, MakeCollectionPropertiesTrackable), status);
        }

        internal T AsTrackable<T>(T target, ChangeTrackingSettings changeTrackingSettings, ChangeStatus status = ChangeStatus.Unchanged) where T : class
        {
            return Core.AsTrackable(target, status, null, changeTrackingSettings, new Graph());
        }
        
        public ICollection<T> AsTrackableCollection<T>(ICollection<T> target) where T : class
        {
            return AsTrackableCollection(target, new ChangeTrackingSettings(MakeComplexPropertiesTrackable, MakeCollectionPropertiesTrackable));
        }
        
        internal ICollection<T> AsTrackableCollection<T>(ICollection<T> target, ChangeTrackingSettings changeTrackingSettings) where T : class
        {
            return Core.AsTrackableCollection(target, changeTrackingSettings);
        }

        public bool MakeComplexPropertiesTrackable { get; set; }
        public bool MakeCollectionPropertiesTrackable { get; set; }
    }
}
