using System;
using System.Runtime.CompilerServices;
using Amethystra.Diagnostics;
using Amethystra.Disposables;
using R3;

namespace Amethystra.Synchronization;

public sealed class SuspensionGate : IDisposable
{
    private readonly ReactiveProperty<int> _count = new(0);
    private readonly RefCountGate _gate;

    public SuspensionGate(Action? onResumed = null)
        => this._gate = new RefCountGate(count => this._count.Value = count, onResumed);

    public Observable<bool> IsSuspended
        => this._count
            .Select(static x => x > 0)
            .DistinctUntilChanged();

    public IDisposable Acquire([CallerMemberName] string member = "", [CallerFilePath] string file = "")
        => this._gate.Acquire(Caller.GetCallerLabel(member, file));

    public void Dispose()
        => this._count.Dispose();
}

public static class SuspensionGateExtensions
{
    public static Observable<T> WhenNotSuspended<T>(
        this Observable<T> source,
        SuspensionGate gate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(gate);

        return source
            .WithLatestFrom(gate.IsSuspended, static (item, isSuspended) => (item, isSuspended))
            .Where(static x => x.isSuspended == false)
            .Select(static x => x.item);
    }
}
