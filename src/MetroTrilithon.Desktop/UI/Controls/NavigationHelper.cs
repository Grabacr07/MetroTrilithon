using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.Xaml.Behaviors;
using WPFUI.Controls;

namespace MetroTrilithon.UI.Controls;

public class NavigationHelper
{
    #region PropagationContext attached property

    private static readonly HashSet<Navigation> _knownItems = new();

    public static readonly DependencyProperty PropagationContextProperty
        = DependencyProperty.RegisterAttached(
            nameof(PropagationContextProperty).GetPropertyName(),
            typeof(object),
            typeof(NavigationHelper),
            new PropertyMetadata(null, HandlePropagationContextChanged));

    public static void SetPropagationContext(DependencyObject element, object value)
        => element.SetValue(PropagationContextProperty, value);


    public static object GetPropagationContext(DependencyObject element)
        => element.GetValue(PropagationContextProperty);

    private static void HandlePropagationContextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not NavigationItem item) throw new NotSupportedException($"The '{nameof(PropagationContextProperty).GetPropertyName()}' attached property is only supported for the '{nameof(NavigationItem)}' type.");

        var navigation = item.GetSelfAndAncestors()
            .OfType<Navigation>()
            .FirstOrDefault();
        if (navigation == null || _knownItems.Add(navigation) == false) return;

        navigation.Navigated += HandleNavigated;
    }

    private static void HandleNavigated(object sender, RoutedEventArgs e)
    {
        if (sender is Navigation { Current: NavigationItem { Instance: FrameworkElement { DataContext: null } element } item })
        {
            element.DataContext = GetPropagationContext(item);
        }
    }

    #endregion
}
