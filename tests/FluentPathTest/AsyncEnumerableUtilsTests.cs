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
            Assert.Equal(TestEnumerable, await LogEnumeration(TestEnumerable.ToAsyncEnumerable()));
        }

        [Fact]
        public async Task SynchronousEnumerableMadeAsynchronousCanBeCancelled()
        {
            var source = new CancellationTokenSource();
            (var loggingEmumerable, var log) = GetAsyncEnumerableAndLog(TestEnumerable.ToAsyncEnumerable(source.Token));
            await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            {
                await foreach (string s in loggingEmumerable)
                {
                    source.Cancel();
                }
            });
            Assert.Equal(new[] { "one" }, log);
        }

        [Fact]
        public async Task SelectMapsItems()
        {
            Assert.Equal(
                new[] { "one-a-reno", "two-a-reno" },
                await LogEnumeration(
                    TestAsyncEnumerable()
                        .Select(s => s + "-a-reno")));
        }

        [Fact]
        public async Task SelectAsynchronouslyMapsItems()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var log = await LogEnumeration(TestAsyncEnumerable().Select(async s =>
            {
                await Task.Delay(30);
                return s + "-a-reno";
            }));
            stopwatch.Stop();
            Assert.True(stopwatch.ElapsedMilliseconds > 80);
            Assert.Equal(new[] { "one-a-reno", "two-a-reno" }, log);
        }

        [Fact]
        public async Task SelectNotNullSkipsNullItems()
        {
            Assert.Equal(
                new[] { "one-a-reno", "two-a-reno" },
                await LogEnumeration(
                    TestAsyncEnumerableWithNulls()
                        .SelectNotNull(s => s == "null" ? null : s + "-a-reno")));
        }

        [Fact]
        public async Task SelectManyFlattensNestedEnumeration()
        {
            Assert.Equal(
                new[] { 1, 1, 2, 1, 2, 3 },
                await LogEnumeration(
                    AsyncEnumerableFrom(1..3)
                        .SelectMany(j => AsyncEnumerableFrom(1..j))));
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
            Assert.Equal(
                new[] { 1, 3, 5 },
                await LogEnumeration(
                    AsyncEnumerableFrom(1..5)
                        .Where(j => j % 2 == 1)));
        }

        [Fact]
        public async Task WhereFiltersAsynchronously()
        {
            Assert.Equal(
                new[] { 1, 3, 5 },
                await LogEnumeration(
                    AsyncEnumerableFrom(1..5)
                    .Where(async j =>
                    {
                        await Task.Delay(10);
                        return j % 2 == 1;
                    })));
        }

        [Fact]
        public async Task AllIsTrueIfAllItemsSatisfyTheCondition()
        {
            Assert.True(await new[] { 1, 3, 5, 7 }.ToAsyncEnumerable().All(i => i % 2 == 1));
        }

        [Fact]
        public async Task AllIsFalseIfAnyItemSatisfiesTheCondition()
        {
            Assert.False(await new[] { 1, 3, 4, 5, 7 }.ToAsyncEnumerable().All(i => i % 2 == 1));
            Assert.False(await new[] { 1, 3, 5, 7, 8 }.ToAsyncEnumerable().All(i => i % 2 == 1));
            Assert.False(await new[] { 0, 1, 3, 5, 7 }.ToAsyncEnumerable().All(i => i % 2 == 1));
            Assert.False(await new[] { 0, 2 }.ToAsyncEnumerable().All(i => i % 2 == 1));
        }

        [Fact]
        public async Task AllIsTrueOnEmptyEnumeration()
        {
            Assert.True(await new int[] { }.ToAsyncEnumerable().All(i => i % 2 == 1));
        }

        [Fact]
        public async Task AnyIsFalseIfNoItemSatisfiesTheCondition()
        {
            Assert.False(await new[] { 1, 3, 5, 7 }.ToAsyncEnumerable().Any(i => i % 2 == 0));
        }

        [Fact]
        public async Task AnyIsTrueIfAnyItemSatisfiesTheCondition()
        {
            Assert.True(await new[] { 1, 3, 4, 5, 7 }.ToAsyncEnumerable().Any(i => i % 2 == 0));
            Assert.True(await new[] { 1, 3, 5, 7, 8 }.ToAsyncEnumerable().Any(i => i % 2 == 0));
            Assert.True(await new[] { 0, 1, 3, 5, 7 }.ToAsyncEnumerable().Any(i => i % 2 == 0));
            Assert.True(await new[] { 0, 2 }.ToAsyncEnumerable().Any(i => i % 2 == 0));
        }

        [Fact]
        public async Task AnyIsFalseOnEmptyEnumeration()
        {
            Assert.False(await new int[] { }.ToAsyncEnumerable().Any(i => i % 2 == 0));
        }

        [Fact]
        public async Task SkipSkips()
        {
            Assert.Equal(
                new[] { 4, 5, 6 },
                await LogEnumeration(new[] { 1, 2, 3, 4, 5, 6 }.ToAsyncEnumerable().Skip(3)));
        }

        [Fact]
        public async Task TakeTakes()
        {
            Assert.Equal(
                new[] { 1, 2, 3 },
                await LogEnumeration(new[] { 1, 2, 3, 4, 5, 6 }.ToAsyncEnumerable().Take(3)));
        }

        [Fact]
        public async Task EmptyIsempty()
        {
            Assert.Equal(
                Array.Empty<int>(),
                await LogEnumeration(AsyncEnumerable.Empty<int>()));
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

        private static async IAsyncEnumerable<T> LoggingEnumerable<T>(IAsyncEnumerable<T> enumerable, IList<T> log)
        {
            await foreach (T item in enumerable)
            {
                log.Add(item);
                yield return item;
            }
        }

        private static (IAsyncEnumerable<T> enumerable, IList<T> log) GetAsyncEnumerableAndLog<T>(IAsyncEnumerable<T> enumerable)
        {
            var log = new List<T>();
            return (LoggingEnumerable(enumerable, log), log);
        }

        private static async ValueTask<IList<T>> LogEnumeration<T>(IAsyncEnumerable<T> enumerable)
        {
            var log = new List<T>();
            await foreach(T item in enumerable)
            {
                log.Add(item);
            }
            return log;
        }
    }
}
