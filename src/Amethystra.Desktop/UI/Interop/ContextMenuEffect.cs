using System.Windows;
using System.Windows.Controls;
using Amethystra.UI.Interactivity;
using Microsoft.Xaml.Behaviors;

namespace Amethystra.UI.Interop;

/// <summary>
/// <see cref="ContextMenu"/> に視覚効果を適用する添付プロパティを提供します。
/// </summary>
public static class ContextMenuEffect
{
    #region Backdrop attached property

    /// <summary>
    /// Backdrop 添付プロパティを識別します。
    /// </summary>
    public static readonly DependencyProperty BackdropProperty
        = DependencyProperty.RegisterAttached(
            nameof(BackdropProperty).GetPropertyName(),
            typeof(WindowBackdrop),
            typeof(ContextMenuEffect),
            new PropertyMetadata(WindowBackdrop.None, HandleBackdropPropertyChanged));

    /// <summary>
    /// 指定された <see cref="ContextMenu"/> に設定されている <see cref="WindowBackdrop"/> を取得します。
    /// </summary>
    /// <param name="menu">対象の <see cref="ContextMenu"/>。</param>
    /// <returns>適用中の <see cref="WindowBackdrop"/>。</returns>
    public static WindowBackdrop GetBackdrop(ContextMenu menu)
        => (WindowBackdrop)menu.GetValue(BackdropProperty);

    /// <summary>
    /// 指定された <see cref="ContextMenu"/> に <see cref="WindowBackdrop"/> を設定します。
    /// </summary>
    /// <param name="menu">対象の <see cref="ContextMenu"/>。</param>
    /// <param name="value">適用する <see cref="WindowBackdrop"/>。</param>
    public static void SetBackdrop(ContextMenu menu, WindowBackdrop value)
        => menu.SetValue(BackdropProperty, value);

    private static void HandleBackdropPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ContextMenu menu) return;

        var behaviors = Interaction.GetBehaviors(menu);

        // 既存の AcrylicContextMenuBehavior を取り除いて状態をリセットする。
        for (var i = behaviors.Count - 1; i >= 0; i--)
        {
            if (behaviors[i] is AcrylicContextMenuBehavior)
            {
                behaviors.RemoveAt(i);
            }
        }

        if ((WindowBackdrop)e.NewValue == WindowBackdrop.Acrylic)
        {
            behaviors.Add(new AcrylicContextMenuBehavior());
        }
    }

    #endregion
}
