using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xaml;

namespace MetroTrilithon.Serialization;

public class FileSettingsProvider : ISerializationProvider
{
    private readonly string _path;
    private readonly object _sync = new();
    private SortedDictionary<string, object?> _settings = new();

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
            if (this._settings.TryGetValue(key, out var obj) && obj is T v)
            {
                value = v;
                return true;
            }
        }

        value = default!;
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
        lock (this._sync)
        {
            if (this._settings.Count == 0) return;
        }

        var dir = Path.GetDirectoryName(this._path);
        if (dir == null) throw new DirectoryNotFoundException();

        if (Directory.Exists(dir) == false)
        {
            Directory.CreateDirectory(dir);
        }

        lock (this._sync)
        {
            using var stream = new FileStream(this._path, FileMode.Create, FileAccess.ReadWrite);
            XamlServices.Save(stream, this._settings);
        }
    }

    public void Load()
    {
        if (File.Exists(this._path))
        {
            using var stream = new FileStream(this._path, FileMode.Open, FileAccess.Read);
            lock (this._sync)
            {
                this._settings = XamlServices.Load(stream) is not IDictionary<string, object?> source
                    ? new SortedDictionary<string, object?>()
                    : new SortedDictionary<string, object?>(source);
            }
        }
        else
        {
            lock (this._sync)
            {
                this._settings = new SortedDictionary<string, object?>();
            }
        }

        this.IsLoaded = true;
    }

    event EventHandler ISerializationProvider.Reloaded
    {
        add
        {
        }
        remove
        {
        }
    }
}
