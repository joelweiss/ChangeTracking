using System.Collections.Generic;

namespace ChangeTracking.Tests
{
    public class InventoryUpdate
    {
        public InventoryUpdate()
        {
            UpdateInfos = new List<UpdateInfo>();
        }

        public virtual int InventoryUpdateId { get; set; }
        public virtual InventoryUpdate LinkedToInventoryUpdate { get; set; }
        public virtual InventoryUpdate LinkedInventoryUpdate { get; set; }

        public virtual IList<UpdateInfo> UpdateInfos { get; set; }
    }

    public class UpdateInfo
    {
        public virtual int UpdateInfoId { get; set; }
        public virtual InventoryUpdate InventoryUpdate { get; set; }
    }
}