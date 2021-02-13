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
        /// Applies a transformation on each item in an enumerable.
        /// </summary>
        /// <param name="source">The enumerable.</param>
        /// <param name="map">The mapping to apply on each item.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The mapped enumerable.</returns>
        public static async IAsyncEnumerable<TDest> Select<TSource, TDest>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, ValueTask<TDest>> map,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (TSource item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                yield return await map(item);
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
        /// Executes an action on each element in the enumerable, and returns the same enumerable.
        /// </summary>
        /// <param name="source">The enumerable.</param>
        /// <param name="action">The Action to execute on each item.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The same enumerable.</returns>
        public static async IAsyncEnumerable<TSource> ForEach<TSource>(
            this IAsyncEnumerable<TSource> source,
            Action<TSource> action,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (TSource item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                action(item);
                yield return item;
            }
        }

        /// <summary>
        /// Executes an action on each element in the enumerable, and returns the same enumerable.
        /// </summary>
        /// <param name="source">The enumerable.</param>
        /// <param name="action">The Action to execute on each item.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The same enumerable.</returns>
        public static async IAsyncEnumerable<TSource> ForEach<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, ValueTask> action,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (TSource item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                await action(item);
                yield return item;
            }
        }

        /// <summary>
        /// Filters an enumerable with a condition.
        /// </summary>
        /// <param name="source">The enumerable.</param>
        /// <param name="condition">The condition to verify on each item.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The filtered enumerable.</returns>
        public static async IAsyncEnumerable<T> Where<T>(
            this IAsyncEnumerable<T> source,
            Predicate<T> condition,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (T item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                if (condition(item)) yield return item;
            }
        }

        /// <summary>
        /// Filters an enumerable with a condition.
        /// </summary>
        /// <param name="source">The enumerable.</param>
        /// <param name="condition">The condition to verify on each item.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The filtered enumerable.</returns>
        public static async IAsyncEnumerable<T> Where<T>(
            this IAsyncEnumerable<T> source,
            Func<T, ValueTask<bool>> condition,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (T item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                if (await condition(item)) yield return item;
            }
        }

        /// <summary>
        /// Checks if all items in an async enumerable satisfy a predicate.
        /// </summary>
        /// <remarks>This method does not bail out early, and will enumerate to the end unless cancelled.</remarks>
        /// <param name="source">The enumerable.</param>
        /// <param name="predicate">The predicate to evaluate on each item.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>True if all items satisfy the condition expressed by the predicate.</returns>
        public static async ValueTask<bool> All<T>(
            this IAsyncEnumerable<T> source,
            Predicate<T> predicate,
            CancellationToken cancellationToken = default)
        {
            bool result = true;
            await foreach(T item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                if (!predicate(item)) result = false;
            }
            return result;
        }

        /// <summary>
        /// Checks if any item in an async enumerable satisfy a predicate.
        /// </summary>
        /// <remarks>This method does not bail out early, and will enumerate to the end unless cancelled.</remarks>
        /// <param name="source">The enumerable.</param>
        /// <param name="predicate">The predicate to evaluate on each item.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>True if any item satisfies the condition expressed by the predicate.</returns>
        public static async ValueTask<bool> Any<T>(
            this IAsyncEnumerable<T> source,
            Predicate<T> predicate,
            CancellationToken cancellationToken = default)
        {
            bool result = false;
            await foreach (T item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                if (predicate(item)) result = true;
            }
            return result;
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
            int i = 0;
            await foreach (T item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                if (i++ < skipCount) continue;
                yield return item;
            }
        }

        /// <summary>
        /// Enumerates the first <see cref="skipCount"/> items from the enumerable, unless the enumerable breaks first.
        /// </summary>
        /// <remarks>This method does not bail out early, and will enumerate to the end unless cancelled.</remarks>
        /// <param name="source">The original enumerable.</param>
        /// <param name="takeCount">The number of items to take.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The first <see cref="skipCount"/> items from the enumerable.</returns>
        public static async IAsyncEnumerable<T> Take<T>(
            this IAsyncEnumerable<T> source,
            int takeCount,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            int i = 0;
            await foreach (T item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                if (i++ < takeCount) yield return item;
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
