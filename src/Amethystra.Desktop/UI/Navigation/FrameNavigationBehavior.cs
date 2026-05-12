using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Navigation;
using Amethystra.Diagnostics;
using Microsoft.Xaml.Behaviors;
using R3;
using Wpf.Ui.Animations;

namespace Amethystra.UI.Navigation;

/// <summary>
/// <see cref="NavigationHost"/> からのイベントを購読し、対応する <see cref="Frame"/> 上のページ ナビゲーションを実施します。
/// </summary>
/// <remarks>
/// <para>
/// XAML での使用方法:
/// </para>
/// <code>
/// &lt;Frame NavigationUIVisibility="Hidden" JournalOwnership="OwnsJournal"&gt;
///     &lt;b:Interaction.Behaviors&gt;
///         &lt;nav:FrameNavigationBehavior Host="{Binding Navigation}"&gt;
///             &lt;nav:PageMapping ViewModel="{x:Type binds:HomeViewModel}" Page="{x:Type pages:HomePage}" /&gt;
///             &lt;nav:PageMapping ViewModel="{x:Type binds:EditViewModel}" Page="{x:Type pages:EditPage}" /&gt;
///         &lt;/nav:FrameNavigationBehavior&gt;
///     &lt;/b:Interaction.Behaviors&gt;
/// &lt;/Frame&gt;
/// </code>
/// </remarks>
[GenerateLogger]
[ContentProperty(nameof(Mappings))]
public partial class FrameNavigationBehavior : Behavior<Frame>
{
    private IDisposable? _subscription;
    private bool _navigatedHooked;
    private bool _navigatingHooked;
    private bool _nextNavigationIsBack;

    #region Host dependency property

    public static readonly DependencyProperty HostProperty
        = DependencyProperty.Register(
            nameof(Host),
            typeof(NavigationHost),
            typeof(FrameNavigationBehavior),
            new PropertyMetadata(null, HandleHostChanged));

    public NavigationHost? Host
    {
        get => (NavigationHost?)this.GetValue(HostProperty);
        set => this.SetValue(HostProperty, value);
    }

