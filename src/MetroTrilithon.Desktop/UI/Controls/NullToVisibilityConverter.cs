using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace MetroTrilithon.UI.Controls;

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
        => value == null
            ? VisibilityBoxes.CollapsedBox
            : VisibilityBoxes.VisibleBox;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
