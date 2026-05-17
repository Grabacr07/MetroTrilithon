using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;
using Amethystra.UI.Interop;

namespace Amethystra.UI.Controls.Primitives;

/// <summary>
/// HwndSource を直接ホストする <see cref="System.Windows.Controls.Primitives.Popup"/> 相当の軽量コントロールを表します。
/// </summary>
/// <remarks>
/// <para>
/// 標準 <see cref="System.Windows.Controls.Primitives.Popup"/> の挙動を WPF 上で再現した学習用の実装で、
/// プロダクションでの利用は想定していません。Acrylic backdrop の検証で得られた知見を、
/// HwndSource、未公開 API、ライトディスミス、フォーカス制御を含めて 1 つのコントロールにまとめて記録するために保全されています。
/// </para>
/// <para>
/// 標準 <see cref="System.Windows.Controls.Primitives.Popup"/> 上で同等の見た目を得る用途では、
/// <see cref="Amethystra.UI.Interactivity.AcrylicPopupBehavior"/> や
/// <see cref="Amethystra.UI.Interactivity.AcrylicContextMenuBehavior"/> を利用してください。
/// </para>
/// <para>
/// Acrylic 適用には新 API (DWMWA_SYSTEMBACKDROP_TYPE) ではなく旧未公開 API
/// (SetWindowCompositionAttribute + ACCENT_ENABLE_ACRYLICBLURBEHIND) を使用します。
/// 新 API は WS_EX_NOACTIVATE 付きの HWND では Acrylic が適用されず、親ウィンドウのアクティブ表示を
/// 維持する用途と両立できないためです。
/// </para>
/// </remarks>
[ContentProperty(nameof(Child))]
internal class CustomPopup : FrameworkElement
{
    #region IsOpen dependency property

    public static readonly DependencyProperty IsOpenProperty
        = DependencyProperty.Register(
            nameof(IsOpen),
            typeof(bool),
            typeof(CustomPopup),
            new FrameworkPropertyMetadata(
                false,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                HandleIsOpenPropertyChanged));

    /// <summary>
    /// ポップアップが表示されているかどうかを示す値を取得または設定します。
    /// </summary>
    /// <value>
    /// ポップアップが表示されている場合は <see langword="true"/>。それ以外の場合は <see langword="false"/>。既定値は <see langword="false"/>。
    /// </value>
    public bool IsOpen
    {
        get => (bool)this.GetValue(IsOpenProperty);
        set => this.SetValue(IsOpenProperty, value);
    }

