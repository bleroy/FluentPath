// Copyright © 2010-2019 Bertrand Le Roy.  All Rights Reserved.
// This code released under the terms of the 
// MIT License http://opensource.org/licenses/MIT

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SystemPath = System.IO.Path;

namespace Fluent.IO
{
    [TypeConverter(typeof(PathConverter))]
    public sealed class Path : IEnumerable<Path>, INotifyCompletion, IPathChain
    {
        #region state
        /// <summary>
        /// The previous set, from which the current one was created.
        /// </summary>
        public Path Previous { get; }

        private Task _task;
        private readonly bool _isPathCached = false;
        private IEnumerable<string> _pathCache = Array.Empty<string>();
        private readonly Func<IEnumerable<string>> _pathResolver = () => Array.Empty<string>();
        #endregion

        #region constructors and factories
        /// <summary>
        /// Creates a collection of paths from a list of path strings.
        /// </summary>
        /// <param name="paths">The list of path strings.</param>
        public Path(params string[] paths) : this(paths, Empty) { }

        /// <summary>
        /// Creates a collection of paths from a list of path strings and a previous list of paths.
        /// </summary>
        /// <param name="path">A path string.</param>
        /// <param name="previous">The previous set.</param>
        public Path(string path, Path previous) : this(new[] { path }, previous) { }

        /// <summary>
        /// Creates a collection of paths from a list of path strings and a previous list of paths.
        /// </summary>
        /// <param name="paths">The list of path strings in the set.</param>
        /// <param name="previous">The previous set.</param>
        public Path(IEnumerable<string> paths, Path previous) : this(Task.FromResult(true), paths, previous) { }

        /// <summary>
        /// Creates a collection of paths from a prerequisiste task, a list of path strings and a previous list of paths.
        /// </summary>
        /// <param name="paths">The list of paths in the set.</param>
        /// <param name="previous">The previous set.</param>
        private Path(Task task, IEnumerable<string> paths, Path previous)
        {
            _task = task;
            _pathCache = Normalize(paths);
            _isPathCached = true;
            Previous = previous;
        }

        /// <summary>
        /// Creates a collection of paths from a prerequisiste task, a path resolver and a previous list of paths.
        /// </summary>
        /// <param name="paths">The list of paths in the set.</param>
        /// <param name="previous">The previous set.</param>
        private Path(Task task, Func<IEnumerable<string>> pathResolver, Path previous)
        {
            _task = task;
            _pathResolver = pathResolver;
            Previous = previous;
        }

        /// <summary>
        /// Creates a collection of paths from a prerequisiste task, a path resolver and a previous list of paths.
        /// </summary>
        /// <param name="paths">The list of paths in the set.</param>
        /// <param name="previous">The previous set.</param>
        private Path(
            Task task,
            bool isPathCached,
            IEnumerable<string> paths,
            Func<IEnumerable<string>> pathResolver,
            Path previous)
        {
            _task = task;
            _isPathCached = isPathCached;
            _pathCache = paths;
            _pathResolver = pathResolver;
            Previous = previous;
        }

        /// <summary>
        /// Creates a new path from its string token representation.
        /// </summary>
        /// <example>Path.Get("c:", "foo", "bar") will get c:\foo\bar on Windows.</example>
        /// <param name="pathTokens">The tokens for the path.</param>
        /// <returns>The path object.</returns>
        public static Path FromTokens(params string[] pathTokens)
            => (pathTokens.Length == 0)
            ? throw new ArgumentException("At least one token needs to be specified.", nameof(pathTokens))
            : new Path(SystemPath.Combine(pathTokens));

        /// <summary>
        /// Normalizes and removes duplicates from a list of path strings.
        /// </summary>
        /// <param name="rawPaths">The paths to normalize</param>
        /// <returns>The normalized paths</returns>
        private static IEnumerable<string> Normalize(IEnumerable<string> rawPaths)
            => rawPaths
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s[^1] == SystemPath.DirectorySeparatorChar && SystemPath.GetPathRoot(s) != s ? s[0..^1] : s)
                .Distinct(StringComparer.CurrentCultureIgnoreCase);
        #endregion

        #region chainability methods for extension implementers
        private IPathChain Chain => this;

        Path IPathChain.Chain(Func<IEnumerable<string>> pathResolver, Path previous)
            => _task.IsCompleted ? new Path(pathResolver(), previous) : new Path(_task, pathResolver, previous);

        Path IPathChain.Chain(Func<IEnumerable<string>> pathResolver) => Chain.Chain(pathResolver, this);

        Path IPathChain.Chain(Func<Task<IEnumerable<string>>> pathResolver, Path previous)
        {
            Task antecedent = _task;
            IEnumerable<string> paths = Array.Empty<string>();
            return Chain.Chain(Task.Run(async () =>
            {
                if (!antecedent.IsCompleted)
                {
                    await antecedent;
                }
                paths = await pathResolver();
            }), () => paths, previous);
        }

        Path IPathChain.Chain(Func<Task<IEnumerable<string>>> pathResolver) => Chain.Chain(pathResolver, this);

        Path IPathChain.Chain(Action continuation, IEnumerable<string> paths, Path previous)
        {
            if (_task.IsCompleted)
            {
                continuation();
                return new Path(paths, previous);
            }
            return new Path(Task.Run(continuation), paths, previous);
        }

        Path IPathChain.Chain(Action continuation)
        {
            if (_task.IsCompleted)
            {
                continuation();
                return new Path(Chain.Paths, this);
            }
            return new Path(Task.Run(continuation), _isPathCached, _pathCache, _pathResolver, this);
        }

        Path IPathChain.Chain(Action continuation, Func<IEnumerable<string>> pathResolver, Path previous)
        {
            if (_task.IsCompleted)
            {
                continuation();
                return new Path(pathResolver(), previous);
            }
            return new Path(Task.Run(continuation), pathResolver, previous);
        }

        Path IPathChain.Chain(Action continuation, Func<IEnumerable<string>> pathResolver)
            => Chain.Chain(continuation, pathResolver, this);

        Path IPathChain.Chain(Task continuation, IEnumerable<string> paths, Path previous)
        {
            if (_task.IsCompleted)
            {
                return new Path(continuation, paths, previous);
            }
            Task antecedent = _task;
            return new Path(Task.Run(async () =>
            {
                await antecedent;
                await continuation;
            }), paths, previous);
        }

        Path IPathChain.Chain(Task continuation)
        {
            if (_task.IsCompleted)
            {
                return new Path(continuation, Chain.Paths, this);
            }
            Task antecedent = _task;
            return new Path(Task.Run(async () =>
            {
                await antecedent;
                await continuation;
            }), _isPathCached, _pathCache, _pathResolver, this);
        }

        Path IPathChain.Chain(Task continuation, Func<IEnumerable<string>> pathResolver, Path previous)
        {
            if (_task.IsCompleted)
            {
                return new Path(continuation, pathResolver(), previous);
            }
            Task antecedent = _task;
            return new Path(Task.Run(async () =>
            {
                await antecedent;
                await continuation;
            }), pathResolver, previous);
        }

        Path IPathChain.Chain(Task continuation, Func<IEnumerable<string>> pathResolver)
            => Chain.Chain(continuation, pathResolver, this);

        Path IPathChain.Chain(Func<Task> continuationFactory, IEnumerable<string> paths, Path previous)
        {
            if (_task.IsCompleted)
            {
                return new Path(continuationFactory(), paths, previous);
            }
            Task antecedent = _task;
            return new Path(Task.Run(async () =>
            {
                await antecedent;
                await continuationFactory();
            }), paths, previous);
        }

        Path IPathChain.Chain(Func<Task> continuationFactory)
        {
            if (_task.IsCompleted)
            {
                return new Path(continuationFactory(), _isPathCached, _pathCache, _pathResolver, this);
            }
            Task antecedent = _task;
            return new Path(Task.Run(async () =>
            {
                await antecedent;
                await continuationFactory();
            }), _isPathCached, _pathCache, _pathResolver, this);
        }

        Path IPathChain.Chain(Func<Task> continuationFactory, Func<IEnumerable<string>> pathResolver, Path previous)
        {
            if (_task.IsCompleted)
            {
                return new Path(continuationFactory(), pathResolver(), previous);
            }
            Task antecedent = _task;
            return new Path(Task.Run(async () =>
            {
                await antecedent;
                await continuationFactory();
            }), pathResolver, previous);
        }

        Path IPathChain.Chain(Func<Task> continuationFactory, Func<IEnumerable<string>> pathResolver)
            => Chain.Chain(continuationFactory, pathResolver, this);

