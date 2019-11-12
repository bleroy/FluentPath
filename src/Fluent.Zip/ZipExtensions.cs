// Copyright © 2010-2015 Bertrand Le Roy.  All Rights Reserved.
// This code released under the terms of the 
// MIT License http://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Path = Fluent.IO.Path;

namespace Fluent.Zip
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
        public static Path Unzip(this Path path, Path target)
        {
            path.Open((s, p) => target.ForEach(t => new ZipArchive(s, ZipArchiveMode.Read).ExtractToDirectory(t.FullPath)));
            return target;
        }

        /// <summary>
        /// Unzips all files in the path.
        /// </summary>
        /// <param name="path">The zip files.</param>
        /// <param name="unzipAction">An action that handles the unzipping of each file.</param>
        /// <returns>The original path object</returns>
        public static Path Unzip(this Path path, Action<string, Stream> unzipAction)
        {
            path.Open((s, p) => Unzip(s, unzipAction));
            return path;
        }

        /// <summary>
        /// Unzips a byte array and calls an action for each unzipped file.
        /// </summary>
        /// <param name="zip">The zip byte array.</param>
        /// <param name="unzipAction">The action to perform with each unzipped file.</param>
        public static void Unzip(byte[] zip, Action<string, Stream> unzipAction)
        {
            Unzip(new MemoryStream(zip, false), unzipAction);
        }

        /// <summary>
        /// Unzips a stream and calls an action for each unzipped file.
        /// </summary>
        /// <param name="zip">The zip byte array.</param>
        /// <param name="unzipAction">The action to perform with each unzipped file.</param>
        public static void Unzip(Stream zip, Action<string, Stream> unzipAction)
        {
            using var zipArchive = new ZipArchive(zip, ZipArchiveMode.Read);
            foreach (ZipArchiveEntry zipEntry in zipArchive.Entries)
            {
                unzipAction(zipEntry.FullName, zipEntry.Open());
            }
        }

        /// <summary>
        /// Unzips all files in the path.
        /// </summary>
        /// <param name="path">The zip files.</param>
        /// <param name="unzipAction">An action that handles the unzipping of each file.</param>
        /// <returns>The original path object</returns>
        public static Path Unzip(this Path path, Action<string, byte[]> unzipAction)
        {
            path.Open((s, p) => Unzip(s, unzipAction));
            return path;
        }

        /// <summary>
        /// Unzips a byte array and calls an action for each unzipped file.
        /// </summary>
        /// <param name="zip">The zip byte array.</param>
        /// <param name="unzipAction">The action to perform with each unzipped file.</param>
        public static void Unzip(byte[] zip, Action<string, byte[]> unzipAction)
        {
            Unzip(new MemoryStream(zip, false), unzipAction);
        }

        /// <summary>
        /// Unzips a stream and calls an action for each unzipped file.
        /// </summary>
        /// <param name="zip">The zip byte array.</param>
        /// <param name="unzipAction">The action to perform with each unzipped file.</param>
        public static void Unzip(Stream zip, Action<string, byte[]> unzipAction)
        {
            using var zipArchive = new ZipArchive(zip, ZipArchiveMode.Read);
            foreach (ZipArchiveEntry zipEntry in zipArchive.Entries)
            {
                var output = new MemoryStream();
                zipEntry.Open().CopyTo(output);
                unzipAction(zipEntry.FullName, output.ToArray());
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
            Dictionary<Path, Path> files = path.AllFiles().ToDictionary(p => p.MakeRelativeTo(path));
            ZipToStream(
                new Path(files.Keys.Select(p => (string)p)),
                p => File.OpenRead((string)files[p]),
                File.OpenWrite((string)target));
            return target;
        }

        /// <summary>
        /// Zips the contents of a dictionary of paths to byte arrays.
        /// </summary>
        /// <param name="target">The path of the zip file to build.</param>
        /// <param name="contents">The contents to zip.</param>
        /// <returns>The path of the zipped file.</returns>
        public static Path Zip(this Path target, IDictionary<Path, byte[]> contents)
        {
            ZipToStream(
                new Path(contents.Keys.Select(p => (string)p)),
                p => new MemoryStream(contents[p]),
                File.OpenWrite((string)target));
            return target;
        }

        /// <summary>
        /// Zips the files in the path into a byte array.
        /// </summary>
        /// <param name="filesToZip">The list of files to zip</param>
        /// <returns>The byte array for the zip</returns>
        public static byte[] Zip(this Path filesToZip)
        {
            var output = new MemoryStream();
            ZipToStream(filesToZip, p => File.OpenRead((string)p), output);
            return output.ToArray();
        }

        private static void ZipToStream(Path zipPaths, Func<Path, Stream> zipPathToContent, Stream output)
        {
            using var zipArchive = new ZipArchive(output, ZipArchiveMode.Create);
            foreach (Path path in zipPaths)
            {
                ZipArchiveEntry entry = zipArchive.CreateEntry(path.First().ToString(), CompressionLevel.Optimal);
                Stream writer = entry.Open();
                Stream reader = zipPathToContent(path);
                reader.CopyTo(writer);
                writer.Close();
                reader.Close();
            }
        }
    }
}
