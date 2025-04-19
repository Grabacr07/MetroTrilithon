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
public abstract class ReactiveSettingsBase : IDisposable
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
    private readonly IReadOnlyCollection<PropertyInfo> _reactiveProperties;
    private readonly Subject<LoadReason> _load = new();
    private readonly Subject<SaveReason> _save = new();
    private readonly CompositeDisposable _disposables = [];
    private readonly ScopedFlag _ignoreChangesFromLoad = new();
    private readonly bool _isInitialized;
    private long _lastSaveTimestamp;

    protected virtual bool AutoSave
        => true;

    /// <summary>
    /// Gets the time span to ignore file system changes that are caused by the program itself.
    /// </summary>
    protected virtual TimeSpan SelfChangeIgnoreSpan
        => TimeSpan.FromMilliseconds(500);

    protected ReactiveSettingsBase(FilePath settingsFilePath)
        : this(settingsFilePath, null, null)
    {
    }

    protected ReactiveSettingsBase(FilePath settingsFilePath, string? settingsSectionName, IScheduler? scheduler)
    {
        this._settingsFilePath = settingsFilePath;
        this._settingsSectionName = settingsSectionName ?? this.GetType().Name;
        this._reactiveProperties = this.SubscribeToReactiveProperties();

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
            .Where(_ => this._isInitialized)
            .Throttle(TimeSpan.FromMilliseconds(1000))
            .ObserveOn(scheduler ?? TaskPoolScheduler.Default)
            .Subscribe(this.LoadSettings)
            .AddTo(this._disposables);

        this._save
            .Where(_ => this._isInitialized)
            .Throttle(TimeSpan.FromMilliseconds(1000))
            .ObserveOn(scheduler ?? TaskPoolScheduler.Default)
            .Subscribe(this.SaveSettings)
            .AddTo(this._disposables);

        this._isInitialized = true;
        this.LoadSettings(LoadReason.Initialize);
    }

    public virtual void Load()
    {
        this._load.OnNext(LoadReason.Explicit);
    }

    public virtual void Save()
    {
        this._save.OnNext(SaveReason.Explicit);
    }

    private PropertyInfo[] SubscribeToReactiveProperties()
    {
        var list = new List<PropertyInfo>();
        var methodInfo = typeof(ReactiveSettingsBase).GetMethod(nameof(this.HandleReactivePropertyChanged), BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Method not found: {nameof(this.HandleReactivePropertyChanged)}");

        foreach (var prop in this.GetType()
                     .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                     .Where(prop =>
                         prop.PropertyType.IsGenericType &&
                         prop.PropertyType.GetGenericTypeDefinition() == typeof(IReactiveProperty<>)))
        {
            var reactiveProp = prop.GetValue(this);
            if (reactiveProp == null) continue;

            var valueType = prop.PropertyType.GetGenericArguments()[0];
            var genericMethod = methodInfo.MakeGenericMethod(valueType);
            var actionType = typeof(Action<>).MakeGenericType(valueType);
            var actionDelegate = Delegate.CreateDelegate(actionType, this, genericMethod);
            var subscribeMethod = typeof(ObservableExtensions)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m.Name == nameof(ObservableExtensions.Subscribe) && m.GetParameters().Length == 2)
                .MakeGenericMethod(valueType);

            if (subscribeMethod.Invoke(null, [reactiveProp, actionDelegate]) is IDisposable disposable)
            {
                this._disposables.Add(disposable);
            }

            list.Add(prop);
        }

        return [.. list];
    }

    private void HandleReactivePropertyChanged<T>(T _)
    {
        if (this._ignoreChangesFromLoad || this._isInitialized == false || this.AutoSave == false)
        {
            return;
        }

        this._save.OnNext(SaveReason.PropertyChanged);
    }

    /// <summary>
    /// Determines whether the received file system change event is caused by an external source or by the program itself.
    /// </summary>
    private bool IsExternalFileChange(Timestamped<EventPattern<FileSystemEventArgs>> args)
    {
        var lastSaveMs = Interlocked.Read(ref this._lastSaveTimestamp);
        var eventMs = args.Timestamp.ToUnixTimeMilliseconds();
        var deltaMs = eventMs - lastSaveMs;
        var result = deltaMs > this.SelfChangeIgnoreSpan.TotalMilliseconds;

        Debug.WriteLine($"📄FileSystemWatcher.Changed: {args.Value.EventArgs.FullPath} \n └{(result ? "✅Pass" : "❌Ignore")} (Timestamp={args.Timestamp:HH:mm:ss.ffff}, LastSave={DateTimeOffset.FromUnixTimeMilliseconds(lastSaveMs):HH:mm:ss.ffff}, Δ={deltaMs}ms)");

        return result;
    }

    private void LoadSettings(LoadReason reason)
    {
        try
        {
            if (this._settingsFilePath.Exists() == false) return;

            var json = this._settingsFilePath.ReadAllText();
            var outerDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, _jsonSerializerOptions);
            if (outerDict == null || outerDict.TryGetValue(this._settingsSectionName, out var sectionElement) == false) return;

            var sectionDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(sectionElement.GetRawText(), _jsonSerializerOptions);
            if (sectionDict == null) return;

            foreach (var prop in this._reactiveProperties)
            {
                if (sectionDict.TryGetValue(prop.Name, out var element) == false) continue;

                var valueType = prop.PropertyType.GetGenericArguments()[0];
                var convertedValue = JsonSerializer.Deserialize(element.GetRawText(), valueType, _jsonSerializerOptions);
                var reactiveProp = prop.GetValue(this);
                if (reactiveProp == null) continue;

                var valueProperty = reactiveProp.GetType().GetProperty("Value");
                if (valueProperty == null) continue;

                using (this._ignoreChangesFromLoad.Enable())
                {
                    valueProperty.SetValue(reactiveProp, convertedValue);
                }
            }

            Debug.WriteLine($"📄{nameof(this.LoadSettings)}({reason}): {this._settingsFilePath.AsDestructive().FullName} \n └✅Success");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"📄{nameof(this.LoadSettings)}({reason}): {this._settingsFilePath.AsDestructive().FullName} \n └❌Error: {ex}");
        }
    }

    /// <summary>
    /// Overwrites the configuration file's section specified by <see cref="_settingsSectionName"/>
    /// with the current <see cref="IReactiveProperty{T}"/> values, preserving the other sections.
    /// </summary>
    private void SaveSettings(SaveReason reason)
    {
        try
        {
            var sectionDictionary = new Dictionary<string, object?>();

            foreach (var prop in this._reactiveProperties)
            {
                var reactiveProp = prop.GetValue(this);
                if (reactiveProp == null) continue;

                var valueProperty = reactiveProp.GetType().GetProperty("Value");
                if (valueProperty == null) continue;

                var currentValue = valueProperty.GetValue(reactiveProp);
                sectionDictionary[prop.Name] = currentValue;
            }

            var outerDictionary = this._settingsFilePath.Exists()
                ? JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(this._settingsFilePath.ReadAllText(), _jsonSerializerOptions)
                    ?.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value) ?? []
                : [];
            outerDictionary[this._settingsSectionName] = sectionDictionary;

            var newJson = JsonSerializer.Serialize(outerDictionary, _jsonSerializerOptions);

            Interlocked.Exchange(ref this._lastSaveTimestamp, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            this._settingsFilePath.AsDestructive().Write(newJson);

            Debug.WriteLine($"📄{nameof(this.SaveSettings)}({reason}): {this._settingsFilePath.AsDestructive().FullName} \n └✅Success");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"📄{nameof(this.SaveSettings)}({reason}): {this._settingsFilePath.AsDestructive().FullName} \n └❌Error: {ex}");
        }
    }

    public virtual void Dispose()
    {
        this._disposables.Dispose();
        GC.SuppressFinalize(this);
    }
}
