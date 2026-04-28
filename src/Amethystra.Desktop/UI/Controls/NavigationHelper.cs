using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Xaml.Behaviors;
using R3;
using Wpf.Ui.Controls;

namespace Amethystra.UI.Controls;

public class NavigationHelper
{
    #region PropagationContext attached property

    private static readonly HashSet<NavigationView> _knownItems = [];

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

        var navigated = Observable.FromEvent<TypedEventHandler<NavigationView, NavigatedEventArgs>, NavigatedEventArgs>(
            handler => (_, args) => handler(args),
            h => navigation.Navigated += h,
            h => navigation.Navigated -= h);

        var selectionChanged = Observable.FromEvent<TypedEventHandler<NavigationView, RoutedEventArgs>, RoutedEventArgs>(
            handler => (_, args) => handler(args),
            h => navigation.SelectionChanged += h,
            h => navigation.SelectionChanged -= h);

        navigated
            .Zip(selectionChanged, static (nav, sel) => (nav, sel))
            .Subscribe(static args =>
            {
                if (args is { nav.Page: FrameworkElement element, sel.Source: NavigationView { SelectedItem: NavigationViewItem selected } })
                {
                    element.DataContext = GetPropagationContext(selected);
                }
            });
    }

    #endregion
}
