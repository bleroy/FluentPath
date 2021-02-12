// Copyright © 2010-2015 Bertrand Le Roy.  All Rights Reserved.
// This code released under the terms of the 
// MIT License http://opensource.org/licenses/MIT

using Fluent.IO.Async;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Xunit;
using SystemIO = System.IO;

namespace FluentPathTest
{
    public class PathTests
    {
        private readonly string _temp = SystemIO.Path.DirectorySeparatorChar + "temp";
        private readonly Path _path = Path.Root.Combine("foo", "bar", "baz");
        private readonly string _root = SystemIO.Path.GetPathRoot(SystemIO.Directory.GetCurrentDirectory()) ?? ".";
        private readonly char _sep = SystemIO.Path.DirectorySeparatorChar;
        private readonly Path _baz = Path.Root.Combine("foo", "bar", "baz.txt");

        [Fact]
        public void CanConvertPathFromString()
        {
            TypeConverter converter = TypeDescriptor.GetConverter(typeof(Path));
            var path = (Path)converter.ConvertFromString(_temp);
            Assert.Equal(_temp, path.ToString());
        }

        [Fact]
        public void CanConvertFromIntIsFalse()
        {
            TypeConverter converter = TypeDescriptor.GetConverter(typeof(Path));
            Assert.False(converter.CanConvertFrom(typeof(int)));
        }

        [Fact]
        public void CanConvertFromStringIsTrue()
        {
            TypeConverter converter = TypeDescriptor.GetConverter(typeof(Path));
            Assert.True(converter.CanConvertFrom(typeof(string)));
        }

        [Fact]
        public void ConvertFromIntThrows()
        {
            TypeConverter converter = TypeDescriptor.GetConverter(typeof(Path));
            Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(0));
        }

        [Fact]
        public void CanCreatePathFromString()
        {
            var path = (Path)_temp;
            Assert.Equal(_temp, path.ToString());
        }

        [Fact]
        public void CanCreatePathFromTokens()
        {
            var path = Path.FromTokens(Path.Root.ToString(), "foo", "bar", "baz");
            Assert.Equal(_path.ToString(), path.ToString());
        }

        [Fact]
        public async void CantCreatePathFromNoTokens() =>
            await Assert.ThrowsAsync<ArgumentException>(async () => await Path.FromTokens());

        [Fact]
        public void CanCreatePathFromListOfPaths()
        {
            var p1 = new Path("foo", "bar", "baz");
            var p2 = new Path("truc", "bar", "bidule");
            var p3 = new Path("toto");
            var combined = new Path(p1, p2, p3);
            Assert.True(combined == new Path("foo", "bar", "baz", "truc", "bidule", "toto"));
        }

        [Fact]
        public void CanExplicitlyCompareToString()
        {
            var path = new Path(_temp);
            Assert.True((string)path == _temp);
        }

        [Fact]
        public void HashCodesForDifferentPathsAreDifferent()
        {
            var path1 = new Path("one");
            var path2 = new Path("two");
            Assert.NotEqual(path1.GetHashCode(), path2.GetHashCode());
        }

        [Fact]
        public void HashCodesForSamePathsAreEqual()
        {
            var path1 = new Path("one");
            var path2 = new Path("one");
            Assert.Equal(path1.GetHashCode(), path2.GetHashCode());
        }

        [Fact]
        public void GetEquivalentToNew() => Assert.True(new Path("foo") == Path.FromTokens("foo"));

        [Fact]
        public void MakeRelative() => Assert.Equal("foo", Path.Current.Combine("foo").MakeRelative().ToString());

        [Fact]
        public void MakeRelativeTo() =>
            Assert.Equal(
                "foo",
                Path.Current.Combine("foo").MakeRelativeTo(Path.Current.ToString()).ToString());

