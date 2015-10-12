// Copyright © 2010-2015 Bertrand Le Roy.  All Rights Reserved.
// This code released under the terms of the 
// MIT License http://opensource.org/licenses/MIT

using System;
using System.Collections;
using System.ComponentModel;
using SystemIO = System.IO;
using Fluent.IO;
using NUnit.Framework;

namespace FluentPathTest {
    [TestFixture]
    public class PathTests {
        private readonly string _temp = SystemIO.Path.DirectorySeparatorChar + "temp";
        private readonly Path _path = Path.Root.Combine("foo", "bar", "baz");
        private readonly string _root =
            SystemIO.Path.GetPathRoot(SystemIO.Directory.GetCurrentDirectory());
        private readonly char _sep = SystemIO.Path.DirectorySeparatorChar;
        private readonly Path _baz = Path.Root.Combine("foo", "bar", "baz.txt");

        [Test]
        public void CanConvertPathFromString() {
            var converter = TypeDescriptor.GetConverter(typeof (Path));
            var path = (Path)converter.ConvertFromString(_temp);
            Assert.AreEqual(_temp, path.ToString());
        }

        [Test]
        public void CanConvertFromIntIsFalse() {
            var converter = TypeDescriptor.GetConverter(typeof(Path));
            Assert.IsFalse(converter.CanConvertFrom(typeof(int)));
        }

        [Test]
        public void CanConvertFromStringIsTrue() {
            var converter = TypeDescriptor.GetConverter(typeof(Path));
            Assert.IsTrue(converter.CanConvertFrom(typeof(string)));
        }

        [Test]
        public void ConvertFromIntThrows() {
            var converter = TypeDescriptor.GetConverter(typeof(Path));
            Assert.Throws(
                typeof (NotSupportedException),
                () => converter.ConvertFrom(0));
        }

        [Test]
        public void CanCreatePathFromString() {
            var path = (Path)_temp;
            Assert.AreEqual(_temp, path.ToString());
        }

        [Test]
        public void CanCreatePathFromTokens() {
            Path path = Path.Get(Path.Root.ToString(), "foo", "bar", "baz");
            Assert.AreEqual(_path.ToString(), path.ToString());
        }

        [Test]
        public void CantCreatePathFromNoTokens() {
            Assert.Throws<ArgumentException>(() => Path.Get());
        }

        [Test]
        public void CanCreatePathFromListOfPaths() {
            var p1 = new Path("foo", "bar", "baz");
            var p2 = new Path("truc", "bar", "bidule");
            var p3 = new Path("toto");
            var combined = new Path(p1, p2, p3);
            Assert.IsTrue(combined == new Path("foo", "bar", "baz", "truc", "bidule", "toto"));
        }

        [Test]
        public void CanExplicitlyCompareToString() {
            var path = new Path(_temp);
            Assert.IsTrue((string)path == _temp);
        }

        [Test]
        public void HashCodesForDifferentPathsAreDifferent() {
            var path1 = new Path("one");
            var path2 = new Path("two");
            Assert.AreNotEqual(path1.GetHashCode(), path2.GetHashCode());
        }

        [Test]
        public void HashCodesForSamePathsAreEqual() {
            var path1 = new Path("one");
            var path2 = new Path("one");
            Assert.AreEqual(path1.GetHashCode(), path2.GetHashCode());
        }

        [Test]
        public void GetEquivalentToNew() {
            Assert.IsTrue(new Path("foo") == Path.Get("foo"));
        }

        [Test]
        public void MakeRelative() {
            Assert.AreEqual("foo", Path.Current.Combine("foo").MakeRelative().ToString());
        }

        [Test]
        public void MakeRelativeTo() {
            Assert.AreEqual("foo",
                Path.Current.Combine("foo").MakeRelativeTo(Path.Current.ToString()).ToString());
        }

        [Test]
        public void MakeRelativeToRooted() {
            Assert.AreEqual(@"Core\Common\manifest.txt",
                String.Join("\\",
                    Path.Root.Combine("Projects", "MyProject", "src", "MyProject.Web", "Core", "Common", "manifest.txt")
                        .MakeRelativeTo(Path.Root.Combine("Projects", "MyProject", "src", "MyProject.Web"))
                        .Tokens));
        }

