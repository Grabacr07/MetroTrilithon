using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using Amethystra.UI.Interop;
using R3;

namespace Amethystra.UI.Controls;

public static partial class WindowFeatures
{
    #region StateStore attached property

    public static readonly DependencyProperty StateStoreProperty
        = DependencyProperty.RegisterAttached(
            nameof(StateStoreProperty).GetPropertyName(),
            typeof(WindowStateStore),
            typeof(WindowFeatures),
            new PropertyMetadata(null, HandleStatePersistencePropertyChanged));

    public static void SetStateStore(Window element, WindowStateStore? value)
        => element.SetValue(StateStoreProperty, value);

    public static WindowStateStore? GetStateStore(Window element)
        => (WindowStateStore?)element.GetValue(StateStoreProperty);

    private static void HandleStatePersistencePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Window window)
        {
            StatePersistence.Reinitialize(window);
        }
    }

    #endregion

    #region StateKey attached property

    public static readonly DependencyProperty StateKeyProperty
        = DependencyProperty.RegisterAttached(
            nameof(StateKeyProperty).GetPropertyName(),
            typeof(string),
            typeof(WindowFeatures),
            new PropertyMetadata(null, HandleStatePersistencePropertyChanged));

    public static void SetStateKey(Window element, string? value)
        => element.SetValue(StateKeyProperty, value);

    public static string? GetStateKey(Window element)
        => (string?)element.GetValue(StateKeyProperty);

    #endregion

    private static class StatePersistence
    {
        private static readonly Dictionary<Window, IDisposable> _subscriptions = [];

        public static void Reinitialize(Window window)
        {
            if (_subscriptions.TryGetValue(window, out var existing))
            {
                existing.Dispose();
                _subscriptions.Remove(window);
            }

            var store = GetStateStore(window);
            if (store is null) return;

            var key = GetStateKey(window) ?? window.GetType().Name;
            var hwnd = new WindowInteropHelper(window).Handle;

            if (hwnd != IntPtr.Zero)
            {
                Restore(window, store, key);
                _subscriptions[window] = Subscribe(window, store, key);
            }
            else
            {
                void OnSourceInitialized(object? _, EventArgs __)
                {
                    window.SourceInitialized -= OnSourceInitialized;
                    Restore(window, store, key);
                    _subscriptions[window] = Subscribe(window, store, key);
                }

                void OnClosedBeforeInit(object? _, EventArgs __)
                {
                    window.SourceInitialized -= OnSourceInitialized;
                    window.Closed -= OnClosedBeforeInit;
                }

                window.SourceInitialized += OnSourceInitialized;
                window.Closed += OnClosedBeforeInit;
            }
        }

        private static void Restore(Window window, WindowStateStore store, string key)
        {
            var state = store.GetState(key);
            if (state is null) return;

            new WindowPlacement(
                    state.IsMaximized ? WindowState.Maximized : WindowState.Normal,
                    new Rect(state.Left, state.Top, state.Width, state.Height))
                .Apply(window);
        }

        private static IDisposable Subscribe(Window window, WindowStateStore store, string key)
        {
            window.LocationChanged += OnGeometryChanged;
            window.SizeChanged += OnSizeChanged;
            window.StateChanged += OnGeometryChanged;
            window.Closing += OnClosing;
            window.Closed += OnClosed;

            var topmostDescriptor = DependencyPropertyDescriptor.FromProperty(Window.TopmostProperty, typeof(Window));
            topmostDescriptor.AddValueChanged(window, OnTopmostChanged);

            return Disposable.Create(() =>
            {
                window.LocationChanged -= OnGeometryChanged;
                window.SizeChanged -= OnSizeChanged;
                window.StateChanged -= OnGeometryChanged;
                window.Closing -= OnClosing;
                window.Closed -= OnClosed;
                topmostDescriptor.RemoveValueChanged(window, OnTopmostChanged);
            });

            void OnGeometryChanged(object? sender, EventArgs args)
                => Save(window, store, key);

            void OnSizeChanged(object? sender, SizeChangedEventArgs args)
                => Save(window, store, key);

            void OnTopmostChanged(object? sender, EventArgs args)
                => Save(window, store, key);

            void OnClosing(object? sender, CancelEventArgs args)
                => Save(window, store, key);

            void OnClosed(object? sender, EventArgs args)
                => _subscriptions.Remove(window);
        }

        private static void Save(Window window, WindowStateStore store, string key)
        {
            var placement = WindowPlacement.Get(window);
            if (placement.Rect.Width == 0 || placement.Rect.Height == 0) return;

            store.SetState(key, new PersistedWindowState(
                placement.Rect.Left,
                placement.Rect.Top,
                placement.Rect.Width,
                placement.Rect.Height,
                placement.State == WindowState.Maximized,
                window.Topmost));
        }
    }
}
