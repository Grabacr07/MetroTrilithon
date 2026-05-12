using System.Windows;
using Microsoft.Xaml.Behaviors;
using Wpf.Ui.Controls;

namespace Amethystra.UI.Navigation;

/// <summary>
/// <see cref="BreadcrumbBar"/> のクリックを受け取り、<see cref="NavigationHost.GoToDepthAsync"/> を呼び出します。
/// </summary>
/// <remarks>
/// <para>
/// 表示自体は <see cref="System.Windows.Controls.ItemsControl.ItemsSource"/> に <see cref="NavigationHost.Stack"/> を
/// 直接バインドして実現します。本ビヘイビアはクリック ハンドリングのみを担います。
/// </para>
/// <para>
/// XAML での使用方法:
/// </para>
/// <code>
/// &lt;ui:BreadcrumbBar ItemsSource="{Binding Navigation.Stack}"&gt;
///     &lt;b:Interaction.Behaviors&gt;
///         &lt;nav:BreadcrumbBarNavigationBehavior Host="{Binding Navigation}" /&gt;
///     &lt;/b:Interaction.Behaviors&gt;
///     &lt;ui:BreadcrumbBar.ItemTemplate&gt;
///         &lt;DataTemplate&gt;
///             &lt;TextBlock Text="{Binding Heading}" /&gt;
///         &lt;/DataTemplate&gt;
///     &lt;/ui:BreadcrumbBar.ItemTemplate&gt;
/// &lt;/ui:BreadcrumbBar&gt;
/// </code>
/// </remarks>
public class BreadcrumbBarNavigationBehavior : Behavior<BreadcrumbBar>
{
    private bool _itemClickedHooked;

    #region Host dependency property

    public static readonly DependencyProperty HostProperty
        = DependencyProperty.Register(
            nameof(Host),
            typeof(NavigationHost),
            typeof(BreadcrumbBarNavigationBehavior),
            new PropertyMetadata(null));

    public NavigationHost? Host
    {
        get => (NavigationHost?)this.GetValue(HostProperty);
        set => this.SetValue(HostProperty, value);
    }

    #endregion

    protected override void OnAttached()
    {
        base.OnAttached();
        this.AssociatedObject.ItemClicked += this.HandleItemClicked;
        this._itemClickedHooked = true;
    }

    protected override void OnDetaching()
    {
        if (this.AssociatedObject != null && this._itemClickedHooked)
        {
            this.AssociatedObject.ItemClicked -= this.HandleItemClicked;
            this._itemClickedHooked = false;
        }

        base.OnDetaching();
    }

    private void HandleItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        var host = this.Host;
        if (host == null) return;

        this.AssociatedObject?.Dispatcher.BeginInvoke(async () =>
        {
            await host.GoToDepthAsync(args.Index);
        });
    }
}
