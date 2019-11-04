// Copyright © 2010-2019 Bertrand Le Roy.  All Rights Reserved.
// This code released under the terms of the 
// MIT License http://opensource.org/licenses/MIT

using Cornichon;
using System;
using System.Text;
using Xunit;

namespace FluentPathSpec
{
    public class FileAndDirectoryCreationSpec : IDisposable
    {
        // Prepare some vocabulary
        private FluentPathSpec I { get; } = new FluentPathSpec();
        private FluentPathSpec there => I;
        private FluentPathSpec the => I;

        public void Dispose() => I.cleanup_test_files();

        [Fact]
        public void CreateDirectory()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.get_the_path_for("newdir"))
                 .And(() => I.create_that_directory())
                .Then(() => there.should_be_a_directory_at("newdir"));

        [Fact]
        public void CreateDirectoryFromPath()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.create_a_directory_with_relative_path(@"newdir\newsubdir"))
                .Then(() => there.should_be_a_directory_at(@"newdir\newsubdir"));

        [Fact]
        public void CreateSubdirectoryFromPath()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.get_the_path_for(@"bar\bar"))
                 .And(() => I.create_that_directory())
                 .And(() => I.get_the_path_for("newsubdir"))
                 .And(() => I.create_that_directory())
                .Then(() => there.should_be_a_directory_at(@"bar\bar\newsubdir"));

        [Fact]
        public void CreateCollectionOfDirectories()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.use_a_lambda_to_create_directories_with_the_same_names_as_each_file_under("bar"))
                .Then(() => the.resulting_set_should_be("baz", "notes", "deep"))
                 .And(() => the.content_of_folder(".").should_be("foo.txt", "bar", "baz", "notes", "deep", "sub"));

        [Fact]
        public void CreateTextFileWithDefaultEncoding()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.create_a_file_under(@"bar\dejavu.txt").with_content("Déjà Vu"))
                .Then(() => the.content_of(@"bar\dejavu.txt").should_be_bytes("44c3a96ac3a0205675"));

        [Fact]
        public void CreateTextFileWithUTF16Encoding()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.create_a_file_under(@"bar\dejavu.txt")
                                .and_use_encoding(Encoding.Unicode)
                                .with_content("Déjà Vu"))
                .Then(() => the.content_of(@"bar\dejavu.txt").should_be_bytes("fffe4400e9006a00e000200056007500"));

        [Fact]
        public void CreateBinaryFile()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.create_a_file_under(@"bar\binary.bin").with_binary_content("010045f974123456"))
                .Then(() => the.content_of(@"bar\binary.bin").should_be_bytes("010045f974123456"));

        [Fact]
        public void CreateDirectoriesWithTheSameNameUnderACollectionOfDirectories()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.create_a_subdirectory_with_name("newdir").under(".", "bar", @"bar\bar"))
                .Then(() => there.should_be_a_directory_at("newdir"))
                 .And(() => there.should_be_a_directory_at(@"bar\newdir"))
                 .And(() => there.should_be_a_directory_at(@"bar\bar\newdir"));
    }
}
