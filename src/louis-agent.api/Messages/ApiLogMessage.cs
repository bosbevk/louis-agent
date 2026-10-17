namespace LouisAgent.Api.Messages;

using System.ComponentModel;

/// <summary>The API's log lines; written to stderr.</summary>
public enum ApiLogMessage
{
    [Description("AGENT_API_KEY not set; the API accepts unauthenticated requests. Only expose it on localhost.")]
    NoApiKey,

    [Description("Turn failed in {Session}")]
    TurnFailed,

    [Description("Benchmark run {Run}: the store failed, so recording stopped; the run goes on")]
    BenchmarkStoreFailed,
}
