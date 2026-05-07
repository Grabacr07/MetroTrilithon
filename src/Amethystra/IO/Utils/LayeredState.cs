/*
 * Mio I/O Library <https://github.com/takeshik/Mio>
 * Copyright © Takeshi KIRIYA (aka takeshik) <takeshik@tksk.io>
 * All rights reserved. Licensed under the MIT License.
 */

using System;
using System.Threading;

namespace Amethystra.IO.Utils
{
    public sealed class LayeredState<TValue, TConditionArg>
    {
        private sealed class Layer
        {
            public Layer? Parent { get; }

            public TValue Value { get; }

            public Func<TConditionArg, bool> Condition { get; }

            public Layer(Layer? parent, TValue value, Func<TConditionArg, bool> condition)
            {
                this.Parent = parent;
                this.Condition = condition;
                this.Value = value;
            }
        }

        private sealed class StateReversion : IDisposable
        {
            private readonly LayeredState<TValue, TConditionArg> _self;
            private readonly Layer? _revertingValue;

            public StateReversion(LayeredState<TValue, TConditionArg> self, Layer? revertingValue)
            {
                this._self = self;
                this._revertingValue = revertingValue;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                this._self._value.Value = this._revertingValue;
            }
        }

        private readonly AsyncLocal<Layer?> _value;

        public TValue FallbackValue { get; set; }

        public LayeredState(TValue fallbackValue)
        {
            this._value = new AsyncLocal<Layer?>();
            this.FallbackValue = fallbackValue;
        }

        public TValue GetValueFor(TConditionArg conditionArg)
        {
            TValue? value = default;
            for (var current = this._value.Value; current != null && value == null; current = current.Parent)
            {
                if (current.Value == null || !current.Condition(conditionArg)) continue;
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
}
