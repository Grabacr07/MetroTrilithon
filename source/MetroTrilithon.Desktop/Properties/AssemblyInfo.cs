using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Markup;

// アセンブリに関する一般情報は以下の属性セットをとおして制御されます。
// アセンブリに関連付けられている情報を変更するには、
// これらの属性値を変更してください。
[assembly: AssemblyTitle("MetroTrilithon.Desktop")]
[assembly: AssemblyDescription("Utilities for Windows Desktop apps")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("grabacr.net")]
[assembly: AssemblyProduct("MetroTrilithon")]
[assembly: AssemblyCopyright("Copyright © 2015 Grabacr07")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// ComVisible を false に設定すると、その型はこのアセンブリ内で COM コンポーネントから
// 参照不可能になります。COM からこのアセンブリ内の型にアクセスする場合は、
// その型の ComVisible 属性を true に設定してください。
[assembly: ComVisible(false)]

[assembly: Guid("4e2eb2e0-e5fe-4feb-a3e5-5f2f05cd1a67")]

[assembly: ThemeInfo(
	// テーマ固有のリソース ディクショナリが置かれている場所
	// (リソースがページ、またはアプリケーション リソース ディクショナリに見つからない場合に使用されます)
	ResourceDictionaryLocation.None,

	// 汎用リソース ディクショナリが置かれている場所
	// (リソースがページ、アプリケーション、またはいずれのテーマ固有のリソース ディクショナリにも見つからない場合に使用されます)
	ResourceDictionaryLocation.SourceAssembly
)]

[assembly: XmlnsDefinition("http://schemes.grabacr.net/winfx/2015/personal/controls", "MetroTrilithon.Controls")]
[assembly: XmlnsDefinition("http://schemes.grabacr.net/winfx/2015/personal/converters", "MetroTrilithon.Converters")]
[assembly: XmlnsDefinition("http://schemes.grabacr.net/winfx/2015/personal/interactivity", "MetroTrilithon.Interactivity")]

// アセンブリのバージョン情報は、以下の 4 つの値で構成されています:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// すべての値を指定するか、下のように '*' を使ってビルドおよびリビジョン番号を 
// 既定値にすることができます:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("0.1.3.0")]
