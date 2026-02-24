using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Amethystra.Diagnostics;

public class Caller
{
    public static string GetCallerTypeToken([CallerFilePath] string file = "")
    {
        var name = Path.GetFileNameWithoutExtension(file);
        var dot = name.IndexOf('.');
        return dot >= 0
            ? name[..dot]
            : name;
    }

    public static string GetCallerLabel(
        [CallerMemberName] string member = "",
        [CallerFilePath] string file = "")
        => $"{GetCallerTypeToken(file)}.{member}";
}
