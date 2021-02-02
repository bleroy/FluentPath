// Copyright © 2010-2015 Bertrand Le Roy.  All Rights Reserved.
// This code released under the terms of the 
// MIT License http://opensource.org/licenses/MIT

using System;
using System.Collections;
using System.ComponentModel;
using SystemIO = System.IO;
using Fluent.IO;
using Xunit;

namespace FluentPathTest {
    public class PathTests {
        private readonly string _temp = SystemIO.Path.DirectorySeparatorChar + "temp";
        private readonly Path _path = Path.Root.Combine("foo", "bar", "baz");
        private readonly string _root =
            SystemIO.Path.GetPathRoot(SystemIO.Directory.GetCurrentDirectory());
        private readonly char _sep = SystemIO.Path.DirectorySeparatorChar;
        private readonly Path _baz = Path.Root.Combine("foo", "bar", "baz.txt");

        [Fact]
        public void CanConvertPathFromString() {
            var converter = TypeDescriptor.GetConverter(typeof (Path));
            var path = (Path)converter.ConvertFromString(_temp);
            Assert.Equal(_temp, path.ToString());
        }

        [Fact]
        public void CanConvertFromIntIsFalse() {
            var converter = TypeDescriptor.GetConverter(typeof(Path));
            Assert.False(converter.CanConvertFrom(typeof(int)));
        }

        [Fact]
        public void CanConvertFromStringIsTrue() {
            var converter = TypeDescriptor.GetConverter(typeof(Path));
            Assert.True(converter.CanConvertFrom(typeof(string)));
        }

        [Fact]
        public void ConvertFromIntThrows() {
            var converter = TypeDescriptor.GetConverter(typeof(Path));
            Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(0));
        }

        [Fact]
        public void CanCreatePathFromString() {
            var path = (Path)_temp;
            Assert.Equal(_temp, path.ToString());
        }

        [Fact]
        public void CanCreatePathFromTokens() {
            Path path = Path.Get(Path.Root.ToString(), "foo", "bar", "baz");
            Assert.Equal(_path.ToString(), path.ToString());
        }

        [Fact]
        public void CantCreatePathFromNoTokens() {
            Assert.Throws<ArgumentException>(() => Path.Get());
        }

        [Fact]
        public void CanCreatePathFromListOfPaths() {
            var p1 = new Path("foo", "bar", "baz");
            var p2 = new Path("truc", "bar", "bidule");
            var p3 = new Path("toto");
            var combined = new Path(p1, p2, p3);
            Assert.True(combined == new Path("foo", "bar", "baz", "truc", "bidule", "toto"));
        }

        [Fact]
        public void CanExplicitlyCompareToString() {
            var path = new Path(_temp);
            Assert.True((string)path == _temp);
        }

        [Fact]
        public void HashCodesForDifferentPathsAreDifferent() {
            var path1 = new Path("one");
            var path2 = new Path("two");
            Assert.NotEqual(path1.GetHashCode(), path2.GetHashCode());
        }

        [Fact]
        public void HashCodesForSamePathsAreEqual() {
            var path1 = new Path("one");
            var path2 = new Path("one");
            Assert.Equal(path1.GetHashCode(), path2.GetHashCode());
        }

        [Fact]
        public void GetEquivalentToNew() {
            Assert.True(new Path("foo") == Path.Get("foo"));
        }

        [Fact]
        public void MakeRelative() {
            Assert.Equal("foo", Path.Current.Combine("foo").MakeRelative().ToString());
        }

        [Fact]
        public void MakeRelativeTo() {
            Assert.Equal("foo",
                Path.Current.Combine("foo").MakeRelativeTo(Path.Current.ToString()).ToString());
        }

        [Fact]
        public void MakeRelativeToRooted() {
            Assert.Equal(@"Core\Common\manifest.txt",
                String.Join("\\",
                    Path.Root.Combine("Projects", "MyProject", "src", "MyProject.Web", "Core", "Common", "manifest.txt")
                        .MakeRelativeTo(Path.Root.Combine("Projects", "MyProject", "src", "MyProject.Web"))
                        .Tokens));
        }

