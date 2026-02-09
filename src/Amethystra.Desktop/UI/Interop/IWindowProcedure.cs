using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Amethystra.UI.Interop;

internal interface IWindowProcedure
{
    IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled);
}
