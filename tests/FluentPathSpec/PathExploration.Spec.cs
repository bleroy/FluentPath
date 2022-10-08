// Copyright © 2010-2019 Bertrand Le Roy.  All Rights Reserved.
// This code released under the terms of the 
// MIT License http://opensource.org/licenses/MIT

using Cornichon;
using System;
using System.Threading.Tasks;
using Xunit;

namespace FluentPathSpec
{
    public class PathExploration : IAsyncDisposable
    {
        // Prepare some vocabulary
        private FluentPathSpec I { get; } = new FluentPathSpec();
        private FluentPathSpec there => I;
        private FluentPathSpec the => I;
        private FluentPathSpec it => I;

        public async ValueTask DisposeAsync() => await I.cleanup_test_files();

        [Fact]
        public void DirectoriesAndFilesExist()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .Then(() => there.should_be_an_entry_under("foo.txt"))
                 .And(() => there.should_be_an_entry_under("bar"))
                 .And(() => there.should_be_an_entry_under(@"bar\baz.txt"))
                 .And(() => there.should_be_an_entry_under(@"bar\notes.txt"))
                 .And(() => there.should_be_an_entry_under(@"bar\bar"))
                 .And(() => there.should_be_an_entry_under(@"bar\bar\deep.txt"))
                 .And(() => there.should_be_an_entry_under("sub"))
                 .And(() => there.should_be_an_entry_under(@"sub\subsub"))
                 .And(() => there.should_be_an_entry_under(@"sub\binary.bin"))
                 .And(() => there.should_be_no_entry_under("unknown"))
                 .And(() => there.should_be_no_entry_under(@"bar\unknown"))
                 .And(() => there.should_be_no_entry_under(@"unknown\unknown"));

        [Fact]
        public void FindAllFiles()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.select_all_files())
                .Then(() => the.resulting_set_should_be(@"bar\bar\deep.txt", @"bar\baz.txt", @"bar\notes.txt", "foo.txt", @"sub\baz.txt", @"sub\binary.bin"));

        [Fact]
        public void FindAllTextFilesInADirectory()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.recursively_select("*.txt"))
                .Then(() => the.resulting_set_should_be(@"bar\bar\deep.txt", @"bar\baz.txt", @"bar\notes.txt", "foo.txt", @"sub\baz.txt"));

        [Fact]
        public void FindSubDirectories()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.select_subdirectories())
                .Then(() => the.resulting_set_should_be("bar", "sub"));

        [Fact]
        public void EnumerateDirectoriesTwice()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.enumerate_directories_twice())
                .Then(() => the.resulting_string_should_be("bar, bar, sub, sub"));

        [Fact]
        public void FindAllSubDirectories()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.select_deep_subdirectories())
                .Then(() => the.resulting_set_should_be("bar", @"bar\bar", "sub", @"sub\subsub"));

        [Fact]
        public void FindSubDirectoriesWithAPattern()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.select_deep_subdirectories_with_the_pattern("sub*"))
                .Then(() => the.resulting_set_should_be("sub", @"sub\subsub"));

        [Fact]
        public void FindSubDirectoriesWithACondition()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.use_a_predicate_to_select_subdirectories_with_a_name_starting_with("sub"))
                .Then(() => the.resulting_set_should_be("sub"));

        [Fact]
        public void FindAllSubDirectoriesWithACondition()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.use_a_predicate_to_select_deep_subdirectories_with_a_name_starting_with("sub"))
                .Then(() => the.resulting_set_should_be("sub", @"sub\subsub"));

        [Fact]
        public void FindFiles()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.select_files())
                .Then(() => the.resulting_set_should_be("foo.txt"));

        [Fact]
        public void FindFilesWithACondition()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.use_a_predicate_to_search_for_files_with_extension(".txt"))
                .Then(() => the.resulting_set_should_be("foo.txt"));

        [Fact]
        public void FindAllFilesWithACondition()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.use_a_predicate_to_search_for_deep_files_with_extension(".txt"))
                .Then(() => the.resulting_set_should_be(@"bar\bar\deep.txt", @"bar\baz.txt", @"bar\notes.txt", "foo.txt", @"sub\baz.txt"));

        [Fact]
        public void FindAllFilesWithAPattern()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.search_for_deep_files_with_pattern("*.txt"))
                .Then(() => the.resulting_set_should_be(@"bar\bar\deep.txt", @"bar\baz.txt", @"bar\notes.txt", "foo.txt", @"sub\baz.txt"));

        [Fact]
        public void FindAllFilesWithSpecificExtensions()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.select_files_with_extensions("bin", "foo"))
                .Then(() => the.resulting_set_should_be(@"sub\binary.bin"));

        [Fact]
        public void FindFileSystemEntries()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.select_file_system_entries())
                .Then(() => the.resulting_set_should_be("bar", "foo.txt", "sub"));

        [Fact]
        public void FindFileSystemEntriesWithACondition()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.search_for_file_system_entries_with_an_a_in_the_name())
                .Then(() => the.resulting_set_should_be("bar"));

        [Fact]
        public void FindAllFileSystemEntriesWithACondition()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.search_for_deep_file_system_entries_with_an_a_in_the_name())
                .Then(() => the.resulting_set_should_be("bar", @"bar\bar", @"bar\baz.txt", @"sub\baz.txt", @"sub\binary.bin"));

        [Fact]
        public void FindAllFileSystemEntriesWithAPattern()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.search_for_deep_file_system_entries_using_the_pattern("*a*"))
                .Then(() => the.resulting_set_should_be("bar", @"bar\bar", @"bar\baz.txt", @"sub\baz.txt", @"sub\binary.bin"));
    }
}
