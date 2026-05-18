using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using Amethystra.UI.Interop;
using Microsoft.Xaml.Behaviors;
using Wpf.Ui.Appearance;

namespace Amethystra.UI.Interactivity;

/// <summary>
/// <see cref="ContextMenu"/> に Acrylic blur を適用します。
/// </summary>
/// <remarks>
/// <para>
/// アタッチ先の <see cref="ContextMenu"/> が開かれたタイミングで、ホスト HWND に対して
/// <see cref="AcrylicWindowEffect"/> 経由で Acrylic を適用します。tint カラーは <see cref="ApplicationThemeManager"/>
/// の現在テーマに追従します。
/// </para>
/// <para>
/// 親ウィンドウのアクティブ表示を維持したまま Acrylic を有効にするため、新 API (<c>DWMWA_SYSTEMBACKDROP_TYPE</c>) ではなく旧未公開 API
/// (<c>SetWindowCompositionAttribute</c> + <c>ACCENT_ENABLE_ACRYLICBLURBEHIND</c>) を使用します。
/// </para>
/// <para>
/// XAML 側では <c>Style.ContextMenu.Acrylic</c> および <c>Style.MenuItem.Acrylic</c> を併用してください。
/// </para>
/// </remarks>
internal class AcrylicContextMenuBehavior : Behavior<ContextMenu>
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

    private void OnOpened(object sender, RoutedEventArgs e)
    {
        if (PresentationSource.FromVisual(this.AssociatedObject) is not HwndSource source) return;

        // HwndTarget の BackgroundColor を Transparent にしないと、WPF レンダラが
        // 不透明の背景を描画してしまい Acrylic が見えなくなる。
        if (source.CompositionTarget is { } compositionTarget)
        {
            compositionTarget.BackgroundColor = Colors.Transparent;
        }

        AcrylicWindowEffect.Apply(source.Handle);
    }
}
