using System.Windows;

namespace MetroTrilithon.UI;

public static class VisibilityBoxes
{
    public static object VisibleBox = Visibility.Visible;
    public static object HiddenBox = Visibility.Hidden;
    public static object CollapsedBox = Visibility.Collapsed;

    public static object Box(Visibility value)
        => value switch
        {
            Visibility.Visible => VisibleBox,
            Visibility.Hidden => HiddenBox,
            _ => CollapsedBox
        };
}
