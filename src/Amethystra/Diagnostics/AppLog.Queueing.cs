using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Channels;

namespace Amethystra.Diagnostics;

partial class AppLog
{
    private void Enqueue(LogEntry entry)
        => this._queueing.Enqueue(entry);

    private sealed class Queueing : IDisposable
    {
        private readonly AppLog _log;
        private readonly AppLogOptions _options;
        private readonly Channel<LogEntry> _queue;
        private readonly CancellationTokenSource _pumpCancellationTokenSource = new();
        private readonly Task _pumpTask;
        private long _droppedCount;

        public Queueing(AppLog log)
        {
            this._log = log;
            this._options = log._options;
            this._queue = Channel.CreateBounded<LogEntry>(new BoundedChannelOptions(log._options.QueueCapacity)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.DropWrite,
                AllowSynchronousContinuations = true,
            });
            this._pumpTask = Task.Factory.StartNew(
                    () => this.PumpAsync(this._pumpCancellationTokenSource.Token),
                    this._pumpCancellationTokenSource.Token,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default)
                .Unwrap();
        }

        public void Enqueue(LogEntry entry)
        {
            Debug.WriteLine(entry.FormattedText);
            if (entry.HasException)
            {
                Debug.WriteLine(entry.ExceptionText);
            }

            if (this._queue.Writer.TryWrite(entry) == false)
            {
                Interlocked.Increment(ref this._droppedCount);
            }
        }

        private async Task PumpAsync(CancellationToken cancellationToken)
        {
            try
            {
                this._options.LogFilePath.Parent.EnsureCreated();
                using var writer = new RotatingLogWriter(this._options);

                await foreach (var entry in this._queue.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
                {
                    var dropped = Interlocked.Exchange(ref this._droppedCount, 0);
                    if (dropped > 0)
                    {
                        var ts = DateTimeOffset.Now;
                        var data = this._log.SerializeData(new Dictionary<string, object?> { ["dropped"] = dropped, });
                        const string message = "Log queue overflow (dropped)";
                        const string source = $"{nameof(AppLog)}.PumpAsync";
                        writer.WriteLine(new LogEntry(ts, LogLevel.Warn, message, source, data).FormattedText);
                    }

                    writer.Write(entry);
                }
            }
            catch (OperationCanceledException)
            {
                // タイムアウト保険で cancel されたケース
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"=== UNKNOWN ERROR: {nameof(this.PumpAsync)} ===");
                Debug.WriteLine(ex);
            }
        }

        public void Dispose()
        {
            try
            {
                try
                {
                    this._queue.Writer.Complete();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }

                if (this._pumpTask.Wait(TimeSpan.FromMilliseconds(this._options.BestEffortTimeoutMsForDispose)) == false)
                {
                    this._pumpCancellationTokenSource.Cancel();

                    try
                    {
                        this._pumpTask.Wait(TimeSpan.FromMilliseconds(200));
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                    }
                }
            }
            finally
            {
                this._pumpCancellationTokenSource.Dispose();
            }
        }
    }
}
