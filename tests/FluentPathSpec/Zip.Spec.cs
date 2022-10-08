// Copyright © 2010-2019 Bertrand Le Roy.  All Rights Reserved.
// This code released under the terms of the 
// MIT License http://opensource.org/licenses/MIT

using Cornichon;
using System.Threading.Tasks;
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
        public async ValueTask ZipAndUnzipAFolder()
        {
            await Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.zip("bar").to("archive.zip"))
                 .And(() => I.unzip("archive.zip").to("unzip"))
                .Then(() => the.content_of("bar").should_be_identical_to_the_content_of("unzip"))
                 .And(() => there.should_be_an_entry_under(@"archive.zip"));
            await I.cleanup_test_files();
        }

        [Fact]
        public async ValueTask ZipAndUnzipInMemory()
            => await Scenario
                .Given(() => I.zip_in_memory("zipped content").to(@"foo\bar.txt"))
                .Then(() => the.zip_contains(path: @"foo\bar.txt", content: "zipped content"));
    }
}
