/*
 * Mio I/O Library <https://github.com/takeshik/Mio>
 * Copyright © Takeshi KIRIYA (aka takeshik) <takeshik@tksk.io>
 * All rights reserved. Licensed under the MIT License.
 */

using System;
using System.IO;
using Amethystra.IO;
using Amethystra.IO.Destructive;

namespace Amethystra.Test.IO;

[TestClass]
[DoNotParallelize]
public class DirectoryPathTests
{
    [TestMethod]
    public void RelativePathIsConvertedWithCurrentDirectory()
    {
        var cwd = Directory.GetCurrentDirectory();
        var dir = new DestructiveDirectoryPath(@"test");
        Assert.AreEqual(dir.FullName, $"{cwd}{Path.DirectorySeparatorChar}test");
    }

    [TestMethod]
    public void PathSeparatorsInPathAreNormalized()
    {
        var cwd = Directory.GetCurrentDirectory();
        var dir = new DestructiveDirectoryPath(@"foo/bar\baz");
        Assert.AreEqual(dir.FullName, $"{cwd}{Path.DirectorySeparatorChar}foo{Path.DirectorySeparatorChar}bar{Path.DirectorySeparatorChar}baz");
    }

    [TestMethod]
    public void TrailingPathSeparatorsInPathAreRemoved()
    {
        var cwd = Directory.GetCurrentDirectory();
        var dir = new DestructiveDirectoryPath(@"foo/bar\baz/\/");
        Assert.AreEqual(dir.FullName, $"{cwd}{Path.DirectorySeparatorChar}foo{Path.DirectorySeparatorChar}bar{Path.DirectorySeparatorChar}baz");
    }

    [TestMethod]
    public void NameReturnsDirectoryName()
    {
        var dir = new DirectoryPath(@"foo/bar.baz");
        Assert.AreEqual("bar.baz", dir.Name);
    }

    [TestMethod]
    public void NameReturnsDirectoryNameWithoutExtension()
    {
        var dir1 = new DirectoryPath(@"foo/bar.baz");
        Assert.AreEqual("bar", dir1.NameWithoutExtension);
        var dir2 = new DirectoryPath(@"foo/bar.baz.qux");
        Assert.AreEqual("bar.baz", dir2.NameWithoutExtension);
    }

    [TestMethod]
    public void ExtensionReturnsDirectoryExtension()
    {
        var dir1 = new DirectoryPath(@"foo/bar.baz");
        Assert.AreEqual(".baz", dir1.Extension);
        var dir2 = new DirectoryPath(@"foo/bar.baz.qux");
        Assert.AreEqual(".qux", dir2.Extension);
    }

    [TestMethod]
    public void ExtensionReturnsEmptyIfDirectoryDoesNotHaveExtension()
    {
        var dir = new DirectoryPath(@"foo/bar");
        Assert.AreEqual("", dir.Extension);
    }

    [TestMethod]
    public void ExtensionReturnsEmptyIfDirectoryEndsWithDot()
    {
        var dir1 = new DirectoryPath(@"foo/bar.");
        Assert.AreEqual("", dir1.Extension);
        var dir2 = new DirectoryPath(@"foo/bar..");
        Assert.AreEqual("", dir2.Extension);
    }

    [TestMethod]
    public void ExtensionEqualsIsTrueIfExtensionIsEqual()
    {
        var dir = new DirectoryPath(@"foo/bar.baz");
        Assert.IsTrue(dir.ExtensionEquals(".baz"));
    }

    [TestMethod]
    public void ExtensionEqualsIsTrueIfExtensionIsEqualWithoutLeadingDot()
    {
        var dir = new DirectoryPath(@"foo/bar.baz");
        Assert.IsTrue(dir.ExtensionEquals("baz"));
    }

    [TestMethod]
    public void ExtensionEqualsIgnoresCase()
    {
        var dir = new DirectoryPath("foo/bar.baz");
        Assert.IsTrue(dir.ExtensionEquals("Baz"));
    }

    [TestMethod]
    public void ExtensionEqualsWithComparerJudgesByTheirBehavior()
    {
        var dir = new DirectoryPath("foo/bar.baz");
        Assert.IsTrue(dir.ExtensionEquals("Baz", FileSystemPathComparer.CaseInsensitive));
        Assert.IsFalse(dir.ExtensionEquals("Baz", FileSystemPathComparer.CaseSensitive));
    }

    [TestMethod]
    public void EqualsIsTrueIfFullNameIsEqual()
    {
        var dir1 = new DirectoryPath("foo/bar.baz");
        var dir2 = new DirectoryPath(@"foo\bar.baz");
        Assert.AreEqual(dir1, dir2);
    }

    [TestMethod]
    public void EqualsIgnoresCase()
    {
        var dir1 = new DirectoryPath("foobar");
        var dir2 = new DirectoryPath("FooBAR");
        Assert.AreEqual(dir1, dir2);
    }

    [TestMethod]
    public void EqualsOperatorIsTrueIfFullNameIsEqual()
    {
        var dir1 = new DirectoryPath("foo/bar.baz");
        var dir2 = new DirectoryPath(@"foo\bar.baz");
        Assert.IsTrue(dir1 == dir2);
        Assert.IsFalse(dir1 != dir2);
    }

