// Copyright © 2010-2015 Bertrand Le Roy.  All Rights Reserved.
// This code released under the terms of the 
// MIT License http://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ionic.Zip;
using Ionic.Zlib;
using Path = Fluent.IO.Path;

namespace Fluent.Zip {
    /// <summary>
    /// A set of FluentPath extensions that surface SharpZipLib features.
    /// This API is unsuitable to zip very large files: streaming is not implemented yet.
    /// If you need to zip big files, please use the SharpZipLib API directly.
    /// </summary>
    public static class ZipExtensions {
        /// <summary>
        /// Unzips all files in the path.
        /// </summary>
        /// <param name="path">The zip files.</param>
        /// <param name="target">The directory where the files must be unzipped.</param>
        /// <returns>The uncompressed files and folders.</returns>
        public static Path Unzip(this Path path, Path target) {
            Unzip(path, (p, ba) => target.Combine(p).Write(ba));
            return target;
        }

        /// <summary>
        /// Unzips all files in the path.
        /// </summary>
        /// <param name="path">The zip files.</param>
        /// <param name="unzipAction">An action that handles the unzipping of each file.</param>
        /// <returns>The original path object</returns>
        public static Path Unzip(this Path path, Action<Path, byte[]> unzipAction) {
            path.Open((s, p) => Unzip(s, unzipAction));
            return path;
        }

        /// <summary>
        /// Unzips a byte array and calls an action for each unzipped file.
        /// </summary>
        /// <param name="zip">The zip byte array.</param>
        /// <param name="unzipAction">The action to perform with each unzipped file.</param>
        public static void Unzip(byte[] zip, Action<Path, byte[]> unzipAction) {
            Unzip(new MemoryStream(zip, false), unzipAction);
        }

        /// <summary>
        /// Unzips a stream and calls an action for each unzipped file.
        /// </summary>
        /// <param name="zip">The zip byte array.</param>
        /// <param name="unzipAction">The action to perform with each unzipped file.</param>
        public static void Unzip(Stream zip, Action<Path, byte[]> unzipAction) {
            using (var zipStream = new ZipInputStream(zip)) {
                ZipEntry theEntry;
                while ((theEntry = zipStream.GetNextEntry()) != null) {
                    if (!theEntry.IsDirectory && theEntry.FileName != "") {
                        using (var streamWriter = new MemoryStream()) {
                            var data = new byte[2048];
                            while (true) {
                                var size = zipStream.Read(data, 0, data.Length);
                                if (size > 0)
                                    streamWriter.Write(data, 0, size);
                                else
                                    break;
                            }
                            streamWriter.Close();
                            unzipAction(new Path(theEntry.FileName), streamWriter.ToArray());
                        }
                    }
                }
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
        /// <param name="path">The files to compress.</param>
        /// <param name="target">
        /// The path of the target zip file.
        /// If target has more than one file, only the first one is used.
        /// </param>
        /// <returns>The zipped path.</returns>
        public static Path Zip(this Path path, Path target) {
            var files = path.AllFiles()
                .ToDictionary(
                    p => p.MakeRelativeTo(path),
                    p => p);
            Zip(target, new Path(files.Keys), p => files[p].ReadBytes());
            return target;
        }

        /// <summary>
        /// Zips all files in the path to the target.
        /// </summary>
        /// <param name="path">The files to compress.</param>
        /// <param name="target">
        /// The path of the target zip file.
        /// If target has more than one file, only the first one is used.</param>
        /// <param name="fileSystemToZip">
        /// A function that maps the paths of the files and directories to zip into relative paths inside the zip.
        /// </param>
        /// <returns>The zipped path.</returns>
        public static Path Zip(this Path path, Path target, Func<Path, Path> fileSystemToZip) {
            var files = path.AllFiles()
               .ToDictionary(
                   fileSystemToZip,
                   p => p);
            Zip(target, new Path(files.Keys), p => files[p].ReadBytes());
            return target;
        }

        /// <summary>
        /// Zips the contents of a dictionary of paths to byte arrays.
        /// </summary>
        /// <param name="target">The path of the zip file to build.</param>
        /// <param name="contents">The contents to zip.</param>
        /// <returns>The path of the zipped file.</returns>
        public static Path Zip(this Path target, IDictionary<Path, byte[]> contents) {
            Zip(target, new Path(contents.Keys), p => contents[p]);
            return target;
        }

        /// <summary>
        /// Zips dynamically created contents.
        /// </summary>
        /// <param name="target">
        /// The path of the target zip file.
        /// If target has more than one file, only the first one is used.</param>
        /// <param name="zipPaths">The zipped paths of the files to zip.</param>
        /// <param name="zipPathToContent">
        /// A function that maps the zipped paths to the binary content of the file to zip.
        /// </param>
        /// <returns>The zipped path.</returns>
        public static Path Zip(this Path target, Path zipPaths, Func<Path, byte[]> zipPathToContent) {
            target.Open(s => ZipToStream(zipPaths, p => p.ReadBytes(), s),
                FileMode.Create, FileAccess.ReadWrite, FileShare.None);
            return target;
        }

        /// <summary>
        /// Zips the files in the path into a byte array.
        /// </summary>
        /// <param name="filesToZip">The list of files to zip</param>
        /// <returns>The byte array for the zip</returns>
        public static byte[] Zip(this Path filesToZip) {
            return Zip(filesToZip, p => p.ReadBytes());
        }

        public static byte[] Zip(Path zipPaths, Func<Path, byte[]> zipPathToContent) {
            using (var result = new MemoryStream()) {
                ZipToStream(zipPaths, zipPathToContent, result);
                return result.ToArray();
            }
        }

        public static void ZipToStream(Path zipPaths, Func<Path, byte[]> zipPathToContent, Stream output) {
            using (var zipStream = new ZipOutputStream(output)) {
                zipStream.CompressionLevel = CompressionLevel.BestCompression;
                foreach (var path in zipPaths) {
                    var buffer = zipPathToContent(path);
                    var entry = zipStream.PutNextEntry(path.ToString());
                    entry.CreationTime = DateTime.Now;
                    zipStream.Write(buffer, 0, buffer.Length);
                }
                zipStream.Flush();
                zipStream.Close();
            }
        }
    }
}
