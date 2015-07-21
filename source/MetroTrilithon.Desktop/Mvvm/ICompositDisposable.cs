using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Livet;

namespace MetroTrilithon.Mvvm
{
	public interface ICompositDisposable : IDisposable
	{
		LivetCompositeDisposable CompositeDisposable { get; }
	}
}