        [Fact]
        public void MakeRelativeToRootedWithTrailingSeparator() {
            Assert.Equal(@"Core\Common\manifest.txt",
                String.Join("\\",
                    new Path(String.Join(
                        System.IO.Path.DirectorySeparatorChar.ToString(),
                        new[] {"", "Projects", "MyProject", "src", "MyProject.Web", "Core", "Common", "manifest.txt"}))
                        .MakeRelativeTo(String.Join(
                            System.IO.Path.DirectorySeparatorChar.ToString(),
                            new[] {"", "Projects", "MyProject", "src", "MyProject.Web", ""}))
                        .Tokens));
        }

        [Fact]
        public void MakeRelativeWithRelativePathThrows() {
            Assert.Throws<InvalidOperationException>(() => Path.Get("foo").MakeRelative());
        }

        [Fact]
        public void MakeRelativeWithOtherPathThrows() {
            Assert.Throws<InvalidOperationException>(() => Path.Get(_sep + "foo")
                          .MakeRelativeTo(_sep + "baz"));
        }

        [Fact]
        public void NullPathThrows() {
            Assert.Throws<ArgumentNullException>(() => new Path((string[])null));
        }

        [Fact]
        public void EqualsTrueForPathsFromSameString() {
            Assert.True(new Path(_temp) == new Path(_temp));
            Assert.True(new Path(_temp).Equals(new Path(_temp)));
        }

        [Fact]
        public void EqualsTrueForPathAndItsString() {
            Assert.True(new Path(_temp).Equals(_temp));
        }

        [Fact]
        public void EqualsFalseForDifferentPaths() {
            Assert.False(new Path(_temp).Equals(new Path("Program Files")));
        }

        [Fact]
        public void EqualsFalseForPathAndDifferentString() {
            Assert.False(new Path(_temp).Equals("Program Files"));
        }

        [Fact]
        public void EqualsFalseWhenComparingWithNull() {
            Assert.False(new Path(_temp).Equals(null));
        }

        [Fact]
        public void Tokens() {
            var path = new Path("foo").Combine("bar").Combine("baz");
            Assert.Equal(path.Tokens, new [] {"foo", "bar", "baz"});
        }

        [Fact]
        public void ToStringGivesBackTheString() {
            const string path = "foo";
            Assert.Equal(path, new Path(path).ToString());
        }

        [Fact]
        public void ParentGoesUpOneDirectory() {
            var pathUp = _path.Parent();
            Assert.Equal(_root + "foo" + _sep + "bar", pathUp.ToString());
        }

        [Fact]
        public void ParentWithTrailingSeparator() {
            var pathUp = new Path(String.Join(
                System.IO.Path.DirectorySeparatorChar.ToString(),
                new[] {"", "Projects", "MyProject", "src", "MyProject.Web", "Core", "Common", "manifest.txt", ""}))
                .Parent();
            Assert.Equal(@"\Projects\MyProject\src\MyProject.Web\Core\Common", String.Join("\\", pathUp.Tokens));
        }

        [Fact]
        public void ParentTwiceGoesUpTwoDirectories() {
            var pathUpUp = _path.Parent().Parent();
            Assert.Equal(_root + "foo", pathUpUp.ToString());
        }

        [Fact]
        public void ParentThriceGoesUpThreeDirectory() {
            var pathUp3 = _path.Parent().Parent().Parent();
            Assert.Equal(_root, pathUp3.ToString());
        }

        [Fact]
        public void ParentMoreTimesThanDepthGoesUpLikeDepth() {
            var pathUp3 = _path.Parent().Parent().Parent();
            Assert.True(pathUp3 == pathUp3.Parent());
        }

        [Fact]
        public void Up2() {
            Assert.Equal(_root + "foo", _path.Up(2).ToString());
        }

        [Fact]
        public void Up3() {
            Assert.Equal(_root, _path.Up(3).ToString());
        }

