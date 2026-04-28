using System.Reflection;
using System.Windows;
using System.Windows.Controls.Primitives;
using Microsoft.Xaml.Behaviors;

namespace Amethystra.UI.Interactivity;

public class CallMethodButtonBehavior : Behavior<ButtonBase>
{
    private bool _hasParameter;

    #region MethodTarget dependency property

    public static readonly DependencyProperty MethodTargetProperty
        = DependencyProperty.Register(
            nameof(MethodTarget),
            typeof(object),
            typeof(CallMethodButtonBehavior),
            new UIPropertyMetadata(null));

    public object? MethodTarget
    {
        get => this.GetValue(MethodTargetProperty);
        set => this.SetValue(MethodTargetProperty, value);
    }

    #endregion

    #region MethodName dependency property

    public static readonly DependencyProperty MethodNameProperty
        = DependencyProperty.Register(
            nameof(MethodName),
            typeof(string),
            typeof(CallMethodButtonBehavior),
            new UIPropertyMetadata(null));

    public string MethodName
    {
        get => (string)this.GetValue(MethodNameProperty);
        set => this.SetValue(MethodNameProperty, value);
    }

    #endregion

    #region MethodParameter dependency property

    public static readonly DependencyProperty MethodParameterProperty
        = DependencyProperty.Register(
            nameof(MethodParameter),
            typeof(object),
            typeof(CallMethodButtonBehavior),
            new UIPropertyMetadata(null, HandleMethodParameterChanged));

    private static void HandleMethodParameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var source = (CallMethodButtonBehavior)d;
        source._hasParameter = true;
    }

    public object? MethodParameter
    {
        get => this.GetValue(MethodParameterProperty);
        set => this.SetValue(MethodParameterProperty, value);
    }

    #endregion

    public CallMethodButtonBehavior()
    {
    }

    public CallMethodButtonBehavior(CallMethodInfo info)
    {
        this.MethodTarget = info.Target;
        this.MethodName = info.Name;
        this.MethodParameter = info.Parameter;
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        this.AssociatedObject.Click += this.HandleClick;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        this.AssociatedObject.Click -= this.HandleClick;
    }

    private void HandleClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(this.MethodName)) return;

        var target = this.MethodTarget ?? this.AssociatedObject.DataContext;
        if (target == null) return;

        InvokeMethod(target, this.MethodName, this._hasParameter ? this.MethodParameter : null, this._hasParameter);
    }

    private static void InvokeMethod(object target, string methodName, object? parameter, bool hasParameter)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (method == null) return;

        method.Invoke(target, hasParameter ? [parameter] : null);
    }
}
