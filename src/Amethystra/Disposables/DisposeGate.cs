using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Amethystra.Disposables;

public interface IDisposeGate
{
    bool IsDisposed { get; }

    void ThrowIfDisposed();

    bool TryDispose();
}

public sealed class DisposeGateSlim<T> : IDisposeGate
{
    private bool _disposed;

    public bool IsDisposed
        => Volatile.Read(ref this._disposed);

    public void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(this.IsDisposed, typeof(T));
    }

    public bool TryDispose()
    {
        if (Interlocked.Exchange(ref this._disposed, true) == false)
        {
            return true;
        }

        Debug.Fail($"Double disposal detected: {typeof(T).FullName}");
        return false;
    }
}

public sealed class DisposeGate<T> : IDisposeGate
{
    private readonly Stack<IDisposable> _disposables = new();
    private readonly DisposeGateSlim<T> _gate = new();

    public bool IsDisposed
        => this._gate.IsDisposed;

    public void ThrowIfDisposed()
        => this._gate.ThrowIfDisposed();

    public bool TryDispose()
    {
        if (this._gate.TryDispose())
        {
            while (this._disposables.TryPop(out var disposable))
            {
                disposable.Dispose();
            }

            return true;
        }

        return false;
    }

    public TItem Add<TItem>(TItem item)
        where TItem : IDisposable
    {
        this.ThrowIfDisposed();
        this._disposables.Push(item);
        return item;
    }
}

public static class DisposeGateExtensions
{
    extension<TDisposable>(TDisposable disposable) where TDisposable : IDisposable
    {
        public TDisposable AddTo<TGate>(DisposeGate<TGate> gate)
        {
            return gate.Add(disposable);
        }
    }
}
