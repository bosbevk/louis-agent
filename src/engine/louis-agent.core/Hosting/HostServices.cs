using LouisAgent.Core.Config;
using LouisAgent.Core.Sessions.Replay.Store;
using LouisAgent.Core.Telemetry;
using LouisAgent.Core.Web;

namespace LouisAgent.Core.Hosting;

/// <summary>
/// What a host's program hands <see cref="AgentHost"/> besides its catalogue (CL33): how to build the web access
/// from the settings and the replay store, and the telemetry to start once the settings have loaded. What is left
/// out is the Null Object: core alone runs with no web and no tracer.
/// </summary>
/// <param name="Web">Builds the web access, <c>WebAccess.Create</c> from <c>louis-agent.core.web</c>; none for no web.</param>
/// <param name="Telemetry">The host's telemetry, <c>OtlpTelemetry.Instance</c> from <c>louis-agent.core.telemetry</c>; none for no tracer.</param>
public sealed record HostServices(
    Func<AgentOptions, ReplayStore?, IWebAccess>? Web = null,
    IHostTelemetry? Telemetry = null)
{
    /// <summary>Core alone: no web, no tracer.</summary>
    public static readonly HostServices None = new();

    /// <summary>The telemetry to use: the one given, else the one that does nothing.</summary>
    public IHostTelemetry TelemetryOrNone => Telemetry ?? NoTelemetry.Instance;

    /// <summary>The web access for these settings and this store: the host's, else the one that says it isn't set up.</summary>
    public IWebAccess WebOrNone(AgentOptions options, ReplayStore? replay) =>
        Web?.Invoke(options, replay) ?? NoWebAccess.Instance;
}
