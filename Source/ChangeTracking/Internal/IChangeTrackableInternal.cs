namespace ChangeTracking.Internal
{
    internal interface IChangeTrackableInternal
    {
        object GetOriginal(UnrollGraph unrollGraph);
        object GetCurrent(UnrollGraph unrollGraph);
    }
}
