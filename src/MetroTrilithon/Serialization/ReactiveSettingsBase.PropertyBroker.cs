using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Reactive.Bindings;

namespace MetroTrilithon.Serialization;

partial class ReactiveSettingsBase
{
    private class ReactivePropertyBroker : IDisposable
    {
        private readonly IDisposable _listener;

        public string PropertyName { get; }

        public Type ValueType { get; }

        public IReactiveProperty Property { get; }

        private ReactivePropertyBroker(object propertyOwner, PropertyInfo propertyInfo, MethodInfo listenerMethodInfo)
        {
            this.PropertyName = propertyInfo.Name;
            this.ValueType = propertyInfo.PropertyType.GetGenericArguments()[0];
            this.Property = propertyInfo.GetValue(propertyOwner) as IReactiveProperty
                ?? throw new InvalidOperationException($"Property '{propertyInfo.Name}' not found");

            this._listener = listenerMethodInfo
                    .MakeGenericMethod(this.ValueType)
                    .Invoke(propertyOwner, [this.Property, this.PropertyName]) as IDisposable
                ?? throw new InvalidOperationException($"Method '{listenerMethodInfo.Name}' invoked failed");
        }

        void IDisposable.Dispose()
            => this._listener.Dispose();

        public static IEnumerable<ReactivePropertyBroker> Enumerate(ReactiveSettingsBase instance)
        {
            var registerMethodInfo = typeof(ReactiveSettingsBase).GetMethod(nameof(RegisterPropertyChanged), BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException($"Method not found: {nameof(RegisterPropertyChanged)}");

            return instance
                .GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(prop =>
                    prop.PropertyType.IsGenericType &&
                    prop.PropertyType.GetGenericTypeDefinition() == typeof(IReactiveProperty<>))
                .Select(x => new ReactivePropertyBroker(instance, x, registerMethodInfo));
        }
    }

    private IDisposable RegisterPropertyChanged<T>(IReactiveProperty<T> property, string propertyName)
        => property
            .Where(_ => this._ignoreChangesFromLoad == false)
            .Subscribe(newValue =>
            {
                Debug.WriteLine($"🔔PropertyChanged ({propertyName}): {newValue}");
                if (this.AutoSave) this._save.OnNext(SaveReason.PropertyChanged);
            });
}
