using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Amethystra.Properties;
using Mio;
using Mio.Destructive;

namespace Amethystra.Diagnostics;

public static partial class AppLog
{
    internal enum LogLevel
    {
        Debug,
        Info,
        Warn,
        Error,
        Fatal,
    }

    private const long _maxLogBytes = 10L * 1024L * 1024L;
    private const int _maxGenerations = 5;

    private static readonly Lock _gate = new();
    private static readonly Encoding _utf8NoBom = new UTF8Encoding(false);
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.General)
    {
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static FilePath _defaultLogFilePath = CreatePath(ThisAssembly.Info);
    private static bool _isInitialized;

    private static FilePath LogFilePath
        => _overrideLogFilePath ?? _defaultLogFilePath;

    private static long MaxLogBytes
        => _overrideMaxLogBytes ?? _maxLogBytes;

    private static int MaxGenerations
        => _overrideMaxGenerations ?? _maxGenerations;

    public static void Initialize(IAssemblyInfo info, Logger logger, params IEnumerable<JsonConverter> converters)
    {
        if (Interlocked.Exchange(ref _isInitialized, true)) return;

        _defaultLogFilePath = CreatePath(info);
        foreach (var converter in converters) _jsonOptions.Converters.Add(converter);

        logger.Info("Logging started", new() { info.Product, info.VersionString, Environment.ProcessId, Environment.OSVersion.VersionString });

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                logger.Fatal(ex, "UnhandledException", caller: nameof(AppDomain.UnhandledException));
            }
            else
            {
                logger.Fatal("UnhandledException (non-Exception)", new() { e.ExceptionObject }, nameof(AppDomain.UnhandledException));
            }
        };
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            logger.Fatal(e.Exception, "UnobservedTaskException", caller: nameof(TaskScheduler.UnobservedTaskException));
            e.SetObserved();
        };
    }

    private static FilePath CreatePath(IAssemblyInfo info)
        => new DirectoryPath(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))
            .ChildDirectory(info.Company)
            .ChildDirectory(info.Product)
            .EnsureCreated()
            .ChildFile($"{Assembly.GetEntryAssembly()?.GetName().Name}.{nameof(FailSafeLog)}.json");

    private static void WriteFromTypedLogger(
        LogLevel level,
        string message,
        Exception? exception,
        string typeName,
        string? memberName,
        Data? data)
        => WriteCore(level, message, exception, ComposeSource(typeName, memberName), data);

    private static void WriteFromOperationScope(
        string source,
        string message,
        Data? data)
        => WriteCore(LogLevel.Info, message, null, source, data);

    private static void WriteCore(
        LogLevel level,
        string message,
        Exception? exception,
        string source,
        Data? data)
    {
        try
        {
            Debug.WriteLine(FormatHeader(level, message, source, data));
            if (exception is not null) Debug.WriteLine(exception.ToString());

            switch (level)
            {
                case LogLevel.Fatal:
                    FailSafeLog.Fatal(message, exception, source, data?.ToDictionary());
                    break;
                case LogLevel.Error:
                    FailSafeLog.Error(message, exception, source, data?.ToDictionary());
                    break;
                case LogLevel.Debug:
                    return; // Debug ログはテキストには書かなくていいでしょう…
            }

            lock (_gate)
            {
                LogFilePath.Parent.EnsureCreated();
                TryRotateLogFileIfNeeded();

                using var stream = new FileStream(LogFilePath.AsDestructive().FullName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                using var writer = new StreamWriter(stream, _utf8NoBom);
                writer.WriteLine(FormatHeader(level, message, source, data));
                if (exception is not null) writer.WriteLine(exception.ToString());

                writer.Flush();
                stream.Flush(true);
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

    private static string FormatHeader(
        LogLevel level,
        string message,
        string source,
        Data? data)
    {
        var ts = DateTimeOffset.Now.ToString("O");
        var pid = Environment.ProcessId;
        var tid = Environment.CurrentManagedThreadId;
        var sb = new StringBuilder();
        sb.Append(ts);
        sb.Append(' ');
        sb.Append('[').Append(level).Append(']');
        sb.Append(" pid=").Append(pid);
        sb.Append(" tid=").Append(tid);
        sb.Append(" src=").Append(source);
        sb.Append(' ');
        sb.Append(message);

        if (data is not null && data.Count > 0)
        {
            sb.Append(" data=");
            sb.Append(SerializeData(data.ToDictionary()));
        }

        return sb.ToString();
    }

    private static string SerializeData(Dictionary<string, object?> data)
    {
        try
        {
            return JsonSerializer.Serialize(data, _jsonOptions);
        }
        catch
        {
            return "{serialization_failed}";
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
            // ローテーションに失敗してもログ書き込み自体は続行したい
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
            if (src.Exists() == false)
            {
                continue;
            }

            var dst = directory.ChildFile($"{baseName}.{i + 1}{extension}");
            if (dst.Exists())
            {
                dst.AsDestructive().Delete();
            }

            src.AsDestructive().MoveTo(dst.AsDestructive());
        }

        var first = directory.ChildFile($"{baseName}.1{extension}");
        if (first.Exists())
        {
            first.AsDestructive().Delete();
        }

        currentPath.AsDestructive().MoveTo(first.AsDestructive());
    }
}
