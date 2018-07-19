[![Build status](https://ci.appveyor.com/api/projects/status/n9j44hcpe2wmkkgd/branch/master?svg=true)](https://ci.appveyor.com/project/joelweiss/changetracking/branch/master)
[![NuGet Badge](https://buildstats.info/nuget/ChangeTracking?includePreReleases=true)](https://www.nuget.org/packages/ChangeTracking/)
[![Test status](https://img.shields.io/appveyor/tests/JoelWeiss/ChangeTracking.svg)](https://ci.appveyor.com/project/JoelWeiss/ChangeTracking/branch/master)

# ChangeTracking

Track changes in your POCO objects, and in your collections.
By using [Castle Dynamic Proxy](http://www.castleproject.org/projects/dynamicproxy/) to create proxies of your classes at runtime, you can use your objects just like you used to, and just by calling the `AsTrackable()` extension method, you get automatic change tracking, and cancellation.

All trackable POCOs implement [`IChangeTrackable<T>`](https://github.com/joelweiss/ChangeTracking/blob/master/Source/ChangeTracking/IChangeTrackable.cs), [`IRevertibleChangeTracking`](http://msdn.microsoft.com/en-us/library/vstudio/system.componentmodel.irevertiblechangetracking.aspx), [`IChangeTracking`](http://msdn.microsoft.com/en-us/library/vstudio/system.componentmodel.ichangetracking.aspx), [`IEditableObject`](http://msdn.microsoft.com/en-us/library/system.componentmodel.ieditableobject.aspx) and [`INotifyPropertyChanged`](http://msdn.microsoft.com/en-us/library/system.componentmodel.inotifypropertychanged.aspx).

And all trackable collections implement [`IChangeTrackableCollection<T>`](https://github.com/joelweiss/ChangeTracking/blob/master/Source/ChangeTracking/IChangeTrackableCollection.cs), [`IBindingList`](http://msdn.microsoft.com/en-us/library/vstudio/system.componentmodel.ibindinglist.aspx) [`ICancelAddNew`](http://msdn.microsoft.com/en-us/library/vstudio/system.componentmodel.icanceladdnew.aspx), [`INotifyCollectionChanged `](https://msdn.microsoft.com/en-us/library/system.collections.specialized.inotifycollectionchanged(v=vs.110).aspx), `IList<T>`, `IList`, `ICollection<T>`, `ICollection`, `IEnumerable<T>` and `IEnumerable`

# Installation
```
PM> Install-Package ChangeTracking
```

Example
---------

### To make an object trackable
```csharp
using ChangeTracking;
//...
Order order = new Order 
{ 
    Id = 1,
    CustumerNumber = "Test",
    Address = new Address
    {
        AddressId = 1,
        City = "New York"
    },
    OrderDetails = new List<OrderDetail>
    {
        new OrderDetail
        {
            OrderDetailId = 1,
            ItemNo = "Item123"
        },
        new OrderDetail
        {
            OrderDetailId = 2,
            ItemNo = "Item369"
        }
    }
};
Order trackedOrder = order.AsTrackable();
```
And here is how you get to the tracked info.
```csharp
var trackable = (IChangeTrackable<Order>)trackedOrder;
// same as
var trackable = trackedOrder.CastToIChangeTrackable();
```
And here is what's available on `trackable`.
```csharp
//Can be Unchanged, Added, Changed, Deleted
ChangeStatus status = trackable.ChangeTrackingStatus;

//Will be true if ChangeTrackingStatus is not Unchanged
bool isChanged = trackable.IsChanged;

//Will retrieve the original value of a property
string originalCustNumber = trackable.GetOriginalValue(o => o.CustumerNumber);

//Will retrieve a copy of the original item
var originalOrder = trackable.GetOriginal();

//Calling RejectChanges will reject all the changes you made, reset all properties to their original values and set ChangeTrackingStatus to Unchanged
trackable.RejectChanges();

//Calling AcceptChanges will accept all the changes you made, clears the original values and set ChangeTrackingStatus to Unchanged
trackable.AcceptChanges();

//If ChangeTrackingStatus is Changed it returns all changed property names, if ChangeTrackingStatus is Added or Deleted it returns all properties
trackable.ChangedProperties();
```
By default complex properties and collection properties will be tracked (if it can be made trackable) as well.
if you do not wish to track them you can set it when creating the trackable.
```csharp
var trackable = order.AsTrackable(makeComplexPropertiesTrackable: false);
```
or
```csharp
var trackable = order.AsTrackable(makeCollectionPropertiesTrackable: false);
```
### And on a collection
```csharp
var orders = new List<Order>{new Order { Id = 1, CustumerNumber = "Test" } };
IList<Order> trackableOrders = orders.AsTrackable();
```
And here is how you get to the tracked info.
```csharp
var trackable = (IChangeTrackableCollection<Order>)trackableOrders;
// Same as
var trackable = trackableOrders.CastToIChangeTrackableCollection();
```
And here is what's available on `trackable`.
```csharp
// Will be true if there are any changed items, added items or deleted items in the collection.
bool isChanged = trackable.IsChanged;

// Will return all items with ChangeTrackingStatus of Unchanged
IEnumerable<Order> unchangedOrders = trackable.UnchangedItems;
// Will return all items that were added to the collection - with ChangeTrackingStatus of Added
IEnumerable<Order> addedOrders = trackable.AddedItems;
// Will return all items with ChangeTrackingStatus of Changed
IEnumerable<Order> changedOrders = trackable.ChangedItems;
// Will return all items that were removed from the collection - with ChangeTrackingStatus of Deleted
IEnumerable<Order> deletedOrders = trackable.DeletedItems;

// Will Accept all the changes in the collection and its items, deleted items will be cleared and all items ChangeTrackingStatus will be Unchanged
trackable.AcceptChanges();

// Will Reject all the changes in the collection and its items, deleted items will be moved back to the collection, added items removed and all items ChangeTrackingStatus will be Unchanged
trackable.RejectChanges();
```
Requirements and restrictions
--------------------------------

* .net 4.5.2 and above
* netstandard 2.0

##### For Plain objects
* Your class must not be `sealed` and all members in your class must be `public virtual`

    ```csharp
    public class Order
    {
        public virtual int Id { get; set; }
        public virtual string CustumerNumber { get; set; }
        public virtual Address  Address { get; set; }
        public virtual IList<OrderDetail> OrderDetails { get; set; }
    }
    ```

##### For Collections 
* You can only assign the created proxy to one of the implemented interfaces, i.e. `ICollection<T>`, `IList<T>` and `IBindingList`, and the `AsTrackable<T>()` will choose the correct extennsion method only if called on `IList<T>`, `IList`, `ICollection<T>` and `ICollection`.

    ```csharp
    IList<Order> orders = new List<Order>().AsTrackable();
    ```
