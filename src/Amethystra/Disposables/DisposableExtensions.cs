using System;
using System.Collections.Generic;
using Amethystra.Linq;

namespace Amethystra.Disposables;

public static class DisposableExtensions
{
    extension(IEnumerable<IDisposable> disposables)
    {
        public void Dispose()
            => disposables.ForEach(static x => x.Dispose());
    }
}
