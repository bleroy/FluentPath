// Copyright © 2010-2015 Bertrand Le Roy.  All Rights Reserved.
// This code released under the terms of the 
// MIT License http://opensource.org/licenses/MIT

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using SystemIO = System.IO;
using Fluent.IO;
using Fluent.IO.Async;
using Fluent.IO.Async.Windows;
using Fluent.IO.Async.Zip;
using Fluent.Utils;
using Xunit;

namespace FluentPathSpec
{
    public class FluentPathSpec
    {
        private Path _testRoot;
        private Path _path;
        private Path _result;
        private string _resultString;
        private byte[] _zipped;
        private Exception _exception;

        public async ValueTask start_with_a_clean_directory()
        {
            // foo.txt
            // bar/
            //   baz.txt
            //   notes.txt
            //   bar/
            //     deep.txt
            // sub/
            //   baz.txt
            //   binary.bin
            //   subsub/
            string randomFolder = SystemIO.Path.GetRandomFileName();
            _path = _testRoot = new Path(SystemIO.Path.GetTempPath())
                .CreateSubDirectory(randomFolder);
            //System.Diagnostics.Debug.WriteLine(_testRoot.ToString());
            await _path
                .FileSystemEntries()
                .Delete(true);
            await _path.CreateFile("foo.txt", "This is a text file named foo.");
            Path bar = _path.CreateSubDirectory("bar");
            await bar.CreateFile("baz.txt", "bar baz")
               .LastWriteTime(DateTime.Now.AddSeconds(-2));
            await bar.CreateFile("notes.txt", "This is a text file containing notes.");
            Path barbar = bar.CreateSubDirectory("bar");
            await barbar.CreateFile("deep.txt", "Deep thoughts");
            Path sub = _path.CreateSubDirectory("sub");
            await sub.CreateSubDirectory("subsub");
            await sub.CreateFile("baz.txt", "sub baz")
               .LastWriteTime(DateTime.Now);
            await sub.CreateFile("binary.bin",
                new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0xFF });
        }

        public async ValueTask cleanup_test_files()
        {
            await _testRoot.Delete(true);
        }

        public async ValueTask DumpTestDirectoryStructure(Path path = null, int tab = 0)
        {
            if (path is null) path = _testRoot;
            string indent = new String(' ', tab);
            System.Diagnostics.Debug.WriteLine(indent + await path.FileName() + "/");
            await foreach (Path child in path.Directories())
            {
                await DumpTestDirectoryStructure(child, tab + 1);
            }
            await foreach (Path child in path.Files())
            {
                System.Diagnostics.Debug.WriteLine(indent + " " + await child.FileName());
            }
        }

        public async ValueTask add_permissions_to(string path)
        {
            SecurityIdentifier user = WindowsIdentity.GetCurrent().User;
            Path filePath = _path.CombineWithWindowsPath(path);
            FileSystemSecurity accessControl = await filePath.AccessControl();
            accessControl.AddAccessRule(
                    new FileSystemAccessRule(
                        user, FileSystemRights.Read, InheritanceFlags.None,
                        PropagationFlags.None, AccessControlType.Allow)
                );
            await filePath.AccessControl(accessControl);
        }

        public async ValueTask recursively_select(string searchPattern) =>
            _result = await _path.FileSystemEntries(searchPattern, true);

        public async ValueTask select_subdirectories() =>
            _result = await _path.Directories();

        public async ValueTask  select_deep_subdirectories() =>
            _result = await _path.Directories("*", true);

        public async ValueTask select_deep_subdirectories_with_the_pattern(string pattern) =>
            _result = await _path.Directories(pattern, true);

        public async ValueTask use_a_predicate_to_select_subdirectories_with_a_name_starting_with(string prefix) =>
            _result = await _path.Directories(async p => (await p.FileName()).StartsWith(prefix)); // = "sub"

        public async ValueTask use_a_predicate_to_select_deep_subdirectories_with_a_name_starting_with(string prefix) =>
            _result = await _path.Directories(async p => (await p.FileName()).StartsWith(prefix), true);

        public async ValueTask select_all_files() =>
            _result = await _path.AllFiles();

        public async ValueTask select_files() =>
            _result = await _path.Files();

        public async ValueTask use_a_predicate_to_search_for_files_with_extension(string extension) =>
            _result = await _path.Files(async f => await f.Extension() == extension); // ".txt"

        public async ValueTask use_a_predicate_to_search_for_deep_files_with_extension(string extension) =>
            _result = await _path.Files(async f => await f.Extension() == extension, true);

        public async ValueTask search_for_deep_files_with_pattern(string pattern) =>
            _result = await _path.Files(pattern, true);

        public async ValueTask select_files_with_extensions(params string[] extensions) =>
            _result = await _path
                .Files("*", true)
                .WhereExtensionIs(extensions.Select(s => s.Trim()).ToArray());

