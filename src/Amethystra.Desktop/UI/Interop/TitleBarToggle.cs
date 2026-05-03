using System.Windows;
using System.Windows.Controls.Primitives;

namespace Amethystra.UI.Interop;

public static class TitleBarToggle
{
    #region IsEnabled attached property

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(TitleBarToggle),
            new PropertyMetadata(BooleanBoxes.FalseBox, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject element)
        => (bool)element.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject element, bool value)
        => element.SetValue(IsEnabledProperty, BooleanBoxes.Box(value));

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ButtonBase button) return;

        if ((bool)e.NewValue)
            button.Click += Toggle;
        else
            button.Click -= Toggle;
    }

    #endregion

    #region IsChecked attached property

    public static readonly DependencyProperty IsCheckedProperty =
        DependencyProperty.RegisterAttached(
            "IsChecked",
            typeof(bool),
            typeof(TitleBarToggle),
            new FrameworkPropertyMetadata(
                BooleanBoxes.FalseBox,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static bool GetIsChecked(DependencyObject element)
        => (bool)element.GetValue(IsCheckedProperty);

    public static void SetIsChecked(DependencyObject element, bool value)
        => element.SetValue(IsCheckedProperty, BooleanBoxes.Box(value));

    #endregion

    private static void Toggle(object sender, RoutedEventArgs e)
    {
        if (sender is DependencyObject d)
            SetIsChecked(d, GetIsChecked(d) == false);
    }
}
