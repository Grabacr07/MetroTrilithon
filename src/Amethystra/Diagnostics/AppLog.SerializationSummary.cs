using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;

namespace Amethystra.Diagnostics;

partial class AppLog
{
    private sealed class SerializationSummary
    {
#if DEBUG
        private const int _topCount = 7;
        private readonly Lock _gate = new();
        private readonly List<TimingEntry> _topEntries = [];
        private long _count;
        private long _totalTicks;

        public Measurement BeginMeasurement()
            => new(this);

        public void EnqueueSummary(AppLog log)
        {
            try
            {
                List<TimingEntry> entries;
                long count;
                long totalTicks;
                lock (this._gate)
                {
                    count = this._count;
                    totalTicks = this._totalTicks;
                    entries = [..this._topEntries];
                }

                const string source = $"{nameof(SerializationSummary)}.{nameof(this.EnqueueSummary)}";
                var timestamp = DateTimeOffset.Now;
                var totalMs = totalTicks * 1000.0 / Stopwatch.Frequency;
                var averageMs = count > 0 ? totalMs / count : 0.0;
                var summary = new Dictionary<string, object?>
                {
                    ["count"] = count,
                    ["totalMs"] = totalMs,
                    ["averageMs"] = averageMs,
                    ["top"] = entries.Count,
                };
                log.Enqueue(new LogEntry(timestamp, LogLevel.Debug, "JSON serialization summary", source, log.SerializeData(summary)));

                for (var i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];
                    var elapsedMs = entry.ElapsedTicks * 1000.0 / Stopwatch.Frequency;
                    var data = new Dictionary<string, object?>
                    {
                        ["rank"] = i + 1,
                        ["elapsedMs"] = elapsedMs,
                        ["elapsedTicks"] = entry.ElapsedTicks,
                        ["json"] = TryConvertToJsonElement(entry.Json),
                    };
                    log.Enqueue(new LogEntry(timestamp, LogLevel.Debug, $"JSON serialization slow #{i + 1}", source, log.SerializeData(data)));
                }
            }
            catch
            {
                // デバッグ用サマリーなので失敗しても本処理は継続
            }
        }

        private void Complete(string? json, long elapsedTicks)
        {
            if (json is null)
            {
                return;
            }

            lock (this._gate)
            {
                this._count++;
                this._totalTicks += elapsedTicks;

                var entry = new TimingEntry(elapsedTicks, json);
                var index = this._topEntries.FindIndex(x => elapsedTicks > x.ElapsedTicks);
                if (index < 0)
                {
                    if (this._topEntries.Count < _topCount)
                    {
                        this._topEntries.Add(entry);
                    }

                    return;
                }

                this._topEntries.Insert(index, entry);
                if (this._topEntries.Count > _topCount)
                {
                    this._topEntries.RemoveAt(_topCount);
                }
            }
        }

        private static object TryConvertToJsonElement(string json)
        {
            try
            {
                using var document = JsonDocument.Parse(json);
                return document.RootElement.Clone();
            }
            catch
            {
                return json;
            }
        }

        public sealed class Measurement : IDisposable
        {
            private readonly SerializationSummary _summary;
            private readonly long _startTicks;
            private string? _json;

            internal Measurement(SerializationSummary summary)
            {
                this._summary = summary;
                this._startTicks = Stopwatch.GetTimestamp();
            }

            public void SetSerializedData(string json)
            {
                this._json = json;
            }

            public void Dispose()
            {
                var elapsedTicks = Stopwatch.GetTimestamp() - this._startTicks;
                this._summary.Complete(this._json, elapsedTicks);
            }
        }

        private readonly record struct TimingEntry(long ElapsedTicks, string Json);

#else
        public Measurement BeginMeasurement()
            => Measurement.Empty;

        public void EnqueueSummary(AppLog log)
        {
        }

        public sealed class Measurement : IDisposable
        {
            internal static readonly Measurement Empty = new();

            private Measurement()
            {
            }

            public void SetSerializedData(string json)
            {
            }

            public void Dispose()
            {
            }
        }
#endif
    }
}
