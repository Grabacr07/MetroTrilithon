/*
 * Mio I/O Library <https://github.com/takeshik/Mio>
 * Copyright © Takeshi KIRIYA (aka takeshik) <takeshik@tksk.io>
 * All rights reserved. Licensed under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Amethystra.IO.Destructive;
using F = System.IO.File;

namespace Amethystra.IO
{
    public class FilePath : FileSystemPath, IEquatable<FilePath>
    {
        public FilePath(string path)
            : base(path, true)
        {
        }

        internal FilePath(string path, bool normalize)
            : base(path, normalize)
        {
        }

        [Pure]
        public static bool operator ==(FilePath? x, FilePath? y)
            => Equals(x, y);

        [Pure]
        public static bool operator !=(FilePath? x, FilePath? y)
            => !Equals(x, y);

        [Pure]
        public static bool Equals(FilePath? x, FilePath? y, FileSystemPathComparer? comparer = null)
            => (comparer ?? Comparer.GetValueFor((x, y))).Equals(x, y);

        [Pure]
        public bool Equals(FilePath? other)
            => Equals(this, other);

        [Pure]
        public bool Equals(FilePath? other, FileSystemPathComparer comparer)
            => Equals(this, other, comparer);

        [Pure]
        public override bool Equals(object? obj)
            => obj is FilePath file && this.Equals(file);

        [Pure]
        public override int GetHashCode()
            => StringComparer.Ordinal.GetHashCode(this.FullName);

        [Pure]
        public override string ToString()
            => "<File: " + this.FullName + ">";

        public override bool Exists()
            => F.Exists(this.FullName);

        public override DateTimeOffset GetCreationTime()
            => new(F.GetCreationTime(this.FullName));

        public override DateTimeOffset GetLastAccessTime()
            => new(F.GetLastAccessTime(this.FullName));

        public override DateTimeOffset GetLastWriteTime()
            => new(F.GetLastWriteTime(this.FullName));

        [MustUseReturnValue]
        public FilePath? NullIfNotExists()
            => this.Exists() ? this : null;

        [Pure]
        public FilePath WithExtension(string? extension)
            => new(Path.ChangeExtension(this.FullName, extension), false);

        [MustUseReturnValue]
        public long GetSize64()
            => new FileInfo(this.FullName).Length;

        [MustUseReturnValue]
        public int GetSize()
            => checked((int)this.GetSize64());

        public byte[] ReadAllBytes()
            => F.ReadAllBytes(this.FullName);

        public string[] ReadAllLines(Encoding? encoding = null)
            => F.ReadAllLines(this.FullName, encoding ?? Encoding.GetValueFor(this));

        public string ReadAllText(Encoding? encoding = null)
            => F.ReadAllText(this.FullName, encoding ?? Encoding.GetValueFor(this));

        public IEnumerable<string> ReadLines(Encoding? encoding = null)
            => F.ReadLines(this.FullName, encoding ?? Encoding.GetValueFor(this));

        public Task<byte[]> ReadAllBytesAsync(CancellationToken cancellationToken = default)
        {
            return F.ReadAllBytesAsync(this.FullName, cancellationToken);
        }

        public Task<string[]> ReadAllLinesAsync(Encoding? encoding = null, CancellationToken cancellationToken = default)
        {
            return F.ReadAllLinesAsync(this.FullName, encoding ?? Encoding.GetValueFor(this), cancellationToken);
        }

        public Task<string> ReadAllTextAsync(Encoding? encoding = null, CancellationToken cancellationToken = default)
        {
            return F.ReadAllTextAsync(this.FullName, encoding ?? Encoding.GetValueFor(this), cancellationToken);
        }

        public FileStream OpenRead(
            FileShare share = FileShare.Read,
            int bufferSize = DefaultFileStreamBufferSize,
            FileOptions options = FileOptions.Asynchronous)
            => new(this.FullName, FileMode.Open, FileAccess.Read, share, bufferSize, options);

        public StreamReader OpenReadText(
            Encoding? encoding = null,
            FileShare share = FileShare.Read,
            int bufferSize = DefaultFileStreamBufferSize,
            FileOptions options = FileOptions.Asynchronous)
            => new(new FileStream(this.FullName, FileMode.Open, FileAccess.Read, share, bufferSize, options), encoding ?? Encoding.GetValueFor(this));

        public void CopyTo(DestructiveFilePath destination, bool overwrite = true)
            => F.Copy(this.FullName, destination.FullName, overwrite);

        public bool TryCopyTo(DestructiveFilePath destination)
        {
            if (destination.Exists()) return false;
            try
            {
                this.CopyTo(destination);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Replace(DestructiveFilePath destination, DestructiveFilePath destinationBackup)
            => F.Replace(this.FullName, destination.FullName, destinationBackup.FullName);

        [Pure]
        internal DestructiveFilePath CreateDestructive()
            => new(this.FullName, false);
    }
}
