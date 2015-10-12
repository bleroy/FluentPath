// Copyright © 2010-2015 Bertrand Le Roy.  All Rights Reserved.
// This code released under the terms of the 
// MIT License http://opensource.org/licenses/MIT

using System;
using System.Text;
using NUnit.Framework;
using TechTalk.SpecFlow;
using Fluent.Zip;
using Fluent.IO;

namespace FluentZipSpec {
    [Binding]
    public class FluentPathSteps {
        private Path _path;
        private byte[] _zipped;

        [Given("a clean test directory")]
        public void GivenACleanDirectory() {
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
            _path = new Path(System.IO.Path.GetTempPath())
                .CreateSubDirectory("FluentPathSpecs")
                .CreateSubDirectory("Source")
                .MakeCurrent();
            _path
                .FileSystemEntries()
                .Delete(true);
            _path.CreateFile("foo.txt", "This is a text file named foo.");
            var bar = _path.CreateSubDirectory("bar");
            bar.CreateFile("baz.txt", "bar baz")
               .LastWriteTime(DateTime.Now.AddSeconds(-2));
            bar.CreateFile("notes.txt", "This is a text file containing notes.");
            var barbar = bar.CreateSubDirectory("bar");
            barbar.CreateFile("deep.txt", "Deep thoughts");
            var sub = _path.CreateSubDirectory("sub");
            sub.CreateSubDirectory("subsub");
            sub.CreateFile("baz.txt", "sub baz")
               .LastWriteTime(DateTime.Now);
            sub.CreateFile("binary.bin",
                new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0xFF });
        }

        [When(@"I zip ([^\s]*) into ([^\s]*)")]
        public void WhenIZip(string source, string destination) {
            var sourcePath = _path.Combine(source.Split('\\'));
            var destinationPath = _path.Combine(destination.Split('\\'));
            sourcePath.Zip(destinationPath);
        }

        [When(@"I zip ""([^""]*)"" in memory as ([^\s]*)")]
        public void WhenIZipInMemory(string content, string path) {
            _zipped = ZipExtensions.Zip(
                new Path(path),
                p => Encoding.Default.GetBytes(content));
        }

        [When(@"I unzip ([^\s]*) into ([^\s]*)")]
        public void WhenIUnzip(string source, string destination) {
            var sourcePath = _path.Combine(source.Split('\\'));
            var destinationPath = _path.Combine(destination.Split('\\'));
            sourcePath.Unzip(destinationPath);
        }

        [Then(@"([^\s]*) should exist")]
        public void ThenFileShouldExist(string file) {
            Assert.IsTrue(_path.Combine(file.Split('\\')).Exists);
        }

        [Then(@"the contents of ([^\s]*) should be identical to the contents of ([^\s]*)")]
        public void ThenContentsOfDirectoriesShouldBeIdentical(string path1, string path2) {
            var p1 = _path.Combine(path1.Split('\\'));
            var files1 = p1.AllFiles();
            var p2 = _path.Combine(path2.Split('\\'));
            var files2 = p2.AllFiles();
            Assert.IsTrue(files1.MakeRelativeTo(p1) == files2.MakeRelativeTo(p2));
            files1.ReadBytes((ba, p) =>
                Assert.That(ba, Is.EquivalentTo(p2.Combine((string) p.MakeRelativeTo(p1)).ReadBytes())));
        }

        [Then(@"the content of the in-memory zip is ([^\s]*):""([^""]*)""")]
        public void ThenTheContentOfTheInMemoryZipIs(string path, string content) {
            ZipExtensions.Unzip(_zipped,
                                (p, ba) => {
                                    Assert.That(p.ToString().Replace('/', '\\'), Is.EqualTo(path));
                                    Assert.That(Encoding.Default.GetString(ba), Is.EqualTo(content));
                                });
        }
    }
}