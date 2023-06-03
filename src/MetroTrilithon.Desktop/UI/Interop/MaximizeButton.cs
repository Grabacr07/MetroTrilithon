using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Windows.Win32;

namespace MetroTrilithon.UI.Interop;

public class MaximizeButton : TitleBarButton
{
    static MaximizeButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(MaximizeButton),
            new FrameworkPropertyMetadata(typeof(MaximizeButton)));
    }

    protected override NCHITTEST HitTestReturnValue
        => NCHITTEST.HTMAXBUTTON;

    #region CanMaximize dependency property

    public static readonly DependencyProperty CanMaximizeProperty
        = DependencyProperty.Register(
            nameof(CanMaximize),
            typeof(bool),
            typeof(MaximizeButton),
            new PropertyMetadata(BooleanBoxes.TrueBox));

    public bool CanMaximize
    {
        get => (bool)this.GetValue(CanMaximizeProperty);
        set => this.SetValue(CanMaximizeProperty, BooleanBoxes.Box(value));
    }

    #endregion

    protected override void OnClick()
    {
        base.OnClick();

        if (this.CanMaximize)
        {
            this.Window.WindowState = this.Window.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }
    }
}
