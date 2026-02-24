using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using Amethystra.Diagnostics;

namespace Amethystra.Disposables;

[GenerateLogger]
internal sealed partial class RefCountGate(Action<int>? onCountChanged = null, Action? onBecameZero = null)
{
    private int _count;

    public int Count
        => Volatile.Read(ref this._count);

    public bool IsActive
        => this.Count > 0;

    public IDisposable Acquire(string caller)
    {
        var afterIncrement = Interlocked.Increment(ref this._count);
        onCountChanged?.Invoke(afterIncrement);
        Log.Debug("+1", new() { afterIncrement, caller });

        var disposed = false;
        return Disposable.Create(() =>
        {
            if (Interlocked.Exchange(ref disposed, true))
            {
                return;
            }

            var afterDecrement = Interlocked.Decrement(ref this._count);
            Debug.Assert(afterDecrement >= 0, "RefCount became negative.");
            onCountChanged?.Invoke(afterDecrement);
            Log.Debug("-1", new() { afterDecrement, caller });

            if (afterDecrement == 0)
            {
                onBecameZero?.Invoke();
            }
        });
    }
}
