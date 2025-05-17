using System;
using System.Collections.Generic;
using System.Windows;
using MetroTrilithon.UI.Controls;

namespace MetroTrilithon.UI.Converters;

public class BooleanToVisibilityConverterEx() : BooleanConverterBase<Visibility>(Visibility.Visible, Visibility.Collapsed);
