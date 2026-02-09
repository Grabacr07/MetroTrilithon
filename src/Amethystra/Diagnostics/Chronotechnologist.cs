using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Amethystra.Diagnostics;

[GenerateLogger]
public static partial class Chronotechnologist
{
    private static readonly Stopwatch _stopwatch = new();
    private static readonly List<Entry> _entries = new(64);

    private static string _title = "";
    private static long _lastElapsedMs;

    [Conditional("DEBUG")]
    public static void Start(string title)
    {
        _title = title;

        _entries.Clear();
        _stopwatch.Restart();
        _lastElapsedMs = 0;
    }

    [Conditional("DEBUG")]
    public static void Mark(string name)
    {
        var elapsedMs = _stopwatch.ElapsedMilliseconds;
        var deltaMs = elapsedMs - _lastElapsedMs;
        _lastElapsedMs = elapsedMs;

        _entries.Add(new Entry(name, elapsedMs, deltaMs));
    }

    [Conditional("DEBUG")]
    public static void Stop()
    {
        _stopwatch.Stop();

        if (_entries.Count == 0)
        {
            Log.Debug($"{_title} (no mark)");
            return;
        }

        var maxElapsedMs = 0L;
        var maxDeltaMs = 0L;

        foreach (var entry in _entries)
        {
            if (entry.ElapsedMs > maxElapsedMs) maxElapsedMs = entry.ElapsedMs;
            if (entry.DeltaMs > maxDeltaMs) maxDeltaMs = entry.DeltaMs;
        }

        var elapsedWidth = Math.Max(4, CountDigits(maxElapsedMs));
        var deltaWidth = Math.Max(4, CountDigits(maxDeltaMs));

        Log.Debug(_title);

        foreach (var entry in _entries)
        {
            Log.Debug($"{entry.ElapsedMs.ToString("D" + elapsedWidth)} ms ({entry.DeltaMs.ToString("D" + deltaWidth)} ms) - {entry.Name}");
        }
    }

    private static int CountDigits(long value)
    {
        if (value < 0) value = -value;
        if (value < 10) return 1;

        var digits = 0;
        while (value > 0)
        {
            value /= 10;
            digits++;
        }

        return digits;
    }

    private readonly record struct Entry(string Name, long ElapsedMs, long DeltaMs);
}
