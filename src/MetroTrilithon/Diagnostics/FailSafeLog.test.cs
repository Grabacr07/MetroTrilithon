using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Mio;

namespace MetroTrilithon.Diagnostics;

partial class FailSafeLog
{
    private static FilePath? _overrideLogFilePath;
    private static long? _overrideMaxLogBytes;
    private static int? _overrideMaxGenerations;

    [Conditional("DEBUG")]
    internal static void SetTestOverrides(FilePath logFilePath, long maxLogBytes, int maxGenerations)
    {
        _overrideLogFilePath = logFilePath;
        _overrideMaxLogBytes = maxLogBytes;
        _overrideMaxGenerations = maxGenerations;
    }
}
