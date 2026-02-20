using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Amethystra.Properties;

namespace Amethystra.Diagnostics;

partial class AppLog
{
    private static AppLog? _default;

    public static AppLog Default
        => _default ?? throw new InvalidOperationException($"{nameof(AppLog)}.{nameof(Default)} is not initialized. Call {nameof(AppLog)}.{nameof(CreateDefault)}(...) first.");

    public static void CreateDefault(IAssemblyInfo info, params IEnumerable<JsonConverter> converters)
        => CreateDefault(new AppLogOptions(info), converters);

    [MemberNotNull(nameof(_default))]
    public static void CreateDefault(AppLogOptions options, params IEnumerable<JsonConverter> converters)
    {
        if (Interlocked.CompareExchange(ref _default, new AppLog(options, converters), null) is not null)
        {
            throw new InvalidOperationException($"{nameof(AppLog)}.{nameof(Default)} is already initialized.");
        }

#if DEBUG
        AppDomain.CurrentDomain.FirstChanceException += (_, e) =>
        {
            _default.For<AppDomain>().Warn(e.Exception, "=== FIRST CHANCE ===", caller: nameof(AppDomain.FirstChanceException));
        };
#endif
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                _default.For<AppDomain>().Fatal(ex, "=== UNKNOWN ERROR ===", caller: nameof(AppDomain.UnhandledException));
            }
            else
            {
                _default.For<AppDomain>().Fatal("=== UNKNOWN ERROR (non-Exception) ===", new() { e.ExceptionObject }, nameof(AppDomain.UnhandledException));
            }
        };
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            _default.For<TaskScheduler>().Fatal(e.Exception, "=== UNKNOWN ERROR ===", caller: nameof(TaskScheduler.UnobservedTaskException));
            e.SetObserved();
        };

        AppDomain.CurrentDomain.ProcessExit += (_, __) => Default.Dispose();

        if (options.Warmup)
        {
            _default.SerializeData(new Dictionary<string, object?> { [_systemSource] = nameof(options.Warmup), });
        }
    }
}