    private static void HandleHostChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameNavigationBehavior behavior)
        {
            behavior.Resubscribe();
        }
    }

    #endregion

    #region Transition dependency properties

    public static readonly DependencyProperty ForwardTransitionProperty
        = DependencyProperty.Register(
            nameof(ForwardTransition),
            typeof(Transition),
            typeof(FrameNavigationBehavior),
            new PropertyMetadata(Transition.FadeInWithSlide));

    public static readonly DependencyProperty BackTransitionProperty
        = DependencyProperty.Register(
            nameof(BackTransition),
            typeof(Transition),
            typeof(FrameNavigationBehavior),
            new PropertyMetadata(Transition.FadeIn));

    public static readonly DependencyProperty TransitionDurationProperty
        = DependencyProperty.Register(
            nameof(TransitionDuration),
            typeof(int),
            typeof(FrameNavigationBehavior),
            new PropertyMetadata(200));

    /// <summary>
    /// 前進方向 (新しいページへの遷移) に適用するトランジションを取得または設定します。
    /// </summary>
    public Transition ForwardTransition
    {
        get => (Transition)this.GetValue(ForwardTransitionProperty);
        set => this.SetValue(ForwardTransitionProperty, value);
    }

    /// <summary>
    /// 後退方向 (戻り遷移) に適用するトランジションを取得または設定します。
    /// </summary>
    public Transition BackTransition
    {
        get => (Transition)this.GetValue(BackTransitionProperty);
        set => this.SetValue(BackTransitionProperty, value);
    }

    /// <summary>
    /// トランジション時間 (ミリ秒) を取得または設定します。
    /// </summary>
    public int TransitionDuration
    {
        get => (int)this.GetValue(TransitionDurationProperty);
        set => this.SetValue(TransitionDurationProperty, value);
    }

    #endregion

    public List<PageMapping> Mappings { get; } = [];

    protected override void OnAttached()
    {
        base.OnAttached();
        this.AssociatedObject.Unloaded += this.HandleUnloaded;
        this.AssociatedObject.Navigating += this.HandleNavigating;
        this._navigatingHooked = true;
        this.AssociatedObject.Navigated += this.HandleNavigated;
        this._navigatedHooked = true;
        this.Resubscribe();
    }

    protected override void OnDetaching()
    {
        this._subscription?.Dispose();
        this._subscription = null;

        if (this.AssociatedObject != null)
        {
            this.AssociatedObject.Unloaded -= this.HandleUnloaded;
            if (this._navigatingHooked)
            {
                this.AssociatedObject.Navigating -= this.HandleNavigating;
                this._navigatingHooked = false;
            }
            if (this._navigatedHooked)
            {
                this.AssociatedObject.Navigated -= this.HandleNavigated;
                this._navigatedHooked = false;
            }
        }

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
        this._subscription = null;

        var host = this.Host;
        var frame = this.AssociatedObject;
        if (host == null || frame == null) return;

        if (host.CurrentViewModel.Value is { } initial)
        {
            this.ApplyPush(initial);
        }

        this._subscription = host.Events
            .ObserveOnCurrentSynchronizationContext()
            .Subscribe(this.HandleEvent);
    }

    private void HandleEvent(NavigationEvent e)
    {
        switch (e)
        {
            case NavigationEvent.Pushed pushed:
                this.ApplyPush(pushed.ViewModel);
                break;

            case NavigationEvent.Popped popped:
                this.ApplyPop(popped.Count);
                break;
        }
    }

    private void ApplyPush(object viewModel)
    {
        var frame = this.AssociatedObject;
        if (frame == null) return;

        var pageType = this.ResolvePageType(viewModel);
        if (pageType == null) return;

        if (Activator.CreateInstance(pageType) is not Page page)
        {
            Log.Warn($"Failed to instantiate page type '{pageType.FullName}'.");
            return;
        }

        page.DataContext = viewModel;
        this._nextNavigationIsBack = false;
        frame.Navigate(page);
    }

    private void ApplyPop(int count)
    {
        var frame = this.AssociatedObject;
        if (frame == null) return;

        var service = frame.NavigationService;

        for (var i = 0; i < count - 1; i++)
        {
            if (service.CanGoBack == false) break;
            service.RemoveBackEntry();
        }

        if (frame.CanGoBack)
        {
            this._nextNavigationIsBack = true;
            frame.GoBack();
        }
    }

    private void HandleNavigating(object sender, NavigatingCancelEventArgs e)
    {
        // ホスト主導以外の Back / Forward 遷移 (マウス戻る/進む、Alt+Left、Backspace 等) は
        // ホストのスタックと同期しなくなるためキャンセルし、ホスト経由で戻し直す。
        if (e.NavigationMode == NavigationMode.Back && this._nextNavigationIsBack == false)
        {
            e.Cancel = true;
            var host = this.Host;
            this.AssociatedObject?.Dispatcher.BeginInvoke(async () =>
            {
                if (host != null) await host.GoBackAsync();
            });
        }
        else if (e.NavigationMode == NavigationMode.Forward)
        {
            e.Cancel = true;
        }
    }

    private void HandleNavigated(object sender, NavigationEventArgs e)
    {
        if (e.Content is not FrameworkElement page) return;

        if (this.Host?.CurrentViewModel.Value is { } vm)
        {
            page.DataContext = vm;
        }

        var transition = this._nextNavigationIsBack
            ? this.BackTransition
            : this.ForwardTransition;
        this._nextNavigationIsBack = false;

        if (transition != Transition.None)
        {
            TransitionAnimationProvider.ApplyTransition(page, transition, this.TransitionDuration);
        }
    }

    private Type? ResolvePageType(object viewModel)
    {
        var vmType = viewModel.GetType();
        var mapping = this.Mappings.FirstOrDefault(m => m.ViewModel == vmType);
        if (mapping?.Page == null)
        {
            Log.Warn($"No page mapping found for ViewModel type '{vmType.FullName}'.");
            return null;
        }

        return mapping.Page;
    }
}
