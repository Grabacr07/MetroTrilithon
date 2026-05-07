using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Amethystra.UI;

public static class BooleanBoxes
{
    public static readonly object TrueBox = true;
    public static readonly object FalseBox = false;

    [Pure]
    public static object Box(bool value)
        => value
            ? TrueBox
            : FalseBox;
}