        [Test]
        public void MakeRelativeToRootedWithTrailingSeparator() {
            Assert.AreEqual(@"Core\Common\manifest.txt",
                String.Join("\\",
                    new Path(String.Join(
                        System.IO.Path.DirectorySeparatorChar.ToString(),
                        new[] {"", "Projects", "MyProject", "src", "MyProject.Web", "Core", "Common", "manifest.txt"}))
                        .MakeRelativeTo(String.Join(
                            System.IO.Path.DirectorySeparatorChar.ToString(),
                            new[] {"", "Projects", "MyProject", "src", "MyProject.Web", ""}))
                        .Tokens));
        }

        [Test]
        public void MakeRelativeWithRelativePathThrows() {
            Assert.Throws(
                typeof (InvalidOperationException),
                () => Path.Get("foo").MakeRelative());
        }

        [Test]
        public void MakeRelativeWithOtherPathThrows() {
            Assert.Throws(
                typeof (InvalidOperationException),
                () => Path.Get(_sep + "foo")
                          .MakeRelativeTo(_sep + "baz"));
        }

        [Test]
        public void NullPathThrows() {
            Assert.Throws(
                typeof (ArgumentNullException),
                () => new Path((string[])null));
        }

        [Test]
        public void EqualsTrueForPathsFromSameString() {
// ReSharper disable EqualExpressionComparison
            Assert.IsTrue(new Path(_temp) == new Path(_temp));
            Assert.IsTrue(new Path(_temp).Equals(new Path(_temp)));
// ReSharper restore EqualExpressionComparison
        }

        [Test]
        public void EqualsTrueForPathAndItsString() {
            Assert.IsTrue(new Path(_temp).Equals(_temp));
        }

        [Test]
        public void EqualsFalseForDifferentPaths() {
            Assert.IsFalse(new Path(_temp).Equals(new Path("Program Files")));
        }

        [Test]
        public void EqualsFalseForPathAndDifferentString() {
            Assert.IsFalse(new Path(_temp).Equals("Program Files"));
        }

        [Test]
        public void EqualsFalseWhenComparingWithNull() {
            Assert.IsFalse(new Path(_temp).Equals(null));
        }

        [Test]
        public void Tokens() {
            var path = new Path("foo").Combine("bar").Combine("baz");
            Assert.That(path.Tokens, Is.EqualTo(new [] {"foo", "bar", "baz"}));
        }

        [Test]
        public void ToStringGivesBackTheString() {
            const string path = "foo";
            Assert.AreEqual(path, new Path(path).ToString());
        }

        [Test]
        public void ParentGoesUpOneDirectory() {
            var pathUp = _path.Parent();
            Assert.AreEqual(_root + "foo" + _sep + "bar", pathUp.ToString());
        }

        [Test]
        public void ParentWithTrailingSeparator() {
            var pathUp = new Path(String.Join(
                System.IO.Path.DirectorySeparatorChar.ToString(),
                new[] {"", "Projects", "MyProject", "src", "MyProject.Web", "Core", "Common", "manifest.txt", ""}))
                .Parent();
            Assert.AreEqual(@"\Projects\MyProject\src\MyProject.Web\Core\Common", String.Join("\\", pathUp.Tokens));
        }

        [Test]
        public void ParentTwiceGoesUpTwoDirectories() {
            var pathUpUp = _path.Parent().Parent();
            Assert.AreEqual(_root + "foo", pathUpUp.ToString());
        }

        [Test]
        public void ParentThriceGoesUpThreeDirectory() {
            var pathUp3 = _path.Parent().Parent().Parent();
            Assert.AreEqual(_root, pathUp3.ToString());
        }

        [Test]
        public void ParentMoreTimesThanDepthGoesUpLikeDepth() {
            var pathUp3 = _path.Parent().Parent().Parent();
            Assert.IsTrue(pathUp3 == pathUp3.Parent());
        }

        [Test]
        public void Up2() {
            Assert.AreEqual(_root + "foo", _path.Up(2).ToString());
        }

        [Test]
        public void Up3() {
            Assert.AreEqual(_root, _path.Up(3).ToString());
        }

