using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Mio;

namespace Amethystra.Diagnostics;

partial class AppLog
{
    private static FilePath? _overrideLogFilePath;
    private static long? _overrideMaxLogBytes;
    private static int? _overrideMaxGenerations;

    [Conditional("DEBUG")]
    internal static void SetTestOverrides(FilePath logFilePath, long maxBytes, int maxGenerations)
    {
        _overrideLogFilePath = logFilePath;
        _overrideMaxLogBytes = maxBytes;
        _overrideMaxGenerations = maxGenerations;
    }
}
