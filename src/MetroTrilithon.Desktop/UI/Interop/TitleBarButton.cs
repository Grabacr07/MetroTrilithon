using System;
using System.Reactive.Disposables;
using System.Reflection;
using System.Windows;
using System.Windows.Controls.Primitives;
using Windows.Win32;
using MetroTrilithon.Lifetime;
using MetroTrilithon.Linq;

namespace MetroTrilithon.UI.Interop;

public class TitleBarButton : ButtonBase, IWndProcListener
{
    private static readonly DependencyPropertyKey IsMouseOverPropertyKey;
    private static readonly DependencyPropertyKey IsPressedPropertyKey;
    private static readonly List<TitleBarButton> _buttons = new();

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

    private readonly CompositeDisposable _listener = new();
    private Window? _window;

    protected Window Window
        => this._window ??= this.GetWindow();

    protected internal TitleBar? TitleBar { get; internal set; }

    protected virtual NCHITTEST HitTestReturnValue
        => NCHITTEST.HTHELP;

    #region IsActive readonly dependency property

    private static readonly DependencyPropertyKey IsActivePropertyKey
        = DependencyProperty.RegisterReadOnly(
            nameof(IsActive),
            typeof(bool),
            typeof(TitleBarButton),
            new PropertyMetadata(BooleanBoxes.FalseBox));

    public static readonly DependencyProperty IsActiveProperty
        = IsActivePropertyKey.DependencyProperty;

    public bool IsActive
    {
        get => (bool)this.GetValue(IsActiveProperty);
        private set => this.SetValue(IsActivePropertyKey, BooleanBoxes.Box(value));
    }

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
                this.TitleBar.Add(this);
            }
            else
            {
                // If no instance of TitleBar is found, make it work with TitleBarButton alone.
                this._listener.Add(InteropHelper.RegisterWndProc(this));
            }
        }

        _buttons.Add(this);
        this._listener.Add(() => _buttons.Remove(this));

        this.Window.StateChanged += this.OnWindowStateChanged;
        this.Window.Activated += this.OnWindowActivated;
        this.Window.Deactivated += this.OnWindowDeactivated;
        this._listener.Add(() => this.Window.StateChanged -= this.OnWindowStateChanged);
        this._listener.Add(() => this.Window.Activated -= this.OnWindowActivated);
        this._listener.Add(() => this.Window.Deactivated -= this.OnWindowDeactivated);
    }

    protected virtual void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (DesignFeatures.IsInDesignMode) return;

        this._listener.Clear();
    }

    protected virtual void OnWindowActivated(object? sender, EventArgs e)
    {
        this.IsActive = true;
    }

    protected virtual void OnWindowDeactivated(object? sender, EventArgs e)
    {
        this.IsActive = false;
    }

    protected virtual void OnWindowStateChanged(object? sender, EventArgs e)
    {
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

    IntPtr IWndProcListener.WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch ((WM)msg)
        {
            case WM.NCHITTEST when this.Contains(lParam):
                this.Enter();
                handled = true;
                return new IntPtr((int)this.HitTestReturnValue);

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
