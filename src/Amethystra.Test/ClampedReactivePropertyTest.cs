using System.Collections.Generic;
using Amethystra.Reactive;
using R3;

namespace Amethystra.Test;

[TestClass]
public sealed class ClampedReactivePropertyTest
{
    [TestMethod]
    public void InitialValue_IsAccepted_WhenInRange()
    {
        using var prop = new ClampedReactiveProperty(7, 5, 10);
        Assert.AreEqual(7, prop.Value);
    }

    [TestMethod]
    public void InitialValue_IsClamped_ToMin_WhenBelowRange()
    {
        using var prop = new ClampedReactiveProperty(0, 5, 10);
        Assert.AreEqual(5, prop.Value);
    }

    [TestMethod]
    public void InitialValue_IsClamped_ToMax_WhenAboveRange()
    {
        using var prop = new ClampedReactiveProperty(100, 5, 10);
        Assert.AreEqual(10, prop.Value);
    }

    [TestMethod]
    public void Value_IsClamped_ToMin_OnSet()
    {
        using var prop = new ClampedReactiveProperty(7, 5, 10);
        prop.Value = 3;
        Assert.AreEqual(5, prop.Value);
    }

    [TestMethod]
    public void Value_IsClamped_ToMax_OnSet()
    {
        using var prop = new ClampedReactiveProperty(7, 5, 10);
        prop.Value = 15;
        Assert.AreEqual(10, prop.Value);
    }

    [TestMethod]
    public void Value_IsAccepted_WithinRange()
    {
        using var prop = new ClampedReactiveProperty(7, 5, 10);
        prop.Value = 8;
        Assert.AreEqual(8, prop.Value);
    }

    [TestMethod]
    public void Value_IsAccepted_AtBoundaries()
    {
        using var prop = new ClampedReactiveProperty(7, 5, 10);

        prop.Value = 5;
        Assert.AreEqual(5, prop.Value);

        prop.Value = 10;
        Assert.AreEqual(10, prop.Value);
    }

    [TestMethod]
    public void Changed_EmitsCurrentValue_OnSubscribe()
    {
        using var prop = new ClampedReactiveProperty(7, 5, 10);
        var values = new List<int>();
        using var _ = prop.Changed.Subscribe(x => values.Add(x));

        Assert.HasCount(1, values);
        Assert.AreEqual(7, values[0]);
    }

    [TestMethod]
    public void Changed_EmitsNewValue_AfterSet()
    {
        using var prop = new ClampedReactiveProperty(7, 5, 10);
        var values = new List<int>();
        using var _ = prop.Changed.Subscribe(x => values.Add(x));

        prop.Value = 9;
        prop.Value = 6;

        Assert.HasCount(3, values);
        Assert.AreEqual(7, values[0]);
        Assert.AreEqual(9, values[1]);
        Assert.AreEqual(6, values[2]);
    }

    [TestMethod]
    public void Changed_EmitsClamped_WhenSetOutOfRange()
    {
        using var prop = new ClampedReactiveProperty(7, 5, 10);
        var values = new List<int>();
        using var _ = prop.Changed.Subscribe(x => values.Add(x));

        prop.Value = 999;

        Assert.HasCount(2, values);
        Assert.AreEqual(10, values[1]);
    }

    [TestMethod]
    public void PropertyChanged_Fires_OnValueChange()
    {
        using var prop = new ClampedReactiveProperty(7, 5, 10);
        var changeCount = 0;
        prop.PropertyChanged += (_, _) => changeCount++;

        prop.Value = 9;
        prop.Value = 6;

        Assert.AreEqual(2, changeCount);
    }
}
