using System.Windows;
using System.Windows.Controls.Primitives;
using Amethystra.UI.Interactivity;
using Microsoft.Xaml.Behaviors;

namespace Amethystra.UI.Interop;

/// <summary>
/// <see cref="Popup"/> に視覚効果を適用する添付プロパティを提供します。
/// </summary>
public static class PopupEffect
{
    #region Backdrop attached property

    /// <summary>
    /// Backdrop 添付プロパティを識別します。
    /// </summary>
    public static readonly DependencyProperty BackdropProperty
        = DependencyProperty.RegisterAttached(
            nameof(BackdropProperty).GetPropertyName(),
            typeof(WindowBackdrop),
            typeof(PopupEffect),
            new PropertyMetadata(WindowBackdrop.None, HandleBackdropPropertyChanged));

    /// <summary>
    /// 指定された <see cref="Popup"/> に設定されている <see cref="WindowBackdrop"/> を取得します。
    /// </summary>
    /// <param name="popup">対象の <see cref="Popup"/>。</param>
    /// <returns>適用中の <see cref="WindowBackdrop"/>。</returns>
    public static WindowBackdrop GetBackdrop(Popup popup)
        => (WindowBackdrop)popup.GetValue(BackdropProperty);

    /// <summary>
    /// 指定された <see cref="Popup"/> に <see cref="WindowBackdrop"/> を設定します。
    /// </summary>
    /// <param name="popup">対象の <see cref="Popup"/>。</param>
    /// <param name="value">適用する <see cref="WindowBackdrop"/>。</param>
    /// <remarks>
    /// <see cref="WindowBackdrop.Acrylic"/> を指定する場合は <see cref="Popup.AllowsTransparency"/> = <see langword="true"/> が必要です。
    /// </remarks>
    public static void SetBackdrop(Popup popup, WindowBackdrop value)
        => popup.SetValue(BackdropProperty, value);

    private static void HandleBackdropPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Popup popup) return;

        var behaviors = Interaction.GetBehaviors(popup);

        // 既存の AcrylicPopupBehavior を取り除いて状態をリセットする。
        for (var i = behaviors.Count - 1; i >= 0; i--)
        {
            if (behaviors[i] is AcrylicPopupBehavior)
            {
                behaviors.RemoveAt(i);
            }
        }

        if ((WindowBackdrop)e.NewValue == WindowBackdrop.Acrylic)
        {
            behaviors.Add(new AcrylicPopupBehavior());
        }
    }

    #endregion
}
