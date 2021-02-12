// Copyright © 2021 Bertrand Le Roy. All Rights Reserved.
// This code released under the terms of the 
// MIT License http://opensource.org/licenses/MIT

using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Fluent.IO.Async
{
    public class SyncAsyncEnumerable<T> : IAsyncEnumerable<T>, IEnumerable<T>
    {
        public IEnumerable<T> SyncEnumerable { get; }

        public SyncAsyncEnumerable(IEnumerable<T> syncEnumerable)
        {
            SyncEnumerable = syncEnumerable;
        }

        public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            foreach (T item in SyncEnumerable)
            {
                if (cancellationToken.IsCancellationRequested) throw new TaskCanceledException();
                yield return item;
            }
            await Task.CompletedTask;
        }

        public IEnumerator<T> GetEnumerator() => SyncEnumerable.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => SyncEnumerable.GetEnumerator();
    }
}
