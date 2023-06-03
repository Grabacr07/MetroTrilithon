namespace MetroTrilithon.UI.Interop;

internal interface IWindowProcedure
{
    IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled);
}
