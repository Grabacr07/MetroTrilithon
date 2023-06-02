using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace MetroTrilithon.UI.Converters;

public class EnumToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => BooleanBoxes.Box(value.ToString() == parameter.ToString());

    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => (bool)value ? Enum.Parse(targetType, parameter.ToString() ?? "", true) : null;
}
