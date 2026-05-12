using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using R3;

namespace Amethystra.UI.Navigation;

/// <summary>
/// ViewModel 主導のページ ナビゲーション スタックを管理します。
/// </summary>
/// <remarks>
/// <para>
/// ViewModel 側で <see cref="NavigateTo"/> / <see cref="GoBackAsync"/> / <see cref="GoToDepthAsync"/> を呼び出すと、
/// 内部スタックを更新したうえで <see cref="Events"/> から購読中のビヘイビアがビュー操作を実施します。
/// </para>
/// <para>
/// スタックから取り除かれた ViewModel が <see cref="IDisposable"/> を実装している場合は <see cref="NavigationHost"/> が dispose します。
/// <see cref="Dispose"/> 時にはスタックに残っているすべての ViewModel も dispose されます。
/// </para>
/// <para>
/// 末尾の ViewModel が <see cref="INavigationGuard"/> を実装している場合、<see cref="GoBackAsync"/> / <see cref="GoToDepthAsync"/> は
/// pop の前にそのガードを呼び出して許可を確認します。
/// </para>
/// </remarks>
public sealed class NavigationHost : IDisposable
{
    private readonly ObservableCollection<object> _stack = [];
    private readonly Subject<NavigationEvent> _events = new();
    private readonly BindableReactiveProperty<object?> _currentViewModel = new();
    private readonly BindableReactiveProperty<bool> _canGoBack = new();
    private bool _disposed;

    /// <summary>
    /// 先頭 (index = 0) が最初に積まれた ViewModel、末尾 (index = <see cref="ReadOnlyObservableCollection{T}.Count"/> - 1) が現在の ViewModel となるスタックを取得します。
    /// </summary>
    /// <remarks>パンくずリストの <c>ItemsSource</c> 等に直接バインドできます。</remarks>
    public ReadOnlyObservableCollection<object> Stack { get; }

    /// <summary>
    /// スタックの末尾にある ViewModel を取得します。スタックが空の場合は <see langword="null"/> です。
    /// </summary>
    public IReadOnlyBindableReactiveProperty<object?> CurrentViewModel
        => this._currentViewModel;

    /// <summary>
    /// 戻り遷移可能かどうかを取得します。スタックの深さが 2 以上のときに <see langword="true"/> です。
    /// </summary>
    public IReadOnlyBindableReactiveProperty<bool> CanGoBack
        => this._canGoBack;

    /// <summary>
    /// ナビゲーションのイベント ストリームを取得します。
    /// </summary>
    public Observable<NavigationEvent> Events
        => this._events;

    /// <summary>
    /// <see cref="GoBackAsync"/> を呼び出すためのコマンドを取得します。<see cref="CanGoBack"/> に追従して有効化されます。
    /// </summary>
    public ReactiveCommand<Unit> GoBackCommand { get; }

    public NavigationHost()
    {
        this.Stack = new ReadOnlyObservableCollection<object>(this._stack);
        this.GoBackCommand = this._canGoBack
            .AsObservable()
            .ToReactiveCommand<Unit>(async (_, _) => await this.GoBackAsync(), false, AwaitOperation.Drop);
    }

    /// <summary>
    /// 新しい ViewModel をスタックの末尾に積みます。
    /// </summary>
    public void NavigateTo(object viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        if (this._disposed) return;

        this._stack.Add(viewModel);
        this.UpdateState();
        this._events.OnNext(new NavigationEvent.Pushed(viewModel));
    }

    /// <summary>
    /// スタックの末尾から ViewModel を取り除き、ひとつ手前の ViewModel をカレントに戻します。
    /// </summary>
    /// <param name="respectGuard">末尾の <see cref="INavigationGuard"/> を尊重する場合は <see langword="true"/>。意図的にバイパスする場合は <see langword="false"/>。</param>
    /// <returns>戻り遷移が実行された場合は <see langword="true"/>、スタックの深さが 1 以下、またはガードに拒否された場合は <see langword="false"/>。</returns>
    /// <remarks>取り除かれた ViewModel が <see cref="IDisposable"/> の場合は dispose されます。</remarks>
    public async ValueTask<bool> GoBackAsync(bool respectGuard = true)
    {
        var popped = await this.GoToDepthAsync(this._stack.Count - 2, respectGuard);
        return popped > 0;
    }

    /// <summary>
    /// スタックを指定した深さ (末尾 index) になるまで pop します。
    /// </summary>
    /// <param name="targetDepthIndex">最終的に末尾としたい index。0 で先頭まで戻します。</param>
    /// <param name="respectGuard">末尾の <see cref="INavigationGuard"/> を尊重する場合は <see langword="true"/>。意図的にバイパスする場合は <see langword="false"/>。</param>
    /// <returns>取り除かれた ViewModel の個数。ガードに拒否された場合は 0。</returns>
    /// <remarks>取り除かれた ViewModel が <see cref="IDisposable"/> の場合は dispose されます。</remarks>
    public async ValueTask<int> GoToDepthAsync(int targetDepthIndex, bool respectGuard = true)
    {
        if (this._disposed) return 0;
        if (targetDepthIndex < 0) return 0;
        if (targetDepthIndex >= this._stack.Count - 1) return 0;

        if (respectGuard && this._stack[^1] is INavigationGuard guard)
        {
            if (await guard.CanLeaveAsync() == false) return 0;
        }

        return this.GoToDepthCore(targetDepthIndex);
    }

    private int GoToDepthCore(int targetDepthIndex)
    {
        if (this._disposed) return 0;
        if (targetDepthIndex < 0) return 0;
        if (targetDepthIndex >= this._stack.Count - 1) return 0;

        var popCount = this._stack.Count - 1 - targetDepthIndex;
        var popped = new List<object>(popCount);

        for (var i = 0; i < popCount; i++)
        {
            var topIndex = this._stack.Count - 1;
            popped.Add(this._stack[topIndex]);
            this._stack.RemoveAt(topIndex);
        }

        this.UpdateState();
        this._events.OnNext(new NavigationEvent.Popped(popCount));

        foreach (var item in popped)
        {
            (item as IDisposable)?.Dispose();
        }

        return popCount;
    }

    private void UpdateState()
    {
        this._currentViewModel.Value = this._stack.Count > 0 ? this._stack[^1] : null;
        this._canGoBack.Value = this._stack.Count > 1;
    }

    public void Dispose()
    {
        if (this._disposed) return;
        this._disposed = true;

        this._events.OnCompleted();
        this._events.Dispose();
        this.GoBackCommand.Dispose();
        this._currentViewModel.Dispose();
        this._canGoBack.Dispose();

        for (var i = this._stack.Count - 1; i >= 0; i--)
        {
            (this._stack[i] as IDisposable)?.Dispose();
        }

        this._stack.Clear();
    }
}
