using System.Linq;
using System.Windows;
using Amethystra.UI.Interactivity;
using Microsoft.Xaml.Behaviors;

namespace Amethystra.UI.Controls;

public static partial class WindowFeatures
{
    #region LifecycleContext attached property

    public static readonly DependencyProperty LifecycleContextProperty
        = DependencyProperty.RegisterAttached(
            nameof(LifecycleContextProperty).GetPropertyName(),
            typeof(object),
            typeof(WindowFeatures),
            new PropertyMetadata(null, HandleLifecycleContextPropertyChanged));

    public static void SetLifecycleContext(Window element, object? value)
        => element.SetValue(LifecycleContextProperty, value);

    public static object? GetLifecycleContext(Window element)
        => element.GetValue(LifecycleContextProperty);

    private static void HandleLifecycleContextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Window window) return;

        var behaviors = Interaction.GetBehaviors(window);
        var existing = behaviors.OfType<WindowLifecycleBehavior>().FirstOrDefault();

        if (e.NewValue != null)
        {
            if (existing == null)
            {
                behaviors.Add(new WindowLifecycleBehavior { Context = e.NewValue });
            }
            else
            {
                existing.Context = e.NewValue;
            }
        }
        else
        {
            if (existing != null) behaviors.Remove(existing);
        }
    }

    #endregion
}
