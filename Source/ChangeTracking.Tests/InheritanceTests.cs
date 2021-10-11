using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace ChangeTracking.Tests
{
    public class InheritanceTests

    {
        public class Container
        {
            public virtual IList<Item> Items { get; set; } = new List<Item>();

            public void AddItem(Item item)
            {
                Items.Add(item);
            }
        }

        public  class Item
        {
            public Item()
            {                
            }

            public virtual int ValueInt { get; set; }
        }

        public class ItemDerived : Item
        {
            public ItemDerived() :
                base()
            {

            }
        }

        [Fact]
        public void ItemsAdd_DerivedCollectionItemClass_AddsItemOfDerivedCollectionItemClass()
        {
            Container container = new Container();

            var trackable = container.AsTrackable();

            trackable.Items.Add(new ItemDerived());

            trackable.CastToIChangeTrackable().AcceptChanges();

            Assert.True(container.Items[0] is ItemDerived);
        }

    }
}
