using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ChangeTracking;
using System.Collections.Generic;
using System.Diagnostics;

namespace ChangeTracking.Tests
{
    [TestClass]
    public class SpeedTest
    {
        public TestContext TestContext { get; set; }
        
        [TestMethod]
        public void TestSpeed()
        {
            int reps = 100000;
            var lists = new[] { new List<Order>(reps), new List<Order>(reps) };
            for (int i = 0; i < 2; i++)
            {
                var list = lists[i];
                for (int j = 0; j < reps; j++)
                {
                    list.Add(new Order
                    {
                        Id = 1,
                        CustumerNumber = "Test"
                    });
                }
            }
            var trackedList = lists[0];
            var swAsTrackable = new Stopwatch();
            swAsTrackable.Start();
            for (int i = 0; i < trackedList.Count; i++)
            {
                trackedList[i] = trackedList[i].AsTrackable();
            }
            swAsTrackable.Stop();
            TestContext.WriteLine("AsTrackable: {0}", swAsTrackable.ElapsedMilliseconds);

            var noneTrackedList = lists[1];
            var swNotTracked = new Stopwatch();
            GC.Collect();
            swNotTracked.Start();
            for (int i = 0; i < noneTrackedList.Count; i++)
            {
                var order = noneTrackedList[i];
                order.Id = 2;
                order.CustumerNumber = "Test2";
                var id = order.Id;
                var cust = order.CustumerNumber;
            }
            swNotTracked.Stop();
            TestContext.WriteLine("None tracked objects: {0}", swNotTracked.ElapsedMilliseconds);
            GC.Collect();
            var swTracked = new Stopwatch();
            swTracked.Start();
            for (int i = 0; i < trackedList.Count; i++)
            {
                var order = trackedList[i];
                order.Id = 2;
                order.CustumerNumber = "Test2";
                var id = order.Id;
                var cust = order.CustumerNumber;
            }
            swTracked.Stop();
            TestContext.WriteLine("Tracked objects: {0}", swTracked.ElapsedMilliseconds);
        }
    }
}
