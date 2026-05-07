/*
 * Mio I/O Library <https://github.com/takeshik/Mio>
 * Copyright © Takeshi KIRIYA (aka takeshik) <takeshik@tksk.io>
 * All rights reserved. Licensed under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using F = System.IO.File;

namespace Amethystra.IO.Destructive;

public sealed class DestructiveFilePath : FilePath
{
    public DestructiveFilePath(string path)
        : base(path)
    {
    }

    internal DestructiveFilePath(string path, bool normalize)
        : base(path, normalize)
    {
    }

    public static DestructiveFilePath CreateTempFile()
        => new(Path.GetTempFileName());

    [Pure]
    public override string ToString()
        => "<DestructiveFile: " + this.FullName + ">";

    [Pure]
    public Uri ToUri()
        => new(this.FullName);

    public new DestructiveFilePath? NullIfNotExists()
        => this.Exists() ? this : null;

    public void SetAttributes(FileAttributes attributes)
        => F.SetAttributes(this.FullName, attributes);

    [SupportedOSPlatform("windows")]
    public void Encrypt()
        => F.Encrypt(this.FullName);

    [SupportedOSPlatform("windows")]
    public void Decrypt()
        => F.Decrypt(this.FullName);

    public void SetCreationTime(DateTimeOffset creationTime)
        => F.SetCreationTimeUtc(this.FullName, creationTime.UtcDateTime);

    public void SetLastAccessTime(DateTimeOffset lastAccessTime)
        => F.SetLastAccessTimeUtc(this.FullName, lastAccessTime.UtcDateTime);

    public void SetLastWriteTime(DateTimeOffset lastWriteTime)
        => F.SetLastWriteTimeUtc(this.FullName, lastWriteTime.UtcDateTime);

    public bool Delete()
    {
        if (F.Exists(this.FullName) == false) return false;
        try
        {
            F.Delete(this.FullName);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void MoveTo(DestructiveFilePath destination, bool overwrite = true)
    {
        F.Move(this.FullName, destination.FullName, overwrite);
    }

    public bool TryMoveTo(DestructiveFilePath destination)
    {
        if (destination.Exists()) return false;
        try
        {
            this.MoveTo(destination);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Append(byte[] bytes)
    {
        using var fs = this.Open(FileMode.Append, FileAccess.Write);
        fs.Write(bytes, 0, bytes.Length);
        fs.Flush();
    }

    public void Append(IEnumerable<string> contents, Encoding? encoding = null)
        => F.AppendAllLines(this.FullName, contents, encoding ?? Encoding.GetValueFor(this));

    public void Append(string contents, Encoding? encoding = null)
        => F.AppendAllText(this.FullName, contents, encoding ?? Encoding.GetValueFor(this));

    public void Write(byte[] bytes)
        => F.WriteAllBytes(this.FullName, bytes);

    public void Write(IEnumerable<string> contents, Encoding? encoding = null)
        => F.WriteAllLines(this.FullName, contents, encoding ?? Encoding.GetValueFor(this));

    public void Write(string[] contents, Encoding? encoding = null)
        => F.WriteAllLines(this.FullName, contents, encoding ?? Encoding.GetValueFor(this));

    public void Write(string contents, Encoding? encoding = null)
        => F.WriteAllText(this.FullName, contents, encoding ?? Encoding.GetValueFor(this));

    public Task AppendAsync(
        byte[] bytes,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        async Task Core()
        {
            await using var fs = this.Open(FileMode.Append, FileAccess.Write);
            await fs.WriteAsync(bytes, cancellationToken);
            await fs.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        return Core();
    }

    public Task AppendAsync(
        IEnumerable<string> contents,
        Encoding? encoding = null,
        CancellationToken cancellationToken = default)
    {
        return F.AppendAllLinesAsync(this.FullName, contents, encoding ?? Encoding.GetValueFor(this), cancellationToken);
    }

    public Task AppendAsync(
        string contents,
        Encoding? encoding = null,
        CancellationToken cancellationToken = default)
    {
        return F.AppendAllTextAsync(this.FullName, contents, encoding ?? Encoding.GetValueFor(this), cancellationToken);
    }

    public Task WriteAsync(
        byte[] bytes,
        CancellationToken cancellationToken = default)
    {
        return F.WriteAllBytesAsync(this.FullName, bytes, cancellationToken);
    }

    public Task WriteAsync(
        IEnumerable<string> contents,
        Encoding? encoding = null,
        CancellationToken cancellationToken = default)
    {
        return F.WriteAllLinesAsync(this.FullName, contents, encoding ?? Encoding.GetValueFor(this), cancellationToken);
    }

    public Task WriteAsync(
        string contents,
        Encoding? encoding = null,
        CancellationToken cancellationToken = default)
    {
        return F.WriteAllTextAsync(this.FullName, contents, encoding ?? Encoding.GetValueFor(this), cancellationToken);
    }

    public FileStream Create(
        FileShare share = FileShare.Read,
        int bufferSize = DefaultFileStreamBufferSize,
        FileOptions options = FileOptions.Asynchronous)
        => new(this.FullName, FileMode.Create, FileAccess.ReadWrite, share, bufferSize, options);

    public StreamWriter CreateText(
        Encoding? encoding = null,
        FileShare share = FileShare.Read,
        int bufferSize = DefaultFileStreamBufferSize,
        FileOptions options = FileOptions.Asynchronous)
        => new(new FileStream(this.FullName, FileMode.Create, FileAccess.ReadWrite, share, bufferSize, options), encoding ?? Encoding.GetValueFor(this));

    public FileStream Open(
        FileMode mode = FileMode.OpenOrCreate,
        FileAccess access = FileAccess.ReadWrite,
        FileShare share = FileShare.Read,
        int bufferSize = DefaultFileStreamBufferSize,
        FileOptions options = FileOptions.Asynchronous)
        => new(this.FullName, mode, access, share, bufferSize, options);

    public FileStream OpenWrite(
        FileShare share = FileShare.Read,
        int bufferSize = DefaultFileStreamBufferSize,
        FileOptions options = FileOptions.Asynchronous)
        => new(this.FullName, FileMode.OpenOrCreate, FileAccess.Write, share, bufferSize, options);

    public StreamWriter OpenWriteText(
        Encoding? encoding = null,
        FileShare share = FileShare.Read,
        int bufferSize = DefaultFileStreamBufferSize,
        FileOptions options = FileOptions.Asynchronous)
        => new(new FileStream(this.FullName, FileMode.OpenOrCreate, FileAccess.Write, share, bufferSize, options), encoding ?? Encoding.GetValueFor(this));
}
