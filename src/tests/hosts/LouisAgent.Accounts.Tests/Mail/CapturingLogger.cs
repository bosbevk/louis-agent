using Microsoft.Extensions.Logging;

namespace LouisAgent.Accounts.Tests.Mail;

/// <summary>Keeps what was logged, formatted, so a test can read the line the sender wrote.</summary>
internal sealed class CapturingLogger<T> : ILogger<T>
{
    public List<string> Lines { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter) => Lines.Add(formatter(state, exception));
}
