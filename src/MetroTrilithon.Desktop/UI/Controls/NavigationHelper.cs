using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using Microsoft.Xaml.Behaviors;
using Wpf.Ui.Controls;

namespace MetroTrilithon.UI.Controls;

public class NavigationHelper
{
    #region PropagationContext attached property

    private static readonly HashSet<NavigationView> _knownItems = new();

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
        if (d is not NavigationViewItem item)
        {
            throw new NotSupportedException($"The '{nameof(PropagationContextProperty).GetPropertyName()}' attached property is only supported for the '{nameof(NavigationViewItem)}' type.");
        }

        var navigation = item.GetSelfAndAncestors()
            .OfType<NavigationView>()
            .FirstOrDefault();
        if (navigation == null || _knownItems.Add(navigation) == false) return;

        Observable.FromEvent<TypedEventHandler<NavigationView, NavigatedEventArgs>, NavigatedEventArgs>(
                handler => (_, args) => handler(args),
                handler => navigation.Navigated += handler,
                handler => navigation.Navigated -= handler)
            .Zip(Observable.FromEvent<TypedEventHandler<NavigationView, RoutedEventArgs>, RoutedEventArgs>(
                handler => (_, args) => handler(args),
                handler => navigation.SelectionChanged += handler,
                handler => navigation.SelectionChanged -= handler))
            .Subscribe(args =>
            {
                if (args is { First.Page: FrameworkElement element, Second.Source: NavigationView { SelectedItem: NavigationViewItem selected} })
                {
                    element.DataContext = GetPropagationContext(selected);
                }
            });
    }

    #endregion
}
