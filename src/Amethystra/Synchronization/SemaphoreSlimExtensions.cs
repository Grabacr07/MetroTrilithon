using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Amethystra.Synchronization;

public static class SemaphoreSlimExtensions
{
    public static async ValueTask<IDisposable> AcquireAsync(this SemaphoreSlim semaphore, CancellationToken cancellationToken = default)
    {
        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new Releaser(semaphore);
    }

    private sealed class Releaser(SemaphoreSlim semaphore) : IDisposable
    {
        private SemaphoreSlim? _semaphore = semaphore;

        public void Dispose()
        {
            var semaphore = Interlocked.Exchange(ref this._semaphore, null);
            semaphore?.Release();
        }
    }
}
