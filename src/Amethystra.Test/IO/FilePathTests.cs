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
public class FilePathTests
{
    [TestMethod]
    public void RelativePathIsConvertedWithCurrentDirectory()
    {
        var cwd = Directory.GetCurrentDirectory();
        var file = new DestructiveFilePath(@"test");
        Assert.AreEqual(file.FullName, $"{cwd}{Path.DirectorySeparatorChar}test");
    }

    [TestMethod]
    public void PathSeparatorsInPathAreNormalized()
    {
        var cwd = Directory.GetCurrentDirectory();
        var file = new DestructiveFilePath(@"foo/bar\baz");
        Assert.AreEqual(file.FullName, $"{cwd}{Path.DirectorySeparatorChar}foo{Path.DirectorySeparatorChar}bar{Path.DirectorySeparatorChar}baz");
    }

    [TestMethod]
    public void TrailingPathSeparatorsInPathAreRemoved()
    {
        var cwd = Directory.GetCurrentDirectory();
        var file = new DestructiveFilePath(@"foo/bar\baz/\/");
        Assert.AreEqual(file.FullName, $"{cwd}{Path.DirectorySeparatorChar}foo{Path.DirectorySeparatorChar}bar{Path.DirectorySeparatorChar}baz");
    }

    [TestMethod]
    public void NameReturnsFileName()
    {
        var file = new FilePath(@"foo/bar.baz");
        Assert.AreEqual("bar.baz", file.Name);
    }

    [TestMethod]
    public void NameReturnsFileNameWithoutExtension()
    {
        var file1 = new FilePath(@"foo/bar.baz");
        Assert.AreEqual("bar", file1.NameWithoutExtension);
        var file2 = new FilePath(@"foo/bar.baz.qux");
        Assert.AreEqual("bar.baz", file2.NameWithoutExtension);
    }

    [TestMethod]
    public void ExtensionReturnsFileExtension()
    {
        var file1 = new FilePath(@"foo/bar.baz");
        Assert.AreEqual(".baz", file1.Extension);
        var file2 = new FilePath(@"foo/bar.baz.qux");
        Assert.AreEqual(".qux", file2.Extension);
    }

    [TestMethod]
    public void ExtensionReturnsEmptyIfFileDoesNotHaveExtension()
    {
        var file = new FilePath(@"foo/bar");
        Assert.AreEqual("", file.Extension);
    }

    [TestMethod]
    public void ExtensionReturnsEmptyIfFileEndsWithDot()
    {
        var file1 = new FilePath(@"foo/bar.");
        Assert.AreEqual("", file1.Extension);
        var file2 = new FilePath(@"foo/bar..");
        Assert.AreEqual("", file2.Extension);
    }

    [TestMethod]
    public void ExtensionEqualsIsTrueIfExtensionIsEqual()
    {
        var file = new FilePath(@"foo/bar.baz");
        Assert.IsTrue(file.ExtensionEquals(".baz"));
    }

    [TestMethod]
    public void ExtensionEqualsIsTrueIfExtensionIsEqualWithoutLeadingDot()
    {
        var file = new FilePath(@"foo/bar.baz");
        Assert.IsTrue(file.ExtensionEquals("baz"));
    }

    [TestMethod]
    public void ExtensionEqualsIgnoresCase()
    {
        var file = new FilePath("foo/bar.baz");
        Assert.IsTrue(file.ExtensionEquals("Baz"));
    }

    [TestMethod]
    public void ExtensionEqualsWithComparerJudgesByTheirBehavior()
    {
        var file = new FilePath("foo/bar.baz");
        Assert.IsTrue(file.ExtensionEquals("Baz", FileSystemPathComparer.CaseInsensitive));
        Assert.IsFalse(file.ExtensionEquals("Baz", FileSystemPathComparer.CaseSensitive));
    }

    [TestMethod]
    public void EqualsIsTrueIfFullNameIsEqual()
    {
        var file1 = new FilePath("foo/bar.baz");
        var file2 = new FilePath(@"foo\bar.baz");
        Assert.AreEqual(file1, file2);
    }

    [TestMethod]
    public void EqualsIgnoresCase()
    {
        var file1 = new FilePath("foobar");
        var file2 = new FilePath("FooBAR");
        Assert.AreEqual(file1, file2);
    }

