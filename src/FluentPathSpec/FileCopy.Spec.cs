// Copyright © 2010-2019 Bertrand Le Roy.  All Rights Reserved.
// This code released under the terms of the 
// MIT License http://opensource.org/licenses/MIT

using Cornichon;
using Fluent.IO;
using System;
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
        public void SingleFileCopyToDirectory()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.copy_from("foo.txt").to("sub"))
                .Then(() => the.content_of_folder("sub").should_be("subsub", "baz.txt", "binary.bin", "foo.txt"))
                 .And(() => there.should_be_an_entry_under("foo.txt"));

        [Fact]
        public void SingleFileCopy()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.copy_from("foo.txt").to(@"sub\foocopy.txt"))
                .Then(() => the.content_of_folder("sub").should_be("subsub", "baz.txt", "binary.bin", "foocopy.txt"))
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
    }
}
