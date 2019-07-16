using System;
using System.Threading;
using System.Threading.Tasks;
using ChangeTracking.Internal;
using FluentAssertions;
using Xunit;

namespace ChangeTracking.Tests
{
    public class GraphTests
    {
        [Fact]
        public async Task Add_To_Graph_OnOther_Thread_Should_Not_Throw()
        {
            Graph graph = new Graph();
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            _ = Task.Run(() =>
            {
                int i = 0;
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    graph.Add(new ProxyWeakTargetMap(i, i));
                    i++;
                }
            });

            try
            {
                for (int i = 0; i < 10; i++)
                {
                    graph.Invoking(g => g.GetExistingProxyForTarget(int.MaxValue)).Should().NotThrow<InvalidOperationException>();
                    await Task.Delay(i);
                }
            }
            finally
            {
                cancellationTokenSource.Cancel();
            }
        }
    }
}
