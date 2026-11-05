using System.Diagnostics;
using LouisAgent.Core.Logging;
using OpenTelemetry;

namespace LouisAgent.Core.Telemetry;

/// <summary>
/// Leaves our calls to the collector out of the traces (F52 step 4b): an <c>HttpClient</c> span whose address starts
/// with the OTLP endpoint is not exported. The trace exporter's own sends are already hidden by the SDK; the log
/// sink's are not, as it sends on its own client, so without this every batch of log lines would be a span.
/// </summary>
/// <remarks>
/// A filter stage in the export pipeline (Pipes and Filters, Hohpe and Woolf): the SDK hands each ended span to its
/// processors before the exporter, and this one drops a span the way the SDK documents, by marking it not recorded.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0064-observability-through-opentelemetry.md">ADR 0064: observability through OpenTelemetry</seealso>
internal sealed class OwnTrafficFilter : BaseProcessor<Activity>
{
    /// <summary>The source .NET's <c>HttpClient</c> traces its calls on.</summary>
    private const string HttpSource = "System.Net.Http";

    /// <summary>The collector's address, read once; every call to it starts with it.</summary>
    private readonly string? _endpoint =
        Environment.GetEnvironmentVariable(AgentLogging.OtlpEndpointVariable)?.Trim().TrimEnd('/');

    /// <summary>Drops a span that is a call to the collector; every other passes untouched.</summary>
    public override void OnEnd(Activity data)
    {
        if (_endpoint is null || data.Source.Name != HttpSource) return;
        // The address's tag is url.full since .NET 8, http.url before; either names a call to the collector.
        string? url = data.GetTagItem("url.full") as string ?? data.GetTagItem("http.url") as string;
        if (url is null || !url.StartsWith(_endpoint, StringComparison.OrdinalIgnoreCase)) return;

        data.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
        data.IsAllDataRequested = false;
    }
}
