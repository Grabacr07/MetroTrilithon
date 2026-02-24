using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using Amethystra.Diagnostics;
using Amethystra.Disposables;

namespace Amethystra.Synchronization;

public sealed class SuspensionGate : IDisposable
{
    private readonly BehaviorSubject<int> _count = new(0);
    private readonly RefCountGate _gate;

    public SuspensionGate(Action? onResumed = null)
        => this._gate = new RefCountGate(this._count.OnNext, onResumed);

    public IObservable<bool> IsSuspended
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
