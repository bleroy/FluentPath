// Copyright © 2021 Bertrand Le Roy.  All Rights Reserved.
// This code released under the terms of the 
// MIT License http://opensource.org/licenses/MIT

using Fluent.IO.Async;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace FluentPathTest
{
    public class SyncAsyncEnumerableTests
    {
        [Fact]
        public async Task SyncAsyncEnumerableCanBeEnumeratedAsynchronously()
        {
            var result = new List<string>();
            var asyncWrap = new SyncAsyncEnumerable<string>(TestEnumerable());

            await foreach(string s in asyncWrap)
            {
                result.Add(s);
            }
            Assert.Equal(new[] { "one", "two" }, result);
        }

        private IEnumerable<string> TestEnumerable()
        {
            yield return "one";
            yield return "two";
        }
    }
}