    private static void HandleIsOpenPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CustomPopup popup) popup.UpdatePopupVisibility((bool)e.NewValue);
    }

    #endregion

    #region Child dependency property

    public static readonly DependencyProperty ChildProperty
        = DependencyProperty.Register(
            nameof(Child),
            typeof(UIElement),
            typeof(CustomPopup),
            new FrameworkPropertyMetadata(null, HandleChildPropertyChanged));

    /// <summary>
    /// ポップアップ内に表示する内容を取得または設定します。
    /// </summary>
    public UIElement? Child
    {
        get => (UIElement?)this.GetValue(ChildProperty);
        set => this.SetValue(ChildProperty, value);
    }

    private static void HandleChildPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not CustomPopup popup) return;

        if (e.OldValue is UIElement oldChild)
        {
            popup.RemoveLogicalChild(oldChild);
        }

        if (e.NewValue is UIElement newChild)
        {
            popup.AddLogicalChild(newChild);
        }

        popup._popupRoot?.Child = e.NewValue as UIElement;
    }

    #endregion

    #region PlacementTarget dependency property

    public static readonly DependencyProperty PlacementTargetProperty
        = DependencyProperty.Register(
            nameof(PlacementTarget),
            typeof(UIElement),
            typeof(CustomPopup),
            new FrameworkPropertyMetadata(null));

    /// <summary>
    /// ポップアップの相対的な配置基準となる要素を取得または設定します。
    /// </summary>
    public UIElement? PlacementTarget
    {
        get => (UIElement?)this.GetValue(PlacementTargetProperty);
        set => this.SetValue(PlacementTargetProperty, value);
    }

    #endregion

    private HwndSource? _hwndSource;
    private CustomPopupRoot? _popupRoot;
    private DispatcherOperation? _pendingCreate;
    private DispatcherOperation? _pendingDestroy;
    private HwndSourceHook? _windowMessageHook;

    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo
    private const int MA_NOACTIVATE = 0x0003;

    static CustomPopup()
    {
        VisibilityProperty.OverrideMetadata(
            typeof(CustomPopup),
            new FrameworkPropertyMetadata(Visibility.Collapsed));
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
        => new();

    private void UpdatePopupVisibility(bool isOpen)
    {
        if (isOpen)
        {
            this.CancelPendingDestroy();
            this.SchedulePendingCreate();
        }
        else
        {
            this.CancelPendingCreate();
            this.SchedulePendingDestroy();
        }
    }

    private void SchedulePendingCreate()
    {
        if (this._pendingCreate != null) return;
        this._pendingCreate = this.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(this.CreateWindow));
    }

    private void SchedulePendingDestroy()
    {
        if (this._pendingDestroy != null) return;
        this._pendingDestroy = this.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(this.DestroyWindow));
    }

    private void CancelPendingCreate()
    {
        this._pendingCreate?.Abort();
        this._pendingCreate = null;
    }

    private void CancelPendingDestroy()
    {
        this._pendingDestroy?.Abort();
        this._pendingDestroy = null;
    }

    private void CreateWindow()
    {
        this._pendingCreate = null;

        if (this._hwndSource != null) return;
        if (this.Child == null) return;

        var (originX, originY) = this.ComputeTargetOrigin();
        var (monitorLeft, monitorTop) = GetMonitorOrigin(originX, originY);
        var ownerHwnd = this.GetOwnerHwnd();

        var style = WINDOW_STYLE.WS_POPUP
            | WINDOW_STYLE.WS_CLIPSIBLINGS;
        var styleEx = WINDOW_EX_STYLE.WS_EX_TOOLWINDOW
            | WINDOW_EX_STYLE.WS_EX_NOACTIVATE
            | WINDOW_EX_STYLE.WS_EX_TOPMOST;

        var parameters = new HwndSourceParameters(string.Empty)
        {
            WindowStyle = unchecked((int)(uint)style),
            ExtendedWindowStyle = unchecked((int)(uint)styleEx),
            UsesPerPixelOpacity = false,
            PositionX = monitorLeft,
            PositionY = monitorTop,
        };

        if (ownerHwnd != IntPtr.Zero)
        {
            parameters.ParentWindow = ownerHwnd;
        }

        var hwndSource = new HwndSource(parameters);
        this._hwndSource = hwndSource;

        this._popupRoot = new CustomPopupRoot
        {
            Child = this.Child,
        };
        hwndSource.RootVisual = this._popupRoot;

        // CompositionTarget.BackgroundColor は RootVisual 設定の前後どちらでも良いが、
        // BetterExplorer の慣例に従い「設定完了後」に Acrylic を当てる流れにする。
        if (hwndSource.CompositionTarget is { } compositionTarget)
        {
            compositionTarget.BackgroundColor = Colors.Transparent;
        }

        AcrylicWindowEffect.Apply(hwndSource.Handle);

        this._windowMessageHook = this.WindowMessageHook;
        hwndSource.AddHook(this._windowMessageHook);
        hwndSource.DpiChanged += this.OnHwndSourceDpiChanged;

        FocusManager.SetIsFocusScope(this._popupRoot, true);

        this._popupRoot.PreviewMouseLeftButtonDown += this.OnPopupRootPreviewMouseDown;
        this._popupRoot.PreviewMouseRightButtonDown += this.OnPopupRootPreviewMouseDown;
        Mouse.Capture(this._popupRoot, CaptureMode.SubTree);

        this._popupRoot.KeyDown += this.OnPopupRootKeyDown;

        MovePopup(hwndSource.Handle, originX, originY);

        this.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(this.MoveInitialFocus));
    }

    private void DestroyWindow()
    {
        this._pendingDestroy = null;

        var hwndSource = this._hwndSource;
        if (hwndSource == null) return;

        if (this._popupRoot != null)
        {
            this._popupRoot.PreviewMouseLeftButtonDown -= this.OnPopupRootPreviewMouseDown;
            this._popupRoot.PreviewMouseRightButtonDown -= this.OnPopupRootPreviewMouseDown;
            this._popupRoot.KeyDown -= this.OnPopupRootKeyDown;

            if (ReferenceEquals(Mouse.Captured, this._popupRoot))
            {
                Mouse.Capture(null);
            }

            this._popupRoot.Child = null;
        }

        hwndSource.DpiChanged -= this.OnHwndSourceDpiChanged;

        if (this._windowMessageHook != null)
        {
            hwndSource.RemoveHook(this._windowMessageHook);
            this._windowMessageHook = null;
        }

        hwndSource.RootVisual = null;
        hwndSource.Dispose();

        this._hwndSource = null;
        this._popupRoot = null;
    }

    private void OnPopupRootPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        var popupRoot = this._popupRoot;
        if (popupRoot == null) return;

        if (ReferenceEquals(e.OriginalSource, popupRoot)
            && popupRoot.InputHitTest(e.GetPosition(popupRoot)) == null)
        {
            this.IsOpen = false;
        }
    }

    private void OnPopupRootKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            this.IsOpen = false;
            e.Handled = true;
        }
    }

    private IntPtr WindowMessageHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch (msg)
        {
            case (int)WM.MOUSEACTIVATE:
                handled = true;
                return MA_NOACTIVATE;

            case (int)WM.ACTIVATEAPP when wParam == IntPtr.Zero:
                this.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(this.CloseFromDeactivateApp));
                break;
        }

        return IntPtr.Zero;
    }

    private void CloseFromDeactivateApp()
    {
        this.IsOpen = false;
    }

    private void OnHwndSourceDpiChanged(object? sender, HwndDpiChangedEventArgs e)
    {
        e.Handled = true;
    }

    private static void MovePopup(IntPtr hwnd, int screenX, int screenY)
    {
        var h = new HWND(hwnd);

        PInvoke.SetWindowPos(
            h,
            HWND.Null,
            screenX,
            screenY,
            0,
            0,
            SET_WINDOW_POS_FLAGS.SWP_NOZORDER
            | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE
            | SET_WINDOW_POS_FLAGS.SWP_NOSIZE);

        PInvoke.ShowWindow(h, SHOW_WINDOW_CMD.SW_SHOWNA);
    }

    private IntPtr GetOwnerHwnd()
        => this.PlacementTarget is Visual target && PresentationSource.FromVisual(target) is HwndSource source
            ? source.Handle
            : IntPtr.Zero;

    private (int x, int y) ComputeTargetOrigin()
    {
        if (this.PlacementTarget is not Visual target || PresentationSource.FromVisual(target) == null) return (100, 100);

        var localBottomLeft = new Point(
            0,
            target is FrameworkElement fe ? fe.ActualHeight : 0);
        var screenBottomLeft = target.PointToScreen(localBottomLeft);
        return ((int)screenBottomLeft.X, (int)screenBottomLeft.Y);
    }

    private static (int x, int y) GetMonitorOrigin(int screenX, int screenY)
    {
        var point = new System.Drawing.Point(screenX, screenY);
        var hMonitor = PInvoke.MonitorFromPoint(point, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
        if (hMonitor.IsNull) return (screenX, screenY);

        var info = new MONITORINFO { cbSize = (uint)Marshal.SizeOf<MONITORINFO>(), };
        return PInvoke.GetMonitorInfo(hMonitor, ref info)
            ? (info.rcMonitor.left, info.rcMonitor.top)
            : (screenX, screenY);
    }

    private void MoveInitialFocus()
    {
        this._popupRoot?.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
    }
}

