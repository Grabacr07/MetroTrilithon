using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using Windows.Win32;

namespace MetroTrilithon.UI.Interop;

public class TitleBarButton : ButtonBase, IWindowProcedure
{
    private static readonly DependencyPropertyKey IsMouseOverPropertyKey;
    private static readonly DependencyPropertyKey IsPressedPropertyKey;
    private static readonly List<TitleBarButton> _buttons = [];

    static TitleBarButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(TitleBarButton),
            new FrameworkPropertyMetadata(typeof(TitleBarButton)));

        IsMouseOverPropertyKey = typeof(UIElement)
            .GetField("IsMouseOverPropertyKey", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Static)?
            .GetValue(null) as DependencyPropertyKey ?? throw new InvalidOperationException("Cannot access to IsMouseOverPropertyKey");
        IsPressedPropertyKey = typeof(ButtonBase)
            .GetField("IsPressedPropertyKey", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Static)?
            .GetValue(null) as DependencyPropertyKey ?? throw new InvalidOperationException("Cannot access to IsPressedPropertyKey");
    }

    private Window? _window;
    private IDisposable? _listener;

    protected Window Window
        => this._window ??= this.GetWindow();

    protected internal TitleBar? TitleBar { get; internal set; }

    protected virtual NCHITTEST HitTestReturnValue
        => NCHITTEST.HTHELP;

    #region WindowIsActive dependency property

    public static readonly DependencyProperty WindowIsActiveProperty
        = DependencyProperty.Register(
            nameof(WindowIsActive),
            typeof(bool),
            typeof(TitleBarButton),
            new PropertyMetadata(BooleanBoxes.FalseBox));

    public bool WindowIsActive
        => (bool)this.GetValue(WindowIsActiveProperty);

    #endregion

    #region WindowState dependency property

    public static readonly DependencyProperty WindowStateProperty
        = DependencyProperty.Register(
            nameof(WindowState),
            typeof(WindowState),
            typeof(TitleBarButton),
            new PropertyMetadata(default(WindowState)));

    public WindowState WindowState
        => (WindowState)this.GetValue(WindowStateProperty);

    #endregion

    public TitleBarButton()
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
                this.TitleBar.TitleBarButtons.Add(this);
            }
            else
            {
                // If no instance of TitleBar is found, make it work with TitleBarButton alone.
                this._listener = InteropHelper.RegisterWndProc(this);
            }
        }

        _buttons.Add(this);

        var bindingIsActive = new Binding(nameof(System.Windows.Window.IsActive))
        {
            Source = this.Window,
        };
        this.SetBinding(WindowIsActiveProperty, bindingIsActive);

        var bindingState = new Binding(nameof(System.Windows.Window.WindowState))
        {
            Source = this.Window,
        };
        this.SetBinding(WindowStateProperty, bindingState);
    }

    protected virtual void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (DesignFeatures.IsInDesignMode) return;

        this._listener?.Dispose();
        _buttons.Remove(this);
    }

    protected override void OnClick()
    {
        base.OnClick();
        this.SetValue(IsPressedPropertyKey, BooleanBoxes.FalseBox);
    }

    private void Enter()
    {
        if (this.TitleBar == null)
        {
            foreach (var other in _buttons.Where(x => x != this)) other.Leave();
        }

        this.SetValue(IsMouseOverPropertyKey, BooleanBoxes.TrueBox);
    }

    internal void Leave()
    {
        this.SetValue(IsPressedPropertyKey, BooleanBoxes.FalseBox);
        this.SetValue(IsMouseOverPropertyKey, BooleanBoxes.FalseBox);
    }

    private void Pressed()
    {
        this.SetValue(IsPressedPropertyKey, BooleanBoxes.TrueBox);
    }

    IntPtr IWindowProcedure.WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch ((WM)msg)
        {
            case WM.NCHITTEST when this.Contains(lParam):
                this.Enter();
                handled = true;
                return (IntPtr)this.HitTestReturnValue;

            case WM.NCHITTEST:
                this.Leave();
                break;

            case WM.NCMOUSELEAVE:
                this.Leave();
                break;

            case WM.NCLBUTTONDOWN when this.Contains(lParam):
                this.Pressed();
                handled = true;
                break;

            case WM.NCLBUTTONUP when this.IsPressed && this.Contains(lParam):
                this.OnClick();
                handled = true;
                break;
        }

        return IntPtr.Zero;
    }
}
