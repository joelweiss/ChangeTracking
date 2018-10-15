namespace ChangeTracking.Tests
{
    public class Address
    {
        public virtual int AddressId { get; set; }
        public virtual string City { get; set; }
        public virtual string Zip => "12345";
    }
}
