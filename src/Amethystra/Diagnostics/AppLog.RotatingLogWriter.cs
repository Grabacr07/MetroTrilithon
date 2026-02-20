using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Mio;
using Mio.Destructive;

namespace Amethystra.Diagnostics;

partial class AppLog
{
    private sealed class RotatingLogWriter : IDisposable
    {
        private const uint _flushLineThreshold = 64;
        private const long _flushTimeThresholdMs = 500;
        private readonly AppLogOptions _options;
        private readonly Stopwatch _lastRotateCheck = Stopwatch.StartNew();
        private readonly Stopwatch _lastFlush = Stopwatch.StartNew();
        private FileStream? _stream;
        private StreamWriter? _writer;
        private int _linesSinceFlush;

        public RotatingLogWriter(AppLogOptions options)
        {
            this._options = options;
            this.OpenWriter();
        }

        public void Write(LogEntry entry)
        {
            this.WriteLine(entry.FormattedText);
            if (entry.ExceptionText is not null)
            {
                this.WriteLine(entry.ExceptionText);
            }
        }

        public void WriteLine(string line)
        {
            this.GetWriter().WriteLine(line);
            this._linesSinceFlush++;

            this.FlushIfNeeded();
            this.RotateIfNeeded();
        }

        private void FlushIfNeeded()
        {
            if (this._linesSinceFlush >= _flushLineThreshold
                || this._lastFlush.ElapsedMilliseconds >= _flushTimeThresholdMs)
            {
                this.Flush(false);
            }
        }

        private void RotateIfNeeded()
        {
            if (this._lastRotateCheck.ElapsedMilliseconds < 1000
                || this.GetStream().Length <= this._options.MaxLogBytes)
            {
                return;
            }

            try
            {
                this.Flush(true);
                this.CloseWriter();

                this.RotateGenerations(this._options.LogFilePath);
            }
            catch (Exception ex)
            {
                // ローテーションに失敗してもログ書き込み自体は続行したい
                Debug.WriteLine(ex);
            }
            finally
            {
                this.OpenWriter();
            }
        }

        private void Flush(bool flushToDisk)
        {
            var writer = this._writer;
            var stream = this._stream;
            if (writer is null || stream is null) return;

            writer.Flush();
            stream.Flush(flushToDisk);

            this._lastFlush.Restart();
            this._linesSinceFlush = 0;
        }

        private void RotateGenerations(FilePath currentPath)
        {
            if (currentPath.Exists() == false) return;

            var directory = currentPath.Parent;
            var baseName = currentPath.NameWithoutExtension;
            var extension = currentPath.Extension;

            var oldest = directory.ChildFile($"{baseName}.{this._options.MaxGenerations}{extension}");
            if (oldest.Exists()) oldest.AsDestructive().Delete();

            for (var i = this._options.MaxGenerations - 1; i >= 1; i--)
            {
                var src = directory.ChildFile($"{baseName}.{i}{extension}");
                if (src.Exists() == false)
                {
                    continue;
                }

                var dst = directory.ChildFile($"{baseName}.{i + 1}{extension}");
                if (dst.Exists()) dst.AsDestructive().Delete();

                src.AsDestructive().MoveTo(dst.AsDestructive());
            }

            var first = directory.ChildFile($"{baseName}.1{extension}");
            if (first.Exists()) first.AsDestructive().Delete();

            currentPath.AsDestructive().MoveTo(first.AsDestructive());
        }

        [MemberNotNull(nameof(_stream), nameof(_writer))]
        private void OpenWriter()
        {
            this._stream?.Close();
            this._stream = new FileStream(this._options.LogFilePath.AsDestructive().FullName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);

            this._writer?.Close();
            this._writer = new StreamWriter(this._stream, this._options.Encoding);
        }

        private void CloseWriter()
        {
            this._writer?.Close();
            this._writer = null;

            this._stream?.Close();
            this._stream = null;
        }

        private FileStream GetStream()
            => this._stream ?? throw new InvalidOperationException("Failed to initialize log stream.");

        private StreamWriter GetWriter()
            => this._writer ?? throw new InvalidOperationException("Failed to initialize log writer.");

        public void Dispose()
        {
            this.Flush(true);
            this.CloseWriter();
        }
    }
}