        public async ValueTask select_file_system_entries() =>
            _result = await _path.FileSystemEntries();

        public async ValueTask search_for_file_system_entries_with_an_a_in_the_name() =>
            _result = await _path.FileSystemEntries(async f => (await f.FileName()).IndexOf('a') != -1);

        public async ValueTask search_for_deep_file_system_entries_with_an_a_in_the_name() =>
            _result = await _path.FileSystemEntries(async f => (await f.FileName()).IndexOf('a') != -1, true);

        public async ValueTask search_for_deep_file_system_entries_using_the_pattern(string pattern) =>
            _result = await _path.FileSystemEntries(pattern, true);

        public class I_set_attributes_result
        {
            private readonly Path _path;
            private readonly SystemIO.FileAttributes[] _attributes;

            public I_set_attributes_result(Path path, SystemIO.FileAttributes[] attributeNames)
            {
                _path = path;
                _attributes = attributeNames;
            }

            public async ValueTask on(string path)
            {
                SystemIO.FileAttributes attributes =
                    _attributes.Aggregate((a, b) => a | b);
                await _path.CombineWithWindowsPath(path).Attributes(attributes);
            }
        }

        public I_set_attributes_result set_attributes(params SystemIO.FileAttributes[] attributes)
            => new I_set_attributes_result(_path, attributes);

        public class I_set_time_of_result
        {
            private readonly Path _path;
            private readonly type_of_time_event _typeOfTimeEvent;

            public I_set_time_of_result(Path path, type_of_time_event typeOfTimeEvent)
            {
                _path = path;
                _typeOfTimeEvent = typeOfTimeEvent;
            }

            public I_set_time_of_result_to_result to(DateTime date) =>
                new I_set_time_of_result_to_result(_path, _typeOfTimeEvent, date);
        }

        public class I_set_time_of_result_to_result
        {
            private readonly Path _path;
            private readonly type_of_time_event _typeOfTimeEvent;
            private readonly DateTime _date;

            public I_set_time_of_result_to_result(Path path, type_of_time_event typeOfTimeEvent, DateTime date)
            {
                _path = path;
                _typeOfTimeEvent = typeOfTimeEvent;
                _date = date;
            }

            public async ValueTask on(string path)
            {
                Path combinedPath = await _path.CombineWithWindowsPath(path);
                switch (_typeOfTimeEvent)
                {
                    case type_of_time_event.creation:
                        await combinedPath.CreationTime(_date);
                        break;
                    case type_of_time_event.UTC_creation:
                        await combinedPath.CreationTimeUtc(_date);
                        break;
                    case type_of_time_event.last_access:
                        await combinedPath.LastAccessTime(_date);
                        break;
                    case type_of_time_event.UTC_last_access:
                        await combinedPath.LastAccessTimeUtc(_date);
                        break;
                    case type_of_time_event.last_write:
                        await combinedPath.LastWriteTime(_date);
                        break;
                    case type_of_time_event.UTC_last_write:
                        await combinedPath.LastWriteTimeUtc(_date);
                        break;
                }
            }
        }

        public I_set_time_of_result set_time_of(type_of_time_event type) =>
            new I_set_time_of_result(_path, type);

        public class I_copy_from_result
        {
            private readonly Path _path;
            private readonly string _from;

            public I_copy_from_result(Path path, string from)
            {
                _path = path;
                _from = from;
            }

            public async ValueTask to(string to)
            {
                Path src = await _path.Combine(_from);
                await src.Copy(await _path.CombineWithWindowsPath(to));
            }
        }

        public I_copy_from_result copy_from(string from) =>
            new I_copy_from_result(_path, from);


        public class I_use_overwrite_mode_result
        {
            private readonly FluentPathSpec _that;
            private readonly Overwrite _overwrite;

            public I_use_overwrite_mode_result(FluentPathSpec that, Overwrite overwrite)
            {
                _that = that;
                _overwrite = overwrite;
            }

            public to_copy_from_result to_copy_from(string from) =>
                new to_copy_from_result(_that, _overwrite, from);
        }

        public class to_copy_from_result
        {
            private readonly FluentPathSpec _that;
            private readonly Overwrite _overwrite;
            private readonly string _from;

            public to_copy_from_result(FluentPathSpec that, Overwrite overwrite, string from)
            {
                _that = that;
                _overwrite = overwrite;
                _from = from;
            }

            public async ValueTask to(string to)
            {
                Path src = await _that._path.Combine(_from);
                if (_overwrite == Overwrite.Throw)
                {
                    try
                    {
                        await src.Copy(_that._path.CombineWithWindowsPath(to), _overwrite);
                    }
                    catch (Exception e)
                    {
                        _that._exception = e;
                    }
                }
                else
                {
                    await src.Copy(_that._path.CombineWithWindowsPath(to), _overwrite);
                }
            }
        }

