#if DEBUG

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;

namespace MetroTrilithon
{
    public static class DebugFeatures
    {
        public static bool IsInDesignMode
            => DesignerProperties.GetIsInDesignMode(new DependencyObject());
    }
}

#endif
