// =============================================================================
// ShadowProtocol.Core — Logging: Structured logger with sinks and ring buffer
// =============================================================================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace ShadowProtocol.Core.Logging
{
    /// <summary>
    /// Log severity levels in ascending order of importance.
    /// </summary>
    public enum LogLevel
    {
        Trace   = 0,
        Debug   = 1,
        Info    = 2,
        Warning = 3,
        Error   = 4,
        Fatal   = 5,
    }

    /// <summary>
    /// Represents a single log entry with structured data.
    /// </summary>
    public readonly struct LogEntry
    {
        /// <summary>When this log entry was created.</summary>
        public readonly DateTime Timestamp;
        /// <summary>Severity level.</summary>
        public readonly LogLevel Level;
        /// <summary>Source category/system name.</summary>
        public readonly string Category;
        /// <summary>Log message.</summary>
        public readonly string Message;
        /// <summary>Optional exception associated with this log.</summary>
        public readonly Exception Exception;
        /// <summary>Optional key-value properties for structured logging.</summary>
        public readonly Dictionary<string, object> Properties;
        /// <summary>Thread that generated this log entry.</summary>
        public readonly int ThreadId;
        /// <summary>Frame number when this log was generated (game-specific).</summary>
        public readonly long FrameNumber;

        public LogEntry(LogLevel level, string category, string message,
            Exception exception = null, Dictionary<string, object> properties = null,
            long frameNumber = 0)
        {
            Timestamp = DateTime.UtcNow;
            Level = level;
            Category = category ?? "General";
            Message = message ?? string.Empty;
            Exception = exception;
            Properties = properties;
            ThreadId = Thread.CurrentThread.ManagedThreadId;
            FrameNumber = frameNumber;
        }

        /// <summary>Formats the log entry as a human-readable string.</summary>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"[{Timestamp:HH:mm:ss.fff}]");
            sb.Append($" [{Level,-7}]");
            sb.Append($" [{Category}]");
            sb.Append($" {Message}");

            if (Properties != null && Properties.Count > 0)
            {
                sb.Append(" {");
                sb.Append(string.Join(", ", Properties.Select(p => $"{p.Key}={p.Value}")));
                sb.Append('}');
            }

            if (Exception != null)
            {
                sb.AppendLine();
                sb.Append($"  Exception: {Exception.GetType().Name}: {Exception.Message}");
                if (Exception.StackTrace != null)
                {
                    sb.AppendLine();
                    sb.Append($"  StackTrace: {Exception.StackTrace}");
                }
            }

            return sb.ToString();
        }

        /// <summary>Formats the log entry as JSON for structured logging pipelines.</summary>
        public string ToJson()
        {
            var sb = new StringBuilder();
            sb.Append('{');
            sb.Append($"\"timestamp\":\"{Timestamp:O}\",");
            sb.Append($"\"level\":\"{Level}\",");
            sb.Append($"\"category\":\"{EscapeJson(Category)}\",");
            sb.Append($"\"message\":\"{EscapeJson(Message)}\",");
            sb.Append($"\"threadId\":{ThreadId},");
            sb.Append($"\"frame\":{FrameNumber}");

            if (Properties != null && Properties.Count > 0)
            {
                sb.Append(",\"properties\":{");
                bool first = true;
                foreach (var p in Properties)
                {
                    if (!first) sb.Append(',');
                    sb.Append($"\"{EscapeJson(p.Key)}\":\"{EscapeJson(p.Value?.ToString())}\"");
                    first = false;
                }
                sb.Append('}');
            }

            if (Exception != null)
            {
                sb.Append($",\"exception\":{{\"type\":\"{Exception.GetType().Name}\",\"message\":\"{EscapeJson(Exception.Message)}\"}}");
            }

            sb.Append('}');
            return sb.ToString();
        }

        private static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"")
                       .Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }
    }

    /// <summary>
    /// Interface for log output destinations (console, file, network, etc.).
    /// </summary>
    public interface ILogSink : IDisposable
    {
        /// <summary>Minimum log level this sink will accept.</summary>
        LogLevel MinLevel { get; set; }
        /// <summary>Whether this sink is currently enabled.</summary>
        bool Enabled { get; set; }
        /// <summary>Writes a log entry to this sink.</summary>
        void Write(in LogEntry entry);
        /// <summary>Flushes any buffered log data.</summary>
        void Flush();
    }

    /// <summary>
    /// Console log sink with colored output based on severity level.
    /// </summary>
    public class ConsoleSink : ILogSink
    {
        public LogLevel MinLevel { get; set; } = LogLevel.Debug;
        public bool Enabled { get; set; } = true;
        public bool UseColors { get; set; } = true;

        private static readonly Dictionary<LogLevel, ConsoleColor> _colors = new()
        {
            [LogLevel.Trace] = ConsoleColor.DarkGray,
            [LogLevel.Debug] = ConsoleColor.Gray,
            [LogLevel.Info] = ConsoleColor.White,
            [LogLevel.Warning] = ConsoleColor.Yellow,
            [LogLevel.Error] = ConsoleColor.Red,
            [LogLevel.Fatal] = ConsoleColor.DarkRed,
        };

        public void Write(in LogEntry entry)
        {
            if (!Enabled || entry.Level < MinLevel) return;

            if (UseColors)
            {
                var prevColor = Console.ForegroundColor;
                Console.ForegroundColor = _colors.GetValueOrDefault(entry.Level, ConsoleColor.White);
                Console.WriteLine(entry.ToString());
                Console.ForegroundColor = prevColor;
            }
            else
            {
                Console.WriteLine(entry.ToString());
            }
        }

        public void Flush() { }
        public void Dispose() { }
    }

    /// <summary>
    /// File log sink that writes to a rotating log file.
    /// Supports file size limits and automatic rotation.
    /// </summary>
    public class FileSink : ILogSink
    {
        public LogLevel MinLevel { get; set; } = LogLevel.Debug;
        public bool Enabled { get; set; } = true;

        private readonly string _basePath;
        private readonly long _maxFileSizeBytes;
        private readonly int _maxFiles;
        private readonly bool _useJson;
        private StreamWriter _writer;
        private long _currentFileSize;
        private readonly object _lock = new();

        /// <summary>
        /// Creates a file log sink.
        /// </summary>
        /// <param name="basePath">Base file path (without extension for rotation).</param>
        /// <param name="maxFileSizeMB">Maximum file size before rotation.</param>
        /// <param name="maxFiles">Maximum number of rotated log files to keep.</param>
        /// <param name="useJson">If true, writes JSON-formatted entries.</param>
        public FileSink(string basePath, int maxFileSizeMB = 10, int maxFiles = 5, bool useJson = false)
        {
            _basePath = basePath;
            _maxFileSizeBytes = maxFileSizeMB * 1024L * 1024L;
            _maxFiles = maxFiles;
            _useJson = useJson;
            OpenFile();
        }

        public void Write(in LogEntry entry)
        {
            if (!Enabled || entry.Level < MinLevel) return;

            lock (_lock)
            {
                string line = _useJson ? entry.ToJson() : entry.ToString();
                _writer?.WriteLine(line);
                _currentFileSize += line.Length + Environment.NewLine.Length;

                if (_currentFileSize >= _maxFileSizeBytes)
                    RotateFiles();
            }
        }

        public void Flush()
        {
            lock (_lock) { _writer?.Flush(); }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _writer?.Flush();
                _writer?.Dispose();
                _writer = null;
            }
        }

        private void OpenFile()
        {
            string dir = Path.GetDirectoryName(_basePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string path = _basePath + ".log";
            _writer = new StreamWriter(path, append: true, Encoding.UTF8);
            _writer.AutoFlush = true;
            _currentFileSize = new FileInfo(path).Length;
        }

        private void RotateFiles()
        {
            _writer?.Dispose();

            // Shift existing files
            for (int i = _maxFiles - 1; i >= 1; i--)
            {
                string src = $"{_basePath}.{i}.log";
                string dst = $"{_basePath}.{i + 1}.log";
                if (File.Exists(dst)) File.Delete(dst);
                if (File.Exists(src)) File.Move(src, dst);
            }

            // Rename current to .1
            string currentPath = _basePath + ".log";
            string firstRotated = $"{_basePath}.1.log";
            if (File.Exists(firstRotated)) File.Delete(firstRotated);
            if (File.Exists(currentPath)) File.Move(currentPath, firstRotated);

            // Delete excess files
            for (int i = _maxFiles + 1; i < _maxFiles + 10; i++)
            {
                string old = $"{_basePath}.{i}.log";
                if (File.Exists(old)) File.Delete(old);
            }

            OpenFile();
        }
    }

    /// <summary>
    /// In-memory ring buffer sink for recent log access (e.g., in-game console).
    /// Thread-safe circular buffer that keeps the most recent N entries.
    /// </summary>
    public class RingBufferSink : ILogSink
    {
        public LogLevel MinLevel { get; set; } = LogLevel.Trace;
        public bool Enabled { get; set; } = true;

        private readonly LogEntry[] _buffer;
        private int _head;
        private int _count;
        private readonly object _lock = new();

        /// <summary>Number of entries currently in the buffer.</summary>
        public int Count
        {
            get { lock (_lock) return _count; }
        }

        /// <summary>Maximum capacity of the ring buffer.</summary>
        public int Capacity => _buffer.Length;

        public RingBufferSink(int capacity = 1000)
        {
            _buffer = new LogEntry[capacity];
        }

        public void Write(in LogEntry entry)
        {
            if (!Enabled || entry.Level < MinLevel) return;

            lock (_lock)
            {
                _buffer[_head] = entry;
                _head = (_head + 1) % _buffer.Length;
                if (_count < _buffer.Length) _count++;
            }
        }

        /// <summary>
        /// Returns all entries in the buffer, oldest first.
        /// </summary>
        public LogEntry[] GetEntries()
        {
            lock (_lock)
            {
                var result = new LogEntry[_count];
                int start = _count < _buffer.Length ? 0 : _head;
                for (int i = 0; i < _count; i++)
                {
                    result[i] = _buffer[(start + i) % _buffer.Length];
                }
                return result;
            }
        }

        /// <summary>
        /// Returns entries matching a filter predicate.
        /// </summary>
        public LogEntry[] GetEntries(Func<LogEntry, bool> filter)
        {
            return GetEntries().Where(filter).ToArray();
        }

        /// <summary>
        /// Returns entries of a specific level or higher.
        /// </summary>
        public LogEntry[] GetEntries(LogLevel minLevel)
        {
            return GetEntries(e => e.Level >= minLevel);
        }

        /// <summary>
        /// Returns entries from a specific category.
        /// </summary>
        public LogEntry[] GetEntriesByCategory(string category)
        {
            return GetEntries(e => e.Category == category);
        }

        /// <summary>Clears all entries.</summary>
        public void Clear()
        {
            lock (_lock) { _count = 0; _head = 0; }
        }

        public void Flush() { }
        public void Dispose() { }
    }

    /// <summary>
    /// Callback sink that invokes a delegate for each log entry.
    /// Useful for forwarding logs to UI elements or analytics.
    /// </summary>
    public class CallbackSink : ILogSink
    {
        public LogLevel MinLevel { get; set; } = LogLevel.Info;
        public bool Enabled { get; set; } = true;

        private readonly Action<LogEntry> _callback;

        public CallbackSink(Action<LogEntry> callback)
        {
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        public void Write(in LogEntry entry)
        {
            if (Enabled && entry.Level >= MinLevel)
                _callback(entry);
        }

        public void Flush() { }
        public void Dispose() { }
    }

    /// <summary>
    /// The central logging system for the game engine.
    /// Manages multiple sinks and provides scoped logging categories.
    /// Thread-safe for use across game systems.
    /// </summary>
    public sealed class Logger : IDisposable
    {
        private static Logger _instance;
        private static readonly object _instanceLock = new();

        private readonly List<ILogSink> _sinks = new();
        private readonly ConcurrentDictionary<string, CategoryLogger> _categories = new();
        private LogLevel _globalMinLevel = LogLevel.Trace;
        private long _currentFrame;

        /// <summary>Global minimum log level. Entries below this are discarded before reaching sinks.</summary>
        public LogLevel GlobalMinLevel
        {
            get => _globalMinLevel;
            set => _globalMinLevel = value;
        }

        /// <summary>Current game frame number (set by the game loop).</summary>
        public long CurrentFrame
        {
            get => _currentFrame;
            set => _currentFrame = value;
        }

        /// <summary>Gets the singleton logger instance.</summary>
        public static Logger Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_instanceLock)
                    {
                        _instance ??= new Logger();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Adds a sink to the logging pipeline.
        /// </summary>
        public Logger AddSink(ILogSink sink)
        {
            _sinks.Add(sink);
            return this;
        }

        /// <summary>
        /// Removes a sink from the pipeline.
        /// </summary>
        public void RemoveSink(ILogSink sink)
        {
            _sinks.Remove(sink);
            sink.Dispose();
        }

        /// <summary>
        /// Gets or creates a category-scoped logger.
        /// </summary>
        public CategoryLogger GetCategory(string category)
        {
            return _categories.GetOrAdd(category, c => new CategoryLogger(this, c));
        }

        /// <summary>
        /// Writes a log entry to all enabled sinks.
        /// </summary>
        public void Log(in LogEntry entry)
        {
            if (entry.Level < _globalMinLevel) return;

            for (int i = 0; i < _sinks.Count; i++)
            {
                try
                {
                    _sinks[i].Write(entry);
                }
                catch
                {
                    // Never let a sink failure crash the game
                }
            }
        }

        // Convenience methods
        public void Trace(string category, string message) =>
            Log(new LogEntry(LogLevel.Trace, category, message, frameNumber: _currentFrame));
        public void Debug(string category, string message) =>
            Log(new LogEntry(LogLevel.Debug, category, message, frameNumber: _currentFrame));
        public void Info(string category, string message) =>
            Log(new LogEntry(LogLevel.Info, category, message, frameNumber: _currentFrame));
        public void Warning(string category, string message) =>
            Log(new LogEntry(LogLevel.Warning, category, message, frameNumber: _currentFrame));
        public void Error(string category, string message, Exception ex = null) =>
            Log(new LogEntry(LogLevel.Error, category, message, ex, frameNumber: _currentFrame));
        public void Fatal(string category, string message, Exception ex = null) =>
            Log(new LogEntry(LogLevel.Fatal, category, message, ex, frameNumber: _currentFrame));

        /// <summary>
        /// Logs with structured properties for analytics and diagnostics.
        /// </summary>
        public void LogStructured(LogLevel level, string category, string message,
            Dictionary<string, object> properties)
        {
            Log(new LogEntry(level, category, message, properties: properties, frameNumber: _currentFrame));
        }

        /// <summary>Flushes all sinks.</summary>
        public void FlushAll()
        {
            foreach (var sink in _sinks)
            {
                try { sink.Flush(); }
                catch { }
            }
        }

        public void Dispose()
        {
            FlushAll();
            foreach (var sink in _sinks)
            {
                try { sink.Dispose(); }
                catch { }
            }
            _sinks.Clear();
        }
    }

    /// <summary>
    /// A category-scoped logger that automatically tags all entries with a category name.
    /// </summary>
    public class CategoryLogger
    {
        private readonly Logger _parent;
        private readonly string _category;

        internal CategoryLogger(Logger parent, string category)
        {
            _parent = parent;
            _category = category;
        }

        public void Trace(string message) => _parent.Trace(_category, message);
        public void Debug(string message) => _parent.Debug(_category, message);
        public void Info(string message) => _parent.Info(_category, message);
        public void Warning(string message) => _parent.Warning(_category, message);
        public void Error(string message, Exception ex = null) => _parent.Error(_category, message, ex);
        public void Fatal(string message, Exception ex = null) => _parent.Fatal(_category, message, ex);

        public void LogStructured(LogLevel level, string message, Dictionary<string, object> properties) =>
            _parent.LogStructured(level, _category, message, properties);
    }

    /// <summary>
    /// Scoped performance timer that logs elapsed time on disposal.
    /// Usage: using var _ = PerformanceScope.Begin("AI.Update");
    /// </summary>
    public sealed class PerformanceScope : IDisposable
    {
        private readonly string _name;
        private readonly DateTime _start;
        private readonly CategoryLogger _logger;

        private PerformanceScope(string name, CategoryLogger logger)
        {
            _name = name;
            _logger = logger;
            _start = DateTime.UtcNow;
        }

        /// <summary>Begins a named performance timing scope.</summary>
        public static PerformanceScope Begin(string name)
        {
            return new PerformanceScope(name, Logger.Instance.GetCategory("Performance"));
        }

        public void Dispose()
        {
            var elapsed = DateTime.UtcNow - _start;
            _logger.LogStructured(
                elapsed.TotalMilliseconds > 16.67 ? LogLevel.Warning : LogLevel.Debug,
                $"{_name} completed",
                new Dictionary<string, object>
                {
                    ["scope"] = _name,
                    ["elapsedMs"] = elapsed.TotalMilliseconds,
                    ["isSlow"] = elapsed.TotalMilliseconds > 16.67
                }
            );
        }
    }

    /// <summary>
    /// Rate-limited logger that prevents log spam.
    /// Throttles repeated messages from the same source.
    /// </summary>
    public class RateLimitedLogger
    {
        private readonly CategoryLogger _logger;
        private readonly Dictionary<string, DateTime> _lastLogTimes = new();
        private readonly float _minIntervalSeconds;

        public RateLimitedLogger(CategoryLogger logger, float minIntervalSeconds = 1f)
        {
            _logger = logger;
            _minIntervalSeconds = minIntervalSeconds;
        }

        public void LogThrottled(LogLevel level, string key, string message)
        {
            var now = DateTime.UtcNow;
            if (_lastLogTimes.TryGetValue(key, out var lastTime))
            {
                if ((now - lastTime).TotalSeconds < _minIntervalSeconds)
                    return;
            }
            _lastLogTimes[key] = now;

            switch (level)
            {
                case LogLevel.Debug: _logger.Debug(message); break;
                case LogLevel.Info: _logger.Info(message); break;
                case LogLevel.Warning: _logger.Warning(message); break;
                case LogLevel.Error: _logger.Error(message); break;
                default: _logger.Trace(message); break;
            }
        }
    }
}
