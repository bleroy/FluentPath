// Copyright © 2021 Bertrand Le Roy. All Rights Reserved.
// This code released under the terms of the 
// MIT License http://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Fluent.IO.Async
{
    public class AsyncEnumerableWithTaskPrelude<T> : IAsyncEnumerable<T>
    {
        private ValueTask<IAsyncEnumerable<T>> _prelude;

        public AsyncEnumerableWithTaskPrelude(Func<ValueTask<IAsyncEnumerable<T>>> prelude)
        {
            _prelude = prelude();
        }

        private async IAsyncEnumerable<T> Enumerate([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach(T item in (await _prelude.ConfigureAwait(false)).WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                yield return item;
            }
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
            Enumerate(cancellationToken).GetAsyncEnumerator(cancellationToken);
    }
}
