using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Wpf.Ui.Controls;

namespace Amethystra.UI.Interop;

public class TitleBarSymbolButton : TitleBarButton
{
    static TitleBarSymbolButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(TitleBarSymbolButton),
            new FrameworkPropertyMetadata(typeof(TitleBarSymbolButton)));
    }

    #region Symbol dependency property

    public static readonly DependencyProperty SymbolProperty
        = DependencyProperty.Register(
            nameof(Symbol),
            typeof(SymbolRegular),
            typeof(TitleBarSymbolButton),
            new PropertyMetadata(default(SymbolRegular)));

    public SymbolRegular Symbol
    {
        get => (SymbolRegular)this.GetValue(SymbolProperty);
        set => this.SetValue(SymbolProperty, value);
    }

    #endregion

    #region Filled dependency property

    public static readonly DependencyProperty FilledProperty
        = DependencyProperty.Register(
            nameof(Filled),
            typeof(bool),
            typeof(TitleBarSymbolButton),
            new PropertyMetadata(BooleanBoxes.FalseBox));

    public bool Filled
    {
        get => (bool)this.GetValue(FilledProperty);
        set => this.SetValue(FilledProperty, BooleanBoxes.Box(value));
    }

    #endregion

    #region CornerRadius dependency property

    public static readonly DependencyProperty CornerRadiusProperty
        = DependencyProperty.Register(
            nameof(CornerRadius),
            typeof(CornerRadius),
            typeof(TitleBarSymbolButton),
            new FrameworkPropertyMetadata(default(CornerRadius), FrameworkPropertyMetadataOptions.None, HandleCornerRadiusPropertyChanged));

    private static void HandleCornerRadiusPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var instance = d as TitleBarSymbolButton;
        instance?.OnCornerRadiusChanged(e);
    }

    protected virtual void OnCornerRadiusChanged(DependencyPropertyChangedEventArgs e)
    {
    }

    public CornerRadius CornerRadius
    {
        get => (CornerRadius)this.GetValue(CornerRadiusProperty);
        set => this.SetValue(CornerRadiusProperty, value);
    }

    #endregion
}
