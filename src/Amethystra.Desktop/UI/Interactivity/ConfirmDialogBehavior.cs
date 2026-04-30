using System;
using System.Windows;
using Microsoft.Xaml.Behaviors;
using R3;
using Wpf.Ui.Controls;

namespace Amethystra.UI.Interactivity;

/// <summary>
/// <see cref="ConfirmMessage"/> を受け取り、アタッチされた <see cref="ContentDialogHost"/> 上で確認ダイアログを表示する機能を提供します。
/// </summary>
public class ConfirmDialogBehavior : Behavior<ContentDialogHost>
{
    private IDisposable? _subscription;

    #region Observable dependency property

    public static readonly DependencyProperty ObservableProperty
        = DependencyProperty.Register(
            nameof(Observable),
            typeof(Observable<ConfirmMessage>),
            typeof(ConfirmDialogBehavior),
            new PropertyMetadata(null, HandleObservableChanged));

    public Observable<ConfirmMessage>? Observable
    {
        get => (Observable<ConfirmMessage>?)this.GetValue(ObservableProperty);
        set => this.SetValue(ObservableProperty, value);
    }

    private static void HandleObservableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ConfirmDialogBehavior behavior)
        {
            behavior.Resubscribe();
        }
    }

    #endregion

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
        this._subscription = this.Observable != null && this.AssociatedObject != null
            ? this.Observable
                .ObserveOnCurrentSynchronizationContext()
                .SubscribeAwait(this.ShowDialogAsync)
            : null;
    }

    private async ValueTask ShowDialogAsync(ConfirmMessage msg, CancellationToken ct)
    {
        try
        {
            var dialog = new ContentDialog(this.AssociatedObject)
            {
                Title = msg.Title,
                Content = msg.Content,
                PrimaryButtonText = msg.PrimaryButtonText,
                CloseButtonText = msg.CloseButtonText,
            };
            var result = await dialog.ShowAsync(ct);
            var confirmed = result == ContentDialogResult.Primary;

            msg.SetReply(confirmed);
        }
        catch
        {
            msg.SetReply(false);
        }
    }
}
