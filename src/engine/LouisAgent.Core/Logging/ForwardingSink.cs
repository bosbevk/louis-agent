namespace LouisAgent.Core.Logging;

using Serilog.Core;
using Serilog.Events;

/// <summary>
/// The root logger's only sink: it passes each event to the current configuration, which <see cref="AgentLogging.Configure"/>
/// replaces. Loggers bind to their root when created, so this is what lets a logger made before the host configured
/// logging (a static field, a type initialised early) still write to the configured sinks.
/// </summary>
internal sealed class ForwardingSink : ILogEventSink, IDisposable
{
    private Logger? _current;

    public void Emit(LogEvent logEvent) => Volatile.Read(ref _current)?.Write(logEvent);

    /// <summary>Sends events to <paramref name="next"/> from now on, and flushes and closes the previous configuration.</summary>
    public void Replace(Logger next) => Interlocked.Exchange(ref _current, next)?.Dispose();

    public void Dispose() => Interlocked.Exchange(ref _current, null)?.Dispose();
}