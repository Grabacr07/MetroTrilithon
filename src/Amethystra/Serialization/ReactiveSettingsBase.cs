using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Amethystra.Diagnostics;
using Amethystra.Disposables;
using Mio;
using Mio.Destructive;
using R3;

namespace Amethystra.Serialization;

/// <summary>
/// Supports reading and writing settings values exposed by the derived type as <see cref="ReactiveProperty{T}"/>
/// in JSON format.
/// </summary>
[GenerateLogger]
public abstract partial class ReactiveSettingsBase : IDisposable
{
    private enum LoadReason
    {
        Initialize,
        FileSystemWatcher,
        Explicit,
    }

    private enum SaveReason
    {
        PropertyChanged,
        Explicit,
    }

    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly FilePath _settingsFilePath;
    private readonly string _settingsSectionName;
    private readonly IReadOnlyCollection<IPropertyBroker> _propertyBrokers;
    private readonly ReactiveProperty<bool> _isInitialized = new();
    private readonly Subject<LoadReason> _load = new();
    private readonly Subject<SaveReason> _save = new();
    private readonly ScopedFlag _ignoreChangesFromLoad = new();
    private readonly DisposeGate<ReactiveSettingsBase> _disposeGate = new();
    private long _lastSaveTimestamp;

    protected virtual bool AutoSave
        => true;

    /// <summary>
    /// Gets the interval for throttling multiple settings load calls.
    /// </summary>
    protected virtual TimeSpan LoadThrottlingDuration
        => TimeSpan.FromMilliseconds(300);

    /// <summary>
    /// Gets the interval for throttling multiple settings save calls.
    /// </summary>
    protected virtual TimeSpan SaveThrottlingDuration
        => TimeSpan.FromMilliseconds(300);

    /// <summary>
    /// Gets the time span to ignore file system changes that are caused by the program itself.
    /// </summary>
    protected virtual TimeSpan SelfChangeDuration
        => TimeSpan.FromMilliseconds(1200);

    public ReadOnlyReactiveProperty<bool> IsInitialized { get; }

    protected ReactiveSettingsBase(FilePath settingsFilePath)
        : this(settingsFilePath, null, null)
    {
    }

    protected ReactiveSettingsBase(FilePath settingsFilePath, string? settingsSectionName, TimeProvider? timeProvider)
    {
        this._settingsFilePath = settingsFilePath;
        this._settingsSectionName = settingsSectionName ?? this.GetType().Name;
        this._propertyBrokers = [.. EnumerateBrokers(this)];
        this.IsInitialized = this._isInitialized.ToReadOnlyReactiveProperty();

        var effectiveTimeProvider = timeProvider ?? TimeProvider.System;

        var watcher = new FileSystemWatcher(this._settingsFilePath.Parent.AsDestructive().FullName, this._settingsFilePath.Name)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
                EnableRaisingEvents = true,
            }
            .AddTo(this._disposeGate);

        var fileSystemChanges = Observable
            .FromEvent<FileSystemEventHandler, FileSystemEventArgs>(
                handler => (_, args) => handler(args),
                h => watcher.Changed += h,
                h => watcher.Changed -= h)
            .Select(static _ => (reason: LoadReason.FileSystemWatcher, timestamp: DateTimeOffset.UtcNow))
            .Where(this.IsExternalFileChange)
            .Select(static x => x.reason);

        // Initialize は即時通過させ、それ以外は仮想プロパティを遅延評価しつつ Debounce する
        var debouncedLoads = this._load
            .Where(static x => x != LoadReason.Initialize)
            .Merge(fileSystemChanges)
            .Select(x => Observable.Timer(this.LoadThrottlingDuration, effectiveTimeProvider).Select(_ => x))
            .Switch();

        this._load
            .Where(static x => x == LoadReason.Initialize)
            .Merge(debouncedLoads)
            .SubscribeAwait(async (reason, _) => await this.LoadSettingsAsync(reason))
            .AddTo(this._disposeGate);

        this._save
            .Select(x => Observable.Timer(this.SaveThrottlingDuration, effectiveTimeProvider).Select(_ => x))
            .Switch()
            .SubscribeAwait(async (reason, _) => await this.SaveSettingsAsync(reason), AwaitOperation.Drop)
            .AddTo(this._disposeGate);

