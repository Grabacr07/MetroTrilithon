using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Animation;

namespace Amethystra.UI.Controls;

public class CornerRadiusTopConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
        => values is [CornerRadius cornerRadius, bool isExpanded, ..]
            ? isExpanded
                ? new CornerRadius(cornerRadius.TopLeft, cornerRadius.TopRight, 0, 0)
                : cornerRadius
            : DependencyProperty.UnsetValue;

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class AnimationFactorToValueConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
        => values is [double height, double factor, ..]
            ? parameter is "negative"
                ? -(height * factor)
                : height * factor
            : DependencyProperty.UnsetValue;

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public static class CardExpanderAnimationHelper
{
    /// <summary>
    /// テンプレート内のコンテンツ コンテナー <see cref="Border"/> に付与する <c>x:Name</c>。
    /// </summary>
    private const string PART_ContentPresenterBorder = nameof(PART_ContentPresenterBorder);

    /// <summary>
    /// 展開アニメーションの <see cref="Storyboard"/> を <see cref="Style"/> のリソースに登録する際のキー。
    /// </summary>
    public const string ExpandStoryboardKey = "Style.CardExpander.Fluent.ExpandStoryboard";

    /// <summary>
    /// 折りたたみアニメーションの <see cref="Storyboard"/> を <see cref="Style"/> のリソースに登録する際のキー。
    /// </summary>
    public const string CollapseStoryboardKey = "Style.CardExpander.Fluent.CollapseStoryboard";

    #region AnimationTagValue attached property

    public static readonly DependencyProperty AnimationTagValueProperty
        = DependencyProperty.RegisterAttached(
            nameof(AnimationTagValueProperty).GetPropertyName(),
            typeof(double),
            typeof(CardExpanderAnimationHelper),
            new PropertyMetadata(0.0));

    public static void SetAnimationTagValue(DependencyObject element, double value)
        => element.SetValue(AnimationTagValueProperty, value);

    public static double GetAnimationTagValue(DependencyObject element)
        => (double)element.GetValue(AnimationTagValueProperty);

    #endregion

    #region IsAnimationManaged attached property

    /// <summary>
    /// 展開・折りたたみアニメーションをコードから管理するかどうかを示す値を取得または設定します。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see langword="true"/> を設定すると、対象要素の初回 <see cref="FrameworkElement.Loaded"/>
    /// 時点で <see cref="Expander.IsExpanded"/> の値に合わせて <see cref="AnimationTagValueProperty"/>
    /// と <see cref="UIElement.VisibilityProperty"/> をアニメーションなしで直接設定します。
    /// 以降の <see cref="Expander.IsExpanded"/> 変更時には <see cref="ExpandStoryboardKey"/>
    /// または <see cref="CollapseStoryboardKey"/> から <see cref="Storyboard"/> を取得して再生します。
    /// </para>
    /// <para>
    /// WPF の <see cref="System.Windows.Trigger"/> はプロパティの初期値が条件にマッチした時点でも
    /// EnterActions を発火するため、起動時から <see cref="Expander.IsExpanded"/> が
    /// <see langword="true"/> である場合に意図しないアニメーションが再生されます。
    /// この添付プロパティはその回避を目的としています。
    /// </para>
    /// </remarks>
    public static readonly DependencyProperty IsAnimationManagedProperty
        = DependencyProperty.RegisterAttached(
            nameof(IsAnimationManagedProperty).GetPropertyName(),
            typeof(bool),
            typeof(CardExpanderAnimationHelper),
            new PropertyMetadata(false, HandleIsAnimationManagedChanged));

    public static void SetIsAnimationManaged(DependencyObject element, bool value)
        => element.SetValue(IsAnimationManagedProperty, value);

    public static bool GetIsAnimationManaged(DependencyObject element)
        => (bool)element.GetValue(IsAnimationManagedProperty);

    private static void HandleIsAnimationManagedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Expander expander) return;

        var enabled = (bool)e.NewValue;
        var wasEnabled = (bool)e.OldValue;

        if (enabled && wasEnabled == false)
        {
            // Page ナビゲーションで Loaded / Unloaded が繰り返されるため、Loaded は一度きりではなく永続的に購読する。
            expander.Loaded += HandleLoaded;
            if (expander.IsLoaded)
            {
                Attach(expander);
            }
        }
        else if (enabled == false && wasEnabled)
        {
            expander.Loaded -= HandleLoaded;
            Detach(expander);
        }
    }

    #endregion

    /// <summary>
    /// 現在 Attach 済みかどうかを示す内部フラグ。
    /// Page ナビゲーションのように Loaded / Unloaded が複数回繰り返されるシナリオで、
    /// 二重 Attach や Attach 漏れを防ぐために使用します。
    /// </summary>
    // ReSharper disable once InconsistentNaming
    private static readonly DependencyProperty IsAttachedProperty
        = DependencyProperty.RegisterAttached(
            "IsAttached",
            typeof(bool),
            typeof(CardExpanderAnimationHelper),
            new PropertyMetadata(false));

    private static void HandleLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not Expander expander) return;
        Attach(expander);
    }

    private static void Attach(Expander expander)
    {
        // 二重 Attach 防止: 同じ expander への AddValueChanged / Unloaded += を繰り返さない。
        if ((bool)expander.GetValue(IsAttachedProperty)) return;
        if (expander.Template?.FindName(PART_ContentPresenterBorder, expander) is not Border border) return;

        // 初期状態は Storyboard を介さず直接設定します。
        SetAnimationTagValue(border, expander.IsExpanded ? 0.0 : 1.0);
        border.Visibility = expander.IsExpanded ? Visibility.Visible : Visibility.Collapsed;

        // 以降の IsExpanded 変更時にコードから Storyboard を再生します。
        var descriptor = DependencyPropertyDescriptor.FromProperty(Expander.IsExpandedProperty, typeof(Expander));
        descriptor.AddValueChanged(expander, HandleIsExpandedChanged);

        // メモリ リークを避けるため Unloaded で descriptor を解除します。
        expander.Unloaded += HandleUnloaded;

        expander.SetValue(IsAttachedProperty, true);
    }

    private static void HandleUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is not Expander expander) return;
        Detach(expander);
    }

    private static void Detach(Expander expander)
    {
        if ((bool)expander.GetValue(IsAttachedProperty) == false) return;

        expander.Unloaded -= HandleUnloaded;

        var descriptor = DependencyPropertyDescriptor.FromProperty(Expander.IsExpandedProperty, typeof(Expander));
        descriptor.RemoveValueChanged(expander, HandleIsExpandedChanged);

        expander.SetValue(IsAttachedProperty, false);
    }

    private static void HandleIsExpandedChanged(object? sender, EventArgs e)
    {
        if (sender is not Expander expander) return;
        if (expander.Template is not FrameworkTemplate template) return;

        var key = expander.IsExpanded ? ExpandStoryboardKey : CollapseStoryboardKey;
        if (expander.TryFindResource(key) is Storyboard storyboard)
        {
            // Storyboard.TargetName の解決にはテンプレートの名前スコープを使用します。
            storyboard.Begin(expander, template);
        }
    }
}
