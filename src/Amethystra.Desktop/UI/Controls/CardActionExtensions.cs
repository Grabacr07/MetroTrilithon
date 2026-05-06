using System.Windows;
using Wpf.Ui.Controls;

namespace Amethystra.UI.Controls;

public static class CardActionExtensions
{
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
}
