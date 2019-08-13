using System.Collections.Generic;

namespace ChangeTracking
{
    public interface IChangeTrackingFactory
    {
        T AsTrackable<T>(T target, ChangeStatus status = ChangeStatus.Unchanged) where T : class;
        T AsTrackable<T>(T target, ChangeTrackingSettings changeTrackingSettings, ChangeStatus status = ChangeStatus.Unchanged) where T : class;
        ICollection<T> AsTrackableCollection<T>(ICollection<T> target) where T : class;
        ICollection<T> AsTrackableCollection<T>(ICollection<T> target, ChangeTrackingSettings changeTrackingSettings) where T : class;
        ChangeTrackingSettings ChangeTrackingDefaultSettings { get; }
    }
}
