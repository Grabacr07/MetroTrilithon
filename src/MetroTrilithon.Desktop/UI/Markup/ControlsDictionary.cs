using System.Windows;
using System.Windows.Markup;

namespace MetroTrilithon.UI.Markup;

[Ambient]
[UsableDuringInitialization(true)]
[Localizability(LocalizationCategory.Ignore)]
public class ControlsDictionary : ResourceDictionary
{
    public ControlsDictionary()
    {
        this.Source = new Uri("pack://application:,,,/MetroTrilithon.Desktop;component/Styles/Controls.xaml");
    }
}
