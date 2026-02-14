using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using System.Threading.Tasks;
using Amethystra.Disposables;
using Amethystra.Properties;
using Mio;

namespace Amethystra.Diagnostics;

public sealed partial class AppLog : IDisposable
{
    public sealed record Options(
        FilePath LogFilePath,
        long MaxLogBytes = 10L * 1024L * 1024L,
        int MaxGenerations = 5,
        int QueueCapacity = 2048,
        int BestEffortTimeoutMsForDispose = 1000)
    {
        public Options(IAssemblyInfo assemblyInfo)
            : this(CreatePath(assemblyInfo))
        {
        }

        public static FilePath CreatePath(IAssemblyInfo info)
            => new DirectoryPath(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))
                .ChildDirectory(info.Company)
                .ChildDirectory(info.Product)
                .EnsureCreated()
                .ChildFile($"{Assembly.GetEntryAssembly()?.GetName().Name}.log");
    }

    private readonly Options _options;
    private readonly Encoding _utf8NoBom = new UTF8Encoding(false);
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly DisposeGateSlim<AppLog> _disposeGate = new();

    private FilePath LogFilePath
        => _overrideLogFilePath ?? this._options.LogFilePath;

    private long MaxLogBytes
        => _overrideMaxLogBytes ?? this._options.MaxLogBytes;

    private int MaxGenerations
        => _overrideMaxGenerations ?? this._options.MaxGenerations;

    public AppLog(Options options, params IEnumerable<JsonConverter> converters)
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
