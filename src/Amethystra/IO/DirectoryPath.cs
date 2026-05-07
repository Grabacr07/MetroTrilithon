/*
 * Mio I/O Library <https://github.com/takeshik/Mio>
 * Copyright © Takeshi KIRIYA (aka takeshik) <takeshik@tksk.io>
 * All rights reserved. Licensed under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using JetBrains.Annotations;
using Amethystra.IO.Destructive;
using D = System.IO.Directory;

namespace Amethystra.IO
{
    public class DirectoryPath : FileSystemPath,
        IEquatable<DirectoryPath>
    {
        private const string _defaultSearchPattern = "*";

        public DirectoryPath(string path)
            : base(path, true)
        {
        }

        internal DirectoryPath(string path, bool normalize)
            : base(path, normalize)
        {
        }

        [Pure]
        public static bool operator ==(DirectoryPath? x, DirectoryPath? y)
            => Equals(x, y);

        [Pure]
        public static bool operator !=(DirectoryPath? x, DirectoryPath? y)
            => !Equals(x, y);

        [MustUseReturnValue]
        public static DirectoryPath GetTempDirectory()
            => new(Path.GetTempPath());

        [MustUseReturnValue]
        public static DirectoryPath GetCurrentDirectory()
            => new(D.GetCurrentDirectory());

        [MustUseReturnValue]
        public static IReadOnlyList<DirectoryPath> GetRootDirectories()
            => Array.ConvertAll(D.GetLogicalDrives(), x => new DirectoryPath(x, false));

        [Pure]
        public static bool Equals(DirectoryPath? x, DirectoryPath? y, FileSystemPathComparer? comparer = null)
            => (comparer ?? Comparer.GetValueFor((x, y))).Equals(x, y);

        [Pure]
        public bool Equals(DirectoryPath? other)
            => Equals(this, other);

        [Pure]
        public override bool Equals(object? obj)
            => obj is DirectoryPath file && this.Equals(file);

        [Pure]
        public bool Equals(DirectoryPath? other, FileSystemPathComparer? comparer)
            => Equals(this, other, comparer);

        [Pure]
        public override int GetHashCode()
            => StringComparer.Ordinal.GetHashCode(this.FullName);

        [Pure]
        public override string ToString()
            => "<Dir: " + this.FullName + ">";

        public override bool Exists()
            => D.Exists(this.FullName);

        public override DateTimeOffset GetCreationTime()
            => new(D.GetCreationTime(this.FullName));

        public override DateTimeOffset GetLastAccessTime()
            => new(D.GetLastAccessTime(this.FullName));

        public override DateTimeOffset GetLastWriteTime()
            => new(D.GetLastWriteTime(this.FullName));

        [MustUseReturnValue]
        public DirectoryPath? NullIfNotExists()
            => this.Exists() ? this : null;

        [Pure]
        public DirectoryPath WithExtension(string? extension)
            => new(Path.ChangeExtension(this.FullName, extension), false);

        [Pure]
        public FilePath ChildFile(string path)
            => new(Path.Combine(this.FullName, path));

        [Pure]
        public DirectoryPath ChildDirectory(string path)
            => new(Path.Combine(this.FullName, path));

        public FilePath RandomNamedChildFile(string prefix = "", string suffix = "")
            => this.ChildFile(prefix + Path.GetRandomFileName() + suffix);

        public DirectoryPath RandomNamedChildDirectory(string prefix = "", string suffix = "")
            => this.ChildDirectory(prefix + Path.GetRandomFileName() + suffix);

        [MustUseReturnValue]
        public IEnumerable<FilePath> EnumerateFiles(string searchPattern = _defaultSearchPattern)
            => D.EnumerateFiles(this.FullName, searchPattern).Select(x => new FilePath(x, false));

        [MustUseReturnValue]
        public IEnumerable<FilePath> EnumerateAllFiles(string searchPattern = _defaultSearchPattern)
            => D.EnumerateFiles(this.FullName, searchPattern, SearchOption.AllDirectories).Select(x => new FilePath(x, false));

        [MustUseReturnValue]
        public IEnumerable<DirectoryPath> EnumerateDirectories(string searchPattern = _defaultSearchPattern)
            => D.EnumerateDirectories(this.FullName, searchPattern).Select(x => new DirectoryPath(x, false));

        [MustUseReturnValue]
        public IEnumerable<DirectoryPath> EnumerateAllDirectories(string searchPattern = _defaultSearchPattern)
            => D.EnumerateDirectories(this.FullName, searchPattern, SearchOption.AllDirectories).Select(x => new DirectoryPath(x, false));

        [MustUseReturnValue]
        public IEnumerable<FileSystemPath> EnumerateEntries(string searchPattern = _defaultSearchPattern)
            => D.EnumerateDirectories(this.FullName, searchPattern).Select(x => (FileSystemPath)new DirectoryPath(x, false))
                .Concat(D.EnumerateFiles(this.FullName, searchPattern).Select(x => (FileSystemPath)new FilePath(x, false)));

        [MustUseReturnValue]
        public IEnumerable<FileSystemPath> EnumerateAllEntries(string searchPattern = _defaultSearchPattern)
            => D.EnumerateDirectories(this.FullName, searchPattern, SearchOption.AllDirectories).Select(x => (FileSystemPath)new DirectoryPath(x, false))
                .Concat(D.EnumerateFiles(this.FullName, searchPattern, SearchOption.AllDirectories).Select(x => (FileSystemPath)new FilePath(x, false)));

        [MustUseReturnValue]
        public IEnumerable<FileSystemPath> EnumerateEntries(
            Func<FileSystemPath, bool>? shouldIncludePredicate,
            Func<FileSystemPath, bool>? shouldRecursePredicate = null,
            EnumerationOptions? enumerationOptions = null)
        {
            static FileSystemPath CreatePath(ref FileSystemEntry entry)
                => entry.IsDirectory
                    ? new DirectoryPath(entry.ToFullPath())
                    : new FilePath(entry.ToFullPath());

            return new FileSystemEnumerable<FileSystemPath>(this.FullName, CreatePath, enumerationOptions)
            {
                ShouldIncludePredicate = shouldIncludePredicate == null
                    ? (FileSystemEnumerable<FileSystemPath>.FindPredicate)((ref FileSystemEntry _) => true)
#pragma warning disable CS8602
                    : (ref FileSystemEntry x) => shouldIncludePredicate(CreatePath(ref x)),
#pragma warning restore CS8602
                ShouldRecursePredicate = shouldRecursePredicate == null
                    ? (FileSystemEnumerable<FileSystemPath>.FindPredicate)((ref FileSystemEntry _) => true)
#pragma warning disable CS8602
                    : (ref FileSystemEntry x) => shouldRecursePredicate(CreatePath(ref x))
#pragma warning restore CS8602
            };
        }

        [MustUseReturnValue]
        public IEnumerable<FileSystemPath> SafeEnumerateEntries(
            FileAttributes attributesToSkip = default,
            Func<DirectoryPath, bool>? recursionPredicate = null)
        {
            return new FileSystemEnumerable<FileSystemPath>(
                this.FullName,
                (ref FileSystemEntry x) => x.IsDirectory
                    ? new DirectoryPath(x.ToFullPath())
                    : new FilePath(x.ToFullPath()),
                new EnumerationOptions()
                {
                    AttributesToSkip = attributesToSkip,
                    IgnoreInaccessible = true,
                    RecurseSubdirectories = true,
                    ReturnSpecialDirectories = false,
                    // MatchCasing and MatchType are not cared since searchPattern is not used
                }
            )
            {
                ShouldRecursePredicate = recursionPredicate == null
                    ? (FileSystemEnumerable<FileSystemPath>.FindPredicate)((ref FileSystemEntry _) => true)
#pragma warning disable CS8602
                    : (ref FileSystemEntry x) => recursionPredicate(new DirectoryPath(x.ToFullPath())),
#pragma warning restore CS8602
            };
        }

        public DirectoryPath EnsureCreated()
        {
            D.CreateDirectory(this.FullName);
            return this;
        }

        public void CopyTo(DestructiveDirectoryPath destination)
        {
            foreach (var file in this.EnumerateFiles())
            {
                file.CopyTo(destination.ChildFile(file.Name).CreateDestructive());
            }

            foreach (var dir in this.EnumerateDirectories())
            {
                dir.CopyTo(destination.ChildDirectory(dir.Name).EnsureCreated().CreateDestructive());
            }
        }

        [Pure]
        internal DestructiveDirectoryPath CreateDestructive()
            => new(this.FullName, false);
    }
}
