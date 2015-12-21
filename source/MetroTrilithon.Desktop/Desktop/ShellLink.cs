using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using MetroTrilithon.Desktop.Interop;

namespace MetroTrilithon.Desktop
{
	public static class ShellLink
	{
		public static void Create(string path)
		{
			var type = Type.GetTypeFromCLSID(CLSID.ShellLink);
			var psl = (IShellLink)Activator.CreateInstance(type);

			psl.SetPath(Assembly.GetExecutingAssembly().Location);

			// ReSharper disable once SuspiciousTypeConversion.Global
			var ppf = (IPersistFile)psl;

			ppf.Save(path, false);
		}
	}
}
