using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace MetroTrilithon.UI.Controls;

public abstract class BooleanConverterBase<T> : IValueConverter
{
    public T True { get; set; }
    public T False { get; set; }

    protected BooleanConverterBase(T trueValue, T falseValue)
    {
        this.True = trueValue;
        this.False = falseValue;
    }

    public virtual object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? this.True : this.False;

    public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is T v && EqualityComparer<T>.Default.Equals(v, this.True);
}
