using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Amethystra.Utils;

public static class DateTimeUtil
{
    public static DateTimeOffset Clamp(this DateTimeOffset value, DateTimeOffset min, DateTimeOffset max)
        => value < min
            ? min
            : max < value
                ? max
                : value;

    public static DateTimeOffset Earlier(DateTimeOffset d1, DateTimeOffset d2)
        => d1 <= d2 ? d1 : d2;

    public static DateTimeOffset Later(DateTimeOffset d1, DateTimeOffset d2)
        => d1 >= d2 ? d1 : d2;
}
