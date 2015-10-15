// Copyright © 2010-2015 Bertrand Le Roy.  All Rights Reserved.
// This code released under the terms of the 
// MIT License http://opensource.org/licenses/MIT

using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace FluentPathSpec {
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SystemIO = System.IO;
using Fluent.IO;
using Fluent.IO.Windows;
using NUnit.Framework;
using TechTalk.SpecFlow;
using System.Globalization;

    [Binding]
    public class FluentPathSteps {
        private Path _path;
        private Path _result;
        private string _resultString;

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
            _path = new Path(SystemIO.Path.GetTempPath())
                .CreateSubDirectory("FluentPathSpecs")
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
                new byte[] {0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0xFF});
        }

        [When(@"I add a permission to ([^\s]*)")]
        public void WhenIAddPermission(string path) {
// ReSharper disable PossibleNullReferenceException
            SecurityIdentifier user = WindowsIdentity.GetCurrent().User;
// ReSharper restore PossibleNullReferenceException
            var filePath = _path.Combine(path.Split('\\'));
            var accessControl = filePath.AccessControl();
            accessControl.AddAccessRule(
                    new FileSystemAccessRule(
                        user, FileSystemRights.Read, InheritanceFlags.None,
                        PropagationFlags.None, AccessControlType.Allow)
                );
            filePath.AccessControl(accessControl);
        }

        [When("I recursively select (.*)")]
        public void WhenIRecursivelySelect(string searchPattern) {
            _result = _path.FileSystemEntries(searchPattern, true);
        }

        [When("I select subdirectories")]
        public void WhenISelectSubdirectories() {
            _result = _path.Directories();
        }

        [When("I select deep subdirectories")]
        public void WhenISelectDeepSubdirectories() {
            _result = _path.Directories("*", true);
        }

        [When("I search for subdirectories with pattern (.*)")]
        public void WhenISelectDeepSubdirectoriesWithAPattern(string pattern) {
            _result = _path.Directories(pattern, true);
        }

        [When("I search for subdirectories with a condition")]
        public void WhenISelectSubdirectoriesWithACondition() {
            _result = _path.Directories(p => p.FileName.StartsWith("sub"));
        }

        [When("I search for deep subdirectories with a condition")]
        public void WhenISelectDeepSubdirectoriesWithACondition() {
            _result = _path.Directories(p => p.FileName.StartsWith("sub"), true);
        }

        [When("I select all files")]
        public void WhenISelectAllFiles() {
            _result = _path.AllFiles();
        }

        [When("I select files")]
        public void WhenISelectFiles() {
            _result = _path.Files();
        }

        [When("I search for files with a condition")]
        public void WhenISearchForFilesWithACondition() {
            _result = _path.Files(f => f.Extension == ".txt");
        }

        [When("I search for deep files with a condition")]
        public void WhenISearchForDeepFilesWithACondition() {
            _result = _path.Files(f => f.Extension == ".txt", true);
        }

        [When("I search for deep files with pattern (.*)")]
        public void WhenISearchForDeepFilesWithPattern(string pattern) {
            _result = _path.Files(pattern, true);
        }

        [When(@"I select files with extensions (.*)")]
        public void WhenISelectFilesWithExtension(string extensions) {
            _result = _path.Files("*", true)
                .WhereExtensionIs(
                    extensions.Split(',').Select(s => s.Trim()).ToArray());
        }

        [When("I select file system entries")]
        public void WhenISelectFileSystemEntries() {
            _result = _path.FileSystemEntries();
        }

        [When("I search for file system entries with a condition")]
        public void WhenISearchForFileSystemEntriesWithACondition() {
            _result = _path.FileSystemEntries(f => f.FileName.IndexOf('a') != -1);
        }

        [When("I search for deep file system entries with a condition")]
        public void WhenISearchForDeepFileSystemEntriesWithACondition() {
            _result = _path.FileSystemEntries(f => f.FileName.IndexOf('a') != -1, true);
        }

        [When("I search for deep file system entries with pattern (.*)")]
        public void WhenISearchForDeepFileSystemEntriesWithPattern(string pattern) {
            _result = _path.FileSystemEntries(pattern, true);
        }

        [When(@"I set attributes (.*) on ([^\s]*)")]
        public void WhenISetAttributes(string attributeNames, string path) {
            var attrNames = attributeNames.Split(',');
            var attributes =
                attrNames.Aggregate<string, FileAttributes>(0,
                    (current, attrName) => current |
                    (FileAttributes)Enum.Parse(typeof (FileAttributes), attrName));
            _path.Combine(path.Split('\\')).Attributes(attributes);
        }

        [When(@"I set (.*) time on ([^\s]*) to (.*)")]
        public void WhenISetTime(string type, string path, string dateString) {
            var combinedPath = _path.Combine(path.Split('\\'));
            var date = DateTime.Parse(dateString, CultureInfo.InvariantCulture);
            switch (type) {
                case "creation":
                    combinedPath.CreationTime(date);
                    break;
                case "UTC creation":
                    combinedPath.CreationTimeUtc(date);
                    break;
                case "last access":
                    combinedPath.LastAccessTime(date);
                    break;
                case "UTC last access":
                    combinedPath.LastAccessTimeUtc(date);
                    break;
                case "last write":
                    combinedPath.LastWriteTime(date);
                    break;
                case "UTC last write":
                    combinedPath.LastWriteTimeUtc(date);
                    break;
            }
        }

        [When("I copy (.*) to (.*)")]
        public void WhenICopy(string from, string to) {
            var src = _path.Combine(from);
            src.Copy(_path.Combine(to.Split('\\')));
        }

        [When("I overwrite (.*) copy with (.*) to (.*)")]
        public void WhenICopyWithOverwrite(string overwriteMode, string from, string to) {
            var overwrite = (Overwrite)Enum.Parse(typeof(Overwrite), overwriteMode, true);
            var src = _path.Combine(from);
            try {
                src.Copy(_path.Combine(to.Split('\\')), overwrite);
            }
            catch(Exception e) {
                ScenarioContext.Current["Exception"] = e;
            }
        }

        [When("I copy (.*) with a transform")]
        public void WhenICopyWithATransform(string path) {
            Path.Current.Combine(path.Split('\\'))
                .Copy(
                    p => p.Parent().Combine(
                        p.FileNameWithoutExtension + p.FileNameWithoutExtension + p.Extension));
        }

        [When("I recursively copy (.*) to (.*)")]
        public void WhenIRecursivelyCopy(string from, string to) {
            var src = _path.Combine(from);
            src.Copy(_path.Combine(to.Split('\\')), Overwrite.Never, true);
        }

        [When("I move (.*) to (.*)")]
        public void WhenIMove(string from, string to) {
            var src = _path.Combine(from);
            src.Move((string)_path.Combine(to.Split('\\')));
        }

        [When("I overwrite (.*) move with (.*) to (.*)")]
        public void WhenIMoveWithOverwrite(string overwriteMode, string from, string to) {
            var overwrite = (Overwrite)Enum.Parse(typeof(Overwrite), overwriteMode, true);
            var src = _path.Combine(from);
            try {
                src.Move((string)_path.Combine(to.Split('\\')), overwrite);
            }
            catch (Exception e) {
                ScenarioContext.Current["Exception"] = e;
            }
        }

        [When("I move (.*) with a transform")]
        public void WhenIMoveWithATransform(string path) {
            Path.Current.Combine(path.Split('\\'))
                .Move(
                    p => p.Parent().Combine(
                        p.FileNameWithoutExtension + p.FileNameWithoutExtension +
                        p.Extension));
        }

        [When(@"I open ([^\s]*)")]
        public void WhenIOpen(string path) {
            Path.Current.Combine(path.Split('\\'))
                .Open(s => {
                          using (var reader = new StreamReader(s)) {
                              _resultString = reader.ReadToEnd();
                          }
                      });
        }

        [When(@"I process the path and content of (.*)")]
        public void WhenIProcessThePathAndContentOf(string path) {
            Path.Current.Combine(path.Split('\\'))
                .Process((p, s) => s.ToUpperInvariant() + " - processed " + p.FileName);
        }

        [When(@"I process the content of (.*)")]
        public void WhenIProcessTheContentOf(string path) {
            Path.Current.Combine(path.Split('\\'))
                .Process(s => s.ToUpperInvariant() + " - processed");
        }

        [When(@"I append ""(.*)"" to ([^\s]*)")]
        public void WhenIAppend(string text, string path) {
            Path.Current.Combine(path.Split('\\'))
                .Write(text, true);
        }

        [When(@"I append ""(.*)"" to ([^\s]*) using ([^\s]*) encoding")]
        public void WhenIAppendEncoded(string content, string path, string encodingName) {
            var encoding = Encoding.GetEncoding(encodingName);
            _path.Combine(path.Split('\\')).Write(
                content, encoding, true);
        }

        [When(@"I replace the text of (.*) with ""(.*)""")]
        public void WhenIReplace(string path, string text) {
            Path.Current.Combine(path.Split('\\'))
                .Write(text);
        }

        [When(@"I binary process the content of (.*)")]
        public void WhenIBinaryProcessTheContentOf(string path) {
            Path.Current.Combine(path.Split('\\'))
                .Process(
                    ba => {
                        for (var i = 0; i < ba.Length; i++) {
                            ba[i] ^= 0xFF;
                        }
                        return ba;
                    }
                );
        }

        [When(@"I binary process the path and content of (.*)")]
        public void WhenIBinaryProcessThePathAndContentOf(string path) {
            Path.Current.Combine(path.Split('\\'))
                .Process(
                    (p, ba) => {
                        for (var i = 0; i < ba.Length; i++) {
                            ba[i] ^= 0xFF;
                        }
                        _resultString = p.FileName;
                        return ba;
                    }
                );
        }

        [When("I get the path for (.*)")]
        public void WhenIGetThePathFor(string path) {
            _path = _path.Combine(path.Split('\\'));
        }

        [When("I create that directory")]
        public void WhenICreateThatDirectory() {
            _path.CreateDirectory();
        }

        [When("I create a directory from (.*)")]
        public void WhenICreateADirectoryFrom(string path) {
            Path.CreateDirectory(
                Path.Current.Combine(path.Split('\\')).FullPath);
        }

        [When("I open (.*) and read the contents")]
        public void WhenIOpenFilesAndReadContents(string fileList) {
            var files =
                new Path(
                    fileList.Split(
                        new[] {',', ' '}, StringSplitOptions.RemoveEmptyEntries));
            files.Open(
                s => {
                    using (var reader = new StreamReader(s)) {
                        _resultString += reader.ReadToEnd();
                    }
                });
        }

        [When("I open (.*) and read the path and contents")]
        public void WhenIOpenFilesAndReadPathAndContents(string fileList) {
            var files =
                new Path(
                    fileList.Split(
                        new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries));
            files.Open(
                (s, p) => {
                    using (var reader = new StreamReader(s)) {
                        _resultString += p.ToString() + ":" + reader.ReadToEnd();
                    }
                });
        }

        [When("I read the contents of (.*)")]
        public void WhenIReadContents(string fileList) {
            var files =
                new Path(
                    fileList.Split(
                        new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries));
            files.Read(
                s => {_resultString += s;});
        }

        [When("I read the path and contents of (.*)")]
        public void WhenIReadPathAndContents(string fileList) {
            var files =
                new Path(
                    fileList.Split(
                        new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries));
            files.Read(
                (s, p) => {
                    _resultString += p.ToString() + ":" + s;
                });
        }

        [When(@"I use a Lambda to create directories with the same names as file in ([^\s]*)")]
        public void WhenIUseALambdaToCreateDirectoriesUnder(string path) {
            _result = _path.Combine(path.Split('\\'))
                .AllFiles()
                .CreateDirectories(p => p.FileNameWithoutExtension);
        }

        [When("I create a (.*) subdirectory from (.*)")]
        public void WhenICreateASubdirectory(string subdirectoryName, string path) {
            Path.Current.Combine(path.Split('\\'))
                .CreateSubDirectory(subdirectoryName);
        }

        [When(@"I create ([^\s]*) subdirectories under (.*)")]
        public void WhenICreateSubDirectoriesUnder(string directoryName, string targetDirectoryNames) {
            var targets =
                new Path(
                    targetDirectoryNames.Split(
                        new[] {',', ' '}, StringSplitOptions.RemoveEmptyEntries));
            targets.CreateDirectories(directoryName);
        }

        [When(@"I create a (.*) text file with the text ""(.*)""")]
        public void WhenICreateATextFileWithText(string path, string content) {
            Path.Current.CreateFiles(
                p => (Path)path.Replace('\\', SystemIO.Path.DirectorySeparatorChar),
                p => content);
        }

        [When(@"I create a (.*) encoded (.*) file with the text ""(.*)""")]
        public void WhenICreateAnEncodedTextFileWithText(string encodingName, string path, string content) {
            var encoding = Encoding.GetEncoding(encodingName);
            Path.Current.CreateFiles(
                p => (Path)path.Replace('\\', SystemIO.Path.DirectorySeparatorChar),
                p => content,
                encoding);
        }

        [When("I create a (.*) binary file with (.*)")]
        public void WhenICreateABinaryFileWith(string path, string hexContent) {
            var content = new byte[hexContent.Length / 2];
            for (var i = 0; i < hexContent.Length; i += 2 ) {
                content[i/2] = byte.Parse(
                    hexContent.Substring(i, 2), NumberStyles.HexNumber);
            }
            Path.Current.CreateFiles(
                p => (Path)path.Replace('\\', SystemIO.Path.DirectorySeparatorChar),
                p => content);
        }

        [When("I change the extension of (.*) to (.*)")]
        public void WhenIChangeTheExtension(string path, string newExtension) {
            var oldPath = Path.Current.Combine(path.Split('\\'));
            oldPath.Move(p => p.ChangeExtension(newExtension));
        }

        [When(@"I delete file ([^\s]*)")]
        public void WhenIDeleteFile(string path) {
            Path.Current.Combine(path.Split('\\'))
                .Delete();
        }

        [When(@"I delete directory ([^\s]*)")]
        public void WhenIDeleteDirectory(string path) {
            Path.Current.Combine(path.Split('\\'))
                .Delete(true);
        }

        [When("I decrypt (.*)")]
        public void WhenIDecrypt(string path) {
            Path.Current.Combine(path.Split('\\'))
                .Decrypt();
        }

        [When("I encrypt (.*)")]
        public void WhenIEncrypt(string path) {
            Path.Current.Combine(path.Split('\\'))
                .Encrypt();
        }

        [When("I enumerate directories twice")]
        public void WhenIEnumerateDirectoriesTwice() {
            var dirs = _path.Directories();
            var dirEnum = dirs.GetEnumerator();
            var dirList = new List<string>();
            while (dirEnum.MoveNext()) {
                dirList.Add(dirEnum.Current.FileName);
            }
            dirEnum = dirs.GetEnumerator();
            var dirEnumNonGeneric = (IEnumerator)dirEnum;
            while (dirEnumNonGeneric.MoveNext()) {
                dirList.Add(((Path)dirEnumNonGeneric.Current).FileName);
            }
            dirList.Sort();
            _resultString = String.Join(", ", dirList.ToArray());
        }

        [When(@"I grep for ""(.*)""")]
        public void WhenIGrepFor(string regularExpression) {
            var matches = new List<string>();
            Path.Current.AllFiles().Grep(
                regularExpression, (p, match, content) => matches.Add(
                    p.MakeRelative().ToString() + ":" +
                    match.Index));
            _resultString = string.Join(", ", matches);
        }

        [When(@"I grep for ""(.*)"" in ([^\s]*)")]
        public void WhenIGrepIn(string regularExpression, string path) {
            var matches = new List<int>();
            _path.Combine(path.Split('\\')).Grep(
                regularExpression, (p, match, content) => matches.Add(match.Index));
            _resultString = string.Join(", ", matches);
        }

        [When(@"I write ""(.*)"" to ([^\s]*) using ([^\s]*) encoding")]
        public void WhenIWriteEncoded(string content, string path, string encodingName) {
            var encoding = Encoding.GetEncoding(encodingName);
            _path.Combine(path.Split('\\')).Write(content, encoding);
        }

        [When(@"I use a Lambda to copy text files into ([^\s]*)")]
        public void WhenIUseALambdaToCopyTextFiles(string destination) {
            var files = _path.AllFiles();
            _result = files.Copy(
                p =>
                p.Extension == ".txt" ? _path.Combine(destination, p.FileName) : null);
        }

        [When(@"I use a Lambda to move text files into ([^\s]*)")]
        public void WhenIUseALambdaToMoveTextFiles(string destination) {
            var files = _path.AllFiles();
            _result = files.Move(
                p =>
                p.Extension == ".txt" ? _path.Combine(destination, p.FileName) : null,
                Overwrite.Always);
        }

        [When(@"I write bytes ([^\s]*) to ([^\s]*)")]
        public void WhenIWriteBytes(string hexContent, string path) {
            var content = new byte[hexContent.Length / 2];
            for (var i = 0; i < hexContent.Length; i += 2) {
                content[i / 2] = byte.Parse(
                    hexContent.Substring(i, 2), NumberStyles.HexNumber);
            }
            _path.Combine(path.Split('\\'))
                .Write(content);
        }

        [Then(@"attributes (.*) should be set on ([^\s]*)")]
        public void ThenAttributesShouldBeSet(string attributeNames, string path) {
            var attrNames = attributeNames.Split(',');
            var fileAttributes = _path.Combine(path.Split('\\')).Attributes();
            foreach (var attrName in attrNames) {
                Assert.AreNotEqual(0,
                    fileAttributes &
                    (FileAttributes)Enum.Parse(typeof (FileAttributes), attrName));
            }
        }

        [Then("the resulting set should be (.*)")]
        public void ThenTheResultingSetShouldBe(string fileList) {
            var resultList = _result.Select(
                p => (p.IsRooted ? p.MakeRelative() : p).ToString().Replace(
                    SystemIO.Path.DirectorySeparatorChar, '\\')).ToList();
            Assert.That(
                resultList,
                Is.EquivalentTo(
                    fileList.Split(
                        new[] {',', ' '}, StringSplitOptions.RemoveEmptyEntries)));
        }

        [Then(@"the resulting string should be ""(.*)""")]
        public void ThenTheResultingStringShouldBe(string resultString) {
            Assert.AreEqual(resultString, _resultString);
        }

        [Then(@"the content of ([^\s]*) should be (.*)")]
        public void ThenTheContentOfFolderShouldBe(string folder, string fileList) {
            var folderPath = _path.Combine(folder);
            var files = folderPath.FileSystemEntries()
                .MakeRelativeTo(folderPath)
                .Select(
                    p => p.ToString().Replace(SystemIO.Path.DirectorySeparatorChar, '\\'))
                .ToList();
            Assert.That(files,
                Is.EquivalentTo(
                    fileList.Split(
                        new[] {',', ' '}, StringSplitOptions.RemoveEmptyEntries)));
        }

        [Then("(.*) should exist")]
        public void ThenEntryShouldExist(string path) {
            Assert.IsTrue(_path.Combine(path.Split('\\')).Exists);
        }

        [Then("(.*) should not exist")]
        public void ThenEntryShouldNotExist(string path) {
            Assert.IsFalse(_path.Combine(path.Split('\\')).Exists);
        }

        [Then("(.*) should be thrown")]
        public void ThenExceptionShouldBeThrown(string exceptionTypeName) {
            var e = ScenarioContext.Current["Exception"];
            Assert.IsNotNull(e);
            Assert.AreEqual(exceptionTypeName, e.GetType().Name);
        }

        [Then("(.*) should be encrypted")]
        public void ThenFileShouldBeEncrypted(string path) {
            Assert.IsTrue(Path.Current.Combine(path.Split('\\')).IsEncrypted);
        }

        [Then("(.*) should not be encrypted")]
        public void ThenFileShouldNotBeEncrypted(string path) {
            Assert.IsFalse(Path.Current.Combine(path.Split('\\')).IsEncrypted);
        }

        [Then(@"there is an additional permission on ([^\s]*)")]
        public void ThenThereIsAnAdditionalPermissionOn(string path) {
// ReSharper disable PossibleNullReferenceException
            SecurityIdentifier user = WindowsIdentity.GetCurrent().User;
// ReSharper restore PossibleNullReferenceException
            var accessRules = _path.Combine(path.Split('\\'))
                .AccessControl().GetAccessRules(
                    true, false, typeof (SecurityIdentifier));
            bool found = accessRules
                .Cast<FileSystemAccessRule>()
                .Any(accessRule => accessRule.IdentityReference.Equals(user) &&
                    !accessRule.IsInherited &&
                    accessRule.AccessControlType == AccessControlType.Allow &&
                    (accessRule.FileSystemRights & FileSystemRights.Read) != 0 &&
                    accessRule.InheritanceFlags == InheritanceFlags.None &&
                    accessRule.PropagationFlags == PropagationFlags.None);
            Assert.IsTrue(found);
        }

        [Then("there should be a (.*) directory")]
        public void ThenThereShouldBeADirectory(string directory) {
            var path = Path.Current.Combine(directory.Split('\\'));
            Assert.IsTrue(path.Exists && path.IsDirectory);
        }

        [Then(@"the text content of ([^\s]*) should be ""(.*)""")]
        public void ThenTheTextContentShouldBe(string path, string textContent) {
            Assert.AreEqual(textContent, Path.Current.Combine(path.Split('\\')).Read());
        }

        [Then(@"the binary content of ([^\s]*) should be (.*)")]
        public void ThenTheBinaryContentShouldBe(string path, string binaryContent) {
            string binaryContentString = null;
            Path.Current.Combine(path.Split('\\')).
                ReadBytes(actualBinaryContent =>
                binaryContentString = String.Join(
                    "",
                    (from b in actualBinaryContent
                     select b.ToString("x2"))
                        .ToArray()
                    ));
            Assert.AreEqual(binaryContent, binaryContentString);
        }

        [Then(@"the text content of ([^\s]*) as read using ([^\s]*) encoding should be ""(.*)""")]
        public void ThenTheContentReadWithEncodingShouldBe(string path, string encodingName, string content) {
            var encoding = Encoding.GetEncoding(encodingName);
            string readContent = null;
                Path.Current.Combine(path.Split('\\')).Read(
                    s => readContent = s, encoding);
            Assert.That(
                readContent, Is.EqualTo(
                    content));
        }

        [Then(@"the (.*) time on ([^\s]*) is (.*)")]
        public void ThenTimeIs(string type, string path, string dateString) {
            var combinedPath = _path.Combine(path.Split('\\'));
            var date = DateTime.Parse(dateString, CultureInfo.InvariantCulture);
            switch (type) {
                case "creation":
                    Assert.AreEqual(date, combinedPath.CreationTime());
                    break;
                case "UTC creation":
                    Assert.AreEqual(date, combinedPath.CreationTimeUtc());
                    break;
                case "last access":
                    Assert.AreEqual(date, combinedPath.LastAccessTime());
                    break;
                case "UTC last access":
                    Assert.AreEqual(date, combinedPath.LastAccessTimeUtc());
                    break;
                case "last write":
                    Assert.AreEqual(date, combinedPath.LastWriteTime());
                    break;
                case "UTC last write":
                    Assert.AreEqual(date, combinedPath.LastWriteTimeUtc());
                    break;
            }
        }
    }
}
