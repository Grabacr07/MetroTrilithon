using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Windows.Win32;
using Wpf.Ui.Controls;

namespace Amethystra.UI.Interop;

public enum IconAction
{
    None,
    CloseWindow,
}

public class TitleBarIcon : ContentControl, IWindowProcedure
{
    static TitleBarIcon()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(TitleBarIcon),
            new FrameworkPropertyMetadata(typeof(TitleBarIcon)));
    }

    private IDisposable? _listener;

    protected internal TitleBar? TitleBar { get; internal set; }

    #region Icon dependency property

    public static readonly DependencyProperty IconProperty
        = DependencyProperty.Register(
            nameof(Icon),
            typeof(IconElement),
            typeof(TitleBarIcon),
            new PropertyMetadata(null));

    public IconElement? Icon
    {
        get => (IconElement?)this.GetValue(IconProperty);
        set => this.SetValue(IconProperty, value);
    }

    #endregion

    #region Action dependency property

    public static readonly DependencyProperty ActionProperty
        = DependencyProperty.Register(
            nameof(Action),
            typeof(IconAction),
            typeof(TitleBarIcon),
            new PropertyMetadata(IconAction.None));

    public IconAction Action
    {
        get => (IconAction)this.GetValue(ActionProperty);
        set => this.SetValue(ActionProperty, value);
    }

    #endregion

    public TitleBarIcon()
    {
        this.Loaded += this.OnLoaded;
        this.Unloaded += this.OnUnloaded;
    }

    protected virtual void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DesignFeatures.IsInDesignMode) return;

        if (this.TitleBar == null)
        {
            var titleBar = this.FindLogicalAncestor<TitleBar>();
            if (titleBar != null)
            {
                this.TitleBar = titleBar;
                this.TitleBar.TitleBarIcon = this;
            }
            else
            {
                // If no instance of TitleBar is found, make it work with TitleBarIcon alone.
                this._listener = InteropHelper.RegisterWndProc(this);
            }
        }
    }

    protected virtual void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (DesignFeatures.IsInDesignMode) return;

        this._listener?.Dispose();
    }

    IntPtr IWindowProcedure.WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch ((WM)msg)
        {
            case WM.NCHITTEST when this.Action == IconAction.CloseWindow && this.Contains(lParam):
                handled = true;
                return (IntPtr)NCHITTEST.HTSYSMENU;

            default:
                return IntPtr.Zero;
        }
    }
}
