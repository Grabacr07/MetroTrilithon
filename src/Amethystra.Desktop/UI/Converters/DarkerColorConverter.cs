using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace Amethystra.UI.Converters;

/// <summary>
/// 指定された <see cref="SolidColorBrush"/> を 30 % 暗くするコンバーターを表します。
/// </summary>
/// <example>
/// 主に、ボタン押下時に参照済みのリソースを使って背景色を暗くしたりするのに使います。
/// <Trigger Property="IsPressed"
///          Value="True">
///     <Setter Property="Background"
///             Value="{Binding Data, Source={StaticResource PaletteRedBrushProxy}, Converter={StaticResource DarkerColorConverter}}" />
/// </Trigger>
/// </example>
public class DarkerColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value switch
        {
            SolidColorBrush brush => ToDarker(brush),
            _ => Binding.DoNothing,
        };

    object IValueConverter.ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    private static SolidColorBrush ToDarker(SolidColorBrush brush)
    {
        var originalColor = brush.Color;
        var darkerColor = Color.FromArgb(
            originalColor.A,
            (byte)(originalColor.R * 0.7), // 30% darker
            (byte)(originalColor.G * 0.7),
            (byte)(originalColor.B * 0.7));

        return new SolidColorBrush(darkerColor);
    }
}
