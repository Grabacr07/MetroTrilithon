using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Livet;
using MetroTrilithon.Serialization;
using Reactive.Bindings;

namespace MetroTrilithon.Mvvm;

public static class DisplayViewModel
{
    public static DisplayViewModel<T> Create<T>(T value, string display)
        => new(value, display);

    public static IEnumerable<DisplayViewModel<TResult>> ToDisplay<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> valueSelector, Func<TSource, string> displaySelector)
        => source.Select(x => new DisplayViewModel<TResult>(valueSelector(x), displaySelector(x)));
}

public interface IHaveDisplayName
{
    IReadOnlyReactiveProperty<string> Display { get; }
}

public class DisplayViewModel<T> : ViewModel, IHaveDisplayName
{
    private readonly ReactiveProperty<T?> _value;
    private readonly ReactiveProperty<string> _display;

    public IReadOnlyReactiveProperty<T?> Value
        => this._value;

    public IReadOnlyReactiveProperty<string> Display
        => this._display;

    public DisplayViewModel(T? value, string display)
    {
        this._value = new ReactiveProperty<T?>(value);
        this._display = new ReactiveProperty<string>(display);
    }

    public override string ToString()
        => this.Display.Value;
}
