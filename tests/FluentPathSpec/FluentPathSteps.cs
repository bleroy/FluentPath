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
using SystemIO = System.IO;
using Fluent.IO;
using Fluent.IO.Windows;
using Xunit;
using Fluent.Zip;
using System.Threading.Tasks;

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

        public void start_with_a_clean_directory()
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
            _path
                .FileSystemEntries()
                .Delete(true);
            _path.CreateFile("foo.txt", "This is a text file named foo.");
            Path bar = _path.CreateSubDirectory("bar");
            bar.CreateFile("baz.txt", "bar baz")
               .LastWriteTime(DateTime.Now.AddSeconds(-2));
            bar.CreateFile("notes.txt", "This is a text file containing notes.");
            Path barbar = bar.CreateSubDirectory("bar");
            barbar.CreateFile("deep.txt", "Deep thoughts");
            Path sub = _path.CreateSubDirectory("sub");
            sub.CreateSubDirectory("subsub");
            sub.CreateFile("baz.txt", "sub baz")
               .LastWriteTime(DateTime.Now);
            sub.CreateFile("binary.bin",
                new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0xFF });
        }

        public void cleanup_test_files()
        {
            _testRoot.Delete(true);
        }

        public void DumpTestDirectoryStructure(Path path = null, int tab = 0)
        {
            if (path is null) path = _testRoot;
            string indent = new String(' ', tab);
            System.Diagnostics.Debug.WriteLine(indent + path.FileName + "/");
            foreach (Path child in path.Directories())
            {
                DumpTestDirectoryStructure(child, tab + 1);
            }
            foreach (Path child in path.Files())
            {
                System.Diagnostics.Debug.WriteLine(indent + " " + child.FileName);
            }
        }

        public void add_permissions_to(string path)
        {
            SecurityIdentifier user = WindowsIdentity.GetCurrent().User;
            Path filePath = _path.CombineWithWindowsPath(path);
            FileSystemSecurity accessControl = filePath.AccessControl();
            accessControl.AddAccessRule(
                    new FileSystemAccessRule(
                        user, FileSystemRights.Read, InheritanceFlags.None,
                        PropagationFlags.None, AccessControlType.Allow)
                );
            filePath.AccessControl(accessControl);
        }

        public void recursively_select(string searchPattern)
            => _result = _path.FileSystemEntries(searchPattern, true);

        public void select_subdirectories() => _result = _path.Directories();

        public void select_deep_subdirectories()
            => _result = _path.Directories("*", true);

        public void select_deep_subdirectories_with_the_pattern(string pattern)
            => _result = _path.Directories(pattern, true);

        public void use_a_predicate_to_select_subdirectories_with_a_name_starting_with(string prefix)
            => _result = _path.Directories(p => p.FileName.StartsWith(prefix)); // = "sub"

        public void use_a_predicate_to_select_deep_subdirectories_with_a_name_starting_with(string prefix)
            => _result = _path.Directories(p => p.FileName.StartsWith(prefix), true);

        public void select_all_files() => _result = _path.AllFiles();

        public void select_files() => _result = _path.Files();

        public void use_a_predicate_to_search_for_files_with_extension(string extension)
            => _result = _path.Files(f => f.Extension == extension); // ".txt"

        public void use_a_predicate_to_search_for_deep_files_with_extension(string extension)
            => _result = _path.Files(f => f.Extension == extension, true);

        public void search_for_deep_files_with_pattern(string pattern)
            => _result = _path.Files(pattern, true);

        public void select_files_with_extensions(params string[] extensions)
            => _result = _path.Files("*", true)
                .WhereExtensionIs(extensions.Select(s => s.Trim()).ToArray());

        public void select_file_system_entries() => _result = _path.FileSystemEntries();

        public void search_for_file_system_entries_with_an_a_in_the_name()
            => _result = _path.FileSystemEntries(f => f.FileName.IndexOf('a') != -1);

        public void search_for_deep_file_system_entries_with_an_a_in_the_name()
            => _result = _path.FileSystemEntries(f => f.FileName.IndexOf('a') != -1, true);

        public void search_for_deep_file_system_entries_using_the_pattern(string pattern)
            => _result = _path.FileSystemEntries(pattern, true);


        public class I_set_attributes_result
        {
            private readonly Path _path;
            private readonly SystemIO.FileAttributes[] _attributes;

            public I_set_attributes_result(Path path, SystemIO.FileAttributes[] attributeNames)
            {
                _path = path;
                _attributes = attributeNames;
            }

            public void on(string path)
            {
                SystemIO.FileAttributes attributes =
                    _attributes.Aggregate((a, b) => a | b);
                _path.CombineWithWindowsPath(path).Attributes(attributes);
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

            public I_set_time_of_result_to_result to(DateTime date)
                => new I_set_time_of_result_to_result(_path, _typeOfTimeEvent, date);
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

            public void on(string path)
            {
                Path combinedPath = _path.CombineWithWindowsPath(path);
                switch (_typeOfTimeEvent)
                {
                    case type_of_time_event.creation:
                        combinedPath.CreationTime(_date);
                        break;
                    case type_of_time_event.UTC_creation:
                        combinedPath.CreationTimeUtc(_date);
                        break;
                    case type_of_time_event.last_access:
                        combinedPath.LastAccessTime(_date);
                        break;
                    case type_of_time_event.UTC_last_access:
                        combinedPath.LastAccessTimeUtc(_date);
                        break;
                    case type_of_time_event.last_write:
                        combinedPath.LastWriteTime(_date);
                        break;
                    case type_of_time_event.UTC_last_write:
                        combinedPath.LastWriteTimeUtc(_date);
                        break;
                }
            }
        }

        public I_set_time_of_result set_time_of(type_of_time_event type)
            => new I_set_time_of_result(_path, type);

        public class I_copy_from_result
        {
            private readonly Path _path;
            private readonly string _from;

            public I_copy_from_result(Path path, string from)
            {
                _path = path;
                _from = from;
            }

            public async Task to(string to)
            {
                Path src = _path.Combine(_from);
                await src.Copy(_path.CombineWithWindowsPath(to));
            }
        }

        public I_copy_from_result copy_from(string from) => new I_copy_from_result(_path, from);


        public class I_use_overwrite_mode_result
        {
            private readonly FluentPathSpec _that;
            private readonly Overwrite _overwrite;

            public I_use_overwrite_mode_result(FluentPathSpec that, Overwrite overwrite)
            {
                _that = that;
                _overwrite = overwrite;
            }

            public to_copy_from_result to_copy_from(string from)
                => new to_copy_from_result(_that, _overwrite, from);
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

            public void to(string to)
            {
                Path src = _that._path.Combine(_from);
                if (_overwrite == Overwrite.Throw)
                {
                    try
                    {
                        src.Copy(_that._path.CombineWithWindowsPath(to), _overwrite);
                    }
                    catch (Exception e)
                    {
                        _that._exception = e;
                    }
                }
                else
                {
                    src.Copy(_that._path.CombineWithWindowsPath(to), _overwrite);
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

            public void with_a_doubled_filename()
            {
                _that._result = _that._path.CombineWithWindowsPath(_relativePath)
                    .Copy(
                        p => p.Parent().Combine(
                            p.FileNameWithoutExtension + p.FileNameWithoutExtension + p.Extension));
            }
        }
        public I_copy_result copy(string path) => new I_copy_result(this, path);


        public class I_make_a_deep_copy_from_result
        {
            private readonly Path _path;
            private readonly string _from;

            public I_make_a_deep_copy_from_result(Path path, string from)
            {
                _path = path;
                _from = from;
            }

            public void to(string to)
            {
                Path src = _path.Combine(_from);
                src.Copy(_path.CombineWithWindowsPath(to), Overwrite.Never, true);
            }
        }

        public I_make_a_deep_copy_from_result make_a_deep_copy_from(string from)
            => new I_make_a_deep_copy_from_result(_path, from);

        public class I_move_from_result
        {
            private readonly FluentPathSpec _that;
            private readonly string _from;

            public I_move_from_result(FluentPathSpec that, string from)
            {
                _that = that;
                _from = from;
            }

            public void to(string to)
            {
                Path src = _that._path.Combine(_from);
                src.Move((string)_that._path.CombineWithWindowsPath(to));
            }
        }

        public I_move_from_result move_from(string from)
            => new I_move_from_result(this, from);

        public class I_use_overwrite_mode_to_move_result
        {
            private readonly FluentPathSpec _that;
            private readonly Overwrite _overwrite;

            public I_use_overwrite_mode_to_move_result(FluentPathSpec that, Overwrite overwrite)
            {
                _that = that;
                _overwrite = overwrite;
            }

            public to_move_from_result from(string from)
                => new to_move_from_result(_that, _overwrite, from);
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

            public void to(string to)
            {
                Path src = _that._path.Combine(_from);
                Path dest = _that._path.CombineWithWindowsPath(to);
                if (_overwrite == Overwrite.Throw)
                {
                    try
                    {
                        src.Move((string)dest, _overwrite);
                    }
                    catch(Exception e)
                    {
                        _that._exception = e;
                    }
                }
                else
                {
                    src.Move((string)dest, _overwrite);
                }
            }
        }

        public I_use_overwrite_mode_to_move_result move_using_overwrite_mode(Overwrite overwrite)
            => new I_use_overwrite_mode_to_move_result(this, overwrite);

        public void move_while_doubling_the_filename(string relativePath)
            => _path
                .CombineWithWindowsPath(relativePath)
                .Move(
                    p => p.Parent().Combine(
                        p.FileNameWithoutExtension + p.FileNameWithoutExtension +
                        p.Extension));

        public void open_and_read(string relativePath)
            => _path
                .CombineWithWindowsPath(relativePath)
                .Open(s =>
                {
                    using var reader = new SystemIO.StreamReader(s);
                    _resultString = reader.ReadToEnd();
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

            public void its_uppercased_content_and_append_some_constant_and_the_filename()
                => _that._path
                    .CombineWithWindowsPath(_relativePath)
                    .Process((p, s) => s.ToUpperInvariant() + " - processed " + p.FileName);
        }

        public I_write_back_to_result write_back_to(string relativePath)
            => new I_write_back_to_result(this, relativePath);

        public void append_processed_to_the_uppercased_content_of(string relativePath)
            => _path
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

            public void to(string relativePath)
                => _that._path
                    .CombineWithWindowsPath(relativePath)
                    .Write(_text, true);
        }

        public I_append_result append(string text) => new I_append_result(this, text);

        public class I_use_encoding
        {
            private readonly Path _path;
            private readonly Encoding _encoding;

            public I_use_encoding(Path path, Encoding encoding)
            {
                _path = path;
                _encoding = encoding;
            }

            public I_append_result_to_write_result to_write(string content)
                => new I_append_result_to_write_result(_path, _encoding, content);
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

            public void to(string relativePath)
                => _path.CombineWithWindowsPath(relativePath).Write(_content, _encoding, true);
        }

        public I_use_encoding use_encoding(Encoding encoding)
            => new I_use_encoding(_path, encoding);

        public class I_replace_result
        {
            private readonly FluentPathSpec _that;
            private readonly string _relativePath;

            public I_replace_result(FluentPathSpec that, string relativePath)
            {
                _that = that;
                _relativePath = relativePath;
            }

            public void with(string text)
                => _that._path.CombineWithWindowsPath(_relativePath).Write(text);
        }

        public I_replace_result replace_the_text_of(string relativePath)
            => new I_replace_result(this, relativePath);

        public void process_the_binary_content_of(string relativePath)
            => _path
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

        public void process_the_binary_content_and_path_of(string relativePath)
            => _path.CombineWithWindowsPath(relativePath)
                .Process(
                    (p, ba) =>
                    {
                        for (var i = 0; i < ba.Length; i++)
                        {
                            ba[i] ^= 0xFF;
                        }
                        _resultString = p.FileName;
                        return ba;
                    }
                );

        public void get_the_path_for(string path) => _path = _path.CombineWithWindowsPath(path);

        public void create_that_directory() => _path.CreateDirectory();

        public void create_a_directory_with_relative_path(string relativePath)
            => Path.CreateDirectory(_path.CombineWithWindowsPath(relativePath).FullPath);

        public void use_a_stream_to_concatenate_the_contents_of(params string[] filePaths)
        {
            Path files = _path.CombineWithWindowsPaths(filePaths);
            files.Open(
                s =>
                {
                    using var reader = new SystemIO.StreamReader(s);
                    _resultString += reader.ReadToEnd();
                });
        }

        public void use_a_stream_to_read_path_and_content_for_each_of(params string[] filePaths)
        {
            Path files = _path.CombineWithWindowsPaths(filePaths);
            files.Open(
                (s, p) =>
                {
                    using var reader = new SystemIO.StreamReader(s);
                    _resultString += p.MakeRelativeTo(_path).ToWindowsPath() + ":" + reader.ReadToEnd();
                });
        }

        public void concatenate_the_contents_of(params string[] filePaths)
        {
            Path files = _path.CombineWithWindowsPaths(filePaths);
            files.Read(s => { _resultString += s; });
        }

        public void read_path_and_content_for_each_of(params string[] filePaths)
        {
            Path files = _path.CombineWithWindowsPaths(filePaths);
            files.Read((s, p) => _resultString += p.MakeRelativeTo(_path).ToWindowsPath() + ":" + s);
        }

        public void use_a_lambda_to_create_directories_with_the_same_names_as_each_file_under(string relativePath)
        {
            _result = _path
                   .CombineWithWindowsPath(relativePath)
                   .AllFiles()
                   .CreateDirectories(p => _testRoot.Combine(p.FileNameWithoutExtension));
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

            public void under(string relativePath)
                => _that._path.CombineWithWindowsPath(relativePath).CreateSubDirectory(_subdirectoryName);

            public void under(params string[] targetDirectoryNames)
                => new Path(targetDirectoryNames.Select(dir => _that._testRoot.Combine(dir)))
                    .CreateDirectories(_subdirectoryName);
        }

        public I_create_a_subdirectory_with_name_result create_a_subdirectory_with_name(string subdirectoryName)
            => new I_create_a_subdirectory_with_name_result(this, subdirectoryName);


        public class I_create_a_file_result
        {
            private readonly FluentPathSpec _that;
            private readonly string _relativePath;

            public I_create_a_file_result(FluentPathSpec that, string relativePath)
            {
                _that = that;
                _relativePath = relativePath;
            }

            public void with_content(string content)
                => _that._path.CreateFiles(
                    p => _relativePath.ToCrossPlatformPath(),
                    p => content);

            public void with_binary_content(string hexContent)
            {
                byte[] content = hexContent.ToBytes();
                _that._path.CreateFiles(
                    p => _relativePath.ToCrossPlatformPath(),
                    p => content);
            }

            public I_create_file_with_encoding_result and_use_encoding(Encoding encoding)
                => new I_create_file_with_encoding_result(_that, _relativePath, encoding);
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

            public void with_content(string content)
                => _that._path.CreateFiles(
                    p => _relativePath.ToCrossPlatformPath(),
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

            public void to(string newExtension)
            {
                Path oldPath = _that._path.CombineWithWindowsPath(_relativePath);
                oldPath.Move(p => p.ChangeExtension(newExtension));
            }
        }

        public I_change_the_extension_result change_the_extension_of(string relativePath)
            => new I_change_the_extension_result(this, relativePath);

        public void delete(string relativePath)
            => _path.CombineWithWindowsPath(relativePath).Delete();

        public void recursively_delete(string relativePath)
            => _path.CombineWithWindowsPath(relativePath).Delete(true);

        public void decrypt(string relativePath)
            => _path.CombineWithWindowsPath(relativePath).Decrypt();

        public void encrypt(string relativePath)
            => _path.CombineWithWindowsPath(relativePath).Encrypt();

        public void enumerate_directories_twice()
        {
            Path dirs = _path.Directories();
            IEnumerator<Path> dirEnum = dirs.GetEnumerator();
            var dirList = new List<string>();
            while (dirEnum.MoveNext())
            {
                dirList.Add(dirEnum.Current.FileName);
            }
            dirEnum = dirs.GetEnumerator();
            var dirEnumNonGeneric = (IEnumerator)dirEnum;
            while (dirEnumNonGeneric.MoveNext())
            {
                dirList.Add(((Path)dirEnumNonGeneric.Current).FileName);
            }
            dirList.Sort();
            _resultString = String.Join(", ", dirList.ToArray());
        }

        public void grep_for(string regularExpression)
        {
            var matches = new List<string>();
            _path.AllFiles().Grep(
                regularExpression, (p, match, content) => matches.Add(
                    p.MakeRelativeTo(_path).ToString() + ":" +
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

            public void @for(string regularExpression)
            {
                var matches = new List<int>();
                _that._path
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

            public void to(string path)
                => _that._path.CombineWithWindowsPath(path).Write(_content, _encoding);
        }

        public I_write_with_encoding_result write_with_encoding(Encoding encoding)
            => new I_write_with_encoding_result(this, encoding);

        public void use_a_lambda_to_copy_all_text_files_to(string destination)
        {
            Path files = _path.AllFiles();
            _result = files.Copy(
                p =>
                p.Extension == ".txt" ? _path.Combine(destination, p.FileName) : null);
        }

        public void use_a_lambda_to_move_all_text_files_to(string destination)
        {
            Path files = _path.AllFiles();
            _result = files.Move(
                p =>
                p.Extension == ".txt" ? _path.Combine(destination, p.FileName) : null,
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

            public void to(string path)
            {
                byte[] content = _hexContent.ToBytes();
                _that._path.CombineWithWindowsPath(path).Write(content);
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

            public void to(string destination)
            {
                Path sourcePath = _that._path.CombineWithWindowsPath(_source);
                Path destinationPath = _that._path.CombineWithWindowsPath(destination);
                destinationPath.Zip(sourcePath);
            }
        }

        public I_zip_result zip(string source) => new I_zip_result(this, source);

        //public class I_zip_in_memory_result
        //{
        //    private readonly FluentPathSpec _that;
        //    private readonly string _content;

        //    public I_zip_in_memory_result(FluentPathSpec that, string content)
        //    {
        //        _that = that;
        //        _content = content;
        //    }

        //    public void to(string destination)
        //    {
        //        _that._zipped = ZipExtensions.Zip(
        //            _that._path.CombineWithWindowsPath(destination),
        //            p => new MemoryStream(Encoding.Default.GetBytes(_content)));
        //    }
        //}

        //public I_zip_in_memory_result zip_in_memory(string content) => new I_zip_in_memory_result(this, content);

        public class I_unzip_result
        {
            private readonly FluentPathSpec _that;
            private readonly string _source;

            public I_unzip_result(FluentPathSpec that, string source)
            {
                _that = that;
                _source = source;
            }

            public void to(string destination)
            {
                Path sourcePath = _that._path.CombineWithWindowsPath(_source);
                Path destinationPath = _that._path.CombineWithWindowsPath(destination);
                sourcePath.Unzip(destinationPath);
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

            public void should_be(params SystemIO.FileAttributes[] attributes)
            {
                SystemIO.FileAttributes fileAttributes =
                    _that._path.CombineWithWindowsPath(_relativePath).Attributes();
                SystemIO.FileAttributes expected = attributes.Aggregate((a, b) => a | b);
                Assert.Equal(expected, fileAttributes);
            }
        }

        public then_the_attributes_on_result attributes_on(string relativePath)
            => new then_the_attributes_on_result(this, relativePath);

        public void resulting_set_should_be(params string[] fileList)
        {
            var resultList = _result
                .Select(p => (p.IsRooted ? p.MakeRelativeTo(_testRoot) : p).ToWindowsPath())
                .ToHashSet();
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

            public void should_be(params string[] fileList)
            {
                Path folderPath = _that._testRoot.Combine(_relativePath);
                var files = folderPath.FileSystemEntries()
                    .MakeRelativeTo(folderPath)
                    .Select(p => p.ToWindowsPath())
                    .ToHashSet();
                Assert.Equal(fileList.ToHashSet(), files);
            }
        }

        public then_the_content_of_folder_result content_of_directory(string folder)
            => new then_the_content_of_folder_result(this, folder);

        public void should_be_an_entry_under(string path)
            => Assert.True(_path.CombineWithWindowsPath(path).Exists);

        public void should_be_no_entry_under(string path)
            => Assert.False(_path.CombineWithWindowsPath(path).Exists);

        public void should_have_thrown<TException>()
        {
            Assert.NotNull(_exception);
            Assert.IsType<TException>(_exception);
        }

        public void should_be_an_encrypted_file_under(string relativePath)
            => Assert.True(_path.CombineWithWindowsPath(relativePath).IsEncrypted);

        public void should_be_an_unencrypted_file_under(string relativePath)
            => Assert.False(_path.CombineWithWindowsPath(relativePath).IsEncrypted);

        public void is_an_additional_permission_on(string path)
        {
            SecurityIdentifier user = WindowsIdentity.GetCurrent().User;
            AuthorizationRuleCollection accessRules = _path
                .CombineWithWindowsPath(path)
                .AccessControl().
                GetAccessRules(true, false, typeof(SecurityIdentifier));
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

        public void should_be_a_directory_at(string directory)
        {
            Path path = _testRoot.CombineWithWindowsPath(directory);
            Assert.True(path.Exists && path.IsDirectory);
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

            public void should_be_the_text(string textContent)
                => Assert.Equal(textContent, _that._path.CombineWithWindowsPath(_relativePath).Read());

            public void should_be_bytes(string binaryContent)
            {
                string binaryContentString = null;
                _that._path
                    .CombineWithWindowsPath(_relativePath)
                    .ReadBytes(actualBinaryContent => binaryContentString = actualBinaryContent.ToHex());
                Assert.Equal(binaryContent, binaryContentString);
            }

            public void should_be_identical_to_the_content_of(string otherPath)
            {
                Path p1 = _that._path.CombineWithWindowsPath(_relativePath);
                Path files1 = p1.AllFiles();
                Path p2 = _that._path.CombineWithWindowsPath(otherPath);
                Path files2 = p2.AllFiles();
                Assert.True(files1.MakeRelativeTo(p1) == files2.MakeRelativeTo(p2));
                files1.ReadBytes((ba, p) =>
                    Assert.Equal(p2.Combine(p.MakeRelativeTo(p1)).ReadBytes(), ba));
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

            public void should_be(string textContent)
            {
                string readContent = null;
                _that._path
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

            public void should_be(DateTime date)
            {
                Path combinedPath = _that._path.CombineWithWindowsPath(_relativePath);
                switch (_typeOfTime)
                {
                    case type_of_time_event.creation:
                        Assert.Equal(date, combinedPath.CreationTime());
                        break;
                    case type_of_time_event.UTC_creation:
                        Assert.Equal(date, combinedPath.CreationTimeUtc());
                        break;
                    case type_of_time_event.last_access:
                        Assert.Equal(date, combinedPath.LastAccessTime());
                        break;
                    case type_of_time_event.UTC_last_access:
                        Assert.Equal(date, combinedPath.LastAccessTimeUtc());
                        break;
                    case type_of_time_event.last_write:
                        Assert.Equal(date, combinedPath.LastWriteTime());
                        break;
                    case type_of_time_event.UTC_last_write:
                        Assert.Equal(date, combinedPath.LastWriteTimeUtc());
                        break;
                }
            }
        }

        public the_time_of_result time_of(type_of_time_event typeOfTime)
            => new the_time_of_result(this, typeOfTime);

        public void zip_contains(string path, string content)
        {
            ZipExtensions.Unzip(_zipped, (string p, byte[] ba) => {
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
            => (Path)path.Replace('\\', SystemIO.Path.DirectorySeparatorChar);

        public static Path CombineWithWindowsPath(this Path path, string relativePath)
            => path is null ? new Path(relativePath) : path.Combine(relativePath.Split('\\'));

        public static Path CombineWithWindowsPaths(this Path path, string[] relativePaths)
            => new Path(relativePaths.Select(p => path.CombineWithWindowsPath(p)));


        public static string ToWindowsPath(this Path path)
            => path.ToString().Replace(SystemIO.Path.DirectorySeparatorChar, '\\');

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
