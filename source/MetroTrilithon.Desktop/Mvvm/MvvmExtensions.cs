using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Livet;

namespace MetroTrilithon.Mvvm
{
	public static class MvvmExtensions
	{
		/// <summary>
		/// <see cref="IDisposable"/> オブジェクトを、指定した <see cref="ViewModel"/> の <see cref="ViewModel.CompositeDisposable"/> に追加します。
		/// </summary>
		public static void AddTo(this IDisposable disposable, ViewModel vm)
		{
			vm.CompositeDisposable.Add(disposable);
		}
	}
}
