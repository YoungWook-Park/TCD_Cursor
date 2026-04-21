using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Tcd.Core.Logging
{
    /// <summary>
    /// Date-based rolling file sink. One line per entry, batch append.
    /// </summary>
    public sealed class FileLogSink : ILogSink
    {
        private readonly string _directory;
        private readonly string _fileNamePrefix;
        private readonly object _lock = new object();
        private string _currentDate;
        private StreamWriter _writer;
        private readonly Encoding _encoding;

        public LogLevel MinLevel { get; }

        public FileLogSink(string directory, string fileNamePrefix = "log", LogLevel minLevel = LogLevel.Debug)
        {
            _directory = directory ?? Path.GetTempPath();
            _fileNamePrefix = string.IsNullOrEmpty(fileNamePrefix) ? "log" : fileNamePrefix;
            MinLevel = minLevel;
            _encoding = new UTF8Encoding(false);
            _currentDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
        }

        public Task WriteBatchAsync(IReadOnlyList<LogEntry> entries, CancellationToken cancellationToken)
        {
            if (entries == null || entries.Count == 0)
                return Task.CompletedTask;

            lock (_lock)
            {
                var date = DateTime.UtcNow.ToString("yyyy-MM-dd");
                if (date != _currentDate)
                {
                    CloseWriter();
                    _currentDate = date;
                }

                if (_writer == null)
                {
                    try
                    {
                        if (!Directory.Exists(_directory))
                            Directory.CreateDirectory(_directory);
                        var path = Path.Combine(_directory, $"{_fileNamePrefix}_{_currentDate}.log");
                        var fs = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read);
                        _writer = new StreamWriter(fs, _encoding) { AutoFlush = true };
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"FileLogSink: {ex.Message}");
                        return Task.CompletedTask;
                    }
                }

                var sb = new StringBuilder();
                foreach (var e in entries)
                {
                    sb.Clear();
                    sb.Append(e.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                    sb.Append(" [").Append(e.Level).Append("]");
                    if (!string.IsNullOrEmpty(e.SequenceKey)) sb.Append(" [").Append(e.SequenceKey).Append("]");
                    if (e.RunId.HasValue) sb.Append(" [").Append(e.RunId.Value.ToString("N")).Append("]");
                    if (!string.IsNullOrEmpty(e.StepName)) sb.Append(" ").Append(e.StepName);
                    if (!string.IsNullOrEmpty(e.AxisName)) sb.Append(" axis=").Append(e.AxisName);
                    sb.Append(" ").Append(e.Message);
                    if (!string.IsNullOrEmpty(e.ExceptionMessage)) sb.Append(" | ").Append(e.ExceptionMessage);
                    _writer.WriteLine(sb.ToString());
                }
            }

            return Task.CompletedTask;
        }

        private void CloseWriter()
        {
            if (_writer != null)
            {
                try { _writer.Dispose(); } catch { }
                _writer = null;
            }
        }

        public void Dispose()
        {
            lock (_lock) { CloseWriter(); }
        }
    }
}
