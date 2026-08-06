using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Microsoft.Xaml.Behaviors;

namespace Amethystra.UI.Interactivity;

/// <summary>
/// クライアント領域の余白 (対話コントロールが占めていない部分) をタイトルバーと同様に扱い、
/// 掴んでのウィンドウ移動、ダブルクリックでの最大化・復元、スナップ操作を可能にします。
/// </summary>
/// <remarks>
/// <para>
/// <c>WM_NCHITTEST</c> に対して余白を <c>HTCAPTION</c> として報告することで、OS ネイティブの
/// キャプション操作へ委譲します。WPF のマウスイベントで <see cref="Window.DragMove"/> を呼ぶ方式と
/// 異なり、入力の取りこぼしがなく、スナップなどのシェル連携も自動で機能します。
/// </para>
/// <para>
/// 余白と判定するには、その位置にヒットテスト可能な背景 (透明ブラシで可) が必要です。
/// また、独自にマウス操作を実装したコントロール (キャンバスなど) は対話コントロールとして
/// 検出できないため、そのようなコントロールを持つウィンドウでの使用は適しません。
/// </para>
/// <example>
/// XAML での使用方法:
/// <code>
/// &lt;b:Interaction.Behaviors>
///     &lt;metro:WindowDragMoveBehavior />
/// &lt;/b:Interaction.Behaviors>
/// </code>
/// または <c>WindowFeatures.IsClientAreaDraggable</c> 添付プロパティを通じて使用します。
/// </example>
/// </remarks>
public class WindowDragMoveBehavior : Behavior<Window>
{
    private const int WM_NCHITTEST = 0x0084;
    private const int HTCAPTION = 2;

    /// <summary>
    /// ウィンドウ端のリサイズ境界とみなす幅 (DIP)。この範囲はキャプションとして扱わず、
    /// 既定のヒットテスト (リサイズ) に委ねます。
    /// </summary>
    private const double _resizeBorderThickness = 8;

    private HwndSource? _source;

    protected override void OnAttached()
    {
        base.OnAttached();
        this.AssociatedObject.SourceInitialized += this.HandleSourceInitialized;
        this.TryAddHook();
    }

    protected override void OnDetaching()
    {
        this.AssociatedObject.SourceInitialized -= this.HandleSourceInitialized;
        this._source?.RemoveHook(this.HandleWindowMessage);
        this._source = null;
        base.OnDetaching();
    }

    private void HandleSourceInitialized(object? sender, EventArgs e)
        => this.TryAddHook();

    private void TryAddHook()
    {
        if (this._source != null) return;
        if (PresentationSource.FromVisual(this.AssociatedObject) is not HwndSource source) return;

        this._source = source;
        source.AddHook(this.HandleWindowMessage);
    }

    private IntPtr HandleWindowMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (handled || msg != WM_NCHITTEST) return IntPtr.Zero;

        var window = this.AssociatedObject;
        if (window == null) return IntPtr.Zero;

        // 座標は下位 16 ビットが X、上位 16 ビットが Y の符号付き値 (マルチモニターで負になりうる)。
        var position = unchecked((int)(long)lParam);
        var screenPoint = new Point((short)(position & 0xFFFF), (short)((position >> 16) & 0xFFFF));

        Point clientPoint;
        try
        {
            clientPoint = window.PointFromScreen(screenPoint);
        }
        catch (InvalidOperationException)
        {
            return IntPtr.Zero;
        }

        // ウィンドウ端はリサイズに使うため、既定の処理 (WindowChrome) に委ねる。
        if (window.WindowState == WindowState.Normal
            && window.ResizeMode is ResizeMode.CanResize or ResizeMode.CanResizeWithGrip
            && (clientPoint.X < _resizeBorderThickness
                || clientPoint.Y < _resizeBorderThickness
                || clientPoint.X > window.ActualWidth - _resizeBorderThickness
                || clientPoint.Y > window.ActualHeight - _resizeBorderThickness))
        {
            return IntPtr.Zero;
        }

        if (window.InputHitTest(clientPoint) is not DependencyObject hit) return IntPtr.Zero;
        if (ContainsInteractiveElement(hit, window)) return IntPtr.Zero;

        handled = true;
        return new IntPtr(HTCAPTION);
    }

    /// <summary>
    /// ヒットした要素からウィンドウまでの経路に、マウス操作を受け付けるコントロールが
    /// 含まれるかどうかを判定します。含まれる場合はキャプションとして扱いません。
    /// </summary>
    private static bool ContainsInteractiveElement(DependencyObject hit, Window window)
    {
        for (var element = hit; element != null && ReferenceEquals(element, window) == false; element = GetParent(element))
        {
            if (element is ButtonBase
                or TextBoxBase
                or PasswordBox
                or RangeBase
                or Selector
                or ScrollViewer
                or Thumb
                or MenuBase
                or MenuItem
                or ToolTip)
            {
                return true;
            }
        }

        return false;
    }

    private static DependencyObject? GetParent(DependencyObject element)
        => element is Visual or Visual3D
            ? VisualTreeHelper.GetParent(element)
            : LogicalTreeHelper.GetParent(element);
}
