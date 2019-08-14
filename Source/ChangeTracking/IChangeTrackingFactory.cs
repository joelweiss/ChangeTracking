using System.Collections.Generic;

namespace ChangeTracking
{
    public interface IChangeTrackingFactory
    {
        T AsTrackable<T>(T target, ChangeStatus status = ChangeStatus.Unchanged) where T : class;
        ICollection<T> AsTrackableCollection<T>(ICollection<T> target) where T : class;
        bool MakeComplexPropertiesTrackable { get; set; }
        bool MakeCollectionPropertiesTrackable { get; set; }
    }
}
