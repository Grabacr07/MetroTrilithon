using System.Windows;

namespace MetroTrilithon.UI;

public static class VisibilityBoxes
{
    public static readonly object VisibleBox = Visibility.Visible;
    public static readonly object HiddenBox = Visibility.Hidden;
    public static readonly object CollapsedBox = Visibility.Collapsed;

    public static object Box(Visibility value)
        => value switch
        {
            Visibility.Visible => VisibleBox,
            Visibility.Hidden => HiddenBox,
            _ => CollapsedBox
        };
}