        [Test]
        public void UpMoreThanDepthSameAsDepth() {
            Assert.AreEqual(_root, _path.Up(4).ToString());
        }

        [Test]
        public void UpOnCollection() {
            var collec = new Path((string)_path, (string)Path.Root, (string)_baz);
            var upCollec = collec.Up();
            Assert.IsTrue(_path.Up().Add(Path.Root) == upCollec);
            var up2Collec = collec.Up(2);
            Assert.IsTrue(_path.Up(2).Add(Path.Root) == up2Collec);
            var up3Collec = collec.Up(3);
            Assert.IsTrue(Path.Root == up3Collec);
        }

        [Test]
        public void ChangeExtensionChangesExtensionWithoutDot() {
            var path = new Path("temp").Combine("foo.bar");
            Assert.IsTrue(new Path("temp").Combine("foo.baz") == path.ChangeExtension("baz"));
        }

        [Test]
        public void ChangeExtensionChangesExtensionWithDot() {
            var path = new Path("temp").Combine("foo.bar");
            Assert.IsTrue(new Path("temp").Combine("foo.baz") == path.ChangeExtension(".baz"));
        }

        [Test]
        public void CombineWithFile() {
            Assert.AreEqual(_sep + "temp" + _sep + "baz.exe",
                new Path(_temp).Combine("baz.exe").ToString());
        }

        [Test]
        public void CombineWithComplexPath() {
            var path = Path.Root.Combine("foo");
            Assert.AreEqual(_root + "foo" + _sep + "bar" + _sep + "baz.exe",
                path.Combine("bar", "baz.exe").ToString());
        }

        [Test]
        public void CombineUsingLambda() {
            var paths = new Path("foo", "bar", "baz");
            var combined = paths.Combine(p => p.FileName);
            Assert.IsTrue(new Path(@"foo\foo", @"bar\bar", @"baz\baz") == combined);
        }

        [Test]
        public void CombineTwoPaths() {
            var paths = new Path("foo", "bar");
            var combined = paths.Combine(new Path("baz").Combine("truc"));
            Assert.IsTrue(new Path(@"foo\baz\truc", @"bar\baz\truc") == combined);
        }

        [Test]
        public void CombineWithNoTokens() {
            var path = Path.Get("foo");
            Assert.IsTrue(path == path.Combine());
        }

        [Test]
        public void CombineOncollection() {
            var paths = new Path("foo", "bar", "baz");
            var combined = paths.Combine("sub", "subsub");
            Assert.IsTrue(new Path(
                        @"foo\sub\subsub",
                        @"bar\sub\subsub",
                        @"baz\sub\subsub") == combined);
        }

        [Test]
        public void DirectoryName() {
            Assert.AreEqual(_root + "foo" + _sep + "bar", _baz.DirectoryName);
        }

        [Test]
        public void Extension() {
            Assert.AreEqual(@".txt", _baz.Extension);
        }

        [Test]
        public void FileName() {
            Assert.AreEqual(@"baz.txt", _baz.FileName);
        }

        [Test]
        public void FileNameWithoutExtension() {
            Assert.AreEqual(@"baz", _baz.FileNameWithoutExtension);
        }

        [Test]
        public void FullPath() {
            var path = Path.Get("foo", "bar", "baz.txt");
            Assert.AreEqual(_sep + "foo" + _sep + "bar" + _sep + "baz.txt",
                path.FullPath.Substring(Path.Current.ToString().Length));
        }

        [Test]
        public void HasExtensionTrue() {
            Assert.IsTrue(_baz.HasExtension);
        }

        [Test]
        public void HasExtensionFalse() {
            var path = Path.Root.Combine("foo", "bar", "baz");
            Assert.IsFalse(path.HasExtension);
        }

        [Test]
        public void IsRootedTrueForFullPath() {
            Assert.IsTrue(_baz.IsRooted);
        }

        [Test]
        public void IsRootedFalseForAppRelative() {
            var path = Path.Get("foo", "bar", "baz.txt");
            Assert.IsFalse(path.IsRooted);
        }

        [Test]
        public void PathRoot() {
            Assert.AreEqual(_root, _baz.PathRoot);
        }

