using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using MetroTrilithon.Properties;

[assembly: ComVisible(false)]
[assembly: Guid("4e2eb2e0-e5fe-4feb-a3e5-5f2f05cd1a67")]

[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]

[assembly: XmlnsPrefix(Definitions.XmlNamespace, Definitions.XmlNamespacePrefix)]
[assembly: XmlnsDefinition(Definitions.XmlNamespace, "MetroTrilithon.UI")]
[assembly: XmlnsDefinition(Definitions.XmlNamespace, "MetroTrilithon.UI.Controls")]
[assembly: XmlnsDefinition(Definitions.XmlNamespace, "MetroTrilithon.UI.Converters")]
[assembly: XmlnsDefinition(Definitions.XmlNamespace, "MetroTrilithon.UI.Interactivity")]
[assembly: XmlnsDefinition(Definitions.XmlNamespace, "MetroTrilithon.UI.Interop")]
[assembly: XmlnsDefinition(Definitions.XmlNamespace, "MetroTrilithon.UI.Markup")]

namespace MetroTrilithon.Properties;

internal static class AssemblyInfo
{
    private static readonly Assembly _assembly = Assembly.GetExecutingAssembly();
    private static string? _title;
    private static string? _description;
    private static string? _company;
    private static string? _product;
    private static string? _copyright;
    private static string? _trademark;
    private static string? _versionString;

    public static string Title
        => _title ??= Prop<AssemblyTitleAttribute>(x => x.Title);

    public static string Description
        => _description ??= Prop<AssemblyDescriptionAttribute>(x => x.Description);

    public static string Company
        => _company ??= Prop<AssemblyCompanyAttribute>(x => x.Company);

    public static string Product
        => _product ??= Prop<AssemblyProductAttribute>(x => x.Product);

    public static string Copyright
        => _copyright ??= Prop<AssemblyCopyrightAttribute>(x => x.Copyright);

    public static string Trademark
        => _trademark ??= Prop<AssemblyTrademarkAttribute>(x => x.Trademark);

    public static Version Version
        => _assembly.GetName().Version ?? new Version();

    public static string VersionString
        => _versionString ??= Version.ToString(3);

    private static string Prop<T>(Func<T, string> propSelector)
        where T : Attribute
    {
        var attribute = _assembly.GetCustomAttribute<T>();
        return attribute != null ? propSelector(attribute) : "";
    }
}
