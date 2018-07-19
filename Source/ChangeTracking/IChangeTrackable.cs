using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;

namespace ChangeTracking
{
    public interface IChangeTrackable<T> : IChangeTrackable
    {
        /// <summary>
        /// Gets the original value of a given property.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="selector">The selector.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">
        /// A property selector expression has an incorrect format
        /// or
        /// A selected member is not a property
        /// </exception>
        TResult GetOriginalValue<TResult>(Expression<Func<T, TResult>> selector);

        /// <summary>
        /// Gets the original.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.MissingMethodException">The type that is specified for T does not have a parameterless constructor.</exception>
        T GetOriginal();

        /// <summary>
        /// Gets the current object unproxied.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.MissingMethodException">The type that is specified for T does not have a parameterless constructor.</exception>
        T GetCurrent();
    }

    public interface IChangeTrackable : INotifyPropertyChanged, IChangeTracking, IRevertibleChangeTracking, IEditableObject
    {
        event EventHandler<StatusChangedEventArgs> StatusChanged;

        ChangeStatus ChangeTrackingStatus { get; }

        IEnumerable<string> ChangedProperties { get; }

        /// <summary>
        /// Gets the original value of a given property.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns></returns>
        object GetOriginalValue(string propertyName);

        /// <summary>
        /// Gets the original value of a given property.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns></returns>
        TResult GetOriginalValue<TResult>(string propertyName);
    }

    internal interface IChangeTrackableInternal
    {
        object GetOriginal();
        object GetCurrent();
    }
}
