using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Livet.Behaviors;
using Microsoft.Xaml.Behaviors;

namespace Amethystra.UI.Interactivity;

public class CallMethodButtonBehavior : Behavior<System.Windows.Controls.Primitives.ButtonBase>
{
    private readonly MethodBinder _binder = new();
    private readonly MethodBinderWithArgument _binderWithArgument = new();
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

        if (this._hasParameter)
        {
            this._binderWithArgument.Invoke(target, this.MethodName, this.MethodParameter);
        }
        else
        {
            this._binder.Invoke(target, this.MethodName);
        }
    }
}
