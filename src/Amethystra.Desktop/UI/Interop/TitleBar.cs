using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Windows.Win32;
using Wpf.Ui.Controls;

namespace Amethystra.UI.Interop;

[TemplatePart(Name = PART_CloseButton, Type = typeof(CloseButton))]
[TemplatePart(Name = PART_MaximizeButton, Type = typeof(MaximizeButton))]
[TemplatePart(Name = PART_MinimizeButton, Type = typeof(MinimizeButton))]
[TemplatePart(Name = PART_Icon, Type = typeof(TitleBarIcon))]
public class TitleBar : ContentControl, IWindowProcedure
{
    private const string PART_CloseButton = nameof(PART_CloseButton);
    private const string PART_MaximizeButton = nameof(PART_MaximizeButton);
    private const string PART_MinimizeButton = nameof(PART_MinimizeButton);
    private const string PART_Icon = nameof(PART_Icon);

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

    #region Icon dependency property

    public static readonly DependencyProperty IconProperty
        = DependencyProperty.Register(
            nameof(Icon),
            typeof(IconElement),
            typeof(TitleBar),
            new PropertyMetadata(null));

    public IconElement? Icon
    {
        get => (IconElement?)this.GetValue(IconProperty);
        set => this.SetValue(IconProperty, value);
    }

    #endregion

    #region Title dependency property

    public static readonly DependencyProperty TitleProperty
        = DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(TitleBar),
            new PropertyMetadata(null));

    public string? Title
    {
        get => (string?)this.GetValue(TitleProperty);
        set => this.SetValue(TitleProperty, value);
    }

    #endregion

    #region IconAction dependency property

    public static readonly DependencyProperty IconActionProperty
        = DependencyProperty.Register(
            nameof(IconAction),
            typeof(IconAction),
            typeof(TitleBar),
            new PropertyMetadata(IconAction.None));

    public IconAction IconAction
    {
        get => (IconAction)this.GetValue(IconActionProperty);
        set => this.SetValue(IconActionProperty, value);
    }

    #endregion

    // ReSharper disable once CollectionNeverQueried.Local
    private readonly CompositeDisposable _listeners = new();
    private readonly List<UIElement> _interactiveElements = new();
    private Window? _window;

    public Window Window
        => this._window ??= this.GetWindow();

    internal TitleBarIcon? TitleBarIcon { get; set; }

    internal HashSet<TitleBarButton> TitleBarButtons { get; } = new();

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
            this.TitleBarButtons.Add(closeButton);
        }

        if (this.GetTemplateChild(PART_MaximizeButton) is MaximizeButton maximizeButton)
        {
            maximizeButton.TitleBar = this;
            this.TitleBarButtons.Add(maximizeButton);
        }

        if (this.GetTemplateChild(PART_MinimizeButton) is MinimizeButton minimizeButton)
        {
            minimizeButton.TitleBar = this;
            this.TitleBarButtons.Add(minimizeButton);
        }

        if (this.GetTemplateChild(PART_Icon) is TitleBarIcon icon)
        {
            icon.TitleBar = this;
            this.TitleBarIcon = icon;
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

    IntPtr IWindowProcedure.WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if ((WM)msg is not (WM.NCHITTEST or WM.NCMOUSELEAVE or WM.NCLBUTTONDOWN or WM.NCLBUTTONUP)
            || this._interactiveElements.Any(element => element.Contains(lParam)))
        {
            return IntPtr.Zero;
        }

        var returnValue = IntPtr.Zero;
        foreach (var button in this.TitleBarButtons)
        {
            if (handled)
            {
                // 他のボタンが応答を返していた場合、残りのボタンはすべて Leave 処理だけ
                button.Leave();
            }
            else
            {
                returnValue = ((IWindowProcedure)button).WndProc(hwnd, msg, wParam, lParam, ref handled);
            }
        }

        if (handled == false && this.TitleBarIcon is not null)
        {
            returnValue = ((IWindowProcedure)this.TitleBarIcon).WndProc(hwnd, msg, wParam, lParam, ref handled);
        }

        if (handled) return returnValue;

        switch ((WM)msg)
        {
            case WM.NCHITTEST when this.Contains(lParam):
                handled = true;
                return (IntPtr)NCHITTEST.HTCAPTION;

            default:
                return IntPtr.Zero;
        }
    }
}
