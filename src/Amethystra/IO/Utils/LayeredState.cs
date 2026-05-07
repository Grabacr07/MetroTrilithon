/*
 * Mio I/O Library <https://github.com/takeshik/Mio>
 * Copyright © Takeshi KIRIYA (aka takeshik) <takeshik@tksk.io>
 * All rights reserved. Licensed under the MIT License.
 */

using System;
using System.Threading;

namespace Amethystra.IO.Utils;

public sealed class LayeredState<TValue, TConditionArg>(TValue fallbackValue)
{
    private sealed class Layer(Layer? parent, TValue value, Func<TConditionArg, bool> condition)
    {
        public Layer? Parent { get; } = parent;

        public TValue Value { get; } = value;

        public Func<TConditionArg, bool> Condition { get; } = condition;
    }

    private sealed class StateReversion(LayeredState<TValue, TConditionArg> self, Layer? revertingValue)
        : IDisposable
    {
        /// <inheritdoc />
        public void Dispose()
        {
            self._value.Value = revertingValue;
        }
    }

    private readonly AsyncLocal<Layer?> _value = new();

    public TValue FallbackValue { get; set; } = fallbackValue;

    public TValue GetValueFor(TConditionArg conditionArg)
    {
        TValue? value = default;
        for (var current = this._value.Value; current != null && value == null; current = current.Parent)
        {
            if (current.Value == null || current.Condition(conditionArg) == false) continue;
            value = current.Value;
            return value;
        }

        return this.FallbackValue;
    }

    public IDisposable BeginWith(TValue value, Func<TConditionArg, bool>? condition = null)
    {
        var current = this._value.Value;
        this._value.Value = new Layer(current, value, condition ?? (_ => true));
        return new StateReversion(this, current);
    }
}