using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Markup;
using Amethystra.Diagnostics;
using Microsoft.Xaml.Behaviors;
using R3;

namespace Amethystra.UI.Interactivity;

/// <summary>
/// ViewModel 型と Window 型のマッピングを表します。
/// </summary>
public class WindowTransitionMapping
{
    public Type? ViewModelType { get; set; }

    public Type? WindowType { get; set; }
}

/// <summary>
/// <see cref="WindowTransitionRequest"/> を受け取り、対応する <see cref="Window"/> を表示する機能を提供します。
/// </summary>
/// <remarks>
/// <para>
/// XAML での使用方法:
/// </para>
/// <code>
/// &lt;b:Interaction.Behaviors>
///     &lt;metro:WindowTransitionBehavior Source="{Binding Transition}">
///         &lt;metro:WindowTransitionMapping ViewModelType="{x:Type binds:MyViewModel}"
///                                        WindowType="{x:Type local:MyWindow}" />
///     &lt;/metro:WindowTransitionBehavior>
/// &lt;/b:Interaction.Behaviors>
/// </code>
/// </remarks>
[GenerateLogger]
[ContentProperty(nameof(Mappings))]
public partial class WindowTransitionBehavior : Behavior<FrameworkElement>
{
    private IDisposable? _subscription;

    #region Source dependency property

    public static readonly DependencyProperty SourceProperty
        = DependencyProperty.Register(
            nameof(Source),
            typeof(Observable<WindowTransitionRequest>),
            typeof(WindowTransitionBehavior),
            new PropertyMetadata(null, HandleSourceChanged));

    public Observable<WindowTransitionRequest>? Source
    {
        get => (Observable<WindowTransitionRequest>?)this.GetValue(SourceProperty);
        set => this.SetValue(SourceProperty, value);
    }

    private static void HandleSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WindowTransitionBehavior behavior)
        {
            behavior.Resubscribe();
        }
    }

    #endregion

    public List<WindowTransitionMapping> Mappings { get; } = [];

    protected override void OnAttached()
    {
        base.OnAttached();
        this.AssociatedObject.Unloaded += this.HandleUnloaded;
        this.Resubscribe();
    }

    protected override void OnDetaching()
    {
        this._subscription?.Dispose();
        this._subscription = null;
        this.AssociatedObject.Unloaded -= this.HandleUnloaded;
        base.OnDetaching();
    }

    private void HandleUnloaded(object sender, RoutedEventArgs e)
    {
        this._subscription?.Dispose();
        this._subscription = null;
    }

    private void Resubscribe()
    {
        this._subscription?.Dispose();
        this._subscription = this.Source != null && this.AssociatedObject != null
            ? this.Source
                .ObserveOnCurrentSynchronizationContext()
                .SubscribeAwait(this.ShowWindowAsync)
            : null;
    }

    private ValueTask ShowWindowAsync(WindowTransitionRequest request, CancellationToken ct)
    {
        try
        {
            var mapping = this.Mappings.FirstOrDefault(m => m.ViewModelType == request.ViewModel.GetType());
            if (mapping?.WindowType == null)
            {
                Log.Warn($"No mapping found for ViewModel type '{request.ViewModel.GetType().FullName}'.");
                request.Complete(null);
                return ValueTask.CompletedTask;
            }

            if (Activator.CreateInstance(mapping.WindowType, true) is not Window window)
            {
                Log.Warn($"Failed to create a Window instance for type '{mapping.WindowType.FullName}'.");
                request.Complete(null);
                return ValueTask.CompletedTask;
            }

            window.DataContext = request.ViewModel;

            switch (request.Mode)
            {
                case WindowTransitionMode.ShowDialog:
                    window.Owner = Window.GetWindow(this.AssociatedObject);
                    window.Closed += (_, _) => request.Complete(window.DialogResult);
                    window.ShowDialog();
                    break;

                case WindowTransitionMode.Replace:
                    window.Closed += (_, _) => request.Complete(null);
                    this.ReplaceWindow(window);
                    break;

                default:
                    window.Closed += (_, _) => request.Complete(null);
                    window.Show();
                    break;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"An exception occurred while showing a window for ViewModel type '{request.ViewModel.GetType().FullName}'.");
            request.Complete(null);
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// 新しいウィンドウを表示し、遷移元のウィンドウを閉じます。
    /// </summary>
    private void ReplaceWindow(Window window)
    {
        var source = Window.GetWindow(this.AssociatedObject);
        var application = Application.Current;

        // 遷移元が Application.MainWindow の場合、それを閉じると
        // ShutdownMode.OnMainWindowClose によってアプリケーションが終了してしまうため、
        // 閉じる前に新しいウィンドウへ MainWindow の役割を引き継がせる。
        var promotesToMainWindow = source != null
            && application != null
            && ReferenceEquals(application.MainWindow, source);

        window.Show();

        if (promotesToMainWindow) application!.MainWindow = window;

        source?.Close();
    }
}
