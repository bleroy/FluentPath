using Fluent.IO.Async;
using Fluent.IO.Async.Zip;
using System;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Threading.Tasks;

namespace UnzipAndAlphabetize;
public class Program
{
    public static async Task<int> Main(params string[] args)
    {
        RootCommand rootCommand = new RootCommand(
            description: "Unzips files in a directory, then puts the resulting files into alphabetized folders. The zip files are then deleted.");
        Argument folderArgument = new Argument("folder", "The folder where to perform the operation.");
        rootCommand.Add(folderArgument);
        rootCommand.Handler = CommandHandler.Create<System.IO.DirectoryInfo>(UnzipAndAlphabetize);
        return await rootCommand.InvokeAsync(args);
    }

    private static async void UnzipAndAlphabetize(System.IO.DirectoryInfo folder)
    {
        Func<Path, ValueTask<Path>> firstLetterFolder = async (Path p) =>
            await p.Parent().Combine(new string(new[] { (await p.FileName())[0] }));

        Console.WriteLine($"Extracting files from {folder.FullName}:");

        await new Path(folder.FullName)
            .Files("*.zip", recursive: false)
            .ForEach(async zip => Console.Write($"{await zip.FullPath()} .. "))
            .CreateDirectories(firstLetterFolder)
            .End()
            .Unzip(firstLetterFolder)
            .ForEach(async unzipped => Console.WriteLine(await unzipped.FullPath()))
            .End()
            .Delete();
    }
}