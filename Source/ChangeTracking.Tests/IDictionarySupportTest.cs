using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace ChangeTracking.Tests
{
    public class IDictionarySupportTest
    {
        public class Building : IDictionary<string, object>
        {
            [Ignore]
            protected virtual Dictionary<string, object> CustomProperties { get; } = new Dictionary<string, object>();
            
            public virtual string Name { get; set; }

            #region IDictionary

            public virtual object this[string key]
            {
                get
                {
                    object result;
                    if (CustomProperties.TryGetValue(key, out result))
                    {
                        return result;
                    }
                    return null;
                }
                set
                {
                    CustomProperties[key] = value;
                    Debug.WriteLine($"this[]: {this.GetType().FullName} : {this.GetHashCode()}");
                }
            }

            public virtual bool ContainsKey(string key)
            {
                return CustomProperties.ContainsKey(key);
            }

            public virtual void Add(string key, object value)
            {
                CustomProperties.Add(key, value);
            }

            public virtual bool Remove(string key)
            {
                return CustomProperties.Remove(key);
            }

            public virtual bool TryGetValue(string key, out object value)
            {
                return (CustomProperties.TryGetValue(key, out value));
            }

            [ChangeTracking.Ignore]
            public virtual ICollection<string> Keys => CustomProperties.Keys;

            [ChangeTracking.Ignore]
            public virtual ICollection<object> Values => CustomProperties.Values;

            public virtual IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            {
                return CustomProperties.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public virtual void Add(KeyValuePair<string, object> item)
            {
                CustomProperties.Add(item.Key, item.Value);
            }

            public virtual void Clear()
            {
                CustomProperties.Clear();
            }

            public virtual bool Contains(KeyValuePair<string, object> item)
            {
                return CustomProperties.Any(x => object.Equals(x, item));
            }

            public virtual void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
            {
                throw new NotImplementedException();
            }

            public virtual bool Remove(KeyValuePair<string, object> item)
            {
                return this.Remove(item.Key);
            }

            [ChangeTracking.Ignore]
            public virtual int Count => CustomProperties.Count;
            [ChangeTracking.Ignore]
            public virtual bool IsReadOnly => false;
            #endregion IDictionary
        }

        [Fact]
        public void CanTrackChanges()
        {
            // Arrange
            var b = new Building();
            var bTrackable = b.AsTrackable();

            // Act
            bTrackable.Name = "building01";
            bTrackable["hello"] = "world";

            // Assert
            var bChanges = bTrackable.CastToIChangeTrackable();
            Assert.True(bChanges.IsChanged);
        }

        [Fact]
        public void CanAcceptChanges()
        {
            // Arrange
            var b = new Building();
            b["hello"] = "sun";
            b.Name = "name";
            var bTrackable = b.AsTrackable();

            bTrackable["hello"] = "world";
            bTrackable.Name = "name 02";
            Assert.True(bTrackable.ContainsKey("hello"));
            var bChanges = bTrackable.CastToIChangeTrackable();
            
            // Act
            bChanges.AcceptChanges();

            // Assert
            var orig = bChanges.GetOriginal();
            Assert.Equal(b["hello"], orig["hello"]);
        }

        [Fact]
        public void CanRejectChanges()
        {
            // Arrange
            var b = new Building();
            b["hello"] = "sun";
            var bTrackable = b.AsTrackable();

            bTrackable["hello"] = "world";
            var bChanges = bTrackable.CastToIChangeTrackable();

            // Act
            bChanges.RejectChanges();

            // Assert
            var orig = bChanges.GetOriginal();
            Assert.Equal("sun", orig["hello"]);
        }
    }
}
