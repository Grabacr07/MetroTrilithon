using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Amethystra.UI.Controls;

public class CornerRadiusTopConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Length < 2
            || values[0] is not CornerRadius cornerRadius
            || values[1] is not bool isExpanded)
        {
            return DependencyProperty.UnsetValue;
        }

        return isExpanded
            ? new CornerRadius(cornerRadius.TopLeft, cornerRadius.TopRight, 0, 0)
            : cornerRadius;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public static class CardExpanderAnimationHelper
{
    #region AnimationTagValue attached property

    public static readonly DependencyProperty AnimationTagValueProperty
        = DependencyProperty.RegisterAttached(
            nameof(AnimationTagValueProperty).GetPropertyName(),
            typeof(double),
            typeof(CardExpanderAnimationHelper),
            new PropertyMetadata(0.0));

    public static void SetAnimationTagValue(DependencyObject element, double value)
        => element.SetValue(AnimationTagValueProperty, value);

    public static double GetAnimationTagValue(DependencyObject element)
        => (double)element.GetValue(AnimationTagValueProperty);

    #endregion
}

public class AnimationFactorToValueConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Length < 2
            || values[0] is not double height
            || values[1] is not double factor)
        {
            return DependencyProperty.UnsetValue;
        }

        return parameter is "negative" ? -(height * factor) : height * factor;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
