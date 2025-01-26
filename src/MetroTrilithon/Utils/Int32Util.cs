using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MetroTrilithon.Utils;

public static class Int32Util
{
    public static int EnsureRange(this int value, int max)
        => Math.Min(value, max);

    public static int EnsureRange(this int value, int min, int max)
        => Math.Clamp(value, min, max);
}
