using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace MetroTrilithon.UI;

public static class UIExtensions
{
    private const string _propertyKeyword = "Property";

    /// <summary>
    /// 'XxxProperty' -> 'Xxx'
    /// </summary>
    public static string GetPropertyName(this string dependencyPropertyName)
        => dependencyPropertyName.Equals(_propertyKeyword, StringComparison.OrdinalIgnoreCase) == false
            && dependencyPropertyName.EndsWith(_propertyKeyword, StringComparison.Ordinal)
                ? dependencyPropertyName[..dependencyPropertyName.LastIndexOf(_propertyKeyword, StringComparison.Ordinal)]
                : dependencyPropertyName;

    public static T? FindLogicalAncestor<T>(this DependencyObject? dependencyObject)
        where T : DependencyObject
    {
        while (dependencyObject != null)
        {
            if (dependencyObject is T ancestor) return ancestor;
            dependencyObject = LogicalTreeHelper.GetParent(dependencyObject);
        }

        return default;
    }

    public static T? FindVisualAncestor<T>(this DependencyObject? dependencyObject)
        where T : DependencyObject
    {
        while (dependencyObject != null)
        {
            if (dependencyObject is T ancestor) return ancestor;
            dependencyObject = VisualTreeHelper.GetParent(dependencyObject);
        }

        return default;
    }
}
