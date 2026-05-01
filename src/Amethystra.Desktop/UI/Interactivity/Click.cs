using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using Microsoft.Xaml.Behaviors;

namespace Amethystra.UI.Interactivity;

public static class Click
{
    #region CallMethod attached property

    public static readonly DependencyProperty CallMethodProperty
        = DependencyProperty.RegisterAttached(
            nameof(CallMethodProperty).GetPropertyName(),
            typeof(object),
            typeof(Click),
            new PropertyMetadata(null, HandleCallMethodPropertyChanged));

    public static void SetCallMethod(DependencyObject element, object value)
        => element.SetValue(CallMethodProperty, value);

    public static object GetCallMethod(DependencyObject element)
        => element.GetValue(CallMethodProperty);

    private static void HandleCallMethodPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        switch (d)
        {
            case ButtonBase button when e.NewValue is CallMethodInfo info:
                Interaction.GetBehaviors(button).Add(new CallMethodButtonBehavior(info));
                break;

            case ButtonBase button when e.NewValue is string name:
                Interaction.GetBehaviors(button).Add(new CallMethodButtonBehavior() { MethodName = name, });
                break;
        }
    }

    #endregion
}

public class CallMethodInfo(string name, object? parameter, object? target)
{
    public string Name { get; set; } = name;

    public object? Parameter { get; set; } = parameter;

    public object? Target { get; set; } = target;

    public CallMethodInfo(string name)
        : this(name, null, null)
    {
    }
}
