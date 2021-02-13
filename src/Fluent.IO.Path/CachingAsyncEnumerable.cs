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
        private IAsyncEnumerable<T>? _syncAsyncCache;
        private bool _cached;
        private object _lock = new();
        private readonly IAsyncEnumerable<T> _wrapped;

        public CachingAsyncEnumerable(IAsyncEnumerable<T> wrapped)
        {
            _wrapped = wrapped;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                if (!_cached)
                {
                    if (_cache is not null)
                    {
                        throw new InvalidOperationException("Multiple concurrent attempts to enumerate.");
                    }
                    _cache = new();
                    _syncAsyncCache = new SyncAsyncEnumerable<T>(_cache);
                    return new CachingEnumerator<T>(_wrapped.GetAsyncEnumerator(cancellationToken), _cache, whenDone: () => { _cached = true; });
                }
            }
            if (_syncAsyncCache is null) throw new InvalidOperationException("This should never happen.");
            return _syncAsyncCache.GetAsyncEnumerator();
        }

        private class CachingEnumerator<U> : IAsyncEnumerator<U>
        {
            private readonly IList<U> _cache;
            private readonly IAsyncEnumerator<U> _wrapped;
            private readonly Action _whenDone;

            public CachingEnumerator(IAsyncEnumerator<U> wrapped, IList<U> cache, Action whenDone)
            {
                _wrapped = wrapped;
                _cache = cache;
                _whenDone = whenDone;
            }

            public U Current => _wrapped.Current;

            public async ValueTask DisposeAsync()
            {
                await _wrapped.DisposeAsync();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (await _wrapped.MoveNextAsync())
                {
                    _cache.Add(_wrapped.Current);
                    return true;
                }
                _whenDone();
                return false;
            }
        }
    }
}
