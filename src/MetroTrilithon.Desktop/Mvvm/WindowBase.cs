using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using Livet;
using Livet.Messaging;
using Livet.Messaging.Windows;
using Reactive.Bindings;

namespace MetroTrilithon.Mvvm;

public abstract class WindowBase : ViewModel
{
    public const string WindowActionMessageKey = nameof(WindowActionMessageKey);

    public IReactiveProperty<string> Title { get; }
        = new ReactiveProperty<string>();

    public IReactiveProperty<bool> CanClose { get; }
        = new ReactiveProperty<bool>();

    public IReactiveProperty<bool> IsClosed { get; }
        = new ReactiveProperty<bool>();

    /// <summary>
    /// <see cref="InitializeCore"/> メソッドが呼ばれたかどうか (通常、これはアタッチされたウィンドウの <see cref="Window.ContentRendered"/> イベントによって呼び出されます) を示す値を取得します。
    /// </summary>
    public bool IsInitialized { get; private set; }

    public bool DialogResult { get; protected set; }

    public WindowState WindowState { get; set; }

    /// <summary>
    /// このメソッドは、アタッチされたウィンドウで <see cref="Window.ContentRendered"/> イベントが発生したときに、Livet インフラストラクチャによって呼び出されます。
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
    /// このメソッドは、アタッチされたウィンドウで <see cref="Window.Closing"/> イベントがキャンセルされたときに、Livet インフラストラクチャによって呼び出されます。
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void CloseCanceledCallback()
    {
        this.CloseCanceledCallbackCore();
    }

    /// <summary>
    /// 派生クラスでオーバーライドされると、アタッチされたウィンドウで <see cref="Window.Closing"/> イベントがキャンセルされたときに <see cref="Livet.Behaviors.WindowCloseCancelBehavior"/> によって呼び出されるコールバック処理を実行します。
    /// </summary>
    protected virtual void CloseCanceledCallbackCore()
    {
    }


    public virtual void Activate()
    {
        if (this.WindowState == WindowState.Minimized)
        {
            this.SendWindowAction(WindowAction.Normal);
        }

        this.SendWindowAction(WindowAction.Active);
    }

    public virtual void Close()
    {
        if (this.IsClosed.Value) return;

        this.SendWindowAction(WindowAction.Close);
    }

    protected override void Dispose(bool disposing)
    {
        this.IsClosed.Value = true;
        this.IsInitialized = false;

        base.Dispose(disposing);
    }

    protected void SendWindowAction(WindowAction action)
    {
        this.Messenger.Raise(new WindowActionMessage(action, WindowActionMessageKey));
    }

    protected void Transition(ViewModel viewModel, Type windowType, TransitionMode mode, bool isOwned)
    {
        var message = new TransitionMessage(windowType, viewModel, mode, isOwned ? "Window.Transition.Child" : "Window.Transition");
        this.Messenger.Raise(message);
    }

    protected void InvokeOnUIDispatcher(Action action)
    {
        DispatcherHelper.UIDispatcher.BeginInvoke(action);
    }
}
