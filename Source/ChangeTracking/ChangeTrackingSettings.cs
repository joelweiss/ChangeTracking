namespace ChangeTracking
{
    public class ChangeTrackingSettings
    {
        public ChangeTrackingSettings(bool makeComplexPropertiesTrackable, bool makeCollectionPropertiesTrackable)
        {
            MakeComplexPropertiesTrackable = makeComplexPropertiesTrackable;
            MakeCollectionPropertiesTrackable = makeCollectionPropertiesTrackable;
        }
        
        public bool MakeComplexPropertiesTrackable { get; }
        public bool MakeCollectionPropertiesTrackable { get; }
    }
}
