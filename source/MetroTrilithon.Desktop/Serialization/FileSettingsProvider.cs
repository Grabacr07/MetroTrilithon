using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xaml;

namespace MetroTrilithon.Serialization
{
	public class FileSettingsProvider : ISerializationProvider
	{
		private readonly string _path;
		private readonly object _sync = new object();
		private Dictionary<string, object> _settings;

		public bool IsLoaded { get; private set; }

		public FileSettingsProvider(string path)
		{
			this._path = path;
		}


		public void SetValue<T>(string key, T value)
		{
			lock (this._sync)
			{
				this._settings[key] = value;
			}
		}

		public bool TryGetValue<T>(string key, out T value)
		{
			lock (this._sync)
			{
				object obj;
				if (this._settings.TryGetValue(key, out obj) && obj is T)
				{
					value = (T)obj;
					return true;
				}
			}

			value = default(T);
			return false;
		}

		public void Save()
		{
			lock (this._sync)
			{
				using (var stream = new FileStream(this._path, FileMode.Create, FileAccess.ReadWrite))
				{
					XamlServices.Save(stream, this._settings);
				}
			}
		}

		public void Load()
		{
			if (File.Exists(this._path))
			{
				using (var stream = new FileStream(this._path, FileMode.Open, FileAccess.Read))
				{
					lock (this._sync)
					{
						var source = XamlServices.Load(stream) as IDictionary<string, object>;
						this._settings = source == null
							? new Dictionary<string, object>()
							: new Dictionary<string, object>(source);
					}
				}
			}
			else
			{
				lock (this._sync)
				{
					this._settings = new Dictionary<string, object>();
				}
			}

			this.IsLoaded = true;
		}
	}
}
