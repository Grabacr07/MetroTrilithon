using System.Threading.Tasks;
using System.Windows;
using Amethystra.UI.Interactivity;
using R3;

namespace Amethystra.Mvvm;

public abstract class WindowViewModelBase : ViewModelBase
{
    private readonly BindableReactiveProperty<string> _title;
    private readonly BindableReactiveProperty<bool> _isClosed;
    private readonly Subject<Unit> _closeRequested = new();
    private readonly Subject<Unit> _activateRequested = new();

    public IBindableReactiveProperty<string> Title
        => this._title;

    public IReadOnlyBindableReactiveProperty<bool> IsClosed
        => this._isClosed;

    public Observable<Unit> CloseRequested
        => this._closeRequested;

    public Observable<Unit> ActivateRequested
        => this._activateRequested;

    protected WindowViewModelBase()
    {
        this._title = new BindableReactiveProperty<string>().AddTo(this);
        this._isClosed = new BindableReactiveProperty<bool>().AddTo(this);
    }

    /// <summary>
    /// <see cref="WindowLifecycleBehavior"/> から呼び出されます。
    /// <see cref="Window.ContentRendered"/> イベント発生時の初期化処理を実行します。
    /// </summary>
    internal ValueTask OnContentRenderedAsync(CancellationToken ct)
        => this.InitializeAsync(ct);

    /// <summary>
    /// <see cref="WindowLifecycleBehavior"/> から呼び出されます。
    /// <see cref="Window.Closing"/> イベント発生時にウィンドウを閉じてよいか確認します。
    /// </summary>
    internal ValueTask<bool> OnClosingAsync(CancellationToken ct)
        => this.CanCloseAsync(ct);

    /// <summary>
    /// <see cref="WindowLifecycleBehavior"/> から呼び出されます。
    /// <see cref="Window.Closed"/> イベント発生時のクリーンアップを実行します。
    /// </summary>
    internal void OnClosed()
    {
        this._isClosed.Value = true;
        this.Dispose();
    }

    /// <summary>
    /// 派生クラスでオーバーライドされると、<see cref="Window.ContentRendered"/> 後の非同期初期化処理を実行します。
    /// </summary>
    protected virtual ValueTask InitializeAsync(CancellationToken ct)
        => ValueTask.CompletedTask;

    /// <summary>
    /// 派生クラスでオーバーライドされると、ウィンドウを閉じてよいかどうかを非同期に判断します。
    /// </summary>
    /// <returns>閉じてよい場合は <see langword="true"/>。</returns>
    protected virtual ValueTask<bool> CanCloseAsync(CancellationToken ct)
        => new(true);

    public virtual void Activate()
        => this._activateRequested.OnNext(Unit.Default);

    public virtual void Close()
    {
        if (this._isClosed.Value) return;
        this._closeRequested.OnNext(Unit.Default);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this._closeRequested.Dispose();
            this._activateRequested.Dispose();
        }

        base.Dispose(disposing);
    }
}
