namespace ChangeTracking
{
    internal interface IChangeTrackingManager
    {
        bool Delete();
        bool UnDelete();
        void UpdateStatus();
        void SetAdded();
    }
}