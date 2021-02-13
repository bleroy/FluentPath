// Copyright © 2021 Bertrand Le Roy.  All Rights Reserved.
// This code released under the terms of the 
// MIT License http://opensource.org/licenses/MIT

using Fluent.IO.Async;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace FluentPathTest
{
    public class CachingAsyncEnumerableTests
    {
        [Fact]
        public async Task CachingAsyncEnumerableCanBeEnumeratedTwiceAndSecondOneIsCached()
        {
            var result = new List<string>();
            var caching = new CachingAsyncEnumerable<string>(TestEnumerable());
            var stopwatch = new Stopwatch();

            // First enumeration
            stopwatch.Start();
            await foreach(string s in caching)
            {
                result.Add(s);
            }
            stopwatch.Stop();
            Assert.Equal(new[] { "one", "two" }, result);
            Assert.True(stopwatch.ElapsedMilliseconds >= 20);

            // Second enumeration
            stopwatch.Restart();
            await foreach (string s in caching)
            {
                result.Add(s);
            }
            stopwatch.Stop();
            Assert.Equal(new[] { "one", "two", "one", "two" }, result);
            Assert.True(stopwatch.ElapsedMilliseconds < 20);
        }

        private async IAsyncEnumerable<string> TestEnumerable()
        {
            await Task.Delay(10);
            yield return "one";
            await Task.Delay(10);
            yield return "two";
        }
    }
}
