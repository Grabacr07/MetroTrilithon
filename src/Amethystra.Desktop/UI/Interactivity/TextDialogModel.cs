using System.Windows;

namespace Amethystra.UI.Interactivity;

/// <summary>
/// 表示専用テキストを 1 ブロックで掲示するダイアログ用のデータ オブジェクトです。
/// </summary>
/// <remarks>
/// <see cref="ConfirmMessage.Content"/> に渡すと、既定の <see cref="DataTemplate"/> によって <see cref="TextDialogContent"/> として展開されます。
/// 既定の UI 表現は固定幅で表示されるため、入力内容に依存してダイアログ サイズが変動しません。
/// </remarks>
public sealed class TextDialogModel
{
    /// <summary>
    /// ダイアログ本体に表示するテキストを取得します。
    /// </summary>
    public required string Text { get; init; }
}