        [Fact]
        public async void MakeRelativeToRooted() => 
            Assert.Equal(
                @"Core\Common\manifest.txt",
                string.Join("\\",
                    await Path.Root.Combine("Projects", "MyProject", "src", "MyProject.Web", "Core", "Common", "manifest.txt")
                        .MakeRelativeTo(Path.Root.Combine("Projects", "MyProject", "src", "MyProject.Web"))
                        .Tokens()));

        [Fact]
        public async void MakeRelativeToRootedWithTrailingSeparator() =>
            Assert.Equal(
                @"Core\Common\manifest.txt",
                string.Join("\\",
                    await new Path(string.Join(
                        System.IO.Path.DirectorySeparatorChar.ToString(),
                        new[] { "", "Projects", "MyProject", "src", "MyProject.Web", "Core", "Common", "manifest.txt" }))
                        .MakeRelativeTo(string.Join(
                            System.IO.Path.DirectorySeparatorChar.ToString(),
                            new[] { "", "Projects", "MyProject", "src", "MyProject.Web", "" }))
                        .Tokens()));

        [Fact]
        public async void MakeRelativeWithRelativePathThrows() =>
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await Path.FromTokens("foo").MakeRelative());

        [Fact]
        public async void MakeRelativeWithOtherPathThrows() =>
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await Path.FromTokens(_sep + "foo").MakeRelativeTo(_sep + "baz"));

        [Fact]
        public void NullPathThrows() => Assert.Throws<ArgumentNullException>(() => Path.Empty);

        [Fact]
        public void EqualsTrueForPathsFromSameString()
        {
            Assert.True(new Path(_temp) == new Path(_temp));
            Assert.True(new Path(_temp).Equals(new Path(_temp)));
        }

        [Fact]
        public void EqualsTrueForPathAndItsString() =>
            Assert.True(new Path(_temp).Equals(_temp));

        [Fact]
        public void EqualsFalseForDifferentPaths() =>
            Assert.False(new Path(_temp).Equals(new Path("Program Files")));

        [Fact]
        public void EqualsFalseForPathAndDifferentString() =>
            Assert.False(new Path(_temp).Equals("Program Files"));

        [Fact]
        public async void Tokens()
        {
            var path = new Path("foo").Combine("bar").Combine("baz");
            Assert.Equal(await path.Tokens(), new[] { "foo", "bar", "baz" });
        }

        [Fact]
        public void ToStringGivesBackTheString()
        {
            const string path = "foo";
            Assert.Equal(path, new Path(path).ToString());
        }

        [Fact]
        public async void ParentGoesUpOneDirectory()
        {
            Path pathUp = await _path.Parent();
            Assert.Equal(_root + "foo" + _sep + "bar", pathUp.ToString());
        }

        [Fact]
        public async void ParentWithTrailingSeparator()
        {
            Path pathUp = new Path(String.Join(
                SystemIO.Path.DirectorySeparatorChar.ToString(),
                new[] { "", "Projects", "MyProject", "src", "MyProject.Web", "Core", "Common", "manifest.txt", "" }))
                .Parent();
            Assert.Equal(@"\Projects\MyProject\src\MyProject.Web\Core\Common", string.Join("\\", await pathUp.Tokens()));
        }

        [Fact]
        public void ParentTwiceGoesUpTwoDirectories()
        {
            string pathUpUp = _path.Parent().Parent().ToString();
            Assert.Equal(_root + "foo", pathUpUp);
        }

        [Fact]
        public void ParentThriceGoesUpThreeDirectory()
        {
            string pathUp3 = _path.Parent().Parent().Parent().ToString();
            Assert.Equal(_root, pathUp3);
        }

        [Fact]
        public void ParentMoreTimesThanDepthGoesUpLikeDepth()
        {
            Path pathUp3 = _path.Parent().Parent().Parent();
            Assert.True(pathUp3 == pathUp3.Parent());
        }

        [Fact]
        public void Up2()
        {
            Assert.Equal(_root + "foo", _path.Up(2).ToString());
        }

        [Fact]
        public void Up3()
        {
            Assert.Equal(_root, _path.Up(3).ToString());
        }

        [Fact]
        public void UpMoreThanDepthSameAsDepth()
        {
            Assert.Equal(_root, _path.Up(4).ToString());
        }

        [Fact]
        public void UpOnCollection()
        {
            var collec = new Path((string)_path, (string)Path.Root, (string)_baz);
            Path upCollec = collec.Up();
            Assert.True(_path.Up().Add(Path.Root) == upCollec);
            Path up2Collec = collec.Up(2);
            Assert.True(_path.Up(2).Add(Path.Root) == up2Collec);
            Path up3Collec = collec.Up(3);
            Assert.True(Path.Root == up3Collec);
        }

        [Fact]
        public void ChangeExtensionChangesExtensionWithoutDot()
        {
            Path path = new Path("temp").Combine("foo.bar");
            Assert.True(new Path("temp").Combine("foo.baz") == path.ChangeExtension("baz"));
        }

        [Fact]
        public void ChangeExtensionChangesExtensionWithDot()
        {
            Path path = new Path("temp").Combine("foo.bar");
            Assert.True(new Path("temp").Combine("foo.baz") == path.ChangeExtension(".baz"));
        }

        [Fact]
        public void CombineWithFile()
        {
            Assert.Equal(_sep + "temp" + _sep + "baz.exe",
                new Path(_temp).Combine("baz.exe").ToString());
        }

        [Fact]
        public void CombineWithComplexPath()
        {
            Path path = Path.Root.Combine("foo");
            Assert.Equal(_root + "foo" + _sep + "bar" + _sep + "baz.exe",
                path.Combine("bar", "baz.exe").ToString());
        }

        [Fact]
        public async void CombineUsingLambda()
        {
            var paths = new Path("foo", "bar", "baz");
            Path combined = await paths.Combine(async p => await p.FileName());
            Assert.True(new Path(@"foo\foo", @"bar\bar", @"baz\baz") == combined);
        }

        [Fact]
        public void CombineTwoPaths()
        {
            var paths = new Path("foo", "bar");
            Path combined = paths.Combine(new Path("baz").Combine("truc"));
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
        public void CombineWithNoTokens()
        {
            var path = Path.FromTokens("foo");
            Assert.True(path == path.Combine());
        }

        [Fact]
        public void CombineOncollection()
        {
            var paths = new Path("foo", "bar", "baz");
            Path combined = paths.Combine("sub", "subsub");
            Assert.True(new Path(
                        @"foo\sub\subsub",
                        @"bar\sub\subsub",
                        @"baz\sub\subsub") == combined);
        }

        [Fact]
        public async void DirectoryName()
        {
            Assert.Equal(_root + "foo" + _sep + "bar", await _baz.DirectoryName());
        }

        [Fact]
        public async void Extension()
        {
            Assert.Equal(@".txt", await _baz.Extension());
        }

        [Fact]
        public async void FileName()
        {
            Assert.Equal(@"baz.txt", await _baz.FileName());
        }

        [Fact]
        public async void FileNameWithoutExtension()
        {
            Assert.Equal(@"baz", await _baz.FileNameWithoutExtension());
        }

        [Fact]
        public async void FullPath()
        {
            var path = Path.FromTokens("foo", "bar", "baz.txt");
            Assert.Equal(_sep + "foo" + _sep + "bar" + _sep + "baz.txt",
                (await path.FullPath())[Path.Current.ToString().Length..]);
        }

        [Fact]
        public async void HasExtensionTrue()
        {
            Assert.True(await _baz.HasExtension());
        }

        [Fact]
        public async void HasExtensionFalse()
        {
            Path path = Path.Root.Combine("foo", "bar", "baz");
            Assert.False(await path.HasExtension());
        }

        [Fact]
        public async void IsRootedTrueForFullPath()
        {
            Assert.True(await _baz.IsRooted());
        }

        [Fact]
        public async void IsRootedFalseForAppRelative()
        {
            var path = Path.FromTokens("foo", "bar", "baz.txt");
            Assert.False(await path.IsRooted());
        }

        [Fact]
        public async void PathRoot()
        {
            Assert.Equal(_root, await _baz.PathRoot());
        }

        [Fact]
        public void PreviousPath()
        {
            var path = new Path("one") / "two";
            Assert.Equal("one", path.End().ToString());
        }

        [Fact]
        public void EndPath()
        {
            var path = new Path("one") / "two";
            Assert.Equal("one", path.End().ToString());
            var previous = new Path("previous");
            var collec = previous / new Path(new[] { "current" });
            Assert.True(previous == collec.End());
        }

        [Fact]
        public async void EnumeratePathsInCollection()
        {
            IAsyncEnumerator<Path> pathEnumerator = ((IAsyncEnumerable<Path>)new Path("foo", "bar")).GetAsyncEnumerator();
            Assert.True(await pathEnumerator.MoveNextAsync());
            Assert.Equal("foo", pathEnumerator.Current.ToString());

            Assert.True(await pathEnumerator.MoveNextAsync());
            Assert.Equal("bar", pathEnumerator.Current.ToString());
            Assert.False(await pathEnumerator.MoveNextAsync());
        }

        [Fact]
        public async void ChangeExtension()
        {
            var paths = new Path("foo.txt", @"bar\foo.zip", @"bar\baz.zip", "foo.avi", "bar.txt");
            Path changed = await paths.ChangeExtension(".txt");
            Assert.True(new Path("foo.txt", @"bar\foo.txt", @"bar\baz.txt", "bar.txt") == changed);
            Assert.True(paths == await changed.End());
        }

        [Fact]
        public void ToStringReturnsCommaSeparatedList()
        {
            Assert.Equal("foo, bar, baz", new Path("foo", "bar", "baz").ToString());
        }

        [Fact]
        public async void FilterCollectionByExtension()
        {
            var collec = new Path("foo.txt", "foo.bin", "foo.avi", "foo");
            Path filtered = await collec.WhereExtensionIs(".txt", "avi");
            Assert.True(new Path("foo.txt", "foo.avi") == filtered);
            Assert.True(collec == await filtered.End());
        }

        [Fact]
        public async void UseForEachOnCollection()
        {
            var collec = new Path("foo.txt", "foo.bin", "foo.avi", "foo");
            string result = "";
            await collec.ForEach(p => result += p.ToString());
            Assert.Equal("foo.txtfoo.binfoo.avifoo", result);
        }

        [Fact]
        public async void MapMaps()
        {
            var collec = new Path("foo.txt", "foo.bin", "foo.avi", "foo");
            Path mapped = await collec.Map(async p => new Path(await p.FileNameWithoutExtension()));
            Assert.True(new Path("foo") == mapped);
            Assert.True(collec == await mapped.End());
            mapped = await collec.Map(async p => new Path(await p.Extension()));
            Assert.True(new Path(".txt", ".bin", ".avi", "") == mapped);
        }

        [Fact]
        public async void FirstPath()
        {
            var collec = new Path("foo.txt", "foo.bin", "foo.avi", "foo");
            Assert.True(new Path("foo.txt") == await collec.First());
        }

        [Fact]
        public void TrailingBackslashDoesntMatterForPathEquality()
        {
            var path1 = new Path("foo" + _sep + "bar");
            var path2 = new Path("foo" + _sep + "bar" + _sep);

            Assert.True(path1 == path2);
        }

        [Fact]
        public async void ParentPathIsCorrectAndTrailingSeparatorDoesntMatter()
        {
            var path = new Path("foo" + _sep + "bar" + _sep + "baz.dll");

            Assert.True(new Path("foo" + _sep + "bar") == await path.Parent());
            Assert.True(new Path("foo" + _sep + "bar" + _sep) == await path.Parent());
        }
    }
}
