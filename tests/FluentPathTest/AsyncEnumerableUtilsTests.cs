// Copyright © 2021 Bertrand Le Roy.  All Rights Reserved.
// This code released under the terms of the 
// MIT License http://opensource.org/licenses/MIT

using Fluent.Utils;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FluentPathTest
{
    public class AsyncEnumerableUtilsTests
    {
        [Fact]
        public async Task CanEnumerateSynchronousEnumerableAsynchronously()
        {
            var result = new List<string>();
            await foreach (string s in TestEnumerable.ToAsyncEnumerable())
            {
                result.Add(s);
            }
            Assert.Equal(TestEnumerable, result);
        }

        [Fact]
        public async Task SynchronousEnumerableMadeAsynchronousCanBeCancelled()
        {
            var result = new List<string>();
            var source = new CancellationTokenSource();
            await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            {
                await foreach (string s in TestEnumerable.ToAsyncEnumerable(source.Token))
                {
                    result.Add(s);
                    source.Cancel();
                }
            });
            Assert.Equal(new[] { "one" }, result);
        }

        private static readonly string[] TestEnumerable = new[] { "one", "two" };

        private static async IAsyncEnumerable<string> TestAsyncEnumerable()
        {
            await Task.Delay(10);
            yield return "one";
            await Task.Delay(10);
            yield return "two";
        }
    }
}
