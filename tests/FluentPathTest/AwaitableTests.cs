using Fluent.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace FluentPathTest
{
    public class AwaitableTests
    {
        [Fact]
        public async Task AwaitableCanBeAwaited()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            string result = await new Awaitable<string>("result", Task.Delay(10));
            stopwatch.Stop();
            Assert.Equal("result", result);
            Assert.True(stopwatch.ElapsedMilliseconds >= 10);
        }

        [Fact]
        public void AwaitableWithoutTaskCanBeUsedSynchronously()
        {
            string result = new Awaitable<string>("result");
            Assert.Equal("result", result);
        }

        [Fact]
        public async Task AwaitableWithFactoryIsEvaluatedAfterTaskHasRun()
        {
            var result = new List<int> { 0 };
            result.Add(
                await new Awaitable<int>(
                    () => result.Count,
                    Task.Run(async () =>
                    {
                        await Task.Delay(10);
                        result.Add(1);
                    })));
            Assert.Equal(new[] { 0, 1, 2 }, result);
        }
    }
}
