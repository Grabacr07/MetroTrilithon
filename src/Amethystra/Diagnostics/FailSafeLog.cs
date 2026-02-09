using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Amethystra.Properties;
using Mio;
using Mio.Destructive;

namespace Amethystra.Diagnostics;

public static partial class FailSafeLog
{
    private const long _maxLogBytes = 10L * 1024L * 1024L;
    private const int _maxGenerations = 5;

    private static FilePath _logFilePath = CreatePath(ThisAssembly.Info);
    private static IAssemblyInfo _assemblyInfo = ThisAssembly.Info;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    private static readonly Lock _gate = new();
    private static readonly Encoding _utf8NoBom = new UTF8Encoding(false);

    private static FilePath LogFilePath
        => _overrideLogFilePath ?? _logFilePath;

    private static long MaxLogBytes
        => _overrideMaxLogBytes ?? _maxLogBytes;

    private static int MaxGenerations
        => _overrideMaxGenerations ?? _maxGenerations;

    public static void Initialize(IAssemblyInfo info)
    {
        _logFilePath = CreatePath(info);
        _assemblyInfo = info;
    }

    public static void Info(string message, string? category = null, IReadOnlyDictionary<string, object?>? data = null)
        => Write(LogLevel.Info, message, category, data, null);

    public static void Warn(string message, string? category = null, IReadOnlyDictionary<string, object?>? data = null)
        => Write(LogLevel.Warn, message, category, data, null);

    public static void Error(string message, Exception? exception = null, string? category = null, IReadOnlyDictionary<string, object?>? data = null)
        => Write(LogLevel.Error, message, category, data, exception);

    public static void Fatal(string message, Exception? exception = null, string? category = null, IReadOnlyDictionary<string, object?>? data = null)
        => Write(LogLevel.Fatal, message, category, data, exception);

    public static IDisposable BeginOperation(string operationName, string? category = null, IReadOnlyDictionary<string, object?>? data = null)
        => new OperationScope(operationName, category, data);

    private static FilePath CreatePath(IAssemblyInfo info)
        => new DirectoryPath(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))
            .ChildDirectory(info.Company)
            .ChildDirectory(info.Product)
            .EnsureCreated()
            .ChildFile($"{Assembly.GetEntryAssembly()?.GetName().Name}.{nameof(FailSafeLog)}.json");

    private static void Write(LogLevel level, string message, string? category, IReadOnlyDictionary<string, object?>? data, Exception? exception)
    {
        var entry = FailSafeLogEntry.Create(level, message, category, data, exception);
        AppendLine(entry);
    }

    private static void AppendLine(FailSafeLogEntry entry)
    {
        try
        {
            var json = JsonSerializer.Serialize(entry, _jsonOptions);

            lock (_gate)
            {
                LogFilePath.Parent.EnsureCreated();

                TryRotateLogFileIfNeeded();

                using var stream = new FileStream(LogFilePath.AsDestructive().FullName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                using var writer = new StreamWriter(stream, _utf8NoBom);

                writer.WriteLine(json);
                writer.Flush();

                stream.Flush(true);
            }
        }
        catch
        {
            // FailSafeLog は “絶対に落ちない” が最優先です。
            // 書けない環境(権限/ディスク/ロック)でも握りつぶします。
        }
    }

    private static void TryRotateLogFileIfNeeded()
    {
        try
        {
            if (LogFilePath.Exists() == false)
            {
                return;
            }

            var length = LogFilePath.GetSize();
            if (length <= MaxLogBytes)
            {
                return;
            }

            RotateGenerations(LogFilePath);
        }
        catch
        {
            // ローテーションに失敗しても、ログ書き込み自体は続行したいので握りつぶします。
        }
    }

    private static void RotateGenerations(FilePath currentPath)
    {
        var directory = currentPath.Parent;

        var baseName = currentPath.NameWithoutExtension;
        var extension = currentPath.Extension;
        var oldest = directory.ChildFile($"{baseName}.{MaxGenerations}{extension}");
        if (oldest.Exists()) oldest.AsDestructive().Delete();

        for (var i = MaxGenerations - 1; i >= 1; i--)
        {
            var src = directory.ChildFile($"{baseName}.{i}{extension}");
            if (src.Exists() == false) continue;

            var dst = directory.ChildFile($"{baseName}.{i + 1}{extension}");
            if (dst.Exists()) dst.AsDestructive().Delete();

            src.AsDestructive().MoveTo(dst.AsDestructive());
        }

        var first = directory.ChildFile($"{baseName}.1{extension}");
        if (first.Exists()) first.AsDestructive().Delete();

        currentPath.AsDestructive().MoveTo(first.AsDestructive());
    }

    private readonly struct OperationScope : IDisposable
    {
        private readonly string _name;
        private readonly string? _category;
        private readonly IReadOnlyDictionary<string, object?>? _data;
        private readonly long _startTimestamp;

        public OperationScope(string name, string? category, IReadOnlyDictionary<string, object?>? data)
        {
            this._name = name;
            this._category = category;
            this._data = data;
            this._startTimestamp = Stopwatch.GetTimestamp();

            Info($"Begin: {name}", category, data);
        }

        public void Dispose()
        {
            var end = Stopwatch.GetTimestamp();
            var elapsedMs = (end - this._startTimestamp) * 1000.0 / Stopwatch.Frequency;
            var extra = this._data is null
                ? new Dictionary<string, object?>()
                : new Dictionary<string, object?>(this._data);

            extra["elapsedMs"] = elapsedMs;

            Info($"End: {this._name}", this._category, extra);
        }
    }

    private enum LogLevel
    {
        Info,
        Warn,
        Error,
        Fatal,
    }

    private sealed record FailSafeLogEntry(
        // ReSharper disable NotAccessedPositionalProperty.Local
        string Timestamp,
        LogLevel Level,
        int ProcessId,
        int ThreadId,
        string AppVersion,
        string? Category,
        string Message,
        Dictionary<string, object?>? Data,
        ExceptionInfo? Exception
        // ReSharper restore NotAccessedPositionalProperty.Local
    )
    {
        public static FailSafeLogEntry Create(
            LogLevel level,
            string message,
            string? category,
            IReadOnlyDictionary<string, object?>? data,
            Exception? exception)
            => new(
                DateTimeOffset.Now.ToString("O"),
                level,
                Environment.ProcessId,
                Environment.CurrentManagedThreadId,
                _assemblyInfo.VersionString,
                category,
                message,
                data is null ? null : new Dictionary<string, object?>(data),
                exception is null ? null : ExceptionInfo.From(exception));
    }

    private sealed record ExceptionInfo(
        // ReSharper disable NotAccessedPositionalProperty.Local
        string Type,
        string Message,
        string? StackTrace,
        int? Hresult,
        ExceptionInfo? Inner
        // ReSharper restore NotAccessedPositionalProperty.Local
    )
    {
        public static ExceptionInfo From(Exception ex)
            => new(
                ex.GetType().FullName ?? ex.GetType().Name,
                ex.Message,
                ex.StackTrace,
                ex.HResult,
                ex.InnerException is null ? null : From(ex.InnerException));
    }
}
