namespace LouisAgent.Api.Messages;

using System.ComponentModel;

/// <summary>The API's log lines; written to stderr.</summary>
public enum ApiLogMessage
{
    [Description("AGENT_AUTH=off: no AGENT_API_KEY, so the API accepts unauthenticated requests, on purpose. Only expose it on localhost.")]
    NoApiKey,

    [Description("Turn failed in {Session}")]
    TurnFailed,

    [Description("Benchmark run {Run}: the store failed, so recording stopped; the run goes on")]
    BenchmarkStoreFailed,

    [Description("{Count} benchmark run(s) a restart cut off are marked failed")]
    BenchmarkOrphansClosed,

    [Description("The benchmark store isn't answering at start-up; a run will say so when it can't write")]
    BenchmarkStoreUnreachable,

    [Description("the eval runner can't be listed ({Reason}), so the Python checks stay")]
    BenchmarkListingUnavailable,
}
