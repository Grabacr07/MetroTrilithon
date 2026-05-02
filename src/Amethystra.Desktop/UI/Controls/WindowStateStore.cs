using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Amethystra.Diagnostics;
using Mio;
using Mio.Destructive;
using R3;

namespace Amethystra.UI.Controls;

[GenerateLogger]
public partial class WindowStateStore : IDisposable
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly FilePath _filePath;
    private Dictionary<string, PersistedWindowState?> _states = [];
    private readonly Subject<Unit> _saveRequested = new();
    private readonly IDisposable _saveSubscription;
    private bool _disposed;

    public WindowStateStore(FilePath filePath)
    {
        this._filePath = filePath;
        this.LoadSync();

        this._saveSubscription = this._saveRequested
            .Select(_ => Observable.Timer(TimeSpan.FromMilliseconds(500)))
            .Switch()
            .SubscribeAwait(async (_, ct) => await this.SaveAsync(ct), AwaitOperation.Drop);
    }

    public PersistedWindowState? GetState(string key)
        => this._states.GetValueOrDefault(key);

    public PersistedWindowState? GetState<TWindow>()
        => this.GetState(typeof(TWindow).Name);

    public PersistedWindowState? GetState<TWindow>(string instanceKey)
        => this.GetState($"{typeof(TWindow).Name}:{instanceKey}");

    public void SetState(string key, PersistedWindowState? state)
    {
        this._states[key] = state;
        this._saveRequested.OnNext(Unit.Default);
    }

    public void SetState<TWindow>(PersistedWindowState? state)
        => this.SetState(typeof(TWindow).Name, state);

    public void SetState<TWindow>(string instanceKey, PersistedWindowState? state)
        => this.SetState($"{typeof(TWindow).Name}:{instanceKey}", state);

    private void LoadSync()
    {
        try
        {
            if (this._filePath.Exists() == false) return;

            var json = File.ReadAllText(this._filePath.AsDestructive().FullName);
            var dict = JsonSerializer.Deserialize<Dictionary<string, PersistedWindowState?>>(json, _jsonOptions);
            if (dict is not null) this._states = dict;

            Log.Info("✅Success", new() { { this._filePath.Name, "file" } });
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "❌Error", new() { { this._filePath.Name, "file" } });
        }
    }

    private async Task SaveAsync(CancellationToken ct = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(this._states, _jsonOptions);
            await this._filePath.AsDestructive().WriteAsync(json, cancellationToken: ct).ConfigureAwait(false);

            Log.Info("✅Success", new() { { this._filePath.Name, "file" } });
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "❌Error", new() { { this._filePath.Name, "file" } });
        }
    }

    public void Dispose()
    {
        if (this._disposed) return;

        this._disposed = true;
        this._saveSubscription.Dispose();
        this._saveRequested.Dispose();
        
        GC.SuppressFinalize(this);
    }
}
