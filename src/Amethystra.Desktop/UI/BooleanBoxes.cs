using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Amethystra.UI;

public static class BooleanBoxes
{
    public static readonly object TrueBox = true;
    public static readonly object FalseBox = false;

    public static object Box(bool value)
        => value
            ? TrueBox
            : FalseBox;
}
