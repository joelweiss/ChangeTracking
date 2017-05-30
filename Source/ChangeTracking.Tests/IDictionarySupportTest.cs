using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChangeTracking.Tests
{
    [TestClass]
    public class IDictionarySupportTest
    {
        public class Building : IDictionary<string, object>
        {
            private readonly Dictionary<string, object> _customProperties = new Dictionary<string, object>();
            
            public virtual string Name { get; set; }

            #region IDictionary

            public virtual object this[string key]
            {
                get
                {
                    object result;
                    if (_customProperties.TryGetValue(key, out result))
                    {
                        return result;
                    }
                    return null;
                }
                set
                {
                    _customProperties[key] = value;
                    Debug.WriteLine($"this[]: {this.GetType().FullName} : {this.GetHashCode()}");
                }
            }

            public virtual bool ContainsKey(string key)
            {
                return _customProperties.ContainsKey(key);
            }

            public virtual void Add(string key, object value)
            {
                _customProperties.Add(key, value);
            }

            public virtual bool Remove(string key)
            {
                return _customProperties.Remove(key);
            }

            public virtual bool TryGetValue(string key, out object value)
            {
                return (_customProperties.TryGetValue(key, out value));
            }

            [ChangeTracking.Ignore]
            public virtual ICollection<string> Keys => _customProperties.Keys;

            [ChangeTracking.Ignore]
            public virtual ICollection<object> Values => _customProperties.Values;

            public virtual IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            {
                return _customProperties.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public virtual void Add(KeyValuePair<string, object> item)
            {
                _customProperties.Add(item.Key, item.Value);
            }

            public virtual void Clear()
            {
                _customProperties.Clear();
            }

            public virtual bool Contains(KeyValuePair<string, object> item)
            {
                return _customProperties.Any(x => object.Equals(x, item));
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
            public virtual int Count => _customProperties.Count;
            [ChangeTracking.Ignore]
            public virtual bool IsReadOnly => false;
            #endregion IDictionary
        }

        [TestMethod]
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
            Assert.IsTrue(bChanges.IsChanged);
        }

        [TestMethod]
        public void CanAcceptChanges()
        {
            // Arrange
            var b = new Building();
            b["hello"] = "sun";
            b.Name = "name";
            var bTrackable = b.AsTrackable();

            bTrackable["hello"] = "world";
            bTrackable.Name = "name 02";
            Assert.IsTrue(bTrackable.ContainsKey("hello"));
            var bChanges = bTrackable.CastToIChangeTrackable();
            
            // Act
            bChanges.AcceptChanges();

            // Assert
            var orig = bChanges.GetOriginal();
            Assert.AreEqual(b["hello"], orig["hello"]);
        }

        [TestMethod]
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
            Assert.AreEqual("sun", orig["hello"]);
        }
    }
}
