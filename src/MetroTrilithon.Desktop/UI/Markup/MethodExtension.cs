using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Markup;
using MetroTrilithon.UI.Interactivity;

namespace MetroTrilithon.UI.Markup;

[MarkupExtensionReturnType(typeof(CallMethodInfo))]
public class MethodExtension(string name) : MarkupExtension
{
    public string Name { get; set; } = name;

    public object? Parameter { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider)
        => new CallMethodInfo(this.Name, this.Parameter, null);
}
