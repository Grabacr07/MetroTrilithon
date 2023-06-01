using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Windows;

namespace MetroTrilithon;

public static class DesignFeatures
{
    public static bool IsInDesignMode
#if DEBUG
        => DesignerProperties.GetIsInDesignMode(new DependencyObject());
#else
        => false;
#endif

    public static IObservable<T> StopIfInDesignMode<T>(this IObservable<T> source)
#if DEBUG
        => source.Where(_ => IsInDesignMode == false);
#else
        => source;
#endif
}
