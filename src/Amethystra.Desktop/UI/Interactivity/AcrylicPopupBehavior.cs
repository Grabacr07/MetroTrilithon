using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using Amethystra.UI.Interop;
using Microsoft.Xaml.Behaviors;

namespace Amethystra.UI.Interactivity;

/// <summary>
/// 標準 <see cref="Popup"/> に Acrylic blur を適用します。
/// </summary>
/// <remarks>
/// <para>
/// アタッチ先の <see cref="Popup"/> が開かれたタイミングで、<see cref="Popup.Child"/> をホストする HWND に対して
/// <see cref="AcrylicWindowEffect"/> 経由で Acrylic を適用します。tint カラーは <c>Wpf.Ui.Appearance.ApplicationThemeManager</c>
/// の現在テーマに追従します。
/// </para>
/// <para>
/// 適用には <see cref="Popup.AllowsTransparency"/> = <see langword="true"/> が必要です。
/// </para>
/// <para>
/// 親ウィンドウのアクティブ表示を維持したまま Acrylic を有効にするため、新 API (<c>DWMWA_SYSTEMBACKDROP_TYPE</c>) ではなく旧未公開 API
/// (<c>SetWindowCompositionAttribute</c> + <c>ACCENT_ENABLE_ACRYLICBLURBEHIND</c>) を使用します。
/// </para>
/// </remarks>
public class AcrylicPopupBehavior : Behavior<Popup>
{
    /// <inheritdoc />
    protected override void OnAttached()
    {
        base.OnAttached();
        this.AssociatedObject.Opened += this.OnOpened;
    }

    /// <inheritdoc />
    protected override void OnDetaching()
    {
        this.AssociatedObject.Opened -= this.OnOpened;
        base.OnDetaching();
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        if (this.AssociatedObject.Child is not Visual child) return;
        if (PresentationSource.FromVisual(child) is not HwndSource source) return;

        if (source.CompositionTarget is { } compositionTarget)
        {
            compositionTarget.BackgroundColor = Colors.Transparent;
        }

        AcrylicWindowEffect.Apply(source.Handle);
    }
}
