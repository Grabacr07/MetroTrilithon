using System;
using System.ComponentModel;
using System.Windows;
using Amethystra.UI;
using R3;

namespace Amethystra.Mvvm;

public abstract class WindowBase : ViewModelBase
{
    public ReactiveProperty<string> Title { get; } = new();

    public ReactiveProperty<bool> CanClose { get; } = new();

    public ReactiveProperty<bool> IsClosed { get; } = new();

    /// <summary>
    /// <see cref="InitializeCore"/> メソッドが呼ばれたかどうか (通常、これはアタッチされたウィンドウの <see cref="Window.ContentRendered"/> イベントによって呼び出されます) を示す値を取得します。
    /// </summary>
    public bool IsInitialized { get; private set; }

    public bool DialogResult { get; protected set; }

    public WindowState WindowState { get; set; }

    public event Action? CloseRequested;

    public event Action? ActivateRequested;

    /// <summary>
    /// このメソッドは、アタッチされたウィンドウで <see cref="Window.ContentRendered"/> イベントが発生したときに呼び出されます。
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void Initialize()
    {
        if (this.IsClosed.Value) return;

        this.DialogResult = false;

        this.InitializeCore();
        this.IsInitialized = true;
    }

    /// <summary>
    /// 派生クラスでオーバーライドされると、アタッチされたウィンドウで <see cref="Window.ContentRendered"/> イベントが発生したときに呼び出される初期化処理を実行します。
    /// </summary>
    protected virtual void InitializeCore()
    {
    }

    /// <summary>
    /// このメソッドは、アタッチされたウィンドウで <see cref="Window.Closing"/> イベントがキャンセルされたときに呼び出されます。
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void CloseCanceledCallback()
    {
        this.CloseCanceledCallbackCore();
    }

    /// <summary>
    /// 派生クラスでオーバーライドされると、<see cref="Window.Closing"/> キャンセル時のコールバック処理を実行します。
    /// </summary>
    protected virtual void CloseCanceledCallbackCore()
    {
    }

    public virtual void Activate()
    {
        this.ActivateRequested?.Invoke();
    }

    public virtual void Close()
    {
        if (this.IsClosed.Value) return;

        this.CloseRequested?.Invoke();
    }

    protected override void Dispose(bool disposing)
    {
        this.IsClosed.Value = true;
        this.IsInitialized = false;

        base.Dispose(disposing);
    }

    protected static void InvokeOnUIDispatcher(Action action)
        => UIDispatcher.Instance.BeginInvoke(action);
}
