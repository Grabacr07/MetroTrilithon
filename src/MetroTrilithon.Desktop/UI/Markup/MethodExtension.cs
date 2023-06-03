using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Markup;
using MetroTrilithon.UI.Interactivity;

namespace MetroTrilithon.UI.Markup;

[MarkupExtensionReturnType(typeof(CallMethodInfo))]
public class MethodExtension : MarkupExtension
{
    public string Name { get; set; }

    public object? Parameter { get; set; }
    
    public MethodExtension(string name)
    {
        this.Name = name;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
        => new CallMethodInfo(this.Name, this.Parameter, null);
}
