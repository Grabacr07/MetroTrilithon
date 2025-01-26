using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MetroTrilithon.Utils;

public static class DoubleUtil
{
    public static double EnsureRange(this double value, double max)
        => Math.Min(value, max);

    public static double EnsureRange(this double value, double min, double max)
        => Math.Clamp(value, min, max);
}
