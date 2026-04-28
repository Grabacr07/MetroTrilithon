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
    IReadOnlyBindableReactiveProperty<string> Display { get; }
}

public class DisplayViewModel<T> : ViewModelBase, IHaveDisplayName
{
    private readonly BindableReactiveProperty<T?> _value;
    private readonly BindableReactiveProperty<string> _display;

    public IReadOnlyBindableReactiveProperty<T?> Value => this._value;

    public IReadOnlyBindableReactiveProperty<string> Display => this._display;

    public DisplayViewModel(T? value, string display)
    {
        this._value = new(value);
        this._display = new(display);
        this._value.AddTo(this);
        this._display.AddTo(this);
    }

    public override string ToString()
        => this._display.Value;
}
