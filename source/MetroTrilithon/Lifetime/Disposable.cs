using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MetroTrilithon.Lifetime
{
	public static class Disposable
	{
		public static IDisposable Create(Action dispose)
		{
			return new AnonymousDisposable(dispose);
		}

		private class AnonymousDisposable : IDisposable
		{
			private bool isDisposed;
			private readonly Action dispose;

			public AnonymousDisposable(Action dispose)
			{
				this.dispose = dispose;
			}

			public void Dispose()
			{
				if (this.isDisposed) return;

				this.isDisposed = true;
				this.dispose();
			}
		}
	}
}
