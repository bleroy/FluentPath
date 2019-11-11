// Copyright © 2010-2019 Bertrand Le Roy.  All Rights Reserved.
// This code released under the terms of the 
// MIT License http://opensource.org/licenses/MIT

using Cornichon;
using System;
using System.Text;
using Xunit;
using static System.IO.FileAttributes;
using static FluentPathSpec.type_of_time_event;

namespace FluentPathSpec
{
    public class ReadWriteSpec : IDisposable
    {
        // Prepare some vocabulary
        private FluentPathSpec I { get; } = new FluentPathSpec();
        private FluentPathSpec there => I;
        private FluentPathSpec the => I;
        private FluentPathSpec it => I;

        public void Dispose() => I.cleanup_test_files();

        [Fact]
        public void ReadATextFile()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .Then(() => the.content_of(@"bar\baz.txt").should_be_the_text("bar baz"));

        [Fact]
        public void ReadABinaryFile()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .Then(() => the.content_of(@"sub\binary.bin").should_be_bytes("000102030405ff"));

        [Fact]
        public void OpenATextFile()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.open_and_read(@"bar\baz.txt"))
                .Then(() => the.resulting_string_should_be("bar baz"));

        [Fact]
        public void OpenTextFiles()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.use_a_stream_to_concatenate_the_contents_of(@"bar\baz.txt", @"sub\baz.txt"))
                .Then(() => the.resulting_string_should_be("bar bazsub baz"));

        [Fact]
        public void OpenTextFilesWithPath()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.use_a_stream_to_read_path_and_content_for_each_of(@"bar\baz.txt", @"sub\baz.txt"))
                .Then(() => the.resulting_string_should_be(@"bar\baz.txt:bar bazsub\baz.txt:sub baz"));

        [Fact]
        public void ReadTextFiles()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.concatenate_the_contents_of(@"bar\baz.txt", @"sub\baz.txt"))
                .Then(() => the.resulting_string_should_be("bar bazsub baz"));

        [Fact]
        public void ReadTextFilesWithPath()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.read_path_and_content_for_each_of(@"bar\baz.txt", @"sub\baz.txt"))
                .Then(() => the.resulting_string_should_be(@"bar\baz.txt:bar bazsub\baz.txt:sub baz"));

        [Fact]
        public void AppendTextToAFile()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.append(" - this was appended").to(@"bar\baz.txt"))
                .Then(() => the.content_of(@"bar\baz.txt").should_be_the_text("bar baz - this was appended"));

        [Fact]
        public void WritingThenReadingAFileWithUTF16Encoding()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.create_a_file_under(@"bar\dejavu.txt").and_use_encoding(Encoding.Unicode).with_content("Déjà Vu"))
                .Then(() => the.content_of(@"bar\dejavu.txt").with_encoding(Encoding.Unicode).should_be("Déjà Vu"));

        [Fact]
        public void WritingToAFileWithUTF16EncodingReplacesTheContents()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.write_with_encoding(Encoding.Unicode).content("Déjà Vu").to(@"bar\baz.txt"))
                .Then(() => the.content_of(@"bar\baz.txt").with_encoding(Encoding.Unicode).should_be("Déjà Vu"));

        [Fact]
        public void BinaryWritingToAFile()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.write_bytes("44c3a96ac3a0205675").to(@"bar\baz.txt"))
                .Then(() => the.content_of(@"bar\baz.txt").should_be_the_text("Déjà Vu"));

        [Fact]
        public void AppendingToAFileWithEncodingUTF16AppendsNewContents()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.create_a_file_under(@"bar\dejavu.txt").and_use_encoding(Encoding.Unicode).with_content("Déjà Vu"))
                 .And(() => I.use_encoding(Encoding.Unicode).to_write(" Déjà Vu").to(@"bar\dejavu.txt"))
                .Then(() => the.content_of(@"bar\dejavu.txt").with_encoding(Encoding.Unicode).should_be("Déjà Vu Déjà Vu"));

        [Fact(Skip = "File encryption needs to be configured before this test can be used")]
        public void EncryptingAndDecryptingAFile()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.encrypt(@"bar\baz.txt"))
                 .And(() => I.copy_from(@"bar\baz.txt").to(@"bar\encrypted.txt"))
                 .And(() => I.decrypt(@"bar\baz.txt"))
                .Then(() => the.content_of(@"bar\baz.txt").should_be_the_text("bar baz"))
                 .And(() => the.content_of(@"bar\encrypted.txt").should_be_the_text("bar baz"))
                 .And(() => there.should_be_an_unencrypted_file_under(@"bar\baz.txt"))
                 .And(() => there.should_be_an_encrypted_file_under(@"bar\encrypted.txt"));

        [Fact]
        public void SettingAttributes()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.set_attributes(Temporary, Hidden).on(@"bar\baz.txt"))
                .Then(() => the.attributes_on(@"bar\baz.txt").should_be(Temporary, Hidden));

        [Fact]
        public void SettingCreationTime()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.set_time_of(creation).to(new DateTime(1970, 5, 21)).on(@"bar\baz.txt"))
                 .And(() => I.set_time_of(creation).to(new DateTime(1970, 5, 22)).on(@"bar\bar"))
                .Then(() => the.time_of(creation).on(@"bar\baz.txt").should_be(new DateTime(1970, 5, 21)))
                 .And(() => the.time_of(creation).on(@"bar\bar").should_be(new DateTime(1970, 5, 22)));

        [Fact]
        public void SettingUTCCreationTime()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.set_time_of(UTC_creation).to(new DateTime(1970, 5, 21)).on(@"bar\baz.txt"))
                 .And(() => I.set_time_of(UTC_creation).to(new DateTime(1970, 5, 22)).on(@"bar\bar"))
                .Then(() => the.time_of(UTC_creation).on(@"bar\baz.txt").should_be(new DateTime(1970, 5, 21)))
                 .And(() => the.time_of(UTC_creation).on(@"bar\bar").should_be(new DateTime(1970, 5, 22)));

        [Fact]
        public void SettingLastAccessTime()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.set_time_of(last_access).to(new DateTime(1970, 5, 21)).on(@"bar\baz.txt"))
                 .And(() => I.set_time_of(last_access).to(new DateTime(1970, 5, 22)).on(@"bar\bar"))
                .Then(() => the.time_of(last_access).on(@"bar\baz.txt").should_be(new DateTime(1970, 5, 21)))
                 .And(() => the.time_of(last_access).on(@"bar\bar").should_be(new DateTime(1970, 5, 22)));

        [Fact]
        public void SettingUTCLastAccessTime()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.set_time_of(UTC_last_access).to(new DateTime(1970, 5, 21)).on(@"bar\baz.txt"))
                 .And(() => I.set_time_of(UTC_last_access).to(new DateTime(1970, 5, 22)).on(@"bar\bar"))
                .Then(() => the.time_of(UTC_last_access).on(@"bar\baz.txt").should_be(new DateTime(1970, 5, 21)))
                 .And(() => the.time_of(UTC_last_access).on(@"bar\bar").should_be(new DateTime(1970, 5, 22)));

        [Fact]
        public void SettingLastWriteTime()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.set_time_of(last_write).to(new DateTime(1970, 5, 21)).on(@"bar\baz.txt"))
                 .And(() => I.set_time_of(last_write).to(new DateTime(1970, 5, 22)).on(@"bar\bar"))
                .Then(() => the.time_of(last_write).on(@"bar\baz.txt").should_be(new DateTime(1970, 5, 21)))
                 .And(() => the.time_of(last_write).on(@"bar\bar").should_be(new DateTime(1970, 5, 22)));

        [Fact]
        public void SettingUTCLastWriteTime()
            => Scenario
                .Given(I.start_with_a_clean_directory)
                .When(() => I.set_time_of(UTC_last_write).to(new DateTime(1970, 5, 21)).on(@"bar\baz.txt"))
                 .And(() => I.set_time_of(UTC_last_write).to(new DateTime(1970, 5, 22)).on(@"bar\bar"))
                .Then(() => the.time_of(UTC_last_write).on(@"bar\baz.txt").should_be(new DateTime(1970, 5, 21)))
                 .And(() => the.time_of(UTC_last_write).on(@"bar\bar").should_be(new DateTime(1970, 5, 22)));
    }
}
