using System.Linq;
using System.Windows;
using Amethystra.UI.Interactivity;
using Microsoft.Xaml.Behaviors;

namespace Amethystra.UI.Controls;

public static partial class WindowFeatures
{
    #region IsClientAreaDraggable attached property

    public static readonly DependencyProperty IsClientAreaDraggableProperty
        = DependencyProperty.RegisterAttached(
            nameof(IsClientAreaDraggableProperty).GetPropertyName(),
            typeof(bool),
            typeof(WindowFeatures),
            new PropertyMetadata(false, HandleIsClientAreaDraggablePropertyChanged));

    public static void SetIsClientAreaDraggable(Window element, bool value)
        => element.SetValue(IsClientAreaDraggableProperty, value);

    public static bool GetIsClientAreaDraggable(Window element)
        => (bool)element.GetValue(IsClientAreaDraggableProperty);

    private static void HandleIsClientAreaDraggablePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Window window) return;

        var behaviors = Interaction.GetBehaviors(window);
        var existing = behaviors.OfType<WindowDragMoveBehavior>().FirstOrDefault();

        if (e.NewValue is true)
        {
            if (existing == null) behaviors.Add(new WindowDragMoveBehavior());
        }
        else
        {
            if (existing != null) behaviors.Remove(existing);
        }
    }

    #endregion
}
