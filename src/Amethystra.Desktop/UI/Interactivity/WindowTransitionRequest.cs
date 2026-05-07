using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Amethystra.UI.Interactivity;

/// <summary>
/// View 側へウィンドウ遷移を要求し、結果を受け取るためのメッセージを表します。
/// </summary>
public sealed class WindowTransitionRequest(object viewModel, WindowTransitionMode mode = WindowTransitionMode.Show)
{
    private readonly TaskCompletionSource<bool?> _result = new();

    /// <summary>
    /// 表示する ViewModel を取得します。
    /// </summary>
    public object ViewModel { get; } = viewModel;

    /// <summary>
    /// ウィンドウの表示モードを取得します。
    /// </summary>
    public WindowTransitionMode Mode { get; } = mode;

    internal void Complete(bool? result) => this._result.TrySetResult(result);

    /// <summary>
    /// ウィンドウが閉じられるまで待機し、ダイアログの結果を返します。
    /// </summary>
    [MustUseReturnValue]
    public Task<bool?> WaitForResultAsync(CancellationToken cancellationToken = default)
        => this._result.Task.WaitAsync(cancellationToken);
}

/// <summary>
/// ウィンドウの表示モードを表します。
/// </summary>
public enum WindowTransitionMode
{
    /// <summary>
    /// モードレスウィンドウとして表示します。
    /// </summary>
    Show,

    /// <summary>
    /// モーダルダイアログとして表示します。
    /// </summary>
    ShowDialog,
}
