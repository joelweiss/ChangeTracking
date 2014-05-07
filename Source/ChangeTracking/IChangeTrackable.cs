using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace ChangeTracking
{
    public interface IChangeTrackable<T>
    {
        event EventHandler StatusChanged;
        ChangeStatus ChangeTrackingStatus { get; }

        /// <summary>
        /// Gets the original value of a given property.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="target">The target.</param>
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
        /// <param name="target">The target.</param>
        /// <returns></returns>
        /// <exception cref="System.MissingMethodException">The type that is specified for T does not have a parameterless constructor.</exception>
        T GetOriginal();

        /// <summary>
        /// Accepts all changes the changes, and markses object as Unchanged.
        /// </summary>
        void AcceptChanges();

        /// <summary>
        /// Reject all changes made (only rejects changes made after last AcceptChanges or RejectChanges), and markes object as Unchanged.
        /// </summary>
        void RejectChanges();
    }
}
