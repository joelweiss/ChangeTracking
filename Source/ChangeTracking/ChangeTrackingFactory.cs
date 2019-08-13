using ChangeTracking.Internal;
using System;
using System.Collections.Generic;

namespace ChangeTracking
{
    public class ChangeTrackingFactory : IChangeTrackingFactory
    {
        static ChangeTrackingFactory() => Default = new ChangeTrackingFactory();
        public static ChangeTrackingFactory Default { get; }

        public ChangeTrackingFactory() : this(new ChangeTrackingSettings(makeComplexPropertiesTrackable: true, makeCollectionPropertiesTrackable: false))
        {

        }

        public ChangeTrackingFactory(ChangeTrackingSettings changeTrackingDefaultSettings)
        {
            ChangeTrackingDefaultSettings = changeTrackingDefaultSettings;
        }

        public T AsTrackable<T>(T target, ChangeStatus status = ChangeStatus.Unchanged) where T : class
        {
            return AsTrackable(target, ChangeTrackingDefaultSettings, status);
        }

        public T AsTrackable<T>(T target, ChangeTrackingSettings changeTrackingSettings, ChangeStatus status = ChangeStatus.Unchanged) where T : class
        {
            return Core.AsTrackable(target, status, null, changeTrackingSettings, new Graph());
        }
        
        public ICollection<T> AsTrackableCollection<T>(ICollection<T> target) where T : class
        {
            return AsTrackableCollection(target, ChangeTrackingDefaultSettings);
        }
        
        public ICollection<T> AsTrackableCollection<T>(ICollection<T> target, ChangeTrackingSettings changeTrackingSettings) where T : class
        {
            return Core.AsTrackableCollection(target, changeTrackingSettings);
        }

        public ChangeTrackingSettings ChangeTrackingDefaultSettings { get; set; }
    }
}
