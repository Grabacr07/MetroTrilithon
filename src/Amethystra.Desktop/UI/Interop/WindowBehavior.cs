using System.Windows;
using System.Windows.Interop;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Amethystra.UI.Interop;

public static class WindowBehavior
{
    #region CanMaximize attached property

    public static readonly DependencyProperty CanMaximizeProperty
        = DependencyProperty.RegisterAttached(
            nameof(CanMaximizeProperty).GetPropertyName(),
            typeof(bool),
            typeof(WindowBehavior),
            new PropertyMetadata(BooleanBoxes.TrueBox, HandleCanMaximizePropertyChanged));

    public static void SetCanMaximize(Window window, bool value)
        => window.SetValue(CanMaximizeProperty, BooleanBoxes.Box(value));

    public static bool GetCanMaximize(Window window)
        => (bool)window.GetValue(CanMaximizeProperty);

    private static void HandleCanMaximizePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Window window) return;

        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd != IntPtr.Zero)
        {
            ApplyCanMaximize(hwnd, (bool)e.NewValue);
        }
        else
        {
            window.SourceInitialized += OnSourceInitialized;
        }
    }

    private static void OnSourceInitialized(object? sender, EventArgs e)
    {
        if (sender is not Window window) return;
        window.SourceInitialized -= OnSourceInitialized;

        var hwnd = new WindowInteropHelper(window).Handle;
        ApplyCanMaximize(hwnd, GetCanMaximize(window));
    }

    internal static void ApplyCanMaximize(IntPtr hwnd, bool canMaximize)
    {
        var h = new HWND(hwnd);
        var style = (WINDOW_STYLE)(uint)PInvoke.GetWindowLong(h, WINDOW_LONG_PTR_INDEX.GWL_STYLE);

        if (canMaximize)
        {
            style |= WINDOW_STYLE.WS_MAXIMIZEBOX;
        }
        else
        {
            style &= ~WINDOW_STYLE.WS_MAXIMIZEBOX;
        }

        _ = PInvoke.SetWindowLong(h, WINDOW_LONG_PTR_INDEX.GWL_STYLE, (int)(uint)style);
    }

    #endregion
}
