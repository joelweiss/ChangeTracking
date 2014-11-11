using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace ChangeTracking
{
    public interface IChangeTrackable<T> : IChangeTrackable, System.ComponentModel.IRevertibleChangeTracking
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
        /// Gets the original value of a given property.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns></returns>
        TResult GetOriginalValue<TResult>(string propertyName);

        /// <summary>
        /// Gets the original value of a given property.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns></returns>
        object GetOriginalValue(string propertyName);

        /// <summary>
        /// Gets the original.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.MissingMethodException">The type that is specified for T does not have a parameterless constructor.</exception>
        T GetOriginal();
    }

    public interface IChangeTrackable
    {
        event EventHandler StatusChanged;
        ChangeStatus ChangeTrackingStatus { get; }
    }

    public interface IChangeTrackableInternal
    {
        object GetOriginal();
    }
}
