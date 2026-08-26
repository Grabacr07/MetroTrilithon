using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Amethystra.UI.Interop;

internal static class InteropHelper
{
    public static IDisposable RegisterWndProc(IWindowProcedure element)
        => new WndProcListener(element.GetWindow(), element);

    private class WndProcListener : IDisposable
    {
        private readonly IWindowProcedure _element;
        private readonly IntPtr _hwnd;

        public WndProcListener(Window window, IWindowProcedure element)
        {
            this._element = element;

            if (DesignFeatures.IsInDesignMode) return;

            this._hwnd = new WindowInteropHelper(window).EnsureHandle();
            var source = HwndSource.FromHwnd(this._hwnd) ?? throw CreateExceptionForMissingParentWindow();
            source.AddHook(this.HwndSourceHook);
        }

        public void Dispose()
        {
            if (DesignFeatures.IsInDesignMode || HwndSource.FromHwnd(this._hwnd) is not { } source) return;

            source.RemoveHook(this.HwndSourceHook);
        }

        private IntPtr HwndSourceHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
            => this._element.WndProc(hwnd, msg, wParam, lParam, ref handled);
    }

    public static Window GetWindow(this object element)
        => element switch
        {
            null => throw new ArgumentNullException(nameof(element)),
            Window window => window,
            Visual visual => Window.GetWindow(visual) ?? throw CreateExceptionForMissingParentWindow(),
            _ => throw new ArgumentException("The element is not a visual.", nameof(element)),
        };

    /// <remarks>
    /// Do not call it outside of WM_NCHITTEST, WM_NCLBUTTONUP, WM_NCLBUTTONDOWN messages.
    /// This method will be invoked very often and must be as simple as possible.
    /// </remarks>
    public static bool Contains(this UIElement element, IntPtr lParam)
    {
        if (lParam == IntPtr.Zero) return false;

        var mousePosScreen = GetScreenPoint(lParam);
        var bounds = new Rect(new Point(), element.RenderSize);
        var mousePosRelative = element.PointFromScreen(mousePosScreen);

        return bounds.Contains(mousePosRelative);
    }

    /// <summary>
    /// <c>WM_NCHITTEST</c> などの lParam に格納されたスクリーン座標を取得します。
    /// </summary>
    public static Point GetScreenPoint(IntPtr lParam)
    {
        // 下位 16 ビットが X、上位 16 ビットが Y の符号付き値 (マルチモニターで負になりうる)
        var value = unchecked((int)(long)lParam);
        return new Point((short)(value & 0xFFFF), (short)((value >> 16) & 0xFFFF));
    }

    public static Exception CreateExceptionForMissingParentWindow()
        => new InvalidOperationException("Failed to identify the parent window.");
}
