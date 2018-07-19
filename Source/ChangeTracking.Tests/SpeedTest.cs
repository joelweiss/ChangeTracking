using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace ChangeTracking.Tests
{
    public class SpeedTest
    {
        private readonly ITestOutputHelper _Output;

        public SpeedTest(ITestOutputHelper output)
        {
            _Output = output;
        }

        [Fact]
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
                        CustomerNumber = "Test"
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
            _Output.WriteLine("Call AsTrackable on {0:N0} objects: {1} ms", reps, swAsTrackable.ElapsedMilliseconds);

            var noneTrackedList = lists[1];
            var swNotTracked = new Stopwatch();
            GC.Collect();
            swNotTracked.Start();
            for (int i = 0; i < noneTrackedList.Count; i++)
            {
                var order = noneTrackedList[i];
                order.Id = 2;
                order.CustomerNumber = "Test2";
                var id = order.Id;
                var cust = order.CustomerNumber;
            }
            swNotTracked.Stop();
            _Output.WriteLine("Write and Read {0:N0} none tracked objects: {1} ms", reps, swNotTracked.ElapsedMilliseconds);
            GC.Collect();
            var swTracked = new Stopwatch();
            swTracked.Start();
            for (int i = 0; i < trackedList.Count; i++)
            {
                var order = trackedList[i];
                order.Id = 2;
                order.CustomerNumber = "Test2";
                var id = order.Id;
                var cust = order.CustomerNumber;
            }
            swTracked.Stop();
            _Output.WriteLine("Write and Read {0:N0} tracked objects: {1} ms", reps, swTracked.ElapsedMilliseconds);

            GC.Collect();
            var swGetOriginal = new Stopwatch();
            swGetOriginal.Start();
            for (int i = 0; i < trackedList.Count; i++)
            {
                var original = ((IChangeTrackableInternal)trackedList[i]).GetOriginal();
            }
            swGetOriginal.Stop();
            _Output.WriteLine("Call GetOriginal on {0:N0} tracked objects: {1} ms", reps, swGetOriginal.ElapsedMilliseconds);

            var timeNeeded = swAsTrackable.ElapsedMilliseconds + swNotTracked.ElapsedMilliseconds + swTracked.ElapsedMilliseconds + swGetOriginal.ElapsedMilliseconds;
            _Output.WriteLine("Finished for {0:N0} tracked objects in {1} ms", reps, timeNeeded);
        }
    }
}
