using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MetroTrilithon.Lifetime;

public class SerialDisposable<T> : IDisposable
    where T : IDisposable
{
    private readonly object _sync = new();

    private T? _current;
    private bool _disposed;

    public bool IsDisposed
    {
        get
        {
            lock (this._sync)
            {
                return this._disposed;
            }
        }
    }

    public T? Disposable
    {
        get => this._current;
        set
        {
            bool shouldDispose;
            var old = default(IDisposable);
            lock (this._sync)
            {
                shouldDispose = this._disposed;
                if (!shouldDispose)
                {
                    old = this._current;
                    this._current = value;
                }
            }

            old?.Dispose();

            if (shouldDispose) value?.Dispose();
        }
    }

    public void Dispose()
    {
        var old = default(IDisposable);

        lock (this._sync)
        {
            if (!this._disposed)
            {
                this._disposed = true;
                old = this._current;
                this._current = default;
            }
        }

        old?.Dispose();
    }
}
