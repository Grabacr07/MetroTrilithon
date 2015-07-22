using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Livet;
using Livet.EventListeners;

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

		/// <summary>
		/// <see cref="IDisposable"/> オブジェクトを、指定した <see cref="ICompositeDisposable.CompositeDisposable"/> に追加します。
		/// </summary>
		public static void AddTo(this IDisposable disposable, ICompositeDisposable compositDisposable)
		{
			compositDisposable.CompositeDisposable.Add(disposable);
		}


		/// <summary>
		/// <see cref="INotifyPropertyChanged.PropertyChanged"/> イベントを購読します。
		/// </summary>
		public static IDisposable Subscribe(this INotifyPropertyChanged source, PropertyChangedEventHandler handler)
		{
			return new PropertyChangedEventListener(source, handler);
		}

		/// <summary>
		/// <see cref="INotifyPropertyChanged.PropertyChanged"/> イベントを購読します。
		/// </summary>
		/// <param name="source">イベント ソース。</param>
		/// <param name="action">イベント発生時に、<see cref="PropertyChangedEventArgs.PropertyName"/> を受け取って実行されるメソッド。</param>
		public static IDisposable Subscribe(this INotifyPropertyChanged source, Action<string> action)
		{
			return new PropertyChangedEventListener(source, (sender, args) => action(args.PropertyName));
		}

		/// <summary>
		/// 指定したプロパティ名で発生した <see cref="INotifyPropertyChanged.PropertyChanged"/> イベントを購読します。
		/// </summary>
		/// <param name="source">イベント ソース。</param>
		/// <param name="propertyName">イベントを購読するプロパティの名前。</param>
		/// <param name="action">イベント発生時に実行するメソッド。</param>
		/// <param name="immediately">このメソッドの呼び出し時点で <paramref name="action"/> を 1 度実行する場合は true、それ以外の場合は false。既定値は true です。</param>
		public static IDisposable Subscribe(this INotifyPropertyChanged source, string propertyName, Action action, bool immediately = true)
		{
			if (immediately) action();

			return new PropertyChangedEventListener(source)
			{
				{ propertyName, (sender, args) => action() },
			};
		}
	}
}
