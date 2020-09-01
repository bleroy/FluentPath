using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fluent.IO
{
    /// <summary>
    /// This interface is explicitly implemented by the Path class so its members are less
    /// visible to regular users of the class, for whom it is useless, confusing or dangerous,
    /// but are still accessible for extensions, which implement the main scenario for them.
    /// </summary>
    public interface IPathChain
    {
        IEnumerable<string> Paths { get; }
        Path Chain(Func<IEnumerable<string>> pathResolver);
        Path Chain(Func<IEnumerable<string>> pathResolver, Path previous);
        Path Chain(Func<Task<IEnumerable<string>>> pathResolver);
        Path Chain(Func<Task<IEnumerable<string>>> pathResolver, Path previous);
        Path Chain(Action continuation);
        Path Chain(Action continuation, IEnumerable<string> paths, Path previous);
        Path Chain(Action continuation, Func<IEnumerable<string>> pathResolver);
        Path Chain(Action continuation, Func<IEnumerable<string>> pathResolver, Path previous);
        Path Chain(Task continuation);
        Path Chain(Task continuation, IEnumerable<string> paths, Path previous);
        Path Chain(Task continuation, Func<IEnumerable<string>> pathResolver);
        Path Chain(Task continuation, Func<IEnumerable<string>> pathResolver, Path previous);
        Path Chain(Func<Task> continuationFactory);
        Path Chain(Func<Task> continuationFactory, IEnumerable<string> paths, Path previous);
        Path Chain(Func<Task> continuationFactory, Func<IEnumerable<string>> pathResolver);
        Path Chain(Func<Task> continuationFactory, Func<IEnumerable<string>> pathResolver, Path previous);
        Awaitable<T> Return<T>(T value) where T : notnull;
        Awaitable<T> Return<T>(Func<T> valueResolver) where T : notnull;
    }
}
