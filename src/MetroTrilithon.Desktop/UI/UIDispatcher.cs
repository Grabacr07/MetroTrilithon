using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace MetroTrilithon.UI;

public static class UIDispatcher
{
    public static Dispatcher? Instance { get; set; }
#if DEBUG
        = DesignFeatures.IsInDesignMode ? Dispatcher.CurrentDispatcher : default;
#endif

    public static DispatcherAwaiter Switch()
        => new(Instance ?? throw new NullReferenceException("Set the UIDispatcher.Instance property to the Dispatcher object when the application starts."));

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
