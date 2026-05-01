using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Amethystra.UI;

public static class DoubleBoxes
{
    public static readonly object ZeroBox = 0.0;
    public static readonly object OneBox = 1.0;

    public static object Box(double value)
        => value switch
        {
            0.0 => ZeroBox,
            1.0 => OneBox,
            _ => value
        };
}
