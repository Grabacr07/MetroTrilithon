using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;

namespace MetroTrilithon.Lifetime;

public static class DisposableExtensions
{
    public static void Add(this ICollection<IDisposable> compositeDisposable, Action disposeAction)
        => compositeDisposable.Add(Disposable.Create(disposeAction));

    /// <summary>
    /// <see cref="IDisposable"/> オブジェクトを、指定した <see cref="IDisposableHolder.CompositeDisposable"/> に追加します。
    /// </summary>
    public static T AddTo<T>(this T disposable, IDisposableHolder? obj)
        where T : IDisposable
    {
        if (obj == null)
        {
            disposable.Dispose();
        }
        else
        {
            obj.CompositeDisposable.Add(disposable);
        }

        return disposable;
    }
}
