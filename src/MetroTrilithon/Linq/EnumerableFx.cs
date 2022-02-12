using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MetroTrilithon.Linq;

public static class EnumerableFx
{
    public static IEnumerable<T> Return<T>(T value)
    {
        yield return value;
    }

    public static IEnumerable<T> Do<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (var item in source)
        {
            action(item);
            yield return item;
        }
    }

    public static IEnumerable<T> Do<T>(this IEnumerable<T> source, Action<T, int> action)
    {
        var i = 0;
        foreach (var item in source)
        {
            action(item, i++);
            yield return item;
        }
    }

    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (var item in source)
        {
            action(item);
        }
    }

    public static void ForEach<T>(this IEnumerable<T> source, Action<T, int> action)
    {
        var i = 0;
        foreach (var item in source)
        {
            action(item, i++);
        }
    }

    public static IEnumerable<T> EnsureCount<T>(this IEnumerable<T> source, int count, Func<int, T> selector)
        => source
            .Take(count)
            .Padding(count, selector);

    public static IEnumerable<T> Padding<T>(this IEnumerable<T> source, int count, Func<int, T> selector)
    {
        // めっっっちゃ適当な実装なので後でなんとかしたい

        if (source is not IList<T> {IsReadOnly: false} list) list = source.ToList();

        while (list.Count < count) list.Add(selector(list.Count));

        return list;
    }

    public static IEnumerable<T> Random<T>(this IEnumerable<T> source)
        => source.OrderBy(_ => Guid.NewGuid());

    public static string JoinString<T>(this IEnumerable<T> source, string separator)
        => string.Join(separator, source is IEnumerable<string> strings ? strings : source.Select(x => $"{x}"));

    public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> source)
        => new(source);

    public static void Dispose(this IEnumerable<IDisposable> disposables)
        => disposables.ForEach(x => x.Dispose());
}
