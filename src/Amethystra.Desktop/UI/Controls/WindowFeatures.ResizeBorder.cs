using System.Linq;
using System.Windows;
using Amethystra.UI.Interactivity;
using Microsoft.Xaml.Behaviors;

namespace Amethystra.UI.Controls;

public static partial class WindowFeatures
{
    #region ExtendsResizeBorder attached property

    public static readonly DependencyProperty ExtendsResizeBorderProperty
        = DependencyProperty.RegisterAttached(
            nameof(ExtendsResizeBorderProperty).GetPropertyName(),
            typeof(bool),
            typeof(WindowFeatures),
            new PropertyMetadata(BooleanBoxes.FalseBox, HandleExtendsResizeBorderPropertyChanged));

    public static void SetExtendsResizeBorder(Window element, bool value)
        => element.SetValue(ExtendsResizeBorderProperty, BooleanBoxes.Box(value));

    public static bool GetExtendsResizeBorder(Window element)
        => (bool)element.GetValue(ExtendsResizeBorderProperty);

    private static void HandleExtendsResizeBorderPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Window window) return;

        var behaviors = Interaction.GetBehaviors(window);
        var existing = behaviors.OfType<WindowResizeBorderBehavior>().FirstOrDefault();

        if (e.NewValue is true)
        {
            if (existing == null) behaviors.Add(new WindowResizeBorderBehavior());
        }
        else
        {
            if (existing != null) behaviors.Remove(existing);
        }
    }

    #endregion
}