        [Fact]
        public void UpMoreThanDepthSameAsDepth() {
            Assert.Equal(_root, _path.Up(4).ToString());
        }

        [Fact]
        public void UpOnCollection() {
            var collec = new Path((string)_path, (string)Path.Root, (string)_baz);
            var upCollec = collec.Up();
            Assert.True(_path.Up().Add(Path.Root) == upCollec);
            var up2Collec = collec.Up(2);
            Assert.True(_path.Up(2).Add(Path.Root) == up2Collec);
            var up3Collec = collec.Up(3);
            Assert.True(Path.Root == up3Collec);
        }

        [Fact]
        public void ChangeExtensionChangesExtensionWithoutDot() {
            var path = new Path("temp").Combine("foo.bar");
            Assert.True(new Path("temp").Combine("foo.baz") == path.ChangeExtension("baz"));
        }

        [Fact]
        public void ChangeExtensionChangesExtensionWithDot() {
            var path = new Path("temp").Combine("foo.bar");
            Assert.True(new Path("temp").Combine("foo.baz") == path.ChangeExtension(".baz"));
        }

        [Fact]
        public void CombineWithFile() {
            Assert.Equal(_sep + "temp" + _sep + "baz.exe",
                new Path(_temp).Combine("baz.exe").ToString());
        }

        [Fact]
        public void CombineWithComplexPath() {
            var path = Path.Root.Combine("foo");
            Assert.Equal(_root + "foo" + _sep + "bar" + _sep + "baz.exe",
                path.Combine("bar", "baz.exe").ToString());
        }

        [Fact]
        public void CombineUsingLambda() {
            var paths = new Path("foo", "bar", "baz");
            var combined = paths.Combine(p => p.FileName);
            Assert.True(new Path(@"foo\foo", @"bar\bar", @"baz\baz") == combined);
        }

        [Fact]
        public void CombineTwoPaths() {
            var paths = new Path("foo", "bar");
            var combined = paths.Combine(new Path("baz").Combine("truc"));
            Assert.True(new Path(@"foo\baz\truc", @"bar\baz\truc") == combined);
        }

        [Fact]
        public void CombinePathsWithSlashOperator()
        {
            var paths = new Path("foo", "bar");
            var combined = paths / "baz" / new Path("truc");
            Assert.True(new Path(@"foo\baz\truc", @"bar\baz\truc") == combined);
        }

        [Fact]
        public void CombineWithNoTokens() {
            var path = Path.Get("foo");
            Assert.True(path == path.Combine());
        }

        [Fact]
        public void CombineOncollection() {
            var paths = new Path("foo", "bar", "baz");
            var combined = paths.Combine("sub", "subsub");
            Assert.True(new Path(
                        @"foo\sub\subsub",
                        @"bar\sub\subsub",
                        @"baz\sub\subsub") == combined);
        }

        [Fact]
        public void DirectoryName() {
            Assert.Equal(_root + "foo" + _sep + "bar", _baz.DirectoryName);
        }

        [Fact]
        public void Extension() {
            Assert.Equal(@".txt", _baz.Extension);
        }

        [Fact]
        public void FileName() {
            Assert.Equal(@"baz.txt", _baz.FileName);
        }

        [Fact]
        public void FileNameWithoutExtension() {
            Assert.Equal(@"baz", _baz.FileNameWithoutExtension);
        }

        [Fact]
        public void FullPath() {
            var path = Path.Get("foo", "bar", "baz.txt");
            Assert.Equal(_sep + "foo" + _sep + "bar" + _sep + "baz.txt",
                path.FullPath.Substring(Path.Current.ToString().Length));
        }

        [Fact]
        public void HasExtensionTrue() {
            Assert.True(_baz.HasExtension);
        }

        [Fact]
        public void HasExtensionFalse() {
            var path = Path.Root.Combine("foo", "bar", "baz");
            Assert.False(path.HasExtension);
        }

