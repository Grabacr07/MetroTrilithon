using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Reactive.Bindings;

namespace MetroTrilithon.Serialization;

partial class ReactiveSettingsBase
{
    private class ReactivePropertyBroker(object propertyOwner, PropertyInfo propertyInfo)
    {
        private PropertyInfo? _valuePropertyInfo;

        public string PropertyName { get; } = propertyInfo.Name;

        public object? CurrentValue
        {
            get => this.GetValuePropertyInfo().GetValue(this.PropertyInstance);
            set => this.GetValuePropertyInfo().SetValue(this.PropertyInstance, value);
        }

        public Type ValueType { get; } = propertyInfo.PropertyType.GetGenericArguments()[0];

        public object PropertyInstance { get; } = propertyInfo.GetValue(propertyOwner)
            ?? throw new InvalidOperationException($"Property '{propertyInfo.Name}' not found");

        private PropertyInfo GetValuePropertyInfo()
            => this._valuePropertyInfo ??= this.PropertyInstance.GetType().GetProperty(nameof(IReactiveProperty.Value))
                ?? throw new InvalidOperationException($"Property '{nameof(IReactiveProperty.Value)}' not found in '{typeof(IReactiveProperty<>).Name}'");
    }
}
