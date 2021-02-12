using Fluent.IO.Async;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace FluentPathTest
{
    public class PathAwaitableTests
    {
        [Fact]
        public async Task PathCanBeAwaited()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            Path result = await new Path("result").ForEach(async _ => await Task.Delay(10));
            stopwatch.Stop();
            Assert.Equal("result", result.ToString());
            Assert.True(stopwatch.ElapsedMilliseconds >= 10);
        }

        [Fact]
        public void PathWithoutAwaitCanBeUsedSynchronously()
        {
            Path result = new Path("result").Map(async p =>
            {
                await Task.Delay(10);
                return new Path("foo");
            });
            Assert.Equal("foo", result.ToString());
        }
    }
}
