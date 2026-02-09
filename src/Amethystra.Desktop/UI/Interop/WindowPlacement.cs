using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Amethystra.UI.Interop;

[DebuggerDisplay("{State} - {Rect}")]
public class WindowPlacement
{
    public WindowState State { get; }

    public Rect Rect { get; }

    public WindowPlacement(WindowState state, Rect rect)
    {
        this.State = state;
        this.Rect = rect;
    }

    private WindowPlacement(WINDOWPLACEMENT placement)
    {
        this.State = placement.showCmd switch
        {
            SHOW_WINDOW_CMD.SW_SHOWMINIMIZED => WindowState.Minimized,
            SHOW_WINDOW_CMD.SW_SHOWMAXIMIZED => WindowState.Maximized,
            _ => WindowState.Normal
        };
        this.Rect = new Rect(
            placement.rcNormalPosition.left,
            placement.rcNormalPosition.top,
            placement.rcNormalPosition.right - placement.rcNormalPosition.left,
            placement.rcNormalPosition.bottom - placement.rcNormalPosition.top);
    }

    public void Apply(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        var placement = new WINDOWPLACEMENT()
        {
            length = (uint)Marshal.SizeOf(typeof(WINDOWPLACEMENT)),
            flags = 0,
            showCmd = this.State switch
            {
                WindowState.Minimized => SHOW_WINDOW_CMD.SW_MAXIMIZE,
                WindowState.Maximized => SHOW_WINDOW_CMD.SW_MAXIMIZE,
                _ => SHOW_WINDOW_CMD.SW_NORMAL,
            },
            rcNormalPosition = new RECT()
            {
                top = (int)this.Rect.Top,
                left = (int)this.Rect.Left,
                right = (int)this.Rect.Right,
                bottom = (int)this.Rect.Bottom,
            },
        };
        PInvoke.SetWindowPlacement(new HWND(hwnd), placement);
    }

    public static WindowPlacement Get(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        var placement = new WINDOWPLACEMENT();
        PInvoke.GetWindowPlacement(new HWND(hwnd), ref placement);

        return new WindowPlacement(placement);
    }
}
