using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Markup;

[assembly: AssemblyTitle("MetroTrilithon.Desktop")]
[assembly: AssemblyDescription("Utilities for Windows Desktop apps")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("grabacr.net")]
[assembly: AssemblyProduct("MetroTrilithon")]
[assembly: AssemblyCopyright("Copyright © 2015 Manato KAMEYA")]

[assembly: ComVisible(false)]
[assembly: Guid("4e2eb2e0-e5fe-4feb-a3e5-5f2f05cd1a67")]

[assembly: ThemeInfo(
	ResourceDictionaryLocation.None,
	ResourceDictionaryLocation.SourceAssembly)]

[assembly: XmlnsDefinition("http://schemes.grabacr.net/winfx/2015/personal/controls", "MetroTrilithon.UI.Controls")]
[assembly: XmlnsDefinition("http://schemes.grabacr.net/winfx/2015/personal/converters", "MetroTrilithon.UI.Converters")]
[assembly: XmlnsDefinition("http://schemes.grabacr.net/winfx/2015/personal/interactivity", "MetroTrilithon.UI.Interactivity")]

[assembly: AssemblyVersion("0.2.3")]
[assembly: AssemblyInformationalVersion("0.2.3")]
