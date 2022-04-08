namespace ChangeTracking.Internal
{
    internal readonly struct ChangeTrackingSettings
    {
        internal ChangeTrackingSettings(bool makeComplexPropertiesTrackable, bool makeCollectionPropertiesTrackable, bool makeCollectionItemsInSourceAsProxies)
        {
            MakeComplexPropertiesTrackable = makeComplexPropertiesTrackable;
            MakeCollectionPropertiesTrackable = makeCollectionPropertiesTrackable;
            MakeCollectionItemsInSourceAsProxies = makeCollectionItemsInSourceAsProxies;
        }

        internal bool MakeComplexPropertiesTrackable { get; }
        internal bool MakeCollectionPropertiesTrackable { get; }
        internal bool MakeCollectionItemsInSourceAsProxies { get; }
    }
}
