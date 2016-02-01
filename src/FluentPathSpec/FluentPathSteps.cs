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
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using SystemIO = System.IO;
    using Fluent.IO;
    using Fluent.IO.Windows;
    using Cornichon;
    using Xunit;

    public class FluentPathSteps {
        private Path _path;
        private Path _result;
        private string _resultString;

        public void HaveACleanDirectory() {
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

        public void AddPermission(string path) {
            SecurityIdentifier user = WindowsIdentity.GetCurrent().User;
            var filePath = _path.Combine(path.Split('\\'));
            var accessControl = filePath.AccessControl();
            accessControl.AddAccessRule(
                    new FileSystemAccessRule(
                        user, FileSystemRights.Read, InheritanceFlags.None,
                        PropagationFlags.None, AccessControlType.Allow)
                );
            filePath.AccessControl(accessControl);
        }

        public void RecursivelySelect(string searchPattern) {
            _result = _path.FileSystemEntries(searchPattern, true);
        }

        public void SelectSubdirectories() {
            _result = _path.Directories();
        }

        public void SelectDeepSubdirectories() {
            _result = _path.Directories("*", true);
        }

        public void SelectDeepSubdirectoriesWithAPattern(string pattern) {
            _result = _path.Directories(pattern, true);
        }

        public void SelectSubdirectoriesWithACondition() {
            _result = _path.Directories(p => p.FileName.StartsWith("sub"));
        }

        public void SelectDeepSubdirectoriesWithACondition() {
            _result = _path.Directories(p => p.FileName.StartsWith("sub"), true);
        }

        public void SelectAllFiles() {
            _result = _path.AllFiles();
        }

        public void SelectFiles() {
            _result = _path.Files();
        }

        public void SearchForFilesWithACondition() {
            _result = _path.Files(f => f.Extension == ".txt");
        }

        public void SearchForDeepFilesWithACondition() {
            _result = _path.Files(f => f.Extension == ".txt", true);
        }

        public void SearchForDeepFilesWithPattern(string pattern) {
            _result = _path.Files(pattern, true);
        }

        public void SelectFilesWithExtension(string extensions) {
            _result = _path.Files("*", true)
                .WhereExtensionIs(
                    extensions.Split(',').Select(s => s.Trim()).ToArray());
        }

        public void SelectFileSystemEntries() {
            _result = _path.FileSystemEntries();
        }

        public void SearchForFileSystemEntriesWithACondition() {
            _result = _path.FileSystemEntries(f => f.FileName.IndexOf('a') != -1);
        }

        public void SearchForDeepFileSystemEntriesWithACondition() {
            _result = _path.FileSystemEntries(f => f.FileName.IndexOf('a') != -1, true);
        }

        public void SearchForDeepFileSystemEntriesWithPattern(string pattern) {
            _result = _path.FileSystemEntries(pattern, true);
        }

        public void SetAttributes(string attributeNames, string path) {
            var attrNames = attributeNames.Split(',');
            var attributes =
                attrNames.Aggregate<string, FileAttributes>(0,
                    (current, attrName) => current |
                    (FileAttributes)Enum.Parse(typeof (FileAttributes), attrName));
            _path.Combine(path.Split('\\')).Attributes(attributes);
        }

        public void SetTime(string type, string path, string dateString) {
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

        public void Copy(string from, string to) {
            var src = _path.Combine(from);
            src.Copy(_path.Combine(to.Split('\\')));
        }

        public void CopyWithOverwrite(string overwriteMode, string from, string to) {
            var overwrite = (Overwrite)Enum.Parse(typeof(Overwrite), overwriteMode, true);
            var src = _path.Combine(from);
            Assert.Throws<Exception>(() =>
            {
                src.Copy(_path.Combine(to.Split('\\')), overwrite);
            });
        }

        public void CopyWithATransform(string path) {
            Path.Current.Combine(path.Split('\\'))
                .Copy(
                    p => p.Parent().Combine(
                        p.FileNameWithoutExtension + p.FileNameWithoutExtension + p.Extension));
        }

        public void RecursivelyCopy(string from, string to) {
            var src = _path.Combine(from);
            src.Copy(_path.Combine(to.Split('\\')), Overwrite.Never, true);
        }

        public void Move(string from, string to) {
            var src = _path.Combine(from);
            src.Move((string)_path.Combine(to.Split('\\')));
        }

        public void MoveWithOverwrite(string overwriteMode, string from, string to) {
            var overwrite = (Overwrite)Enum.Parse(typeof(Overwrite), overwriteMode, true);
            var src = _path.Combine(from);
            Assert.Throws<Exception>(() =>
            {
                src.Move((string)_path.Combine(to.Split('\\')), overwrite);
            });
        }

        public void MoveWithATransform(string path) {
            Path.Current.Combine(path.Split('\\'))
                .Move(
                    p => p.Parent().Combine(
                        p.FileNameWithoutExtension + p.FileNameWithoutExtension +
                        p.Extension));
        }

        public void Open(string path) {
            Path.Current.Combine(path.Split('\\'))
                .Open(s => {
                          using (var reader = new StreamReader(s)) {
                              _resultString = reader.ReadToEnd();
                          }
                      });
        }

        public void ProcessThePathAndContentOf(string path) {
            Path.Current.Combine(path.Split('\\'))
                .Process((p, s) => s.ToUpperInvariant() + " - processed " + p.FileName);
        }

        public void ProcessTheContentOf(string path) {
            Path.Current.Combine(path.Split('\\'))
                .Process(s => s.ToUpperInvariant() + " - processed");
        }

        public void Append(string text, string path) {
            Path.Current.Combine(path.Split('\\'))
                .Write(text, true);
        }

        public void AppendEncoded(string content, string path, string encodingName) {
            var encoding = Encoding.GetEncoding(encodingName);
            _path.Combine(path.Split('\\')).Write(
                content, encoding, true);
        }

        public void Replace(string path, string text) {
            Path.Current.Combine(path.Split('\\'))
                .Write(text);
        }

        public void BinaryProcessTheContentOf(string path) {
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

        public void BinaryProcessThePathAndContentOf(string path) {
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

        public void GetThePathFor(string path) {
            _path = _path.Combine(path.Split('\\'));
        }

        public void CreateThatDirectory() {
            _path.CreateDirectory();
        }

        public void CreateADirectoryFrom(string path) {
            Path.CreateDirectory(
                Path.Current.Combine(path.Split('\\')).FullPath);
        }

        public void OpenFilesAndReadContents(string fileList) {
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

        public void OpenFilesAndReadPathAndContents(string fileList) {
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

        public void ReadContents(string fileList) {
            var files =
                new Path(
                    fileList.Split(
                        new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries));
            files.Read(
                s => {_resultString += s;});
        }

        public void ReadPathAndContents(string fileList) {
            var files =
                new Path(
                    fileList.Split(
                        new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries));
            files.Read(
                (s, p) => {
                    _resultString += p.ToString() + ":" + s;
                });
        }

        public void UseALambdaToCreateDirectoriesUnder(string path) {
            _result = _path.Combine(path.Split('\\'))
                .AllFiles()
                .CreateDirectories(p => p.FileNameWithoutExtension);
        }

        public void CreateASubdirectory(string subdirectoryName, string path) {
            Path.Current.Combine(path.Split('\\'))
                .CreateSubDirectory(subdirectoryName);
        }

        public void CreateSubDirectoriesUnder(string directoryName, string targetDirectoryNames) {
            var targets =
                new Path(
                    targetDirectoryNames.Split(
                        new[] {',', ' '}, StringSplitOptions.RemoveEmptyEntries));
            targets.CreateDirectories(directoryName);
        }

        public void CreateATextFileWithText(string path, string content) {
            Path.Current.CreateFiles(
                p => (Path)path.Replace('\\', SystemIO.Path.DirectorySeparatorChar),
                p => content);
        }

        public void CreateAnEncodedTextFileWithText(string encodingName, string path, string content) {
            var encoding = Encoding.GetEncoding(encodingName);
            Path.Current.CreateFiles(
                p => (Path)path.Replace('\\', SystemIO.Path.DirectorySeparatorChar),
                p => content,
                encoding);
        }

        public void CreateABinaryFileWith(string path, string hexContent) {
            var content = new byte[hexContent.Length / 2];
            for (var i = 0; i < hexContent.Length; i += 2 ) {
                content[i/2] = byte.Parse(
                    hexContent.Substring(i, 2), NumberStyles.HexNumber);
            }
            Path.Current.CreateFiles(
                p => (Path)path.Replace('\\', SystemIO.Path.DirectorySeparatorChar),
                p => content);
        }

        public void ChangeTheExtension(string path, string newExtension) {
            var oldPath = Path.Current.Combine(path.Split('\\'));
            oldPath.Move(p => p.ChangeExtension(newExtension));
        }

        public void DeleteFile(string path) {
            Path.Current.Combine(path.Split('\\'))
                .Delete();
        }

        public void DeleteDirectory(string path) {
            Path.Current.Combine(path.Split('\\'))
                .Delete(true);
        }

        public void Decrypt(string path) {
            Path.Current.Combine(path.Split('\\'))
                .Decrypt();
        }

        public void Encrypt(string path) {
            Path.Current.Combine(path.Split('\\'))
                .Encrypt();
        }

        public void EnumerateDirectoriesTwice() {
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

        public void GrepFor(string regularExpression) {
            var matches = new List<string>();
            Path.Current.AllFiles().Grep(
                regularExpression, (p, match, content) => matches.Add(
                    p.MakeRelative().ToString() + ":" +
                    match.Index));
            _resultString = string.Join(", ", matches);
        }

        public void GrepIn(string regularExpression, string path) {
            var matches = new List<int>();
            _path.Combine(path.Split('\\')).Grep(
                regularExpression, (p, match, content) => matches.Add(match.Index));
            _resultString = string.Join(", ", matches);
        }

        public void WriteEncoded(string content, string path, string encodingName) {
            var encoding = Encoding.GetEncoding(encodingName);
            _path.Combine(path.Split('\\')).Write(content, encoding);
        }

        public void UseALambdaToCopyTextFiles(string destination) {
            var files = _path.AllFiles();
            _result = files.Copy(
                p =>
                p.Extension == ".txt" ? _path.Combine(destination, p.FileName) : null);
        }

        public void UseALambdaToMoveTextFiles(string destination) {
            var files = _path.AllFiles();
            _result = files.Move(
                p =>
                p.Extension == ".txt" ? _path.Combine(destination, p.FileName) : null,
                Overwrite.Always);
        }

        public void WriteBytes(string hexContent, string path) {
            var content = new byte[hexContent.Length / 2];
            for (var i = 0; i < hexContent.Length; i += 2) {
                content[i / 2] = byte.Parse(
                    hexContent.Substring(i, 2), NumberStyles.HexNumber);
            }
            _path.Combine(path.Split('\\'))
                .Write(content);
        }

        public void ThenAttributesShouldBeSet(string attributeNames, string path) {
            var attrNames = attributeNames.Split(',');
            var fileAttributes = _path.Combine(path.Split('\\')).Attributes();
            foreach (var attrName in attrNames) {
                Assert.Equal((FileAttributes)0,
                    fileAttributes &
                    (FileAttributes)Enum.Parse(typeof (FileAttributes), attrName));
            }
        }

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

        public void ThenTheResultingStringShouldBe(string resultString) {
            Assert.AreEqual(resultString, _resultString);
        }

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
