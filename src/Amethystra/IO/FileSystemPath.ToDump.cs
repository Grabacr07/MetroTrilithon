/*
 * Mio I/O Library <https://github.com/takeshik/Mio>
 * Copyright © Takeshi KIRIYA (aka takeshik) <takeshik@tksk.io>
 * All rights reserved. Licensed under the MIT License.
 */

using System;
using JetBrains.Annotations;

namespace Amethystra.IO;

partial class FileSystemPath
{
    private static readonly Type? _hyperlinqType = Type.GetType("LINQPad.Hyperlinq, LINQPad.Runtime")
        // LINQPad 5 or below
        ?? Type.GetType("LINQPad.Hyperlinq, LINQPad");

    [UsedImplicitly]
    private protected object? ToDump()
        => _hyperlinqType == null
            ? this.ToString()
            : Activator.CreateInstance(_hyperlinqType, this.FullName, this.ToString());
}