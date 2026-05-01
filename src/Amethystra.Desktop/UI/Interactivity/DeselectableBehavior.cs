using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Xaml.Behaviors;

namespace Amethystra.UI.Interactivity;

public class DeselectableBehavior : Behavior<ListBox>
{
    #region IsSelectionEnabled dependency property

    public static readonly DependencyProperty IsSelectionEnabledProperty
        = DependencyProperty.Register(
            nameof(IsSelectionEnabled),
            typeof(bool),
            typeof(DeselectableBehavior),
            new FrameworkPropertyMetadata(BooleanBoxes.TrueBox, OnIsSelectionEnabledChanged));

    public bool IsSelectionEnabled
    {
        get => (bool)this.GetValue(IsSelectionEnabledProperty);
        set => this.SetValue(IsSelectionEnabledProperty, BooleanBoxes.Box(value));
    }

    private static void OnIsSelectionEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DeselectableBehavior { AssociatedObject: { } listBox } && (bool)e.NewValue == false)
        {
            listBox.SelectedItem = null;
        }
    }

    #endregion

    protected override void OnAttached()
    {
        base.OnAttached();
        this.AssociatedObject.PreviewMouseLeftButtonDown += this.OnPreviewMouseLeftButtonDown;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        this.AssociatedObject.PreviewMouseLeftButtonDown -= this.OnPreviewMouseLeftButtonDown;
    }

    private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (this.IsSelectionEnabled == false) return;
        if (e.OriginalSource is not DependencyObject originalSource) return;
        if (ItemsControl.ContainerFromElement(this.AssociatedObject, originalSource) is not ListBoxItem item) return;

        // アイテム内のボタン類へのクリックは選択変更の対象外とする。
        // ここで e.Handled = true にすると ButtonBase の Click が発火しなくなるため除外する。
        if (IsDescendantOf<ButtonBase>(originalSource, item)) return;

        if (item.IsSelected)
        {
            // 選択済みアイテムの再クリックで選択を解除する。
            // イベントを消費することで、アイテム内コントロールによる再選択を防ぐ。
            this.AssociatedObject.SelectedItem = null;
            e.Handled = true;
        }
        else
        {
            // テキスト選択等を行うコントロールがトンネルイベントを内部処理して
            // e.Handled = true にする場合、バブル側の MouseLeftButtonDown が発生せず
            // ListBoxItem の標準選択ロジックが動作しない。
            // そのためトンネル段階で明示的に選択する。
            item.IsSelected = true;
        }
    }

    private static bool IsDescendantOf<T>(DependencyObject source, DependencyObject boundary)
        where T : DependencyObject
    {
        var current = source;
        while (current != null && ReferenceEquals(current, boundary) == false)
        {
            if (current is T) return true;
            current = VisualTreeHelper.GetParent(current);
        }

        return false;
    }
}
