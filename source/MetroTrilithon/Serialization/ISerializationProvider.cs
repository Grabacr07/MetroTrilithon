using System;
using System.Collections.Generic;
using System.Linq;

namespace MetroTrilithon.Serialization
{
	public interface ISerializationProvider
	{
		bool IsLoaded { get; }

		void Save();

		void Load();

		void SetValue<T>(string key, T value);

		bool TryGetValue<T>(string key, out T value);

		bool RemoveValue(string key);
	}
}
