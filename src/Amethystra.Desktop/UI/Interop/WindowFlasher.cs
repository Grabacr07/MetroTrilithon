using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Amethystra.UI.Interop;

/// <summary>
/// Specifies which parts of the window are flashed by <see cref="WindowFlasher"/>.
/// </summary>
[Flags]
public enum WindowFlashTarget
{
    /// <summary>Flash the window caption.</summary>
    Caption = 0x1,

    /// <summary>Flash the taskbar button.</summary>
    Tray = 0x2,

    /// <summary>Flash both the window caption and the taskbar button.</summary>
    All = Caption | Tray,
}

/// <summary>
/// Specifies how long <see cref="WindowFlasher"/> continues flashing.
/// </summary>
public enum WindowFlashMode
{
    /// <summary>
    /// Flash exactly <see cref="WindowFlashOptions.Count"/> times and then stop.
    /// </summary>
    Count,

    /// <summary>
    /// Flash continuously until <see cref="WindowFlasher.Stop"/> is called.
    /// </summary>
    Continuous,

    /// <summary>
    /// Flash continuously until the window comes to the foreground or
    /// <see cref="WindowFlasher.Stop"/> is called.
    /// </summary>
    UntilForeground,
}

/// <summary>
/// Options that control the flash behavior of <see cref="WindowFlasher.Flash()"/>.
/// </summary>
public readonly record struct WindowFlashOptions()
{
    /// <summary>The parts of the window to flash. Defaults to <see cref="WindowFlashTarget.All"/>.</summary>
    public WindowFlashTarget Target { get; init; } = WindowFlashTarget.All;

    /// <summary>The stopping condition for the flash. Defaults to <see cref="WindowFlashMode.UntilForeground"/>.</summary>
    public WindowFlashMode Mode { get; init; } = WindowFlashMode.UntilForeground;

    /// <summary>
    /// Number of times to flash when <see cref="Mode"/> is <see cref="WindowFlashMode.Count"/>.
    /// Ignored for other modes.
    /// </summary>
    public uint Count { get; init; } = 3;

    /// <summary>
    /// Interval between flashes. <see cref="TimeSpan.Zero"/> uses the system default
    /// (typically the caret blink rate).
    /// </summary>
    public TimeSpan Interval { get; init; } = TimeSpan.Zero;

    /// <summary>
    /// Flash both caption and taskbar button until the window is brought to the foreground.
    /// </summary>
    public static WindowFlashOptions UntilForeground
        => new() { Mode = WindowFlashMode.UntilForeground };

    /// <summary>
    /// Flash both caption and taskbar button three times.
    /// </summary>
    public static WindowFlashOptions ThreeTimes
        => new() { Mode = WindowFlashMode.Count, Count = 3 };
}

/// <summary>
/// Wraps <c>FlashWindowEx</c> to draw the user's attention to a window via taskbar / caption
/// blinking without stealing focus.
/// </summary>
public sealed class WindowFlasher
{
    private readonly HWND _hwnd;

    /// <summary>
    /// Creates a flasher that targets the supplied WPF window. The window must have been shown
    /// at least once so it has a backing HWND.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="window"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The window does not yet have a handle.</exception>
    public WindowFlasher(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero)
        {
            throw new InvalidOperationException("The window does not yet have a handle.");
        }

        this._hwnd = (HWND)hwnd;
    }

    /// <summary>
    /// Creates a flasher that targets the supplied native window handle.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="hwnd"/> is <see cref="IntPtr.Zero"/>.</exception>
    public WindowFlasher(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
        {
            throw new ArgumentException("Window handle is invalid.", nameof(hwnd));
        }

        this._hwnd = (HWND)hwnd;
    }

    /// <summary>
    /// Starts flashing the window using the default <see cref="WindowFlashOptions"/> (flash both
    /// caption and taskbar button until the window is brought to the foreground).
    /// </summary>
    public void Flash()
        => this.Flash(new WindowFlashOptions());

    /// <summary>
    /// Starts flashing the window using the supplied options.
    /// </summary>
    /// <remarks>
    /// The return value of <c>FlashWindowEx</c> reports the previous flash state and is
    /// intentionally ignored.
    /// </remarks>
    public void Flash(WindowFlashOptions options)
    {
        var flags = (FLASHWINFO_FLAGS)options.Target;
        var count = options.Count;

        switch (options.Mode)
        {
            case WindowFlashMode.Continuous:
                flags |= FLASHWINFO_FLAGS.FLASHW_TIMER;
                count = 0;
                break;
            case WindowFlashMode.UntilForeground:
                flags |= FLASHWINFO_FLAGS.FLASHW_TIMERNOFG;
                count = 0;
                break;
            case WindowFlashMode.Count:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(options), options.Mode, "Unknown flash mode.");
        }

        var info = new FLASHWINFO()
        {
            cbSize = (uint)Marshal.SizeOf<FLASHWINFO>(),
            hwnd = this._hwnd,
            dwFlags = flags,
            uCount = count,
            dwTimeout = (uint)Math.Clamp(options.Interval.TotalMilliseconds, 0, uint.MaxValue),
        };

        PInvoke.FlashWindowEx(in info);
    }

    /// <summary>
    /// Stops any in-progress flashing of the window.
    /// </summary>
    public void Stop()
    {
        var info = new FLASHWINFO()
        {
            cbSize = (uint)Marshal.SizeOf<FLASHWINFO>(),
            hwnd = this._hwnd,
            dwFlags = FLASHWINFO_FLAGS.FLASHW_STOP,
            uCount = 0,
            dwTimeout = 0,
        };

        PInvoke.FlashWindowEx(in info);
    }
}
