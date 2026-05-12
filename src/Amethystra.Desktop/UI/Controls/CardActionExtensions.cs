using System.Windows;
using Wpf.Ui.Controls;

namespace Amethystra.UI.Controls;

public static class CardActionExtensions
{
    #region ChevronGlyph attached property

    public static readonly DependencyProperty ChevronGlyphProperty
        = DependencyProperty.RegisterAttached(
            nameof(ChevronGlyphProperty).GetPropertyName(),
            typeof(string),
            typeof(CardActionExtensions),
            new PropertyMetadata(null));

    public static void SetChevronGlyph(CardAction element, string? value)
        => element.SetValue(ChevronGlyphProperty, value);

    public static string? GetChevronGlyph(CardAction element)
        => (string?)element.GetValue(ChevronGlyphProperty);

    #endregion

    #region CornerRadius attached property

    public static readonly DependencyProperty CornerRadiusProperty
        = DependencyProperty.RegisterAttached(
            nameof(CornerRadiusProperty).GetPropertyName(),
            typeof(CornerRadius),
            typeof(CardActionExtensions),
            new PropertyMetadata(new CornerRadius(4)));

    public static void SetCornerRadius(CardAction element, CornerRadius value)
        => element.SetValue(CornerRadiusProperty, value);

    public static CornerRadius GetCornerRadius(CardAction element)
        => (CornerRadius)element.GetValue(CornerRadiusProperty);

    #endregion
}
