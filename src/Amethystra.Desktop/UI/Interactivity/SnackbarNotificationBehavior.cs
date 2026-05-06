using System;
using System.Windows;
using Microsoft.Xaml.Behaviors;
using R3;
using Wpf.Ui;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;

namespace Amethystra.UI.Interactivity;

public sealed class SnackbarNotificationBehavior : Behavior<SnackbarPresenter>
{
    private readonly SnackbarService _snackbarService = new();
    private IDisposable? _subscription;

    #region Source dependency property

    public static readonly DependencyProperty SourceProperty
        = DependencyProperty.Register(
            nameof(Source),
            typeof(Observable<Notification>),
            typeof(SnackbarNotificationBehavior),
            new PropertyMetadata(null, HandleSourceChanged));

    public Observable<Notification>? Source
    {
        get => (Observable<Notification>?)this.GetValue(SourceProperty);
        set => this.SetValue(SourceProperty, value);
    }

    private static void HandleSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SnackbarNotificationBehavior behavior)
        {
            behavior.Resubscribe();
        }
    }

    #endregion

    protected override void OnAttached()
    {
        base.OnAttached();
        this._snackbarService.SetSnackbarPresenter(this.AssociatedObject);
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
                .Subscribe(this.ShowNotification)
            : null;
    }

    private void ShowNotification(Notification notification)
    {
        var appearance = notification.Severity switch
        {
            NotificationSeverity.Success => ControlAppearance.Success,
            NotificationSeverity.Caution => ControlAppearance.Caution,
            NotificationSeverity.Danger => ControlAppearance.Danger,
            _ => ControlAppearance.Secondary,
        };
        this._snackbarService.Show(notification.Title, notification.Message, appearance);
    }
}
