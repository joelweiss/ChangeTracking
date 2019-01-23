namespace ChangeTracking.Tests
{
    public class InventoryUpdate
    {
        public virtual int InventoryUpdateId { get; set; }
        public virtual InventoryUpdate LinkedToInventoryUpdate { get; set; }
        public virtual InventoryUpdate LinkedInventoryUpdate { get; set; }
    }
}