using System;
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
}
