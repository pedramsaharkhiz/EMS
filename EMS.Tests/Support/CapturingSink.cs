using Serilog.Core;
using Serilog.Events;

namespace EMS.Tests.Support
{
    /// <summary>Serilog sink that keeps every event in memory so tests can assert on what was logged.</summary>
    public sealed class CapturingSink : ILogEventSink
    {
        private readonly object _gate = new();
        private readonly List<LogEvent> _events = new();

        public void Emit(LogEvent logEvent)
        {
            lock (_gate) _events.Add(logEvent);
        }

        public IReadOnlyList<LogEvent> Snapshot()
        {
            lock (_gate) return _events.ToList();
        }

        public IReadOnlyList<LogEvent> ForCorrelation(string correlationId) =>
            Snapshot().Where(e => PropertyText(e, "CorrelationId") == correlationId).ToList();

        public static string? PropertyText(LogEvent e, string name) =>
            e.Properties.TryGetValue(name, out var value) && value is ScalarValue { Value: not null } scalar
                ? scalar.Value.ToString()
                : null;

        public static string? SourceContext(LogEvent e) => PropertyText(e, "SourceContext");
    }
}
