using System;
using System.Collections.Generic;
using System.Text;
using Mio;
using Mio.Destructive;

namespace Amethystra.Diagnostics;

partial class AppLog
{

    private void TryRotateLogFileIfNeeded()
    {
        try
        {
            if (this.LogFilePath.Exists() == false)
            {
                return;
            }

            var length = this.LogFilePath.GetSize();
            if (length <= this.MaxLogBytes)
            {
                return;
            }

            this.RotateGenerations(this.LogFilePath);
        }
        catch
        {
            // ローテーションに失敗してもログ書き込み自体は続行したい
        }
    }

    private void RotateGenerations(FilePath currentPath)
    {
        var directory = currentPath.Parent;
        var baseName = currentPath.NameWithoutExtension;
        var extension = currentPath.Extension;

        var oldest = directory.ChildFile($"{baseName}.{this.MaxGenerations}{extension}");
        if (oldest.Exists()) oldest.AsDestructive().Delete();

        for (var i = this.MaxGenerations - 1; i >= 1; i--)
        {
            var src = directory.ChildFile($"{baseName}.{i}{extension}");
            if (src.Exists() == false)
            {
                continue;
            }

            var dst = directory.ChildFile($"{baseName}.{i + 1}{extension}");
            if (dst.Exists())
            {
                dst.AsDestructive().Delete();
            }

            src.AsDestructive().MoveTo(dst.AsDestructive());
        }

        var first = directory.ChildFile($"{baseName}.1{extension}");
        if (first.Exists())
        {
            first.AsDestructive().Delete();
        }

        currentPath.AsDestructive().MoveTo(first.AsDestructive());
    }
}
