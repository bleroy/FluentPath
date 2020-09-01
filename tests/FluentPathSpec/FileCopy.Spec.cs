// Copyright © 2010-2019 Bertrand Le Roy.  All Rights Reserved.
// This code released under the terms of the 
// MIT License http://opensource.org/licenses/MIT

using Cornichon;
using Fluent.IO;
using System;
using System.Threading.Tasks;
using Xunit;

namespace FluentPathSpec
{
    public class FileCopySpec : IDisposable
    {
        // Prepare some vocabulary
        private FluentPathSpec I { get; } = new FluentPathSpec();
        private FluentPathSpec there => I;
        private FluentPathSpec the => I;
        private FluentPathSpec it => I;

        public void Dispose() => I.cleanup_test_files();

        [Fact]
        public async Task SingleFileCopyToDirectory()
            => await Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.copy_from("foo.txt").to("sub"))
                .Then(() => the.content_of_directory("sub").should_be("subsub", "baz.txt", "binary.bin", "foo.txt"))
                 .And(() => there.should_be_an_entry_under("foo.txt"));

        [Fact]
        public void SingleFileCopy()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.copy_from("foo.txt").to(@"sub\foocopy.txt"))
                .Then(() => the.content_of_directory("sub").should_be("subsub", "baz.txt", "binary.bin", "foocopy.txt"))
                 .And(() => there.should_be_an_entry_under("foo.txt"));

        [Fact]
        public void FileCopyWithOverwriteAlways()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.use_overwrite_mode(Overwrite.Always).to_copy_from(@"bar\baz.txt").to("sub"))
                .Then(() => the.content_of(@"bar\baz.txt").should_be_the_text("bar baz"))
                 .And(() => the.content_of(@"sub\baz.txt").should_be_the_text("bar baz"));

        [Fact]
        public void FileCopyWithOverwriteNever()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.use_overwrite_mode(Overwrite.Never).to_copy_from(@"bar\baz.txt").to("sub"))
                .Then(() => the.content_of(@"bar\baz.txt").should_be_the_text("bar baz"))
                 .And(() => the.content_of(@"sub\baz.txt").should_be_the_text("sub baz"));

        [Fact]
        public void CopyOlderFileWithOverwriteIfNewer()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.use_overwrite_mode(Overwrite.IfNewer).to_copy_from(@"bar\baz.txt").to("sub"))
                .Then(() => the.content_of(@"bar\baz.txt").should_be_the_text("bar baz"))
                 .And(() => the.content_of(@"sub\baz.txt").should_be_the_text("sub baz"));

        [Fact]
        public void CopyNewerFileWithOverwriteIfNewer()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.use_overwrite_mode(Overwrite.IfNewer).to_copy_from(@"sub\baz.txt").to("bar"))
                .Then(() => the.content_of(@"bar\baz.txt").should_be_the_text("sub baz"))
                 .And(() => the.content_of(@"sub\baz.txt").should_be_the_text("sub baz"));

        [Fact]
        public void FileCopyWithOverwriteThrow()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.use_overwrite_mode(Overwrite.Throw).to_copy_from(@"bar\baz.txt").to("sub"))
                .Then(() => it.should_have_thrown<InvalidOperationException>());

        [Fact]
        public void CopySingleFileWithTransform()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.copy("foo.txt").with_a_doubled_filename())
                .Then(() => the.resulting_set_should_be("foofoo.txt"));

        [Fact]
        public void ShallowDirectoryCopy()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.copy_from("bar").to("sub"))
                .Then(() => the.content_of_directory("sub").should_be("subsub", "baz.txt", "binary.bin", "notes.txt"))
                 .And(() => there.should_be_no_entry_under(@"sub\bar"))
                 .And(() => the.content_of_directory("bar").should_be("bar", "baz.txt", "notes.txt"));

        [Fact]
        public void DeepDirectoryCopy()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.make_a_deep_copy_from("bar").to("sub"))
                .Then(() => the.content_of_directory("sub").should_be("bar", "subsub", "baz.txt", "binary.bin", "notes.txt"))
                 .And(() => the.content_of_directory(@"sub\bar").should_be("deep.txt"));
 
        [Fact]
        public void SingleFileMoveToDirectory()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.move_from("foo.txt").to("sub"))
                .Then(() => the.content_of_directory("sub").should_be("subsub", "baz.txt", "binary.bin", "foo.txt"))
                 .And(() => there.should_be_no_entry_under("foo.txt"));

        [Fact]
        public void SingleFileMove()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.move_from("foo.txt").to(@"sub\foocopy.txt"))
                .Then(() => the.content_of_directory("sub").should_be("subsub", "baz.txt", "binary.bin", "foocopy.txt"))
                 .And(() => there.should_be_no_entry_under("foo.txt"));

        [Fact]
        public void FileMoveWithOverwriteAlways()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.move_using_overwrite_mode(Overwrite.Always).from(@"bar\baz.txt").to("sub"))
                .Then(() => the.content_of(@"sub\baz.txt").should_be_the_text("bar baz"))
                 .And(() => there.should_be_no_entry_under(@"bar\baz.txt"));

        [Fact]
        public void FileMoveWithOverwriteNever()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.move_using_overwrite_mode(Overwrite.Never).from(@"bar\baz.txt").to("sub"))
                .Then(() => the.content_of(@"bar\baz.txt").should_be_the_text("bar baz"))
                 .And(() => the.content_of(@"sub\baz.txt").should_be_the_text("sub baz"));

        [Fact]
        public void MoveOlderFileWithOverwriteIfNewer()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.move_using_overwrite_mode(Overwrite.IfNewer).from(@"bar\baz.txt").to("sub"))
                .Then(() => the.content_of(@"bar\baz.txt").should_be_the_text("bar baz"))
                 .And(() => the.content_of(@"sub\baz.txt").should_be_the_text("sub baz"));

        [Fact]
        public void MoveNewerFileWithOverwriteIfNewer()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.move_using_overwrite_mode(Overwrite.IfNewer).from(@"sub\baz.txt").to("bar"))
                .Then(() => there.should_be_no_entry_under(@"sub\baz.txt"))
                 .And(() => the.content_of(@"bar\baz.txt").should_be_the_text("sub baz"));

        [Fact]
        public void FileMoveWithOverwriteThrow()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.move_using_overwrite_mode(Overwrite.Throw).from(@"bar\baz.txt").to("sub"))
                .Then(() => it.should_have_thrown<InvalidOperationException>());

        [Fact]
        public void MoveSingleFileWithTransform()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.move_while_doubling_the_filename("foo.txt"))
                 .And(() => I.select_files())
                .Then(() => the.resulting_set_should_be("foofoo.txt"));

        [Fact]
        public void DirectoryMove()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.move_from("bar").to("sub"))
                .Then(() => the.content_of_directory("sub").should_be("bar", "subsub", "baz.txt", "binary.bin", "notes.txt"))
                 .And(() => there.should_be_an_entry_under(@"sub\bar"))
                 .And(() => the.content_of_directory(@"sub\bar").should_be("deep.txt"))
                 .And(() => the.content_of(@"bar\baz.txt").should_be_the_text("bar baz"))
                 .And(() => the.content_of(@"sub\baz.txt").should_be_the_text("sub baz"));

        [Fact]
        public void CollectionCopy()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.use_a_lambda_to_copy_all_text_files_to("copy"))
                .Then(() => the.resulting_set_should_be(@"copy\foo.txt", @"copy\baz.txt", @"copy\notes.txt", @"copy\deep.txt"))
                 .And(() => the.content_of_directory("copy").should_be("foo.txt", "baz.txt", "notes.txt", "deep.txt"))
                 .And(() => the.content_of_directory(".").should_be("foo.txt", "bar", "sub", "copy"));

        [Fact]
        public void CollectionMove()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.use_a_lambda_to_move_all_text_files_to("moved"))
                .Then(() => the.resulting_set_should_be(@"moved\foo.txt", @"moved\baz.txt", @"moved\notes.txt", @"moved\deep.txt"))
                 .And(() => the.content_of_directory("moved").should_be("foo.txt", "baz.txt", "notes.txt", "deep.txt"))
                 .And(() => the.content_of_directory(".").should_be("bar", "sub", "moved"))
                 .And(() => the.content_of_directory("bar").should_be("bar"))
                 .And(() => the.content_of_directory("sub").should_be("binary.bin", "subsub"));
    }
}
