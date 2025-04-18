using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Mio;
using Mio.Destructive;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace MetroTrilithon.Serialization;

public abstract class ReactiveSettingsBase : IDisposable
{
    private static readonly ConcurrentDictionary<string, FileSystemWatcher> _watcherCache = new();
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly FilePath _settingsFilePath;
    private readonly string _settingsSectionName;
    private readonly IReadOnlyCollection<PropertyInfo> _reactiveProperties;
    private readonly Subject<Unit> _save = new();
    private readonly Subject<Unit> _load = new();
    private readonly CompositeDisposable _disposables = [];
    private readonly bool _isInitialized;
    private bool _ignoreFileChanges;

    protected virtual bool AutoSave
        => true;

    protected ReactiveSettingsBase(FilePath settingsFilePath)
        : this(settingsFilePath, null)
    {
    }

    protected ReactiveSettingsBase(FilePath settingsFilePath, string? settingsSectionName)
    {
        this._settingsFilePath = settingsFilePath;
        this._settingsSectionName = settingsSectionName ?? this.GetType().Name;
        this._reactiveProperties = this.SubscribeToReactiveProperties();

        this._save
            .Where(_ => this._isInitialized)
            .Throttle(TimeSpan.FromMilliseconds(1000))
            .ObserveOn(Scheduler.Default)
            .Subscribe(_ => this.SaveSettings())
            .AddTo(this._disposables);
        this._load
            .Where(_ => this._isInitialized)
            .Throttle(TimeSpan.FromMilliseconds(1000))
            .ObserveOn(Scheduler.Default)
            .Subscribe(_ => this.LoadSettings())
            .AddTo(this._disposables);

        var watcher = _watcherCache.GetOrAdd(
            this._settingsFilePath.AsDestructive().FullName,
            _ => new FileSystemWatcher(
                this._settingsFilePath.Parent.AsDestructive().FullName,
                this._settingsFilePath.Name)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
                EnableRaisingEvents = true,
            });

        watcher.Changed += this.HandleFileChanged;
        this._disposables.Add(Disposable.Create(() => watcher.Changed -= this.HandleFileChanged));
        this._isInitialized = true;

        this.LoadSettings();
    }

    public virtual void Load()
    {
        this._load.OnNext(Unit.Default);
    }

    public virtual void Save()
    {
        this._save.OnNext(Unit.Default);
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
        if (this.AutoSave == false) return;

        this._save.OnNext(Unit.Default);
    }

    private void HandleFileChanged(object sender, FileSystemEventArgs e)
    {
        if (this._ignoreFileChanges) return;

        this._load.OnNext(Unit.Default);
    }

    private void LoadSettings()
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

                valueProperty.SetValue(reactiveProp, convertedValue);
            }

            Console.WriteLine("🔧LoadSettings\n✅Success");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🔧LoadSettings\n❌Error: {ex}");
        }
    }

    /// <summary>
    /// 現在の <see cref="IReactiveProperty{T}"/> 値を、<see cref="_settingsSectionName"/> で指定したセクションで設定ファイルに上書き保存します。他のセクションは保持します。
    /// </summary>
    private void SaveSettings()
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

            this._ignoreFileChanges = true;
            this._settingsFilePath.AsDestructive().Write(newJson);
            this._ignoreFileChanges = false;

            Console.WriteLine("🔧SaveSettings\n✅Success");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🔧SaveSettings\n❌Error: {ex}");
        }
    }

    public virtual void Dispose()
    {
        this._disposables.Dispose();
        GC.SuppressFinalize(this);
    }
}
