using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace MetroTrilithon.UI.Controls;

[ContentProperty(nameof(Templates))]
public class DataTemplateSelectorEx : DataTemplateSelector
{
    public List<DataTemplate> Templates { get; set; } = new();

    public override DataTemplate? SelectTemplate(object? item, DependencyObject container)
        => this.Templates.FirstOrDefault(x => x.DataType is Type type && item != null && IsMatch(type, item.GetType()))
            ?? base.SelectTemplate(item, container);

    private static bool IsMatch(Type dataType, Type itemType)
        => dataType == itemType || itemType.IsSubclassOf(dataType);
}
