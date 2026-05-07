/*
 * Mio I/O Library <https://github.com/takeshik/Mio>
 * Copyright © Takeshi KIRIYA (aka takeshik) <takeshik@tksk.io>
 * All rights reserved. Licensed under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.IO.Enumeration;

namespace Amethystra.IO;

public abstract class FileSystemPathComparer :
    IEqualityComparer<FilePath>,
    IEqualityComparer<DirectoryPath>
{
    public abstract int GetHashCode(FilePath obj);

    public abstract int GetHashCode(DirectoryPath obj);

    protected internal abstract bool Equals(string? x, string? y);

    protected internal abstract bool Matches(string? input, string? pattern);

    public bool Equals(FilePath? x, FilePath? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null) return false;
        if (y is null) return false;
        return this.Equals(x.FullName, y.FullName);
    }

    public bool Equals(DirectoryPath? x, DirectoryPath? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null) return false;
        if (y is null) return false;
        return this.Equals(x.FullName, y.FullName);
    }

    private sealed class CaseSensitiveImpl : FileSystemPathComparer
    {
        public override int GetHashCode(FilePath obj)
            => obj.FullName.GetHashCode();

        public override int GetHashCode(DirectoryPath obj)
            => obj.FullName.GetHashCode();

        protected internal override bool Equals(string? x, string? y)
            => x == y;

        protected internal override bool Matches(string? input, string? pattern)
            => FileSystemName.MatchesSimpleExpression(pattern, input, false);
    }

    private sealed class CaseInsensitiveImpl : FileSystemPathComparer
    {
        public override int GetHashCode(FilePath obj)
            => StringComparer.OrdinalIgnoreCase.GetHashCode(obj.FullName);

        public override int GetHashCode(DirectoryPath obj)
            => StringComparer.OrdinalIgnoreCase.GetHashCode(obj.FullName);

        protected internal override bool Equals(string? x, string? y)
            => string.Equals(x, y, StringComparison.OrdinalIgnoreCase);

        protected internal override bool Matches(string? input, string? pattern)
            => FileSystemName.MatchesSimpleExpression(pattern, input);
    }

    public static FileSystemPathComparer CaseSensitive { get; } = new CaseSensitiveImpl();

    public static FileSystemPathComparer CaseInsensitive { get; } = new CaseInsensitiveImpl();
}