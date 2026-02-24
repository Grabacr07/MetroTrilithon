using System;
using System.Diagnostics;
using System.Reactive.Disposables;

namespace Amethystra.Disposables;

internal sealed class RefCountGate(Action<int>? onCountChanged = null, Action? onBecameZero = null)
{
    private int _count;

    public int Count
        => Volatile.Read(ref this._count);

    public bool IsActive
        => this.Count > 0;

    public IDisposable Acquire()
    {
        var afterEnter = Interlocked.Increment(ref this._count);
        onCountChanged?.Invoke(afterEnter);

        var disposed = false;
        return Disposable.Create(() =>
        {
            if (Interlocked.Exchange(ref disposed, true))
            {
                return;
            }

            var afterExit = Interlocked.Decrement(ref this._count);
            Debug.Assert(afterExit >= 0, "RefCount became negative.");
            onCountChanged?.Invoke(afterExit);

            if (afterExit == 0)
            {
                onBecameZero?.Invoke();
            }
        });
    }
}
