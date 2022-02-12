using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace MetroTrilithon.UI;

public static class UIDispatcher
{
    public static Dispatcher Instance { get; set; }
#if DEBUG
        = DebugFeatures.IsInDesignMode ? Dispatcher.CurrentDispatcher : default!;
#else
= default!;
#endif

    public static DispatcherAwaiter Switch()
        => new(Instance);

    public readonly struct DispatcherAwaiter : INotifyCompletion
    {
        private readonly Dispatcher _dispatcher;

        public bool IsCompleted
            => this._dispatcher.CheckAccess();

        public DispatcherAwaiter(Dispatcher dispatcher)
        {
            this._dispatcher = dispatcher;
        }

        public void GetResult()
        {
        }

        public DispatcherAwaiter GetAwaiter()
            => this;

        public void OnCompleted(Action continuation)
            => this._dispatcher.BeginInvoke(continuation);
    }
}
