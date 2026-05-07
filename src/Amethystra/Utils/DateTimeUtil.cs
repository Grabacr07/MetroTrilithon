using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Amethystra.Utils;

public static class DateTimeUtil
{
    [Pure]
    public static DateTimeOffset Clamp(this DateTimeOffset value, DateTimeOffset min, DateTimeOffset max)
        => value < min
            ? min
            : max < value
                ? max
                : value;

    [Pure]
    public static DateTimeOffset Earlier(DateTimeOffset d1, DateTimeOffset d2)
        => d1 <= d2 ? d1 : d2;

    [Pure]
    public static DateTimeOffset Later(DateTimeOffset d1, DateTimeOffset d2)
        => d1 >= d2 ? d1 : d2;
}
