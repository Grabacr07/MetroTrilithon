using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Amethystra.UI.Converters;

/// <summary>
/// Builds a <see cref="Rect"/> from a (width, height) value pair, suitable for driving a
/// <see cref="System.Windows.Media.RectangleGeometry"/>'s <c>Rect</c> from an element's
/// <c>ActualWidth</c> and <c>ActualHeight</c>.
/// </summary>
public sealed class SizeToRectConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Length < 2) return Rect.Empty;
        if (values[0] is not double width || values[1] is not double height) return Rect.Empty;
        if (double.IsNaN(width) || double.IsNaN(height) || width <= 0 || height <= 0) return Rect.Empty;

        return new Rect(0, 0, width, height);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
