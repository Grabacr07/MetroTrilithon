using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace MetroTrilithon.UI;

public static class UIDispatcher
{
    public static Dispatcher? Instance { get; set; }
#if DEBUG
        = DesignFeatures.IsInDesignMode ? Dispatcher.CurrentDispatcher : null;
#endif

    public static DispatcherAwaiter Switch()
        => new(Instance ?? throw new NullReferenceException($"Set the {nameof(UIDispatcher)}.{nameof(Instance)} property to the dispatcher object when the application starts."));

    public readonly struct DispatcherAwaiter(Dispatcher dispatcher) : INotifyCompletion
    {
        public bool IsCompleted
            => dispatcher.CheckAccess();

        public void GetResult()
        {
        }

        public DispatcherAwaiter GetAwaiter()
            => this;

        public void OnCompleted(Action continuation)
            => dispatcher.BeginInvoke(continuation);
    }
}