        Awaitable<T> IPathChain.Return<T>(Func<T> valueResolver) => new Awaitable<T>(valueResolver, _task);

        Awaitable<T> IPathChain.Return<T>(T value) => new Awaitable<T>(value, _task);

        IEnumerable<string> IPathChain.Paths
            => _task.IsCompleted ? _isPathCached ? _pathCache : _pathCache = _pathResolver()
                : throw new InvalidOperationException("Can't evaluate the paths on a pending path operation.");
        #endregion

        #region equality, hash code and cast to/from string
        public static explicit operator string(Path path) => path.FirstPath();

        public static explicit operator Path(string path) => new Path(path);

        public static bool operator ==(Path path1, Path path2)
            => ReferenceEquals(path1, path2) ? true : path1.IsSameAs(path2);

        public static bool operator !=(Path path1, Path path2) => !(path1 == path2);

        // Overrides
        public override bool Equals(object obj)
        {
            if (!(obj is Path paths))
            {
                if (!(obj is string str)) return false;
                if (!_task.IsCompleted)
                {
                    throw WasntAwaitedException("Equality");
                }
                IEnumerator<string> enumerator = Chain.Paths.GetEnumerator();
                if (!enumerator.MoveNext()) return false;
                if (enumerator.Current != str) return false;
                return !enumerator.MoveNext();
            }
            return IsSameAs(paths);
        }

        protected bool IsSameAs(Path other)
            => _task.IsCompleted && other._task.IsCompleted
                ? new HashSet<string>(Chain.Paths).SetEquals(other.Chain.Paths)
                : throw WasntAwaitedException("IsSameAs");

        public override int GetHashCode()
        {
            if (!_task.IsCompleted)
            {
                throw WasntAwaitedException("GetHashCode");
            }
            var hashCode = new HashCode();
            foreach (string path in Chain.Paths)
            {
                hashCode.Add(path);
            }
            return hashCode.ToHashCode();
        }

        private Exception WasntAwaitedException(string operation)
            => new InvalidOperationException(
                        $"{operation} can't be evaluated on paths that have pending tasks. " +
                        "Await the path before performing this operation.");

        #endregion

        #region enumerable<Path>
        public IEnumerator<Path> GetEnumerator()
            => _task.IsCompleted
                ? Chain.Paths.Select(path => new Path(path, this)).GetEnumerator()
                : throw WasntAwaitedException("GetEnumerator");

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion

        #region task-like implementation
        public Path GetAwaiter() => this;

        public bool IsCompleted => _task?.IsCompleted ?? true;

        void INotifyCompletion.OnCompleted(Action continuation)
        {
            if (_task.IsCompleted)
            {
                continuation();
                return;
            }
            Task antecedent = _task;
            _task = Task.Run(async () =>
            {
                await antecedent;
                continuation();
            });
        }

        public Path GetResult() => this;
        #endregion

        #region static paths and methods
        /// <summary>
        /// An empty set path. Can be used as default value. C#'s default keyword is useless for paths.
        /// </summary>
        public static Path Empty { get; } = new Path(Array.Empty<string>());

        /// <summary>
        /// The current path for the application.
        /// </summary>
        public static Path Current
        {
            get => new Path(Directory.GetCurrentDirectory());
            set => Directory.SetCurrentDirectory(value.FirstPath());
        }

        public static Path Root => new Path(SystemPath.GetPathRoot(Current.ToString()));

        /// <summary>
        /// Creates a directory in the file system.
        /// </summary>
        /// <param name="directoryName">The name of the directory to create.</param>
        /// <returns>The path of the new directory.</returns>
        public static Path CreateDirectory(string directoryName)
        {
            Directory.CreateDirectory(directoryName);
            return new Path(directoryName);
        }
        #endregion

        #region properties
        /// <summary>
        /// The paths in this Path set.
        /// </summary>
        public Awaitable<IEnumerable<string>> Paths
            => Chain.Return(() => Chain.Paths);

        /// <summary>
        /// The name of the directory for the first path in the collection.
        /// This is the string representation of the parent directory path.
        /// </summary>
        public Awaitable<string> DirectoryName => Chain.Return(() => SystemPath.GetDirectoryName(FirstPath()));

        /// <summary>
        /// The extension for the first path in the collection, including the ".".
        /// </summary>
        public Awaitable<string> Extension => Chain.Return(() => SystemPath.GetExtension(FirstPath()));

        /// <summary>
        /// The filename or folder name for the first path in the collection, including the extension.
        /// </summary>
        public Awaitable<string> FileName => Chain.Return(() => SystemPath.GetFileName(FirstPath()));

        /// <summary>
        /// The filename or folder name for the first path in the collection, without the extension.
        /// </summary>
        public Awaitable<string> FileNameWithoutExtension
            => Chain.Return(() => SystemPath.GetFileNameWithoutExtension(FirstPath()));

        /// <summary>
        /// The fully qualified path string for the first path in the collection.
        /// </summary>
        public Awaitable<string> FullPath => Chain.Return(() => SystemPath.GetFullPath(FirstPath()));

        /// <summary>
        /// The fully qualified path strings for all the paths in the set.
        /// </summary>
        public Awaitable<string[]> FullPaths
            => Chain.Return(() => Chain.Paths.Select(path => SystemPath.GetFullPath(path)).Distinct().ToArray());

        /// <summary>
        /// True all the paths in the collection have an extension.
        /// </summary>
        public Awaitable<bool> HasExtension => Chain.Return(() => Chain.Paths.All(SystemPath.HasExtension));

        /// <summary>
        /// True if each path in the set is the path of
        /// a directory in the file system.
        /// </summary>
        public Awaitable<bool> IsDirectory => Chain.Return(() => Chain.Paths.All(Directory.Exists));

        /// <summary>
        /// True if all the files in the collection are encrypted on disc.
        /// </summary>
        public Awaitable<bool> IsEncrypted
            => Chain.Return(() => Chain.Paths.All(p =>
                Directory.Exists(p) ||
                (File.GetAttributes(p) & FileAttributes.Encrypted) != 0));

        /// <summary>
        /// True if all the paths in the collection are fully-qualified.
        /// </summary>
        public Awaitable<bool> IsRooted => Chain.Return(() => Chain.Paths.All(SystemPath.IsPathRooted));

        /// <summary>
        /// The parent path for the first path in the collection.
        /// </summary>
        public Path Parent() => First().Up();

        /// <summary>
        /// The parent paths for the paths in the collection.
        /// </summary>
        public Path Parents() => Up();

        /// <summary>
        /// The root directory of the first path of the collection,
        /// such as "C:\".
        /// </summary>
        public Awaitable<string> PathRoot => Chain.Return(() => SystemPath.GetPathRoot(FirstPath()));
        #endregion

        #region file extensions
        /// <summary>
        /// Changes the path on each path in the set.
        /// Does not do any physical change to the file system.
        /// </summary>
        /// <param name="newExtension">The new extension.</param>
        /// <returns>The set</returns>
        public Path ChangeExtension(string newExtension) => ChangeExtension(p => newExtension);

        /// <summary>
        /// Changes the path on each path in the set.
        /// Does not do any physical change to the file system.
        /// </summary>
        /// <param name="extensionTransformation">A function that maps each path to an extension.</param>
        /// <returns>The set of files with the new extension</returns>
        public Path ChangeExtension(Func<Path, string> extensionTransformation)
            => Chain.Chain(() => Chain.Paths
                    .Where(p => !Directory.Exists(p))
                    .Select(p => SystemPath.ChangeExtension(p, extensionTransformation(new Path(p, this)))));
        #endregion

        #region combine
        /// <summary>
        /// Combines each path in the set with the specified file or directory name.
        /// Does not do any physical change to the file system.
        /// </summary>
        /// <param name="directoryNameGenerator">A function that maps each path to a file or directory name.</param>
        /// <returns>The set</returns>
        public Path Combine(Func<Path, string> directoryNameGenerator)
            => Chain.Chain(() => Chain.Paths
                    .Select(p => SystemPath.Combine(p, directoryNameGenerator(new Path(p, this)))));

        /// <summary>
        /// Combines each path in the set with the specified relative path.
        /// Does not do any physical change to the file system.
        /// </summary>
        /// <param name="relativePath">The path to combine. Only the first path is used.</param>
        /// <returns>The combined paths.</returns>
        public Path Combine(Path relativePath) => Combine(relativePath.Tokens);

        /// <summary>
        /// Combines each path in the set with the specified tokens.
        /// Does not do any physical change to the file system.
        /// </summary>
        /// <param name="pathTokens">One or several directory and file names to combine</param>
        /// <returns>The new set of combined paths</returns>
        public Path Combine(params string[] pathTokens)
            => pathTokens.Length == 0 ? this
                : pathTokens.Length == 1 ? Combine(p => pathTokens[0])
                : Chain.Chain(() => Chain.Paths
                    .Select(p => SystemPath.Combine(new string[] { p }.Concat(pathTokens).ToArray())));
        #endregion

