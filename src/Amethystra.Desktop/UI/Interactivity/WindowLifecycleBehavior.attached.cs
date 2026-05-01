using System.Windows;
using Microsoft.Xaml.Behaviors;

namespace Amethystra.UI.Interactivity;

public partial class WindowLifecycleBehavior
{
    #region Enabled attached property

    public static readonly DependencyProperty EnabledProperty
        = DependencyProperty.RegisterAttached(
            nameof(EnabledProperty).GetPropertyName(),
            typeof(bool),
            typeof(WindowLifecycleBehavior),
            new PropertyMetadata(BooleanBoxes.FalseBox, HandleEnabledPropertyChanged));

    public static bool GetEnabled(Window window)
        => (bool)window.GetValue(EnabledProperty);

    public static void SetEnabled(Window window, bool value)
        => window.SetValue(EnabledProperty, BooleanBoxes.Box(value));

    private static void HandleEnabledPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Window window) return;

        var behaviors = Interaction.GetBehaviors(window);
        var existing = behaviors.OfType<WindowLifecycleBehavior>().FirstOrDefault();

        if ((bool)e.NewValue)
        {
            if (existing == null) behaviors.Add(new WindowLifecycleBehavior());
        }
        else
        {
            if (existing != null) behaviors.Remove(existing);
        }
    }

    #endregion
}
