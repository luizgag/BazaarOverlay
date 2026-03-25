using Microsoft.Extensions.Logging;

namespace BazaarOverlay.Tests.Helpers;

public record LogEntry(LogLevel Level, string Message, Exception? Exception);

public class TestLogger<T> : ILogger<T>
{
    public List<LogEntry> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Entries.Add(new LogEntry(logLevel, formatter(state, exception), exception));
    }

    public bool HasEntry(LogLevel level, string messageContains)
        => Entries.Any(e => e.Level == level && e.Message.Contains(messageContains, StringComparison.OrdinalIgnoreCase));
}
