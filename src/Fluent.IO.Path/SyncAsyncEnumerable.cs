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

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
            new SyncAsyncEnumerator<T>(SyncEnumerable.GetEnumerator());

        public IEnumerator<T> GetEnumerator() => SyncEnumerable.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => SyncEnumerable.GetEnumerator();

        private class SyncAsyncEnumerator<U> : IAsyncEnumerator<U>
        {
            private IEnumerator<U> _enumerator;

            public SyncAsyncEnumerator(IEnumerator<U> enumerator)
            {
                _enumerator = enumerator;
            }

            public U Current => _enumerator.Current;

            public async ValueTask DisposeAsync() => await Task.CompletedTask;

            public async ValueTask<bool> MoveNextAsync()
            {
                return await new ValueTask<bool>(_enumerator.MoveNext());
            }
        }
    }
}
