using Microsoft.Extensions.Logging;

namespace LouisAgent.Evals.Tests.Running;

/// <summary>Keeps what the runner logs, formatted and by level, so a test can read a warning.</summary>
internal sealed class CapturingLogger : ILogger
{
    public List<string> Warnings { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (logLevel == LogLevel.Warning) Warnings.Add(formatter(state, exception));
    }
}
