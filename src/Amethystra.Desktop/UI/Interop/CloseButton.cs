using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Windows.Win32;

namespace Amethystra.UI.Interop;

public class CloseButton : TitleBarButton
{
    static CloseButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CloseButton),
            new FrameworkPropertyMetadata(typeof(CloseButton)));
    }

    protected override NCHITTEST HitTestReturnValue
        => NCHITTEST.HTCLOSE;

    protected override void OnClick()
    {
        base.OnClick();
        this.Window.Close();
    }
}
