using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Amethystra.UI.Converters;

public class BooleanToVisibilityConverterEx() : BooleanConverterBase<Visibility>(Visibility.Visible, Visibility.Collapsed);
