﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace MetroTrilithon.Serialization;

[DebuggerDisplay("Value={Value}, Key={Key}, Default={Default}")]
public abstract class SerializablePropertyBase<T> : INotifyPropertyChanged
{
    private T? _value;

    private bool _cached;

    public string Key { get; }

    public ISerializationProvider Provider { get; }

    public bool AutoSave { get; set; }

    public T? Default { get; }

    public virtual T? Value
    {
        get
        {
            if (this._cached) return this._value;

            if (!this.Provider.IsLoaded)
            {
                this.Provider.Load();
            }

            if (this.Provider.TryGetValue(this.Key, out object obj))
            {
                this._value = this.DeserializeCore(obj);
                this._cached = true;
            }
            else
            {
                this._value = this.Default;
            }

            return this._cached ? this._value : this.Default;
        }
        set
        {
            if (this._cached && Equals(this._value, value)) return;

            if (!this.Provider.IsLoaded)
            {
                this.Provider.Load();
            }

            var old = this._value;
            this._value = value;
            this._cached = true;
            this.Provider.SetValue(this.Key, this.SerializeCore(value));
            this.OnValueChanged(old, value);

            if (this.AutoSave) this.Provider.Save();
        }
    }

    protected SerializablePropertyBase(string key, ISerializationProvider provider)
        : this(key, provider, default!)
    {
    }

    protected SerializablePropertyBase(string key, ISerializationProvider provider, T? defaultValue)
    {
        this.Key = key ?? throw new ArgumentNullException(nameof(key));
        this.Provider = provider ?? throw new ArgumentNullException(nameof(provider));
        this.Default = defaultValue;

        this.Provider.Reloaded += (_, _) =>
        {
            if (this._cached)
            {
                this._cached = false;

                var oldValue = this._value;
                var newValue = this.Value;
                if (!Equals(oldValue, newValue))
                {
                    this.OnValueChanged(oldValue, newValue);
                }
            }
            else
            {
                var oldValue = default(T);
                var newValue = this.Value;
                this.OnValueChanged(oldValue, newValue);
            }
        };
    }


    protected virtual object? SerializeCore(T? value)
        => value;

    protected virtual T? DeserializeCore(object? value)
        => (T?)value;


    public virtual IDisposable Subscribe(Action<T?> listener)
    {
        listener(this.Value);
        return new ValueChangedEventListener(this, listener);
    }

    public virtual void Reset()
    {
        if (!this.Provider.IsLoaded)
        {
            this.Provider.Load();
        }

        if (this.Provider.TryGetValue(this.Key, out object old))
        {
            if (this.Provider.RemoveValue(this.Key))
            {
                this._value = default!;
                this._cached = false;
                this.OnValueChanged(this.DeserializeCore(old), this.Default);

                if (this.AutoSave) this.Provider.Save();
            }
        }
    }

    private class ValueChangedEventListener : IDisposable
    {
        private readonly Action<T?> _listener;
        private readonly SerializablePropertyBase<T> _source;

        public ValueChangedEventListener(SerializablePropertyBase<T> property, Action<T?> listener)
        {
            this._listener = listener;
            this._source = property;
            this._source.ValueChanged += this.HandleValueChanged;
        }

        private void HandleValueChanged(object? sender, ValueChangedEventArgs<T?> args)
        {
            this._listener(args.NewValue);
        }

        public void Dispose()
        {
            this._source.ValueChanged -= this.HandleValueChanged;
        }
    }


    public static implicit operator T?(SerializablePropertyBase<T> property)
        => property.Value;


    #region events

    public event EventHandler<ValueChangedEventArgs<T?>>? ValueChanged;

    protected virtual void OnValueChanged(T? oldValue, T? newValue)
    {
        this.ValueChanged?.Invoke(this, new ValueChangedEventArgs<T?>(oldValue, newValue));
    }

    private readonly Dictionary<PropertyChangedEventHandler, EventHandler<ValueChangedEventArgs<T?>>> _handlers
        = new();

    event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
    {
        add
        {
            this.ValueChanged += this._handlers[value!] = (sender, _) => value!(sender, new PropertyChangedEventArgs(nameof(this.Value)));
        }
        remove
        {
            if (this._handlers.TryGetValue(value!, out var handler))
            {
                this.ValueChanged -= handler;
                this._handlers.Remove(value!);
            }
        }
    }

    #endregion
}
