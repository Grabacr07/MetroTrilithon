namespace Amethystra.UI.Interactivity;

/// <summary>
/// <see cref="ConfirmMessage"/> が伝える確認操作の意図を表します。
/// プライマリ ボタンの見た目のヒントとしてビヘイビアに渡されます。
/// </summary>
public enum ConfirmIntent
{
    /// <summary>
    /// 通常の確認 (Primary 系の見た目)。
    /// </summary>
    Default,

    /// <summary>
    /// 破棄・削除など、取り消しが効きにくい操作 (Danger 系の見た目)。
    /// </summary>
    Destructive,
}
