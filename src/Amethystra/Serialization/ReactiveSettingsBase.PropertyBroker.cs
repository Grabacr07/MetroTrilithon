using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using R3;

namespace Amethystra.Serialization;

partial class ReactiveSettingsBase
{
    private interface IPropertyBroker : IDisposable
    {
        string SerializedPropertyName { get; }

        Type ValueType { get; }

        object? Value { get; set; }

        object? DefaultValue { get; }
    }

    private sealed class ReactivePropertyBroker<T>(
        string serializedPropertyName,
        ReactiveProperty<T> property,
        IDisposable listener)
        : IPropertyBroker
    {
        public string SerializedPropertyName { get; } = serializedPropertyName;

        public Type ValueType
            => typeof(T);

        public object? DefaultValue { get; } = property.Value;

        object? IPropertyBroker.Value
        {
            get => property.Value;
            set => property.Value = value is T typed ? typed : (T)Convert.ChangeType(value, typeof(T))!;
        }

        void IDisposable.Dispose()
            => listener.Dispose();
    }

    private static IEnumerable<IPropertyBroker> EnumerateBrokers(ReactiveSettingsBase instance)
    {
        var registerMethodInfo = typeof(ReactiveSettingsBase)
                .GetMethod(nameof(RegisterPropertyChanged), BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Method not found: {nameof(RegisterPropertyChanged)}");

        return instance
            .GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(static prop =>
                prop.PropertyType.IsGenericType &&
                prop.PropertyType.GetGenericTypeDefinition() == typeof(ReactiveProperty<>))
            .Select(prop =>
            {
                var valueType = prop.PropertyType.GetGenericArguments()[0];
                var serializedName = prop.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? prop.Name;
                var property = prop.GetValue(instance)
                    ?? throw new InvalidOperationException($"Property '{prop.Name}' is null");

                var listener = (IDisposable)(registerMethodInfo
                        .MakeGenericMethod(valueType)
                        .Invoke(instance, [property, prop.Name])
                    ?? throw new InvalidOperationException($"Failed to register '{prop.Name}'"));

                return (IPropertyBroker)Activator.CreateInstance(
                    typeof(ReactivePropertyBroker<>).MakeGenericType(valueType),
                    serializedName, property, listener)!;
            });
    }

    private IDisposable RegisterPropertyChanged<T>(ReactiveProperty<T> property, string propertyName)
        => property
            .Where(_ => this._ignoreChangesFromLoad == false)
            .Subscribe(newValue =>
            {
                Log.Debug("🔔PropertyChanged", new() { propertyName, newValue });
                if (this.AutoSave) this._save.OnNext(SaveReason.PropertyChanged);
            });
}
