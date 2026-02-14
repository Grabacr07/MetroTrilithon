using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Mio.Destructive;

namespace Amethystra.Diagnostics;

partial class AppLog
{
    private readonly Channel<LogEntry> _queue;
    private Task? _pumpTask;
    private CancellationTokenSource? _pumpCancellationTokenSource;
    private long _droppedCount;

    private void Enqueue(LogEntry entry)
    {
        Debug.WriteLine(entry.FormattedText);
        if (entry.HasException) Debug.WriteLine(entry.ExceptionText);

        if (this._queue.Writer.TryWrite(entry) == false)
        {
            Interlocked.Increment(ref this._droppedCount);
        }
    }

    private async Task PumpAsync(CancellationToken cancellationToken)
    {
        try
        {
            this.LogFilePath.Parent.EnsureCreated();

            await using var stream = new FileStream(this.LogFilePath.AsDestructive().FullName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            await using var writer = new StreamWriter(stream, this._utf8NoBom);
            var lastRotateCheck = Stopwatch.StartNew();
            var lastFlush = Stopwatch.StartNew();
            var linesSinceFlush = 0;

            await foreach (var entry in this._queue.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                // 専用スレッドでの直列処理のため、非同期 I/O を行うメリットが薄い（むしろオーバーヘッドになる）
                // あえて同期 I/O を使用する
                // ReSharper disable MethodHasAsyncOverload / MethodHasAsyncOverloadWithCancellation

                var dropped = Interlocked.Exchange(ref this._droppedCount, 0);
                if (dropped > 0)
                {
                    var ts = DateTimeOffset.Now;
                    var data = new Dictionary<string, object?> { ["dropped"] = dropped, };
                    const string message = "Log queue overflow (dropped)";
                    const string source = $"{nameof(AppLog)}.{nameof(this.PumpAsync)}";
                    writer.WriteLine(new LogEntry(this, ts, LogLevel.Warn, message, source, null, data).FormattedText);
                }

                writer.WriteLine(entry.FormattedText);
                if (entry.ExceptionText is not null) writer.WriteLine(entry.ExceptionText);

                linesSinceFlush++;

                if (lastRotateCheck.ElapsedMilliseconds >= 1000)
                {
                    lastRotateCheck.Restart();
                    this.TryRotateLogFileIfNeeded();
                }

                if (linesSinceFlush >= 64 || lastFlush.ElapsedMilliseconds >= 250)
                {
                    lastFlush.Restart();
                    linesSinceFlush = 0;

                    writer.Flush();
                    stream.Flush(false);
                }

                // ReSharper restore MethodHasAsyncOverload / MethodHasAsyncOverloadWithCancellation
            }

            await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
            stream.Flush(true);
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

    private string SerializeData(Dictionary<string, object?> data)
    {
        try
        {
            return JsonSerializer.Serialize(data, this._jsonOptions);
        }
        catch
        {
            return "{serialization_failed}";
        }
    }

    private sealed record LogEntry(
        AppLog Log,
        DateTimeOffset Timestamp,
        LogLevel Level,
        string Message,
        string Source,
        Exception? Exception,
        // ReSharper disable once MemberHidesStaticFromOuterClass
        Dictionary<string, object?>? Data)
    {
        public bool HasException
            => string.IsNullOrEmpty(this.ExceptionText) == false;

        public string? ExceptionText
            => field ??= this.Exception?.ToString();

        public string FormattedText
            => field ??= this.Format();

        private string Format()
        {
            var pid = Environment.ProcessId;
            var tid = Environment.CurrentManagedThreadId;
            var sb = new StringBuilder();
            sb.Append(this.Timestamp.ToString("O"));
            sb.Append(' ');
            sb.Append('[').Append(this.Level).Append(']');
            sb.Append(" pid=").Append(pid);
            sb.Append(" tid=").Append(tid);
            sb.Append(" src=").Append(this.Source);

            if (string.IsNullOrWhiteSpace(this.Message) == false)
            {
                sb.Append(" msg=\"").Append(this.Message).Append('"');
            }

            if (this.Data is not null && this.Data.Count > 0)
            {
                sb.Append(" data=");
                sb.Append(this.Log.SerializeData(this.Data));
            }

            return sb.ToString();
        }
    }
}
