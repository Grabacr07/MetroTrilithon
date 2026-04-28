using System;
using System.ComponentModel;
using R3;

namespace Amethystra.Reactive;

/// <summary>
/// <see cref="ReactiveProperty{T}"/> に値の範囲クランプを適用するラッパーです。
/// </summary>
public sealed class ClampedReactiveProperty : INotifyPropertyChanged, IDisposable
{
    private readonly ReactiveProperty<int> _inner;
    private readonly int _min;
    private readonly int _max;
    private readonly IDisposable _propertyChangedSubscription;

    private event PropertyChangedEventHandler? _propertyChanged;

    public int Value
    {
        get => this._inner.Value;
        set => this._inner.Value = Math.Clamp(value, this._min, this._max);
    }

    public Observable<int> Changed
        => this._inner;

    public event PropertyChangedEventHandler? PropertyChanged
    {
        add => this._propertyChanged += value;
        remove => this._propertyChanged -= value;
    }

    public ClampedReactiveProperty(int initialValue, int min, int max)
    {
        this._min = min;
        this._max = max;
        this._inner = new ReactiveProperty<int>(Math.Clamp(initialValue, min, max));
        this._propertyChangedSubscription = this._inner
            .Skip(1)
            .Subscribe(_ => this._propertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Value))));
    }

    public void Dispose()
    {
        this._propertyChangedSubscription.Dispose();
        this._inner.Dispose();
    }
}
