using System;
using System.Collections.Generic;
using System.Text;

namespace Amethystra.Diagnostics;

partial class AppLog
{
    private sealed record LogEntry(
        DateTimeOffset Timestamp,
        LogLevel Level,
        string Message,
        string Source,
        string? SerializedData,
        Exception? Exception = null)
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

            if (string.IsNullOrWhiteSpace(this.SerializedData) == false)
            {
                sb.Append(" data=");
                sb.Append(this.SerializedData);
            }

            return sb.ToString();
        }
    }
}
