using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Amethystra.UI;

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
