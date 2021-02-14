// Copyright © 2021 Bertrand Le Roy.  All Rights Reserved.
// This code released under the terms of the 
// MIT License http://opensource.org/licenses/MIT

using Fluent.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        [Fact]
        public async Task SelectMapsItems()
        {
            var result = new List<string>();
            await foreach (string s in TestAsyncEnumerable().Select(s => s + "-a-reno"))
            {
                result.Add(s);
            }
            Assert.Equal(new[] { "one-a-reno", "two-a-reno" }, result);
        }

        [Fact]
        public async Task SelectAsynchronouslyMapsItems()
        {
            var result = new List<string>();
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            await foreach (string s in TestAsyncEnumerable().Select(async s =>
            {
                await Task.Delay(30);
                return s + "-a-reno";
            }))
            {
                result.Add(s);
            }
            stopwatch.Stop();
            Assert.True(stopwatch.ElapsedMilliseconds > 80);
            Assert.Equal(new[] { "one-a-reno", "two-a-reno" }, result);
        }

        [Fact]
        public async Task SelectNotNullSkipsNullItems()
        {
            var result = new List<string>();
            await foreach (string s in TestAsyncEnumerableWithNulls().SelectNotNull(s => s == "null" ? null : s + "-a-reno"))
            {
                result.Add(s);
            }
            Assert.Equal(new[] { "one-a-reno", "two-a-reno" }, result);
        }

        [Fact]
        public async Task SelectManyFlattensNestedEnumeration()
        {
            var result = new List<int>();
            await foreach (int i in AsyncEnumerableFrom(1..3).SelectMany(j => AsyncEnumerableFrom(1..j)))
            {
                result.Add(i);
            }
            Assert.Equal(new[] { 1, 1, 2, 1, 2, 3 }, result);
        }

        [Fact]
        public async Task ForEachCallsActionForEachItem()
        {
            var result = new List<string>();
            await foreach (string s in TestAsyncEnumerable().ForEach(j =>
            {
                result.Add($"{j}-a-reno");
            }))
            {
                result.Add(s);
            }
            Assert.Equal(new[] { "one-a-reno", "one", "two-a-reno", "two" }, result);
        }

        [Fact]
        public async Task ForEachCallsActionAsynchronouslyForEachItem()
        {
            var result = new List<string>();
            await foreach (string s in TestAsyncEnumerable().ForEach(async j =>
            {
                await Task.Delay(10);
                result.Add($"{j}-a-reno");
            }))
            {
                result.Add(s);
            }
            Assert.Equal(new[] { "one-a-reno", "one", "two-a-reno", "two" }, result);
        }

        [Fact]
        public async Task WhereFilters()
        {
            var result = new List<int>();
            await foreach (int i in AsyncEnumerableFrom(1..5).Where(j => j % 2 == 1))
            {
                result.Add(i);
            }
            Assert.Equal(new[] { 1, 3, 5 }, result);
        }

        [Fact]
        public async Task WhereFiltersAsynchronously()
        {
            var result = new List<int>();
            await foreach (int i in AsyncEnumerableFrom(1..5).Where(async j =>
            {
                await Task.Delay(10);
                return j % 2 == 1;
            }))
            {
                result.Add(i);
            }
            Assert.Equal(new[] { 1, 3, 5 }, result);
        }

        private static readonly string[] TestEnumerable = new[] { "one", "two" };

        private static async IAsyncEnumerable<string> TestAsyncEnumerable()
        {
            await Task.Delay(10);
            yield return "one";
            await Task.Delay(10);
            yield return "two";
        }

        private static async IAsyncEnumerable<string> TestAsyncEnumerableWithNulls()
        {
            await Task.CompletedTask;
            yield return "null";
            yield return "one";
            yield return "null";
            yield return "two";
            yield return "null";
        }

        private static async IAsyncEnumerable<int> AsyncEnumerableFrom(Range r)
        {
            for (int i = r.Start.Value; i <= r.End.Value; i++)
            {
                yield return i;
            }
            await Task.CompletedTask;
        }
    }
}
