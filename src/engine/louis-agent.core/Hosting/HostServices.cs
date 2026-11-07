using LouisAgent.Core.Telemetry;

namespace LouisAgent.Core.Hosting;

/// <summary>
/// What a host's program hands <see cref="AgentHost"/> besides its catalogue (CL33): the telemetry to start once the
/// settings have loaded. What is left out is the Null Object: core alone runs with no tracer.
/// </summary>
/// <param name="Telemetry">The host's telemetry, <c>OtlpTelemetry.Instance</c> from <c>louis-agent.core.telemetry</c>; none for no tracer.</param>
public sealed record HostServices(IHostTelemetry? Telemetry = null)
{
    /// <summary>Core alone: no tracer.</summary>
    public static readonly HostServices None = new();

    /// <summary>The telemetry to use: the one given, else the one that does nothing.</summary>
    public IHostTelemetry TelemetryOrNone => Telemetry ?? NoTelemetry.Instance;
}
