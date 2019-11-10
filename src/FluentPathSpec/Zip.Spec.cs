// Copyright © 2010-2019 Bertrand Le Roy.  All Rights Reserved.
// This code released under the terms of the 
// MIT License http://opensource.org/licenses/MIT

using Cornichon;
using Xunit;

namespace FluentPathSpec
{
    public class Zip
    {
        // Prepare some vocabulary
        private FluentPathSpec I { get; } = new FluentPathSpec();
        private FluentPathSpec there => I;
        private FluentPathSpec the => I;

        [Fact]
        public void ZipAndUnzipAFolder()
        {
            Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.zip("bar").to("archive.zip"))
                 .And(() => I.unzip("archive.zip").to("unzip"))
                .Then(() => the.content_of("bar").should_be_identical_to_the_content_of("unzip"))
                 .And(() => there.should_be_an_entry_under(@"archive.zip"));
            I.cleanup_test_files();
        }

        [Fact]
        public void ZipAndUnzipInMemory()
            => Scenario
                .Given(() => I.zip_in_memory("zipped content").to(@"foo\bar.txt"))
                .Then(() => the.zip_contains(path: @"foo\bar.txt", content: "zipped content"));
    }
}
