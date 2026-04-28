using System;

namespace Amethystra.Mvvm;

public static class ViewModelExtensions
{
    /// <summary>
    /// <see cref="IDisposable"/> オブジェクトを、指定した <see cref="ViewModelBase"/> の <see cref="ViewModelBase.CompositeDisposable"/> に追加します。
    /// </summary>
    public static T AddTo<T>(this T disposable, ViewModelBase? vm)
        where T : IDisposable
    {
        if (vm == null)
        {
            disposable.Dispose();
            return disposable;
        }

        vm.CompositeDisposable.Add(disposable);
        return disposable;
    }
}
