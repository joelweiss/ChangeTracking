using System;

namespace ChangeTracking
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class DoNoTrackAttribute : Attribute
    {
    }
}
