using System;
using System.Collections.Generic;
using System.Linq;
using R3;

namespace Amethystra.Mvvm;

public static class DisplayViewModel
{
    public static DisplayViewModel<T> Create<T>(T value, string display)
        => new(value, display);

    public static IEnumerable<DisplayViewModel<TResult>> ToDisplay<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> valueSelector, Func<TSource, string> displaySelector)
        => source.Select(x => new DisplayViewModel<TResult>(valueSelector(x), displaySelector(x)));
}

public interface IHaveDisplayName
{
    ReadOnlyReactiveProperty<string> Display { get; }
}

public class DisplayViewModel<T>(T? value, string display) : ViewModelBase, IHaveDisplayName
{
    private readonly ReactiveProperty<T?> _value = new(value);
    private readonly ReactiveProperty<string> _display = new(display);

    public ReadOnlyReactiveProperty<T?> Value
        => this._value;

    public ReadOnlyReactiveProperty<string> Display
        => this._display;

    public override string ToString()
        => this.Display.CurrentValue;
}
