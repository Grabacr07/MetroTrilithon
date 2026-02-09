using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Amethystra.UI.Converters;

public abstract class BooleanConverterBase<T>(T trueValue, T falseValue) : IValueConverter
{
    public T True { get; set; } = trueValue;

    public T False { get; set; } = falseValue;

    public virtual object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? this.True : this.False;

    public virtual object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is T v && EqualityComparer<T>.Default.Equals(v, this.True);
}
