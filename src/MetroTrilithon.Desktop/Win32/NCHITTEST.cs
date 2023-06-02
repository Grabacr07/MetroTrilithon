// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming
// ReSharper disable IdentifierTypo

namespace Windows.Win32;

public enum NCHITTEST
{
    HTNOWHERE = 0,
    HTCLIENT = 1,
    HTCAPTION = 2,
    HTSYSMENU = 3,
    HTGROWBOX = 4,
    HTSIZE = HTGROWBOX,
    HTMENU = 5,
    HTHSCROLL = 6,
    HTVSCROLL = 7,
    HTMINBUTTON = 8,
    HTREDUCE = HTMINBUTTON,
    HTMAXBUTTON = 9,
    HTZOOM = HTMAXBUTTON,
    HTLEFT = 10,
    HTSIZEFIRST = HTLEFT,
    HTRIGHT = 11,
    HTTOP = 12,
    HTTOPLEFT = 13,
    HTTOPRIGHT = 14,
    HTBOTTOM = 15,
    HTBOTTOMLEFT = 16,
    HTBOTTOMRIGHT = 17,
    HTSIZELAST = HTBOTTOMRIGHT,
    HTBORDER = 18,
    HTOBJECT = 19,
    HTCLOSE = 20,
    HTHELP = 21,
}
