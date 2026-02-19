using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amethystra.Disposables;

namespace Amethystra.Diagnostics;

public sealed partial class AppLog : IDisposable
{
    private readonly AppLogOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly DisposeGateSlim<AppLog> _disposeGate = new();
    private readonly SerializationSummary _serializationSummary = new();
    private readonly Queueing _queueing;

    public AppLog(AppLogOptions options, params IEnumerable<JsonConverter> converters)
    {
        this._options = options;
        this._jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.General)
        {
            WriteIndented = false,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        foreach (var c in converters) this._jsonOptions.Converters.Add(c);
        this._queueing = new Queueing(this);
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
            var serialized = dictionary != null ? this.SerializeData(dictionary) : null;

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
                this.Enqueue(new LogEntry(timestamp, level, message, source, serialized, exception));
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

    private string SerializeData(Dictionary<string, object?>? data)
    {
        using var measurement = this._serializationSummary.BeginMeasurement();
        try
        {
            var json = JsonSerializer.Serialize(data, this._jsonOptions);
            measurement.SetSerializedData(json);
            return json;
        }
        catch
        {
            return "{serialization_failed}";
        }
    }

    public void Dispose()
    {
        if (this._disposeGate.TryDispose() == false) return;

        try
        {
            this._serializationSummary.EnqueueSummary(this);
            this._queueing.Dispose();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"=== UNKNOWN ERROR: {nameof(this.Dispose)} ===");
            Debug.WriteLine(ex);
        }

        Debug.WriteLine($"{nameof(AppLog)} stopped.");
    }
}
