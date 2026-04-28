using System;
using System.Collections.Generic;

namespace Amethystra.Mvvm;

public abstract class ViewModelBase : Notifier, IDisposable
{
    private readonly List<IDisposable> _compositeDisposable = [];
    private bool _disposed;

    public ICollection<IDisposable> CompositeDisposable
        => this._compositeDisposable;

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

        for (var i = this._compositeDisposable.Count - 1; i >= 0; i--)
        {
            this._compositeDisposable[i].Dispose();
        }

        this._compositeDisposable.Clear();
    }
}
