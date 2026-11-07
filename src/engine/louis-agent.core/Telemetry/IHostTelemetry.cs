using Serilog;

namespace LouisAgent.Core.Telemetry;

/// <summary>
/// What a host does with telemetry at start-up, and all core knows of it (CL33): a sink for the log lines, the
/// tracer to start once the settings have loaded, and a flush before the process ends. The engine itself only opens
/// spans on <see cref="TurnSpans.Source"/>; whether anything listens is this seam's business.
/// </summary>
/// <remarks>
/// Separated Interface (Fowler): the interface lives with the code that needs it, the OpenTelemetry side lives in
/// <c>louis-agent.core.telemetry</c>, so core ships no tracer and a host opts in through <c>HostServices</c>.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0064-observability-through-opentelemetry.md">ADR 0064: observability through OpenTelemetry</seealso>
public interface IHostTelemetry
{
    /// <summary>Adds the sinks the log lines go to besides the console and the file, if any.</summary>
    void AddSinks(LoggerConfiguration logging);

    /// <summary>
    /// Starts the process's tracing, after the .env files loaded; disposing the handle stops it, the process's exit
    /// does that too.
    /// </summary>
    IDisposable Start();

    /// <summary>Sends what the tracer still holds, waiting a few seconds at most: a crash's last word.</summary>
    void Flush();
}
