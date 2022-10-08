// Copyright © 2010-2021 Bertrand Le Roy.  All Rights Reserved.
// This code released under the terms of the 
// MIT License http://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Threading.Tasks;

namespace Fluent.IO.Async.Windows
{
    public static class WindowsExtensions
    {
        /// <summary>
        /// Decrypts all files in the set.
        /// </summary>
        /// <returns>The set</returns>
        public static Path Decrypt(this Path path)
        {
            return new Path(DecryptImpl(path.Paths), path);

            async IAsyncEnumerable<string> DecryptImpl(IAsyncEnumerable<string> paths)
            {
                await foreach (string p in paths.WithCancellation(path.CancellationToken).ConfigureAwait(false))
                {
                    if (!Directory.Exists(p))
                    {
                        File.Decrypt(p);
                    }
                    yield return p;
                }
            }
        }

        /// <summary>
        /// Encrypts all files in the set.
        /// </summary>
        /// <returns>The set</returns>
        public static Path Encrypt(this Path path)
        {
            return new Path(DecryptImpl(path.Paths), path);

            async IAsyncEnumerable<string> DecryptImpl(IAsyncEnumerable<string> paths)
            {
                await foreach (string p in paths.WithCancellation(path.CancellationToken).ConfigureAwait(false))
                {
                    if (!Directory.Exists(p))
                    {
                        File.Encrypt(p);
                    }
                    yield return p;
                }
            }
        }

        /// <summary>
        /// The access control security information for the first path in the collection.
        /// </summary>
        /// <returns>The security information</returns>
        public static async ValueTask<FileSystemSecurity> AccessControl(this Path path)
        {
            string firstPath = await path.FirstPath();
            if (firstPath == null) throw new InvalidOperationException("Can't get access control from an empty path.");
            return Directory.Exists(firstPath)
                ? new DirectoryInfo(firstPath).GetAccessControl()
                : new FileInfo(firstPath).GetAccessControl() as FileSystemSecurity;
        }

        /// <summary>
        /// The access control security information for the first path in the collection.
        /// </summary>
        /// <param name="action">An action that gets called for each path in the set.</param>
        /// <returns>The set</returns>
        public static Path AccessControl(this Path path, Action<FileSystemSecurity> action)
            => AccessControl(path, (p, fss) => action(fss));

        /// <summary>
        /// Calls an action with the access control security information for paths in the collection.
        /// </summary>
        /// <param name="action">An action that gets called for each path in the set.</param>
        /// <returns>The set</returns>
        public static Path AccessControl(this Path path, Action<Path, FileSystemSecurity> action)
        {
            return new Path(AccessControlImpl(path.Paths), path);

            async IAsyncEnumerable<string> AccessControlImpl(IAsyncEnumerable<string> paths)
            {
                await foreach(string p in paths.WithCancellation(path.CancellationToken).ConfigureAwait(false))
                {
                    action(new Path(p, path),
                        Directory.Exists(p)
                            ? new DirectoryInfo(p).GetAccessControl()
                            : new FileInfo(p).GetAccessControl() as FileSystemSecurity);
                    yield return p;
                }
            }
        }

        /// <summary>
        /// Calls an action with the access control security information for paths in the collection.
        /// </summary>
        /// <param name="action">An action that gets called for each path in the set.</param>
        /// <returns>The set</returns>
        public static Path AccessControl(
            this Path path,
            Func<Path, FileSystemSecurity, ValueTask> action)
        {
            return new Path(AccessControlImpl(path.Paths), path);

            async IAsyncEnumerable<string> AccessControlImpl(IAsyncEnumerable<string> paths)
            {
                await foreach (string p in paths.WithCancellation(path.CancellationToken).ConfigureAwait(false))
                {
                    await action(new Path(p, path),
                        Directory.Exists(p)
                            ? new DirectoryInfo(p).GetAccessControl()
                            : new FileInfo(p).GetAccessControl() as FileSystemSecurity);
                    yield return p;
                }
            }
        }

        /// <summary>
        /// Sets the access control security on all files and directories in the set.
        /// </summary>
        /// <param name="security">The security to apply.</param>
        /// <returns>The set</returns>
        public static Path AccessControl(this Path path, FileSystemSecurity security) =>
            AccessControl(path, p => security);

        /// <summary>
        /// Sets the access control security on all files and directories in the set.
        /// </summary>
        /// <param name="securityFunction">A function that returns the security for each path.</param>
        /// <returns>The set</returns>
        public static Path AccessControl(
            this Path path,
            Func<Path, FileSystemSecurity> securityFunction)
        {
            return new Path(AccessControlImpl(path.Paths), path);

            async IAsyncEnumerable<string> AccessControlImpl(IAsyncEnumerable<string> paths)
            {
                await foreach (string p in paths.WithCancellation(path.CancellationToken).ConfigureAwait(false))
                {
                    if (Directory.Exists(p))
                    {
                        if (securityFunction(new Path(p, path)) is DirectorySecurity dirSecurity)
                        {
                            new DirectoryInfo(p).SetAccessControl(dirSecurity);
                        }
                    }
                    else
                    {
                        if (securityFunction(new Path(p, path)) is FileSecurity fileSecurity)
                        {
                            new FileInfo(p).SetAccessControl(fileSecurity);
                        }
                    }
                    yield return p;
                }
            }
        }

        /// <summary>
        /// Sets the access control security on all files and directories in the set.
        /// </summary>
        /// <param name="securityFunction">A function that returns the security for each path.</param>
        /// <returns>The set</returns>
        public static Path AccessControl(
            this Path path,
            Func<Path, ValueTask<FileSystemSecurity>> securityFunction)
        {
            return new Path(AccessControlImpl(path.Paths), path);

            async IAsyncEnumerable<string> AccessControlImpl(IAsyncEnumerable<string> paths)
            {
                await foreach (string p in paths.WithCancellation(path.CancellationToken).ConfigureAwait(false))
                {
                    if (Directory.Exists(p))
                    {
                        if (await securityFunction(new Path(p, path)) is DirectorySecurity dirSecurity)
                        {
                            new DirectoryInfo(p).SetAccessControl(dirSecurity);
                        }
                    }
                    else
                    {
                        if (await securityFunction(new Path(p, path)) is FileSecurity fileSecurity)
                        {
                            new FileInfo(p).SetAccessControl(fileSecurity);
                        }
                    }
                    yield return p;
                }
            }
        }
    }
}
