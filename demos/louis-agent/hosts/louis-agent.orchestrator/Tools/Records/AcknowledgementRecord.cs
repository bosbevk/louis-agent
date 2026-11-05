using System.Text.Json.Serialization;

namespace LouisAgent.Orchestrator.Tools.Records;

/// <summary>One line of acknowledged.jsonl: an error the runbook calls expected, so nobody needs to act.</summary>
/// <remarks>A named record, not an anonymous object (ADR 0041), snake_case like every orchestrator file.</remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
internal sealed record AcknowledgementRecord(
    [property: JsonPropertyName("error_id")] string ErrorId,
    [property: JsonPropertyName("reason")] string Reason,
    [property: JsonPropertyName("at")] DateTimeOffset At);