        public I_use_overwrite_mode_result use_overwrite_mode(Overwrite overwrite)
            => new I_use_overwrite_mode_result(this, overwrite);

        public class I_copy_result
        {
            private readonly FluentPathSpec _that;
            private readonly string _relativePath;

            public I_copy_result(FluentPathSpec that, string relativePath)
            {
                _that = that;
                _relativePath = relativePath;
            }

            public async ValueTask with_a_doubled_filename()
            {
                _that._result = await _that._path
                    .CombineWithWindowsPath(_relativePath)
                    .Copy(
                        async p => await p.Parent().Combine(
                            await p.FileNameWithoutExtension() +
                            await p.FileNameWithoutExtension() +
                            await p.Extension()));
            }
        }
        public I_copy_result copy(string path) =>
            new I_copy_result(this, path);

        public class I_make_a_deep_copy_from_result
        {
            private readonly Path _path;
            private readonly string _from;

            public I_make_a_deep_copy_from_result(Path path, string from)
            {
                _path = path;
                _from = from;
            }

            public async ValueTask to(string to)
            {
                Path src = await _path.Combine(_from);
                await src.Copy(await _path.CombineWithWindowsPath(to), Overwrite.Never, true);
            }
        }

        public I_make_a_deep_copy_from_result make_a_deep_copy_from(string from) =>
            new I_make_a_deep_copy_from_result(_path, from);

        public class I_move_from_result
        {
            private readonly FluentPathSpec _that;
            private readonly string _from;

            public I_move_from_result(FluentPathSpec that, string from)
            {
                _that = that;
                _from = from;
            }

            public async ValueTask to(string to)
            {
                Path src = await _that._path.Combine(_from);
                await src.Move((string) await _that._path.CombineWithWindowsPath(to));
            }
        }

        public I_move_from_result move_from(string from) =>
            new I_move_from_result(this, from);

        public class I_use_overwrite_mode_to_move_result
        {
            private readonly FluentPathSpec _that;
            private readonly Overwrite _overwrite;

            public I_use_overwrite_mode_to_move_result(FluentPathSpec that, Overwrite overwrite)
            {
                _that = that;
                _overwrite = overwrite;
            }

            public to_move_from_result from(string from) =>
                new to_move_from_result(_that, _overwrite, from);
        }

        public class to_move_from_result
        {
            private readonly FluentPathSpec _that;
            private readonly Overwrite _overwrite;
            private readonly string _from;

            public to_move_from_result(FluentPathSpec that, Overwrite overwrite, string from)
            {
                _that = that;
                _overwrite = overwrite;
                _from = from;
            }

            public async ValueTask to(string to)
            {
                Path src = await _that._path.Combine(_from);
                Path dest = await _that._path.CombineWithWindowsPath(to);
                if (_overwrite == Overwrite.Throw)
                {
                    try
                    {
                        await src.Move((string)dest, _overwrite);
                    }
                    catch(Exception e)
                    {
                        _that._exception = e;
                    }
                }
                else
                {
                    await src.Move((string)dest, _overwrite);
                }
            }
        }

        public I_use_overwrite_mode_to_move_result move_using_overwrite_mode(Overwrite overwrite) =>
            new I_use_overwrite_mode_to_move_result(this, overwrite);

        public async ValueTask move_while_doubling_the_filename(string relativePath) =>
            await _path
                .CombineWithWindowsPath(relativePath)
                .Move(
                    async p => await p.Parent().Combine(
                        await p.FileNameWithoutExtension() +
                        await p.FileNameWithoutExtension() +
                        await p.Extension()));

        public async ValueTask open_and_read(string relativePath) =>
            await _path
                .CombineWithWindowsPath(relativePath)
                .Open(async s =>
                {
                    using var reader = new SystemIO.StreamReader(s);
                    _resultString = await reader.ReadToEndAsync();
                });

        public class I_write_back_to_result
        {
            private readonly FluentPathSpec _that;
            private readonly string _relativePath;

            public I_write_back_to_result(FluentPathSpec that, string relativePath)
            {
                _that = that;
                _relativePath = relativePath;
            }

            public async ValueTask its_uppercased_content_and_append_some_constant_and_the_filename() =>
                await _that._path
                    .CombineWithWindowsPath(_relativePath)
                    .Process(async (p, s) => s.ToUpperInvariant() + " - processed " + await p.FileName());
        }

        public I_write_back_to_result write_back_to(string relativePath) =>
            new I_write_back_to_result(this, relativePath);

        public async ValueTask append_processed_to_the_uppercased_content_of(string relativePath) =>
            await _path
                .CombineWithWindowsPath(relativePath)
                .Process(s => s.ToUpperInvariant() + " - processed");