/// <summary>
/// <see cref="CustomPopup"/> がホストする HwndSource のルートビジュアルを表します。
/// </summary>
/// <remarks>
/// 子要素を論理ツリーに参加させない非論理アダプタとして実装されています。
/// 論理ツリーに参加させると、子要素が外側の <see cref="CustomPopup"/> インスタンスと、
/// この HwndSource の両方を親に持つことになり <see cref="InvalidOperationException"/> が発生します。
/// </remarks>
internal class CustomPopupRoot : FrameworkElement
{
    private UIElement? _child;

    /// <summary>
    /// このルートに表示する子要素を取得または設定します。
    /// </summary>
    public UIElement? Child
    {
        get => this._child;
        set
        {
            if (ReferenceEquals(this._child, value)) return;

            if (this._child != null)
            {
                this.RemoveVisualChild(this._child);
            }

            this._child = value;

            if (this._child != null)
            {
                this.AddVisualChild(this._child);
            }

            this.InvalidateMeasure();
        }
    }

    /// <inheritdoc />
    protected override int VisualChildrenCount
        => this._child == null ? 0 : 1;

    /// <inheritdoc />
    protected override Visual GetVisualChild(int index)
    {
        if (this._child == null || index != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return this._child;
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        if (this._child == null) return new Size();

        this._child.Measure(availableSize);
        return this._child.DesiredSize;
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        this._child?.Arrange(new Rect(finalSize));
        return finalSize;
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer()
        => new FrameworkElementAutomationPeer(this);
}