        #region copy
        /// <summary>
        /// Copies the file or folder for this path to another location.
        /// The copy is not recursive.
        /// Existing files won't be overwritten.
        /// </summary>
        /// <param name="destination">The destination path.</param>
        /// <returns>The destination path.</returns>
        public Path Copy(Path destination) => Copy(p => destination, Overwrite.Never, false);

        /// <summary>
        /// Copies the file or folder for this path to another location. The copy is not recursive.
        /// </summary>
        /// <param name="destination">The destination path.</param>
        /// <param name="overwrite">Overwriting policy. Default is never.</param>
        /// <returns>The destination path.</returns>
        public Path Copy(Path destination, Overwrite overwrite) => Copy(p => destination, overwrite, false);

        /// <summary>
        /// Copies the file or folder for this path to another location.
        /// </summary>
        /// <param name="destination">The destination path.</param>
        /// <param name="overwrite">Overwriting policy. Default is never.</param>
        /// <param name="recursive">True if the copy should be deep and include subdirectories recursively. Default is false.</param>
        /// <returns>The source path.</returns>
        public Path Copy(Path destination, Overwrite overwrite, bool recursive)
            => Copy(p => destination, overwrite, recursive);

        /// <summary>
        /// Copies the file or folder for this path to another location.
        /// The copy is not recursive.
        /// Existing files won't be overwritten.
        /// </summary>
        /// <param name="destination">The destination path string.</param>
        /// <returns>The destination path.</returns>
        public Path Copy(string destination) => Copy(p => new Path(destination, this), Overwrite.Never, false);

        /// <summary>
        /// Copies the file or folder for this path to another location.
        /// The copy is not recursive.
        /// </summary>
        /// <param name="destination">The destination path string.</param>
        /// <param name="overwrite">Overwriting policy. Default is never.</param>
        /// <returns>The destination path.</returns>
        public Path Copy(string destination, Overwrite overwrite)
            => Copy(p => new Path(destination, this), overwrite, false);

        /// <summary>
        /// Copies the file or folder for this path to another location.
        /// </summary>
        /// <param name="destination">The destination path string.</param>
        /// <param name="overwrite">Overwriting policy. Default is never.</param>
        /// <param name="recursive">True if the copy should be deep and include subdirectories recursively. Default is false.</param>
        /// <returns>The destination path.</returns>
        public Path Copy(string destination, Overwrite overwrite, bool recursive) 
            => Copy(p => new Path(destination, this), overwrite, recursive);

        /// <summary>
        /// Does a copy of all files and directories in the set.
        /// </summary>
        /// <param name="pathMapping">
        /// A function that determines the destination path for each source path.
        /// If the function returns a null path, the file or directory is not copied.
        /// </param>
        /// <returns>The set</returns>
        public Path Copy(Func<Path, Path> pathMapping) => Copy(pathMapping, Overwrite.Never, false);

        /// <summary>
        /// Does a copy of all files and directories in the set.
        /// </summary>
        /// <param name="pathMapping">
        /// A function that determines the destination path for each source path.
        /// If the function returns a null path, the file or directory is not copied.
        /// </param>
        /// <param name="overwrite">Destination file overwriting policy. Default is never.</param>
        /// <param name="recursive">True if the copy should be deep and go into subdirectories recursively. Default is false.</param>
        /// <returns>The set</returns>
        public Path Copy(Func<Path, Path> pathMapping, Overwrite overwrite, bool recursive)
        {
            var result = new HashSet<string>();
            return Chain.Chain(async () =>
            {
                var tasks = new List<Task>();
                foreach (string sourcePath in Chain.Paths)
                {
                    if (sourcePath == null) continue;
                    var source = new Path(sourcePath, this);
                    Path dest = pathMapping(source);
                    if (!dest._task.IsCompleted)
                    {
                        await dest._task;
                    }
                    foreach (string destPath in ((IPathChain)dest).Paths)
                    {
                        string p = destPath;
                        if (Directory.Exists(sourcePath))
                        {
                            // source is a directory
                            tasks.Add(CopyDirectory(sourcePath, p, overwrite, recursive));
                        }
                        else
                        {
                            // source is a file
                            p = Directory.Exists(p)
                                ? SystemPath.Combine(p, SystemPath.GetFileName(sourcePath)) : p;
                            tasks.Add(CopyFile(sourcePath, p, overwrite));
                            result.Add(p);
                        }
                    }
                }
                await Task.WhenAll(tasks);
                return result;
            });
        }