        [Fact]
        public void IsRootedTrueForFullPath() {
            Assert.True(_baz.IsRooted);
        }

        [Fact]
        public void IsRootedFalseForAppRelative() {
            var path = Path.Get("foo", "bar", "baz.txt");
            Assert.False(path.IsRooted);
        }

        [Fact]
        public void PathRoot() {
            Assert.Equal(_root, _baz.PathRoot);
        }

        [Fact]
        public void PreviousPath() {
            var path = new Path("two", new Path("one"));
            Assert.Equal("one", path.Previous().ToString());
        }

        [Fact]
        public void EndPath() {
            var path = new Path("two", new Path("one"));
            Assert.Equal("one", path.End().ToString());
            var previous = new Path("previous");
            var collec = new Path(new[] {"current"}, previous);
            Assert.True(previous == collec.End());
        }

        [Fact]
        public void EnumeratePathsInCollection() {
            var pathEnumerator = ((IEnumerable)new Path("foo", "bar")).GetEnumerator();
            Assert.True(pathEnumerator.MoveNext());
            Assert.Equal("foo", pathEnumerator.Current.ToString());
            Assert.True(pathEnumerator.MoveNext());
            Assert.Equal("bar", pathEnumerator.Current.ToString());
            Assert.False(pathEnumerator.MoveNext());
        }

        [Fact]
        public void ChangeExtension() {
            var paths = new Path(
                "foo.txt", @"bar\foo.zip", @"bar\baz.zip", "foo.avi", "bar.txt");
            var changed = paths.ChangeExtension(".txt");
            Assert.True(new Path("foo.txt", @"bar\foo.txt", @"bar\baz.txt", "bar.txt") == changed);
            Assert.True(paths == changed.Previous());
        }

        [Fact]
        public void ToStringReturnsCommaSeparatedList() {
            Assert.Equal("foo, bar, baz", new Path("foo", "bar", "baz").ToString());
        }

        [Fact]
        public void FilterCollectionByExtension() {
            var collec = new Path("foo.txt", "foo.bin", "foo.avi", "foo");
            var filtered = collec.WhereExtensionIs(".txt", "avi");
            Assert.True(new Path("foo.txt", "foo.avi") == filtered);
            Assert.True(collec == filtered.Previous());
        }

        [Fact]
        public void UseForEachOnCollection() {
            var collec = new Path("foo.txt", "foo.bin", "foo.avi", "foo");
            var result = "";
            collec.ForEach(p => result += p.ToString());
            Assert.Equal("foo.txtfoo.binfoo.avifoo", result);
        }

        [Fact]
        public void MapMaps() {
            var collec = new Path("foo.txt", "foo.bin", "foo.avi", "foo");
            var mapped = collec.Map(p => (Path)p.FileNameWithoutExtension);
            Assert.True(new Path("foo") == mapped);
            Assert.True(collec == mapped.Previous());
            mapped = collec.Map(p => (Path)p.Extension);
            Assert.True(new Path(".txt", ".bin", ".avi", "") == mapped);
        }

        [Fact]
        public void FirstPath() {
            var collec = new Path("foo.txt", "foo.bin", "foo.avi", "foo");
            Assert.True(new Path("foo.txt") == collec.First());
        }

        [Fact]
        public void FirstPathFailsOnEmptyCollection() {
            Assert.Throws<InvalidOperationException>(() => new Path().First());
        }

        [Fact]
        public void TrailingBackslashDoesntMatterForPathEquality() {
            var path1 = new Path("foo" + _sep + "bar");
            var path2 = new Path("foo" + _sep + "bar" + _sep);

            Assert.True(path1 == path2);
        }

        [Fact]
        public void ParentPathIsCorrectAndTrailingSeparatorDoesntMatter() {
            var path = new Path("foo" + _sep + "bar" + _sep + "baz.dll");

            Assert.True(new Path("foo" + _sep + "bar") == path.Parent());
            Assert.True(new Path("foo" + _sep + "bar" + _sep) == path.Parent());
        }
    }
}
