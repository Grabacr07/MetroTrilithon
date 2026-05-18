using System;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.UI.Controls;
using Amethystra.Win32;
using Wpf.Ui.Appearance;

namespace Amethystra.UI.Interop;

/// <summary>
/// ウィンドウへ Acrylic 効果を適用するためのユーティリティを提供します。
/// </summary>
/// <remarks>
/// <para>
/// 新 API (<c>DWMWA_SYSTEMBACKDROP_TYPE</c>) は <c>WS_EX_NOACTIVATE</c> 付きのウィンドウでは効果が現れません。
/// <see cref="Popup"/> や <see cref="ContextMenu"/> の HWND は親ウィンドウのアクティブ表示を維持するために <c>WS_EX_NOACTIVATE</c>
/// が付与されており、新 API での Acrylic 適用とは両立しません。
/// </para>
/// <para>
/// 代わりに、旧未公開 API (<see cref="WindowComposition.SetAccentPolicy"/> +
/// <see cref="AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND"/>) を使用します。
/// </para>
/// </remarks>
public static class AcrylicWindowEffect
{
    /// <summary>
    /// 指定されたウィンドウに DWM 角丸と Acrylic blur を適用します。
    /// </summary>
    /// <param name="hwnd">適用対象のウィンドウ ハンドル。</param>
    /// <remarks>
    /// tint カラーは <see cref="ApplicationThemeManager"/> の現在テーマに追従し、
    /// Dark 時は黒系、Light 時は白系に切り替えます。
    /// </remarks>
    public static unsafe void Apply(IntPtr hwnd)
    {
        var target = new HWND(hwnd);
        var cornerPreference = DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;
        PInvoke.DwmSetWindowAttribute(
            target,
            DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE,
            &cornerPreference,
            sizeof(DWM_WINDOW_CORNER_PREFERENCE));

        var margins = new MARGINS()
        {
            cxLeftWidth = 0,
            cxRightWidth = 0,
            cyTopHeight = 0,
            cyBottomHeight = 0,
        };
        PInvoke.DwmExtendFrameIntoClientArea(target, &margins);

        // tint カラーは下位 24bit = RGB、上位 8bit = alpha。
        // Dark テーマでは黒系 tint、Light テーマでは白系 tint にして、popup の見た目を
        // アプリ全体のテーマに合わせる。
        var isDark = IsDarkTheme();
        var tintRgb = isDark ? 0x00000000u : 0x00FFFFFFu;
        var policy = new AccentPolicy()
        {
            AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND,
            AccentFlags = AccentFlags.None,
            GradientColor = (125u << 24) | tintRgb,
            AnimationId = 0,
        };
        WindowComposition.SetAccentPolicy(hwnd, policy);
    }

    private static bool IsDarkTheme()
        => ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark;
}
