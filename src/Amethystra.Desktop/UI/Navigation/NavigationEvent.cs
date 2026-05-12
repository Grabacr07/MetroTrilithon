namespace Amethystra.UI.Navigation;

/// <summary>
/// <see cref="NavigationHost"/> が発行するナビゲーション操作のイベントを表します。
/// </summary>
public abstract record NavigationEvent
{
    /// <summary>
    /// 新しい ViewModel がスタックの末尾へ積まれたことを示すイベントを表します。
    /// </summary>
    public sealed record Pushed(object ViewModel) : NavigationEvent;

    /// <summary>
    /// スタックの末尾から ViewModel が取り除かれたことを示すイベントを表します。
    /// </summary>
    /// <param name="Count">取り除かれた個数 (1 以上)。</param>
    public sealed record Popped(int Count) : NavigationEvent;
}
