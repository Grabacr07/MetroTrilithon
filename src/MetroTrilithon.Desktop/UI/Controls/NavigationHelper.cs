using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Navigation;
using Microsoft.Xaml.Behaviors;
using Wpf.Ui.Common;
using Wpf.Ui.Controls;
using Wpf.Ui.Controls.Navigation;

namespace MetroTrilithon.UI.Controls;

public class NavigationHelper
{
    #region PropagationContext attached property

    private static readonly HashSet<NavigationBase> _knownItems = new();

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
            .OfType<NavigationBase>()
            .FirstOrDefault();
        if (navigation?.Frame == null || _knownItems.Add(navigation) == false) return;

        Observable.FromEvent<RoutedNavigationEvent, RoutedNavigationEventArgs>(
#pragma warning disable CS8622
                handler => (_, args) => handler(args),
#pragma warning restore CS8622
                handler => navigation.Navigated += handler,
                handler => navigation.Navigated -= handler)
            .Zip(Observable.FromEvent<NavigatedEventHandler, NavigationEventArgs>(
                    handler => (_, args) => handler(args),
                    handler => navigation.Frame.Navigated += handler,
                    handler => navigation.Frame.Navigated -= handler))
            .Subscribe(args =>
            {
                if (args.First.CurrentPage is NavigationItem navigationItem
                    && args.Second.Content is FrameworkElement element)
                {
                    element.DataContext = element.DataContext = GetPropagationContext(navigationItem);
                }
            });

        #endregion
    }
}
