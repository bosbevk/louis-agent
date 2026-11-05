using System.Diagnostics;
using LouisAgent.Core.Logging;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace LouisAgent.Core.Telemetry;

/// <summary>
/// Sends a process's spans to an OpenTelemetry collector, when <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> names one: every
/// model request and tool call Microsoft.Extensions.AI traces, every request ASP.NET Core serves and
/// <c>HttpClient</c> sends, and our own spans, named for the host. Every host starts it once, right after its
/// settings load; with no endpoint nothing listens, so a host with no collector pays nothing for its spans. At exit
/// the last spans and log lines go out, waiting a few seconds at most, and a crash is logged once first
/// (<see cref="LastChance"/>), collector or not.
/// </summary>
/// <remarks>
/// Composition Root (Seemann): the one entry a process calls while it is put together, so no host builds a tracer of
/// its own and the sources, the resource and the exporter are chosen in one place. The exporter reads the endpoint,
/// protocol and headers from the environment itself (the spec's settings), so on Azure the managed agent's injected
/// ones work with nothing of ours. The handle is the process's one: a second start gets it again.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0064-observability-through-opentelemetry.md">ADR 0064: observability through OpenTelemetry</seealso>
public static class AgentTelemetry
{
    /// <summary>Our own spans' source: a turn opens its span here (step 3).</summary>
    public static readonly ActivitySource Source = new("louis-agent");

    /// <summary>
    /// What the tracer listens to: ours, Microsoft.Extensions.AI's model requests and tool calls, and .NET's own for
    /// the requests a web host serves and every <c>HttpClient</c> call (which also carries the trace to a peer).
    /// </summary>
    private static readonly string[] Sources =
        [Source.Name, "Experimental.Microsoft.Extensions.AI", "Microsoft.AspNetCore", "System.Net.Http"];

    /// <summary>How long a flush waits for a collector, so a dead one never holds a host's exit.</summary>
    private static readonly TimeSpan FlushWait = TimeSpan.FromSeconds(3);

    /// <summary>Guards the handle, which a host starts once but a test starts again and again.</summary>
    private static readonly Lock Gate = new();

    /// <summary>The process's tracer, while one is started.</summary>
    private static Tracing? _current;

    /// <summary>The last spans and log lines go out when the process ends.</summary>
    static AgentTelemetry() => AppDomain.CurrentDomain.ProcessExit += OnExit;

    /// <summary>
    /// Starts tracing for this process, or hands back the tracing already started. Disposing it sends what is left and
    /// stops the tracer; the process's exit does that too.
    /// </summary>
    public static IDisposable Start()
    {
        lock (Gate)
        {
            // Before the tracer, so a host with no collector still logs a crash.
            LastChance.Install();
            return _current ??= new Tracing(Build());
        }
    }

    /// <summary>Sends the spans the tracer still holds, waiting a few seconds at most.</summary>
    internal static void Flush()
    {
        Tracing? current;
        lock (Gate) current = _current;
        current?.Send();
    }

    /// <summary>
    /// The tracer over our sources with the OTLP exporter, or none without an endpoint. The service name is ours first
    /// and then the environment's, so <c>OTEL_SERVICE_NAME</c> and <c>OTEL_RESOURCE_ATTRIBUTES</c> win. The calls to
    /// the collector itself are filtered out before the exporter sees them.
    /// </summary>
    private static TracerProvider? Build()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(AgentLogging.OtlpEndpointVariable)))
            return null;

        return Sdk.CreateTracerProviderBuilder()
            .AddSource(Sources)
            .SetResourceBuilder(ResourceBuilder.CreateEmpty().AddService(AgentLogging.ServiceName)
                .AddEnvironmentVariableDetector())
            // Before the exporter: processors run in the order they're added.
            .AddProcessor(new OwnTrafficFilter())
            .AddOtlpExporter()
            .Build();
    }

    /// <summary>At exit, what the tracer and the logs still hold goes out.</summary>
    private static void OnExit(object? sender, EventArgs e)
    {
        Tracing? current;
        lock (Gate) current = _current;
        current?.Dispose();
        AgentLogging.Flush();
    }

    /// <summary>The process's tracing: its tracer, none when there is no collector.</summary>
    /// <remarks>A class, not a record: it owns the tracer and stops it (ADR 0040).</remarks>
    private sealed class Tracing(TracerProvider? tracer) : IDisposable
    {
        /// <summary>Sends what the tracer holds, waiting a few seconds at most.</summary>
        public void Send() => tracer?.ForceFlush((int)FlushWait.TotalMilliseconds);

        /// <summary>Sends what is left, stops the tracer, and lets a later start begin again.</summary>
        public void Dispose()
        {
            lock (Gate)
                if (_current == this)
                    _current = null;

            // A shutdown with a limit, so a dead collector can't hold the exit; disposing after it is then quick.
            tracer?.Shutdown((int)FlushWait.TotalMilliseconds);
            tracer?.Dispose();
        }
    }
}