        this._load.OnNext(LoadReason.Initialize);
    }

    public async Task EnsureLoadedAsync(CancellationToken ct = default)
        => await this._isInitialized
            .Where(static x => x)
            .FirstAsync(ct)
            .ConfigureAwait(false);

    public virtual void Load()
    {
        this._disposeGate.ThrowIfDisposed();
        this._load.OnNext(LoadReason.Explicit);
    }

    public virtual Task LoadAsync()
    {
        this._disposeGate.ThrowIfDisposed();
        return this.LoadSettingsAsync(LoadReason.Explicit);
    }

    public virtual void Save()
    {
        this._disposeGate.ThrowIfDisposed();
        this._save.OnNext(SaveReason.Explicit);
    }

    public virtual Task SaveAsync()
    {
        this._disposeGate.ThrowIfDisposed();
        return this.SaveSettingsAsync(SaveReason.Explicit);
    }

    /// <summary>
    /// Determines whether the received file system change event is caused by an external source or by the program itself.
    /// </summary>
    private bool IsExternalFileChange((LoadReason reason, DateTimeOffset timestamp) x)
    {
        var lastSaveMs = Interlocked.Read(ref this._lastSaveTimestamp);
        var eventMs = x.timestamp.ToUnixTimeMilliseconds();
        var deltaMs = eventMs - lastSaveMs;
        var result = deltaMs > this.SelfChangeDuration.TotalMilliseconds;

        Log.Debug(
            result ? "✅Pass" : "❌Ignore",
            new() { { this._settingsFilePath.Name, "path" }, { $"{x.timestamp:HH:mm:ss.fff}", "timestamp" }, { $"{DateTimeOffset.FromUnixTimeMilliseconds(lastSaveMs):HH:mm:ss.fff}", "lastSave" }, { $"{TimeSpan.FromMilliseconds(deltaMs):g}", "Δ" } });
        return result;
    }

    private async Task LoadSettingsAsync(LoadReason reason)
    {
        try
        {
            if (this._settingsFilePath.Exists() == false) return;

            var json = await this._settingsFilePath.ReadAllTextAsync().ConfigureAwait(false);
            var outerDictionary = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, _jsonSerializerOptions);
            if (outerDictionary == null || outerDictionary.TryGetValue(this._settingsSectionName, out var sectionElement) == false) return;

            var sectionDictionary = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(sectionElement.GetRawText(), _jsonSerializerOptions);
            if (sectionDictionary == null) return;

            foreach (var broker in this._propertyBrokers)
            {
                if (sectionDictionary.TryGetValue(broker.SerializedPropertyName, out var element) == false) continue;

                var convertedValue = JsonSerializer.Deserialize(element.GetRawText(), broker.ValueType, _jsonSerializerOptions);
                using (this._ignoreChangesFromLoad.Enable())
                {
                    broker.Value = convertedValue;
                }
            }

            Log.Info("✅Success", new() { reason, { this._settingsFilePath.AsDestructive().FullName, "fullname" } });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "❌Error", new() { reason, { this._settingsFilePath.AsDestructive().FullName, "fullname" } });
        }
        finally
        {
            if (reason == LoadReason.Initialize)
            {
                this._isInitialized.Value = true;
            }
        }
    }

    /// <summary>
    /// Overwrites the configuration file's section specified by <see cref="_settingsSectionName"/>
    /// with the current <see cref="ReactiveProperty{T}"/> values, preserving the other sections.
    /// </summary>
    private async Task SaveSettingsAsync(SaveReason reason)
    {
        try
        {
            var sectionDictionary = this._propertyBrokers
                .Where(x => Equals(x.Value, x.DefaultValue) == false)
                .ToDictionary(static x => x.SerializedPropertyName, x => x.Value);
            var outerDictionary = this._settingsFilePath.Exists()
                ? JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(await this._settingsFilePath.ReadAllTextAsync().ConfigureAwait(false), _jsonSerializerOptions) ?? []
                : [];
            outerDictionary[this._settingsSectionName] = JsonSerializer.SerializeToElement(sectionDictionary, _jsonSerializerOptions);

            var newJson = JsonSerializer.Serialize(outerDictionary, _jsonSerializerOptions);

            Interlocked.Exchange(ref this._lastSaveTimestamp, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            await this._settingsFilePath.AsDestructive().WriteAsync(newJson).ConfigureAwait(false);

            Log.Info("✅Success", new() { reason, { this._settingsFilePath.AsDestructive().FullName, "fullname" } });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "❌Error", new() { reason, { this._settingsFilePath.AsDestructive().FullName, "fullname" } });
        }
    }

    public virtual void Dispose()
    {
        if (this._disposeGate.TryDispose())
        {
            this._propertyBrokers.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
