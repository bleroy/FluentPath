// Copyright © 2010-2015 Bertrand Le Roy.  All Rights Reserved.
// This code released under the terms of the 
// MIT License http://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace Fluent.IO.Async.Zip
{
    /// <summary>
    /// A set of FluentPath extensions that surface Compression features.
    /// </summary>
    public static class ZipExtensions
    {
        /// <summary>
        /// Unzips all files in the path.
        /// </summary>
        /// <param name="path">The zip files.</param>
        /// <param name="target">The directory where the files must be unzipped.</param>
        /// <returns>The uncompressed files and folders.</returns>
        public static Path Unzip(this Path path, Path target) =>
            path.Open(async (s, p) =>
                await target.ForEach(async t =>
                    new ZipArchive(s, ZipArchiveMode.Read)
                        .ExtractToDirectory(await t.FullPath())));

        /// <summary>
        /// Unzips all files in the path.
        /// </summary>
        /// <param name="path">The zip files.</param>
        /// <param name="target">A function that maps the archive's path to a target path.</param>
        /// <returns>The uncompressed files and folders.</returns>
        public static Path Unzip(this Path path, Func<Path, Path> target) =>
            path.Open(async (s, p) =>
                await target(p).ForEach(async t =>
                    new ZipArchive(s, ZipArchiveMode.Read)
                        .ExtractToDirectory(await t.FullPath())));

        /// <summary>
        /// Unzips all files in the path.
        /// </summary>
        /// <param name="path">The zip files.</param>
        /// <param name="target">A function that maps the archive's path to a target path.</param>
        /// <returns>The uncompressed files and folders.</returns>
        public static Path Unzip(this Path path, Func<Path, ValueTask<Path>> target) =>
            path.Open(async (s, p) =>
                await (await target(p)).ForEach(async t =>
                    new ZipArchive(s, ZipArchiveMode.Read)
                        .ExtractToDirectory(await t.FullPath())));

        /// <summary>
        /// Unzips all files in the path.
        /// </summary>
        /// <param name="path">The zip files.</param>
        /// <param name="unzipAction">An action that handles the unzipping of each file.</param>
        /// <returns>The original path object</returns>
        public static Path Unzip(this Path path, Action<string, Stream> unzipAction) =>
            path.Open((s, p) => Unzip(s, unzipAction));

        /// <summary>
        /// Unzips all files in the path.
        /// </summary>
        /// <param name="path">The zip files.</param>
        /// <param name="unzipAction">An action that handles the unzipping of each file.</param>
        /// <returns>The original path object</returns>
        public static Path Unzip(this Path path, Func<string, Stream, ValueTask> unzipAction) =>
            path.Open(async (s, p) => await Unzip(s, unzipAction));

        /// <summary>
        /// Unzips a byte array and calls an action for each unzipped file.
        /// </summary>
        /// <param name="zip">The zip byte array.</param>
        /// <param name="unzipAction">The action to perform with each unzipped file.</param>
        public static void Unzip(this byte[] zip, Action<string, Stream> unzipAction) =>
            Unzip(new MemoryStream(zip, false), unzipAction);

        /// <summary>
        /// Unzips a byte array and calls an action for each unzipped file.
        /// </summary>
        /// <param name="zip">The zip byte array.</param>
        /// <param name="unzipAction">The action to perform with each unzipped file.</param>
        public static async ValueTask Unzip(
            this byte[] zip,
            Func<string, Stream, ValueTask> unzipAction) =>
            await Unzip(new MemoryStream(zip, false), unzipAction);

        /// <summary>
        /// Unzips a stream and calls an action for each unzipped file.
        /// </summary>
        /// <param name="zip">The zip byte array.</param>
        /// <param name="unzipAction">The action to perform with each unzipped file.</param>
        public static void Unzip(this Stream zip, Action<string, Stream> unzipAction)
        {
            using var zipArchive = new ZipArchive(zip, ZipArchiveMode.Read);
            foreach (ZipArchiveEntry zipEntry in zipArchive.Entries)
            {
                unzipAction(zipEntry.FullName, zipEntry.Open());
            }
        }

        /// <summary>
        /// Unzips a stream and calls an action for each unzipped file.
        /// </summary>
        /// <param name="zip">The zip byte array.</param>
        /// <param name="unzipAction">The action to perform with each unzipped file.</param>
        public static async ValueTask Unzip(
            this Stream zip,
            Func<string, Stream, ValueTask> unzipAction)
        {
            using var zipArchive = new ZipArchive(zip, ZipArchiveMode.Read);
            foreach (ZipArchiveEntry zipEntry in zipArchive.Entries)
            {
                await unzipAction(zipEntry.FullName, zipEntry.Open());
            }
        }

        /// <summary>
        /// Unzips all files in the path.
        /// </summary>
        /// <param name="path">The zip files.</param>
        /// <param name="unzipAction">An action that handles the unzipping of each file.</param>
        /// <returns>The original path object</returns>
        public static Path Unzip(this Path path, Action<string, byte[]> unzipAction) =>
            path.Open((s, p) => Unzip(s, unzipAction));

        /// <summary>
        /// Unzips all files in the path.
        /// </summary>
        /// <param name="path">The zip files.</param>
        /// <param name="unzipAction">An action that handles the unzipping of each file.</param>
        /// <returns>The original path object</returns>
        public static Path Unzip(this Path path, Func<string, byte[], ValueTask> unzipAction) =>
            path.Open(async (s, p) => await Unzip(s, unzipAction));

        /// <summary>
        /// Unzips a byte array and calls an action for each unzipped file.
        /// </summary>
        /// <param name="zip">The zip byte array.</param>
        /// <param name="unzipAction">The action to perform with each unzipped file.</param>
        public static async ValueTask Unzip(this byte[] zip, Action<string, byte[]> unzipAction) =>
            await Unzip(new MemoryStream(zip, false), unzipAction);

        /// <summary>
        /// Unzips a byte array and calls an action for each unzipped file.
        /// </summary>
        /// <param name="zip">The zip byte array.</param>
        /// <param name="unzipAction">The action to perform with each unzipped file.</param>
        public static async ValueTask Unzip(
            this byte[] zip,
            Func<string, byte[], ValueTask> unzipAction) =>
            await Unzip(new MemoryStream(zip, false), unzipAction);

        /// <summary>
        /// Unzips a stream and calls an action for each unzipped file.
        /// </summary>
        /// <param name="zip">The zip byte array.</param>
        /// <param name="unzipAction">The action to perform with each unzipped file.</param>
        public static async ValueTask Unzip(this Stream zip, Action<string, byte[]> unzipAction)
        {
            using var zipArchive = new ZipArchive(zip, ZipArchiveMode.Read);
            foreach (ZipArchiveEntry zipEntry in zipArchive.Entries)
            {
                var output = new MemoryStream();
                await zipEntry.Open().CopyToAsync(output);
                unzipAction(zipEntry.FullName, output.ToArray());
            }
        }

        /// <summary>
        /// Unzips a stream and calls an action for each unzipped file.
        /// </summary>
        /// <param name="zip">The zip byte array.</param>
        /// <param name="unzipAction">The action to perform with each unzipped file.</param>
        public static async ValueTask Unzip(this Stream zip, Func<string, byte[], ValueTask> unzipAction)
        {
            using var zipArchive = new ZipArchive(zip, ZipArchiveMode.Read);
            foreach (ZipArchiveEntry zipEntry in zipArchive.Entries)
            {
                var output = new MemoryStream();
                await zipEntry.Open().CopyToAsync(output);
                await unzipAction(zipEntry.FullName, output.ToArray());
            }
        }

        /// <summary>
        /// Zips all files in the path to the target.
        ///     <remarks>
        ///     When a directory is being pointed to as a source, all contents are recursively added.
        ///     Individual files are added at the root, and directories are added at the root under their name.
        ///     To have more control over the path of the files in the zip, use the overload.
        ///     </remarks>
        /// </summary>
        /// <param name="target">
        /// The path of the target zip file.
        /// If target has more than one file, only the first one is used.
        /// </param>
        /// <param name="path">The files to compress.</param>
        /// <returns>The zipped path.</returns>
        public static Path Zip(this Path target, Path path)
        {
            return new Path(ZipImpl(target.Paths), target);

            async IAsyncEnumerable<string> ZipImpl(IAsyncEnumerable<string> paths)
            {
                var files = await path.AllFiles().MakeRelativeTo(path);
                await foreach (string targetPath in paths.WithCancellation(target.CancellationToken).ConfigureAwait(false))
                {
                    await ZipToStream(
                        files,
                        filePath => File.OpenRead(filePath),
                        File.OpenWrite(targetPath));
                    yield return targetPath;
                }
            }
        }

        /// <summary>
        /// Zips the contents of a dictionary of paths to byte arrays.
        /// </summary>
        /// <param name="target">The path of the zip file to build.</param>
        /// <param name="contents">The contents to zip.</param>
        /// <returns>The path of the zipped file.</returns>
        public static Path Zip(this Path target, IDictionary<string, byte[]> contents)
        {
            return new Path(ZipImpl(target.Paths), target);

            async IAsyncEnumerable<string> ZipImpl(IAsyncEnumerable<string> paths)
            {
                await foreach(string targetPath in paths.WithCancellation(target.CancellationToken).ConfigureAwait(false))
                {
                    await ZipToStream(
                        new Path(contents.Keys, target),
                        p => new MemoryStream(contents[p]),
                        File.OpenWrite(targetPath));
                    yield return targetPath;
                }
            }
        }

        /// <summary>
        /// Zips the files in the path into a byte array.
        /// </summary>
        /// <param name="filesToZip">The list of files to zip</param>
        /// <returns>The byte array for the zip</returns>
        public static async ValueTask<byte[]> Zip(this Path filesToZip)
        {
            var output = new MemoryStream();
            await ZipToStream(filesToZip, p => File.OpenRead(p), output);
            return output.ToArray();
        }

        public static async ValueTask ZipToStream(
            Path zipPaths,
            Func<string, Stream> zipPathToContent,
            Stream output)
        {
            using var zipArchive = new ZipArchive(output, ZipArchiveMode.Create);
            await foreach (string path in zipPaths.WithCancellation(zipPaths.CancellationToken).ConfigureAwait(false))
            {
                ZipArchiveEntry entry = zipArchive.CreateEntry(path, CompressionLevel.Optimal);
                using Stream writer = entry.Open();
                using Stream reader = zipPathToContent(path);
                await reader.CopyToAsync(writer);
                writer.Close();
                reader.Close();
            }
        }
    }
}
