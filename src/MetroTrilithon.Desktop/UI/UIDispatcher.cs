using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace MetroTrilithon.UI;

public static class UIDispatcher
{
    private static readonly Lazy<Dispatcher> _instance = new(() => Dispatcher.CurrentDispatcher);

    public static Dispatcher Instance
#if DEBUG
        => DesignFeatures.IsInDesignMode ? Dispatcher.CurrentDispatcher : _instance.Value;
#else
        => _instance.Value;
#endif

    public static void Initialize()
    {
        _ = _instance.Value;
    }

    public static DispatcherAwaiter Switch()
        => new(_instance.Value);

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
