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
    public class CachingAsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        private List<T>? _cache;
        private bool _cached;
        private object _lock = new();
        private readonly IAsyncEnumerable<T> _wrapped;

        public CachingAsyncEnumerable(IAsyncEnumerable<T> wrapped)
        {
            _wrapped = wrapped;
        }

        private async IAsyncEnumerable<T> Enumerate([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            lock(_lock)
            {
                if (!_cached)
                {
                    if (_cache is not null)
                    {
                        throw new InvalidOperationException("Multiple concurrent attempts to enumerate.");
                    }
                    _cache = new();
                }
            }
            if (_cache is null)
            {
                throw new InvalidOperationException("_cache should not be null.");
            }
            if (_cached)
            {
                foreach(T item in _cache)
                {
                    yield return item;
                }
            }
            else
            {
                await foreach (T item in _wrapped.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    _cache.Add(item);
                    yield return item;
                }
                _cached = true;
            }
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
            Enumerate(cancellationToken).GetAsyncEnumerator(cancellationToken);
    }
}