    [TestMethod]
    public void EqualsWithComparerJudgesByTheirBehavior()
    {
        var file1 = new FilePath("foobar");
        var file2 = new FilePath("FooBAR");
        Assert.IsTrue(file1.Equals(file2, FileSystemPathComparer.CaseInsensitive));
        Assert.IsFalse(file1.Equals(file2, FileSystemPathComparer.CaseSensitive));
    }

    [TestMethod]
    public void EqualsOperatorIsTrueIfFullNameIsEqual()
    {
        var file1 = new FilePath("foo/bar.baz");
        var file2 = new FilePath(@"foo\bar.baz");
        Assert.IsTrue(file1 == file2);
        Assert.IsFalse(file1 != file2);
    }

    [TestMethod]
    public void EqualsOperatorIgnoresCase()
    {
        var file1 = new FilePath("foobar");
        var file2 = new FilePath("FooBAR");
        Assert.IsTrue(file1 == file2);
        Assert.IsFalse(file1 != file2);
    }

    [TestMethod]
    public void ExistIsTrueIfFileExists()
    {
        try
        {
            File.WriteAllText("file1", "test");
            var file = new FilePath("file1");
            Assert.IsTrue(file.Exists());
        }
        finally
        {
            File.Delete("file1");
        }
    }

    [TestMethod]
    public void ExistIsFalseIfFileDoesNotExists()
    {
        var file = new FilePath("file0");
        Assert.IsFalse(file.Exists());
    }

    [TestMethod]
    public void ExistIsFalseIfDirectoryExists()
    {
        try
        {
            Directory.CreateDirectory("dir1");
            var file = new FilePath("dir");
            Assert.IsFalse(file.Exists());
        }
        finally
        {
            Directory.Delete("dir1");
        }
    }

    [TestMethod]
    public void ExistIsNotCached()
    {
        File.WriteAllText("file1", "test");
        var file = new FilePath("file1");
        Assert.IsTrue(file.Exists());
        File.Delete("file1");
        Assert.IsFalse(file.Exists());
    }

    [TestMethod]
    public void NullIfNotExistsReturnsItselfIfFileExists()
    {
        try
        {
            File.WriteAllText("file1", "test");
            var file = new FilePath("file1");
            Assert.AreSame(file.NullIfNotExists(), file);
        }
        finally
        {
            File.Delete("file1");
        }
    }

    [TestMethod]
    public void NullIfNotExistsReturnsNullIfFileDoesNotExists()
    {
        var file = new FilePath("file0");
        Assert.IsNull(file.NullIfNotExists());
    }

    [TestMethod]
    public void WithExtensionReturnsFileWithSpecifiedExtension()
    {
        var file = new FilePath("file1.foo");
        Assert.AreEqual(file.WithExtension("bar"), new FilePath("file1.bar"));
    }

    [TestMethod]
    public void IsDescendantIsTrueIfDirectoryIsDescendant()
    {
        var file = new FilePath("foo/bar/baz");
        var dir = new DirectoryPath("foo");
        Assert.IsTrue(file.IsDescendantOf(dir));
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
        var file = new FilePath("foo/bar/baz");
        var dir = new DirectoryPath("FOO");
        Assert.IsTrue(file.IsDescendantOf(dir));
    }

    [TestMethod]
    public void IsDescendantWithComparerJudgesByTheirBehavior()
    {
        var file = new FilePath("foo/bar/baz");
        var dir = new DirectoryPath("FOO");
        Assert.IsTrue(file.IsDescendantOf(dir, FileSystemPathComparer.CaseInsensitive));
        Assert.IsFalse(file.IsDescendantOf(dir, FileSystemPathComparer.CaseSensitive));
    }

    [TestMethod]
    public void ParentReturnsParentDirectory()
    {
        var file = new FilePath("foo/bar/baz");
        Assert.AreEqual(file.Parent, new DirectoryPath("foo/bar"));
    }

    [TestMethod]
    public void RootReturnsRootDirectory()
    {
        var file = new FilePath("foo/bar/baz");
        Assert.AreEqual(file.Root, new DirectoryPath("/"));
    }

    [TestMethod]
    public void ParentThrowsIfFileIsRootDirectory()
    {
        var file = new FilePath("/");
        Assert.ThrowsExactly<InvalidOperationException>(() => { _ = file.Parent; });
    }

    [TestMethod]
    public void TryGetParentReturnsNullIfFileIsRootDirectory()
    {
        var file = new FilePath("/");
        Assert.IsNull(file.TryGetParent());
    }
}
