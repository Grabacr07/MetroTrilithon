using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
    private readonly List<IDisposable> _disposables = [];
    private readonly DisposeGateSlim<T> _gate = new();

    public bool IsDisposed
        => this._gate.IsDisposed;

    public void ThrowIfDisposed()
        => this._gate.ThrowIfDisposed();

    public bool TryDispose()
    {
        if (this._gate.TryDispose())
        {
            var snapshot = this._disposables.ToArray();
            this._disposables.Clear();

            for (var i = snapshot.Length - 1; i >= 0; i--)
            {
                snapshot[i].Dispose();
            }

            return true;
        }

        return false;
    }

    public TItem Add<TItem>(TItem item)
        where TItem : IDisposable
    {
        this.EnsureAccessDisposables().Add(item);
        return item;
    }

    private ICollection<IDisposable> EnsureAccessDisposables()
    {
        this.ThrowIfDisposed();
        return this._disposables;
    }

    void ICollection<IDisposable>.Add(IDisposable item)
        => this.EnsureAccessDisposables().Add(item);

    int ICollection<IDisposable>.Count
        => this.EnsureAccessDisposables().Count;

    bool ICollection<IDisposable>.IsReadOnly
        => this.EnsureAccessDisposables().IsReadOnly;

    void ICollection<IDisposable>.Clear()
        => this.EnsureAccessDisposables().Clear();

    bool ICollection<IDisposable>.Contains(IDisposable item)
        => this.EnsureAccessDisposables().Contains(item);

    void ICollection<IDisposable>.CopyTo(IDisposable[] array, int arrayIndex)
        => this.EnsureAccessDisposables().CopyTo(array, arrayIndex);

    bool ICollection<IDisposable>.Remove(IDisposable item)
        => this.EnsureAccessDisposables().Remove(item);

    IEnumerator<IDisposable> IEnumerable<IDisposable>.GetEnumerator()
        => this.EnsureAccessDisposables().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => this.EnsureAccessDisposables().GetEnumerator();
}
