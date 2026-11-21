using System.Text.Json.Serialization;

namespace LouisAgent.Packs.Triage.Feed;

/// <summary>
/// One exception a service reported, the shape an error tracker gives and the sample service writes as a JSON line.
/// </summary>
/// <remarks>
/// A record with the log's snake_case names (ADR 0041), moved from the orchestrator (F106.3.1) so every department's
/// workflow and tool reads the same error.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public sealed record ServiceError(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("timestamp")]
    DateTimeOffset Timestamp,
    [property: JsonPropertyName("service")]
    string Service,
    [property: JsonPropertyName("method")] string Method,
    [property: JsonPropertyName("arguments")]
    string Arguments,
    [property: JsonPropertyName("exception_type")]
    string ExceptionType,
    [property: JsonPropertyName("message")]
    string Message,
    [property: JsonPropertyName("stack_trace")]
    string? StackTrace);
