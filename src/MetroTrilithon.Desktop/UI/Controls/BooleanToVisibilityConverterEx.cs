using System;
using System.Collections.Generic;
using System.Windows;

namespace MetroTrilithon.UI.Controls;

public class BooleanToVisibilityConverterEx : BooleanConverterBase<Visibility>
{
    public BooleanToVisibilityConverterEx()
        : base(Visibility.Visible, Visibility.Collapsed)
    {
    }
}
