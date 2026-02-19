using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using System.Threading.Tasks;
using Amethystra.Disposables;

namespace Amethystra.Diagnostics;

public sealed partial class AppLog : IDisposable
{
    private readonly AppLogOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly DisposeGateSlim<AppLog> _disposeGate = new();

    public AppLog(AppLogOptions options, params IEnumerable<JsonConverter> converters)
    {
        this._options = options;
        this._jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.General)
        {
            WriteIndented = false,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        foreach (var c in converters) this._jsonOptions.Converters.Add(c);

        this._queue = Channel.CreateBounded<LogEntry>(new BoundedChannelOptions(options.QueueCapacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropWrite,
            AllowSynchronousContinuations = true,
        });
        this._pumpCancellationTokenSource = new CancellationTokenSource();
        this._pumpTask = Task.Factory.StartNew(
                () => this.PumpAsync(this._pumpCancellationTokenSource.Token), this._pumpCancellationTokenSource.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default)
            .Unwrap();
    }

    private void WriteFromTypedLogger(
        LogLevel level,
        string message,
        Exception? exception,
        string typeName,
        string? memberName,
        Data? data)
        => this.WriteCore(level, message, exception, ComposeSource(typeName, memberName), data);

    private void WriteFromOperationScope(
        string source,
        string message,
        Data? data)
        => this.WriteCore(LogLevel.Info, message, null, source, data);

    private void WriteCore(
        LogLevel level,
        string message,
        Exception? exception,
        string source,
        Data? data)
    {
        try
        {
            var timestamp = DateTimeOffset.Now;
            var dictionary = data?.ToDictionary();

            switch (level)
            {
                case LogLevel.Fatal:
                    FailSafeLog.Fatal(message, exception, source, dictionary);
                    break;
                case LogLevel.Error:
                    FailSafeLog.Error(message, exception, source, dictionary);
                    break;
            }

            if (this._disposeGate.IsDisposed == false)
            {
                this.Enqueue(new LogEntry(this, timestamp, level, message, source, exception, dictionary));
            }
        }
        catch
        {
            // ログが原因でアプリを落とさない
        }
    }

    private static string ComposeSource(string typeName, string? memberName)
        => string.IsNullOrWhiteSpace(memberName)
            ? typeName
            : $"{typeName}.{memberName}";

    public void Dispose()
    {
        if (this._disposeGate.TryDispose() == false) return;

        try
        {
            try
            {
                this._queue.Writer.Complete();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"=== UNKNOWN ERROR: {nameof(this.Dispose)} (_queue.Writer.Complete()) ===");
                Debug.WriteLine(ex);
            }

            if (this._pumpTask is not null
                && this._pumpTask.Wait(TimeSpan.FromMilliseconds(this._options.BestEffortTimeoutMsForDispose)) == false)
            {
                this._pumpCancellationTokenSource?.Cancel();

                try
                {
                    this._pumpTask.Wait(TimeSpan.FromMilliseconds(200));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"=== UNKNOWN ERROR: {nameof(this.Dispose)} (_pumpTask.Wait()) ===");
                    Debug.WriteLine(ex);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"=== UNKNOWN ERROR: {nameof(this.Dispose)} ===");
            Debug.WriteLine(ex);
        }
        finally
        {
            this._pumpCancellationTokenSource?.Dispose();
            this._pumpCancellationTokenSource = null;
            this._pumpTask = null;
            Debug.WriteLine($"{nameof(AppLog)} stopped.");
        }
    }
}