    [TestMethod]
    public void EqualsOperatorIgnoresCase()
    {
        var dir1 = new DirectoryPath("foobar");
        var dir2 = new DirectoryPath("FooBAR");
        Assert.IsTrue(dir1 == dir2);
        Assert.IsFalse(dir1 != dir2);
    }

    [TestMethod]
    public void EqualsWithComparerJudgesByTheirBehavior()
    {
        var dir1 = new DirectoryPath("foobar");
        var dir2 = new DirectoryPath("FooBAR");
        Assert.IsTrue(dir1.Equals(dir2, FileSystemPathComparer.CaseInsensitive));
        Assert.IsFalse(dir1.Equals(dir2, FileSystemPathComparer.CaseSensitive));
    }

    [TestMethod]
    public void ExistIsTrueIfDirectoryExists()
    {
        try
        {
            Directory.CreateDirectory("dir");
            var dir = new DirectoryPath("dir");
            Assert.IsTrue(dir.Exists());
        }
        finally
        {
            Directory.Delete("dir");
        }
    }

    [TestMethod]
    public void ExistIsFalseIfDirectoryDoesNotExists()
    {
        var dir = new DirectoryPath("dir0");
        Assert.IsFalse(dir.Exists());
    }

    [TestMethod]
    public void ExistIsFalseIfFileExists()
    {
        try
        {
            File.WriteAllText("file1", "test");
            var dir = new DirectoryPath("file");
            Assert.IsFalse(dir.Exists());
        }
        finally
        {
            File.Delete("file1");
        }
    }

    [TestMethod]
    public void ExistIsNotCached()
    {
        Directory.CreateDirectory("dir1");
        var dir = new DirectoryPath("dir1");
        Assert.IsTrue(dir.Exists());
        Directory.Delete("dir1");
        Assert.IsFalse(dir.Exists());
    }

    [TestMethod]
    public void NullIfNotExistsReturnsItselfIfDirectoryExists()
    {
        try
        {
            Directory.CreateDirectory("dir1");
            var dir = new DirectoryPath("dir1");
            Assert.AreSame(dir.NullIfNotExists(), dir);
        }
        finally
        {
            Directory.Delete("dir1");
        }
    }

    [TestMethod]
    public void NullIfNotExistsReturnsNullIfDirectoryDoesNotExists()
    {
        var dir = new DirectoryPath("dir0");
        Assert.IsNull(dir.NullIfNotExists());
    }

    [TestMethod]
    public void WithExtensionReturnsDirectoryWithSpecifiedExtension()
    {
        var dir = new DirectoryPath("dir1.foo");
        Assert.AreEqual(dir.WithExtension("bar"), new DirectoryPath("dir1.bar"));
    }

    [TestMethod]
    public void IsDescendantIsTrueIfDirectoryIsDescendant()
    {
        var dir1 = new DirectoryPath("foo/bar/baz");
        var dir2 = new DirectoryPath("foo");
        Assert.IsTrue(dir1.IsDescendantOf(dir2));
    }

    [TestMethod]
    public void IsDescendantIsFalseIfFragmentStartsWith()
    {
        var dir1 = new DirectoryPath("fooo/bar/baz");
        var dir2 = new DirectoryPath("foo");
        Assert.IsFalse(dir1.IsDescendantOf(dir2));
    }

    [TestMethod]
    public void IsDescendantIgnoresCase()
    {
        var dir1 = new DirectoryPath("foo/bar/baz");
        var dir2 = new DirectoryPath("FOO");
        Assert.IsTrue(dir1.IsDescendantOf(dir2));
    }

    [TestMethod]
    public void IsDescendantWithComparerJudgesByTheirBehavior()
    {
        var dir1 = new DirectoryPath("foo/bar/baz");
        var dir2 = new DirectoryPath("FOO");
        Assert.IsTrue(dir1.IsDescendantOf(dir2, FileSystemPathComparer.CaseInsensitive));
        Assert.IsFalse(dir1.IsDescendantOf(dir2, FileSystemPathComparer.CaseSensitive));
    }

    [TestMethod]
    public void ParentReturnsParentDirectory()
    {
        var dir = new DirectoryPath("foo/bar/baz");
        Assert.AreEqual(dir.Parent, new DirectoryPath("foo/bar"));
    }

    [TestMethod]
    public void RootReturnsRootDirectory()
    {
        var dir = new DirectoryPath("foo/bar/baz");
        Assert.AreEqual(dir.Root, new DirectoryPath("/"));
    }

    [TestMethod]
    public void ParentThrowsIfDirectoryIsRootDirectory()
    {
        var dir = new DirectoryPath("/");
        Assert.ThrowsExactly<InvalidOperationException>(() => { _ = dir.Parent; });
    }

    [TestMethod]
    public void TryGetParentReturnsNullIfDirectoryIsRootDirectory()
    {
        var dir = new DirectoryPath("/");
        Assert.IsNull(dir.TryGetParent());
    }
}