        private static Task CopyFile(string srcPath, string destPath, Overwrite overwrite)
            => Task.Run(async () =>
            {
                if ((overwrite == Overwrite.Throw) && File.Exists(destPath))
                {
                    throw new InvalidOperationException($"File {destPath} already exists.");
                }
                if (((overwrite != Overwrite.Always) &&
                    ((overwrite != Overwrite.Never) || File.Exists(destPath))) &&
                    ((overwrite != Overwrite.IfNewer) || (File.Exists(destPath) &&
                    (File.GetLastWriteTime(srcPath) <= File.GetLastWriteTime(destPath))))) return;
                string dir = SystemPath.GetDirectoryName(destPath);
                if (dir == null)
                {
                    throw new InvalidOperationException($"Directory {destPath} not found.");
                }
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                using var sourceStream = new FileStream(srcPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
                using var destinationStream = new FileStream(destPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
                await sourceStream.CopyToAsync(destinationStream);
            });

        private static Task CopyDirectory(string source, string destination, Overwrite overwrite, bool recursive)
            => Task.Run(async () =>
            {
                if (!Directory.Exists(destination))
                {
                    Directory.CreateDirectory(destination);
                }
                var tasks = new List<Task>();
                if (recursive)
                {
                    tasks.AddRange(Directory.GetDirectories(source)
                        .Where(d => d != null)
                        .Select(d => CopyDirectory(
                            d,
                            SystemPath.Combine(destination, SystemPath.GetFileName(d)), overwrite, true)));
                }
                tasks.AddRange(Directory.GetFiles(source)
                    .Where(f => f != null)
                    .Select(f => CopyFile(f, SystemPath.Combine(destination, SystemPath.GetFileName(f)), overwrite)));
                await Task.WhenAll(tasks);
            });
        #endregion

        #region create directory
        /// <summary>
        /// Creates subdirectories for each directory.
        /// </summary>
        /// <param name="directoryNameGenerator">
        /// A function that returns the new directory name for each path.
        /// If the function returns null, no directory is created.
        /// </param>
        /// <returns>The set</returns>
        public Path CreateDirectories(Func<Path, string> directoryNameGenerator) 
            => CreateDirectories(p => new Path(directoryNameGenerator(p)));

        /// <summary>
        /// Creates subdirectories for each directory.
        /// </summary>
        /// <param name="directoryNameGenerator">
        /// A function that returns the new directory name for each path.
        /// If the function returns null, no directory is created.
        /// </param>
        /// <returns>The set</returns>
        public Path CreateDirectories(Func<Path, Path> directoryNameGenerator)
            => Chain.Chain(() => Chain.Paths
                    .Select(path => directoryNameGenerator(new Path(path, this)))
                    .SelectMany(dest => ((IPathChain)dest).Paths),
                path => Directory.CreateDirectory(path));

        /// <summary>
        /// Creates directories for each path in the set.
        /// </summary>
        /// <returns>The set</returns>
        public Path CreateDirectories() => CreateDirectories(p => p);

        /// <summary>
        /// Creates subdirectories for each directory.
        /// </summary>
        /// <param name="directoryName">The name of the new directory.</param>
        /// <returns>The set</returns>
        public Path CreateDirectories(string directoryName)
            => CreateDirectories(p => p.Combine(directoryName));

        /// <summary>
        /// Creates a directory for the first path in the set.
        /// </summary>
        /// <returns>The created path</returns>
        public Path CreateDirectory() => First().CreateDirectories();

        public Path CreateSubDirectory(string directoryName)
            => CreateSubDirectories(p => directoryName);

        public Path CreateSubDirectories(Func<Path, string> directoryNameGenerator)
            => Chain(
                Combine(directoryNameGenerator).Paths,
                path => Directory.CreateDirectory(path));
        #endregion

        /// <summary>
        /// Creates a file under the first path in the set.
        /// </summary>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="fileContent">The content of the file.</param>
        /// <returns>A set with the created file.</returns>
        public Path CreateFile(string fileName, string fileContent) 
            => First().CreateFiles(p => new Path(fileName, this), p => fileContent);

        /// <summary>
        /// Creates a file under the first path in the set.
        /// </summary>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="fileContent">The content of the file.</param>
        /// <param name="encoding">The encoding to use.</param>
        /// <returns>A set with the created file.</returns>
        public Path CreateFile(string fileName, string fileContent, Encoding encoding) 
            => First().CreateFiles(p => new Path(fileName, this), p => fileContent, encoding);

        /// <summary>
        /// Creates a file under the first path in the set.
        /// </summary>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="fileContent">The content of the file.</param>
        /// <returns>A set with the created file.</returns>
        public Path CreateFile(string fileName, byte[] fileContent) 
            => First().CreateFiles(p => new Path(fileName, this), p => fileContent);

        /// <summary>
        /// Creates files under each of the paths in the set.
        /// </summary>
        /// <param name="fileNameGenerator">A function that returns a file name for each path.</param>
        /// <param name="fileContentGenerator">A function that returns file content for each path.</param>
        /// <returns>The set of created files.</returns>
        public Path CreateFiles(Func<Path, Path> fileNameGenerator, Func<string, string> fileContentGenerator)
            => Chain(
                Paths
                    .Select(path => new Path(path, this))
                    .SelectMany(path => path.Combine(fileNameGenerator(path).FirstPath()).Paths),
                async path =>
                {
                    EnsureDirectoryExists(path);
                    await File.WriteAllTextAsync(path, fileContentGenerator(path));
                });

        /// <summary>
        /// Creates files under each of the paths in the set.
        /// </summary>
        /// <param name="fileNameGenerator">A function that returns a file name for each path.</param>
        /// <param name="fileContentGenerator">A function that returns file content for each path.</param>
        /// <param name="encoding">The encoding to use.</param>
        /// <returns>The set of created files.</returns>
        public Path CreateFiles(
            Func<Path, Path> fileNameGenerator,
            Func<string, string> fileContentGenerator,
            Encoding encoding)
            => Chain(
                Paths
                    .Select(path => new Path(path, this))
                    .SelectMany(path => path.Combine(fileNameGenerator(path).FirstPath()).Paths),
                async path =>
                {
                    EnsureDirectoryExists(path);
                    await File.WriteAllTextAsync(path, fileContentGenerator(path), encoding);
                });

        /// <summary>
        /// Creates files under each of the paths in the set.
        /// </summary>
        /// <param name="fileNameGenerator">A function that returns a file name for each path.</param>
        /// <param name="fileContentGenerator">A function that returns file content for each path.</param>
        /// <returns>The set of created files.</returns>
        public Path CreateFiles(
            Func<Path, Path> fileNameGenerator,
            Func<string, byte[]> fileContentGenerator)
            => Chain(
                Paths
                    .Select(path => new Path(path, this))
                    .SelectMany(path => path.Combine(fileNameGenerator(path).FirstPath()).Paths),
                async path =>
                {
                    EnsureDirectoryExists(path);
                    await File.WriteAllBytesAsync(path, fileContentGenerator(path));
                });

        /// <summary>
        /// Deletes this path from the file system.
        /// </summary>
        /// <returns>The parent path.</returns>
        public Path Delete() => Delete(false);

        /// <summary>
        /// Deletes all files and folders in the set, including non-empty directories if recursive is true.
        /// </summary>
        /// <param name="recursive">If true, also deletes the content of directories. Default is false.</param>
        /// <returns>The set of parent directories of all deleted file system entries.</returns>
        public Path Delete(bool recursive)
        {
            var result = new HashSet<string>();
            var fileTasks = new List<Task>();
            var directoryTasks = new List<Task>();
            foreach (string path in Paths) {
                if (Directory.Exists(path)) {
                    if (recursive) {
                        foreach (string file in Directory.GetFiles(path, "*", SearchOption.AllDirectories)) {
                            fileTasks.Add(Task.Run(() => File.Delete(file)));
                        }
                    }
                    directoryTasks.Add(Task.Run(() => Directory.Delete(path, recursive)));
                }
                else {
                    fileTasks.Add(Task.Run(() => File.Delete(path)));
                }
                result.Add(SystemPath.GetDirectoryName(path));
            }
            // Files can be deleted in parallel, directories should be sequential after files are done.
            var task = Task.Run(async () =>
            {
                await Task.WhenAll(fileTasks);
                foreach(Task directoryTask in directoryTasks)
                {
                    await directoryTask;
                }
            });
            return Chain(task, result, this);
        }

        /// <summary>
        /// Filters the set according to the predicate.
        /// </summary>
        /// <param name="predicate">A predicate that returns true for the entries that must be in the returned set.</param>
        /// <returns>The filtered set.</returns>
        public Path Where(Predicate<Path> predicate)
            => new Path(_task, Paths.Where(path => predicate(new Path(path, this))), this);

        /// <summary>
        /// Filters the set 
        /// </summary>
        /// <param name="extensions"></param>
        /// <returns></returns>
        public Path WhereExtensionIs(params string[] extensions) 
            => Where(
                p => {
                    string ext = p.Extension;
                    return extensions.Contains(ext) ||
                           (ext.Length > 0 && extensions.Contains(ext.Substring(1)));
                });

        /// <summary>
        /// Executes an action for each file or folder in the set.
        /// </summary>
        /// <param name="action">An action that takes the path of each entry as its parameter.</param>
        /// <returns>The set</returns>
        public Path ForEach(Action<Path> action)
            => Chain(Paths, p => action(new Path(p, this)));

        /// <summary>
        /// Executes an action for each file or folder in the set, in parallel.
        /// </summary>
        /// <param name="action">An action that takes the path of each entry as its parameter.</param>
        /// <returns>The set</returns>
        public Path ForEachParallel(Func<Path, Task> action)
            => Chain(Task.WhenAll(Paths.Select(p => action(new Path(p, this)))), Paths, Previous);

        /// <summary>
        /// Gets the subdirectories of folders in the set.
        /// </summary>
        /// <returns>The set of matching subdirectories.</returns>
        public Path Directories() => Directories(p => true, "*", false);

        /// <summary>
        /// Gets all the subdirectories of folders in the set that match the provided pattern and using the provided options.
        /// </summary>
        /// <param name="searchPattern">A search pattern such as "*.jpg". Default is "*".</param>
        /// <param name="recursive">True if subdirectories should also be searched recursively. Default is false.</param>
        /// <returns>The set of matching subdirectories.</returns>
        public Path Directories(string searchPattern, bool recursive) 
            => Directories(p => true, searchPattern, recursive);

        /// <summary>
        /// Creates a set from all the subdirectories that satisfy the specified predicate.
        /// </summary>
        /// <param name="predicate">A function that returns true if the directory should be included.</param>
        /// <returns>The set of directories that satisfy the predicate.</returns>
        public Path Directories(Predicate<Path> predicate) => Directories(predicate, "*", false);

        /// <summary>
        /// Creates a set from all the subdirectories that satisfy the specified predicate.
        /// </summary>
        /// <param name="predicate">A function that returns true if the directory should be included.</param>
        /// <param name="recursive">True if subdirectories should be recursively included.</param>
        /// <returns>The set of directories that satisfy the predicate.</returns>
        public Path Directories(Predicate<Path> predicate, bool recursive) 
            => Directories(predicate, "*", recursive);

        /// <summary>
        /// Creates a set from all the subdirectories that satisfy the specified predicate.
        /// </summary>
        /// <param name="predicate">A function that returns true if the directory should be included.</param>
        /// <param name="searchPattern">A search pattern such as "*.jpg". Default is "*".</param>
        /// <param name="recursive">True if subdirectories should be recursively included.</param>
        /// <returns>The set of directories that satisfy the predicate.</returns>
        public Path Directories(Predicate<Path> predicate, string searchPattern, bool recursive)
            => new Path(
                _task,
                Paths
                    .Select(p => Directory.GetDirectories(p, searchPattern, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
                    .SelectMany(dirs => dirs.Where(dir => predicate(new Path(dir, this)))),
                this);

        /// <summary>
        /// Gets all the files under the directories of the set.
        /// </summary>
        /// <returns>The set of files.</returns>
        public Path Files() => Files(p => true, "*", false);

        /// <summary>
        /// Gets all the files under the directories of the set that match the pattern, going recursively into subdirectories if recursive is set to true.
        /// </summary>
        /// <param name="searchPattern">A search pattern such as "*.jpg". Default is "*".</param>
        /// <param name="recursive">If true, subdirectories are explored as well. Default is false.</param>
        /// <returns>The set of files that match the pattern.</returns>
        public Path Files(string searchPattern, bool recursive) => Files(p => true, searchPattern, recursive);

        /// <summary>
        /// Creates a set from all the files under the path that satisfy the specified predicate.
        /// </summary>
        /// <param name="predicate">A function that returns true if the path should be included.</param>
        /// <returns>The set of paths that satisfy the predicate.</returns>
        public Path Files(Predicate<Path> predicate) => Files(predicate, "*", false);

        /// <summary>
        /// Creates a set from all the files under the path that satisfy the specified predicate.
        /// </summary>
        /// <param name="predicate">A function that returns true if the path should be included.</param>
        /// <param name="recursive">True if subdirectories should be recursively included.</param>
        /// <returns>The set of paths that satisfy the predicate.</returns>
        public Path Files(Predicate<Path> predicate, bool recursive) => Files(predicate, "*", recursive);

        /// <summary>
        /// Creates a set from all the files under the path that satisfy the specified predicate.
        /// </summary>
        /// <param name="predicate">A function that returns true if the path should be included.</param>
        /// <param name="searchPattern">A search pattern such as "*.jpg". Default is "*".</param>
        /// <param name="recursive">True if subdirectories should be recursively included.</param>
        /// <returns>The set of paths that satisfy the predicate.</returns>
        public Path Files(Predicate<Path> predicate, string searchPattern, bool recursive) {
            var result = new HashSet<string>();
            foreach (string file in Paths
                .Select(p => Directory.GetFiles(p, searchPattern,
                    recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
                .SelectMany(files => files.Where(f => predicate(new Path(f, this))))) {

                result.Add(file);
            }
            return new Path(result, this);
        }

        /// <summary>
        /// Gets all the files and subdirectories under the directories of the set.
        /// </summary>
        /// <returns>The set of files and folders.</returns>
        public Path FileSystemEntries() => FileSystemEntries(p => true, "*", false);

        /// <summary>
        /// Gets all the files and subdirectories under the directories of the set that match the pattern, going recursively into subdirectories if recursive is set to true.
        /// </summary>
        /// <param name="searchPattern">A search pattern such as "*.jpg". Default is "*".</param>
        /// <param name="recursive">If true, subdirectories are explored as well. Default is false.</param>
        /// <returns>The set of files and folders that match the pattern.</returns>
        public Path FileSystemEntries(string searchPattern, bool recursive) 
            => FileSystemEntries(p => true, searchPattern, recursive);

        /// <summary>
        /// Creates a set from all the files and subdirectories under the path that satisfy the specified predicate.
        /// </summary>
        /// <param name="predicate">A function that returns true if the path should be included.</param>
        /// <returns>The set of fils and subdirectories that satisfy the predicate.</returns>
        public Path FileSystemEntries(Predicate<Path> predicate) => FileSystemEntries(predicate, "*", false);

        /// <summary>
        /// Creates a set from all the files and subdirectories under the path that satisfy the specified predicate.
        /// </summary>
        /// <param name="predicate">A function that returns true if the path should be included.</param>
        /// <param name="recursive">True if subdirectories should be recursively included.</param>
        /// <returns>The set of fils and subdirectories that satisfy the predicate.</returns>
        public Path FileSystemEntries(Predicate<Path> predicate, bool recursive) 
            => FileSystemEntries(predicate, "*", recursive);

        /// <summary>
        /// Creates a set from all the files and subdirectories under the path that satisfy the specified predicate.
        /// </summary>
        /// <param name="predicate">A function that returns true if the path should be included.</param>
        /// <param name="searchPattern">A search pattern such as "*.jpg". Default is "*".</param>
        /// <param name="recursive">True if subdirectories should be recursively included.</param>
        /// <returns>The set of fils and subdirectories that satisfy the predicate.</returns>
        public Path FileSystemEntries(Predicate<Path> predicate, string searchPattern, bool recursive) {
            var result = new HashSet<string>();
            SearchOption searchOptions = recursive
                ? SearchOption.AllDirectories
                : SearchOption.TopDirectoryOnly;
            foreach (string p in Paths) {
                string[] directories = Directory.GetDirectories(p, searchPattern, searchOptions);
                foreach (string entry in directories.Where(d => predicate(new Path(d, this)))) {
                    result.Add(entry);
                }
                string[] files = Directory.GetFiles(p, searchPattern, searchOptions);
                foreach (string entry in files.Where(f => predicate(new Path(f, this)))) {
                    result.Add(entry);
                }
            }
            return new Path(result, this);
        }

        /// <summary>
        /// Gets the first path of the set.
        /// </summary>
        /// <returns>A new path from the first path of the set</returns>
        public Path First() {
            string first = Paths.FirstOrDefault();
            if (first != null) {
                return new Path(first, this);
            }
            throw new InvalidOperationException(
                "Can't get the first element of an empty collection.");
        }

        protected string FirstPath() {
            string first = Paths.FirstOrDefault();
            if (first != null) {
                return first;
            }
            throw new InvalidOperationException(
                "Can't get the first element of an empty collection.");
        }

        /// <summary>
        /// Looks for a specific text pattern in each file in the set.
        /// </summary>
        /// <param name="regularExpression">The pattern to look for</param>
        /// <param name="action">The action to execute for each match</param>
        /// <returns>The set</returns>
        public Path Grep(string regularExpression, Action<Path, Match, string> action) 
            => Grep(new Regex(regularExpression, RegexOptions.Multiline), action);

        /// <summary>
        /// Looks for a specific text pattern in each file in the set.
        /// </summary>
        /// <param name="regularExpression">The pattern to look for</param>
        /// <param name="action">The action to execute for each match</param>
        /// <returns>The set</returns>
        public Path Grep(Regex regularExpression, Action<Path, Match, string> action) {
            foreach (string path in Paths.Where(p => !Directory.Exists(p))) {
                string contents = File.ReadAllText(path);
                MatchCollection matches = regularExpression.Matches(contents);
                var p = new Path(path, this);
                foreach (Match match in matches) {
                    action(p, match, contents);
                }
            }
            return this;
        }

        /// <summary>
        /// Makes this path the current path for the application.
        /// </summary>
        /// <returns>The set.</returns>
        public Path MakeCurrent() {
            Current = this;
            return this;
        }

        /// <summary>
        /// Makes each path relative to the current path.
        /// </summary>
        /// <returns>The set of relative paths.</returns>
        public Path MakeRelative() => MakeRelativeTo(Current);

        /// <summary>
        /// Makes each path relative to the provided one.
        /// </summary>
        /// <param name="parent">The path to which the new one is relative to.</param>
        /// <returns>The set of relative paths.</returns>
        public Path MakeRelativeTo(string parent) => MakeRelativeTo(new Path(parent, this));

        /// <summary>
        /// Makes each path relative to the provided one.
        /// </summary>
        /// <param name="parent">The path to which the new one is relative to.</param>
        /// <returns>The set of relative paths.</returns>
        public Path MakeRelativeTo(Path parent) => MakeRelativeTo(p => parent);

        /// <summary>
        /// Makes each path relative to the provided one.
        /// </summary>
        /// <param name="parentGenerator">A function that returns a path to which the new one is relative to for each of the paths in the set.</param>
        /// <returns>The set of relative paths.</returns>
        public Path MakeRelativeTo(Func<Path, Path> parentGenerator)
        {
            var result = new HashSet<string>();
            foreach (string path in Paths) {
                if (!SystemPath.IsPathRooted(path)) {
                    throw new InvalidOperationException("Path must be rooted to be made relative.");
                }
                string fullPath = SystemPath.GetFullPath(path);
                string parentFull = parentGenerator(new Path(path, this)).FullPath;
                if (parentFull[^1] != SystemPath.DirectorySeparatorChar) {
                    parentFull += SystemPath.DirectorySeparatorChar;
                }
                if (!fullPath.StartsWith(parentFull)) {
                    throw new InvalidOperationException("Path must start with parent.");
                }
                result.Add(fullPath.Substring(parentFull.Length));
            }
            return new Path(result, this);
        }

        /// <summary>
        /// Maps all the paths in the set to a new set of paths using the provided mapping function.
        /// </summary>
        /// <param name="pathMapping">A function that takes a path and returns a transformed path.</param>
        /// <returns>The mapped set.</returns>
        public Path Map(Func<Path, Path> pathMapping)
        {
            var result = new HashSet<string>();
            foreach (string mapped in
                from path in Paths
                select pathMapping(new Path(path))
                into mappedPaths
                from mapped in mappedPaths.Paths select mapped) {

                result.Add(mapped);
            }
            return new Path(result, this);
        }

        /// <summary>
        /// Moves the current path in the file system.
        /// Existing files are never overwritten.
        /// </summary>
        /// <param name="destination">The destination path.</param>
        /// <returns>The destination path.</returns>
        public Path Move(string destination) => Move(p => new Path(destination, this), Overwrite.Never);

        /// <summary>
        /// Moves the current path in the file system.
        /// </summary>
        /// <param name="destination">The destination path.</param>
        /// <param name="overwrite">Overwriting policy. Default is never.</param>
        /// <returns>The destination path.</returns>
        public Path Move(string destination, Overwrite overwrite) 
            => Move(p => new Path(destination, this), overwrite);

        /// <summary>
        /// Moves all the files and folders in the set to new locations as specified by the mapping function.
        /// </summary>
        /// <param name="pathMapping">The function that maps from the current path to the new one.</param>
        /// <returns>The moved set.</returns>
        public Path Move(Func<Path, Path> pathMapping) => Move(pathMapping, Overwrite.Never);

        /// <summary>
        /// Moves all the files and folders in the set to new locations as specified by the mapping function.
        /// </summary>
        /// <param name="pathMapping">The function that maps from the current path to the new one.</param>
        /// <param name="overwrite">Overwriting policy. Default is never.</param>
        /// <returns>The moved set.</returns>
        public Path Move(Func<Path, Path> pathMapping, Overwrite overwrite)
        {
            var result = new HashSet<string>();
            foreach (string path in Paths) {
                if (path == null) continue;
                var source = new Path(path, this);
                Path dest = pathMapping(source);
                foreach (string destPath in dest.Paths) {
                    string d = destPath;
                    if (Directory.Exists(path)) {
                        MoveDirectory(path, d, overwrite);
                    }
                    else {
                        d = Directory.Exists(d)
                            ? SystemPath.Combine(d, SystemPath.GetFileName(path)) : d;
                        MoveFile(path, d, overwrite);
                    }
                    result.Add(d);
                }
            }
            return new Path(result, this);
        }

        private static bool MoveFile(string srcPath, string destPath, Overwrite overwrite)
        {
            if ((overwrite == Overwrite.Throw) && File.Exists(destPath)) {
                throw new InvalidOperationException($"File {destPath} already exists.");
            }
            if ((overwrite != Overwrite.Always) && ((overwrite != Overwrite.Never) || File.Exists(destPath)) &&
                ((overwrite != Overwrite.IfNewer) ||
                 (File.Exists(destPath) && (File.GetLastWriteTime(srcPath) <= File.GetLastWriteTime(destPath)))))
                return false;
            EnsureDirectoryExists(destPath);
            File.Delete(destPath);
            File.Move(srcPath, destPath);
            return true;
        }

        private static void EnsureDirectoryExists(string destPath) {
            string dir = SystemPath.GetDirectoryName(destPath);
            if (dir == null) {
                throw new InvalidOperationException($"Directory {destPath} not found.");
            }
            if (!Directory.Exists(dir)) {
                Directory.CreateDirectory(dir);
            }
        }

        private static bool MoveDirectory(
            string source, string destination, Overwrite overwrite) {

            bool everythingMoved = true;
            if (!Directory.Exists(destination)) {
                Directory.CreateDirectory(destination);
            }
            foreach (string subdirectory in Directory.GetDirectories(source)) {
                if (subdirectory == null) continue;
                everythingMoved &=
                    MoveDirectory(subdirectory,
                        SystemPath.Combine(destination, SystemPath.GetFileName(subdirectory)), overwrite);
            }
            foreach (string file in Directory.GetFiles(source)) {
                if (file == null) continue;
                everythingMoved &= MoveFile(file, SystemPath.Combine(destination, SystemPath.GetFileName(file)), overwrite);
            }
            if (everythingMoved) {
                Directory.Delete(source);
            }
            return everythingMoved;
        }

        /// <summary>
        /// Opens all the files in the set and hands them to the provided action.
        /// </summary>
        /// <param name="action">The action to perform on the open files.</param>
        /// <param name="mode">The FileMode to use. Default is OpenOrCreate.</param>
        /// <param name="access">The FileAccess to use. Default is ReadWrite.</param>
        /// <param name="share">The FileShare to use. Default is None.</param>
        /// <returns>The set</returns>
        public Path Open(Action<FileStream> action, FileMode mode, FileAccess access, FileShare share) {
            foreach (string path in Paths) {
                using FileStream stream = File.Open(path, mode, access, share);
                action(stream);
            }
            return this;
        }

        /// <summary>
        /// Opens all the files in the set and hands them to the provided action.
        /// </summary>
        /// <param name="action">The action to perform on the open streams.</param>
        /// <returns>The set</returns>
        public Path Open(Action<FileStream> action) 
            => Open(action, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

        /// <summary>
        /// Opens all the files in the set and hands them to the provided action.
        /// </summary>
        /// <param name="action">The action to perform on the open streams.</param>
        /// <returns>The set</returns>
        public Path Open(Action<FileStream, Path> action) 
            => Open(action, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

        /// <summary>
        /// Opens all the files in the set and hands them to the provided action.
        /// </summary>
        /// <param name="action">The action to perform on the open streams.</param>
        /// <param name="mode">The FileMode to use. Default is OpenOrCreate.</param>
        /// <param name="access">The FileAccess to use. Default is ReadWrite.</param>
        /// <param name="share">The FileShare to use. Default is None.</param>
        /// <returns>The set</returns>
        public Path Open(
            Action<FileStream, Path> action,
            FileMode mode,
            FileAccess access,
            FileShare share) {

            foreach (string path in Paths) {
                using FileStream stream = File.Open(path, mode, access, share);
                action(stream, new Path(path, this));
            }
            return this;
        }

        /// <summary>
        /// Returns the previous path collection. Use this to end a sequence of commands
        /// on a path obtained from a previous path.
        /// <example>
        /// <code>
        /// Path.Get("c:\temp")
        ///     .CreateSubDirectory("foo")
        ///         .CreateFile("bar.txt", "This is the bar file.")
        ///         .End()
        ///         .CreateFile("baz.txt", "This is the bar file.")
        ///         .End()
        ///     .End()
        ///     .CreateFile("foo.txt", "This is the foo file.");
        /// </code>
        /// </example>
        /// </summary>
        /// <returns>The previous path collection.</returns>
        public Path End() => Previous;

        /// <summary>
        /// Runs the provided process function on the content of the file
        /// for the current path and writes the result back to the file.
        /// </summary>
        /// <param name="processFunction">The processing function.</param>
        /// <returns>The set.</returns>
        public Path Process(Func<string, string> processFunction) 
            => Process((p, s) => processFunction(s));

        /// <summary>
        /// Runs the provided process function on the content of the file
        /// for the current path and writes the result back to the file.
        /// </summary>
        /// <param name="processFunction">The processing function.</param>
        /// <returns>The set.</returns>
        public Path Process(Func<Path, string, string> processFunction) {
            foreach (string path in Paths) {
                if (Directory.Exists(path)) continue;
                var p = new Path(path, this);
                string read = File.ReadAllText(path);
                File.WriteAllText(path, processFunction(p, read));
            }
            return this;
        }

        /// <summary>
        /// Runs the provided process function on the content of the file
        /// for the current path and writes the result back to the file.
        /// </summary>
        /// <param name="processFunction">The processing function.</param>
        /// <returns>The set.</returns>
        public Path Process(Func<byte[], byte[]> processFunction) 
            => Process((p, s) => processFunction(s));

        /// <summary>
        /// Runs the provided process function on the content of the file
        /// for the current path and writes the result back to the file.
        /// </summary>
        /// <param name="processFunction">The processing function.</param>
        /// <returns>The set.</returns>
        public Path Process(Func<Path, byte[], byte[]> processFunction) {
            foreach (string path in Paths) {
                if (Directory.Exists(path)) continue;
                var p = new Path(path, this);
                byte[] read = File.ReadAllBytes(path);
                File.WriteAllBytes(path, processFunction(p, read));
            }
            return this;
        }

        /// <summary>
        /// Reads all text in files in the set.
        /// </summary>
        /// <returns>The string as read from the files.</returns>
        public string Read() 
            => string.Join("",
                (from p in Paths
                    where !Directory.Exists(p)
                    select File.ReadAllText(p)));

        /// <summary>
        /// Reads all text in files in the set.
        /// </summary>
        /// <param name="encoding">The encoding to use when reading the file.</param>
        /// <returns>The string as read from the files.</returns>
        public string Read(Encoding encoding) 
            => string.Join("",
                (from p in Paths
                    where !Directory.Exists(p)
                    select File.ReadAllText(p, encoding)));

        /// <summary>
        /// Reads all text in files in the set and hands the results to the provided action.
        /// </summary>
        /// <param name="action">An action that takes the content of the file.</param>
        /// <returns>The set</returns>
        public Path Read(Action<string> action) => Read((s, p) => action(s));

        /// <summary>
        /// Reads all text in files in the set and hands the results to the provided action.
        /// </summary>
        /// <param name="action">An action that takes the content of the file.</param>
        /// <param name="encoding">The encoding to use when reading the file.</param>
        /// <returns>The set</returns>
        public Path Read(Action<string> action, Encoding encoding) 
            => Read((s, p) => action(s), encoding);

        /// <summary>
        /// Reads all text in files in the set and hands the results to the provided action.
        /// </summary>
        /// <param name="action">An action that takes the content of the file and its path.</param>
        /// <returns>The set</returns>
        public Path Read(Action<string, Path> action) {
            foreach (string path in Paths) {
                action(File.ReadAllText(path), new Path(path, this));
            }
            return this;
        }

        /// <summary>
        /// Reads all text in files in the set and hands the results to the provided action.
        /// </summary>
        /// <param name="action">An action that takes the content of the file and its path.</param>
        /// <param name="encoding">The encoding to use when reading the file.</param>
        /// <returns>The set</returns>
        public Path Read(Action<string, Path> action, Encoding encoding) {
            foreach (string path in Paths) {
                action(File.ReadAllText(path, encoding), new Path(path, this));
            }
            return this;
        }

        /// <summary>
        /// Reads all the bytes in the files in the set.
        /// </summary>
        /// <returns>The bytes from the files.</returns>
        public byte[] ReadBytes() {
            var bytes = (
                from p in Paths
                where !Directory.Exists(p)
                select File.ReadAllBytes(p)
                ).ToList();
            if (!bytes.Any()) return new byte[] {};
            if (bytes.Count() == 1) return bytes.First();
            byte[] result = new byte[bytes.Aggregate(0, (i, b) => i + b.Length)];
            int offset = 0;
            foreach (byte[] b in bytes) {
                b.CopyTo(result, offset);
                offset += b.Length;
            }
            return result;
        }

        /// <summary>
        /// Reads all the bytes in a file and hands them to the provided action.
        /// </summary>
        /// <param name="action">An action that takes an array of bytes.</param>
        /// <returns>The set</returns>
        public Path ReadBytes(Action<byte[]> action) => ReadBytes((b, p) => action(b));

        /// <summary>
        /// Reads all the bytes in a file and hands them to the provided action.
        /// </summary>
        /// <param name="action">An action that takes an array of bytes and a path.</param>
        /// <returns>The set</returns>
        public Path ReadBytes(Action<byte[], Path> action) {
            foreach (string path in Paths) {
                action(File.ReadAllBytes(path), new Path(path, this));
            }
            return this;
        }

        /// <summary>
        /// The tokens for the first path.
        /// </summary>
        public string[] Tokens {
            get {
                var tokens = new List<string>();
                string current = FirstPath();
                while (!string.IsNullOrEmpty(current)) {
                    tokens.Add(SystemPath.GetFileName(current));
                    current = SystemPath.GetDirectoryName(current);
                }
                tokens.Reverse();
                return tokens.ToArray();
            }
        }

        public override string ToString() => string.Join(", ", Paths);

        public string[] ToStringArray() => Paths.ToArray();

        /// <summary>
        /// Adds several paths to the current one and makes one set out of the result.
        /// </summary>
        /// <param name="paths">The paths to add to the current set.</param>
        /// <returns>The composite set.</returns>
        public Path Add(params string[] paths) => new Path(paths.Union(Paths), this);

        /// <summary>
        /// Adds several paths to the current one and makes one set out of the result.
        /// </summary>
        /// <param name="paths">The paths to add to the current set.</param>
        /// <returns>The composite set.</returns>
        public Path Add(params Path[] paths) 
            => new Path(paths.SelectMany(p => p.Paths).Union(Paths), this);

        /// <summary>
        /// Gets all files under this path.
        /// </summary>
        /// <returns>The collection of file paths.</returns>
        public Path AllFiles() => Files("*", true);

        /// <summary>
        /// The attributes for the file for the first path in the collection.
        /// </summary>
        /// <returns>The attributes</returns>
        public FileAttributes Attributes() => File.GetAttributes(FirstPath());

        /// <summary>
        /// The attributes for the file for the first path in the collection.
        /// </summary>
        /// <param name="action">An action to perform on the attributes of each file.</param>
        /// <returns>The attributes</returns>
        public Path Attributes(Action<FileAttributes> action) 
            => Attributes((p, fa) => action(fa));

        /// <summary>
        /// The attributes for the file for the first path in the collection.
        /// </summary>
        /// <param name="action">An action to perform on the attributes of each file.</param>
        /// <returns>The attributes</returns>
        public Path Attributes(Action<Path, FileAttributes> action) {
            foreach (string path in Paths.Where(path => !Directory.Exists(path))) {
                action(new Path(path, this), File.GetAttributes(path));
            }
            return this;
        }

        /// <summary>
        /// Sets attributes on all files in the set.
        /// </summary>
        /// <param name="attributes">The attributes to set.</param>
        /// <returns>The set</returns>
        public Path Attributes(FileAttributes attributes) => Attributes(p => attributes);

        /// <summary>
        /// Sets attributes on all files in the set.
        /// </summary>
        /// <param name="attributeFunction">A function that gives the attributes to set for each path.</param>
        /// <returns>The set</returns>
        public Path Attributes(Func<Path, FileAttributes> attributeFunction) {
            foreach (string p in Paths) {
                File.SetAttributes(p, attributeFunction(new Path(p, this)));
            }
            return this;
        }

        /// <summary>
        /// Gets the creation time of the first path in the set
        /// </summary>
        /// <returns>The creation time</returns>
        public DateTime CreationTime() {
            string firstPath = FirstPath();
            return Directory.Exists(firstPath)
                ? Directory.GetCreationTime(firstPath)
                : File.GetCreationTime(firstPath);
        }

        /// <summary>
        /// Sets the creation time across the set.
        /// </summary>
        /// <param name="creationTime">The time to set.</param>
        /// <returns>The set</returns>
        public Path CreationTime(DateTime creationTime) => CreationTime(p => creationTime);

        /// <summary>
        /// Sets the creation time across the set.
        /// </summary>
        /// <param name="creationTimeFunction">A function that returns the new creation time for each path.</param>
        /// <returns>The set</returns>
        public Path CreationTime(Func<Path, DateTime> creationTimeFunction) {
            foreach (string path in Paths) {
                DateTime t = creationTimeFunction(new Path(path, this));
                if (Directory.Exists(path)) {
                    Directory.SetCreationTime(path, t);
                }
                else {
                    File.SetCreationTime(path, t);
                }
            }
            return this;
        }

        /// <summary>
        /// Gets the UTC creation time of the first path in the set
        /// </summary>
        /// <returns>The UTC creation time</returns>
        public DateTime CreationTimeUtc() {
            string firstPath = FirstPath();
            return Directory.Exists(firstPath)
                ? Directory.GetCreationTimeUtc(firstPath)
                : File.GetCreationTimeUtc(firstPath);
        }

        /// <summary>
        /// Sets the UTC creation time across the set.
        /// </summary>
        /// <param name="creationTimeUtc">The time to set.</param>
        /// <returns>The set</returns>
        public Path CreationTimeUtc(DateTime creationTimeUtc) => CreationTimeUtc(p => creationTimeUtc);

        /// <summary>
        /// Sets the UTC creation time across the set.
        /// </summary>
        /// <param name="creationTimeFunctionUtc">A function that returns the new time for each path.</param>
        /// <returns>The set</returns>
        public Path CreationTimeUtc(Func<Path, DateTime> creationTimeFunctionUtc) {
            foreach (string path in Paths) {
                DateTime t = creationTimeFunctionUtc(new Path(path, this));
                if (Directory.Exists(path)) {
                    Directory.SetCreationTimeUtc(path, t);
                }
                else {
                    File.SetCreationTimeUtc(path, t);
                }
            }
            return this;
        }

        /// <summary>
        /// Tests the existence of the paths in the set.
        /// </summary>
        /// <returns>True if all paths exist</returns>
        public bool Exists =>Paths.All(path => (Directory.Exists(path) || File.Exists(path)));

        /// <summary>
        /// Gets the last access time of the first path in the set
        /// </summary>
        /// <returns>The last access time</returns>
        public DateTime LastAccessTime() {
            string firstPath = FirstPath();
            return Directory.Exists(firstPath)
                ? Directory.GetLastAccessTime(firstPath)
                : File.GetLastAccessTime(firstPath);
        }

        /// <summary>
        /// Sets the last access time across the set.
        /// </summary>
        /// <param name="lastAccessTime">The time to set.</param>
        /// <returns>The set</returns>
        public Path LastAccessTime(DateTime lastAccessTime) => LastAccessTime(p => lastAccessTime);

        /// <summary>
        /// Sets the last access time across the set.
        /// </summary>
        /// <param name="lastAccessTimeFunction">A function that returns the new time for each path.</param>
        /// <returns>The set</returns>
        public Path LastAccessTime(Func<Path, DateTime> lastAccessTimeFunction) {
            foreach (string path in Paths) {
                DateTime t = lastAccessTimeFunction(new Path(path, this));
                if (Directory.Exists(path)) {
                    Directory.SetLastAccessTime(path, t);
                }
                else {
                    File.SetLastAccessTime(path, t);
                }
            }
            return this;
        }

        /// <summary>
        /// Gets the last access UTC time of the first path in the set
        /// </summary>
        /// <returns>The last access UTC time</returns>
        public DateTime LastAccessTimeUtc() {
            string firstPath = FirstPath();
            return Directory.Exists(firstPath)
                ? Directory.GetLastAccessTimeUtc(firstPath)
                : File.GetLastAccessTimeUtc(firstPath);
        }

        /// <summary>
        /// Sets the UTC last access time across the set.
        /// </summary>
        /// <param name="lastAccessTimeUtc">The time to set.</param>
        /// <returns>The set</returns>
        public Path LastAccessTimeUtc(DateTime lastAccessTimeUtc) => LastAccessTimeUtc(p => lastAccessTimeUtc);

        /// <summary>
        /// Sets the UTC last access time across the set.
        /// </summary>
        /// <param name="lastAccessTimeFunctionUtc">A function that returns the new time for each path.</param>
        /// <returns>The set</returns>
        public Path LastAccessTimeUtc(Func<Path, DateTime> lastAccessTimeFunctionUtc) {
            foreach (string path in Paths) {
                DateTime t = lastAccessTimeFunctionUtc(new Path(path, this));
                if (Directory.Exists(path)) {
                    Directory.SetLastAccessTimeUtc(path, t);
                }
                else {
                    File.SetLastAccessTimeUtc(path, t);
                }
            }
            return this;
        }

        /// <summary>
        /// Gets the last write time of the first path in the set
        /// </summary>
        /// <returns>The last write time</returns>
        public DateTime LastWriteTime() {
            string firstPath = FirstPath();
            return Directory.Exists(firstPath)
                ? Directory.GetLastWriteTime(firstPath)
                : File.GetLastWriteTime(firstPath);
        }

        /// <summary>
        /// Sets the last write time across the set.
        /// </summary>
        /// <param name="lastWriteTime">The time to set.</param>
        /// <returns>The set</returns>
        public Path LastWriteTime(DateTime lastWriteTime) => LastWriteTime(p => lastWriteTime);

        /// <summary>
        /// Sets the last write time across the set.
        /// </summary>
        /// <param name="lastWriteTimeFunction">A function that returns the new time for each path.</param>
        /// <returns>The set</returns>
        public Path LastWriteTime(Func<Path, DateTime> lastWriteTimeFunction) {
            foreach (string path in Paths) {
                DateTime t = lastWriteTimeFunction(new Path(path, this));
                if (Directory.Exists(path)) {
                    Directory.SetLastWriteTime(path, t);
                }
                else {
                    File.SetLastWriteTime(path, t);
                }
            }
            return this;
        }

        /// <summary>
        /// Gets the last write UTC time of the first path in the set
        /// </summary>
        /// <returns>The last write UTC time</returns>
        public DateTime LastWriteTimeUtc() {
            string firstPath = FirstPath();
            return Directory.Exists(firstPath)
                ? Directory.GetLastWriteTimeUtc(firstPath)
                : File.GetLastWriteTimeUtc(firstPath);
        }

        /// <summary>
        /// Sets the UTC last write time across the set.
        /// </summary>
        /// <param name="lastWriteTimeUtc">The time to set.</param>
        /// <returns>The set</returns>
        public Path LastWriteTimeUtc(DateTime lastWriteTimeUtc) => LastWriteTimeUtc(p => lastWriteTimeUtc);

        /// <summary>
        /// Sets the UTC last write time across the set.
        /// </summary>
        /// <param name="lastWriteTimeFunctionUtc">A function that returns the new time for each path.</param>
        /// <returns>The set</returns>
        public Path LastWriteTimeUtc(Func<Path, DateTime> lastWriteTimeFunctionUtc) {
            foreach (string path in Paths) {
                DateTime t = lastWriteTimeFunctionUtc(new Path(path, this));
                if (Directory.Exists(path)) {
                    Directory.SetLastWriteTimeUtc(path, t);
                }
                else {
                    File.SetLastWriteTimeUtc(path, t);
                }
            }
            return this;
        }

        /// <summary>
        /// Goes up the specified number of levels on each path in the set.
        /// Never goes above the root of the drive.
        /// </summary>
        /// <returns>The new set</returns>
        public Path Up() => Up(1);

        /// <summary>
        /// Goes up the specified number of levels on each path in the set.
        /// Never goes above the root of the drive.
        /// </summary>
        /// <param name="levels">The number of levels to go up.</param>
        /// <returns>The new set</returns>
        public Path Up(int levels) {
            var result = new HashSet<string>();
            foreach (string path in Paths) {
                string str = path;
                for (int i = 0; i < levels; i++) {
                    string strUp = SystemPath.GetDirectoryName(str);
                    if (strUp == null) break;
                    str = strUp;
                }
                result.Add(str);
            }
            return new Path(result, this);
        }

        /// <summary>
        /// Writes to all files in the set using UTF8.
        /// </summary>
        /// <param name="text">The text to write.</param>
        /// <returns>The set</returns>
        public Path Write(string text) => Write(p => text, false);

        /// <summary>
        /// Writes to all files in the set using UTF8.
        /// </summary>
        /// <param name="text">The text to write.</param>
        /// <param name="append">True if the text should be appended to the existing content. Default is false.</param>
        /// <returns>The set</returns>
        public Path Write(string text, bool append) => Write(p => text, append);

        /// <summary>
        /// Writes to all files in the set.
        /// </summary>
        /// <param name="text">The text to write.</param>
        /// <param name="encoding">The encoding to use.</param>
        /// <returns>The set</returns>
        public Path Write(string text, Encoding encoding) => Write(p => text, encoding, false);

        /// <summary>
        /// Writes to all files in the set.
        /// </summary>
        /// <param name="text">The text to write.</param>
        /// <param name="encoding">The encoding to use.</param>
        /// <param name="append">True if the text should be appended to the existing content. Default is false.</param>
        /// <returns>The set</returns>
        public Path Write(string text, Encoding encoding, bool append) => Write(p => text, encoding, append);

        /// <summary>
        /// Writes to all files in the set.
        /// </summary>
        /// <param name="textFunction">A function that returns the text to write for each path.</param>
        /// <param name="append">True if the text should be appended to the existing content. Default is false.</param>
        /// <returns>The set</returns>
        public Path Write(Func<Path, string> textFunction, bool append) => Write(textFunction, Encoding.GetEncoding("utf-8"), append);

        /// <summary>
        /// Writes to all files in the set.
        /// </summary>
        /// <param name="textFunction">A function that returns the text to write for each path.</param>
        /// <param name="encoding">The encoding to use.</param>
        /// <param name="append">True if the text should be appended to the existing content. Default is false.</param>
        /// <returns>The set</returns>
        public Path Write(Func<Path, string> textFunction, Encoding encoding, bool append) {
            foreach (string p in Paths) {
                EnsureDirectoryExists(p);
                if (append) {
                    File.AppendAllText(p, textFunction(new Path(p, this)), encoding);
                }
                else {
                    File.WriteAllText(p, textFunction(new Path(p, this)), encoding);
                }
            }
            return this;
        }

        /// <summary>
        /// Writes to all files in the set.
        /// </summary>
        /// <param name="bytes">The byte array to write.</param>
        /// <returns>The set</returns>
        public Path Write(byte[] bytes) => Write(p => bytes);

        /// <summary>
        /// Writes to all files in the set.
        /// </summary>
        /// <param name="byteFunction">A function that returns a byte array to write for each path.</param>
        /// <returns>The set</returns>
        public Path Write(Func<Path, byte[]> byteFunction) {
            foreach (string p in Paths) {
                EnsureDirectoryExists(p);
                File.WriteAllBytes(p, byteFunction(new Path(p, this)));
            }
            return this;
        }
    }
}