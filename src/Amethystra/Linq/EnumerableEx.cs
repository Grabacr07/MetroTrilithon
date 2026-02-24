using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Amethystra.Linq;

public static class EnumerableEx
{
    public static IEnumerable<T> Return<T>(T value)
    {
        yield return value;
    }

    extension<T>(IEnumerable<T> source)
    {
        public IEnumerable<T> Do(Action<T> action)
        {
            foreach (var item in source)
            {
                action(item);
                yield return item;
            }
        }

        public IEnumerable<T> Do(Action<T, int> action)
        {
            var i = 0;
            foreach (var item in source)
            {
                action(item, i++);
                yield return item;
            }
        }

        public void ForEach(Action<T> action)
        {
            foreach (var item in source)
            {
                action(item);
            }
        }

        public void ForEach(Action<T, int> action)
        {
            var i = 0;
            foreach (var item in source)
            {
                action(item, i++);
            }
        }

        public IEnumerable<T> EnsureCount(int count, Func<int, T> selector)
            => source
                .Take(count)
                .Padding(count, selector);

        public IEnumerable<T> Padding(int count, Func<int, T> selector)
        {
            // めっっっちゃ適当な実装なので後でなんとかしたい

            if (source is not List<T> list) list = [.. source];

            while (list.Count < count) list.Add(selector(list.Count));

            return list;
        }

        public IEnumerable<T> Random()
            => source.OrderBy(static _ => Guid.NewGuid());

        public string JoinString(string separator)
            => string.Join(separator, source as IEnumerable<string> ?? source.Select(static x => $"{x}"));

        public ObservableCollection<T> ToObservableCollection()
            => [.. source];
    }
}
