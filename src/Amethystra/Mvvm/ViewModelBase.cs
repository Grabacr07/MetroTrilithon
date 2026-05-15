using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using R3;

namespace Amethystra.Mvvm;

public abstract class ViewModelBase : IDisposable
{
    private DisposableBag _disposables;
    private bool _disposed;

    protected ref DisposableBag Disposables
        => ref this._disposables;

    internal void Add(IDisposable disposable)
        => this._disposables.Add(disposable);

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (this._disposed) return;
        this._disposed = true;

        if (disposing == false) return;

        this._disposables.Dispose();
    }
}

public static class ViewModelExtensions
{
    extension(IDisposable disposable)
    {
        public void AddTo(ViewModelBase vm)
        {
            vm.Add(disposable);
        }
    }

    extension<TDisposable>(TDisposable disposable)
        where TDisposable : IDisposable
    {
        [MustUseReturnValue]
        public TDisposable AddTo(ViewModelBase vm)
        {
            vm.Add(disposable);
            return disposable;
        }
    }

    extension<T>(Observable<T> source)
    {
        public void SubscribeWith(ViewModelBase vm, Action<T> onNext)
        {
            source.Subscribe(onNext).AddTo(vm);
        }

        /// <summary>
        /// Subscribes <paramref name="vm"/> to <paramref name="source"/>, passing
        /// <paramref name="vm"/> itself as the state argument to <paramref name="onNext"/> so the
        /// callback can be a <c>static</c> lambda without forming a closure. The subscription is
        /// automatically tied to the lifetime of <paramref name="vm"/>.
        /// </summary>
        public void SubscribeWith<TVm>(TVm vm, Action<T, TVm> onNext)
            where TVm : ViewModelBase
        {
            source.Subscribe(vm, onNext).AddTo(vm);
        }
    }

    extension<T>(ReactiveCommand<T> command)
    {
        [MustUseReturnValue]
        public ReactiveCommand<T> SubscribeWith(ViewModelBase vm, Action<T> onNext)
        {
            vm.Add(command);
            command.Subscribe(onNext).AddTo(vm);
            return command;
        }

        /// <inheritdoc cref="SubscribeWith{T, TVm}(R3.Observable{T}, TVm, System.Action{T, TVm})"/>
        [MustUseReturnValue]
        public ReactiveCommand<T> SubscribeWith<TVm>(TVm vm, Action<T, TVm> onNext)
            where TVm : ViewModelBase
        {
            vm.Add(command);
            command.Subscribe(vm, onNext).AddTo(vm);
            return command;
        }

        [MustUseReturnValue]
        public ReactiveCommand<T> SubscribeWith(ViewModelBase vm, Func<T, CancellationToken, ValueTask> onNextAsync, AwaitOperation awaitOperation = AwaitOperation.Sequential)
        {
            vm.Add(command);
            command.SubscribeAwait(onNextAsync, awaitOperation).AddTo(vm);
            return command;
        }

        /// <inheritdoc cref="SubscribeWith{T, TVm}(R3.Observable{T}, TVm, System.Action{T, TVm})"/>
        [MustUseReturnValue]
        public ReactiveCommand<T> SubscribeWith<TVm>(TVm vm, Func<T, TVm, CancellationToken, ValueTask> onNextAsync, AwaitOperation awaitOperation = AwaitOperation.Sequential)
            where TVm : ViewModelBase
        {
            vm.Add(command);
            command.SubscribeAwait(vm, onNextAsync, awaitOperation).AddTo(vm);
            return command;
        }
    }
}
