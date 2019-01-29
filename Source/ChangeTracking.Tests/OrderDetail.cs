namespace ChangeTracking.Tests
{
    public class OrderDetail
    {
        public virtual int OrderDetailId { get; set; }
        public virtual string ItemNo { get; set; }

        public virtual Order Order { get; set; }
    }
}