        public class I_append_result
        {
            private readonly FluentPathSpec _that;
            private readonly string _text;

            public I_append_result(FluentPathSpec that, string text)
            {
                _that = that;
                _text = text;
            }

            public async ValueTask to(string relativePath) =>
                await _that._path
                    .CombineWithWindowsPath(relativePath)
                    .Write(_text, true);
        }

        public I_append_result append(string text) =>
            new I_append_result(this, text);

        public class I_use_encoding
        {
            private readonly Path _path;
            private readonly Encoding _encoding;

            public I_use_encoding(Path path, Encoding encoding)
            {
                _path = path;
                _encoding = encoding;
            }

            public I_append_result_to_write_result to_write(string content) =>
                new I_append_result_to_write_result(_path, _encoding, content);
        }

        public class I_append_result_to_write_result
        {
            private readonly Path _path;
            private readonly Encoding _encoding;
            private readonly string _content;

            public I_append_result_to_write_result(Path path, Encoding encoding, string content)
            {
                _path = path;
                _encoding = encoding;
                _content = content;
            }

            public async ValueTask to(string relativePath) =>
                await _path
                    .CombineWithWindowsPath(relativePath)
                    .Write(_content, _encoding, true);
        }

        public I_use_encoding use_encoding(Encoding encoding) =>
            new I_use_encoding(_path, encoding);

        public class I_replace_result
        {
            private readonly FluentPathSpec _that;
            private readonly string _relativePath;

            public I_replace_result(FluentPathSpec that, string relativePath)
            {
                _that = that;
                _relativePath = relativePath;
            }

            public async ValueTask with(string text) =>
                await _that._path
                    .CombineWithWindowsPath(_relativePath)
                    .Write(text);
        }

        public I_replace_result replace_the_text_of(string relativePath) =>
            new I_replace_result(this, relativePath);

        public async ValueTask process_the_binary_content_of(string relativePath) =>
            await _path
                .CombineWithWindowsPath(relativePath)
                .Process(
                    ba =>
                    {
                        for (var i = 0; i < ba.Length; i++)
                        {
                            ba[i] ^= 0xFF;
                        }
                        return ba;
                    }
                );

        public async ValueTask process_the_binary_content_and_path_of(string relativePath) =>
            await _path
                .CombineWithWindowsPath(relativePath)
                .Process(
                    async (p, ba) =>
                    {
                        for (var i = 0; i < ba.Length; i++)
                        {
                            ba[i] ^= 0xFF;
                        }
                        _resultString = await p.FileName();
                        return ba;
                    }
                );

        public async ValueTask get_the_path_for(string path) =>
            _path = await _path.CombineWithWindowsPath(path);

        public async ValueTask create_that_directory() =>
            await _path.CreateDirectory();

        public async ValueTask create_a_directory_with_relative_path(string relativePath) =>
            await Path.CreateDirectory(await _path.CombineWithWindowsPath(relativePath).FullPath());

        public async ValueTask use_a_stream_to_concatenate_the_contents_of(params string[] filePaths)
        {
            Path files = await _path.CombineWithWindowsPaths(filePaths);
            await files.Open(
                async s =>
                {
                    using var reader = new SystemIO.StreamReader(s);
                    _resultString += await reader.ReadToEndAsync();
                });
        }

        public async ValueTask use_a_stream_to_read_path_and_content_for_each_of(params string[] filePaths)
        {
            Path files = await _path.CombineWithWindowsPaths(filePaths);
            await files.Open(
                async (s, p) =>
                {
                    using var reader = new SystemIO.StreamReader(s);
                    _resultString +=
                        await p.MakeRelativeTo(_path).ToWindowsPath() +
                        ":" +
                        await reader.ReadToEndAsync();
                });
        }

        public async ValueTask concatenate_the_contents_of(params string[] filePaths)
        {
            Path files = await _path.CombineWithWindowsPaths(filePaths);
            await files.Read(s => { _resultString += s; });
        }

        public async ValueTask read_path_and_content_for_each_of(params string[] filePaths)
        {
            Path files = await _path.CombineWithWindowsPaths(filePaths);
            await files.Read(async (s, p) => _resultString +=
            await p.MakeRelativeTo(_path).ToWindowsPath() + ":" + s);
        }

        public async ValueTask use_a_lambda_to_create_directories_with_the_same_names_as_each_file_under(string relativePath)
        {
            _result = await _path
                .CombineWithWindowsPath(relativePath)
                .AllFiles()
                .CreateDirectories(async p =>
                    await _testRoot.Combine(await p.FileNameWithoutExtension()));
            // DumpTestDirectoryStructure();
        }

