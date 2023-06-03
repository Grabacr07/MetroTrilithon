using System;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Windows.Win32;

namespace MetroTrilithon.UI.Interop;

[TemplatePart(Name = PART_CloseButton, Type = typeof(CloseButton))]
[TemplatePart(Name = PART_MaximizeButton, Type = typeof(MaximizeButton))]
[TemplatePart(Name = PART_MinimizeButton, Type = typeof(MinimizeButton))]
public class TitleBar : ContentControl, IWndProcListener
{
    private const string PART_CloseButton = nameof(PART_CloseButton);
    private const string PART_MaximizeButton = nameof(PART_MaximizeButton);
    private const string PART_MinimizeButton = nameof(PART_MinimizeButton);

    static TitleBar()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(TitleBar),
            new FrameworkPropertyMetadata(typeof(TitleBar)));
    }

    #region Interactive attached property

    public static readonly DependencyProperty InteractiveProperty
        = DependencyProperty.RegisterAttached(
            nameof(InteractiveProperty).GetPropertyName(),
            typeof(bool),
            typeof(TitleBar),
            new FrameworkPropertyMetadata(BooleanBoxes.FalseBox, HandleInteractivePropertyChanged));

    public static void SetInteractive(DependencyObject element, bool value)
        => element.SetValue(InteractiveProperty, value);

    public static bool GetInteractive(DependencyObject element)
        => (bool)element.GetValue(InteractiveProperty);

    private static void HandleInteractivePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement element && element.FindLogicalAncestor<TitleBar>() is { } titleBar)
        {
            if (e.NewValue is true)
            {
                titleBar._interactiveElements.Add(element);
            }
            else
            {
                titleBar._interactiveElements.Remove(element);
            }
        }
    }

    #endregion

    #region CanMaximize dependency property

    public static readonly DependencyProperty CanMaximizeProperty
        = DependencyProperty.Register(
            nameof(CanMaximize),
            typeof(bool),
            typeof(TitleBar),
            new PropertyMetadata(BooleanBoxes.TrueBox));

    public bool CanMaximize
    {
        get => (bool)this.GetValue(CanMaximizeProperty);
        set => this.SetValue(CanMaximizeProperty, BooleanBoxes.Box(value));
    }

    #endregion

    #region CanMinimize dependency property

    public static readonly DependencyProperty CanMinimizeProperty
        = DependencyProperty.Register(
            nameof(CanMinimize),
            typeof(bool),
            typeof(TitleBar),
            new PropertyMetadata(BooleanBoxes.TrueBox));

    public bool CanMinimize
    {
        get => (bool)this.GetValue(CanMinimizeProperty);
        set => this.SetValue(CanMinimizeProperty, BooleanBoxes.Box(value));
    }

    #endregion

    // ReSharper disable once CollectionNeverQueried.Local
    private readonly CompositeDisposable _listeners = new();
    private readonly HashSet<TitleBarButton> _knownButtons = new();
    private readonly List<UIElement> _interactiveElements = new();
    private Window? _window;

    public Window Window
        => this._window ??= this.GetWindow();

    public TitleBar()
    {
        this.Loaded += this.OnLoaded;
        this.Unloaded += this.OnUnloaded;
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (this.GetTemplateChild(PART_CloseButton) is CloseButton closeButton)
        {
            closeButton.TitleBar = this;
            this._knownButtons.Add(closeButton);
        }

        if (this.GetTemplateChild(PART_MaximizeButton) is MaximizeButton maximizeButton)
        {
            maximizeButton.TitleBar = this;
            var binding = new Binding(nameof(this.CanMaximize))
            {
                Source = this,
                Mode = BindingMode.TwoWay,
            };
            BindingOperations.SetBinding(maximizeButton, MaximizeButton.CanMaximizeProperty, binding);
            this._knownButtons.Add(maximizeButton);
        }

        if (this.GetTemplateChild(PART_MinimizeButton) is MinimizeButton minimizeButton)
        {
            minimizeButton.TitleBar = this;
            var binding = new Binding(nameof(this.CanMinimize))
            {
                Source = this,
                Mode = BindingMode.TwoWay,
            };
            BindingOperations.SetBinding(minimizeButton, MinimizeButton.CanMinimizeProperty, binding);
            this._knownButtons.Add(minimizeButton);
        }
    }

    protected virtual void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DesignFeatures.IsInDesignMode) return;

        this._listeners.Add(InteropHelper.RegisterWndProc(this));
    }

    protected virtual void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (DesignFeatures.IsInDesignMode) return;

        this._listeners.Clear();
    }

    internal void Add(TitleBarButton button)
        => this._knownButtons.Add(button);

    IntPtr IWndProcListener.WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if ((WM)msg is not (WM.NCHITTEST or WM.NCMOUSELEAVE or WM.NCLBUTTONDOWN or WM.NCLBUTTONUP)
            || this._interactiveElements.Any(element => element.Contains(lParam)))
        {
            return IntPtr.Zero;
        }

        var returnValue = IntPtr.Zero;
        foreach (var button in this._knownButtons)
        {
            if (handled)
            {
                // 他のボタンが応答を返していた場合、残りのボタンはすべて Leave 処理だけ
                button.Leave();
            }
            else
            {
                returnValue = ((IWndProcListener)button).WndProc(hwnd, msg, wParam, lParam, ref handled);
            }
        }

        if (handled) return returnValue;

        switch ((WM)msg)
        {
            case WM.NCHITTEST when this.Contains(lParam):
                handled = true;
                return new IntPtr((int)NCHITTEST.HTCAPTION);

            default:
                return IntPtr.Zero;
        }
    }
}
