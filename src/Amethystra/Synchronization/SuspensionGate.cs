using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace Amethystra.Synchronization;

public sealed class SuspensionGate(Action? onResumed = null) : IDisposable
{
    private readonly BehaviorSubject<int> _count = new(0);
    private int _suspendCount;

    public IObservable<bool> IsSuspended
        => this._count
            .Select(static x => x > 0)
            .DistinctUntilChanged();

    public IDisposable Acquire()
    {
        this._count.OnNext(Interlocked.Increment(ref this._suspendCount));
        var disposed = 0;

        return Disposable.Create(() =>
        {
            if (Interlocked.Exchange(ref disposed, 1) == 1)
            {
                return;
            }

            var after = Interlocked.Decrement(ref this._suspendCount);
            if (after <= 0)
            {
                Interlocked.Exchange(ref this._suspendCount, 0);
                this._count.OnNext(0);
                onResumed?.Invoke();
                return;
            }

            this._count.OnNext(after);
        });
    }

    public void Dispose()
        => this._count.Dispose();
}

public static class SuspensionGateExtensions
{
    public static IObservable<T> WhenNotSuspended<T>(
        this IObservable<T> source,
        SuspensionGate gate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(gate);

        return source
            .WithLatestFrom(gate.IsSuspended)
            .Where(static x => x.Second == false)
            .Select(static x => x.First);
    }
}