        public class I_create_a_subdirectory_with_name_result
        {
            private readonly FluentPathSpec _that;
            private readonly string _subdirectoryName;

            public I_create_a_subdirectory_with_name_result(FluentPathSpec that, string subdirectoryName)
            {
                _that = that;
                _subdirectoryName = subdirectoryName;
            }

            public async ValueTask under(string relativePath) =>
                await _that._path
                    .CombineWithWindowsPath(relativePath)
                    .CreateSubDirectory(_subdirectoryName);

            public async ValueTask under(params string[] targetDirectoryNames) =>
                await new Path(targetDirectoryNames
                    .ToAsyncEnumerable()
                    .Select(async dir => await _that._testRoot.Combine(dir).FirstPath()), _that._testRoot)
                    .CreateDirectories(_subdirectoryName);
        }

        public I_create_a_subdirectory_with_name_result create_a_subdirectory_with_name(string subdirectoryName) =>
            new I_create_a_subdirectory_with_name_result(this, subdirectoryName);


        public class I_create_a_file_result
        {
            private readonly FluentPathSpec _that;
            private readonly string _relativePath;

            public I_create_a_file_result(FluentPathSpec that, string relativePath)
            {
                _that = that;
                _relativePath = relativePath;
            }

            public async ValueTask with_content(string content) =>
                await _that._path.CreateFiles(
                    async p => await _relativePath.ToCrossPlatformPath(),
                    p => content);

            public async ValueTask with_binary_content(string hexContent)
            {
                byte[] content = hexContent.ToBytes();
                await _that._path.CreateFiles(
                    async p => await _relativePath.ToCrossPlatformPath(),
                    p => content);
            }

            public I_create_file_with_encoding_result and_use_encoding(Encoding encoding) =>
                new I_create_file_with_encoding_result(_that, _relativePath, encoding);
        }

        public class I_create_file_with_encoding_result
        {
            private readonly FluentPathSpec _that;
            private readonly string _relativePath;
            private readonly Encoding _encoding;

            public I_create_file_with_encoding_result(
                FluentPathSpec that,
                string relativePath,
                Encoding encoding)
            {
                _that = that;
                _relativePath = relativePath;
                _encoding = encoding;
            }

            public async ValueTask with_content(string content) =>
                await _that._path.CreateFiles(
                    async p => await _relativePath.ToCrossPlatformPath(),
                    p => content,
                    _encoding);
        }

        public I_create_a_file_result create_a_file_under(string relativePath)
            => new I_create_a_file_result(this, relativePath);

        public class I_change_the_extension_result
        {
            private readonly FluentPathSpec _that;
            private readonly string _relativePath;

            public I_change_the_extension_result(FluentPathSpec that, string relativePath)
            {
                _that = that;
                _relativePath = relativePath;
            }

            public async ValueTask to(string newExtension)
            {
                Path oldPath = await _that._path.CombineWithWindowsPath(_relativePath);
                await oldPath.Move(async p => await p.ChangeExtension(newExtension));
            }
        }

        public I_change_the_extension_result change_the_extension_of(string relativePath) =>
            new I_change_the_extension_result(this, relativePath);

        public async ValueTask delete(string relativePath) =>
            await _path.CombineWithWindowsPath(relativePath).Delete();

        public async ValueTask recursively_delete(string relativePath) =>
            await _path.CombineWithWindowsPath(relativePath).Delete(true);

        public async ValueTask decrypt(string relativePath) =>
            await _path.CombineWithWindowsPath(relativePath).Decrypt();

        public async ValueTask encrypt(string relativePath) =>
            await _path.CombineWithWindowsPath(relativePath).Encrypt();

        public async ValueTask enumerate_directories_twice()
        {
            Path dirs = await _path.Directories();
            IAsyncEnumerator<Path> dirEnum = dirs.GetAsyncEnumerator();
            var dirList = new List<string>();
            while (await dirEnum.MoveNextAsync())
            {
                dirList.Add(await dirEnum.Current.FileName());
            }
            dirEnum = dirs.GetAsyncEnumerator();
            while (await dirEnum.MoveNextAsync())
            {
                dirList.Add(await dirEnum.Current.FileName());
            }
            dirList.Sort();
            _resultString = String.Join(", ", dirList.ToArray());
        }

        public async ValueTask grep_for(string regularExpression)
        {
            var matches = new List<string>();
            await _path.AllFiles().Grep(
                regularExpression, async (p, match, content) => matches.Add(
                    await p.MakeRelativeTo(_path).FirstPath() + ":" +
                    match.Index));
            _resultString = string.Join(", ", matches);
        }

        public class I_grep_result
        {
            private readonly FluentPathSpec _that;
            private readonly string _relativePath;

            public I_grep_result(FluentPathSpec that, string relativePath)
            {
                _that = that;
                _relativePath = relativePath;
            }

