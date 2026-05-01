using System.ComponentModel;
using System.Windows;
using Amethystra.Diagnostics;
using Amethystra.Disposables;
using Amethystra.Mvvm;
using Microsoft.Xaml.Behaviors;
using R3;

namespace Amethystra.UI.Interactivity;

/// <summary>
/// <see cref="Window"/> のライフサイクルイベントを <see cref="WindowViewModelBase"/> に橋渡しします。
/// DataContext が <see cref="WindowViewModelBase"/> であるウィンドウにアタッチして使用します。
/// </summary>
/// <remarks>
/// <para>
/// 各ライフサイクルイベントにおける Window・Behavior・ViewModel 間の通信フローを示します。
/// 水平の矢印は呼び出し、右端の注釈はその場でのローカル処理を表します。
/// </para>
/// <code>
///  Window            Behavior               ViewModel
///    |                   |                      |
///    |--ContentRendered->|                      |
///    |                   |--InitializeAsync()-->|
///    |                   |                      |
///    |--Closing--------->|                      |
///    |                   |  e.Cancel = true     |
///    |                   |  [Task.Yield()]      |
///    |                   |--CanCloseAsync()---->|
///    |                   |            true      |
///    |&lt;--Close()---------|&lt;---------------------|
///    |                   |            false     |
///    |                   |&lt;---------------------| (cancel)
///    |                   |                      |
///    |--Closed---------->|                      |
///    |                   |--OnClosed()--------->|
///    |                   |  Dispose()           |
///    |                   |                      |
///    |&lt;--Close()---------|&lt;--CloseRequested-----|
///    |&lt;--Activate()------|&lt;--ActivateRequested--|
/// </code>
/// </remarks>
[GenerateLogger]
public partial class WindowLifecycleBehavior : Behavior<Window>
{
    private readonly Subject<CancelEventArgs> _closingSubject = new();
    private readonly ScopedFlag _skipClosingCheck = new();
    private WindowViewModelBase? _viewModel;
    private IDisposable? _closeSubscription;
    private IDisposable? _activateSubscription;
    private IDisposable? _contentRenderedSubscription;
    private IDisposable? _closingSubscription;

    protected override void OnAttached()
    {
        base.OnAttached();

        // ContentRendered: Observable に変換して非同期処理を SubscribeAwait に委譲
        this._contentRenderedSubscription = Observable
            .FromEvent<EventHandler, EventArgs>(
                h => (_, e) => h(e),
                h => this.AssociatedObject.ContentRendered += h,
                h => this.AssociatedObject.ContentRendered -= h)
            .Take(1)
            .SubscribeAwait(this.HandleContentRenderedAsync);

        // Closing: 同期ハンドラーで Subject に push し、SubscribeAwait で非同期処理
        // AwaitOperation.Drop で処理中の二重起動を排除する
        this._closingSubscription = this._closingSubject
            .SubscribeAwait(this.HandleClosingAsync, AwaitOperation.Drop);

        this.AssociatedObject.DataContextChanged += this.HandleDataContextChanged;
        this.AssociatedObject.Closing += this.HandleClosing;
        this.AssociatedObject.Closed += this.HandleClosed;

        this.UpdateViewModel(this.AssociatedObject.DataContext);
    }

    protected override void OnDetaching()
    {
        this.AssociatedObject.DataContextChanged -= this.HandleDataContextChanged;
        this.AssociatedObject.Closing -= this.HandleClosing;
        this.AssociatedObject.Closed -= this.HandleClosed;
        this.UpdateViewModel(null);
        this.Cleanup();
        base.OnDetaching();
    }

    private void HandleDataContextChanged(object? sender, DependencyPropertyChangedEventArgs e)
        => this.UpdateViewModel(e.NewValue);

    private void UpdateViewModel(object? dataContext)
    {
        this._closeSubscription?.Dispose();
        this._activateSubscription?.Dispose();
        this._closeSubscription = null;
        this._activateSubscription = null;

        this._viewModel = dataContext as WindowViewModelBase;
        if (this._viewModel == null) return;

        var vm = this._viewModel;
        var window = this.AssociatedObject;
        this._closeSubscription = vm.CloseRequested.Subscribe(_ => window.Close());
        this._activateSubscription = vm.ActivateRequested.Subscribe(_ => window.Activate());
    }

    private async ValueTask HandleContentRenderedAsync(EventArgs _, CancellationToken ct)
    {
        var vm = this._viewModel;
        if (vm == null) return;
        try
        {
            await vm.OnContentRenderedAsync(ct);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"{nameof(WindowViewModelBase.OnContentRenderedAsync)} failed.", new() { this.AssociatedObject, });
        }
    }

    private void HandleClosing(object? sender, CancelEventArgs e)
    {
        // ScopedFlag によるスキップ: Close() 再呼び出し時はチェックをバイパス
        if (this._skipClosingCheck) return;

        // まず同期的にキャンセルし、非同期チェックを Subject 経由で SubscribeAwait に委譲
        e.Cancel = true;
        this._closingSubject.OnNext(e);
    }

    private async ValueTask HandleClosingAsync(CancelEventArgs _, CancellationToken ct)
    {
        // WPF の InternalClose が _isClosing = true を保持したまま Closing イベントを発火するため、
        // ハンドラーが返りきって _isClosing が false になるまで待ってから Close() を再呼び出しする。
        await Task.Yield();

        var vm = this._viewModel;
        try
        {
            var canClose = vm == null || await vm.OnClosingAsync(ct);
            if (canClose)
            {
                // スキップフラグを立てて Close() を再呼び出し。
                // WPF は Close() 内で Closing を同期的に発火するため、
                // using スコープが終わる前にハンドラーが呼ばれてスキップされる。
                using (this._skipClosingCheck.Enable())
                    this.AssociatedObject.Close();
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"{nameof(WindowViewModelBase.OnClosingAsync)} threw an exception.", new() { this.AssociatedObject, });
        }
    }

    private void HandleClosed(object? sender, EventArgs e)
    {
        this._viewModel?.OnClosed();
        this.Cleanup();
    }

    private void Cleanup()
    {
        this._closingSubject.OnCompleted();

        var contentRendered = this._contentRenderedSubscription;
        var closing = this._closingSubscription;
        this._contentRenderedSubscription = null;
        this._closingSubscription = null;
        contentRendered?.Dispose();
        closing?.Dispose();
    }
}
