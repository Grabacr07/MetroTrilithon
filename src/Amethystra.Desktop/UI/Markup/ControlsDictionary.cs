using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using Amethystra.Properties;

namespace Amethystra.UI.Markup;

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
