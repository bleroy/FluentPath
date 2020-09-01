// Copyright © 2010-2015 Bertrand Le Roy.  All Rights Reserved.
// This code released under the terms of the 
// MIT License http://opensource.org/licenses/MIT

using System;
using System.Linq;
using Fluent.IO;

namespace PutMoviesInFolders {
    class Program {
        static void Main(string[] args) {
            if (args.Length == 1 && (
                args[0] == "help" ||
                args[0] == "/?" ||
                args[0] == "-?")) {
                Console.WriteLine(@"Copies all files in the directory into their own folder.

Usage:
putmoviesinfolders [path]
where [path] is the path of the folder to process.");
                return;
            }
            Path.FromTokens(args.Length != 0 ? args[0] : ".")
                .Files(
                    p => new[] {
                        ".avi", ".m4v", ".wmv",
                        ".mp4", ".dvr-ms", ".mpg", ".mkv"
                    }.Contains(p.Extension))
                .CreateDirectories(
                    p => p.Parent()
                          .Combine(p.FileNameWithoutExtension))
                .End()
                .Move(
                    p => p.Parent()
                          .Combine(p.FileNameWithoutExtension)
                          .Combine(p.FileName));
        }
    }
}
