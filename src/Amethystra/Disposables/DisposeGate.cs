using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;

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

public sealed class DisposeGate<T> : IDisposeGate, ICollection<IDisposable>
{
    private readonly CompositeDisposable _subscriptions = [];
    private readonly DisposeGateSlim<T> _gate = new();

    public bool IsDisposed
        => this._gate.IsDisposed;

    public void ThrowIfDisposed()
        => this._gate.ThrowIfDisposed();

    public bool TryDispose()
    {
        if (this._gate.TryDispose())
        {
            this._subscriptions.Dispose();
            return true;
        }

        return false;
    }

    private CompositeDisposable EnsureAccessSubscriptions()
    {
        this.ThrowIfDisposed();
        return this._subscriptions;
    }

    public void Add(IDisposable item)
        => this.EnsureAccessSubscriptions().Add(item);

    int ICollection<IDisposable>.Count
        => this.EnsureAccessSubscriptions().Count;

    bool ICollection<IDisposable>.IsReadOnly
        => this.EnsureAccessSubscriptions().IsReadOnly;

    void ICollection<IDisposable>.Clear()
        => this.EnsureAccessSubscriptions().Clear();

    bool ICollection<IDisposable>.Contains(IDisposable item)
        => this.EnsureAccessSubscriptions().Contains(item);

    void ICollection<IDisposable>.CopyTo(IDisposable[] array, int arrayIndex)
        => this.EnsureAccessSubscriptions().CopyTo(array, arrayIndex);

    bool ICollection<IDisposable>.Remove(IDisposable item)
        => this.EnsureAccessSubscriptions().Remove(item);

    IEnumerator<IDisposable> IEnumerable<IDisposable>.GetEnumerator()
        => this.EnsureAccessSubscriptions().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => this.EnsureAccessSubscriptions().GetEnumerator();
}
