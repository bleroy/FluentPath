// Copyright © 2010-2021 Bertrand Le Roy. All Rights Reserved.
// This code released under the terms of the 
// MIT License http://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Fluent.Utils
{
    /// <summary>
    /// A set of internal simple extensions to work with asynchronous enumerables
    /// </summary>
    public static class AsyncEnumerable
    {
        /// <summary>
        /// Creates an async enumerable from a regular enumerable.
        /// </summary>
        /// <param name="source">The enumerable.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The async enumerable.</returns>
        public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(
            this IEnumerable<T> source,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (T item in source)
            {
                if (cancellationToken.IsCancellationRequested) throw new TaskCanceledException();
                yield return await Task.FromResult(item).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Generates a synchronous enumerable from an asynchronous one, which of course comes at a potential perf cost.
        /// </summary>
        /// <param name="source">The async enumerable to enumerate synchronously.</param>
        /// <returns>A synchronous enumerable with the same contents as the original asynchronous one.</returns>
        public static IEnumerable<T> ToEnumerable<T>(
            this IAsyncEnumerable<T> source,
            CancellationToken cancellationToken = default)
        {
            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            try
            {
                while (enumerator.MoveNextAsync().GetAwaiter().GetResult())
                {
                    yield return enumerator.Current;
                }
            }
            finally
            {
                if (enumerator != null) enumerator.DisposeAsync().GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// Applies a transformation on each item in an enumerable.
        /// </summary>
        /// <param name="source">The enumerable.</param>
        /// <param name="map">The mapping to apply on each item.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The mapped enumerable.</returns>
        public static async IAsyncEnumerable<TDest> Select<TSource, TDest>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, TDest> map,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (TSource item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                yield return map(item);
            }
        }

        /// <summary>
        /// Applies a transformation on each item in an enumerable and removes null entries.
        /// </summary>
        /// <param name="source">The enumerable.</param>
        /// <param name="map">The mapping to apply on each item.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The mapped enumerable.</returns>
        public static async IAsyncEnumerable<TDest> SelectNotNull<TSource, TDest>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, TDest?> map,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (TSource item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                TDest? mapped = map(item);
                if (mapped is not null) yield return mapped;
            }
        }

        /// <summary>
        /// Applies a transformation on each item in an enumerable and flattens the result onto a single enumerable.
        /// </summary>
        /// <param name="source">The enumerable.</param>
        /// <param name="map">The mapping to apply on each item.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The mapped enumerable.</returns>
        public static async IAsyncEnumerable<TDest> SelectMany<TSource, TDest>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, IAsyncEnumerable<TDest>> map,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (TSource item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                await foreach(TDest inner in map(item).WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    yield return inner;
                }
            }
        }

        /// <summary>
        /// Returns the first item in the enumerable or the default value for the type.
        /// </summary>
        /// <param name="source">The enumerable.</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>The first item in the enumerable or default.</returns>
        public static async ValueTask<T?> FirstOrDefault<T>(
            this IAsyncEnumerable<T> source,
            CancellationToken cancellationToken = default)
        {
            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            try
            {
                if (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    return enumerator.Current;
                }
                return default;
            }
            finally
            {
                if (enumerator != null) await enumerator.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Checks if all items in an async enumerable satisfy a predicate.
        /// </summary>
        /// <param name="source">The enumerable.</param>
        /// <param name="predicate">The predicate to evaluate on each item.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>True if all items satisfy the condition expressed by the predicate.</returns>
        public static async ValueTask<bool> All<T>(
            this IAsyncEnumerable<T> source,
            Predicate<T> predicate,
            CancellationToken cancellationToken = default)
        {
            await foreach(T item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                if (!predicate(item)) return false;
            }
            return true;
        }

        /// <summary>
        /// Checks if any item in an async enumerable satisfy a predicate.
        /// </summary>
        /// <param name="source">The enumerable.</param>
        /// <param name="predicate">The predicate to evaluate on each item.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>True if any item satisfies the condition expressed by the predicate.</returns>
        public static async ValueTask<bool> Any<T>(
            this IAsyncEnumerable<T> source,
            Predicate<T> predicate,
            CancellationToken cancellationToken = default)
        {
            await foreach (T item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                if (predicate(item)) return true;
            }
            return false;
        }

        /// <summary>
        /// Skips the provided number of items from the enumerable.
        /// </summary>
        /// <param name="source">The original enumerable.</param>
        /// <param name="skipCount">The number of items to skip.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The original enumeration minus the skipped ones.</returns>
        public static async IAsyncEnumerable<T> Skip<T>(
            this IAsyncEnumerable<T> source,
            int skipCount,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            try
            {
                for (int i = 0; i < skipCount; i++)
                {
                    if (!await enumerator.MoveNextAsync().ConfigureAwait(false)) yield break;
                }
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    yield return enumerator.Current;
                }
            }
            finally
            {
                if (enumerator != null) await enumerator.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Enumerates the first <see cref="skipCount"/> items from the enumerable, unless the enumerable breaks first.
        /// </summary>
        /// <param name="source">The original enumerable.</param>
        /// <param name="takeCount">The number of items to take.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The first <see cref="skipCount"/> items from the enumerable.</returns>
        public static async IAsyncEnumerable<T> Take<T>(
            this IAsyncEnumerable<T> source,
            int takeCount,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            try
            {
                for (int i = 0; i < takeCount; i++)
                {
                    if (await enumerator.MoveNextAsync().ConfigureAwait(false))
                    {
                        yield return enumerator.Current;
                    }
                }
            }
            finally
            {
                if (enumerator != null) await enumerator.DisposeAsync();
            }
        }

        /// <summary>
        /// An empty async enumerable.
        /// </summary>
        /// <returns>An empty async enumerable of the specified type.</returns>
        public static async IAsyncEnumerable<T> Empty<T>()
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}
