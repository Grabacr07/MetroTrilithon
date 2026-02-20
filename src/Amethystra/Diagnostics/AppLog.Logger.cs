using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Amethystra.Diagnostics;

partial class AppLog
{
    public Logger For<T>()
        => new(this, typeof(T).Name);

    public Logger For(Type type)
        => new(this, type.Name);

    public readonly struct Logger(AppLog log, string typeName)
    {
        [Conditional("DEBUG")]
        public void Debug(
            string message,
            Data? data = null,
            [CallerMemberName] string? caller = null)
            => log.WriteFromTypedLogger(LogLevel.Debug, message, null, typeName, caller, data);

        public void Info(
            string message,
            Data? data = null,
            [CallerMemberName] string? caller = null)
            => log.WriteFromTypedLogger(LogLevel.Info, message, null, typeName, caller, data);

        public void Warn(
            string message,
            Data? data = null,
            [CallerMemberName] string? caller = null)
            => log.WriteFromTypedLogger(LogLevel.Warn, message, null, typeName, caller, data);

        public void Warn(
            Exception exception,
            string message,
            Data? data = null,
            [CallerMemberName] string? caller = null)
            => log.WriteFromTypedLogger(LogLevel.Warn, message, exception, typeName, caller, data);

        public void Error(
            Exception exception,
            string message,
            Data? data = null,
            [CallerMemberName] string? caller = null)
            => log.WriteFromTypedLogger(LogLevel.Error, message, exception, typeName, caller, data);

        public void Error(
            string message,
            Data? data = null,
            [CallerMemberName] string? caller = null)
            => log.WriteFromTypedLogger(LogLevel.Error, message, null, typeName, caller, data);

        public void Fatal(
            Exception exception,
            string message,
            Data? data = null,
            [CallerMemberName] string? caller = null)
            => log.WriteFromTypedLogger(LogLevel.Fatal, message, exception, typeName, caller, data);

        public void Fatal(
            string message,
            Data? data = null,
            [CallerMemberName] string? caller = null)
            => log.WriteFromTypedLogger(LogLevel.Fatal, message, null, typeName, caller, data);

        public IDisposable BeginOperation(
            string operationName,
            Data? data = null,
            [CallerMemberName] string? caller = null)
            => new OperationScope(log, typeName, operationName, data, caller);
    }

    public sealed class Data : IEnumerable<(string key, object? value)>
    {
        private readonly List<(string key, object? value)> _items = [];

        public int Count
            => this._items.Count;

        public void Add<T>(T value, [CallerArgumentExpression(nameof(value))] string? key = null)
            => this._items.Add((key ?? string.Empty, value));

        public Dictionary<string, object?> ToDictionary()
            => this._items.ToDictionary();

        IEnumerator<(string key, object? value)> IEnumerable<(string key, object? value)>.GetEnumerator()
            => this._items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => this._items.GetEnumerator();
    }
}
