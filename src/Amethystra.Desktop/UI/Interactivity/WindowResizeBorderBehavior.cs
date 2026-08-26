using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Shell;
using Microsoft.Xaml.Behaviors;

namespace Amethystra.UI.Interactivity;

/// <summary>
/// ウィンドウのリサイズ境界 (<see cref="WindowChrome.ResizeBorderThickness"/>) を、
/// Windows 標準のウィンドウと同等の幅に拡張します。
/// </summary>
/// <remarks>
/// <para>
/// WPF-UI の <c>FluentWindow</c> は <see cref="WindowChrome.ResizeBorderThickness"/> を 4px 固定で設定するため、
/// リサイズ境界が Windows 標準のウィンドウ (SM_CXSIZEFRAME + SM_CXPADDEDBORDER、既定で 8px 相当) より狭くなります。
/// この behavior は <see cref="WindowChrome"/> 添付プロパティの変更を監視し、リサイズ境界をシステム標準の幅へ
/// 上書きします。<c>FluentWindow</c> は <c>WindowBackdropType</c> の変更時に <see cref="WindowChrome"/> を
/// 再生成して差し替えるため、初回適用後も監視を継続します。
/// </para>
/// <example>
/// XAML での使用方法:
/// <code>
/// &lt;b:Interaction.Behaviors>
///     &lt;metro:WindowResizeBorderBehavior />
/// &lt;/b:Interaction.Behaviors>
/// </code>
/// または <c>WindowFeatures.ExtendsResizeBorder</c> 添付プロパティを通じて使用します。
/// </example>
/// </remarks>
public class WindowResizeBorderBehavior : Behavior<Window>
{
    /// <summary>
    /// SM_CXPADDEDBORDER 相当のパディング幅 (DIP)。<see cref="SystemParameters"/> からは取得できないため、
    /// 既定値の 4px を用います。
    /// </summary>
    private const double _paddedBorderThickness = 4;

    private static readonly DependencyPropertyDescriptor _windowChromeDescriptor
        = DependencyPropertyDescriptor.FromProperty(WindowChrome.WindowChromeProperty, typeof(Window));

    protected override void OnAttached()
    {
        base.OnAttached();

        // DependencyPropertyDescriptor.AddValueChanged はグローバルテーブルで対象を強参照するため、
        // デタッチ時に加え、ウィンドウが閉じられた時点でも購読を解除する
        _windowChromeDescriptor.AddValueChanged(this.AssociatedObject, this.HandleWindowChromeChanged);
        this.AssociatedObject.Closed += this.HandleWindowClosed;
        this.Apply();
    }

    protected override void OnDetaching()
    {
        this.AssociatedObject.Closed -= this.HandleWindowClosed;
        _windowChromeDescriptor.RemoveValueChanged(this.AssociatedObject, this.HandleWindowChromeChanged);
        base.OnDetaching();
    }

    private void HandleWindowChromeChanged(object? sender, EventArgs e)
        => this.Apply();

    private void HandleWindowClosed(object? sender, EventArgs e)
        => _windowChromeDescriptor.RemoveValueChanged(this.AssociatedObject, this.HandleWindowChromeChanged);

    private void Apply()
    {
        var window = this.AssociatedObject;
        if (window == null) return;

        var chrome = WindowChrome.GetWindowChrome(window);

        // リサイズ境界を持たない (ResizeMode.NoResize など) 場合は対象外
        if (chrome == null || chrome.ResizeBorderThickness.Top <= 0) return;

        var thickness = GetSystemResizeBorderThickness();
        if (chrome.ResizeBorderThickness == thickness) return;

        if (chrome.IsFrozen)
        {
            chrome = (WindowChrome)chrome.Clone();
            chrome.ResizeBorderThickness = thickness;
            WindowChrome.SetWindowChrome(window, chrome);
        }
        else
        {
            chrome.ResizeBorderThickness = thickness;
        }
    }

    private static Thickness GetSystemResizeBorderThickness()
    {
        var frame = SystemParameters.WindowResizeBorderThickness;
        return new Thickness(
            frame.Left + _paddedBorderThickness,
            frame.Top + _paddedBorderThickness,
            frame.Right + _paddedBorderThickness,
            frame.Bottom + _paddedBorderThickness);
    }
}
