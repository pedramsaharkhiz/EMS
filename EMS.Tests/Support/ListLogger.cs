using Microsoft.Extensions.Logging;

namespace EMS.Tests.Support
{
    public sealed record LogEntry(string Category, LogLevel Level, string Message, Exception? Exception);

    /// <summary>Minimal in-memory Microsoft.Extensions.Logging implementation for unit tests.</summary>
    public sealed class ListLoggerFactory : ILoggerFactory
    {
        public List<LogEntry> Entries { get; } = new();

        public ILogger CreateLogger(string categoryName) => new ListLogger(categoryName, Entries);
        public ILogger<T> CreateLogger<T>() => new ListLogger<T>(Entries);
        public void AddProvider(ILoggerProvider provider) { }
        public void Dispose() { }
    }

    public class ListLogger : ILogger
    {
        private readonly string _category;
        private readonly List<LogEntry> _entries;

        public ListLogger(string category, List<LogEntry> entries)
        {
            _category = category;
            _entries = entries;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            _entries.Add(new LogEntry(_category, logLevel, formatter(state, exception), exception));
        }
    }

    public sealed class ListLogger<T> : ListLogger, ILogger<T>
    {
        public ListLogger(List<LogEntry> entries) : base(typeof(T).FullName!, entries) { }
    }
}
