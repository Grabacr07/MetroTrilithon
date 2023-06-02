using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Wpf.Ui.Common;

namespace MetroTrilithon.UI.Interop;

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
}
