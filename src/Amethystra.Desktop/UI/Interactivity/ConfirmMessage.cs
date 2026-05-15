using System.Threading.Tasks;
using System.Windows;
using R3;
using Wpf.Ui.Controls;

namespace Amethystra.UI.Interactivity;

/// <summary>
/// UI へ確認ダイアログの表示を要求し、結果を受け取るためのメッセージを表します。
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Content"/> には <see cref="string"/> のような単純な値だけでなく、任意のデータ オブジェクトを渡せます。
/// 渡されたオブジェクトは <see cref="ContentDialog.Content"/> に設定されるため、対応する
/// <see cref="DataTemplate"/> が解決できれば自動的に UI として展開されます。
/// </para>
/// <para>
/// プライマリ ボタンの有効/無効を入力検証などに応じて動的に切り替えたい場合は <see cref="CanCommit"/> を指定します。
/// </para>
/// </remarks>
public sealed class ConfirmMessage
{
    private readonly TaskCompletionSource<bool> _reply = new();

    public required string Title { get; init; }

    /// <summary>
    /// ダイアログ本体に表示する内容を取得または設定します。
    /// </summary>
    /// <remarks>
    /// <see cref="string"/> をはじめとする任意のオブジェクトを指定できます。
    /// <see cref="DataTemplate"/> が登録されている場合はそのテンプレートに従ってビジュアル ツリーが構築されます。
    /// </remarks>
    public required object Content { get; init; }

    public string PrimaryButtonText { get; init; }
        = "OK";

    public string CloseButtonText { get; init; }
        = "キャンセル";

    /// <summary>
    /// 確認操作の意図を取得します。<see cref="ConfirmIntent.Destructive"/> の場合、
    /// ビヘイビアはプライマリ ボタンを Danger 系の見た目で表示します。
    /// </summary>
    public ConfirmIntent Intent { get; init; }
        = ConfirmIntent.Default;

    /// <summary>
    /// プライマリ ボタンの有効/無効を制御する <see cref="Observable{T}"/> を取得または設定します。
    /// </summary>
    /// <remarks>
    /// 値を <see langword="null"/> にした場合、プライマリ ボタンは常に有効です。
    /// </remarks>
    public Observable<bool>? CanCommit { get; init; }

    /// <summary>
    /// ユーザーの応答を待機するタスクを取得します。
    /// <see langword="true"/> の場合、プライマリボタンが選択されたことを示します。
    /// </summary>
    public Task<bool> ReplyTask
        => this._reply.Task;

    internal void SetReply(bool result)
        => this._reply.TrySetResult(result);
}