            public async ValueTask @for(string regularExpression)
            {
                var matches = new List<int>();
                await _that._path
                    .CombineWithWindowsPath(_relativePath)
                    .Grep(regularExpression, (p, match, content) => matches.Add(match.Index));
                _that._resultString = string.Join(", ", matches);
            }
        }

        public I_grep_result grep_in(string relativePath)
            => new I_grep_result(this, relativePath);

        public class I_write_with_encoding_result
        {
            private readonly FluentPathSpec _that;
            private readonly Encoding _encoding;

            public I_write_with_encoding_result(FluentPathSpec that, Encoding encoding)
            {
                _that = that;
                _encoding = encoding;
            }

            public I_write_with_encoding_to_result content(string content)
                => new I_write_with_encoding_to_result(_that, _encoding, content);
        }

        public class I_write_with_encoding_to_result
        {
            private readonly FluentPathSpec _that;
            private readonly Encoding _encoding;
            private readonly string _content;

            public I_write_with_encoding_to_result(FluentPathSpec that, Encoding encoding, string content)
            {
                _that = that;
                _encoding = encoding;
                _content = content;
            }

            public async ValueTask to(string path)
                => await _that._path.CombineWithWindowsPath(path).Write(_content, _encoding);
        }

        public I_write_with_encoding_result write_with_encoding(Encoding encoding)
            => new I_write_with_encoding_result(this, encoding);

        public async ValueTask use_a_lambda_to_copy_all_text_files_to(string destination)
        {
            Path files = await _path.AllFiles();
            _result = await files.Copy(
                async p => await ((await p.Extension() == ".txt") ?
                    (await _path.Combine(destination, await p.FileName())) :
                    Path.Empty));
        }

        public async ValueTask use_a_lambda_to_move_all_text_files_to(string destination)
        {
            Path files = await _path.AllFiles();
            _result = await files.Move(
                async p => await ((await p.Extension() == ".txt") ?
                    (await _path.Combine(destination, await p.FileName())) :
                    Path.Empty),
                Overwrite.Always);
        }

        public class I_write_bytes_result
        {
            private readonly FluentPathSpec _that;
            private readonly string _hexContent;

            public I_write_bytes_result(FluentPathSpec that, string hexContent)
            {
                _that = that;
                _hexContent = hexContent;
            }

            public async ValueTask to(string path)
            {
                byte[] content = _hexContent.ToBytes();
                await _that._path.CombineWithWindowsPath(path).Write(content);
            }
        }

        public I_write_bytes_result write_bytes(string hexContent)
            => new I_write_bytes_result(this, hexContent);

        public class I_zip_result
        {
            private readonly FluentPathSpec _that;
            private readonly string _source;

            public I_zip_result(FluentPathSpec that, string source)
            {
                _that = that;
                _source = source;
            }

            public async ValueTask to(string destination)
            {
                Path sourcePath = await _that._path.CombineWithWindowsPath(_source);
                Path destinationPath = await _that._path.CombineWithWindowsPath(destination);
                await destinationPath.Zip(sourcePath);
            }
        }

        public I_zip_result zip(string source) => new I_zip_result(this, source);

        public class I_zip_in_memory_result
        {
            private readonly FluentPathSpec _that;
            private readonly string _content;

            public I_zip_in_memory_result(FluentPathSpec that, string content)
            {
                _that = that;
                _content = content;
            }

            public async ValueTask to(string destination)
            {
                var outStream = new SystemIO.MemoryStream();
                await ZipExtensions.ZipToStream(
                    await _that._path.CombineWithWindowsPath(destination),
                    p => new SystemIO.MemoryStream(Encoding.Default.GetBytes(_content)),
                    outStream);
                _that._zipped = outStream.ToArray();
            }
        }

        public I_zip_in_memory_result zip_in_memory(string content) => new I_zip_in_memory_result(this, content);

        public class I_unzip_result
        {
            private readonly FluentPathSpec _that;
            private readonly string _source;

            public I_unzip_result(FluentPathSpec that, string source)
            {
                _that = that;
                _source = source;
            }

            public async ValueTask to(string destination)
            {
                Path sourcePath = await _that._path.CombineWithWindowsPath(_source);
                Path destinationPath = await _that._path.CombineWithWindowsPath(destination);
                await sourcePath.Unzip(destinationPath);
            }
        }

        public I_unzip_result unzip(string source) => new I_unzip_result(this, source);

        public class then_the_attributes_on_result
        {
            private readonly FluentPathSpec _that;
            private readonly string _relativePath;

            public then_the_attributes_on_result(FluentPathSpec that, string relativePath)
            {
                _that = that;
                _relativePath = relativePath;
            }

