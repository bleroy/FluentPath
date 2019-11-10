// Copyright © 2010-2019 Bertrand Le Roy.  All Rights Reserved.
// This code released under the terms of the 
// MIT License http://opensource.org/licenses/MIT

using Cornichon;
using System;
using Xunit;

namespace FluentPathSpec
{
    public class FileManipulation : IDisposable
    {
        // Prepare some vocabulary
        private FluentPathSpec I { get; } = new FluentPathSpec();
        private FluentPathSpec there => I;
        private FluentPathSpec the => I;
        private FluentPathSpec it => I;

        public void Dispose() => I.cleanup_test_files();

        [Fact]
        public void ChangeExtension()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.change_the_extension_of(@"bar\notes.txt").to("foo"))
                .Then(() => there.should_be_no_entry_under(@"bar\notes.txt"))
                 .And(() => there.should_be_an_entry_under(@"bar\notes.foo"));

        [Fact]
        public void DeleteAFile()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.delete(@"bar\notes.txt"))
                .Then(() => there.should_be_no_entry_under(@"bar\notes.txt"));

        [Fact]
        public void DeleteANonExistingFile()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.delete(@"bar\thisfiledoesnotexist.txt"))
                .Then(() => there.should_be_no_entry_under(@"bar\thisfiledoesnotexist.txt"));

        [Fact]
        public void DeleteADirectory()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.recursively_delete("bar"))
                .Then(() => there.should_be_no_entry_under("bar"))
                 .And(() => there.should_be_no_entry_under(@"bar\baz.txt"))
                 .And(() => there.should_be_no_entry_under(@"bar\notes.txt"))
                 .And(() => there.should_be_no_entry_under(@"bar\bar"))
                 .And(() => there.should_be_no_entry_under(@"bar\bar\deep.txt"));

        [Fact]
        public void ProcessATextFile()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.write_back_to(@"bar\baz.txt").its_uppercased_content_and_append_some_constant_and_the_filename())
                .Then(() => the.content_of(@"bar\baz.txt").should_be_the_text("BAR BAZ - processed baz.txt"));

        [Fact]
        public void ProcessTheContentOfATextFile()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.append_processed_to_the_uppercased_content_of(@"bar\baz.txt"))
                .Then(() => the.content_of(@"bar\baz.txt").should_be_the_text("BAR BAZ - processed"));

        [Fact]
        public void BinaryProcessTheContentOfAFile()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.process_the_binary_content_of(@"sub\binary.bin"))
                .Then(() => the.content_of(@"sub\binary.bin").should_be_bytes("fffefdfcfbfa00"));

        [Fact]
        public void BinaryProcessThePathAndContentOfAFile()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.process_the_binary_content_and_path_of(@"sub\binary.bin"))
                .Then(() => the.content_of(@"sub\binary.bin").should_be_bytes("fffefdfcfbfa00"))
                 .And(() => the.resulting_string_should_be("binary.bin"));

        [Fact]
        public void ReplaceTheTextOfAFile()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.replace_the_text_of(@"bar\baz.txt").with("replaced"))
                .Then(() => the.content_of(@"bar\baz.txt").should_be_the_text("replaced"));

        [Fact]
        public void GrepFiles()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.grep_for(@"\stext\s"))
                .Then(() => the.resulting_string_should_be(@"foo.txt:9, bar\notes.txt:9"));

        [Fact]
        public void GrepASingleFile()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.grep_in(@"bar\notes.txt").@for("is"))
                .Then(() => the.resulting_string_should_be("2, 5"));

        [Fact]
        public void AddPermissionsToAFile()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.add_permissions_to(@"bar\baz.txt"))
                .Then(() => there.is_an_additional_permission_on(@"bar\baz.txt"));

        [Fact]
        public void AddPermissionsToADirectory()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.add_permissions_to(@"bar\bar"))
                .Then(() => there.is_an_additional_permission_on(@"bar\bar"));
    }
}
