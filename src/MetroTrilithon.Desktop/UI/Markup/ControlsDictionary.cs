using System.Windows;
using System.Windows.Markup;
using MetroTrilithon.Properties;

namespace MetroTrilithon.UI.Markup;

[Ambient]
[UsableDuringInitialization(true)]
[Localizability(LocalizationCategory.Ignore)]
public class ControlsDictionary : ResourceDictionary
{
    public ControlsDictionary()
    {
        this.Source = new Uri(Definitions.MainDictionaryUri);
    }
}
