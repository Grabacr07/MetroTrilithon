using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Livet;

namespace MetroTrilithon.Mvvm
{
	public class DisplayViewModel<T> : ViewModel
	{

		#region Value 変更通知プロパティ

		private T _Value;

		public T Value
		{
			get { return this._Value; }
			set
			{
				if (!Equals(this._Value, value))
				{
					this._Value = value;
					this.RaisePropertyChanged();
				}
			}
		}

		#endregion

		#region Display 変更通知プロパティ

		private string _Display;

		public string Display
		{
			get { return this._Display; }
			set
			{
				if (this._Display != value)
				{
					this._Display = value;
					this.RaisePropertyChanged();
				}
			}
		}

		#endregion

		public static implicit operator T(DisplayViewModel<T> dvm)
		{
			return dvm.Value;
		}

		public override string ToString()
		{
			return this.Display;
		}
	}
}
