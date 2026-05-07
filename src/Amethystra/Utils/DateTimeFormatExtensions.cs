using System;
using JetBrains.Annotations;

namespace Amethystra.Utils;

public static class DateTimeFormatExtensions
{
    extension(string? format)
    {
        [Pure]
        public bool IsValidDateTimeOffsetFormat()
            => format.FormatDateTimeOffset(DateTimeOffset.UnixEpoch) != null;

        [Pure]
        public string? FormatDateTimeOffset(DateTimeOffset value)
        {
            if (string.IsNullOrEmpty(format)) return null;
            try
            {
                return value.ToString(format);
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}