            public async ValueTask should_be(params SystemIO.FileAttributes[] attributes)
            {
                SystemIO.FileAttributes fileAttributes =
                    await _that._path.CombineWithWindowsPath(_relativePath).Attributes();
                SystemIO.FileAttributes expected = attributes.Aggregate((a, b) => a | b);
                Assert.Equal(expected, fileAttributes);
            }
        }

        public then_the_attributes_on_result attributes_on(string relativePath)
            => new then_the_attributes_on_result(this, relativePath);

        public async ValueTask resulting_set_should_be(params string[] fileList)
        {
            var resultList = new HashSet<string>();
            await _result
                .Map(async p => await (await p.IsRooted() ? await p.MakeRelativeTo(_testRoot) : p))
                .ForEach(async p => resultList.Add(await p.ToWindowsPath()));
            Assert.Equal(fileList.ToHashSet(), resultList);
        }

        public void resulting_string_should_be(string resultString)
            => Assert.Equal(resultString, _resultString);

        public class then_the_content_of_folder_result
        {
            private readonly FluentPathSpec _that;
            private readonly string _relativePath;

            public then_the_content_of_folder_result(FluentPathSpec that, string relativePath)
            {
                _that = that;
                _relativePath = relativePath;
            }

            public async ValueTask should_be(params string[] fileList)
            {
                Path folderPath = await _that._testRoot.Combine(_relativePath);
                var files = new HashSet<string>();
                await folderPath.FileSystemEntries()
                    .MakeRelativeTo(folderPath)
                    .ForEach(async p => files.Add(await p.ToWindowsPath()));
                Assert.Equal(fileList.ToHashSet(), files);
            }
        }

        public then_the_content_of_folder_result content_of_directory(string folder)
            => new then_the_content_of_folder_result(this, folder);

        public async ValueTask should_be_an_entry_under(string path)
            => Assert.True(await _path.CombineWithWindowsPath(path).Exists());

        public async ValueTask should_be_no_entry_under(string path)
            => Assert.False(await _path.CombineWithWindowsPath(path).Exists());

        public void should_have_thrown<TException>()
        {
            Assert.NotNull(_exception);
            Assert.IsType<TException>(_exception);
        }

        public async ValueTask should_be_an_encrypted_file_under(string relativePath)
            => Assert.True(await _path.CombineWithWindowsPath(relativePath).IsEncrypted());

        public async ValueTask should_be_an_unencrypted_file_under(string relativePath)
            => Assert.False(await _path.CombineWithWindowsPath(relativePath).IsEncrypted());

        public async ValueTask is_an_additional_permission_on(string path)
        {
            SecurityIdentifier user = WindowsIdentity.GetCurrent().User;
            AuthorizationRuleCollection accessRules = (await _path
                .CombineWithWindowsPath(path)
                .AccessControl())
                .GetAccessRules(true, false, typeof(SecurityIdentifier));
            bool found = accessRules
                .Cast<FileSystemAccessRule>()
                .Any(accessRule => accessRule.IdentityReference.Equals(user) &&
                    !accessRule.IsInherited &&
                    accessRule.AccessControlType == AccessControlType.Allow &&
                    (accessRule.FileSystemRights & FileSystemRights.Read) != 0 &&
                    accessRule.InheritanceFlags == InheritanceFlags.None &&
                    accessRule.PropagationFlags == PropagationFlags.None);
            Assert.True(found);
        }

        public async ValueTask should_be_a_directory_at(string directory)
        {
            Path path = _testRoot.CombineWithWindowsPath(directory);
            Assert.True(await path.Exists() && await path.IsDirectory());
        }

        public class the_content_of_result
        {
            private readonly FluentPathSpec _that;
            private readonly string _relativePath;

            public the_content_of_result(FluentPathSpec that, string relativePath)
            {
                _that = that;
                _relativePath = relativePath;
            }

            public async ValueTask should_be_the_text(string textContent)
                => Assert.Equal(textContent, await _that._path.CombineWithWindowsPath(_relativePath).Read());

            public async ValueTask should_be_bytes(string binaryContent)
            {
                string binaryContentString = "";
                await _that._path
                    .CombineWithWindowsPath(_relativePath)
                    .ReadBytes(actualBinaryContent => binaryContentString = actualBinaryContent.ToHex());
                Assert.Equal(binaryContent, binaryContentString);
            }

            public async ValueTask should_be_identical_to_the_content_of(string otherPath)
            {
                Path p1 = await _that._path.CombineWithWindowsPath(_relativePath);
                Path files1 = await p1.AllFiles();
                Path p2 = await _that._path.CombineWithWindowsPath(otherPath);
                Path files2 = await p2.AllFiles();
                Assert.True(await files1.MakeRelativeTo(p1) == await files2.MakeRelativeTo(p2));
                await files1.ReadBytes(async (ba, p) =>
                    Assert.Equal(await p2.Combine(await p.MakeRelativeTo(p1)).ReadBytes(), ba));
            }

