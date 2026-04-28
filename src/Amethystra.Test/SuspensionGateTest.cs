using System;
using System.Collections.Generic;
using Amethystra.Synchronization;
using R3;

namespace Amethystra.Test;

[TestClass]
public sealed class SuspensionGateTest
{
    [TestMethod]
    public void IsSuspended_EmitsFalse_OnSubscribe()
    {
        using var gate = new SuspensionGate();
        var values = new List<bool>();
        using var _ = gate.IsSuspended.Subscribe(x => values.Add(x));

        Assert.HasCount(1, values);
        Assert.IsFalse(values[0]);
    }

    [TestMethod]
    public void IsSuspended_BecomesTrue_AfterAcquire()
    {
        using var gate = new SuspensionGate();
        var values = new List<bool>();
        using var _ = gate.IsSuspended.Subscribe(x => values.Add(x));

        var handle = gate.Acquire();

        Assert.HasCount(2, values);
        Assert.IsTrue(values[1]);

        handle.Dispose();

        Assert.HasCount(3, values);
        Assert.IsFalse(values[2]);
    }

    [TestMethod]
    public void IsSuspended_StaysTrue_WhileMultipleHandlesHeld()
    {
        using var gate = new SuspensionGate();
        var values = new List<bool>();
        using var _ = gate.IsSuspended.Subscribe(x => values.Add(x));

        var handle1 = gate.Acquire();
        var handle2 = gate.Acquire();

        // Releasing the first handle should not emit false — gate is still held
        handle1.Dispose();

        Assert.HasCount(2, values, "Should only emit true once for nested acquire");
        Assert.IsTrue(values[1]);

        handle2.Dispose();

        Assert.HasCount(3, values);
        Assert.IsFalse(values[2]);
    }

    [TestMethod]
    public void WhenNotSuspended_FiltersItems_WhileGateIsHeld()
    {
        using var gate = new SuspensionGate();
        var subject = new Subject<int>();
        var results = new List<int>();
        using var _ = subject.WhenNotSuspended(gate).Subscribe(x => results.Add(x));

        subject.OnNext(1);

        using (var handle = gate.Acquire())
        {
            subject.OnNext(2);
            subject.OnNext(3);
        }

        subject.OnNext(4);

        Assert.HasCount(2, results);
        Assert.AreEqual(1, results[0]);
        Assert.AreEqual(4, results[1]);
    }

    [TestMethod]
    public void WhenNotSuspended_ThrowsArgumentNullException_ForNullSource()
    {
        using var gate = new SuspensionGate();
        Observable<int> source = null!;
        Assert.ThrowsExactly<ArgumentNullException>(() => source.WhenNotSuspended(gate));
    }

    [TestMethod]
    public void WhenNotSuspended_ThrowsArgumentNullException_ForNullGate()
    {
        var subject = new Subject<int>();
        SuspensionGate gate = null!;
        Assert.ThrowsExactly<ArgumentNullException>(() => subject.WhenNotSuspended(gate));
    }
}
