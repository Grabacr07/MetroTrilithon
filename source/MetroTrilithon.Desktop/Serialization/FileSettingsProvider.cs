using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xaml;

namespace MetroTrilithon.Serialization
{
	public class FileSettingsProvider : DictionaryProviderBase<SortedDictionary<string, object>>
	{
		private readonly string _path;

		public FileSettingsProvider(string path)
		{
			this._path = path;
		}

		protected override void SaveCore(SortedDictionary<string, object> settings)
		{
			var dir = Path.GetDirectoryName(this._path);
			if (dir == null) throw new DirectoryNotFoundException();

			if (!Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
			}

			using (var stream = new FileStream(this._path, FileMode.Create, FileAccess.ReadWrite))
			{
				XamlServices.Save(stream, settings);
			}
		}

		protected override SortedDictionary<string, object> LoadCore()
		{
			if (File.Exists(this._path))
			{
				using (var stream = new FileStream(this._path, FileMode.Open, FileAccess.Read))
				{
					var source = XamlServices.Load(stream) as IDictionary<string, object>;
					return source == null
						? new SortedDictionary<string, object>()
						: new SortedDictionary<string, object>(source);
				}
			}

			return new SortedDictionary<string, object>();
		}
	}
}
