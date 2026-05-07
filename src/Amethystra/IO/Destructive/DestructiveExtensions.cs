/*
 * Mio I/O Library <https://github.com/takeshik/Mio>
 * Copyright © Takeshi KIRIYA (aka takeshik) <takeshik@tksk.io>
 * All rights reserved. Licensed under the MIT License.
 */

using JetBrains.Annotations;

namespace Amethystra.IO.Destructive
{
    public static class DestructiveExtensions
    {
        [Pure]
        public static DestructiveFilePath AsDestructive(this FilePath file)
            => file.CreateDestructive();

        [Pure]
        public static DestructiveDirectoryPath AsDestructive(this DirectoryPath directory)
            => directory.CreateDestructive();
    }
}