            public the_content_of_result_with_encoding_result with_encoding(Encoding encoding)
                => new the_content_of_result_with_encoding_result(_that, _relativePath, encoding);
        }

        public class the_content_of_result_with_encoding_result
        {
            private readonly FluentPathSpec _that;
            private readonly string _relativePath;
            private readonly Encoding _encoding;

            public the_content_of_result_with_encoding_result(
                FluentPathSpec that,
                string relativePath,
                Encoding encoding)
            {
                _that = that;
                _relativePath = relativePath;
                _encoding = encoding;
            }

            public async ValueTask should_be(string textContent)
            {
                string readContent = "";
                await _that._path
                    .CombineWithWindowsPath(_relativePath)
                    .Read(s => readContent = s, _encoding);
                Assert.Equal(textContent, readContent);
            }
        }

        public the_content_of_result content_of(string path)
            => new the_content_of_result(this, path);

        public class the_time_of_result
        {
            private readonly FluentPathSpec _that;
            private readonly type_of_time_event _typeOfTime;

            public the_time_of_result(FluentPathSpec that, type_of_time_event typeOfTime)
            {
                _that = that;
                _typeOfTime = typeOfTime;
            }

            public the_time_of_result_on_result on(string relativePath)
                => new the_time_of_result_on_result(_that, _typeOfTime, relativePath);
        }

        public class the_time_of_result_on_result
        {
            private readonly FluentPathSpec _that;
            private readonly type_of_time_event _typeOfTime;
            private readonly string _relativePath;

            public the_time_of_result_on_result(FluentPathSpec that, type_of_time_event typeOfTime, string relativePath)
            {
                _that = that;
                _typeOfTime = typeOfTime;
                _relativePath = relativePath;
            }

            public async ValueTask should_be(DateTime date)
            {
                Path combinedPath = _that._path.CombineWithWindowsPath(_relativePath);
                switch (_typeOfTime)
                {
                    case type_of_time_event.creation:
                        Assert.Equal(date, await combinedPath.CreationTime());
                        break;
                    case type_of_time_event.UTC_creation:
                        Assert.Equal(date, await combinedPath.CreationTimeUtc());
                        break;
                    case type_of_time_event.last_access:
                        Assert.Equal(date, await combinedPath.LastAccessTime());
                        break;
                    case type_of_time_event.UTC_last_access:
                        Assert.Equal(date, await combinedPath.LastAccessTimeUtc());
                        break;
                    case type_of_time_event.last_write:
                        Assert.Equal(date, await combinedPath.LastWriteTime());
                        break;
                    case type_of_time_event.UTC_last_write:
                        Assert.Equal(date, await combinedPath.LastWriteTimeUtc());
                        break;
                }
            }
        }

        public the_time_of_result time_of(type_of_time_event typeOfTime)
            => new the_time_of_result(this, typeOfTime);

        public async ValueTask zip_contains(string path, string content)
        {
            await ZipExtensions.Unzip(_zipped, (string p, byte[] ba) => {
                Assert.Equal(path, p);
                Assert.Equal(content, Encoding.Default.GetString(ba));
            });
        }
    }

    public enum type_of_time_event
    {
        creation, UTC_creation, last_access, UTC_last_access, last_write, UTC_last_write
    }

    public static class Helpers
    {
        public static Path ToCrossPlatformPath(this string path)
            => new Path(path.Replace('\\', SystemIO.Path.DirectorySeparatorChar));

        public static Path CombineWithWindowsPath(this Path path, string relativePath)
            => path is null ? new Path(relativePath) : path.Combine(relativePath.Split('\\'));

        public static Path CombineWithWindowsPaths(this Path path, string[] relativePaths)
        {
            return new Path(CombineWithWindowsImpl(), path);

            async IAsyncEnumerable<string> CombineWithWindowsImpl()
            {
                foreach (string p in relativePaths)
                {
                    yield return await path.CombineWithWindowsPath(p).FirstPath();
                }
            }
        }


        public static async ValueTask<string> ToWindowsPath(this Path path)
            => (await path.FirstPath()).Replace(SystemIO.Path.DirectorySeparatorChar, '\\');

        public static byte[] ToBytes(this string hexContent)
        {
            byte[] content = new byte[hexContent.Length / 2];
            for (int i = 0; i < hexContent.Length; i += 2)
            {
                content[i / 2] = byte.Parse(
                    hexContent.Substring(i, 2), NumberStyles.HexNumber);
            }

            return content;
        }

        public static string ToHex(this byte[] bytes)
            => String.Join("", (from b in bytes select b.ToString("x2")).ToArray());
    }
}
