using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using MetroTrilithon.Utils;
using Mio;
using Mio.Destructive;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace MetroTrilithon.Serialization;

/// <summary>  
/// Supports reading and writing settings values exposed by the derived type as <see cref="IReactiveProperty{T}"/>  
/// in JSON format.  
/// </summary>
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
    };

    private readonly FilePath _settingsFilePath;
    private readonly string _settingsSectionName;
    private readonly IReadOnlyCollection<ReactivePropertyBroker> _propertyBrokers;
    private readonly Subject<LoadReason> _load = new();
    private readonly Subject<SaveReason> _save = new();
    private readonly CompositeDisposable _disposables = [];
    private readonly ScopedFlag _ignoreChangesFromLoad = new();
    private long _lastSaveTimestamp;

    protected virtual bool AutoSave
        => true;

    /// <summary>
    /// Gets the interval for throttling multiple settings load calls.
    /// </summary>
    protected virtual TimeSpan LoadThrottlingDuration
        => TimeSpan.FromMilliseconds(1000);

    /// <summary>
    /// Gets the interval for throttling multiple settings save calls.
    /// </summary>
    protected virtual TimeSpan SaveThrottlingDuration
        => TimeSpan.FromMilliseconds(1000);

    /// <summary>
    /// Gets the time span to ignore file system changes that are caused by the program itself.
    /// </summary>
    protected virtual TimeSpan SelfChangeDuration
        => TimeSpan.FromMilliseconds(500);

    protected ReactiveSettingsBase(FilePath settingsFilePath)
        : this(settingsFilePath, null, null)
    {
    }

    protected ReactiveSettingsBase(FilePath settingsFilePath, string? settingsSectionName, IScheduler? scheduler)
    {
        this._settingsFilePath = settingsFilePath;
        this._settingsSectionName = settingsSectionName ?? this.GetType().Name;
        this._propertyBrokers = this.SubscribeToReactiveProperties();

        var watcher = new FileSystemWatcher(this._settingsFilePath.Parent.AsDestructive().FullName, this._settingsFilePath.Name)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
                EnableRaisingEvents = true,
            }
            .AddTo(this._disposables);

        this._load
            .Merge(Observable
                .FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                    h => watcher.Changed += h,
                    h => watcher.Changed -= h)
                .Timestamp()
                // ↑ 発生時刻を付与
                // ↓ 自分の書き込みから一定時間が経過していれば通す
                .Where(this.IsExternalFileChange)
                .Select(_ => LoadReason.FileSystemWatcher))
            .Throttle(x => x == LoadReason.Initialize
                ? Observable.Empty<long>()
                : Observable.Timer(this.LoadThrottlingDuration))
            .ObserveOn(scheduler ?? TaskPoolScheduler.Default)
            .SelectMany(reason => Observable.FromAsync(() => this.LoadSettingsAsync(reason)))
            .Subscribe()
            .AddTo(this._disposables);

        this._save
            .Throttle(_ => Observable.Timer(this.SaveThrottlingDuration))
            .ObserveOn(scheduler ?? TaskPoolScheduler.Default)
            .SelectMany(reason => Observable.FromAsync(() => this.SaveSettingsAsync(reason)))
            .Subscribe()
            .AddTo(this._disposables);

        this._load.OnNext(LoadReason.Initialize);
    }

    public virtual void Load()
    {
        this._load.OnNext(LoadReason.Explicit);
    }

    public virtual void Save()
    {
        this._save.OnNext(SaveReason.Explicit);
    }

    private ReactivePropertyBroker[] SubscribeToReactiveProperties()
    {
        var list = new List<ReactivePropertyBroker>();
        var registerMethodInfo = typeof(ReactiveSettingsBase).GetMethod(nameof(this.RegisterPropertyChanged), BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Method not found: {nameof(this.RegisterPropertyChanged)}");

        foreach (var broker in this.GetType()
                     .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                     .Where(prop =>
                         prop.PropertyType.IsGenericType &&
                         prop.PropertyType.GetGenericTypeDefinition() == typeof(IReactiveProperty<>))
                     .Select(x => new ReactivePropertyBroker(this, x)))
        {
            var registerGenericMethodInfo = registerMethodInfo.MakeGenericMethod(broker.ValueType);
            if (registerGenericMethodInfo.Invoke(this, [broker.PropertyInstance, broker.PropertyName]) is IDisposable disposable)
            {
                this._disposables.Add(disposable);
            }

            list.Add(broker);
        }

        return [.. list];
    }

    private IDisposable RegisterPropertyChanged<T>(IReactiveProperty<T> property, string propertyName)
        => property
            .Where(_ => this._ignoreChangesFromLoad == false)
            .Subscribe(newValue =>
            {
                Debug.WriteLine($"🔔PropertyChanged ({propertyName}): {newValue}");
                if (this.AutoSave) this._save.OnNext(SaveReason.PropertyChanged);
            });

    /// <summary>
    /// Determines whether the received file system change event is caused by an external source or by the program itself.
    /// </summary>
    private bool IsExternalFileChange(Timestamped<EventPattern<FileSystemEventArgs>> args)
    {
        var lastSaveMs = Interlocked.Read(ref this._lastSaveTimestamp);
        var eventMs = args.Timestamp.ToUnixTimeMilliseconds();
        var deltaMs = eventMs - lastSaveMs;
        var result = deltaMs > this.SelfChangeDuration.TotalMilliseconds;

        Debug.WriteLine($"👀FileSystemWatcher.Changed: {args.Value.EventArgs.FullPath} \n └{(result ? "✅Pass" : "❌Ignore")} (Timestamp={args.Timestamp:HH:mm:ss.fff}, LastSave={DateTimeOffset.FromUnixTimeMilliseconds(lastSaveMs):HH:mm:ss.fff}, Δ={TimeSpan.FromMilliseconds(deltaMs):g})");

        return result;
    }

    private async Task LoadSettingsAsync(LoadReason reason)
    {
        try
        {
            if (this._settingsFilePath.Exists() == false) return;

            var json = await this._settingsFilePath.ReadAllTextAsync();
            var outerDictionary = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, _jsonSerializerOptions);
            if (outerDictionary == null || outerDictionary.TryGetValue(this._settingsSectionName, out var sectionElement) == false) return;

            var sectionDictionary = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(sectionElement.GetRawText(), _jsonSerializerOptions);
            if (sectionDictionary == null) return;

            foreach (var broker in this._propertyBrokers)
            {
                if (sectionDictionary.TryGetValue(broker.PropertyName, out var element) == false) continue;

                var convertedValue = JsonSerializer.Deserialize(element.GetRawText(), broker.ValueType, _jsonSerializerOptions);
                using (this._ignoreChangesFromLoad.Enable())
                {
                    broker.CurrentValue = convertedValue;
                }
            }

            Debug.WriteLine($"📄{nameof(this.LoadSettingsAsync)} ({reason}): {this._settingsFilePath.AsDestructive().FullName} \n └✅Success");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"📄{nameof(this.LoadSettingsAsync)} ({reason}): {this._settingsFilePath.AsDestructive().FullName} \n └❌Error: {ex}");
        }
    }

    /// <summary>
    /// Overwrites the configuration file's section specified by <see cref="_settingsSectionName"/>
    /// with the current <see cref="IReactiveProperty{T}"/> values, preserving the other sections.
    /// </summary>
    private async Task SaveSettingsAsync(SaveReason reason)
    {
        try
        {
            var sectionDictionary = this._propertyBrokers.ToDictionary(x => x.PropertyName, x => x.CurrentValue);
            var outerDictionary = this._settingsFilePath.Exists()
                ? JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(await this._settingsFilePath.ReadAllTextAsync(), _jsonSerializerOptions) ?? []
                : [];
            outerDictionary[this._settingsSectionName] = JsonSerializer.SerializeToElement(sectionDictionary, _jsonSerializerOptions);

            var newJson = JsonSerializer.Serialize(outerDictionary, _jsonSerializerOptions);

            Interlocked.Exchange(ref this._lastSaveTimestamp, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            await this._settingsFilePath.AsDestructive().WriteAsync(newJson);

            Debug.WriteLine($"💾{nameof(this.SaveSettingsAsync)} ({reason}): {this._settingsFilePath.AsDestructive().FullName} \n └✅Success");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"💾{nameof(this.SaveSettingsAsync)} ({reason}): {this._settingsFilePath.AsDestructive().FullName} \n └❌Error: {ex}");
        }
    }

    public virtual void Dispose()
    {
        this._disposables.Dispose();
        GC.SuppressFinalize(this);
    }
}
