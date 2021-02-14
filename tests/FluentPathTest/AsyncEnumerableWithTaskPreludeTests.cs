// Copyright © 2010-2021 Bertrand Le Roy.  All Rights Reserved.
// This code released under the terms of the 
// MIT License http://opensource.org/licenses/MIT

using Fluent.IO.Async;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace FluentPathTest
{
    public class AsyncEnumerableWithTaskPreludeTests
    {
        [Fact]
        public async Task AsyncEnumerableWithPreludeExecutesTaskBeforeEnumerating()
        {
            bool taskRan = false;
            var result = new List<string>();

            await foreach (string s in new AsyncEnumerableWithTaskPrelude<string>(async () =>
            {
                await Task.Delay(10);
                taskRan = true;
                return TestAsyncEnumerable();
            }))
            {
                Assert.True(taskRan);
                result.Add(s);
            }
            Assert.True(taskRan);
            Assert.Equal(new[] { "one", "two" }, result);
        }

        private static async IAsyncEnumerable<string> TestAsyncEnumerable()
        {
            await Task.Delay(10);
            yield return "one";
            await Task.Delay(10);
            yield return "two";
        }
    }
}