using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LouisAgent.Composition.Logging;

/// <summary>
/// A logger for a pack. It writes wherever the running host set up logging, and a pack needs nothing of the engine's to
/// use it. With no host, as in a pack's own tests, the lines go nowhere.
/// </summary>
/// <remarks>
/// Each logger looks up the factory when it writes, because a pack's logger is often a static field, made before the host
/// has set logging up.
/// </remarks>
public static class PackLog
{
    private static ILoggerFactory _factory = NullLoggerFactory.Instance;

    /// <summary>Where every pack's lines go from now on. The engine calls this as it starts.</summary>
    public static void Use(ILoggerFactory factory) => _factory = factory;

    /// <summary>A logger named for <typeparamref name="T"/>.</summary>
    public static ILogger For<T>() => new Forwarding(typeof(T).FullName ?? typeof(T).Name);

    /// <summary>Hands every call to the current factory's logger of that name.</summary>
    private sealed class Forwarding(string category) : ILogger
    {
        private ILogger Current => _factory.CreateLogger(category);

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => Current.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => Current.IsEnabled(logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Current.Log(logLevel, eventId, state, exception, formatter);
    }
}
