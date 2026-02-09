using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Amethystra.Properties;

internal static class Definitions
{
    public const string XmlNamespace = "http://schemes.grabacr.net/winfx/2015/personal/controls";
    public const string XmlNamespacePrefix = "amethystra";
    public const string PackageUriAuthority = "pack://application:,,,";
    public static readonly string MainDictionaryUri = $"{PackageUriAuthority}/{ThisAssembly.Info.Title};component/Styles/Controls.xaml";
}
