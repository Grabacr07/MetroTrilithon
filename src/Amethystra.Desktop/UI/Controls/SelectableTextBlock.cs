using System.Windows;
using System.Windows.Controls;

namespace Amethystra.UI.Controls;

public class SelectableTextBlock : TextBox
{
    static SelectableTextBlock()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(SelectableTextBlock),
            new FrameworkPropertyMetadata(typeof(SelectableTextBlock)));

        IsReadOnlyProperty.OverrideMetadata(
            typeof(SelectableTextBlock),
            new FrameworkPropertyMetadata(true));
    }
}
