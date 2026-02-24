using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using System.Windows;
using Amethystra.Disposables;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.TinyLinq;

namespace Amethystra.UI.Controls;

public static partial class WindowSettings
{
    #region Property attached property

    public static readonly DependencyProperty LocationProperty
        = DependencyProperty.RegisterAttached(
            nameof(LocationProperty).GetPropertyName(),
            typeof(IReactiveProperty<Point?>),
            typeof(WindowSettings),
            new PropertyMetadata(null, HandleLocationPropertyChanged));

    public static void SetLocation(Window element, IReactiveProperty<Point> value)
        => element.SetValue(LocationProperty, value);

    public static IReactiveProperty<Point?> GetLocation(Window element)
        => (IReactiveProperty<Point?>)element.GetValue(LocationProperty);

    private static void HandleLocationPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Window window)
        {
            if (Location.IgnoreChanges.TryGetValue(window, out var ignoreLocationChanged) == false)
            {
                Location.IgnoreChanges.Add(window, ignoreLocationChanged = new ScopedFlag());

                window.SourceInitialized += Location.HandleSourceInitialized;
                window.LocationChanged += Location.HandleLocationChanged;
                window.Closed += Location.HandleWindowClosed;
            }

            if (Location.Listeners.TryGetValue(window, out var listener) == false)
            {
                Location.Listeners.Add(window, listener = new SerialDisposable());
            }

            if (e.NewValue is IReactiveProperty<Point?> property)
            {
                listener.Disposable = property
                    .Where(_ => ignoreLocationChanged == false)
                    .ObserveOnUIDispatcher()
                    .Subscribe(x => Location.Set(window, x));
            }
            else
            {
                listener.Disposable = Disposable.Empty;
            }
        }
    }

    #endregion

    private static class Location
    {
        internal static Dictionary<Window, ScopedFlag> IgnoreChanges { get; } = [];

        internal static Dictionary<Window, SerialDisposable> Listeners { get; } = [];

        public static void HandleSourceInitialized(object? sender, EventArgs e)
        {
            if (sender is Window window && GetLocation(window) is { } property)
            {
                Set(window, property.Value);
            }
        }

        public static void HandleLocationChanged(object? sender, EventArgs e)
        {
            if (sender is Window window && GetLocation(window) is { } property)
            {
                using (IgnoreChanges[window].Enable())
                {
                    property.Value = new Point(window.Left, window.Top);
                }
            }
        }

        public static void HandleWindowClosed(object? sender, EventArgs eventArgs)
        {
            if (sender is Window window)
            {
                window.SourceInitialized -= HandleSourceInitialized;
                window.LocationChanged -= HandleLocationChanged;
                window.Closed -= HandleWindowClosed;

                IgnoreChanges.Remove(window);
                Listeners[window].Dispose();
                Listeners.Remove(window);
            }
        }

        public static void Set(Window target, Point? location)
        {
            if (location is { } p)
            {
                target.Left = p.X;
                target.Top = p.Y;
            }
        }
    }
}
