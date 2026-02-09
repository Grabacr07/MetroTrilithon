using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Amethystra.Diagnostics;

[GenerateLogger]
public static partial class TaskExtensions
{
    extension(Task task)
    {
        public void FireAndForget(
            Action<Exception>? onError = null,
            bool ignoreCancellation = true,
            [CallerMemberName] string? caller = null)
        {
            ArgumentNullException.ThrowIfNull(task);

            _ = ForgetCoreAsync(task, onError, ignoreCancellation, caller);
            return;

            static async Task ForgetCoreAsync(
                Task task,
                Action<Exception>? onError,
                bool ignoreCancellation,
                string? caller)
            {
                try
                {
                    await task.ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (ignoreCancellation)
                {
                }
                catch (Exception ex)
                {
                    if (onError is not null)
                    {
                        onError(ex);
                    }
                    else
                    {
                        Log.Error(ex, $"{caller}");
                    }
                }
            }
        }

        public void FireAndForget(
            CancellationToken cancellationToken,
            Action<Exception>? onError = null,
            [CallerMemberName] string? caller = null)
        {
            ArgumentNullException.ThrowIfNull(task);

            _ = ForgetCoreAsync(task, cancellationToken, onError, caller);
            return;

            static async Task ForgetCoreAsync(
                Task task,
                CancellationToken cancellationToken,
                Action<Exception>? onError,
                string? caller)
            {
                try
                {
                    await task.ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                }
                catch (Exception ex)
                {
                    if (onError is not null)
                    {
                        onError(ex);
                    }
                    else
                    {
                        Log.Error(ex, $"{caller}");
                    }
                }
            }
        }
    }
}
