using System.Threading.Tasks;

namespace Amethystra.UI.Interactivity;

/// <summary>
/// View 側へ確認ダイアログの表示を要求し、結果を受け取るためのメッセージを表します。
/// </summary>
public sealed class ConfirmMessage
{
    private readonly TaskCompletionSource<bool> _reply = new();

    public required string Title { get; init; }

    public required string Content { get; init; }

    public string PrimaryButtonText { get; init; } = "OK";

    public string CloseButtonText { get; init; } = "キャンセル";

    /// <summary>
    /// ユーザーの応答を待機するタスクを取得します。
    /// <see langword="true"/> の場合、プライマリボタンが選択されたことを示します。
    /// </summary>
    public Task<bool> ReplyTask => this._reply.Task;

    internal void SetReply(bool result) => this._reply.TrySetResult(result);
}
