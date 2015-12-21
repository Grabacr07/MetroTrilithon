using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetroTrilithon.Serialization
{
	public abstract class DictionaryProviderBase<TDictionary> : ISerializationProvider where TDictionary : class, IDictionary<string, object>, new()
	{
		private readonly object _sync = new object();
		private TDictionary _settings = new TDictionary();

		public bool IsLoaded { get; private set; }

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

		public bool RemoveValue(string key)
		{
			lock (this._sync)
			{
				return this._settings.Remove(key);
			}
		}


		public void Save()
		{
			if (this._settings.Count == 0) return;

			lock (this._sync)
			{
				this.SaveCore(this._settings);
			}
		}

		protected abstract void SaveCore(TDictionary settings);

		public void Load()
		{
			lock (this._sync)
			{
				this._settings = this.LoadCore() ?? new TDictionary();
			}

			this.IsLoaded = true;
		}

		protected abstract TDictionary LoadCore();
	}
}
