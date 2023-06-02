using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Windows.Win32;

namespace MetroTrilithon.UI.Interop;

public class MinimizeButton : TitleBarButton
{
    static MinimizeButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(MinimizeButton),
            new FrameworkPropertyMetadata(typeof(MinimizeButton)));
    }

    protected override NCHITTEST HitTestReturnValue
        => NCHITTEST.HTMINBUTTON;

    #region CanMinimize dependency property

    public static readonly DependencyProperty CanMinimizeProperty
        = DependencyProperty.Register(
            nameof(CanMinimize),
            typeof(bool),
            typeof(MinimizeButton),
            new PropertyMetadata(BooleanBoxes.TrueBox));

    public bool CanMinimize
    {
        get => (bool)this.GetValue(CanMinimizeProperty);
        set => this.SetValue(CanMinimizeProperty, BooleanBoxes.Box(value));
    }

    #endregion

    protected override void OnClick()
    {
        base.OnClick();

        if (this.CanMinimize)
        {
            this.Window.WindowState = WindowState.Minimized;
        }
    }
}
