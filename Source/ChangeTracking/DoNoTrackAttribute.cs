using System;

namespace ChangeTracking
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    sealed class DoNoTrackAttribute : Attribute
    {
    }
}
