// Copyright © 2010-2015 Bertrand Le Roy.  All Rights Reserved.
// This code released under the terms of the 
// MIT License http://opensource.org/licenses/MIT

using System;
using System.Linq;
using System.Threading.Tasks;
using Fluent.IO.Async;

namespace PutMoviesInFolders {
    class Program {
        static async ValueTask Main(string[] args) {
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
            await Path.FromTokens(args.Length != 0 ? args[0] : ".")
                .Files(
                    async p => new[] {
                        ".avi", ".m4v", ".wmv",
                        ".mp4", ".dvr-ms", ".mpg", ".mkv"
                    }.Contains(await p.Extension()))
                .CreateDirectories(
                    async p => await p.Parent()
                          .Combine(await p.FileNameWithoutExtension()))
                .End()
                .Move(
                    async p => await p.Parent()
                          .Combine(await p.FileNameWithoutExtension())
                          .Combine(await p.FileName()));
        }
    }
}
