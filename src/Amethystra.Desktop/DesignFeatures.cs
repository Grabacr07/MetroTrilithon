using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using R3;

namespace Amethystra;

public static class DesignFeatures
{
    public static bool IsInDesignMode
#if DEBUG
        => DesignerProperties.GetIsInDesignMode(new DependencyObject());
#else
        => false;
#endif

    public static Observable<T> StopIfInDesignMode<T>(this Observable<T> source)
#if DEBUG
        => source.Where(_ => IsInDesignMode == false);
#else
        => source;
#endif
}