        [Test]
        public void PreviousPath() {
            var path = new Path("two", new Path("one"));
            Assert.AreEqual("one", path.Previous().ToString());
        }

        [Test]
        public void EndPath() {
            var path = new Path("two", new Path("one"));
            Assert.AreEqual("one", path.End().ToString());
            var previous = new Path("previous");
            var collec = new Path(new[] {"current"}, previous);
            Assert.IsTrue(previous == collec.End());
        }

        [Test]
        public void EnumeratePathsInCollection() {
            var pathEnumerator = ((IEnumerable)new Path("foo", "bar")).GetEnumerator();
            Assert.IsTrue(pathEnumerator.MoveNext());
            Assert.AreEqual("foo", pathEnumerator.Current.ToString());
            Assert.IsTrue(pathEnumerator.MoveNext());
            Assert.AreEqual("bar", pathEnumerator.Current.ToString());
            Assert.IsFalse(pathEnumerator.MoveNext());
        }

        [Test]
        public void ChangeExtension() {
            var paths = new Path(
                "foo.txt", @"bar\foo.zip", @"bar\baz.zip", "foo.avi", "bar.txt");
            var changed = paths.ChangeExtension(".txt");
            Assert.IsTrue(new Path("foo.txt", @"bar\foo.txt", @"bar\baz.txt", "bar.txt") == changed);
            Assert.IsTrue(paths == changed.Previous());
        }

        [Test]
        public void ToStringReturnsCommaSeparatedList() {
            Assert.AreEqual("foo, bar, baz", new Path("foo", "bar", "baz").ToString());
        }

        [Test]
        public void FilterCollectionByExtension() {
            var collec = new Path("foo.txt", "foo.bin", "foo.avi", "foo");
            var filtered = collec.WhereExtensionIs(".txt", "avi");
            Assert.IsTrue(new Path("foo.txt", "foo.avi") == filtered);
            Assert.IsTrue(collec == filtered.Previous());
        }

        [Test]
        public void UseForEachOnCollection() {
            var collec = new Path("foo.txt", "foo.bin", "foo.avi", "foo");
            var result = "";
            collec.ForEach(p => result += p.ToString());
            Assert.AreEqual("foo.txtfoo.binfoo.avifoo", result);
        }

        [Test]
        public void MapMaps() {
            var collec = new Path("foo.txt", "foo.bin", "foo.avi", "foo");
            var mapped = collec.Map(p => (Path)p.FileNameWithoutExtension);
            Assert.IsTrue(new Path("foo") == mapped);
            Assert.IsTrue(collec == mapped.Previous());
            mapped = collec.Map(p => (Path)p.Extension);
            Assert.IsTrue(new Path(".txt", ".bin", ".avi", "") == mapped);
        }

        [Test]
        public void FirstPath() {
            var collec = new Path("foo.txt", "foo.bin", "foo.avi", "foo");
            Assert.IsTrue(new Path("foo.txt") == collec.First());
        }

        [Test]
        public void FirstPathFailsOnEmptyCollection() {
            Assert.Throws<InvalidOperationException>(() => new Path().First());
        }

        [Test]
        public void TrailingBackslashDoesntMatterForPathEquality() {
            var path1 = new Path("foo" + _sep + "bar");
            var path2 = new Path("foo" + _sep + "bar" + _sep);

            Assert.IsTrue(path1 == path2);
        }

        [Test]
        public void ParentPathIsCorrectAndTrailingSeparatorDoesntMatter() {
            var path = new Path("foo" + _sep + "bar" + _sep + "baz.dll");

            Assert.IsTrue(new Path("foo" + _sep + "bar") == path.Parent());
            Assert.IsTrue(new Path("foo" + _sep + "bar" + _sep) == path.Parent());
        }

        private class DerivedPath : PathBase<DerivedPath> {
            public DerivedPath() { }
            public DerivedPath(string path) : base(path) {}

            public bool DoStuff() {
                return true;
            }
        }

        [Test]
        public void DerivedPathWorks() {
            var derivedPath = new DerivedPath("foo");

            Assert.IsTrue(derivedPath.DoStuff());
            Assert.IsTrue(derivedPath.Parent().DoStuff());
        }
    }
}
