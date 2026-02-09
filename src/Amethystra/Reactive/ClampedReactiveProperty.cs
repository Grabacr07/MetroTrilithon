using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Reactive.Bindings;

namespace Amethystra.Reactive;

public sealed class ClampedReactiveProperty : IReactiveProperty<int>
{
    private readonly IReactiveProperty<int> _inner;
    private readonly int _min;
    private readonly int _max;

    public int Value
    {
        get => this._inner.Value;
        set => this._inner.Value = Math.Clamp(value, this._min, this._max);
    }

    object IReadOnlyReactiveProperty.Value
        => this.Value;

    object? IReactiveProperty.Value
    {
        get => this.Value;
        set
        {
            if (value is null)
            {
                this.Value = this._min;
                return;
            }

            this.Value = value switch
            {
                int x => x,
                _ => Convert.ToInt32(value),
            };
        }
    }

    public bool HasErrors
        => this._inner.HasErrors;

    public ClampedReactiveProperty(IReactiveProperty<int> inner, int min, int max)
    {
        ArgumentNullException.ThrowIfNull(inner);
        this._inner = inner;
        this._min = min;
        this._max = max;
    }

    public void ForceNotify()
        => this._inner.ForceNotify();

    public IDisposable Subscribe(IObserver<int> observer)
        => this._inner.Subscribe(observer);

    public IObservable<bool> ObserveHasErrors
        => this._inner.ObserveHasErrors;

    public IObservable<IEnumerable?> ObserveErrorChanged
        => this._inner.ObserveErrorChanged;

    public event PropertyChangedEventHandler? PropertyChanged
    {
        add
        {
            if (this._inner is INotifyPropertyChanged n) n.PropertyChanged += value;
        }
        remove
        {
            if (this._inner is INotifyPropertyChanged n) n.PropertyChanged -= value;
        }
    }

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged
    {
        add
        {
            if (this._inner is INotifyDataErrorInfo e) e.ErrorsChanged += value;
        }
        remove
        {
            if (this._inner is INotifyDataErrorInfo e) e.ErrorsChanged -= value;
        }
    }

    public IEnumerable GetErrors(string? propertyName)
        => this._inner.GetErrors(propertyName);

    public void Dispose()
        => this._inner.Dispose();
}
