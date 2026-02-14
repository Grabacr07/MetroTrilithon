using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Amethystra.Diagnostics;

partial class AppLog
{
    private readonly struct OperationScope : IDisposable
    {
        private readonly AppLog _log;
        private readonly string _source;
        private readonly string _name;
        private readonly Data? _data;
        private readonly long _startTimestamp;

        public OperationScope(
            AppLog log,
            string typeName,
            string name,
            Data? data,
            string? memberName)
        {
            this._log = log;
            this._source = ComposeSource(typeName, memberName);
            this._name = name;
            this._data = data;
            this._startTimestamp = Stopwatch.GetTimestamp();

            log.WriteFromOperationScope(this._source, $"Operation begin: {name}", data);
        }

        public void Dispose()
        {
            var end = Stopwatch.GetTimestamp();
            var elapsedMs = (end - this._startTimestamp) * 1000.0 / Stopwatch.Frequency;
            var data = this._data ?? [];
            data.Add(elapsedMs);

            this._log.WriteFromOperationScope(this._source, $"Operation end: {this._name}", data);
        }
    }
}
