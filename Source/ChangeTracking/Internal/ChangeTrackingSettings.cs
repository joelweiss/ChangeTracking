namespace ChangeTracking.Internal
{
    internal readonly struct ChangeTrackingSettings
    {
        internal ChangeTrackingSettings(bool makeComplexPropertiesTrackable, bool makeCollectionPropertiesTrackable)
        {
            MakeComplexPropertiesTrackable = makeComplexPropertiesTrackable;
            MakeCollectionPropertiesTrackable = makeCollectionPropertiesTrackable;
        }

        internal bool MakeComplexPropertiesTrackable { get; }
        internal bool MakeCollectionPropertiesTrackable { get; }
    }
}
