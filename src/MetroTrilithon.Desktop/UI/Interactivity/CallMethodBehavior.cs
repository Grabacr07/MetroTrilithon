using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Livet.Behaviors;
using Microsoft.Xaml.Behaviors;

namespace MetroTrilithon.UI.Interactivity;

public class CallMethodBehavior : Behavior<System.Windows.Controls.Primitives.ButtonBase>
{
    private readonly MethodBinder _binder = new();
    private readonly MethodBinderWithArgument _binderWithArgument = new();
    private bool _hasParameter;

    #region MethodTarget dependency property

    public static readonly DependencyProperty MethodTargetProperty
        = DependencyProperty.Register(
            nameof(MethodTarget),
            typeof(object),
            typeof(CallMethodBehavior),
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
            typeof(CallMethodBehavior),
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
            typeof(CallMethodBehavior),
            new UIPropertyMetadata(null, HandleMethodParameterChanged));

    private static void HandleMethodParameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var source = (CallMethodBehavior)d;
        source._hasParameter = true;
    }

    public object MethodParameter
    {
        get => this.GetValue(MethodParameterProperty);
        set => this.SetValue(MethodParameterProperty, value);
    }

    #endregion

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
